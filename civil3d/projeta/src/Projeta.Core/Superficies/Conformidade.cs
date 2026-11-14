using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Projeta.Core.Relatorios;

namespace Projeta.Core.Superficies;

/// <summary>Classe de desvio da obra em relação ao projeto (desvio = cota da obra − cota do projeto).</summary>
public sealed class FaixaDesvio
{
    /// <summary>+1 = obra acima do projeto (falta cortar), 0 = dentro da tolerância, −1 = obra abaixo do projeto (falta aterrar).</summary>
    public int Sinal { get; init; }
    /// <summary>Limites do |desvio| (m). Na faixa de dentro: De = tolerância para baixo, Ate = tolerância para cima.</summary>
    public double De { get; init; }
    public double Ate { get; init; } = double.PositiveInfinity;
    /// <summary>Cor RRGGBB.</summary>
    public string Cor { get; init; } = "";
    public double Area { get; set; }
    /// <summary>Soma de |desvio| × área das células da faixa (m³).</summary>
    public double Volume { get; set; }

    public string Rotulo => Sinal == 0
        ? $"Dentro da tolerância (-{M(De)} a +{M(Ate)} m)"
        : (Sinal > 0 ? "Acima do projeto (cortar): " : "Abaixo do projeto (aterrar): ") +
          (double.IsPositiveInfinity(Ate) ? $"mais de {M(De)} m" : $"{M(De)} a {M(Ate)} m");

    /// <summary>Rótulo curto para a legenda do desenho.</summary>
    public string RotuloCurto => Sinal == 0
        ? $"Dentro (-{M(De)} a +{M(Ate)} m)"
        : (Sinal > 0 ? "Acima " : "Abaixo ") + (double.IsPositiveInfinity(Ate) ? $"> {M(De)} m" : $"{M(De)} a {M(Ate)} m");

    internal static string M(double v) => v.ToString("0.00#", Num.Br);
}

/// <summary>Tolerâncias da obra em relação ao projeto e faixas de desvio além dela.</summary>
public sealed class ToleranciaObra
{
    /// <summary>Quanto a obra pode ficar acima do projeto (m).</summary>
    public double Acima { get; set; } = 0.05;
    /// <summary>Quanto a obra pode ficar abaixo do projeto (m).</summary>
    public double Abaixo { get; set; } = 0.05;
    /// <summary>Limites (m) das faixas fora da tolerância; os que não passam da tolerância são ignorados.</summary>
    public List<double> Limites { get; set; } = new() { 0.10, 0.20 };
    /// <summary>Área dentro da tolerância (%) a partir da qual a área é considerada conforme.</summary>
    public double PercentualMinimo { get; set; } = 95;

    public const string CorDentro = "91CF60";
    private static readonly string[] CoresAcima = { "FEE08B", "FDAE61", "F46D43", "D73027", "A50026" };
    private static readonly string[] CoresAbaixo = { "E0F3F8", "ABD9E9", "74ADD1", "4575B4", "313695" };
    private const double Eps = 1e-9;

    public void Validar()
    {
        if (!(Acima >= 0) || !(Abaixo >= 0) || double.IsInfinity(Acima) || double.IsInfinity(Abaixo))
            throw new ArgumentException("As tolerâncias devem ser zero ou positivas.");
        if (Acima > 5 || Abaixo > 5) throw new ArgumentException("Tolerância acima de 5 m: confira a unidade (metros).");
        if (!(PercentualMinimo >= 0 && PercentualMinimo <= 100)) throw new ArgumentException("O percentual mínimo deve estar entre 0 e 100.");
        Limites = (Limites ?? new()).Where(x => x > 0 && !double.IsNaN(x) && !double.IsInfinity(x)).Distinct().OrderBy(x => x).ToList();
    }

    /// <summary>Faixas de cima para baixo: obra acima (mais alta primeiro), dentro, obra abaixo (mais funda por último).</summary>
    public List<FaixaDesvio> Faixas()
    {
        var acima = Lado(Acima, +1);
        acima.Reverse();
        var r = new List<FaixaDesvio>(acima) { new() { Sinal = 0, De = Abaixo, Ate = Acima, Cor = CorDentro } };
        r.AddRange(Lado(Abaixo, -1));
        return r;
    }

