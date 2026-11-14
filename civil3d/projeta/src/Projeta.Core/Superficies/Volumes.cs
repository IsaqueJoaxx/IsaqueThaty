using Projeta.Core.Comum;
using Projeta.Core.Relatorios;

namespace Projeta.Core.Superficies;

/// <summary>Faixa de altura de corte ou de aterro (mapa de cores e quadro por faixas).</summary>
public sealed class FaixaAltura
{
    public bool Corte { get; init; }
    /// <summary>Limites da faixa em metros (valor absoluto da diferença entre as superfícies).</summary>
    public double De { get; init; }
    public double Ate { get; init; } = double.PositiveInfinity;
    /// <summary>Cor RRGGBB.</summary>
    public string Cor { get; init; } = "";
    public double Area { get; set; }
    public double Volume { get; set; }

    public string Rotulo => (Corte ? "Corte " : "Aterro ") +
        (double.IsPositiveInfinity(Ate) ? $"acima de {M(De)} m" : $"{M(De)} a {M(Ate)} m");

    private static string M(double v) => v.ToString("0.00", Num.Br);
}

/// <summary>Volumes de uma área (polígono ou a superfície inteira), em m³ geométricos (sem fator).</summary>
public sealed class AreaVolume
{
    public string Nome { get; set; } = "";
    public double Area2D { get; set; }
    public double Corte { get; set; }
    public double Aterro { get; set; }
}

/// <summary>Resultado da amostragem em malha da diferença entre as superfícies (comparação − base).</summary>
public sealed class ResultadoMalha
{
    public double X0 { get; init; }
    public double Y0 { get; init; }
    public double Celula { get; init; }
    public int Linhas { get; init; }
    public int Colunas { get; init; }
    /// <summary>Faixa de cada célula (índice na lista de faixas; −1 = fora ou sem alteração).</summary>
    public int[,] Faixa { get; init; } = new int[0, 0];
    public double Area { get; set; }
    public double Corte { get; set; }
    public double Aterro { get; set; }
}

/// <summary>Cálculos de malha usados no mapa de corte/aterro e no quadro por faixas de altura.</summary>
public static class Malha
{
    public static readonly double[] LimitesPadrao = { 0.5, 1, 2, 3 };
    private static readonly string[] CoresCorte = { "FDD49E", "FDBB84", "FC8D59", "E34A33", "B30000" };
    private static readonly string[] CoresAterro = { "C6DBEF", "9ECAE1", "6BAED6", "3182BD", "08519C" };
    /// <summary>Diferença abaixo disto (m) conta como "sem alteração": não pinta nem entra nas faixas.</summary>
    public const double Neutro = 0.01;

    /// <summary>Faixas de corte (primeiro) e de aterro, das mais rasas para as mais profundas.</summary>
    public static List<FaixaAltura> Faixas(IReadOnlyList<double>? limites = null)
    {
        var l = (limites ?? LimitesPadrao).Where(x => x > 0 && !double.IsNaN(x)).Distinct().OrderBy(x => x).ToList();
        var r = new List<FaixaAltura>();
        foreach (var corte in new[] { true, false })
        {
            var cores = corte ? CoresCorte : CoresAterro;
            double de = 0;
            for (int i = 0; i <= l.Count; i++)
            {
                double ate = i < l.Count ? l[i] : double.PositiveInfinity;
                int k = l.Count == 0 ? cores.Length - 1 : (int)Math.Round(i * (cores.Length - 1.0) / l.Count);
                r.Add(new FaixaAltura { Corte = corte, De = de, Ate = ate, Cor = cores[k] });
                de = ate;
            }
        }
        return r;
    }

    /// <summary>Índice da faixa da diferença <paramref name="d"/> (comparação − base; negativo = corte), ou −1.</summary>
    public static int Indice(IReadOnlyList<FaixaAltura> faixas, double d)
    {
        if (double.IsNaN(d) || Math.Abs(d) < Neutro) return -1;
        bool corte = d < 0;
        double a = Math.Abs(d);
        for (int i = 0; i < faixas.Count; i++)
            if (faixas[i].Corte == corte && a >= faixas[i].De && a < faixas[i].Ate) return i;
        return -1;
    }

