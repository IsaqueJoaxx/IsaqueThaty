using Projeta.Core.Comum;
using Projeta.Core.Relatorios;

namespace Projeta.Core.Pavimentacao;

/// <summary>Um elemento da seção que vira um grupo de colunas na nota (bordo da pista, drenagem, meio-fio, passeio...).</summary>
public sealed class ElementoSecao
{
    /// <summary>Título do grupo na planilha.</summary>
    public string Nome { get; set; } = "";
    /// <summary>Códigos de ponto do corredor, em ordem de preferência: vale o primeiro que existir na seção.</summary>
    public List<string> Codigos { get; set; } = new();
    /// <summary>Mostra a coluna Incl. (%) — inclinação do ponto anterior (ou do eixo) até este ponto.</summary>
    public bool Inclinacao { get; set; }

    public ElementoSecao() { }
    public ElementoSecao(string nome, bool inclinacao, params string[] codigos)
    {
        Nome = nome; Inclinacao = inclinacao; Codigos = codigos.ToList();
    }

    public override string ToString() => $"{Nome} = {string.Join(" | ", Codigos)}{(Inclinacao ? " (com Incl.)" : "")}";
}

/// <summary>Parâmetros da nota de serviço de pavimentação (gravados fora do DWG, valem para os próximos usos).</summary>
public sealed class ConfigNotaServico
{
    /// <summary>Distância entre as linhas regulares (m). As estacas são múltiplos deste valor.</summary>
    public double Intervalo { get; set; } = 10;
    /// <summary>Comprimento da estaca (m) para escrever 12+5,300.</summary>
    public double IntervaloEstaca { get; set; } = 20;
    public int CasasEstaca { get; set; } = 3;
    /// <summary>Inclui o início e o fim de cada região do corredor (onde a seção muda).</summary>
    public bool IncluirRegioes { get; set; } = true;
    /// <summary>Esconde os grupos de colunas sem nenhum valor (ex.: lado esquerdo sem passeio).</summary>
    public bool OmitirColunasVazias { get; set; } = true;
    /// <summary>Pontos a menos disto do eixo (m) não contam como lado esquerdo nem direito.</summary>
    public double ToleranciaEixo { get; set; } = 0.001;
    /// <summary>Elementos do eixo para fora (iguais nos dois lados; o lado esquerdo sai espelhado na planilha).</summary>
    public List<ElementoSecao> Elementos { get; set; } = Padrao();

    /// <summary>Inclui, de cada lado, as banquetas e o offset (crista do corte / pé do aterro).</summary>
    public bool Terraplenagem { get; set; } = true;
    /// <summary>Códigos do offset (aceitam * e ?). Vale o ponto mais afastado do eixo que casar.</summary>
    public List<string> CodigosOffset { get; set; } = PadraoOffset();
    /// <summary>Códigos dos pontos de banqueta (aceitam * e ?). Do eixo para fora, cada par de pontos é uma banqueta (início e fim).</summary>
    public List<string> CodigosBanqueta { get; set; } = PadraoBanqueta();
    /// <summary>Sem código de offset na seção: usa o ponto mais externo que está sobre o terreno (o "pega" do talude).</summary>
    public bool OffsetPeloTerreno { get; set; } = true;

    /// <summary>Códigos dos subassemblies padrão do Civil 3D (Lane, UrbanCurbGutter, UrbanSidewalk) e dos subassemblies PROJETA.</summary>
    public static List<ElementoSecao> Padrao() => new()
    {
        new("Bordo Pista", true, "ETW", "BORDO_PISTA"),
        new("Drenagem", true, "Flowline_Gutter", "FLUXO_SARJETA"),
        new("Topo Meio Fio", false, "Back_Curb", "Top_Curb", "FIM_GUIA", "TOPO_GUIA"),
        new("Passeio", true, "Sidewalk_Out"),
    };

