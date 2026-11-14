using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(Projeta.Civil3D.Commands))]
[assembly: CommandClass(typeof(Projeta.Civil3D.Commands))]

namespace Projeta.Civil3D;

public sealed class Commands : IExtensionApplication
{
#if NET10_0_OR_GREATER
    public const string Target = "Civil 3D 2027";
#else
    public const string Target = "Civil 3D 2026";
#endif

    private static PaletteSet? palette;
    private static SurfacesPage? surfacesPage;
    private static VolumesPage? volumesPage;
    private static SectionsPage? sectionsPage;
    private static CorridorsPage? corridorsPage;
    private static DrawingSnapshot? current;

    private static IEnumerable<ToolPage> Pages =>
        new ToolPage?[] { surfacesPage, volumesPage, sectionsPage, corridorsPage }.OfType<ToolPage>();

    public void Initialize()
    {
        // Ao carregar: só eventos. Nenhuma leitura do desenho, nenhuma alteração.
        Theme.Refresh();
        AcadApplication.DocumentManager.DocumentActivated += OnDocumentActivated;
        AcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        AcadApplication.SystemVariableChanged += OnSystemVariableChanged;
    }

    public void Terminate()
    {
        AcadApplication.DocumentManager.DocumentActivated -= OnDocumentActivated;
        AcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        AcadApplication.SystemVariableChanged -= OnSystemVariableChanged;
        palette?.Dispose();
        palette = null;
    }

    private static void Broadcast(DrawingSnapshot? snapshot, string? message = null)
    {
        current = snapshot;
        foreach (var page in Pages) page.ShowSnapshot(snapshot, message);
    }

    // A leitura vale só para o desenho de onde veio.
    private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
    {
        if (current is not null && e.Document is not null && !Actions.SameDrawing(e.Document, current.DrawingPath))
            Broadcast(null, "O desenho ativo mudou. Clique em Ler o desenho ativo.");
    }

    private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
    {
        if (current is not null && e.Document is not null && Actions.SameDrawing(e.Document, current.DrawingPath))
            Broadcast(null, "O desenho lido foi fechado. Clique em Ler o desenho ativo.");
    }

    private static void OnSystemVariableChanged(object sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
        if (!string.Equals(e.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase)) return;
        Theme.Refresh();
        main?.ApplyTheme();
    }

    private static void EnsurePalette()
    {
        if (palette is not null) return;
        surfacesPage = new SurfacesPage();
        volumesPage = new VolumesPage();
        sectionsPage = new SectionsPage();
        corridorsPage = new CorridorsPage();
        // O segundo argumento é o comando que o Civil 3D executa ao abrir, se a paleta ficou aberta.
        // Sem ele, o Civil 3D digitava o título como comando ("PROJETA" virava PROJETARGEOMETRIA).
        palette = new PaletteSet("PROJETA — ASSISTENTE CIVIL — IFM · " + Target, "PROJETAASSISTENTE", new Guid("96158a08-5909-4237-99c4-3589335228bc"))
        {
            Size = new Size(480, 700), MinimumSize = new Size(380, 480),
            DockEnabled = DockSides.Left | DockSides.Right,
            Style = PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowAutoHideButton | PaletteSetStyles.ShowPropertiesMenu
        };
        main = new MainPanel(("Superfícies", surfacesPage), ("Volumes", volumesPage), ("Seções", sectionsPage), ("Corredores", corridorsPage));
        palette.Add("PROJETA", main);
        main.ApplyTheme();
        if (current is not null) Broadcast(current);
    }

    private static MainPanel? main;

    private static void Show(ToolPage? page)
    {
        EnsurePalette();
        palette!.Visible = true;
        if (page is not null) main!.Select(page);
    }

    private static void Report(Document document, ToolPage? page, string text, bool ok)
    {
        page?.SetStatus(text, ok ? "good" : "warning");
        document.Editor.WriteMessage($"\nPROJETA: {text}\n");
    }

    [CommandMethod("PROJETAASSISTENTE", CommandFlags.Modal)]
    public static void OpenPanel() => Show(null);