    /// <summary>Lado da célula "redondo" para no máximo <paramref name="maxCelulas"/> células na área.</summary>
    public static double TamanhoCelula(double area, int maxCelulas = 60000)
    {
        double[] passos = { 0.25, 0.5, 1, 2, 2.5, 5, 10, 20, 25, 50, 100 };
        foreach (var p in passos) if (area / (p * p) <= maxCelulas) return p;
        return Math.Ceiling(Math.Sqrt(area / maxCelulas));
    }

    /// <summary>Área do polígono (fórmula do laço), m².</summary>
    public static double AreaPoligono(IReadOnlyList<(double X, double Y)> p)
    {
        double s = 0;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += (p[j].X + p[i].X) * (p[j].Y - p[i].Y);
        return Math.Abs(s) / 2;
    }

    /// <summary>Ponto dentro do polígono (paridade de cruzamentos).</summary>
    public static bool Dentro(IReadOnlyList<(double X, double Y)> p, double x, double y)
    {
        bool dentro = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            if ((p[i].Y > y) != (p[j].Y > y) && x < (p[j].X - p[i].X) * (y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)
                dentro = !dentro;
        return dentro;
    }

    /// <summary>Células grossas onde há dados (com uma célula de folga em volta) e a área estimada com dados.</summary>
    public sealed class Ocupacao
    {
        public double X0 { get; init; }
        public double Y0 { get; init; }
        public double Celula { get; init; }
        public bool[,] Ativo { get; init; } = new bool[0, 0];
        public double AreaEstimada { get; init; }

        public bool Contem(double x, double y)
        {
            int c = (int)Math.Floor((x - X0) / Celula), l = (int)Math.Floor((y - Y0) / Celula);
            return l >= 0 && c >= 0 && l < Ativo.GetLength(0) && c < Ativo.GetLength(1) && Ativo[l, c];
        }
    }

    /// <summary>
    /// Passada grossa: marca onde a diferença existe (superfícies sobrepostas) para a malha fina só amostrar ali —
    /// num corredor estreito o retângulo envolvente é quase todo vazio.
    /// </summary>
    public static Ocupacao Ocupar(double x0, double y0, double x1, double y1, double grossa, Func<double, double, double> dif)
    {
        int cols = Math.Max(1, (int)Math.Ceiling((x1 - x0) / grossa - 1e-9));
        int lins = Math.Max(1, (int)Math.Ceiling((y1 - y0) / grossa - 1e-9));
        var ok = new bool[lins, cols];
        int n = 0;
        for (int l = 0; l < lins; l++)
            for (int c = 0; c < cols; c++)
                if (!double.IsNaN(dif(x0 + (c + 0.5) * grossa, y0 + (l + 0.5) * grossa))) { ok[l, c] = true; n++; }
        var ativo = new bool[lins, cols];
        for (int l = 0; l < lins; l++)
            for (int c = 0; c < cols; c++)
                if (ok[l, c])
                    for (int dl = -1; dl <= 1; dl++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int a = l + dl, b = c + dc;
                            if (a >= 0 && b >= 0 && a < lins && b < cols) ativo[a, b] = true;
                        }
        return new Ocupacao { X0 = x0, Y0 = y0, Celula = grossa, Ativo = ativo, AreaEstimada = n * grossa * grossa };
    }

    /// <summary>
    /// Amostra a diferença no centro de cada célula do retângulo, soma área e volumes e acumula nas faixas
    /// (as áreas e volumes das faixas são zerados antes). <paramref name="dif"/> devolve NaN fora das superfícies.
    /// Com <paramref name="ocupacao"/>, só amostra as células que caem nas células grossas ativas.
    /// </summary>
    public static ResultadoMalha Calcular(double x0, double y0, double x1, double y1, double celula,
        Func<double, double, double> dif, Func<double, double, bool>? dentro, IList<FaixaAltura> faixas, Ocupacao? ocupacao = null)
    {
        if (!(celula > 0)) throw new ArgumentException("O tamanho da célula deve ser positivo.");
        int cols = Math.Max(1, (int)Math.Ceiling((x1 - x0) / celula - 1e-9));
        int lins = Math.Max(1, (int)Math.Ceiling((y1 - y0) / celula - 1e-9));
        if ((long)cols * lins > 4_000_000) throw new ArgumentException($"Malha grande demais ({cols} × {lins} células); aumente a célula.");
        foreach (var f in faixas) { f.Area = 0; f.Volume = 0; }
        var faixa = new int[lins, cols];
        double a1 = celula * celula, area = 0, corte = 0, aterro = 0;
        for (int r = 0; r < lins; r++)
        {
            double cy = y0 + (r + 0.5) * celula;
            for (int c = 0; c < cols; c++)
            {
                double cx = x0 + (c + 0.5) * celula;
                faixa[r, c] = -1;
                if (ocupacao is not null && !ocupacao.Contem(cx, cy)) continue;
                if (dentro is not null && !dentro(cx, cy)) continue;
                double d = dif(cx, cy);
                if (double.IsNaN(d)) continue;
                area += a1;
                if (d < 0) corte -= d * a1; else aterro += d * a1;
                int k = Indice((IReadOnlyList<FaixaAltura>)faixas, d);
                faixa[r, c] = k;
                if (k >= 0) { faixas[k].Area += a1; faixas[k].Volume += Math.Abs(d) * a1; }
            }
        }
        return new ResultadoMalha { X0 = x0, Y0 = y0, Celula = celula, Linhas = lins, Colunas = cols, Faixa = faixa, Area = area, Corte = corte, Aterro = aterro };
    }

    /// <summary>Trechos contínuos de mesma faixa em cada linha da malha (um retângulo por trecho no desenho).</summary>
    public static List<(int Linha, int Col0, int Col1, int Faixa)> Corridas(int[,] faixa)
    {
        var r = new List<(int, int, int, int)>();
        int lins = faixa.GetLength(0), cols = faixa.GetLength(1);
        for (int l = 0; l < lins; l++)
        {
            int c = 0;
            while (c < cols)
            {
                int f = faixa[l, c];
                if (f < 0) { c++; continue; }
                int c1 = c;
                while (c1 + 1 < cols && faixa[l, c1 + 1] == f) c1++;
                r.Add((l, c, c1, f));
                c = c1 + 1;
            }
        }
        return r;
    }
}

/// <summary>Relatório de volumes entre duas superfícies, com fatores e quadro por faixas de altura.</summary>
public sealed class RelatorioVolumes
{
    public string Base { get; set; } = "";
    public string Comparacao { get; set; } = "";
    /// <summary>Multiplica o corte geométrico (empolamento, quando o corte vai para transporte).</summary>
    public double FatorCorte { get; set; } = 1;
    /// <summary>Multiplica o aterro geométrico (volume de corte necessário para compactar o aterro).</summary>
    public double FatorAterro { get; set; } = 1;
    public List<AreaVolume> Areas { get; } = new();
    public List<FaixaAltura> Faixas { get; set; } = new();
    /// <summary>Lado da célula da malha (m) usada no mapa e nas faixas; 0 = sem malha.</summary>
    public double Celula { get; set; }
    public double CorteMalha { get; set; }
    public double AterroMalha { get; set; }
    public List<(string Item, string Valor)> Origem { get; } = new();
    public List<string> Avisos { get; } = new();

