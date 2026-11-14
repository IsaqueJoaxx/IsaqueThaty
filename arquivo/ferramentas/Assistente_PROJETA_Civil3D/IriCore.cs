using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Projeta.Civil3D;

internal sealed record IriSegment(string Road, string Lane, string Direction, string Track, double Start, double End, double Iri)
{
    public double Length => End - Start;
    public string Key => string.Join(" / ", Road, Lane, Direction, Track);
}
internal sealed record IriSummary(string Key, int Count, double Length, double Mean, double Max, double AboveLength, int Gaps);

// Analisa IRI já calculado pelo levantamento. Não estima IRI a partir de greide ou superfície TIN.
internal static class IriCore
{
    public const string Header = "rodovia;faixa;sentido;trilha;inicio_m;fim_m;iri_m_km";
    public const string Example = Header + "\r\nEXEMPLO;1;Crescente;Direita;0;100;2,1\r\nEXEMPLO;1;Crescente;Direita;100;200;3,4\r\nEXEMPLO;1;Crescente;Esquerda;0;100;2,3\r\n";
    public static List<IriSegment> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Importe um CSV ou cole os resultados de IRI.");
        if (text.Length > 5_000_000) throw new ArgumentException("O arquivo excede o limite de 5 milhões de caracteres.");
        using var parser = new TextFieldParser(new StringReader(text.TrimStart('\uFEFF')));
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(text.Split('\n')[0].Contains('\t') ? "\t" : ";");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;
        var header = parser.ReadFields() ?? Array.Empty<string>();
        var expected = Header.Split(';');
        if (!header.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Cabeçalho esperado: " + Header + ". Use ponto e vírgula ou tabulação; estações em metros.");
        var rows = new List<IriSegment>();
        while (!parser.EndOfData)
        {
            var line = parser.LineNumber;
            string[] f;
            try { f = parser.ReadFields() ?? Array.Empty<string>(); }
            catch (MalformedLineException) { throw new ArgumentException($"Linha {line}: aspas ou separadores inválidos."); }
            if (f.Length != 7) throw new ArgumentException($"Linha {line}: são necessárias 7 colunas.");
            if (f.Take(4).Any(string.IsNullOrWhiteSpace)) throw new ArgumentException($"Linha {line}: informe rodovia, faixa, sentido e trilha.");
            double N(int i)
            {
                var s = f[i].Trim();
                if (s.Contains(',') && s.Contains('.')) throw new ArgumentException($"Linha {line}: não use separador de milhares.");
                if (!double.TryParse(s.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n))
                    throw new ArgumentException($"Linha {line}: número inválido em {expected[i]}.");
                return n;
            }
            var a = N(4); var b = N(5); var iri = N(6);
            if (b <= a || !double.IsFinite(b-a)) throw new ArgumentException($"Linha {line}: fim_m deve ser maior que inicio_m, em distância acumulada crescente.");
            if (iri < 0) throw new ArgumentException($"Linha {line}: IRI não pode ser negativo.");
            rows.Add(new(f[0], f[1], f[2], f[3], a, b, iri));
            if (rows.Count > 20_000) throw new ArgumentException("Limite de 20.000 trechos por importação.");
        }
        if (rows.Count == 0) throw new ArgumentException("O arquivo contém apenas o cabeçalho.");
        foreach (var group in Groups(rows))
        {
            IriSegment? previous = null;
            foreach (var r in group.OrderBy(r => r.Start))
            {
                if (previous is not null && r.Start < previous.End - 1e-7)
                    throw new ArgumentException($"Trechos sobrepostos ou duplicados em {r.Key}, junto a {r.Start:G} m.");
                previous = r;
            }
        }
        return rows;
    }
    private static IEnumerable<IGrouping<(string, string, string, string), IriSegment>> Groups(IEnumerable<IriSegment> rows) =>
        rows.GroupBy(r => (r.Road, r.Lane, r.Direction, r.Track));

    public static List<IriSummary> Summarize(IEnumerable<IriSegment> rows, double? limit)
    {
        if (limit is not null && (!double.IsFinite(limit.Value) || limit <= 0)) throw new ArgumentException("Informe limite positivo em m/km.");
        var result = new List<IriSummary>();
        foreach (var group in Groups(rows))
        {
            var ordered = group.OrderBy(r => r.Start).ToArray();
            var length = ordered.Sum(r => r.Length);
            if (!double.IsFinite(length) || length <= 0) throw new ArgumentException("Extensão acumulada inválida.");
            var mean = ordered.Sum(r => r.Iri * (r.Length / length));
            if (!double.IsFinite(mean)) throw new ArgumentException("Valores excedem a precisão numérica da análise.");
            var gaps = ordered.Zip(ordered.Skip(1), (a,b) => b.Start > a.End + 1e-7 ? 1 : 0).Sum();
            result.Add(new(ordered[0].Key, ordered.Length, length, mean, ordered.Max(r => r.Iri),
                limit is null ? 0 : ordered.Where(r => r.Iri > limit).Sum(r => r.Length), gaps));
        }
        return result;
    }
    public static string Export(IEnumerable<IriSegment> rows, double? limit, string reference)
    {
        Summarize(rows, limit);
        static string Q(string s)
        {
            // Neutraliza fórmulas ao abrir o CSV em planilha.
            if (s.TrimStart().StartsWith('=') || s.TrimStart().StartsWith('+') || s.TrimStart().StartsWith('-') || s.TrimStart().StartsWith('@')) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        static string N(double n) => n.ToString("G17", CultureInfo.InvariantCulture);
        var b = new StringBuilder(Header + ";limite_m_km;criterio;comparacao\r\n");
        foreach (var r in rows)
            b.AppendLine(string.Join(";", Q(r.Road), Q(r.Lane), Q(r.Direction), Q(r.Track), N(r.Start), N(r.End), N(r.Iri),
                limit is null ? "" : N(limit.Value), Q(reference), Q(limit is null ? "Sem comparação" : r.Iri > limit ? "Acima do limite informado" : "Até o limite informado")));
        return b.ToString();
    }
}
