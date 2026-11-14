namespace Projeta.Core.Alinhamentos;

/// <summary>Eixo em polilinha com estaca (s) e afastamento (v, positivo à ESQUERDA do sentido da polilinha).</summary>
public sealed class PolilinhaEixo
{
    private readonly (double X, double Y)[] _p;
    private readonly double[] _s;

    public PolilinhaEixo(IReadOnlyList<(double X, double Y)> pontos)
    {
        var lista = new List<(double X, double Y)>();
        foreach (var q in pontos)
            if (lista.Count == 0 || Math.Abs(q.X - lista[^1].X) + Math.Abs(q.Y - lista[^1].Y) > 1e-9) lista.Add(q);
        if (lista.Count < 2) throw new ArgumentException("A polilinha precisa de 2 pontos distintos.");
        _p = lista.ToArray();
        _s = new double[_p.Length];
        for (int i = 1; i < _p.Length; i++) _s[i] = _s[i - 1] + Math.Sqrt(Sq(_p[i].X - _p[i - 1].X) + Sq(_p[i].Y - _p[i - 1].Y));
    }

    private static double Sq(double v) => v * v;
    public double Comprimento => _s[^1];
    public IReadOnlyList<(double X, double Y)> Pontos => _p;

    private int Segmento(double s)
    {
        if (s <= 0) return 0;
        if (s >= _s[^1]) return _p.Length - 2;
        int lo = 0, hi = _p.Length - 1;
        while (hi - lo > 1) { int m = (lo + hi) / 2; if (_s[m] <= s) lo = m; else hi = m; }
        return lo;
    }

    /// <summary>Direção (unitária) do segmento que contém a estaca (estende as pontas em reta).</summary>
    public (double Dx, double Dy) Direcao(double s)
    {
        int i = Segmento(s);
        double dx = _p[i + 1].X - _p[i].X, dy = _p[i + 1].Y - _p[i].Y, n = Math.Sqrt(dx * dx + dy * dy);
        return (dx / n, dy / n);
    }

    /// <summary>Ponto na estaca s e afastamento v (esquerda +); além das pontas, segue em reta.</summary>
    public (double X, double Y) Ponto(double s, double v)
    {
        int i = Segmento(s);
        var (dx, dy) = Direcao(s);
        double t = s - _s[i];
        return (_p[i].X + dx * t - dy * v, _p[i].Y + dy * t + dx * v);
    }

    /// <summary>Estaca e afastamento do ponto mais próximo (com as pontas prolongadas em reta).</summary>
    public (double S, double V) Projetar(double x, double y)
    {
        double melhor = double.MaxValue, sb = 0, vb = 0;
        int n = _p.Length - 1;
        for (int i = 0; i < n; i++)
        {
            double ax = _p[i].X, ay = _p[i].Y, dx = _p[i + 1].X - ax, dy = _p[i + 1].Y - ay;
            double l2 = dx * dx + dy * dy, l = Math.Sqrt(l2);
            double t = ((x - ax) * dx + (y - ay) * dy) / l2;
            double tc = i == 0 ? Math.Min(t, 1) : i == n - 1 ? Math.Max(t, 0) : Math.Clamp(t, 0, 1);
            if (n == 1) tc = t;
            double px = ax + dx * tc, py = ay + dy * tc;
            double d2 = Sq(x - px) + Sq(y - py);
            if (d2 < melhor)
            {
                melhor = d2;
                sb = _s[i] + tc * l;
                vb = (dx * (y - ay) - dy * (x - ax)) / l;
            }
        }
        return (sb, vb);
    }
}

/// <summary>Um braço da rotatória visto pelo cálculo: eixo do ramo saindo do anel, bordos, meios-fios das conexões e ilha.</summary>
public sealed class BracoRotatoria
{
    public string Nome { get; set; } = "";
    public string Rumo { get; set; } = "";
    /// <summary>Posição angular do braço no anel (rad, do leste, anti-horário).</summary>
    public double Angulo { get; set; }
    /// <summary>Eixo do ramo começando no eixo do anel (s = 0) e indo para fora. Esquerda (+) = lado anti-horário = faixa de entrada.</summary>
    public PolilinhaEixo Eixo { get; set; } = null!;
    /// <summary>Estaca (no eixo do braço) onde ele cruza o bordo externo do anel: a linha de retenção.</summary>
    public double SRetencao { get; set; }
    /// <summary>Distância do eixo ao meio-fio junto da rotatória: lado da entrada (esquerda, +) e da saída (direita).</summary>
    public double BordoEntrada { get; set; }
    public double BordoSaida { get; set; }
    /// <summary>Meia largura normal do ramo (fora do alargamento); ≤ 0 = sem estreitamento.</summary>
    public double BordoNormal { get; set; }
    /// <summary>O alargamento vai de BordoNormal (em SAlargFim) até o bordo da rotatória (em SAlargIni).</summary>
    public double SAlargIni { get; set; } = double.PositiveInfinity;
    public double SAlargFim { get; set; } = double.PositiveInfinity;
    /// <summary>Meio-fio da conexão de entrada e de saída (centro e raio do arco); null = não coube.</summary>
    public (double X, double Y, double R)? MeioFioEntrada { get; set; }
    public (double X, double Y, double R)? MeioFioSaida { get; set; }
    /// <summary>Estaca no eixo do braço onde cada conexão tangencia o bordo do ramo.</summary>
    public double STangEntrada { get; set; } = double.NaN;
    public double STangSaida { get; set; } = double.NaN;
    /// <summary>Ilha divisória: de SIlhaIni (junto do anel) a SIlhaFim (nariz), largura variando linear.</summary>
    public double SIlhaIni { get; set; }
    public double SIlhaFim { get; set; }
    public double IlhaLarguraIni { get; set; }
    public double IlhaLarguraFim { get; set; }

