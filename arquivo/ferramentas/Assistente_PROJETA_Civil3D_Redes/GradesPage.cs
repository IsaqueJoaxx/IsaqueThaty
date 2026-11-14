using System.Windows.Forms;

namespace Projeta.Civil3D;

// Aba Greide — IFM: tangentes e PIVs que acompanham o terreno sem passar da rampa máxima.
// Prévia primeiro; a criação usa exatamente a prévia revisada. Curvas verticais ficam com o usuário.
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
    private readonly NumericUpDown step = Ui.Number(10m, 0.1m, 1000m, 2, 1m);
    private readonly NumericUpDown maximum = Ui.Number(7m, 0.01m, 7m, 2, 0.1m);
    private readonly NumericUpDown tolerance = Ui.Number(0.50m, 0m, 100m, 2, 0.05m);
    private readonly NumericUpDown spacing = Ui.Number(40m, 1m, 10000m, 2, 5m);
    private readonly NumericUpDown warnOffset = Ui.Number(1.00m, 0m, 1000m, 2, 0.25m);
    private readonly CheckBox startOnGround = new() { Text = "Começar na cota do terreno", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 0, 2) };
    private readonly ThemedListView result = new(new[] { "Prog. (m)", "Cota (m)", "Rampa (%)", "Afast. (m)" }, new[] { 26, 26, 24, 24 });

    public GradesPage() : base("GREIDE AUTOMÁTICO — IFM",
        "Gera tangentes e PIVs que acompanham o terreno sem passar da rampa máxima. Revise a prévia e crie o perfil; as curvas verticais e o ajuste final ficam com você.")
    {
        AddRow("Eixo", alignment);
        AddRow("Perfil do terreno", terrain);
        AddRow("Intervalo de leitura (m)", step);
        AddWide(Hint("De quantos em quantos metros a cota do terreno é lida no perfil. Menor = mais detalhe (até 20.000 leituras: a cada 5 m, cerca de 100 km)."));
        AddRow("Rampa máxima (%)", maximum);
        AddWide(Hint("Nenhuma rampa passa deste valor (no máximo 7%). É limite, não inclinação obrigatória: onde o terreno é mais suave, o greide acompanha o terreno."));
        AddRow("Tolerância de simplificação (m)", tolerance);
        AddWide(Hint("Quanto o greide pode se afastar da linha que acompanha o terreno quando um PIV é retirado. Maior = menos PIVs."));
        AddRow("Distância desejada entre PIVs (m)", spacing);
        AddWide(Hint("Preferência, não regra: PIVs mais próximos que isso só ficam quando o relevo pede."));
        AddRow("Avisar corte/aterro acima de (m)", warnOffset);
        AddWide(Hint("Onde o terreno passa da rampa máxima, o greide corta e aterra o necessário e volta ao terreno logo depois. Esses trechos aparecem como aviso; não impedem a prévia nem a criação."));
        AddWide(startOnGround);
        AddWide(Hint("Desmarcado, o início também pode ficar em corte ou aterro. O fim nunca é fixado."));
        AddWide(Wrap("Valores iniciais editáveis; não são critérios certificados do DER-MG. Sem curvas verticais automáticas.", "warning"));
        var preview = Ui.Button("Gerar prévia de tangentes", true);
        preview.Click += (_, _) => Prepare();
        AddWide(preview);
        AddRow("Nome do greide", name);
        AddRow("Estilo do perfil", profileStyle);
        AddRow("Conjunto de rótulos", labelSet);
        create.Enabled = false;
        create.Click += (_, _) => Create();
        AddWide(create);
        AddWide(Hint("Rampa: de saída de cada PIV. Afast.: greide menos terreno no PIV (+ aterro, − corte)."));
        AddWide(result, fill: true, minHeight: 180);
        alignment.SelectedIndexChanged += (_, _) => LoadProfiles();
        terrain.SelectedIndexChanged += (_, _) => InvalidatePreview();
        foreach (var number in new[] { step, maximum, tolerance, spacing, warnOffset })
            number.ValueChanged += (_, _) => Changed();
        startOnGround.CheckedChanged += (_, _) => Changed();
    }

    private Label Hint(string text)
    {
        var label = Wrap(text, "muted");
        label.Margin = new Padding(0, 0, 0, Px(6));
        return label;
    }

    private void Changed()
    {
        InvalidatePreview();
        SetStatus("Parâmetros alterados. Gere uma nova prévia.");
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
            new GradeOptions((double)maximum.Value, (double)spacing.Value, (double)tolerance.Value, (double)warnOffset.Value, startOnGround.Checked));
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
            $"Estilo: {style.Text}\nRótulos: {labels.Text}\n\nAvisos da prévia:\n{warningText}")) return;
        Pending.GradeCreate = new GradeCreateRequest(reviewedInput, reviewedPreview, name.Text.Trim(), style.Handle, labels.Handle);
        if (!Actions.Send("PROJETACRIARGREIDE", this)) Pending.GradeCreate = null;
    }

    public void Display(GradeRequest input, GradePreview preview)
    {
        reviewedInput = input;
        reviewedPreview = preview;
        create.Enabled = true;
        result.Items.Clear();
        var grades = new List<double>();
        for (var i = 0; i < preview.Pvis.Count; i++)
        {
            var p = preview.Pvis[i];
            double? grade = i + 1 < preview.Pvis.Count
                ? 100 * (preview.Pvis[i + 1].Elevation - p.Elevation) / (preview.Pvis[i + 1].Station - p.Station) : null;
            if (grade is double g) grades.Add(g);
            double? offset = i < preview.PviOffsets.Count ? preview.PviOffsets[i] : null;
            result.Items.Add(new ListViewItem(new[] { Fmt.N(p.Station, 3), Fmt.N(p.Elevation, 3), Fmt.N(grade, 3), Fmt.N(offset, 2) }));
        }
        ShowOutcome($"{preview.Pvis.Count} PIVs e {preview.Pvis.Count - 1} tangentes; rampas de {Fmt.N(grades.Min(), 2)}% a {Fmt.N(grades.Max(), 2)}%. " +
                    "Nada foi criado ainda.\n" + string.Join("\n", preview.Warnings), false);
    }
}
