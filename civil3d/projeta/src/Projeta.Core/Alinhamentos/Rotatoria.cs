namespace Projeta.Core.Alinhamentos;

/// <summary>Geometria plana de uma rotatória (anel anti-horário, mão direita): braços, emenda do anel, conexões.</summary>
public static class GeometriaRotatoria
{
    /// <summary>Ângulo em [0, 2π).</summary>
    public static double Normalizar(double a)
    {
        double r = a % (2 * Math.PI);
        return r < 0 ? r + 2 * Math.PI : r;
    }

    /// <summary>Rumo (L, NE, N, NO, O, SO, S, SE) de um ângulo medido do leste no sentido anti-horário.</summary>
    public static string Rumo(double angulo)
    {
        string[] r = { "L", "NE", "N", "NO", "O", "SO", "S", "SE" };
        int k = (int)Math.Round(Normalizar(angulo) / (Math.PI / 4)) % 8;
        return r[k];
    }

    /// <summary>Onde começar o eixo do anel: no meio do maior vão entre os braços (a emenda do eixo fechado fica longe de tudo).</summary>
    public static double AnguloEmenda(IReadOnlyList<double> angulosBracos)
    {
        if (angulosBracos.Count == 0) return 0;
        var a = angulosBracos.Select(Normalizar).OrderBy(x => x).ToList();
        if (a.Count == 1) return Normalizar(a[0] + Math.PI);
        double melhor = -1, emenda = 0;
        for (int i = 0; i < a.Count; i++)
        {
            double ini = a[i], fim = i + 1 < a.Count ? a[i + 1] : a[0] + 2 * Math.PI;
            if (fim - ini > melhor) { melhor = fim - ini; emenda = Normalizar((ini + fim) / 2); }
        }
        return emenda;
    }

    /// <summary>
    /// Primeiro palpite para o centro do arco de conexão de um braço: o braço tratado como reta pelo ponto J (no eixo do anel)
    /// com direção <paramref name="ux"/>,<paramref name="uy"/> para fora; o centro fica a <paramref name="d"/> do braço (bordo + raio)
    /// do lado pedido e a <paramref name="raioCentro"/> (bordo externo do anel + raio) do centro da rotatória.
    /// </summary>
    /// <param name="ladoAntiHorario">+1 = canto do lado anti-horário do braço (entrada, na mão direita); −1 = horário (saída).</param>
    public static (double X, double Y) ChuteCentroConexao(double jx, double jy, double ux, double uy, int ladoAntiHorario,
                                                          double d, double ox, double oy, double raioCentro)
    {
        double n = Math.Sqrt(ux * ux + uy * uy);
        ux /= n; uy /= n;
        double vx = ladoAntiHorario > 0 ? -uy : uy, vy = ladoAntiHorario > 0 ? ux : -ux;
        double wx = jx - ox + d * vx, wy = jy - oy + d * vy;
        double b = wx * ux + wy * uy;
        double disc = b * b - (wx * wx + wy * wy) + raioCentro * raioCentro;
        double t = disc >= 0 ? -b + Math.Sqrt(disc) : d;
        if (t < 0) t = d;
        return (jx + t * ux + d * vx, jy + t * uy + d * vy);
    }

    /// <summary>
    /// Folga (rad) no bordo externo entre a conexão de entrada de um braço e a de saída do próximo (anti-horário);
    /// negativa = as duas se sobrepõem.
    /// </summary>
    public static double Folga(double anguloBraco, double anguloTangenteEntrada, double anguloProximoBraco, double anguloTangenteSaidaProximo)
    {
        double vao = Normalizar(anguloProximoBraco - anguloBraco);
        if (vao == 0) vao = 2 * Math.PI;
        double e = Normalizar(anguloTangenteEntrada - anguloBraco);
        double x = Normalizar(anguloProximoBraco - anguloTangenteSaidaProximo);
        if (e > Math.PI) e -= 2 * Math.PI;
        if (x > Math.PI) x -= 2 * Math.PI;
        return vao - e - x;
    }
}

