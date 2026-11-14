using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Projeta.Sinalizacao.Core.Desenho;
using Cor = Projeta.Sinalizacao.Core.Desenho.Cor;

namespace Projeta.Sinalizacao.UI;

/// <summary>Prévia de um <see cref="Desenho2D"/> (placas, setas, legendas) em GDI+.</summary>
internal sealed class Previa : Control
{
    private Desenho2D? _d;
    public Desenho2D? Desenho { get => _d; set { _d = value; Invalidate(); } }
    public string Legenda { get; set; } = "";

    public Previa()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Color.FromArgb(214, 219, 224);
    }

    private static Color C(Cor c) { var (r, g, b) = Cores.Rgb(c); return Color.FromArgb(r, g, b); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float altLeg = string.IsNullOrEmpty(Legenda) ? 0 : 18;
        if (_d is null || _d.Itens.Count == 0) { TextRenderer.DrawText(g, "(sem prévia)", Font, ClientRectangle, Color.Gray); return; }
        var cx = _d.Limites();
        if (cx.EhVazia) return;
        float m = 10;
        float w = Width - 2 * m, h = Height - 2 * m - altLeg;
        double k = Math.Min(w / Math.Max(1e-9, cx.Largura), h / Math.Max(1e-9, cx.Altura));
        float ox = (float)(m + (w - cx.Largura * k) / 2), oy = (float)(m + (h - cx.Altura * k) / 2);
        PointF P(Projeta.Sinalizacao.Core.Comum.V2 v) => new((float)(ox + (v.X - cx.XMin) * k), (float)(oy + (cx.YMax - v.Y) * k));
        foreach (var p in _d.Itens)
        {
            switch (p)
            {
                case PArea a:
                    using (var br = new SolidBrush(C(a.Cor)))
                    using (var pen = new Pen(C(a.Cor), 0.6f))
                    {
                        var pts = a.Pts.Select(P).ToArray();
                        g.FillPolygon(br, pts);
                        g.DrawPolygon(pen, pts);
                    }
                    break;
                case PDisco d:
                    using (var br = new SolidBrush(C(d.Cor)))
                    {
                        var c = P(d.C);
                        float r = (float)(d.R * k);
                        g.FillEllipse(br, c.X - r, c.Y - r, 2 * r, 2 * r);
                    }
                    break;
                case PLinha l:
                    using (var pen = new Pen(C(l.Cor), 1f))
                    {
                        var pts = l.Pts.Select(P).ToArray();
                        if (pts.Length >= 2) { if (l.Fechada) g.DrawPolygon(pen, pts); else g.DrawLines(pen, pts); }
                    }
                    break;
            }
        }
        if (altLeg > 0)
            TextRenderer.DrawText(g, Legenda, Font, new Rectangle(0, Height - (int)altLeg - 2, Width, (int)altLeg), Color.FromArgb(40, 40, 40), TextFormatFlags.HorizontalCenter);
    }
}
