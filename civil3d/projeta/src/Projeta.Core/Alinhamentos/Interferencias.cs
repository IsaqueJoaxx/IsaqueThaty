using System.Globalization;
using System.Text;
using Projeta.Core.Relatorios;
using Projeta.Giro.Core.Geometria;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Interferências e desapropriação (PRJINTERFERENCIAS): o limite da obra (com afastamento) cruzado com os lotes (desenho, KML/KMZ
// ou shapefile) dá a área atingida por lote, o remanescente e a situação; cruzado com postes, árvores, redes, muros e edificações
// dá a lista do que a obra invade. Saem a planta (hachuras e marcas), os quadros e o Excel.
// ======================================================================================================================

public sealed class ParametrosInterferencias
{
    /// <summary>Afastamento somado ao limite da obra (m): faixa de segurança, de serviço ou de domínio.</summary>
    public double Afastamento { get; set; } = 0;
    /// <summary>Faixa de atenção fora do limite (m): o que fica a menos disso entra na lista como "próximo".</summary>
    public double FaixaAtencao { get; set; } = 2;
    /// <summary>Áreas atingidas menores que isso são ignoradas (m²).</summary>
    public double AreaMinima { get; set; } = 0.5;
    /// <summary>Lote mínimo (m²): remanescente menor que isso é apontado (Lei 6.766/79, art. 4º, II: 125 m²; a lei municipal pode exigir mais).</summary>
    public double LoteMinimo { get; set; } = 125;
    public bool ParametrosDoUsuario { get; set; }
}

public sealed class LoteDesapropriacao
{
    public string Id { get; set; } = "";
    public string Proprietario { get; set; } = "";
    public string Origem { get; set; } = "";
    /// <summary>Anéis do lote (externo e furos; regra não-nula: furos no sentido contrário).</summary>
    public List<List<(double X, double Y)>> Aneis { get; set; } = new();
}

public sealed class ResultadoLote
{
    public LoteDesapropriacao Lote { get; set; } = new();
    public double Area { get; set; }
    public double AreaAtingida { get; set; }
    public double Remanescente => Math.Max(0, Area - AreaAtingida);
    public double Percentual => Area > 0 ? AreaAtingida / Area : 0;
    public string Situacao { get; set; } = "";
    public List<List<(double X, double Y)>> Atingido { get; set; } = new();
    public (double X, double Y) Rotulo { get; set; }
    public string Obs { get; set; } = "";
}

/// <summary>Objeto existente a conferir: ponto (poste, árvore, PV) ou linha (rede, cerca, muro, edificação).</summary>
public sealed class ObjetoExistente
{
    public string Tipo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Camada { get; set; } = "";
    public bool Linear { get; set; }
    public bool Fechado { get; set; }
    public List<(double X, double Y)> Pontos { get; set; } = new();
}

public sealed class ResultadoInterferencia
{
    public ObjetoExistente Objeto { get; set; } = new();
    /// <summary>"dentro" (atingido) ou "próximo" (na faixa de atenção).</summary>
    public string Situacao { get; set; } = "";
    /// <summary>Distância ao limite (m): negativa = dentro.</summary>
    public double Distancia { get; set; }
    public double ComprimentoDentro { get; set; }
    public List<List<(double X, double Y)>> TrechosDentro { get; set; } = new();
    public (double X, double Y) Ponto { get; set; }
}

public sealed class AnaliseInterferencias
{
    public List<List<(double X, double Y)>> Limite { get; } = new();
    public double AreaLimite { get; set; }
    public List<ResultadoLote> Lotes { get; } = new();
    public List<ResultadoInterferencia> Interferencias { get; } = new();
    public List<string> Avisos { get; } = new();
}

public static class Interferencias
{
    private static V2 V((double X, double Y) p) => new(p.X, p.Y);
    private static List<V2> Vs(IEnumerable<(double X, double Y)> p) => p.Select(V).ToList();
    private static List<(double X, double Y)> Ts(IEnumerable<V2> p) => p.Select(q => (q.X, q.Y)).ToList();

    private static string SemAcento(string s)
    {
        var n = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in n) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().ToUpperInvariant();
    }

    /// <summary>Tipo do objeto pela camada, pelo nome do bloco e pela descrição (vazio = não reconhecido).</summary>
    public static string Classificar(params string?[] textos)
    {
        string t = SemAcento(string.Join(" ", textos.Where(x => !string.IsNullOrWhiteSpace(x))));
        bool Tem(params string[] k) => k.Any(x => t.Contains(x, StringComparison.Ordinal));
        if (Tem("POSTE", "ILUMIN", "LUMINARIA")) return "Poste";
        if (Tem("ARVORE", "ARV ", "ARV-", "ARV_", "TREE", "VEGET", "PALMEIRA")) return "Árvore";
        if (t == "ARV" || t.StartsWith("ARV", StringComparison.Ordinal) && t.Length <= 4) return "Árvore";
        if (Tem("DRENAG", "PLUVIA", "BOCA DE LOBO", "BOCA-DE-LOBO", "BOCALOBO", "GALERIA", "BUEIRO", "CAIXA COLETORA")) return "Drenagem";
        if (Tem("ESGOTO", "ESG", "SANITARI")) return "Rede de esgoto";
        if (Tem("AGUA", "ADUTORA", "HIDRANTE", "HIDROMETRO", "COPASA", "SABESP", "SANEAGO", "CEDAE")) return "Rede de água";
        if (Tem("ELETR", "ENERGIA", "CEMIG", "CPFL", "ENEL", "LIGHT", "COPEL", "CELESC", "TRANSFORMADOR")) return "Rede elétrica";
        if (Tem("TELEF", "TELEC", "FIBRA", "OPTIC", "OPTIK", "TELECOM")) return "Telecomunicações";
        if (Tem("GAS", "GASODUTO")) return "Gás";
        if (Tem("MURO", "CERCA", "ALAMBRADO", "GRADIL", "PORTAO")) return "Muro / cerca";
        if (Tem("EDIF", "CONSTRU", "CASA", "PREDIO", "GALPAO", "BENFEITORIA", "BUILDING")) return "Edificação";
        if (Tem("PLACA", "SINALIZ")) return "Placa";
        if (Tem("PV", "POCO DE VISITA")) return "Poço de visita";
        return "";
    }

    /// <summary>Limite da obra: união dos polígonos, aumentada do afastamento.</summary>
    public static List<List<(double X, double Y)>> Limite(IEnumerable<IReadOnlyList<(double X, double Y)>> poligonos, double afastamento)
    {
        var u = Poligonos.Uniao(poligonos.Where(p => p.Count >= 3).Select(p => (IReadOnlyList<V2>)Vs(p)));
        if (afastamento > 1e-6) u = Poligonos.Inflar(u.Cast<IReadOnlyList<V2>>(), afastamento);
        return u.Where(p => p.Count >= 3).Select(Ts).ToList();
    }

    /// <summary>Área com sinal (anti-horário +).</summary>
    public static double AreaComSinal(IReadOnlyList<(double X, double Y)> p) => Geo.Area(Vs(p));

    /// <summary>Área líquida de um conjunto de anéis (externos menos furos), qualquer que seja o sentido de cada anel.</summary>
    public static double AreaLiquida(IReadOnlyList<IReadOnlyList<(double X, double Y)>> aneis)
    {
        if (aneis.Count == 1) return Math.Abs(AreaComSinal(aneis[0]));
        var u = Poligonos.Uniao(aneis.Select(a => (IReadOnlyList<V2>)Vs(a)));
        return Math.Abs(u.Sum(a => Geo.Area(a)));
    }

    /// <summary>O ponto está dentro dos anéis (paridade: furos contam).</summary>
    public static bool Dentro((double X, double Y) q, IEnumerable<IReadOnlyList<(double X, double Y)>> aneis)
    {
        int n = 0;
        var v = V(q);
        foreach (var a in aneis) if (a.Count >= 3 && Geo.DentroPoligono(v, Vs(a))) n++;
        return n % 2 == 1;
    }

    /// <summary>Menor distância do ponto aos contornos (m).</summary>
    public static double DistanciaContorno((double X, double Y) q, IEnumerable<IReadOnlyList<(double X, double Y)>> aneis)
    {
        double m = double.PositiveInfinity;
        var v = V(q);
        foreach (var a in aneis)
            for (int i = 0; i < a.Count; i++)
                m = Math.Min(m, Geo.DistPontoSegmento(v, V(a[i]), V(a[(i + 1) % a.Count])));
        return m;
    }

    /// <summary>Ponto bom para o rótulo de um polígono: o centro da caixa se estiver dentro, senão o vértice médio.</summary>
    public static (double X, double Y) PontoInterno(IReadOnlyList<IReadOnlyList<(double X, double Y)>> aneis)
    {
        var maior = aneis.OrderByDescending(a => Math.Abs(AreaComSinal(a))).FirstOrDefault();
        if (maior is null || maior.Count == 0) return (0, 0);
        double cx = maior.Average(p => p.X), cy = maior.Average(p => p.Y);
        if (Dentro((cx, cy), aneis)) return (cx, cy);
        // varre uma linha horizontal pelo meio e pega o meio do maior trecho dentro
        double y = (maior.Min(p => p.Y) + maior.Max(p => p.Y)) / 2;
        var xs = new List<double>();
        foreach (var a in aneis)
            for (int i = 0; i < a.Count; i++)
            {
                var p = a[i]; var r = a[(i + 1) % a.Count];
                if ((p.Y > y) != (r.Y > y)) xs.Add(p.X + (y - p.Y) * (r.X - p.X) / (r.Y - p.Y));
            }
        xs.Sort();
        double melhor = -1, mx = cx;
        for (int i = 0; i + 1 < xs.Count; i += 2) if (xs[i + 1] - xs[i] > melhor) { melhor = xs[i + 1] - xs[i]; mx = (xs[i] + xs[i + 1]) / 2; }
        return (mx, y);
    }

    public static AnaliseInterferencias Analisar(IReadOnlyList<List<(double X, double Y)>> limite, IEnumerable<LoteDesapropriacao> lotes, IEnumerable<ObjetoExistente> objetos, ParametrosInterferencias p)
    {
        var a = new AnaliseInterferencias();
        a.Limite.AddRange(limite);
        var lim = limite.Select(x => (IReadOnlyList<V2>)Vs(x)).ToList();
        a.AreaLimite = Math.Abs(lim.Sum(x => Geo.Area(x)));
        var limT = limite.Cast<IReadOnlyList<(double X, double Y)>>().ToList();

        // desapropriação
        foreach (var l in lotes)
        {
            if (l.Aneis.Count == 0) continue;
            var aneis = l.Aneis.Cast<IReadOnlyList<(double X, double Y)>>().ToList();
            var r = new ResultadoLote { Lote = l, Area = AreaLiquida(aneis) };
            var inter = Poligonos.Intersecao(aneis.Select(x => (IReadOnlyList<V2>)Vs(x)), lim);
            double ai = Math.Abs(inter.Sum(x => Geo.Area(x)));
            if (ai >= p.AreaMinima)
            {
                r.AreaAtingida = Math.Min(ai, r.Area);
                r.Atingido = inter.Where(x => x.Count >= 3).Select(Ts).ToList();
                r.Rotulo = PontoInterno(r.Atingido.Cast<IReadOnlyList<(double X, double Y)>>().ToList());
                r.Situacao = r.Percentual >= 0.99 ? "total" : "parcial";
                if (r.Situacao == "parcial" && r.Remanescente < p.LoteMinimo)
                    r.Obs = $"remanescente de {r.Remanescente:0.00} m² abaixo do lote mínimo de {p.LoteMinimo:0} m²: avaliar a desapropriação total";
            }
            else { r.Situacao = "não atingido"; r.Rotulo = PontoInterno(aneis); }
            a.Lotes.Add(r);
        }

        // interferências
        foreach (var o in objetos)
        {
            if (o.Pontos.Count == 0) continue;
            if (!o.Linear)
            {
                var q = o.Pontos[0];
                bool dentro = Dentro(q, limT);
                double d = DistanciaContorno(q, limT);
                if (!dentro && d > p.FaixaAtencao) continue;
                a.Interferencias.Add(new ResultadoInterferencia { Objeto = o, Situacao = dentro ? "dentro" : "próximo", Distancia = dentro ? -d : d, Ponto = q });
            }
            else
            {
                var linha = o.Fechado && o.Pontos.Count >= 3 ? o.Pontos.Append(o.Pontos[0]).ToList() : o.Pontos;
                var trechos = Poligonos.LinhasDentro(new[] { (IReadOnlyList<V2>)Vs(linha) }, lim).Where(t => Geo.Comprimento(t) > 0.01).ToList();
                double comp = trechos.Sum(t => Geo.Comprimento(t));
                if (comp > 0.01)
                {
                    var t0 = trechos.OrderByDescending(t => Geo.Comprimento(t)).First();
                    var meio = Geo.PontoNaDistancia(t0, Geo.Acumulado(t0), Geo.Comprimento(t0) / 2).P;
                    a.Interferencias.Add(new ResultadoInterferencia { Objeto = o, Situacao = "dentro", ComprimentoDentro = comp, TrechosDentro = trechos.Select(Ts).ToList(), Ponto = (meio.X, meio.Y), Distancia = 0 });
                    continue;
                }
                // fora: a menor distância de um vértice ou segmento ao limite
                double dm = double.PositiveInfinity; (double X, double Y) pm = linha[0];
                foreach (var q in linha) { double d = DistanciaContorno(q, limT); if (d < dm) { dm = d; pm = q; } }
                if (dm <= p.FaixaAtencao) a.Interferencias.Add(new ResultadoInterferencia { Objeto = o, Situacao = "próximo", Distancia = dm, Ponto = pm });
            }
        }
        return a;
    }

    // ------------------------------------------------------------------------------------------------ shapefile

    /// <summary>Feição de um shapefile: anéis ou linhas (por partes), ou pontos, com os atributos do .dbf.</summary>
    public sealed record FeicaoShape(int Tipo, List<List<(double X, double Y)>> Partes, Dictionary<string, string> Atributos);

    /// <summary>
    /// Lê um shapefile (.shp com o .dbf ao lado, se houver): pontos (1, 11, 21), linhas (3, 13, 23), polígonos (5, 15, 25) e
    /// multipontos (8, 18, 28). As coordenadas vêm como estão no arquivo (quem chama converte se forem geográficas).
    /// </summary>
    public static List<FeicaoShape> LerShapefile(string shp)
    {
        var r = new List<FeicaoShape>();
        var bytes = File.ReadAllBytes(shp);
        if (bytes.Length < 100 || BigEndian(bytes, 0) != 9994) throw new InvalidDataException("O arquivo não é um shapefile (.shp).");
        var atributos = LerDbf(Path.ChangeExtension(shp, ".dbf"));
        int pos = 100, k = 0;
        while (pos + 8 <= bytes.Length)
        {
            int compr = BigEndian(bytes, pos + 4) * 2;
            int c = pos + 8;
            pos = c + compr;
            if (c + 4 > bytes.Length) break;
            int tipo = BitConverter.ToInt32(bytes, c);
            var partes = new List<List<(double X, double Y)>>();
            switch (tipo)
            {
                case 1: case 11: case 21:
                    partes.Add(new List<(double, double)> { (BitConverter.ToDouble(bytes, c + 4), BitConverter.ToDouble(bytes, c + 12)) });
                    break;
                case 3: case 13: case 23: case 5: case 15: case 25:
                    {
                        int np = BitConverter.ToInt32(bytes, c + 36), npt = BitConverter.ToInt32(bytes, c + 40);
                        int ip = c + 44, ipt = ip + 4 * np;
                        for (int j = 0; j < np; j++)
                        {
                            int ini = BitConverter.ToInt32(bytes, ip + 4 * j), fim = j + 1 < np ? BitConverter.ToInt32(bytes, ip + 4 * (j + 1)) : npt;
                            var lst = new List<(double, double)>();
                            for (int q = ini; q < fim; q++) lst.Add((BitConverter.ToDouble(bytes, ipt + 16 * q), BitConverter.ToDouble(bytes, ipt + 16 * q + 8)));
                            if (tipo % 10 == 5 && lst.Count > 1 && lst[0] == lst[^1]) lst.RemoveAt(lst.Count - 1);
                            partes.Add(lst);
                        }
                        break;
                    }
                case 8: case 18: case 28:
                    {
                        int npt = BitConverter.ToInt32(bytes, c + 36);
                        for (int q = 0; q < npt; q++) partes.Add(new List<(double, double)> { (BitConverter.ToDouble(bytes, c + 40 + 16 * q), BitConverter.ToDouble(bytes, c + 48 + 16 * q)) });
                        break;
                    }
                default:
                    k++;
                    continue;
            }
            r.Add(new FeicaoShape(tipo % 10, partes, k < atributos.Count ? atributos[k] : new Dictionary<string, string>()));
            k++;
        }
        return r;
    }

    private static int BigEndian(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];

    /// <summary>Lê o .dbf (dBase III): uma lista de registros (campo → valor). Codificação pelo .cpg (UTF-8) ou Latin-1.</summary>
    public static List<Dictionary<string, string>> LerDbf(string dbf)
    {
        var r = new List<Dictionary<string, string>>();
        if (!File.Exists(dbf)) return r;
        var b = File.ReadAllBytes(dbf);
        if (b.Length < 32) return r;
        Encoding enc = Encoding.Latin1;
        var cpg = Path.ChangeExtension(dbf, ".cpg");
        if (File.Exists(cpg) && File.ReadAllText(cpg).Contains("UTF", StringComparison.OrdinalIgnoreCase)) enc = Encoding.UTF8;
        int n = BitConverter.ToInt32(b, 4), cab = BitConverter.ToInt16(b, 8), tam = BitConverter.ToInt16(b, 10);
        var campos = new List<(string Nome, int Tam)>();
        for (int i = 32; i + 32 <= cab && b[i] != 0x0D; i += 32)
        {
            string nome = Encoding.ASCII.GetString(b, i, 11).TrimEnd('\0', ' ');
            campos.Add((nome, b[i + 16]));
        }
        for (int k = 0; k < n; k++)
        {
            int ini = cab + k * tam;
            if (ini + tam > b.Length) break;
            var reg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int c = ini + 1;
            foreach (var (nome, t) in campos) { reg[nome] = enc.GetString(b, c, t).Trim(); c += t; }
            r.Add(reg);
        }
        return r;
    }

    /// <summary>Atributo que identifica o lote (lote, inscrição, id, nome...) e o do proprietário.</summary>
    public static (string Id, string Proprietario) IdDosAtributos(IReadOnlyDictionary<string, string> at, int indice)
    {
        string? Achar(params string[] chaves)
        {
            foreach (var c in chaves)
                foreach (var kv in at)
                    if (SemAcento(kv.Key).Contains(c, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(kv.Value)) return kv.Value;
            return null;
        }
        string id = Achar("LOTE", "INSCR", "MATRIC", "CODIGO", "COD", "ID", "NOME", "NAME") ?? $"Lote {indice}";
        string prop = Achar("PROPRIET", "DONO", "OWNER", "TITULAR") ?? "";
        return (id, prop);
    }

    /// <summary>As coordenadas parecem geográficas (graus)?</summary>
    public static bool PareceGeografico(IEnumerable<(double X, double Y)> pts) => pts.Take(50).All(p => Math.Abs(p.X) <= 180 && Math.Abs(p.Y) <= 90);
}

/// <summary>Planilha do PRJINTERFERENCIAS.</summary>
public sealed class RelatorioInterferencias
{
    public string Titulo { get; set; } = "Interferências e desapropriação";
    public string Desenho { get; set; } = "";
    public string Limite { get; set; } = "";
    public string FonteLotes { get; set; } = "";
    public AnaliseInterferencias Analise { get; set; } = new();
    public ParametrosInterferencias Parametros { get; set; } = new();

    public static TabelaDados QuadroDesapropriacao(AnaliseInterferencias a)
    {
        var t = new TabelaDados { Titulo = "QUADRO DE DESAPROPRIAÇÃO", Colunas = new() { "Lote", "Proprietário", "Área do lote (m²)", "Área atingida (m²)", "%", "Remanescente (m²)", "Situação" } };
        foreach (var l in a.Lotes.Where(x => x.AreaAtingida > 0).OrderBy(x => x.Lote.Id, StringComparer.CurrentCultureIgnoreCase))
            t.Linhas.Add(new[] { l.Lote.Id, l.Lote.Proprietario, l.Area.ToString("N2"), l.AreaAtingida.ToString("N2"), (l.Percentual * 100).ToString("0.0"), l.Remanescente.ToString("N2"), l.Situacao });
        double tot = a.Lotes.Sum(x => x.AreaAtingida);
        t.Linhas.Add(new[] { "TOTAL", "", "", tot.ToString("N2"), "", "", $"{a.Lotes.Count(x => x.AreaAtingida > 0)} lote(s)" });
        return t;
    }

    public static TabelaDados QuadroInterferencias(AnaliseInterferencias a)
    {
        var t = new TabelaDados { Titulo = "QUADRO DE INTERFERÊNCIAS", Colunas = new() { "Tipo", "Dentro do limite", "Extensão dentro (m)", "Próximos" } };
        foreach (var g in a.Interferencias.GroupBy(x => x.Objeto.Tipo).OrderBy(g => g.Key))
        {
            int dentro = g.Count(x => x.Situacao == "dentro"), prox = g.Count(x => x.Situacao == "próximo");
            double ext = g.Where(x => x.Objeto.Linear).Sum(x => x.ComprimentoDentro);
            t.Linhas.Add(new[] { g.Key, dentro.ToString(), g.Any(x => x.Objeto.Linear) ? ext.ToString("N2") : "-", prox.ToString() });
        }
        return t;
    }

    public PastaXlsx Planilha()
    {
        var a = Analise;
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = "D9E1F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n2 = new EstiloCelula { Formato = "#,##0.00" };
        var pc = new EstiloCelula { Formato = "0.0%" };
        var n3 = new EstiloCelula { Formato = "0.000" };
        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string x, string y) { fr.Texto(lr, 1, x, txt with { Negrito = true }); fr.Texto(lr, 2, y, q); lr++; }
        Par("Limite da obra", $"{Limite}; {a.AreaLimite:N2} m²{(Parametros.Afastamento > 0 ? $" (com afastamento de {Parametros.Afastamento:0.##} m)" : "")}");
        Par("Lotes", $"{a.Lotes.Count} conferido(s) ({FonteLotes}); {a.Lotes.Count(x => x.AreaAtingida > 0)} atingido(s), {a.Lotes.Count(x => x.Situacao == "total")} totalmente");
        Par("Área a desapropriar", $"{a.Lotes.Sum(x => x.AreaAtingida):N2} m²");
        Par("Interferências", $"{a.Interferencias.Count(x => x.Situacao == "dentro")} dentro do limite e {a.Interferencias.Count(x => x.Situacao == "próximo")} a menos de {Parametros.FaixaAtencao:0.##} m dele");
        foreach (var g in a.Interferencias.GroupBy(x => x.Objeto.Tipo).OrderBy(g => g.Key))
            Par("  " + g.Key, $"{g.Count(x => x.Situacao == "dentro")} dentro" + (g.Any(x => x.Objeto.Linear) ? $", {g.Sum(x => x.ComprimentoDentro):N2} m dentro" : "") + $", {g.Count(x => x.Situacao == "próximo")} próximo(s)");
        foreach (var av in a.Avisos) Par("Aviso", av);
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: áreas e confrontações a confirmar com levantamento cadastral, matrícula do imóvel e o órgão; a validação e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 28); fr.Largura(2, 100);

        var fl = pasta.NovaFolha("Desapropriação");
        Cab(fl, 1, ("Lote", 18), ("Proprietário", 26), ("Origem", 16), ("Área do lote (m²)", 13), ("Área atingida (m²)", 13), ("Atingido", 9), ("Remanescente (m²)", 13), ("Situação", 13), ("Observação", 50));
        int l = 2;
        foreach (var x in a.Lotes.OrderByDescending(x => x.AreaAtingida).ThenBy(x => x.Lote.Id))
        {
            fl.Texto(l, 1, x.Lote.Id, txt); fl.Texto(l, 2, x.Lote.Proprietario, q); fl.Texto(l, 3, x.Lote.Origem, txt);
            fl.Numero(l, 4, x.Area, n2); fl.Numero(l, 5, x.AreaAtingida, n2); fl.Numero(l, 6, x.Percentual, pc); fl.Numero(l, 7, x.Remanescente, n2);
            fl.Texto(l, 8, x.Situacao, txt with { Negrito = x.AreaAtingida > 0, Fundo = x.Situacao == "total" ? "FFC7CE" : x.Situacao == "parcial" ? "FFEB9C" : null });
            fl.Texto(l, 9, x.Obs, q);
            l++;
        }
        fl.Texto(l, 1, "TOTAL", txt with { Negrito = true }); fl.Numero(l, 5, a.Lotes.Sum(x => x.AreaAtingida), n2 with { Negrito = true });

        var fi = pasta.NovaFolha("Interferências");
        Cab(fi, 1, ("Tipo", 18), ("Objeto", 30), ("Camada", 22), ("Situação", 10), ("Distância ao limite (m)", 12), ("Extensão dentro (m)", 12), ("E (X)", 14), ("N (Y)", 14));
        int li = 2;
        foreach (var x in a.Interferencias.OrderBy(x => x.Situacao).ThenBy(x => x.Objeto.Tipo))
        {
            fi.Texto(li, 1, x.Objeto.Tipo, txt); fi.Texto(li, 2, x.Objeto.Nome, q); fi.Texto(li, 3, x.Objeto.Camada, q); fi.Texto(li, 4, x.Situacao, txt with { Fundo = x.Situacao == "dentro" ? "FFC7CE" : "FFEB9C" });
            fi.Numero(li, 5, x.Distancia, n2);
            if (x.Objeto.Linear) fi.Numero(li, 6, x.ComprimentoDentro, n2); else fi.Texto(li, 6, "-", txt);
            fi.Numero(li, 7, x.Ponto.X, n3); fi.Numero(li, 8, x.Ponto.Y, n3);
            li++;
        }

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 40), ("Valor", 14), ("Fonte", 80));
        string pre = Parametros.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA]";
        var pars = new (string, string, string)[]
        {
            ("Afastamento somado ao limite da obra (m)", Parametros.Afastamento.ToString("0.00"), pre + " (faixa de serviço, de segurança ou de domínio)"),
            ("Faixa de atenção fora do limite (m)", Parametros.FaixaAtencao.ToString("0.00"), pre),
            ("Área mínima considerada (m²)", Parametros.AreaMinima.ToString("0.00"), pre),
            ("Lote mínimo para o remanescente (m²)", Parametros.LoteMinimo.ToString("0"), "Lei 6.766/79, art. 4º, II (125 m²); a lei municipal pode exigir mais [A CONFIRMAR]"),
            ("Classificação dos objetos", "pela camada, pelo bloco e pela descrição", "palavras-chave (POSTE, ARV, AGUA, ESGOTO, DRENAGEM, ELETRICA, TELEFONE, GAS, MURO, CERCA, EDIFICAÇÃO...)"),
        };
        int lp = 2;
        foreach (var (n, v, f) in pars) { fp.Texto(lp, 1, n, q); fp.Texto(lp, 2, v, txt); fp.Texto(lp, 3, f, q); lp++; }

        var fd = pasta.NovaFolha("Pendências");
        Cab(fd, 1, ("Pendência", 120));
        int ld = 2;
        foreach (var t in new[]
        {
            "Confirmar os lotes com o cadastro da prefeitura e as matrículas (área registrada × área medida) e as confrontações.",
            "Levantamento cadastral das interferências (posição, profundidade e diâmetro das redes) e consulta às concessionárias para o remanejamento.",
            "Laudo de avaliação dos imóveis atingidos (NBR 14653) e o decreto de utilidade pública.",
            "Objetos não reconhecidos pela camada ou pelo nome do bloco não entram na lista: confira as camadas do levantamento.",
        }) { fd.Texto(ld, 1, t, q); ld++; }
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
