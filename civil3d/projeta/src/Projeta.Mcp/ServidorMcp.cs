using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>
/// Servidor MCP (JSON-RPC 2.0, uma mensagem por linha via stdio) que o Claude Desktop inicia.
/// Repassa tools/list e tools/call para a ponte do plugin. Com o Civil 3D fechado, lista as ferramentas
/// do último catálogo conhecido e responde às chamadas com uma mensagem explicando o que fazer.
/// </summary>
public sealed class ServidorMcp
{
    public const string NomeServidor = "projeta-civil3d";
    public const string FerramentaConexao = "projeta_conexao";
    public const string VersaoProtocoloPadrao = "2025-06-18";
    public static readonly string[] VersoesProtocolo = { "2025-11-25", "2025-06-18", "2025-03-26", "2024-11-05" };

    public const string Instrucoes =
        "Ferramentas do PROJETA (plugin de drenagem, esgoto e água) para consultar e operar o Autodesk Civil 3D aberto no computador do usuário.\n" +
        "- Unidades: metros; cotas em m; declividades em m/m (0.005 = 0,5%); vazões em L/s quando o nome do campo indicar; " +
        "estacas em metros desde o início do alinhamento (o campo estaca_fmt mostra no formato 12+5,30 com estacas de 20 m).\n" +
        "- Comece por desenho_status e projeta_listar_redes / civil_listar_objetos e use os nomes exatamente como retornados.\n" +
        "- Ferramentas de escrita alteram o DWG aberto; cada chamada vira um passo de UNDO no Civil 3D (o usuário desfaz com U). " +
        "Confirme com o usuário antes de mudanças amplas e relate o que mudou.\n" +
        "- Se o Civil 3D estiver ocupado (comando em andamento ou caixa de diálogo aberta), a chamada é recusada: peça para o usuário terminar ou cancelar (Esc).\n" +
        "- projeta_conexao informa se o Civil 3D está conectado.\n" +
        "- Toda ferramenta devolve três formas do mesmo resultado: um texto de leitura, o JSON serializado e o campo " +
        "structuredContent com esse JSON já em objeto. Prefira structuredContent para montar tabelas, quadros e relatórios.\n" +
        "- Estas ferramentas leem o DWG realmente aberto nesta máquina. Quando a pergunta for sobre 'este desenho', " +
        "use-as em vez de responder por conhecimento geral do produto.";

    private readonly PonteCliente _ponte;
    private readonly TextWriter _log;
    private readonly string _versao;
    private readonly object _travaSaida = new();
    private TextWriter? _saida;
    private volatile string? _hashServido;

    public TimeSpan IntervaloMonitor { get; init; } = TimeSpan.FromSeconds(15);

    public ServidorMcp(PonteCliente ponte, TextWriter log, string versao)
    {
        _ponte = ponte; _log = log; _versao = versao;
    }

    private void Log(string msg)
    {
        try { lock (_log) _log.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}"); } catch { }
    }

