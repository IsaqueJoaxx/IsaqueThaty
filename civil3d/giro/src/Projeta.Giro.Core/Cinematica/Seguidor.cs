using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;

namespace Projeta.Giro.Core.Cinematica;

/// <summary>
/// Cinemática de baixa velocidade (sem deriva dos pneus): cada eixo efetivo segue o seu ponto guia como uma tractriz.
/// Para o ponto guia andando em linha reta, a solução é exata: tan(φ₁/2) = tan(φ₀/2)·e^(−s/L).
/// </summary>
public static class Seguidor
{
    /// <summary>Novo rumo de uma unidade cujo ponto guia anda em linha reta de g0 a g1.</summary>
    public static double Tractriz(double rumo0, V2 g0, V2 g1, double entreEixos)
    {
        var d = g1 - g0;
        double s = d.Comprimento;
        if (s < 1e-12) return rumo0;
        double psi = d.Angulo;
        double phi0 = Geo.AngNorm(rumo0 - psi);
        double phi1 = 2 * Math.Atan(Math.Tan(phi0 / 2) * Math.Exp(-s / entreEixos));
        return psi + phi1;
    }

    /// <summary>Veículo todo alinhado (sem articulação), com o eixo dianteiro em <paramref name="eixoDianteiro"/>.</summary>
    public static PoseUnidade[] Alinhado(Veiculo v, V2 eixoDianteiro, double rumo)
    {
        var r = new PoseUnidade[v.Unidades.Count];
        var g = eixoDianteiro;
        for (int i = 0; i < r.Length; i++)
        {
            r[i] = new PoseUnidade(g, rumo);
            g = r[i].Ponto(v.Unidades[i].XEngate);
        }
        return r;
    }

    /// <summary>Move o eixo dianteiro da motriz para <paramref name="novo"/> (passo curto, em linha reta) e arrasta as demais unidades.</summary>
    public static PoseUnidade[] Avancar(Veiculo v, PoseUnidade[] atual, V2 novo)
    {
        int n = atual.Length;
        var r = new PoseUnidade[n];
        V2 g0 = atual[0].Guia, g1 = novo;
        for (int i = 0; i < n; i++)
        {
            var u = v.Unidades[i];
            if (i > 0)
            {
                g0 = Corpo.Engate(v.Unidades[i - 1], atual[i - 1]);
                g1 = Corpo.Engate(v.Unidades[i - 1], r[i - 1]);
            }
            r[i] = new PoseUnidade(g1, Tractriz(atual[i].Rumo, g0, g1, u.EntreEixos));
        }
        return r;
    }

    /// <summary>
    /// Marcha à ré de veículo rígido guiada pelo eixo traseiro: o eixo traseiro anda sempre na direção do eixo do veículo,
    /// então o rumo é a direção do movimento + 180°.
    /// </summary>
    public static PoseUnidade[] RePeloEixoTraseiro(Veiculo v, V2 traseiro, double direcaoMovimento)
    {
        double rumo = direcaoMovimento + Math.PI;
        var frente = traseiro + V2.Polar(rumo, v.Motriz.EntreEixos);
        return new[] { new PoseUnidade(frente, rumo) };
    }
}
