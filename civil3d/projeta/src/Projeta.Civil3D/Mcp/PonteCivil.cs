using System.Text.Json.Nodes;
using Autodesk.AutoCAD.ApplicationServices;
using Projeta.Mcp;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D.Mcp;

/// <summary>
/// Liga/desliga a ponte MCP dentro do Civil 3D. Com ela ativa, o servidor Projeta.Mcp.Servidor (iniciado pelo
/// Claude Desktop) consegue chamar as ferramentas registradas em <see cref="RegistroProjeta"/>.
/// Iniciar/Parar devem ser chamados na thread principal (comando PRJMCP, Initialize/Terminate do plugin).
/// </summary>
internal static class PonteCivil
{
    private static PonteServidor? _ponte;
    private static ExecutorCivil? _exec;
    private static RegistroFerramentas? _reg;
    private static volatile string _desenho = "";

    public static RegistroFerramentas Registro => _reg ??= RegistroProjeta.Criar();
    public static bool Ativa => _ponte?.Ativa == true;
    public static InfoPonte? Info => _ponte?.Info;

    public static void Iniciar()
    {
        if (Ativa) return;
        _exec ??= new ExecutorCivil();
        var p = new PonteServidor(Registro, _exec, NomeAplicativo(), VersaoPlugin(), () => new JsonObject { ["desenho"] = _desenho });
        p.Iniciar();
        _ponte = p;
        AtualizarDesenho();
        AcCore.DocumentManager.DocumentActivated += AoAtivarDocumento;
        AcCore.DocumentManager.DocumentDestroyed += AoFecharDocumento;
    }

    public static void Parar()
    {
        if (_ponte is null) return;
        try
        {
            AcCore.DocumentManager.DocumentActivated -= AoAtivarDocumento;
            AcCore.DocumentManager.DocumentDestroyed -= AoFecharDocumento;
        }
        catch { }
        _ponte.Dispose();
        _ponte = null;
    }

    /// <summary>Chamado no Initialize do plugin: liga a ponte no primeiro momento ocioso, se o usuário pediu início automático.</summary>
    public static void IniciarSeConfigurado()
    {
        if (!ConfigMcp.Ler().IniciarAutomatico) return;
        AcCore.Idle += AoOcioso;
    }

    private static void AoOcioso(object? sender, EventArgs e)
    {
        AcCore.Idle -= AoOcioso;
        try
        {
            Iniciar();
            Msg($"PROJETA MCP: ponte ativa (início automático) — {Registro.Quantidade} ferramentas disponíveis para o Claude. PRJMCP para detalhes.");
        }
        catch (Exception ex) { Msg("PROJETA MCP: não foi possível iniciar a ponte: " + ex.Message); }
    }

    private static void AoAtivarDocumento(object? sender, DocumentCollectionEventArgs e) => AtualizarDesenho();
    private static void AoFecharDocumento(object? sender, DocumentDestroyedEventArgs e) => AtualizarDesenho();

    private static void AtualizarDesenho()
    {
        try { _desenho = AcCore.DocumentManager.MdiActiveDocument?.Name ?? ""; }
        catch { _desenho = ""; }
    }

    private static void Msg(string s)
    {
        try { AcCore.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + s); } catch { }
    }

    public static string NomeAplicativo()
    {
        string v = "";
        try { v = AcCore.GetSystemVariable("ACADVER")?.ToString() ?? ""; } catch { }
        var ano = v.StartsWith("26.0") ? "2027" : v.StartsWith("25.1") ? "2026" : v.StartsWith("25.0") ? "2025" : null;
        return ano is null ? $"Civil 3D (AutoCAD {v.Trim()})" : $"Civil 3D {ano}";
    }

    public static string VersaoPlugin() => typeof(PonteCivil).Assembly.GetName().Version?.ToString(3) ?? "?";
}
