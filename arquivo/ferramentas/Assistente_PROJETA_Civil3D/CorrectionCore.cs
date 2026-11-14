using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Projeta.Civil3D;

internal sealed record CorrectionInput(decimal Station, decimal Grade, decimal ExistingAxis, decimal ExistingLeft, decimal ExistingRight,
    decimal WidthLeft, decimal WidthRight, decimal SlopeLeft, decimal SlopeRight, decimal CorrectionMm);
internal sealed record CorrectionResult(CorrectionInput Input, decimal BaseAxis, decimal TopAxis, decimal BaseLeft, decimal TopLeft,
    decimal BaseRight, decimal TopRight, decimal GapAxis, decimal GapLeft, decimal GapRight, decimal? DeltaMm, string Check);

internal static class CorrectionCore
{
    public const string Header = "distancia_m;greide_base_m;existente_eixo_m;existente_esq_m;existente_dir_m;largura_esq_m;largura_dir_m;caimento_esq_pct;caimento_dir_pct;correcao_mm";
    public const string Example = Header + "\r\n0;100;100;99,93;99,93;3,5;3,5;-2;-2;0\r\n5;100;100;99,93;99,93;3,5;3,5;-2;-2;2,5\r\n10;100;100;99,93;99,93;3,5;3,5;-2;-2;6\r\n";
    public static List<CorrectionInput> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 5_000_000) throw new ArgumentException("Informe até 5 milhões de caracteres de dados.");
        using var p = new TextFieldParser(new StringReader(text.TrimStart('\uFEFF')));
        p.SetDelimiters(text.Split('\n')[0].Contains('\t') ? "\t" : ";");
        p.HasFieldsEnclosedInQuotes = true; p.TrimWhiteSpace = true;
        if (!(p.ReadFields() ?? []).SequenceEqual(Header.Split(';'), StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Cabeçalho esperado: " + Header);
        var rows = new List<CorrectionInput>();
        while (!p.EndOfData)
        {
            var line = p.LineNumber;
            var f = p.ReadFields() ?? [];
            if (f.Length != 10) throw new ArgumentException($"Linha {line}: são necessárias 10 colunas.");
            decimal N(int i)
            {
                if (f[i].Contains(',') && f[i].Contains('.')) throw new ArgumentException($"Linha {line}: não use separador de milhares.");
                if (!decimal.TryParse(f[i].Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
                    throw new ArgumentException($"Linha {line}: número inválido na coluna {i+1}.");
                return n;
            }
            rows.Add(new(N(0),N(1),N(2),N(3),N(4),N(5),N(6),N(7),N(8),N(9)));
            if (rows.Count > 20_000) throw new ArgumentException("Limite de 20.000 seções.");
        }
        if (rows.Count == 0) throw new ArgumentException("O arquivo não contém seções.");
        return rows;
    }
    public static List<CorrectionResult> Calculate(IReadOnlyList<CorrectionInput> rows, decimal concrete = .23m)
    {
        if (concrete <= 0 || concrete > 2) throw new ArgumentException("Informe espessura vertical de concreto positiva, até 2 m.");
        if (rows.Count == 0) throw new ArgumentException("Informe as seções.");
        var result = new List<CorrectionResult>();
        CorrectionInput? previous = null;
        foreach (var r in rows)
        {
            if (r.WidthLeft <= 0 || r.WidthRight <= 0) throw new ArgumentException($"Seção {r.Station}: larguras devem ser positivas.");
            if (Math.Abs(r.SlopeLeft)>20 || Math.Abs(r.SlopeRight)>20) throw new ArgumentException($"Seção {r.Station}: confira os caimentos em porcentagem (ex.: -2).");
            if (previous is not null && r.Station <= previous.Station) throw new ArgumentException("As distâncias devem estar em ordem crescente, sem repetição. Separe eixos diferentes em arquivos próprios.");
            var axis = r.Grade + r.CorrectionMm/1000;
            var left = axis + r.WidthLeft*r.SlopeLeft/100;
            var right = axis + r.WidthRight*r.SlopeRight/100;
            decimal? delta = previous is null ? null : r.CorrectionMm-previous.CorrectionMm;
            var check = previous is null ? "Inicial — sem comparação" : r.Station-previous.Station != 5 ? "Intervalo diferente de 5 m — não verificado" :
                Math.Abs(delta!.Value) <= 2.5m ? "Dentro da regra" : "Excede 2,5 mm / 5 m";
            result.Add(new(r,axis,axis+concrete,left,left+concrete,right,right+concrete,
                axis-r.ExistingAxis,left-r.ExistingLeft,right-r.ExistingRight,delta,check));
            previous = r;
        }
        return result;
    }
    public static string Export(IReadOnlyList<CorrectionResult> rows, decimal concrete)
    {
        var b = new StringBuilder(Header + ";concreto_vertical_m;base_corrigida_eixo_m;topo_eixo_m;base_esq_m;topo_esq_m;base_dir_m;topo_dir_m;saldo_base_eixo_m;saldo_base_esq_m;saldo_base_dir_m;delta_correcao_mm;controle\r\n");
        static string N(decimal n) => n.ToString(CultureInfo.InvariantCulture);
        foreach(var x in rows)
        {
            var r=x.Input;
            b.AppendLine(string.Join(";", new[]{r.Station,r.Grade,r.ExistingAxis,r.ExistingLeft,r.ExistingRight,r.WidthLeft,r.WidthRight,r.SlopeLeft,r.SlopeRight,r.CorrectionMm,
                concrete,x.BaseAxis,x.TopAxis,x.BaseLeft,x.TopLeft,x.BaseRight,x.TopRight,x.GapAxis,x.GapLeft,x.GapRight}.Select(N)) + ";" +
                (x.DeltaMm is null ? "" : N(x.DeltaMm.Value)) + ";" + x.Check);
        }
        return b.ToString();
    }
}
