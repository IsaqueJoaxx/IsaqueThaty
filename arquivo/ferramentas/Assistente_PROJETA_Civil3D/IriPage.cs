using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Projeta.Civil3D;

internal sealed class IriPage : ToolPage
{
    private readonly TextBox input = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Height = 125, MaxLength = 5_000_000 };
    private readonly CheckBox compare = new() { Text = "Comparar com limite informado", AutoSize = true };
    private readonly NumericUpDown limit = Ui.Number(0, 0, 1000, 3, .1m);
    private readonly TextBox reference = new() { Dock = DockStyle.Fill, MaxLength = 300 };
    private readonly Label source;
    private readonly Label summary;
    private readonly ThemedListView results;
    private readonly Button export;
    private List<IriSegment>? analyzed;
    private double? analyzedLimit;
    private string analyzedReference = "";

    public IriPage() : base("IRI RODOVIÁRIO — IFM", "Analisa resultados de IRI medidos, em m/km, por rodovia, faixa, sentido e trilha. Não calcula IRI a partir do greide do desenho e não atesta aceitação normativa.")
    {
        var load = Ui.Button("Importar CSV"); load.Click += (_, _) => LoadCsv();
        var example = Ui.Button("Carregar exemplo"); example.Click += (_, _) => { input.Text = IriCore.Example; source!.Text = "Dados fictícios de demonstração"; };
        AddWide(Ui.Row(load, example));
        source = Wrap("Fonte: dados externos; sem vínculo automático com o desenho.", "muted"); AddWide(source);
        AddWide(Wrap("Cole o cabeçalho e os trechos abaixo. Separador: ponto e vírgula ou tabulação. Início/fim em metros acumulados, sempre crescentes; não use estaca+fração.", "muted"));
        AddWide(input, minHeight: 125);
        AddWide(Wrap(IriCore.Header, "muted"));
        AddWide(compare); AddRow("Limite (m/km)", limit);
        AddRow("Norma / contrato", reference);
        limit.Enabled = reference.Enabled = false;
        compare.CheckedChanged += (_, _) => { limit.Enabled = reference.Enabled = compare.Checked; InvalidateResult(); };
        input.TextChanged += (_, _) => InvalidateResult();
        limit.ValueChanged += (_, _) => InvalidateResult(); reference.TextChanged += (_, _) => InvalidateResult();
        var analyze = Ui.Button("Analisar trechos", primary: true); analyze.Click += (_, _) => Analyze(); AddWide(analyze);
        summary = Wrap("Nenhum resultado analisado.", "muted"); AddWide(summary);
        results = new ThemedListView(new[] { "Rodovia / faixa / sentido / trilha", "Ext. (m)", "Média", "Máx.", "Acima (m)", "Lacunas" }, new[] { 40, 14, 12, 12, 14, 8 });
        AddWide(results, fill: true, minHeight: 150);
        AddWide(Wrap("Média ponderada pela extensão efetivamente medida. Lacunas ficam fora da média. Trilhas, faixas e sentidos não são combinados. Igualdade ao limite fica em ‘até o limite’. A comparação não constitui aprovação da obra.", "muted"));
        export = Ui.Button("Exportar análise CSV"); export.Enabled = false; export.Click += (_, _) => Export(); AddWide(export);
    }
    private void InvalidateResult()
    {
        analyzed = null;
        if (export is not null) export.Enabled = false;
        if (results is not null) results.Items.Clear();
        if (summary is not null) summary.Text = "Dados ou critério alterados. Clique em Analisar trechos.";
    }
    private void LoadCsv()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV ou texto UTF-8|*.csv;*.txt|Todos os arquivos|*.*", Title = "IRI RODOVIÁRIO — IFM • Importar resultados" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > 10_000_000) throw new ArgumentException("Arquivo muito grande. Limite: 10 MB.");
            var text = File.ReadAllText(dialog.FileName, new UTF8Encoding(false, true));
            IriCore.Parse(text); // Importação inválida não substitui os dados atuais.
            input.Text = text; source.Text = "Fonte externa: " + dialog.FileName;
            ShowOutcome("Arquivo importado. Confira os dados e o critério antes de analisar.", true);
        }
        catch (System.Exception ex) { ShowOutcome("Não foi possível importar: " + ex.Message, false); }
    }
    private void Analyze()
    {
        InvalidateResult();
        try
        {
            var rows = IriCore.Parse(input.Text);
            double? threshold = compare.Checked ? (double)limit.Value : null;
            if (compare.Checked && string.IsNullOrWhiteSpace(reference.Text)) throw new ArgumentException("Informe a norma, contrato ou origem do limite escolhido.");
            var groups = IriCore.Summarize(rows, threshold);
            results.BeginUpdate();
            try
            {
                foreach (var g in groups)
                    results.Items.Add(new ListViewItem(new[] { g.Key, Fmt.N(g.Length), Fmt.N(g.Mean,3), Fmt.N(g.Max,3), threshold is null ? "—" : Fmt.N(g.AboveLength), g.Gaps.ToString() })
                    { ToolTipText = $"{g.Key}\n{g.Count} trechos. IRI em m/km. {g.Gaps} lacunas excluídas da média." });
            }
            finally { results.EndUpdate(); }
            analyzed = rows; analyzedLimit = threshold; analyzedReference = threshold is null ? "" : reference.Text.Trim();
            summary.Text = $"{rows.Count} trechos · {groups.Count} séries independentes · {groups.Sum(g=>g.Gaps)} lacunas. IRI em m/km. " +
                (threshold is null ? "Sem comparação com limite." : $"Limite informado: {Fmt.N(threshold,3)} m/km; {analyzedReference}.");
            export.Enabled = true;
            ShowOutcome("Análise concluída. O desenho não foi alterado.", true);
        }
        catch (System.Exception ex) { ShowOutcome("Análise não concluída: " + ex.Message, false); }
    }
    private void Export()
    {
        if (analyzed is null) return;
        using var dialog = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "IRI_RODOVIARIO_IFM_analise.csv", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(dialog.FileName, IriCore.Export(analyzed, analyzedLimit, analyzedReference), new UTF8Encoding(true));
            ShowOutcome("Análise exportada: " + dialog.FileName, true);
        }
        catch (System.Exception ex) { ShowOutcome("Não foi possível exportar: " + ex.Message, false); }
    }
    // Dados importados permanecem explicitamente externos quando o desenho muda.
    protected override void OnSnapshot(DrawingSnapshot? snapshot) { }
}
