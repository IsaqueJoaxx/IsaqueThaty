using System.Windows.Forms;

namespace Projeta.Civil3D;

// Aba Seções: grupo de linhas de amostragem num eixo, por intervalo e largura, com as superfícies
// escolhidas amostradas e, se pedido, as vistas de seção.
internal sealed class SectionsPage : ToolPage
{
    private const int MaxLines = 1500;
    private readonly ComboBox alignment;
    private readonly NumericUpDown start;
    private readonly NumericUpDown end;
    private readonly NumericUpDown interval;
    private readonly NumericUpDown left;
    private readonly NumericUpDown right;
    private readonly CheckedListBox surfaces;
    private readonly CheckBox views;
    private readonly Label preview;

    public SectionsPage() : base("Seções",
        "Cria linhas de amostragem ao longo de um eixo, amostra as superfícies marcadas e, se quiser, " +
        "desenha as vistas de seção a partir de um ponto que você clica.")
    {
        alignment = Ui.Combo();
        alignment.SelectedIndexChanged += (_, _) => LoadAlignmentRange();
        AddRow("Eixo", alignment);
        start = Ui.Number(0m, -1_000_000m, 10_000_000m, 2, 20m);
        end = Ui.Number(0m, -1_000_000m, 10_000_000m, 2, 20m);
        interval = Ui.Number(20m, 1m, 1000m, 2, 5m);
        left = Ui.Number(20m, 0.5m, 1000m, 2, 1m);
        right = Ui.Number(20m, 0.5m, 1000m, 2, 1m);
        AddRow("Estaca inicial (m)", start);
        AddRow("Estaca final (m)", end);
        AddRow("Intervalo (m)", interval);
        AddRow("Largura à esquerda (m)", left);
        AddRow("Largura à direita (m)", right);

        AddWide(Ui.Label("Superfícies amostradas"));
        surfaces = new CheckedListBox { CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false };
        AddWide(surfaces, fill: true, minHeight: 70);

        views = new CheckBox { Text = "Desenhar as vistas de seção (pede um ponto)", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 0, 6) };
        AddWide(views);

        var showPreview = Ui.Button("Prévia");
        showPreview.Click += (_, _) => Preview(out _);
        var create = Ui.Button("Criar seções…", primary: true);
        create.Click += (_, _) => Create();
        AddWide(Ui.Row(showPreview, create));
        preview = Wrap("", null);
        AddWide(preview);

        foreach (var input in new[] { start, end, interval, left, right }) input.ValueChanged += (_, _) => Preview(out _);
        // ItemCheck chega antes da marcação mudar; a prévia é refeita logo depois.
        surfaces.ItemCheck += (_, _) => { if (IsHandleCreated) BeginInvoke(new Action(() => Preview(out _))); };
        views.CheckedChanged += (_, _) => Preview(out _);
    }

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        Ui.SetItems(alignment, snapshot?.Alignments.Select(a => new Choice(a.Name, a.Handle)) ?? Array.Empty<Choice>());
        if (alignment.SelectedIndex < 0 && alignment.Items.Count > 0) alignment.SelectedIndex = 0;

        var checkedHandles = surfaces.CheckedItems.OfType<Choice>().Select(c => c.Handle).ToHashSet();
        surfaces.Items.Clear();
        if (snapshot is not null)
            foreach (var s in snapshot.Surfaces.Where(s => !s.IsVolume))
            {
                var role = SurfaceNaming.Role(s.Name);
                var mark = checkedHandles.Count > 0 ? checkedHandles.Contains(s.Handle)
                    : role is SurfaceNaming.Ground or SurfaceNaming.Design;
                surfaces.Items.Add(new Choice(s.Name, s.Handle), mark);
            }
        LoadAlignmentRange();
    }

    private AlignmentInfo? SelectedAlignment =>
        alignment.SelectedItem is Choice c ? Snapshot?.Alignments.FirstOrDefault(a => a.Handle == c.Handle) : null;

    // A faixa de estacas só volta ao eixo inteiro quando o eixo escolhido muda.
    private void LoadAlignmentRange()
    {
        var a = SelectedAlignment;
        if (a is not null && a.Handle != rangeOf)
        {
            rangeOf = a.Handle;
            start.Value = Clamp(start, a.Start);
            end.Value = Clamp(end, a.End);
        }
        Preview(out _);
    }

    private string? rangeOf;

    private static decimal Clamp(NumericUpDown n, double v) => Math.Min(n.Maximum, Math.Max(n.Minimum, (decimal)Math.Round(v, 2)));

    // Mesma regra do comando: início, múltiplos do intervalo e fim.
    public static List<double> Stations(double from, double to, double step)
    {
        var list = new List<double> { from };
        for (var k = Math.Floor(from / step) + 1; k * step < to - 1e-6; k++) list.Add(k * step);
        if (to - from > 1e-6) list.Add(to);
        return list;
    }

    private SectionRequest? Preview(out string? problem)
    {
        problem = null;
        var a = SelectedAlignment;
        var chosen = surfaces.CheckedItems.OfType<Choice>().ToList();
        if (Snapshot is null) problem = "Abra um desenho no Civil 3D.";
        else if (a is null) problem = "O desenho não tem eixos (alinhamentos).";
        else if (end.Value <= start.Value) problem = "A estaca final precisa ser maior que a inicial.";
        else if (chosen.Count == 0) problem = "Marque pelo menos uma superfície para amostrar.";
        if (problem is not null || a is null || Snapshot is null) { preview.Text = problem ?? ""; return null; }

        var from = Math.Max((double)start.Value, a.Start);
        var to = Math.Min((double)end.Value, a.End);
        var count = Stations(from, to, (double)interval.Value).Count;
        if (count > MaxLines)
        {
            problem = $"Seriam {count} linhas de amostragem; o limite é {MaxLines}. Aumente o intervalo.";
            preview.Text = problem;
            return null;
        }
        preview.Text = $"Vai criar o grupo \"PROJETA SL\" no eixo {a.Name} com {count} linhas de amostragem, " +
                       $"de {Fmt.N(from)} a {Fmt.N(to)} m a cada {Fmt.N((double)interval.Value)} m, " +
                       $"{Fmt.N((double)left.Value)} m à esquerda e {Fmt.N((double)right.Value)} m à direita, " +
                       $"amostrando {string.Join(", ", chosen.Select(c => c.Text))}" +
                       (views.Checked ? $", e {count} vistas de seção a partir do ponto que você clicar." : ".");
        return new SectionRequest(Snapshot.DrawingPath, a.Handle, from, to, (double)interval.Value,
            (double)left.Value, (double)right.Value, chosen.Select(c => c.Handle).ToArray(), views.Checked);
    }

    private void Create()
    {
        var request = Preview(out var problem);
        if (request is null) { SetStatus(problem ?? "Revise os campos.", "warning"); return; }
        if (!Ui.Confirm(this, "Criar seções", preview.Text)) return;
        Pending.Sections = request;
        if (Actions.Send("PROJETASECOES", this))
            SetStatus(request.Views ? "Clique no desenho o ponto de inserção das vistas de seção." : "Criando as linhas de amostragem…");
    }

    public void ShowResult(string text, bool ok) => SetStatus(text, ok ? "good" : "warning");
}
