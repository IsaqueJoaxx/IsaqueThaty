using System.Reflection;
using System.Text.Json;
using Projeta.Giro.Core.Comum;

namespace Projeta.Giro.Core.Veiculos;

/// <summary>
/// Biblioteca de veículos: os de referência (DNIT, embutidos) mais os do usuário (arquivos .json numa pasta).
/// Um veículo do usuário com o mesmo código substitui o de referência.
/// </summary>
public sealed class Biblioteca
{
    public const string ArquivoUsuario = "veiculos_usuario.json";

    private readonly Dictionary<string, Veiculo> _v = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Padrao { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Mensagens { get; } = new();
    public string? Pasta { get; private set; }

    public IReadOnlyCollection<Veiculo> Todos => _v.Values;
    public IEnumerable<Veiculo> Ordenados => _v.Values.OrderBy(v => Padrao.Contains(v.Id) ? 0 : 1).ThenBy(v => v.Comprimento);

    public Veiculo? Obter(string? id) => id is not null && _v.TryGetValue(id, out var v) ? JsonGiro.Clonar(v) : null;
    public bool Existe(string id) => _v.ContainsKey(id);

    public static List<Veiculo> Referencia()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Projeta.Giro.Core.Recursos.veiculos_dnit.json")
                      ?? throw new InvalidOperationException("recurso veiculos_dnit.json ausente");
        using var r = new StreamReader(s);
        return JsonGiro.Ler<List<Veiculo>>(r.ReadToEnd()) ?? new();
    }

    public static Biblioteca Carregar(string? pasta)
    {
        var b = new Biblioteca { Pasta = pasta };
        foreach (var v in Referencia()) { b._v[v.Id] = v; b.Padrao.Add(v.Id); }
        if (string.IsNullOrWhiteSpace(pasta) || !Directory.Exists(pasta)) return b;
        foreach (var arq in Directory.GetFiles(pasta, "*.json"))
        {
            try
            {
                var texto = File.ReadAllText(arq);
                var lista = texto.TrimStart().StartsWith("[") ? JsonGiro.Ler<List<Veiculo>>(texto) : new List<Veiculo> { JsonGiro.Ler<Veiculo>(texto)! };
                foreach (var v in lista ?? new())
                {
                    if (v is null || string.IsNullOrWhiteSpace(v.Id)) continue;
                    var erros = v.Validar().Where(m => m.Gravidade == Gravidade.Erro).ToList();
                    if (erros.Count > 0) { b.Mensagens.Add($"{Path.GetFileName(arq)} / {v.Id}: {string.Join("; ", erros.Select(e => e.Texto))}"); continue; }
                    b._v[v.Id] = v;
                    b.Padrao.Remove(v.Id);
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                b.Mensagens.Add($"{Path.GetFileName(arq)}: {ex.Message}");
            }
        }
        return b;
    }

    /// <summary>Grava (ou substitui) um veículo no arquivo do usuário.</summary>
    public void Salvar(Veiculo v)
    {
        if (Pasta is null) throw new InvalidOperationException("pasta da biblioteca não definida");
        Directory.CreateDirectory(Pasta);
        var arq = Path.Combine(Pasta, ArquivoUsuario);
        var lista = File.Exists(arq) ? JsonGiro.Ler<List<Veiculo>>(File.ReadAllText(arq)) ?? new() : new List<Veiculo>();
        lista.RemoveAll(x => string.Equals(x.Id, v.Id, StringComparison.OrdinalIgnoreCase));
        lista.Add(v);
        File.WriteAllText(arq, JsonGiro.Serializar(lista));
        _v[v.Id] = JsonGiro.Clonar(v);
        Padrao.Remove(v.Id);
    }

    /// <summary>Remove um veículo do usuário (os de referência voltam a valer).</summary>
    public bool Remover(string id)
    {
        if (Pasta is null) return false;
        var arq = Path.Combine(Pasta, ArquivoUsuario);
        if (!File.Exists(arq)) return false;
        var lista = JsonGiro.Ler<List<Veiculo>>(File.ReadAllText(arq)) ?? new();
        if (lista.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) == 0) return false;
        File.WriteAllText(arq, JsonGiro.Serializar(lista));
        _v.Remove(id);
        var refe = Referencia().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        if (refe is not null) { _v[refe.Id] = refe; Padrao.Add(refe.Id); }
        return true;
    }
}
