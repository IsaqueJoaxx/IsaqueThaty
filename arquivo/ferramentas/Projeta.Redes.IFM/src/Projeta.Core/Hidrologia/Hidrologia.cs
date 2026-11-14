namespace Projeta.Core.Hidrologia;

public enum MetodoTc
{
    /// <summary>Usa o tempo informado na bacia (ou o tc mínimo da rede).</summary>
    Informado,
    /// <summary>Kirpich: tc = 57·(L³/H)^0,385 (L em km, H em m, tc em min).</summary>
    Kirpich,
}

public static class TempoConcentracao
{
    /// <summary>Tempo de concentração (min) por Kirpich. L = comprimento do talvegue (m), H = desnível (m).</summary>
    public static double Kirpich(double comprimentoM, double desnivelM)
    {
        if (comprimentoM <= 0) return 0;
        desnivelM = Math.Max(desnivelM, 0.01);
        double lKm = comprimentoM / 1000.0;
        return 57.0 * Math.Pow(lKm * lKm * lKm / desnivelM, 0.385);
    }

    public static double Calcular(MetodoTc metodo, double comprimentoM, double desnivelM, double informado)
    {
        return metodo switch
        {
            MetodoTc.Kirpich => Kirpich(comprimentoM, desnivelM),
            _ => informado,
        };
    }
}

public static class MetodoRacional
{
    /// <summary>Vazão (m³/s) = C·i·A / 360, com i em mm/h e A em hectares.</summary>
    public static double Vazao(double coefEscoamento, double intensidadeMmH, double areaHa) => coefEscoamento * intensidadeMmH * areaHa / 360.0;

    /// <summary>Vazão (m³/s) a partir do produto C·A acumulado (ha).</summary>
    public static double VazaoCA(double somaCA_ha, double intensidadeMmH) => somaCA_ha * intensidadeMmH / 360.0;

    /// <summary>Coeficiente de escoamento médio ponderado pelas áreas.</summary>
    public static double CPonderado(IEnumerable<(double area, double c)> partes)
    {
        double a = 0, ca = 0;
        foreach (var (area, c) in partes) { a += area; ca += area * c; }
        return a <= 0 ? 0 : ca / a;
    }
}

/// <summary>Chuva de projeto pelo método dos blocos alternados.</summary>
public static class Hietograma
{
    /// <summary>
    /// Retorna as intensidades (mm/h) de cada intervalo, de duração total <paramref name="duracaoMin"/>
    /// e passo <paramref name="passoMin"/>, com o pico posicionado no centro.
    /// </summary>
    public static double[] BlocosAlternados(EquacaoChuva eq, double tempoRetorno, double duracaoMin, double passoMin)
    {
        int n = Math.Max(1, (int)Math.Round(duracaoMin / passoMin));
        var incrementos = new double[n];
        double acumAnterior = 0;
        for (int k = 1; k <= n; k++)
        {
            double t = k * passoMin;
            double p = eq.Altura(tempoRetorno, t);
            incrementos[k - 1] = Math.Max(0, p - acumAnterior);
            acumAnterior = p;
        }
        var ordenados = incrementos.OrderByDescending(x => x).ToArray();
        var res = new double[n];
        int centro = (n - 1) / 2;
        for (int k = 0; k < n; k++)
        {
            // 0 -> centro, 1 -> centro+1, 2 -> centro-1, 3 -> centro+2 ...
            int desloc = (k + 1) / 2 * (k % 2 == 1 ? 1 : -1);
            int pos = centro + desloc;
            if (pos < 0 || pos >= n) pos = Array.IndexOf(res, 0.0);
            res[pos] = ordenados[k];
        }
        return res.Select(h => h * 60.0 / passoMin).ToArray();
    }
}
