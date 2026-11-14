using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using Projeta.Mcp;

// ============================================================================
// Testes da ponte MCP do PROJETA (sem AutoCAD). Uso:  dotnet run
// Sobe a ponte com ferramentas de teste numa pasta temporária e exercita o servidor MCP
// como o Claude Desktop faria (initialize, tools/list, tools/call), inclusive com o "Civil 3D" fechado.
// ============================================================================

var pasta = Path.Combine(Path.GetTempPath(), "projeta-mcp-testes-" + Environment.ProcessId);
Directory.CreateDirectory(pasta);
Environment.SetEnvironmentVariable("PROJETA_MCP_DIR", pasta);
int ok = 0, falhas = 0;

async Task Teste(string nome, Func<Task> corpo)
{
    try { await corpo(); ok++; Console.WriteLine($"  ok   {nome}"); }
    catch (Exception ex) { falhas++; Console.WriteLine($"  FALHA {nome}: {ex.Message}"); }
}
void Verdade(bool cond, string oque) { if (!cond) throw new Exception(oque); }
void Igual<T>(T esperado, T obtido, string oque) { if (!Equals(esperado, obtido)) throw new Exception($"{oque}: esperado {esperado}, obtido {obtido}"); }
async Task<T> Lanca<T>(Func<Task> f) where T : Exception
{
    try { await f(); }
    catch (T ex) { return ex; }
    throw new Exception($"deveria lançar {typeof(T).Name}");
}
T LancaSync<T>(Action f) where T : Exception
{
    try { f(); }
    catch (T ex) { return ex; }
    throw new Exception($"deveria lançar {typeof(T).Name}");
}
string Txt(JsonNode? n) => n?.GetValue<string>() ?? "";

// ---------------------------------------------------------------- ferramentas de teste
int escritas = 0;
bool rapidaRodou = false;
RegistroFerramentas Registro(bool extra = false)
{
    var r = new RegistroFerramentas()
        .Adicionar(new FerramentaDelegada("eco", "Eco", "Devolve os argumentos.",
            () => new Esquema().Texto("texto", "Qualquer texto").Numero("valor", "Um número"),
            a => ResultadoFerramenta.Ok("eco: " + a.Texto("texto"), new JsonObject { ["texto"] = a.Texto("texto"), ["valor"] = JsonMcp.Numero(a.Numero("valor")) })))
        .Adicionar(new FerramentaDelegada("somar", "Somar", "Soma números.",
            () => new Esquema().ListaNumeros("numeros", "Parcelas", obrigatorio: true),
            a => ResultadoFerramenta.Ok(a.ListaNumeros("numeros").Sum().ToString(System.Globalization.CultureInfo.InvariantCulture))))
        .Adicionar(new FerramentaDelegada("escrever", "Escrever", "Altera o estado.", () => new Esquema(),
            _ => { Interlocked.Increment(ref escritas); return ResultadoFerramenta.Ok("escrito"); }, somenteLeitura: false))
        .Adicionar(new FerramentaDelegada("lenta", "Lenta", "Demora 1,5 s.", () => new Esquema(),
            _ => { Thread.Sleep(1500); return ResultadoFerramenta.Ok("terminou"); }, somenteLeitura: false, limite: TimeSpan.FromSeconds(10)))
        .Adicionar(new FerramentaDelegada("apressada", "Apressada", "Limite de 0,3 s.", () => new Esquema(),
            _ => { rapidaRodou = true; return ResultadoFerramenta.Ok("rodou"); }, limite: TimeSpan.FromSeconds(0.3)))
        .Adicionar(new FerramentaDelegada("explode", "Explode", "Lança exceção.", () => new Esquema(),
            _ => throw new InvalidOperationException("eLockViolation simulado")))
        .Adicionar(new FerramentaDelegada("acentos", "Acentos", "Texto com acentos e quebras.", () => new Esquema(),
            _ => ResultadoFerramenta.Ok("Ø800 — ação, coração, ü\nsegunda linha \"aspas\"", new JsonObject { ["seção"] = "Ø800", ["cota"] = 851.234 })));
    if (extra)
        r.Adicionar(new FerramentaDelegada("nova", "Nova", "Só existe na segunda versão do plugin.", () => new Esquema(), _ => ResultadoFerramenta.Ok("nova")));
    return r;
}

