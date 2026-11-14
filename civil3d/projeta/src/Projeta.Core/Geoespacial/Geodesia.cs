using System.Globalization;
using System.Text.RegularExpressions;
using Projeta.Core.Comum;

namespace Projeta.Core.Geoespacial;

/// <summary>Elipsoide de referência (semieixo maior e inverso do achatamento).</summary>
public sealed record Elipsoide(string Nome, double A, double InvF)
{
    public double F => 1 / InvF;
    /// <summary>Excentricidade ao quadrado.</summary>
    public double E2 => F * (2 - F);
    /// <summary>Terceiro achatamento n = f / (2 − f).</summary>
    public double N => F / (2 - F);

    public static readonly Elipsoide Grs80 = new("GRS 80", 6378137.0, 298.257222101);
    public static readonly Elipsoide Wgs84 = new("WGS 84", 6378137.0, 298.257223563);
}

/// <summary>
/// Sistema UTM sobre SIRGAS 2000 (oficial no Brasil) ou WGS 84. Os dois referenciais coincidem na prática
/// (diferença de centímetros), então a conversão entre eles é tratada como identidade.
/// </summary>
public sealed record SistemaUtm(int Fuso, bool Sul, string Datum = SistemaUtm.Sirgas2000)
{
    public const string Sirgas2000 = "SIRGAS 2000";
    public const string Wgs84 = "WGS 84";

    public Elipsoide Elipsoide => Datum == Wgs84 ? Elipsoide.Wgs84 : Elipsoide.Grs80;
    public double MeridianoCentral => -183 + 6.0 * Fuso;
    public string Nome => $"{Datum} / UTM {Fuso}{(Sul ? "S" : "N")}";
    public override string ToString() => Nome;

    /// <summary>Código EPSG do sistema (null quando não há um código conhecido).</summary>
    public int? Epsg
    {
        get
        {
            if (Datum == Wgs84) return (Sul ? 32700 : 32600) + Fuso;
            if (Sul && Fuso >= 17 && Fuso <= 25) return 31960 + Fuso;      // 31977 (17S) ... 31985 (25S)
            if (!Sul && Fuso >= 11 && Fuso <= 22) return 31954 + Fuso;     // 31965 (11N) ... 31976 (22N)
            return null;
        }
    }

    /// <summary>EPSG do sistema geográfico (latitude/longitude) do mesmo referencial.</summary>
    public int EpsgGeografico => Datum == Wgs84 ? 4326 : 4674;

    public static int FusoDe(double lon) => Math.Clamp((int)Math.Floor((lon + 180) / 6) + 1, 1, 60);

    public static SistemaUtm DoPonto(double lat, double lon, string datum = Sirgas2000) => new(FusoDe(lon), lat < 0, datum);

    public static SistemaUtm? DoEpsg(int epsg)
    {
        if (epsg >= 31977 && epsg <= 31985) return new SistemaUtm(epsg - 31960, true);
        if (epsg >= 31965 && epsg <= 31976) return new SistemaUtm(epsg - 31954, false);
        if (epsg > 32600 && epsg <= 32660) return new SistemaUtm(epsg - 32600, false, Wgs84);
        if (epsg > 32700 && epsg <= 32760) return new SistemaUtm(epsg - 32700, true, Wgs84);
        return null;
    }

