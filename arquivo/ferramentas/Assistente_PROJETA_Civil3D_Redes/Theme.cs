using System.Drawing;
using System.Windows.Forms;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D;

internal sealed record Palette(Color Back, Color Field, Color Header, Color Line, Color Text, Color Muted,
    Color Accent, Color ButtonBack, Color ButtonHover, Color ButtonText, Color SecondaryBack, Color Warning, Color Good);

// Cores do painel. COLORTHEME 0 = escuro, 1 = claro. O papel de cada controle vem do Tag:
// "title", "muted", "warning", "good" para rótulos; "primary" para o botão principal.
internal static class Theme
{
    public static readonly Palette Dark = new(
        Back: Color.FromArgb(24, 34, 45), Field: Color.FromArgb(16, 26, 36), Header: Color.FromArgb(36, 51, 64),
        Line: Color.FromArgb(58, 72, 86), Text: Color.FromArgb(230, 237, 245), Muted: Color.FromArgb(168, 184, 199),
        Accent: Color.FromArgb(101, 212, 186), ButtonBack: Color.FromArgb(101, 212, 186),
        ButtonHover: Color.FromArgb(136, 229, 207), ButtonText: Color.FromArgb(16, 45, 39),
        SecondaryBack: Color.FromArgb(41, 62, 74), Warning: Color.FromArgb(230, 190, 117), Good: Color.FromArgb(136, 229, 207));

    public static readonly Palette Light = new(
        Back: Color.FromArgb(245, 247, 249), Field: Color.White, Header: Color.FromArgb(226, 232, 237),
        Line: Color.FromArgb(196, 206, 215), Text: Color.FromArgb(28, 38, 48), Muted: Color.FromArgb(84, 99, 113),
        Accent: Color.FromArgb(13, 117, 98), ButtonBack: Color.FromArgb(13, 117, 98),
        ButtonHover: Color.FromArgb(17, 140, 118), ButtonText: Color.White,
        SecondaryBack: Color.FromArgb(232, 237, 241), Warning: Color.FromArgb(150, 92, 0), Good: Color.FromArgb(13, 117, 98));

    public static Palette Current { get; private set; } = Dark;

    public static void Refresh()
    {
        try { Current = Convert.ToInt32(AcadApplication.GetSystemVariable("COLORTHEME")) == 1 ? Light : Dark; }
        catch { Current = Dark; }
    }

    public static void Apply(Control root)
    {
        Paint(root, Current);
        foreach (Control child in root.Controls) Apply(child);
    }

    private static void Paint(Control c, Palette p)
    {
        var role = c.Tag as string;
        switch (c)
        {
            case Button b:
                var primary = role == "primary";
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = primary ? 0 : 1;
                b.FlatAppearance.BorderColor = p.Line;
                b.BackColor = primary ? p.ButtonBack : p.SecondaryBack;
                b.ForeColor = primary ? p.ButtonText : p.Text;
                b.FlatAppearance.MouseOverBackColor = primary ? p.ButtonHover : p.Header;
                b.FlatAppearance.MouseDownBackColor = primary ? p.ButtonHover : p.Header;
                break;
            case ListView or ComboBox or TextBox or NumericUpDown or CheckedListBox:
                c.BackColor = p.Field;
                c.ForeColor = p.Text;
                c.Invalidate();
                break;
            case Label label:
                label.BackColor = p.Back;
                label.ForeColor = role switch
                {
                    "title" => p.Accent, "muted" => p.Muted, "warning" => p.Warning, "good" => p.Good, _ => p.Text
                };
                break;
            default:
                c.BackColor = p.Back;
                c.ForeColor = p.Text;
                break;
        }
    }
}

// Lista com cabeçalho no tema do painel e colunas que ocupam exatamente a largura útil.
internal sealed class ThemedListView : ListView
{
    private readonly int[] weights;
    private bool fitting;

    public ThemedListView(string[] headers, int[] weights)
    {
        this.weights = weights;
        View = View.Details;
        FullRowSelect = true;
        OwnerDraw = true;
        ShowItemToolTips = true;
        HideSelection = false;
        MultiSelect = false;
        BorderStyle = BorderStyle.FixedSingle;
        HeaderStyle = ColumnHeaderStyle.Nonclickable;
        Dock = DockStyle.Fill;
        foreach (var header in headers) Columns.Add(header);
        DrawColumnHeader += DrawHeader;
        DrawItem += (_, e) => e.DrawDefault = true;
        DrawSubItem += (_, e) => e.DrawDefault = true;
        ClientSizeChanged += (_, _) => FitColumns();
        // Com desenho próprio, a caixa de marcação às vezes não se redesenha sozinha.
        ItemChecked += (_, e) => Invalidate(e.Item.Bounds);
    }

    private void DrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        var p = Theme.Current;
        using (var back = new SolidBrush(p.Header)) e.Graphics.FillRectangle(back, e.Bounds);
        using (var line = new Pen(p.Line))
        {
            e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            if (e.ColumnIndex < Columns.Count - 1)
                e.Graphics.DrawLine(line, e.Bounds.Right - 1, e.Bounds.Top + LogicalToDeviceUnits(4),
                    e.Bounds.Right - 1, e.Bounds.Bottom - LogicalToDeviceUnits(5));
        }
        var text = Rectangle.FromLTRB(e.Bounds.Left + LogicalToDeviceUnits(6), e.Bounds.Top,
            e.Bounds.Right - LogicalToDeviceUnits(4), e.Bounds.Bottom);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", Font, text, p.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    public void FitColumns()
    {
        if (fitting || Columns.Count != weights.Length) return;
        var width = ClientSize.Width - 1;
        if (width <= 0) return;
        fitting = true;
        try
        {
            var total = weights.Sum();
            var used = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                var w = i == weights.Length - 1 ? width - used : width * weights[i] / total;
                Columns[i].Width = w;
                used += w;
            }
        }
        finally { fitting = false; }
    }
}