Console.WriteLine("== Esquema e argumentos");
await Teste("esquema: obrigatórios, enum, anulável e sem propriedades extras", () =>
{
    var e = new Esquema().Texto("rede", "Rede", obrigatorio: true, valores: new[] { "a", "b" }).Numero("d", "D", anulavel: true).Json();
    Igual(false, e["additionalProperties"]!.GetValue<bool>(), "additionalProperties");
    Igual("rede", Txt(e["required"]![0]), "required");
    Igual(2, e["properties"]!["rede"]!["enum"]!.AsArray().Count, "enum");
    Igual("null", Txt(e["properties"]!["d"]!["type"]![1]), "anulável");
    return Task.CompletedTask;
});
await Teste("argumentos: vírgula decimal, inteiros, booleanos, null x ausente", () =>
{
    var a = Argumentos.De("""{"a":"1,5","b":"1.234,56","c":3,"d":"sim","e":null,"l":"x","n":[1,"2,5"]}""");
    Igual(1.5, a.Numero("a"), "1,5");
    Igual(1234.56, a.Numero("b"), "1.234,56");
    Igual(3, a.Inteiro("c"), "inteiro");
    Igual(true, a.Booleano("d", false), "sim");
    Verdade(a.Tem("e") && a.EhNulo("e") && a.Numero("e") is null, "null presente");
    Verdade(!a.Tem("z") && !a.EhNulo("z"), "ausente");
    Igual(1, a.ListaTextos("l").Count, "texto solto vira lista");
    Igual(3.5, a.ListaNumeros("n").Sum(), "lista de números");
    LancaSync<ErroArgumento>(() => a.Inteiro("a"));
    LancaSync<ErroArgumento>(() => Argumentos.De("""{"x":"abc"}""").Numero("x"));
    return Task.CompletedTask;
});
await Teste("validação: argumento desconhecido e obrigatório ausente", () =>
{
    var f = Registro().Obter("somar")!;
    var r1 = f.ExecutarSeguro(Argumentos.De("""{"numeros":[1,2],"nmeros":[3]}"""));
    Verdade(r1.Erro && r1.Texto.Contains("nmeros") && r1.Texto.Contains("numeros"), r1.Texto);
    var r2 = f.ExecutarSeguro(Argumentos.De("{}"));
    Verdade(r2.Erro && r2.Texto.Contains("obrigatório"), r2.Texto);
    Igual("3", f.ExecutarSeguro(Argumentos.De("""{"numeros":[1,2]}""")).Texto, "soma");
    return Task.CompletedTask;
});
await Teste("nome de ferramenta inválido é recusado", () =>
{
    LancaSync<ArgumentException>(() => new RegistroFerramentas().Adicionar(new FerramentaDelegada("com espaço", "x", "x", () => new Esquema(), _ => ResultadoFerramenta.Ok(""))));
    LancaSync<ArgumentException>(() => Registro().Adicionar(new FerramentaDelegada("eco", "x", "x", () => new Esquema(), _ => ResultadoFerramenta.Ok(""))));
    return Task.CompletedTask;
});