    private List<FaixaDesvio> Lado(double tol, int sinal)
    {
        var l = Limites.Where(x => x > tol + Eps).ToList();
        var cores = sinal > 0 ? CoresAcima : CoresAbaixo;
        var r = new List<FaixaDesvio>();
        double de = tol;
        for (int i = 0; i <= l.Count; i++)
        {
            double ate = i < l.Count ? l[i] : double.PositiveInfinity;
            int k = l.Count == 0 ? 3 : 1 + (int)Math.Round(i * 3.0 / l.Count);
            r.Add(new FaixaDesvio { Sinal = sinal, De = de, Ate = ate, Cor = cores[k] });
            de = ate;
        }
        return r;
    }

    /// <summary>O desvio está dentro da tolerância (limites inclusive)?</summary>
    public bool Dentro(double d) => !double.IsNaN(d) && d <= Acima + Eps && d >= -Abaixo - Eps;

    /// <summary>Quanto o desvio passa da tolerância (0 se dentro; NaN se sem dado).</summary>
    public double Excesso(double d) => double.IsNaN(d) ? double.NaN : d > Acima + Eps ? d - Acima : d < -Abaixo - Eps ? -Abaixo - d : 0;

    public static int IndiceDentro(IReadOnlyList<FaixaDesvio> faixas)
    {
        for (int i = 0; i < faixas.Count; i++) if (faixas[i].Sinal == 0) return i;
        throw new ArgumentException("Lista de faixas sem a faixa de dentro da tolerância.");
    }

    /// <summary>Índice da faixa do desvio (obra − projeto), ou −1 se não há dado.</summary>
    public static int Indice(IReadOnlyList<FaixaDesvio> faixas, double d, int dentro)
    {
        if (double.IsNaN(d)) return -1;
        var fd = faixas[dentro];
        if (d <= fd.Ate + Eps && d >= -fd.De - Eps) return dentro;
        int s = d > 0 ? 1 : -1;
        double a = Math.Abs(d);
        int ultimo = -1;
        for (int i = 0; i < faixas.Count; i++)
        {
            if (faixas[i].Sinal != s) continue;
            ultimo = i;
            if (a >= faixas[i].De - Eps && a < faixas[i].Ate) return i;
        }
        return ultimo;
    }

    public static int Indice(IReadOnlyList<FaixaDesvio> faixas, double d) => Indice(faixas, d, IndiceDentro(faixas));

    public string Descricao => $"+{FaixaDesvio.M(Acima)} / -{FaixaDesvio.M(Abaixo)} m";
}

/// <summary>Estatística dos desvios numa área (ponderada pela área das células).</summary>
public sealed class EstatisticaDesvio
{
    public string Nome { get; set; } = "";
    public double Area { get; private set; }
    public double AreaDentro { get; private set; }
    public double AreaAcima { get; private set; }
    public double AreaAbaixo { get; private set; }
    public int Celulas { get; private set; }
    /// <summary>Volume da obra acima do projeto (a cortar), m³. A malha soma; o Civil 3D troca pelo valor exato da superfície de volume.</summary>
    public double VolumeAcima { get; set; }
    /// <summary>Volume da obra abaixo do projeto (a aterrar), m³.</summary>
    public double VolumeAbaixo { get; set; }
    public double Maximo { get; private set; } = double.NaN;
    public double XMaximo { get; private set; } = double.NaN;
    public double YMaximo { get; private set; } = double.NaN;
    public double Minimo { get; private set; } = double.NaN;
    public double XMinimo { get; private set; } = double.NaN;
    public double YMinimo { get; private set; } = double.NaN;
    private double _s, _s2;

    public double Media => Area > 0 ? _s / Area : double.NaN;
    public double DesvioPadrao => Area > 0 ? Math.Sqrt(Math.Max(0, _s2 / Area - Media * Media)) : double.NaN;
    /// <summary>Erro médio quadrático (raiz da média dos quadrados dos desvios).</summary>
    public double Emq => Area > 0 ? Math.Sqrt(_s2 / Area) : double.NaN;
    public double PctDentro => Area > 0 ? AreaDentro / Area * 100 : double.NaN;
    public double PctAcima => Area > 0 ? AreaAcima / Area * 100 : double.NaN;
    public double PctAbaixo => Area > 0 ? AreaAbaixo / Area * 100 : double.NaN;

    public void Somar(double d, double area, int sinal, double x, double y)
    {
        Area += area;
        Celulas++;
        if (sinal == 0) AreaDentro += area; else if (sinal > 0) AreaAcima += area; else AreaAbaixo += area;
        _s += d * area;
        _s2 += d * d * area;
        if (d > 0) VolumeAcima += d * area; else VolumeAbaixo -= d * area;
        if (double.IsNaN(Maximo) || d > Maximo) { Maximo = d; XMaximo = x; YMaximo = y; }
        if (double.IsNaN(Minimo) || d < Minimo) { Minimo = d; XMinimo = x; YMinimo = y; }
    }

