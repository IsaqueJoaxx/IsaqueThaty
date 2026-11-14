using System.Text;
using System.Text.Json.Nodes;
using Projeta.Mcp;

// ============================================================================
// PROJETA • servidor MCP para o Claude Desktop.
//   Projeta.Mcp.Servidor.exe                         servidor MCP (stdio) — é assim que o Claude Desktop o inicia
//   Projeta.Mcp.Servidor.exe --status                mostra se o Civil 3D está conectado
//   Projeta.Mcp.Servidor.exe --ferramentas           lista as ferramentas
//   Projeta.Mcp.Servidor.exe --chamar <nome> [json]  chama uma ferramenta (teste sem o Claude)
//   Projeta.Mcp.Servidor.exe --instalar-claude [--config <arquivo>]
//   Projeta.Mcp.Servidor.exe --remover-claude  [--config <arquivo>]
// ============================================================================

var utf8 = new UTF8Encoding(false);
var versao = typeof(ServidorMcp).Assembly.GetName().Version?.ToString(3) ?? "?";

if (args.Length == 0 || args[0] == "--stdio")
{
    // stdout é exclusivo do protocolo; tudo que for log vai para stderr (o Claude Desktop guarda em mcp-server-*.log)
    var entrada = new StreamReader(Console.OpenStandardInput(), utf8);
    var saida = new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = false, NewLine = "\n" };
    var log = new StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true };
    log.WriteLine($"PROJETA MCP {versao} — pasta da ponte: {Locais.Pasta}");
    await new ServidorMcp(new PonteCliente(), log, versao).Executar(entrada, saida);
    return 0;
}

Console.OutputEncoding = utf8;
string? Opcao(string nome)
{
    int i = Array.IndexOf(args, nome);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
var ponte = new PonteCliente();

try
{
    switch (args[0])
    {
        case "--status":
        {
            var pedido = new JsonObject
            {
                ["jsonrpc"] = "2.0", ["id"] = 1, ["method"] = "tools/call",
                ["params"] = new JsonObject { ["name"] = ServidorMcp.FerramentaConexao, ["arguments"] = new JsonObject() },
            };
            var r = await new ServidorMcp(ponte, TextWriter.Null, versao).Processar(pedido, default);
            foreach (var c in (JsonArray)r!["result"]!["content"]!) Console.WriteLine(c!["text"]!.GetValue<string>());
            Console.WriteLine($"Claude Desktop configurado: {(InstalacaoClaude.Instalado(Opcao("--config") ?? InstalacaoClaude.ConfigPadrao) ? "sim" : "não")}");
            return 0;
        }
        case "--ferramentas":
        {
            JsonArray cat;
            try { cat = (await ponte.Listar()).Ferramentas; Console.WriteLine("(do Civil 3D conectado)"); }
            catch (Exception ex) when (ex is PonteIndisponivel or ErroPonte)
            {
                cat = CatalogoLocal.Ler() ?? new JsonArray();
                Console.WriteLine($"(Civil 3D não conectado: {ex.Message} — mostrando o catálogo salvo)");
            }
            foreach (var t in cat)
            {
                var ro = t?["annotations"]?["readOnlyHint"]?.GetValue<bool>() == true;
                Console.WriteLine($"  {t?["name"],-30} {(ro ? "leitura" : "ESCRITA"),-8} {t?["title"]}");
            }
            return 0;
        }
        case "--chamar":
        {
            if (args.Length < 2) { Console.Error.WriteLine("uso: --chamar <ferramenta> [json]"); return 2; }
            var json = args.Length > 2 ? string.Join(" ", args.Skip(2)) : "{}";
            JsonObject a;
            try { a = JsonNode.Parse(json, documentOptions: JsonMcp.Leitura) as JsonObject ?? throw new System.Text.Json.JsonException("não é um objeto"); }
            catch (System.Text.Json.JsonException ex)
            {
                Console.Error.WriteLine($"Argumentos JSON inválidos ({ex.Message}). No PowerShell 5, escape as aspas: '{{\\\"rede\\\":\\\"Drenagem 1\\\"}}'");
                return 2;
            }
            var r = await ponte.Chamar(args[1], a);
            Console.WriteLine(r.Texto);
            if (r.Dados is not null) Console.WriteLine(JsonMcp.Texto(r.Dados, true));
            return r.Erro ? 1 : 0;
        }
        case "--config-mcp":
        {
            // Bloco de configuração para QUALQUER cliente MCP com servidor local por stdio
            // (Autodesk Assistant, VS Code, Cursor, Claude Desktop...). Só varia a chave de topo.
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("caminho do executável desconhecido");
            var servidor = new JsonObject
            {
                ["command"] = exe,
                ["args"] = new JsonArray(),
                ["type"] = "stdio",
                ["env"] = new JsonObject(),
            };
            var chave = Opcao("--chave") ?? "mcpServers";
            var bloco = new JsonObject { [chave] = new JsonObject { [ServidorMcp.NomeServidor] = servidor } };
            Console.WriteLine(bloco.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Console.Error.WriteLine();
            Console.Error.WriteLine("Cole este bloco no arquivo de configuração do cliente MCP.");
            Console.Error.WriteLine("  Claude Desktop / Autodesk Assistant: chave 'mcpServers' (padrão).");
            Console.Error.WriteLine("  VS Code: use --chave servers.");
            Console.Error.WriteLine("O Civil 3D precisa estar aberto com o PROJETA carregado e o comando PRJMCP rodado.");
            return 0;
        }
        case "--instalar-claude":
        {
            var cfg = Opcao("--config") ?? InstalacaoClaude.ConfigPadrao;
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("caminho do executável desconhecido");
            var bak = InstalacaoClaude.Instalar(cfg, exe);
            Console.WriteLine($"Servidor '{ServidorMcp.NomeServidor}' configurado em {cfg}");
            Console.WriteLine($"  comando: {exe}");
            if (bak is not null) Console.WriteLine($"  backup:  {bak}");
            Console.WriteLine("Reinicie o Claude Desktop (feche pela bandeja do sistema) para ele carregar o servidor.");
            return 0;
        }
        case "--remover-claude":
        {
            var cfg = Opcao("--config") ?? InstalacaoClaude.ConfigPadrao;
            Console.WriteLine(InstalacaoClaude.Remover(cfg, out var bak)
                ? $"Removido de {cfg} (backup: {bak})."
                : $"O servidor '{ServidorMcp.NomeServidor}' não estava em {cfg}.");
            return 0;
        }
        default:
            Console.WriteLine("PROJETA MCP " + versao);
            Console.WriteLine("  (sem argumentos)          servidor MCP via stdio — usado pelo Claude Desktop");
            Console.WriteLine("  --status                  verifica a conexão com o Civil 3D");
            Console.WriteLine("  --ferramentas             lista as ferramentas");
            Console.WriteLine("  --chamar <nome> [json]    chama uma ferramenta, ex.: --chamar projeta_listar_redes {}");
            Console.WriteLine("  --config-mcp [--chave k]  imprime o bloco de configuração para qualquer cliente MCP (stdio)");
            Console.WriteLine("  --instalar-claude         inclui este servidor no claude_desktop_config.json (com backup)");
            Console.WriteLine("  --remover-claude          remove este servidor do claude_desktop_config.json");
            Console.WriteLine("  --config <arquivo>        outro claude_desktop_config.json");
            return args[0] is "-h" or "--help" or "--ajuda" ? 0 : 2;
    }
}
catch (Exception ex) when (ex is PonteIndisponivel or ErroPonte)
{
    Console.Error.WriteLine("Civil 3D não conectado: " + ex.Message);
    return 3;
}