    // Lê superfícies, eixos, greides e seções tipo do desenho ativo. Só leitura.
    [CommandMethod("PROJETASUPERFICIES", CommandFlags.Modal)]
    public static void ReadDrawing()
    {
        EnsurePalette();
        palette!.Visible = true;
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) { Broadcast(null, "Abra um desenho no Civil 3D."); return; }
        try
        {
            var snapshot = DrawingReader.Read(document);
            Broadcast(snapshot);
            document.Editor.WriteMessage($"\nPROJETA: leitura de {snapshot.Drawing} — {snapshot.Surfaces.Count} superfície(s), " +
                $"{snapshot.Alignments.Count} eixo(s), {snapshot.Assemblies.Count} seção(ões) tipo. Nada foi alterado.\n");
        }
        catch (System.Exception ex)
        {
            Broadcast(null, "Não foi possível ler o desenho: " + ex.Message);
            document.Editor.WriteMessage("\nPROJETA: leitura não concluída. " + ex.Message + "\n");
        }
    }

    [CommandMethod("PROJETAZOOM", CommandFlags.Modal | CommandFlags.Redraw)]
    public static void Zoom()
    {
        var handle = Pending.Take(ref Pending.ZoomHandle);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null || handle is null) { Show(surfacesPage); return; }
        try { Report(document, surfacesPage, ZoomOps.Run(document, handle), true); }
        catch (System.Exception ex) { Report(document, surfacesPage, "Não foi possível ir até a superfície: " + ex.Message, false); }
    }

    [CommandMethod("PROJETALIMITE", CommandFlags.Modal)]
    public static void PickLimit()
    {
        Show(volumesPage);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        var options = new PromptEntityOptions("\nPROJETA: selecione a polilinha fechada do limite: ");
        options.SetRejectMessage("\nSelecione uma polilinha (LWPOLYLINE).");
        options.AddAllowedClass(typeof(Polyline), true);
        var answer = document.Editor.GetEntity(options);
        if (answer.Status != PromptStatus.OK) { Report(document, volumesPage, "Seleção do limite cancelada.", false); return; }
        using var tr = document.Database.TransactionManager.StartOpenCloseTransaction();
        var pl = (Polyline)tr.GetObject(answer.ObjectId, OpenMode.ForRead);
        if (!Geometry.IsClosed(pl) || pl.Area <= 0)
        {
            Report(document, volumesPage, "A polilinha precisa ser fechada e ter área. Escolha outra.", false);
            return;
        }
        volumesPage!.SetLimit(answer.ObjectId.Handle.ToString(), pl.Area, document.Name);
        Report(document, volumesPage, $"Limite definido: polilinha {answer.ObjectId.Handle}. Agora clique em Calcular volumes.", true);
    }

    [CommandMethod("PROJETAVOLUMES", CommandFlags.Modal)]
    public static void Volumes()
    {
        var request = Pending.Take(ref Pending.Volume);
        Show(volumesPage);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { Report(document, volumesPage, "Use a aba Volumes do painel para calcular.", false); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, volumesPage, "O desenho ativo não é o que foi lido. Leia o desenho de novo.", false);
            return;
        }
        try
        {
            var result = VolumeOps.Run(document, request);
            volumesPage!.ShowResult(request, result);
            document.Editor.WriteMessage($"\nPROJETA: corte {Fmt.N(result.Cut)}, aterro {Fmt.N(result.Fill)}" +
                (result.CreatedName is null ? " (nada gravado).\n" : $"; criada a superfície \"{result.CreatedName}\".\n"));
            if (result.CreatedName is not null) Broadcast(DrawingReader.Read(document), $"Superfície \"{result.CreatedName}\" criada. Para desfazer: U.");
        }
        catch (System.Exception ex) { Report(document, volumesPage, "Cálculo não concluído: " + ex.Message, false); }
    }

    [CommandMethod("PROJETASECOES", CommandFlags.Modal)]
    public static void Sections()
    {
        var request = Pending.Take(ref Pending.Sections);
        Show(sectionsPage);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { Report(document, sectionsPage, "Use a aba Seções do painel.", false); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, sectionsPage, "O desenho ativo não é o que foi lido. Leia o desenho de novo.", false);
            return;
        }
        try { Report(document, sectionsPage, SectionOps.Run(document, request), true); }
        catch (System.Exception ex) { Report(document, sectionsPage, "Seções não criadas: " + ex.Message, false); }
    }

    [CommandMethod("PROJETACORREDORES", CommandFlags.Modal)]
    public static void Corridors()
    {
        var request = Pending.Take(ref Pending.Corridors);
        Show(corridorsPage);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { Report(document, corridorsPage, "Use a aba Corredores do painel.", false); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, corridorsPage, "O desenho ativo não é o que foi lido. Leia o desenho de novo.", false);
            return;
        }
        try { Report(document, corridorsPage, CorridorOps.Run(document, request), true); }
        catch (System.Exception ex) { Report(document, corridorsPage, "Corredores não criados: " + ex.Message, false); }
    }
}
