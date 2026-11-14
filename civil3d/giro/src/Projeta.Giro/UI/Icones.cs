using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Projeta.Giro.UI;

/// <summary>Ícones desenhados em código (originais, sem arquivos de imagem). Grade de desenho 32×32.</summary>
internal static class Icones
{
    private static readonly Dictionary<string, ImageSource> Cache = new();
    private static readonly Color Azul = Color.FromRgb(0x1F, 0x6F, 0xB5);
    private static readonly Color Ciano = Color.FromRgb(0x3C, 0xB4, 0xD8);
    private static readonly Color Verde = Color.FromRgb(0x2E, 0x9E, 0x4F);
    private static readonly Color Vermelho = Color.FromRgb(0xD3, 0x3A, 0x2C);
    private static readonly Color Magenta = Color.FromRgb(0xB0, 0x3A, 0xB5);
    private static readonly Color Cinza = Color.FromRgb(0x55, 0x5B, 0x63);
    private static readonly Color Amarelo = Color.FromRgb(0xE8, 0xA8, 0x1C);

    private static ImageSource Criar(string nome, int tam, Action<DrawingContext> desenho)
    {
        string chave = nome + tam;
        if (Cache.TryGetValue(chave, out var c)) return c;
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(tam / 32.0, tam / 32.0));
            desenho(dc);
            dc.Pop();
        }
        var bmp = new RenderTargetBitmap(tam, tam, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        bmp.Freeze();
        Cache[chave] = bmp;
        return bmp;
    }

    private static Pen Caneta(Color c, double w, bool tracejada = false)
    {
        var p = new Pen(new SolidColorBrush(c), w) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        if (tracejada) p.DashStyle = new DashStyle(new double[] { 1.5, 1.5 }, 0);
        return p;
    }

    private static Brush Pincel(Color c, byte alfa = 255) => new SolidColorBrush(Color.FromArgb(alfa, c.R, c.G, c.B));

    private static Geometry G(string caminho) => Geometry.Parse(caminho);

    /// <summary>Caminhão visto de cima (cavalo + semirreboque).</summary>
    private static void Caminhao(DrawingContext dc, double x, double y, double ang, double esc = 1)
    {
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new RotateTransform(ang));
        dc.PushTransform(new ScaleTransform(esc, esc));
        dc.DrawRoundedRectangle(Pincel(Azul), Caneta(Cinza, 0.8), new Rect(-4, -14, 8, 6), 1.2, 1.2);
        dc.DrawRoundedRectangle(Pincel(Ciano), Caneta(Cinza, 0.8), new Rect(-4.5, -7, 9, 19), 0.8, 0.8);
        dc.Pop(); dc.Pop(); dc.Pop();
    }

    public static ImageSource Veiculo(int t) => Criar("veiculo", t, dc =>
    {
        Caminhao(dc, 16, 16, 0, 1.0);
        dc.DrawLine(Caneta(Cinza, 1.2), new Point(9, 28), new Point(23, 28));
        dc.DrawLine(Caneta(Cinza, 1.2), new Point(9, 26.5), new Point(9, 29.5));
        dc.DrawLine(Caneta(Cinza, 1.2), new Point(23, 26.5), new Point(23, 29.5));
    });

    public static ImageSource Gabarito(int t) => Criar("gabarito", t, dc =>
    {
        dc.DrawGeometry(null, Caneta(Ciano, 5.5), G("M 6,30 L 6,16 A 10,10 0 0 1 26,16 L 26,30"));
        dc.DrawGeometry(null, Caneta(Magenta, 1.4), G("M 6,30 L 6,16 A 10,10 0 0 1 26,16 L 26,30"));
        dc.DrawGeometry(Pincel(Magenta), null, G("M 22.5,25 L 26,31 L 29.5,25 Z"));
        dc.DrawEllipse(Pincel(Cinza), null, new Point(16, 16), 1.4, 1.4);
    });

    public static ImageSource Arco(int t) => Criar("arco", t, dc =>
    {
        dc.DrawGeometry(null, Caneta(Ciano, 6), G("M 3,28 L 10,28 A 14,14 0 0 0 24,14 L 24,6"));
        dc.DrawGeometry(null, Caneta(Magenta, 1.4), G("M 3,28 L 10,28 A 14,14 0 0 0 24,14 L 24,6"));
        foreach (var p in new[] { new Point(3, 28), new Point(10, 28), new Point(24, 6) })
            dc.DrawEllipse(Pincel(Colors.White), Caneta(Vermelho, 1.4), p, 2.2, 2.2);
    });

    public static ImageSource Seguir(int t) => Criar("seguir", t, dc =>
    {
        dc.DrawGeometry(null, Caneta(Magenta, 1.6, true), G("M 3,29 C 12,29 10,8 29,6"));
        Caminhao(dc, 13, 19, 40, 0.75);
    });

    public static ImageSource Atualizar(int t) => Criar("atualizar", t, dc =>
    {
        dc.DrawGeometry(null, Caneta(Verde, 2.6), G("M 26,14 A 10,10 0 1 0 24.5,22"));
        dc.DrawGeometry(Pincel(Verde), null, G("M 21,13 L 28,9.5 L 28.5,17 Z"));
    });

    public static ImageSource Verificar(int t) => Criar("verificar", t, dc =>
    {
        dc.DrawGeometry(Pincel(Ciano, 120), Caneta(Ciano, 1), G("M 2,26 L 12,26 A 12,12 0 0 0 24,14 L 24,2 L 31,2 L 31,14 A 19,19 0 0 1 12,31 L 2,31 Z"));
        dc.DrawGeometry(null, Caneta(Cinza, 1.6), G("M 2,22 L 10,22 A 12,12 0 0 0 20,12 L 20,2"));
        dc.DrawEllipse(null, Caneta(Vermelho, 2), new Point(15.5, 19), 4.5, 4.5);
    });

    public static ImageSource Animar(int t) => Criar("animar", t, dc =>
    {
        dc.DrawEllipse(Pincel(Verde), null, new Point(16, 16), 13, 13);
        dc.DrawGeometry(Pincel(Colors.White), null, G("M 12.5,9 L 23,16 L 12.5,23 Z"));
    });

    public static ImageSource Relatorio(int t) => Criar("relatorio", t, dc =>
    {
        dc.DrawRoundedRectangle(Pincel(Colors.White), Caneta(Cinza, 1.4), new Rect(6, 3, 20, 26), 1.5, 1.5);
        for (int i = 0; i < 5; i++) dc.DrawLine(Caneta(i == 0 ? Azul : Cinza, 1.4), new Point(10, 9 + i * 4.2), new Point(i == 0 ? 22 : 20 - (i % 2) * 4, 9 + i * 4.2));
    });

    public static ImageSource Config(int t) => Criar("config", t, dc =>
    {
        for (int i = 0; i < 8; i++)
        {
            double a = i * Math.PI / 4;
            dc.DrawLine(Caneta(Cinza, 4.2), new Point(16 + 7 * Math.Cos(a), 16 + 7 * Math.Sin(a)), new Point(16 + 12 * Math.Cos(a), 16 + 12 * Math.Sin(a)));
        }
        dc.DrawEllipse(Pincel(Cinza), null, new Point(16, 16), 9, 9);
        dc.DrawEllipse(Pincel(Colors.White), null, new Point(16, 16), 4, 4);
    });

    public static ImageSource Demo(int t) => Criar("demo", t, dc =>
    {
        dc.DrawRectangle(Pincel(Color.FromRgb(0xCF, 0xD4, 0xDA)), null, new Rect(1, 18, 30, 9));
        dc.DrawRectangle(Pincel(Color.FromRgb(0xCF, 0xD4, 0xDA)), null, new Rect(11.5, 1, 9, 18));
        dc.DrawGeometry(null, Caneta(Amarelo, 1.2, true), G("M 1,22.5 L 31,22.5 M 16,1 L 16,18"));
        dc.DrawGeometry(null, Caneta(Magenta, 1.6), G("M 31,20 L 24,20 A 6,6 0 0 1 18,14 L 18,1"));
    });

    public static ImageSource Ajuda(int t) => Criar("ajuda", t, dc =>
    {
        dc.DrawEllipse(Pincel(Azul), null, new Point(16, 16), 13, 13);
        var ft = new FormattedText("?", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 20, Brushes.White, 1.0);
        dc.DrawText(ft, new Point(16 - ft.Width / 2, 16 - ft.Height / 2));
    });
}
