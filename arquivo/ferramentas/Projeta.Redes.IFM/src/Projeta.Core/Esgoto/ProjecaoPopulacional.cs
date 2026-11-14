namespace Projeta.Core.Esgoto;

public enum MetodoProjecao { Aritmetico, Geometrico, TaxaDecrescente, Logistico, TaxaArbitrada }

public sealed record ResultadoProjecao(MetodoProjecao Metodo, double Populacao, string Detalhe, bool Valido);

/// <summary>
/// Métodos clássicos de projeção populacional (ver p.ex. Von Sperling, "Introdução à qualidade das águas e ao tratamento de esgotos").
/// Censos: (t0, P0), (t1, P1), (t2, P2) em ordem cronológica.
/// </summary>
public static class ProjecaoPopulacional
{
    public static ResultadoProjecao Aritmetico(double t0, double p0, double t2, double p2, double t)
    {
        double ka = (p2 - p0) / (t2 - t0);
        return new(MetodoProjecao.Aritmetico, p0 + ka * (t - t0), $"Ka = {ka:0.###} hab/ano", t2 > t0);
    }

    public static ResultadoProjecao Geometrico(double t0, double p0, double t2, double p2, double t)
    {
        if (p0 <= 0 || p2 <= 0) return new(MetodoProjecao.Geometrico, double.NaN, "Populações devem ser positivas", false);
        double kg = (Math.Log(p2) - Math.Log(p0)) / (t2 - t0);
        return new(MetodoProjecao.Geometrico, p0 * Math.Exp(kg * (t - t0)), $"Kg = {kg:0.######} /ano ({(Math.Exp(kg) - 1) * 100:0.###}% a.a.)", true);
    }

    /// <summary>População de saturação a partir de 3 censos igualmente espaçados.</summary>
    public static double Saturacao(double p0, double p1, double p2)
    {
        double den = p0 * p2 - p1 * p1;
        if (Math.Abs(den) < 1e-9) return double.NaN;
        return (2 * p0 * p1 * p2 - p1 * p1 * (p0 + p2)) / den;
    }

    public static ResultadoProjecao TaxaDecrescente(double t0, double p0, double t1, double p1, double t2, double p2, double t)
    {
        double ps = Saturacao(p0, p1, p2);
        if (double.IsNaN(ps) || ps <= p2)
            return new(MetodoProjecao.TaxaDecrescente, double.NaN, "Censos não indicam saturação (requer P0·P2 < P1² e crescimento decrescente)", false);
        double kd = -Math.Log((ps - p2) / (ps - p0)) / (t2 - t0);
        double pt = p0 + (ps - p0) * (1 - Math.Exp(-kd * (t - t0)));
        return new(MetodoProjecao.TaxaDecrescente, pt, $"Ps = {ps:0}; Kd = {kd:0.######}", true);
    }

    public static ResultadoProjecao Logistico(double t0, double p0, double t1, double p1, double t2, double p2, double t)
    {
        bool igualmenteEspacados = Math.Abs((t1 - t0) - (t2 - t1)) < 1e-6;
        double ps = Saturacao(p0, p1, p2);
        if (!igualmenteEspacados) return new(MetodoProjecao.Logistico, double.NaN, "Os censos devem ser igualmente espaçados", false);
        if (double.IsNaN(ps) || ps <= p2 || !(p0 < p1 && p1 < p2) || !(p0 * p2 < p1 * p1))
            return new(MetodoProjecao.Logistico, double.NaN, "Condições não atendidas: P0 < P1 < P2 e P0·P2 < P1²", false);
        double c = (ps - p0) / p0;
        double k1 = 1.0 / (t2 - t1) * Math.Log(p0 * (ps - p1) / (p1 * (ps - p0)));
        double pt = ps / (1 + c * Math.Exp(k1 * (t - t0)));
        return new(MetodoProjecao.Logistico, pt, $"Ps = {ps:0}; c = {c:0.####}; K1 = {k1:0.######}", true);
    }

    public static ResultadoProjecao TaxaArbitrada(double tBase, double pBase, double taxaPercentAno, double t)
    {
        double pt = pBase * Math.Pow(1 + taxaPercentAno / 100.0, t - tBase);
        return new(MetodoProjecao.TaxaArbitrada, pt, $"{taxaPercentAno:0.###}% a.a. sobre {pBase:0} hab ({tBase:0})", true);
    }

    public static List<ResultadoProjecao> Todos(double t0, double p0, double t1, double p1, double t2, double p2, double t, double taxaArbitrada = double.NaN)
    {
        var l = new List<ResultadoProjecao>
        {
            Aritmetico(t0, p0, t2, p2, t),
            Geometrico(t0, p0, t2, p2, t),
            TaxaDecrescente(t0, p0, t1, p1, t2, p2, t),
            Logistico(t0, p0, t1, p1, t2, p2, t),
        };
        if (!double.IsNaN(taxaArbitrada)) l.Add(TaxaArbitrada(t2, p2, taxaArbitrada, t));
        return l;
    }
}
