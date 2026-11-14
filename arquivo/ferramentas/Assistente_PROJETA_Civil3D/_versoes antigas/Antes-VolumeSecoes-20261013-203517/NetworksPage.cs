using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D;

// Aba Redes — IFM: esgoto (NBR 9649), drenagem pluvial (método racional), escavação e quantitativos,
// renomear na ordem do escoamento e associar aos eixos, sobre as redes de tubulação do desenho.
// Calcular só lê; renomear, associar, gravar vazão pontual, áreas de contribuição e vazões nos tubos
// alteram o desenho depois de confirmação e podem ser desfeitos com U.
internal sealed class NetworksPage : ToolPage
{
    private const string SettingsKey = @"Software\PROJETA\AssistenteCivil\Redes";
    // Mesma ordem de NetworkMode.
    private static readonly string[] Modes = { "Esgoto (NBR 9649)", "Drenagem pluvial (método racional)", "Escavação e quantitativos", "Renomear na ordem do fluxo", "Associar aos eixos" };
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
    // drenagem pluvial
    private readonly NumericUpDown returnPeriod = Ui.Number(10m, 1m, 10000m, 0, 5m);
    private readonly NumericUpDown inletTime = Ui.Number(10m, 1m, 1440m, 1, 1m);
    private readonly NumericUpDown idfK = Ui.Number(0m, 0m, 100000m, 3, 10m);
    private readonly NumericUpDown idfA = Ui.Number(0m, 0m, 2m, 4, 0.01m);
    private readonly NumericUpDown idfB = Ui.Number(0m, 0m, 200m, 3, 1m);
    private readonly NumericUpDown idfC = Ui.Number(0m, 0m, 3m, 4, 0.01m);
    private readonly TextBox idfSource = new() { Dock = DockStyle.Fill };
    private readonly TextBox city = new() { Dock = DockStyle.Fill };
    private readonly Button findStudy = Ui.Button("Estudos de chuva do SGB…");
    private readonly NumericUpDown stormManning = Ui.Number(0.015m, 0.008m, 0.05m, 3, 0.001m);
    private readonly NumericUpDown stormRatio = Ui.Number(0.85m, 0.10m, 0.93m, 2, 0.05m);
    private readonly NumericUpDown stormVmin = Ui.Number(0.60m, 0m, 10m, 2, 0.05m);
    private readonly NumericUpDown stormVmax = Ui.Number(5.00m, 0m, 20m, 2, 0.5m);
    private readonly NumericUpDown stormCover = Ui.Number(1.00m, 0m, 10m, 2, 0.05m);
    private readonly NumericUpDown stormDmin = Ui.Number(400m, 100m, 3000m, 0, 50m);
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

    private readonly TableLayoutPanel sewerBox, stormBox, trenchBox, renameBox, associateBox;
    private readonly Button run = Ui.Button("Calcular", true);
    private readonly Button apply = Ui.Button("Aplicar no desenho…");
    private readonly Button pointFlow = Ui.Button("Vazão pontual…");
    private readonly Button areas = Ui.Button("Áreas de contribuição…");
    private readonly Button export = Ui.Button("Exportar Excel…");
    private readonly Label details;
    private readonly Panel lists = new() { Dock = DockStyle.Fill };
    private readonly ThemedListView sewerList = new(new[] { "Trecho", "Qf (L/s)", "y/D", "V (m/s)", "σ (Pa)", "Situação" }, new[] { 22, 15, 12, 14, 13, 24 });
    private readonly ThemedListView stormList = new(new[] { "Trecho", "A (ha)", "tc (min)", "Q (L/s)", "y/D", "V (m/s)", "Situação" }, new[] { 17, 14, 13, 15, 10, 12, 19 });
    private readonly ThemedListView trenchList = new(new[] { "Trecho", "Prof. (m)", "Larg. (m)", "Escav. (m³)", "Reat. (m³)", "Faixa" }, new[] { 19, 14, 13, 18, 17, 19 });
    private readonly ThemedListView renameList = new(new[] { "Tipo", "Nome atual", "Nome novo" }, new[] { 18, 41, 41 });
    private readonly ThemedListView associateList = new(new[] { "Peça", "Eixo", "Estaca", "Afast. (m)" }, new[] { 28, 32, 22, 18 });

