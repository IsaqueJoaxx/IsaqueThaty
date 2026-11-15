using System.Globalization;
using System.Text;
using Projeta.Core.Relatorios;
using Projeta.Giro.Core.Geometria;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Comparador de alternativas (PRJALTERNATIVAS): no mesmo cruzamento, gera em planta o PARE simples, a interseção canalizada, a
// mini-rotatória e as rotatórias de 1 e de 2 faixas; mede área ocupada, pavimento, meio-fio, ilhas e (com o terreno) a
// terraplenagem indicativa; aplica a tabela de preços do usuário (SICRO, SINAPI ou outra) às quantidades; calcula a capacidade
// (HCM, como no PRJCAPACIDADE) e conta os pontos de conflito. Pontua cada alternativa com pesos e monta o relatório de decisão.
// ======================================================================================================================

/// <summary>Braço do cruzamento: direção (do centro para fora) e meia largura da pista.</summary>
public sealed class BracoAlternativa
{
    public string Nome { get; set; } = "";
    public string Rumo { get; set; } = "";
    /// <summary>Ângulo do braço (rad, do leste no sentido anti-horário), do centro para fora.</summary>
    public double Angulo { get; set; }
    /// <summary>Meia largura da pista existente (eixo ao bordo), m.</summary>
    public double MeiaLargura { get; set; } = 3.5;
    /// <summary>Via principal (preferencial no PARE).</summary>
    public bool Principal { get; set; }
}

/// <summary>Cruzamento a comparar: centro, braços e volumes da hora de pico por movimento (veíc/h).</summary>
public sealed class CenarioAlternativas
{
    public string Nome { get; set; } = "";
    public string Origem { get; set; } = "";
    public double Ox { get; set; }
    public double Oy { get; set; }
    public List<BracoAlternativa> Bracos { get; set; } = new();
    public Dictionary<(int O, int D), double> Volumes { get; } = new();
}

/// <summary>Preço unitário da tabela do usuário (SICRO, SINAPI ou composição própria).</summary>
public sealed class PrecoUnitario
{
    public string Codigo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Unidade { get; set; } = "";
    public double? Preco { get; set; }
    /// <summary>Código e data-base da referência (ex.: "SICRO MG 07/2026 4011209").</summary>
    public string Fonte { get; set; } = "";
}

public sealed class ParametrosAlternativas
{
    /// <summary>Comprimento dos braços considerado a partir do centro, igual para todas as alternativas (m).</summary>
    public double Zona { get; set; } = 60;
    /// <summary>Faixa além do bordo que já pertence à via existente (passeio), m.</summary>
    public double Passeio { get; set; } = 3.0;
    public double RaioEsquina { get; set; } = 12;
    public double RaioCanalizada { get; set; } = 25;
    public double LarguraGiroDireita { get; set; } = 5.0;
    public double AfastamentoIlha { get; set; } = 0.6;
    public double IlhaMinima { get; set; } = 5.0;
    public double LarguraConversao { get; set; } = 3.0;
    public double ArmazenamentoConversao { get; set; } = 30;
    public double TeiperConversao { get; set; } = 30;
    public double DciMini { get; set; } = 24;
    public double LarguraAnelMini { get; set; } = 6.0;
    public double RaioConexaoMini { get; set; } = 8;
    public double Dci1 { get; set; } = 40;
    public double LarguraAnel1 { get; set; } = 6.0;
    public double Apron1 { get; set; } = 2.0;
    public double LarguraEntrada1 { get; set; } = 4.5;
    public double LarguraSaida1 { get; set; } = 5.0;
    public double Dci2 { get; set; } = 50;
    public double LarguraAnel2 { get; set; } = 9.5;
    public double Apron2 { get; set; } = 2.0;
    public double LarguraEntrada2 { get; set; } = 8.0;
    public double LarguraSaida2 { get; set; } = 8.0;
    public double RaioEntrada { get; set; } = 15;
    public double RaioSaida { get; set; } = 20;
    public double IlhaDivisoriaComprimento { get; set; } = 15;
    public double IlhaDivisoriaLargura { get; set; } = 3.0;
    public double Revestimento { get; set; } = 0.05;
    public double Base { get; set; } = 0.15;
    public double SubBase { get; set; } = 0.15;
    public double DensidadeCbuq { get; set; } = 2.4;
    /// <summary>Malha da terraplenagem indicativa (m).</summary>
    public double Malha { get; set; } = 1.0;
    public double PesoArea { get; set; } = 15;
    public double PesoTerraplenagem { get; set; } = 10;
    public double PesoCusto { get; set; } = 25;
    public double PesoCapacidade { get; set; } = 30;
    public double PesoConflitos { get; set; } = 20;
    public List<PrecoUnitario> Precos { get; set; } = PrecosPadrao();
    public bool ParametrosDoUsuario { get; set; }
    public bool PrecosDoUsuario { get; set; }

    public static List<PrecoUnitario> PrecosPadrao() => new()
    {
        new() { Codigo = "PAV-CBUQ", Descricao = "Concreto betuminoso usinado a quente (revestimento)", Unidade = "t" },
        new() { Codigo = "PAV-IMP", Descricao = "Imprimação e pintura de ligação", Unidade = "m²" },
        new() { Codigo = "PAV-BASE", Descricao = "Base de brita graduada simples", Unidade = "m³" },
        new() { Codigo = "PAV-SUB", Descricao = "Sub-base granular", Unidade = "m³" },
        new() { Codigo = "MEIO-FIO", Descricao = "Meio-fio de concreto", Unidade = "m" },
        new() { Codigo = "ILHA", Descricao = "Ilha elevada (enchimento, acabamento e grama)", Unidade = "m²" },
        new() { Codigo = "TRANSP", Descricao = "Faixa / ilha transponível em concreto", Unidade = "m²" },
        new() { Codigo = "CORTE", Descricao = "Escavação, carga e transporte (corte)", Unidade = "m³" },
        new() { Codigo = "ATERRO", Descricao = "Aterro compactado", Unidade = "m³" },
        new() { Codigo = "DESAP", Descricao = "Desapropriação (área fora da faixa existente)", Unidade = "m²" },
    };

    public PrecoUnitario Preco(string codigo) =>
        Precos.FirstOrDefault(x => string.Equals(x.Codigo, codigo, StringComparison.OrdinalIgnoreCase)) ?? PrecosPadrao().First(x => x.Codigo == codigo);
}

/// <summary>Uma alternativa: a planta, as quantidades, o custo, a capacidade, os conflitos e a nota.</summary>
public sealed class Alternativa
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public bool Aplicavel { get; set; } = true;
    public string Motivo { get; set; } = "";
    public double Dci { get; set; }
    public List<List<V2>> Pavimento { get; set; } = new();
    public List<List<V2>> Ilhas { get; set; } = new();
    public List<List<V2>> Transponivel { get; set; } = new();
    public double AreaPavimento { get; set; }
    public double AreaIlhas { get; set; }
    public double AreaTransponivel { get; set; }
    public double AreaOcupada { get; set; }
    public double AreaAdicional { get; set; }
    public double MeioFio { get; set; }
    public double? Corte { get; set; }
    public double? Aterro { get; set; }
    public List<(string Codigo, double Quantidade)> Quantidades { get; } = new();
    public double Custo { get; set; }
    public bool CustoCompleto { get; set; }
    public AnaliseCapacidade? Capacidade { get; set; }
    public double XMax { get; set; } = double.NaN;
    public double AtrasoMedio { get; set; } = double.NaN;
    public string PiorNivel { get; set; } = "-";
    public int Cruzamento { get; set; }
    public int Convergencia { get; set; }
    public int Divergencia { get; set; }
    public int TrocaFaixa { get; set; }
    public int Conflitos => Cruzamento + Convergencia + Divergencia + TrocaFaixa;
    public string Situacao { get; set; } = "";
    public Dictionary<string, double> Notas { get; } = new();
    public double Pontuacao { get; set; }
    public int Posicao { get; set; }
    public List<string> Premissas { get; } = new();
    public List<string> Avisos { get; } = new();
}

