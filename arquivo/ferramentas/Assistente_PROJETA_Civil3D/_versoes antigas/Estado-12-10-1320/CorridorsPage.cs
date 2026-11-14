using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Projeta.Civil3D;

// Aba Corredores: liga cada eixo ao seu greide de projeto e à seção tipo, em um corredor único
// (várias linhas de base) ou um corredor por eixo, com a superfície alvo dos taludes.
internal sealed class CorridorsPage : ToolPage
{
    private static readonly Regex PreferredProfile = new(@"greide|projeto|\bfg\b|\bproj\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly ComboBox assembly;
    private readonly ComboBox target;
    private readonly ComboBox layout;
    private readonly TextBox baseName;
    private readonly NumericUpDown frequency;
    private readonly ThemedListView pairs;
    private readonly Label preview;

    public CorridorsPage() : base("Corredores",
        "Cada eixo usa o próprio greide de projeto. Marque os eixos, escolha a seção tipo e a superfície " +
        "onde os taludes encontram o terreno.")
    {
        assembly = Ui.Combo();
        target = Ui.Combo();
        layout = Ui.Combo();
        layout.Items.AddRange(new object[] { "Um corredor com todos os eixos", "Um corredor por eixo" });
        layout.SelectedIndex = 0;
        baseName = new TextBox { Text = "PROJETA", Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 4, 0, 4) };
        frequency = Ui.Number(10m, 1m, 200m, 2, 1m);
        AddRow("Seção tipo", assembly);
        AddRow("Superfície alvo", target);
        AddRow("Organização", layout);
        AddRow("Nome", baseName);
        AddRow("Frequência (m)", frequency);

        pairs = new ThemedListView(new[] { "Eixo", "Greide", "Situação" }, new[] { 34, 36, 30 }) { CheckBoxes = true };
        AddWide(pairs, fill: true, minHeight: 120);

        var showPreview = Ui.Button("Prévia");
        showPreview.Click += (_, _) => Preview();
        var create = Ui.Button("Criar corredores…", primary: true);
        create.Click += (_, _) => Create();
        AddWide(Ui.Row(showPreview, create));
        preview = Wrap("", null);
        AddWide(preview);

        pairs.ItemChecked += (_, _) => Preview();
        foreach (var combo in new[] { assembly, target, layout }) combo.SelectedIndexChanged += (_, _) => Preview();
        baseName.TextChanged += (_, _) => Preview();
        frequency.ValueChanged += (_, _) => Preview();
    }

    // Greide do próprio eixo: de projeto (FG). Havendo vários, prefere o que tem greide/projeto/FG no nome.
    public static ProfileInfo? PickProfile(AlignmentInfo a, out string situation)
    {
        var layoutProfiles = a.Profiles.Where(p => p.IsLayout).ToList();
        if (layoutProfiles.Count == 0) { situation = "Sem greide"; return null; }
        var chosen = layoutProfiles.FirstOrDefault(p => PreferredProfile.IsMatch(p.Name)) ?? layoutProfiles[0];
        situation = layoutProfiles.Count == 1 ? "OK" : $"Usando 1 de {layoutProfiles.Count}";
        return chosen;
    }

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        Ui.SetItems(assembly, snapshot?.Assemblies.Select(a => new Choice(a.Name, a.Handle)) ?? Array.Empty<Choice>());
        if (assembly.SelectedIndex < 0 && assembly.Items.Count > 0) assembly.SelectedIndex = 0;
        var targets = new List<Choice> { new("Nenhuma (taludes sem alvo)", "") };
        if (snapshot is not null)
            targets.AddRange(snapshot.Surfaces.Where(s => !s.IsVolume).Select(s => new Choice(s.Name, s.Handle)));
        Ui.SetItems(target, targets);
        if (target.SelectedIndex <= 0 && snapshot is not null && SurfaceNaming.First(snapshot.Surfaces, SurfaceNaming.Ground) is { } ground)
            Ui.Select(target, c => c.Handle == ground.Handle);

        var wasChecked = pairs.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => ((AlignmentInfo)i.Tag!).Handle).ToHashSet();
        var hadItems = pairs.Items.Count > 0;
        pairs.BeginUpdate();
        pairs.Items.Clear();
        if (snapshot is not null)
            foreach (var a in snapshot.Alignments)
            {
                var profile = PickProfile(a, out var situation);
                var item = new ListViewItem(new[] { a.Name, profile?.Name ?? "—", situation })
                {
                    Tag = a,
                    ToolTipText = profile is null
                        ? $"{a.Name}: sem greide de projeto (FG). Crie um greide para incluir este eixo."
                        : $"{a.Name} → {profile.Name}\nGreides de projeto do eixo: {string.Join(", ", a.Profiles.Where(p => p.IsLayout).Select(p => p.Name))}",
                    Checked = profile is not null && (!hadItems || wasChecked.Contains(a.Handle))
                };
                pairs.Items.Add(item);
            }
        pairs.EndUpdate();
        Preview();
    }

    private CorridorRequest? Preview()
    {
        if (preview is null) return null;
        string? problem = null;
        var chosen = pairs.CheckedItems.Cast<ListViewItem>()
            .Select(i => (Alignment: (AlignmentInfo)i.Tag!, Profile: PickProfile((AlignmentInfo)i.Tag!, out _)))
            .Where(p => p.Profile is not null).ToList();
        if (Snapshot is null) problem = "Leia o desenho ativo primeiro.";
        else if (assembly.SelectedItem is not Choice) problem = "O desenho não tem seção tipo (assembly).";
        else if (chosen.Count == 0) problem = "Marque pelo menos um eixo que tenha greide de projeto.";
        else if (string.IsNullOrWhiteSpace(baseName.Text)) problem = "Informe um nome para o corredor.";
        if (problem is not null || Snapshot is null || assembly.SelectedItem is not Choice asm)
        {
            preview.Text = problem ?? "";
            return null;
        }
        var single = layout.SelectedIndex == 0;
        var targetChoice = target.SelectedItem as Choice;
        var targetText = string.IsNullOrEmpty(targetChoice?.Handle) ? "sem superfície alvo" : $"taludes até {targetChoice!.Text}";
        var name = baseName.Text.Trim();
        preview.Text = (single
                ? $"Vai criar o corredor \"{name}\" com {chosen.Count} linha(s) de base"
                : $"Vai criar {chosen.Count} corredor(es) \"{name} - <eixo>\"") +
            $", seção tipo {asm.Text}, {targetText}, frequência de {Fmt.N((double)frequency.Value)} m: " +
            string.Join("; ", chosen.Select(c => $"{c.Alignment.Name} → {c.Profile!.Name}")) + ".";
        return new CorridorRequest(Snapshot.DrawingPath,
            chosen.Select(c => new CorridorItem(c.Alignment.Handle, c.Profile!.Handle)).ToArray(),
            asm.Handle, string.IsNullOrEmpty(targetChoice?.Handle) ? null : targetChoice!.Handle,
            single, name, (double)frequency.Value);
    }

    private void Create()
    {
        var request = Preview();
        if (request is null) { SetStatus(preview.Text.Length > 0 ? preview.Text : "Revise os campos.", "warning"); return; }
        if (!Ui.Confirm(this, "Criar corredores", preview.Text)) return;
        Pending.Corridors = request;
        if (Actions.Send("PROJETACORREDORES", this)) SetStatus("Criando e construindo os corredores…");
    }

    public void ShowResult(string text, bool ok) => SetStatus(text, ok ? "good" : "warning");
}