    public bool TemIlha => IlhaLarguraIni > 0 && SIlhaFim > SIlhaIni;

    public double MeiaIlha(double s)
    {
        if (!TemIlha || s < SIlhaIni || s > SIlhaFim) return 0;
        double t = (s - SIlhaIni) / (SIlhaFim - SIlhaIni);
        return (IlhaLarguraIni + (IlhaLarguraFim - IlhaLarguraIni) * t) / 2;
    }

    /// <summary>Distância do eixo ao meio-fio na estaca s (com o estreitamento até a largura normal).</summary>
    public double Bordo(double s, bool entrada)
    {
        double b = entrada ? BordoEntrada : BordoSaida;
        if (BordoNormal <= 0 || BordoNormal >= b || s <= SAlargIni) return b;
        if (s >= SAlargFim) return BordoNormal;
        return b + (BordoNormal - b) * (s - SAlargIni) / (SAlargFim - SAlargIni);
    }
}

/// <summary>Geometria plana da rotatória para a trajetória mais rápida e o desenho de ilhas e bordos.</summary>
public sealed class ModeloRotatoria
{
    public double Ox { get; set; }
    public double Oy { get; set; }
    /// <summary>Raio do bordo externo do anel (DCI/2).</summary>
    public double RExt { get; set; }
    /// <summary>Raio do meio-fio da ilha central (sem a faixa transponível).</summary>
    public double RIlha { get; set; }
    /// <summary>Braços em ordem anti-horária.</summary>
    public List<BracoRotatoria> Bracos { get; } = new();
    /// <summary>Afastamento do centro do veículo a meio-fio e ilhas (NCHRP 672: 1,5 m).</summary>
    public double AfastMeioFio { get; set; } = 1.5;
    /// <summary>Afastamento ao eixo da via (divisa com o tráfego oposto; NCHRP 672: 1,5 m).</summary>
    public double AfastEixo { get; set; } = 1.5;
    /// <summary>A trajetória começa e termina a esta distância da linha de retenção (NCHRP 672: pelo menos 50 m).</summary>
    public double Antes { get; set; } = 50;
    /// <summary>Recuo da ponta da ilha divisória em relação ao bordo externo do anel.</summary>
    public double RecuoIlha { get; set; } = 1;

    /// <summary>Faixa angular (anti-horária, de/até) onde o braço abre o bordo externo do anel.</summary>
    public (double De, double Ate) Abertura(BracoRotatoria b)
    {
        double ang(double x, double y) => GeometriaRotatoria.Normalizar(Math.Atan2(y - Oy, x - Ox));
        double de = b.MeioFioSaida is { } s ? ang(s.X, s.Y) : GeometriaRotatoria.Normalizar(b.Angulo - Math.Asin(Math.Min(1, b.BordoSaida / RExt)));
        double ate = b.MeioFioEntrada is { } e ? ang(e.X, e.Y) : GeometriaRotatoria.Normalizar(b.Angulo + Math.Asin(Math.Min(1, b.BordoEntrada / RExt)));
        return (de, ate);
    }

    public bool NaAbertura(BracoRotatoria b, double angulo)
    {
        var (de, ate) = Abertura(b);
        double largura = GeometriaRotatoria.Normalizar(ate - de);
        return GeometriaRotatoria.Normalizar(angulo - de) <= largura;
    }