public sealed class ComparacaoAlternativas
{
    public CenarioAlternativas Cenario { get; set; } = new();
    public ParametrosAlternativas Parametros { get; set; } = new();
    public ParametrosCapacidade ParametrosCapacidade { get; set; } = new();
    public List<Alternativa> Alternativas { get; } = new();
    public Alternativa? Recomendada { get; set; }
    public bool ComTerreno { get; set; }
    public bool ComVolumes { get; set; }
    public bool ComPrecos { get; set; }
    public double Zona { get; set; }
    public List<(string Criterio, double Peso, string Unidade)> Criterios { get; } = new();
    public List<string> Avisos { get; } = new();
}

public static class ComparadorAlternativas
{
    public const string Fhwa = "FHWA, Roundabouts: An Informational Guide (FHWA-RD-00-067), Exhibits 5-1 e 5-2";
    public const string Nacto = "NACTO, Urban Street Design Guide (Roundabouts): faixas usuais do DCI";
    public static readonly string[] Codigos = { "PARE", "CANALIZADA", "MINI", "ROT1", "ROT2" };

    /// <summary>
    /// Pontos de conflito veículo-veículo da interseção convencional de n ramos de mão dupla, uma faixa por sentido e todos os
    /// movimentos: cruzamento n²(n-1)(n-2)/6, convergência e divergência n(n-2) (3 ramos: 3 + 3 + 3 = 9; 4 ramos: 16 + 8 + 8 = 32).
    /// </summary>
    public static (int Cruzamento, int Convergencia, int Divergencia) ConflitosConvencional(int n) =>
        n < 2 ? (0, 0, 0) : (n * n * (n - 1) * (n - 2) / 6, n * (n - 2), n * (n - 2));

    /// <summary>Rotatória de 1 faixa com n ramos: n convergências e n divergências, nenhum cruzamento (3 ramos: 6; 4 ramos: 8).</summary>
    public static (int Cruzamento, int Convergencia, int Divergencia) ConflitosRotatoria(int n) => (0, n, n);

    private static V2 U(double a) => V2.Polar(a);
    private static V2 N(double a) => V2.Polar(a + Math.PI / 2);
    private static double Norm(double a) { while (a > Math.PI) a -= 2 * Math.PI; while (a <= -Math.PI) a += 2 * Math.PI; return a; }
    private static double Norm2Pi(double a) { a %= 2 * Math.PI; return a < 0 ? a + 2 * Math.PI : a; }

    public static List<V2> Circulo(V2 c, double r)
    {
        var p = Geo.PontosArco(c, r, 0, 2 * Math.PI, 0.02);
        if (p.Count > 2 && p[0].Dist(p[^1]) < 1e-6) p.RemoveAt(p.Count - 1);
        return p;
    }

    /// <summary>Faixa ao longo do braço: perfil (s, meia largura à esquerda, meia largura à direita), do centro para fora.</summary>
    public static List<V2> Faixa(V2 o, double a, IReadOnlyList<(double S, double E, double D)> perfil)
    {
        var u = U(a); var n = N(a);
        var l = new List<V2>();
        foreach (var (s, e, _) in perfil) l.Add(o + u * s + n * e);
        for (int k = perfil.Count - 1; k >= 0; k--) l.Add(o + u * perfil[k].S - n * perfil[k].D);
        return Ccw(l);
    }

    /// <summary>Anel no sentido anti-horário (a união do Clipper usa a regra do número de voltas: anéis em sentidos opostos se anulam).</summary>
    public static List<V2> Ccw(List<V2> l) { if (Geo.Area(l) < 0) l.Reverse(); return l; }

    /// <summary>
    /// Concordância da esquina entre o bordo esquerdo do braço i e o bordo direito do braço seguinte j (anti-horário): o arco de raio
    /// <paramref name="r"/> tangente aos dois bordos. Devolve o polígono (vértice dos bordos, tangente, arco, tangente) ou null quando os
    /// braços estão quase alinhados (ângulo ≥ 170°).
    /// </summary>
    public static List<V2>? Esquina(V2 o, double ai, double wi, double aj, double wj, double r, out V2 centro)
    {
        centro = o;
        double th = Norm2Pi(aj - ai);
        if (th >= 170 * Math.PI / 180 || th < 1e-3) return null;
        V2 ui = U(ai), ni = N(ai), uj = U(aj), nj = N(aj);
        if (!Geo.IntersecaoRetas(o + ni * wi, ui, o - nj * wj, uj, out var p)) return null;
        if (!Geo.IntersecaoRetas(o + ni * (wi + r), ui, o - nj * (wj + r), uj, out centro)) return null;
        var t1 = centro - ni * r;
        var t2 = centro + nj * r;
        if ((t1 - o).Dot(ui) < 0 || (t2 - o).Dot(uj) < 0) return null;
        double a1 = (t1 - centro).Angulo, a2 = (t2 - centro).Angulo;
        var l = new List<V2> { p };
        l.AddRange(Geo.PontosArco(centro, r, a1, Norm(a2 - a1), 0.02));
        return Ccw(l);
    }

    /// <summary>
    /// Concordância do bordo do braço (à distância <paramref name="e"/> do eixo, do lado <paramref name="lado"/>: +1 esquerda / entrada,
    /// -1 direita / saída) com o bordo externo do anel de raio <paramref name="rExt"/>, por um arco de raio <paramref name="r"/>.
    /// </summary>
    public static List<V2>? ConexaoAnel(V2 o, double a, double rExt, double e, double r, int lado)
    {
        if (e >= rExt - 0.1 || r <= 0) return null;
        var u = U(a); var n = N(a) * lado;
        double t = Math.Sqrt(Math.Max(0, (rExt + r) * (rExt + r) - (e + r) * (e + r)));
        var c = o + u * t + n * (e + r);
        var t1 = o + u * t + n * e;
        var t2 = o + (c - o) * (rExt / (rExt + r));
        var q = o + u * Math.Sqrt(rExt * rExt - e * e) + n * e;
        double a1 = (t1 - c).Angulo, a2 = (t2 - c).Angulo;
        var l = new List<V2> { q };
        l.AddRange(Geo.PontosArco(c, r, a1, Norm(a2 - a1), 0.02));
        return Ccw(l);
    }

    private static double AreaDe(IEnumerable<IReadOnlyList<V2>> p) => Math.Abs(p.Sum(x => Geo.Area(x)));

    private static bool Dentro(V2 q, List<(List<V2> Anel, double X0, double Y0, double X1, double Y1)> aneis)
    {
        bool d = false;
        foreach (var (a, x0, y0, x1, y1) in aneis)
        {
            if (q.X < x0 || q.X > x1 || q.Y < y0 || q.Y > y1) continue;
            for (int i = 0, j = a.Count - 1; i < a.Count; j = i++)
                if ((a[i].Y > q.Y) != (a[j].Y > q.Y) && q.X < (a[j].X - a[i].X) * (q.Y - a[i].Y) / (a[j].Y - a[i].Y) + a[i].X) d = !d;
        }
        return d;
    }

    private static List<(List<V2>, double, double, double, double)> Indice(IEnumerable<List<V2>> aneis) =>
        aneis.Where(a => a.Count >= 3).Select(a => (a, a.Min(p => p.X), a.Min(p => p.Y), a.Max(p => p.X), a.Max(p => p.Y))).ToList();

    private static double Perimetro(IReadOnlyList<V2> a) { double s = 0; for (int i = 0; i < a.Count; i++) s += a[i].Dist(a[(i + 1) % a.Count]); return s; }