    public static List<string> PadraoOffset() => new() { "*OFFSET*", "Daylight*", "Corte*33-*", "Aterro*33-*" };
    public static List<string> PadraoBanqueta() => new() { "*BERMA*", "*Ban*", "*Bench*" };

    /// <summary>Atualiza uma configuração gravada por versão anterior: listas de códigos iguais ao padrão antigo viram o padrão novo.</summary>
    public void Migrar()
    {
        var antigos = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bordo Pista"] = new[] { "ETW" },
            ["Drenagem"] = new[] { "Flowline_Gutter" },
            ["Topo Meio Fio"] = new[] { "Back_Curb", "Top_Curb" },
        };
        var novos = Padrao();
        foreach (var e in Elementos ?? new())
            if (antigos.TryGetValue(e.Nome ?? "", out var velho) && (e.Codigos ?? new()).SequenceEqual(velho, StringComparer.OrdinalIgnoreCase))
                e.Codigos = novos.First(n => string.Equals(n.Nome, e.Nome, StringComparison.OrdinalIgnoreCase)).Codigos;
        if (CodigosOffset is null || CodigosOffset.Count == 0) CodigosOffset = PadraoOffset();
        if (CodigosBanqueta is null || CodigosBanqueta.Count == 0) CodigosBanqueta = PadraoBanqueta();
    }

    public void Validar()
    {
        if (!(Intervalo > 0)) throw new ArgumentException("O intervalo entre linhas deve ser positivo.");
        if (!(IntervaloEstaca > 0)) throw new ArgumentException("O comprimento da estaca deve ser positivo.");
        if (CasasEstaca < 0 || CasasEstaca > 6) throw new ArgumentException("Casas decimais da estaca: de 0 a 6.");
        if (!(ToleranciaEixo >= 0)) throw new ArgumentException("A tolerância do eixo não pode ser negativa.");
        if (Elementos is null || Elementos.Count == 0) throw new ArgumentException("Informe ao menos um elemento da seção.");
        foreach (var e in Elementos)
        {
            if (string.IsNullOrWhiteSpace(e.Nome)) throw new ArgumentException("Há um elemento da seção sem nome.");
            e.Codigos = Limpar(e.Codigos);
            if (e.Codigos.Count == 0) throw new ArgumentException($"O elemento '{e.Nome}' não tem código de ponto.");
        }
        CodigosOffset = Limpar(CodigosOffset);
        CodigosBanqueta = Limpar(CodigosBanqueta);
    }

    private static List<string> Limpar(List<string>? l) =>
        (l ?? new()).Select(c => c.Trim()).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Text.RegularExpressions.Regex> _padroes = new();

    /// <summary>O código casa com o padrão? Sem diferença de maiúsculas; * = qualquer sequência, ? = um caractere.</summary>
    public static bool Casa(string codigo, string padrao)
    {
        if (padrao.IndexOfAny(new[] { '*', '?' }) < 0) return string.Equals(codigo, padrao, StringComparison.OrdinalIgnoreCase);
        var rx = _padroes.GetOrAdd(padrao, p => new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(p).Replace("\\*", ".*").Replace("\\?", ".") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant));
        return rx.IsMatch(codigo);
    }

    public static bool CasaAlgum(IEnumerable<string> codigos, IEnumerable<string> padroes) =>
        codigos.Any(c => padroes.Any(p => Casa(c, p)));
}

public enum TipoTrechoEixo { Reta, Curva, Espiral }

/// <summary>Trecho do alinhamento horizontal, em estacas (m).</summary>
public readonly record struct TrechoEixo(TipoTrechoEixo Tipo, double Inicio, double Fim, bool Horario = false);

public readonly record struct PontoNotavel(double Estaca, string Rotulo);

/// <summary>Ponto da seção: afastamento do eixo (m, negativo à esquerda) e cota (m).</summary>
public readonly record struct PontoSecao(double Afastamento, double Cota);