    public AreaVolume Total => new()
    {
        Nome = "Total",
        Area2D = Areas.Sum(a => a.Area2D),
        Corte = Areas.Sum(a => a.Corte),
        Aterro = Areas.Sum(a => a.Aterro),
    };

    /// <summary>Corte × fator − aterro × fator: positivo = sobra (bota-fora), negativo = falta (empréstimo).</summary>
    public double Saldo(AreaVolume a) => a.Corte * FatorCorte - a.Aterro * FatorAterro;

    /// <summary>
    /// Equilíbrio quando o saldo é menor que 1 % de <paramref name="referencia"/> (o maior dos volumes com fator) ou que 0,5 m³;
    /// senão sobra (bota-fora) ou falta (empréstimo).
    /// </summary>
    public static string Situacao(double saldo, double referencia = 0) =>
        Math.Abs(saldo) < Math.Max(0.5, 0.01 * Math.Abs(referencia)) ? "Equilíbrio" : saldo > 0 ? "Sobra (bota-fora)" : "Falta (empréstimo)";

    /// <summary>Situação da área, com a tolerância de 1 % do maior volume (corte × fator ou aterro × fator).</summary>
    public string SituacaoDe(AreaVolume a) => Situacao(Saldo(a), Math.Max(a.Corte * FatorCorte, a.Aterro * FatorAterro));

