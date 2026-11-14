using System.Windows.Forms;

namespace Projeta.Civil3D;

// Aba Volumes: corte e aterro entre terreno natural e projeto, de dois jeitos.
// Superfícies: superfície de volume TIN, na área comum ou dentro de uma polilinha. "Calcular" não grava nada;
// "Criar superfície de volume" grava, depois de confirmar.
// Seções: as seções que o Civil 3D já amostrou num grupo de linhas de amostragem, somadas pelo método das
// áreas médias, estaca por estaca. Só lê o desenho.
internal sealed class VolumesPage : ToolPage
{
    private const int BySurfaces = 0, BySections = 1;
    private readonly ComboBox mode;
    private readonly ComboBox ground;
    private readonly ComboBox design;
    private readonly Label limitText;
    private readonly ComboBox group;
    private readonly ComboBox groundSection;
    private readonly ComboBox designSection;
    private readonly NumericUpDown from;
    private readonly NumericUpDown to;
    private readonly Label groupInfo;
    private readonly NumericUpDown cutFactor;
    private readonly NumericUpDown fillFactor;
    private readonly Button create;
    private readonly ThemedListView results;
    private readonly ThemedListView stations;
    private readonly Button export;
    private readonly List<Control> surfaceOnly = new();
    private readonly List<Control> sectionOnly = new();
    private VolumeResult? lastResult;
    private string? resultDrawing;
    private string? limitHandle;
    private string? limitDrawing;
    private VolumeRequest? lastCalculated;
    private SectionVolumeResult? lastSection;
    private string? sectionDrawing;
    private string? rangeOf;
    private (double Start, double End)? rangeShown;

    public VolumesPage() : base("VOLUMES — IFM",
        "Compara o terreno natural com o projeto. Por superfícies, usa uma superfície de volume; por seções, usa as seções " +
        "que o Civil 3D já amostrou num grupo de linhas de amostragem, pelo método das áreas médias. Calcular não grava nada no desenho.")
    {
        mode = Ui.Combo();
        mode.Items.AddRange(new object[] { "Superfícies (superfície de volume)", "Seções do Civil 3D (áreas médias)" });
        mode.SelectedIndex = BySurfaces;
        AddRow("Calcular por", mode);

        // ---- por superfícies
        ground = Ui.Combo();
        design = Ui.Combo();
        SurfaceRow("Terreno natural", ground);
        SurfaceRow("Projeto", design);
        limitText = Ui.Label("Área comum às duas superfícies");
        limitText.AutoEllipsis = true;
        SurfaceRow("Limite", limitText);
        var pick = Ui.Button("Selecionar polilinha");
        pick.Click += (_, _) => { if (Actions.Send("PROJETALIMITE", this)) SetStatus("Selecione a polilinha fechada no desenho."); };
        var clear = Ui.Button("Usar área comum");
        clear.Click += (_, _) => SetLimit(null, null, null);
        var limitButtons = Ui.Row(pick, clear);
        AddWide(limitButtons);
        surfaceOnly.Add(limitButtons);

        // ---- por seções
        group = Ui.Combo();
        groundSection = Ui.Combo();
        designSection = Ui.Combo();
        SectionRow("Grupo de seções", group);
        SectionRow("Seção do terreno", groundSection);
        SectionRow("Seção do projeto", designSection);
        from = Ui.Number(0m, -1_000_000m, 10_000_000m, 3, 20m);
        to = Ui.Number(0m, -1_000_000m, 10_000_000m, 3, 20m);
        SectionRow("Da estaca (m)", from);
        SectionRow("Até a estaca (m)", to);
        groupInfo = Wrap("", "muted");
        AddWide(groupInfo);
        sectionOnly.Add(groupInfo);

        // ---- comum
        cutFactor = Ui.Number(1m, 0.01m, 10m, 3, 0.01m);
        fillFactor = Ui.Number(1m, 0.01m, 10m, 3, 0.01m);
        AddRow("Fator de corte", cutFactor);
        AddRow("Fator de aterro", fillFactor);
        var tips = new ToolTip();
        tips.SetToolTip(cutFactor, "Multiplica o volume de corte (por exemplo, empolamento), como na superfície de volume do Civil 3D.");
        tips.SetToolTip(fillFactor, "Multiplica o volume de aterro (por exemplo, compactação), como na superfície de volume do Civil 3D.");
        tips.SetToolTip(from, "Estaca em metros. Só entram as linhas de amostragem dentro deste trecho.");
        tips.SetToolTip(to, "Estaca em metros. Só entram as linhas de amostragem dentro deste trecho.");

        var calculate = Ui.Button("Calcular volumes", primary: true);
        calculate.Click += (_, _) => Calculate();
        create = Ui.Button("Criar superfície de volume…");
        create.Enabled = false;
        create.Click += (_, _) => Create();
        AddWide(Ui.Row(calculate, create));

        results = new ThemedListView(new[] { "Resultado", "Valor" }, new[] { 45, 55 });
        AddWide(results, fill: true, minHeight: 140);
        stations = new ThemedListView(new[] { "Estaca", "Corte m²", "Aterro m²", "V. corte m³", "V. aterro m³" }, new[] { 24, 18, 18, 20, 20 });
        AddWide(stations, fill: true, minHeight: 160);
        export = Ui.Button("Exportar para Excel (CSV)…");
        export.Enabled = false;
        export.Click += (_, _) => { if (SectionMode) ExportSections(); else Export(); };
        AddWide(export);
        var table = Ui.Button("Tabela de volumes por estaca — IFM…");
        table.Click += (_, _) =>
        {
            if (!Ui.Confirm(this, "TABELA DE VOLUMES — IFM",
                "Abrir TABVOL para comparar superfícies em seções ou importar áreas de CSV/TXT?\n\n" +
                "Continue as escolhas na linha de comando. A rotina insere a tabela e exporta CSV pelo método " +
                "das áreas médias, sem aplicar os fatores e a polilinha deste painel.\n\n" +
                "A rotina usa a cobertura comum das superfícies e pode registrar área zero onde faltam dados. " +
                "Confira os avisos antes de usar os totais. Um U desfaz a inserção; não apaga o CSV.")) return;
            if (Actions.Send("PROJETATABVOL", this)) SetStatus("Continue as escolhas na linha de comando do Civil 3D.");
        };
        AddWide(table);

        mode.SelectedIndexChanged += (_, _) => ApplyMode();
        ground.SelectedIndexChanged += (_, _) => FieldsChanged();
        design.SelectedIndexChanged += (_, _) => FieldsChanged();
        group.SelectedIndexChanged += (_, _) => { if (!updating) LoadGroup(); FieldsChanged(); };
        groundSection.SelectedIndexChanged += (_, _) => FieldsChanged();
        designSection.SelectedIndexChanged += (_, _) => FieldsChanged();
        from.ValueChanged += (_, _) => FieldsChanged();
        to.ValueChanged += (_, _) => FieldsChanged();
        cutFactor.ValueChanged += (_, _) => FieldsChanged();
        fillFactor.ValueChanged += (_, _) => FieldsChanged();
        ApplyMode();
    }

