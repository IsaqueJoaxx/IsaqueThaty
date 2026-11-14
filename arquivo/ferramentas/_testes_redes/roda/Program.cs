using System.Globalization;
using System.Runtime.InteropServices;

// Teste da roda do mouse sobre o campo numérico: o valor não muda e o painel rola.
internal static class Program
{
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        var form = new Form { Width = 400, Height = 300, StartPosition = FormStartPosition.Manual, Left = 20, Top = 20, ShowInTaskbar = false };
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        var boxes = new List<NumericUpDown>();
        var original = args.Length > 0 && args[0] == "original";
        for (var i = 0; i < 40; i++)
        {
            NumericUpDown n = original ? new NumericUpDown() : new BrNumber();
            n.Minimum = 0; n.Maximum = 100; n.DecimalPlaces = 2; n.Increment = 0.05m; n.Value = 1; n.Dock = DockStyle.Fill;
            boxes.Add(n);
            table.Controls.Add(new Label { Text = "campo " + i, AutoSize = true }, 0, i);
            table.Controls.Add(n, 1, i);
        }
        panel.Controls.Add(table);
        form.Controls.Add(panel);
        var result = 1;
        var report = Path.Combine(AppContext.BaseDirectory, "roda.txt");
        form.Shown += (_, _) =>
        {
            Application.DoEvents();
            var target = boxes[2];
            var edit = target.Controls[1]; // caixa de texto interna do NumericUpDown
            var p = edit.PointToScreen(new System.Drawing.Point(5, 5));
            var l = (IntPtr)((p.Y << 16) | (p.X & 0xFFFF));
            for (var k = 0; k < 5; k++) SendMessage(edit.Handle, 0x020A, (IntPtr)((-120) << 16), l); // WM_MOUSEWHEEL para baixo
            Application.DoEvents();
            var ok = target.Value == 1 && panel.VerticalScroll.Value > 0;
            File.WriteAllText(report, $"{(original ? "original" : "BrNumber")}: valor {target.Value.ToString(CultureInfo.InvariantCulture)}; rolagem do painel {panel.VerticalScroll.Value} -> {(ok ? "OK" : "FALHOU")}");
            result = ok ? 0 : 1;
            form.Close();
        };
        Application.Run(form);
        return result;
    }
}

// Cópia exata do BrNumber de Support.cs (sem as partes do AutoCAD).
internal sealed class BrNumber : NumericUpDown
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    protected override void UpdateEditText() => Text = Value.ToString((ThousandsSeparator ? "N" : "F") + DecimalPlaces, Br);
    protected override void ValidateEditText()
    {
        if (decimal.TryParse(Text, NumberStyles.Number, Br, out var typed))
            Value = Math.Min(Maximum, Math.Max(Minimum, Math.Round(typed, DecimalPlaces)));
        UserEdit = false;
        UpdateEditText();
    }
    protected override void OnMouseWheel(MouseEventArgs e) { }
}