    /// <summary>Diferença relativa entre o total da malha e o total exato (TIN), em % do maior volume.</summary>
    public double DiferencaMalha()
    {
        var t = Total;
        double refv = Math.Max(Math.Max(t.Corte, t.Aterro), 1e-9);
        return (Math.Abs(CorteMalha - t.Corte) + Math.Abs(AterroMalha - t.Aterro)) / 2 / refv * 100;
    }

    private const string Cinza = "D9D9D9";
    private const string Fmt2 = "#,##0.00";

    /// <summary>Planilha de volumes; com <paramref name="pasta"/>, as abas entram depois das que ela já tem.</summary>
    public PastaXlsx Planilha(PastaXlsx? pasta = null)
    {
        if (Areas.Count == 0) throw new InvalidOperationException("Nenhuma área calculada.");
        pasta ??= new PastaXlsx { Titulo = $"Volumes {Base} x {Comparacao}" };
        var titulo = new EstiloCelula { Tamanho = 16, Negrito = true, Fundo = Cinza, Horizontal = AlinhamentoH.Centro };
        var sub = titulo with { Tamanho = 10, Negrito = false };
        var cab = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula { Tamanho = 10 };
        var num = new EstiloCelula { Tamanho = 10, Formato = Fmt2 };
        var numN = num with { Negrito = true };
        var txtN = txt with { Negrito = true };

        // ---- Volumes
        var f = pasta.NovaFolha("Volumes");
        string[] cols = { "Área", "Área 2D (m²)", "Corte (m³)", "Aterro (m³)", $"Corte × {F(FatorCorte)} (m³)", $"Aterro × {F(FatorAterro)} (m³)", "Saldo (m³)", "Situação" };
        double[] larg = { 34, 14, 14, 14, 16, 16, 14, 20 };
        int n = cols.Length;
        f.Mesclar(1, 1, 1, n, "Volumes entre superfícies", titulo);
        f.Mesclar(2, 1, 2, n, $"Base: {Base}   ×   Comparação: {Comparacao}", sub);
        f.Mesclar(3, 1, 3, n, $"Fator do corte {F(FatorCorte)} · fator do aterro {F(FatorAterro)} · saldo = corte × fator − aterro × fator (positivo = sobra, negativo = falta)", sub);
        f.Altura(1, 30); f.Altura(2, 18); f.Altura(3, 18); f.Altura(5, 30);
        for (int c = 0; c < n; c++) { f.Texto(5, c + 1, cols[c], cab); f.Largura(c + 1, larg[c]); }
        int lin = 6;
        void Linha(AreaVolume a, bool negrito)
        {
            var en = negrito ? numN : num;
            f.Texto(lin, 1, a.Nome, negrito ? txtN : txt);
            f.Numero(lin, 2, Math.Round(a.Area2D, 2), en);
            f.Numero(lin, 3, Math.Round(a.Corte, 2), en);
            f.Numero(lin, 4, Math.Round(a.Aterro, 2), en);
            f.Numero(lin, 5, Math.Round(a.Corte * FatorCorte, 2), en);
            f.Numero(lin, 6, Math.Round(a.Aterro * FatorAterro, 2), en);
            f.Numero(lin, 7, Math.Round(Saldo(a), 2), en);
            f.Texto(lin, 8, SituacaoDe(a), negrito ? txtN : txt);
            lin++;
        }
        foreach (var a in Areas) Linha(a, false);
        if (Areas.Count > 1) Linha(Total, true);
        f.CongelarLinhas = 5;
        f.Paisagem = true;
        f.CaberNaLargura = true;
        f.TituloImpressaoAte = 5;

        // ---- Faixas
        if (Celula > 0 && Faixas.Count > 0)
        {
            var g = pasta.NovaFolha("Faixas de altura");
            g.Mesclar(1, 1, 1, 5, "Corte e aterro por faixa de altura", titulo);
            g.Mesclar(2, 1, 2, 5, $"Malha de {F(Celula)} m · diferença para o cálculo exato: {DiferencaMalha().ToString("0.0", Num.Br)} %", sub);
            string[] cf = { "", "Faixa", "Área (m²)", "% da área", "Volume (m³)" };
            double[] lf = { 4, 26, 14, 11, 14 };
            for (int c = 0; c < cf.Length; c++) { g.Texto(4, c + 1, cf[c], cab); g.Largura(c + 1, lf[c]); }
            double areaTot = Faixas.Sum(x => x.Area);
            var pct = num with { Formato = "0.0%" };
            int l2 = 5;
            foreach (var corte in new[] { true, false })
            {
                foreach (var x in Faixas.Where(x => x.Corte == corte))
                {
                    g.Vazio(l2, 1, new EstiloCelula { Fundo = x.Cor });
                    g.Texto(l2, 2, x.Rotulo, txt);
                    g.Numero(l2, 3, Math.Round(x.Area, 2), num);
                    g.Numero(l2, 4, areaTot > 0 ? x.Area / areaTot : double.NaN, pct);
                    g.Numero(l2, 5, Math.Round(x.Volume, 2), num);
                    l2++;
                }
                var tot = Faixas.Where(x => x.Corte == corte).ToList();
                g.Vazio(l2, 1, txt);
                g.Texto(l2, 2, corte ? "Total de corte" : "Total de aterro", txtN);
                g.Numero(l2, 3, Math.Round(tot.Sum(x => x.Area), 2), numN);
                g.Numero(l2, 4, areaTot > 0 ? tot.Sum(x => x.Area) / areaTot : double.NaN, pct with { Negrito = true });
                g.Numero(l2, 5, Math.Round(tot.Sum(x => x.Volume), 2), numN);
                l2 += 2;
            }
        }

        // ---- Origem
        var o = pasta.NovaFolha("Origem");
        var rot = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2" };
        var val = new EstiloCelula { Tamanho = 10, Quebra = true };
        o.Largura(1, 30); o.Largura(2, 110);
        o.Texto(1, 1, "Origem dos dados", new EstiloCelula { Tamanho = 12, Negrito = true, Borda = false });
        int l3 = 3;
        void Item(string a, string b) { o.Texto(l3, 1, a, rot); o.Texto(l3, 2, b, val); l3++; }
        foreach (var (a, b) in Origem) Item(a, b);
        Item("Superfície base", Base);
        Item("Superfície de comparação", Comparacao);
        Item("Convenção", "Corte = comparação abaixo da base; aterro = comparação acima da base.");
        if (Celula > 0)
            Item("Malha (mapa e faixas)", $"células de {F(Celula)} m; corte {Math.Round(CorteMalha, 1).ToString("#,##0.0", Num.Br)} m³ e aterro {Math.Round(AterroMalha, 1).ToString("#,##0.0", Num.Br)} m³ pela malha " +
                                         $"(diferença de {DiferencaMalha().ToString("0.0", Num.Br)} % para o cálculo exato, que é o da aba Volumes)");
        foreach (var a in Avisos) Item("Aviso", a);
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);

    private static string F(double v) => v.ToString("0.###", Num.Br);
}
