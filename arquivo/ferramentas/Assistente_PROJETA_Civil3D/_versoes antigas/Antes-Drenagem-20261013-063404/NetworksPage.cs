using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D;

// Aba Redes — IFM: esgoto (NBR 9649), escavação e quantitativos, renomear na ordem do escoamento e
// associar aos eixos, sobre as redes de tubulação do desenho. Calcular só lê; renomear, associar e
// gravar vazão pontual alteram o desenho depois de confirmação e podem ser desfeitos com U.
internal sealed class NetworksPage : ToolPage
{
    private const string SettingsKey = @"Software\PROJETA\AssistenteCivil\Redes";
    private static readonly string[] Modes = { "Esgoto (NBR 9649)", "Escavação e quantitativos", "Renomear na ordem do fluxo", "Associar aos eixos" };
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ComboBox network = Ui.Combo();
    private readonly ComboBox mode = Ui.Combo();
    // esgoto
    private readonly NumericUpDown popStart = Ui.Number(0m, 0m, 10_000_000m, 0, 100m);
    private readonly NumericUpDown popEnd = Ui.Number(0m, 0m, 10_000_000m, 0, 100m);
    private readonly NumericUpDown perCapita = Ui.Number(150m, 1m, 1000m, 1, 10m);
    private readonly NumericUpDown returnC = Ui.Number(0.80m, 0.01m, 1m, 2, 0.05m);
    private readonly NumericUpDown k1 = Ui.Number(1.20m, 1m, 3m, 2, 0.05m);
    private readonly NumericUpDown k2 = Ui.Number(1.50m, 1m, 3m, 2, 0.05m);
    private readonly NumericUpDown infiltration = Ui.Number(0.10m, 0m, 5m, 3, 0.05m);
    private readonly NumericUpDown minFlow = Ui.Number(1.50m, 0m, 100m, 2, 0.1m);
    private readonly NumericUpDown manning = Ui.Number(0.013m, 0.008m, 0.05m, 3, 0.001m);
    private readonly NumericUpDown maxRatio = Ui.Number(0.75m, 0.10m, 0.90m, 2, 0.05m);
    private readonly NumericUpDown minTractive = Ui.Number(1.00m, 0m, 10m, 2, 0.1m);
    private readonly NumericUpDown minCover = Ui.Number(0.90m, 0m, 10m, 2, 0.05m);
    private readonly NumericUpDown minDiameter = Ui.Number(150m, 50m, 3000m, 0, 50m);
    private readonly NumericUpDown maxVelocity = Ui.Number(5.00m, 0m, 20m, 2, 0.5m);
    private readonly NumericUpDown dropLimit = Ui.Number(0.50m, 0m, 20m, 2, 0.05m);
    // vala
    private readonly NumericUpDown clearance = Ui.Number(0.30m, 0m, 5m, 2, 0.05m);
    private readonly NumericUpDown minWidth = Ui.Number(0.60m, 0m, 10m, 2, 0.05m);
    private readonly NumericUpDown bedding = Ui.Number(0.10m, 0m, 2m, 2, 0.05m);
    private readonly NumericUpDown sideSlope = Ui.Number(0m, 0m, 5m, 2, 0.1m);
    private readonly NumericUpDown shoring = Ui.Number(1.25m, 0m, 20m, 2, 0.25m);
    // renomear
    private readonly TextBox pvPrefix = new() { Text = "PV-", Dock = DockStyle.Fill };
    private readonly TextBox blPrefix = new() { Text = "BL-", Dock = DockStyle.Fill };
    private readonly TextBox pipePrefix = new() { Text = "TR-", Dock = DockStyle.Fill };
    private readonly NumericUpDown first = Ui.Number(1m, 0m, 9999m, 0, 1m);
    private readonly NumericUpDown digits = Ui.Number(2m, 1m, 6m, 0, 1m);
    // associar
    private readonly CheckedListBox axes = new() { CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Height = 110, Dock = DockStyle.Fill };
    private readonly NumericUpDown maxOffset = Ui.Number(30m, 0.5m, 1000m, 2, 5m);

    private readonly TableLayoutPanel sewerBox, trenchBox, renameBox, associateBox;
    private readonly Button run = Ui.Button("Calcular", true);
    private readonly Button apply = Ui.Button("Aplicar no desenho…");
    private readonly Button pointFlow = Ui.Button("Vazão pontual…");
    private readonly Button export = Ui.Button("Exportar Excel…");
    private readonly Label details;
    private readonly Panel lists = new() { Dock = DockStyle.Fill };
    private readonly ThemedListView sewerList = new(new[] { "Trecho", "Qf (L/s)", "y/D", "V (m/s)", "σ (Pa)", "Situação" }, new[] { 22, 15, 12, 14, 13, 24 });
    private readonly ThemedListView trenchList = new(new[] { "Trecho", "Prof. (m)", "Larg. (m)", "Escav. (m³)", "Reat. (m³)", "Faixa" }, new[] { 19, 14, 13, 18, 17, 19 });
    private readonly ThemedListView renameList = new(new[] { "Tipo", "Nome atual", "Nome novo" }, new[] { 18, 41, 41 });
    private readonly ThemedListView associateList = new(new[] { "Peça", "Eixo", "Estaca", "Afast. (m)" }, new[] { 28, 32, 22, 18 });

