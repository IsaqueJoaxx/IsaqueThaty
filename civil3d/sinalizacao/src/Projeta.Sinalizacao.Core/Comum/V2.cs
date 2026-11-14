using System.Globalization;
using System.Text.Json.Serialization;

namespace Projeta.Sinalizacao.Core.Comum;

/// <summary>Vetor/ponto 2D em metros.</summary>
public readonly record struct V2(double X, double Y)
{
    public static readonly V2 Zero = new(0, 0);
    public static V2 operator +(V2 a, V2 b) => new(a.X + b.X, a.Y + b.Y);
    public static V2 operator -(V2 a, V2 b) => new(a.X - b.X, a.Y - b.Y);
    public static V2 operator -(V2 a) => new(-a.X, -a.Y);
    public static V2 operator *(V2 a, double k) => new(a.X * k, a.Y * k);
    public static V2 operator *(double k, V2 a) => new(a.X * k, a.Y * k);
    public static V2 operator /(V2 a, double k) => new(a.X / k, a.Y / k);

    [JsonIgnore]
    public double Comprimento => Math.Sqrt(X * X + Y * Y);
    [JsonIgnore]
    public double Comprimento2 => X * X + Y * Y;
    [JsonIgnore]
    public double Angulo => Math.Atan2(Y, X);

    public V2 Unitario()
    {
        double l = Comprimento;
        return l < 1e-12 ? new V2(1, 0) : new V2(X / l, Y / l);
    }

    /// <summary>Normal à esquerda (rotação de +90°).</summary>
    [JsonIgnore]
    public V2 Esquerda => new(-Y, X);

    public V2 Girar(double ang)
    {
        double c = Math.Cos(ang), s = Math.Sin(ang);
        return new V2(X * c - Y * s, X * s + Y * c);
    }

    public static double Dot(V2 a, V2 b) => a.X * b.X + a.Y * b.Y;
    public static double Cross(V2 a, V2 b) => a.X * b.Y - a.Y * b.X;
    public static double Dist(V2 a, V2 b) => (a - b).Comprimento;
    public static V2 Polar(double ang, double r = 1) => new(Math.Cos(ang) * r, Math.Sin(ang) * r);
    public static V2 Lerp(V2 a, V2 b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.###}, {Y:0.###})");
}

/// <summary>Retângulo envolvente.</summary>
public readonly record struct Caixa(double XMin, double YMin, double XMax, double YMax)
{
    public static readonly Caixa Vazia = new(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);
    [JsonIgnore]
    public bool EhVazia => XMin > XMax;
    [JsonIgnore]
    public double Largura => EhVazia ? 0 : XMax - XMin;
    [JsonIgnore]
    public double Altura => EhVazia ? 0 : YMax - YMin;
    [JsonIgnore]
    public V2 Centro => new((XMin + XMax) / 2, (YMin + YMax) / 2);
    public Caixa Incluir(V2 p) => new(Math.Min(XMin, p.X), Math.Min(YMin, p.Y), Math.Max(XMax, p.X), Math.Max(YMax, p.Y));
    public Caixa Unir(Caixa o) => o.EhVazia ? this : EhVazia ? o : new(Math.Min(XMin, o.XMin), Math.Min(YMin, o.YMin), Math.Max(XMax, o.XMax), Math.Max(YMax, o.YMax));
    public Caixa Expandir(double m) => EhVazia ? this : new(XMin - m, YMin - m, XMax + m, YMax + m);
}
