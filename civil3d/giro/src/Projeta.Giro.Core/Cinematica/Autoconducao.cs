using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;

namespace Projeta.Giro.Core.Cinematica;

public sealed class OpcoesConducao
{
    /// <summary>Velocidade de manobra (km/h). Com o tempo batente a batente, define a taxa de giro do volante por metro.</summary>
    public double Velocidade { get; set; } = 10;
    /// <summary>Raio desejado do ponto de referência (eixo dianteiro; na ré, eixo traseiro). Nulo = raio mínimo do veículo.</summary>
    public double? Raio { get; set; }
    /// <summary>Passo de integração do caminho (m).</summary>
    public double Passo { get; set; } = 0.05;
}

/// <summary>Trecho gerado pela autocondução: caminho do ponto de referência e estado final.</summary>
public sealed class TrechoAuto
{
    public List<V2> Pontos { get; init; } = new();
    public bool Re { get; init; }
    /// <summary>Direção do movimento no fim do trecho (rad).</summary>
    public double DirecaoFinal { get; init; }
    /// <summary>Esterçamento máximo usado (rad, com sinal).</summary>
    public double DeltaPico { get; init; }
    /// <summary>Comprimento do giro (rampas + arco), sem a reta final (m).</summary>
    public double ComprimentoGiro { get; init; }
    public string? Erro { get; init; }
    public bool Ok => Erro is null;
}

/// <summary>
/// Autocondução em arco: a partir de uma posição alinhada (volante reto), gira o volante na taxa máxima até o esterçamento de projeto,
/// mantém, desgira e segue em reta até o ponto alvo. O ângulo total girado é achado por bisseção para a reta final passar pelo alvo.
/// </summary>
public static class Autoconducao
{
    /// <summary>Taxa de esterçamento por metro percorrido (rad/m); infinito se o tempo batente a batente for zero.</summary>
    public static double TaxaPorMetro(Veiculo v, double velocidadeKmh)
    {
        if (v.TempoBatente <= 1e-6 || velocidadeKmh <= 1e-6) return double.PositiveInfinity;
        double radPorSeg = 2 * v.AnguloMaximo / v.TempoBatente;
        return radPorSeg / (velocidadeKmh / 3.6);
    }

    /// <summary>Esterçamento de projeto para o raio pedido (ou o máximo do veículo).</summary>
    public static double DeltaProjeto(Veiculo v, bool re, double? raio, out string? aviso)
    {
        aviso = null;
        double max = v.AnguloMaximo, l = v.Motriz.EntreEixos;
        if (raio is not double r || r <= 0) return max;
        double d = re ? Math.Atan(l / r) : (r <= l ? Math.PI / 2 : Math.Asin(l / r));
        if (d > max + 1e-9)
        {
            aviso = $"raio de {Num.F(r)} m menor que o mínimo do veículo; usado o mínimo ({Num.F(RaioMinimo(v, re))} m).";
            return max;
        }
        return d;
    }

    /// <summary>Raio mínimo do ponto de referência (eixo dianteiro; na ré, eixo traseiro).</summary>
    public static double RaioMinimo(Veiculo v, bool re) =>
        re ? v.Motriz.EntreEixos / Math.Tan(v.AnguloMaximo) : v.Motriz.EntreEixos / Math.Sin(v.AnguloMaximo);

    private static double G(bool re, double d, double l) => re ? Math.Tan(d) / l : Math.Sin(d) / l;

    /// <summary>Giro de rumo acumulado nas duas rampas (subida e descida) até o esterçamento dp.</summary>
    private static double GiroRampas(bool re, double dp, double taxa, double l)
    {
        if (double.IsInfinity(taxa)) return 0;
        return re ? -2 * Math.Log(Math.Cos(dp)) / (taxa * l) : 2 * (1 - Math.Cos(dp)) / (taxa * l);
    }

    /// <summary>Perfil de esterçamento para girar o rumo de |a| rad: (pico, comprimento de cada rampa, comprimento mantido).</summary>
    public static (double Pico, double Rampa, double Mantido) Perfil(bool re, double a, double dProj, double taxa, double l)
    {
        a = Math.Abs(a);
        if (a < 1e-12) return (0, 0, 0);
        double gr = GiroRampas(re, dProj, taxa, l);
        if (a >= gr)
        {
            double rampa = double.IsInfinity(taxa) ? 0 : dProj / taxa;
            return (dProj, rampa, (a - gr) / G(re, dProj, l));
        }
        double x = a * taxa * l / 2;
        double dp = re ? Math.Acos(Math.Exp(-x)) : Math.Acos(Math.Clamp(1 - x, -1, 1));
        return (dp, dp / taxa, 0);
    }

