using System.Windows.Forms;

namespace Projeta.Civil3D;

// Aba Superfícies: lista com a função sugerida pelo nome e a ficha da superfície selecionada.
internal sealed class SurfacesPage : ToolPage
{
    private readonly ThemedListView list;
    private readonly ThemedListView sheet;
    private readonly Button zoom;

    public SurfacesPage() : base("Superfícies",
        "Lista as superfícies do desenho e sugere terreno natural (nome começando com 1) e projeto (com 2). " +
        "Selecione uma para ver a ficha; duplo clique leva até ela no desenho.")
    {
        list = new ThemedListView(new[] { "Superfície", "Função sugerida", "Tipo" }, new[] { 34, 44, 22 });
        list.SelectedIndexChanged += (_, _) => ShowSheet();
        list.DoubleClick += (_, _) => ZoomToSelected();
        AddWide(list, fill: true, minHeight: 120);

        sheet = new ThemedListView(new[] { "Ficha", "Valor" }, new[] { 42, 58 });
        AddWide(sheet, fill: true, minHeight: 120);

        zoom = Ui.Button("Ir até a superfície no desenho");
        zoom.Enabled = false;
        zoom.Click += (_, _) => ZoomToSelected();
        AddWide(zoom);
    }

    // Fichas já lidas, por handle. Valem enquanto a superfície não for reconstruída (mesma revisão).
    private readonly Dictionary<string, SurfaceInfo> details = new();

    private SurfaceInfo Detailed(SurfaceInfo s)
    {
        if (details.TryGetValue(s.Handle, out var known) && known.Revision == s.Revision) return known;
        var document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        if (document is null || Snapshot is null || !Actions.SameDrawing(document, Snapshot.DrawingPath)
            || document.CommandInProgress.Length != 0) return s;
        var cursor = Cursor.Current;
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            SurfaceInfo read;
            using (document.LockDocument()) read = DrawingReader.ReadDetails(document, s.Handle);
            return details[s.Handle] = read with { Revision = s.Revision };
        }
        catch (System.Exception ex)
        {
            SetStatus("Não foi possível ler a ficha da superfície: " + ex.Message, "warning");
            return s;
        }
        finally { Cursor.Current = cursor; }
    }

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        if (snapshot is null) details.Clear();
        // A atualização automática mantém a superfície que estava selecionada.
        var keep = Selected?.Handle;
        list.BeginUpdate();
        list.Items.Clear();
        if (snapshot is not null)
            foreach (var s in snapshot.Surfaces)
                list.Items.Add(new ListViewItem(new[] { s.Name, SurfaceNaming.Suggestion(s, snapshot.Surfaces), s.Type })
                    { Tag = s, ToolTipText = $"{s.Name}\nHandle {s.Handle}" });
        list.EndUpdate();
        var again = list.Items.Cast<ListViewItem>().FirstOrDefault(i => ((SurfaceInfo)i.Tag!).Handle == keep)
                    ?? (list.Items.Count > 0 ? list.Items[0] : null);
        if (again is not null) { again.Selected = true; again.EnsureVisible(); }
        ShowSheet();
    }

    private SurfaceInfo? Selected => list.SelectedItems.Count == 1 ? list.SelectedItems[0].Tag as SurfaceInfo : null;

    private void ShowSheet()
    {
        sheet.BeginUpdate();
        sheet.Items.Clear();
        var s = Selected;
        zoom.Enabled = s is not null;
        if (s is not null) s = Detailed(s);
        if (s is not null && Snapshot is not null)
        {
            var u = Snapshot.Unit;
            void Add(string name, string value) => sheet.Items.Add(new ListViewItem(new[] { name, value }) { ToolTipText = $"{name}: {value}" });
            Add("Tipo", s.Type);
            Add("Função sugerida", SurfaceNaming.Suggestion(s, Snapshot.Surfaces));
            Add("Pontos", Fmt.N(s.Points, 0));
            if (s.Triangles is not null) Add("Triângulos", Fmt.N(s.Triangles, 0));
            if (s.Area2D is not null) Add("Área 2D", Fmt.Area(s.Area2D, u));
            if (s.Area3D is not null) Add("Área 3D", Fmt.Area(s.Area3D, u));
            Add("Cota mínima", $"{Fmt.N(s.MinZ, 3)} {u}");
            Add("Cota média", $"{Fmt.N(s.MeanZ, 3)} {u}");
            Add("Cota máxima", $"{Fmt.N(s.MaxZ, 3)} {u}");
            Add("Extensão X × Y", $"{Fmt.N(s.SizeX, 1)} × {Fmt.N(s.SizeY, 1)} {u}");
            if (s.IsVolume)
            {
                Add("Base", s.BaseName ?? "—");
                Add("Comparação", s.ComparisonName ?? "—");
                Add("Corte", $"{Fmt.N(s.Cut)} {u}³");
                Add("Aterro", $"{Fmt.N(s.Fill)} {u}³");
                if (s.Cut is double cut && s.Fill is double fill) Add("Saldo", Fmt.Balance(cut - fill, u));
                Add("Fatores corte / aterro", $"{Fmt.N(s.CutFactor, 3)} / {Fmt.N(s.FillFactor, 3)}");
            }
            Add("Handle", s.Handle);
        }
        sheet.EndUpdate();
    }

    private void ZoomToSelected()
    {
        var s = Selected;
        if (s is null || Snapshot is null) return;
        Pending.ZoomHandle = s.Handle;
        if (Actions.Send("PROJETAZOOM", this)) SetStatus($"Indo até {s.Name}…");
    }
}
