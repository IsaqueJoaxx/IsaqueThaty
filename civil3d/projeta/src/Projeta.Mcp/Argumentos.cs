using System.Text.Json;
using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>
/// Argumentos recebidos numa chamada de ferramenta, com leitura tipada e mensagens de erro claras.
/// Distingue "ausente" (não mexer) de "null" (remover valor forçado) — veja <see cref="Tem"/> e <see cref="EhNulo"/>.
/// </summary>
public sealed class Argumentos
{
    private readonly JsonObject _o;
    private readonly string _prefixo;

    public Argumentos(JsonObject? o, string prefixo = "")
    {
        _o = o ?? new JsonObject();
        _prefixo = prefixo;
    }

    public static Argumentos De(string json) =>
        new(string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json, documentOptions: JsonMcp.Leitura) as JsonObject
            ?? throw new ErroArgumento("os argumentos devem ser um objeto JSON."));

    public JsonObject Json => _o;
    public IEnumerable<string> Chaves => _o.Select(p => p.Key);

    private string N(string nome) => _prefixo + nome;

    public bool Tem(string nome) => _o.ContainsKey(nome);
    public bool EhNulo(string nome) => _o.TryGetPropertyValue(nome, out var n) && n is null;
    public JsonNode? Bruto(string nome) => _o.TryGetPropertyValue(nome, out var n) ? n : null;

    public string? Texto(string nome)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return null;
        if (n is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s)) return s;
            if (v.TryGetValue<JsonElement>(out var e) && e.ValueKind == JsonValueKind.String) return e.GetString();
            return v.ToJsonString();
        }
        throw new ErroArgumento($"'{N(nome)}' deve ser texto.");
    }

    public string TextoObrigatorio(string nome)
    {
        var s = Texto(nome);
        if (string.IsNullOrWhiteSpace(s)) throw new ErroArgumento($"'{N(nome)}' é obrigatório.");
        return s.Trim();
    }

    public double? Numero(string nome)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return null;
        return LerNumero(n, N(nome));
    }

    public double NumeroObrigatorio(string nome) => Numero(nome) ?? throw new ErroArgumento($"'{N(nome)}' é obrigatório.");

    public double Numero(string nome, double padrao) => Numero(nome) ?? padrao;

    public int? Inteiro(string nome)
    {
        var d = Numero(nome);
        if (d is null) return null;
        if (Math.Abs(d.Value - Math.Round(d.Value)) > 1e-9 || Math.Abs(d.Value) > int.MaxValue) throw new ErroArgumento($"'{N(nome)}' deve ser inteiro.");
        return (int)Math.Round(d.Value);
    }

    public int Inteiro(string nome, int padrao) => Inteiro(nome) ?? padrao;

    public bool Booleano(string nome, bool padrao)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return padrao;
        if (n is JsonValue v)
        {
            if (v.TryGetValue<bool>(out var b)) return b;
            if (v.TryGetValue<JsonElement>(out var e) && (e.ValueKind is JsonValueKind.True or JsonValueKind.False)) return e.GetBoolean();
            var s = Texto(nome)?.Trim().ToLowerInvariant();
            if (s is "true" or "sim" or "s" or "1") return true;
            if (s is "false" or "nao" or "não" or "n" or "0") return false;
        }
        throw new ErroArgumento($"'{N(nome)}' deve ser true ou false.");
    }

    public IReadOnlyList<Argumentos> ListaObjetos(string nome)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return System.Array.Empty<Argumentos>();
        if (n is not JsonArray a) throw new ErroArgumento($"'{N(nome)}' deve ser uma lista de objetos.");
        var l = new List<Argumentos>();
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] is not JsonObject o) throw new ErroArgumento($"'{N(nome)}[{i}]' deve ser um objeto.");
            l.Add(new Argumentos(o, $"{N(nome)}[{i}]."));
        }
        return l;
    }

    public IReadOnlyList<double> ListaNumeros(string nome)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return System.Array.Empty<double>();
        if (n is not JsonArray a) throw new ErroArgumento($"'{N(nome)}' deve ser uma lista de números.");
        var l = new List<double>(a.Count);
        for (int i = 0; i < a.Count; i++)
            l.Add(a[i] is null ? throw new ErroArgumento($"'{N(nome)}[{i}]' é nulo.") : LerNumero(a[i]!, $"{N(nome)}[{i}]"));
        return l;
    }

    public IReadOnlyList<string> ListaTextos(string nome)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return System.Array.Empty<string>();
        if (n is JsonValue) return new[] { Texto(nome)! };   // tolera "x" no lugar de ["x"]
        if (n is not JsonArray a) throw new ErroArgumento($"'{N(nome)}' deve ser uma lista de textos.");
        return a.Select((x, i) => x is JsonValue v && v.TryGetValue<string>(out var s) ? s
            : x is JsonValue v2 && v2.TryGetValue<JsonElement>(out var e) && e.ValueKind == JsonValueKind.String ? e.GetString()!
            : throw new ErroArgumento($"'{N(nome)}[{i}]' deve ser texto.")).ToList();
    }

    public JsonObject? Objeto(string nome)
    {
        if (!_o.TryGetPropertyValue(nome, out var n) || n is null) return null;
        return n as JsonObject ?? throw new ErroArgumento($"'{N(nome)}' deve ser um objeto.");
    }

    public static double LerNumero(JsonNode n, string nome)
    {
        if (n is JsonValue v)
        {
            if (v.TryGetValue<double>(out var d) && double.IsFinite(d)) return d;
            if (v.TryGetValue<JsonElement>(out var e))
            {
                if (e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out d) && double.IsFinite(d)) return d;
                if (e.ValueKind == JsonValueKind.String && JsonMcp.TentarLerNumero(e.GetString(), out d)) return d;
            }
            if (v.TryGetValue<string>(out var s) && JsonMcp.TentarLerNumero(s, out d)) return d;
            if (v.TryGetValue<int>(out var i)) return i;
            if (v.TryGetValue<long>(out var l)) return l;
        }
        throw new ErroArgumento($"'{nome}' deve ser um número.");
    }
}
