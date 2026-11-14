using System.Globalization;

namespace Projeta.Civil3D;

// Volume por seções sem nada do AutoCAD, para dar para testar fora do Civil 3D.
// Uma seção é uma linha de afastamento (x) e cota (z), com quebras onde a superfície não existe.
internal sealed class SectionLine
{
    private readonly List<(double X0, double Z0, double X1, double Z1)> segments = new();

    public IReadOnlyList<(double X0, double Z0, double X1, double Z1)> Segments => segments;
    public bool IsEmpty => segments.Count == 0;
    public double Left => segments.Count == 0 ? double.NaN : segments.Min(s => s.X0);
    public double Right => segments.Count == 0 ? double.NaN : segments.Max(s => s.X1);

    // points em ordem; connectedToNext[i] diz se existe trecho entre o ponto i e o i + 1 (sem a lista, todos existem).
    public static SectionLine From(IReadOnlyList<(double X, double Z)> points, IReadOnlyList<bool>? connectedToNext = null)
    {
        var line = new SectionLine();
        for (var i = 0; i + 1 < points.Count; i++)
        {
            if (connectedToNext is not null && i < connectedToNext.Count && !connectedToNext[i]) continue;
            var (x0, z0) = points[i];
            var (x1, z1) = points[i + 1];
            if (!double.IsFinite(x0) || !double.IsFinite(z0) || !double.IsFinite(x1) || !double.IsFinite(z1)) continue;
            if (Math.Abs(x1 - x0) < 1e-9) continue; // trecho vertical: sem largura, sem área
            line.segments.Add(x0 < x1 ? (x0, z0, x1, z1) : (x1, z1, x0, z0));
        }
        return line;
    }

    // Como o Civil 3D entrega: next[i] é o índice do ponto ligado ao ponto i (SegmentTo), ou -1 se nenhum.
    // Os pontos sem ligação ficam de fora: numa superfície mais estreita que a linha de amostragem, o Civil 3D
    // lista pontos nas pontas da linha (fora da superfície) que não pertencem à seção. Visto no teste da Prévia 18.
    public static SectionLine FromLinks(IReadOnlyList<(double X, double Z)> points, IReadOnlyList<int> next)
    {
        var line = new SectionLine();
        for (var i = 0; i < points.Count && i < next.Count; i++)
        {
            var j = next[i];
            if (j < 0 || j >= points.Count || j == i) continue;
            var (x0, z0) = points[i];
            var (x1, z1) = points[j];
            if (!double.IsFinite(x0) || !double.IsFinite(z0) || !double.IsFinite(x1) || !double.IsFinite(z1)) continue;
            if (Math.Abs(x1 - x0) < 1e-9) continue;
            line.segments.Add(x0 < x1 ? (x0, z0, x1, z1) : (x1, z1, x0, z0));
        }
        return line;
    }

    // Trecho que cobre o afastamento x; false onde a seção não existe.
    public bool TryCover(double x, out (double X0, double Z0, double X1, double Z1) segment)
    {
        foreach (var s in segments)
            if (x >= s.X0 - 1e-9 && x <= s.X1 + 1e-9) { segment = s; return true; }
        segment = default;
        return false;
    }

    public static double At((double X0, double Z0, double X1, double Z1) s, double x) =>
        s.Z0 + (s.Z1 - s.Z0) * (x - s.X0) / (s.X1 - s.X0);
}

// Width: largura onde as duas seções existem (só nela há área). Left/Right: afastamentos das pontas dessa faixa.
internal readonly record struct SectionAreas(double Cut, double Fill, double Width, double Left, double Right);

// Área de corte e aterro numa linha de amostragem. Cut/Fill nulos: faltou uma das seções (entra como zero no volume).
internal sealed record SectionStation(double Station, string Name, double? Cut, double? Fill, string? Note);

internal sealed record SectionVolumeRow(double Station, string Name, double CutArea, double FillArea,
    double CutVolume, double FillVolume, double CumulativeCut, double CumulativeFill, string? Note);

internal static class SectionMath
{
    // Corte: terreno acima do projeto. Aterro: projeto acima do terreno. Só onde as duas seções existem,
    // como na lista de materiais do Civil 3D; onde uma delas falta, a área não é contada.
    public static SectionAreas Compare(SectionLine ground, SectionLine design)
    {
        if (ground.IsEmpty || design.IsEmpty) return new SectionAreas(0, 0, 0, double.NaN, double.NaN);
        var lo = Math.Max(ground.Left, design.Left);
        var hi = Math.Min(ground.Right, design.Right);
        if (hi - lo <= 1e-9) return new SectionAreas(0, 0, 0, double.NaN, double.NaN);
        var xs = new List<double> { lo, hi };
        foreach (var line in new[] { ground, design })
            foreach (var s in line.Segments)
            {
                if (s.X0 > lo && s.X0 < hi) xs.Add(s.X0);
                if (s.X1 > lo && s.X1 < hi) xs.Add(s.X1);
            }
        xs.Sort();
        double cut = 0, fill = 0, width = 0, left = double.NaN, right = double.NaN;
        for (var i = 0; i + 1 < xs.Count; i++)
        {
            var a = xs[i];
            var b = xs[i + 1];
            var h = b - a;
            if (h <= 1e-9) continue;
            // Entre dois vértices seguidos as duas linhas são retas: a diferença também é.
            var m = (a + b) / 2;
            if (!ground.TryCover(m, out var g) || !design.TryCover(m, out var d)) continue;
            var d0 = SectionLine.At(d, a) - SectionLine.At(g, a);
            var d1 = SectionLine.At(d, b) - SectionLine.At(g, b);
            width += h;
            if (double.IsNaN(left)) left = a;
            right = b;
            if (d0 >= 0 && d1 >= 0) fill += (d0 + d1) / 2 * h;
            else if (d0 <= 0 && d1 <= 0) cut -= (d0 + d1) / 2 * h;
            else
            {
                // As linhas se cruzam dentro do trecho: um triângulo de cada lado do ponto de passagem.
                var t = d0 / (d0 - d1);
                if (d0 > 0) { fill += d0 * t * h / 2; cut -= d1 * (1 - t) * h / 2; }
                else { cut -= d0 * t * h / 2; fill += d1 * (1 - t) * h / 2; }
            }
        }
        return new SectionAreas(cut, fill, width, left, right);
    }

