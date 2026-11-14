using System.Drawing;
using System.Windows.Forms;

namespace Projeta.Civil3D;

// Painel único com abas próprias no topo. As abas nativas da PaletteSet não aparecem
// no Civil 3D 2026/2027 quando a paleta está solta, então a navegação fica aqui.
internal sealed class MainPanel : UserControl
{
    private readonly FlowLayoutPanel tabs;
    private readonly Panel host;
    private readonly List<(Button Button, ToolPage Page)> items = new();
    private ToolPage? active;

    public MainPanel(params (string Title, ToolPage Page)[] pages)
    {
        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 9.75f);
        tabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, WrapContents = true,
            Padding = new Padding(LogicalToDeviceUnits(12), LogicalToDeviceUnits(10), LogicalToDeviceUnits(12), 0)
        };
        host = new Panel { Dock = DockStyle.Fill };
        foreach (var (title, page) in pages)
        {
            var button = new Button
            {
                Text = title, AutoSize = true, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, LogicalToDeviceUnits(4), LogicalToDeviceUnits(4)),
                Padding = new Padding(LogicalToDeviceUnits(6), LogicalToDeviceUnits(2), LogicalToDeviceUnits(6), LogicalToDeviceUnits(2))
            };
            button.Click += (_, _) => Select(page);
            tabs.Controls.Add(button);
            items.Add((button, page));
            page.Visible = false;
            host.Controls.Add(page);
        }
        Controls.Add(host);
        Controls.Add(tabs);
        if (items.Count > 0) Select(items[0].Page);
    }

    public void Select(ToolPage page)
    {
        active = page;
        foreach (var (_, p) in items) p.Visible = p == page;
        PaintTabs();
    }

    public void ApplyTheme()
    {
        Theme.Apply(this);
        foreach (var (_, page) in items) page.ApplyTheme();
        PaintTabs();
    }

    // Aba ativa com o estilo do botão principal; as demais, discretas.
    private void PaintTabs()
    {
        foreach (var (button, page) in items)
        {
            button.Tag = page == active ? "primary" : null;
            Theme.Apply(button);
        }
    }
}