    private NetworkRequest? lastRequest;
    private NetworkGraph? lastGraph;
    private SewerResult? lastSewer;
    private IReadOnlyList<TrenchRow>? lastTrench;
    private IReadOnlyList<QuantityRow>? lastQuantities;
    private IReadOnlyList<RenameRow>? lastRename;
    private IReadOnlyList<AssociationRow>? lastAssociation;
    private bool loading;

    public NetworksPage() : base("REDES — IFM",
        "Calcula esgoto pela NBR 9649, escavação de vala e quantitativos, renomeia na ordem do escoamento e associa aos eixos, sobre as redes de tubulação do desenho.")
    {
        AddRow("Rede", network);
        Ui.SetItems(mode, Modes.Select((m, i) => new Choice(m, i.ToString(CultureInfo.InvariantCulture))));
        mode.SelectedIndex = 0;
        AddRow("Função", mode);

        sewerBox = Box(
            ("População início de plano (hab)", popStart), ("População fim de plano (hab)", popEnd),
            "Distribuída pela extensão da rede (taxa linear). Vazões pontuais vêm da Vazão conhecida de cada estrutura.",
            ("Consumo per capita (L/hab·dia)", perCapita), ("Coef. de retorno C", returnC), ("K1 (dia de maior consumo)", k1), ("K2 (hora de maior consumo)", k2),
            ("Infiltração (L/s·km)", infiltration), "NBR 9649: infiltração de 0,05 a 1,0 L/s·km. Use os valores da concessionária quando houver.",
            ("Vazão mínima por trecho (L/s)", minFlow), ("n de Manning", manning), ("y/D máximo", maxRatio),
            "Acima da velocidade crítica (6·√(g·Rh)) o limite passa a 0,50.",
            ("Tensão trativa mínima (Pa)", minTractive), ("Recobrimento mínimo (m)", minCover), ("Diâmetro mínimo (mm)", minDiameter),
            "A NBR 9649 aceita DN 100; muitas concessionárias exigem 150.",
            ("Velocidade máxima (m/s)", maxVelocity), ("Tubo de queda a partir de (m)", dropLimit));
        trenchBox = Box(
            ("Folga lateral de cada lado (m)", clearance), ("Largura mínima da vala (m)", minWidth),
            "Largura = diâmetro externo + 2 × folga, no mínimo a largura mínima.",
            ("Berço/lastro sob o tubo (m)", bedding), ("Talude das paredes (H:V)", sideSlope), ("Escorar acima de (m)", shoring),
            "Profundidade da vala = terreno até a geratriz externa inferior + berço, nas duas pontas. Terreno: superfície de referência da rede ou, sem ela, a tampa das estruturas. Faixas de profundidade pela média do trecho.");
        renameBox = Box(
            ("Prefixo dos poços (PV)", pvPrefix), ("Prefixo das bocas de lobo", blPrefix), ("Prefixo dos tubos", pipePrefix),
            ("Primeiro número", first), ("Dígitos", digits),
            "Coletor mais longo primeiro, de montante para jusante; depois os ramais. Cada tubo leva o número da ordem da sua estrutura de montante.");
        associateBox = Box(("Afastamento máximo (m)", maxOffset),
            "Cada estrutura, e cada tubo pelo ponto médio, fica com o eixo marcado mais próximo. Peças mais longe que o máximo não mudam.");
        associateBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        associateBox.Controls.Add(axes, 0, associateBox.RowCount);
        associateBox.SetColumnSpan(axes, 2);
        associateBox.RowCount++;
        AddWide(sewerBox);
        AddWide(trenchBox);
        AddWide(renameBox);
        AddWide(associateBox);
        AddWide(Ui.Row(run, apply, pointFlow, export));
        details = Wrap("", "muted");
        AddWide(details);
        lists.Controls.AddRange(new Control[] { sewerList, trenchList, renameList, associateList });
        AddWide(lists, fill: true, minHeight: 200);

        mode.SelectedIndexChanged += (_, _) => ShowMode();
        network.SelectedIndexChanged += (_, _) =>
        {
            if (!loading && lastRequest is not null && (network.SelectedItem as Choice)?.Handle != lastRequest.NetworkHandle) Clear();
        };
        network.DropDown += (_, _) => LoadNetworks();
        run.Click += (_, _) => Run();
        apply.Click += (_, _) => Apply();
        pointFlow.Click += (_, _) => PointFlow();
        export.Click += (_, _) => Export();
        foreach (var list in new[] { sewerList, trenchList, renameList, associateList })
            list.SelectedIndexChanged += (_, _) => ShowDetails();
        LoadSettings();
        ShowMode();
    }