    public string Situacao(double pctMinimo) =>
        !(Area > 0) ? "Sem dados" : PctDentro >= pctMinimo - 1e-9 ? "Conforme" : "Não conforme";
}

/// <summary>Malha de desvios (obra − projeto) com a faixa de cada célula.</summary>
public sealed class ResultadoConformidade
{
    public double X0 { get; init; }
    public double Y0 { get; init; }
    public double Celula { get; init; }
    public int Linhas { get; init; }
    public int Colunas { get; init; }
    /// <summary>Faixa de cada célula (índice na lista de faixas; −1 = sem dado ou fora das áreas).</summary>
    public int[,] Faixa { get; init; } = new int[0, 0];
    /// <summary>Desvio no centro da célula (NaN = sem dado).</summary>
    public float[,] Desvio { get; init; } = new float[0, 0];
    public EstatisticaDesvio Geral { get; init; } = new();
    public List<EstatisticaDesvio> Areas { get; init; } = new();

    public (double X, double Y) Centro(int linha, int coluna) => (X0 + (coluna + 0.5) * Celula, Y0 + (linha + 0.5) * Celula);
}

/// <summary>Ponto de maior desvio fora da tolerância (um por região, para conferência em campo).</summary>
public sealed class PontoCritico
{
    public int Numero { get; set; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Desvio { get; set; }
    public double Excesso { get; set; }
    public int Faixa { get; set; }
    public double Estaca { get; set; } = double.NaN;
    public double Afastamento { get; set; } = double.NaN;
    public double CotaProjeto { get; set; } = double.NaN;
    public double CotaObra { get; set; } = double.NaN;
}

/// <summary>Desvios de uma estaca nos afastamentos do controle (NaN = sem as duas superfícies).</summary>
public sealed class LinhaControle
{
    public double Estaca { get; init; }
    public double[] Desvios { get; init; } = Array.Empty<double>();
}

/// <summary>Resumo de uma estaca do controle.</summary>
public readonly record struct ResumoEstaca(int Pontos, int Fora, double Media, double Maximo, double AfMaximo, double Minimo, double AfMinimo)
{
    public string Situacao => Pontos == 0 ? "Sem dados" : Fora == 0 ? "Conforme" : $"{Fora} ponto(s) fora";
}

/// <summary>Controle por estaca: desvios em afastamentos fixos ao longo de um alinhamento.</summary>
public sealed class ControleEstacas
{
    public string Alinhamento { get; set; } = "";
    public double[] Afastamentos { get; set; } = Array.Empty<double>();
    public List<LinhaControle> Linhas { get; } = new();
    public double IntervaloEstaca { get; set; } = 20;
    public int CasasEstaca { get; set; } = 3;