    /// <summary>Polígono (fechado) da ilha divisória: lados acompanhando o eixo, ponta junto do anel em arco e nariz arredondado.</summary>
    public List<(double X, double Y)> Ilha(BracoRotatoria b)
    {
        var r = new List<(double X, double Y)>();
        if (!b.TemIlha) return r;
        double rr = RExt + RecuoIlha;
        // estaca, de cada lado, onde o lado da ilha encontra o círculo do recuo
        double Inicio(int lado)
        {
            double a = 0, c = b.SIlhaFim;
            for (int i = 0; i < 60; i++)
            {
                double m = (a + c) / 2;
                var p = b.Eixo.Ponto(m, lado * b.MeiaIlha(Math.Max(m, b.SIlhaIni)));
                if (Math.Sqrt((p.X - Ox) * (p.X - Ox) + (p.Y - Oy) * (p.Y - Oy)) < rr) a = m; else c = m;
            }
            return (a + c) / 2;
        }
        double ini1 = Inicio(+1), ini2 = Inicio(-1);
        double w(double s) => b.MeiaIlha(Math.Clamp(s, b.SIlhaIni, b.SIlhaFim));
        double fimLados = b.SIlhaFim - b.IlhaLarguraFim / 2;
        // lado esquerdo (+), do anel para fora
        for (double s = ini1; s < fimLados; s += 1) r.Add(b.Eixo.Ponto(s, w(s)));
        // nariz: semicírculo de raio = meia largura final
        double rn = Math.Max(b.IlhaLarguraFim / 2, 0.05);
        var c0 = b.Eixo.Ponto(fimLados, 0);
        var (dx, dy) = b.Eixo.Direcao(fimLados);
        double a0 = Math.Atan2(dx, -dy); // direção da esquerda (−dy, dx)
        for (int k = 0; k <= 12; k++)
        {
            double a = a0 - Math.PI * k / 12;
            r.Add((c0.X + rn * Math.Cos(a), c0.Y + rn * Math.Sin(a)));
        }
        // lado direito (−), de fora para o anel
        var dir = new List<(double X, double Y)>();
        for (double s = ini2; s < fimLados; s += 1) dir.Add(b.Eixo.Ponto(s, -w(s)));
        dir.Reverse();
        r.AddRange(dir);
        // ponta junto do anel: arco do círculo do recuo, do lado direito ao esquerdo
        var pd = b.Eixo.Ponto(ini2, -w(ini2));
        var pe = b.Eixo.Ponto(ini1, w(ini1));
        double ad = Math.Atan2(pd.Y - Oy, pd.X - Ox), ae = Math.Atan2(pe.Y - Oy, pe.X - Ox);
        double delta = ae - ad;
        while (delta > Math.PI) delta -= 2 * Math.PI;
        while (delta < -Math.PI) delta += 2 * Math.PI;
        for (int k = 1; k < 8; k++)
        {
            double a = ad + delta * k / 8;
            r.Add((Ox + rr * Math.Cos(a), Oy + rr * Math.Sin(a)));
        }
        return r;
    }

    /// <summary>Meio-fio do ramo de um lado, da tangência da conexão até <paramref name="ate"/> (estaca no eixo do braço).</summary>
    public List<(double X, double Y)> BordoRamo(BracoRotatoria b, bool entrada, double ate)
    {
        var r = new List<(double X, double Y)>();
        double de = entrada ? b.STangEntrada : b.STangSaida;
        if (double.IsNaN(de)) de = b.SRetencao;
        int lado = entrada ? 1 : -1;
        for (double s = de; s < ate; s += 1) r.Add(b.Eixo.Ponto(s, lado * b.Bordo(s, entrada)));
        r.Add(b.Eixo.Ponto(ate, lado * b.Bordo(ate, entrada)));
        return r;
    }

    /// <summary>Linha de retenção (dê a preferência): arco do bordo externo da ilha (ou do eixo) até o meio-fio da entrada.</summary>
    public List<(double X, double Y)> Retencao(BracoRotatoria b)
    {
        var r = new List<(double X, double Y)>();
        var p0 = b.Eixo.Ponto(b.SRetencao, b.TemIlha ? b.MeiaIlha(Math.Max(b.SRetencao, b.SIlhaIni)) : 0);
        double a0 = Math.Atan2(p0.Y - Oy, p0.X - Ox);
        double a1 = b.MeioFioEntrada is { } e ? Math.Atan2(e.Y - Oy, e.X - Ox) : a0 + b.BordoEntrada / RExt;
        double delta = a1 - a0;
        while (delta > Math.PI) delta -= 2 * Math.PI;
        while (delta < -Math.PI) delta += 2 * Math.PI;
        for (int k = 0; k <= 10; k++)
        {
            double a = a0 + delta * k / 10;
            r.Add((Ox + RExt * Math.Cos(a), Oy + RExt * Math.Sin(a)));
        }
        return r;
    }
}

/// <summary>Movimento analisado: entra por um braço e sai por outro.</summary>
public enum Movimento { Direto, Esquerda, Direita }

/// <summary>Resultado da trajetória mais rápida de um movimento.</summary>
public sealed record Trajetoria(Movimento Tipo, int Entrada, int Saida, List<(double X, double Y)> Pontos,
                                List<double> Raios, int Iteracoes, double Violacao);

