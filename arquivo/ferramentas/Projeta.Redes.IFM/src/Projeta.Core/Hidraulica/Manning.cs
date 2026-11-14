namespace Projeta.Core.Hidraulica;

/// <summary>Escoamento uniforme pela fórmula de Manning (unidades SI).</summary>
public static class Manning
{
    public const double Gama = 9810.0; // peso específico da água (N/m³)
    public const double G = 9.81;

    public static double Vazao(ISecao s, double y, double n, double declividade, int celulas = 1)
    {
        if (y <= 0 || declividade <= 0) return 0;
        double a = s.Area(y), p = s.PerimetroMolhado(y);
        if (a <= 0 || p <= 0) return 0;
        double r = a / p;
        return celulas * (1.0 / n) * a * Math.Pow(r, 2.0 / 3.0) * Math.Sqrt(declividade);
    }

    public static double RaioHidraulico(ISecao s, double y)
    {
        double p = s.PerimetroMolhado(y);
        return p <= 0 ? 0 : s.Area(y) / p;
    }

    /// <summary>Vazão máxima possível na seção (no ponto de máximo da curva de Manning).</summary>
    public static double VazaoMaxima(ISecao s, double n, double declividade, int celulas = 1) => Vazao(s, s.AlturaVazaoMaxima, n, declividade, celulas);

    /// <summary>Vazão a seção plena.</summary>
    public static double VazaoPlena(ISecao s, double n, double declividade, int celulas = 1)
    {
        if (s is SecaoCircular c)
        {
            double a = Math.PI * c.D * c.D / 4, r = c.D / 4;
            return celulas * (1.0 / n) * a * Math.Pow(r, 2.0 / 3.0) * Math.Sqrt(declividade);
        }
        return Vazao(s, s.Altura, n, declividade, celulas);
    }

    /// <summary>
    /// Altura normal (m) para a vazão Q. Retorna NaN se Q excede a capacidade máxima da seção.
    /// </summary>
    public static double AlturaNormal(ISecao s, double q, double n, double declividade, int celulas = 1)
    {
        if (q <= 0) return 0;
        if (declividade <= 0) return double.NaN;
        double ymax = s.AlturaVazaoMaxima;
        if (Vazao(s, ymax, n, declividade, celulas) < q) return double.NaN;
        double lo = 0, hi = ymax;
        for (int i = 0; i < 80; i++)
        {
            double mid = (lo + hi) / 2;
            if (Vazao(s, mid, n, declividade, celulas) < q) lo = mid; else hi = mid;
            if (hi - lo < 1e-7) break;
        }
        return (lo + hi) / 2;
    }

    public static double Velocidade(ISecao s, double q, double y, int celulas = 1)
    {
        double a = s.Area(y) * celulas;
        return a <= 0 ? 0 : q / a;
    }

    /// <summary>Tensão trativa média (Pa): σ = γ·Rh·I.</summary>
    public static double TensaoTrativa(ISecao s, double y, double declividade) => Gama * RaioHidraulico(s, y) * declividade;

    /// <summary>Velocidade crítica (NBR 9649): vc = 6·√(g·Rh).</summary>
    public static double VelocidadeCritica(ISecao s, double y) => 6.0 * Math.Sqrt(G * RaioHidraulico(s, y));

    public static double Froude(ISecao s, double q, double y, int celulas = 1)
    {
        double t = s.LarguraSuperficial(y);
        if (t <= 0) return 0;
        double a = s.Area(y);
        double v = Velocidade(s, q, y, celulas);
        return v / Math.Sqrt(G * a / t);
    }

    /// <summary>
    /// Declividade (m/m) em que a velocidade, para a vazão Q, atinge <paramref name="velocidadeAlvo"/>.
    /// A velocidade cresce com a declividade, então usa bisseção em escala logarítmica.
    /// Retorna NaN se não for possível no intervalo [1e-6, 2].
    /// </summary>
    public static double DeclividadeParaVelocidade(ISecao s, double q, double n, double velocidadeAlvo, int celulas = 1)
    {
        if (q <= 0) return double.NaN;
        double V(double sx)
        {
            double y = AlturaNormal(s, q, n, sx, celulas);
            return double.IsNaN(y) ? 0 : Velocidade(s, q, y, celulas);
        }
        double lo = Math.Log(1e-6), hi = Math.Log(2.0);
        if (V(Math.Exp(hi)) < velocidadeAlvo) return double.NaN;
        // abaixo de certa declividade a seção não comporta Q (V=0): procura a menor declividade viável
        for (int i = 0; i < 100; i++)
        {
            double mid = (lo + hi) / 2;
            double v = V(Math.Exp(mid));
            if (v < velocidadeAlvo) lo = mid; else hi = mid;
            if (hi - lo < 1e-6) break;
        }
        return Math.Exp(hi);
    }

    /// <summary>Menor declividade que comporta Q com lâmina relativa y/H &lt;= limite.</summary>
    public static double DeclividadeParaLamina(ISecao s, double q, double n, double laminaRelativaMax, int celulas = 1)
    {
        if (q <= 0) return 0;
        double y = laminaRelativaMax * s.Altura;
        double a = s.Area(y) * celulas, r = RaioHidraulico(s, y);
        if (a <= 0 || r <= 0) return double.NaN;
        double raiz = q * n / (a * Math.Pow(r, 2.0 / 3.0));
        return raiz * raiz;
    }
}