    private NetworkMode Mode => (NetworkMode)Math.Max(0, mode.SelectedIndex);

    // Bloco de parâmetros: linhas (rótulo, campo) e textos de ajuda.
    private TableLayoutPanel Box(params object[] items)
    {
        var box = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0) };
        box.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var item in items)
        {
            var row = box.RowCount++;
            box.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (item is string hint)
            {
                var label = Wrap(hint, "muted");
                label.Margin = new Padding(0, 0, 0, Px(6));
                box.Controls.Add(label, 0, row);
                box.SetColumnSpan(label, 2);
            }
            else if (item is ValueTuple<string, NumericUpDown> n) { box.Controls.Add(Ui.Label(n.Item1), 0, row); box.Controls.Add(n.Item2, 1, row); }
            else if (item is ValueTuple<string, TextBox> t) { box.Controls.Add(Ui.Label(t.Item1), 0, row); box.Controls.Add(t.Item2, 1, row); }
        }
        return box;
    }

    private void ShowMode()
    {
        var m = Mode;
        sewerBox.Visible = m == NetworkMode.Sewer;
        trenchBox.Visible = m == NetworkMode.Trench;
        renameBox.Visible = m == NetworkMode.Rename;
        associateBox.Visible = m == NetworkMode.Associate;
        sewerList.Visible = m == NetworkMode.Sewer;
        trenchList.Visible = m == NetworkMode.Trench;
        renameList.Visible = m == NetworkMode.Rename;
        associateList.Visible = m == NetworkMode.Associate;
        run.Visible = m != NetworkMode.Associate;
        run.Text = m == NetworkMode.Rename ? "Gerar prévia" : "Calcular";
        apply.Visible = m is NetworkMode.Rename or NetworkMode.Associate;
        apply.Text = m == NetworkMode.Rename ? "Renomear no desenho…" : "Associar no desenho…";
        pointFlow.Visible = m == NetworkMode.Sewer;
        UpdateButtons();
        details.Text = "";
    }

    private void UpdateButtons()
    {
        var m = Mode;
        export.Enabled = m switch
        {
            NetworkMode.Sewer => lastSewer is not null,
            NetworkMode.Trench => lastTrench is not null,
            NetworkMode.Rename => lastRename is not null,
            _ => lastAssociation is not null
        };
        apply.Enabled = m == NetworkMode.Associate || (lastRename?.Any(r => r.NewName != r.OldName) ?? false);
        pointFlow.Enabled = lastSewer is not null && sewerList.SelectedItems.Count == 1;
    }

    protected override void OnSnapshot(DrawingSnapshot? snapshot)
    {
        if (lastRequest is not null && (snapshot is null || !string.Equals(lastRequest.DrawingPath, snapshot.DrawingPath, StringComparison.OrdinalIgnoreCase)))
            Clear();
        var checkedAxes = axes.CheckedItems.OfType<Choice>().Select(c => c.Handle).ToHashSet();
        axes.Items.Clear();
        foreach (var a in snapshot?.Alignments ?? Array.Empty<AlignmentInfo>())
            axes.Items.Add(new Choice(a.Name, a.Handle), checkedAxes.Contains(a.Handle));
        LoadNetworks();
    }

    private void LoadNetworks()
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        IReadOnlyList<NetworkInfo> found = Array.Empty<NetworkInfo>();
        if (document is not null && Snapshot is not null && Actions.SameDrawing(document, Snapshot.DrawingPath))
        {
            try { using (document.LockDocument()) found = NetworkReader.List(document.Database); }
            catch { found = Array.Empty<NetworkInfo>(); }
        }
        loading = true;
        try
        {
            Ui.SetItems(network, found.Select(n => new Choice($"{n.Name} ({n.Structures} estr., {n.Pipes} tubos)", n.Handle)));
            if (network.SelectedIndex < 0 && network.Items.Count > 0) network.SelectedIndex = 0;
        }
        finally { loading = false; }
        if (found.Count == 0 && Snapshot is not null) details.Text = "Nenhuma rede de tubulação no desenho.";
    }

    private void Clear()
    {
        lastRequest = null; lastGraph = null; lastSewer = null; lastTrench = null; lastQuantities = null; lastRename = null; lastAssociation = null;
        foreach (var list in new[] { sewerList, trenchList, renameList, associateList }) list.Items.Clear();
        details.Text = "";
        UpdateButtons();
    }

    private NetworkRequest? Build()
    {
        if (Snapshot is null || Snapshot.Unit != "m") { SetStatus("Abra um desenho em metros e clique em Atualizar.", "warning"); return null; }
        if (network.SelectedItem is not Choice n) { SetStatus("Escolha a rede (o desenho precisa ter uma rede de tubulação).", "warning"); return null; }
        var sewer = new SewerOptions((double)popStart.Value, (double)popEnd.Value, (double)perCapita.Value, (double)returnC.Value,
            (double)k1.Value, (double)k2.Value, (double)infiltration.Value, (double)minFlow.Value, (double)manning.Value,
            (double)maxRatio.Value, (double)minTractive.Value, (double)minCover.Value, (double)minDiameter.Value / 1000,
            (double)maxVelocity.Value, (double)dropLimit.Value);
        var trench = new TrenchOptions((double)clearance.Value, (double)minWidth.Value, (double)bedding.Value, (double)sideSlope.Value, (double)shoring.Value);
        var rename = new RenameOptions(pvPrefix.Text.Trim(), blPrefix.Text.Trim(), pipePrefix.Text.Trim(), (int)first.Value, (int)digits.Value);
        return new NetworkRequest(Snapshot.DrawingPath, n.Handle, Mode, sewer, trench, rename,
            axes.CheckedItems.OfType<Choice>().Select(c => c.Handle).ToArray(), (double)maxOffset.Value);
    }

    private void Run()
    {
        var request = Build();
        if (request is null) return;
        SaveSettings();
        NetworkPending.Calc = request;
        if (!Actions.Send("PROJETAREDES", this)) NetworkPending.Calc = null;
    }

    private void Apply()
    {
        if (Mode == NetworkMode.Rename)
        {
            if (lastRequest is null || lastRename is null) { SetStatus("Gere a prévia primeiro.", "warning"); return; }
            var changes = lastRename.Count(r => r.NewName != r.OldName);
            if (!Ui.Confirm(this, "RENOMEAR REDE", $"Renomear {changes} peça(s) da rede {lastGraph?.Data.Name} conforme a prévia?")) return;
            NetworkPending.Rename = new RenameApply(lastRequest, lastRename);
            if (!Actions.Send("PROJETAREDESRENOMEAR", this)) NetworkPending.Rename = null;
            return;
        }
        var request = Build();
        if (request is null) return;
        if (request.Alignments.Count == 0) { SetStatus("Marque os eixos para associar.", "warning"); return; }
        if (!Ui.Confirm(this, "ASSOCIAR AOS EIXOS",
            $"Associar as estruturas e tubos da rede ao eixo marcado mais próximo ({request.Alignments.Count} eixo(s)), até {Fmt.N(request.MaxOffset)} m de afastamento?")) return;
        SaveSettings();
        NetworkPending.Associate = request;
        if (!Actions.Send("PROJETAREDESASSOCIAR", this)) NetworkPending.Associate = null;
    }

    private void PointFlow()
    {
        if (lastRequest is null || sewerList.SelectedItems.Count != 1 || sewerList.SelectedItems[0].Tag is not SewerRow row) return;
        var handle = row.Reach.Up;
        if (handle is null || lastGraph is null || !lastGraph.Structures.TryGetValue(handle, out var s)) { SetStatus("O trecho não tem estrutura de montante.", "warning"); return; }
        var value = AskNumber("Vazão pontual", $"Vazão pontual em {s.Name} (L/s), somada no início e no fim de plano:", s.KnownFlow * 1000);
        if (value is null) return;
        var request = Build() ?? lastRequest;
        NetworkPending.PointFlow = new PointFlowRequest(request with { Mode = NetworkMode.Sewer }, handle, value.Value);
        if (!Actions.Send("PROJETAREDESPONTUAL", this)) NetworkPending.PointFlow = null;
    }

    private double? AskNumber(string title, string text, double value)
    {
        using var form = new Form
        {
            Text = "PROJETA — " + title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(Px(12)), Font = Font
        };
        var number = Ui.Number((decimal)Math.Max(0, value), 0m, 100000m, 3, 0.5m);
        number.Width = Px(140);
        number.Dock = DockStyle.None;
        var ok = Ui.Button("OK", true);
        var cancel = Ui.Button("Cancelar");
        ok.DialogResult = DialogResult.OK;
        cancel.DialogResult = DialogResult.Cancel;
        var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = text, AutoSize = true, MaximumSize = new System.Drawing.Size(Px(360), 0), Margin = new Padding(0, 0, 0, Px(6)) });
        layout.Controls.Add(number);
        layout.Controls.Add(Ui.Row(ok, cancel));
        form.Controls.Add(layout);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        Theme.Apply(form);
        return form.ShowDialog(this) == DialogResult.OK ? (double)number.Value : null;
    }

    public void AddNote(string text) => details.Text = text;

    // ------------------------------------------------------------ resultados

    public void ShowSewer(NetworkRequest request, NetworkGraph g, SewerResult result)
    {
        lastRequest = request; lastGraph = g; lastSewer = result;
        SwitchTo(NetworkMode.Sewer);
        sewerList.BeginUpdate();
        sewerList.Items.Clear();
        foreach (var r in result.Rows)
            sewerList.Items.Add(new ListViewItem(new[] { r.Reach.Pipe.Name, Fmt.N(r.QEndUsed), Fmt.N(r.RatioEnd), Fmt.N(r.VelocityEnd), Fmt.N(r.Tractive), r.Ok ? "OK" : "Revisar" })
                { Tag = r, ToolTipText = r.Ok ? "" : r.Notes });
        sewerList.EndUpdate();
        var bad = result.Rows.Count(r => !r.Ok);
        var text = $"{g.Data.Name}: {result.Rows.Count} trecho(s), {Fmt.N(result.TotalLength)} m. " +
                   $"Contribuição linear: início {Fmt.N(result.RateStart * 1000, 3)} e fim {Fmt.N(result.RateEnd * 1000, 3)} L/s·km, mais infiltração de {Fmt.N(result.Infiltration * 1000, 3)} L/s·km. " +
                   (bad == 0 ? "Todos os trechos atendem aos critérios informados." : $"{bad} trecho(s) para revisar: selecione para ver o motivo.") +
                   (result.Problems.Count > 0 ? "\n" + string.Join("\n", result.Problems.Take(6)) : "") +
                   (request.Sewer.PopStart == 0 && request.Sewer.PopEnd == 0 ? "\nPopulação zero: só infiltração, vazões pontuais e a vazão mínima." : "");
        ShowOutcome(text, bad == 0 && result.Problems.Count == 0);
        UpdateButtons();
    }

    public void ShowTrench(NetworkRequest request, NetworkGraph g, IReadOnlyList<TrenchRow> rows, IReadOnlyList<QuantityRow> quantities)
    {
        lastRequest = request; lastGraph = g; lastTrench = rows; lastQuantities = quantities;
        SwitchTo(NetworkMode.Trench);
        trenchList.BeginUpdate();
        trenchList.Items.Clear();
        foreach (var r in rows)
            trenchList.Items.Add(new ListViewItem(new[] { r.Reach.Pipe.Name, Fmt.N(r.DepthMean), Fmt.N(r.Width), Fmt.N(r.Excavation), Fmt.N(r.Backfill), r.Band })
                { Tag = r, ToolTipText = r.Notes });
        trenchList.EndUpdate();
        var bands = rows.Where(r => r.DepthMean.HasValue).GroupBy(r => r.Band).OrderBy(b => Array.FindIndex(Trench.Bands, x => x.Name == b.Key))
            .Select(b => $"{b.Key}: {Fmt.N(b.Sum(r => r.Excavation))} m³");
        var pipes = quantities.Where(q => q.Group == "Tubos").Select(q => $"{q.Detail}: {Fmt.N(q.Extra)} m");
        var missing = rows.Count(r => !r.DepthMean.HasValue);
        var text = $"{g.Data.Name}: escavação {Fmt.N(rows.Sum(r => r.Excavation))} m³, reaterro {Fmt.N(rows.Sum(r => r.Backfill))} m³, " +
                   $"berço {Fmt.N(rows.Sum(r => r.BeddingVolume))} m³, escoramento {Fmt.N(rows.Sum(r => r.ShoringArea))} m².\n" +
                   "Por faixa de profundidade: " + string.Join("; ", bands) + ".\n" +
                   "Tubos (eixo a eixo): " + string.Join("; ", pipes) + $". Estruturas: {g.Structures.Count}." +
                   (missing > 0 ? $"\n{missing} trecho(s) sem cota de terreno: defina a superfície de referência da rede." : "");
        ShowOutcome(text, missing == 0);
        UpdateButtons();
    }

    public void ShowRename(NetworkRequest request, NetworkGraph g, IReadOnlyList<RenameRow> plan)
    {
        lastRequest = request; lastGraph = g; lastRename = plan;
        SwitchTo(NetworkMode.Rename);
        renameList.BeginUpdate();
        renameList.Items.Clear();
        foreach (var r in plan)
        {
            var kind = r.IsPipe ? "Tubo" : g.Structures.TryGetValue(r.Handle, out var s) ? s.Kind : "Estrutura";
            renameList.Items.Add(new ListViewItem(new[] { kind, r.OldName, r.NewName }) { Tag = r });
        }
        renameList.EndUpdate();
        var changes = plan.Count(r => r.NewName != r.OldName);
        ShowOutcome(changes == 0 ? $"{g.Data.Name}: os nomes já seguem a ordem do escoamento."
            : $"{g.Data.Name}: {changes} de {plan.Count} peça(s) mudam de nome. Revise e clique em Renomear no desenho." +
              (g.Problems.Count > 0 ? "\n" + string.Join("\n", g.Problems.Take(4)) : ""), g.Problems.Count == 0);
        UpdateButtons();
    }

    public void ShowAssociation(NetworkRequest request, IReadOnlyList<AssociationRow> rows, string message)
    {
        lastRequest = request; lastAssociation = rows;
        SwitchTo(NetworkMode.Associate);
        associateList.BeginUpdate();
        associateList.Items.Clear();
        foreach (var r in rows)
            associateList.Items.Add(new ListViewItem(new[] { r.Part, r.Alignment, Estaca(r.Station), Fmt.N(r.Offset) }) { Tag = r });
        associateList.EndUpdate();
        ShowOutcome(message, rows.All(r => r.Station.HasValue));
        UpdateButtons();
    }

    private void SwitchTo(NetworkMode m)
    {
        if (Mode != m) mode.SelectedIndex = (int)m;
    }

    private void ShowDetails()
    {
        UpdateButtons();
        var item = Mode switch
        {
            NetworkMode.Sewer => sewerList.SelectedItems.Count == 1 ? sewerList.SelectedItems[0].Tag : null,
            NetworkMode.Trench => trenchList.SelectedItems.Count == 1 ? trenchList.SelectedItems[0].Tag : null,
            _ => null
        };
        details.Text = item switch
        {
            SewerRow r => $"{r.Reach.Pipe.Name}: {r.Up} → {r.Down} · L {Fmt.N(r.Length)} m · DN interno {Fmt.N(r.Diameter * 1000, 0)} mm · " +
                          $"I {Fmt.N(r.Slope, 4)} m/m (mínima {Fmt.N(r.MinSlope, 4)}) · Qi {Fmt.N(r.QStartUsed)} / Qf {Fmt.N(r.QEndUsed)} L/s " +
                          $"(calculadas {Fmt.N(r.QStart)} / {Fmt.N(r.QEnd)}) · y/D {Fmt.N(r.RatioStart)} / {Fmt.N(r.RatioEnd)} · " +
                          $"V {Fmt.N(r.VelocityStart)} / {Fmt.N(r.VelocityEnd)} m/s (crítica {Fmt.N(r.CriticalVelocity)}) · σ {Fmt.N(r.Tractive)} Pa · " +
                          $"recobrimento {Fmt.N(r.CoverUp)} / {Fmt.N(r.CoverDown)} m" + (r.Ok ? "" : "\nRevisar: " + r.Notes),
            TrenchRow t => $"{t.Reach.Pipe.Name}: {t.Up} → {t.Down} · extensão {Fmt.N(t.Length)} m · DE {Fmt.N(t.OuterDiameter * 1000, 0)} mm · largura {Fmt.N(t.Width)} m · " +
                           $"profundidade {Fmt.N(t.DepthUp)} / {Fmt.N(t.DepthDown)} m · escavação {Fmt.N(t.Excavation)} m³ · berço {Fmt.N(t.BeddingVolume)} m³ · " +
                           $"tubo {Fmt.N(t.PipeVolume)} m³ · reaterro {Fmt.N(t.Backfill)} m³ · escoramento {Fmt.N(t.ShoringArea)} m²" + (t.Notes.Length > 0 ? "\n" + t.Notes : ""),
            _ => details.Text
        };
    }

    // ------------------------------------------------------------ Excel

    private void Export()
    {
        if (lastRequest is null) return;
        var m = Mode;
        var sheets = m switch
        {
            NetworkMode.Sewer when lastSewer is not null => SewerSheets(lastRequest, lastSewer),
            NetworkMode.Trench when lastTrench is not null && lastQuantities is not null => TrenchSheets(lastRequest, lastTrench, lastQuantities),
            NetworkMode.Rename when lastRename is not null => new List<XlsxSheet>
            {
                new("Renomear", new[] { "Tipo", "Nome atual", "Nome novo" },
                    lastRename.Select(r => new object?[] { r.IsPipe ? "Tubo" : lastGraph?.Structures.GetValueOrDefault(r.Handle)?.Kind ?? "Estrutura", r.OldName, r.NewName }).ToList(),
                    new[] { -1, -1, -1 })
            },
            NetworkMode.Associate when lastAssociation is not null => new List<XlsxSheet>
            {
                new("Associação", new[] { "Peça", "Tipo", "Eixo", "Estaca", "Estaca (m)", "Afastamento (m)" },
                    lastAssociation.Select(r => new object?[] { r.Part, r.Kind, r.Alignment, r.Station.HasValue ? Estaca(r.Station) : null, r.Station, r.Offset }).ToList(),
                    new[] { -1, -1, -1, -1, 3, 3 })
            },
            _ => null
        };
        if (sheets is null) return;
        var drawing = lastRequest.DrawingPath;
        var folder = Path.IsPathRooted(drawing) && Directory.Exists(Path.GetDirectoryName(drawing)) ? Path.GetDirectoryName(drawing)!
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var label = m switch { NetworkMode.Sewer => "Esgoto", NetworkMode.Trench => "Escavação", NetworkMode.Rename => "Renomear", _ => "Associação" };
        var name = $"{Path.GetFileNameWithoutExtension(drawing)} - {lastGraph?.Data.Name} - {label}.xlsx";
        name = string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c));
        using var dialog = new System.Windows.Forms.SaveFileDialog { Filter = "Planilha do Excel (*.xlsx)|*.xlsx", InitialDirectory = folder, FileName = name, OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            Xlsx.Write(dialog.FileName, sheets);
            SetStatus("Planilha salva: " + dialog.FileName, "good");
        }
        catch (Exception ex) { SetStatus("Planilha não salva: " + ex.Message, "warning"); }
    }

    private static List<XlsxSheet> SewerSheets(NetworkRequest q, SewerResult r)
    {
        var o = q.Sewer;
        var rows = r.Rows.Select(x => new object?[]
        {
            x.Reach.Pipe.Name, x.Up, x.Down, x.Length, x.Diameter * 1000, x.Slope, x.MinSlope, x.QStart, x.QEnd, x.QStartUsed, x.QEndUsed,
            x.RatioStart, x.RatioEnd, x.VelocityStart, x.VelocityEnd, x.CriticalVelocity, x.Tractive,
            x.Reach.UpInvert, x.Reach.DownInvert, x.Reach.UpGround, x.Reach.DownGround, x.CoverUp, x.CoverDown, x.Ok ? "OK" : "Revisar", x.Notes
        }).ToList();
        var headers = new[]
        {
            "Trecho", "Montante", "Jusante", "Extensão (m)", "DN interno (mm)", "Declividade (m/m)", "Decl. mínima (m/m)", "Qi calculada (L/s)", "Qf calculada (L/s)",
            "Qi adotada (L/s)", "Qf adotada (L/s)", "y/D início", "y/D fim", "V início (m/s)", "V fim (m/s)", "V crítica (m/s)", "Tensão trativa (Pa)",
            "GI montante (m)", "GI jusante (m)", "Terreno montante (m)", "Terreno jusante (m)", "Recobr. montante (m)", "Recobr. jusante (m)", "Situação", "Observações"
        };
        var decimals = new[] { -1, -1, -1, 2, 0, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 2, 2, -1, -1 };
        var notes = new List<string>
        {
            "Vazões: Qi = C·K2·Pi·q/86400 e Qf = C·K1·K2·Pf·q/86400 distribuídas pela extensão, mais infiltração e vazões pontuais (Vazão conhecida das estruturas).",
            "Lâmina e velocidade com a vazão final; tensão trativa com a vazão inicial (γ = 10⁴ N/m³). Vazão mínima de cálculo aplicada a cada trecho.",
            "Calculado pelo PROJETA — Assistente Civil. Confira com os critérios da concessionária antes de usar no projeto."
        };
        if (r.Problems.Count > 0) notes.AddRange(r.Problems.Select(p => "Atenção: " + p));
        var parameters = new List<object?[]>
        {
            new object?[] { "População início de plano", o.PopStart, "hab" }, new object?[] { "População fim de plano", o.PopEnd, "hab" },
            new object?[] { "Consumo per capita", o.PerCapita, "L/hab·dia" }, new object?[] { "Coeficiente de retorno C", o.Return, "" },
            new object?[] { "K1", o.K1, "" }, new object?[] { "K2", o.K2, "" }, new object?[] { "Infiltração", o.Infiltration, "L/s·km" },
            new object?[] { "Vazão mínima por trecho", o.MinFlow, "L/s" }, new object?[] { "n de Manning", o.Manning, "" },
            new object?[] { "y/D máximo", o.MaxRatio, "" }, new object?[] { "Tensão trativa mínima", o.MinTractive, "Pa" },
            new object?[] { "Recobrimento mínimo", o.MinCover, "m" }, new object?[] { "Diâmetro mínimo", o.MinDiameter * 1000, "mm" },
            new object?[] { "Velocidade máxima", o.MaxVelocity, "m/s" }, new object?[] { "Tubo de queda a partir de", o.DropLimit, "m" },
            new object?[] { "Extensão total da rede", r.TotalLength, "m" }, new object?[] { "Taxa linear início (doméstica)", r.RateStart * 1000, "L/s·km" },
            new object?[] { "Taxa linear fim (doméstica)", r.RateEnd * 1000, "L/s·km" }
        };
        return new List<XlsxSheet>
        {
            new("Esgoto", headers, rows, decimals, notes),
            new("Parâmetros", new[] { "Parâmetro", "Valor", "Unidade" }, parameters, new[] { -1, 3, -1 })
        };
    }

    private static List<XlsxSheet> TrenchSheets(NetworkRequest q, IReadOnlyList<TrenchRow> rows, IReadOnlyList<QuantityRow> quantities)
    {
        var o = q.Trench;
        var data = rows.Select(x => new object?[]
        {
            x.Reach.Pipe.Name, x.Up, x.Down, x.Length, x.OuterDiameter * 1000, x.Width, x.DepthUp, x.DepthDown, x.DepthMean, x.Band,
            x.Excavation, x.BeddingVolume, x.PipeVolume, x.Backfill, x.ShoringArea, x.Notes
        }).ToList();
        data.Add(new object?[] { "Total", null, null, rows.Sum(x => x.Length), null, null, null, null, null, null,
            rows.Sum(x => x.Excavation), rows.Sum(x => x.BeddingVolume), rows.Sum(x => x.PipeVolume), rows.Sum(x => x.Backfill), rows.Sum(x => x.ShoringArea), null });
        var bands = Trench.Bands.Select(b => b.Name).Select(name => new object?[]
        {
            name, rows.Where(x => x.Band == name).Sum(x => x.Length), rows.Where(x => x.Band == name).Sum(x => x.Excavation),
            rows.Where(x => x.Band == name).Sum(x => x.Backfill), rows.Where(x => x.Band == name).Sum(x => x.ShoringArea)
        }).Where(b => (double)b[1]! > 0).ToList();
        var pipes = quantities.Where(x => x.Group == "Tubos").Select(x => new object?[] { x.Item, x.Detail, x.Count, x.Length, x.Extra }).ToList();
        var structures = quantities.Where(x => x.Group == "Estruturas").Select(x => new object?[] { x.Item, x.Detail, x.Count, x.Extra }).ToList();
        var notes = new List<string>
        {
            $"Largura = diâmetro externo + 2 × {Fmt.N(o.SideClearance)} m (mínimo {Fmt.N(o.MinWidth)} m); berço {Fmt.N(o.Bedding)} m; talude {Fmt.N(o.SideSlope)} (H:V); escoramento acima de {Fmt.N(o.ShoringDepth)} m.",
            "Profundidade: terreno até a geratriz externa inferior mais o berço, nas pontas; volume pela variação linear da profundidade entre as pontas. Faixa pela profundidade média.",
            "Extensão eixo a eixo das estruturas. Reaterro = escavação − tubo − berço. Calculado pelo PROJETA — Assistente Civil."
        };
        return new List<XlsxSheet>
        {
            new("Escavação", new[] { "Trecho", "Montante", "Jusante", "Extensão (m)", "DE (mm)", "Largura (m)", "Prof. montante (m)", "Prof. jusante (m)",
                    "Prof. média (m)", "Faixa", "Escavação (m³)", "Berço (m³)", "Tubo (m³)", "Reaterro (m³)", "Escoramento (m²)", "Observações" },
                data, new[] { -1, -1, -1, 2, 0, 2, 2, 2, 2, -1, 2, 2, 2, 2, 2, -1 }, notes),
            new("Por profundidade", new[] { "Faixa", "Extensão (m)", "Escavação (m³)", "Reaterro (m³)", "Escoramento (m²)" }, bands, new[] { -1, 2, 2, 2, 2 }),
            new("Tubos", new[] { "Tubo", "Detalhe", "Quantidade", "Comprimento 3D (m)", "Extensão eixo a eixo (m)" }, pipes, new[] { -1, -1, 0, 2, 2 }),
            new("Estruturas", new[] { "Tipo · descrição", "Altura (tampa ao fundo)", "Quantidade", "Soma das alturas (m)" }, structures, new[] { -1, -1, 0, 2 })
        };
    }

    // Estaca de 20 m: 245,6 → "12+5,600".
    private static string Estaca(double? station)
    {
        if (station is not double s || !double.IsFinite(s)) return "—";
        var n = Math.Floor(s / 20 + 1e-9);
        var rest = s - n * 20;
        return $"{n:0}+{rest.ToString("0.000", Br)}";
    }

    // ------------------------------------------------------------ parâmetros lembrados (por usuário)

    private IEnumerable<(string Name, NumericUpDown Box)> Numbers() => new[]
    {
        ("PopInicio", popStart), ("PopFim", popEnd), ("PerCapita", perCapita), ("Retorno", returnC), ("K1", k1), ("K2", k2),
        ("Infiltracao", infiltration), ("VazaoMinima", minFlow), ("Manning", manning), ("LaminaMax", maxRatio), ("TensaoMin", minTractive),
        ("RecobrimentoMin", minCover), ("DiametroMin", minDiameter), ("VelocidadeMax", maxVelocity), ("TuboQueda", dropLimit),
        ("Folga", clearance), ("LarguraMin", minWidth), ("Berco", bedding), ("Talude", sideSlope), ("Escorar", shoring),
        ("Primeiro", first), ("Digitos", digits), ("AfastMax", maxOffset)
    };

    private void LoadSettings()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKey);
            if (key is null) return;
            foreach (var (name, box) in Numbers())
                if (key.GetValue(name) is string text && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                    box.Value = Math.Min(box.Maximum, Math.Max(box.Minimum, v));
            foreach (var (name, box) in new[] { ("PrefixoPV", pvPrefix), ("PrefixoBL", blPrefix), ("PrefixoTubo", pipePrefix) })
                if (key.GetValue(name) is string text) box.Text = text;
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKey);
            foreach (var (name, box) in Numbers()) key.SetValue(name, box.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var (name, box) in new[] { ("PrefixoPV", pvPrefix), ("PrefixoBL", blPrefix), ("PrefixoTubo", pipePrefix) }) key.SetValue(name, box.Text);
        }
        catch { }
    }
}
