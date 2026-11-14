using System.Globalization;

namespace Projeta.Sinalizacao.Core.Comum;

/// <summary>Formatação numérica no padrão brasileiro e utilidades.</summary>
public static class Num
{
    public static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string F(double v, int casas = 2) => v.ToString("N" + casas, Br);
    public static string F0(double v) => v.ToString("0.##", Br);

    /// <summary>Dimensão em metros sem zeros à direita: 0,5 → "0,50"; 0,75 → "0,75"; 1 → "1,00".</summary>
    public static string M(double v) => v.ToString("0.00", Br);

    /// <summary>Lê número aceitando vírgula ou ponto como decimal ("0,75", "0.75").</summary>
    public static bool TentarLer(string? s, out double v)
    {
        v = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().Replace(" ", "");
        // "1.234,56" → 1234.56 ; "1234,56" → 1234.56 ; "1234.56" → 1234.56
        if (s.Contains(',') && s.Contains('.')) s = s.Replace(".", "").Replace(',', '.');
        else s = s.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Float, Inv, out v);
    }

    public static double Ler(string? s, double padrao) => TentarLer(s, out var v) ? v : padrao;

    public static double Graus(double rad) => rad * 180.0 / Math.PI;
    public static double Rad(double graus) => graus * Math.PI / 180.0;

    /// <summary>Normaliza ângulo para (-π, π].</summary>
    public static double NormAng(double a)
    {
        while (a <= -Math.PI) a += 2 * Math.PI;
        while (a > Math.PI) a -= 2 * Math.PI;
        return a;
    }

    public static double Arred(double v, int casas = 3) => Math.Round(v, casas, MidpointRounding.AwayFromZero);

    /// <summary>Arredonda para cima no múltiplo informado (ex.: 0,05 m).</summary>
    public static double ArredCima(double v, double passo) => passo <= 0 ? v : Math.Ceiling(v / passo - 1e-9) * passo;
}

/// <summary>Estaqueamento (estacas de 20 m ou km).</summary>
public static class Estaca
{
    /// <summary>Formata estaca: 245,6 com 20 m → "12+5,600"; com 1000 m → "0+245,600".</summary>
    public static string Formatar(double estacao, double passo = 20.0, int casas = 3)
    {
        if (passo <= 0) passo = 20;
        bool neg = estacao < 0;
        double a = Math.Abs(estacao);
        long inteira = (long)Math.Floor(a / passo + 1e-9);
        double resto = a - inteira * passo;
        if (resto < 0) resto = 0;
        if (Math.Round(resto, casas) >= passo) { inteira++; resto = 0; }
        return (neg ? "-" : "") + inteira.ToString(Num.Inv) + "+" + resto.ToString("0." + new string('0', casas), Num.Br);
    }

    /// <summary>Lê "12+5,600" (ou número puro em metros).</summary>
    public static bool TentarLer(string? s, double passo, out double estacao)
    {
        estacao = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        int i = s.IndexOf('+');
        if (i < 0) return Num.TentarLer(s, out estacao);
        if (!long.TryParse(s[..i].Trim(), out long est)) return false;
        if (!Num.TentarLer(s[(i + 1)..], out double resto)) return false;
        estacao = est * passo + resto;
        return true;
    }
}