/// <summary>Uma linha da nota (uma estaca).</summary>
public sealed class LinhaNota
{
    public double Estaca { get; set; }
    public string NotavelH { get; set; } = "";
    public string NotavelV { get; set; } = "";
    public double CotaProjeto { get; set; } = double.NaN;
    public double CotaTerreno { get; set; } = double.NaN;
    /// <summary>Um item por elemento da configuração (null = elemento ausente nesta estaca).</summary>
    public PontoSecao?[] Esquerdo { get; set; } = Array.Empty<PontoSecao?>();
    public PontoSecao?[] Direito { get; set; } = Array.Empty<PontoSecao?>();
    /// <summary>Pontos de banqueta de cada lado, do eixo para fora (pares = início e fim de cada banqueta).</summary>
    public PontoSecao[] BanquetasEsquerdo { get; set; } = Array.Empty<PontoSecao>();
    public PontoSecao[] BanquetasDireito { get; set; } = Array.Empty<PontoSecao>();
    /// <summary>Offset de cada lado: onde o talude encontra o terreno (crista do corte ou pé do aterro).</summary>
    public PontoSecao? OffsetEsquerdo { get; set; }
    public PontoSecao? OffsetDireito { get; set; }
    /// <summary>A estaca não é uma seção calculada do corredor: valores interpolados entre as seções vizinhas.</summary>
    public bool Interpolada { get; set; }
    public string? Regiao { get; set; }

    /// <summary>Projeto − terreno com as cotas já arredondadas a mm (zero sai como "-", igual à planilha).</summary>
    public double CotaVermelha => Math.Round(Math.Round(CotaProjeto, 3) - Math.Round(CotaTerreno, 3), 3);
}

/// <summary>
/// Nota de serviço de pavimentação: uma linha por estaca com eixo (cota de projeto, terreno e vermelha, pontos notáveis)
/// e os pontos da seção de cada lado (afastamento, cota e inclinação). Gera a planilha no layout E-NS (Excel).
/// </summary>
public sealed class NotaServico
{
    public const double Tolerancia = 5e-4;

    /// <summary>Nome do trecho (normalmente o alinhamento), vai no subtítulo.</summary>
    public string Titulo { get; set; } = "";
    public ConfigNotaServico Config { get; set; } = new();
    public List<LinhaNota> Linhas { get; } = new();
    /// <summary>De onde vieram os dados (corredor, baseline, greide, superfície...), vai na aba Origem.</summary>
    public List<(string Item, string Valor)> Origem { get; } = new();
    public List<string> Avisos { get; } = new();

    public double EstacaInicial => Linhas.Count > 0 ? Linhas[0].Estaca : double.NaN;
    public double EstacaFinal => Linhas.Count > 0 ? Linhas[^1].Estaca : double.NaN;
    public string Estaca(double e) => FormatarEstaca(e, Config.IntervaloEstaca, Config.CasasEstaca);

    // ------------------------------------------------------------------ cálculo (sem Civil 3D)

    /// <summary>Estaca no formato 12+5,300, arredondando antes de separar (39,9996 → 2+0,000 e não 1+20,000).</summary>
    public static string FormatarEstaca(double metros, double intervalo = 20, int casas = 3)
    {
        if (double.IsNaN(metros) || double.IsInfinity(metros)) return "";
        double r = Math.Round(metros, casas, MidpointRounding.AwayFromZero);
        long n = (long)Math.Floor(r / intervalo + 1e-9);
        double resto = Math.Round(r - n * intervalo, casas, MidpointRounding.AwayFromZero);
        if (resto >= intervalo) { n++; resto = 0; }
        if (resto < 0) resto = 0;
        return $"{n}+{resto.ToString("F" + casas, Num.Br)}";
    }