Console.WriteLine("== Ponte (lado do plugin)");
var exec = new ExecutorFila();
var ponte = new PonteServidor(Registro(), exec, "Civil 3D (teste)", "9.9.9", () => new JsonObject { ["desenho"] = "teste.dwg" });
ponte.Iniciar();
var cliente = new PonteCliente();
await Teste("ponte.json e catalogo.json gravados", () =>
{
    var info = InfoPonte.Ler(Locais.ArquivoPonte)!;
    Verdade(info.Porta > 0 && info.Token.Length == 48 && info.Pid == Environment.ProcessId, "info da ponte");
    Igual(7, CatalogoLocal.Ler()!.Count, "catálogo");
    return Task.CompletedTask;
});
await Teste("ping e listar", async () =>
{
    var p = await cliente.Ping();
    Igual("teste.dwg", Txt(p["desenho"]), "desenho");
    Igual(7, (await cliente.Listar()).Ferramentas.Count, "ferramentas");
});
await Teste("chamar com argumentos (ida e volta)", async () =>
{
    var r = await cliente.Chamar("eco", new JsonObject { ["texto"] = "PV-01", ["valor"] = 2.5 });
    Verdade(!r.Erro && r.Texto == "eco: PV-01" && r.Dados!["valor"]!.GetValue<double>() == 2.5, r.Texto);
});
await Teste("token errado é recusado", async () =>
{
    var info = InfoPonte.Ler(Locais.ArquivoPonte)!;
    var falso = new PonteCliente(() => new InfoPonte { Porta = info.Porta, Token = new string('0', 48) });
    var ex = await Lanca<ErroPonte>(() => falso.Ping());
    Igual("token", ex.Codigo, "código");
});
await Teste("ferramenta desconhecida", async () => Igual("ferramenta", (await Lanca<ErroPonte>(() => cliente.Chamar("nao_existe", null))).Codigo, "código"));
await Teste("argumento desconhecido volta como erro da ferramenta", async () =>
{
    var r = await cliente.Chamar("eco", new JsonObject { ["txt"] = "x" });
    Verdade(r.Erro && r.Texto.StartsWith("Argumento inválido"), r.Texto);
});
await Teste("exceção na ferramenta vira resultado de erro", async () =>
{
    var r = await cliente.Chamar("explode", null);
    Verdade(r.Erro && r.Texto.Contains("eLockViolation"), r.Texto);
});
await Teste("acentos e quebras de linha preservados", async () =>
{
    var r = await cliente.Chamar("acentos", null);
    Verdade(r.Texto.StartsWith("Ø800 — ação") && r.Texto.Contains("\nsegunda linha \"aspas\""), r.Texto);
    Igual("Ø800", Txt(r.Dados!["seção"]), "chave com acento");
});
await Teste("fila ocupada: chamada que não começa no prazo é cancelada e nunca roda", async () =>
{
    rapidaRodou = false;
    var lenta = cliente.Chamar("lenta", null);
    await Task.Delay(150);
    var r = await cliente.Chamar("apressada", null);
    Verdade(r.Erro && r.Texto.Contains("NÃO será executada"), r.Texto);
    Igual("terminou", (await lenta).Texto, "lenta");
    await Task.Delay(300);
    Verdade(!rapidaRodou, "a chamada cancelada não pode rodar depois");
});
await Teste("20 chamadas simultâneas (serializadas pela fila)", async () =>
{
    escritas = 0;
    var rs = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => cliente.Chamar("escrever", null)));
    Verdade(rs.All(x => !x.Erro), "todas ok");
    Igual(20, escritas, "contador");
});

