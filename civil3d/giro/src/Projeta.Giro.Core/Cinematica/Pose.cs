using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;

namespace Projeta.Giro.Core.Cinematica;

/// <summary>Posição de uma unidade: ponto guia (eixo dianteiro da motriz ou engate do reboque) e rumo (rad) do eixo longitudinal.</summary>
public readonly record struct PoseUnidade(V2 Guia, double Rumo)
{
    public V2 Dir => V2.Polar(Rumo);
    public V2 Ponto(double x, double y = 0) => V2.Local(Guia, Rumo, x, y);
}

/// <summary>Posição do veículo inteiro num instante.</summary>
public sealed class Pose
{
    /// <summary>Distância percorrida pelo ponto de referência desde o início da manobra (m).</summary>
    public double S { get; init; }
    /// <summary>Esterçamento equivalente do eixo dianteiro (rad, positivo = para a esquerda).</summary>
    public double Delta { get; init; }
    /// <summary>Em marcha à ré.</summary>
    public bool Re { get; init; }
    public PoseUnidade[] U { get; init; } = Array.Empty<PoseUnidade>();

    public V2 EixoDianteiro => U[0].Guia;
    public double Rumo => U[0].Rumo;

    /// <summary>Ângulo de articulação entre a unidade i-1 e a unidade i (rad).</summary>
    public double Articulacao(int i) => Geo.AngNorm(U[i - 1].Rumo - U[i].Rumo);
}

/// <summary>Contornos do veículo numa pose (carrocerias, rodas, barras).</summary>
public static class Corpo
{
    public static V2 Engate(Unidade u, PoseUnidade p) => p.Ponto(u.XEngate);
    public static V2 EixoEfetivo(Unidade u, PoseUnidade p) => p.Ponto(-u.EntreEixos);

    /// <summary>Polígonos da carroceria (um retângulo; a barra do dolly vira barra fina + quadro do eixo).</summary>
    public static List<V2[]> Carroceria(Unidade u, PoseUnidade p)
    {
        var r = new List<V2[]>();
        if (u.Barra)
        {
            r.Add(Geo.Retangulo(p.Guia, p.Rumo, 0, -u.EntreEixos, 0.15));
            double xa = -u.EntreEixos;
            r.Add(Geo.Retangulo(p.Guia, p.Rumo, xa + 0.45, xa - Math.Max(0.45, u.BalancoTraseiro), Math.Min(u.Largura, u.BitolaMax)));
        }
        else r.Add(Geo.Retangulo(p.Guia, p.Rumo, u.XFrente, u.XTras, u.Largura));
        return r;
    }

    /// <summary>Ângulo de uma roda direcional na posição lateral y (Ackermann), dado o esterçamento equivalente.</summary>
    public static double AnguloRoda(double entreEixos, double delta, double y)
    {
        double t = Math.Tan(delta);
        if (Math.Abs(t) < 1e-9) return 0;
        return Math.Atan(entreEixos * t / (entreEixos - y * t));
    }

    /// <summary>Retângulos das rodas da unidade i (as do eixo dianteiro da motriz esterçadas).</summary>
    public static List<V2[]> Rodas(Veiculo v, int i, PoseUnidade p, double delta)
    {
        var u = v.Unidades[i];
        var r = new List<V2[]>();
        foreach (var e in u.Eixos)
        {
            bool direcional = i == 0 && Math.Abs(e.X) < 0.05;
            for (int lado = -1; lado <= 1; lado += 2)
            {
                double y = lado * (e.Bitola / 2 - e.LarguraRodado / 2);
                double ang = direcional ? AnguloRoda(u.EntreEixos, delta, y) : 0;
                var c = p.Ponto(e.X, y);
                r.Add(RetanguloCentrado(c, p.Rumo + ang, e.DiametroRoda, e.LarguraRodado));
            }
        }
        return r;
    }

    public static V2[] RetanguloCentrado(V2 c, double ang, double comp, double larg)
    {
        double a = comp / 2, b = larg / 2;
        return new[] { V2.Local(c, ang, -a, -b), V2.Local(c, ang, a, -b), V2.Local(c, ang, a, b), V2.Local(c, ang, -a, b) };
    }

    /// <summary>Todos os contornos fechados do veículo (carrocerias e rodas) e as linhas abertas (eixos, engates).</summary>
    public static (List<V2[]> Fechados, List<V2[]> Linhas) Desenho(Veiculo v, Pose pose)
    {
        var f = new List<V2[]>();
        var l = new List<V2[]>();
        for (int i = 0; i < v.Unidades.Count; i++)
        {
            var u = v.Unidades[i];
            var p = pose.U[i];
            f.AddRange(Carroceria(u, p));
            f.AddRange(Rodas(v, i, p, pose.Delta));
            foreach (var e in u.Eixos)
                l.Add(new[] { p.Ponto(e.X, -e.Bitola / 2 + e.LarguraRodado), p.Ponto(e.X, e.Bitola / 2 - e.LarguraRodado) });
            if (i + 1 < v.Unidades.Count)
            {
                // marca do engate (pequena cruz) e ligação ao eixo efetivo quando o engate fica fora da carroceria
                var h = Engate(u, p);
                l.Add(new[] { h + V2.Polar(p.Rumo, 0.25), h - V2.Polar(p.Rumo, 0.25) });
                l.Add(new[] { h + V2.Polar(p.Rumo + Math.PI / 2, 0.25), h - V2.Polar(p.Rumo + Math.PI / 2, 0.25) });
            }
            if (i == 0)
            {
                // seta de sentido na frente da motriz
                var a = p.Ponto(u.XFrente - 0.2);
                l.Add(new[] { p.Ponto(u.XFrente - 0.9, -0.35), a, p.Ponto(u.XFrente - 0.9, 0.35) });
            }
        }
        return (f, l);
    }

    /// <summary>Polígonos que compõem a envoltória (carrocerias e rodas) numa pose.</summary>
    public static List<V2[]> PoligonosEnvoltoria(Veiculo v, Pose pose)
    {
        var r = new List<V2[]>();
        for (int i = 0; i < v.Unidades.Count; i++)
        {
            r.AddRange(Carroceria(v.Unidades[i], pose.U[i]));
            r.AddRange(Rodas(v, i, pose.U[i], pose.Delta));
        }
        return r;
    }
}