    /// <summary>
    /// Rótulos dos pontos notáveis do eixo horizontal a partir dos trechos em ordem:
    /// PC/PT (curva circular), TS/SC/CS/ST (espirais), PCC (curva composta) e PRC (curva reversa).
    /// </summary>
    public static List<PontoNotavel> PontosNotaveisHorizontais(IReadOnlyList<TrechoEixo> trechos, double tol = Tolerancia)
    {
        var r = new List<PontoNotavel>();
        for (int i = 0; i < trechos.Count; i++)
        {
            var t = trechos[i];
            TrechoEixo? ant = i > 0 && Math.Abs(trechos[i - 1].Fim - t.Inicio) <= Math.Max(tol, 0.01) ? trechos[i - 1] : null;
            if (i > 0 && ant is null)
            {
                var fimAnt = Rotulo(trechos[i - 1], null);
                if (fimAnt is not null) r.Add(new(trechos[i - 1].Fim, fimAnt));
            }
            var ini = Rotulo(ant, t);
            if (ini is not null) r.Add(new(t.Inicio, ini));
        }
        if (trechos.Count > 0)
        {
            var fim = Rotulo(trechos[^1], null);
            if (fim is not null) r.Add(new(trechos[^1].Fim, fim));
        }
        return r;
    }

    private static string? Rotulo(TrechoEixo? a, TrechoEixo? b) => (a?.Tipo, b?.Tipo) switch
    {
        (null or TipoTrechoEixo.Reta, TipoTrechoEixo.Curva) => "PC",
        (null or TipoTrechoEixo.Reta, TipoTrechoEixo.Espiral) => "TS",
        (TipoTrechoEixo.Curva, null or TipoTrechoEixo.Reta) => "PT",
        (TipoTrechoEixo.Espiral, null or TipoTrechoEixo.Reta) => "ST",
        (TipoTrechoEixo.Espiral, TipoTrechoEixo.Curva) => "SC",
        (TipoTrechoEixo.Curva, TipoTrechoEixo.Espiral) => "CS",
        (TipoTrechoEixo.Curva, TipoTrechoEixo.Curva) => a!.Value.Horario == b!.Value.Horario ? "PCC" : "PRC",
        (TipoTrechoEixo.Espiral, TipoTrechoEixo.Espiral) => "SS",
        _ => null,
    };

    /// <summary>
    /// Estacas da nota: início e fim, múltiplos do intervalo, pontos notáveis e estacas extras (mudanças de região),
    /// em ordem, juntando as que caem a menos de <paramref name="tol"/> uma da outra.
    /// </summary>
    public static List<(double Estaca, string H, string V)> Estacas(double ini, double fim, double intervalo,
        IEnumerable<PontoNotavel>? horizontais = null, IEnumerable<PontoNotavel>? verticais = null,
        IEnumerable<double>? extras = null, double tol = Tolerancia, int maximo = 100_000)
    {
        if (double.IsNaN(ini) || double.IsNaN(fim) || fim < ini) throw new ArgumentException($"Trecho inválido: {ini} a {fim}.");
        if (!(intervalo > 0)) throw new ArgumentException("O intervalo deve ser positivo.");
        if ((fim - ini) / intervalo > maximo) throw new ArgumentException($"Seriam mais de {maximo} linhas; aumente o intervalo.");

        var itens = new List<(double E, string H, string V)> { (ini, "", "") };
        for (double k = Math.Floor(ini / intervalo + 1e-9) + 1; k * intervalo < fim - tol; k++) itens.Add((k * intervalo, "", ""));
        itens.Add((fim, "", ""));
        bool Dentro(double e) => e >= ini - tol && e <= fim + tol;
        double Prender(double e) => Math.Min(Math.Max(e, ini), fim);
        foreach (var p in horizontais ?? Enumerable.Empty<PontoNotavel>()) if (Dentro(p.Estaca)) itens.Add((Prender(p.Estaca), p.Rotulo, ""));
        foreach (var p in verticais ?? Enumerable.Empty<PontoNotavel>()) if (Dentro(p.Estaca)) itens.Add((Prender(p.Estaca), "", p.Rotulo));
        foreach (var e in extras ?? Enumerable.Empty<double>()) if (Dentro(e)) itens.Add((Prender(e), "", ""));

        var r = new List<(double Estaca, string H, string V)>();
        foreach (var it in itens.OrderBy(t => t.E))
        {
            if (r.Count > 0 && it.E - r[^1].Estaca <= tol)
            {
                var u = r[^1];
                r[^1] = (u.Estaca, Juntar(u.H, it.H), Juntar(u.V, it.V));
            }
            else r.Add(it);
        }
        return r;
    }

