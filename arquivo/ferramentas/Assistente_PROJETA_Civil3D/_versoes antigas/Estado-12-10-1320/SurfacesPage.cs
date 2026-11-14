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

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        list.BeginUpdate();
        list.Items.Clear();
        if (snapshot is not null)
            foreach (var s in snapshot.Surfaces)
                list.Items.Add(new ListViewItem(new[] { s.Name, SurfaceNaming.Suggestion(s, snapshot.Surfaces), s.Type })
                    { Tag = s, ToolTipText = $"{s.Name}\nHandle {s.Handle}" });
        list.EndUpdate();
        if (list.Items.Count > 0) list.Items[0].Selected = true;
        ShowSheet();
    }

    private SurfaceInfo? Selected => list.SelectedItems.Count == 1 ? list.SelectedItems[0].Tag as SurfaceInfo : null;

    private void ShowSheet()
    {
        sheet.BeginUpdate();
        sheet.Items.Clear();
        var s = Selected;
        zoom.Enabled = s is not null;
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
