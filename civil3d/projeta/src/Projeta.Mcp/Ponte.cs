using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>Arquivos locais da ponte. Pasta padrão: %LOCALAPPDATA%\Projeta\mcp (pode ser trocada pela variável PROJETA_MCP_DIR).</summary>
public static class Locais
{
    public static string Pasta
    {
        get
        {
            var p = Environment.GetEnvironmentVariable("PROJETA_MCP_DIR");
            return !string.IsNullOrWhiteSpace(p) ? p : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Projeta", "mcp");
        }
    }

    /// <summary>Porta e token da ponte ativa (escrito pelo plugin, lido pelo servidor MCP).</summary>
    public static string ArquivoPonte => Path.Combine(Pasta, "ponte.json");
    /// <summary>Última lista de ferramentas conhecida (para o Claude enxergar as ferramentas mesmo com o Civil 3D fechado).</summary>
    public static string ArquivoCatalogo => Path.Combine(Pasta, "catalogo.json");
    public static string ArquivoConfig => Path.Combine(Pasta, "config.json");

    /// <summary>Grava de forma atômica (arquivo temporário + troca), para o leitor nunca ver um JSON pela metade.</summary>
    public static void GravarAtomico(string arquivo, string conteudo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        var tmp = arquivo + "." + Environment.ProcessId + ".tmp";
        File.WriteAllText(tmp, conteudo, new UTF8Encoding(false));
        File.Move(tmp, arquivo, true);
    }
}

/// <summary>Dados de conexão da ponte ativa.</summary>
public sealed class InfoPonte
{
    public int Protocolo { get; set; } = PonteServidor.VersaoProtocolo;
    public int Porta { get; set; }
    public string Token { get; set; } = "";
    public int Pid { get; set; }
    public string Aplicativo { get; set; } = "";
    public string Versao { get; set; } = "";
    public DateTime Iniciado { get; set; }

    public static InfoPonte? Ler(string arquivo)
    {
        try { return File.Exists(arquivo) ? JsonSerializer.Deserialize<InfoPonte>(File.ReadAllText(arquivo), JsonMcp.Arquivo) : null; }
        catch { return null; }
    }

    public void Gravar(string arquivo) => Locais.GravarAtomico(arquivo, JsonSerializer.Serialize(this, JsonMcp.Arquivo));
}

/// <summary>Configuração do lado do plugin.</summary>
public sealed class ConfigMcp
{
    /// <summary>Liga a ponte sozinha quando o Civil 3D carrega o PROJETA.</summary>
    public bool IniciarAutomatico { get; set; }

    public static ConfigMcp Ler()
    {
        try { return File.Exists(Locais.ArquivoConfig) ? JsonSerializer.Deserialize<ConfigMcp>(File.ReadAllText(Locais.ArquivoConfig), JsonMcp.Arquivo) ?? new() : new(); }
        catch { return new(); }
    }

    public void Salvar() => Locais.GravarAtomico(Locais.ArquivoConfig, JsonSerializer.Serialize(this, JsonMcp.Arquivo));
}

/// <summary>Cache do catálogo de ferramentas.</summary>
public static class CatalogoLocal
{
    public static void Gravar(JsonArray ferramentas, string aplicativo, string versao)
    {
        var o = new JsonObject
        {
            ["gerado"] = DateTime.Now.ToString("s"),
            ["aplicativo"] = aplicativo,
            ["versao"] = versao,
            ["ferramentas"] = ferramentas.DeepClone(),
        };
        Locais.GravarAtomico(Locais.ArquivoCatalogo, o.ToJsonString(JsonMcp.Indentado));
    }

    public static JsonArray? Ler()
    {
        try
        {
            if (!File.Exists(Locais.ArquivoCatalogo)) return null;
            return (JsonNode.Parse(File.ReadAllText(Locais.ArquivoCatalogo), documentOptions: JsonMcp.Leitura) as JsonObject)?["ferramentas"]?.DeepClone() as JsonArray;
        }
        catch { return null; }
    }
}

// ============================================================================ execução

/// <summary>Uma chamada agendada. Ou começa (e conclui), ou é cancelada antes de começar — nunca as duas coisas.</summary>
public sealed class Execucao
{
    private int _estado; // 0 = pendente, 1 = iniciada, 2 = cancelada
    private readonly TaskCompletionSource<ResultadoFerramenta> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<ResultadoFerramenta> Tarefa => _tcs.Task;
    public bool Iniciada => Volatile.Read(ref _estado) == 1;

    /// <summary>Chamado por quem executa, imediatamente antes de rodar. Falso = já foi cancelada; não execute.</summary>
    public bool TentarIniciar() => Interlocked.CompareExchange(ref _estado, 1, 0) == 0;

