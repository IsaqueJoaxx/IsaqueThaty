using Autodesk.AutoCAD.Runtime;
using Projeta.Giro.Infra;

[assembly: ExtensionApplication(typeof(Projeta.Giro.App))]
[assembly: CommandClass(typeof(Projeta.Giro.Comandos.CmdVarredura))]
[assembly: CommandClass(typeof(Projeta.Giro.Comandos.CmdVerificacao))]
[assembly: CommandClass(typeof(Projeta.Giro.Comandos.CmdGeral))]

namespace Projeta.Giro;

/// <summary>Ponto de entrada do plugin (NETLOAD ou bundle).</summary>
public sealed class App : IExtensionApplication
{
    public void Initialize()
    {
        try
        {
            if (Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument is not null)
                Ctx.Msg("PROJETA • Giro de Veículos carregado. Aba PROJETA Giro ou GIROAJUDA para a lista de comandos.");
        }
        catch { }
        if (Ctx.ComInterface)
        {
            try { UI.Faixa.Iniciar(); } catch (System.Exception ex) { try { Ctx.Msg("PROJETA Giro: não foi possível criar a aba (" + ex.Message + ")."); } catch { } }
        }
    }

    public void Terminate() { }
}
