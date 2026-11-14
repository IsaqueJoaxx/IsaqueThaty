using System.Globalization;

namespace Projeta.Civil3D;

internal sealed record GradePoint(double Station, double Elevation);

// MaxPercent: teto das rampas (no máximo 7%), limite e não inclinação obrigatória.
// Spacing: distância desejada entre PIVs; só preferência, nunca bloqueia.
// Tolerance: quanto o greide pode se afastar da linha que acompanha o terreno ao retirar PIVs.
// WarnOffset: corte ou aterro acima disso vira aviso; nunca bloqueia.
// StartOnGround: o greide começa na cota do terreno.
internal sealed record GradeOptions(double MaxPercent, double Spacing, double Tolerance, double WarnOffset, bool StartOnGround);

internal sealed record GradePreview(IReadOnlyList<GradePoint> Pvis, IReadOnlyList<string> Warnings)
{
    public string SourceSignature { get; init; } = "";
    // Greide menos terreno em cada PIV: positivo = aterro, negativo = corte.
    public IReadOnlyList<double> PviOffsets { get; init; } = Array.Empty<double>();
}

// Greide automático em três passos.
// 1. Linha que acompanha o terreno sem passar da rampa máxima: entre todas as linhas com rampas
//    dentro do limite, a de menor soma de cortes e aterros (área). Onde o terreno é mais suave que o
//    limite, ela é o próprio terreno; onde é mais íngreme, corta e aterra o necessário e volta ao
//    terreno logo depois. O trecho inteiro é resolvido de uma vez, então o afastamento não passa de
//    uma tangente para a seguinte.
// 2. Menor número de PIVs que fica dentro da tolerância dessa linha. Tangente mais curta que a
//    distância desejada custa mais, então só fica quando o relevo pede.
// 3. Dois PIVs mais próximos que a distância desejada viram um só, no cruzamento das tangentes
//    vizinhas, quando isso continua dentro da tolerância.
// Os PIVs dos passos 2 e 3 ficam sobre a linha do passo 1 ou sobre tangentes já aceitas, e a linha
// nunca passa da rampa máxima; por isso nenhuma tangente passa. Sem curvas verticais.
internal static class GradeSolver
{
    public const int MaxSamples = 20000;
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static GradePreview Solve(IReadOnlyList<GradePoint> pts, GradeOptions o)
    {
        if (!double.IsFinite(o.MaxPercent) || o.MaxPercent <= 0 || o.MaxPercent > 7)
            throw new ArgumentException("A rampa máxima precisa estar acima de zero e no máximo em 7%.");
        if (!double.IsFinite(o.Spacing) || o.Spacing <= 0)
            throw new ArgumentException("Informe uma distância desejada entre PIVs maior que zero.");
        if (!double.IsFinite(o.Tolerance) || o.Tolerance < 0 || !double.IsFinite(o.WarnOffset) || o.WarnOffset < 0)
            throw new ArgumentException("A tolerância e o limite de aviso não podem ser negativos.");
        if (pts.Count < 3 || pts.Count > MaxSamples)
            throw new ArgumentException($"São necessárias de 3 a {MaxSamples.ToString("N0", Br)} leituras do terreno.");
        var n = pts.Count;
        var s = new double[n];
        var z = new double[n];
        for (var i = 0; i < n; i++)
        {
            s[i] = pts[i].Station;
            z[i] = pts[i].Elevation;
            if (!double.IsFinite(s[i]) || !double.IsFinite(z[i]) || (i > 0 && s[i] <= s[i - 1]))
                throw new ArgumentException("As leituras precisam ser finitas e ter progressivas crescentes.");
        }
        var slope = o.MaxPercent / 100;
        var line = Follow(s, z, slope, o.StartOnGround);
        for (var i = 1; i < n; i++)
            if (!double.IsFinite(line[i]) || Math.Abs(line[i] - line[i - 1]) > slope * (s[i] - s[i - 1]) + 1e-7)
                throw new InvalidOperationException("A linha que acompanha o terreno passou da rampa máxima.");
        var pvis = Simplify(s, line, o.Tolerance, o.Spacing);
        pvis = MergeShort(pvis, s, line, o.Tolerance, o.Spacing, slope);
        for (var i = 1; i < pvis.Count; i++)
        {
            var percent = 100 * Math.Abs((pvis[i].Elevation - pvis[i - 1].Elevation) / (pvis[i].Station - pvis[i - 1].Station));
            if (!double.IsFinite(percent) || percent > o.MaxPercent + 1e-7)
                throw new InvalidOperationException("A prévia excedeu a rampa máxima.");
        }
        var offsets = pvis.Select(p => p.Elevation - Interpolate(s, z, p.Station)).ToArray();
        return new GradePreview(pvis, Warnings(s, z, pvis, o)) { PviOffsets = offsets };
    }

