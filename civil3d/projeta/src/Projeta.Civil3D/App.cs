using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Infra;

[assembly: ExtensionApplication(typeof(Projeta.Civil3D.App))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdRede))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdLancamento))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdCalculo))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdSaidas))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdImportar))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdDevolverPipe))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdAlinhamento))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdGreide))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdNotaServico))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdVolumes))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdConformidade))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdPlato))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdCubacao))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdLongitudinal))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdSimulacao))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdModelador))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdGeral))]

namespace Projeta.Civil3D;

/// <summary>Ponto de entrada do plugin (carregado por NETLOAD ou pelo bundle).</summary>
public sealed class App : IExtensionApplication
{
    public void Initialize()
    {
        try
        {
            if (Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument is not null)
                Ctx.Msg("PROJETA • Assistente Civil (drenagem, longitudinais, SWMM/EPANET, modelador) carregado. Digite PRJPAINEL ou PRJAJUDA.");
        }
        catch { }
        // ponte com o Claude (MCP): liga sozinha se o usuário ativou PRJMCP → Automatico
        try { Mcp.PonteCivil.IniciarSeConfigurado(); }
        catch { }
    }

    public void Terminate()
    {
        try { Mcp.PonteCivil.Parar(); }
        catch { }
    }
}
