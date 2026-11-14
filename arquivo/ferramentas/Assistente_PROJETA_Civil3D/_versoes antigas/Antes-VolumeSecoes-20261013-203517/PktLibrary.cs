using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using IOPath = System.IO.Path;

namespace Projeta.Civil3D;

// Biblioteca de submontagens do Subassembly Composer (.pkt) da PROJETA.
// Seções tipo trazidas de outro computador guardam o caminho do PKT de lá (por exemplo
// C:\Users\Admin\Desktop\...\Aduela.pkt). Sem o arquivo nesse caminho, o Civil 3D acusa
// "submontagens ausentes" e o corredor sai vazio. Aqui o PKT é procurado na pasta da
// biblioteca: primeiro pelo nome do arquivo, depois pela classe gravada dentro do PKT.
internal static class PktLibrary
{
    private const string RegistryKey = @"Software\PROJETA\AssistenteCivil";
    private const string DefaultFolder = @"D:\PROJETA ENGENHARIA\_FERRAMENTAS\pkt";
    private static readonly Regex ClassPattern =
        new(@"<DotNetClass\b[^>]*>\s*([^<\s]+)\s*</DotNetClass>", RegexOptions.CultureInvariant);

    // Pasta escolhida pelo usuário (guardada no registro do Windows, só para este usuário).
    public static string Folder
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (key?.GetValue("PastaPKT") is string saved && saved.Length > 0) return saved;
            }
            catch { }
            return DefaultFolder;
        }
        set
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
                key.SetValue("PastaPKT", value);
            }
            catch { }
        }
    }

    // Nomes dos arquivos .pkt da pasta, para o painel dizer o que será encontrado antes de corrigir.
    public static HashSet<string> FileNames(string folder) =>
        new(Files(folder).Select(f => IOPath.GetFileName(f)), StringComparer.OrdinalIgnoreCase);

    // Arquivos .pkt da pasta e das subpastas, os mais próximos da raiz primeiro.
    public static IReadOnlyList<string> Files(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !IODirectory.Exists(folder)) return Array.Empty<string>();
        try
        {
            return IODirectory.EnumerateFiles(folder, "*.pkt", System.IO.SearchOption.AllDirectories)
                .Take(2000)
                .OrderBy(f => f.Count(c => c == IOPath.DirectorySeparatorChar))
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    // Índice da pasta para uma correção: busca pelo nome do arquivo e, se falhar, pela classe.
    public sealed class Index
    {
        private readonly IReadOnlyList<string> files;
        private Dictionary<string, string>? byClass;

        public Index(string folder) => files = Files(folder);

        public int Count => files.Count;

        public string? Find(string oldPath, string className)
        {
            var name = IOPath.GetFileName(oldPath ?? "");
            if (name.Length > 0)
            {
                var byName = files.FirstOrDefault(f => string.Equals(IOPath.GetFileName(f), name, StringComparison.OrdinalIgnoreCase));
                if (byName is not null) return byName;
            }
            if (string.IsNullOrEmpty(className)) return null;
            byClass ??= BuildClassMap();
            return byClass.TryGetValue(className, out var path) ? path : null;
        }

        private Dictionary<string, string> BuildClassMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in files)
                foreach (var cls in Classes(file))
                    map.TryAdd(cls, file);
            return map;
        }
    }

    // Classes declaradas no catálogo (.atc) de dentro do PKT. Um PKT é um arquivo zip.
    public static IReadOnlyList<string> Classes(string pkt)
    {
        var found = new List<string>();
        try
        {
            if (!IOFile.Exists(pkt)) return found;
            using var zip = ZipFile.OpenRead(pkt);
            foreach (var entry in zip.Entries)
            {
                if (!entry.FullName.EndsWith(".atc", StringComparison.OrdinalIgnoreCase) || entry.Length > 1_000_000) continue;
                using var reader = new System.IO.StreamReader(entry.Open());
                foreach (Match m in ClassPattern.Matches(reader.ReadToEnd())) found.Add(m.Groups[1].Value);
            }
        }
        catch { }
        return found;
    }
}