    // Passo 1. Menor soma de |linha − terreno| ponderada pelo comprimento de cada leitura, com
    // |linha[k+1] − linha[k]| ≤ rampa × distância. Programação dinâmica exata sobre funções convexas
    // lineares por partes: as quebras ficam em duas filas, à esquerda e à direita do mínimo.
    internal static double[] Follow(double[] s, double[] z, double slope, bool startOnGround)
    {
        var n = s.Length;
        var w = new double[n];
        for (var k = 0; k < n; k++)
            w[k] = Math.Max(((k > 0 ? s[k] - s[k - 1] : 0) + (k < n - 1 ? s[k + 1] - s[k] : 0)) / 2, 1e-9);
        if (startOnGround) w[0] = 1e4 * w.Sum(); // peso que prende o início na cota do terreno
        var left = new PriorityQueue<(double At, double Weight), double>();  // maior primeiro
        var right = new PriorityQueue<(double At, double Weight), double>(); // menor primeiro
        double shiftLeft = 0, shiftRight = 0;
        double TopLeft() => left.Peek().At + shiftLeft;
        double TopRight() => right.Peek().At + shiftRight;
        void PushLeft(double at, double weight) => left.Enqueue((at - shiftLeft, weight), -(at - shiftLeft));
        void PushRight(double at, double weight) => right.Enqueue((at - shiftRight, weight), at - shiftRight);
        void LeftToRight(double weight)
        {
            while (weight > 1e-12 && left.Count > 0)
            {
                var (at, wt) = left.Dequeue();
                at += shiftLeft;
                if (wt > weight + 1e-12) { PushLeft(at, wt - weight); PushRight(at, weight); weight = 0; }
                else { PushRight(at, wt); weight -= wt; }
            }
        }
        void RightToLeft(double weight)
        {
            while (weight > 1e-12 && right.Count > 0)
            {
                var (at, wt) = right.Dequeue();
                at += shiftRight;
                if (wt > weight + 1e-12) { PushRight(at, wt - weight); PushLeft(at, weight); weight = 0; }
                else { PushLeft(at, wt); weight -= wt; }
            }
        }
        var best = new double[n];
        for (var k = 0; k < n; k++)
        {
            if (k > 0)
            {
                var c = slope * (s[k] - s[k - 1]);
                shiftLeft -= c;
                shiftRight += c;
            }
            // + w·max(0, x − z)
            if (left.Count == 0 || z[k] >= TopLeft()) PushRight(z[k], w[k]);
            else { PushLeft(z[k], w[k]); LeftToRight(w[k]); }
            // + w·max(0, z − x)
            if (right.Count == 0 || z[k] <= TopRight()) PushLeft(z[k], w[k]);
            else { PushRight(z[k], w[k]); RightToLeft(w[k]); }
            best[k] = (TopLeft() + TopRight()) / 2;
        }
        var line = new double[n];
        line[n - 1] = best[n - 1];
        for (var k = n - 2; k >= 0; k--)
        {
            var c = slope * (s[k + 1] - s[k]);
            line[k] = Math.Clamp(best[k], line[k + 1] - c, line[k + 1] + c);
        }
        return line;
    }