    /// <summary>
    /// Integra o caminho do ponto de referência para um giro de rumo <paramref name="giro"/> (rad, com sinal: + = esquerda do movimento),
    /// começando e terminando com o volante reto. Retorna os pontos (a partir de p0) e o estado final.
    /// </summary>
    public static (List<V2> Pontos, V2 Fim, double DirFim, double DeltaPico, double Comprimento) Curva(
        Veiculo v, bool re, V2 p0, double dir0, double giro, double dProj, double taxa, double passo, bool guardar = true)
    {
        double l = v.Motriz.EntreEixos;
        var (pico, rampa, mantido) = Perfil(re, giro, dProj, taxa, l);
        double sinal = Math.Sign(giro);
        // na ré, o volante à esquerda gira a direção do movimento para a direita
        double sd = re ? -sinal : sinal;
        double total = 2 * rampa + mantido;
        var pts = new List<V2>();
        if (guardar) pts.Add(p0);
        if (total < 1e-9) return (pts, p0, dir0, 0, 0);
        int n = Math.Max(2, (int)Math.Ceiling(total / passo));
        double ds = total / n;
        // estado: posição P do ponto de referência, rumo θ do veículo
        double theta = re ? dir0 + Math.PI : dir0;
        var p = p0;
        double Delta(double s)
        {
            if (rampa < 1e-12) return pico;
            double d = Math.Min(pico, Math.Min(s / rampa * pico, (total - s) / rampa * pico));
            return Math.Max(0, d);
        }
        for (int k = 0; k < n; k++)
        {
            double sm = (k + 0.5) * ds;
            double d = sd * Delta(sm);
            if (re)
            {
                double dth = -ds * Math.Tan(d) / l;
                p += V2.Polar(theta + dth / 2 + Math.PI, ds);
                theta += dth;
            }
            else
            {
                double dth = ds * Math.Sin(d) / l;
                p += V2.Polar(theta + dth / 2 + d, ds);
                theta += dth;
            }
            if (guardar) pts.Add(p);
        }
        double dirFim = re ? theta - Math.PI : theta;
        return (pts, p, dirFim, sd * pico, total);
    }

    /// <summary>Trecho de autocondução do ponto p0 (direção do movimento dir0, volante reto) até o alvo.</summary>
    public static TrechoAuto Ate(Veiculo v, V2 p0, double dir0, V2 alvo, bool re, OpcoesConducao op)
    {
        if (re && v.Articulado) return new TrechoAuto { Erro = "marcha à ré só para veículos rígidos (sem reboque) nesta versão.", Re = true };
        double dist = p0.Dist(alvo);
        if (dist < 0.05) return new TrechoAuto { Erro = "ponto alvo coincide com a posição atual.", Re = re };
        double dProj = DeltaProjeto(v, re, op.Raio, out _);
        double taxa = TaxaPorMetro(v, op.Velocidade);
        var u0 = V2.Polar(dir0);
        var w = alvo - p0;
        double lado = u0.Cross(w);
        if (Math.Abs(lado) < 1e-4 * Math.Max(1, dist) && u0.Dot(w) > 0)
            return new TrechoAuto { Pontos = new() { p0, alvo }, Re = re, DirecaoFinal = dir0 };

        double sinal = lado >= 0 ? 1 : -1;
        double F(double a, out double frente)
        {
            var c = Curva(v, re, p0, dir0, sinal * a, dProj, taxa, op.Passo, guardar: false);
            var u = V2.Polar(c.DirFim);
            var t = alvo - c.Fim;
            frente = u.Dot(t);
            return sinal * u.Cross(t);
        }

        // varredura grossa do ângulo girado até a reta final cruzar o alvo à frente
        double aAnt = 0, fAnt = F(0, out _);
        double a0 = double.NaN, a1 = double.NaN;
        const double passoA = 1.0 * Math.PI / 180;
        for (double a = passoA; a < 2 * Math.PI - 0.02; a += passoA)
        {
            double f = F(a, out double frente);
            if (fAnt > 0 && f <= 0 && frente > 0) { a0 = aAnt; a1 = a; break; }
            aAnt = a; fAnt = f;
        }
        if (double.IsNaN(a0))
            return new TrechoAuto
            {
                Re = re,
                Erro = $"o alvo fica dentro do círculo de giro mínimo (raio ≈ {Num.F(RaioMinimo(v, re), 1)} m). Afaste o ponto ou siga em reta antes de girar.",
            };
        for (int it = 0; it < 60; it++)
        {
            double m = (a0 + a1) / 2;
            if (F(m, out _) > 0) a0 = m; else a1 = m;
        }
        double ang = (a0 + a1) / 2;
        var curva = Curva(v, re, p0, dir0, sinal * ang, dProj, taxa, op.Passo);
        var pts = curva.Pontos;
        if (curva.Fim.Dist(alvo) > 0.01) pts.Add(alvo);
        return new TrechoAuto
        {
            Pontos = pts,
            Re = re,
            DirecaoFinal = curva.DirFim,
            DeltaPico = curva.DeltaPico,
            ComprimentoGiro = curva.Comprimento,
        };
    }

    /// <summary>Gabarito de giro: reta de entrada, giro de <paramref name="anguloGraus"/> no raio mínimo (com transições) e reta de saída.</summary>
    public static List<V2> Gabarito(Veiculo v, V2 p0, double dir0, double anguloGraus, bool esquerda, OpcoesConducao op, double? retas = null)
    {
        double lr = retas ?? Math.Max(10, v.Comprimento * 1.2);
        double dProj = DeltaProjeto(v, false, op.Raio, out _);
        double taxa = TaxaPorMetro(v, op.Velocidade);
        var pts = new List<V2> { p0 };
        var a = p0 + V2.Polar(dir0, lr);
        pts.Add(a);
        var c = Curva(v, false, a, dir0, (esquerda ? 1 : -1) * Num.Rad(anguloGraus), dProj, taxa, op.Passo);
        pts.AddRange(c.Pontos.Skip(1));
        pts.Add(c.Fim + V2.Polar(c.DirFim, lr));
        return pts;
    }
}