    /// <summary>
    /// Reconhece o sistema num código ou nome livre: "SIRGAS2000.UTM-23S", "UTM84-23S", "EPSG:31983",
    /// "SIRGAS 2000 / UTM zone 23S", "WGS 84 / UTM zone 23 South", "SIRGAS 2000 UTM fuso 23 sul".
    /// SAD 69 e Córrego Alegre não são aceitos (null).
    /// </summary>
    public static SistemaUtm? DeTexto(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var t = codigo.Trim();
        var me = Regex.Match(t, @"EPSG\D{0,3}(\d{4,5})", RegexOptions.IgnoreCase);
        if (me.Success) return DoEpsg(int.Parse(me.Groups[1].Value, CultureInfo.InvariantCulture));
        if (Regex.IsMatch(t, @"^\d{4,5}$")) return DoEpsg(int.Parse(t, CultureInfo.InvariantCulture));
        string? datum = null;
        if (Regex.IsMatch(t, "SIRGAS", RegexOptions.IgnoreCase)) datum = Sirgas2000;
        else if (Regex.IsMatch(t, @"WGS\s*-?\s*84|UTM\s*-?\s*84", RegexOptions.IgnoreCase)) datum = Wgs84;
        if (datum is null) return null;
        if (!Regex.IsMatch(t, "UTM", RegexOptions.IgnoreCase)) return null;
        var mz = Regex.Match(t, @"(?:zone|fuso)\s*(\d{1,2})\s*(south|north|sul|norte|s|n)\b", RegexOptions.IgnoreCase);
        if (!mz.Success) mz = Regex.Matches(t, @"(?<![0-9])(\d{1,2})\s*([NS])(?![A-Za-z])", RegexOptions.IgnoreCase).Cast<Match>().LastOrDefault() ?? Match.Empty;
        if (!mz.Success) return null;
        int fuso = int.Parse(mz.Groups[1].Value, CultureInfo.InvariantCulture);
        if (fuso < 1 || fuso > 60) return null;
        bool sul = char.ToUpperInvariant(mz.Groups[2].Value[0]) == 'S';
        return new SistemaUtm(fuso, sul, datum);
    }
}

/// <summary>Ponto em coordenadas UTM com fator de escala e convergência meridiana (graus).</summary>
public readonly record struct PontoUtm(double E, double N, double FatorEscala, double Convergencia);

/// <summary>
/// Projeção Transversa de Mercator (UTM) pelas séries de Krüger em n até a 4ª ordem (erro abaixo de 0,1 mm dentro
/// do fuso) e latitude conforme exata (Newton), como em Karney (2011).
/// </summary>
public static class Utm
{
    public const double K0 = 0.9996;
    public const double FalsoLeste = 500000;
    public const double FalsoNorteSul = 10000000;

    private sealed class Coef
    {
        public double A, E;
        public double[] Alfa = new double[5], Beta = new double[5];
    }

    private static readonly Dictionary<Elipsoide, Coef> _coef = new();

    private static Coef C(Elipsoide el)
    {
        lock (_coef)
        {
            if (_coef.TryGetValue(el, out var c)) return c;
            double n = el.N, n2 = n * n, n3 = n2 * n, n4 = n3 * n;
            c = new Coef
            {
                A = el.A / (1 + n) * (1 + n2 / 4 + n4 / 64),
                E = Math.Sqrt(el.E2),
            };
            c.Alfa[1] = n / 2 - 2 * n2 / 3 + 5 * n3 / 16 + 41 * n4 / 180;
            c.Alfa[2] = 13 * n2 / 48 - 3 * n3 / 5 + 557 * n4 / 1440;
            c.Alfa[3] = 61 * n3 / 240 - 103 * n4 / 140;
            c.Alfa[4] = 49561 * n4 / 161280;
            c.Beta[1] = n / 2 - 2 * n2 / 3 + 37 * n3 / 96 - n4 / 360;
            c.Beta[2] = n2 / 48 + n3 / 15 - 437 * n4 / 1440;
            c.Beta[3] = 17 * n3 / 480 - 37 * n4 / 840;
            c.Beta[4] = 4397 * n4 / 161280;
            _coef[el] = c;
            return c;
        }
    }

    /// <summary>tan da latitude conforme a partir de tan da latitude geodésica.</summary>
    private static double TauLinha(double tau, double e)
    {
        double s = Math.Sqrt(1 + tau * tau);
        double sig = Math.Sinh(e * Atanh(e * tau / s));
        return tau * Math.Sqrt(1 + sig * sig) - sig * s;
    }

    /// <summary>Inverso de <see cref="TauLinha"/> por Newton.</summary>
    private static double Tau(double taul, double e)
    {
        double e2m = 1 - e * e;
        double tau = taul / e2m;
        for (int i = 0; i < 8; i++)
        {
            double tl = TauLinha(tau, e);
            double d = (taul - tl) * (1 + e2m * tau * tau) / (e2m * Math.Sqrt(1 + tl * tl) * Math.Sqrt(1 + tau * tau));
            tau += d;
            if (Math.Abs(d) < 1e-14 * Math.Max(1, Math.Abs(tau))) break;
        }
        return tau;
    }

    private static double Atanh(double x) => 0.5 * Math.Log((1 + x) / (1 - x));
    private static double Asinh(double x) => Math.Log(x + Math.Sqrt(x * x + 1));

