using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>Opções e utilidades JSON comuns à ponte e ao servidor MCP.</summary>
public static class JsonMcp
{
    /// <summary>Compacto, sem escapar acentos (o canal é UTF-8). Quebras de linha dentro de textos são sempre escapadas.</summary>
    public static readonly JsonSerializerOptions Compacto = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    public static readonly JsonSerializerOptions Indentado = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static readonly JsonSerializerOptions Arquivo = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static readonly JsonDocumentOptions Leitura = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static string Texto(JsonNode? n, bool indentado = false) =>
        n is null ? "null" : n.ToJsonString(indentado ? Indentado : Compacto);

    /// <summary>Número para JSON: NaN/infinito viram null (JSON não tem NaN). <paramref name="casas"/> &lt; 0 = sem arredondar.</summary>
    public static JsonNode? Numero(double v, int casas = -1) =>
        double.IsFinite(v) ? JsonValue.Create(casas >= 0 ? Math.Round(v, casas) : v) : null;

    public static JsonNode? Numero(double? v, int casas = -1) => v.HasValue ? Numero(v.Value, casas) : null;

    /// <summary>Lê número aceitando vírgula ou ponto decimal ("1,5", "1.234,56", "1234.5").</summary>
    public static bool TentarLerNumero(string? s, out double v)
    {
        v = double.NaN;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().Replace(" ", "");
        if (s.Contains(',') && s.Contains('.'))
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.')) s = s.Replace(".", "").Replace(',', '.');
            else s = s.Replace(",", "");
        }
        else if (s.Contains(',')) s = s.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && double.IsFinite(v);
    }

    public static JsonArray Array<T>(IEnumerable<T> itens, Func<T, JsonNode?> f)
    {
        var a = new JsonArray();
        foreach (var i in itens) a.Add(f(i));
        return a;
    }

    public static JsonArray Textos(IEnumerable<string> itens) => Array(itens, s => JsonValue.Create(s));
}
