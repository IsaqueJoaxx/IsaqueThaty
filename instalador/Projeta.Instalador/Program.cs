using System.Runtime.InteropServices;

namespace Projeta.Instalador;

/// <summary>
/// Instalar-PROJETA.exe
///   (sem argumentos)                         janela de instalação
///   --silencioso [--componentes civil,mcp,claude,pointtop,iri,lisp] [--pasta X] [--raiz-teste Y]
///   --raiz-teste Y   instala tudo dentro de Y (não toca o Windows, o Civil 3D nem o Claude Desktop) — para conferir o pacote
///   --conteudo       lista o que tem no pacote
/// </summary>
internal static class Program
{
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);

    [STAThread]
    private static int Main(string[] args)
    {
        string? Valor(string nome) { int i = Array.FindIndex(args, a => a.Equals(nome, StringComparison.OrdinalIgnoreCase)); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        bool Tem(string nome) => args.Any(a => a.Equals(nome, StringComparison.OrdinalIgnoreCase));

        if (Tem("--silencioso") || Tem("--conteudo"))
        {
            AttachConsole(-1);
            try
            {
                if (Tem("--conteudo"))
                {
                    using var s = Instalador.Pacote();
                    if (s is null) { Console.WriteLine("Sem pacote."); return 1; }
                    using var z = new System.IO.Compression.ZipArchive(s);
                    Console.WriteLine($"Versão {Instalador.Versao()}: {z.Entries.Count} arquivos, {z.Entries.Sum(e => e.Length) / 1048576.0:F0} MB");
                    foreach (var g in z.Entries.GroupBy(e => string.Join('/', e.FullName.Replace('\\', '/').Split('/').Take(2))).OrderBy(g => g.Key))
                        Console.WriteLine($"  {g.Key}: {g.Count()} arquivo(s), {g.Sum(e => e.Length) / 1048576.0:F1} MB");
                    return 0;
                }
                var raiz = Valor("--raiz-teste");
                var d = raiz is null ? new Destinos() : Destinos.ParaTeste(Path.GetFullPath(raiz));
                if (Valor("--pasta") is { } pasta) d.Programas = Path.GetFullPath(pasta);
                var c = Componentes.Nenhum;
                var lista = Valor("--componentes") ?? "civil,mcp,pointtop,iri,lisp";
                foreach (var p in lista.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    c |= p.ToLowerInvariant() switch
                    {
                        "civil" => Componentes.Civil, "mcp" => Componentes.Mcp, "claude" => Componentes.Mcp | Componentes.Claude,
                        "pointtop" => Componentes.PointTop, "iri" => Componentes.Iri, "lisp" => Componentes.Lisp, "tudo" => Componentes.Tudo,
                        _ => throw new ArgumentException($"Componente desconhecido: {p}"),
                    };
                Directory.CreateDirectory(d.Programas);
                var arqLog = Path.Combine(d.Programas, "instalacao.log");
                File.WriteAllText(arqLog, $"Instalar-PROJETA {Instalador.Versao()} - {DateTime.Now:s}{Environment.NewLine}");
                var inst = new Instalador(s => { Console.WriteLine(s); File.AppendAllText(arqLog, s + Environment.NewLine); });
                inst.Instalar(c, d);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO: " + ex.Message);
                try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "Instalar-PROJETA-erro.log"), $"{DateTime.Now:s} {ex}{Environment.NewLine}"); } catch { }
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Janela());
        return 0;
    }
}
