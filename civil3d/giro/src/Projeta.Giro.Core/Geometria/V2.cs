using System.Text.Json.Serialization;

namespace Projeta.Giro.Core.Geometria;

/// <summary>Vetor/ponto 2D (metros, X = Leste, Y = Norte).</summary>
public readonly record struct V2(double X, double Y)
{
    public static readonly V2 Zero = new(0, 0);

    public static V2 operator +(V2 a, V2 b) => new(a.X + b.X, a.Y + b.Y);
    public static V2 operator -(V2 a, V2 b) => new(a.X - b.X, a.Y - b.Y);
    public static V2 operator -(V2 a) => new(-a.X, -a.Y);
    public static V2 operator *(V2 a, double k) => new(a.X * k, a.Y * k);
    public static V2 operator *(double k, V2 a) => new(a.X * k, a.Y * k);
    public static V2 operator /(V2 a, double k) => new(a.X / k, a.Y / k);

    [JsonIgnore] public double Comprimento => Math.Sqrt(X * X + Y * Y);
    [JsonIgnore] public double Comprimento2 => X * X + Y * Y;
    [JsonIgnore] public double Angulo => Math.Atan2(Y, X);

    public double Dot(V2 b) => X * b.X + Y * b.Y;
    /// <summary>Produto vetorial (z): positivo se b está à esquerda de this.</summary>
    public double Cross(V2 b) => X * b.Y - Y * b.X;
    public double Dist(V2 b) => (this - b).Comprimento;

    public V2 Unit()
    {
        double l = Comprimento;
        return l < 1e-15 ? Zero : new V2(X / l, Y / l);
    }

    /// <summary>Perpendicular à esquerda (rotação de +90°).</summary>
    public V2 Esq() => new(-Y, X);
    /// <summary>Perpendicular à direita (rotação de -90°).</summary>
    public V2 Dir() => new(Y, -X);

    public V2 Rot(double ang)
    {
        double c = Math.Cos(ang), s = Math.Sin(ang);
        return new V2(X * c - Y * s, X * s + Y * c);
    }

    public static V2 Polar(double ang, double r = 1) => new(Math.Cos(ang) * r, Math.Sin(ang) * r);
    public static V2 Lerp(V2 a, V2 b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>Ponto no referencial de um corpo: origem o, eixo x na direção do ângulo h.</summary>
    public static V2 Local(V2 o, double h, double x, double y) => o + Polar(h, x) + Polar(h + Math.PI / 2, y);

    public override string ToString() => $"({X:0.###}; {Y:0.###})";
}

/// <summary>Vértice de polilinha com bulge (tan(ângulo/4) do arco até o próximo vértice; 0 = reta).</summary>
public readonly record struct VB(V2 P, double Bulge = 0);

/// <summary>Polilinha com arcos (bulges), como a LWPOLYLINE do AutoCAD.</summary>
public sealed class Pl
{
    public List<VB> V { get; set; } = new();
    public bool Fechada { get; set; }

    public Pl() { }
    public Pl(IEnumerable<V2> pts, bool fechada = false)
    {
        V = pts.Select(p => new VB(p)).ToList();
        Fechada = fechada;
    }

    public int Count => V.Count;

    public void Add(V2 p, double bulge = 0) => V.Add(new VB(p, bulge));

    /// <summary>Define o bulge do último vértice (arco até o próximo que for adicionado).</summary>
    public void BulgeUltimo(double b)
    {
        if (V.Count == 0) return;
        V[^1] = V[^1] with { Bulge = b };
    }

    /// <summary>Pontos discretizados (arcos em cordas com flecha máxima <paramref name="flecha"/>).</summary>
    public List<V2> Pontos(double flecha = 0.01)
    {
        var r = new List<V2>();
        int n = V.Count;
        if (n == 0) return r;
        int segs = Fechada ? n : n - 1;
        for (int i = 0; i < segs; i++)
        {
            var a = V[i];
            var b = V[(i + 1) % n];
            r.Add(a.P);
            if (Math.Abs(a.Bulge) > 1e-12) r.AddRange(Geo.PontosArcoBulge(a.P, b.P, a.Bulge, flecha, false));
        }
        if (!Fechada) r.Add(V[^1].P);
        return r;
    }

    public double Comprimento()
    {
        double s = 0;
        int n = V.Count, segs = Fechada ? n : n - 1;
        for (int i = 0; i < segs; i++) s += Geo.ComprimentoSegmento(V[i].P, V[(i + 1) % n].P, V[i].Bulge);
        return s;
    }
}
