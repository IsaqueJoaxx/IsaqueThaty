namespace Projeta.Civil3D;

internal sealed record GradePoint(double Station, double Elevation);
internal sealed record GradeOptions(double MaxPercent, double MinTangent, double Tolerance);
internal sealed record GradePreview(IReadOnlyList<GradePoint> Pvis, IReadOnlyList<string> Warnings)
{
    public string SourceSignature { get; init; } = "";
}

// Primeira integração do gerador de tangentes de GreideAutoB.dyn.
// Nenhum critério de curva vertical é presumido. Não gera um perfil aprovado.
internal static class GradeSolver
{
    public static GradePreview Solve(IReadOnlyList<GradePoint> pts, GradeOptions options)
    {
        if (!double.IsFinite(options.MaxPercent) || options.MaxPercent <= 0 || options.MaxPercent > 7)
            throw new ArgumentException("A rampa máxima precisa estar acima de zero e no máximo em 7%.");
        if (!double.IsFinite(options.MinTangent) || options.MinTangent <= 0 ||
            !double.IsFinite(options.Tolerance) || options.Tolerance < 0)
            throw new ArgumentException("Informe distância mínima positiva e tolerância não negativa.");
        if (pts.Count < 3 || pts.Count > 600) throw new ArgumentException("São necessárias de 3 a 600 amostras.");
        for (var i = 0; i < pts.Count; i++)
            if (!double.IsFinite(pts[i].Station) || !double.IsFinite(pts[i].Elevation) ||
                (i > 0 && pts[i].Station <= pts[i - 1].Station))
                throw new ArgumentException("As amostras precisam ser finitas e ter progressivas crescentes.");
        var pvis = new List<GradePoint> { pts[0] };
        var warnings = new List<string>();
        var imax = options.MaxPercent / 100;
        var i0 = 0;
        while (i0 < pts.Count - 1)
        {
            var origin = pvis[^1];
            double Fit(int end)
            {
                double num = 0, den = 0;
                for (var k = i0 + 1; k <= end; k++)
                {
                    var ds = pts[k].Station - origin.Station;
                    num += ds * (pts[k].Elevation - origin.Elevation);
                    den += ds * ds;
                }
                return Math.Clamp(den > 0 ? num / den : 0, -imax, imax);
            }
            double Deviation(int end, double slope)
            {
                double result = 0;
                for (var k = i0; k <= end; k++)
                    result = Math.Max(result, Math.Abs(origin.Elevation + slope * (pts[k].Station - origin.Station) - pts[k].Elevation));
                return result;
            }
            var best = -1;
            var first = -1;
            double gbest = 0;
            for (var j = i0 + 1; j < pts.Count; j++)
            {
                if (pts[j].Station - origin.Station < options.MinTangent - 1e-6) continue;
                if (first < 0) first = j;
                var g = Fit(j);
                if (Deviation(j, g) <= options.Tolerance) { best = j; gbest = g; }
                else if (best >= 0) break;
            }
            if (best < 0)
            {
                best = first >= 0 ? first : pts.Count - 1;
                gbest = Fit(best);
            }
            if (pts[^1].Station - pts[best].Station < options.MinTangent - 1e-6)
            {
                best = pts.Count - 1;
                gbest = Fit(best);
            }
            var length = pts[best].Station - origin.Station;
            var deviation = Deviation(best, gbest);
            if (deviation > options.Tolerance + 1e-8)
                warnings.Add($"Progressiva {origin.Station:F3}: desvio nas amostras {deviation:F3} m excede a tolerância.");
            if (length < options.MinTangent - 1e-6)
                warnings.Add($"Progressiva {origin.Station:F3}: distância entre PIVs abaixo da mínima informada.");
            pvis.Add(new GradePoint(pts[best].Station, origin.Elevation + gbest * length));
            i0 = best;
        }
        var before = pvis.Count;
        pvis = Simplify(pvis, pts, options.Tolerance, imax);
        if (pvis.Count < before)
            warnings.Add($"Simplificação: {before - pvis.Count} PIV(s) redundante(s) retirado(s). {pvis.Count} mantido(s).");
        for (var i = 1; i < pvis.Count; i++)
        {
            var percent = 100 * Math.Abs((pvis[i].Elevation - pvis[i - 1].Elevation) / (pvis[i].Station - pvis[i - 1].Station));
            if (percent > options.MaxPercent + 1e-7) throw new InvalidOperationException("A prévia excedeu a rampa máxima.");
        }
        warnings.Add("Curvas verticais, rampa mínima, visibilidade, drenagem e critérios DER-MG ainda não validados. Prévia de tangentes; não usar como greide final.");
        return new GradePreview(pvis, warnings);
    }

    // Compare every candidate to the original proposed grade, so errors cannot accumulate
    // after consecutive removals. Ground deviations may not exceed the original local
    // worst deviation (or the user's tolerance if that is larger).
    private static List<GradePoint> Simplify(List<GradePoint> proposed, IReadOnlyList<GradePoint> ground, double tolerance, double maxSlope)
    {
        var original = proposed.ToArray();
        var result = proposed.ToList();
        double OriginalElevation(double station)
        {
            for (var j = 1; j < original.Length; j++)
                if (station <= original[j].Station)
                {
                    var a = original[j - 1]; var b = original[j];
                    return a.Elevation + (b.Elevation - a.Elevation) * (station - a.Station) / (b.Station - a.Station);
                }
            return original[^1].Elevation;
        }
        bool changed;
        do
        {
            changed = false;
            for (var i = 1; i < result.Count - 1;)
            {
                var a = result[i - 1]; var b = result[i + 1];
                var slope = (b.Elevation - a.Elevation) / (b.Station - a.Station);
                bool removable = Math.Abs(slope) <= maxSlope + 1e-10;
                foreach (var p in original)
                    if (removable && p.Station >= a.Station && p.Station <= b.Station &&
                        Math.Abs(a.Elevation + slope * (p.Station - a.Station) - p.Elevation) > tolerance + 1e-9)
                        removable = false;
                if (removable)
                {
                    double previousWorst = 0, newWorst = 0;
                    foreach (var p in ground)
                        if (p.Station >= a.Station && p.Station <= b.Station)
                        {
                            previousWorst = Math.Max(previousWorst, Math.Abs(OriginalElevation(p.Station) - p.Elevation));
                            newWorst = Math.Max(newWorst, Math.Abs(a.Elevation + slope * (p.Station - a.Station) - p.Elevation));
                        }
                    removable = newWorst <= Math.Max(tolerance, previousWorst) + 1e-9;
                }
                if (removable) { result.RemoveAt(i); changed = true; }
                else i++;
            }
        } while (changed);
        return result;
    }
}