    public async Task Executar(TextReader entrada, TextWriter saida, CancellationToken ct = default)
    {
        _saida = saida;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var monitor = Task.Run(() => Monitorar(cts.Token));
        var pendentes = new List<Task>();
        while (true)
        {
            string? linha;
            try { linha = await entrada.ReadLineAsync(cts.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            if (linha is null) break;
            if (string.IsNullOrWhiteSpace(linha)) continue;
            var t = Task.Run(() => TratarLinha(linha, cts.Token));
            lock (pendentes) { pendentes.RemoveAll(x => x.IsCompleted); pendentes.Add(t); }
        }
        Task[] resto;
        lock (pendentes) resto = pendentes.ToArray();
        await Task.WhenAny(Task.WhenAll(resto), Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
        cts.Cancel();
        try { await monitor.ConfigureAwait(false); } catch { }
        Log("entrada encerrada — servidor finalizado");
    }

    private void Enviar(JsonNode msg)
    {
        var texto = msg.ToJsonString(JsonMcp.Compacto);
        lock (_travaSaida)
        {
            _saida!.Write(texto);
            _saida.Write('\n');
            _saida.Flush();
        }
    }

    private async Task TratarLinha(string linha, CancellationToken ct)
    {
        try
        {
            JsonNode? msg;
            try { msg = JsonNode.Parse(linha); }
            catch (JsonException ex)
            {
                Enviar(ErroRpc(null, -32700, "Parse error: " + ex.Message));
                return;
            }
            if (msg is JsonArray lote)
            {
                var respostas = new JsonArray();
                foreach (var m in lote.ToList())
                {
                    var r = await Processar(m, ct).ConfigureAwait(false);
                    if (r is not null) respostas.Add(r);
                }
                if (respostas.Count > 0) Enviar(respostas);
                return;
            }
            var resp = await Processar(msg, ct).ConfigureAwait(false);
            if (resp is not null) Enviar(resp);
        }
        catch (Exception ex) { Log("falha ao tratar mensagem: " + ex); }
    }

    private static JsonObject ErroRpc(JsonNode? id, int codigo, string mensagem) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = codigo, ["message"] = mensagem },
    };

    private sealed class ErroRpcEx : Exception
    {
        public int Codigo { get; }
        public ErroRpcEx(int codigo, string msg) : base(msg) { Codigo = codigo; }
    }

    /// <summary>Processa uma mensagem; devolve a resposta ou null (notificações).</summary>
    public async Task<JsonNode?> Processar(JsonNode? m, CancellationToken ct)
    {
        if (m is not JsonObject o) return ErroRpc(null, -32600, "Invalid Request");
        bool temId = o.TryGetPropertyValue("id", out var id);
        var metodo = Linha.Str(o["method"]);
        if (metodo is null)
        {
            // resposta do cliente a algo nosso (não fazemos pedidos ao cliente) — ignora
            if (o.ContainsKey("result") || o.ContainsKey("error")) return null;
            return ErroRpc(id, -32600, "Invalid Request");
        }
        var p = o["params"] as JsonObject;
        try
        {
            JsonNode? resultado;
            switch (metodo)
            {
                case "initialize": resultado = Inicializar(p); break;
                case "ping": resultado = new JsonObject(); break;
                case "tools/list": resultado = await ListarFerramentas(ct).ConfigureAwait(false); break;
                case "tools/call": resultado = await ChamarFerramenta(p, ct).ConfigureAwait(false); break;
                case "prompts/list": resultado = new JsonObject { ["prompts"] = Prompts.Catalogo() }; break;
                case "prompts/get": resultado = Prompts.Obter(Linha.Str(p?["name"]), p?["arguments"] as JsonObject); break;
                // resources/* fica sem resposta de propósito: não anunciamos a capacidade 'resources',
                // então -32601 é o que diz ao cliente que o servidor não tem recursos (lista vazia enganaria).
                default:
                    if (!temId) return null; // notificações (initialized, cancelled...) não têm resposta
                    throw new ErroRpcEx(-32601, $"Method not found: {metodo}");
            }
            if (!temId) return null;
            return new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id?.DeepClone(), ["result"] = resultado ?? new JsonObject() };
        }
        catch (ErroRpcEx ex) { return temId ? ErroRpc(id, ex.Codigo, ex.Message) : null; }
        catch (ArgumentException ex) { return temId ? ErroRpc(id, -32602, "Invalid params: " + ex.Message) : null; }
        catch (Exception ex)
        {
            Log($"erro em {metodo}: {ex}");
            return temId ? ErroRpc(id, -32603, "Internal error: " + ex.Message) : null;
        }
    }

