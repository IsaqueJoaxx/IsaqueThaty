using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Infra;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Projeta.Sinalizacao.TesteUI")]
[assembly: ExtensionApplication(typeof(Projeta.Sinalizacao.App))]
[assembly: CommandClass(typeof(Projeta.Sinalizacao.Comandos.CmdGeral))]
[assembly: CommandClass(typeof(Projeta.Sinalizacao.Comandos.CmdHorizontal))]
[assembly: CommandClass(typeof(Projeta.Sinalizacao.Comandos.CmdVertical))]
[assembly: CommandClass(typeof(Projeta.Sinalizacao.Comandos.CmdDispositivos))]
[assembly: CommandClass(typeof(Projeta.Sinalizacao.Comandos.CmdGestao))]
[assembly: CommandClass(typeof(Projeta.Sinalizacao.Comandos.CmdDemo))]

namespace Projeta.Sinalizacao;

/// <summary>Ponto de entrada do plugin (NETLOAD ou bundle).</summary>
public sealed class App : IExtensionApplication
{
    public void Initialize()
    {
        try
        {
            if (Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument is not null)
                Ctx.Msg("PROJETA • Sinalização Viária carregado. Digite SINPAINEL (painel) ou SINAJUDA (comandos).");
        }
        catch { }
    }

    public void Terminate() { }
}
