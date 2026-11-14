using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Projeta.Mcp;

/// <summary>Argumento ausente, de tipo errado ou fora do esperado. A mensagem volta para o Claude corrigir a chamada.</summary>
public sealed class ErroArgumento : Exception
{
    public ErroArgumento(string mensagem) : base(mensagem) { }
}

/// <summary>Erro esperado durante a execução (ex.: "rede não encontrada"). A mensagem volta sem stack trace.</summary>
public sealed class ErroFerramenta : Exception
{
    public ErroFerramenta(string mensagem) : base(mensagem) { }
}

/// <summary>Resultado de uma ferramenta: um resumo em texto (o que o Claude lê primeiro) e dados estruturados opcionais.</summary>
public sealed class ResultadoFerramenta
{
    public string Texto { get; init; } = "";
    public JsonNode? Dados { get; init; }
    public bool Erro { get; init; }

    public static ResultadoFerramenta Ok(string texto, JsonNode? dados = null) => new() { Texto = texto, Dados = dados };
    public static ResultadoFerramenta Falha(string texto, JsonNode? dados = null) => new() { Texto = texto, Dados = dados, Erro = true };

    public JsonObject ParaJson()
    {
        var o = new JsonObject { ["texto"] = Texto, ["erro"] = Erro };
        if (Dados is not null) o["dados"] = Dados.DeepClone();
        return o;
    }

    public static ResultadoFerramenta DeJson(JsonObject o) => new()
    {
        Texto = o["texto"]?.GetValue<string>() ?? "",
        Erro = o["erro"]?.GetValue<bool>() ?? false,
        Dados = o["dados"]?.DeepClone(),
    };
}

/// <summary>
/// Uma ferramenta exposta ao Claude. A execução acontece no contexto que o <see cref="IExecutor"/> garante
/// (no Civil 3D: thread principal, documento travado e um passo de UNDO por chamada que altera o desenho).
/// </summary>
public abstract class Ferramenta
{
    private static readonly Regex NomeValido = new("^[a-zA-Z0-9_-]{1,64}$", RegexOptions.Compiled);
    private JsonObject? _esquema;
    private Esquema? _esquemaObj;
    private JsonObject? _esquemaSaida;
    private bool _saidaMontada;

    /// <summary>Nome técnico (a-z, 0-9, _ e -). É o que o Claude usa para chamar.</summary>
    public abstract string Nome { get; }
    public abstract string Titulo { get; }
    /// <summary>Quando usar, o que faz, unidades e o que devolve — o Claude decide a partir deste texto.</summary>
    public abstract string Descricao { get; }

    /// <summary>Só consulta (não grava no desenho nem em disco).</summary>
    public virtual bool SomenteLeitura => true;
    /// <summary>Grava no DWG aberto (trava em modo escrita e gera um passo de UNDO).</summary>
    public virtual bool AlteraDesenho => !SomenteLeitura;
    /// <summary>Pode apagar ou sobrescrever algo que não se recupera com UNDO.</summary>
    public virtual bool Destrutiva => false;
    public virtual bool Idempotente => SomenteLeitura;
    /// <summary>Tempo máximo de espera pela execução na thread do Civil 3D.</summary>
    public virtual TimeSpan Limite => TimeSpan.FromSeconds(SomenteLeitura ? 60 : 300);

    protected abstract Esquema MontarEsquema();

    /// <summary>
    /// Esquema do objeto devolvido em <c>structuredContent</c> (o "outputSchema" do MCP). null = não declara.
    /// Declare sempre com <see cref="Projeta.Mcp.Esquema.Aberto"/> e sem campos obrigatórios: o cliente valida
    /// o resultado contra ele, e um campo a mais ou a menos não pode fazer a chamada falhar.
    /// </summary>
    protected virtual Esquema? MontarEsquemaSaida() => null;

    public abstract ResultadoFerramenta Executar(Argumentos args);

    public Esquema EsquemaObjeto => _esquemaObj ??= MontarEsquema();
    public JsonObject Esquema => _esquema ??= EsquemaObjeto.Json();

    /// <summary>outputSchema já em JSON, ou null quando a ferramenta não declara formato de saída.</summary>
    public JsonObject? EsquemaSaida
    {
        get
        {
            if (!_saidaMontada) { _esquemaSaida = MontarEsquemaSaida()?.Json(); _saidaMontada = true; }
            return _esquemaSaida;
        }
    }

    internal void Validar()
    {
        if (!NomeValido.IsMatch(Nome)) throw new ArgumentException($"Nome de ferramenta inválido: '{Nome}' (use a-z, 0-9, _ e -; até 64 caracteres).");
        _ = Esquema;
        _ = EsquemaSaida;
    }

