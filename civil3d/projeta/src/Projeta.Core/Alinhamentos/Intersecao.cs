using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

/// <summary>Braço de uma interseção (para o relatório).</summary>
public sealed record DadosBracoIntersecao(string Rumo, string Eixo, string Estaca, double AnguloGraus, double MeiaLargura, double? Rampa, double? Tangencia, bool Principal);

/// <summary>Esquina (curva de concordância entre dois braços).</summary>
public sealed record EsquinaIntersecao(string Nome, string Entre, double Raio, double? Desenvolvimento, double AnguloGraus, string LadoPasseio, bool ComGreide, bool Coube);

/// <summary>Triângulo de visibilidade de uma aproximação com PARE.</summary>
public sealed record VisadaIntersecao(string Aproximacao, double Distancia, double Velocidade, double Tempo, string Situacao);

/// <summary>
/// Alargamento da via principal num braço: faixa de conversão à esquerda (largura plena até o fim do armazenamento e da
/// desaceleração, depois o teiper) ou só a transição de saída (o braço do outro lado de um T).
/// </summary>
public sealed record ConversaoIntersecao(string Braco, string Eixo, string Tipo, double Largura, double Armazenamento, double Desaceleracao, double Teiper,
    double DistanciaPlena, string Inicio, string Fim);

/// <summary>
/// Ilha canalizadora de uma esquina com pista de conversão à direita. O bordo interno da pista de conversão (meio-fio do lado
/// da quadra) é o arco de raio <see cref="RaioGiro"/>; o bordo do lado da ilha fica a <see cref="LarguraGiro"/> dele; a ilha
/// vai desse bordo até o nariz, afastada <see cref="Recuo"/> dos bordos das vias. Pontos relativos ao centro da interseção.
/// </summary>
public sealed record IlhaCanalizadora(double RaioGiro, double LarguraGiro, double Recuo, bool Cabe,
    (double X, double Y) Centro, (double X, double Y) Nariz, (double X, double Y) PontaA, (double X, double Y) PontaB,
    IReadOnlyList<(double X, double Y)> Contorno, IReadOnlyList<(double X, double Y)> BordoGiro,
    double Area, double LadoA, double LadoB, double LadoArco)
{
    public string Entre { get; init; } = "";
}

/// <summary>Geometria da interseção em nível: centro das curvas de esquina e distância de visibilidade de cruzamento.</summary>
public static class GeometriaIntersecao
{
    /// <summary>
    /// Centro da curva de esquina entre o braço de direção <paramref name="a1"/> e o braço seguinte no sentido anti-horário,
    /// de direção <paramref name="a2"/> (radianos, a partir do centro da interseção), com bordos a <paramref name="w1"/> (lado
    /// anti-horário do braço 1) e <paramref name="w2"/> (lado horário do braço 2) do eixo e raio <paramref name="r"/>.
    /// Coordenadas relativas ao centro. Null quando os braços estão em linha (sem esquina).
    /// </summary>
    public static (double X, double Y)? CentroEsquina(double a1, double a2, double w1, double w2, double r)
    {
        double n1x = -Math.Sin(a1), n1y = Math.Cos(a1);   // normal à esquerda do braço 1
        double n2x = Math.Sin(a2), n2y = -Math.Cos(a2);   // normal à direita do braço 2
        double det = n1x * n2y - n1y * n2x;
        if (Math.Abs(det) < 1e-6) return null;
        double c1 = w1 + r, c2 = w2 + r;
        return ((c1 * n2y - n1y * c2) / det, (n1x * c2 - c1 * n2x) / det);
    }

    /// <summary>Ângulo anti-horário de a1 até a2, em [0, 2π).</summary>
    public static double AnguloAntiHorario(double a1, double a2) => GeometriaRotatoria.Normalizar(a2 - a1);

    /// <summary>Tempo de travessia padrão (s): conversão à esquerda partindo do PARE, carro de passeio (AASHTO, caso B1).</summary>
    public const double TempoPadrao = 7.5;

    /// <summary>Recuo do motorista parado em relação ao bordo da via principal (m) (AASHTO: 14,5 ft).</summary>
    public const double RecuoMotorista = 4.4;

    /// <summary>Distância de visibilidade ao longo da via principal: d = 0,278·V·tg (m, km/h, s).</summary>
    public static double DistanciaVisibilidade(double vKmh, double tg = TempoPadrao) => 0.278 * vKmh * tg;

