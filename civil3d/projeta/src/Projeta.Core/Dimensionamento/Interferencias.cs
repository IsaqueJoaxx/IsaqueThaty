using Projeta.Core.Comum;
using Projeta.Core.Redes;

namespace Projeta.Core.Dimensionamento;

public sealed record Interferencia(
    string RedeA, string TrechoA, string RedeB, string TrechoB,
    P2 Ponto, double TopoInferior, double FundoSuperior, double Folga, string Descricao);

/// <summary>Verifica cruzamentos em planta entre trechos e a folga vertical entre eles.</summary>
public static class VerificadorInterferencias
{
    public static List<Interferencia> Verificar(IEnumerable<Rede> redes, double folgaMinima, bool mesmaRede = false)
    {
        var segs = new List<(Rede rede, Trecho t, ResultadoTrecho r, P2 a, P2 b)>();
        foreach (var rede in redes)
        {
            if (rede.Resultado is null) continue;
            foreach (var t in rede.Trechos)
            {
                var r = rede.Resultado.Trecho(t.Id);
                var na = rede.NoPorId(t.NoMontanteId); var nb = rede.NoPorId(t.NoJusanteId);
                if (r is null || na is null || nb is null) continue;
                segs.Add((rede, t, r, na.XY, nb.XY));
            }
        }
        var lista = new List<Interferencia>();
        for (int i = 0; i < segs.Count; i++)
            for (int j = i + 1; j < segs.Count; j++)
            {
                var s1 = segs[i]; var s2 = segs[j];
                if (s1.rede == s2.rede && !mesmaRede) continue;
                // trechos que compartilham nó não se "cruzam"
                if (s1.rede == s2.rede && (s1.t.NoMontanteId == s2.t.NoMontanteId || s1.t.NoMontanteId == s2.t.NoJusanteId ||
                                           s1.t.NoJusanteId == s2.t.NoMontanteId || s1.t.NoJusanteId == s2.t.NoJusanteId)) continue;
                if (!Geo.IntersecaoSegmentos(s1.a, s1.b, s2.a, s2.b, out var u, out var w)) continue;
                var p = s1.a + (s1.b - s1.a) * u;
                double fundo1 = s1.r.CotaFundoMontante + (s1.r.CotaFundoJusante - s1.r.CotaFundoMontante) * u;
                double fundo2 = s2.r.CotaFundoMontante + (s2.r.CotaFundoJusante - s2.r.CotaFundoMontante) * w;
                double topo1 = fundo1 + s1.r.Altura + s1.r.Espessura;
                double topo2 = fundo2 + s2.r.Altura + s2.r.Espessura;
                double base1 = fundo1 - s1.r.Espessura, base2 = fundo2 - s2.r.Espessura;
                double folga; double topoInf, fundoSup;
                if (base1 >= base2) { folga = base1 - topo2; topoInf = topo2; fundoSup = base1; }
                else { folga = base2 - topo1; topoInf = topo1; fundoSup = base2; }
                if (folga < folgaMinima)
                    lista.Add(new Interferencia(s1.rede.Nome, s1.t.Nome, s2.rede.Nome, s2.t.Nome, p, topoInf, fundoSup, folga,
                        folga < 0 ? $"Colisão ({-folga:0.00} m de sobreposição)" : $"Folga de {folga:0.00} m < {folgaMinima:0.00} m"));
            }
        return lista;
    }
}