    private static string Juntar(string a, string b)
    {
        if (b.Length == 0 || a.Split('/').Contains(b)) return a;
        return a.Length == 0 ? b : a + "/" + b;
    }

    /// <summary>
    /// Ponto numa estaca entre duas seções calculadas: interpola o afastamento e a altura em relação ao greide,
    /// e soma a cota do greide na estaca (assim a curva vertical sai exata e só a forma da seção é interpolada).
    /// </summary>
    public static PontoSecao? Interpolar(double s, double s0, PontoSecao? p0, double greide0, double s1, PontoSecao? p1, double greide1, double greide)
    {
        if (p0 is not { } a || p1 is not { } b) return null;
        double t = Math.Abs(s1 - s0) < 1e-9 ? 0 : (s - s0) / (s1 - s0);
        double off = a.Afastamento + t * (b.Afastamento - a.Afastamento);
        if (double.IsNaN(greide0) || double.IsNaN(greide1) || double.IsNaN(greide))
            return new PontoSecao(off, a.Cota + t * (b.Cota - a.Cota));
        double d0 = a.Cota - greide0, d1 = b.Cota - greide1;
        return new PontoSecao(off, greide + d0 + t * (d1 - d0));
    }

    /// <summary>Inclinação (%) do elemento <paramref name="i"/>: do ponto presente mais próximo do eixo (ou do eixo) até ele, para fora.</summary>
    public static double Inclinacao(IReadOnlyList<PontoSecao?> lado, int i, double cotaEixo)
    {
        if (i < 0 || i >= lado.Count || lado[i] is not { } p) return double.NaN;
        double a0 = 0, c0 = cotaEixo;
        for (int j = i - 1; j >= 0; j--)
            if (lado[j] is { } q) { a0 = q.Afastamento; c0 = q.Cota; break; }
        double da = Math.Abs(p.Afastamento) - Math.Abs(a0);
        if (Math.Abs(da) < 1e-6 || double.IsNaN(c0)) return double.NaN;
        return (p.Cota - c0) / da * 100;
    }

    // ------------------------------------------------------------------ terraplenagem

    /// <summary>"C" se o offset fica acima da borda da plataforma (ponto mais externo dos elementos do lado, ou o eixo), senão "A".</summary>
    public static string CorteAterro(PontoSecao offset, IReadOnlyList<PontoSecao?> lado, double cotaEixo)
    {
        double refAf = 0, refCota = cotaEixo;
        foreach (var p in lado)
            if (p is { } q && Math.Abs(q.Afastamento) > Math.Abs(refAf)) { refAf = q.Afastamento; refCota = q.Cota; }
        if (double.IsNaN(refCota)) return "";
        return offset.Cota >= refCota ? "C" : "A";
    }

    /// <summary>Número de banquetas de um lado (cada par de pontos é uma banqueta: início e fim).</summary>
    public static int Banquetas(IReadOnlyCollection<PontoSecao> pontos) => (pontos.Count + 1) / 2;

    /// <summary>Banquetas numa estaca entre duas seções: só interpola se as duas têm o mesmo número de pontos.</summary>
    public static PontoSecao[] InterpolarLista(double s, double s0, PontoSecao[] a, double greide0, double s1, PontoSecao[] b, double greide1, double greide)
    {
        if (a.Length == 0 || a.Length != b.Length) return Array.Empty<PontoSecao>();
        var r = new PontoSecao[a.Length];
        for (int i = 0; i < a.Length; i++) r[i] = Interpolar(s, s0, a[i], greide0, s1, b[i], greide1, greide)!.Value;
        return r;
    }

