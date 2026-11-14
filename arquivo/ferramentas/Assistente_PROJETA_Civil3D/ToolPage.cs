using System.Drawing;
using System.Windows.Forms;

namespace Projeta.Civil3D;

// Base das abas do painel: título, ajuda curta, botão de leitura, linha de situação e uma grade
// de duas colunas (rótulo | campo). As linhas de texto quebram na largura atual da paleta.
internal abstract class ToolPage : UserControl
{
    private readonly TableLayoutPanel grid;
    private readonly Label status;
    private readonly List<Label> wrapping = new();

    protected DrawingSnapshot? Snapshot { get; private set; }

    protected ToolPage(string title, string help)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Font = new Font("Segoe UI", 9.75f);
        grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 0,
            Padding = new Padding(Px(14), Px(10), Px(14), Px(10))
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.SizeChanged += (_, _) => Rewrap();

        var heading = Ui.Label(title, "title");
        heading.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        heading.Margin = new Padding(0, 0, 0, Px(2));
        AddWide(heading);
        AddWide(Wrap(help, "muted"));
        // A leitura é automática (depois de cada comando e ao trocar de desenho); o botão força uma releitura.
        var read = Ui.Button("Atualizar");
        read.Click += (_, _) =>
        {
            var document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (document is not null && document.CommandInProgress.Length != 0) { SetStatus("Conclua o comando atual e tente de novo.", "warning"); return; }
            Commands.Refresh(force: true);
        };
        AddWide(read);
        status = Wrap("Lendo o desenho ativo…", "muted");
        AddWide(status);
        Controls.Add(grid);
    }

    protected int Px(int logical) => LogicalToDeviceUnits(logical);

    protected Label Wrap(string text, string? role)
    {
        var label = Ui.Label(text, role);
        label.Margin = new Padding(0, Px(3), 0, Px(3));
        wrapping.Add(label);
        return label;
    }

    // Devolve o rótulo, para a aba poder esconder a linha inteira (rótulo e campo).
    protected Label AddRow(string label, Control control)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var text = Ui.Label(label);
        grid.Controls.Add(text, 0, row);
        grid.Controls.Add(control, 1, row);
        return text;
    }

    // Troca a altura de uma linha já criada (por exemplo, uma lista que só ocupa espaço num dos modos da aba).
    protected void SetRowStyle(Control control, SizeType type, float value)
    {
        var row = grid.GetRow(control);
        if (row < 0 || row >= grid.RowStyles.Count) return;
        grid.RowStyles[row] = new RowStyle(type, value);
        PerformLayout();
    }

    protected void AddWide(Control control, bool fill = false, int minHeight = 0)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        if (fill) control.Dock = DockStyle.Fill;
        if (minHeight > 0) control.MinimumSize = new Size(0, Px(minHeight));
        if (fill) fillMinimum[row] = Px(Math.Max(minHeight, 40)) + control.Margin.Vertical;
        grid.Controls.Add(control, 0, row);
        grid.SetColumnSpan(control, 2);
    }

    // Altura mínima da aba: linhas de texto e campos no tamanho natural, listas no mínimo.
    // Se a paleta for mais baixa que isso, aparece a barra de rolagem em vez de cortar botões.
    private readonly Dictionary<int, int> fillMinimum = new();

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (grid is null || fillMinimum is null) return; // chamado durante a construção
        var heights = grid.GetRowHeights();
        var minimum = grid.Padding.Vertical;
        // Com mais de uma lista (linhas em %), a sobra é dividida na proporção: cada lista só chega ao seu mínimo
        // se a sobra inteira for pelo menos mínimo ÷ fração. Antes, duas listas se sobrepunham (aba Volumes por seções).
        var percentTotal = 0f;
        for (var i = 0; i < grid.RowStyles.Count; i++)
            if (grid.RowStyles[i].SizeType == SizeType.Percent) percentTotal += grid.RowStyles[i].Height;
        var share = 0;
        for (var i = 0; i < heights.Length && i < grid.RowStyles.Count; i++)
        {
            var style = grid.RowStyles[i];
            if (style.SizeType == SizeType.Percent && style.Height > 0)
                share = Math.Max(share, (int)Math.Ceiling(fillMinimum.GetValueOrDefault(i, Px(40)) * percentTotal / style.Height));
            else if (style.SizeType != SizeType.Percent) minimum += heights[i];
        }
        minimum += share;
        if (Math.Abs(AutoScrollMinSize.Height - minimum) > 2) AutoScrollMinSize = new Size(0, minimum);
    }

    public void SetStatus(string text, string? role = null)
    {
        status.Text = text;
        status.Tag = role ?? "muted";
        Theme.Apply(status);
        Rewrap();
    }

    // Resultado de um comando (criou, calculou, falhou): rola a aba até a linha de situação,
    // que fica no topo, para a mensagem não ficar escondida quando a aba está rolada para baixo.
    public void ShowOutcome(string text, bool ok)
    {
        SetStatus(text, ok ? "good" : "warning");
        ScrollControlIntoView(status);
    }

    public void ShowSnapshot(DrawingSnapshot? snapshot, string? message = null)
    {
        Snapshot = snapshot;
        SetStatus(message ?? (snapshot is null
            ? "Nenhum desenho lido."
            : $"{snapshot.Drawing} · atualizado às {snapshot.CapturedAt:HH:mm:ss}"));
        OnSnapshot(snapshot);
    }

    protected abstract void OnSnapshot(DrawingSnapshot? snapshot);

    public void ApplyTheme()
    {
        Theme.Apply(this);
        OnThemeApplied();
    }

    protected virtual void OnThemeApplied() { }

    private void Rewrap()
    {
        var width = grid.ClientSize.Width - grid.Padding.Horizontal;
        if (width <= 0) return;
        foreach (var label in wrapping)
            label.MaximumSize = new Size(Math.Max(Px(60), width - label.Margin.Horizontal), 0);
    }
}