    /// <summary>Definição no formato do MCP (tools/list).</summary>
    public JsonObject Definicao()
    {
        var d = new JsonObject
        {
            ["name"] = Nome,
            ["title"] = Titulo,
            ["description"] = Descricao,
            ["inputSchema"] = Esquema.DeepClone(),
            ["annotations"] = new JsonObject
            {
                ["title"] = Titulo,
                ["readOnlyHint"] = SomenteLeitura,
                ["destructiveHint"] = Destrutiva,
                ["idempotentHint"] = Idempotente,
                ["openWorldHint"] = false,
            },
        };
        var saida = EsquemaSaida;
        if (saida is not null) d["outputSchema"] = saida.DeepClone();
        return d;
    }

    /// <summary>Confere os argumentos contra o esquema (obrigatórios e nomes desconhecidos).</summary>
    public void ValidarArgumentos(Argumentos a)
    {
        var aceitos = EsquemaObjeto.Propriedades.ToHashSet(StringComparer.Ordinal);
        var desconhecidos = a.Chaves.Where(k => !aceitos.Contains(k)).ToList();
        if (desconhecidos.Count > 0)
            throw new ErroArgumento($"argumento(s) desconhecido(s): {string.Join(", ", desconhecidos)}. Aceitos: {(aceitos.Count == 0 ? "(nenhum)" : string.Join(", ", aceitos))}.");
        foreach (var o in EsquemaObjeto.Obrigatorios)
            if (!a.Tem(o) || a.EhNulo(o)) throw new ErroArgumento($"'{o}' é obrigatório.");
    }

    /// <summary>Valida e executa, convertendo exceções em resultado de erro legível.</summary>
    public ResultadoFerramenta ExecutarSeguro(Argumentos a)
    {
        try
        {
            ValidarArgumentos(a);
            return Executar(a);
        }
        catch (ErroArgumento ex) { return ResultadoFerramenta.Falha("Argumento inválido: " + ex.Message); }
        catch (ErroFerramenta ex) { return ResultadoFerramenta.Falha(ex.Message); }
        catch (Exception ex) { return ResultadoFerramenta.Falha($"Erro inesperado em {Nome}: {ex.GetType().Name}: {ex.Message}"); }
    }
}

/// <summary>Ferramenta definida por delegado — forma curta para registrar no plugin.</summary>
public sealed class FerramentaDelegada : Ferramenta
{
    private readonly Func<Argumentos, ResultadoFerramenta> _corpo;
    private readonly Func<Esquema> _esquema;
    private readonly Func<Esquema>? _esquemaSaida;

    public FerramentaDelegada(string nome, string titulo, string descricao, Func<Esquema> esquema, Func<Argumentos, ResultadoFerramenta> corpo,
        bool somenteLeitura = true, bool? alteraDesenho = null, bool destrutiva = false, TimeSpan? limite = null, Func<Esquema>? esquemaSaida = null)
    {
        Nome = nome; Titulo = titulo; Descricao = descricao;
        _esquema = esquema; _corpo = corpo; _esquemaSaida = esquemaSaida;
        SomenteLeitura = somenteLeitura;
        AlteraDesenho = alteraDesenho ?? !somenteLeitura;
        Destrutiva = destrutiva;
        Limite = limite ?? TimeSpan.FromSeconds(somenteLeitura ? 60 : 300);
    }

    public override string Nome { get; }
    public override string Titulo { get; }
    public override string Descricao { get; }
    public override bool SomenteLeitura { get; }
    public override bool AlteraDesenho { get; }
    public override bool Destrutiva { get; }
    public override TimeSpan Limite { get; }
    protected override Esquema MontarEsquema() => _esquema();
    protected override Esquema? MontarEsquemaSaida() => _esquemaSaida?.Invoke();
    public override ResultadoFerramenta Executar(Argumentos args) => _corpo(args);
}

/// <summary>Conjunto de ferramentas disponíveis.</summary>
public sealed class RegistroFerramentas
{
    private readonly Dictionary<string, Ferramenta> _f = new(StringComparer.Ordinal);
    private readonly List<string> _ordem = new();

    public RegistroFerramentas Adicionar(Ferramenta f)
    {
        f.Validar();
        if (!_f.TryAdd(f.Nome, f)) throw new ArgumentException($"Ferramenta repetida: {f.Nome}");
        _ordem.Add(f.Nome);
        return this;
    }

    public Ferramenta? Obter(string nome) => _f.TryGetValue(nome, out var f) ? f : null;
    public IEnumerable<Ferramenta> Todas => _ordem.Select(n => _f[n]);
    public int Quantidade => _f.Count;

    public JsonArray Catalogo() => JsonMcp.Array(Todas, f => f.Definicao());
}