    // ------------------------------------------------------------------ planilha

    private const string Cinza = "D9D9D9";
    private const string Formato3 = "_-* #,##0.000_-;\\-* #,##0.000_-;_-* \"-\"???_-;_-@_-";
    private const string Formato2 = "_-* #,##0.00_-;\\-* #,##0.00_-;_-* \"-\"??_-;_-@_-";

    /// <summary>Índices dos elementos que aparecem em cada lado (esquerdo de fora para dentro, direito de dentro para fora).</summary>
    public (List<int> Esquerdo, List<int> Direito) Colunas()
    {
        int n = Config.Elementos.Count;
        bool Usa(Func<LinhaNota, PontoSecao?[]> lado, int i) =>
            !Config.OmitirColunasVazias || Linhas.Any(l => i < lado(l).Length && lado(l)[i] is not null);
        var esq = Enumerable.Range(0, n).Where(i => Usa(l => l.Esquerdo, i)).Reverse().ToList();
        var dir = Enumerable.Range(0, n).Where(i => Usa(l => l.Direito, i)).ToList();
        return (esq, dir);
    }

    /// <summary>Uma coluna: título, largura e o valor em cada linha (double, texto ou null).</summary>
    private sealed record Col(string Titulo, double Largura, Func<LinhaNota, object?> Valor, int Casas = 3);
    private sealed record Grp(string Nome, List<Col> Cols);

    /// <summary>Grupos de colunas de um lado, na ordem da planilha (o esquerdo sai espelhado: de fora para dentro).</summary>
    private List<Grp> Grupos(bool esquerdo)
    {
        var (esq, dir) = Colunas();
        var idx = esquerdo ? Enumerable.Reverse(esq).ToList() : dir;
        PontoSecao?[] Lado(LinhaNota l) => esquerdo ? l.Esquerdo : l.Direito;
        PontoSecao[] Banq(LinhaNota l) => esquerdo ? l.BanquetasEsquerdo : l.BanquetasDireito;
        PontoSecao? Off(LinhaNota l) => esquerdo ? l.OffsetEsquerdo : l.OffsetDireito;
        static PontoSecao? Em(IReadOnlyList<PontoSecao?> a, int i) => i < a.Count ? a[i] : null;
        static PontoSecao? EmB(PontoSecao[] a, int i) => i < a.Length ? a[i] : null;

        var g = new List<Grp>();
        foreach (var i in idx)
        {
            var cols = new List<Col>
            {
                new("Afast. (m)", 9.3, l => Em(Lado(l), i)?.Afastamento),
                new("Cota (m)", 9.3, l => Em(Lado(l), i)?.Cota),
            };
            if (Config.Elementos[i].Inclinacao) cols.Add(new("Incl. (%)", 8.3, l => Inclinacao(Lado(l), i, l.CotaProjeto), 2));
            g.Add(new Grp(Config.Elementos[i].Nome, cols));
        }
        if (Config.Terraplenagem)
        {
            int nb = Linhas.Count == 0 ? 0 : Linhas.Max(l => Banquetas(Banq(l)));
            for (int k = 0; k < nb; k++)
            {
                int a = 2 * k, b = 2 * k + 1;
                g.Add(new Grp($"Banqueta {k + 1}", new List<Col>
                {
                    new("Afast. início (m)", 9.3, l => EmB(Banq(l), a)?.Afastamento),
                    new("Cota início (m)", 9.3, l => EmB(Banq(l), a)?.Cota),
                    new("Afast. fim (m)", 9.3, l => EmB(Banq(l), b)?.Afastamento),
                    new("Cota fim (m)", 9.3, l => EmB(Banq(l), b)?.Cota),
                }));
            }
            if (!Config.OmitirColunasVazias || Linhas.Any(l => Off(l) is not null))
                g.Add(new Grp("Offset", new List<Col>
                {
                    new("Afast. (m)", 9.3, l => Off(l)?.Afastamento),
                    new("Cota (m)", 9.3, l => Off(l)?.Cota),
                    new("C/A", 5.5, l => Off(l) is { } o ? CorteAterro(o, Lado(l), l.CotaProjeto) : null),
                }));
        }
        if (esquerdo) g.Reverse();
        return g;
    }

