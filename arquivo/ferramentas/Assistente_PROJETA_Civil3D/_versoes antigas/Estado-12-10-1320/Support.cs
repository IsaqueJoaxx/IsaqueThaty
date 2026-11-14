using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;

namespace Projeta.Civil3D;

// Item de lista que guarda o handle do objeto, nunca o objeto da base.
internal sealed record Choice(string Text, string Handle)
{
    public override string ToString() => Text;
}

internal static class Ui
{
    public static Label Label(string text, string? role = null) => new()
    {
        Text = text, Tag = role, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 10, 7)
    };

    public static Button Button(string text, bool primary = false) => new()
    {
        Text = text, Tag = primary ? "primary" : null, AutoSize = true, FlatStyle = FlatStyle.Flat,
        Padding = new Padding(8, 3, 8, 3), Margin = new Padding(0, 4, 8, 4), UseVisualStyleBackColor = false
    };

    // Lista suspensa desenhada no tema do painel (a lista aberta do Windows seria sempre branca).
    public static ComboBox Combo()
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4), DrawMode = DrawMode.OwnerDrawFixed
        };
        combo.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            var p = Theme.Current;
            var selected = (e.State & DrawItemState.Selected) != 0;
            using (var back = new SolidBrush(selected ? p.Accent : p.Field)) e.Graphics.FillRectangle(back, e.Bounds);
            TextRenderer.DrawText(e.Graphics, combo.Items[e.Index]?.ToString() ?? "", e.Font ?? combo.Font, e.Bounds,
                selected ? p.ButtonText : p.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };
        return combo;
    }

    public static NumericUpDown Number(decimal value, decimal min, decimal max, int decimals, decimal increment) => new BrNumber
    {
        Minimum = min, Maximum = max, DecimalPlaces = decimals, Increment = increment, Value = value,
        Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Right,
        ThousandsSeparator = true, Margin = new Padding(0, 4, 0, 4)
    };

    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
        row.Controls.AddRange(controls);
        return row;
    }

    // Confirmação antes de qualquer criação no desenho.
    public static bool Confirm(IWin32Window owner, string title, string text) =>
        MessageBox.Show(owner, text + "\n\nTudo pode ser desfeito depois com o comando U.", "PROJETA — " + title,
            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK;

    public static void Select(ComboBox combo, Func<Choice, bool> match)
    {
        for (var i = 0; i < combo.Items.Count; i++)
            if (combo.Items[i] is Choice c && match(c)) { combo.SelectedIndex = i; return; }
        if (combo.Items.Count > 0 && combo.SelectedIndex < 0) combo.SelectedIndex = 0;
    }

    public static void SetItems(ComboBox combo, IEnumerable<Choice> items)
    {
        var previous = (combo.SelectedItem as Choice)?.Handle;
        combo.BeginUpdate();
        combo.Items.Clear();
        foreach (var item in items) combo.Items.Add(item);
        combo.EndUpdate();
        if (previous is not null) Select(combo, c => c.Handle == previous);
    }
}

internal static class Fmt
{
    public static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static string N(double? value, int decimals = 2) =>
        value is double v && double.IsFinite(v) ? v.ToString("N" + decimals, Br) : "—";

    public static string Unit(UnitsValue units) => units switch
    {
        UnitsValue.Meters => "m", UnitsValue.Feet => "ft", UnitsValue.Centimeters => "cm",
        UnitsValue.Millimeters => "mm", UnitsValue.Kilometers => "km", _ => "un"
    };

    public static string Area(double? value, string unit) =>
        value is null ? "—" : unit == "m" ? $"{N(value)} m² ({N(value / 10000, 4)} ha)" : $"{N(value)} {unit}²";

    // Saldo = corte − aterro. Positivo sobra material; negativo falta para o aterro.
    public static string Balance(double value, string unit) => value switch
    {
        > 0.005 => $"{N(value)} {unit}³ (sobra de corte)",
        < -0.005 => $"{N(-value)} {unit}³ (falta para aterro)",
        _ => $"0,00 {unit}³ (equilibrado)"
    };
}

// Campo numérico sempre no formato brasileiro (vírgula decimal), mesmo com o Windows em outro idioma.
internal sealed class BrNumber : NumericUpDown
{
    protected override void UpdateEditText() =>
        Text = Value.ToString((ThousandsSeparator ? "N" : "F") + DecimalPlaces, Fmt.Br);

    protected override void ValidateEditText()
    {
        if (decimal.TryParse(Text, NumberStyles.Number, Fmt.Br, out var typed))
            Value = Math.Min(Maximum, Math.Max(Minimum, Math.Round(typed, DecimalPlaces)));
        UserEdit = false; // sem isto, ler Value voltaria a validar o texto sem parar
        UpdateEditText();
    }
}

internal static class Names
{
    // Primeiro nome livre: "Nome", "Nome (2)", "Nome (3)"...
    public static string Unique(IEnumerable<string> existing, string desired)
    {
        var used = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(desired)) return desired;
        for (var i = 2; ; i++)
        {
            var candidate = $"{desired} ({i})";
            if (!used.Contains(candidate)) return candidate;
        }
    }
}

internal static class Ids
{
    public static ObjectId Resolve(Database db, string handle, string what)
    {
        if (long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)
            && db.TryGetObjectId(new Handle(value), out var id) && !id.IsNull && !id.IsErased)
            return id;
        throw new InvalidOperationException($"Não encontrei {what} no desenho. Leia o desenho de novo.");
    }
}