    /// <summary>Latitude/longitude (graus) → UTM no sistema dado.</summary>
    public static PontoUtm ParaUtm(double lat, double lon, SistemaUtm sis)
    {
        if (double.IsNaN(lat) || double.IsNaN(lon) || Math.Abs(lat) >= 90) throw new ArgumentException("Latitude/longitude inválida.");
        var c = C(sis.Elipsoide);
        double phi = lat * Math.PI / 180;
        double dl = NormalizarLon(lon - sis.MeridianoCentral) * Math.PI / 180;
        double tau = Math.Tan(phi);
        double taul = TauLinha(tau, c.E);
        double cl = Math.Cos(dl);
        double xip = Math.Atan2(taul, cl);
        double etap = Asinh(Math.Sin(dl) / Math.Sqrt(taul * taul + cl * cl));
        double xi = xip, eta = etap, sigma = 1, t = 0;
        for (int j = 1; j <= 4; j++)
        {
            double a = c.Alfa[j];
            xi += a * Math.Sin(2 * j * xip) * Math.Cosh(2 * j * etap);
            eta += a * Math.Cos(2 * j * xip) * Math.Sinh(2 * j * etap);
            sigma += 2 * j * a * Math.Cos(2 * j * xip) * Math.Cosh(2 * j * etap);
            t += 2 * j * a * Math.Sin(2 * j * xip) * Math.Sinh(2 * j * etap);
        }
        double e = FalsoLeste + K0 * c.A * eta;
        double n = (sis.Sul ? FalsoNorteSul : 0) + K0 * c.A * xi;
        // fator de escala e convergência meridiana (Karney 2011)
        var el = sis.Elipsoide;
        double k = K0 * c.A / el.A * Math.Sqrt(1 + (1 - el.E2) * tau * tau) * Math.Sqrt(sigma * sigma + t * t) / Math.Sqrt(taul * taul + cl * cl);
        double s1 = Math.Sqrt(1 + taul * taul), tl = Math.Tan(dl);
        double gama = Math.Atan2(t * s1 + sigma * taul * tl, sigma * s1 - t * taul * tl);
        return new PontoUtm(e, n, k, gama * 180 / Math.PI);
    }

    /// <summary>UTM → latitude/longitude (graus) no sistema dado.</summary>
    public static (double Lat, double Lon) ParaGeo(double e, double n, SistemaUtm sis)
    {
        var c = C(sis.Elipsoide);
        double xi = (n - (sis.Sul ? FalsoNorteSul : 0)) / (K0 * c.A);
        double eta = (e - FalsoLeste) / (K0 * c.A);
        double xip = xi, etap = eta;
        for (int j = 1; j <= 4; j++)
        {
            xip -= c.Beta[j] * Math.Sin(2 * j * xi) * Math.Cosh(2 * j * eta);
            etap -= c.Beta[j] * Math.Cos(2 * j * xi) * Math.Sinh(2 * j * eta);
        }
        double se = Math.Sinh(etap), cx = Math.Cos(xip);
        double taul = Math.Sin(xip) / Math.Sqrt(se * se + cx * cx);
        double tau = Tau(taul, c.E);
        double lat = Math.Atan(tau) * 180 / Math.PI;
        double lon = sis.MeridianoCentral + Math.Atan2(se, cx) * 180 / Math.PI;
        return (lat, NormalizarLon(lon));
    }

    public static double NormalizarLon(double lon)
    {
        lon %= 360;
        if (lon > 180) lon -= 360;
        if (lon < -180) lon += 360;
        return lon;
    }
}

