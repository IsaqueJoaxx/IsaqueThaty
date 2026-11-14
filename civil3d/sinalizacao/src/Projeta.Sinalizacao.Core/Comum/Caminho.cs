using System.Text.Json.Serialization;

namespace Projeta.Sinalizacao.Core.Comum;

/// <summary>Vértice de polilinha com bulge (convenção do AutoCAD: bulge = tan(θ/4), positivo = anti-horário).</summary>
public readonly record struct Vertice(double X, double Y, double Bulge = 0)
{
    [JsonIgnore]
    public V2 P => new(X, Y);
}

/// <summary>Trecho de um caminho: reta ou arco.</summary>
public abstract class Segmento
{
    public abstract V2 Inicio { get; }
    public abstract V2 Fim { get; }
    public abstract double Comprimento { get; }
    /// <summary>Ponto a uma distância t (0..Comprimento) do início.</summary>
    public abstract V2 Ponto(double t);
    /// <summary>Direção unitária em t.</summary>
    public abstract V2 Tangente(double t);
    /// <summary>Raio (∞ para retas) e sentido (+1 esquerda / -1 direita).</summary>
    public virtual double Raio => double.PositiveInfinity;
}

public sealed class SegReta : Segmento
{
    private readonly V2 _a, _b, _u;
    private readonly double _l;
    public SegReta(V2 a, V2 b) { _a = a; _b = b; _l = V2.Dist(a, b); _u = (b - a).Unitario(); }
    public override V2 Inicio => _a;
    public override V2 Fim => _b;
    public override double Comprimento => _l;
    public override V2 Ponto(double t) => _a + _u * t;
    public override V2 Tangente(double t) => _u;
}

public sealed class SegArco : Segmento
{
    public V2 Centro { get; }
    public double R { get; }
    public double A0 { get; }
    /// <summary>Ângulo varrido com sinal (positivo = anti-horário).</summary>
    public double Varredura { get; }
    private readonly V2 _a, _b;

    public SegArco(V2 a, V2 b, double bulge)
    {
        _a = a; _b = b;
        double c = V2.Dist(a, b);
        Varredura = 4 * Math.Atan(bulge);
        R = c * (1 + bulge * bulge) / (4 * Math.Abs(bulge));
        var m = (a + b) / 2;
        var n = (b - a).Unitario().Esquerda;
        double d = R - Math.Abs(bulge) * c / 2; // distância do meio da corda ao centro (com sinal)
        Centro = m + n * (Math.Sign(bulge) * d);
        A0 = (a - Centro).Angulo;
    }

    public override V2 Inicio => _a;
    public override V2 Fim => _b;
    public override double Comprimento => R * Math.Abs(Varredura);
    public override double Raio => R;
    private double Ang(double t) => A0 + Math.Sign(Varredura) * t / R;
    public override V2 Ponto(double t) => Centro + V2.Polar(Ang(t), R);
    public override V2 Tangente(double t)
    {
        double a = Ang(t);
        return Varredura > 0 ? new V2(-Math.Sin(a), Math.Cos(a)) : new V2(Math.Sin(a), -Math.Cos(a));
    }
}

/// <summary>
/// Caminho (polilinha com retas e arcos) parametrizado pela distância acumulada.
/// Usado para lançar faixas, tachas, defensas, cones e projetar estacas.
/// </summary>
public sealed class Caminho
{
    private readonly List<Segmento> _segs = new();
    private readonly List<double> _acum = new(); // distância acumulada no início de cada segmento
    public IReadOnlyList<Segmento> Segmentos => _segs;
    public double Comprimento { get; private set; }
    public bool Fechado { get; }
    public IReadOnlyList<Vertice> Vertices { get; }

