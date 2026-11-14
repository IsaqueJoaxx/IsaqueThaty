using System.ComponentModel;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Redes;

namespace Projeta.Core.Escavacao;

public sealed class ParametrosVala
{
    [DisplayName("Folga lateral de cada lado (m)")] public double FolgaLateral { get; set; } = 0.30;
    [DisplayName("Largura mínima da vala (m)")] public double LarguraMinima { get; set; } = 0.80;
    [DisplayName("Espessura do berço/lastro (m)")] public double Berco { get; set; } = 0.10;
    [DisplayName("Talude da vala (H:V) — 0 = vertical")] public double Talude { get; set; } = 0.0;
    [DisplayName("Profundidade a partir da qual há escoramento (m)")] public double ProfundidadeEscoramento { get; set; } = 1.25;
    [DisplayName("Empolamento (fator)")] public double Empolamento { get; set; } = 1.25;
    [DisplayName("Descontar volume do PV/caixa")] public bool DescontarNos { get; set; } = false;
}

public sealed class QuantitativoVala
{
    public string Trecho { get; set; } = "";
    public string Secao { get; set; } = "";
    public double Comprimento { get; set; }
    public double LarguraFundo { get; set; }
    public double ProfundidadeMedia { get; set; }
    public double Escavacao { get; set; }
    public double Berco { get; set; }
    public double VolumeTubo { get; set; }
    public double Reaterro { get; set; }
    public double BotaFora { get; set; }
    public double Escoramento { get; set; }
    /// <summary>Volume por faixas de profundidade (0–1.5, 1.5–3.0, 3.0–4.5, &gt;4.5 m).</summary>
    public double[] PorFaixa { get; set; } = new double[4];
}

public static class CalculoVala
{
    public static readonly string[] Faixas = { "0 a 1,50 m", "1,50 a 3,00 m", "3,00 a 4,50 m", "acima de 4,50 m" };

    /// <summary>
    /// Volumes de vala por trecho, integrando a profundidade linearmente entre montante e jusante
    /// (o terreno é considerado linear entre as tampas dos PVs).
    /// </summary>
    public static List<QuantitativoVala> Calcular(ResultadoRede res, ParametrosVala p)
    {
        var lista = new List<QuantitativoVala>();
        foreach (var t in res.Trechos)
        {
            if (t.Comprimento <= 0 || double.IsNaN(t.CotaFundoMontante)) continue;
            double larguraExt = (t.Forma == FormaSecao.Circular ? t.Diametro : t.Diametro) * t.Celulas + 2 * t.Espessura + (t.Celulas - 1) * 0.15;
            double b = Math.Max(p.LarguraMinima, larguraExt + 2 * p.FolgaLateral);
            double h1 = t.CotaTerrenoMontante - t.CotaFundoMontante + p.Berco + t.Espessura;
            double h2 = t.CotaTerrenoJusante - t.CotaFundoJusante + p.Berco + t.Espessura;
            h1 = Math.Max(h1, 0); h2 = Math.Max(h2, 0);
            var q = new QuantitativoVala { Trecho = t.Nome, Secao = t.Secao, Comprimento = t.Comprimento, LarguraFundo = b, ProfundidadeMedia = (h1 + h2) / 2 };

            // integração em 20 fatias
            const int nf = 20;
            double dx = t.Comprimento / nf, vol = 0, esc = 0;
            for (int i = 0; i < nf; i++)
            {
                double hm = h1 + (h2 - h1) * (i + 0.5) / nf;
                double area = (b + p.Talude * hm) * hm;
                double dv = area * dx;
                vol += dv;
                if (hm > p.ProfundidadeEscoramento && p.Talude <= 0) esc += 2 * hm * dx;
                // faixas de profundidade (fatia horizontal)
                DistribuirPorFaixa(q.PorFaixa, b, p.Talude, hm, dx);
            }
            q.Escavacao = vol;
            q.Escoramento = esc;
            q.Berco = b * p.Berco * t.Comprimento;
            double aExt = t.Forma == FormaSecao.Circular
                ? t.Celulas * Math.PI * Math.Pow(t.Diametro / 2 + t.Espessura, 2)
                : t.Celulas * (t.Diametro + 2 * t.Espessura) * (t.Altura + 2 * t.Espessura);
            q.VolumeTubo = aExt * t.Comprimento;
            q.Reaterro = Math.Max(0, q.Escavacao - q.Berco - q.VolumeTubo);
            q.BotaFora = (q.Escavacao - q.Reaterro) * p.Empolamento;
            lista.Add(q);
        }
        return lista;
    }

    private static void DistribuirPorFaixa(double[] faixas, double b, double talude, double h, double dx)
    {
        double[] lim = { 0, 1.5, 3.0, 4.5, double.MaxValue };
        for (int k = 0; k < 4; k++)
        {
            double z1 = lim[k], z2 = Math.Min(lim[k + 1], h);
            if (z2 <= z1) break;
            // largura a uma profundidade z medida a partir do terreno: no topo = b + 2·talude·h; no fundo = b
            double Larg(double z) => b + 2 * talude * (h - z);
            double area = (Larg(z1) + Larg(z2)) / 2 * (z2 - z1);
            faixas[k] += area * dx;
        }
    }
}
