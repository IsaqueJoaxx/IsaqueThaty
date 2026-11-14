using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Projeta.Sinalizacao.Core.Quantitativos;

public enum Estilo { Normal = 0, Cabecalho = 1, Numero2 = 2, Inteiro = 3, Titulo = 4, Total = 5, Numero3 = 6, Subtitulo = 7 }

/// <summary>Célula: texto ou número, com estilo.</summary>
public readonly record struct Celula(object? Valor, Estilo Estilo = Estilo.Normal)
{
    public static implicit operator Celula(string s) => new(s);
    public static implicit operator Celula(double d) => new(d, Estilo.Numero2);
    public static implicit operator Celula(int i) => new((double)i, Estilo.Inteiro);
}

public sealed class Aba
{
    public string Nome { get; }
    public List<List<Celula>> Linhas { get; } = new();
    public List<double> Larguras { get; } = new();
    /// <summary>Linha (base 1) do cabeçalho para autofiltro e congelamento; 0 = nenhum.</summary>
    public int LinhaCabecalho { get; set; }
    public int NumColunasFiltro { get; set; }
    public Aba(string nome) { Nome = nome.Length > 31 ? nome[..31] : nome; }
    public Aba Linha(params Celula[] c) { Linhas.Add(c.ToList()); return this; }
    public Aba Vazia() { Linhas.Add(new List<Celula>()); return this; }
}

/// <summary>Gravador XLSX mínimo (Office Open XML) sem dependências externas.</summary>
public static class Xlsx
{
    public static void Salvar(string caminho, IList<Aba> abas)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminho))!);
        using var fs = new FileStream(caminho, FileMode.Create, FileAccess.Write);
        Escrever(fs, abas);
    }

    public static void Escrever(Stream destino, IList<Aba> abas)
    {
        using var zip = new ZipArchive(destino, ZipArchiveMode.Create, leaveOpen: true);
        void Arq(string nome, string conteudo)
        {
            var e = zip.CreateEntry(nome, CompressionLevel.Optimal);
            using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
            w.Write(conteudo);
        }

        var ct = new StringBuilder();
        ct.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        ct.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        ct.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        ct.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        for (int i = 0; i < abas.Count; i++)
            ct.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        ct.Append("<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>");
        ct.Append("</Types>");
        Arq("[Content_Types].xml", ct.ToString());

        Arq("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/></Relationships>");

        Arq("docProps/core.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
            "<dc:title>Quantitativos de sinalização</dc:title><dc:creator>PROJETA Sinalização</dc:creator>" +
            $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</dcterms:created></cp:coreProperties>");

        var wb = new StringBuilder();
        wb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
        for (int i = 0; i < abas.Count; i++) wb.Append($"<sheet name=\"{Esc(abas[i].Nome)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        wb.Append("</sheets>");
        // nomes definidos para o autofiltro
        var filtros = abas.Select((a, i) => (a, i)).Where(x => x.a.LinhaCabecalho > 0 && x.a.NumColunasFiltro > 0).ToList();
        if (filtros.Count > 0)
        {
            wb.Append("<definedNames>");
            foreach (var (a, i) in filtros)
            {
                int ultima = Math.Max(a.LinhaCabecalho, a.Linhas.Count);
                wb.Append($"<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"{i}\" hidden=\"1\">'{Esc(a.Nome.Replace("'", "''"))}'!$A${a.LinhaCabecalho}:${Col(a.NumColunasFiltro)}${ultima}</definedName>");
            }
            wb.Append("</definedNames>");
        }
        wb.Append("</workbook>");
        Arq("xl/workbook.xml", wb.ToString());

        var rels = new StringBuilder();
        rels.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (int i = 0; i < abas.Count; i++)
            rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
        rels.Append($"<Relationship Id=\"rId{abas.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        rels.Append("</Relationships>");
        Arq("xl/_rels/workbook.xml.rels", rels.ToString());

        Arq("xl/styles.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.00\"/><numFmt numFmtId=\"165\" formatCode=\"#,##0.000\"/></numFmts>" +
            "<fonts count=\"4\"><font><sz val=\"10\"/><name val=\"Arial\"/></font><font><b/><sz val=\"10\"/><color rgb=\"FFFFFFFF\"/><name val=\"Arial\"/></font><font><b/><sz val=\"13\"/><name val=\"Arial\"/></font><font><b/><sz val=\"10\"/><name val=\"Arial\"/></font></fonts>" +
            "<fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
            "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF2F5D50\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
            "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE6EEEA\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
            "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
            "<border><left style=\"thin\"><color rgb=\"FFBFBFBF\"/></left><right style=\"thin\"><color rgb=\"FFBFBFBF\"/></right><top style=\"thin\"><color rgb=\"FFBFBFBF\"/></top><bottom style=\"thin\"><color rgb=\"FFBFBFBF\"/></bottom><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"8\">" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>" +
            "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
            "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
            "<xf numFmtId=\"3\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
            "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
            "<xf numFmtId=\"164\" fontId=\"3\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
            "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
            "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
            "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");

        for (int i = 0; i < abas.Count; i++) Arq($"xl/worksheets/sheet{i + 1}.xml", Planilha(abas[i]));
    }

    private static string Planilha(Aba a)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        if (a.LinhaCabecalho > 0)
            sb.Append($"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"{a.LinhaCabecalho}\" topLeftCell=\"A{a.LinhaCabecalho + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        if (a.Larguras.Count > 0)
        {
            sb.Append("<cols>");
            for (int c = 0; c < a.Larguras.Count; c++) sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{a.Larguras[c].ToString("0.##", inv)}\" customWidth=\"1\"/>");
            sb.Append("</cols>");
        }
        sb.Append("<sheetData>");
        for (int r = 0; r < a.Linhas.Count; r++)
        {
            var linha = a.Linhas[r];
            sb.Append($"<row r=\"{r + 1}\">");
            for (int c = 0; c < linha.Count; c++)
            {
                var cel = linha[c];
                string refc = Col(c + 1) + (r + 1);
                int s = (int)cel.Estilo;
                switch (cel.Valor)
                {
                    case null: if (s != 0) sb.Append($"<c r=\"{refc}\" s=\"{s}\"/>"); break;
                    case double d when double.IsFinite(d):
                        sb.Append($"<c r=\"{refc}\" s=\"{s}\"><v>{d.ToString("R", inv)}</v></c>"); break;
                    case int i: sb.Append($"<c r=\"{refc}\" s=\"{s}\"><v>{i}</v></c>"); break;
                    default:
                        string t = cel.Valor.ToString() ?? "";
                        sb.Append($"<c r=\"{refc}\" s=\"{s}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Esc(t)}</t></is></c>");
                        break;
                }
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData>");
        if (a.LinhaCabecalho > 0 && a.NumColunasFiltro > 0)
            sb.Append($"<autoFilter ref=\"A{a.LinhaCabecalho}:{Col(a.NumColunasFiltro)}{Math.Max(a.LinhaCabecalho, a.Linhas.Count)}\"/>");
        sb.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/>");
        sb.Append("<pageSetup orientation=\"landscape\" paperSize=\"9\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    public static string Col(int n)
    {
        var s = "";
        while (n > 0) { int m = (n - 1) % 26; s = (char)('A' + m) + s; n = (n - 1) / 26; }
        return s;
    }

    private static string Esc(string s)
    {
        var limpo = new StringBuilder(s.Length);
        foreach (var ch in s) if (ch == '\t' || ch == '\n' || ch == '\r' || ch >= 0x20) limpo.Append(ch);
        return SecurityElement.Escape(limpo.ToString()) ?? "";
    }
}