    /// <summary>Gera e compara as cinco alternativas.</summary>
    /// <param name="terreno">Cota do terreno em (x, y), ou null sem superfície.</param>
    public static ComparacaoAlternativas Gerar(CenarioAlternativas c, ParametrosAlternativas p, ParametrosCapacidade pc, Func<double, double, double?>? terreno)
    {
        var r = new ComparacaoAlternativas { Cenario = c, Parametros = p, ParametrosCapacidade = pc };
        var o = new V2(c.Ox, c.Oy);
        int n = c.Bracos.Count;
        var ordem = Enumerable.Range(0, n).OrderBy(i => Norm2Pi(c.Bracos[i].Angulo)).ToList();
        double wMax = c.Bracos.Count > 0 ? c.Bracos.Max(b => b.MeiaLargura) : 3.5;
        double zona = Math.Max(p.Zona, Math.Max(p.Dci1, p.Dci2) / 2 + 1.5 + p.IlhaDivisoriaComprimento + 15 + 5);
        r.Zona = zona;
        if (Math.Abs(zona - p.Zona) > 1e-6) r.Avisos.Add($"Braços considerados até {zona:0} m do centro (a rotatória maior não cabe nos {p.Zona:0} m pedidos).");
        r.ComVolumes = c.Volumes.Values.Any(v => v > 0);

        // braços simples e via existente (para a área adicional): o cruzamento simples com as esquinas no raio do PARE, mais o passeio
        List<V2> Simples(BracoAlternativa b) => Faixa(o, b.Angulo, new[] { (0.0, b.MeiaLargura, b.MeiaLargura), (zona, b.MeiaLargura, b.MeiaLargura) });
        var basica = new List<IReadOnlyList<V2>> { Circulo(o, wMax) };
        basica.AddRange(c.Bracos.Select(Simples));
        for (int k = 0; k < n && n >= 2; k++)
        {
            var bi = c.Bracos[ordem[k]]; var bj = c.Bracos[ordem[(k + 1) % n]];
            if (Esquina(o, bi.Angulo, bi.MeiaLargura, bj.Angulo, bj.MeiaLargura, p.RaioEsquina, out _) is { } e) basica.Add(e);
        }
        var existente = Poligonos.Uniao(Poligonos.Inflar(Poligonos.Uniao(basica).Cast<IReadOnlyList<V2>>().ToList(), p.Passeio).Cast<IReadOnlyList<V2>>()
            .Concat(c.Bracos.Select(b => (IReadOnlyList<V2>)Faixa(o, b.Angulo, new[] { (0.0, b.MeiaLargura + p.Passeio, b.MeiaLargura + p.Passeio), (zona + p.Passeio + 1, b.MeiaLargura + p.Passeio, b.MeiaLargura + p.Passeio) }))).ToList());

        // terreno: cache da malha e plano pelas pontas dos braços
        var cache = new Dictionary<(long, long), double?>();
        double? Zt(long i, long j)
        {
            if (terreno is null) return null;
            if (!cache.TryGetValue((i, j), out var z)) cache[(i, j)] = z = terreno((i + 0.5) * p.Malha, (j + 0.5) * p.Malha);
            return z;
        }
        Func<double, double, double>? plano = null;
        if (terreno is not null)
        {
            var pts = c.Bracos.Select(b => o + U(b.Angulo) * zona).Select(q => (q, z: terreno(q.X, q.Y))).Where(x => x.z.HasValue).Select(x => (x.q, z: x.z!.Value)).ToList();
            if (pts.Count > 0)
            {
                double zm = pts.Average(x => x.z);
                plano = (_, _) => zm;
                if (pts.Count >= 3)
                {
                    // mínimos quadrados z = a + b·dx + c·dy
                    double sxx = 0, sxy = 0, syy = 0, sxz = 0, syz = 0, mx = pts.Average(x => x.q.X - o.X), my = pts.Average(x => x.q.Y - o.Y);
                    foreach (var (q, z) in pts) { double dx = q.X - o.X - mx, dy = q.Y - o.Y - my, dz = z - zm; sxx += dx * dx; sxy += dx * dy; syy += dy * dy; sxz += dx * dz; syz += dy * dz; }
                    double det = sxx * syy - sxy * sxy;
                    if (Math.Abs(det) > 1e-6)
                    {
                        double bx = (sxz * syy - syz * sxy) / det, by = (syz * sxx - sxz * sxy) / det;
                        plano = (x, y) => zm + bx * (x - o.X - mx) + by * (y - o.Y - my);
                    }
                }
                r.ComTerreno = true;
            }
            else r.Avisos.Add("A superfície não tem cota nas pontas dos braços: comparo sem a terraplenagem.");
        }

        foreach (var cod in Codigos)
        {
            var alt = new Alternativa { Codigo = cod };
            r.Alternativas.Add(alt);
            try { Montar(alt, c, p, o, zona, ordem, Simples); }
            catch (Exception ex) { alt.Aplicavel = false; alt.Motivo = "a geometria não fechou: " + ex.Message; }
            if (!alt.Aplicavel) { alt.Situacao = "não se aplica"; continue; }

            // áreas
            alt.AreaPavimento = AreaDe(alt.Pavimento);
            alt.AreaIlhas = AreaDe(alt.Ilhas);
            alt.AreaTransponivel = AreaDe(alt.Transponivel);
            var tudo = Poligonos.Uniao(alt.Pavimento.Concat(alt.Ilhas).Concat(alt.Transponivel).Cast<IReadOnlyList<V2>>().ToList());
            alt.AreaOcupada = AreaDe(tudo);
            var inflado = p.Passeio > 0 ? Poligonos.Inflar(tudo.Cast<IReadOnlyList<V2>>().ToList(), p.Passeio) : tudo;
            alt.AreaAdicional = AreaDe(Poligonos.Diferenca(inflado.Cast<IReadOnlyList<V2>>().ToList(), existente.Cast<IReadOnlyList<V2>>().ToList()));

            // meio-fio: contorno externo (sem os cortes nas pontas dos braços) + ilhas elevadas + borda externa das partes transponíveis
            double mf = 0;
            foreach (var a in tudo)
                for (int i = 0; i < a.Count; i++)
                {
                    var q1 = a[i]; var q2 = a[(i + 1) % a.Count];
                    bool ponta = c.Bracos.Any(b => (q1 - o).Dot(U(b.Angulo)) >= zona - 0.05 && (q2 - o).Dot(U(b.Angulo)) >= zona - 0.05);
                    if (!ponta) mf += q1.Dist(q2);
                }
            mf += alt.Ilhas.Sum(Perimetro);
            mf += alt.Transponivel.Where(a => Geo.Area(a) > 0).Sum(Perimetro);
            alt.MeioFio = mf;

            // terraplenagem indicativa
            if (plano is not null)
            {
                double eTot = p.Revestimento + p.Base + p.SubBase, corte = 0, aterro = 0, a2 = p.Malha * p.Malha;
                var idx = Indice(tudo);
                if (idx.Count > 0)
                {
                    long i0 = (long)Math.Floor(idx.Min(x => x.Item2) / p.Malha), i1 = (long)Math.Ceiling(idx.Max(x => x.Item4) / p.Malha);
                    long j0 = (long)Math.Floor(idx.Min(x => x.Item3) / p.Malha), j1 = (long)Math.Ceiling(idx.Max(x => x.Item5) / p.Malha);
                    int sem = 0;
                    for (long i = i0; i <= i1; i++)
                        for (long j = j0; j <= j1; j++)
                        {
                            var q = new V2((i + 0.5) * p.Malha, (j + 0.5) * p.Malha);
                            if (!Dentro(q, idx)) continue;
                            var z = Zt(i, j);
                            if (z is null) { sem++; continue; }
                            double d = z.Value - (plano(q.X, q.Y) - eTot);
                            if (d > 0) corte += d * a2; else aterro -= d * a2;
                        }
                    if (sem > 0) alt.Avisos.Add($"{sem * a2:N0} m² sem cota na superfície ficaram fora da terraplenagem.");
                }
                alt.Corte = corte; alt.Aterro = aterro;
            }

            // quantidades e custo
            void Q(string codigo, double q) { if (q > 1e-6) alt.Quantidades.Add((codigo, q)); }
            double ap = alt.AreaPavimento;
            Q("PAV-CBUQ", ap * p.Revestimento * p.DensidadeCbuq);
            Q("PAV-IMP", ap);
            Q("PAV-BASE", ap * p.Base);
            Q("PAV-SUB", ap * p.SubBase);
            Q("MEIO-FIO", alt.MeioFio);
            Q("ILHA", alt.AreaIlhas);
            Q("TRANSP", alt.AreaTransponivel);
            if (alt.Corte is { } ct) Q("CORTE", ct);
            if (alt.Aterro is { } at) Q("ATERRO", at);
            Q("DESAP", alt.AreaAdicional);
            alt.CustoCompleto = alt.Quantidades.All(x => p.Preco(x.Codigo).Preco.HasValue);
            alt.Custo = alt.Quantidades.Sum(x => x.Quantidade * (p.Preco(x.Codigo).Preco ?? 0));

            // conflitos
            if (cod is "PARE" or "CANALIZADA")
            {
                (alt.Cruzamento, alt.Convergencia, alt.Divergencia) = ConflitosConvencional(n);
                if (cod == "CANALIZADA") alt.Premissas.Add("A canalização não reduz o número de pontos de conflito: separa-os no espaço.");
            }
            else
            {
                (alt.Cruzamento, alt.Convergencia, alt.Divergencia) = ConflitosRotatoria(n);
                if (cod == "ROT2")
                {
                    alt.TrocaFaixa = n;
                    alt.Premissas.Add("Conflitos de troca e de uso de faixa no anel de 2 faixas: contei 1 por trecho entre ramos [PREMISSA] (veja FHWA, Exhibits 5-3 e 5-4).");
                }
            }

            // capacidade
            if (r.ComVolumes) Capacidade(alt, c, p, pc);
            else alt.Situacao = "sem volumes";
        }

        Pontuar(r);
        return r;
    }