Console.WriteLine("== Servidor MCP (como o Claude Desktop usa)");
async Task<List<JsonObject>> Sessao(params string[] linhas)
{
    var saida = new StringWriter();
    var srv = new ServidorMcp(new PonteCliente(), TextWriter.Null, "1.0.0") { IntervaloMonitor = TimeSpan.FromHours(1) };
    await srv.Executar(new StringReader(string.Join("\n", linhas) + "\n"), saida);
    return saida.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => JsonNode.Parse(l) switch
    {
        JsonObject o => o,
        JsonArray a => new JsonObject { ["lote"] = a.DeepClone() },
        _ => throw new Exception("resposta inválida: " + l),
    }).ToList();
}
JsonObject PorId(List<JsonObject> rs, int id) => rs.FirstOrDefault(r => r["id"]?.ToJsonString() == id.ToString()) ?? throw new Exception($"sem resposta para id {id}");
const string Init = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"claude-ai","version":"0.1"}}}""";
const string Inicializado = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";

await Teste("initialize, initialized e tools/list", async () =>
{
    var rs = await Sessao(Init, Inicializado, """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""");
    Igual(2, rs.Count, "a notificação não tem resposta");
    var ini = PorId(rs, 1)["result"]!;
    Igual("2025-06-18", Txt(ini["protocolVersion"]), "versão");
    Igual("projeta-civil3d", Txt(ini["serverInfo"]!["name"]), "nome");
    Igual(true, ini["capabilities"]!["tools"]!["listChanged"]!.GetValue<bool>(), "listChanged");
    var tools = PorId(rs, 2)["result"]!["tools"]!.AsArray();
    Igual(8, tools.Count, "7 da ponte + projeta_conexao");
    Igual("projeta_conexao", Txt(tools[0]!["name"]), "primeira");
    Verdade(tools.All(t => t!["inputSchema"]!["type"]!.GetValue<string>() == "object"), "inputSchema");
    Igual(false, tools.First(t => Txt(t!["name"]) == "escrever")!["annotations"]!["readOnlyHint"]!.GetValue<bool>(), "readOnlyHint");
});
await Teste("versão de protocolo desconhecida cai na padrão", async () =>
{
    var rs = await Sessao("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"1999-01-01"}}""");
    Igual(ServidorMcp.VersaoProtocoloPadrao, Txt(PorId(rs, 1)["result"]!["protocolVersion"]), "versão");
});
await Teste("tools/call: sucesso, erro de ferramenta e conexão", async () =>
{
    var rs = await Sessao(Init,
        """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"somar","arguments":{"numeros":[1.5,"2,5"]}}}""",
        """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"explode","arguments":{}}}""",
        """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"projeta_conexao"}}""",
        """{"jsonrpc":"2.0","id":"seis","method":"tools/call","params":{"name":"acentos"}}""");
    var r3 = PorId(rs, 3)["result"]!;
    Igual(false, r3["isError"]!.GetValue<bool>(), "isError");
    Igual("4", Txt(r3["content"]![0]!["text"]), "soma");
    Igual(true, PorId(rs, 4)["result"]!["isError"]!.GetValue<bool>(), "explode isError");
    var r5 = PorId(rs, 5)["result"]!["content"]!;
    Verdade(Txt(r5[0]!["text"]).StartsWith("Conectado ao Civil 3D (teste)") && Txt(r5[0]!["text"]).Contains("teste.dwg"), Txt(r5[0]!["text"]));
    var r6 = rs.First(r => r["id"]?.ToJsonString() == "\"seis\"")["result"]!["content"]!;
    Verdade(Txt(r6[0]!["text"]).Contains("coração") && Txt(r6[1]!["text"]).Contains("\"seção\":\"Ø800\""), "acentos no MCP");
});
await Teste("erros de protocolo: JSON inválido, método desconhecido, lote", async () =>
{
    var rs = await Sessao("{isto não é json",
        """{"jsonrpc":"2.0","id":7,"method":"resources/list"}""",
        """[{"jsonrpc":"2.0","id":8,"method":"ping"},{"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":3}}]""");
    Verdade(rs.Any(r => r["error"]?["code"]?.GetValue<int>() == -32700), "parse error");
    Igual(-32601, PorId(rs, 7)["error"]!["code"]!.GetValue<int>(), "method not found");
    var lote = rs.First(r => r.ContainsKey("lote"))["lote"]!.AsArray();
    Igual(1, lote.Count, "lote só responde o ping");
});