    /// <summary>Cancela se ainda não começou.</summary>
    public bool TentarCancelar()
    {
        if (Interlocked.CompareExchange(ref _estado, 2, 0) != 0) return false;
        _tcs.TrySetResult(ResultadoFerramenta.Falha("Chamada cancelada antes de começar."));
        return true;
    }

    public void Concluir(ResultadoFerramenta r) => _tcs.TrySetResult(r);
}

/// <summary>Decide onde e como a ferramenta roda (no Civil 3D: na thread principal, com o documento travado).</summary>
public interface IExecutor
{
    Execucao Agendar(Ferramenta f, Argumentos a);
}

/// <summary>Executor com fila numa thread própria, uma chamada por vez (testes e uso fora do AutoCAD).</summary>
public sealed class ExecutorFila : IExecutor, IDisposable
{
    private readonly BlockingCollection<Action> _fila = new();

    public ExecutorFila()
    {
        new Thread(() => { foreach (var a in _fila.GetConsumingEnumerable()) a(); }) { IsBackground = true, Name = "PROJETA-ExecutorFila" }.Start();
    }

    public Execucao Agendar(Ferramenta f, Argumentos a)
    {
        var e = new Execucao();
        _fila.Add(() =>
        {
            if (!e.TentarIniciar()) return;
            e.Concluir(f.ExecutarSeguro(a));
        });
        return e;
    }

    public void Dispose() => _fila.CompleteAdding();
}

// ============================================================================ canal

/// <summary>Mensagens de uma linha (JSON + '\n') com limite de tamanho.</summary>
internal static class Linha
{
    public static async Task<string?> Ler(Stream s, int limite, CancellationToken ct)
    {
        var buf = new byte[16384];
        using var ms = new MemoryStream();
        while (true)
        {
            int n = await s.ReadAsync(buf, ct).ConfigureAwait(false);
            if (n == 0) break;
            int idx = System.Array.IndexOf(buf, (byte)'\n', 0, n);
            if (idx >= 0) { ms.Write(buf, 0, idx); break; }
            ms.Write(buf, 0, n);
            if (ms.Length > limite) throw new InvalidDataException($"mensagem maior que {limite / 1024} KB");
        }
        if (ms.Length == 0) return null;
        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length).TrimEnd('\r');
    }

    public static async Task Escrever(Stream s, string texto, CancellationToken ct)
    {
        var b = Encoding.UTF8.GetBytes(texto + "\n");
        await s.WriteAsync(b, ct).ConfigureAwait(false);
        await s.FlushAsync(ct).ConfigureAwait(false);
    }

    public static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
}

/// <summary>
/// Lado do plugin: escuta em 127.0.0.1 (porta escolhida pelo sistema), exige o token gravado em ponte.json
/// e repassa cada chamada ao <see cref="IExecutor"/>. Só executa ferramentas registradas — nada de código arbitrário.
/// Protocolo: uma requisição por conexão, JSON numa linha: {"token","metodo":"ping|listar|chamar","ferramenta","argumentos"}.
/// </summary>
public sealed class PonteServidor : IDisposable
{
    public const int VersaoProtocolo = 1;
    private const int LimiteRequisicao = 4 * 1024 * 1024;

    private readonly RegistroFerramentas _reg;
    private readonly IExecutor _exec;
    private readonly Func<JsonObject>? _infoExtra;
    private TcpListener? _l;
    private CancellationTokenSource? _cts;

    public InfoPonte Info { get; } = new();
    public bool Ativa => _l is not null;
    public RegistroFerramentas Registro => _reg;

    /// <param name="infoExtra">Dados extras para "ping" (ex.: desenho ativo). Chamado fora da thread principal — devolva só valores já guardados.</param>
    public PonteServidor(RegistroFerramentas reg, IExecutor exec, string aplicativo, string versao, Func<JsonObject>? infoExtra = null)
    {
        _reg = reg; _exec = exec; _infoExtra = infoExtra;
        Info.Aplicativo = aplicativo; Info.Versao = versao;
    }

    public void Iniciar()
    {
        if (Ativa) return;
        var l = new TcpListener(IPAddress.Loopback, 0);
        if (OperatingSystem.IsWindows()) l.ExclusiveAddressUse = true;
        l.Start();
        Info.Porta = ((IPEndPoint)l.LocalEndpoint).Port;
        Info.Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        Info.Pid = Environment.ProcessId;
        Info.Iniciado = DateTime.Now;
        _l = l;
        _cts = new CancellationTokenSource();
        try
        {
            Info.Gravar(Locais.ArquivoPonte);
            CatalogoLocal.Gravar(_reg.Catalogo(), Info.Aplicativo, Info.Versao);
        }
        catch
        {
            Parar();
            throw;
        }
        var ct = _cts.Token;
        _ = Task.Run(() => LoopAceitar(l, ct));
    }