    // Passo 2. Custo 1 por tangente, mais até 1 se ela for mais curta que a distância desejada.
    // Cada tangente precisa ficar dentro da tolerância da linha em todas as leituras que cobre.
    private static List<GradePoint> Simplify(double[] s, double[] line, double tolerance, double spacing)
    {
        var n = s.Length;
        var cost = new double[n];
        var previous = new int[n];
        Array.Fill(cost, double.PositiveInfinity);
        Array.Fill(previous, -1);
        cost[0] = 0;
        for (var a = 0; a < n - 1; a++)
        {
            if (double.IsPositiveInfinity(cost[a])) continue;
            double lo = double.NegativeInfinity, hi = double.PositiveInfinity;
            for (var b = a + 1; b < n; b++)
            {
                var ds = s[b] - s[a];
                var m = (line[b] - line[a]) / ds;
                if (m >= lo - 1e-12 && m <= hi + 1e-12)
                {
                    var c = cost[a] + 1 + (ds < spacing ? (spacing - ds) / spacing : 0);
                    if (c < cost[b] - 1e-12) { cost[b] = c; previous[b] = a; }
                }
                lo = Math.Max(lo, (line[b] - tolerance - line[a]) / ds);
                hi = Math.Min(hi, (line[b] + tolerance - line[a]) / ds);
                if (lo > hi + 1e-12) break;
            }
        }
        var order = new List<int>();
        for (var b = n - 1; b >= 0; b = previous[b]) order.Add(b);
        order.Reverse();
        return order.Select(i => new GradePoint(s[i], line[i])).ToList();
    }