await Teste("cliente genérico: structuredContent, outputSchema e prompts", async () =>
{
    var rs = await Sessao(Init,
        """{"jsonrpc":"2.0","id":10,"method":"tools/list"}""",
        """{"jsonrpc":"2.0","id":11,"method":"tools/call","params":{"name":"projeta_conexao","arguments":{}}}""",
        """{"jsonrpc":"2.0","id":12,"method":"prompts/list"}""",
        """{"jsonrpc":"2.0","id":13,"method":"prompts/get","params":{"name":"perfil_do_terreno","arguments":{"alinhamento":"EIXO 1"}}}""",
        """{"jsonrpc":"2.0","id":14,"method":"prompts/get","params":{"name":"perfil_do_terreno","arguments":{}}}""");

    // initialize anuncia a capacidade de prompts
    var caps = PorId(rs, 1)["result"]!["capabilities"]!;
    Verdade(caps["prompts"] is not null, "capabilities.prompts");

    // a ferramenta de conexão declara o formato de saída
    var conexao = PorId(rs, 10)["result"]!["tools"]!.AsArray()
        .First(t => Txt(t!["name"]) == ServidorMcp.FerramentaConexao);
    Verdade(conexao["outputSchema"]?["type"] is not null, "outputSchema na definição");

    // e o resultado vem também como objeto, não só como texto
    var r11 = PorId(rs, 11)["result"]!;
    var est = r11["structuredContent"];
    Verdade(est is JsonObject, "structuredContent é objeto");
    Verdade(est!["conectado"] is not null, "structuredContent.conectado");
    Verdade(Txt(est["resumo"]).Length > 0, "structuredContent.resumo");
    Verdade(r11["content"]!.AsArray().Count >= 1, "content continua vindo (compatibilidade)");

    var prompts = PorId(rs, 12)["result"]!["prompts"]!.AsArray();
    Verdade(prompts.Count >= 5, $"prompts/list trouxe {prompts.Count}");
    Verdade(prompts.Any(p => Txt(p!["name"]) == "inventario_desenho"), "prompt inventario_desenho");

    var msg = Txt(PorId(rs, 13)["result"]!["messages"]![0]!["content"]!["text"]);
    Verdade(msg.Contains("EIXO 1") && msg.Contains("civil_perfil_terreno"), msg);

    Igual(-32602, PorId(rs, 14)["error"]!["code"]!.GetValue<int>(), "argumento obrigatório ausente");
});

Console.WriteLine("== Civil 3D fechado");
ponte.Parar();
await Teste("ponte.json removido ao parar", () => { Verdade(!File.Exists(Locais.ArquivoPonte), "ponte.json"); return Task.CompletedTask; });
await Teste("tools/list usa o catálogo salvo; tools/call explica o que fazer", async () =>
{
    var rs = await Sessao(Init, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
        """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"eco","arguments":{"texto":"x"}}}""",
        """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"projeta_conexao","arguments":{}}}""");
    Igual(8, PorId(rs, 2)["result"]!["tools"]!.AsArray().Count, "catálogo salvo");
    var r3 = PorId(rs, 3)["result"]!;
    Verdade(r3["isError"]!.GetValue<bool>() && Txt(r3["content"]![0]!["text"]).Contains("PRJMCP"), Txt(r3["content"]![0]!["text"]));
    var r4 = PorId(rs, 4)["result"]!;
    Verdade(!r4["isError"]!.GetValue<bool>() && Txt(r4["content"]![0]!["text"]).Contains("NÃO conectado"), Txt(r4["content"]![0]!["text"]));
});
await Teste("ponte.json velho (Civil 3D fechou sem avisar)", async () =>
{
    new InfoPonte { Porta = 9, Token = "x", Pid = 1 }.Gravar(Locais.ArquivoPonte);
    var ex = await Lanca<PonteIndisponivel>(() => cliente.Ping());
    Verdade(ex.Message.Contains("porta 9"), ex.Message);
    File.Delete(Locais.ArquivoPonte);
});

Console.WriteLine("== Plugin atualizado com o Claude aberto");
await Teste("catálogo novo gera notifications/tools/list_changed", async () =>
{
    var entrada = new LeitorFila();
    var saida = new EscritorFila();
    var srv = new ServidorMcp(new PonteCliente(), TextWriter.Null, "1.0.0") { IntervaloMonitor = TimeSpan.FromMilliseconds(200) };
    var rodando = srv.Executar(entrada, saida);
    entrada.Enviar(Init);
    entrada.Enviar("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");
    Igual(8, saida.Esperar(l => l.Contains("\"id\":2"))!.Let(l => JsonNode.Parse(l)!["result"]!["tools"]!.AsArray().Count), "lista do cache");
    using var ponte2 = new PonteServidor(Registro(extra: true), exec, "Civil 3D (teste)", "9.9.10");
    ponte2.Iniciar();
    var aviso = saida.Esperar(l => l.Contains("notifications/tools/list_changed"), TimeSpan.FromSeconds(5));
    Verdade(aviso is not null, "deveria avisar a mudança");
    entrada.Enviar("""{"jsonrpc":"2.0","id":3,"method":"tools/list"}""");
    Igual(9, saida.Esperar(l => l.Contains("\"id\":3"))!.Let(l => JsonNode.Parse(l)!["result"]!["tools"]!.AsArray().Count), "lista nova");
    entrada.Fechar();
    await rodando;
});

