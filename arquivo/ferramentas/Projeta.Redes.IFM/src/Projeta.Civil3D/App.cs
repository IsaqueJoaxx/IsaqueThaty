using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Infra;

[assembly: ExtensionApplication(typeof(Projeta.Civil3D.App))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdRede))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdLancamento))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdCalculo))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdSaidas))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdImportar))]
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
                Ctx.Msg("PROJETA — IFM (drenagem, longitudinais, SWMM/EPANET, modelador) carregado. Digite PRJPAINEL ou PRJAJUDA.");
        }
        catch { }
    }

    public void Terminate() { }
}
