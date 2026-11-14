using Projeta.Core.Comum;

namespace Projeta.Core.Hidraulica;

/// <summary>Seção transversal hidráulica (geometria em função da altura de lâmina y, em metros).</summary>
public interface ISecao
{
    /// <summary>Altura interna total da seção (m).</summary>
    double Altura { get; }
    double Area(double y);
    double PerimetroMolhado(double y);
    double LarguraSuperficial(double y);
    /// <summary>Altura de lâmina em que a vazão por Manning é máxima (para busca monotônica).</summary>
    double AlturaVazaoMaxima { get; }
    string Descricao { get; }
}

public sealed class SecaoCircular : ISecao
{
    public double D { get; }
    public SecaoCircular(double diametro) { if (diametro <= 0) throw new ArgumentOutOfRangeException(nameof(diametro)); D = diametro; }
    public double Altura => D;
    public double AlturaVazaoMaxima => 0.9382 * D;
    public string Descricao => $"Ø{D * 1000:0}";

    private double Theta(double y)
    {
        y = Math.Clamp(y, 0, D);
        return 2 * Math.Acos(1 - 2 * y / D);
    }
    public double Area(double y) { var t = Theta(y); return D * D / 8 * (t - Math.Sin(t)); }
    public double PerimetroMolhado(double y) => Theta(y) * D / 2;
    public double LarguraSuperficial(double y) => D * Math.Sin(Theta(y) / 2);
}

/// <summary>Galeria retangular fechada (b x h).</summary>
public sealed class SecaoRetangularFechada : ISecao
{
    public double B { get; }
    public double H { get; }
    public SecaoRetangularFechada(double largura, double altura) { B = largura; H = altura; }
    public double Altura => H;
    public double AlturaVazaoMaxima => 0.97 * H; // aproximação: perímetro aumenta bruscamente ao fechar
    public string Descricao => $"{B:0.00}x{H:0.00}";
    public double Area(double y) => B * Math.Clamp(y, 0, H);
    public double PerimetroMolhado(double y) => y >= H ? 2 * (B + H) : B + 2 * Math.Clamp(y, 0, H);
    public double LarguraSuperficial(double y) => y >= H ? 0 : B;
}

/// <summary>
/// Seção aberta definida por um contorno (offset, cota) da borda esquerda, passando pelo fundo, até a borda direita.
/// A lâmina y é medida a partir do ponto mais baixo do contorno.
/// </summary>
public sealed class SecaoPoligonalAberta : ISecao
{
    private readonly List<P2> _contorno;
    private readonly double _zMin;
    public SecaoPoligonalAberta(IEnumerable<P2> contorno)
    {
        _contorno = contorno.ToList();
        if (_contorno.Count < 3) throw new ArgumentException("Contorno com menos de 3 pontos");
        _zMin = _contorno.Min(p => p.Y);
        Altura = Math.Min(_contorno[0].Y, _contorno[^1].Y) - _zMin;
    }
    public double Altura { get; }
    public double AlturaVazaoMaxima => Altura;
    public string Descricao => "Seção aberta";
    public IReadOnlyList<P2> Contorno => _contorno;

    public double Area(double y)
    {
        if (y <= 0) return 0;
        var nivel = _zMin + y;
        var rec = Geo.RecortarAbaixoDe(_contorno, nivel);
        return rec.Count < 3 ? 0 : Geo.Area(rec);
    }

    public double PerimetroMolhado(double y)
    {
        if (y <= 0) return 0;
        var nivel = _zMin + y;
        double p = 0;
        for (int i = 0; i < _contorno.Count - 1; i++)
        {
            var a = _contorno[i]; var b = _contorno[i + 1];
            if (a.Y > nivel && b.Y > nivel) continue;
            if (a.Y <= nivel && b.Y <= nivel) { p += a.Distancia(b); continue; }
            double t = (nivel - a.Y) / (b.Y - a.Y);
            var m = new P2(a.X + t * (b.X - a.X), nivel);
            p += a.Y <= nivel ? a.Distancia(m) : m.Distancia(b);
        }
        return p;
    }

    public double LarguraSuperficial(double y)
    {
        double nivel = _zMin + y, xmin = double.MaxValue, xmax = double.MinValue;
        for (int i = 0; i < _contorno.Count - 1; i++)
        {
            var a = _contorno[i]; var b = _contorno[i + 1];
            if ((a.Y - nivel) * (b.Y - nivel) <= 0 && Math.Abs(b.Y - a.Y) > 1e-12)
            {
                double t = (nivel - a.Y) / (b.Y - a.Y);
                double x = a.X + t * (b.X - a.X);
                xmin = Math.Min(xmin, x); xmax = Math.Max(xmax, x);
            }
        }
        return xmax > xmin ? xmax - xmin : 0;
    }

    public static SecaoPoligonalAberta Trapezoidal(double baseB, double taludeEsq, double taludeDir, double altura) =>
        new(new[] { new P2(-baseB / 2 - taludeEsq * altura, altura), new P2(-baseB / 2, 0), new P2(baseB / 2, 0), new P2(baseB / 2 + taludeDir * altura, altura) });

    public static SecaoPoligonalAberta Triangular(double taludeEsq, double taludeDir, double altura) =>
        new(new[] { new P2(-taludeEsq * altura, altura), new P2(0, 0), new P2(taludeDir * altura, altura) });
}