    public void Parar()
    {
        try { _cts?.Cancel(); } catch { }
        try { _l?.Stop(); } catch { }
        _l = null;
        try
        {
            var atual = InfoPonte.Ler(Locais.ArquivoPonte);
            if (atual is not null && atual.Token == Info.Token) File.Delete(Locais.ArquivoPonte);
        }
        catch { }
    }

    public void Dispose() => Parar();

    private async Task LoopAceitar(TcpListener l, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient c;
            try { c = await l.AcceptTcpClientAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { if (ct.IsCancellationRequested) break; continue; }
            catch (InvalidOperationException) { break; }
            _ = Task.Run(() => Atender(c, ct));
        }
    }

    private async Task Atender(TcpClient c, CancellationToken ct)
    {
        using (c)
        {
            JsonObject resp;
            NetworkStream s;
            try
            {
                c.NoDelay = true;
                s = c.GetStream();
                string? linha;
                using (var lim = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    lim.CancelAfter(TimeSpan.FromSeconds(30));
                    linha = await Linha.Ler(s, LimiteRequisicao, lim.Token).ConfigureAwait(false);
                }
                if (linha is null) return;
                resp = await Processar(linha, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                resp = Erro("falha na requisição: " + ex.Message);
                try { s = c.GetStream(); } catch { return; }
            }
            try { await Linha.Escrever(s, resp.ToJsonString(JsonMcp.Compacto), ct).ConfigureAwait(false); }
            catch { /* cliente desistiu */ }
        }
    }

    private static JsonObject Ok(JsonNode? resultado) => new() { ["ok"] = true, ["resultado"] = resultado };
    private static JsonObject Erro(string msg, string codigo = "erro") => new() { ["ok"] = false, ["erro"] = msg, ["codigo"] = codigo };

    private bool TokenConfere(string? token) =>
        token is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(Info.Token));

    public JsonObject InfoJson()
    {
        var o = new JsonObject
        {
            ["protocolo"] = VersaoProtocolo,
            ["aplicativo"] = Info.Aplicativo,
            ["versao"] = Info.Versao,
            ["pid"] = Info.Pid,
            ["iniciado"] = Info.Iniciado.ToString("s"),
            ["ferramentas"] = _reg.Quantidade,
        };
        if (_infoExtra is not null)
        {
            try { foreach (var (k, v) in _infoExtra()) o[k] = v?.DeepClone(); }
            catch { }
        }
        return o;
    }

    private async Task<JsonObject> Processar(string linha, CancellationToken ct)
    {
        JsonObject req;
        try { req = JsonNode.Parse(linha) as JsonObject ?? throw new JsonException("não é objeto"); }
        catch (JsonException ex) { return Erro("JSON inválido: " + ex.Message, "json"); }

        if (!TokenConfere(Linha.Str(req["token"]))) return Erro("token inválido", "token");

        switch (Linha.Str(req["metodo"]))
        {
            case "ping":
                return Ok(InfoJson());
            case "listar":
                return Ok(new JsonObject { ["ferramentas"] = _reg.Catalogo(), ["info"] = InfoJson() });
            case "chamar":
                return await Chamar(req, ct).ConfigureAwait(false);
            default:
                return Erro("método desconhecido", "metodo");
        }
    }

    private async Task<JsonObject> Chamar(JsonObject req, CancellationToken ct)
    {
        var nome = Linha.Str(req["ferramenta"]) ?? "";
        var f = _reg.Obter(nome);
        if (f is null) return Erro($"ferramenta desconhecida: '{nome}'. Disponíveis: {string.Join(", ", _reg.Todas.Select(x => x.Nome))}", "ferramenta");

        var bruto = req["argumentos"];
        if (bruto is not null and not JsonObject) return Ok(ResultadoFerramenta.Falha("Argumento inválido: 'arguments' deve ser um objeto JSON.").ParaJson());
        var args = new Argumentos((bruto as JsonObject)?.DeepClone() as JsonObject);

        var exec = _exec.Agendar(f, args);
        var limite = f.Limite;
        var venceu = await Task.WhenAny(exec.Tarefa, Task.Delay(limite, ct)).ConfigureAwait(false);
        if (venceu != exec.Tarefa)
        {
            if (exec.TentarCancelar())
                return Ok(ResultadoFerramenta.Falha(
                    $"O Civil 3D não começou a executar '{f.Nome}' em {limite.TotalSeconds:0} s — provavelmente há uma caixa de diálogo aberta ou um processamento longo. " +
                    "A chamada foi cancelada e NÃO será executada. Peça ao usuário para conferir o Civil 3D e tente de novo.").ParaJson());
            return Ok(ResultadoFerramenta.Falha(
                $"'{f.Nome}' começou mas passou de {limite.TotalSeconds:0} s e ainda está rodando no Civil 3D. Não repita a chamada; " +
                "aguarde e consulte o resultado depois (ex.: projeta_listar_redes).").ParaJson());
        }
        return Ok((await exec.Tarefa.ConfigureAwait(false)).ParaJson());
    }
}