/// <summary>Raio crítico achado numa trajetória (ponto onde ocorre, para rotular o desenho).</summary>
public sealed record RaioCritico(string Nome, double Raio, double Velocidade, (double X, double Y) Ponto);

/// <summary>R1 a R5 de um braço (NCHRP 672) e as velocidades.</summary>
public sealed record AnaliseBraco(int Braco, RaioCritico? R1, RaioCritico? R2, RaioCritico? R3, RaioCritico? R4, RaioCritico? R5,
                                  Trajetoria? Direto, Trajetoria? Esquerda, Trajetoria? Direita)
{
    /// <summary>Movimentos cuja trajetória não coube nos afastamentos (meios-fios sobrepostos, faixa estreita demais): os raios dela não valem.</summary>
    public List<string> Problemas { get; init; } = new();
}

/// <summary>
/// Trajetória mais rápida pela rotatória (NCHRP 672): caminho mais suave possível que respeita 1,5 m dos meios-fios, das
/// ilhas e do eixo da via, começando e terminando a 50 m das linhas de retenção. Calculada como a curva de menor energia
/// de flexão (Σ curvatura²) com as pontas e as direções das pontas fixas, por passos implícitos (sistema pentadiagonal)
/// alternados com projeção nas restrições. Os raios saem do arco de 3 pontos ao longo da curva.
/// </summary>
public static class TrajetoriaRapida
{
    /// <summary>NCHRP 672 (ajuste dos dados do AASHTO): V = 8,7602·R^0,3861 (e = +0,02) e V = 8,6164·R^0,3673 (e = −0,02), V em km/h, R em m.</summary>
    public static double Velocidade(double raio, bool superelevacaoPositiva) =>
        double.IsInfinity(raio) || raio <= 0 ? double.PositiveInfinity
        : superelevacaoPositiva ? 8.7602 * Math.Pow(raio, 0.3861) : 8.6164 * Math.Pow(raio, 0.3673);

    /// <summary>Braço de saída de cada movimento a partir do braço de entrada (ordem anti-horária, mão direita).</summary>
    public static int SaidaDe(ModeloRotatoria m, int entrada, Movimento mov)
    {
        int n = m.Bracos.Count;
        if (n < 2) return -1;
        if (mov == Movimento.Direita) return (entrada + 1) % n;
        if (mov == Movimento.Esquerda) return (entrada + n - 1) % n;
        double alvo = m.Bracos[entrada].Angulo + Math.PI;
        int melhor = -1; double dm = double.MaxValue;
        for (int k = 0; k < n; k++)
        {
            if (k == entrada) continue;
            double d = Math.Abs(GeometriaRotatoria.Normalizar(m.Bracos[k].Angulo - alvo + Math.PI) - Math.PI);
            if (d < dm) { dm = d; melhor = k; }
        }
        return melhor;
    }

    /// <summary>R1–R5 de um braço: direto (R1 entrada, R2 em volta da ilha, R3 saída), à esquerda (R4) e à direita (R5).</summary>
    public static AnaliseBraco Analisar(ModeloRotatoria m, int braco, double comprimentoArco = 10, double passo = 1)
    {
        Trajetoria? dir = null, esq = null, dta = null;
        RaioCritico? r1 = null, r2 = null, r3 = null, r4 = null, r5 = null;
        var problemas = new List<string>();
        string Para(int k) => $"{m.Bracos[braco].Rumo}→{m.Bracos[k].Rumo}";
        // o menor raio possível de verdade: contornando a ilha central ou um meio-fio de conexão a 1,5 m
        double rPossivel = m.RIlha + m.AfastMeioFio;
        foreach (var b in m.Bracos)
        {
            if (b.MeioFioEntrada is { } e) rPossivel = Math.Min(rPossivel, e.R + m.AfastMeioFio);
            if (b.MeioFioSaida is { } s) rPossivel = Math.Min(rPossivel, s.R + m.AfastMeioFio);
        }
        bool Valida(Trajetoria t, string mov)
        {
            if (t.Violacao > ToleranciaViolacao)
            {
                problemas.Add($"{mov} ({Para(t.Saida)}): a trajetória não cabe nos afastamentos (fica {t.Violacao:0.00} m fora) — meios-fios sobrepostos, conexão que invade o ramo vizinho ou faixa estreita demais");
                return false;
            }
            double rmin = t.Raios.Where(x => !double.IsInfinity(x)).Select(Math.Abs).DefaultIfEmpty(double.PositiveInfinity).Min();
            if (rmin < 0.7 * rPossivel)
            {
                problemas.Add($"{mov} ({Para(t.Saida)}): não há caminho suave entre os meios-fios (a trajetória fica com quina, raio {rmin:0.0} m) — meios-fios sobrepostos ou conexão que invade o ramo vizinho");
                return false;
            }
            return true;
        }
        int sd = SaidaDe(m, braco, Movimento.Direto);
        if (sd >= 0)
        {
            dir = Calcular(m, braco, sd, Movimento.Direto, comprimentoArco, passo);
            if (Valida(dir, "direto")) (r1, r2, r3) = RaiosDireto(m, dir);
        }
        int se = SaidaDe(m, braco, Movimento.Esquerda);
        if (se >= 0 && m.Bracos.Count >= 3)
        {
            esq = Calcular(m, braco, se, Movimento.Esquerda, comprimentoArco, passo);
            if (Valida(esq, "à esquerda")) r4 = Minimo("R4", esq, +1, 0, esq.Pontos.Count - 1, false);
        }
        int sr = SaidaDe(m, braco, Movimento.Direita);
        if (sr >= 0 && m.Bracos.Count >= 3)
        {
            dta = Calcular(m, braco, sr, Movimento.Direita, comprimentoArco, passo);
            if (Valida(dta, "à direita")) r5 = Minimo("R5", dta, -1, 0, dta.Pontos.Count - 1, true);
        }
        return new AnaliseBraco(braco, r1, r2, r3, r4, r5, dir, esq, dta) { Problemas = problemas };
    }