    private void SurfaceRow(string label, Control control)
    {
        surfaceOnly.Add(AddRow(label, control));
        surfaceOnly.Add(control);
    }

    private void SectionRow(string label, Control control)
    {
        sectionOnly.Add(AddRow(label, control));
        sectionOnly.Add(control);
    }

    private bool SectionMode => mode.SelectedIndex == BySections;

    // Mostra só os campos do modo escolhido; as duas listas dividem a altura no modo por seções.
    private void ApplyMode()
    {
        var sections = SectionMode;
        SuspendLayout();
        foreach (var c in surfaceOnly) c.Visible = !sections;
        foreach (var c in sectionOnly) c.Visible = sections;
        create.Visible = !sections;
        stations.Visible = sections;
        SetRowStyle(results, SizeType.Percent, sections ? 40 : 100);
        SetRowStyle(stations, sections ? SizeType.Percent : SizeType.Absolute, sections ? 60 : 0);
        export.Text = sections ? "Exportar para Excel (.xlsx)…" : "Exportar para Excel (CSV)…";
        ResumeLayout(true);
        if (sections) DescribeGroup();
        RenderCurrent();
    }

    // Atualização vinda do desenho (não do usuário): as listas são refeitas sem disparar o aviso de "campos mudaram".
    private bool updating;

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        updating = true;
        try
        {
            var options = snapshot?.Surfaces.Where(s => !s.IsVolume).Select(s => new Choice(s.Name, s.Handle)).ToArray() ?? Array.Empty<Choice>();
            Ui.SetItems(ground, options);
            Ui.SetItems(design, options);
            if (snapshot is not null)
            {
                // Mantém a escolha anterior; sem ela, sugere pelo nome (1 = terreno natural, 2 = projeto).
                var g = SurfaceNaming.First(snapshot.Surfaces, SurfaceNaming.Ground);
                var d = SurfaceNaming.First(snapshot.Surfaces, SurfaceNaming.Design);
                if (ground.SelectedIndex < 0) Ui.Select(ground, c => c.Handle == g?.Handle);
                if (design.SelectedIndex < 0) Ui.Select(design, c => c.Handle == d?.Handle);
                if (limitDrawing is not null && limitDrawing != snapshot.DrawingPath) SetLimit(null, null, null);
            }

            var groups = snapshot?.SampleLineGroups.Select(x => new Choice($"{x.AlignmentName} · {x.Name} ({x.Lines} linhas)", x.Handle))
                         .ToArray() ?? Array.Empty<Choice>();
            Ui.SetItems(group, groups);
            if (group.SelectedIndex < 0 && group.Items.Count > 0) group.SelectedIndex = 0;
            LoadGroup();
        }
        finally { updating = false; }

