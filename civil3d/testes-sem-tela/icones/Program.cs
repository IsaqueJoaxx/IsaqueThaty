using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Projeta.Civil3D.UI;

// Lê os grupos e comandos do PainelProjeta.cs (mesma ordem da aba), desenha cada ícone em 32 e 16 px na cor do
// grupo, sobre fundo claro e escuro (os dois temas do Civil), e lista os comandos sem desenho.
var painel = args.Length > 0 ? args[0] : @"..\..\projeta\src\Projeta.Civil3D\UI\PainelProjeta.cs";
var saida = args.Length > 1 ? args[1] : "icones.png";
var texto = File.ReadAllText(painel);

var grupos = new List<(string Nome, List<(string Rotulo, string Cmd)> Itens)>();
foreach (var linha in texto.Split('\n'))
{
    var g = Regex.Match(linha, @"^\s*\(""([^""]+)"",\s*(""[^""]+"",\s*)?new\[\]");
    if (g.Success) { grupos.Add((g.Groups[1].Value, new())); continue; }
    var c = Regex.Match(linha, @"^\s*\(""([^""]+)"",\s*""([A-Z]+)"",");
    if (c.Success && grupos.Count > 0) grupos[^1].Itens.Add((c.Groups[1].Value, c.Groups[2].Value));
}

var t = new Thread(() =>
{
    const int col = 8, cel = 150, alt = 92;
    int w = col * cel, h = 6000;
    var dv = new DrawingVisual();
    var semDesenho = new List<string>();
    using (var dc = dv.RenderOpen())
    {
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
        double y = 6;
        var tf = new Typeface("Segoe UI");
        for (int gi = 0; gi < grupos.Count; gi++)
        {
            var (nome, itens) = grupos[gi];
            var cor = IconesProjeta.CorDoGrupo(gi);
            dc.DrawText(new FormattedText(nome, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 13, Brushes.Black, 1.0), new Point(6, y));
            y += 22;
            for (int i = 0; i < itens.Count; i++)
            {
                double x = (i % col) * cel;
                if (i > 0 && i % col == 0) y += alt;
                var (rot, cmd) = itens[i];
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x3B, 0x44, 0x53)), null, new Rect(x + 74, y, 74, 50));
                var i32 = IconesProjeta.Criar(cmd, cor, 32);
                var i16 = IconesProjeta.Criar(cmd, cor, 16);
                if (i32 is null || i16 is null) { semDesenho.Add(cmd); continue; }
                dc.DrawImage(i32, new Rect(x + 6, y + 4, 32, 32));
                dc.DrawImage(i16, new Rect(x + 44, y + 12, 16, 16));
                dc.DrawImage(i32, new Rect(x + 80, y + 4, 32, 32));
                dc.DrawImage(i16, new Rect(x + 120, y + 12, 16, 16));
                dc.DrawText(new FormattedText(cmd, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, Brushes.Black, 1.0), new Point(x + 4, y + 52));
                dc.DrawText(new FormattedText(rot.Length > 26 ? rot[..26] : rot, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, Brushes.DimGray, 1.0), new Point(x + 4, y + 65));
            }
            y += alt + 4;
        }
        h = (int)y + 10;
    }
    var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
    bmp.Render(dv);
    var enc = new PngBitmapEncoder();
    enc.Frames.Add(BitmapFrame.Create(bmp));
    using (var fs = File.Create(saida)) enc.Save(fs);
    Console.WriteLine($"{grupos.Count} grupos, {grupos.Sum(g => g.Itens.Count)} comandos -> {saida}");
    Console.WriteLine(semDesenho.Count == 0 ? "Todos com desenho." : "Sem desenho: " + string.Join(", ", semDesenho));
});
t.SetApartmentState(ApartmentState.STA);
t.Start();
t.Join();
