using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>
/// Monta o JSON Schema dos argumentos de uma ferramenta (o "inputSchema" do MCP).
/// Exemplo: <c>new Esquema().Texto("rede", "Nome da rede").Numero("intervalo", "Intervalo (m)", minimo: 0.1)</c>.
/// </summary>
public sealed class Esquema
{
    private readonly JsonObject _props = new();
    private readonly List<string> _obrigatorios = new();
    private bool _aberto;

    /// <summary>
    /// Aceita propriedades além das declaradas. Use em esquemas de SAÍDA (outputSchema): o cliente valida o
    /// structuredContent contra o esquema, e campos a mais não podem derrubar a validação.
    /// </summary>
    public Esquema Aberto() { _aberto = true; return this; }

    public IEnumerable<string> Propriedades => _props.Select(p => p.Key);
    public IReadOnlyList<string> Obrigatorios => _obrigatorios;

    private Esquema Prop(string nome, JsonObject def, bool obrigatorio)
    {
        if (_props.ContainsKey(nome)) throw new ArgumentException($"Propriedade repetida no esquema: {nome}");
        _props[nome] = def;
        if (obrigatorio) _obrigatorios.Add(nome);
        return this;
    }

    private static JsonNode Tipo(string tipo, bool anulavel) => anulavel ? new JsonArray(tipo, "null") : JsonValue.Create(tipo);

    /// <param name="anulavel">Aceita null. Use em esquemas de SAÍDA, em campo que pode não existir
    /// (caminho de desenho nunca salvo, sistema de coordenadas não definido).</param>
    public Esquema Texto(string nome, string descricao, bool obrigatorio = false, IEnumerable<string>? valores = null, string? padrao = null, bool anulavel = false)
    {
        var d = new JsonObject { ["type"] = Tipo("string", anulavel), ["description"] = descricao };
        if (valores is not null) d["enum"] = JsonMcp.Textos(valores);
        if (padrao is not null) d["default"] = padrao;
        return Prop(nome, d, obrigatorio);
    }

    /// <param name="anulavel">Aceita null (usado para "remover valor forçado").</param>
    public Esquema Numero(string nome, string descricao, bool obrigatorio = false, double? minimo = null, double? maximo = null, double? padrao = null, bool anulavel = false)
    {
        var d = new JsonObject { ["type"] = Tipo("number", anulavel), ["description"] = descricao };
        if (minimo.HasValue) d["minimum"] = minimo.Value;
        if (maximo.HasValue) d["maximum"] = maximo.Value;
        if (padrao.HasValue) d["default"] = padrao.Value;
        return Prop(nome, d, obrigatorio);
    }

    public Esquema Inteiro(string nome, string descricao, bool obrigatorio = false, int? minimo = null, int? maximo = null, int? padrao = null, bool anulavel = false)
    {
        var d = new JsonObject { ["type"] = Tipo("integer", anulavel), ["description"] = descricao };
        if (minimo.HasValue) d["minimum"] = minimo.Value;
        if (maximo.HasValue) d["maximum"] = maximo.Value;
        if (padrao.HasValue) d["default"] = padrao.Value;
        return Prop(nome, d, obrigatorio);
    }

    public Esquema Booleano(string nome, string descricao, bool? padrao = null)
    {
        var d = new JsonObject { ["type"] = "boolean", ["description"] = descricao };
        if (padrao.HasValue) d["default"] = padrao.Value;
        return Prop(nome, d, false);
    }

    public Esquema ListaTextos(string nome, string descricao, bool obrigatorio = false, int? maxItens = null)
    {
        var d = new JsonObject { ["type"] = "array", ["description"] = descricao, ["items"] = new JsonObject { ["type"] = "string" } };
        if (maxItens.HasValue) d["maxItems"] = maxItens.Value;
        return Prop(nome, d, obrigatorio);
    }

    public Esquema ListaNumeros(string nome, string descricao, bool obrigatorio = false, int? maxItens = null)
    {
        var d = new JsonObject { ["type"] = "array", ["description"] = descricao, ["items"] = new JsonObject { ["type"] = "number" } };
        if (maxItens.HasValue) d["maxItems"] = maxItens.Value;
        return Prop(nome, d, obrigatorio);
    }

    public Esquema ListaObjetos(string nome, string descricao, Esquema item, bool obrigatorio = false, int? maxItens = null)
    {
        var d = new JsonObject { ["type"] = "array", ["description"] = descricao, ["items"] = item.Json() };
        if (maxItens.HasValue) d["maxItems"] = maxItens.Value;
        return Prop(nome, d, obrigatorio);
    }

    /// <summary>Objeto com chaves livres (ex.: {"TempoRetorno": 25, "RecobrimentoMinimo": 1.2}).</summary>
    public Esquema ObjetoLivre(string nome, string descricao, bool obrigatorio = false) =>
        Prop(nome, new JsonObject { ["type"] = "object", ["description"] = descricao, ["additionalProperties"] = true }, obrigatorio);

    public JsonObject Json()
    {
        var o = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = _props.DeepClone(),
            ["additionalProperties"] = _aberto,
        };
        if (_obrigatorios.Count > 0) o["required"] = JsonMcp.Textos(_obrigatorios);
        return o;
    }
}