/// <summary>Leitura e escrita de ângulos em graus decimais ou graus, minutos e segundos.</summary>
public static class Angulos
{
    /// <summary>
    /// Lê "-19.9208", "-19,9208", "19°55'15,1\"S", "19º 55' 15\" S", "S 19 55 15.1", "43d56m08sW", "43°56'08\"O" (Oeste)
    /// ou "L" (Leste). Hemisfério S, W ou O deixa o valor negativo. Retorna NaN se não entender.
    /// </summary>
    public static double LerGraus(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return double.NaN;
        var t = texto.Trim().ToUpperInvariant().Replace("''", "\"").Replace("′", "'").Replace("″", "\"").Replace("’", "'").Replace("”", "\"");
        int sinal = 1;
        // "19D55M15S" (sem hemisfério): o S final é o símbolo dos segundos
        bool sSegundos = Regex.IsMatch(t, @"\d\s*[D°º]\s*\d+\s*M\s*[\d.,]+\s*S$");
        var mh = Regex.Match(t, @"(^[NSEWLO]\s*)|(\s*[NSEWLO]$)");
        if (mh.Success && !(sSegundos && mh.Index > 0))
        {
            char h = mh.Value.Trim()[0];
            if (h is 'S' or 'W' or 'O') sinal = -1;
            t = t.Remove(mh.Index, mh.Length).Trim();
        }
        if (t.StartsWith('-')) { sinal = -sinal; t = t[1..].Trim(); }
        else if (t.StartsWith('+')) t = t[1..].Trim();
        if (t.Length == 0) return double.NaN;
        var partes = Regex.Split(t, @"\s*[°ºD:'M""S]\s*|\s+").Where(p => p.Length > 0).ToList();
        if (partes.Count == 0 || partes.Count > 3) return double.NaN;
        double total = 0, div = 1;
        for (int i = 0; i < partes.Count; i++)
        {
            if (!Num.TentarLer(partes[i], out var v) || v < 0) return double.NaN;
            if (i > 0 && v >= 60) return double.NaN;
            total += v / div;
            div *= 60;
        }
        return sinal * total;
    }

    /// <summary>Formata em graus, minutos e segundos com hemisfério: 19°55'15,123"S.</summary>
    public static string Gms(double graus, bool latitude, int casas = 3)
    {
        if (double.IsNaN(graus)) return "-";
        char h = latitude ? (graus < 0 ? 'S' : 'N') : (graus < 0 ? 'W' : 'E');
        double a = Math.Abs(graus);
        int g = (int)Math.Floor(a);
        double mf = (a - g) * 60;
        int m = (int)Math.Floor(mf);
        double s = Math.Round((mf - m) * 60, casas);
        if (s >= 60) { s -= 60; m++; }
        if (m >= 60) { m -= 60; g++; }
        return $"{g}°{m:00}'{s.ToString("00." + new string('0', casas), Num.Br)}\"{h}";
    }
}

/// <summary>Coordenada digitada pelo usuário: geográfica (lat/long) ou UTM (E/N).</summary>
public readonly record struct CoordenadaDigitada(bool Geografica, double A, double B)
{
    public double Lat => A;
    public double Lon => B;
    public double E => A;
    public double N => B;

    /// <summary>
    /// Entende "-19.9208, -43.9378" (como o Google Maps copia), "-19,9208; -43,9378", "19°55'15\"S 43°56'08\"W",
    /// "612345,67 7795432,10" ou "612345.67; 7795432.10" (UTM: E e N). Lat/long podem vir em qualquer ordem se tiverem
    /// hemisfério (N/S e E/W/L/O).
    /// </summary>
    public static CoordenadaDigitada? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var t = texto.Trim();
        string? a = null, b = null;
        if (t.Contains(';'))
        {
            var p = t.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (p.Length != 2) return null;
            a = p[0]; b = p[1];
        }
        else
        {
            var mh = Regex.Match(t, @"^(.*?\d[^NSEWLOnsewlo]*[NSEWLOnsewlo])[\s,]+(.+)$");
            var prefixo = Regex.Match(t, @"^([NSEWLOnsewlo]\s*[^NSEWLOnsewlo]+?)[\s,]+([NSEWLOnsewlo].+)$");
            if (mh.Success && Regex.IsMatch(mh.Groups[2].Value, @"\d")) { a = mh.Groups[1].Value; b = mh.Groups[2].Value; }
            else if (prefixo.Success) { a = prefixo.Groups[1].Value; b = prefixo.Groups[2].Value; }
            else if (t.Count(c => c == ',') == 1 && t.Contains('.')) { var p = t.Split(','); a = p[0]; b = p[1]; }
            else
            {
                var p = Regex.Split(t, @"\s+").Where(x => x.Length > 0).ToArray();
                if (p.Length == 2) { a = p[0].TrimEnd(','); b = p[1]; }
                else if (p.Length == 1 && t.Count(c => c == ',') == 1) { var q = t.Split(','); a = q[0]; b = q[1]; }
            }
        }
        if (a is null || b is null) return null;
        a = a.Trim().TrimEnd(','); b = b.Trim();
        static bool TemHem(string s) => Regex.IsMatch(s, @"(^[NSEWLOnsewlo]\s*\d)|(\d[\s°º'""]*[NSEWLOnsewlo]$)");
        static bool EhLon(string s) => Regex.IsMatch(s, @"(^[EWLOewlo]\s*\d)|(\d[\s°º'""]*[EWLOewlo]$)");
        bool gms = Regex.IsMatch(a + b, @"[°º'""]") || TemHem(a) || TemHem(b);
        if (!gms && Num.TentarLer(a, out var x) && Num.TentarLer(b, out var y))
        {
            if (Math.Abs(x) <= 90 && Math.Abs(y) <= 180) return new CoordenadaDigitada(true, x, y);
            if (x > 100000 && x < 900000 && y >= 0 && y <= 10000000) return new CoordenadaDigitada(false, x, y);
            if (y > 100000 && y < 900000 && x > 1000000 && x <= 10000000) return new CoordenadaDigitada(false, y, x);
            return null;
        }
        double ga = Angulos.LerGraus(a), gb = Angulos.LerGraus(b);
        if (double.IsNaN(ga) || double.IsNaN(gb)) return null;
        if (EhLon(a) && !EhLon(b)) (ga, gb) = (gb, ga);
        if (Math.Abs(ga) > 90 || Math.Abs(gb) > 180) return null;
        return new CoordenadaDigitada(true, ga, gb);
    }
}