    public Caminho(IList<Vertice> vertices, bool fechado = false)
    {
        Fechado = fechado;
        Vertices = vertices.ToList();
        int n = vertices.Count;
        int nseg = fechado ? n : n - 1;
        for (int i = 0; i < nseg; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % n];
            if (V2.Dist(a.P, b.P) < 1e-9) continue;
            Segmento s = Math.Abs(a.Bulge) > 1e-9 ? new SegArco(a.P, b.P, a.Bulge) : new SegReta(a.P, b.P);
            _acum.Add(Comprimento);
            _segs.Add(s);
            Comprimento += s.Comprimento;
        }
    }

    public static Caminho DePontos(params V2[] pts) => new(pts.Select(p => new Vertice(p.X, p.Y)).ToList());
    public static Caminho DePontos(IEnumerable<V2> pts, bool fechado = false) => new(pts.Select(p => new Vertice(p.X, p.Y)).ToList(), fechado);

    private (int i, double t) Localizar(double s)
    {
        if (_segs.Count == 0) return (-1, 0);
        if (s <= 0) return (0, 0);
        if (s >= Comprimento) return (_segs.Count - 1, _segs[^1].Comprimento);
        int lo = 0, hi = _segs.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (_acum[mid] <= s) lo = mid; else hi = mid - 1;
        }
        return (lo, s - _acum[lo]);
    }

    public V2 Ponto(double s)
    {
        var (i, t) = Localizar(s);
        if (i < 0) return Vertices.Count > 0 ? Vertices[0].P : V2.Zero;
        // extrapolação linear além das pontas
        if (s < 0) return _segs[0].Inicio + _segs[0].Tangente(0) * s;
        if (s > Comprimento) return _segs[^1].Fim + _segs[^1].Tangente(_segs[^1].Comprimento) * (s - Comprimento);
        return _segs[i].Ponto(t);
    }

    public V2 Tangente(double s)
    {
        var (i, t) = Localizar(s);
        return i < 0 ? new V2(1, 0) : _segs[i].Tangente(t);
    }

    /// <summary>Ponto deslocado lateralmente (positivo = à esquerda do sentido do caminho).</summary>
    public V2 PontoDeslocado(double s, double desloc) => Ponto(s) + Tangente(s).Esquerda * desloc;

    /// <summary>Raio de curvatura em s (∞ em tangente).</summary>
    public double Raio(double s)
    {
        var (i, _) = Localizar(s);
        return i < 0 ? double.PositiveInfinity : _segs[i].Raio;
    }

    /// <summary>
    /// Pontos do caminho entre s0 e s1, incluindo vértices internos e amostrando arcos
    /// com flecha máxima <paramref name="tolCorda"/>.
    /// </summary>
    public List<V2> Trecho(double s0, double s1, double tolCorda = 0.01)
    {
        var r = new List<V2>();
        if (_segs.Count == 0) return r;
        if (s1 < s0) (s0, s1) = (s1, s0);
        r.Add(Ponto(s0));
        for (int i = 0; i < _segs.Count; i++)
        {
            double a = _acum[i], b = a + _segs[i].Comprimento;
            if (b <= s0 || a >= s1) continue;
            double ta = Math.Max(s0, a) - a, tb = Math.Min(s1, b) - a;
            if (_segs[i] is SegArco arco)
            {
                double dAng = 2 * Math.Acos(Math.Max(-1, Math.Min(1, 1 - tolCorda / arco.R)));
                double passo = Math.Max(1e-3, Math.Min(arco.R * dAng, 5.0));
                int n = (int)Math.Ceiling((tb - ta) / passo);
                for (int k = 1; k < n; k++) r.Add(arco.Ponto(ta + (tb - ta) * k / n));
            }
            if (b < s1) r.Add(_segs[i].Fim);
        }
        var fim = Ponto(s1);
        if (V2.Dist(r[^1], fim) > 1e-9) r.Add(fim);
        return r;
    }

    /// <summary>Desloca uma polilinha aberta (positivo = esquerda) com juntas em esquadria limitadas.</summary>
    public static List<V2> Deslocar(IList<V2> pl, double d)
    {
        var r = new List<V2>(pl.Count);
        if (pl.Count < 2) { r.AddRange(pl); return r; }
        for (int i = 0; i < pl.Count; i++)
        {
            V2 n;
            if (i == 0) n = (pl[1] - pl[0]).Unitario().Esquerda;
            else if (i == pl.Count - 1) n = (pl[i] - pl[i - 1]).Unitario().Esquerda;
            else
            {
                var n1 = (pl[i] - pl[i - 1]).Unitario().Esquerda;
                var n2 = (pl[i + 1] - pl[i]).Unitario().Esquerda;
                var m = (n1 + n2);
                if (m.Comprimento < 1e-9) { n = n1; }
                else
                {
                    m = m.Unitario();
                    double cos = V2.Dot(m, n1);
                    double k = cos < 0.25 ? 4 : 1 / cos; // limite da esquadria
                    r.Add(pl[i] + m * (d * k));
                    continue;
                }
            }
            r.Add(pl[i] + n * d);
        }
        return r;
    }

    /// <summary>Polígono de uma faixa pintada entre s0 e s1, centrada no deslocamento informado.</summary>
    public List<V2> Faixa(double s0, double s1, double desloc, double largura, double tolCorda = 0.01)
    {
        var eixo = Trecho(s0, s1, tolCorda);
        var esq = Deslocar(eixo, desloc + largura / 2);
        var dir = Deslocar(eixo, desloc - largura / 2);
        dir.Reverse();
        esq.AddRange(dir);
        return esq;
    }

    /// <summary>Projeta um ponto no caminho: distância ao longo (s) e afastamento (positivo à esquerda).</summary>
    public (double s, double afast, double dist) Projetar(V2 p)
    {
        double melhorD = double.MaxValue, melhorS = 0, melhorOff = 0;
        for (int i = 0; i < _segs.Count; i++)
        {
            var seg = _segs[i];
            double t;
            if (seg is SegReta)
            {
                var u = seg.Tangente(0);
                t = Math.Max(0, Math.Min(seg.Comprimento, V2.Dot(p - seg.Inicio, u)));
            }
            else
            {
                var arco = (SegArco)seg;
                double ang = Num.NormAng((p - arco.Centro).Angulo - arco.A0);
                double sweep = arco.Varredura;
                double frac = sweep > 0 ? (ang < 0 ? ang + 2 * Math.PI : ang) / sweep : (ang > 0 ? ang - 2 * Math.PI : ang) / sweep;
                if (frac > 1)
                {
                    // fora do arco: escolhe a ponta mais próxima
                    frac = V2.Dist(p, arco.Inicio) < V2.Dist(p, arco.Fim) ? 0 : 1;
                }
                t = Math.Max(0, Math.Min(1, frac)) * seg.Comprimento;
            }
            var q = seg.Ponto(t);
            double d = V2.Dist(p, q);
            if (d < melhorD - 1e-12)
            {
                melhorD = d;
                melhorS = _acum[i] + t;
                melhorOff = V2.Cross(seg.Tangente(t), p - q) >= 0 ? d : -d;
            }
        }
        return (melhorS, melhorOff, melhorD);
    }

    /// <summary>Caminho invertido (mesma geometria, sentido oposto).</summary>
    public Caminho Invertido()
    {
        var v = Vertices.ToList();
        var r = new List<Vertice>(v.Count);
        for (int i = v.Count - 1; i >= 0; i--)
        {
            double b = i > 0 ? -v[i - 1].Bulge : 0;
            r.Add(new Vertice(v[i].X, v[i].Y, b));
        }
        return new Caminho(r, Fechado);
    }

    /// <summary>Caminho paralelo (amostrado) deslocado lateralmente.</summary>
    public Caminho Paralelo(double desloc, double tolCorda = 0.01)
    {
        var pts = Deslocar(Trecho(0, Comprimento, tolCorda), desloc);
        return DePontos(pts);
    }
}

