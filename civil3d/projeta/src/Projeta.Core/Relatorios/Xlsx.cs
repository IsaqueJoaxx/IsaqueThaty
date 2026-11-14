using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Projeta.Core.Relatorios;

public enum AlinhamentoH { Geral, Esquerda, Centro, Direita }

/// <summary>Aparência de uma célula. Registros iguais viram o mesmo estilo no arquivo.</summary>
public sealed record EstiloCelula
{
    public string Fonte { get; init; } = "Calibri";
    public double Tamanho { get; init; } = 10;
    public bool Negrito { get; init; }
    public bool Italico { get; init; }
    /// <summary>Cor de fundo RRGGBB (null = sem preenchimento).</summary>
    public string? Fundo { get; init; }
    /// <summary>Borda fina nas quatro faces.</summary>
    public bool Borda { get; init; } = true;
    public AlinhamentoH Horizontal { get; init; } = AlinhamentoH.Geral;
    public bool Quebra { get; init; }
    /// <summary>Código de formato numérico do Excel (sintaxe invariante: ponto decimal, vírgula de milhar). null = Geral.</summary>
    public string? Formato { get; init; }

    public static readonly EstiloCelula Padrao = new() { Borda = false };
}

/// <summary>
/// Gravador .xlsx mínimo (Office Open XML), sem dependências externas: valores, estilos, células mescladas,
/// larguras, painel congelado e configuração de impressão. Abre no Excel e no LibreOffice.
/// </summary>
public sealed class PastaXlsx
{
    private readonly List<FolhaXlsx> _folhas = new();
    private readonly List<EstiloCelula> _estilos = new() { EstiloCelula.Padrao };
    private readonly Dictionary<EstiloCelula, int> _indice = new() { [EstiloCelula.Padrao] = 0 };

    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "PROJETA";
    public IReadOnlyList<FolhaXlsx> Folhas => _folhas;

    public FolhaXlsx NovaFolha(string nome)
    {
        var limpo = new string(nome.Where(c => "[]:*?/\\".IndexOf(c) < 0).ToArray()).Trim();
        if (limpo.Length == 0) limpo = "Planilha" + (_folhas.Count + 1);
        if (limpo.Length > 31) limpo = limpo[..31];
        var f = new FolhaXlsx(this, limpo);
        _folhas.Add(f);
        return f;
    }

    internal int Estilo(EstiloCelula e)
    {
        if (_indice.TryGetValue(e, out var i)) return i;
        _estilos.Add(e);
        return _indice[e] = _estilos.Count - 1;
    }

    /// <summary>Grava o arquivo. Escreve num temporário e troca no fim: se o .xlsx estiver aberto no Excel, falha sem corromper.</summary>
    public void Salvar(string arquivo)
    {
        var pasta = Path.GetDirectoryName(Path.GetFullPath(arquivo))!;
        Directory.CreateDirectory(pasta);
        var tmp = Path.Combine(pasta, "~" + Guid.NewGuid().ToString("N")[..8] + ".tmp");
        try
        {
            File.WriteAllBytes(tmp, Bytes());
            File.Move(tmp, arquivo, overwrite: true);
        }
        catch (IOException ex)
        {
            try { File.Delete(tmp); } catch { }
            throw new IOException($"Não foi possível gravar {Path.GetFileName(arquivo)} (o arquivo está aberto no Excel?). {ex.Message}", ex);
        }
    }

    public byte[] Bytes()
    {
        if (_folhas.Count == 0) NovaFolha("Planilha1");
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            Parte(zip, "[Content_Types].xml", TiposConteudo());
            Parte(zip, "_rels/.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
                "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>" +
                "</Relationships>");
            Parte(zip, "docProps/core.xml",
                "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " +
                "xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                $"<dc:title>{X(Titulo)}</dc:title><dc:creator>{X(Autor)}</dc:creator>" +
                $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</dcterms:created></cp:coreProperties>");
            Parte(zip, "docProps/app.xml",
                "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Application>PROJETA</Application></Properties>");
            Parte(zip, "xl/workbook.xml", Workbook());
            Parte(zip, "xl/_rels/workbook.xml.rels", RelacoesWorkbook());
            for (int i = 0; i < _folhas.Count; i++) Parte(zip, $"xl/worksheets/sheet{i + 1}.xml", _folhas[i].Xml());
            Parte(zip, "xl/styles.xml", Estilos());
        }
        return ms.ToArray();
    }

    private static void Parte(ZipArchive zip, string nome, string xml)
    {
        var e = zip.CreateEntry(nome, CompressionLevel.Optimal);
        using var s = new StreamWriter(e.Open(), new UTF8Encoding(false));
        s.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
        s.Write(xml);
    }

