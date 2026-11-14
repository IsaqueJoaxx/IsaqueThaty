using System.Drawing;
using System.Windows.Forms;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D;

internal sealed class AssistantPanel : UserControl
{
#if NET10_0_OR_GREATER
    public const string Target = "Civil 3D 2027";
#else
    public const string Target = "Civil 3D 2026";
#endif

    private sealed record Theme(Color Back, Color Field, Color Header, Color Line, Color Text, Color Muted,
        Color Accent, Color ButtonBack, Color ButtonHover, Color ButtonText);

    // COLORTHEME 0 = escuro, 1 = claro.
    private static readonly Theme Dark = new(
        Back: Color.FromArgb(24, 34, 45), Field: Color.FromArgb(16, 26, 36), Header: Color.FromArgb(36, 51, 64),
        Line: Color.FromArgb(58, 72, 86), Text: Color.FromArgb(230, 237, 245), Muted: Color.FromArgb(168, 184, 199),
        Accent: Color.FromArgb(101, 212, 186), ButtonBack: Color.FromArgb(101, 212, 186),
        ButtonHover: Color.FromArgb(136, 229, 207), ButtonText: Color.FromArgb(16, 45, 39));

    private static readonly Theme Light = new(
        Back: Color.FromArgb(245, 247, 249), Field: Color.White, Header: Color.FromArgb(226, 232, 237),
        Line: Color.FromArgb(196, 206, 215), Text: Color.FromArgb(28, 38, 48), Muted: Color.FromArgb(84, 99, 113),
        Accent: Color.FromArgb(13, 117, 98), ButtonBack: Color.FromArgb(13, 117, 98),
        ButtonHover: Color.FromArgb(17, 140, 118), ButtonText: Color.White);

    // Pesos das colunas Superfície, Função sugerida e Tipo; somam a largura útil da tabela.
    // O handle fica na dica da linha, para a tabela caber na paleta.
    private static readonly int[] ColumnWeights = { 34, 44, 22 };

    private readonly TableLayoutPanel layout;
    private readonly Label title;
    private readonly Label subtitle;
    private readonly Button refresh;
    private readonly Label status;
    private readonly ListView surfaces;
    private readonly Label footer;
    private readonly ToolTip tips = new();
    private Theme theme = Dark;
    private bool fittingColumns;