/// <summary>Civil 3D fechado, ponte desligada ou sem resposta.</summary>
public sealed class PonteIndisponivel : Exception
{
    public PonteIndisponivel(string msg, Exception? inner = null) : base(msg, inner) { }
}

/// <summary>A ponte respondeu com erro (token, ferramenta desconhecida...).</summary>
public sealed class ErroPonte : Exception
{
    public string Codigo { get; }
    public ErroPonte(string msg, string codigo) : base(msg) { Codigo = codigo; }
}

/// <summary>Lado do servidor MCP: fala com a ponte do plugin.</summary>
public sealed class PonteCliente
{
    private readonly Func<InfoPonte?> _info;

    public PonteCliente(Func<InfoPonte?>? info = null) => _info = info ?? (() => InfoPonte.Ler(Locais.ArquivoPonte));

    public InfoPonte? InfoAtual() => _info();

    public async Task<JsonObject> Enviar(string metodo, JsonObject? corpo, TimeSpan espera, CancellationToken ct = default)
    {
        var info = _info() ?? throw new PonteIndisponivel("a ponte do PROJETA não está ativa (nenhum Civil 3D com PRJMCP ligado).");
        if (info.Protocolo != PonteServidor.VersaoProtocolo)
            throw new PonteIndisponivel($"versão de protocolo diferente (plugin {info.Protocolo}, servidor {PonteServidor.VersaoProtocolo}) — atualize o plugin e o servidor juntos.");

        using var c = new TcpClient { NoDelay = true };
        using (var cc = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            cc.CancelAfter(TimeSpan.FromSeconds(3));
            try { await c.ConnectAsync(IPAddress.Loopback, info.Porta, cc.Token).ConfigureAwait(false); }
            catch (Exception ex) when (ex is SocketException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
            {
                throw new PonteIndisponivel($"o Civil 3D não atendeu na porta {info.Porta} (fechado ou ponte desligada).", ex);
            }
        }

        var req = corpo?.DeepClone() as JsonObject ?? new JsonObject();
        req["token"] = info.Token;
        req["metodo"] = metodo;
        var s = c.GetStream();
        string? linha;
        try
        {
            await Linha.Escrever(s, req.ToJsonString(JsonMcp.Compacto), ct).ConfigureAwait(false);
            using var lim = CancellationTokenSource.CreateLinkedTokenSource(ct);
            lim.CancelAfter(espera);
            linha = await Linha.Ler(s, 512 * 1024 * 1024, lim.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new PonteIndisponivel($"o Civil 3D não respondeu em {espera.TotalSeconds:0} s.");
        }
        catch (IOException ex)
        {
            throw new PonteIndisponivel("a conexão com o Civil 3D caiu: " + ex.Message, ex);
        }
        if (linha is null) throw new PonteIndisponivel("o Civil 3D encerrou a conexão sem responder.");

        JsonObject resp;
        try { resp = JsonNode.Parse(linha) as JsonObject ?? throw new JsonException("não é objeto"); }
        catch (JsonException ex) { throw new PonteIndisponivel("resposta inválida da ponte: " + ex.Message, ex); }
        if (resp["ok"] is not JsonValue ok || !ok.TryGetValue<bool>(out var b) || !b)
            throw new ErroPonte(Linha.Str(resp["erro"]) ?? "erro desconhecido na ponte", Linha.Str(resp["codigo"]) ?? "erro");
        return resp["resultado"] as JsonObject ?? new JsonObject();
    }

    public Task<JsonObject> Ping(CancellationToken ct = default) => Enviar("ping", null, TimeSpan.FromSeconds(5), ct);

    public async Task<(JsonArray Ferramentas, JsonObject Info)> Listar(CancellationToken ct = default)
    {
        var r = await Enviar("listar", null, TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
        return (r["ferramentas"]?.DeepClone() as JsonArray ?? new JsonArray(), r["info"]?.DeepClone() as JsonObject ?? new JsonObject());
    }

    /// <summary>Chama uma ferramenta. A própria ponte aplica o limite de tempo da ferramenta; aqui só há uma espera máxima de segurança.</summary>
    public async Task<ResultadoFerramenta> Chamar(string nome, JsonObject? argumentos, CancellationToken ct = default)
    {
        var r = await Enviar("chamar", new JsonObject { ["ferramenta"] = nome, ["argumentos"] = argumentos?.DeepClone() },
            TimeSpan.FromMinutes(20), ct).ConfigureAwait(false);
        return ResultadoFerramenta.DeJson(r);
    }
}
