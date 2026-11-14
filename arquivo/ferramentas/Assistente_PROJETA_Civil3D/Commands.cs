using System.Drawing;
using System.Text;
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
    private static MainPanel? main;
    private static SurfacesPage? surfacesPage;
    private static VolumesPage? volumesPage;
    private static SectionsPage? sectionsPage;
    private static CorridorsPage? corridorsPage;
    private static GradesPage? gradesPage;
    private static TraversesPage? traversesPage;
    private static NetworksPage? networksPage;
    private static IriPage? iriPage;
    private static CorrectionPage? correctionPage;
    internal static NetworksPage? NetworksPage => networksPage;
    private static DrawingSnapshot? current;
    private static string? currentSignature;
    private static bool refreshQueued;

    private static IEnumerable<ToolPage> Pages =>
        new ToolPage?[] { surfacesPage, volumesPage, sectionsPage, corridorsPage, gradesPage, traversesPage, networksPage, iriPage, correctionPage }.OfType<ToolPage>();

    public void Initialize()
    {
        // Ao carregar: só eventos e a aba do ribbon. Nenhuma leitura do desenho, nenhuma alteração.
        Theme.Refresh();
        var documents = AcadApplication.DocumentManager;
        documents.DocumentActivated += OnDocumentActivated;
        documents.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        documents.DocumentCreated += OnDocumentCreated;
        foreach (Document document in documents) Watch(document);
        AcadApplication.SystemVariableChanged += OnSystemVariableChanged;
        try { ProjetaRibbon.Install(); }
        catch (System.Exception ex) { documents.MdiActiveDocument?.Editor.WriteMessage($"\nPROJETA: aba do ribbon não criada ({ex.Message}).\n"); }
    }

    public void Terminate()
    {
        var documents = AcadApplication.DocumentManager;
        documents.DocumentActivated -= OnDocumentActivated;
        documents.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        documents.DocumentCreated -= OnDocumentCreated;
        AcadApplication.SystemVariableChanged -= OnSystemVariableChanged;
        palette?.Dispose();
        palette = null;
    }

    // ---------------------------------------------------------------- atualização automática
    // Depois de cada comando, e ao trocar de desenho, o painel relê o desenho ativo no próximo
    // momento ocioso. A leitura é leve (nomes, tipos, eixos, greides, seções tipo) e só chega às
    // abas se algo mudou, para não apagar escolhas e resultados à toa.

    private static void Watch(Document? document)
    {
        if (document is null) return;
        document.CommandEnded += OnCommandFinished;
        document.CommandCancelled += OnCommandFinished;
        document.CommandFailed += OnCommandFinished;
    }

    private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e) => Watch(e.Document);

    // Depois de U/Desfazer/Refazer a releitura chega às abas mesmo sem mudança nas listas, para
    // não ficar na tela um "criado" que o U já desfez (corredores não entram na assinatura).
    private static bool undoSeen;

    private static void OnCommandFinished(object sender, CommandEventArgs e)
    {
        var name = e.GlobalCommandName?.ToUpperInvariant();
        if (name is "U" or "UNDO" or "REDO" or "MREDO") undoSeen = true;
        QueueRefresh();
    }

    private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e) => QueueRefresh();

    private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
    {
        if (current is not null && e.Document is not null && Actions.SameDrawing(e.Document, current.DrawingPath))
            Broadcast(null, "O desenho lido foi fechado.");
        QueueRefresh();
    }

    public static void QueueRefresh()
    {
        if (refreshQueued || palette is null || !palette.Visible) return;
        refreshQueued = true;
        AcadApplication.Idle += OnIdle;
    }

    private static void OnIdle(object? sender, EventArgs e)
    {
        AcadApplication.Idle -= OnIdle;
        refreshQueued = false;
        var afterUndo = undoSeen;
        undoSeen = false;
        Refresh(force: false, afterUndo: afterUndo);
    }

    // force: lê e mostra mesmo sem mudança (botão Atualizar e comando PROJETASUPERFICIES).
    // afterUndo: mostra mesmo sem mudança, mas respeita comando em andamento e paleta fechada.
    public static void Refresh(bool force, bool afterUndo = false)
    {
        if (!force && (palette is null || !palette.Visible)) return;
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
        {
            currentSignature = null;
            Broadcast(null, "Abra um desenho no Civil 3D.");
            return;
        }
        // Com um comando em andamento a leitura espera: o fim do comando chama de novo.
        if (!force && document.CommandInProgress.Length != 0) { if (afterUndo) undoSeen = true; return; }
        try
        {
            DrawingSnapshot snapshot;
            using (document.LockDocument()) snapshot = DrawingReader.Read(document);
            var signature = Signature(snapshot);
            if (!force && !afterUndo && signature == currentSignature) return;
            currentSignature = signature;
            Broadcast(snapshot, afterUndo && !force
                ? $"{snapshot.Drawing} · relido às {snapshot.CapturedAt:HH:mm:ss}, depois de desfazer/refazer."
                : null);
        }
        catch (System.Exception ex)
        {
            foreach (var page in Pages) page.SetStatus("Não foi possível ler o desenho: " + ex.Message, "warning");
        }
    }

    private static string Signature(DrawingSnapshot s)
    {
        var b = new StringBuilder(s.DrawingPath).Append('\n');
        foreach (var x in s.Surfaces) b.Append(x.Name).Append('|').Append(x.Type).Append('|').Append(x.Handle).Append('|').Append(x.Revision).Append('\n');
        foreach (var a in s.Alignments)
        {
            b.Append(a.Name).Append('|').Append(a.Handle).Append('|').Append(a.Start).Append('|').Append(a.End).Append('\n');
            foreach (var p in a.Profiles) b.Append(' ').Append(p.Name).Append('|').Append(p.Handle).Append('|').Append(p.Kind).Append('|').Append(p.Start).Append('|').Append(p.End).Append('\n');
        }
        foreach (var a in s.Assemblies)
            b.Append(a.Name).Append('|').Append(a.Handle).Append('|').Append(string.Join(",", a.MissingPkt))
             .Append('|').Append(string.Join(",", a.OutdatedPkt)).Append('\n');
        foreach (var g in s.SampleLineGroups)
            b.Append(g.Name).Append('|').Append(g.Handle).Append('|').Append(g.Lines).Append('|').Append(g.Start).Append('|').Append(g.End)
             .Append('|').Append(string.Join(",", g.Sources.Select(x => x.Key + "=" + x.Name))).Append('\n');
        return b.ToString();
    }

    private static void Broadcast(DrawingSnapshot? snapshot, string? message = null)
    {
        current = snapshot;
        if (snapshot is null) currentSignature = null;
        foreach (var page in Pages) page.ShowSnapshot(snapshot, message);
    }

    private static void OnSystemVariableChanged(object sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
        if (string.Equals(e.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase))
        {
            Theme.Refresh();
            main?.ApplyTheme();
        }
        // Trocar de espaço de trabalho recarrega o ribbon e some com abas criadas por código.
        else if (string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase))
        {
            try { ProjetaRibbon.Install(); } catch { }
        }
    }

    // ---------------------------------------------------------------- paleta

    private static void EnsurePalette()
    {
        if (palette is not null) return;
        surfacesPage = new SurfacesPage();
        volumesPage = new VolumesPage();
        sectionsPage = new SectionsPage();
        corridorsPage = new CorridorsPage();
        gradesPage = new GradesPage();
        traversesPage = new TraversesPage();
        networksPage = new NetworksPage();
        iriPage = new IriPage();
        correctionPage = new CorrectionPage();
        // O segundo argumento é o comando que o Civil 3D executa ao abrir, se a paleta ficou aberta.
        // Sem ele, o Civil 3D digitava o título como comando ("PROJETA" virava PROJETARGEOMETRIA).
        palette = new PaletteSet("PROJETA — ASSISTENTE CIVIL — IFM · " + Target, "PROJETAASSISTENTE", new Guid("96158a08-5909-4237-99c4-3589335228bc"))
        {
            Size = new Size(480, 700), MinimumSize = new Size(380, 480),
            DockEnabled = DockSides.Left | DockSides.Right,
            Style = PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowAutoHideButton | PaletteSetStyles.ShowPropertiesMenu
        };
        main = new MainPanel(("Superfícies", surfacesPage), ("Volumes", volumesPage), ("Seções", sectionsPage), ("Corredores", corridorsPage), ("Greide — IFM", gradesPage), ("Poligonal — IFM", traversesPage), ("Redes — IFM", networksPage), ("IRI RODOVIÁRIO — IFM", iriPage), ("CORREÇÃO DO GREIDE — IFM", correctionPage));
        palette.Add("PROJETA", main);
        // Ao reaparecer (fechada e aberta de novo, ou auto-ocultar), lê o desenho outra vez.
        palette.StateChanged += (_, e) => { if (e.NewState == StateEventIndex.Show) QueueRefresh(); };
        main.ApplyTheme();
        if (current is not null) Broadcast(current);
    }

    // Abre a paleta numa aba. Usado pelos comandos e pelos botões do ribbon (contexto da aplicação).
    public static void ShowPage(string key)
    {
        Show(key);
        QueueRefresh();
    }

    // A aba é escolhida pela chave, depois de criar a paleta: na primeira vez as abas ainda não existem
    // quando o comando começa (antes, PROJETAVOLUMES digitado com a paleta nunca aberta caía em Superfícies).
    private static void Show(string? key)
    {
        EnsurePalette();
        palette!.Visible = true;
        ToolPage? page = key switch
        {
            null => null, "volumes" => volumesPage, "secoes" => sectionsPage, "corredores" => corridorsPage, "greides" => gradesPage, "poligonal" => traversesPage, "redes" => networksPage, "iri" => iriPage, "correcao" => correctionPage, _ => surfacesPage
        };
        if (page is not null) main!.Select(page);
    }

    private static void Report(Document document, ToolPage? page, string text, bool ok)
    {
        page?.ShowOutcome(text, ok);
        document.Editor.WriteMessage($"\nPROJETA: {text}\n");
    }

    [CommandMethod("PROJETAPOLIGONAL", CommandFlags.Modal)]
    public static void OpenTraverse() => ShowPage("poligonal");
    [CommandMethod("PROJETAIRI", CommandFlags.Modal)]
    public static void OpenIri() => ShowPage("iri");
    [CommandMethod("PROJETACORRECAO", CommandFlags.Modal)]
    public static void OpenCorrection() => ShowPage("correcao");

    // ---------------------------------------------------------------- comandos

    [CommandMethod("PROJETATABVOL", CommandFlags.Modal)]
    public static void VolumeTable()
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        var folder = System.IO.Path.GetDirectoryName(typeof(Commands).Assembly.Location)!;
        var path = System.IO.Path.Combine(folder, "TABVOL.lsp");
        if (!System.IO.File.Exists(path))
        {
            document.Editor.WriteMessage("\nPROJETA: TABVOL.lsp ausente. Reinstale o pacote completo.\n");
            return;
        }
        // Load only the packaged routine, without interpolating user input.
        var literal = path.Replace('\\', '/').Replace("\"", "\\\"");
        // LOAD may return nil on success because the routine ends with (princ).
        // A genuine load error stops PROGN before TABVOL is called.
        document.SendStringToExecute("(progn (load \"" + literal + "\") (c:TABVOL))\n", true, false, false);
    }

    [CommandMethod("PROJETAGREIDE", CommandFlags.Modal)]
    public static void Grade()
    {
        var request = Pending.Take(ref Pending.Grade);
        Show("greides");
        if (request is null) return;
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) { gradesPage!.ShowOutcome("Abra um desenho no Civil 3D.", false); return; }
        try { gradesPage!.Display(request, GradeReader.Read(document, request)); }
        catch (System.Exception ex) { gradesPage!.ShowOutcome("Prévia não concluída: " + ex.Message, false); }
    }
    [CommandMethod("PROJETACRIARGREIDE", CommandFlags.Modal)]
    public static void CreateGrade()
    {
        var request = Pending.Take(ref Pending.GradeCreate);
        Show("greides");
        if (request is null) { gradesPage!.ShowOutcome("Use Criar greide no desenho após revisar a prévia.", false); return; }
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        try
        {
            var message = GradeCreator.Create(document, request);
            Refresh(force: true);
            Report(document, gradesPage, message, true);
        }
        catch (System.Exception ex) { Report(document, gradesPage, "Greide não criado: " + ex.Message, false); }
    }

    [CommandMethod("PROJETAASSISTENTE", CommandFlags.Modal)]
    public static void OpenPanel() => ShowPage("superficies");

    // Lê superfícies, eixos, greides e seções tipo do desenho ativo. Só leitura.
    [CommandMethod("PROJETASUPERFICIES", CommandFlags.Modal)]
    public static void ReadDrawing()
    {
        Show(null);
        Refresh(force: true);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is not null && current is not null)
            document.Editor.WriteMessage($"\nPROJETA: leitura de {current.Drawing} — {current.Surfaces.Count} superfície(s), " +
                $"{current.Alignments.Count} eixo(s), {current.Assemblies.Count} seção(ões) tipo. Nada foi alterado.\n");
    }

    [CommandMethod("PROJETAZOOM", CommandFlags.Modal | CommandFlags.Redraw)]
    public static void Zoom()
    {
        var handle = Pending.Take(ref Pending.ZoomHandle);
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null || handle is null) { Show("superficies"); return; }
        try { Report(document, surfacesPage, ZoomOps.Run(document, handle), true); }
        catch (System.Exception ex) { Report(document, surfacesPage, "Não foi possível ir até a superfície: " + ex.Message, false); }
    }

    [CommandMethod("PROJETALIMITE", CommandFlags.Modal)]
    public static void PickLimit()
    {
        Show("volumes");
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        // Seleção com filtro: o clique só enxerga polilinhas, então a superfície por baixo não atrapalha.
        var options = new PromptSelectionOptions
        {
            MessageForAdding = "\nPROJETA: clique na polilinha fechada do limite: ",
            SingleOnly = true, SinglePickInSpace = true
        };
        var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") });
        var answer = document.Editor.GetSelection(options, filter);
        if (answer.Status != PromptStatus.OK || answer.Value.Count == 0)
        {
            Report(document, volumesPage, "Seleção do limite cancelada.", false);
            return;
        }
        var id = answer.Value[0].ObjectId;
        using var tr = document.Database.TransactionManager.StartOpenCloseTransaction();
        var pl = (Polyline)tr.GetObject(id, OpenMode.ForRead);
        if (!Geometry.IsClosed(pl) || pl.Area <= 0)
        {
            Report(document, volumesPage, "A polilinha precisa ser fechada e ter área. Escolha outra.", false);
            return;
        }
        volumesPage!.SetLimit(id.Handle.ToString(), pl.Area, document.Name);
        Report(document, volumesPage, $"Limite definido: polilinha {id.Handle}. Agora clique em Calcular volumes.", true);
    }

    [CommandMethod("PROJETAVOLUMES", CommandFlags.Modal)]
    public static void Volumes()
    {
        var request = Pending.Take(ref Pending.Volume);
        Show("volumes");
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { volumesPage!.SetStatus("Escolha as superfícies e clique em Calcular volumes."); QueueRefresh(); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, volumesPage, "O desenho ativo mudou. Confira as superfícies e calcule de novo.", false);
            return;
        }
        try
        {
            var result = VolumeOps.Run(document, request);
            // A superfície nova entra nas listas antes de o resultado aparecer, para a mensagem final ser a do resultado.
            if (result.CreatedName is not null) Refresh(force: true);
            volumesPage!.ShowResult(request, result);
            document.Editor.WriteMessage($"\nPROJETA: corte {Fmt.N(result.Cut)}, aterro {Fmt.N(result.Fill)}" +
                (result.CreatedName is null ? " (nada gravado).\n" : $"; criada a superfície \"{result.CreatedName}\".\n"));
        }
        catch (System.Exception ex) { Report(document, volumesPage, "Cálculo não concluído: " + ex.Message, false); }
    }

    // Volume pelas seções de um grupo de linhas de amostragem (áreas médias). Só leitura.
    [CommandMethod("PROJETAVOLSECOES", CommandFlags.Modal)]
    public static void SectionVolumes()
    {
        var request = Pending.Take(ref Pending.SectionVolume);
        Show("volumes");
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null)
        {
            volumesPage!.SetStatus("Em \"Calcular por\", escolha Seções do Civil 3D, depois o grupo, e clique em Calcular volumes.");
            QueueRefresh();
            return;
        }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, volumesPage, "O desenho ativo mudou. Confira o grupo de seções e calcule de novo.", false);
            return;
        }
        try
        {
            var result = SectionVolumeOps.Run(document, request);
            volumesPage!.ShowSectionResult(request, result);
            document.Editor.WriteMessage($"\nPROJETA: seções de {result.GroupName} ({result.Rows.Count} linhas): corte {Fmt.N(result.Cut)}, " +
                $"aterro {Fmt.N(result.Fill)} pelo método das áreas médias (nada gravado).\n");
        }
        catch (System.Exception ex) { Report(document, volumesPage, "Cálculo não concluído: " + ex.Message, false); }
    }

    [CommandMethod("PROJETASECOES", CommandFlags.Modal)]
    public static void Sections()
    {
        var request = Pending.Take(ref Pending.Sections);
        Show("secoes");
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { sectionsPage!.SetStatus("Escolha o eixo e as superfícies e clique em Criar seções."); QueueRefresh(); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, sectionsPage, "O desenho ativo mudou. Confira os campos e tente de novo.", false);
            return;
        }
        try { Report(document, sectionsPage, SectionOps.Run(document, request), true); }
        catch (System.Exception ex) { Report(document, sectionsPage, "Seções não criadas: " + ex.Message, false); }
    }

    // Aponta as submontagens do Subassembly Composer não encontradas para os PKT da pasta da biblioteca.
    [CommandMethod("PROJETAPKT", CommandFlags.Modal)]
    public static void FixPkt()
    {
        var request = Pending.Take(ref Pending.Pkt);
        Show("corredores");
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { corridorsPage!.SetStatus("Use \"Corrigir PKT ausentes\" na aba Corredores."); QueueRefresh(); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, corridorsPage, "O desenho ativo mudou. Confira e tente de novo.", false);
            return;
        }
        try
        {
            var text = PktOps.Run(document, request, out var ok);
            // Relê antes de mostrar o resultado, para a releitura automática não cobrir a mensagem.
            Refresh(force: true);
            Report(document, corridorsPage, text, ok);
        }
        catch (System.Exception ex) { Report(document, corridorsPage, "PKT não corrigidos: " + ex.Message, false); }
    }

    [CommandMethod("PROJETACORREDORES", CommandFlags.Modal)]
    public static void Corridors()
    {
        var request = Pending.Take(ref Pending.Corridors);
        Show("corredores");
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        if (request is null) { corridorsPage!.SetStatus("Marque os eixos, escolha a seção tipo e clique em Criar corredores."); QueueRefresh(); return; }
        if (!Actions.SameDrawing(document, request.DrawingPath))
        {
            Report(document, corridorsPage, "O desenho ativo mudou. Confira os eixos e tente de novo.", false);
            return;
        }
        try { Report(document, corridorsPage, CorridorOps.Run(document, request), true); }
        catch (System.Exception ex) { Report(document, corridorsPage, "Corredores não criados: " + ex.Message, false); }
    }
}