    /// <summary>Se o segmento PQ entra no círculo (centro C, raio r).</summary>
    public static bool SegmentoCortaCirculo((double X, double Y) p, (double X, double Y) q, (double X, double Y) c, double r)
    {
        double dx = q.X - p.X, dy = q.Y - p.Y, l2 = dx * dx + dy * dy;
        double t = l2 < 1e-12 ? 0 : Math.Clamp(((c.X - p.X) * dx + (c.Y - p.Y) * dy) / l2, 0, 1);
        double x = p.X + t * dx - c.X, y = p.Y + t * dy - c.Y;
        return x * x + y * y < r * r - 1e-6;
    }

    // ---------- faixa de conversão à esquerda ----------

    private static readonly int[] VTabela6 = { 40, 50, 60, 70, 80, 90, 100, 110, 120 };
    private static readonly int[] TeiperTabela6 = { 40, 45, 55, 60, 70, 80, 85, 90, 100 };
    private static readonly int[] DesacTabela6 = { 60, 75, 95, 110, 130, 145, 170, 180, 200 };

    /// <summary>
    /// Comprimento da faixa de desaceleração até a parada (velocidade de saída 0) e do teiper pela velocidade diretriz:
    /// DNIT (2024), Manual de Projeto de Acessos de Áreas Lindeiras a Rodovias Federais, 2ª ed., Tabela 6 (faixa de
    /// desaceleração, níveis 3 e 4), que vem do Manual de Projeto de Interseções (2005). Usa a primeira linha com V maior ou
    /// igual (a favor da segurança); abaixo de 40 km/h, a de 40; acima de 120, a de 120.
    /// </summary>
    public static (double Desaceleracao, double Teiper, int VTabela) DesaceleracaoDnit(double vKmh)
    {
        int i = Array.FindIndex(VTabela6, v => v >= vKmh - 1e-9);
        if (i < 0) i = VTabela6.Length - 1;
        return (DesacTabela6[i], TeiperTabela6[i], VTabela6[i]);
    }

    /// <summary>
    /// Meia largura da via principal com o alargamento da faixa de conversão (metade de cada lado do eixo): plena até
    /// <paramref name="dPlena"/> do centro, teiper linear até <paramref name="dPlena"/> + <paramref name="teiper"/>.
    /// </summary>
    public static double MeiaLarguraAlargada(double w, double alargamento, double dPlena, double teiper, double d)
    {
        if (d <= dPlena) return w + alargamento / 2;
        if (teiper <= 1e-9 || d >= dPlena + teiper) return w;
        return w + alargamento / 2 * (1 - (d - dPlena) / teiper);
    }

    // ---------- ilha canalizadora ----------

    /// <summary>
    /// Ilha canalizadora da esquina entre o braço <paramref name="a1"/> e o seguinte no sentido anti-horário <paramref name="a2"/>
    /// (bordos a <paramref name="w1"/> e <paramref name="w2"/> do eixo, como em <see cref="CentroEsquina"/>): pista de conversão
    /// de largura <paramref name="largura"/> com bordo interno de raio <paramref name="raio"/>; ilha afastada
    /// <paramref name="recuo"/> dos bordos das vias. Null se os braços estão em linha; <c>Cabe = false</c> se a pista de
    /// conversão não deixa espaço para a ilha (aumente o raio ou diminua a largura).
    /// </summary>
    public static IlhaCanalizadora? Ilha(double a1, double a2, double w1, double w2, double raio, double largura, double recuo, int pontosArco = 24)
    {
        if (CentroEsquina(a1, a2, w1, w2, raio) is not { } c) return null;
        if (CentroEsquina(a1, a2, w1 + recuo, w2 + recuo, 0) is not { } q0) return null;
        double r = raio + largura;
        (double X, double Y) u1 = (Math.Cos(a1), Math.Sin(a1)), u2 = (Math.Cos(a2), Math.Sin(a2));
        var bordo = new List<(double X, double Y)>();
        if (CentroEsquina(a1, a2, w1, w2, 0) is { } p0 && Corte(p0, u1, c, r) is { } t1 && Corte(p0, u2, c, r) is { } t2)
            bordo = Arco(c, r, (p0.X + t1 * u1.X, p0.Y + t1 * u1.Y), (p0.X + t2 * u2.X, p0.Y + t2 * u2.Y), pontosArco, out _);
        double dq = Math.Sqrt((q0.X - c.X) * (q0.X - c.X) + (q0.Y - c.Y) * (q0.Y - c.Y));
        if (dq <= r + 1e-6 || Corte(q0, u1, c, r) is not { } s1 || Corte(q0, u2, c, r) is not { } s2)
            return new IlhaCanalizadora(raio, largura, recuo, false, c, q0, q0, q0, Array.Empty<(double, double)>(), bordo, 0, 0, 0, 0);
        (double X, double Y) qa = (q0.X + s1 * u1.X, q0.Y + s1 * u1.Y), qb = (q0.X + s2 * u2.X, q0.Y + s2 * u2.Y);
        var contorno = new List<(double X, double Y)> { q0 };
        contorno.AddRange(Arco(c, r, qa, qb, pontosArco, out double delta));
        double area = 0;
        for (int i = 0; i < contorno.Count; i++)
        {
            var (x1, y1) = contorno[i];
            var (x2, y2) = contorno[(i + 1) % contorno.Count];
            area += x1 * y2 - x2 * y1;
        }
        return new IlhaCanalizadora(raio, largura, recuo, true, c, q0, qa, qb, contorno, bordo, Math.Abs(area) / 2, s1, s2, r * Math.Abs(delta));
    }

