using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Projeta.Civil3D.UI;

/// <summary>
/// Ícones da aba PROJETA: quadrado na cor do grupo com um desenho branco de traço (rede, perfil, superfícies, rotatória,
/// globo...) e, quando cabe, um selo no canto (+ criar, × excluir, ↓ importar, ↑ exportar, ✓ conferir, ▶ rodar, lápis
/// editar). Desenhado em vetor num quadro de 32 e reduzido para 16, sem arquivo de imagem. Só WPF: não depende do
/// AutoCAD, então dá para gerar a folha de conferência fora do Civil (testes-sem-tela\icones).
/// </summary>
internal static class IconesProjeta
{
    private enum Selo { Nenhum, Mais, Xis, Entra, Sai, Ok, Play, Refaz, Lapis, Alerta, Raio }

    private sealed record Icone(string Traco, string Cheio = "", Selo Selo = Selo.Nenhum);

    private static readonly Color[] Cores =
    {
        Color.FromRgb(0x1F, 0x6F, 0xB2), Color.FromRgb(0x2E, 0x8B, 0x57),
        Color.FromRgb(0xC2, 0x6A, 0x1B), Color.FromRgb(0x7B, 0x4F, 0xA6),
        Color.FromRgb(0x0F, 0x8A, 0x8A), Color.FromRgb(0xB0, 0x3A, 0x3A),
        Color.FromRgb(0x5D, 0x6D, 0x7E), Color.FromRgb(0x8D, 0x6E, 0x2F),
        Color.FromRgb(0x2B, 0x5C, 0x8A), Color.FromRgb(0x6A, 0x8E, 0x23),
        Color.FromRgb(0xA0, 0x40, 0x7A), Color.FromRgb(0x3F, 0x51, 0xB5),
        Color.FromRgb(0x55, 0x55, 0x55), Color.FromRgb(0x00, 0x79, 0x6B),
    };

    /// <summary>Cor do grupo <paramref name="g"/> da aba (a mesma em todos os botões do painel).</summary>
    public static Color CorDoGrupo(int g) => Cores[g % Cores.Length];

    private static readonly Dictionary<string, Icone> Tabela = Montar();

    /// <summary>Comandos que têm desenho (para a folha de conferência).</summary>
    public static IReadOnlyCollection<string> Comandos => Tabela.Keys;

