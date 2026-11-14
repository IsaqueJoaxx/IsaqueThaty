using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projeta.Core.Comum;

/// <summary>Formatação e leitura de números (aceita vírgula ou ponto decimal).</summary>
public static class Num
{
    /// <summary>Formato brasileiro (vírgula decimal, ponto de milhar) sem depender de dados de cultura do sistema.</summary>
    public static readonly CultureInfo Br = CriarBr();

    private static CultureInfo CriarBr()
    {
        var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.NumberDecimalSeparator = ",";
        c.NumberFormat.NumberGroupSeparator = ".";
        c.NumberFormat.PercentDecimalSeparator = ",";
        c.NumberFormat.CurrencyDecimalSeparator = ",";
        return c;
    }
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static bool TentarLer(string? s, out double v)
    {
        v = double.NaN;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().Replace(" ", "");
        // "1.234,56" -> pt-BR ; "1234.56" -> invariante ; "1234,56" -> pt-BR
        if (s.Contains(',') && s.Contains('.'))
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.')) s = s.Replace(".", "").Replace(',', '.');
            else s = s.Replace(",", "");
        }
        else if (s.Contains(',')) s = s.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Float, Inv, out v);
    }

    public static double Ler(string? s, double padrao = double.NaN) => TentarLer(s, out var v) ? v : padrao;

    /// <summary>Formata com vírgula decimal (padrão brasileiro).</summary>
    public static string F(double v, int casas = 2)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return "-";
        return v.ToString("F" + casas, Br);
    }

    /// <summary>Formata com ponto decimal (para arquivos INP/CSV técnicos).</summary>
    public static string I(double v, int casas = 4) => v.ToString("0." + new string('#', Math.Max(casas, 1)), Inv);

    /// <summary>Formata estaca no padrão 20 m: 12+5,30.</summary>
    public static string Estaca(double estacaMetros, double intervalo = 20.0, int casas = 2)
    {
        if (double.IsNaN(estacaMetros)) return "-";
        int inteiro = (int)Math.Floor(estacaMetros / intervalo + 1e-9);
        double resto = estacaMetros - inteiro * intervalo;
        if (resto < 0) resto = 0;
        return $"{inteiro}+{resto.ToString("F" + casas, Br)}";
    }
}

/// <summary>Configuração JSON padrão do projeto.</summary>
public static class JsonProjeta
{
    public static readonly JsonSerializerOptions Opcoes = Criar(true);
    public static readonly JsonSerializerOptions Compacto = Criar(false);

    private static JsonSerializerOptions Criar(bool indentar)
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = indentar,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public static string Serializar<T>(T obj, bool indentar = true) => JsonSerializer.Serialize(obj, indentar ? Opcoes : Compacto);
    public static T? Desserializar<T>(string json) => JsonSerializer.Deserialize<T>(json, Opcoes);

    /// <summary>Clona via JSON (deep copy).</summary>
    public static T Clonar<T>(T obj) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj, Compacto), Opcoes)!;
}

public enum Gravidade { Info, Aviso, Erro }

public sealed record Mensagem(Gravidade Gravidade, string Texto, string? Objeto = null)
{
    public override string ToString() => (Objeto is null ? "" : Objeto + ": ") + Texto;
}

/// <summary>Leitura/escrita CSV simples com separador configurável (padrão ';' para Excel pt-BR).</summary>
public static class Csv
{
    public static List<string[]> Ler(string texto, char sep = ';')
    {
        var linhas = new List<string[]>();
        var campo = new StringBuilder();
        var atual = new List<string>();
        bool aspas = false;
        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];
            if (aspas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"') { campo.Append('"'); i++; }
                    else aspas = false;
                }
                else campo.Append(c);
                continue;
            }
            if (c == '"') { aspas = true; continue; }
            if (c == sep) { atual.Add(campo.ToString()); campo.Clear(); continue; }
            if (c == '\r') continue;
            if (c == '\n')
            {
                atual.Add(campo.ToString()); campo.Clear();
                if (atual.Count > 1 || atual[0].Length > 0) linhas.Add(atual.ToArray());
                atual = new List<string>();
                continue;
            }
            campo.Append(c);
        }
        if (campo.Length > 0 || atual.Count > 0)
        {
            atual.Add(campo.ToString());
            if (atual.Count > 1 || atual[0].Length > 0) linhas.Add(atual.ToArray());
        }
        return linhas;
    }

    /// <summary>Detecta o separador (';', ',' ou tab) pela primeira linha.</summary>
    public static char DetectarSeparador(string texto)
    {
        var primeira = texto.Split('\n').FirstOrDefault() ?? "";
        int pv = primeira.Count(c => c == ';'), tab = primeira.Count(c => c == '\t'), vg = primeira.Count(c => c == ',');
        if (pv >= tab && pv >= vg && pv > 0) return ';';
        if (tab >= vg && tab > 0) return '\t';
        return vg > 0 ? ',' : ';';
    }

    public static string Escapar(string? s, char sep = ';')
    {
        s ??= "";
        if (s.IndexOfAny(new[] { sep, '"', '\n', '\r' }) >= 0) return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    public static string Linha(IEnumerable<string?> campos, char sep = ';') => string.Join(sep, campos.Select(c => Escapar(c, sep)));
}

public static class Texto
{
    /// <summary>Remove acentos e caracteres inválidos para IDs (SWMM/EPANET não aceitam espaço).</summary>
    public static string IdSeguro(string nome, int max = 31)
    {
        var norm = nome.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in norm)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') sb.Append(ch);
            else if (char.IsWhiteSpace(ch)) sb.Append('_');
        }
        var r = sb.ToString();
        if (r.Length == 0) r = "X";
        return r.Length > max ? r[..max] : r;
    }

    public static string Html(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
}
