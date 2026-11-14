using System.Reflection;
using System.Text.Json;
using Projeta.Core.Comum;

namespace Projeta.Core.Modelador;

/// <summary>
/// Biblioteca de dispositivos e seções-tipo: definições padrão (embutidas) + arquivos JSON do usuário.
/// Pasta do usuário: %APPDATA%\Projeta\biblioteca\dispositivos\*.json e ...\secoes\*.json
/// (cada arquivo pode conter um objeto ou uma lista). Definições do usuário com o mesmo Id substituem as padrão.
/// </summary>
public sealed class Biblioteca
{
    public Dictionary<string, DefinicaoDispositivo> Dispositivos { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SecaoTipo> Secoes { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Arquivo de origem de cada definição (vazio = embutida).</summary>
    public Dictionary<string, string> Origem { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Mensagem> Mensagens { get; } = new();
    public string? PastaUsuario { get; private set; }

    public static string PastaPadrao => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "biblioteca");

    public static Biblioteca Carregar(string? pastaUsuario = null)
    {
        var b = new Biblioteca();
        foreach (var d in LerRecurso<DefinicaoDispositivo>("dispositivos.json")) { b.Dispositivos[d.Id] = d; b.Origem["D:" + d.Id] = ""; }
        foreach (var s in LerRecurso<SecaoTipo>("secoes.json")) { b.Secoes[s.Id] = s; b.Origem["S:" + s.Id] = ""; }
        b.PastaUsuario = pastaUsuario ?? PastaPadrao;
        b.CarregarPasta(Path.Combine(b.PastaUsuario, "dispositivos"), true);
        b.CarregarPasta(Path.Combine(b.PastaUsuario, "secoes"), false);
        return b;
    }

    private void CarregarPasta(string pasta, bool dispositivos)
    {
        if (!Directory.Exists(pasta)) return;
        foreach (var arq in Directory.GetFiles(pasta, "*.json").OrderBy(x => x))
        {
            try
            {
                var txt = File.ReadAllText(arq);
                if (dispositivos)
                    foreach (var d in LerObjetoOuLista<DefinicaoDispositivo>(txt)) { Dispositivos[d.Id] = d; Origem["D:" + d.Id] = arq; }
                else
                    foreach (var s in LerObjetoOuLista<SecaoTipo>(txt)) { Secoes[s.Id] = s; Origem["S:" + s.Id] = arq; }
            }
            catch (Exception ex)
            {
                Mensagens.Add(new(Gravidade.Erro, $"Erro ao ler {Path.GetFileName(arq)}: {ex.Message}"));
            }
        }
    }

    public static List<T> LerObjetoOuLista<T>(string json)
    {
        var doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<T>>(json, JsonProjeta.Opcoes) ?? new();
        var o = JsonSerializer.Deserialize<T>(json, JsonProjeta.Opcoes);
        return o is null ? new() : new() { o };
    }

    private static List<T> LerRecurso<T>(string nome)
    {
        var asm = typeof(Biblioteca).Assembly;
        var res = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(nome, StringComparison.OrdinalIgnoreCase));
        if (res is null) return new();
        using var st = asm.GetManifestResourceStream(res)!;
        using var rd = new StreamReader(st);
        return LerObjetoOuLista<T>(rd.ReadToEnd());
    }

    public static string TextoRecurso(string nome)
    {
        var asm = typeof(Biblioteca).Assembly;
        var res = asm.GetManifestResourceNames().First(n => n.EndsWith(nome, StringComparison.OrdinalIgnoreCase));
        using var st = asm.GetManifestResourceStream(res)!;
        using var rd = new StreamReader(st);
        return rd.ReadToEnd();
    }

    /// <summary>Salva (ou substitui) uma definição de dispositivo na pasta do usuário.</summary>
    public string SalvarDispositivo(DefinicaoDispositivo d)
    {
        var pasta = Path.Combine(PastaUsuario ?? PastaPadrao, "dispositivos");
        Directory.CreateDirectory(pasta);
        var arq = Path.Combine(pasta, Texto.IdSeguro(d.Id, 60) + ".json");
        File.WriteAllText(arq, JsonProjeta.Serializar(d));
        Dispositivos[d.Id] = d; Origem["D:" + d.Id] = arq;
        return arq;
    }

    public string SalvarSecao(SecaoTipo s)
    {
        var pasta = Path.Combine(PastaUsuario ?? PastaPadrao, "secoes");
        Directory.CreateDirectory(pasta);
        var arq = Path.Combine(pasta, Texto.IdSeguro(s.Id, 60) + ".json");
        File.WriteAllText(arq, JsonProjeta.Serializar(s));
        Secoes[s.Id] = s; Origem["S:" + s.Id] = arq;
        return arq;
    }

    /// <summary>Valida todas as definições com os valores padrão.</summary>
    public List<Mensagem> Validar()
    {
        var l = new List<Mensagem>();
        foreach (var d in Dispositivos.Values)
        {
            var m = AvaliadorDispositivo.Avaliar(d);
            l.AddRange(m.Mensagens);
            if (m.Saida.Count == 0 && d.Solidos.Count > 0) l.Add(new(Gravidade.Erro, "Nenhum sólido de saída", d.Id));
        }
        foreach (var s in Secoes.Values)
        {
            var a = AvaliadorDispositivo.AvaliarSecao(s);
            l.AddRange(a.Mensagens);
            if (a.Concreto.Count < 3) l.Add(new(Gravidade.Erro, "Polígono de concreto inválido", s.Id));
        }
        return l;
    }
}