    private static void Montar(Alternativa alt, CenarioAlternativas c, ParametrosAlternativas p, V2 o, double zona, List<int> ordem, Func<BracoAlternativa, List<V2>> simples)
    {
        int n = c.Bracos.Count;
        var bracos = c.Bracos;
        double wMax = bracos.Max(b => b.MeiaLargura);
        int principais = bracos.Count(b => b.Principal);
        IReadOnlyList<V2> R(List<V2> l) => l;
        switch (alt.Codigo)
        {
            case "PARE":
            case "CANALIZADA":
                {
                    bool canal = alt.Codigo == "CANALIZADA";
                    alt.Nome = canal ? "Interseção canalizada" : "PARE simples";
                    if (n < 3 || n > 4 || principais != 2)
                    {
                        alt.Aplicavel = false;
                        alt.Motivo = n < 3 || n > 4 ? $"o método do HCM (cap. 20) e o desenho valem para 3 ou 4 ramos ({n} aqui)" : $"precisa de 2 braços na via principal ({principais} marcados)";
                        return;
                    }
                    var partes = new List<IReadOnlyList<V2>> { Circulo(o, wMax) };
                    double h = canal ? p.LarguraConversao / 2 : 0;
                    double sIni = bracos.Where(b => !b.Principal).Select(b => b.MeiaLargura).DefaultIfEmpty(wMax).Max() + 2;
                    double s1 = Math.Min(zona - 1, sIni + p.ArmazenamentoConversao), s2 = Math.Min(zona, s1 + p.TeiperConversao);
                    double WEf(BracoAlternativa b) => b.MeiaLargura + (canal && b.Principal ? h : 0);
                    foreach (var b in bracos)
                    {
                        if (canal && b.Principal)
                            partes.Add(Faixa(o, b.Angulo, new[] { (0.0, b.MeiaLargura + h, b.MeiaLargura + h), (s1, b.MeiaLargura + h, b.MeiaLargura + h), (s2, b.MeiaLargura, b.MeiaLargura), (zona, b.MeiaLargura, b.MeiaLargura) }));
                        else partes.Add(simples(b));
                    }
                    var ilhas = new List<List<V2>>();
                    for (int k = 0; k < n; k++)
                    {
                        var bi = bracos[ordem[k]]; var bj = bracos[ordem[(k + 1) % n]];
                        double raio = canal ? p.RaioCanalizada : p.RaioEsquina;
                        var esq = Esquina(o, bi.Angulo, WEf(bi), bj.Angulo, WEf(bj), raio, out var cc);
                        if (esq is null) continue;
                        partes.Add(esq);
                        if (!canal) continue;
                        // ilha do giro à direita: entre os bordos (recuados) e o bordo interno da faixa de giro
                        double ai = bi.Angulo, aj = bj.Angulo;
                        if (!Geo.IntersecaoRetas(o + N(ai) * (WEf(bi) + p.AfastamentoIlha), U(ai), o - N(aj) * (WEf(bj) + p.AfastamentoIlha), U(aj), out var p2)) continue;
                        var cunha = Ccw(new List<V2> { p2, p2 + U(ai) * (3 * raio), p2 + U(aj) * (3 * raio) });
                        var ilha = Poligonos.Diferenca(Poligonos.Intersecao(new[] { R(cunha) }, new[] { R(esq) }), new[] { R(Circulo(cc, raio + p.LarguraGiroDireita)) });
                        double area = AreaDe(ilha);
                        if (area >= p.IlhaMinima) ilhas.AddRange(ilha.Where(x => Math.Abs(Geo.Area(x)) > 0.5));
                        else if (area > 0.01) alt.Avisos.Add($"Ilha do giro à direita entre {bi.Rumo} e {bj.Rumo} com {area:0.0} m² (< {p.IlhaMinima:0.#} m²): fica pintada (zebrada), não elevada.");
                    }
                    var uniao = Poligonos.Uniao(partes);
                    alt.Ilhas = ilhas;
                    alt.Pavimento = ilhas.Count > 0 ? Poligonos.Diferenca(uniao.Cast<IReadOnlyList<V2>>().ToList(), ilhas.Cast<IReadOnlyList<V2>>().ToList()) : uniao;
                    if (canal)
                    {
                        alt.Premissas.Add($"Faixa de conversão à esquerda de {p.LarguraConversao:0.0#} m nas duas aproximações da principal: armazenamento de {p.ArmazenamentoConversao:0} m e teiper de {p.TeiperConversao:0} m [PREMISSA; o PRJCAPACIDADE dá o armazenamento pela fila].");
                        alt.Premissas.Add($"Giro à direita canalizado: raio externo {p.RaioCanalizada:0.#} m, faixa de {p.LarguraGiroDireita:0.0#} m, ilha recuada {p.AfastamentoIlha:0.0#} m dos bordos e elevada a partir de {p.IlhaMinima:0.#} m² [PREMISSA; A CONFIRMAR na norma do órgão].");
                        alt.Premissas.Add("Ilha divisória (gota) nas aproximações secundárias não modelada.");
                    }
                    else alt.Premissas.Add($"Raio de meio-fio das esquinas {p.RaioEsquina:0.#} m [PREMISSA; confira o giro do veículo de projeto no PRJSEGURANCA ou no GIROVERIFICAR].");
                    break;
                }
            case "MINI":
                {
                    alt.Nome = "Mini-rotatória";
                    alt.Dci = p.DciMini;
                    double rExt = p.DciMini / 2, rIlha = rExt - p.LarguraAnelMini;
                    if (rIlha < 1) { alt.Aplicavel = false; alt.Motivo = $"DCI de {p.DciMini:0.#} m sem ilha central com anel de {p.LarguraAnelMini:0.#} m"; return; }
                    var partes = new List<IReadOnlyList<V2>> { Circulo(o, rExt) };
                    foreach (var b in bracos)
                    {
                        partes.Add(simples(b));
                        foreach (int lado in new[] { 1, -1 })
                            if (ConexaoAnel(o, b.Angulo, rExt, b.MeiaLargura, p.RaioConexaoMini, lado) is { } cx) partes.Add(cx);
                    }
                    var ilha = Circulo(o, rIlha);
                    alt.Transponivel = new() { ilha };
                    alt.Pavimento = Poligonos.Diferenca(Poligonos.Uniao(partes).Cast<IReadOnlyList<V2>>().ToList(), new[] { R(ilha) });
                    alt.Premissas.Add($"DCI {p.DciMini:0.#} m (faixa usual de 13,5 a 28,5 m: {Nacto}); anel de {p.LarguraAnelMini:0.0#} m; ilha central de {2 * rIlha:0.#} m toda transponível; ilhas divisórias pintadas; concordância dos bordos com raio {p.RaioConexaoMini:0.#} m [PREMISSA].");
                    if (p.DciMini < 13.5 || p.DciMini > 28.5) alt.Avisos.Add($"DCI de {p.DciMini:0.#} m fora da faixa usual da mini-rotatória (13,5 a 28,5 m).");
                    break;
                }
            default:
                {
                    bool duas = alt.Codigo == "ROT2";
                    alt.Nome = duas ? "Rotatória de 2 faixas" : "Rotatória de 1 faixa";
                    double dci = duas ? p.Dci2 : p.Dci1, anel = duas ? p.LarguraAnel2 : p.LarguraAnel1, apron = duas ? p.Apron2 : p.Apron1;
                    double lEnt = duas ? p.LarguraEntrada2 : p.LarguraEntrada1, lSai = duas ? p.LarguraSaida2 : p.LarguraSaida1;
                    alt.Dci = dci;
                    double rExt = dci / 2, rApron = rExt - anel, rIlha = rApron - apron;
                    if (rApron < 1.5) { alt.Aplicavel = false; alt.Motivo = $"DCI de {dci:0.#} m pequeno para o anel de {anel:0.#} m"; return; }
                    double wi = p.IlhaDivisoriaLargura, sI = rExt + 1.5, sF = sI + p.IlhaDivisoriaComprimento, sT = sF + 15;
                    var partes = new List<IReadOnlyList<V2>> { Circulo(o, rExt) };
                    var ilhas = new List<List<V2>>();
                    foreach (var b in bracos)
                    {
                        double w = b.MeiaLargura;
                        double e0 = wi / 2 + lEnt, d0 = wi / 2 + lSai;
                        // cada lado (entrada à esquerda do eixo, saída à direita) fica com a largura cheia até passar a tangente da
                        // concordância com o anel e depois afina até a pista normal
                        foreach (var (lado, e, r0, fim) in new[] { (1, e0, p.RaioEntrada, Math.Max(w, 0.5 + lEnt)), (-1, d0, p.RaioSaida, Math.Max(w, 0.5 + lSai)) })
                        {
                            double tg = Math.Sqrt(Math.Max(0, (rExt + r0) * (rExt + r0) - (e + r0) * (e + r0)));
                            double s1 = Math.Max(sI, tg + 1), s2 = Math.Max(sF, s1 + 5), s3 = Math.Min(zona, Math.Max(sT, s2 + 10));
                            var perfil = new List<(double, double, double)> { (0.0, e, 0), (s1, e, 0), (Math.Min(s2, zona), fim, 0), (s3, w, 0), (zona, w, 0) }
                                .Select(x => lado > 0 ? x : (x.Item1, 0.0, x.Item2)).ToList();
                            partes.Add(Faixa(o, b.Angulo, perfil));
                        }
                        if (ConexaoAnel(o, b.Angulo, rExt, e0, p.RaioEntrada, 1) is { } ce) partes.Add(ce);
                        if (ConexaoAnel(o, b.Angulo, rExt, d0, p.RaioSaida, -1) is { } cs) partes.Add(cs);
                        ilhas.Add(Faixa(o, b.Angulo, new[] { (sI, wi / 2, wi / 2), (sF, 0.5, 0.5) }));
                    }
                    var centro = new List<List<V2>>();
                    if (rIlha >= 1) ilhas.Add(Circulo(o, rIlha));
                    alt.Transponivel = rIlha >= 1 ? Poligonos.Diferenca(new[] { R(Circulo(o, rApron)) }, new[] { R(Circulo(o, rIlha)) }) : new() { Circulo(o, rApron) };
                    var tira = ilhas.Where(x => x.Count > 0).Cast<IReadOnlyList<V2>>().Append(Circulo(o, rApron)).ToList();
                    alt.Pavimento = Poligonos.Diferenca(Poligonos.Uniao(partes).Cast<IReadOnlyList<V2>>().ToList(), tira);
                    alt.Ilhas = ilhas;
                    alt.Premissas.Add($"DCI {dci:0.#} m; anel de {anel:0.0#} m; faixa transponível de {apron:0.0#} m; ilha central elevada de {Math.Max(0, 2 * rIlha):0.#} m; entradas de {lEnt:0.0#} m e saídas de {lSai:0.0#} m; ilhas divisórias de {p.IlhaDivisoriaComprimento:0} × {wi:0.0#} m; raios de entrada {p.RaioEntrada:0.#} m e de saída {p.RaioSaida:0.#} m [PREMISSA]. Faixas usuais do DCI: {Nacto}.");
                    if (duas && (dci < 40.5 || dci > 54)) alt.Avisos.Add($"DCI de {dci:0.#} m fora da faixa usual da rotatória de 2 faixas (40,5 a 54 m).");
                    alt.Premissas.Add("Confira o giro do veículo de projeto e a deflexão das entradas no PRJSEGURANCA depois de montar a geometria com o PRJROTATORIA.");
                    break;
                }
        }
    }