/// <summary>
/// Transformação de semelhança 2D (Helmert): x' = a·x − b·y + tx ; y' = b·x + a·y + ty, ajustada por mínimos
/// quadrados a pares de pontos. Com escala fixa vira um movimento rígido (rotação + translação).
/// </summary>
public sealed class Transformacao2D
{
    public double A { get; init; } = 1;
    public double B { get; init; }
    public double Tx { get; init; }
    public double Ty { get; init; }

    public double Escala => Math.Sqrt(A * A + B * B);
    /// <summary>Rotação em graus (anti-horária).</summary>
    public double Rotacao => Math.Atan2(B, A) * 180 / Math.PI;

    public (double X, double Y) Aplicar(double x, double y) => (A * x - B * y + Tx, B * x + A * y + Ty);

    /// <summary>Resíduos (m) de cada par depois da transformação.</summary>
    public List<double> Residuos { get; } = new();
    public double Rms => Residuos.Count == 0 ? 0 : Math.Sqrt(Residuos.Sum(r => r * r) / Residuos.Count);

    public static Transformacao2D Ajustar(IReadOnlyList<(double X, double Y)> origem, IReadOnlyList<(double X, double Y)> destino, bool escalaFixa)
    {
        if (origem.Count != destino.Count) throw new ArgumentException("As listas de pontos têm tamanhos diferentes.");
        if (origem.Count < 2) throw new ArgumentException("São precisos pelo menos 2 pares de pontos.");
        int n = origem.Count;
        double ox = origem.Average(p => p.X), oy = origem.Average(p => p.Y);
        double dx = destino.Average(p => p.X), dy = destino.Average(p => p.Y);
        double sxx = 0, sab = 0, sba = 0;
        for (int i = 0; i < n; i++)
        {
            double x = origem[i].X - ox, y = origem[i].Y - oy, u = destino[i].X - dx, v = destino[i].Y - dy;
            sxx += x * x + y * y;
            sab += x * u + y * v;
            sba += x * v - y * u;
        }
        if (sxx < 1e-12) throw new ArgumentException("Os pontos de origem coincidem: escolha pontos afastados.");
        double a = sab / sxx, b = sba / sxx;
        if (escalaFixa)
        {
            double s = Math.Sqrt(a * a + b * b);
            if (s < 1e-15) throw new ArgumentException("Não foi possível determinar a rotação.");
            a /= s; b /= s;
        }
        var t = new Transformacao2D { A = a, B = b, Tx = dx - (a * ox - b * oy), Ty = dy - (b * ox + a * oy) };
        for (int i = 0; i < n; i++)
        {
            var (x, y) = t.Aplicar(origem[i].X, origem[i].Y);
            t.Residuos.Add(Math.Sqrt((x - destino[i].X) * (x - destino[i].X) + (y - destino[i].Y) * (y - destino[i].Y)));
        }
        return t;
    }
}