        // O resultado na tela só deixa de valer se as superfícies escolhidas mudaram ou sumiram.
        if (lastCalculated is not null && (snapshot is null
            || (ground.SelectedItem as Choice)?.Handle != lastCalculated.BaseHandle
            || (design.SelectedItem as Choice)?.Handle != lastCalculated.ComparisonHandle))
            ResetCalculation();
        // A superfície de volume criada sumiu (U ou apagada): o cálculo continua valendo e dá para criar de novo.
        if (snapshot is not null && lastResult is { CreatedName: { } created } && snapshot.DrawingPath == resultDrawing
            && !snapshot.Surfaces.Any(s => s.Name == created))
        {
            lastResult = lastResult with { CreatedName = null };
            if (!SectionMode) RenderSurface();
            create.Enabled = lastCalculated is not null;
            SetStatus($"A superfície \"{created}\" não está mais no desenho (desfeita ou apagada). " +
                      (create.Enabled ? "O cálculo continua valendo; dá para criar de novo." : "Calcule de novo para criar."), "warning");
        }
        // Volume por seções de outro desenho não fica na tela.
        if (lastSection is not null && (snapshot is null || snapshot.DrawingPath != sectionDrawing))
        {
            lastSection = null;
            if (SectionMode) RenderCurrent();
        }
    }

    // ------------------------------------------------------------------ superfícies

    // Chamado pelo comando PROJETALIMITE depois que a polilinha foi escolhida.
    public void SetLimit(string? handle, double? area, string? drawingPath)
    {
        limitHandle = handle;
        limitDrawing = drawingPath;
        limitText.Text = handle is null ? "Área comum às duas superfícies"
            : $"Polilinha {handle} · {Fmt.Area(area, Snapshot?.Unit ?? "m")}";
        ResetCalculation();
    }

    private VolumeRequest? BuildRequest(bool createSurface)
    {
        if (Snapshot is null) { SetStatus("Abra um desenho no Civil 3D.", "warning"); return null; }
        if (ground.SelectedItem is not Choice g || design.SelectedItem is not Choice d)
        {
            SetStatus("Escolha o terreno natural e o projeto.", "warning");
            return null;
        }
        if (g.Handle == d.Handle) { SetStatus("Terreno natural e projeto precisam ser superfícies diferentes.", "warning"); return null; }
        return new VolumeRequest(Snapshot.DrawingPath, g.Handle, d.Handle, limitHandle,
            (double)cutFactor.Value, (double)fillFactor.Value, createSurface);
    }

    private void Calculate()
    {
        if (SectionMode)
        {
            var sectionRequest = BuildSectionRequest();
            if (sectionRequest is null) return;
            Pending.SectionVolume = sectionRequest;
            if (Actions.Send("PROJETAVOLSECOES", this)) SetStatus("Lendo as seções e calculando…");
            return;
        }
        var request = BuildRequest(false);
        if (request is null) return;
        Pending.Volume = request;
        if (Actions.Send("PROJETAVOLUMES", this)) SetStatus("Calculando…");
    }

    private void Create()
    {
        var request = BuildRequest(true);
        if (request is null || lastCalculated != request with { Create = false }) { create.Enabled = false; return; }
        var limit = limitHandle is null ? "na área comum às duas superfícies" : $"limitada pela polilinha {limitHandle}";
        var text = $"Vai criar uma superfície de volume entre {((Choice)ground.SelectedItem!).Text} (base) e " +
                   $"{((Choice)design.SelectedItem!).Text} (comparação), {limit}, com fator de corte " +
                   $"{Fmt.N(request.CutFactor, 3)} e de aterro {Fmt.N(request.FillFactor, 3)}.";
        if (!Ui.Confirm(this, "Criar superfície de volume", text)) return;
        Pending.Volume = request;
        if (Actions.Send("PROJETAVOLUMES", this)) SetStatus("Criando a superfície de volume…");
    }

    // Mudança feita pelo usuário: o resultado na tela deixa de valer (fica à vista até calcular de novo)
    // e o botão de criar desliga.
    private void FieldsChanged()
    {
        if (updating) return;
        var hadSurface = lastCalculated is not null;
        ResetCalculation();
        if (SectionMode ? lastSection is not null : hadSurface)
            SetStatus("Os campos mudaram. Clique em Calcular volumes de novo.", "warning");
    }

    private void ResetCalculation()
    {
        lastCalculated = null;
        create.Enabled = false;
    }

    public void ShowResult(VolumeRequest request, VolumeResult r)
    {
        lastResult = r;
        resultDrawing = request.DrawingPath;
        if (SectionMode) mode.SelectedIndex = BySurfaces; // ApplyMode desenha o resultado
        else RenderSurface();
        lastCalculated = request with { Create = false };
        create.Enabled = r.CreatedName is null;
        SetStatus(r.CreatedName is null
            ? $"Calculado às {DateTime.Now:HH:mm:ss}. Nada foi gravado no desenho."
            : $"Superfície \"{r.CreatedName}\" criada. Para desfazer: U.", "good");
    }

    private void RenderCurrent()
    {
        if (SectionMode) RenderSection();
        else RenderSurface();
    }

    private void RenderSurface()
    {
        results.BeginUpdate();
        results.Items.Clear();
        stations.Items.Clear();
        var r = lastResult;
        if (r is not null)
        {
            var u = Snapshot?.Unit ?? "m";
            Add("Terreno natural (base)", r.BaseName);
            Add("Projeto (comparação)", r.ComparisonName);
            Add("Limite", r.Limit);
            if (r.LimitArea is not null) Add("Área do limite", Fmt.Area(r.LimitArea, u));
            AddTotals(r.Cut, r.Fill, r.CutFactor, r.FillFactor, u);
            if (r.CreatedName is not null) Add("Criada no desenho", r.CreatedName);
        }
        results.EndUpdate();
        export.Enabled = r is not null;
    }

    private void Add(string name, string value, string? tip = null) =>
        results.Items.Add(new ListViewItem(new[] { name, value }) { ToolTipText = tip ?? $"{name}: {value}" });

    private void AddTotals(double cut, double fill, double cutFactor, double fillFactor, string u)
    {
        Add("Corte", $"{Fmt.N(cut)} {u}³");
        Add("Aterro", $"{Fmt.N(fill)} {u}³");
        Add("Saldo", Fmt.Balance(cut - fill, u));
        if (cutFactor != 1 || fillFactor != 1)
        {
            Add($"Corte × {Fmt.N(cutFactor, 3)}", $"{Fmt.N(cut * cutFactor)} {u}³");
            Add($"Aterro × {Fmt.N(fillFactor, 3)}", $"{Fmt.N(fill * fillFactor)} {u}³");
            Add("Saldo com fatores", Fmt.Balance(cut * cutFactor - fill * fillFactor, u));
        }
    }

    // CSV com ponto e vírgula e vírgula decimal: o Excel em português abre direto, com números de verdade.
    private void Export()
    {
        if (lastResult is not { } r) return;
        var drawing = Snapshot?.Drawing ?? "desenho";
        using var dialog = new SaveFileDialog
        {
            Title = "Exportar volumes", Filter = "CSV para Excel (*.csv)|*.csv", OverwritePrompt = true,
            FileName = $"Volumes - {System.IO.Path.GetFileNameWithoutExtension(drawing)} - {DateTime.Now:yyyy-MM-dd HHmm}.csv"
        };
        var folder = System.IO.Path.GetDirectoryName(Snapshot?.DrawingPath ?? "");
        if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder)) dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            System.IO.File.WriteAllText(dialog.FileName, VolumeCsv(r, drawing, Snapshot?.Unit ?? "m"), new System.Text.UTF8Encoding(true));
            SetStatus($"Exportado: {System.IO.Path.GetFileName(dialog.FileName)}", "good");
        }
        catch (Exception ex) { SetStatus("Não foi possível salvar o arquivo: " + ex.Message, "warning"); }
    }

    internal static string VolumeCsv(VolumeResult r, string drawing, string u)
    {
        static string Cell(string s) => s.IndexOfAny(new[] { ';', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        static string Num(double v, int d = 2) => v.ToString("F" + d, Fmt.Br);
        var lines = new List<string>
        {
            "PROJETA - Volumes",
            "Desenho;" + Cell(drawing),
            "Data;" + DateTime.Now.ToString("dd/MM/yyyy HH:mm", Fmt.Br),
            "Terreno natural (base);" + Cell(r.BaseName),
            "Projeto (comparação);" + Cell(r.ComparisonName),
            "Limite;" + Cell(r.Limit),
        };
        if (r.LimitArea is double area) lines.Add($"Área do limite ({u}²);{Num(area)}");
        if (r.CreatedName is not null) lines.Add("Superfície de volume criada;" + Cell(r.CreatedName));
        lines.Add("");
        lines.Add($";Volume ({u}³);Fator;Volume com fator ({u}³)");
        lines.Add($"Corte;{Num(r.Cut)};{Num(r.CutFactor, 3)};{Num(r.Cut * r.CutFactor)}");
        lines.Add($"Aterro;{Num(r.Fill)};{Num(r.FillFactor, 3)};{Num(r.Fill * r.FillFactor)}");
        lines.Add($"Saldo (corte - aterro);{Num(r.Cut - r.Fill)};;{Num(r.Cut * r.CutFactor - r.Fill * r.FillFactor)}");
        return string.Join("\r\n", lines) + "\r\n";
    }

    // ------------------------------------------------------------------ seções

    private SampleLineGroupInfo? SelectedGroup =>
        group.SelectedItem is Choice c ? Snapshot?.SampleLineGroups.FirstOrDefault(g => g.Handle == c.Handle) : null;

    // Preenche as fontes amostradas do grupo e o trecho. Ao trocar de grupo, sugere de novo o terreno e o
    // projeto e volta ao grupo inteiro; no mesmo grupo, mantém as escolhas (e o trecho, se foi mudado à mão).
    private void LoadGroup()
    {
        var g = SelectedGroup;
        var wasUpdating = updating;
        updating = true;
        try
        {
            var changed = g?.Handle != rangeOf;
            if (changed)
            {
                groundSection.Items.Clear();
                designSection.Items.Clear();
            }
            var options = g?.Sources.Select(s => new Choice($"{s.Name} ({s.Kind})", s.Key)).ToArray() ?? Array.Empty<Choice>();
            Ui.SetItems(groundSection, options);
            Ui.SetItems(designSection, options);
            if (g is not null)
            {
                if (changed || groundSection.SelectedIndex < 0)
                    Ui.Select(groundSection, c => c.Handle == SuggestGround(g)?.Key);
                var groundKey = (groundSection.SelectedItem as Choice)?.Handle;
                if (changed || designSection.SelectedIndex < 0)
                    Ui.Select(designSection, c => c.Handle == SuggestDesign(g, groundKey)?.Key);
                var untouched = rangeShown is { } shown && (double)from.Value == Round(shown.Start) && (double)to.Value == Round(shown.End);
                if ((changed || untouched) && g.Start is double s && g.End is double e)
                {
                    from.Value = Clamp(from, s);
                    to.Value = Clamp(to, e);
                    rangeShown = (s, e);
                }
            }
            rangeOf = g?.Handle;
        }
        finally { updating = wasUpdating; }
        DescribeGroup();
    }

    private static double Round(double v) => Math.Round(v, 3);

    private static decimal Clamp(NumericUpDown n, double v) => Math.Min(n.Maximum, Math.Max(n.Minimum, (decimal)Round(v)));

    // Terreno: nome começando por 1 (como nas superfícies), depois nomes de terreno, depois a primeira que não é de corredor.
    private static SectionSourceInfo? SuggestGround(SampleLineGroupInfo g) =>
        g.Sources.FirstOrDefault(s => SurfaceNaming.Role(s.Name) == SurfaceNaming.Ground)
        ?? g.Sources.FirstOrDefault(s => s.Kind != SectionVolumeOps.CorridorKind && LooksLikeGround(s.Name))
        ?? g.Sources.FirstOrDefault(s => s.Kind != SectionVolumeOps.CorridorKind)
        ?? g.Sources.FirstOrDefault();

    // Projeto: nome começando por 2; depois a superfície de terraplenagem do corredor (Datum, subleito...), que é a
    // que dá o volume de terraplenagem; depois qualquer superfície de corredor; depois qualquer outra que não seja o terreno.
    private static SectionSourceInfo? SuggestDesign(SampleLineGroupInfo g, string? groundKey) =>
        g.Sources.FirstOrDefault(s => s.Key != groundKey && SurfaceNaming.Role(s.Name) == SurfaceNaming.Design)
        ?? g.Sources.FirstOrDefault(s => s.Key != groundKey && s.Kind == SectionVolumeOps.CorridorKind && LooksLikeEarthwork(s.Name))
        ?? g.Sources.FirstOrDefault(s => s.Key != groundKey && s.Kind == SectionVolumeOps.CorridorKind)
        ?? g.Sources.FirstOrDefault(s => s.Key != groundKey);

    private static bool LooksLikeEarthwork(string name)
    {
        var n = name.ToUpperInvariant();
        return n.Contains("DATUM") || n.Contains("TERRAP") || n.Contains("SUBLEITO") || n.Contains("PLATAFORMA");
    }

    private static bool LooksLikeGround(string name)
    {
        var n = " " + name.ToUpperInvariant().Replace('_', ' ').Replace('-', ' ') + " ";
        return n.Contains(" TN ") || n.Contains("TERRENO") || n.Contains("NATURAL") || n.Contains(" EG ")
               || n.Contains("EXISTENTE") || n.Contains("LEVANTAMENTO") || n.Contains("PRIMITIVO");
    }

    private void DescribeGroup()
    {
        if (!SectionMode) return;
        var g = SelectedGroup;
        if (Snapshot is null) groupInfo.Text = "";
        else if (Snapshot.SampleLineGroups.Count == 0)
            groupInfo.Text = "O desenho não tem grupos de linhas de amostragem. Crie um na aba Seções ou no Civil 3D, " +
                             "amostrando o terreno natural e o projeto.";
        else if (g is null) groupInfo.Text = "Escolha o grupo de seções.";
        else if (g.Sources.Count < 2)
            groupInfo.Text = $"O grupo {g.Name} tem {g.Sources.Count} superfície(s) amostrada(s). Para calcular, amostre o terreno " +
                             "natural e o projeto no grupo (propriedades do grupo de linhas de amostragem, aba Seções).";
        else
            groupInfo.Text = $"{g.Lines} linhas de amostragem, da estaca {Estacas.Text(g.Start)} à {Estacas.Text(g.End)}. " +
                             $"Amostradas: {string.Join(", ", g.Sources.Select(s => s.Name))}.";
    }

    private SectionVolumeRequest? BuildSectionRequest()
    {
        if (Snapshot is null) { SetStatus("Abra um desenho no Civil 3D.", "warning"); return null; }
        var g = SelectedGroup;
        if (g is null)
        {
            SetStatus(Snapshot.SampleLineGroups.Count == 0
                ? "O desenho não tem grupos de linhas de amostragem."
                : "Escolha o grupo de seções.", "warning");
            return null;
        }
        if (groundSection.SelectedItem is not Choice a || designSection.SelectedItem is not Choice b)
        {
            SetStatus("Escolha a seção do terreno e a do projeto. O grupo precisa ter as duas superfícies amostradas.", "warning");
            return null;
        }
        if (a.Handle == b.Handle) { SetStatus("A seção do terreno e a do projeto precisam ser de superfícies diferentes.", "warning"); return null; }
        if (to.Value <= from.Value) { SetStatus("A estaca final precisa ser maior que a inicial.", "warning"); return null; }
        string Name(string key) => g.Sources.FirstOrDefault(s => s.Key == key)?.Name ?? key;
        return new SectionVolumeRequest(Snapshot.DrawingPath, g.Handle, a.Handle, Name(a.Handle), b.Handle, Name(b.Handle),
            (double)from.Value, (double)to.Value, (double)cutFactor.Value, (double)fillFactor.Value);
    }

    public void ShowSectionResult(SectionVolumeRequest request, SectionVolumeResult r)
    {
        lastSection = r;
        sectionDrawing = request.DrawingPath;
        if (!SectionMode) mode.SelectedIndex = BySections; // ApplyMode desenha o resultado
        else RenderSection();
        var warnings = r.Warnings.Count;
        SetStatus($"Calculado às {DateTime.Now:HH:mm:ss} com {r.Rows.Count} seções, pelo método das áreas médias. Nada foi gravado no desenho." +
                  (warnings > 0 ? $" Veja {warnings} aviso(s) na lista." : ""), warnings > 0 ? "warning" : "good");
    }

    private void RenderSection()
    {
        results.BeginUpdate();
        stations.BeginUpdate();
        results.Items.Clear();
        stations.Items.Clear();
        var r = lastSection;
        if (r is not null)
        {
            // Totais primeiro (a lista é baixa neste modo), depois a conferência, os avisos e de onde veio o cálculo.
            var u = Snapshot?.Unit ?? "m";
            AddTotals(r.Cut, r.Fill, r.CutFactor, r.FillFactor, u);
            foreach (var m in r.MaterialLists)
            {
                var about = $"Lista de materiais \"{m.Name}\" calculada pelo Civil 3D" + (m.Method.Length > 0 ? $" ({m.Method})" : "") +
                            (m.Factors.Length > 0 ? $". Materiais: {m.Factors}" : "") + ". Mesmo trecho de estacas.";
                if (m.Problem is not null) { Add($"Civil 3D · {m.Name}", m.Problem, about + " " + m.Problem); continue; }
                Add($"Civil 3D · {m.Name}", $"corte {Fmt.N(m.Cut)} · aterro {Fmt.N(m.Fill)} {u}³",
                    about + " Os volumes do Civil 3D já vêm com os fatores da lista.");
                // O Civil 3D aplica os fatores da lista; a diferença compara com o PROJETA nos mesmos fatores.
                if (m.CutFactor is double cf && m.FillFactor is double ff)
                    Add("   diferença", $"corte {Signed(r.Cut * cf - m.Cut)} · aterro {Signed(r.Fill * ff - m.Fill)} {u}³",
                        $"PROJETA com os fatores da lista (corte ×{Fmt.N(cf, 3)}, aterro ×{Fmt.N(ff, 3)}) menos Civil 3D. " +
                        "Diferenças vêm de superfícies diferentes na lista de materiais, do método de cálculo ou da correção de curva.");
                else
                    Add("   diferença", "lista com vários materiais; compare na planilha",
                        "A lista tem mais de um material com fatores, então não dá para comparar o total direto com o PROJETA.");
            }
            foreach (var w in r.Warnings) Add("Aviso", w, w);
            Add("Grupo de seções", $"{r.AlignmentName} · {r.GroupName}");
            Add("Terreno natural (seção)", r.BaseName);
            Add("Projeto (seção)", r.ComparisonName);
            Add("Trecho", $"{Estacas.Text(r.Start)} a {Estacas.Text(r.End)} · {r.Rows.Count} seções");
            Add("Método", "Áreas médias entre seções vizinhas");

            foreach (var x in r.Rows)
            {
                var label = (x.Note is null ? "" : "⚠ ") + Estacas.Text(x.Station);
                var tip = $"Estaca {Estacas.Text(x.Station)} ({Fmt.N(x.Station, 3)} m) · linha {x.Name}\n" +
                          $"Áreas: corte {Fmt.N(x.CutArea, 3)} {u}², aterro {Fmt.N(x.FillArea, 3)} {u}²\n" +
                          $"Volumes desde a seção anterior: corte {Fmt.N(x.CutVolume)} {u}³, aterro {Fmt.N(x.FillVolume)} {u}³\n" +
                          $"Acumulados: corte {Fmt.N(x.CumulativeCut)} {u}³, aterro {Fmt.N(x.CumulativeFill)} {u}³" +
                          (x.Note is null ? "" : "\n" + x.Note);
                stations.Items.Add(new ListViewItem(new[]
                    { label, Fmt.N(x.CutArea), Fmt.N(x.FillArea), Fmt.N(x.CutVolume), Fmt.N(x.FillVolume) }) { ToolTipText = tip });
            }
        }
        stations.EndUpdate();
        results.EndUpdate();
        export.Enabled = r is not null;
    }

    private static string Signed(double v) => (v < -0.005 ? "−" : "+") + Fmt.N(Math.Abs(v));

    private void ExportSections()
    {
        if (lastSection is not { } r) return;
        var drawing = sectionDrawing ?? Snapshot?.DrawingPath ?? "desenho";
        var name = $"Volumes por seções - {System.IO.Path.GetFileNameWithoutExtension(drawing)} - {r.GroupName} - {DateTime.Now:yyyy-MM-dd HHmm}.xlsx";
        name = string.Concat(name.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '-' : c));
        using var dialog = new SaveFileDialog
        {
            Title = "Exportar volumes por seções", Filter = "Planilha do Excel (*.xlsx)|*.xlsx", OverwritePrompt = true, FileName = name
        };
        var folder = System.IO.Path.GetDirectoryName(drawing);
        if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder)) dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            Xlsx.Write(dialog.FileName, SectionSheets(r, System.IO.Path.GetFileName(drawing), Snapshot?.Unit ?? "m"));
            SetStatus("Planilha salva: " + System.IO.Path.GetFileName(dialog.FileName), "good");
        }
        catch (Exception ex) { SetStatus("Planilha não salva: " + ex.Message, "warning"); }
    }

    // Resumo, tabela por estaca e, se houver, a tabela de cada lista de materiais do Civil 3D no mesmo trecho.
    internal static List<XlsxSheet> SectionSheets(SectionVolumeResult r, string drawing, string u)
    {
        var headers = new[]
        {
            "Estaca", "Estaca (m)", "Linha de amostragem", $"Área de corte ({u}²)", $"Área de aterro ({u}²)",
            $"Volume de corte ({u}³)", $"Volume de aterro ({u}³)", $"Corte acumulado ({u}³)", $"Aterro acumulado ({u}³)", "Observação"
        };
        var decimals = new[] { -1, 3, -1, 3, 3, 3, 3, 3, 3, -1 };
        static List<object?[]> Table(IReadOnlyList<SectionVolumeRow> rows, double cut, double fill)
        {
            var list = rows.Select(x => new object?[]
            {
                Estacas.Text(x.Station), x.Station, x.Name, x.CutArea, x.FillArea, x.CutVolume, x.FillVolume,
                x.CumulativeCut, x.CumulativeFill, x.Note
            }).ToList();
            list.Add(new object?[] { "Total", null, null, null, null, cut, fill, null, null, null });
            return list;
        }

        var summary = new List<object?[]>
        {
            new object?[] { "Desenho", drawing, null },
            new object?[] { "Data", DateTime.Now.ToString("dd/MM/yyyy HH:mm", Fmt.Br), null },
            new object?[] { "Grupo de seções", $"{r.AlignmentName} · {r.GroupName}", null },
            new object?[] { "Terreno natural (seção)", r.BaseName, null },
            new object?[] { "Projeto (seção)", r.ComparisonName, null },
            new object?[] { "Trecho", $"{Estacas.Text(r.Start)} a {Estacas.Text(r.End)}", null },
            new object?[] { "Seções no trecho", r.Rows.Count.ToString(Fmt.Br), null },
            new object?[] { "Método", "Áreas médias: V = (A1 + A2) / 2 × distância", null },
            new object?[] { "Corte", r.Cut, $"{u}³" },
            new object?[] { "Aterro", r.Fill, $"{u}³" },
            new object?[] { "Saldo (corte − aterro)", r.Cut - r.Fill, $"{u}³" },
            new object?[] { "Fator de corte", r.CutFactor, null },
            new object?[] { "Fator de aterro", r.FillFactor, null },
            new object?[] { "Corte com fator", r.Cut * r.CutFactor, $"{u}³" },
            new object?[] { "Aterro com fator", r.Fill * r.FillFactor, $"{u}³" },
            new object?[] { "Saldo com fatores", r.Cut * r.CutFactor - r.Fill * r.FillFactor, $"{u}³" },
        };
        foreach (var m in r.MaterialLists)
        {
            if (m.Problem is not null) { summary.Add(new object?[] { $"Civil 3D · {m.Name}", m.Problem, null }); continue; }
            summary.Add(new object?[] { $"Civil 3D · {m.Name} · corte (com fatores da lista)", m.Cut, $"{u}³" });
            summary.Add(new object?[] { $"Civil 3D · {m.Name} · aterro (com fatores da lista)", m.Fill, $"{u}³" });
            summary.Add(new object?[] { $"Civil 3D · {m.Name} · método e materiais", $"{m.Method}. {m.Factors}".Trim(' ', '.'), null });
        }
        var notes = new List<string>
        {
            "Corte: terreno acima do projeto. Aterro: projeto acima do terreno. A área só conta na largura em que as duas seções existem.",
            "Volumes na tabela por estaca sem fatores; os volumes com fatores estão neste Resumo."
        };
        notes.AddRange(r.Warnings.Select(w => "Aviso: " + w));

        var sheets = new List<XlsxSheet>
        {
            new("Resumo", new[] { "Item", "Valor", "Unidade" }, summary, new[] { -1, 3, -1 }, notes),
            new("Volumes por estaca", headers, Table(r.Rows, r.Cut, r.Fill), decimals,
                new[] { $"PROJETA · {drawing} · {r.AlignmentName} · {r.GroupName} · terreno: {r.BaseName} · projeto: {r.ComparisonName}" })
        };
        foreach (var m in r.MaterialLists.Where(m => m.Problem is null && m.Rows.Count > 0))
            sheets.Add(new XlsxSheet("Civil 3D - " + m.Name, headers, Table(m.Rows, m.Cut, m.Fill), decimals,
                new[] { $"Lista de materiais \"{m.Name}\" calculada pelo Civil 3D ({m.Method}). {m.Factors}".TrimEnd(' ', '.') + ".",
                        "Áreas e volumes como o Civil 3D calculou, no mesmo trecho de estacas do Resumo. " +
                        "As áreas vêm sem fatores; os volumes, com os fatores da lista." }));
        return sheets;
    }
}