    /// <summary>Distância t ≥ 0 ao longo de <paramref name="u"/> (unitário), a partir de p, até o primeiro encontro com o círculo.</summary>
    private static double? Corte((double X, double Y) p, (double X, double Y) u, (double X, double Y) c, double r)
    {
        double fx = p.X - c.X, fy = p.Y - c.Y, b = fx * u.X + fy * u.Y, disc = b * b - (fx * fx + fy * fy - r * r);
        if (disc < 0) return null;
        double sq = Math.Sqrt(disc);
        if (-b - sq >= -1e-9) return -b - sq;
        if (-b + sq >= -1e-9) return -b + sq;
        return null;
    }

    /// <summary>Pontos do arco curto de a até b em volta de c (inclui as pontas).</summary>
    private static List<(double X, double Y)> Arco((double X, double Y) c, double r, (double X, double Y) a, (double X, double Y) b, int n, out double delta)
    {
        double ta = Math.Atan2(a.Y - c.Y, a.X - c.X), tb = Math.Atan2(b.Y - c.Y, b.X - c.X);
        delta = tb - ta;
        while (delta > Math.PI) delta -= 2 * Math.PI;
        while (delta < -Math.PI) delta += 2 * Math.PI;
        var l = new List<(double X, double Y)>(n + 1);
        for (int k = 0; k <= n; k++)
        {
            double t = ta + delta * k / n;
            l.Add((c.X + r * Math.Cos(t), c.Y + r * Math.Sin(t)));
        }
        return l;
    }
}