    private NetworkRequest? lastRequest;
    private NetworkGraph? lastGraph;
    private SewerResult? lastSewer;
    private StormResult? lastStorm;
    private IReadOnlyList<TrenchRow>? lastTrench;
    private IReadOnlyList<QuantityRow>? lastQuantities;
    private IReadOnlyList<RenameRow>? lastRename;
    private IReadOnlyList<AssociationRow>? lastAssociation;
    private bool loading;

    public NetworksPage() : base("REDES — IFM",
        "Calcula esgoto pela NBR 9649 e drenagem pluvial pelo método racional, escavação de vala e quantitativos, renomeia na ordem do escoamento e associa aos eixos, sobre as redes de tubulação do desenho.")
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
        stormBox = Box(
            ("Período de retorno TR (anos)", returnPeriod), ("Tempo de entrada (min)", inletTime),
            "Tempo de concentração inicial das estruturas sem tc próprio. A jusante, tc = maior tempo que chega (tc de montante + percurso no tubo).",
            "Chuva intensa (IDF): i = K·TR^a / (t + b)^c, com i em mm/h e t em minutos.",
            ("K", idfK), ("a", idfA), ("b", idfB), ("c", idfC), ("Fonte da equação", idfSource),
            ("Município (UF)", city), findStudy,
            "Lista os estudos de chuvas intensas do Serviço Geológico do Brasil (SGB) para o município e abre o estudo para conferir K, a, b e c.",
            ("n de Manning", stormManning), ("y/D máximo", stormRatio), ("Velocidade mínima (m/s)", stormVmin), ("Velocidade máxima (m/s)", stormVmax),
            ("Recobrimento mínimo (m)", stormCover), ("Diâmetro mínimo (mm)", stormDmin),
            "Confira os limites no manual de drenagem da prefeitura ou do órgão contratante.",
            "Área, C e tc de cada estrutura vêm da guia Áreas de contribuição das propriedades da estrutura no Civil 3D (ou das bacias ligadas a ela). Para editar todas de uma vez, use Áreas de contribuição…; aceita colar do Excel.");
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
        AddWide(stormBox);
        AddWide(trenchBox);
        AddWide(renameBox);
        AddWide(associateBox);
        AddWide(Ui.Row(run, apply, pointFlow, areas, export));
        details = Wrap("", "muted");
        AddWide(details);
        lists.Controls.AddRange(new Control[] { sewerList, stormList, trenchList, renameList, associateList });
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
        areas.Click += (_, _) => EditAreas();
        findStudy.Click += (_, _) => FindStudy();
        export.Click += (_, _) => Export();
        foreach (var list in new[] { sewerList, stormList, trenchList, renameList, associateList })
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
            else if (item is System.Runtime.CompilerServices.ITuple { Length: 2 } t && t[0] is string text && t[1] is Control field)
            {
                box.Controls.Add(Ui.Label(text), 0, row);
                box.Controls.Add(field, 1, row);
            }
            else if (item is Control wide)
            {
                box.Controls.Add(wide, 0, row);
                box.SetColumnSpan(wide, 2);
            }
        }
        return box;
    }

    private void ShowMode()
    {
        var m = Mode;
        sewerBox.Visible = m == NetworkMode.Sewer;
        stormBox.Visible = m == NetworkMode.Storm;
        trenchBox.Visible = m == NetworkMode.Trench;
        renameBox.Visible = m == NetworkMode.Rename;
        associateBox.Visible = m == NetworkMode.Associate;
        sewerList.Visible = m == NetworkMode.Sewer;
        stormList.Visible = m == NetworkMode.Storm;
        trenchList.Visible = m == NetworkMode.Trench;
        renameList.Visible = m == NetworkMode.Rename;
        associateList.Visible = m == NetworkMode.Associate;
        run.Visible = m != NetworkMode.Associate;
        run.Text = m == NetworkMode.Rename ? "Gerar prévia" : "Calcular";
        apply.Visible = m is NetworkMode.Rename or NetworkMode.Associate or NetworkMode.Storm;
        apply.Text = m switch { NetworkMode.Rename => "Renomear no desenho…", NetworkMode.Storm => "Gravar vazões nos tubos…", _ => "Associar no desenho…" };
        pointFlow.Visible = m == NetworkMode.Sewer;
        areas.Visible = m == NetworkMode.Storm;
        UpdateButtons();
        details.Text = "";
    }

    private void UpdateButtons()
    {
        var m = Mode;
        export.Enabled = m switch
        {
            NetworkMode.Sewer => lastSewer is not null,
            NetworkMode.Storm => lastStorm is not null,
            NetworkMode.Trench => lastTrench is not null,
            NetworkMode.Rename => lastRename is not null,
            _ => lastAssociation is not null
        };
        apply.Enabled = m switch
        {
            NetworkMode.Associate => true,
            NetworkMode.Storm => lastStorm is not null,
            _ => lastRename?.Any(r => r.NewName != r.OldName) ?? false
        };
        pointFlow.Enabled = lastSewer is not null && sewerList.SelectedItems.Count == 1;
        areas.Enabled = lastGraph is not null;
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
        lastRequest = null; lastGraph = null; lastSewer = null; lastStorm = null; lastTrench = null; lastQuantities = null; lastRename = null; lastAssociation = null;
        foreach (var list in new[] { sewerList, stormList, trenchList, renameList, associateList }) list.Items.Clear();
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
        var storm = new StormOptions((double)returnPeriod.Value, (double)inletTime.Value, (double)idfK.Value, (double)idfA.Value,
            (double)idfB.Value, (double)idfC.Value, (double)stormManning.Value, (double)stormRatio.Value, (double)stormVmin.Value,
            (double)stormVmax.Value, (double)stormCover.Value, (double)stormDmin.Value / 1000, idfSource.Text.Trim());
        var trench = new TrenchOptions((double)clearance.Value, (double)minWidth.Value, (double)bedding.Value, (double)sideSlope.Value, (double)shoring.Value);
        var rename = new RenameOptions(pvPrefix.Text.Trim(), blPrefix.Text.Trim(), pipePrefix.Text.Trim(), (int)first.Value, (int)digits.Value);
        return new NetworkRequest(Snapshot.DrawingPath, n.Handle, Mode, sewer, storm, trench, rename,
            axes.CheckedItems.OfType<Choice>().Select(c => c.Handle).ToArray(), (double)maxOffset.Value);
    }

    private void Run()
    {
        var request = Build();
        if (request is null) return;
        SaveSettings();
        if (request.Mode == NetworkMode.Storm && (request.Storm.K <= 0 || request.Storm.C <= 0))
        {
            SetStatus("Informe a equação de chuva (K, a, b e c). Estudos de chuva do SGB… mostra o estudo do município.", "warning");
            return;
        }
        NetworkPending.Calc = request;
        if (!Actions.Send("PROJETAREDES", this)) NetworkPending.Calc = null;
    }

    private void Apply()
    {
        if (Mode == NetworkMode.Storm)
        {
            var input = Build();
            if (input is null || lastStorm is null) return;
            if (!Ui.Confirm(this, "GRAVAR VAZÕES", $"Gravar a vazão de projeto calculada em {lastStorm.Rows.Count} tubo(s) da rede {lastGraph?.Data.Name}, " +
                "na propriedade Vazão de cada tubo (m³/s)? Rótulos, tabelas e perfis do Civil 3D passam a mostrar esse valor. O cálculo é refeito com os parâmetros atuais.")) return;
            SaveSettings();
            NetworkPending.Flows = input with { Mode = NetworkMode.Storm };
            if (!Actions.Send("PROJETAREDESVAZOES", this)) NetworkPending.Flows = null;
            return;
        }
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

    // ------------------------------------------------------------ drenagem: áreas e estudos de chuva

    private void EditAreas()
    {
        if (lastGraph is null || lastRequest is null) { SetStatus("Calcule a rede primeiro.", "warning"); return; }
        var g = lastGraph;
        var values = AreasDialog(g);
        if (values is null) return;
        if (values.Count == 0) { SetStatus("Nenhuma área mudou.", null); return; }
        var input = Build() ?? lastRequest;
        NetworkPending.Areas = new AreasRequest(input with { Mode = NetworkMode.Storm, NetworkHandle = g.Data.Handle }, values);
        if (!Actions.Send("PROJETAREDESAREAS", this)) NetworkPending.Areas = null;
    }

    private static bool ParseNumber(object? value, out double result)
    {
        var text = Convert.ToString(value, Br)?.Trim() ?? "";
        result = 0;
        if (text.Length == 0) return true;
        return double.TryParse(text, NumberStyles.Number, Br, out result) || double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    // Tabela com área (m²), C e tc (min) de todas as estruturas; devolve só o que mudou (null = cancelado).
    private List<CatchmentInput>? AreasDialog(NetworkGraph g)
    {
        var p = Theme.Current;
        using var form = new Form
        {
            Text = $"PROJETA — Áreas de contribuição · {g.Data.Name}", StartPosition = FormStartPosition.CenterParent, MinimizeBox = false,
            ShowInTaskbar = false, Font = Font, Width = Px(640), Height = Px(560), Padding = new Padding(Px(10))
        };
        var hint = new Label
        {
            Dock = DockStyle.Top, AutoSize = true, MaximumSize = new System.Drawing.Size(Px(600), 0), Tag = "muted", Padding = new Padding(0, 0, 0, Px(8)),
            Text = "Área em m², C de 0 a 1 e tc em minutos (vazio = tempo de entrada). Para colar do Excel, selecione a primeira célula e use Ctrl+V. " +
                   "Estruturas com bacias do Civil 3D ligadas ficam bloqueadas: os valores vêm das bacias."
        };
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2, BorderStyle = BorderStyle.FixedSingle, EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        DataGridViewTextBoxColumn Column(string header, int weight, bool readOnly, bool number) => new()
        {
            HeaderText = header, FillWeight = weight, ReadOnly = readOnly, SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = number ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft }
        };
        grid.Columns.AddRange(Column("Estrutura", 30, true, false), Column("Tipo", 12, true, false), Column("Área (m²)", 22, false, true),
            Column("C", 14, false, true), Column("tc (min)", 14, false, true));
        foreach (var s in g.Structures.Values.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var i = grid.Rows.Add(s.Name, s.Kind, s.CatchArea.ToString("N2", Br), s.CatchC.ToString("N2", Br), s.CatchTc > 0 ? s.CatchTc.ToString("N1", Br) : "");
            var row = grid.Rows[i];
            row.Tag = s;
            // Texto original de cada valor: linha sem texto mudado não é regravada (o arredondamento da tela não vira alteração).
            for (var k = 2; k <= 4; k++) row.Cells[k].Tag = Convert.ToString(row.Cells[k].Value, Br) ?? "";
            if (s.LinkedCatchments > 0)
            {
                row.ReadOnly = true;
                row.DefaultCellStyle.ForeColor = p.Muted;
                foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = $"{s.LinkedCatchments} bacia(s) do Civil 3D ligada(s): edite as bacias.";
            }
        }
        var total = new Label { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, Px(8), 0, Px(4)) };
        void Sum()
        {
            double area = 0, runoff = 0;
            foreach (DataGridViewRow row in grid.Rows)
                if (ParseNumber(row.Cells[2].Value, out var a) && ParseNumber(row.Cells[3].Value, out var c) && a > 0 && c >= 0 && c <= 1) { area += a; runoff += a * c; }
            total.Text = $"Total: {area.ToString("N2", Br)} m² ({(area / 10000).ToString("N4", Br)} ha) · C médio {(area > 0 ? runoff / area : 0).ToString("N2", Br)}";
        }
        grid.CellValueChanged += (_, _) => Sum();
        // Valor digitado volta no formato da tabela (1500 → 1.500,00).
        grid.CellEndEdit += (_, e) =>
        {
            var cell = grid[e.ColumnIndex, e.RowIndex];
            var text = Convert.ToString(cell.Value, Br)?.Trim() ?? "";
            if (e.ColumnIndex < 2 || text.Length == 0 || !ParseNumber(text, out var v)) return;
            cell.Value = v.ToString(e.ColumnIndex == 2 ? "N2" : e.ColumnIndex == 3 ? "N2" : "N1", Br);
        };
        grid.KeyDown += (_, e) =>
        {
            if (!(e.Control && e.KeyCode == Keys.V) || grid.CurrentCell is null) return;
            var text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            if (text.Length == 0) return;
            var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            for (var i = 0; i < lines.Length && grid.CurrentCell.RowIndex + i < grid.Rows.Count; i++)
            {
                var cells = lines[i].Split('\t');
                for (var j = 0; j < cells.Length && grid.CurrentCell.ColumnIndex + j < grid.Columns.Count; j++)
                {
                    var cell = grid[grid.CurrentCell.ColumnIndex + j, grid.CurrentCell.RowIndex + i];
                    if (!cell.ReadOnly) cell.Value = cells[j].Trim();
                }
            }
            e.Handled = true;
        };
        var ok = Ui.Button("Gravar no desenho", true);
        var cancel = Ui.Button("Cancelar");
        cancel.DialogResult = DialogResult.Cancel;
        var buttons = Ui.Row(ok, cancel);
        buttons.Dock = DockStyle.Bottom;
        form.Controls.Add(grid);
        form.Controls.Add(hint);
        form.Controls.Add(total);
        form.Controls.Add(buttons);
        form.AcceptButton = null; // Enter confirma a célula, não a janela
        form.CancelButton = cancel;
        var changes = new List<CatchmentInput>();
        ok.Click += (_, _) =>
        {
            grid.EndEdit();
            changes.Clear();
            var bad = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.ReadOnly || row.Tag is not NetStructure s) continue;
                var okArea = ParseNumber(row.Cells[2].Value, out var a) && a >= 0;
                var okC = ParseNumber(row.Cells[3].Value, out var c) && c >= 0 && c <= 1;
                var okTc = ParseNumber(row.Cells[4].Value, out var t) && t >= 0;
                row.Cells[2].ErrorText = okArea ? "" : "Área em m², não negativa.";
                row.Cells[3].ErrorText = okC ? "" : "C de 0 a 1.";
                row.Cells[4].ErrorText = okTc ? "" : "tc em minutos, não negativo (vazio = tempo de entrada).";
                if (!(okArea && okC && okTc)) { bad++; continue; }
                bool Edited(int k) => (Convert.ToString(row.Cells[k].Value, Br)?.Trim() ?? "") != ((row.Cells[k].Tag as string)?.Trim() ?? "");
                if (Edited(2) || Edited(3) || Edited(4))
                    changes.Add(new CatchmentInput(s.Handle, Edited(2) ? a : s.CatchArea, Edited(3) ? c : s.CatchC, Edited(4) ? t : s.CatchTc));
            }
            if (bad > 0) { MessageBox.Show(form, $"{bad} linha(s) com valor inválido (marcadas em vermelho).", "PROJETA", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (changes.Count > 0 && !Ui.Confirm(form, "ÁREAS DE CONTRIBUIÇÃO", $"Gravar área, C e tc em {changes.Count} estrutura(s) da rede {g.Data.Name}?")) return;
            form.DialogResult = DialogResult.OK;
        };
        Theme.Apply(form);
        grid.BackgroundColor = p.Field;
        grid.GridColor = p.Line;
        grid.DefaultCellStyle.BackColor = p.Field;
        grid.DefaultCellStyle.ForeColor = p.Text;
        grid.DefaultCellStyle.SelectionBackColor = p.Header;
        grid.DefaultCellStyle.SelectionForeColor = p.Text;
        grid.ColumnHeadersDefaultCellStyle.BackColor = p.Header;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = p.Muted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = p.Header;
        foreach (DataGridViewColumn column in grid.Columns)
            if (column.ReadOnly) column.DefaultCellStyle.ForeColor = p.Muted;
        Sum();
        return form.ShowDialog(this) == DialogResult.OK ? changes : null;
    }

    private void FindStudy()
    {
        List<RainStudy> all;
        try { all = RainCatalog.Bundled(); }
        catch (Exception ex) { SetStatus("Catálogo de estudos de chuva indisponível: " + ex.Message, "warning"); return; }
        var text = city.Text.Trim();
        var name = text;
        string? uf = null;
        var m = System.Text.RegularExpressions.Regex.Match(text, @"^(.*?)\s*[/,(\-–]\s*([A-Za-z]{2})\)?$");
        if (m.Success) { name = m.Groups[1].Value.Trim(); uf = m.Groups[2].Value.ToUpperInvariant(); }
        var key = RainCatalog.Normalize(name);
        if (key.Length == 0 && uf is null) { SetStatus("Escreva o município, por exemplo Belo Horizonte/MG.", "warning"); return; }
        var found = all.Where(s => (key.Length == 0 || RainCatalog.Normalize(s.Municipio).Contains(key)) && (uf is null || s.Uf.Equals(uf, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(s => s.Municipio, StringComparer.CurrentCultureIgnoreCase).ThenByDescending(s => s.Ano).ToList();
        if (found.Count == 0) { SetStatus($"Nenhum estudo do SGB para \"{text}\" entre os {all.Count} do catálogo. Confira a grafia.", "warning"); return; }
        using var form = new Form
        {
            Text = "PROJETA — Estudos de chuvas intensas (SGB)", StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false,
            ShowInTaskbar = false, Font = Font, Width = Px(560), Height = Px(400), Padding = new Padding(Px(10))
        };
        var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
        foreach (var s in found) list.Items.Add(s);
        var info = new Label { Dock = DockStyle.Bottom, AutoSize = true, MaximumSize = new System.Drawing.Size(Px(520), 0), Tag = "muted", Padding = new Padding(0, Px(6), 0, Px(6)) };
        var open = Ui.Button("Abrir estudo");
        var use = Ui.Button("Usar como fonte", true);
        var close = Ui.Button("Fechar");
        close.DialogResult = DialogResult.Cancel;
        var buttons = Ui.Row(open, use, close);
        buttons.Dock = DockStyle.Bottom;
        var top = new Label { Dock = DockStyle.Top, AutoSize = true, MaximumSize = new System.Drawing.Size(Px(520), 0), Padding = new Padding(0, 0, 0, Px(6)),
            Text = "O catálogo do SGB indica o estudo de cada município. Abra o estudo, confira a equação e digite K, a, b e c na aba." };
        form.Controls.Add(list);
        form.Controls.Add(top);
        form.Controls.Add(info);
        form.Controls.Add(buttons);
        form.CancelButton = close;
        RainStudy? Selected() => list.SelectedItem as RainStudy;
        list.SelectedIndexChanged += (_, _) =>
        {
            var s = Selected();
            info.Text = s is null ? "" : $"{s.Municipio}/{s.Uf} · {(string.IsNullOrWhiteSpace(s.Estacao) ? "" : "estação " + s.Estacao + " · ")}{s.Ano}" +
                                         (string.IsNullOrWhiteSpace(s.Observacao) ? "" : " · " + s.Observacao) + "\n" + s.Url;
        };
        open.Click += (_, _) =>
        {
            if (Selected() is not RainStudy s || !RainCatalog.IsOfficial(s.Url)) return;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(s.Url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(form, "Não foi possível abrir o navegador: " + ex.Message, "PROJETA"); }
        };
        use.Click += (_, _) =>
        {
            if (Selected() is not RainStudy s) return;
            idfSource.Text = $"SGB {s.Ano} — {s.Municipio}/{s.Uf}{(string.IsNullOrWhiteSpace(s.Estacao) ? "" : " — estação " + s.Estacao)} — {s.Url}";
            form.DialogResult = DialogResult.OK;
        };
        list.DoubleClick += (_, _) => use.PerformClick();
        if (list.Items.Count > 0) list.SelectedIndex = 0;
        Theme.Apply(form);
        form.ShowDialog(this);
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

    public void ShowStorm(NetworkRequest request, NetworkGraph g, StormResult result)
    {
        lastRequest = request; lastGraph = g; lastStorm = result;
        SwitchTo(NetworkMode.Storm);
        stormList.BeginUpdate();
        stormList.Items.Clear();
        foreach (var r in result.Rows)
            stormList.Items.Add(new ListViewItem(new[] { r.Reach.Pipe.Name, Fmt.N(r.Area, 3), Fmt.N(r.Tc, 1), Fmt.N(r.Flow, 1), Fmt.N(r.Ratio), Fmt.N(r.Velocity), r.Ok ? "OK" : "Revisar" })
                { Tag = r, ToolTipText = r.Ok ? "" : r.Notes });
        stormList.EndUpdate();
        var o = request.Storm;
        var bad = result.Rows.Count(r => !r.Ok);
        var text = $"{g.Data.Name}: {result.Rows.Count} trecho(s), área de contribuição {Fmt.N(result.TotalArea, 4)} ha, vazão no lançamento {Fmt.N(result.OutletFlow, 1)} L/s " +
                   $"(TR {Fmt.N(o.ReturnPeriod, 0)} anos, tempo de entrada {Fmt.N(o.InletTime, 1)} min). " +
                   (result.TotalArea <= 0 ? "Nenhuma área de contribuição nas estruturas: informe em Áreas de contribuição…. "
                       : bad == 0 ? "Todos os trechos atendem aos critérios informados." : $"{bad} trecho(s) para revisar: selecione para ver o motivo.") +
                   (result.Problems.Count > 0 ? "\n" + string.Join("\n", result.Problems.Take(6)) : "");
        ShowOutcome(text, bad == 0 && result.Problems.Count == 0 && result.TotalArea > 0);
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
            NetworkMode.Storm => stormList.SelectedItems.Count == 1 ? stormList.SelectedItems[0].Tag : null,
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
            StormRow r => $"{r.Reach.Pipe.Name}: {r.Up} → {r.Down} · L {Fmt.N(r.Length)} m · DN interno {Fmt.N(r.Diameter * 1000, 0)} mm · I {Fmt.N(r.Slope, 4)} m/m · " +
                          $"área própria {Fmt.N(r.OwnArea, 4)} ha (C {Fmt.N(r.OwnC)}, tc {Fmt.N(r.OwnTc, 1)} min) · acumulada {Fmt.N(r.Area, 4)} ha, C médio {Fmt.N(r.MeanC)} · " +
                          $"tc {Fmt.N(r.Tc, 1)} min · i {Fmt.N(r.Intensity, 1)} mm/h · Q {Fmt.N(r.Flow, 1)} L/s" + (r.PointFlow > 0 ? $" (pontual {Fmt.N(r.PointFlow, 1)})" : "") +
                          $" · capacidade a y/D máx {Fmt.N(r.Capacity, 1)} L/s · y/D {Fmt.N(r.Ratio)} · V {Fmt.N(r.Velocity)} m/s · percurso {Fmt.N(r.TravelTime, 2)} min · " +
                          $"recobrimento {Fmt.N(r.CoverUp)} / {Fmt.N(r.CoverDown)} m · DN sugerido {(r.Suggested is double dn ? Fmt.N(dn * 1000, 0) + " mm" : "—")}" +
                          (r.Ok ? "" : "\nRevisar: " + r.Notes),
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
            NetworkMode.Storm when lastStorm is not null && lastGraph is not null => StormSheets(lastRequest, lastGraph, lastStorm),
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
        var label = m switch { NetworkMode.Sewer => "Esgoto", NetworkMode.Storm => "Drenagem", NetworkMode.Trench => "Escavação", NetworkMode.Rename => "Renomear", _ => "Associação" };
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

    private static List<XlsxSheet> StormSheets(NetworkRequest q, NetworkGraph g, StormResult r)
    {
        var o = q.Storm;
        var rows = r.Rows.Select(x => new object?[]
        {
            x.Reach.Pipe.Name, x.Up, x.Down, x.Length, x.Diameter * 1000, x.Slope, x.OwnArea, x.OwnC, x.OwnTc, x.Area, x.MeanC, x.RunoffArea,
            x.Tc, x.Intensity, x.PointFlow, x.Flow, x.Capacity, x.Ratio, x.Velocity, x.TravelTime,
            x.Reach.UpInvert, x.Reach.DownInvert, x.Reach.UpGround, x.Reach.DownGround, x.CoverUp, x.CoverDown,
            x.Suggested * 1000, x.Ok ? "OK" : "Revisar", x.Notes
        }).ToList();
        var headers = new[]
        {
            "Trecho", "Montante", "Jusante", "Extensão (m)", "DN interno (mm)", "Declividade (m/m)", "Área própria (ha)", "C próprio", "tc próprio (min)",
            "Área acumulada (ha)", "C médio", "C·A acumulado (ha)", "tc (min)", "i (mm/h)", "Vazão pontual (L/s)", "Q (L/s)", "Capacidade a y/D máx (L/s)",
            "y/D", "V (m/s)", "Percurso (min)", "GI montante (m)", "GI jusante (m)", "Terreno montante (m)", "Terreno jusante (m)",
            "Recobr. montante (m)", "Recobr. jusante (m)", "DN sugerido (mm)", "Situação", "Observações"
        };
        var decimals = new[] { -1, -1, -1, 2, 0, 4, 4, 2, 1, 4, 2, 4, 1, 1, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 2, 2, 0, -1, -1 };
        var notes = new List<string>
        {
            "Método racional: Q = C·i·A/360, com Q em m³/s, i em mm/h e A em ha (na tabela, Q em L/s). C e A acumulados de montante para jusante.",
            $"Chuva: i = K·TR^a/(t + b)^c com K = {Fmt.N(o.K, 3)}, a = {Fmt.N(o.A, 4)}, b = {Fmt.N(o.B, 3)}, c = {Fmt.N(o.C, 4)}, TR = {Fmt.N(o.ReturnPeriod, 0)} anos" +
                (o.Source.Length > 0 ? $". Fonte: {o.Source}." : ". Fonte não informada."),
            $"tc: tempo de entrada de {Fmt.N(o.InletTime, 1)} min nas estruturas sem tc próprio; a jusante, o maior entre o tc da própria área e o tc de montante mais o percurso no tubo (velocidade do trecho).",
            "Capacidade, lâmina e velocidade por Manning em seção circular parcialmente cheia. DN sugerido: menor diâmetro comercial (NBR 8890) que leva a vazão com a lâmina máxima, sem diminuir para jusante.",
            "Calculado pelo PROJETA — Assistente Civil. Confira com o manual de drenagem adotado antes de usar no projeto."
        };
        if (r.Problems.Count > 0) notes.AddRange(r.Problems.Select(p => "Atenção: " + p));
        var parameters = new List<object?[]>
        {
            new object?[] { "Período de retorno", o.ReturnPeriod, "anos" }, new object?[] { "Tempo de entrada", o.InletTime, "min" },
            new object?[] { "K", o.K, "" }, new object?[] { "a", o.A, "" }, new object?[] { "b", o.B, "min" }, new object?[] { "c", o.C, "" },
            new object?[] { "Fonte da equação", o.Source, "" }, new object?[] { "n de Manning", o.Manning, "" },
            new object?[] { "y/D máximo", o.MaxRatio, "" }, new object?[] { "Velocidade mínima", o.MinVelocity, "m/s" },
            new object?[] { "Velocidade máxima", o.MaxVelocity, "m/s" }, new object?[] { "Recobrimento mínimo", o.MinCover, "m" },
            new object?[] { "Diâmetro mínimo", o.MinDiameter * 1000, "mm" }, new object?[] { "Área total de contribuição", r.TotalArea, "ha" },
            new object?[] { "Vazão no lançamento", r.OutletFlow, "L/s" }
        };
        var areas = g.Structures.Values.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).Select(s => new object?[]
        {
            s.Name, s.Kind, s.CatchArea, s.CatchArea / 10000, s.CatchC, s.CatchTc > 0 ? s.CatchTc : null,
            s.LinkedCatchments > 0 ? $"{s.LinkedCatchments} bacia(s) do Civil 3D" : "propriedades da estrutura"
        }).ToList();
        return new List<XlsxSheet>
        {
            new("Drenagem", headers, rows, decimals, notes),
            new("Áreas", new[] { "Estrutura", "Tipo", "Área (m²)", "Área (ha)", "C", "tc (min)", "Origem" }, areas, new[] { -1, -1, 2, 4, 2, 1, -1 }),
            new("Parâmetros", new[] { "Parâmetro", "Valor", "Unidade" }, parameters, new[] { -1, 4, -1 })
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
        ("TR", returnPeriod), ("TempoEntrada", inletTime), ("IdfK", idfK), ("IdfA", idfA), ("IdfB", idfB), ("IdfC", idfC),
        ("PluvialManning", stormManning), ("PluvialLamina", stormRatio), ("PluvialVmin", stormVmin), ("PluvialVmax", stormVmax),
        ("PluvialRecobrimento", stormCover), ("PluvialDN", stormDmin),
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
            foreach (var (name, box) in new[] { ("PrefixoPV", pvPrefix), ("PrefixoBL", blPrefix), ("PrefixoTubo", pipePrefix), ("IdfFonte", idfSource), ("Municipio", city) })
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
            foreach (var (name, box) in new[] { ("PrefixoPV", pvPrefix), ("PrefixoBL", blPrefix), ("PrefixoTubo", pipePrefix), ("IdfFonte", idfSource), ("Municipio", city) }) key.SetValue(name, box.Text);
        }
        catch { }
    }
}