/// <summary>Greide do eixo do anel: fechado, passando exatamente nas cotas dos braços.</summary>
public static class PerfilAnel
{
    public sealed record Alvo(double Estaca, double Cota);

    /// <summary>
    /// PVIs do anel (estacas de 0 a <paramref name="comprimento"/>, a partir do início do eixo): um PVI em cada braço, com
    /// parábola simétrica de <paramref name="fracaoCurva"/> do menor vão vizinho, e a cota do PVI corrigida para a CURVA
    /// passar na cota do braço (z_PVI = z_alvo − (i2 − i1)·L/8, iterado). Na emenda (estacas 0 e fim) o greide segue a
    /// tangente entre o último e o primeiro braço, então a rampa não quebra ali. Um braço só: anel plano.
    /// </summary>
    public static List<PerfilConexao.Pvi> Calcular(double comprimento, IReadOnlyList<Alvo> alvos, double fracaoCurva = 0.8)
    {
        if (!(comprimento > 0)) throw new ArgumentException("Comprimento do anel inválido.");
        if (alvos.Count == 0) throw new ArgumentException("Sem cotas dos braços.");
        double c = comprimento;
        var a = alvos.Select(x => new Alvo(Math.Clamp(x.Estaca, 1e-6, c - 1e-6), x.Cota)).OrderBy(x => x.Estaca).ToList();
        int n = a.Count;
        if (n == 1) return new List<PerfilConexao.Pvi> { new(0, a[0].Cota, 0), new(c, a[0].Cota, 0) };

        double Vao(int i) => i < n - 1 ? a[i + 1].Estaca - a[i].Estaca : a[0].Estaca + c - a[n - 1].Estaca; // de i até o próximo
        int Ant(int i) => (i + n - 1) % n;
        var l = new double[n];
        for (int i = 0; i < n; i++)
        {
            double m = Math.Min(Vao(Ant(i)), Vao(i));
            l[i] = fracaoCurva * m;
        }
        // as curvas da primeira e da última não podem chegar na emenda
        l[0] = Math.Min(l[0], 2 * fracaoCurva * a[0].Estaca);
        l[n - 1] = Math.Min(l[n - 1], 2 * fracaoCurva * (c - a[n - 1].Estaca));

        var p = a.Select(x => x.Cota).ToArray();
        for (int it = 0; it < 200; it++)
        {
            double maior = 0;
            var novo = new double[n];
            for (int i = 0; i < n; i++)
            {
                int ant = Ant(i), prox = (i + 1) % n;
                double gin = (p[i] - p[ant]) / Vao(ant), gout = (p[prox] - p[i]) / Vao(i);
                novo[i] = a[i].Cota - (gout - gin) * l[i] / 8;
                maior = Math.Max(maior, Math.Abs(novo[i] - p[i]));
            }
            p = novo;
            if (maior < 1e-9) break;
        }
        double zEmenda = p[n - 1] + (p[0] - p[n - 1]) * (c - a[n - 1].Estaca) / Vao(n - 1);
        var r = new List<PerfilConexao.Pvi> { new(0, zEmenda, 0) };
        for (int i = 0; i < n; i++) r.Add(new PerfilConexao.Pvi(a[i].Estaca, p[i], l[i]));
        r.Add(new PerfilConexao.Pvi(c, zEmenda, 0));
        return r;
    }

    /// <summary>Maior rampa (em módulo) das tangentes do greide e o trecho onde ocorre.</summary>
    public static (double Rampa, double De, double Ate) RampaMaxima(IReadOnlyList<PerfilConexao.Pvi> p)
    {
        (double, double, double) m = (0, 0, 0);
        for (int i = 0; i + 1 < p.Count; i++)
        {
            double ds = p[i + 1].Estaca - p[i].Estaca;
            if (ds <= 0) continue;
            double g = (p[i + 1].Cota - p[i].Cota) / ds;
            if (Math.Abs(g) > Math.Abs(m.Item1)) m = (g, p[i].Estaca, p[i + 1].Estaca);
        }
        return m;
    }
}