    private static void Capacidade(Alternativa alt, CenarioAlternativas c, ParametrosAlternativas p, ParametrosCapacidade pc)
    {
        var vols = c.Volumes;
        AnaliseCapacidade a;
        if (alt.Codigo is "PARE" or "CANALIZADA")
        {
            var m = new ModeloIntersecaoSinal { Ox = c.Ox, Oy = c.Oy };
            foreach (var b in c.Bracos)
                m.Bracos.Add(new BracoIntersecaoSinal
                {
                    Nome = b.Nome, Rumo = b.Rumo, Principal = b.Principal, MeiaLargura = b.MeiaLargura, Conversao = alt.Codigo == "CANALIZADA" && b.Principal,
                    Eixo = new() { new[] { c.Ox, c.Oy }, new[] { c.Ox + 100 * Math.Cos(b.Angulo), c.Oy + 100 * Math.Sin(b.Angulo) } },
                });
            a = CalculoCapacidade.Pare(alt.Nome, m, vols, pc, alt.Codigo == "CANALIZADA" ? p.ArmazenamentoConversao : 0);
            if (alt.Codigo == "CANALIZADA") alt.Premissas.Add("Capacidade: secundária em faixa única compartilhada também na canalizada [PREMISSA: a faixa própria do giro à direita aumentaria a capacidade].");
        }
        else
        {
            var m = new ModeloRotatoriaSinal { Ox = c.Ox, Oy = c.Oy, RExt = alt.Dci / 2 };
            foreach (var b in c.Bracos)
                m.Bracos.Add(new BracoRotatoriaSinal { Nome = b.Nome, Rumo = b.Rumo, Angulo = b.Angulo, Eixo = new() { new[] { c.Ox, c.Oy }, new[] { c.Ox + 100 * Math.Cos(b.Angulo), c.Oy + 100 * Math.Sin(b.Angulo) } } });
            int f = alt.Codigo == "ROT2" ? 2 : 1;
            a = CalculoCapacidade.Rotatoria(alt.Nome, m, vols, pc, f, f);
            if (alt.Codigo == "MINI") alt.Premissas.Add("Capacidade da mini-rotatória pela equação da entrada de 1 faixa (HCM, eq. 22-1) [A CONFIRMAR: o HCM não traz modelo próprio para a mini-rotatória].");
        }
        alt.Capacidade = a;
        var ap = a.Aproximacoes.Where(x => x.Fluxo > 0).ToList();
        alt.XMax = ap.Count > 0 ? ap.Max(x => x.X) : 0;
        double fl = ap.Sum(x => x.Fluxo);
        alt.AtrasoMedio = ap.Count == 0 ? 0 : ap.Any(x => double.IsInfinity(x.Atraso) || double.IsNaN(x.Atraso)) ? double.PositiveInfinity : ap.Sum(x => x.Atraso * x.Fluxo) / fl;
        alt.PiorNivel = ap.Select(x => x.Nivel).Where(x => x.Length == 1).DefaultIfEmpty("A").Max()!;
        alt.Situacao = alt.XMax <= pc.XProjeto + 1e-9 ? "atende" : alt.XMax <= 1 + 1e-9 ? "atenção" : "não atende";
    }

