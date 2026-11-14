using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Projeta.Instalador;

[Flags]
public enum Componentes
{
    Nenhum = 0,
    Civil = 1,     // plugins do Civil 3D (PROJETA, Giro, Sinalização, Terraplenagem IFM, IQ C3D) + biblioteca
    Mcp = 2,       // servidor MCP do PROJETA
    Claude = 4,    // registra o servidor MCP no Claude Desktop
    PointTop = 8,
    Iri = 16,
    Lisp = 32,
    Tudo = Civil | Mcp | PointTop | Iri | Lisp,
}

/// <summary>Onde cada coisa vai. Em teste (raiz), tudo vai para dentro da pasta de teste e nada toca o registro nem o Claude.</summary>
public sealed class Destinos
{
    public string AppData { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public string LocalAppData { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public string ProgramData { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    public string Desktop { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public string MenuIniciar { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    public string Programas { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PROJETA");
    public bool Teste { get; init; }

    public static Destinos ParaTeste(string raiz) => new()
    {
        AppData = Path.Combine(raiz, "AppData"), LocalAppData = Path.Combine(raiz, "LocalAppData"), ProgramData = Path.Combine(raiz, "ProgramData"),
        Desktop = Path.Combine(raiz, "Desktop"), MenuIniciar = Path.Combine(raiz, "MenuIniciar"), Programas = Path.Combine(raiz, "Programas", "PROJETA"), Teste = true,
    };

    public string Plugins => Path.Combine(AppData, "Autodesk", "ApplicationPlugins");
    public string PluginsTodos => Path.Combine(ProgramData, "Autodesk", "ApplicationPlugins");
}

public sealed class Instalador
{
    private readonly Action<string> _log;
    private readonly Action<int>? _progresso;
    private readonly string _carimbo = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    private readonly List<string> _instalados = new();
    public List<string> Avisos { get; } = new();

    public Instalador(Action<string> log, Action<int>? progresso = null) { _log = log; _progresso = progresso; }

    public static Stream? Pacote() => Assembly.GetExecutingAssembly().GetManifestResourceStream("pacote.zip");

    public static string Versao()
    {
        using var s = Pacote();
        if (s is null) return "(sem pacote)";
        using var z = new ZipArchive(s, ZipArchiveMode.Read);
        var m = z.GetEntry("manifesto.json");
        if (m is null) return "?";
        using var r = new StreamReader(m.Open());
        using var d = JsonDocument.Parse(r.ReadToEnd());
        return d.RootElement.TryGetProperty("versao", out var v) ? v.GetString() ?? "?" : "?";
    }

    /// <summary>Versões do Civil 3D instaladas nesta máquina.</summary>
    public static List<string> CivilInstalado()
    {
        var r = new List<string>();
        foreach (var v in new[] { "2026", "2027" })
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", $"AutoCAD {v}", "C3D", "AeccDbMgd.dll"))) r.Add(v);
        return r;
    }

    public void Instalar(Componentes c, Destinos d)
    {
        using var s = Pacote() ?? throw new InvalidOperationException("Este instalador foi gerado sem o pacote (rode o build.ps1).");
        using var z = new ZipArchive(s, ZipArchiveMode.Read);
        var etapas = new List<(string, Action)>();
        if (c.HasFlag(Componentes.Civil)) etapas.Add(("Plugins do Civil 3D", () => Civil(z, d)));
        if (c.HasFlag(Componentes.Mcp)) etapas.Add(("Servidor MCP", () => Mcp(z, d, c.HasFlag(Componentes.Claude))));
        if (c.HasFlag(Componentes.PointTop)) etapas.Add(("POINT TOP", () => PointTop(z, d)));
        if (c.HasFlag(Componentes.Iri)) etapas.Add(("IRI Rodoviário", () => Iri(z, d)));
        if (c.HasFlag(Componentes.Lisp)) etapas.Add(("LISPs", () => Lisp(z, d)));
        if (etapas.Count == 0) throw new InvalidOperationException("Nenhum componente escolhido.");
        for (int i = 0; i < etapas.Count; i++)
        {
            _log($"== {etapas[i].Item1}");
            etapas[i].Item2();
            _progresso?.Invoke((i + 1) * 100 / (etapas.Count + 1));
        }
        _log("== Desinstalador");
        Desinstalador(d, c);
        _progresso?.Invoke(100);
        foreach (var a in Avisos) _log("AVISO: " + a);
        _log("Pronto.");
    }

    // ------------------------------------------------------------------ extração

    /// <summary>Extrai as entradas que começam com <paramref name="prefixo"/> para <paramref name="destino"/>; devolve quantos arquivos.</summary>
    private int Extrair(ZipArchive z, string prefixo, string destino, bool manterExistentes = false, Func<string, bool>? ultimo = null)
    {
        int n = 0;
        // o zip do PowerShell 5 usa "\" nos nomes; pasta vazia é uma entrada terminada em barra
        var entradas = z.Entries.Where(e => e.FullName.Replace('\\', '/').StartsWith(prefixo, StringComparison.OrdinalIgnoreCase)).ToList();
        // o que o Civil 3D "vê" primeiro (PackageContents.xml) vai por último
        foreach (var e in entradas.OrderBy(e => ultimo?.Invoke(e.Name) == true ? 1 : 0))
        {
            var nome = e.FullName.Replace('\\', '/');
            var rel = nome.Substring(prefixo.Length).TrimStart('/');
            if (rel.Length == 0) continue;
            var alvo = Path.GetFullPath(Path.Combine(destino, rel.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar)));
            if (!alvo.StartsWith(Path.GetFullPath(destino), StringComparison.OrdinalIgnoreCase)) continue; // nada fora da pasta
            if (nome.EndsWith('/')) { Directory.CreateDirectory(alvo); continue; }
            if (manterExistentes && File.Exists(alvo)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(alvo)!);
            Gravar(e, alvo);
            n++;
        }
        return n;
    }

    /// <summary>Grava o arquivo; se estiver em uso (Civil 3D aberto), renomeia o antigo e grava o novo no lugar.</summary>
    private void Gravar(ZipArchiveEntry e, string alvo)
    {
        try { e.ExtractToFile(alvo, true); }
        catch (IOException) when (File.Exists(alvo))
        {
            File.Move(alvo, alvo + ".antigo-" + _carimbo);
            e.ExtractToFile(alvo, true);
            if (!Avisos.Any(a => a.StartsWith("Arquivos em uso")))
                Avisos.Add("Arquivos em uso foram trocados com o programa aberto: a versão nova vale quando o Civil 3D (ou o Claude Desktop) for aberto de novo.");
        }
    }

    // ------------------------------------------------------------------ componentes

    private void Civil(ZipArchive z, Destinos d)
    {
        var civil = CivilInstalado();
        _log(civil.Count == 0 ? "Civil 3D 2026/2027 não encontrado nesta máquina; os plugins ficam instalados e carregam quando ele for instalado."
                              : $"Civil 3D encontrado: {string.Join(", ", civil)}");
        if (civil.Count == 0) Avisos.Add("Civil 3D 2026/2027 não encontrado: os plugins foram copiados, mas só funcionam com ele instalado.");
        var bundles = z.Entries.Select(e => e.FullName.Replace('\\', '/'))
                               .Where(f => f.StartsWith("civil3d/") && f.Count(ch => ch == '/') >= 2)
                               .Select(f => f.Split('/')[1]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var b in bundles)
        {
            string raiz = d.Plugins;
            // o IQ C3D já ficava em ProgramData nesta máquina: atualiza lá para não carregar duas vezes
            if (b.Equals("IQC3D.bundle", StringComparison.OrdinalIgnoreCase) && Directory.Exists(Path.Combine(d.PluginsTodos, b)))
            {
                if (Gravavel(Path.Combine(d.PluginsTodos, b))) raiz = d.PluginsTodos;
                else Avisos.Add($"Há um IQ C3D antigo em {Path.Combine(d.PluginsTodos, b)} que precisa de administrador para remover; ele pode carregar junto com o novo.");
            }
            var dest = Path.Combine(raiz, b);
            if (b.Equals("IQC3D.bundle", StringComparison.OrdinalIgnoreCase)) LimparIqc3dAntigo(dest);
            int n = Extrair(z, $"civil3d/{b}/", dest, ultimo: nome => nome.Equals("PackageContents.xml", StringComparison.OrdinalIgnoreCase));
            _instalados.Add(dest);
            _log($"  {b}: {n} arquivo(s) em {dest}");
        }
        var bib = Path.Combine(d.AppData, "Projeta", "biblioteca");
        int nb = Extrair(z, "biblioteca/", bib, manterExistentes: true);
        _log($"  biblioteca de dispositivos e seções: {nb} arquivo(s) novo(s) em {bib} (os seus não são substituídos)");
    }

    private static bool Gravavel(string pasta)
    {
        try { var t = Path.Combine(pasta, ".teste-" + Guid.NewGuid().ToString("N")); File.WriteAllText(t, ""); File.Delete(t); return true; }
        catch { return false; }
    }

    /// <summary>As versões antigas do IQ C3D tinham a data no nome da DLL; tira as que não estão em uso.</summary>
    private static void LimparIqc3dAntigo(string bundle)
    {
        var c = Path.Combine(bundle, "Contents");
        if (!Directory.Exists(c)) return;
        foreach (var f in Directory.GetFiles(c, "IQC3D_*.dll", SearchOption.AllDirectories))
            try { File.Delete(f); } catch { }
    }

    private void Mcp(ZipArchive z, Destinos d, bool claude)
    {
        var dest = Path.Combine(d.LocalAppData, "Projeta", "mcp", "servidor");
        int n = Extrair(z, "mcp/servidor/", dest);
        _instalados.Add(dest);
        _log($"  {n} arquivo(s) em {dest}");
        if (!claude) { _log("  (não registrado no Claude Desktop)"); return; }
        var exe = Path.Combine(dest, "Projeta.Mcp.Servidor.exe");
        var args = new List<string> { "--instalar-claude" };
        if (d.Teste) { args.Add("--config"); args.Add(Path.Combine(d.AppData, "Claude", "claude_desktop_config.json")); }
        var (code, saida) = Rodar(exe, args);
        _log("  Claude Desktop: " + saida.Trim().Replace(Environment.NewLine, " | "));
        if (code != 0) Avisos.Add("Não consegui registrar o servidor no Claude Desktop; rode de novo com o Claude fechado.");
        else _log("  Feche o Claude Desktop pela bandeja e abra de novo; no Civil 3D, rode PRJMCP → Iniciar.");
    }

    private void PointTop(ZipArchive z, Destinos d)
    {
        var dest = Path.Combine(d.Programas, "POINT TOP");
        int n = Extrair(z, "point-top/", dest);
        _instalados.Add(dest);
        var py = Path.Combine(dest, "python-portatil");
        var cfg = JsonSerializer.Serialize(new { executable = Path.Combine(py, "python.exe"), home = py }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(dest, "python-runtime.json"), cfg, new UTF8Encoding(false));
        var node = Path.Combine(dest, "node", "node.exe");
        File.WriteAllText(Path.Combine(dest, "ABRIR_POINT_TOP.cmd"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"POINT TOP\" /min \"%~dp0node\\node.exe\" server.cjs --open\r\n", Encoding.ASCII);
        _log($"  {n} arquivo(s) em {dest} (com Node e Python próprios)");
        Atalho(Path.Combine(d.MenuIniciar, "PROJETA", "POINT TOP.lnk"), node, "server.cjs --open", dest, "POINT TOP - IFM (abre no navegador)", 7);
        Atalho(Path.Combine(d.Desktop, "POINT TOP.lnk"), node, "server.cjs --open", dest, "POINT TOP - IFM (abre no navegador)", 7);
    }

    private void Iri(ZipArchive z, Destinos d)
    {
        var dest = Path.Combine(d.Programas, "IRI Rodoviario");
        int n = Extrair(z, "iri/", dest);
        _instalados.Add(dest);
        _log($"  {n} arquivo(s) em {dest}");
        var exe = Path.Combine(dest, "IRI_RODOVIARIO.exe");
        Atalho(Path.Combine(d.MenuIniciar, "PROJETA", "IRI Rodoviário.lnk"), exe, "", dest, "IRI Rodoviário", 1);
        Atalho(Path.Combine(d.Desktop, "IRI Rodoviário.lnk"), exe, "", dest, "IRI Rodoviário", 1);
    }

    private void Lisp(ZipArchive z, Destinos d)
    {
        var dest = Path.Combine(d.Programas, "LISP");
        int n = Extrair(z, "lisp/", dest);
        _instalados.Add(dest);
        _log($"  {n} arquivo(s) em {dest} — no AutoCAD/Civil 3D, carregue com APPLOAD (ou arraste o .lsp para o desenho)");
        Atalho(Path.Combine(d.MenuIniciar, "PROJETA", "LISPs (pasta).lnk"), dest, "", dest, "Pasta dos LISPs do PROJETA", 1);
    }

    // ------------------------------------------------------------------ atalhos, desinstalador

    private void Atalho(string lnk, string alvo, string args, string pasta, string descricao, int janela)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lnk)!);
            var tipo = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell indisponível");
            dynamic sh = Activator.CreateInstance(tipo)!;
            dynamic a = sh.CreateShortcut(lnk);
            a.TargetPath = alvo; a.Arguments = args; a.WorkingDirectory = pasta; a.Description = descricao; a.WindowStyle = janela;
            a.Save();
            _instalados.Add(lnk);
        }
        catch (Exception ex) { Avisos.Add($"Atalho não criado ({Path.GetFileName(lnk)}): {ex.Message}"); }
    }

    private void Desinstalador(Destinos d, Componentes c)
    {
        Directory.CreateDirectory(d.Programas);
        var lista = Path.Combine(d.Programas, "instalados.txt");
        var antigos = File.Exists(lista) ? File.ReadAllLines(lista) : Array.Empty<string>();
        var todos = antigos.Concat(_instalados).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        File.WriteAllLines(lista, todos, new UTF8Encoding(true));
        var mcpExe = Path.Combine(d.LocalAppData, "Projeta", "mcp", "servidor", "Projeta.Mcp.Servidor.exe");
        var ps = new StringBuilder();
        ps.AppendLine("# Desinstala o PROJETA: tira o servidor do Claude Desktop e apaga o que o instalador colocou (lista em instalados.txt).");
        ps.AppendLine("$ErrorActionPreference = 'Continue'");
        ps.AppendLine($"$mcp = '{mcpExe.Replace("'", "''")}'");
        if (!d.Teste) ps.AppendLine("if (Test-Path $mcp) { & $mcp --remover-claude | Out-Null }");
        ps.AppendLine("Get-Process Projeta.Mcp.Servidor -ErrorAction SilentlyContinue | Stop-Process -Force");
        ps.AppendLine("$lista = Join-Path $PSScriptRoot 'instalados.txt'");
        ps.AppendLine("foreach ($p in Get-Content $lista -Encoding UTF8) { if ($p -and (Test-Path -LiteralPath $p)) { Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction SilentlyContinue; if (Test-Path -LiteralPath $p) { \"Nao removido (em uso? feche o Civil 3D): $p\" } else { \"Removido: $p\" } } }");
        if (!d.Teste) ps.AppendLine("Remove-Item 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\PROJETA' -Recurse -ErrorAction SilentlyContinue");
        ps.AppendLine($"Remove-Item -LiteralPath '{Path.Combine(d.MenuIniciar, "PROJETA").Replace("'", "''")}' -Recurse -Force -ErrorAction SilentlyContinue");
        ps.AppendLine("Write-Host 'PROJETA desinstalado. A pasta de programas pode ser apagada.'; Start-Sleep 3");
        var ps1 = Path.Combine(d.Programas, "Desinstalar-PROJETA.ps1");
        File.WriteAllText(ps1, ps.ToString(), new UTF8Encoding(true));
        var cmd = Path.Combine(d.Programas, "Desinstalar PROJETA.cmd");
        File.WriteAllText(cmd, "@echo off\r\npowershell -NoProfile -ExecutionPolicy Bypass -File \"%~dp0Desinstalar-PROJETA.ps1\"\r\n", Encoding.ASCII);
        _log($"  lista do que foi instalado e desinstalador em {d.Programas}");
        if (d.Teste) return;
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PROJETA");
            k.SetValue("DisplayName", "PROJETA (plugins do Civil 3D, POINT TOP, IRI, LISPs)");
            k.SetValue("DisplayVersion", Versao());
            k.SetValue("Publisher", "PROJETA ENGENHARIA");
            k.SetValue("InstallLocation", d.Programas);
            k.SetValue("UninstallString", $"\"{cmd}\"");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            _log("  aparece em Configurações → Aplicativos (Desinstalar PROJETA)");
        }
        catch (Exception ex) { Avisos.Add("Não registrei o desinstalador no Windows: " + ex.Message); }
    }

    private static (int, string) Rodar(string exe, IEnumerable<string> args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = System.Diagnostics.Process.Start(psi)!;
        var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(60000);
        return (p.HasExited ? p.ExitCode : -1, o);
    }
}
