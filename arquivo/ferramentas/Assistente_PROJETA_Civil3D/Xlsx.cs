using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Projeta.Civil3D;

// Planilha .xlsx mínima (OpenXML escrito à mão, sem bibliotecas externas): uma aba por tabela,
// cabeçalho em destaque e congelado, números como números (o Excel mostra no formato do Windows).
// Decimals: casas por coluna (-1 = texto). Células: string, double, int ou null.
internal sealed record XlsxSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<object?[]> Rows,
    IReadOnlyList<int> Decimals, IReadOnlyList<string>? Notes = null);

internal static class Xlsx
{
    public static void Write(string path, IReadOnlyList<XlsxSheet> sheets)
    {
        if (sheets.Count == 0) throw new ArgumentException("Nada para exportar.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var safe = sheets.Select(s => UniqueName(s.Name, names)).ToList();
        var temp = path + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write))
        using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
        {
            void Put(string name, string content)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
            var types = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
                .Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">")
                .Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>")
                .Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>")
                .Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>")
                .Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (var i = 1; i <= sheets.Count; i++)
                types.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            Put("[Content_Types].xml", types.Append("</Types>").ToString());
            Put("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            var book = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
                .Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
            var rels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
                .Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (var i = 1; i <= sheets.Count; i++)
            {
                book.Append($"<sheet name=\"{Escape(safe[i - 1])}\" sheetId=\"{i}\" r:id=\"rId{i}\"/>");
                rels.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
            }
            rels.Append($"<Relationship Id=\"rId{sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            Put("xl/workbook.xml", book.Append("</sheets></workbook>").ToString());
            Put("xl/_rels/workbook.xml.rels", rels.Append("</Relationships>").ToString());
            Put("xl/styles.xml", Styles);
            for (var i = 0; i < sheets.Count; i++) Put($"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i]));
        }
        File.Move(temp, path, true);
    }

    // Estilos: 0 padrão, 1 cabeçalho, 2 texto, 3 inteiro, 4 0,00, 5 0,000, 6 0,0000, 7 nota.
    private const string Styles = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.000\"/><numFmt numFmtId=\"165\" formatCode=\"0.0000\"/></numFmts>" +
        "<fonts count=\"3\"><font><sz val=\"10\"/><name val=\"Arial\"/></font><font><b/><sz val=\"10\"/><name val=\"Arial\"/></font><font><i/><sz val=\"9\"/><color rgb=\"FF595959\"/><name val=\"Arial\"/></font></fonts>" +
        "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9D9D9\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
        "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
        "<border><left style=\"thin\"><color rgb=\"FFA6A6A6\"/></left><right style=\"thin\"><color rgb=\"FFA6A6A6\"/></right><top style=\"thin\"><color rgb=\"FFA6A6A6\"/></top><bottom style=\"thin\"><color rgb=\"FFA6A6A6\"/></bottom><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"8\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"3\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
        "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    private static string Sheet(XlsxSheet s)
    {
        var columns = s.Headers.Count;
        var widths = new double[columns];
        for (var c = 0; c < columns; c++)
        {
            var longest = Math.Max(s.Headers[c].Length * 0.8, 6);
            foreach (var row in s.Rows)
                if (c < row.Length && row[c] is not null) longest = Math.Max(longest, Text(row[c], Dec(s, c)).Length);
            widths[c] = Math.Min(60, longest + 2.5);
        }
        var x = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
            .Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">")
            .Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>")
            .Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>")
            .Append("<cols>");
        for (var c = 0; c < columns; c++)
            x.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{widths[c].ToString("0.0", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        x.Append("</cols><sheetData>");
        x.Append("<row r=\"1\">");
        for (var c = 0; c < columns; c++) x.Append(Cell(c, 1, s.Headers[c], 1));
        x.Append("</row>");
        var r = 2;
        foreach (var row in s.Rows)
        {
            x.Append($"<row r=\"{r}\">");
            for (var c = 0; c < columns; c++)
            {
                var value = c < row.Length ? row[c] : null;
                var d = Dec(s, c);
                var style = value switch
                {
                    null => 2, string => 2,
                    _ => d switch { <= 0 => 3, 1 or 2 => 4, 3 => 5, _ => 6 }
                };
                x.Append(Cell(c, r, value, style));
            }
            x.Append("</row>");
            r++;
        }
        if (s.Notes is { Count: > 0 })
        {
            r++;
            foreach (var note in s.Notes) x.Append($"<row r=\"{r}\">").Append(Cell(0, r++, note, 7)).Append("</row>");
        }
        x.Append("</sheetData>");
        if (s.Rows.Count > 0) x.Append($"<autoFilter ref=\"A1:{Column(columns - 1)}{s.Rows.Count + 1}\"/>");
        x.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/>");
        x.Append("<pageSetup orientation=\"landscape\" paperSize=\"9\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
        return x.Append("</worksheet>").ToString();
    }

    private static int Dec(XlsxSheet s, int column) => column < s.Decimals.Count ? s.Decimals[column] : -1;

    private static string Cell(int column, int row, object? value, int style)
    {
        var reference = Column(column) + row.ToString(CultureInfo.InvariantCulture);
        return value switch
        {
            null => $"<c r=\"{reference}\" s=\"{style}\"/>",
            double d when double.IsFinite(d) => $"<c r=\"{reference}\" s=\"{style}\"><v>{d.ToString("R", CultureInfo.InvariantCulture)}</v></c>",
            double => $"<c r=\"{reference}\" s=\"{style}\"/>",
            int i => $"<c r=\"{reference}\" s=\"{style}\"><v>{i.ToString(CultureInfo.InvariantCulture)}</v></c>",
            _ => $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(value.ToString() ?? "")}</t></is></c>"
        };
    }

    private static string Text(object? value, int decimals) => value switch
    {
        double d => d.ToString("N" + Math.Max(0, decimals), CultureInfo.GetCultureInfo("pt-BR")),
        int i => i.ToString(CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? ""
    };

    public static string Column(int index)
    {
        var name = "";
        for (var n = index + 1; n > 0; n = (n - 1) / 26) name = (char)('A' + (n - 1) % 26) + name;
        return name;
    }

    private static string Escape(string text)
    {
        var b = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r') continue; // caracteres proibidos em XML
            b.Append(ch switch { '&' => "&amp;", '<' => "&lt;", '>' => "&gt;", '"' => "&quot;", _ => ch.ToString() });
        }
        return b.ToString();
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        var clean = new string(name.Where(c => "[]:*?/\\".IndexOf(c) < 0).ToArray()).Trim();
        if (clean.Length == 0) clean = "Planilha";
        if (clean.Length > 31) clean = clean[..31];
        var candidate = clean;
        for (var i = 2; !used.Add(candidate); i++)
            candidate = (clean.Length > 27 ? clean[..27] : clean) + $" ({i})";
        return candidate;
    }
}
