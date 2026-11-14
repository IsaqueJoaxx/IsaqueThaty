using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(Projeta.Civil3D.Commands))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Commands))]

namespace Projeta.Civil3D;

public sealed class Commands : IExtensionApplication
{
    private static PaletteSet? palette;
    private static AssistantPanel? panel;

    public void Initialize()
    {
        // No drawing access, automatic reads or installation changes during load.
        AcadApplication.DocumentManager.DocumentActivated += OnDocumentActivated;
        AcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
    }

    public void Terminate()
    {
        AcadApplication.DocumentManager.DocumentActivated -= OnDocumentActivated;
        AcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        palette?.Dispose();
        palette = null;
        panel = null;
    }

    private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e) =>
        panel?.InvalidateSnapshot("Desenho ativo alterado. Consulte as superfícies novamente.");

    private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e) =>
        panel?.InvalidateSnapshot("Um desenho foi fechado. Consulte novamente no desenho ativo.");

    [CommandMethod("PROJETAASSISTENTE", CommandFlags.Modal)]
    public static void OpenPanel()
    {
        if (palette is null)
        {
            panel = new AssistantPanel();
            palette = new PaletteSet("PROJETA — ASSISTENTE CIVIL — IFM", new Guid("96158a08-5909-4237-99c4-3589335228bc"))
            {
                Size = new Size(480, 650), MinimumSize = new Size(380, 480),
                DockEnabled = DockSides.Left | DockSides.Right,
                Style = PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowAutoHideButton | PaletteSetStyles.ShowPropertiesMenu
            };
            palette.Add("Superfícies", panel);
        }
        palette.Visible = true;
    }

    [CommandMethod("PROJETASUPERFICIES", CommandFlags.Modal)]
    public static void ReadSurfaces()
    {
        OpenPanel();
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) { panel!.InvalidateSnapshot("Abra um desenho no Civil 3D."); return; }
        try
        {
            var snapshot = SurfaceReader.Read(document);
            panel!.Display(snapshot);
            document.Editor.WriteMessage($"\nPROJETA: {snapshot.Rows.Count} superfície(s) consultada(s), somente leitura.\n");
        }
        catch (System.Exception exception)
        {
            panel!.InvalidateSnapshot("Não foi possível consultar as superfícies. Verifique se está no Civil 3D 2027.\n" + exception.Message);
            document.Editor.WriteMessage("\nPROJETA: consulta não concluída. " + exception.Message + "\n");
        }
    }
}

