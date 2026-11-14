using Clipper2Lib;

namespace Projeta.Giro.Core.Geometria;

/// <summary>Operações booleanas e offset de polígonos (Clipper2, precisão de 1 mm).</summary>
public static class Poligonos
{
    private const int Precisao = 3;

    private static PathD Para(IReadOnlyList<V2> p)
    {
        var r = new PathD(p.Count);
        foreach (var v in p) r.Add(new PointD(v.X, v.Y));
        return r;
    }

    private static PathsD Para(IEnumerable<IReadOnlyList<V2>> ps)
    {
        var r = new PathsD();
        foreach (var p in ps) if (p.Count >= 2) r.Add(Para(p));
        return r;
    }

    private static List<List<V2>> De(PathsD ps) =>
        ps.Select(p => p.Select(q => new V2(q.x, q.y)).ToList()).Where(p => p.Count >= 2).ToList();

    /// <summary>União de polígonos (regra não-nula). Externos anti-horários, furos horários.</summary>
    public static List<List<V2>> Uniao(IEnumerable<IReadOnlyList<V2>> polis)
    {
        var c = new ClipperD(Precisao);
        c.AddSubject(Para(polis));
        var sol = new PathsD();
        c.Execute(ClipType.Union, FillRule.NonZero, sol);
        return De(sol);
    }

    /// <summary>
    /// União de muitos polígonos que se sobrepõem em sequência (posições sucessivas de um corpo):
    /// une em lotes de vizinhos e depois junta os resultados dois a dois — bem mais rápido que uma união única.
    /// </summary>
    public static List<List<V2>> UniaoEmLotes(IReadOnlyList<IReadOnlyList<V2>> polis, int lote = 48)
    {
        if (polis.Count <= lote) return Uniao(polis);
        var nivel = new List<PathsD>();
        for (int i = 0; i < polis.Count; i += lote)
        {
            var c = new ClipperD(Precisao);
            var sub = new PathsD();
            for (int k = i; k < Math.Min(polis.Count, i + lote); k++) if (polis[k].Count >= 3) sub.Add(Para(polis[k]));
            c.AddSubject(sub);
            var sol = new PathsD();
            c.Execute(ClipType.Union, FillRule.NonZero, sol);
            nivel.Add(sol);
        }
        while (nivel.Count > 1)
        {
            var prox = new List<PathsD>((nivel.Count + 1) / 2);
            for (int i = 0; i < nivel.Count; i += 2)
            {
                if (i + 1 >= nivel.Count) { prox.Add(nivel[i]); continue; }
                var c = new ClipperD(Precisao);
                c.AddSubject(nivel[i]);
                c.AddSubject(nivel[i + 1]);
                var sol = new PathsD();
                c.Execute(ClipType.Union, FillRule.NonZero, sol);
                prox.Add(sol);
            }
            nivel = prox;
        }
        return De(nivel[0]);
    }

    public static List<List<V2>> Intersecao(IEnumerable<IReadOnlyList<V2>> a, IEnumerable<IReadOnlyList<V2>> b)
    {
        var c = new ClipperD(Precisao);
        c.AddSubject(Para(a));
        c.AddClip(Para(b));
        var sol = new PathsD();
        c.Execute(ClipType.Intersection, FillRule.NonZero, sol);
        return De(sol);
    }

    public static List<List<V2>> Diferenca(IEnumerable<IReadOnlyList<V2>> a, IEnumerable<IReadOnlyList<V2>> b)
    {
        var c = new ClipperD(Precisao);
        c.AddSubject(Para(a));
        c.AddClip(Para(b));
        var sol = new PathsD();
        c.Execute(ClipType.Difference, FillRule.NonZero, sol);
        return De(sol);
    }

    /// <summary>Trechos de linhas abertas que ficam dentro dos polígonos.</summary>
    public static List<List<V2>> LinhasDentro(IEnumerable<IReadOnlyList<V2>> linhas, IEnumerable<IReadOnlyList<V2>> polis)
    {
        var c = new ClipperD(Precisao);
        c.AddOpenSubject(Para(linhas));
        c.AddClip(Para(polis));
        var fechadas = new PathsD();
        var abertas = new PathsD();
        c.Execute(ClipType.Intersection, FillRule.NonZero, fechadas, abertas);
        return De(abertas);
    }

    /// <summary>Expande (delta &gt; 0) ou contrai polígonos, com cantos arredondados.</summary>
    public static List<List<V2>> Inflar(IEnumerable<IReadOnlyList<V2>> polis, double delta)
    {
        var r = Clipper.InflatePaths(Para(polis), delta, JoinType.Round, EndType.Polygon, 2.0, Precisao, 0.005);
        return De(r);
    }

    /// <summary>Faixa de largura 2·meia em torno de linhas abertas (para tratar linhas como obstáculos com espessura).</summary>
    public static List<List<V2>> FaixaLinhas(IEnumerable<IReadOnlyList<V2>> linhas, double meia)
    {
        var r = Clipper.InflatePaths(Para(linhas), meia, JoinType.Round, EndType.Round, 2.0, Precisao, 0.005);
        return De(r);
    }

    public static double AreaTotal(IEnumerable<IReadOnlyList<V2>> polis) => polis.Sum(p => Geo.Area(p));

    public static List<V2> SimplificarFechado(IReadOnlyList<V2> p, double tol) =>
        Clipper.SimplifyPath(Para(p), tol, true).Select(q => new V2(q.x, q.y)).ToList();
}
