using System.Globalization;

namespace Projeta.Core.Comum;

/// <summary>Ponto/vetor 2D simples (independente do AutoCAD).</summary>
public readonly record struct P2(double X, double Y)
{
    public static P2 operator +(P2 a, P2 b) => new(a.X + b.X, a.Y + b.Y);
    public static P2 operator -(P2 a, P2 b) => new(a.X - b.X, a.Y - b.Y);
    public static P2 operator *(P2 a, double k) => new(a.X * k, a.Y * k);
    public static P2 operator *(double k, P2 a) => new(a.X * k, a.Y * k);
    public double Comprimento => Math.Sqrt(X * X + Y * Y);
    public double Distancia(P2 o) => (this - o).Comprimento;
    public P2 Unitario() { var c = Comprimento; return c < 1e-12 ? new P2(0, 0) : new P2(X / c, Y / c); }
    public P2 Perpendicular() => new(-Y, X);
    public double Dot(P2 o) => X * o.X + Y * o.Y;
    public double Cross(P2 o) => X * o.Y - Y * o.X;
    public double Angulo => Math.Atan2(Y, X);
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.###}, {Y:0.###})");
}

/// <summary>Ponto/vetor 3D simples.</summary>
public readonly record struct P3(double X, double Y, double Z)
{
    public static P3 operator +(P3 a, P3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static P3 operator -(P3 a, P3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static P3 operator *(P3 a, double k) => new(a.X * k, a.Y * k, a.Z * k);
    public double Comprimento => Math.Sqrt(X * X + Y * Y + Z * Z);
    public P2 XY => new(X, Y);
    public P3 Unitario() { var c = Comprimento; return c < 1e-12 ? new P3(0, 0, 0) : new P3(X / c, Y / c, Z / c); }
    public double Dot(P3 o) => X * o.X + Y * o.Y + Z * o.Z;
    public P3 Cross(P3 o) => new(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);
}

public static class Geo
{
    /// <summary>Área (com sinal) de um polígono pela fórmula de Gauss. Positiva no sentido anti-horário.</summary>
    public static double AreaComSinal(IReadOnlyList<P2> pts)
    {
        double a = 0;
        for (int i = 0, n = pts.Count; i < n; i++)
        {
            var p = pts[i]; var q = pts[(i + 1) % n];
            a += p.X * q.Y - q.X * p.Y;
        }
        return a / 2;
    }

    public static double Area(IReadOnlyList<P2> pts) => Math.Abs(AreaComSinal(pts));

    public static double Perimetro(IReadOnlyList<P2> pts, bool fechado = true)
    {
        double s = 0;
        int n = pts.Count;
        for (int i = 0; i < (fechado ? n : n - 1); i++) s += pts[i].Distancia(pts[(i + 1) % n]);
        return s;
    }

    public static P2 Centroide(IReadOnlyList<P2> pts)
    {
        double a = AreaComSinal(pts);
        if (Math.Abs(a) < 1e-12)
            return new P2(pts.Average(p => p.X), pts.Average(p => p.Y));
        double cx = 0, cy = 0;
        for (int i = 0, n = pts.Count; i < n; i++)
        {
            var p = pts[i]; var q = pts[(i + 1) % n];
            double f = p.X * q.Y - q.X * p.Y;
            cx += (p.X + q.X) * f; cy += (p.Y + q.Y) * f;
        }
        return new P2(cx / (6 * a), cy / (6 * a));
    }

    /// <summary>Interseção de dois segmentos 2D. Retorna os parâmetros t (em AB) e u (em CD) se houver.</summary>
    public static bool IntersecaoSegmentos(P2 a, P2 b, P2 c, P2 d, out double t, out double u)
    {
        var r = b - a; var s = d - c;
        double den = r.Cross(s);
        t = u = double.NaN;
        if (Math.Abs(den) < 1e-12) return false;
        var ca = c - a;
        t = ca.Cross(s) / den;
        u = ca.Cross(r) / den;
        return t >= -1e-9 && t <= 1 + 1e-9 && u >= -1e-9 && u <= 1 + 1e-9;
    }

    /// <summary>
    /// Recorta um polígono (lista de pontos) pelo semiplano Y &lt;= nivel (algoritmo de Sutherland–Hodgman).
    /// Usado para obter a área molhada de seções abertas.
    /// </summary>
    public static List<P2> RecortarAbaixoDe(IReadOnlyList<P2> poly, double nivel)
    {
        var saida = new List<P2>();
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            var atual = poly[i]; var prox = poly[(i + 1) % n];
            bool dentroA = atual.Y <= nivel + 1e-12, dentroP = prox.Y <= nivel + 1e-12;
            if (dentroA) saida.Add(atual);
            if (dentroA != dentroP)
            {
                double t = (nivel - atual.Y) / (prox.Y - atual.Y);
                saida.Add(new P2(atual.X + t * (prox.X - atual.X), nivel));
            }
        }
        return saida;
    }

    /// <summary>Verdadeiro se arestas não adjacentes do polígono fechado se cruzam (polígono inválido para sólidos).</summary>
    public static bool AutoIntersecta(IReadOnlyList<P2> pts)
    {
        int n = pts.Count;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                if (j == i + 1 || (i == 0 && j == n - 1)) continue; // adjacentes
                var a = pts[i]; var b = pts[(i + 1) % n]; var c = pts[j]; var d = pts[(j + 1) % n];
                if (IntersecaoSegmentos(a, b, c, d, out var t, out var u) && t > 1e-9 && t < 1 - 1e-9 && u > 1e-9 && u < 1 - 1e-9) return true;
                // vértice tocando outra aresta (ex.: polígono que passa duas vezes pelo mesmo ponto)
                if (a.Distancia(c) < 1e-9 || a.Distancia(d) < 1e-9 || b.Distancia(c) < 1e-9 || b.Distancia(d) < 1e-9) return true;
            }
        return false;
    }

    /// <summary>Ângulo de texto legível (entre -90° e +90°) a partir de um ângulo qualquer em radianos.</summary>
    public static double AnguloLegivel(double ang)
    {
        while (ang > Math.PI) ang -= 2 * Math.PI;
        while (ang <= -Math.PI) ang += 2 * Math.PI;
        if (ang > Math.PI / 2 + 1e-9) ang -= Math.PI;
        else if (ang < -Math.PI / 2 - 1e-9) ang += Math.PI;
        return ang;
    }
}