    public ResumoEstaca Resumo(LinhaControle l, ToleranciaObra t)
    {
        int n = 0, fora = 0;
        double s = 0, max = double.NaN, min = double.NaN, afMax = double.NaN, afMin = double.NaN;
        for (int i = 0; i < l.Desvios.Length && i < Afastamentos.Length; i++)
        {
            double d = l.Desvios[i];
            if (double.IsNaN(d)) continue;
            n++;
            s += d;
            if (!t.Dentro(d)) fora++;
            if (double.IsNaN(max) || d > max) { max = d; afMax = Afastamentos[i]; }
            if (double.IsNaN(min) || d < min) { min = d; afMin = Afastamentos[i]; }
        }
        return new ResumoEstaca(n, fora, n > 0 ? s / n : double.NaN, max, afMax, min, afMin);
    }
}

/// <summary>Cálculos do Projeto × obra: malha de desvios, pontos críticos e afastamentos do controle por estaca.</summary>
public static class Conformidade
{
    /// <summary>
    /// Amostra o desvio (obra − projeto) no centro de cada célula, classifica pelas faixas e soma as estatísticas gerais e
    /// de cada área. Com <paramref name="areas"/>, só entram as células dentro de alguma delas. <paramref name="dif"/>
    /// devolve NaN fora das superfícies. As áreas e volumes das faixas são zerados antes.
    /// </summary>
    public static ResultadoConformidade Calcular(double x0, double y0, double x1, double y1, double celula,
        Func<double, double, double> dif, IReadOnlyList<(string Nome, Func<double, double, bool> Dentro)>? areas,
        IList<FaixaDesvio> faixas, Malha.Ocupacao? ocupacao = null)
    {
        if (!(celula > 0)) throw new ArgumentException("O tamanho da célula deve ser positivo.");
        int cols = Math.Max(1, (int)Math.Ceiling((x1 - x0) / celula - 1e-9));
        int lins = Math.Max(1, (int)Math.Ceiling((y1 - y0) / celula - 1e-9));
        if ((long)cols * lins > 4_000_000) throw new ArgumentException($"Malha grande demais ({cols} × {lins} células); aumente a célula.");
        foreach (var f in faixas) { f.Area = 0; f.Volume = 0; }
        var lista = (IReadOnlyList<FaixaDesvio>)faixas.ToList();
        int dentro = ToleranciaObra.IndiceDentro(lista);
        bool comAreas = areas is { Count: > 0 };
        var geral = new EstatisticaDesvio { Nome = comAreas ? "Total" : "Sobreposição das superfícies" };
        var est = comAreas ? areas!.Select(a => new EstatisticaDesvio { Nome = a.Nome }).ToList() : new List<EstatisticaDesvio>();
        var marca = new bool[est.Count];
        var faixa = new int[lins, cols];
        var desv = new float[lins, cols];
        double a1 = celula * celula;
        for (int r = 0; r < lins; r++)
        {
            double cy = y0 + (r + 0.5) * celula;
            for (int c = 0; c < cols; c++)
            {
                double cx = x0 + (c + 0.5) * celula;
                faixa[r, c] = -1;
                desv[r, c] = float.NaN;
                if (ocupacao is not null && !ocupacao.Contem(cx, cy)) continue;
                if (comAreas)
                {
                    bool alguma = false;
                    for (int i = 0; i < marca.Length; i++) { marca[i] = areas![i].Dentro(cx, cy); alguma |= marca[i]; }
                    if (!alguma) continue;
                }
                double d = dif(cx, cy);
                if (double.IsNaN(d)) continue;
                int k = ToleranciaObra.Indice(lista, d, dentro);
                int sinal = faixas[k].Sinal;
                geral.Somar(d, a1, sinal, cx, cy);
                for (int i = 0; i < marca.Length; i++) if (marca[i]) est[i].Somar(d, a1, sinal, cx, cy);
                faixas[k].Area += a1;
                faixas[k].Volume += Math.Abs(d) * a1;
                faixa[r, c] = k;
                desv[r, c] = (float)d;
            }
        }
        return new ResultadoConformidade
        {
            X0 = x0, Y0 = y0, Celula = celula, Linhas = lins, Colunas = cols, Faixa = faixa, Desvio = desv, Geral = geral, Areas = est,
        };
    }

    /// <summary>
    /// Pontos de maior excesso sobre a tolerância, no máximo <paramref name="maximo"/>, separados por pelo menos
    /// <paramref name="distanciaMinima"/> (um por região, não vários na mesma mancha).
    /// </summary>
    public static List<PontoCritico> Criticos(ResultadoConformidade r, ToleranciaObra t, int maximo, double distanciaMinima)
    {
        var cand = new List<(double Exc, int L, int C)>();
        for (int l = 0; l < r.Linhas; l++)
            for (int c = 0; c < r.Colunas; c++)
            {
                double e = t.Excesso(r.Desvio[l, c]);
                if (e > 1e-9) cand.Add((e, l, c));
            }
        cand.Sort((a, b) => b.Exc.CompareTo(a.Exc));
        var sel = new List<PontoCritico>();
        double d2 = distanciaMinima * distanciaMinima;
        foreach (var (exc, l, c) in cand)
        {
            if (sel.Count >= maximo) break;
            var (x, y) = r.Centro(l, c);
            bool perto = false;
            foreach (var p in sel)
                if ((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y) < d2) { perto = true; break; }
            if (perto) continue;
            sel.Add(new PontoCritico { Numero = sel.Count + 1, X = x, Y = y, Desvio = r.Desvio[l, c], Excesso = exc, Faixa = r.Faixa[l, c] });
        }
        return sel;
    }

    /// <summary>
    /// Afastamentos "redondos" do controle por estaca entre <paramref name="minimo"/> e <paramref name="maximo"/>
    /// (negativo = esquerda), com o menor passo que cabe em <paramref name="maxColunas"/> colunas.
    /// </summary>
    public static double[] Afastamentos(double minimo, double maximo, int maxColunas = 41)
    {
        if (double.IsNaN(minimo) || double.IsNaN(maximo) || maximo < minimo) return Array.Empty<double>();
        double[] passos = { 0.5, 1, 2, 2.5, 5, 10, 20, 25, 50, 100 };
        double[]? ultimo = null;
        foreach (var p in passos)
        {
            double a = Math.Ceiling(minimo / p - 1e-9) * p, b = Math.Floor(maximo / p + 1e-9) * p;
            if (b < a) { a = Math.Round((minimo + maximo) / 2 / p) * p; b = a; }
            int n = (int)Math.Round((b - a) / p) + 1;
            ultimo = Enumerable.Range(0, n).Select(i => Math.Round(a + i * p, 3)).ToArray();
            if (n <= maxColunas) return ultimo;
        }
        return ultimo ?? Array.Empty<double>();
    }