Console.WriteLine("== Configuração do Claude Desktop");
await Teste("instalar preserva os outros servidores e faz backup; remover desfaz", () =>
{
    var cfg = Path.Combine(pasta, "claude", "claude_desktop_config.json");
    Directory.CreateDirectory(Path.GetDirectoryName(cfg)!);
    File.WriteAllText(cfg, """{ "mcpServers": { "outro": { "command": "x.exe", "args": ["--a"] } }, "preferencias": { "tema": "escuro" } }""");
    var bak = InstalacaoClaude.Instalar(cfg, @"C:\Users\User\AppData\Local\Projeta\mcp\servidor\Projeta.Mcp.Servidor.exe");
    var j = JsonNode.Parse(File.ReadAllText(cfg))!;
    Igual("x.exe", Txt(j["mcpServers"]!["outro"]!["command"]), "outro preservado");
    Verdade(Txt(j["mcpServers"]!["projeta-civil3d"]!["command"]).EndsWith("Projeta.Mcp.Servidor.exe"), "projeta incluído");
    Igual("escuro", Txt(j["preferencias"]!["tema"]), "outras chaves");
    Verdade(bak is not null && File.Exists(bak), "backup");
    Verdade(InstalacaoClaude.Instalado(cfg), "instalado");
    Verdade(InstalacaoClaude.Remover(cfg, out _), "remover");
    j = JsonNode.Parse(File.ReadAllText(cfg))!;
    Verdade(j["mcpServers"]!["outro"] is not null && j["mcpServers"]!["projeta-civil3d"] is null, "removido só o nosso");
    var novo = Path.Combine(pasta, "claude2", "claude_desktop_config.json");
    Verdade(InstalacaoClaude.Instalar(novo, "a.exe") is null && InstalacaoClaude.Instalado(novo), "arquivo novo");
    return Task.CompletedTask;
});

exec.Dispose();
Console.WriteLine();
Console.WriteLine($"Resultado: {ok} ok, {falhas} falha(s).");
try { Directory.Delete(pasta, true); } catch { }
return falhas > 0 ? 1 : 0;

// ---------------------------------------------------------------- auxiliares de E/S
sealed class LeitorFila : TextReader
{
    private readonly BlockingCollection<string?> _fila = new();
    public void Enviar(string linha) => _fila.Add(linha);
    public void Fechar() => _fila.Add(null);
    public override ValueTask<string?> ReadLineAsync(CancellationToken ct) => new(Task.Run(() => _fila.Take(ct), ct));
    public override string? ReadLine() => _fila.Take();
}

sealed class EscritorFila : TextWriter
{
    private readonly StringBuilder _atual = new();
    private readonly BlockingCollection<string> _linhas = new();
    private readonly List<string> _vistas = new();
    public override Encoding Encoding => Encoding.UTF8;
    public override void Write(char c)
    {
        lock (_atual)
        {
            if (c == '\n') { _linhas.Add(_atual.ToString()); _atual.Clear(); }
            else _atual.Append(c);
        }
    }
    public override void Write(string? s) { foreach (var c in s ?? "") Write(c); }
    public string? Esperar(Func<string, bool> cond, TimeSpan? limite = null)
    {
        var fim = DateTime.UtcNow + (limite ?? TimeSpan.FromSeconds(5));
        lock (_vistas) { var j = _vistas.FirstOrDefault(cond); if (j is not null) return j; }
        while (DateTime.UtcNow < fim)
        {
            if (!_linhas.TryTake(out var l, 100)) continue;
            lock (_vistas) _vistas.Add(l);
            if (cond(l)) return l;
        }
        return null;
    }
}

static class Ext
{
    public static R Let<T, R>(this T v, Func<T, R> f) => f(v);
}