    private static (RaioCritico?, RaioCritico?, RaioCritico?) RaiosDireto(ModeloRotatoria m, Trajetoria t)
    {
        var p = t.Pontos;
        int entra = p.FindIndex(q => Dist(q, m.Ox, m.Oy) < m.RExt);
        int sai = p.FindLastIndex(q => Dist(q, m.Ox, m.Oy) < m.RExt);
        if (entra < 0) { entra = p.Count / 3; sai = 2 * p.Count / 3; }
        var r2 = Minimo("R2", t, +1, entra, sai, false);
        int k2 = r2 is null ? (entra + sai) / 2 : p.FindIndex(q => q == r2.Ponto);
        var r1 = Minimo("R1", t, -1, 0, entra, true);
        var r3 = Minimo("R3", t, -1, Math.Max(k2, 0), p.Count - 1, true);
        return (r1, r2, r3);
    }

    /// <summary>Menor raio com curvatura do sinal pedido (+1 esquerda, −1 direita) entre os índices.</summary>
    private static RaioCritico? Minimo(string nome, Trajetoria t, int sinal, int de, int ate, bool ePositivo)
    {
        double melhor = double.PositiveInfinity; int k = -1;
        for (int i = Math.Max(de, 0); i <= Math.Min(ate, t.Raios.Count - 1); i++)
        {
            double r = t.Raios[i];
            if (double.IsInfinity(r) || Math.Sign(r) != sinal) continue;
            if (Math.Abs(r) < melhor) { melhor = Math.Abs(r); k = i; }
        }
        if (k < 0) return null;
        return new RaioCritico(nome, melhor, Velocidade(melhor, ePositivo), t.Pontos[k]);
    }

    private static double Dist((double X, double Y) p, double x, double y) => Math.Sqrt((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y));

    /// <summary>Raio assinado (+ curva à esquerda) do arco de 3 pontos de comprimento <paramref name="arco"/> centrado em cada ponto.</summary>
    public static List<double> Raios(IReadOnlyList<(double X, double Y)> p, double arco, double passo)
    {
        int w = Math.Max(1, (int)Math.Round(arco / 2 / passo));
        var r = new List<double>(p.Count);
        for (int k = 0; k < p.Count; k++)
        {
            if (k - w < 0 || k + w >= p.Count) { r.Add(double.PositiveInfinity); continue; }
            var a = p[k - w]; var b = p[k]; var c = p[k + w];
            double ab = Dist(a, b.X, b.Y), bc = Dist(b, c.X, c.Y), ca = Dist(c, a.X, a.Y);
            double cruz = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            r.Add(Math.Abs(cruz) < 1e-9 ? double.PositiveInfinity : Math.Sign(cruz) * ab * bc * ca / (2 * Math.Abs(cruz)));
        }
        return r;
    }

