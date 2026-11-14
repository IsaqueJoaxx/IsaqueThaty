using Projeta.Sinalizacao.Core.Comum;

namespace Projeta.Sinalizacao.Core.Desenho;

/// <summary>Traços espessos (viram áreas preenchidas), setas e arcos.</summary>
public static class Pincel
{
    /// <summary>Polilinha espessa: um retângulo por segmento e um disco em cada junta.</summary>
    public static double Tracos(Desenho2D d, IList<V2> pts, double esp, Cor cor, bool pontasRedondas = false, bool fechado = false)
    {
        double comp = 0;
        if (pts.Count == 0) return 0;
        if (pts.Count == 1)
        {
            // ponto isolado = quadrado (pingo do i, ponto final)
            var p = pts[0];
            d.Area(new[] { p + new V2(-esp / 2, -esp / 2), p + new V2(esp / 2, -esp / 2), p + new V2(esp / 2, esp / 2), p + new V2(-esp / 2, esp / 2) }, cor);
            return esp;
        }
        int n = fechado ? pts.Count : pts.Count - 1;
        for (int i = 0; i < n; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            double l = V2.Dist(a, b);
            if (l < 1e-9) continue;
            comp += l;
            var nn = (b - a).Unitario().Esquerda * (esp / 2);
            d.Area(new[] { a - nn, b - nn, b + nn, a + nn }, cor);
        }
        int ini = fechado ? 0 : 1, fim = fechado ? pts.Count : pts.Count - 1;
        for (int i = ini; i < fim; i++) d.Disco(pts[i], esp / 2, cor);
        if (pontasRedondas && !fechado) { d.Disco(pts[0], esp / 2, cor); d.Disco(pts[^1], esp / 2, cor); }
        return comp;
    }

    /// <summary>Seta: corpo espesso ao longo do caminho e cabeça triangular na ponta final.</summary>
    public static void Seta(Desenho2D d, IList<V2> pts, double esp, Cor cor, double? largCabeca = null, double? compCabeca = null, bool cabecaInicio = false)
    {
        if (pts.Count < 2) return;
        double lc = largCabeca ?? esp * 2.8;
        double cc = compCabeca ?? esp * 2.4;
        var corpo = pts.ToList();
        if (cabecaInicio) corpo.Reverse();
        // encurta o corpo pela cabeça
        var ponta = corpo[^1];
        var dir = (corpo[^1] - corpo[^2]).Unitario();
        double resta = cc * 0.9;
        while (corpo.Count >= 2 && resta > 0)
        {
            double l = V2.Dist(corpo[^1], corpo[^2]);
            if (l > resta) { corpo[^1] = corpo[^1] + (corpo[^2] - corpo[^1]).Unitario() * resta; resta = 0; }
            else { resta -= l; corpo.RemoveAt(corpo.Count - 1); }
        }
        if (corpo.Count >= 2) Tracos(d, corpo, esp, cor);
        var baseC = ponta - dir * cc;
        var n = dir.Esquerda * (lc / 2);
        d.Area(new[] { baseC + n, ponta, baseC - n }, cor);
    }

    /// <summary>Pontos de um arco (ângulos em graus, sentido de a0 para a1).</summary>
    public static List<V2> Arco(V2 c, double r, double a0, double a1, int n = 24)
    {
        var l = new List<V2>(n + 1);
        for (int i = 0; i <= n; i++) l.Add(c + V2.Polar(Num.Rad(a0 + (a1 - a0) * i / n), r));
        return l;
    }

    /// <summary>Anel (circunferência espessa) como traços fechados.</summary>
    public static void Anel(Desenho2D d, V2 c, double r, double esp, Cor cor, int n = 48)
    {
        var pts = Arco(c, r, 0, 360, n);
        pts.RemoveAt(pts.Count - 1);
        Tracos(d, pts, esp, cor, fechado: true);
    }

    public static void Retangulo(Desenho2D d, double x0, double y0, double x1, double y1, Cor cor)
        => d.Area(new[] { new V2(x0, y0), new V2(x1, y0), new V2(x1, y1), new V2(x0, y1) }, cor);
}