    private static void Pontuar(ComparacaoAlternativas r)
    {
        var p = r.Parametros;
        var ap = r.Alternativas.Where(a => a.Aplicavel).ToList();
        r.ComPrecos = ap.Count > 0 && ap.All(a => a.CustoCompleto);
        var crit = new List<(string Nome, double Peso, string Unidade, Func<Alternativa, double> Valor)>
        {
            ("Área ocupada adicional", p.PesoArea, "m²", a => a.AreaAdicional),
        };
        if (r.ComTerreno) crit.Add(("Terraplenagem (corte + aterro)", p.PesoTerraplenagem, "m³", a => (a.Corte ?? 0) + (a.Aterro ?? 0)));
        crit.Add(r.ComPrecos ? ("Custo", p.PesoCusto, "R$", a => a.Custo) : ("Pavimento, ilhas e transponível (sem preços)", p.PesoCusto, "m²", a => a.AreaPavimento + a.AreaIlhas + a.AreaTransponivel));
        if (r.ComVolumes) crit.Add(("Capacidade (x máximo)", p.PesoCapacidade, "", a => a.XMax));
        crit.Add(("Pontos de conflito", p.PesoConflitos, "", a => a.Conflitos));
        foreach (var (nome, peso, un, _) in crit) r.Criterios.Add((nome, peso, un));
        if (!r.ComPrecos) r.Avisos.Add("Sem a tabela de preços completa: o critério de custo usa a área de pavimento, ilhas e transponível como indicador (importe os preços em Precos).");

        double somaPesos = crit.Sum(x => Math.Max(0, x.Peso));
        foreach (var (nome, peso, _, valor) in crit)
        {
            var vs = ap.Select(valor).Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToList();
            if (vs.Count == 0) continue;
            double mn = vs.Min(), mx = vs.Max();
            bool cap = nome.StartsWith("Capacidade");
            foreach (var a in ap)
            {
                double v = valor(a);
                // capacidade: reserva (1 - x) relativa à da melhor; x ≥ 1 vale 0. Os outros: (pior - valor) / (pior - melhor)
                double nota = double.IsNaN(v) || double.IsInfinity(v) ? 0
                            : cap ? (1 - mn > 1e-9 ? Math.Clamp((1 - v) / (1 - mn), 0, 1) : 0)
                            : mx - mn < 1e-9 ? 1 : (mx - v) / (mx - mn);
                a.Notas[nome] = nota;
            }
        }
        foreach (var a in ap)
            a.Pontuacao = somaPesos > 0 ? 100 * crit.Sum(x => Math.Max(0, x.Peso) * (a.Notas.TryGetValue(x.Nome, out var nn) ? nn : 0)) / somaPesos : 0;
        // ranking: primeiro as que atendem a demanda (x ≤ x de projeto), depois as com margem pequena e por fim as que não atendem;
        // em cada grupo, pela pontuação
        static int Grupo(Alternativa a) => a.Situacao switch { "não atende" => 2, "atenção" => 1, _ => 0 };
        int pos = 1;
        foreach (var a in ap.OrderBy(Grupo).ThenByDescending(a => a.Pontuacao)) a.Posicao = pos++;
        var ord = ap.OrderBy(a => a.Posicao).ToList();
        r.Recomendada = ord.FirstOrDefault(a => Grupo(a) < 2);
        if (r.Recomendada is null && ap.Count > 0)
            r.Avisos.Add("Nenhuma das alternativas atende a demanda (x > 1 em todas): avalie semáforo (PRJSEMAFORO), mais faixas ou desnível.");
        else if (r.Recomendada?.Situacao == "atenção")
            r.Avisos.Add($"A recomendada tem x entre {r.ParametrosCapacidade.XProjeto:0.00} e 1,00: margem pequena.");
        if (r.Recomendada is not null)
            foreach (var a in ord.Where(a => Grupo(a) > Grupo(r.Recomendada) && a.Pontuacao > r.Recomendada.Pontuacao))
                r.Avisos.Add($"{a.Nome} tem pontuação maior ({a.Pontuacao:0} contra {r.Recomendada.Pontuacao:0}), mas {(a.Situacao == "não atende" ? "não atende a demanda" : "tem margem pequena de capacidade")}: fica depois no ranking.");
        if (!r.ComVolumes) r.Avisos.Add("Sem volumes por movimento: a capacidade ficou fora da comparação (informe a contagem no PRJCAPACIDADE ou em Volumes).");
    }

    /// <summary>Quadro resumo para a prancha.</summary>
    public static TabelaDados Quadro(ComparacaoAlternativas r)
    {
        var ap = r.Alternativas.Where(a => a.Aplicavel).ToList();
        var t = new TabelaDados { Titulo = $"COMPARATIVO DE ALTERNATIVAS — {r.Cenario.Nome}".Trim(' ', '—') };
        t.Colunas.Add("Critério");
        t.Colunas.AddRange(ap.Select(a => a.Nome));
        void L(string nome, Func<Alternativa, string> f) => t.Linhas.Add(new[] { nome }.Concat(ap.Select(f)).ToArray());
        L("DCI (m)", a => a.Dci > 0 ? a.Dci.ToString("0.#") : "-");
        L("Pavimento (m²)", a => a.AreaPavimento.ToString("N0"));
        L("Ilhas elevadas (m²)", a => a.AreaIlhas.ToString("N0"));
        L("Transponível (m²)", a => a.AreaTransponivel.ToString("N0"));
        L("Meio-fio (m)", a => a.MeioFio.ToString("N0"));
        L("Área ocupada (m²)", a => a.AreaOcupada.ToString("N0"));
        L("Área adicional (m²)", a => a.AreaAdicional.ToString("N0"));
        if (r.ComTerreno) L("Corte / aterro (m³)", a => $"{a.Corte ?? 0:N0} / {a.Aterro ?? 0:N0}");
        L("Custo (R$)", a => a.CustoCompleto ? a.Custo.ToString("N0") : "sem preço");
        if (r.ComVolumes)
        {
            L("x máximo", a => CalculoCapacidade.TxtX(a.XMax));
            L("Atraso médio (s/veíc)", a => CalculoCapacidade.TxtAtraso(a.AtrasoMedio));
            L("Pior nível de serviço", a => a.PiorNivel);
        }
        L("Conflitos (cruzamento / total)", a => $"{a.Cruzamento} / {a.Conflitos}");
        L("Situação", a => a.Situacao);
        L("Pontuação", a => a.Pontuacao.ToString("0"));
        L("Posição", a => a.Posicao + "ª" + (ReferenceEquals(a, r.Recomendada) ? " (recomendada)" : ""));
        return t;
    }
}

/// <summary>Planilha do PRJALTERNATIVAS (relatório de decisão).</summary>
public sealed class RelatorioAlternativas
{
    public string Titulo { get; set; } = "Comparador de alternativas";
    public string Desenho { get; set; } = "";
    public string Superficie { get; set; } = "";
    public ComparacaoAlternativas Comparacao { get; set; } = new();