    private string TiposConteudo()
    {
        var sb = new StringBuilder("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        for (int i = 0; i < _folhas.Count; i++)
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        sb.Append("<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>");
        sb.Append("<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>");
        return sb.Append("</Types>").ToString();
    }

    private string Workbook()
    {
        var sb = new StringBuilder("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        sb.Append("<bookViews><workbookView/></bookViews><sheets>");
        for (int i = 0; i < _folhas.Count; i++) sb.Append($"<sheet name=\"{X(_folhas[i].Nome)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        sb.Append("</sheets>");
        var titulos = _folhas.Select((f, i) => (f, i)).Where(t => t.f.TituloImpressaoAte > 0).ToList();
        if (titulos.Count > 0)
        {
            sb.Append("<definedNames>");
            foreach (var (f, i) in titulos)
                sb.Append($"<definedName name=\"_xlnm.Print_Titles\" localSheetId=\"{i}\">'{X(f.Nome.Replace("'", "''"))}'!$1:${f.TituloImpressaoAte}</definedName>");
            sb.Append("</definedNames>");
        }
        return sb.Append("</workbook>").ToString();
    }

    private string RelacoesWorkbook()
    {
        var sb = new StringBuilder("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (int i = 0; i < _folhas.Count; i++)
            sb.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
        sb.Append($"<Relationship Id=\"rId{_folhas.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        return sb.Append("</Relationships>").ToString();
    }

    private string Estilos()
    {
        var formatos = new List<string>();
        var fontes = new List<(string, double, bool, bool)>();
        var fundos = new List<string>();
        var xfs = new StringBuilder();
        foreach (var e in _estilos)
        {
            int numFmt = 0;
            if (!string.IsNullOrEmpty(e.Formato))
            {
                int k = formatos.IndexOf(e.Formato);
                if (k < 0) { formatos.Add(e.Formato); k = formatos.Count - 1; }
                numFmt = 164 + k;
            }
            var chaveFonte = (e.Fonte, e.Tamanho, e.Negrito, e.Italico);
            int fonte = fontes.IndexOf(chaveFonte);
            if (fonte < 0) { fontes.Add(chaveFonte); fonte = fontes.Count - 1; }
            int fundo = 0;
            if (!string.IsNullOrEmpty(e.Fundo))
            {
                int k = fundos.IndexOf(e.Fundo);
                if (k < 0) { fundos.Add(e.Fundo); k = fundos.Count - 1; }
                fundo = 2 + k; // 0 e 1 são reservados pelo Excel
            }
            string h = e.Horizontal switch { AlinhamentoH.Esquerda => "left", AlinhamentoH.Centro => "center", AlinhamentoH.Direita => "right", _ => "" };
            xfs.Append($"<xf numFmtId=\"{numFmt}\" fontId=\"{fonte}\" fillId=\"{fundo}\" borderId=\"{(e.Borda ? 1 : 0)}\" xfId=\"0\"");
            if (numFmt != 0) xfs.Append(" applyNumberFormat=\"1\"");
            xfs.Append(" applyFont=\"1\"");
            if (fundo != 0) xfs.Append(" applyFill=\"1\"");
            if (e.Borda) xfs.Append(" applyBorder=\"1\"");
            xfs.Append(" applyAlignment=\"1\"><alignment");
            if (h.Length > 0) xfs.Append($" horizontal=\"{h}\"");
            xfs.Append(" vertical=\"center\"");
            if (e.Quebra) xfs.Append(" wrapText=\"1\"");
            xfs.Append("/></xf>");
        }

        var sb = new StringBuilder("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        if (formatos.Count > 0)
        {
            sb.Append($"<numFmts count=\"{formatos.Count}\">");
            for (int i = 0; i < formatos.Count; i++) sb.Append($"<numFmt numFmtId=\"{164 + i}\" formatCode=\"{X(formatos[i])}\"/>");
            sb.Append("</numFmts>");
        }
        sb.Append($"<fonts count=\"{fontes.Count}\">");
        foreach (var (nome, tam, neg, ita) in fontes)
        {
            sb.Append("<font>");
            if (neg) sb.Append("<b/>");
            if (ita) sb.Append("<i/>");
            sb.Append($"<sz val=\"{tam.ToString(CultureInfo.InvariantCulture)}\"/><name val=\"{X(nome)}\"/><family val=\"2\"/></font>");
        }
        sb.Append("</fonts>");
        sb.Append($"<fills count=\"{2 + fundos.Count}\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>");
        foreach (var c in fundos) sb.Append($"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF{c}\"/><bgColor indexed=\"64\"/></patternFill></fill>");
        sb.Append("</fills>");
        sb.Append("<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
                  "<border><left style=\"thin\"><color indexed=\"64\"/></left><right style=\"thin\"><color indexed=\"64\"/></right>" +
                  "<top style=\"thin\"><color indexed=\"64\"/></top><bottom style=\"thin\"><color indexed=\"64\"/></bottom><diagonal/></border></borders>");
        sb.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");
        sb.Append($"<cellXfs count=\"{_estilos.Count}\">").Append(xfs).Append("</cellXfs>");
        sb.Append("<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>");
        return sb.Append("</styleSheet>").ToString();
    }

    /// <summary>Escapa texto para XML e remove caracteres de controle que o Excel recusa.</summary>
    internal static string X(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length + 8);
        foreach (var c in s)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                default:
                    if (c < 0x20 && c != '\t' && c != '\n' && c != '\r') continue;
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}

/// <summary>Uma planilha da pasta. Linhas e colunas começam em 1.</summary>
public sealed class FolhaXlsx
{
    private readonly PastaXlsx _pasta;
    private readonly SortedDictionary<int, SortedDictionary<int, (object? Valor, int Estilo)>> _linhas = new();
    private readonly List<(int L1, int C1, int L2, int C2)> _mescladas = new();
    private readonly SortedDictionary<int, double> _larguras = new();
    private readonly SortedDictionary<int, double> _alturas = new();

    internal FolhaXlsx(PastaXlsx pasta, string nome) { _pasta = pasta; Nome = nome; }

    public string Nome { get; }
    /// <summary>Linhas e colunas fixas no topo/à esquerda ao rolar.</summary>
    public int CongelarLinhas { get; set; }
    public int CongelarColunas { get; set; }
    /// <summary>Repete as linhas 1..N no topo de cada página impressa (0 = não repete).</summary>
    public int TituloImpressaoAte { get; set; }
    public bool Paisagem { get; set; }
    /// <summary>Ajusta a impressão para caber em 1 página de largura (altura livre).</summary>
    public bool CaberNaLargura { get; set; }
    public bool MostrarGrade { get; set; } = true;
    /// <summary>Código de papel do Excel: 9 = A4, 8 = A3.</summary>
    public int Papel { get; set; } = 9;
    /// <summary>Com CaberNaLargura: em quantas páginas de largura a planilha se divide.</summary>
    public int PaginasLargura { get; set; } = 1;
    public string? Rodape { get; set; }

    public void Numero(int lin, int col, double v, EstiloCelula e) => Por(lin, col, double.IsNaN(v) || double.IsInfinity(v) ? null : v, e);
    public void Texto(int lin, int col, string? s, EstiloCelula e) => Por(lin, col, string.IsNullOrEmpty(s) ? null : s, e);
    public void Vazio(int lin, int col, EstiloCelula e) => Por(lin, col, null, e);

    /// <summary>Mescla o intervalo, põe o texto na primeira célula e aplica o estilo em todas (para as bordas saírem completas).</summary>
    public void Mesclar(int l1, int c1, int l2, int c2, string? texto, EstiloCelula e)
    {
        for (int l = l1; l <= l2; l++)
            for (int c = c1; c <= c2; c++)
                Por(l, c, l == l1 && c == c1 && !string.IsNullOrEmpty(texto) ? texto : null, e);
        if (l2 > l1 || c2 > c1) _mescladas.Add((l1, c1, l2, c2));
    }

    public void Largura(int col, double caracteres) => _larguras[col] = caracteres;
    public void Altura(int lin, double pontos) => _alturas[lin] = pontos;

    public object? Valor(int lin, int col) => _linhas.TryGetValue(lin, out var l) && l.TryGetValue(col, out var c) ? c.Valor : null;
    public int UltimaLinha => _linhas.Count == 0 ? 0 : _linhas.Keys.Max();
    public int UltimaColuna => _linhas.Count == 0 ? 0 : _linhas.Values.Max(l => l.Count == 0 ? 0 : l.Keys.Max());
    public IReadOnlyList<(int L1, int C1, int L2, int C2)> Mescladas => _mescladas;

    private void Por(int lin, int col, object? v, EstiloCelula e)
    {
        if (lin < 1 || col < 1) throw new ArgumentOutOfRangeException(nameof(lin), "linhas e colunas começam em 1");
        if (!_linhas.TryGetValue(lin, out var l)) _linhas[lin] = l = new SortedDictionary<int, (object?, int)>();
        l[col] = (v, _pasta.Estilo(e));
    }

    public static string Coluna(int col)
    {
        var s = "";
        for (int n = col; n > 0; n = (n - 1) / 26) s = (char)('A' + (n - 1) % 26) + s;
        return s;
    }

    public static string Ref(int lin, int col) => Coluna(col) + lin.ToString(CultureInfo.InvariantCulture);

    internal string Xml()
    {
        var sb = new StringBuilder("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        if (CaberNaLargura) sb.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
        int ul = Math.Max(UltimaLinha, 1), uc = Math.Max(UltimaColuna, 1);
        sb.Append($"<dimension ref=\"A1:{Ref(ul, uc)}\"/>");
        sb.Append("<sheetViews><sheetView workbookViewId=\"0\"");
        if (!MostrarGrade) sb.Append(" showGridLines=\"0\"");
        if (CongelarLinhas > 0 || CongelarColunas > 0)
        {
            var canto = Ref(CongelarLinhas + 1, CongelarColunas + 1);
            string painel = CongelarLinhas > 0 && CongelarColunas > 0 ? "bottomRight" : CongelarLinhas > 0 ? "bottomLeft" : "topRight";
            sb.Append("><pane");
            if (CongelarColunas > 0) sb.Append($" xSplit=\"{CongelarColunas}\"");
            if (CongelarLinhas > 0) sb.Append($" ySplit=\"{CongelarLinhas}\"");
            sb.Append($" topLeftCell=\"{canto}\" activePane=\"{painel}\" state=\"frozen\"/><selection pane=\"{painel}\" activeCell=\"{canto}\" sqref=\"{canto}\"/></sheetView>");
        }
        else sb.Append("/>");
        sb.Append("</sheetViews><sheetFormatPr defaultRowHeight=\"15\"/>");
        if (_larguras.Count > 0)
        {
            sb.Append("<cols>");
            foreach (var (c, w) in _larguras)
                sb.Append($"<col min=\"{c}\" max=\"{c}\" width=\"{w.ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
            sb.Append("</cols>");
        }
        sb.Append("<sheetData>");
        var linhas = new SortedSet<int>(_linhas.Keys);
        linhas.UnionWith(_alturas.Keys);
        foreach (var r in linhas)
        {
            sb.Append($"<row r=\"{r}\"");
            if (_alturas.TryGetValue(r, out var h)) sb.Append($" ht=\"{h.ToString("0.##", CultureInfo.InvariantCulture)}\" customHeight=\"1\"");
            sb.Append('>');
            if (_linhas.TryGetValue(r, out var cels))
                foreach (var (c, (v, s)) in cels)
                {
                    sb.Append($"<c r=\"{Ref(r, c)}\"");
                    if (s != 0) sb.Append($" s=\"{s}\"");
                    switch (v)
                    {
                        case null: sb.Append("/>"); break;
                        case double d: sb.Append("><v>").Append(d.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>"); break;
                        default:
                            var t = v.ToString() ?? "";
                            bool espaco = t.Length > 0 && (char.IsWhiteSpace(t[0]) || char.IsWhiteSpace(t[^1]));
                            sb.Append(" t=\"inlineStr\"><is><t").Append(espaco ? " xml:space=\"preserve\"" : "").Append('>')
                              .Append(PastaXlsx.X(t)).Append("</t></is></c>");
                            break;
                    }
                }
            sb.Append("</row>");
        }
        sb.Append("</sheetData>");
        if (_mescladas.Count > 0)
        {
            sb.Append($"<mergeCells count=\"{_mescladas.Count}\">");
            foreach (var (l1, c1, l2, c2) in _mescladas) sb.Append($"<mergeCell ref=\"{Ref(l1, c1)}:{Ref(l2, c2)}\"/>");
            sb.Append("</mergeCells>");
        }
        sb.Append("<printOptions horizontalCentered=\"1\"/>");
        sb.Append("<pageMargins left=\"0.4\" right=\"0.4\" top=\"0.5\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/>");
        sb.Append($"<pageSetup paperSize=\"{Papel}\"");
        if (Paisagem) sb.Append(" orientation=\"landscape\"");
        if (CaberNaLargura) sb.Append($" fitToWidth=\"{Math.Max(1, PaginasLargura)}\" fitToHeight=\"0\"");
        sb.Append("/>");
        if (!string.IsNullOrEmpty(Rodape)) sb.Append("<headerFooter><oddFooter>").Append(PastaXlsx.X(Rodape)).Append("</oddFooter></headerFooter>");
        return sb.Append("</worksheet>").ToString();
    }
}