    public PastaXlsx Planilha()
    {
        if (Linhas.Count == 0) throw new InvalidOperationException("A nota de serviço não tem linhas.");
        var pasta = new PastaXlsx { Titulo = $"Nota de Serviço {Titulo}".Trim() };
        var f = pasta.NovaFolha("Nota de Serviço");
        var gE = Grupos(true);
        var gD = Grupos(false);
        int nEsq = gE.Sum(x => x.Cols.Count), nDir = gD.Sum(x => x.Cols.Count), total = nEsq + 6 + nDir;
        int cEixo = nEsq + 1, cDir = cEixo + 6;

        var titulo = new EstiloCelula { Tamanho = 20, Negrito = true, Italico = true, Fundo = Cinza, Horizontal = AlinhamentoH.Centro };
        var faixa = titulo with { Tamanho = 11 };
        var grupo = new EstiloCelula { Tamanho = 9, Negrito = true, Italico = true, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var num3 = new EstiloCelula { Tamanho = 9, Formato = Formato3 };
        var num2 = num3 with { Formato = Formato2 };
        var texto = new EstiloCelula { Tamanho = 9, Horizontal = AlinhamentoH.Centro };

        f.Mesclar(1, 1, 1, total, "Nota de Serviço", titulo);
        var sub = $"{Titulo} - Estaca {Estaca(EstacaInicial)} a {Estaca(EstacaFinal)}".TrimStart(' ', '-');
        f.Mesclar(2, 1, 2, total, sub, faixa);
        if (nEsq > 0) f.Mesclar(3, 1, 3, nEsq, "Lado Esquerdo", faixa);
        f.Mesclar(3, cEixo, 3, cEixo + 5, "Eixo", faixa);
        if (nDir > 0) f.Mesclar(3, cDir, 3, total, "Lado Direito", faixa);
        bool longos = gE.Concat(gD).Any(x => x.Nome.StartsWith("Banqueta", StringComparison.Ordinal));
        f.Altura(1, 40); f.Altura(2, 26); f.Altura(3, 18); f.Altura(4, 24); f.Altura(5, longos ? 30 : 18);

        void Cabecalho(int c, List<Grp> gs)
        {
            foreach (var g in gs)
            {
                f.Mesclar(4, c, 4, c + g.Cols.Count - 1, g.Nome, grupo);
                foreach (var col in g.Cols) { f.Texto(5, c, col.Titulo, grupo); f.Largura(c, col.Largura); c++; }
            }
        }
        Cabecalho(1, gE);
        Cabecalho(cDir, gD);
        string[] eixo = { "Estaca", "Pontos Notáveis Horizontal", "Pontos Notáveis Vertical", "Cota Projeto", "Cota Terreno", "Cota Vermelha" };
        double[] largEixo = { 10.5, 9.5, 9.5, 9.3, 9.3, 9.3 };
        for (int k = 0; k < 6; k++) { f.Mesclar(4, cEixo + k, 5, cEixo + k, eixo[k], grupo); f.Largura(cEixo + k, largEixo[k]); }

        void Dados(int lin, int c, List<Grp> gs, LinhaNota l)
        {
            foreach (var g in gs)
                foreach (var col in g.Cols)
                {
                    var est = col.Casas == 2 ? num2 : num3;
                    switch (col.Valor(l))
                    {
                        case double d: f.Numero(lin, c, R(d, col.Casas), est); break;
                        case string s: f.Texto(lin, c, s, texto); break;
                        default: f.Vazio(lin, c, est); break;
                    }
                    c++;
                }
        }

        int lin = 6;
        foreach (var l in Linhas)
        {
            f.Altura(lin, 16.5);
            Dados(lin, 1, gE, l);
            f.Texto(lin, cEixo, Estaca(l.Estaca), texto);
            f.Texto(lin, cEixo + 1, l.NotavelH, texto);
            f.Texto(lin, cEixo + 2, l.NotavelV, texto);
            f.Numero(lin, cEixo + 3, R(l.CotaProjeto, 3), num3);
            f.Numero(lin, cEixo + 4, R(l.CotaTerreno, 3), num3);
            f.Numero(lin, cEixo + 5, l.CotaVermelha, num3);
            Dados(lin, cDir, gD, l);
            lin++;
        }

        f.CongelarLinhas = 5;
        f.TituloImpressaoAte = 5;
        f.Paisagem = true;
        f.CaberNaLargura = true;
        // banquetas alargam a nota: A3 e, se ainda for larga demais, duas páginas de largura (letra legível na impressão)
        f.Papel = total > 30 ? 8 : 9;
        f.PaginasLargura = total > 44 ? 2 : 1;
        f.MostrarGrade = false;
        f.Rodape = "&CPágina &P de &N";

        AbaOrigem(pasta);
        return pasta;
    }

    private static double R(double v, int casas) => double.IsNaN(v) ? v : Math.Round(v, casas, MidpointRounding.AwayFromZero) + 0.0;

    private void AbaOrigem(PastaXlsx pasta)
    {
        var f = pasta.NovaFolha("Origem");
        var tit = new EstiloCelula { Tamanho = 12, Negrito = true, Borda = false };
        var rot = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2" };
        var val = new EstiloCelula { Tamanho = 10, Quebra = true };
        f.Largura(1, 30); f.Largura(2, 110);
        f.Texto(1, 1, "Origem dos dados da nota de serviço", tit);
        int lin = 3;
        void Item(string a, string b) { f.Texto(lin, 1, a, rot); f.Texto(lin, 2, b, val); lin++; }
        foreach (var (a, b) in Origem) Item(a, b);
        Item("Trecho", $"{Estaca(EstacaInicial)} a {Estaca(EstacaFinal)} ({Linhas.Count} linhas)");
        Item("Intervalo entre linhas", $"{Config.Intervalo.ToString("0.###", Num.Br)} m (estaca de {Config.IntervaloEstaca.ToString("0.###", Num.Br)} m)");
        foreach (var e in Config.Elementos)
            Item("Códigos — " + e.Nome, string.Join(", ", e.Codigos) + (e.Inclinacao ? "  (com inclinação)" : ""));
        if (Config.Terraplenagem)
        {
            Item("Códigos — Banquetas", string.Join(", ", Config.CodigosBanqueta) + "  (do eixo para fora, cada par de pontos = início e fim de uma banqueta)");
            Item("Códigos — Offset", string.Join(", ", Config.CodigosOffset) +
                 (Config.OffsetPeloTerreno ? "  (sem esses códigos: ponto mais externo sobre o terreno)" : "") +
                 ". C/A: C quando o offset fica acima da borda da plataforma, A quando abaixo.");
        }
        var interp = Linhas.Where(l => l.Interpolada).Select(l => Estaca(l.Estaca)).ToList();
        Item("Estacas interpoladas", interp.Count == 0
            ? "nenhuma — todas as linhas são seções calculadas do corredor"
            : $"{interp.Count}: {string.Join("; ", interp)}. Afastamento e altura sobre o greide interpolados entre as seções vizinhas do corredor; " +
              "para valores exatos, inclua essas estacas na frequência do corredor.");
        foreach (var a in Avisos) Item("Aviso", a);
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