    /// <summary>Trajetória mais rápida de um movimento (entrada → saída).</summary>
    public static Trajetoria Calcular(ModeloRotatoria m, int entrada, int saida, Movimento tipo, double comprimentoArco = 10, double passo = 1)
    {
        var bi = m.Bracos[entrada];
        var bo = m.Bracos[saida];
        // pontas: a 50 m das linhas de retenção, encostadas no limite junto do eixo da via (lado de dentro da faixa)
        double si = bi.SRetencao + m.Antes, so = bo.SRetencao + m.Antes;
        var p0 = bi.Eixo.Ponto(si, m.AfastEixo);
        var p1 = bi.Eixo.Ponto(si - passo, m.AfastEixo);
        var pn = bo.Eixo.Ponto(so, -m.AfastEixo);
        var pn1 = bo.Eixo.Ponto(so - passo, -m.AfastEixo);

        // caminho inicial: pelo meio da faixa de entrada, pelo meio do anel (anti-horário) e pelo meio da faixa de saída
        var ini = new List<(double X, double Y)>();
        for (double s = si; s > bi.SRetencao; s -= passo)
            ini.Add(bi.Eixo.Ponto(s, (bi.MeiaIlha(s) + Math.Max(bi.Bordo(s, true), bi.MeiaIlha(s) + 2 * m.AfastMeioFio)) / 2));
        double rm = (m.RIlha + m.RExt) / 2;
        var pa = ini[^1];
        double a0 = Math.Atan2(pa.Y - m.Oy, pa.X - m.Ox);
        var pb = bo.Eixo.Ponto(bo.SRetencao, -(bo.MeiaIlha(bo.SRetencao) + bo.Bordo(bo.SRetencao, false)) / 2);
        double a1 = Math.Atan2(pb.Y - m.Oy, pb.X - m.Ox);
        double da = GeometriaRotatoria.Normalizar(a1 - a0);
        if (da < 0.05) da += 2 * Math.PI;
        int narco = Math.Max(4, (int)(da * rm / passo));
        for (int k = 0; k <= narco; k++) { double a = a0 + da * k / narco; ini.Add((m.Ox + rm * Math.Cos(a), m.Oy + rm * Math.Sin(a))); }
        for (double s = bo.SRetencao; s <= so; s += passo)
            ini.Add(bo.Eixo.Ponto(s, -(bo.MeiaIlha(s) + Math.Max(bo.Bordo(s, false), bo.MeiaIlha(s) + 2 * m.AfastMeioFio)) / 2));
        ini[0] = p0; ini.Add(pn);
        var pts = ini;
        var restr = new Restricoes(m, bi, bo);
        int it = 0;
        const double tau = 4;
        // do grosso para o fino: com pontos espaçados a curva longa converge em poucas iterações; o passo pequeno de τ
        // mantém a curva contínua (não pula para o outro lado da ilha central — o giro é sempre anti-horário)
        foreach (double h in new[] { 8 * passo, 4 * passo, 2 * passo, passo })
        {
            p1 = bi.Eixo.Ponto(si - h, m.AfastEixo);
            pn1 = bo.Eixo.Ponto(so - h, -m.AfastEixo);
            pts = Reamostrar(pts, h);
            Fixar(pts, p0, p1, pn1, pn);
            for (int k = 2; k < pts.Count - 2; k++) pts[k] = restr.Projetar(pts[k]);
            double eAnt = double.MaxValue;
            int parado = 0;
            for (int i = 0; i < MaxIteracoes; i++, it++)
            {
                PassoImplicito(pts, tau);
                for (int k = 2; k < pts.Count - 2; k++) pts[k] = restr.Projetar(pts[k]);
                if (i % 5 == 4) { pts = Reamostrar(pts, h); Fixar(pts, p0, p1, pn1, pn); for (int k = 2; k < pts.Count - 2; k++) pts[k] = restr.Projetar(pts[k]); }
                // convergência pela energia de flexão (o deslizamento dos pontos ao longo da curva não conta)
                double e = Energia(pts);
                parado = Math.Abs(eAnt - e) <= 1e-5 * e ? parado + 1 : 0;
                eAnt = e;
                if (parado >= 10) break;
            }
        }
        double viol = 0;
        for (int k = 2; k < pts.Count - 2; k++) { var q = restr.Projetar(pts[k]); viol = Math.Max(viol, Dist(q, pts[k].X, pts[k].Y)); }
        return new Trajetoria(tipo, entrada, saida, pts, Raios(pts, comprimentoArco, passo), it, viol);
    }

    /// <summary>Quanto a trajetória final pode ficar fora das restrições (m) e ainda valer.</summary>
    public const double ToleranciaViolacao = 0.05;

    /// <summary>Limite de iterações por nível de refinamento.</summary>
    public static int MaxIteracoes { get; set; } = 800;

    /// <summary>Σ|P[k−1] − 2P[k] + P[k+1]|² / h³ (energia de flexão discreta, independente do passo).</summary>
    public static double Energia(IReadOnlyList<(double X, double Y)> p)
    {
        if (p.Count < 3) return 0;
        double h = Dist(p[1], p[0].X, p[0].Y), e = 0;
        for (int k = 1; k < p.Count - 1; k++)
        {
            double dx = p[k - 1].X - 2 * p[k].X + p[k + 1].X, dy = p[k - 1].Y - 2 * p[k].Y + p[k + 1].Y;
            e += dx * dx + dy * dy;
        }
        return h > 0 ? e / (h * h * h) : e;
    }

