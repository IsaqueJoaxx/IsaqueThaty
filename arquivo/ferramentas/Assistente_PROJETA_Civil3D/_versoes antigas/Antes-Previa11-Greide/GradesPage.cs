using System.Windows.Forms;

namespace Projeta.Civil3D;

internal sealed class GradesPage : ToolPage
{
    private readonly ComboBox alignment = Ui.Combo();
    private readonly ComboBox terrain = Ui.Combo();
    private readonly ComboBox profileStyle = Ui.Combo();
    private readonly ComboBox labelSet = Ui.Combo();
    private readonly TextBox name = new() { Text = "2 — GREIDE IFM", Dock = DockStyle.Fill };
    private readonly Button create = Ui.Button("Criar greide no desenho…");
    private GradeRequest? reviewedInput;
    private GradePreview? reviewedPreview;
    private readonly NumericUpDown maximum = Ui.Number(7m, 0.01m, 7m, 2, 0.1m);
    private readonly NumericUpDown minimumLength = Ui.Number(40m, 1m, 10000m, 2, 1m);
    private readonly NumericUpDown tolerance = Ui.Number(0.20m, 0m, 1000m, 3, 0.05m);
    private readonly NumericUpDown step = Ui.Number(10m, 0.1m, 1000m, 2, 1m);
    private readonly ThemedListView result = new(new[] { "Progressiva (m)", "Cota (m)", "Rampa de saída (%)" }, new[] { 34, 33, 33 });

    public GradesPage() : base("GREIDE AUTOMÁTICO — IFM",
        "Revise a prévia e crie o perfil com tangentes e PIVs simplificados. Rampa até 7%; curvas verticais serão inseridas manualmente por você.")
    {
        AddRow("Eixo", alignment);
        AddRow("Perfil do terreno", terrain);
        AddRow("Rampa máxima (%)", maximum);
        AddRow("Distância mín. PIVs (m)", minimumLength);
        AddRow("Tolerância nas amostras (m)", tolerance);
        AddWide(Wrap("A mesma tolerância limita a mudança ao retirar PIVs. O simplificador mantém os extremos e não ultrapassa a rampa máxima; não busca o menor número global de PIVs.", "muted"));
        AddRow("Passo de leitura (m)", step);
        AddWide(Wrap("40 m, 0,20 m e 10 m vêm da rotina enviada e são editáveis; não estão certificados como critérios do DER-MG. Cota inicial = terreno. Sem cota final fixada.", "warning"));
        var preview = Ui.Button("Gerar prévia de tangentes", true);
        preview.Click += (_, _) => Prepare();
        AddWide(preview);
        AddRow("Nome do greide", name);
        AddRow("Estilo do perfil", profileStyle);
        AddRow("Conjunto de rótulos", labelSet);
        create.Enabled = false;
        create.Click += (_, _) => Create();
        AddWide(create);
        AddWide(result, fill: true, minHeight: 180);
        alignment.SelectedIndexChanged += (_, _) => LoadProfiles();
        terrain.SelectedIndexChanged += (_, _) => InvalidatePreview();
        foreach (var number in new[] { maximum, minimumLength, tolerance, step })
            number.ValueChanged += (_, _) => { InvalidatePreview(); SetStatus("Parâmetros alterados. Gere uma nova prévia."); };
    }

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        InvalidatePreview();
        Ui.SetItems(alignment, snapshot?.Alignments.Select(a => new Choice(a.Name, a.Handle)).ToArray() ?? Array.Empty<Choice>());
        LoadStyles(profileStyle, snapshot?.GradeStyles ?? Array.Empty<StyleInfo>());
        LoadStyles(labelSet, snapshot?.GradeLabelSets ?? Array.Empty<StyleInfo>());
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        InvalidatePreview();
        var selected = alignment.SelectedItem as Choice;
        var axis = Snapshot?.Alignments.FirstOrDefault(a => a.Handle == selected?.Handle);
        // Only surface-derived ground profiles; no silent adoption of a design profile.
        Ui.SetItems(terrain, axis?.Profiles.Where(p => p.Kind == "Terreno").Select(p => new Choice(p.Name, p.Handle)).ToArray() ?? Array.Empty<Choice>());
    }

    private void Prepare()
    {
        InvalidatePreview();
        if (Snapshot is null || Snapshot.Unit != "m") { SetStatus("Leia um desenho com unidades em metros.", "warning"); return; }
        if (alignment.SelectedItem is not Choice a || terrain.SelectedItem is not Choice p)
        { SetStatus("Escolha explicitamente o eixo e o perfil do terreno.", "warning"); return; }
        var request = new GradeRequest(Snapshot.DrawingPath, a.Handle, p.Handle, (double)step.Value,
            new GradeOptions((double)maximum.Value, (double)minimumLength.Value, (double)tolerance.Value));
        Pending.Grade = request;
        if (!Actions.Send("PROJETAGREIDE", this)) Pending.Grade = null;
    }

    private static void LoadStyles(ComboBox combo, IReadOnlyList<StyleInfo> styles)
    {
        Ui.SetItems(combo, styles.Select(s => new Choice(s.Name, s.Handle)));
        var infra = styles.Where(s => s.Name.Contains("INFRA", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (combo.SelectedIndex < 0 && infra.Length == 1)
            Ui.Select(combo, c => c.Handle == infra[0].Handle);
    }

    private void InvalidatePreview()
    {
        reviewedInput = null;
        reviewedPreview = null;
        create.Enabled = false;
        result.Items.Clear();
    }

    private void Create()
    {
        if (reviewedInput is null || reviewedPreview is null) { SetStatus("Gere e revise uma prévia primeiro.", "warning"); return; }
        if (profileStyle.SelectedItem is not Choice style || labelSet.SelectedItem is not Choice labels || string.IsNullOrWhiteSpace(name.Text))
        { SetStatus("Informe nome, estilo de perfil e conjunto de rótulos.", "warning"); return; }
        var warningText = string.Join("\n", reviewedPreview.Warnings);
        if (!Ui.Confirm(this, "CRIAR GREIDE — IFM",
            $"Criar '{name.Text.Trim()}' com {reviewedPreview.Pvis.Count} PIVs e {reviewedPreview.Pvis.Count - 1} tangentes?\n" +
            $"Rampa máxima: {Fmt.N(reviewedInput.Options.MaxPercent)}%. Sem curvas automáticas.\n" +
            $"Estilo: {style.Text}\nRótulos: {labels.Text}\n\nPendências da prévia:\n{warningText}")) return;
        Pending.GradeCreate = new GradeCreateRequest(reviewedInput, reviewedPreview, name.Text.Trim(), style.Handle, labels.Handle);
        if (!Actions.Send("PROJETACRIARGREIDE", this)) Pending.GradeCreate = null;
    }

    public void Display(GradeRequest input, GradePreview preview)
    {
        reviewedInput = input;
        reviewedPreview = preview;
        create.Enabled = true;
        result.Items.Clear();
        for (var i = 0; i < preview.Pvis.Count; i++)
        {
            var p = preview.Pvis[i];
            double? grade = i + 1 < preview.Pvis.Count
                ? 100 * (preview.Pvis[i + 1].Elevation - p.Elevation) / (preview.Pvis[i + 1].Station - p.Station) : null;
            result.Items.Add(new ListViewItem(new[] { Fmt.N(p.Station, 3), Fmt.N(p.Elevation, 3), Fmt.N(grade, 3) }));
        }
        ShowOutcome($"{preview.Pvis.Count} PIVs propostos. Nada foi criado.\n" + string.Join("\n", preview.Warnings), false);
    }
}