    private JsonObject Inicializar(JsonObject? p)
    {
        var pedida = Linha.Str(p?["protocolVersion"]);
        var versao = pedida is not null && VersoesProtocolo.Contains(pedida) ? pedida : VersaoProtocoloPadrao;
        Log($"initialize: cliente {p?["clientInfo"]?.ToJsonString() ?? "?"}, protocolo pedido {pedida ?? "?"} → {versao}");
        return new JsonObject
        {
            ["protocolVersion"] = versao,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject { ["listChanged"] = true },
                ["prompts"] = new JsonObject { ["listChanged"] = false },
            },
            ["serverInfo"] = new JsonObject { ["name"] = NomeServidor, ["title"] = "PROJETA • Civil 3D", ["version"] = _versao },
            ["instructions"] = Instrucoes,
        };
    }

    // ------------------------------------------------------------------ ferramentas

    private static JsonObject DefinicaoConexao() => new()
    {
        ["name"] = FerramentaConexao,
        ["title"] = "Conexão com o Civil 3D",
        ["description"] = "Verifica se o Civil 3D está aberto com a ponte do PROJETA ligada (comando PRJMCP) e mostra o aplicativo, a versão do plugin, " +
                          "o desenho ativo e quantas ferramentas estão disponíveis. Use quando outra ferramenta disser que o Civil 3D não respondeu.",
        ["inputSchema"] = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject(), ["additionalProperties"] = false },
        ["outputSchema"] = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = true,
            ["properties"] = new JsonObject
            {
                ["conectado"] = new JsonObject { ["type"] = "boolean", ["description"] = "true quando o Civil 3D respondeu." },
                ["aplicativo"] = new JsonObject { ["type"] = "string", ["description"] = "Aplicativo conectado (ex.: AutoCAD Civil 3D 2027)." },
                ["versao"] = new JsonObject { ["type"] = "string", ["description"] = "Versão do plugin PROJETA." },
                ["desenho"] = new JsonObject { ["type"] = "string", ["description"] = "Desenho ativo." },
                ["ferramentas"] = new JsonObject { ["type"] = "integer", ["description"] = "Quantas ferramentas o plugin expõe." },
                ["motivo"] = new JsonObject { ["type"] = "string", ["description"] = "Por que não conectou, quando conectado = false." },
            },
        },
        ["annotations"] = new JsonObject { ["title"] = "Conexão com o Civil 3D", ["readOnlyHint"] = true, ["openWorldHint"] = false },
    };

    private static string Hash(JsonArray a) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(a.ToJsonString(JsonMcp.Compacto))));

    private async Task<(JsonArray Catalogo, string Origem)> ObterCatalogo(CancellationToken ct)
    {
        try
        {
            var (cat, info) = await _ponte.Listar(ct).ConfigureAwait(false);
            try { CatalogoLocal.Gravar(cat, Linha.Str(info["aplicativo"]) ?? "", Linha.Str(info["versao"]) ?? ""); } catch { }
            return (cat, "civil3d");
        }
        catch (Exception ex) when (ex is PonteIndisponivel or ErroPonte)
        {
            var cache = CatalogoLocal.Ler();
            Log($"tools/list sem Civil 3D ({ex.Message}) — {(cache is null ? "sem catálogo salvo" : $"catálogo salvo com {cache.Count} ferramentas")}");
            return (cache ?? new JsonArray(), cache is null ? "vazio" : "cache");
        }
    }

    private async Task<JsonObject> ListarFerramentas(CancellationToken ct)
    {
        var (cat, origem) = await ObterCatalogo(ct).ConfigureAwait(false);
        var tools = new JsonArray { DefinicaoConexao() };
        foreach (var t in cat) tools.Add(t?.DeepClone());
        _hashServido = Hash(cat);
        // tools.Count = catálogo do plugin + a projeta_conexao, que é sempre acrescentada aqui.
        Log($"tools/list: {tools.Count} ferramentas ({origem})");
        return new JsonObject { ["tools"] = tools };
    }

    private async Task<JsonObject> ChamarFerramenta(JsonObject? p, CancellationToken ct)
    {
        var nome = Linha.Str(p?["name"]) ?? throw new ErroRpcEx(-32602, "params.name ausente");
        var bruto = p?["arguments"];
        if (bruto is not null and not JsonObject) throw new ErroRpcEx(-32602, "params.arguments deve ser um objeto");

        if (nome == FerramentaConexao) return ParaMcp(await Conexao(ct).ConfigureAwait(false));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var r = await _ponte.Chamar(nome, bruto as JsonObject, ct).ConfigureAwait(false);
            Log($"tools/call {nome}: {(r.Erro ? "erro" : "ok")} em {sw.ElapsedMilliseconds} ms");
            return ParaMcp(r);
        }
        catch (PonteIndisponivel ex)
        {
            Log($"tools/call {nome}: Civil 3D indisponível — {ex.Message}");
            return ParaMcp(ResultadoFerramenta.Falha(
                $"Não foi possível falar com o Civil 3D: {ex.Message} Peça ao usuário para abrir o Civil 3D com o PROJETA carregado e rodar o comando PRJMCP " +
                "(ou ligar o início automático em PRJMCP → Automatico) e tente de novo."));
        }
        catch (ErroPonte ex)
        {
            Log($"tools/call {nome}: {ex.Codigo} — {ex.Message}");
            return ParaMcp(ResultadoFerramenta.Falha(ex.Message));
        }
    }

    private async Task<ResultadoFerramenta> Conexao(CancellationToken ct)
    {
        try
        {
            var ping = await _ponte.Ping(ct).ConfigureAwait(false);
            var desenho = Linha.Str(ping["desenho"]);
            var texto = $"Conectado ao {Linha.Str(ping["aplicativo"]) ?? "Civil 3D"} (PROJETA {Linha.Str(ping["versao"]) ?? "?"})" +
                        (string.IsNullOrEmpty(desenho) ? ", sem desenho ativo." : $", desenho ativo: {desenho}.") +
                        $" {ping["ferramentas"]?.ToJsonString() ?? "?"} ferramentas disponíveis.";
            var dados = (JsonObject)ping.DeepClone();
            dados["conectado"] = true;
            dados["servidorMcp"] = _versao;
            return ResultadoFerramenta.Ok(texto, dados);
        }
        catch (Exception ex) when (ex is PonteIndisponivel or ErroPonte)
        {
            return ResultadoFerramenta.Ok(
                $"Civil 3D NÃO conectado: {ex.Message} Para conectar: abrir o Civil 3D com o PROJETA carregado e rodar PRJMCP.",
                new JsonObject { ["conectado"] = false, ["motivo"] = ex.Message, ["servidorMcp"] = _versao, ["pasta"] = Locais.Pasta });
        }
    }

    /// <summary>
    /// Converte para o formato de resultado do MCP. Devolve as três formas que um cliente pode querer:
    /// um texto de leitura, o JSON serializado (compatibilidade com clientes antigos) e o mesmo JSON em
    /// <c>structuredContent</c>, que é o que clientes genéricos (Autodesk Assistant, por exemplo) consomem.
    /// </summary>
    public static JsonObject ParaMcp(ResultadoFerramenta r)
    {
        var content = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = string.IsNullOrEmpty(r.Texto) ? (r.Erro ? "Erro." : "Ok.") : r.Texto } };
        if (r.Dados is not null) content.Add(new JsonObject { ["type"] = "text", ["text"] = JsonMcp.Texto(r.Dados) });
        var o = new JsonObject { ["content"] = content, ["isError"] = r.Erro };
        var estruturado = Estruturar(r);
        if (estruturado is not null) o["structuredContent"] = estruturado;
        return o;
    }

    /// <summary>structuredContent tem de ser um objeto JSON; listas e valores soltos vão embrulhados.</summary>
    private static JsonObject? Estruturar(ResultadoFerramenta r)
    {
        if (r.Dados is null) return null;
        var dados = r.Dados.DeepClone();
        var o = dados as JsonObject ?? new JsonObject { ["resultado"] = dados };
        if (!o.ContainsKey("resumo")) o["resumo"] = r.Texto;
        return o;
    }

    // ------------------------------------------------------------------ catálogo mudou?

    private async Task Monitorar(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(IntervaloMonitor, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            if (_hashServido is null) continue;
            try
            {
                var (cat, info) = await _ponte.Listar(ct).ConfigureAwait(false);
                var h = Hash(cat);
                if (h == _hashServido) continue;
                _hashServido = h;
                try { CatalogoLocal.Gravar(cat, Linha.Str(info["aplicativo"]) ?? "", Linha.Str(info["versao"]) ?? ""); } catch { }
                Log($"catálogo mudou ({cat.Count} ferramentas) — avisando o cliente");
                Enviar(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "notifications/tools/list_changed" });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (ex is PonteIndisponivel or ErroPonte) { }
            catch (Exception ex) { Log("monitor: " + ex.Message); }
        }
    }
}