    public AssistantPanel()
    {
        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 10);
        // Alturas em pixels lógicos, convertidas para a escala do monitor (125%, 150%...).
        // As linhas de texto (1, 3 e 5) são recalculadas em FitRows; a tabela fica com o restante.
        layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(Px(16)) };
        foreach (var height in new[] { 40, 64, 40, 56 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(height)));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(80)));
        layout.SizeChanged += (_, _) => FitRows();

        title = new Label { Text = "PROJETA / ASSISTENTE CIVIL — IFM", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold) };
        subtitle = new Label { Dock = DockStyle.Fill,
            Text = $"BASE EXPERIMENTAL · SOMENTE LEITURA · {Target.ToUpperInvariant()}\nListe as superfícies do desenho ativo. Nenhum cálculo ou alteração é realizado." };
        refresh = new Button { Text = "Consultar superfícies do desenho ativo", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        refresh.FlatAppearance.BorderSize = 0;
        refresh.Click += (_, _) => QueueRead();
        status = new Label { Dock = DockStyle.Fill, Padding = new Padding(0, Px(10), 0, Px(4)), AutoEllipsis = true };
        surfaces = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, OwnerDraw = true,
            ShowItemToolTips = true, BorderStyle = BorderStyle.FixedSingle, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        foreach (var header in new[] { "Superfície", "Função sugerida", "Tipo" })
            surfaces.Columns.Add(header);
        // O Windows desenha o cabeçalho sempre claro; aqui ele segue o tema do painel.
        surfaces.DrawColumnHeader += DrawHeader;
        surfaces.DrawItem += (_, e) => e.DrawDefault = true;
        surfaces.DrawSubItem += (_, e) => e.DrawDefault = true;
        surfaces.ClientSizeChanged += (_, _) => FitColumns();
        footer = new Label { Dock = DockStyle.Fill, Padding = new Padding(0, Px(10), 0, 0),
            Text = "Consulta pontual: clique novamente após editar o desenho.\nVolumes: próxima etapa. Drenagem: aguardando planilha e critérios. Conversa: disponível na prévia HTML, sem IA conectada." };

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        layout.Controls.Add(refresh, 0, 2);
        layout.Controls.Add(status, 0, 3);
        layout.Controls.Add(surfaces, 0, 4);
        layout.Controls.Add(footer, 0, 5);
        Controls.Add(layout);
        ApplyTheme(LightThemeActive());
        FitColumns();
        InvalidateSnapshot("Nenhuma consulta realizada.");
    }

    private int Px(int logical) => LogicalToDeviceUnits(logical);

    // Cada linha de texto recebe a altura do texto quebrado na largura atual da paleta.
    // Assim a tabela não perde espaço para linhas vazias, nem o texto é cortado em paletas estreitas.
    private void FitRows()
    {
        // O evento de tamanho pode chegar antes de o construtor criar os rótulos.
        if (subtitle is null || status is null || footer is null) return;
        var width = layout.ClientSize.Width - layout.Padding.Horizontal;
        if (width <= 0) return;
        layout.SuspendLayout();
        FitRow(1, subtitle, width);
        FitRow(3, status, width);
        FitRow(5, footer, width);
        layout.ResumeLayout();
    }

    private void FitRow(int row, Label label, int width)
    {
        var textWidth = Math.Max(1, width - label.Margin.Horizontal - label.Padding.Horizontal);
        var text = label.Text.Length == 0 ? " " : label.Text;
        var size = TextRenderer.MeasureText(text, label.Font, new Size(textWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        layout.RowStyles[row].Height = size.Height + label.Padding.Vertical + label.Margin.Vertical + Px(4);
    }

    public static bool LightThemeActive()
    {
        try { return Convert.ToInt32(AcadApplication.GetSystemVariable("COLORTHEME")) == 1; }
        catch { return false; }
    }

    public void ApplyTheme(bool light)
    {
        theme = light ? Light : Dark;
        BackColor = layout.BackColor = theme.Back;
        ForeColor = layout.ForeColor = theme.Text;
        foreach (var label in new[] { title, subtitle, status, footer })
        {
            label.BackColor = theme.Back;
            label.ForeColor = theme.Text;
        }
        title.ForeColor = theme.Accent;
        footer.ForeColor = theme.Muted;
        refresh.BackColor = theme.ButtonBack;
        refresh.ForeColor = theme.ButtonText;
        refresh.FlatAppearance.MouseOverBackColor = theme.ButtonHover;
        refresh.FlatAppearance.MouseDownBackColor = theme.ButtonHover;
        surfaces.BackColor = theme.Field;
        surfaces.ForeColor = theme.Text;
        surfaces.Invalidate();
    }

    private void DrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using (var back = new SolidBrush(theme.Header))
            e.Graphics.FillRectangle(back, e.Bounds);
        using (var line = new Pen(theme.Line))
        {
            e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            if (e.ColumnIndex < surfaces.Columns.Count - 1)
                e.Graphics.DrawLine(line, e.Bounds.Right - 1, e.Bounds.Top + Px(4), e.Bounds.Right - 1, e.Bounds.Bottom - Px(5));
        }
        var text = Rectangle.FromLTRB(e.Bounds.Left + Px(6), e.Bounds.Top, e.Bounds.Right - Px(4), e.Bounds.Bottom);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", surfaces.Font, text, theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    // As colunas ocupam exatamente a largura útil: sem rolagem lateral em nenhuma largura da paleta.
    private void FitColumns()
    {
        if (fittingColumns || surfaces.Columns.Count != ColumnWeights.Length) return;
        var width = surfaces.ClientSize.Width - 1;
        if (width <= 0) return;
        fittingColumns = true;
        try
        {
            var total = ColumnWeights.Sum();
            var used = 0;
            for (var i = 0; i < ColumnWeights.Length; i++)
            {
                var columnWidth = i == ColumnWeights.Length - 1 ? width - used : width * ColumnWeights[i] / total;
                surfaces.Columns[i].Width = columnWidth;
                used += columnWidth;
            }
        }
        finally { fittingColumns = false; }
    }

    private void QueueRead()
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) { InvalidateSnapshot("Abra um desenho no Civil 3D."); return; }
        if (document.CommandInProgress.Length != 0)
        {
            InvalidateSnapshot("Conclua o comando atual e consulte novamente.");
            return;
        }
        InvalidateSnapshot("Consulta solicitada. Aguardando execução do comando no desenho ativo…");
        // Fixed command only: user input never becomes command text. No cancellation or write command.
        document.SendStringToExecute("PROJETASUPERFICIES ", true, false, false);
    }

    public void InvalidateSnapshot(string message)
    {
        surfaces.Items.Clear();
        status.Text = message;
        tips.SetToolTip(status, null);
        FitRows();
    }

    public void Display(SurfaceSnapshot snapshot)
    {
        surfaces.BeginUpdate();
        try
        {
            surfaces.Items.Clear();
            foreach (var row in snapshot.Rows)
                surfaces.Items.Add(new ListViewItem(new[] { row.Name, SurfaceNaming.Suggestion(row, snapshot.Rows), row.Type })
                    { ToolTipText = $"{row.Name}\nHandle {row.Handle}" });
        }
        finally { surfaces.EndUpdate(); }
        status.Text = $"Desenho: {snapshot.Drawing}\nConsulta: {snapshot.CapturedAt:HH:mm:ss} · {snapshot.Rows.Count} superfície(s)" +
            (snapshot.Rows.Count == 0 ? "\nNenhuma superfície encontrada neste desenho." : "");
        // O caminho completo fica na dica do status, para não cortar o texto.
        tips.SetToolTip(status, snapshot.DrawingPath);
        FitRows();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tips.Dispose();
        base.Dispose(disposing);
    }
}
