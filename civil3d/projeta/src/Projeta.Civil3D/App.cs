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
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdGeo))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdMapas))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdRotatoria))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdVias))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdSinalizacao))]
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
                Ctx.Msg("PROJETA • Assistente Civil carregado: os comandos estão na aba PROJETA da faixa de opções (ou PRJPAINEL / PRJAJUDA).");
        }
        catch { }
        // ponte com o Claude (MCP): liga sozinha se o usuário ativou PRJMCP → Automatico
        try { Mcp.PonteCivil.IniciarSeConfigurado(); }
        catch { }
        // aba PROJETA na faixa de opções (no accoreconsole não há faixa: fica quieto)
        try { CriarAba(); }
        catch { }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void CriarAba() => UI.RibbonProjeta.Garantir();

    public void Terminate()
    {
        try { Mcp.PonteCivil.Parar(); }
        catch { }
    }
}