    public static List<ParametroRotatoria> ParametrosDe(ParametrosAlternativas p)
    {
        string pre = p.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA]";
        return new()
        {
            new("Comprimento dos braços considerado", $"{p.Zona:0}", "m", pre),
            new("Passeio além do bordo (via existente)", $"{p.Passeio:0.0#}", "m", pre),
            new("Raio de meio-fio do PARE simples", $"{p.RaioEsquina:0.#}", "m", pre),
            new("Raio externo do giro à direita canalizado", $"{p.RaioCanalizada:0.#}", "m", pre),
            new("Largura da faixa de giro à direita", $"{p.LarguraGiroDireita:0.0#}", "m", pre),
            new("Área mínima da ilha elevada", $"{p.IlhaMinima:0.#}", "m²", "[A CONFIRMAR na norma do órgão]"),
            new("Faixa de conversão à esquerda (largura / armazenamento / teiper)", $"{p.LarguraConversao:0.0#} / {p.ArmazenamentoConversao:0} / {p.TeiperConversao:0}", "m", pre),
            new("Mini-rotatória: DCI / anel / raio de concordância", $"{p.DciMini:0.#} / {p.LarguraAnelMini:0.0#} / {p.RaioConexaoMini:0.#}", "m", $"{pre}; DCI usual de 13,5 a 28,5 m ({ComparadorAlternativas.Nacto})"),
            new("Rotatória de 1 faixa: DCI / anel / transponível", $"{p.Dci1:0.#} / {p.LarguraAnel1:0.0#} / {p.Apron1:0.0#}", "m", $"{pre}; DCI conforme o veículo de projeto ({ComparadorAlternativas.Nacto})"),
            new("Rotatória de 1 faixa: entrada / saída", $"{p.LarguraEntrada1:0.0#} / {p.LarguraSaida1:0.0#}", "m", pre),
            new("Rotatória de 2 faixas: DCI / anel / transponível", $"{p.Dci2:0.#} / {p.LarguraAnel2:0.0#} / {p.Apron2:0.0#}", "m", $"{pre}; DCI usual de 40,5 a 54 m ({ComparadorAlternativas.Nacto})"),
            new("Rotatória de 2 faixas: entrada / saída", $"{p.LarguraEntrada2:0.0#} / {p.LarguraSaida2:0.0#}", "m", pre),
            new("Raios de entrada / saída", $"{p.RaioEntrada:0.#} / {p.RaioSaida:0.#}", "m", pre),
            new("Ilha divisória (comprimento × largura junto ao anel)", $"{p.IlhaDivisoriaComprimento:0} × {p.IlhaDivisoriaLargura:0.0#}", "m", pre),
            new("Pavimento: revestimento / base / sub-base", $"{p.Revestimento:0.00} / {p.Base:0.00} / {p.SubBase:0.00}", "m", $"{pre} (dimensione pelo tráfego)"),
            new("Densidade do CBUQ", $"{p.DensidadeCbuq:0.0#}", "t/m³", pre),
            new("Terraplenagem indicativa", "greide em plano pelas cotas do terreno nas pontas dos braços; caixa do pavimento em toda a área ocupada", "", "[PREMISSA: só para comparar; o volume do projeto sai do corredor]"),
            new("Pesos: área / terraplenagem / custo / capacidade / conflitos", $"{p.PesoArea:0} / {p.PesoTerraplenagem:0} / {p.PesoCusto:0} / {p.PesoCapacidade:0} / {p.PesoConflitos:0}", "", pre),
            new("Nota de cada critério", "(pior − valor) / (pior − melhor), de 0 a 1; capacidade: reserva (1 − x) / (1 − x da melhor), x ≥ 1 vale 0", "", "método do PROJETA"),
            new("Via existente (para a área adicional)", "cruzamento simples com as esquinas no raio do PARE, mais o passeio", "", "[PREMISSA]"),
            new("Pontos de conflito da interseção convencional", "cruzamento n²(n−1)(n−2)/6; convergência e divergência n(n−2)", "", $"{ComparadorAlternativas.Fhwa} (3 ramos: 9; 4 ramos: 32)"),
            new("Pontos de conflito da rotatória de 1 faixa", "n convergências + n divergências", "", $"{ComparadorAlternativas.Fhwa} (3 ramos: 6; 4 ramos: 8)"),
        };
    }

    public PastaXlsx Planilha()
    {
        var r = Comparacao;
        var p = r.Parametros;
        var ap = r.Alternativas.Where(a => a.Aplicavel).ToList();
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = "D9E1F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n0 = new EstiloCelula { Formato = "#,##0" };
        var n2 = new EstiloCelula { Formato = "#,##0.00" };
        var x2 = new EstiloCelula { Formato = "0.00" };
        string Cor(Alternativa a) => a.Situacao switch { "atende" => "C6EFCE", "atenção" => "FFEB9C", "não atende" => "FFC7CE", _ => "F2F2F2" };

        // resumo
        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string a, string b) { fr.Texto(lr, 1, a, txt with { Negrito = true }); fr.Texto(lr, 2, b, q); lr++; }
        var c = r.Cenario;
        Par("Cruzamento", $"{c.Nome} ({c.Origem}); centro E {c.Ox:0.000} N {c.Oy:0.000}");
        Par("Braços", string.Join("; ", c.Bracos.Select(b => $"{b.Rumo} '{b.Nome}' (meia largura {b.MeiaLargura:0.0#} m{(b.Principal ? ", principal" : "")})")));
        Par("Volumes", r.ComVolumes ? $"{c.Volumes.Values.Sum():0} veíc/h na hora de pico ({c.Volumes.Count(x => x.Value > 0)} movimentos)" : "sem volumes (capacidade fora da comparação)");
        Par("Terreno", r.ComTerreno ? Superficie : "sem superfície (terraplenagem fora da comparação)");
        Par("Preços", r.ComPrecos ? "tabela do usuário completa" : "tabela incompleta: custo indicado pela área");
        Par("Recomendada", r.Recomendada is { } rec ? $"{rec.Nome} — {rec.Pontuacao:0} pontos ({rec.Situacao})" : "nenhuma atende a demanda");
        foreach (var av in r.Avisos) Par("Aviso", av);
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        fr.Texto(lr, 1, "Posição", cab); fr.Texto(lr, 2, "Alternativa", cab); fr.Texto(lr, 3, "Pontuação", cab); fr.Texto(lr, 4, "Situação", cab);
        lr++;
        foreach (var a in ap.OrderBy(a => a.Posicao))
        {
            fr.Texto(lr, 1, $"{a.Posicao}ª", txt); fr.Texto(lr, 2, a.Nome + (ReferenceEquals(a, r.Recomendada) ? " (recomendada)" : ""), txt with { Negrito = ReferenceEquals(a, r.Recomendada) });
            fr.Numero(lr, 3, a.Pontuacao, n0); fr.Texto(lr, 4, a.Situacao, txt with { Fundo = Cor(a) });
            lr++;
        }
        foreach (var a in r.Alternativas.Where(a => !a.Aplicavel)) { fr.Texto(lr, 1, "-", txt); fr.Texto(lr, 2, a.Nome, txt); fr.Texto(lr, 4, "não se aplica: " + a.Motivo, q); lr++; }
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA para a escolha da solução: quantidades e custos são de estudo, sem projeto geométrico, de pavimento e de drenagem; a validação e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 16); fr.Largura(2, 70); fr.Largura(3, 12); fr.Largura(4, 40);

        // comparativo
        var fc = pasta.NovaFolha("Comparativo");
        fc.Texto(1, 1, "Critério", cab); fc.Largura(1, 42);
        for (int k = 0; k < ap.Count; k++) { fc.Texto(1, k + 2, ap[k].Nome, cab with { Fundo = Cor(ap[k]) }); fc.Largura(k + 2, 18); }
        fc.CongelarLinhas = 1;
        int l = 2;
        void Lin(string nome, Func<Alternativa, double> v, EstiloCelula e) { fc.Texto(l, 1, nome, txt); for (int k = 0; k < ap.Count; k++) fc.Numero(l, k + 2, v(ap[k]), e); l++; }
        void LinT(string nome, Func<Alternativa, string> v) { fc.Texto(l, 1, nome, txt); for (int k = 0; k < ap.Count; k++) fc.Texto(l, k + 2, v(ap[k]), q); l++; }
        Lin("DCI (m)", a => a.Dci > 0 ? a.Dci : double.NaN, n2);
        Lin("Pavimento (m²)", a => a.AreaPavimento, n0);
        Lin("Ilhas elevadas (m²)", a => a.AreaIlhas, n0);
        Lin("Faixa / ilha transponível (m²)", a => a.AreaTransponivel, n0);
        Lin("Meio-fio (m)", a => a.MeioFio, n0);
        Lin("Área ocupada (m²)", a => a.AreaOcupada, n0);
        Lin($"Área adicional, fora da via existente com passeio de {p.Passeio:0.0#} m (m²)", a => a.AreaAdicional, n0);
        if (r.ComTerreno) { Lin("Corte indicativo (m³)", a => a.Corte ?? double.NaN, n0); Lin("Aterro indicativo (m³)", a => a.Aterro ?? double.NaN, n0); }
        Lin("Custo (R$)", a => a.CustoCompleto ? a.Custo : double.NaN, n2);
        if (r.ComVolumes)
        {
            Lin("Grau de saturação máximo (x)", a => double.IsInfinity(a.XMax) ? double.NaN : a.XMax, x2);
            LinT("Atraso médio ponderado (s/veíc)", a => CalculoCapacidade.TxtAtraso(a.AtrasoMedio));
            LinT("Pior nível de serviço", a => a.PiorNivel);
        }
        Lin("Conflitos de cruzamento", a => a.Cruzamento, n0);
        Lin("Conflitos de convergência", a => a.Convergencia, n0);
        Lin("Conflitos de divergência", a => a.Divergencia, n0);
        Lin("Conflitos de troca de faixa no anel", a => a.TrocaFaixa, n0);
        Lin("Pontos de conflito (total)", a => a.Conflitos, n0);
        foreach (var (crit, peso, _) in r.Criterios) Lin($"Nota: {crit} (peso {peso:0})", a => a.Notas.TryGetValue(crit, out var v) ? v : double.NaN, x2);
        Lin("Pontuação (0 a 100)", a => a.Pontuacao, n0);
        LinT("Posição", a => a.Posicao + "ª" + (ReferenceEquals(a, r.Recomendada) ? " (recomendada)" : ""));
        LinT("Situação", a => a.Situacao);
        LinT("Premissas", a => string.Join(" ", a.Premissas));
        LinT("Avisos", a => string.Join(" ", a.Avisos));

        // quantitativos
        var fq = pasta.NovaFolha("Quantitativos");
        string[] cq = { "Alternativa", "Item", "Descrição", "Unidade", "Quantidade", "Preço unitário (R$)", "Total (R$)", "Fonte do preço" };
        double[] lq = { 24, 11, 46, 9, 13, 15, 15, 30 };
        for (int k = 0; k < cq.Length; k++) { fq.Texto(1, k + 1, cq[k], cab); fq.Largura(k + 1, lq[k]); }
        fq.CongelarLinhas = 1;
        l = 2;
        foreach (var a in ap)
        {
            foreach (var (cod, qt) in a.Quantidades)
            {
                var pr = p.Preco(cod);
                fq.Texto(l, 1, a.Nome, txt); fq.Texto(l, 2, cod, txt); fq.Texto(l, 3, pr.Descricao, q); fq.Texto(l, 4, pr.Unidade, txt);
                fq.Numero(l, 5, qt, n2);
                if (pr.Preco is { } v) { fq.Numero(l, 6, v, n2); fq.Numero(l, 7, v * qt, n2); }
                else { fq.Texto(l, 6, "[A INFORMAR]", txt with { Fundo = "FFEB9C" }); fq.Texto(l, 7, "-", txt); }
                fq.Texto(l, 8, pr.Fonte.Length > 0 ? pr.Fonte : "[A INFORMAR: SICRO / SINAPI e data-base]", q);
                l++;
            }
            fq.Texto(l, 1, a.Nome, txt with { Negrito = true }); fq.Texto(l, 3, "TOTAL" + (a.CustoCompleto ? "" : " (incompleto)"), txt with { Negrito = true });
            fq.Numero(l, 7, a.Custo, n2 with { Negrito = true });
            l += 2;
        }

        // capacidade
        if (r.ComVolumes)
        {
            var fk = pasta.NovaFolha("Capacidade");
            string[] ck = { "Alternativa", "Onde", "Movimento", "Controle", "Fluxo (veíc/h)", "Capacidade (veíc/h)", "x", "Atraso (s/veíc)", "NS", "Fila 95 % (m)" };
            double[] lk = { 24, 30, 30, 22, 12, 13, 8, 12, 6, 12 };
            for (int k = 0; k < ck.Length; k++) { fk.Texto(1, k + 1, ck[k], cab); fk.Largura(k + 1, lk[k]); }
            fk.CongelarLinhas = 1;
            l = 2;
            foreach (var a in ap.Where(a => a.Capacidade is not null))
                foreach (var x in a.Capacidade!.Aproximacoes)
                {
                    fk.Texto(l, 1, a.Nome, txt); fk.Texto(l, 2, x.Onde, q); fk.Texto(l, 3, x.Movimento, q); fk.Texto(l, 4, x.Controle, q);
                    fk.Numero(l, 5, x.Fluxo, n0); fk.Numero(l, 6, x.Capacidade, n0);
                    fk.Texto(l, 7, CalculoCapacidade.TxtX(x.X), txt with { Fundo = x.X > 1 ? "FFC7CE" : x.X > r.ParametrosCapacidade.XProjeto ? "FFEB9C" : "C6EFCE" });
                    fk.Texto(l, 8, CalculoCapacidade.TxtAtraso(x.Atraso), txt); fk.Texto(l, 9, x.Nivel, txt);
                    fk.Numero(l, 10, double.IsInfinity(x.Fila95m) ? double.NaN : x.Fila95m, n0);
                    l++;
                }
        }

        // preços
        var fp = pasta.NovaFolha("Preços");
        string[] cp = { "Item", "Descrição", "Unidade", "Preço unitário (R$)", "Fonte (tabela, data-base, código)" };
        double[] lp = { 11, 50, 9, 16, 50 };
        for (int k = 0; k < cp.Length; k++) { fp.Texto(1, k + 1, cp[k], cab); fp.Largura(k + 1, lp[k]); }
        l = 2;
        foreach (var d in ParametrosAlternativas.PrecosPadrao())
        {
            var pr = p.Preco(d.Codigo);
            fp.Texto(l, 1, d.Codigo, txt); fp.Texto(l, 2, pr.Descricao, q); fp.Texto(l, 3, pr.Unidade, txt);
            if (pr.Preco is { } v) fp.Numero(l, 4, v, n2); else fp.Texto(l, 4, "[A INFORMAR]", txt with { Fundo = "FFEB9C" });
            fp.Texto(l, 5, pr.Fonte, q);
            l++;
        }
        l++;
        fp.Texto(l, 1, "Os preços vêm do arquivo importado em PRJALTERNATIVAS > Precos (CSV item;descrição;unidade;preço;fonte). O PROJETA não traz preço de tabela: use o SICRO ou o SINAPI da data-base e do estado do orçamento.", txt with { Italico = true, Borda = false });

        // parâmetros
        var fpa = pasta.NovaFolha("Parâmetros");
        string[] cpa = { "Parâmetro", "Valor", "Unidade", "Fonte" };
        double[] lpa = { 52, 40, 9, 70 };
        for (int k = 0; k < cpa.Length; k++) { fpa.Texto(1, k + 1, cpa[k], cab); fpa.Largura(k + 1, lpa[k]); }
        l = 2;
        foreach (var x in ParametrosDe(p).Concat(RelatorioCapacidade.ParametrosDe(r.ParametrosCapacidade, "Rotatória")))
        {
            fpa.Texto(l, 1, x.Nome, q); fpa.Texto(l, 2, x.Valor, q); fpa.Texto(l, 3, x.Unidade, txt); fpa.Texto(l, 4, x.Fonte, q);
            l++;
        }

        // pendências
        var fpe = pasta.NovaFolha("Pendências");
        fpe.Texto(1, 1, "Pendência", cab); fpe.Largura(1, 120);
        l = 2;
        var pend = new List<string>();
        if (!r.ComVolumes) pend.Add("Contagem classificada da hora de pico por movimento (PRJCAPACIDADE) para a capacidade de cada alternativa.");
        if (!r.ComPrecos) pend.Add("Preços unitários (SICRO ou SINAPI, com estado e data-base) de todos os itens: PRJALTERNATIVAS > Precos > Modelo gera o CSV para preencher.");
        if (!r.ComTerreno) pend.Add("Superfície do terreno para a terraplenagem indicativa.");
        pend.Add("Dimensionamento do pavimento pelo tráfego (as espessuras são premissas).");
        pend.Add("Giro do veículo de projeto e deflexão das entradas da alternativa escolhida (PRJSEGURANCA / GIROVERIFICAR).");
        pend.Add("Interferências e desapropriação da alternativa escolhida, com o cadastro dos lotes (PRJINTERFERENCIAS).");
        pend.Add("Norma do órgão que rege o projeto para raios, ilhas, faixas e DCI.");
        foreach (var x in pend) { fpe.Texto(l, 1, x, q); l++; }
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);

    /// <summary>Lê o CSV de preços (item;descrição;unidade;preço;fonte; separador ; ou , e decimal com vírgula ou ponto).</summary>
    public static List<PrecoUnitario> LerPrecos(string csv)
    {
        var r = new List<PrecoUnitario>();
        foreach (var linha in File.ReadAllLines(csv, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;
            char sep = linha.Contains(';') ? ';' : ',';
            var c = linha.Split(sep).Select(x => x.Trim().Trim('"')).ToArray();
            if (c.Length < 4 || c[0].Equals("item", StringComparison.OrdinalIgnoreCase) || c[0].StartsWith("#")) continue;
            string pv = c[3].Replace("R$", "").Replace(" ", "");
            if (pv.Contains(',') && pv.Contains('.')) pv = pv.Replace(".", "");
            pv = pv.Replace(',', '.');
            double? preco = double.TryParse(pv, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
            r.Add(new PrecoUnitario { Codigo = c[0].ToUpperInvariant(), Descricao = c[1], Unidade = c[2], Preco = preco, Fonte = c.Length > 4 ? string.Join(sep.ToString(), c.Skip(4)) : "" });
        }
        return r;
    }

    /// <summary>CSV modelo com os itens e os preços atuais.</summary>
    public static string ModeloPrecos(ParametrosAlternativas p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("item;descrição;unidade;preço;fonte");
        foreach (var d in ParametrosAlternativas.PrecosPadrao())
        {
            var pr = p.Preco(d.Codigo);
            sb.AppendLine($"{d.Codigo};{pr.Descricao};{pr.Unidade};{(pr.Preco is { } v ? v.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR")) : "")};{pr.Fonte}");
        }
        return sb.ToString();
    }
}