/// <summary>Operações com polígonos simples.</summary>
public static class Poligonos
{
    /// <summary>Área com sinal (positiva se anti-horário).</summary>
    public static double AreaComSinal(IList<V2> p)
    {
        double a = 0;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) a += p[j].X * p[i].Y - p[i].X * p[j].Y;
        return a / 2;
    }

    public static double Area(IList<V2> p) => Math.Abs(AreaComSinal(p));

    public static double Perimetro(IList<V2> p, bool fechado = true)
    {
        double s = 0;
        for (int i = 1; i < p.Count; i++) s += V2.Dist(p[i - 1], p[i]);
        if (fechado && p.Count > 2) s += V2.Dist(p[^1], p[0]);
        return s;
    }

    public static bool Contem(IList<V2> poly, V2 p)
    {
        bool dentro = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if ((poly[i].Y > p.Y) != (poly[j].Y > p.Y) &&
                p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                dentro = !dentro;
        }
        return dentro;
    }

    /// <summary>
    /// Recorta um polígono qualquer (pode ser côncavo) por um polígono CONVEXO (Sutherland–Hodgman).
    /// Usado no zebrado: cada listra (convexa) recorta a área.
    /// </summary>
    public static List<V2> RecortarPorConvexo(IList<V2> sujeito, IList<V2> janelaConvexa)
    {
        var saida = sujeito.ToList();
        var jan = janelaConvexa.ToList();
        if (AreaComSinal(jan) < 0) jan.Reverse();
        for (int i = 0; i < jan.Count && saida.Count > 0; i++)
        {
            var a = jan[i];
            var b = jan[(i + 1) % jan.Count];
            var entrada = saida;
            saida = new List<V2>();
            for (int k = 0; k < entrada.Count; k++)
            {
                var p = entrada[k];
                var q = entrada[(k + 1) % entrada.Count];
                bool pDentro = V2.Cross(b - a, p - a) >= 0;
                bool qDentro = V2.Cross(b - a, q - a) >= 0;
                if (pDentro) saida.Add(p);
                if (pDentro != qDentro)
                {
                    var d = q - p;
                    double den = V2.Cross(b - a, d);
                    if (Math.Abs(den) > 1e-15)
                    {
                        double t = V2.Cross(b - a, a - p) / den;
                        saida.Add(p + d * t);
                    }
                }
            }
        }
        return saida;
    }

    /// <summary>Remove vértices repetidos consecutivos.</summary>
    public static List<V2> Limpar(IList<V2> p, double tol = 1e-6)
    {
        var r = new List<V2>();
        foreach (var v in p)
            if (r.Count == 0 || V2.Dist(r[^1], v) > tol) r.Add(v);
        if (r.Count > 1 && V2.Dist(r[0], r[^1]) <= tol) r.RemoveAt(r.Count - 1);
        return r;
    }

    public static List<V2> Retangulo(V2 centro, V2 dir, double comprimento, double largura)
    {
        var u = dir.Unitario();
        var n = u.Esquerda;
        double a = comprimento / 2, b = largura / 2;
        return new List<V2> { centro - u * a - n * b, centro + u * a - n * b, centro + u * a + n * b, centro - u * a + n * b };
    }

    public static List<V2> Circulo(V2 c, double r, int n = 48)
    {
        var l = new List<V2>(n);
        for (int i = 0; i < n; i++) l.Add(c + V2.Polar(2 * Math.PI * i / n, r));
        return l;
    }

    public static List<V2> PoligonoRegular(V2 c, double raioCircunscrito, int lados, double rotacao)
    {
        var l = new List<V2>(lados);
        for (int i = 0; i < lados; i++) l.Add(c + V2.Polar(rotacao + 2 * Math.PI * i / lados, raioCircunscrito));
        return l;
    }
}