    private static void Fixar(List<(double X, double Y)> p, (double X, double Y) p0, (double X, double Y) p1, (double X, double Y) pn1, (double X, double Y) pn)
    {
        p[0] = p0; p[1] = p1; p[^2] = pn1; p[^1] = pn;
    }

    /// <summary>Reamostra a polilinha com passo constante (as pontas ficam).</summary>
    public static List<(double X, double Y)> Reamostrar(IReadOnlyList<(double X, double Y)> p, double passo)
    {
        var acum = new double[p.Count];
        for (int i = 1; i < p.Count; i++) acum[i] = acum[i - 1] + Dist(p[i], p[i - 1].X, p[i - 1].Y);
        double total = acum[^1];
        int n = Math.Max(4, (int)Math.Round(total / passo));
        var r = new List<(double X, double Y)>(n + 1);
        int j = 0;
        for (int k = 0; k <= n; k++)
        {
            double s = total * k / n;
            while (j < p.Count - 2 && acum[j + 1] < s) j++;
            double seg = acum[j + 1] - acum[j];
            double t = seg > 1e-12 ? (s - acum[j]) / seg : 0;
            r.Add((p[j].X + (p[j + 1].X - p[j].X) * t, p[j].Y + (p[j + 1].Y - p[j].Y) * t));
        }
        return r;
    }

    /// <summary>
    /// Um passo implícito da flexão: minimiza |P − P_antes|² + τ·Σ|P[k−1] − 2P[k] + P[k+1]|² com os dois primeiros e os dois
    /// últimos pontos fixos (posição e direção das pontas). Sistema pentadiagonal simétrico, resolvido por eliminação.
    /// </summary>
    public static void PassoImplicito(List<(double X, double Y)> p, double tau)
    {
        int n = p.Count;
        if (n < 6) return;
        // M = BᵀB em banda (desvios −2..+2)
        var a = new double[n, 5];
        for (int i = 0; i < n; i++) a[i, 2] = 1;
        int[] off = { -1, 0, 1 };
        double[] cf = { 1, -2, 1 };
        for (int k = 1; k < n - 1; k++)
            for (int u = 0; u < 3; u++)
                for (int v = 0; v < 3; v++)
                {
                    int i = k + off[u], j = k + off[v];
                    a[i, j - i + 2] += tau * cf[u] * cf[v];
                }
        // sistema reduzido nos livres 2..n−3
        int m = n - 4;
        var bx = new double[m]; var by = new double[m];
        var band = new double[m, 5];
        for (int r = 0; r < m; r++)
        {
            int i = r + 2;
            bx[r] = p[i].X; by[r] = p[i].Y;
            for (int d = -2; d <= 2; d++)
            {
                int j = i + d;
                if (j < 0 || j >= n) continue;
                double c = a[i, d + 2];
                if (j < 2 || j > n - 3) { bx[r] -= c * p[j].X; by[r] -= c * p[j].Y; }
                else band[r, d + 2] = c;
            }
        }
        // eliminação gaussiana em banda (simétrica positiva definida, sem pivotamento)
        for (int r = 0; r < m; r++)
        {
            double piv = band[r, 2];
            for (int d = 1; d <= 2 && r + d < m; d++)
            {
                double f = band[r + d, 2 - d] / piv;
                if (f == 0) continue;
                for (int e = 0; e <= 2 && r + e < m; e++) band[r + d, 2 - d + e] -= f * band[r, 2 + e];
                bx[r + d] -= f * bx[r]; by[r + d] -= f * by[r];
            }
        }
        for (int r = m - 1; r >= 0; r--)
        {
            double sx = bx[r], sy = by[r];
            for (int e = 1; e <= 2 && r + e < m; e++) { sx -= band[r, 2 + e] * bx[r + e]; sy -= band[r, 2 + e] * by[r + e]; }
            bx[r] = sx / band[r, 2]; by[r] = sy / band[r, 2];
        }
        for (int r = 0; r < m; r++) p[r + 2] = (bx[r], by[r]);
    }

    /// <summary>Restrições de uma trajetória: ilha central, bordo externo do anel (fora das aberturas dos dois braços), meios-fios das conexões, faixas dos ramos e ilhas divisórias.</summary>
    private sealed class Restricoes
    {
        private readonly ModeloRotatoria _m;
        private readonly BracoRotatoria _in, _out;
        private readonly List<(double X, double Y, double R)> _meiosFios = new();

        private readonly (double De, double Largura) _setorIn, _setorOut;

        public Restricoes(ModeloRotatoria m, BracoRotatoria entrada, BracoRotatoria saida)
        {
            _m = m; _in = entrada; _out = saida;
            foreach (var b in m.Bracos)
            {
                if (b.MeioFioEntrada is { } e) _meiosFios.Add(e);
                if (b.MeioFioSaida is { } s) _meiosFios.Add(s);
            }
            _setorIn = Setor(entrada);
            _setorOut = Setor(saida);
        }