    /// <summary>Ícone do comando em <paramref name="px"/> pixels, ou null se ele não tiver desenho (cai na sigla).</summary>
    public static ImageSource? Criar(string cmd, Color cor, int px)
    {
        if (!Tabela.TryGetValue(cmd, out var ic)) return null;
        bool pequeno = px < 24;
        var fundo = new SolidColorBrush(cor);
        fundo.Freeze();
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(px / 32.0, px / 32.0));
            dc.DrawRoundedRectangle(fundo, null, new Rect(0.5, 0.5, 31, 31), 6.5, 6.5);
            var caneta = Caneta(Brushes.White, pequeno ? 2.9 : 2.1);
            if (ic.Cheio.Length > 0) dc.DrawGeometry(Brushes.White, null, Geometry.Parse(ic.Cheio));
            if (ic.Traco.Length > 0) dc.DrawGeometry(null, caneta, Geometry.Parse(ic.Traco));
            if (ic.Selo != Selo.Nenhum) DesenharSelo(dc, ic.Selo, fundo, pequeno);
            dc.Pop();
        }
        var bmp = new RenderTargetBitmap(px, px, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        bmp.Freeze();
        return bmp;
    }

    private static Pen Caneta(Brush b, double esp)
    {
        var p = new Pen(b, esp) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        p.Freeze();
        return p;
    }

    private static void DesenharSelo(DrawingContext dc, Selo selo, Brush fundo, bool pequeno)
    {
        double cx = 24.6, cy = 24.6, r = pequeno ? 8.2 : 7.2, k = r / 7.2;
        dc.DrawEllipse(Brushes.White, Caneta(fundo, 1.6), new Point(cx, cy), r, r);
        string traco = selo switch
        {
            Selo.Mais => L(cx, cy - 4 * k, cx, cy + 4 * k) + L(cx - 4 * k, cy, cx + 4 * k, cy),
            Selo.Xis => L(cx - 3 * k, cy - 3 * k, cx + 3 * k, cy + 3 * k) + L(cx + 3 * k, cy - 3 * k, cx - 3 * k, cy + 3 * k),
            Selo.Entra => L(cx, cy - 4.2 * k, cx, cy + 3.6 * k) + L(cx - 3.2 * k, cy + 0.6 * k, cx, cy + 3.8 * k, cx + 3.2 * k, cy + 0.6 * k),
            Selo.Sai => L(cx, cy + 4.2 * k, cx, cy - 3.6 * k) + L(cx - 3.2 * k, cy - 0.6 * k, cx, cy - 3.8 * k, cx + 3.2 * k, cy - 0.6 * k),
            Selo.Ok => L(cx - 3.8 * k, cy + 0.2 * k, cx - 1.2 * k, cy + 2.9 * k, cx + 3.9 * k, cy - 2.6 * k),
            Selo.Refaz => $"M{F(cx + 3.6 * k)},{F(cy)} A{F(3.6 * k)},{F(3.6 * k)} 0 1 1 {F(cx)},{F(cy - 3.6 * k)} "
                          + L(cx - 1.4 * k, cy - 5.8 * k, cx + 0.9 * k, cy - 3.6 * k, cx - 1.4 * k, cy - 1.4 * k),
            Selo.Lapis => L(cx - 3.6 * k, cy + 3.6 * k, cx + 3.2 * k, cy - 3.2 * k),
            _ => "",
        };
        if (selo == Selo.Play)
            dc.DrawGeometry(fundo, null, Geometry.Parse(Z(cx - 2.4 * k, cy - 3.9 * k, cx + 4 * k, cy, cx - 2.4 * k, cy + 3.9 * k)));
        else if (selo == Selo.Raio)
            dc.DrawGeometry(fundo, null, Geometry.Parse(Z(cx + 1.2 * k, cy - 5 * k, cx - 3.2 * k, cy + 0.9 * k, cx - 0.3 * k, cy + 0.9 * k,
                cx - 1.2 * k, cy + 5 * k, cx + 3.2 * k, cy - 0.9 * k, cx + 0.3 * k, cy - 0.9 * k)));
        else if (selo == Selo.Alerta)
        {
            dc.DrawGeometry(null, Caneta(fundo, pequeno ? 2.8 : 2.1), Geometry.Parse(L(cx, cy - 4 * k, cx, cy + 0.8 * k)));
            dc.DrawEllipse(fundo, null, new Point(cx, cy + 3.6 * k), 1.3 * k, 1.3 * k);
        }
        else
            dc.DrawGeometry(null, Caneta(fundo, selo == Selo.Lapis ? (pequeno ? 3.6 : 2.8) : (pequeno ? 2.6 : 1.9)), Geometry.Parse(traco));
    }

    // ---- primitivas de traçado (mini-linguagem de caminho do WPF, quadro de 32) ----

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string P(double x, double y) => F(x) + "," + F(y);

    /// <summary>Polilinha aberta.</summary>
    private static string L(params double[] p)
    {
        var sb = new StringBuilder("M").Append(P(p[0], p[1])).Append(" L");
        for (int i = 2; i < p.Length; i += 2) sb.Append(P(p[i], p[i + 1])).Append(' ');
        return sb.ToString();
    }

    /// <summary>Polígono fechado.</summary>
    private static string Z(params double[] p) => L(p) + "Z ";

    private static string C(double x, double y, double r) => E(x, y, r, r);

    private static string E(double x, double y, double rx, double ry) =>
        $"M{P(x - rx, y)} A{F(rx)},{F(ry)} 0 1 0 {P(x + rx, y)} A{F(rx)},{F(ry)} 0 1 0 {P(x - rx, y)} Z ";

    private static string R(double x, double y, double w, double h) => Z(x, y, x + w, y, x + w, y + h, x, y + h);

    private static string Seta(double x1, double y1, double x2, double y2, double c = 4.5)
    {
        double dx = x2 - x1, dy = y2 - y1, n = Math.Sqrt(dx * dx + dy * dy);
        dx /= n; dy /= n;
        double bx = x2 - dx * c, by = y2 - dy * c, px = -dy * c * 0.75, py = dx * c * 0.75;
        return L(x1, y1, x2, y2) + L(bx + px, by + py, x2, y2, bx - px, by - py);
    }

    private static string Globo(double x, double y, double r) =>
        C(x, y, r) + E(x, y, r * 0.45, r) + L(x - r, y, x + r, y)
        + L(x - r * 0.87, y - r * 0.5, x + r * 0.87, y - r * 0.5) + L(x - r * 0.87, y + r * 0.5, x + r * 0.87, y + r * 0.5);

    private static string Documento(double x, double y, double w, double h) =>
        Z(x, y, x + w - 5, y, x + w, y + 5, x + w, y + h, x, y + h) + L(x + w - 5, y, x + w - 5, y + 5, x + w, y + 5);

    /// <summary>Engrenagem cheia (com furo; regra par-ímpar).</summary>
    private static string Engrenagem(double x, double y, double rExt, double rInt, double furo)
    {
        var pts = new List<double>();
        for (int i = 0; i < 8; i++)
        {
            double a = i * Math.PI / 4;
            foreach (var (da, r) in new[] { (-0.38, rInt), (-0.2, rExt), (0.2, rExt), (0.38, rInt) })
            {
                pts.Add(x + r * Math.Cos(a + da));
                pts.Add(y + r * Math.Sin(a + da));
            }
        }
        return "F0 " + Z(pts.ToArray()) + C(x, y, furo);
    }

    private static Dictionary<string, Icone> Montar()
    {
        string rede = C(7, 23, 3) + C(15, 9, 3) + C(26, 11, 3) + L(8.5, 20.3, 13.5, 11.7) + L(18, 9.5, 23, 10.4);
        string pipe = R(3.5, 20, 6, 6) + R(12, 5, 6, 6) + R(22, 9, 6, 6) + L(8, 20, 13.5, 11) + L(18, 8.5, 22, 10);
        string grade = R(4, 5, 20, 17) + L(4, 10.5, 24, 10.5) + L(4, 16, 24, 16) + L(11, 5, 11, 22) + L(17.5, 5, 17.5, 22);
        string gotaS = "M14,3.5 C14,3.5 6,13 6,17.5 A8,8 0 0 0 22,17.5 C22,13 14,3.5 14,3.5 Z ";
        string teS = "M3,9 L11,9 L11,3.5 M17,3.5 L17,9 L26,9 M3,16 L26,16 ";
        string valeta = L(3, 9, 11, 9, 16, 21, 21, 9, 29, 9);
        string cubo = Z(16, 4, 27, 10, 27, 22, 16, 28, 5, 22, 5, 10) + L(5, 10, 16, 16, 27, 10) + L(16, 16, 16, 28);
        string corte = L(3, 16, 29, 16) + "M3,16 C7,6 12,6 16,16 C20,26 25,26 29,16 ";
        string corteCheio = "M3,16 C7,6 12,6 16,16 Z ";
        string pessoas = C(10.5, 10, 3.3) + "M4,24 A6.5,6.5 0 0 1 17,24 " + C(21.5, 10, 3.3) + "M15,24 A6.5,6.5 0 0 1 28,24 ";

        return new Dictionary<string, Icone>(StringComparer.OrdinalIgnoreCase)
        {
            // Rede
            ["PRJREDE"] = new(rede, Selo: Selo.Mais),
            ["PRJOPCOES"] = new("", Engrenagem(16, 16, 12, 8.6, 4)),
            ["PRJEQUACOES"] = new("M8,17 A4.5,4.5 0 0 1 9.5,8.5 A6.5,6.5 0 0 1 21.5,7.5 A4.8,4.8 0 0 1 24,17 Z "
                                  + L(10.5, 21, 9, 26) + L(16.5, 21, 15, 26) + L(22.5, 21, 21, 26)),
            ["PRJLANCAR"] = new(L(5, 25, 12, 12, 20, 18, 27, 6), C(5, 25, 2.3) + C(12, 12, 2.3) + C(20, 18, 2.3) + C(27, 6, 2.3)),
            ["PRJNO"] = new(C(13, 13, 8) + L(9, 13, 17, 13) + L(13, 9, 13, 17), Selo: Selo.Mais),
            ["PRJTUBO"] = new(C(7, 16, 4) + C(25, 16, 4) + L(11, 14.3, 21, 14.3) + L(11, 17.7, 21, 17.7)),
            ["PRJBACIA"] = new(Z(5, 12, 12, 5, 24, 7, 27, 17, 21, 27, 9, 25),
                               "M16,10.5 C16,10.5 11.5,16 11.5,19 A4.5,4.5 0 0 0 20.5,19 C20.5,16 16,10.5 16,10.5 Z "),
            ["PRJPROPRIEDADES"] = new(L(5, 9, 27, 9) + L(5, 16, 27, 16) + L(5, 23, 27, 23), C(11, 9, 2.8) + C(21, 16, 2.8) + C(14, 23, 2.8)),
            ["PRJEXPORTARREDE"] = new(rede, Selo: Selo.Sai),
            ["PRJIMPORTARREDE"] = new(rede, Selo: Selo.Entra),
            ["PRJEXCLUIRREDE"] = new(rede, Selo: Selo.Xis),

            // Edição
            ["PRJCONECTAR"] = new(C(24, 9, 4.5) + Seta(6, 25, 20.5, 12.5), C(6, 25, 2.2)),
            ["PRJINVERTER"] = new(Seta(5, 10, 27, 10) + Seta(27, 22, 5, 22)),
            ["PRJDIVIDIR"] = new(L(3, 16, 10.5, 16) + L(21.5, 16, 29, 16) + C(16, 16, 5.5) + L(16, 3, 16, 7.5) + L(16, 24.5, 16, 29)),
            ["PRJTROCAR"] = new(R(4, 4, 11, 11) + C(22, 22, 5.5) + "M18.5,6.5 Q25,6.5 25,13.5 " + L(22, 11, 25, 14.5, 28, 11)
                                + "M13.5,25.5 Q7,25.5 7,18.5 " + L(4, 21, 7, 17.5, 10, 21)),
            ["PRJREMOVER"] = new(L(7, 9, 25, 9) + L(13, 9, 13, 5.5, 19, 5.5, 19, 9) + L(9.5, 9, 11, 27, 21, 27, 22.5, 9)
                                 + L(14, 13.5, 14.4, 22.5) + L(18, 13.5, 17.6, 22.5)),
            ["PRJNUMERAR"] = new(L(12.5, 5, 10, 27) + L(21.5, 5, 19, 27) + L(6, 12, 27, 12) + L(5, 20, 26, 20)),

            // Cálculo e saídas
            ["PRJCALCULAR"] = new(R(8, 3.5, 16, 25) + R(11, 6.5, 10, 5),
                                  C(12.5, 16, 1.5) + C(16, 16, 1.5) + C(19.5, 16, 1.5) + C(12.5, 20, 1.5) + C(16, 20, 1.5) + C(19.5, 20, 1.5)
                                  + C(12.5, 24, 1.5) + C(16, 24, 1.5) + C(19.5, 24, 1.5)),
            ["PRJATUALIZAR"] = new("M25.3,13 A9.5,9.5 0 0 0 7.4,11.5 " + L(4.2, 9.6, 7.2, 12.4, 10.6, 10.2)
                                   + "M6.7,19 A9.5,9.5 0 0 0 24.6,20.5 " + L(21.4, 21.8, 24.8, 19.6, 27.8, 22.4)),
            ["PRJTABELA"] = new(R(4, 6, 24, 20) + L(4, 18.5, 28, 18.5) + L(13, 12, 13, 26) + L(20.5, 12, 20.5, 26), R(4, 6, 24, 6)),
            ["PRJRELATORIO"] = new(Documento(6, 3.5, 20, 25) + L(12.5, 14, 9.5, 17.5, 12.5, 21) + L(19.5, 14, 22.5, 17.5, 19.5, 21) + L(17.5, 12.5, 14.5, 22.5)),
            ["PRJNOTACAIXAS"] = new(Documento(6, 3.5, 20, 25) + L(10.5, 13, 21.5, 13) + L(10.5, 17.5, 21.5, 17.5) + L(10.5, 22, 18, 22)),
            ["PRJPERFIL"] = new(L(4, 4, 4, 28, 29, 28) + "M6,10 C10,6 14,13 18,9 S25,7 29,9 " + L(10, 8.6, 10, 22) + L(24, 8.2, 24, 18),
                                R(8.5, 19.5, 3, 4) + Z(10, 21, 24, 16.5, 24, 19.3, 10, 23.8)),
            ["PRJINTERFERENCIA"] = new(L(3, 23, 23, 3) + L(3, 6, 17, 21) + C(12.6, 12.6, 3.2), Selo: Selo.Alerta),
            ["PRJESCAVACAO"] = new(L(3, 9, 8, 9, 11, 26, 21, 26, 24, 9, 29, 9) + L(4, 9, 2.5, 12) + L(8, 9, 6.5, 12) + L(25, 9, 23.5, 12) + L(29, 9, 27.5, 12),
                                   C(16, 21.5, 3.4)),

            // Integração
            ["PRJIMPORTARPIPE"] = new(pipe, Selo: Selo.Entra),
            ["PRJDEVOLVERPIPE"] = new(pipe, Selo: Selo.Sai),
            ["PRJIMPORTARCSV"] = new(grade, Selo: Selo.Entra),
            ["PRJEXPORTARCSV"] = new(grade, Selo: Selo.Sai),
            ["PRJMODELOCSV"] = new(grade, Selo: Selo.Mais),

            // Longitudinais
            ["PRJLONGITUDINAL"] = new(valeta, Z(13.4, 15, 18.6, 15, 16, 21) + L(3, 25, 29, 25)),
            ["PRJLONGEDITAR"] = new(L(3, 8, 10, 8, 14.5, 19, 19, 8, 26, 8), Z(12.2, 14, 16.8, 14, 14.5, 19), Selo.Lapis),
            ["PRJLONGQUANT"] = new(L(24, 6, 8, 6, 17, 16, 8, 26, 24, 26)),

            // Alinhamento
            ["PRJAPAGARPI"] = new(L(3, 23, 14, 7, 28, 16), C(14, 7, 2.8), Selo.Xis),
            ["PRJVIAS"] = new(L(3, 25, 13, 25) + "M13,25 Q22,25 25,17 L28,8 " + L(13, 3, 13, 29) + L(3, 12, 13, 12) + L(22, 3, 22, 12.5), C(13, 25, 2.4) + C(13, 12, 2.4)),
            ["PRJAPAGARPIV"] = new(L(4, 4, 4, 27, 16, 27) + L(6, 20, 15, 9, 28, 15), C(15, 9, 2.8), Selo.Xis),

            // Superfícies
            ["PRJVOLUMES"] = new(corte, corteCheio),
            ["PRJCONFORMIDADE"] = new("M3,12 C8,6 12,16 17,10 S24,7 29,9 M3,19 C8,14 12,23 17,18 S24,15 29,16 ", Selo: Selo.Ok),
            ["PRJRELEVO"] = new(R(10, 4.5, 5, 5) + L(9, 7, 6, 7) + L(16, 7, 19, 7) + R(2.5, 5, 3.5, 4) + R(19, 5, 3.5, 4) + L(14.5, 9.5, 17, 12),
                                Z(3, 28, 12, 15, 17, 21, 22, 14, 29, 28)),
            ["PRJGEORREF"] = new("M16,28.5 C16,28.5 7,18.5 7,12.5 A9,9 0 0 1 25,12.5 C25,18.5 16,28.5 16,28.5 Z " + C(16, 12.5, 3.3)),
            ["PRJKMZ"] = new(Globo(14, 14, 10.5), Selo: Selo.Sai),
            ["PRJKML"] = new(Globo(14, 14, 10.5), Selo: Selo.Entra),
            ["PRJVOLRAPIDO"] = new(corte, corteCheio, Selo.Raio),
            ["PRJFAIXAS"] = new(L(3, 27, 29, 27), R(4, 20, 24, 4.5) + R(8, 13.5, 16, 4.5) + R(12, 7, 8, 4.5)),

            // Interseções e rotatórias
            ["PRJINTERSECAO"] = new("M3,12 L8,12 A4,4 0 0 0 12,8 L12,3 M20,3 L20,8 A4,4 0 0 0 24,12 L29,12 "
                                    + "M29,20 L24,20 A4,4 0 0 0 20,24 L20,29 M12,29 L12,24 A4,4 0 0 0 8,20 L3,20 "
                                    + L(4, 16, 8, 16) + L(24, 16, 28, 16) + L(16, 4, 16, 8) + L(16, 24, 16, 28)),
            ["PRJROTATORIA"] = new(C(16, 16, 8.5) + L(16, 2.5, 16, 7.5) + L(16, 24.5, 16, 29.5) + L(2.5, 16, 7.5, 16) + L(24.5, 16, 29.5, 16),
                                   C(16, 16, 4)),
            ["PRJSINALIZA"] = new(Z(9, 3.5, 23, 3.5, 16, 15) + L(16, 15, 16, 29), R(3, 21, 2.6, 7) + R(7.4, 21, 2.6, 7) + R(22, 21, 2.6, 7) + R(26.4, 21, 2.6, 7)),
            ["PRJSEMAFORO"] = new(R(11, 2.5, 10, 21) + L(16, 23.5, 16, 29.5) + L(10, 29.5, 22, 29.5), C(16, 7.5, 2.4) + C(16, 13, 2.4) + C(16, 18.5, 2.4)),
            ["PRJCAPACIDADE"] = new(L(3, 16, 29, 16) + L(24, 10, 24, 22), R(4, 18.5, 4.5, 4) + R(10, 18.5, 4.5, 4) + R(16, 18.5, 4.5, 4) + R(10, 9.5, 4.5, 4) + R(16, 9.5, 4.5, 4)),
            ["PRJCASAMENTO"] = new(L(3, 24, 14, 16) + "M14,16 Q19,12.4 28,12 ", C(14, 16, 2.6), Selo.Ok),
            ["PRJCONECTAREIXO"] = new(L(3, 8, 29, 8) + L(8, 3, 8, 29) + "M21,8 A13,13 0 0 0 8,21 ", C(21, 8, 2.4) + C(8, 21, 2.4)),

            // Terraplenagem
            ["PRJPLATO"] = new(L(2, 26, 30, 26) + "M3,21 C7,17 9,23 12,20 M20,18 C23,15 26,20 29,17 ", Z(7, 26, 12, 14, 20, 14, 25, 26)),
            ["PRJCUBACAO"] = new(L(3, 10, 29, 8.5) + L(16, 23.5, 16, 28) + L(8, 25.5, 24, 25.5),
                                 Z(6, 9.83, 11.5, 21, 20.5, 21, 26, 8.68)),

            // Pavimentação
            ["PRJNOTASERVICO"] = new(R(4, 14.5, 24, 4.5) + R(4, 19, 24, 5) + L(4, 28, 28, 28) + L(4, 24, 4, 28) + L(28, 24, 28, 28),
                                     R(4, 9, 24, 5.5)),

            // SWMM / EPANET
            ["PRJSWMMCALCULAR"] = new(gotaS, Selo: Selo.Play),
            ["PRJSWMMEXPORTAR"] = new(gotaS, Selo: Selo.Sai),
            ["PRJSWMMIMPORTAR"] = new(gotaS, Selo: Selo.Entra),
            ["PRJEPANETCALCULAR"] = new(teS, Selo: Selo.Play),
            ["PRJEPANETEXPORTAR"] = new(teS, Selo: Selo.Sai),
            ["PRJEPANETIMPORTAR"] = new(teS, Selo: Selo.Entra),
            ["PRJPOPULACAO"] = new(pessoas + Seta(5, 29, 27, 29, 3.5)),
            ["PRJMOTORES"] = new("", Engrenagem(14, 14, 11, 7.8, 3.6), Selo.Play),

            // Modelador
            ["PRJMODELADOR"] = new(cubo),
            ["PRJSECOES"] = new(L(3, 7, 9, 7, 12.5, 21, 19.5, 21, 23, 7, 29, 7) + L(12.5, 26, 19.5, 26) + L(12.5, 24, 12.5, 28) + L(19.5, 24, 19.5, 28)),
            ["PRJDISPOSITIVO"] = new(R(4, 5, 18, 17) + R(8.5, 9.5, 9, 8), Selo: Selo.Mais),
            ["PRJBIBLIOTECA"] = new(R(5, 6, 5, 20) + R(11.5, 6, 5, 20) + Z(18.5, 8.2, 23, 6.8, 28.3, 25, 23.8, 26.4) + L(3, 27.5, 29, 27.5)),
            ["PRJDEMO"] = new("", Z(10, 6, 26, 16, 10, 26)),

            // Geral
            ["PRJPAINEL"] = new(R(4, 6, 24, 20) + L(12, 6, 12, 26) + L(6.5, 11, 9.5, 11) + L(6.5, 15, 9.5, 15) + L(6.5, 19, 9.5, 19)),
            ["PRJMCP"] = new(L(12.5, 4, 12.5, 11) + L(19.5, 4, 19.5, 11) + "M9,11 L23,11 L23,16 A7,7 0 0 1 9,16 Z " + L(16, 23, 16, 29)),
            ["PRJAJUDA"] = new("M10.5,11 A5.5,5.5 0 1 1 18.8,15.7 C17,16.8 16,18 16,20.5 ", C(16, 26, 1.9)),
            ["PRJABA"] = new("M3,26 L3,12 L9,12 L11,6.5 L20,6.5 L22,12 L29,12 L29,26 ", Selo: Selo.Refaz),

            // CTGeo (CivilTools): só o desenho do botão, os comandos são do CTGeo
            ["CTGEO"] = new(Globo(16, 16, 11)),
            ["CTSTREET"] = new(E(16, 26.5, 10, 2.8), C(16, 7, 3.5) + Z(12, 12, 20, 12, 21.5, 20, 18.5, 20, 18, 27, 14, 27, 13.5, 20, 10.5, 20)),
            ["CTBROWSER"] = new(R(4, 6, 24, 20) + L(4, 11.5, 28, 11.5) + C(16, 19, 4.3) + L(11.7, 19, 20.3, 19), C(7.5, 8.8, 1) + C(10.8, 8.8, 1)),
            ["CTEARTH"] = new(Globo(16, 16, 8.5) + "M4.2,21.5 A13,4.5 -25 1 0 27.8,10.5 "),
            ["CTGEOEXPORT"] = new(Globo(14, 14, 10.5), Selo: Selo.Sai),
            ["CTKMLABRIR"] = new(Z(4, 8.5, 12, 8.5, 14.5, 12, 28, 12, 28, 26, 4, 26)),
            ["CTKMLIMPORTAR"] = new(Globo(14, 14, 10.5), Selo: Selo.Entra),
            ["CSTKML"] = new("M11,5 Q4,16 11,27 M21,5 Q28,16 21,27 " + C(16, 16, 4.5) + L(11.5, 16, 20.5, 16)),
            ["CTGEOCOORD"] = new(C(16, 16, 8) + L(16, 3, 16, 10) + L(16, 22, 16, 29) + L(3, 16, 10, 16) + L(22, 16, 29, 16), C(16, 16, 2)),
            ["CTGEOSTATUS"] = new(C(16, 16, 12) + L(16, 14, 16, 23), C(16, 9.5, 1.9)),
        };
    }
}