    /// <summary>Estacas do controle: múltiplos do intervalo entre o início e o fim, mais o início e o fim.</summary>
    public static List<double> Estacas(double inicio, double fim, double intervalo)
    {
        if (!(intervalo > 0)) throw new ArgumentException("O intervalo entre estacas deve ser positivo.");
        var r = new List<double> { inicio };
        for (double s = Math.Ceiling(inicio / intervalo + 1e-9) * intervalo; s < fim - 1e-6; s += intervalo)
            if (s > inicio + 1e-6) r.Add(Math.Round(s, 6));
        if (fim > inicio + 1e-6) r.Add(fim);
        return r;
    }
}

/// <summary>Relatório do Projeto × obra: resumo por área, faixas, controle por estaca, pontos críticos e origem.</summary>
public sealed class RelatorioConformidade
{
    public string Projeto { get; set; } = "";
    public string Obra { get; set; } = "";
    public ToleranciaObra Tolerancia { get; set; } = new();
    public EstatisticaDesvio Geral { get; set; } = new();
    public List<EstatisticaDesvio> Areas { get; set; } = new();
    public List<FaixaDesvio> Faixas { get; set; } = new();
    public double Celula { get; set; }
    /// <summary>Volumes acima/abaixo vindos do cálculo exato (superfície de volume) e não da malha.</summary>
    public bool VolumesExatos { get; set; }
    public List<PontoCritico> Criticos { get; } = new();
    public ControleEstacas? Estacas { get; set; }
    public List<(string Item, string Valor)> Origem { get; } = new();
    public List<string> Avisos { get; } = new();

    /// <summary>Linhas do resumo: cada área e o total (ou só a sobreposição, sem áreas).</summary>
    public List<EstatisticaDesvio> Linhas => Areas.Count == 0 ? new() { Geral } : Areas.Count == 1 ? new(Areas) : Areas.Append(Geral).ToList();

    public string Situacao => Geral.Situacao(Tolerancia.PercentualMinimo);

    private const string Cinza = "D9D9D9";
    private const string FmtD = "+0.000;-0.000;0.000";
    private const string Fmt2 = "#,##0.00";
    private const string Fmt3 = "0.000";
    private const string Pct = "0.0%";