        /// <summary>Setor angular do braço: entre as bissetrizes com o braço anterior e o seguinte (o que está ali é deste ramo).</summary>
        private (double De, double Largura) Setor(BracoRotatoria b)
        {
            var angs = _m.Bracos.Select(x => x.Angulo).OrderBy(x => x).ToList();
            int i = angs.FindIndex(a => Math.Abs(a - b.Angulo) < 1e-12);
            if (angs.Count < 2 || i < 0) return (0, 2 * Math.PI);
            double ant = angs[(i + angs.Count - 1) % angs.Count], prox = angs[(i + 1) % angs.Count];
            double de = b.Angulo - GeometriaRotatoria.Normalizar(b.Angulo - ant) / 2;
            double ate = b.Angulo + GeometriaRotatoria.Normalizar(prox - b.Angulo) / 2;
            if (angs.Count == 2) { de = b.Angulo - Math.PI / 2; ate = b.Angulo + Math.PI / 2; }
            return (GeometriaRotatoria.Normalizar(de), ate - de);
        }

        private static bool NoSetor((double De, double Largura) s, double ang) => GeometriaRotatoria.Normalizar(ang - s.De) <= s.Largura;

        public (double X, double Y) Projetar((double X, double Y) q)
        {
            double off = _m.AfastMeioFio;
            for (int passe = 0; passe < 3; passe++)
            {
                double dx = q.X - _m.Ox, dy = q.Y - _m.Oy, r = Math.Sqrt(dx * dx + dy * dy);
                if (r < _m.RIlha + off && r > 1e-9) q = (_m.Ox + dx / r * (_m.RIlha + off), _m.Oy + dy / r * (_m.RIlha + off));
                dx = q.X - _m.Ox; dy = q.Y - _m.Oy; r = Math.Sqrt(dx * dx + dy * dy);
                double ang = GeometriaRotatoria.Normalizar(Math.Atan2(dy, dx));
                // só na faixa junto do bordo externo: longe do anel o ponto está num ramo (e um ramo curvo pode sair do setor da abertura)
                if (r > _m.RExt - off && r < _m.RExt + 5 && !_m.NaAbertura(_in, ang) && !_m.NaAbertura(_out, ang))
                    q = (_m.Ox + dx / r * (_m.RExt - off), _m.Oy + dy / r * (_m.RExt - off));
                foreach (var c in _meiosFios)
                {
                    double ex = q.X - c.X, ey = q.Y - c.Y, d = Math.Sqrt(ex * ex + ey * ey);
                    if (d < c.R + off && d > 1e-9) q = (c.X + ex / d * (c.R + off), c.Y + ey / d * (c.R + off));
                }
                q = Faixa(_in, q, entrada: true);
                q = Faixa(_out, q, entrada: false);
            }
            return q;
        }

        /// <summary>No ramo, fora do anel: entre o meio-fio externo e a ilha divisória (ou o eixo da via), com os afastamentos.</summary>
        private (double X, double Y) Faixa(BracoRotatoria b, (double X, double Y) q, bool entrada)
        {
            double r = Math.Sqrt((q.X - _m.Ox) * (q.X - _m.Ox) + (q.Y - _m.Oy) * (q.Y - _m.Oy));
            if (r < _m.RExt - 0.5) return q;
            // só o que está no setor deste braço (entre as bissetrizes com os vizinhos) é deste ramo
            double ang = GeometriaRotatoria.Normalizar(Math.Atan2(q.Y - _m.Oy, q.X - _m.Ox));
            if (!NoSetor(entrada ? _setorIn : _setorOut, ang)) return q;
            var (s, v) = b.Eixo.Projetar(q.X, q.Y);
            if (s < b.SRetencao - 0.5) return q;
            int lado = entrada ? 1 : -1;
            double w = lado * v; // positivo = do lado certo do eixo
            double off = _m.AfastMeioFio;
            double tang = entrada ? b.STangEntrada : b.STangSaida;
            bool comMeioFio = double.IsNaN(tang) ? s >= b.SRetencao : s >= tang;
            double sup = comMeioFio ? b.Bordo(s, entrada) - off : double.PositiveInfinity;
            double inf = double.NegativeInfinity;
            if (b.TemIlha)
            {
                if (s >= b.SIlhaIni - off && s <= b.SIlhaFim + off) inf = b.MeiaIlha(Math.Clamp(s, b.SIlhaIni, b.SIlhaFim)) + off;
                else if (s > b.SIlhaFim) inf = _m.AfastEixo;
            }
            else if (s >= b.SRetencao) inf = _m.AfastEixo;
            double wn = w;
            if (wn > sup) wn = sup;
            if (wn < inf) wn = inf;
            if (inf > sup) wn = (inf + sup) / 2;
            return wn == w ? q : b.Eixo.Ponto(s, lado * wn);
        }
    }
}