    // Passo 3. Tangente curta entre dois PIVs internos: os dois PIVs dão lugar ao cruzamento das
    // tangentes vizinhas, que mantêm as mesmas rampas, se o resultado continuar na tolerância.
    private static List<GradePoint> MergeShort(List<GradePoint> pvis, double[] s, double[] line, double tolerance, double spacing, double slope)
    {
        var result = pvis.ToList();
        bool Fits(GradePoint a, GradePoint b)
        {
            var m = (b.Elevation - a.Elevation) / (b.Station - a.Station);
            if (!double.IsFinite(m) || Math.Abs(m) > slope + 1e-12) return false;
            var k = Array.BinarySearch(s, a.Station);
            if (k < 0) k = ~k;
            for (; k < s.Length && s[k] <= b.Station + 1e-9; k++)
                if (Math.Abs(a.Elevation + m * (s[k] - a.Station) - line[k]) > tolerance + 1e-9) return false;
            return true;
        }
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var i in Enumerable.Range(1, Math.Max(0, result.Count - 3)).OrderBy(i => result[i + 1].Station - result[i].Station))
            {
                if (result[i + 1].Station - result[i].Station >= spacing - 1e-9) break;
                var (p0, p1, p2, p3) = (result[i - 1], result[i], result[i + 1], result[i + 2]);
                var m1 = (p1.Elevation - p0.Elevation) / (p1.Station - p0.Station);
                var m2 = (p3.Elevation - p2.Elevation) / (p3.Station - p2.Station);
                if (Math.Abs(m1 - m2) < 1e-9) continue;
                var x = (p2.Elevation - p1.Elevation + m1 * p1.Station - m2 * p2.Station) / (m1 - m2);
                if (!(x > p1.Station + 1e-3 && x < p2.Station - 1e-3)) continue;
                var cross = new GradePoint(x, p1.Elevation + m1 * (x - p1.Station));
                if (!Fits(p0, cross) || !Fits(cross, p3)) continue;
                result.RemoveRange(i, 2);
                result.Insert(i, cross);
                changed = true;
                break;
            }
        }
        return result;
    }

    private static List<string> Warnings(double[] s, double[] z, List<GradePoint> pvis, GradeOptions o)
    {
        var n = s.Length;
        var dev = new double[n];
        for (var k = 0; k < n; k++) dev[k] = Interpolate(pvis, s[k]) - z[k];
        var fill = Enumerable.Range(0, n).MaxBy(k => dev[k]);
        var cut = Enumerable.Range(0, n).MinBy(k => dev[k]);
        var warnings = new List<string>
        {
            $"Maior aterro: {M(Math.Max(0, dev[fill]))} m (prog. {M(s[fill])}). Maior corte: {M(Math.Max(0, -dev[cut]))} m (prog. {M(s[cut])})."
        };
        var runs = new List<(int From, int To, double Fill, double Cut, bool Steep)>();
        for (var k = 0; k < n;)
        {
            if (Math.Abs(dev[k]) <= o.WarnOffset + 1e-9) { k++; continue; }
            var from = k;
            double f = 0, c = 0;
            for (; k < n && Math.Abs(dev[k]) > o.WarnOffset + 1e-9; k++) { f = Math.Max(f, dev[k]); c = Math.Max(c, -dev[k]); }
            var to = k - 1;
            var steep = false;
            for (var j = Math.Max(0, from - 1); j < Math.Min(n - 1, to + 1); j++)
                if (Math.Abs(z[j + 1] - z[j]) > o.MaxPercent / 100 * (s[j + 1] - s[j]) + 1e-9) steep = true;
            runs.Add((from, to, f, c, steep));
        }
        if (runs.Count > 0)
        {
            warnings.Add($"{runs.Count} trecho(s) com corte ou aterro acima de {M(o.WarnOffset)} m (aviso, não impede a criação):");
            foreach (var r in runs.OrderByDescending(r => Math.Max(r.Fill, r.Cut)).Take(8).OrderBy(r => r.From))
            {
                var parts = new List<string>();
                if (r.Cut > o.WarnOffset + 1e-9) parts.Add($"corte até {M(r.Cut)} m");
                if (r.Fill > o.WarnOffset + 1e-9) parts.Add($"aterro até {M(r.Fill)} m");
                warnings.Add($"• prog. {M(s[r.From])} a {M(s[r.To])}: {string.Join(" e ", parts)}" +
                             (r.Steep ? $"; terreno com rampa acima de {M(o.MaxPercent)}%" : ""));
            }
            if (runs.Count > 8) warnings.Add($"• e mais {runs.Count - 8} trecho(s) menores.");
        }
        var shortCount = 0;
        for (var i = 1; i < pvis.Count; i++)
            if (pvis[i].Station - pvis[i - 1].Station < o.Spacing - 1e-6) shortCount++;
        if (shortCount > 0)
            warnings.Add($"{shortCount} tangente(s) com menos de {M(o.Spacing)} m, mantida(s) porque o relevo pede dentro da tolerância.");
        warnings.Add("Curvas verticais, rampa mínima, visibilidade, drenagem e critérios do DER-MG não são verificados. Base para você editar; não é greide final.");
        return warnings;
    }

    private static string M(double value) => value.ToString("N2", Br);

    private static double Interpolate(double[] s, double[] z, double x)
    {
        var k = Array.BinarySearch(s, x);
        if (k >= 0) return z[k];
        k = ~k;
        if (k <= 0) return z[0];
        if (k >= s.Length) return z[^1];
        return z[k - 1] + (z[k] - z[k - 1]) * (x - s[k - 1]) / (s[k] - s[k - 1]);
    }

    private static double Interpolate(List<GradePoint> pvis, double x)
    {
        for (var j = 1; j < pvis.Count; j++)
            if (x <= pvis[j].Station + 1e-9)
            {
                var a = pvis[j - 1];
                var b = pvis[j];
                return a.Elevation + (b.Elevation - a.Elevation) * (x - a.Station) / (b.Station - a.Station);
            }
        return pvis[^1].Elevation;
    }
}