    public PastaXlsx Planilha()
    {
        var pasta = new PastaXlsx { Titulo = $"Projeto x obra {Projeto} x {Obra}" };
        var titulo = new EstiloCelula { Tamanho = 16, Negrito = true, Fundo = Cinza, Horizontal = AlinhamentoH.Centro };
        var sub = titulo with { Tamanho = 10, Negrito = false };
        var cab = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula { Tamanho = 10 };
        var txtC = txt with { Horizontal = AlinhamentoH.Centro };
        var num = new EstiloCelula { Tamanho = 10, Formato = Fmt2 };
        var dsv = new EstiloCelula { Tamanho = 10, Formato = FmtD };
        var pct = new EstiloCelula { Tamanho = 10, Formato = Pct };
        var ok = txtC with { Fundo = "C6EFCE", Negrito = true };
        var nok = txtC with { Fundo = "FFC7CE", Negrito = true };
        string legenda = $"Desvio = cota da obra - cota do projeto · tolerância {Tolerancia.Descricao} · conforme quando pelo menos " +
                         $"{Tolerancia.PercentualMinimo.ToString("0.#", Num.Br)} % da área está dentro da tolerância";

        // ---- Resumo
        var f = pasta.NovaFolha("Resumo");
        string[] cols = { "Área", "Área analisada (m²)", "Dentro da tolerância", "Acima do projeto", "Abaixo do projeto",
                          "Desvio médio (m)", "Desvio-padrão (m)", "EMQ (m)", "Desvio máximo (m)", "Desvio mínimo (m)",
                          "Volume acima do projeto - cortar (m³)", "Volume abaixo do projeto - aterrar (m³)", "Situação" };
        double[] larg = { 30, 13, 12, 11, 11, 11, 11, 10, 11, 11, 15, 15, 15 };
        int n = cols.Length;
        f.Mesclar(1, 1, 1, n, "Projeto × obra - conformidade da superfície executada", titulo);
        f.Mesclar(2, 1, 2, n, $"Projeto: {Projeto}   ×   Obra (executado): {Obra}", sub);
        f.Mesclar(3, 1, 3, n, legenda, sub);
        f.Altura(1, 30); f.Altura(2, 18); f.Altura(3, 18); f.Altura(5, 42);
        for (int c = 0; c < n; c++) { f.Texto(5, c + 1, cols[c], cab); f.Largura(c + 1, larg[c]); }
        int lin = 6;
        foreach (var e in Linhas)
        {
            bool tot = ReferenceEquals(e, Geral) && Areas.Count > 1;
            EstiloCelula N(EstiloCelula s) => tot ? s with { Negrito = true } : s;
            f.Texto(lin, 1, e.Nome, N(txt));
            f.Numero(lin, 2, Math.Round(e.Area, 2), N(num));
            f.Numero(lin, 3, e.PctDentro / 100, N(pct));
            f.Numero(lin, 4, e.PctAcima / 100, N(pct));
            f.Numero(lin, 5, e.PctAbaixo / 100, N(pct));
            f.Numero(lin, 6, R3(e.Media), N(dsv));
            f.Numero(lin, 7, R3(e.DesvioPadrao), N(num with { Formato = Fmt3 }));
            f.Numero(lin, 8, R3(e.Emq), N(num with { Formato = Fmt3 }));
            f.Numero(lin, 9, R3(e.Maximo), N(dsv));
            f.Numero(lin, 10, R3(e.Minimo), N(dsv));
            f.Numero(lin, 11, Math.Round(e.VolumeAcima, 2), N(num));
            f.Numero(lin, 12, Math.Round(e.VolumeAbaixo, 2), N(num));
            var sit = e.Situacao(Tolerancia.PercentualMinimo);
            f.Texto(lin, 13, sit, sit == "Conforme" ? ok : sit == "Não conforme" ? nok : txtC);
            lin++;
        }
        lin++;
        var nota = new EstiloCelula { Tamanho = 9, Italico = true, Borda = false };
        f.Texto(lin++, 1, "Acima do projeto = a obra ficou mais alta que o projeto (falta cortar); abaixo = ficou mais baixa (falta aterrar).", nota);
        f.Texto(lin++, 1, "EMQ = erro médio quadrático dos desvios. Percentuais e estatísticas pela malha de " +
                          $"{F(Celula)} m; volumes {(VolumesExatos ? "pelo cálculo exato da superfície de volume do Civil 3D" : "pela malha")}.", nota);
        if (Criticos.Count > 0) f.Texto(lin++, 1, $"{Criticos.Count} ponto(s) crítico(s) na aba Pontos críticos, para conferência em campo.", nota);
        f.CongelarLinhas = 5;
        f.Paisagem = true;
        f.CaberNaLargura = true;
        f.TituloImpressaoAte = 5;

        // ---- Faixas
        if (Faixas.Count > 0)
        {
            var g = pasta.NovaFolha("Faixas de desvio");
            g.Mesclar(1, 1, 1, 5, "Área por faixa de desvio", titulo);
            g.Mesclar(2, 1, 2, 5, $"Malha de {F(Celula)} m · tolerância {Tolerancia.Descricao}", sub);
            string[] cf = { "", "Faixa", "Área (m²)", "% da área", "Volume (m³)" };
            double[] lf = { 4, 42, 14, 11, 14 };
            for (int c = 0; c < cf.Length; c++) { g.Texto(4, c + 1, cf[c], cab); g.Largura(c + 1, lf[c]); }
            double areaTot = Faixas.Sum(x => x.Area);
            int l2 = 5;
            foreach (var x in Faixas)
            {
                g.Vazio(l2, 1, new EstiloCelula { Fundo = x.Cor });
                g.Texto(l2, 2, x.Rotulo, x.Sinal == 0 ? txt with { Negrito = true } : txt);
                g.Numero(l2, 3, Math.Round(x.Area, 2), num);
                g.Numero(l2, 4, areaTot > 0 ? x.Area / areaTot : double.NaN, pct);
                g.Numero(l2, 5, Math.Round(x.Volume, 2), num);
                l2++;
            }
            g.Vazio(l2, 1, txt);
            g.Texto(l2, 2, "Total", txt with { Negrito = true });
            g.Numero(l2, 3, Math.Round(areaTot, 2), num with { Negrito = true });
            g.Numero(l2, 4, areaTot > 0 ? 1 : double.NaN, pct with { Negrito = true });
            g.Numero(l2, 5, Math.Round(Faixas.Sum(x => x.Volume), 2), num with { Negrito = true });
            g.Texto(l2 + 2, 2, "Volume da faixa = soma de |desvio| × área das células (m³).", nota);
        }

        // ---- Por estaca
        if (Estacas is { Linhas.Count: > 0 } ce && ce.Afastamentos.Length > 0)
        {
            var h = pasta.NovaFolha("Por estaca");
            int na = ce.Afastamentos.Length;
            string[] cr = { "Pontos", "Fora da tolerância", "Desvio médio (m)", "Desvio máximo (m)", "Afast. (m)", "Desvio mínimo (m)", "Afast. (m)", "Situação" };
            double[] lr = { 8, 10, 10, 10, 9, 10, 9, 16 };
            int total = 1 + na + cr.Length;
            h.Mesclar(1, 1, 1, total, "Projeto × obra - controle por estaca", titulo);
            h.Mesclar(2, 1, 2, total, $"Alinhamento {ce.Alinhamento} · desvio da obra em relação ao projeto (m) · tolerância {Tolerancia.Descricao} · afastamento negativo = esquerda", sub);
            h.Mesclar(3, 1, 3, total, "Células coloridas = fora da tolerância (laranja/vermelho: obra acima do projeto, cortar; azul: obra abaixo, aterrar) · \"-\" = sem as duas superfícies", sub);
            h.Altura(1, 30); h.Altura(2, 18); h.Altura(3, 18); h.Altura(5, 30);
            h.Mesclar(4, 1, 5, 1, "Estaca", cab);
            h.Largura(1, 12);
            h.Mesclar(4, 2, 4, 1 + na, "Desvio por afastamento do eixo (m)", cab);
            var cabN = cab with { Formato = "+0.0#;-0.0#;0.0#" };
            for (int i = 0; i < na; i++) { h.Numero(5, 2 + i, ce.Afastamentos[i], cabN); h.Largura(2 + i, 7.5); }
            for (int i = 0; i < cr.Length; i++) { h.Mesclar(4, 2 + na + i, 5, 2 + na + i, cr[i], cab); h.Largura(2 + na + i, lr[i]); }
            var dsvP = new EstiloCelula { Tamanho = 9, Formato = FmtD };
            var vazio = new EstiloCelula { Tamanho = 9, Horizontal = AlinhamentoH.Centro };
            var lista = (IReadOnlyList<FaixaDesvio>)Faixas;
            int dentro = Faixas.Count > 0 ? ToleranciaObra.IndiceDentro(lista) : -1;
            int l3 = 6;
            foreach (var lc in ce.Linhas)
            {
                h.Texto(l3, 1, NotaServico.FormatarEstaca(lc.Estaca, ce.IntervaloEstaca, ce.CasasEstaca), txtC);
                for (int i = 0; i < na; i++)
                {
                    double d = i < lc.Desvios.Length ? lc.Desvios[i] : double.NaN;
                    if (double.IsNaN(d)) { h.Texto(l3, 2 + i, "-", vazio); continue; }
                    var est = dsvP;
                    if (!Tolerancia.Dentro(d) && dentro >= 0)
                    {
                        int k = ToleranciaObra.Indice(lista, d, dentro);
                        if (k >= 0) est = dsvP with { Fundo = Clara(Faixas[k].Cor), Negrito = true };
                    }
                    h.Numero(l3, 2 + i, R3(d), est);
                }
                var rs = ce.Resumo(lc, Tolerancia);
                int c0 = 2 + na;
                h.Numero(l3, c0, rs.Pontos, txtC);
                h.Numero(l3, c0 + 1, rs.Fora, txtC);
                h.Numero(l3, c0 + 2, R3(rs.Media), dsv);
                h.Numero(l3, c0 + 3, R3(rs.Maximo), dsv);
                h.Numero(l3, c0 + 4, rs.AfMaximo, num);
                h.Numero(l3, c0 + 5, R3(rs.Minimo), dsv);
                h.Numero(l3, c0 + 6, rs.AfMinimo, num);
                h.Texto(l3, c0 + 7, rs.Situacao, rs.Pontos == 0 ? txtC : rs.Fora == 0 ? ok : nok);
                l3++;
            }
            h.CongelarLinhas = 5;
            h.TituloImpressaoAte = 5;
            h.Paisagem = true;
            h.CaberNaLargura = true;
            h.Papel = total > 30 ? 8 : 9;
            h.PaginasLargura = total > 44 ? 2 : 1;
        }

        // ---- Pontos críticos
        if (Criticos.Count > 0)
        {
            var p = pasta.NovaFolha("Pontos críticos");
            string[] cp = { "Nº", "E (m)", "N (m)", "Estaca", "Afast. (m)", "Cota do projeto (m)", "Cota da obra (m)", "Desvio (m)", "Passa da tolerância (m)", "Classe" };
            double[] lp = { 6, 14, 14, 12, 10, 12, 12, 10, 12, 44 };
            p.Mesclar(1, 1, 1, cp.Length, "Pontos críticos - maior desvio fora da tolerância em cada região", titulo);
            p.Mesclar(2, 1, 2, cp.Length, $"Projeto: {Projeto} × obra: {Obra} · tolerância {Tolerancia.Descricao} · numerados do maior para o menor excesso", sub);
            p.Altura(1, 30); p.Altura(4, 44);
            for (int c = 0; c < cp.Length; c++) { p.Texto(4, c + 1, cp[c], cab); p.Largura(c + 1, lp[c]); }
            var crd = num with { Formato = Fmt3 };
            int l4 = 5;
            foreach (var q in Criticos)
            {
                p.Numero(l4, 1, q.Numero, txtC);
                p.Numero(l4, 2, Math.Round(q.X, 3), crd);
                p.Numero(l4, 3, Math.Round(q.Y, 3), crd);
                if (double.IsNaN(q.Estaca)) p.Texto(l4, 4, "-", txtC);
                else p.Texto(l4, 4, NotaServico.FormatarEstaca(q.Estaca, Estacas?.IntervaloEstaca ?? 20, Estacas?.CasasEstaca ?? 3), txtC);
                p.Numero(l4, 5, Math.Round(q.Afastamento, 2), num);
                p.Numero(l4, 6, R3(q.CotaProjeto), crd);
                p.Numero(l4, 7, R3(q.CotaObra), crd);
                var fx = q.Faixa >= 0 && q.Faixa < Faixas.Count ? Faixas[q.Faixa] : null;
                p.Numero(l4, 8, R3(q.Desvio), fx is null ? dsv : dsv with { Fundo = Clara(fx.Cor), Negrito = true });
                p.Numero(l4, 9, R3(q.Excesso), crd);
                p.Texto(l4, 10, fx?.Rotulo ?? "", txt);
                l4++;
            }
            p.CongelarLinhas = 4;
            p.TituloImpressaoAte = 4;
            p.Paisagem = true;
            p.CaberNaLargura = true;
        }

        // ---- Origem
        var o = pasta.NovaFolha("Origem");
        var rot = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2" };
        var val = new EstiloCelula { Tamanho = 10, Quebra = true };
        o.Largura(1, 30); o.Largura(2, 110);
        o.Texto(1, 1, "Origem dos dados", new EstiloCelula { Tamanho = 12, Negrito = true, Borda = false });
        int l5 = 3;
        void Item(string a, string b) { o.Texto(l5, 1, a, rot); o.Texto(l5, 2, b, val); l5++; }
        foreach (var (a, b) in Origem) Item(a, b);
        Item("Superfície de projeto", Projeto);
        Item("Superfície da obra (executado)", Obra);
        Item("Convenção", "Desvio = cota da obra - cota do projeto: positivo = obra acima do projeto (cortar), negativo = obra abaixo (aterrar).");
        Item("Tolerância", $"{Tolerancia.Descricao}; faixas além dela em {string.Join(" / ", Tolerancia.Limites.Select(x => F(x) + " m"))}; " +
                           $"conforme com pelo menos {Tolerancia.PercentualMinimo.ToString("0.#", Num.Br)} % da área dentro da tolerância.");
        if (Celula > 0) Item("Malha", $"células de {F(Celula)} m (desvio no centro de cada célula)");
        foreach (var a in Avisos) Item("Aviso", a);
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);

    private static double R3(double v) => double.IsNaN(v) ? double.NaN : Math.Round(v, 3);
    private static string F(double v) => v.ToString("0.###", Num.Br);

    /// <summary>Cor da faixa clareada (metade do caminho até o branco), para o número continuar legível na célula.</summary>
    internal static string Clara(string rgb)
    {
        if (rgb.Length != 6) return rgb;
        int C(int i) => (Convert.ToInt32(rgb.Substring(i, 2), 16) + 255 + 1) / 2;
        return $"{C(0):X2}{C(2):X2}{C(4):X2}";
    }
}
