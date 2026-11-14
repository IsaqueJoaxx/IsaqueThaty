using System.Windows.Forms;

namespace Projeta.Civil3D;

// Aba Volumes: corte e aterro entre terreno natural e projeto, na área comum ou dentro de uma polilinha.
// "Calcular" não grava nada; "Criar superfície de volume" grava, depois de confirmar.
internal sealed class VolumesPage : ToolPage
{
    private readonly ComboBox ground;
    private readonly ComboBox design;
    private readonly Label limitText;
    private readonly NumericUpDown cutFactor;
    private readonly NumericUpDown fillFactor;
    private readonly Button create;
    private readonly ThemedListView results;
    private readonly Button export;
    private VolumeResult? lastResult;
    private string? resultDrawing;
    private string? limitHandle;
    private string? limitDrawing;
    private VolumeRequest? lastCalculated;

    public VolumesPage() : base("VOLUMES — IFM",
        "Compara o terreno natural (base) com o projeto (comparação). Calcular não grava nada no desenho; " +
        "depois, se quiser, crie a superfície de volume.")
    {
        ground = Ui.Combo();
        design = Ui.Combo();
        AddRow("Terreno natural", ground);
        AddRow("Projeto", design);

        limitText = Ui.Label("Área comum às duas superfícies");
        limitText.AutoEllipsis = true;
        AddRow("Limite", limitText);
        var pick = Ui.Button("Selecionar polilinha");
        pick.Click += (_, _) => { if (Actions.Send("PROJETALIMITE", this)) SetStatus("Selecione a polilinha fechada no desenho."); };
        var clear = Ui.Button("Usar área comum");
        clear.Click += (_, _) => SetLimit(null, null, null);
        AddWide(Ui.Row(pick, clear));

        cutFactor = Ui.Number(1m, 0.01m, 10m, 3, 0.01m);
        fillFactor = Ui.Number(1m, 0.01m, 10m, 3, 0.01m);
        AddRow("Fator de corte", cutFactor);
        AddRow("Fator de aterro", fillFactor);
        var tips = new ToolTip();
        tips.SetToolTip(cutFactor, "Multiplica o volume de corte (por exemplo, empolamento), como na superfície de volume do Civil 3D.");
        tips.SetToolTip(fillFactor, "Multiplica o volume de aterro (por exemplo, compactação), como na superfície de volume do Civil 3D.");

        var calculate = Ui.Button("Calcular volumes", primary: true);
        calculate.Click += (_, _) => Calculate();
        create = Ui.Button("Criar superfície de volume…");
        create.Enabled = false;
        create.Click += (_, _) => Create();
        AddWide(Ui.Row(calculate, create));

        results = new ThemedListView(new[] { "Resultado", "Valor" }, new[] { 45, 55 });
        AddWide(results, fill: true, minHeight: 140);
        export = Ui.Button("Exportar para Excel (CSV)…");
        export.Enabled = false;
        export.Click += (_, _) => Export();
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
        ground.SelectedIndexChanged += (_, _) => FieldsChanged();
        design.SelectedIndexChanged += (_, _) => FieldsChanged();
        cutFactor.ValueChanged += (_, _) => FieldsChanged();
        fillFactor.ValueChanged += (_, _) => FieldsChanged();
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
            foreach (ListViewItem item in results.Items)
                if (item.Text == "Criada no desenho") { results.Items.Remove(item); break; }
            create.Enabled = lastCalculated is not null;
            SetStatus($"A superfície \"{created}\" não está mais no desenho (desfeita ou apagada). " +
                      (create.Enabled ? "O cálculo continua valendo; dá para criar de novo." : "Calcule de novo para criar."), "warning");
        }
    }

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

    // Mudança feita pelo usuário: o resultado na tela deixa de valer e o botão de criar desliga.
    private void FieldsChanged()
    {
        if (updating) return;
        var hadResult = lastCalculated is not null;
        ResetCalculation();
        if (hadResult) SetStatus("Os campos mudaram. Clique em Calcular volumes de novo.", "warning");
    }

    private void ResetCalculation()
    {
        lastCalculated = null;
        create.Enabled = false;
    }

    public void ShowResult(VolumeRequest request, VolumeResult r)
    {
        var u = Snapshot?.Unit ?? "m";
        results.BeginUpdate();
        results.Items.Clear();
        void Add(string name, string value) => results.Items.Add(new ListViewItem(new[] { name, value }) { ToolTipText = $"{name}: {value}" });
        Add("Terreno natural (base)", r.BaseName);
        Add("Projeto (comparação)", r.ComparisonName);
        Add("Limite", r.Limit);
        if (r.LimitArea is not null) Add("Área do limite", Fmt.Area(r.LimitArea, u));
        Add("Corte", $"{Fmt.N(r.Cut)} {u}³");
        Add("Aterro", $"{Fmt.N(r.Fill)} {u}³");
        Add("Saldo", Fmt.Balance(r.Cut - r.Fill, u));
        if (r.CutFactor != 1 || r.FillFactor != 1)
        {
            Add($"Corte × {Fmt.N(r.CutFactor, 3)}", $"{Fmt.N(r.Cut * r.CutFactor)} {u}³");
            Add($"Aterro × {Fmt.N(r.FillFactor, 3)}", $"{Fmt.N(r.Fill * r.FillFactor)} {u}³");
            Add("Saldo com fatores", Fmt.Balance(r.Cut * r.CutFactor - r.Fill * r.FillFactor, u));
        }
        if (r.CreatedName is not null) Add("Criada no desenho", r.CreatedName);
        results.EndUpdate();

        lastCalculated = request with { Create = false };
        lastResult = r;
        resultDrawing = request.DrawingPath;
        export.Enabled = true;
        create.Enabled = r.CreatedName is null;
        SetStatus(r.CreatedName is null
            ? $"Calculado às {DateTime.Now:HH:mm:ss}. Nada foi gravado no desenho."
            : $"Superfície \"{r.CreatedName}\" criada. Para desfazer: U.", "good");
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
}
