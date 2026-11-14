using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>Inclui/remove o servidor do PROJETA no claude_desktop_config.json, preservando o resto e fazendo backup.</summary>
public static class InstalacaoClaude
{
    public static string ConfigPadrao =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude", "claude_desktop_config.json");

    private static JsonObject Ler(string arquivo)
    {
        if (!File.Exists(arquivo)) return new JsonObject();
        var texto = File.ReadAllText(arquivo);
        if (string.IsNullOrWhiteSpace(texto)) return new JsonObject();
        return JsonNode.Parse(texto, documentOptions: JsonMcp.Leitura) as JsonObject
               ?? throw new InvalidDataException($"{arquivo} não contém um objeto JSON.");
    }

    private static string? Backup(string arquivo)
    {
        if (!File.Exists(arquivo)) return null;
        var bak = arquivo + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        File.Copy(arquivo, bak, true);
        return bak;
    }

    public static bool Instalado(string arquivo)
    {
        try { return Ler(arquivo)["mcpServers"] is JsonObject s && s.ContainsKey(ServidorMcp.NomeServidor); }
        catch { return false; }
    }

    /// <returns>Caminho do backup (ou null se o arquivo não existia).</returns>
    public static string? Instalar(string arquivo, string executavel)
    {
        var raiz = Ler(arquivo);
        if (raiz["mcpServers"] is not JsonObject servidores)
        {
            servidores = new JsonObject();
            raiz["mcpServers"] = servidores;
        }
        servidores[ServidorMcp.NomeServidor] = new JsonObject
        {
            ["command"] = executavel,
            ["args"] = new JsonArray(),
        };
        var bak = Backup(arquivo);
        Locais.GravarAtomico(arquivo, raiz.ToJsonString(JsonMcp.Indentado));
        return bak;
    }

    public static bool Remover(string arquivo, out string? backup)
    {
        backup = null;
        var raiz = Ler(arquivo);
        if (raiz["mcpServers"] is not JsonObject s || !s.Remove(ServidorMcp.NomeServidor)) return false;
        backup = Backup(arquivo);
        Locais.GravarAtomico(arquivo, raiz.ToJsonString(JsonMcp.Indentado));
        return true;
    }
}