    // Método das áreas médias: V = (A1 + A2) / 2 × distância entre as estacas.
    public static List<SectionVolumeRow> AverageEndArea(IEnumerable<SectionStation> stations)
    {
        var rows = new List<SectionVolumeRow>();
        SectionStation? previous = null;
        double cumulativeCut = 0, cumulativeFill = 0;
        foreach (var s in stations.OrderBy(s => s.Station))
        {
            double cutVolume = 0, fillVolume = 0;
            var cut = s.Cut ?? 0;
            var fill = s.Fill ?? 0;
            if (previous is not null)
            {
                var distance = s.Station - previous.Station;
                cutVolume = ((previous.Cut ?? 0) + cut) / 2 * distance;
                fillVolume = ((previous.Fill ?? 0) + fill) / 2 * distance;
            }
            cumulativeCut += cutVolume;
            cumulativeFill += fillVolume;
            rows.Add(new SectionVolumeRow(s.Station, s.Name, cut, fill, cutVolume, fillVolume, cumulativeCut, cumulativeFill, s.Note));
            previous = s;
        }
        return rows;
    }

    // Volumes prontos do Civil 3D (lista de materiais), recortados no trecho pedido: a primeira estaca do
    // trecho não carrega o volume do intervalo anterior, e os acumulados recomeçam do zero.
    public static List<SectionVolumeRow> Clip(IEnumerable<(double Station, string Name, double CutArea, double FillArea,
        double CutVolume, double FillVolume)> results, double start, double end)
    {
        var rows = new List<SectionVolumeRow>();
        double cumulativeCut = 0, cumulativeFill = 0;
        foreach (var r in results.Where(r => r.Station >= start - 1e-6 && r.Station <= end + 1e-6).OrderBy(r => r.Station))
        {
            var first = rows.Count == 0;
            var cutVolume = first ? 0 : r.CutVolume;
            var fillVolume = first ? 0 : r.FillVolume;
            cumulativeCut += cutVolume;
            cumulativeFill += fillVolume;
            rows.Add(new SectionVolumeRow(r.Station, r.Name, r.CutArea, r.FillArea, cutVolume, fillVolume, cumulativeCut, cumulativeFill, null));
        }
        return rows;
    }

    // Intervalos bem maiores que o comum no grupo (ponte, linhas apagadas) e estacas repetidas.
    // "Comum" é o quartil de cima dos intervalos, para curvas a 10 m e tangentes a 20 m não virarem aviso.
    public static List<string> SpacingWarnings(IReadOnlyList<double> stations)
    {
        var warnings = new List<string>();
        var sorted = stations.OrderBy(s => s).ToList();
        if (sorted.Count < 2) return warnings;
        var gaps = new List<double>();
        for (var i = 1; i < sorted.Count; i++) gaps.Add(sorted[i] - sorted[i - 1]);
        var positive = gaps.Where(g => g > 1e-6).OrderBy(g => g).ToList();
        var common = positive.Count == 0 ? 0 : positive[Math.Max(0, (int)Math.Ceiling(0.75 * positive.Count) - 1)];
        for (var i = 0; i < gaps.Count; i++)
        {
            if (gaps[i] <= 1e-6)
                warnings.Add($"Duas linhas de amostragem na mesma estaca {Estacas.Text(sorted[i + 1])}; o volume entre elas é zero.");
            else if (positive.Count >= 3 && gaps[i] > 2 * common + 1e-6 && gaps[i] - common > 5)
                warnings.Add($"Intervalo de {Estacas.N(gaps[i])} m entre {Estacas.Text(sorted[i])} e {Estacas.Text(sorted[i + 1])} " +
                             $"(o comum no grupo é {Estacas.N(common)} m). O volume desse trecho sai só das duas seções das pontas.");
        }
        return warnings;
    }
}

// Estaca de 20 m, como nas notas de serviço: 245,6 → "12+5,600".
internal static class Estacas
{
    public static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static string Text(double? station)
    {
        if (station is not double s || !double.IsFinite(s)) return "—";
        var n = Math.Floor(s / 20 + 1e-9);
        var rest = Math.Max(0, s - n * 20);
        if (Math.Round(rest, 3) >= 20) { n += 1; rest = 0; } // 39,99999 → 2+0,000, e não 1+20,000
        return $"{n.ToString("0", Br)}+{rest.ToString("0.000", Br)}";
    }

    public static string N(double value, int decimals = 2) => value.ToString("N" + decimals, Br);
}