/// <summary>Planilha do PRJINTERSECAO.</summary>
public sealed class RelatorioIntersecao
{
    public string Titulo { get; set; } = "Interseção";
    public string Desenho { get; set; } = "";
    public double Ox { get; set; }
    public double Oy { get; set; }
    public string Principal { get; set; } = "";
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<DadosBracoIntersecao> Bracos { get; } = new();
    public List<EsquinaIntersecao> Esquinas { get; } = new();
    public List<VisadaIntersecao> Visadas { get; } = new();
    public List<ConversaoIntersecao> Conversoes { get; } = new();
    public List<IlhaCanalizadora> Ilhas { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<RegiaoCorredor> Corredor { get; } = new();
    public List<string> Montagens { get; } = new();
    public List<string> Avisos { get; } = new();
    public List<string> Pendencias { get; } = new();

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";

    public PastaXlsx Planilha()
    {
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n1 = new EstiloCelula { Formato = "0.0" };
        var n2 = new EstiloCelula { Formato = "0.00" };

        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };

        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int l = 3;
        void Par(string a, string b) { fr.Texto(l, 1, a, txt with { Negrito = true }); fr.Texto(l, 2, b, q); l++; }
        Par("Centro", $"E {Ox:F3}  N {Oy:F3}");
        Par("Braços", Bracos.Count.ToString());
        Par("Via principal", Principal);
        Par("Esquinas", $"{Esquinas.Count(e => e.Coube)} de {Esquinas.Count}");
        if (Conversoes.Count > 0) Par("Faixa de conversão à esquerda", string.Join("; ", Conversoes.Where(c => c.Armazenamento > 0).Select(c => $"braço {c.Braco}")) is { Length: > 0 } fc ? fc : "só transições");
        if (Ilhas.Count > 0) Par("Ilhas canalizadoras", $"{Ilhas.Count(i => i.Cabe)} de {Ilhas.Count} couberam");
        int ap = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado), rp = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado), at = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Atencao);
        Par("Verificações", $"{ap} aprovadas, {rp} reprovadas, {at} com atenção");
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        l++;
        fr.Texto(l, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 26); fr.Largura(2, 70);

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 40), ("Valor", 14), ("Unidade", 9), ("Fonte", 60));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, txt); fp.Texto(lp, 2, p.Valor, txt with { Horizontal = AlinhamentoH.Direita }); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        var fb = pasta.NovaFolha("Braços");
        Cab(fb, 1, ("Braço", 10), ("Eixo", 28), ("Estaca no círculo de busca", 14), ("Ângulo até o próximo (°)", 12), ("Meia largura (m)", 10), ("Rampa na aproximação (%)", 12), ("Esquina a (m do centro)", 12), ("Via", 12));
        int lb = 2;
        foreach (var b in Bracos)
        {
            fb.Texto(lb, 1, b.Rumo, txt); fb.Texto(lb, 2, b.Eixo, txt); fb.Texto(lb, 3, b.Estaca, txt);
            fb.Numero(lb, 4, b.AnguloGraus, n1); fb.Numero(lb, 5, b.MeiaLargura, n2);
            if (b.Rampa is { } r) fb.Numero(lb, 6, r * 100, n2); else fb.Texto(lb, 6, "sem greide", txt);
            if (b.Tangencia is { } t) fb.Numero(lb, 7, t, n2); else fb.Texto(lb, 7, "-", txt);
            fb.Texto(lb, 8, b.Principal ? "principal" : "secundária", txt);
            lb++;
        }

        var fe = pasta.NovaFolha("Esquinas");
        Cab(fe, 1, ("Esquina", 40), ("Entre", 16), ("Raio (m)", 9), ("Desenvolvimento (m)", 12), ("Ângulo (°)", 9), ("Passeio", 10), ("Greide", 12));
        int le = 2;
        foreach (var e in Esquinas)
        {
            fe.Texto(le, 1, e.Nome, q); fe.Texto(le, 2, e.Entre, txt); fe.Numero(le, 3, e.Raio, n2);
            if (e.Desenvolvimento is { } d) fe.Numero(le, 4, d, n2); else fe.Texto(le, 4, "não coube", Res(VerificacaoRotatoria.Reprovado));
            fe.Numero(le, 5, e.AnguloGraus, n1); fe.Texto(le, 6, e.LadoPasseio, txt); fe.Texto(le, 7, e.ComGreide ? "casa com os dois" : "sem greide", txt);
            le++;
        }

        if (Visadas.Count > 0)
        {
            var fs = pasta.NovaFolha("Visibilidade");
            Cab(fs, 1, ("Aproximação com PARE", 24), ("Distância (m)", 12), ("V da via principal (km/h)", 12), ("tg (s)", 8), ("Situação", 70));
            int ls = 2;
            foreach (var v in Visadas)
            {
                fs.Texto(ls, 1, v.Aproximacao, txt); fs.Numero(ls, 2, v.Distancia, n1); fs.Numero(ls, 3, v.Velocidade, n1); fs.Numero(ls, 4, v.Tempo, n1); fs.Texto(ls, 5, v.Situacao, q);
                ls++;
            }
            ls++;
            fs.Texto(ls, 1, "Motorista parado a 4,4 m do bordo da via principal, no meio da faixa; d = 0,278·V·tg ao longo da via principal, para os dois lados (AASHTO, caso B1 — referência complementar). O triângulo desenhado tem de ficar livre de obstáculos.", txt with { Borda = false, Italico = true });
        }

        if (Conversoes.Count > 0)
        {
            var fq = pasta.NovaFolha("Conversão");
            Cab(fq, 1, ("Braço", 10), ("Eixo", 26), ("Alargamento", 30), ("Largura da faixa (m)", 10), ("Armazenamento (m)", 12), ("Desaceleração (m)", 12),
                ("Teiper (m)", 10), ("Largura plena até (m do centro)", 13), ("Início (est.)", 12), ("Fim do teiper (est.)", 12));
            int lq = 2;
            foreach (var c in Conversoes)
            {
                fq.Texto(lq, 1, c.Braco, txt); fq.Texto(lq, 2, c.Eixo, q); fq.Texto(lq, 3, c.Tipo, q); fq.Numero(lq, 4, c.Largura, n2);
                fq.Numero(lq, 5, c.Armazenamento, n1); fq.Numero(lq, 6, c.Desaceleracao, n1); fq.Numero(lq, 7, c.Teiper, n1); fq.Numero(lq, 8, c.DistanciaPlena, n1);
                fq.Texto(lq, 9, c.Inicio, txt); fq.Texto(lq, 10, c.Fim, txt);
                lq++;
            }
            lq++;
            fq.Texto(lq, 1, "Alargamento simétrico (metade de cada lado do eixo): a faixa de conversão fica centrada no eixo e as faixas de passagem se afastam; o braço do outro lado do T só tem a transição de saída. O teiper é linear.", txt with { Borda = false, Italico = true });
        }

        if (Ilhas.Count > 0)
        {
            var fi = pasta.NovaFolha("Ilhas");
            Cab(fi, 1, ("Esquina", 14), ("Raio do bordo interno (m)", 12), ("Largura da pista de conversão (m)", 13), ("Afastamento dos bordos (m)", 12),
                ("Área (m²)", 10), ("Lado junto do 1º braço (m)", 12), ("Lado junto do 2º braço (m)", 12), ("Lado em arco (m)", 11), ("Cabe", 10));
            int li = 2;
            foreach (var i in Ilhas)
            {
                fi.Texto(li, 1, i.Entre, txt); fi.Numero(li, 2, i.RaioGiro, n2); fi.Numero(li, 3, i.LarguraGiro, n2); fi.Numero(li, 4, i.Recuo, n2);
                fi.Numero(li, 5, i.Area, n2); fi.Numero(li, 6, i.LadoA, n2); fi.Numero(li, 7, i.LadoB, n2); fi.Numero(li, 8, i.LadoArco, n2);
                fi.Texto(li, 9, i.Cabe ? "sim" : "não", Res(i.Cabe ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                li++;
            }
            li++;
            fi.Texto(li, 1, "Vértices da ilha sem arredondamento: arredonde com raio de 0,3 a 0,6 m e confira de novo os lados (AASHTO: lados de pelo menos 3,6 m depois do arredondamento).", txt with { Borda = false, Italico = true });
        }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 38), ("Onde", 22), ("Valor", 40), ("Critério", 22), ("Fonte", 50), ("Resultado", 13));
        int lv = 2;
        foreach (var v in Verificacoes)
        {
            fv.Texto(lv, 1, v.Item, q); fv.Texto(lv, 2, v.Braco, q); fv.Texto(lv, 3, v.Valor, q); fv.Texto(lv, 4, v.Criterio, q); fv.Texto(lv, 5, v.Fonte, q);
            fv.Texto(lv, 6, v.Resultado, Res(v.Resultado));
            lv++;
        }

        if (Corredor.Count > 0 || Montagens.Count > 0)
        {
            var fc = pasta.NovaFolha("Corredor");
            Cab(fc, 1, ("Eixo (baseline)", 44), ("De (est.)", 12), ("Até (est.)", 12), ("Montagem", 46), ("Observação", 70));
            int lc = 2;
            foreach (var r in Corredor)
            {
                fc.Texto(lc, 1, r.Baseline, q); fc.Texto(lc, 2, Comum.Num.Estaca(r.De), txt); fc.Texto(lc, 3, Comum.Num.Estaca(r.Ate), txt);
                fc.Texto(lc, 4, r.Montagem, q); fc.Texto(lc, 5, r.Obs, q);
                lc++;
            }
            lc++;
            foreach (var m in Montagens) fc.Texto(lc++, 1, m, txt with { Borda = false });
        }

        var fn = pasta.NovaFolha("Pendências");
        Cab(fn, 1, ("Pendência / aviso", 120));
        int ln = 2;
        foreach (var a in Avisos) fn.Texto(ln++, 1, "AVISO: " + a, q);
        foreach (var p in Pendencias) fn.Texto(ln++, 1, p, q);
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
