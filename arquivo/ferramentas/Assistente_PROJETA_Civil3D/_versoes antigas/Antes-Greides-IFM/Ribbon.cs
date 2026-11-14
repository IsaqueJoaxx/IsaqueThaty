using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Windows;
// O projeto tem a própria classe Geometry (contorno de polilinha); aqui é a do WPF.
using MediaGeometry = System.Windows.Media.Geometry;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;

namespace Projeta.Civil3D;

// Aba PROJETA no ribbon. Criada por código a cada sessão (não grava nada no CUI).
// Os botões só abrem o painel na aba certa; nenhum deles altera o desenho.
internal static class ProjetaRibbon
{
    private const string TabId = "PROJETA_ASSISTENTE_TAB";

    public static void Install()
    {
        if (ComponentManager.Ribbon is null)
        {
            ComponentManager.ItemInitialized -= OnItemInitialized;
            ComponentManager.ItemInitialized += OnItemInitialized;
            return;
        }
        Create();
    }

    private static void OnItemInitialized(object? sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon is null) return;
        ComponentManager.ItemInitialized -= OnItemInitialized;
        Create();
    }

    private static void Create()
    {
        var ribbon = ComponentManager.Ribbon;
        if (ribbon is null || ribbon.Tabs.Any(t => t.Id == TabId)) return;
        var source = new RibbonPanelSource { Title = "Assistente Civil · " + Commands.Target };
        source.Items.Add(Button("Superfícies", "superficies", Icons.Surface, "Lista as superfícies e sugere terreno natural e projeto."));
        source.Items.Add(Button("Volumes", "volumes", Icons.Volume, "Corte e aterro entre terreno natural e projeto, com limite e fatores."));
        source.Items.Add(Button("Seções", "secoes", Icons.Section, "Linhas de amostragem e vistas de seção ao longo de um eixo."));
        source.Items.Add(Button("Corredores", "corredores", Icons.Corridor, "Corredores em lote: eixo, greide de projeto e seção tipo."));
        var tab = new RibbonTab { Title = "PROJETA", Id = TabId };
        tab.Panels.Add(new RibbonPanel { Source = source });
        ribbon.Tabs.Add(tab);
    }

    private static RibbonButton Button(string text, string page, string glyph, string tip) => new()
    {
        Text = text, ShowText = true, ShowImage = true, Size = RibbonItemSize.Large,
        Orientation = System.Windows.Controls.Orientation.Vertical,
        LargeImage = Icons.Draw(glyph, 32), Image = Icons.Draw(glyph, 16),
        ToolTip = tip, Id = "PROJETA_" + page, CommandParameter = page, CommandHandler = OpenPage.Instance
    };

    private sealed class OpenPage : ICommand
    {
        public static readonly OpenPage Instance = new();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            if (parameter is RibbonButton b) parameter = b.CommandParameter;
            Commands.ShowPage(parameter as string ?? "superficies");
        }
    }
}

// Ícones desenhados em código (traço verde-água, legível no tema claro e no escuro).
internal static class Icons
{
    // Desenhos numa grade de 32 × 32.
    public const string Surface = "M3,25 L12,8 L20,19 L29,6 M3,25 L20,19 M12,8 L20,19 M3,25 L29,25 L29,6 M20,19 L29,25";
    public const string Volume = "M3,12 C9,6 15,18 21,10 C24,7 27,8 29,9 M3,20 L29,20 M3,27 L29,27";
    public const string Section = "M3,22 L9,22 L13,14 L19,14 L23,22 L29,22 M16,4 L16,28";
    public const string Corridor = "M5,29 C9,18 14,12 27,4 M11,29 C14,21 18,16 29,10 M8,29 C11.5,19.5 16,14 28,7";

    private static readonly Brush Stroke = Frozen(new SolidColorBrush(Color.FromRgb(38, 166, 140)));
    private static readonly Brush Fill = Frozen(new SolidColorBrush(Color.FromArgb(70, 101, 212, 186)));

    private static T Frozen<T>(T f) where T : Freezable { f.Freeze(); return f; }

    public static ImageSource Draw(string path, int size)
    {
        var geometry = MediaGeometry.Parse(path);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(size / 32.0, size / 32.0));
            var pen = new Pen(Stroke, size >= 32 ? 2.4 : 3.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            if (path == Volume) dc.DrawGeometry(Fill, null, MediaGeometry.Parse("M3,12 C9,6 15,18 21,10 C24,7 27,8 29,9 L29,20 L3,20 Z"));
            dc.DrawGeometry(null, pen, geometry);
            dc.Pop();
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
