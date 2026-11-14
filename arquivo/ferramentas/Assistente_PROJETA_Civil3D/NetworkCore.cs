using System.Globalization;

namespace Projeta.Civil3D;

// Aba Redes: cálculos sobre uma rede de tubulação lida do desenho. Este arquivo não usa a API do
// AutoCAD; a leitura fica em NetworkReader.cs e as alterações no desenho em NetworkOps.cs.

// Kind: "PV", "BL" (boca de lobo/caixa com grelha), "Saída" ou "Outro".
// KnownFlow em m³/s. Área de contribuição da estrutura (guia Áreas de contribuição do Civil 3D):
// CatchArea em m², CatchC de 0 a 1, CatchTc em minutos (0 = usa o tempo de entrada); LinkedCatchments =
// bacias (catchments) do Civil 3D ligadas à estrutura, que então definem esses valores.
internal sealed record NetStructure(string Handle, string Name, string Kind, string Description,
    double X, double Y, double Rim, double Sump, double KnownFlow, double? Station, double? Offset, string Alignment,
    double CatchArea = 0, double CatchC = 0, double CatchTc = 0, int LinkedCatchments = 0);

// Cotas de geratriz inferior interna (invert) nas duas pontas, terreno nas pontas quando há superfície.
// Flow: +1 do início para o fim, −1 do fim para o início, 0 nos dois sentidos (decide pela cota).
internal sealed record NetPipe(string Handle, string Name, string? StartStructure, string? EndStructure,
    double Length2D, double Length3D, double CenterLength, double InnerDiameter, double OuterDiameter,
    double StartInvert, double EndInvert, double? StartGround, double? EndGround,
    int Flow, string SizeName, string Material, double? Station, double? Offset, string Alignment);

internal sealed record NetworkData(string Handle, string Name, string Surface,
    IReadOnlyList<NetStructure> Structures, IReadOnlyList<NetPipe> Pipes);

// Tubo orientado no sentido do escoamento: de montante (Up) para jusante (Down).
internal sealed record Reach(NetPipe Pipe, string? Up, string? Down, double UpInvert, double DownInvert,
    double? UpGround, double? DownGround)
{
    public double Slope => (UpInvert - DownInvert) / Pipe.Length2D;
    public double Wall => Math.Max(0, (Pipe.OuterDiameter - Pipe.InnerDiameter) / 2);
}

internal sealed class NetworkGraph
{
    public required NetworkData Data { get; init; }
    public required IReadOnlyList<Reach> Reaches { get; init; }   // montante primeiro
    public required IReadOnlyDictionary<string, NetStructure> Structures { get; init; }
    public required IReadOnlyList<string> Problems { get; init; }

    public string Name(string? handle) => handle is not null && Structures.TryGetValue(handle, out var s) ? s.Name : "—";
    public IEnumerable<Reach> Into(string? handle) => handle is null ? Enumerable.Empty<Reach>() : Reaches.Where(r => r.Down == handle);
    public IEnumerable<Reach> OutOf(string? handle) => handle is null ? Enumerable.Empty<Reach>() : Reaches.Where(r => r.Up == handle);

    public static NetworkGraph Build(NetworkData data)
    {
        var problems = new List<string>();
        var structures = data.Structures.ToDictionary(s => s.Handle);
        var reaches = new List<Reach>();
        foreach (var p in data.Pipes)
        {
            if (!(p.Length2D > 0.001)) { problems.Add($"{p.Name}: comprimento nulo, fora do cálculo."); continue; }
            var forward = p.Flow > 0 || (p.Flow == 0 && p.StartInvert >= p.EndInvert);
            reaches.Add(forward
                ? new Reach(p, p.StartStructure, p.EndStructure, p.StartInvert, p.EndInvert, p.StartGround, p.EndGround)
                : new Reach(p, p.EndStructure, p.StartStructure, p.EndInvert, p.StartInvert, p.EndGround, p.StartGround));
            if (p.StartStructure is null || p.EndStructure is null) problems.Add($"{p.Name}: ponta sem estrutura ligada.");
        }
        // Ordem de montante para jusante: um trecho entra depois de todos os que chegam à sua estrutura de montante.
        var pending = reaches.ToDictionary(r => r, r => reaches.Count(o => o.Down is not null && o.Down == r.Up));
        var ready = new Queue<Reach>(reaches.Where(r => pending[r] == 0).OrderBy(r => r.Pipe.Name, StringComparer.CurrentCultureIgnoreCase));
        var ordered = new List<Reach>();
        while (ready.Count > 0)
        {
            var r = ready.Dequeue();
            ordered.Add(r);
            if (r.Down is null) continue;
            foreach (var next in reaches.Where(o => o.Up == r.Down).OrderBy(o => o.Pipe.Name, StringComparer.CurrentCultureIgnoreCase))
                if (--pending[next] == 0) ready.Enqueue(next);
        }
        if (ordered.Count < reaches.Count)
        {
            problems.Add("A rede tem circuito fechado (anel): o sentido do escoamento não define montante e jusante. Revise os sentidos dos tubos.");
            ordered.AddRange(reaches.Except(ordered).OrderBy(r => r.Pipe.Name, StringComparer.CurrentCultureIgnoreCase));
        }
        foreach (var group in reaches.Where(r => r.Up is not null).GroupBy(r => r.Up!).Where(g => g.Count() > 1))
            problems.Add($"{(structures.TryGetValue(group.Key, out var s) ? s.Name : group.Key)}: {group.Count()} tubos saem da mesma estrutura.");
        return new NetworkGraph { Data = data, Reaches = ordered, Structures = structures, Problems = problems };
    }
}

// Seção circular parcialmente cheia, fórmula de Manning (SI).
internal static class Hydraulics
{
    public const double FullRatio = 0.938; // y/D de vazão máxima

    public static (double Area, double Perimeter) Wet(double d, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        var theta = 2 * Math.Acos(1 - 2 * ratio);
        return (d * d / 8 * (theta - Math.Sin(theta)), d * theta / 2);
    }

    public static double Flow(double d, double ratio, double n, double slope)
    {
        var (a, p) = Wet(d, ratio);
        return a <= 0 || p <= 0 || slope <= 0 ? 0 : a / n * Math.Pow(a / p, 2.0 / 3) * Math.Sqrt(slope);
    }

    // y/D para a vazão q (m³/s); null se não cabe no tubo (sobrecarga) ou sem declividade.
    public static double? Ratio(double q, double d, double n, double slope)
    {
        if (q <= 0) return 0;
        if (slope <= 0 || d <= 0) return null;
        if (q > Flow(d, FullRatio, n, slope)) return null;
        double lo = 0, hi = FullRatio;
        for (var i = 0; i < 80; i++)
        {
            var mid = (lo + hi) / 2;
            if (Flow(d, mid, n, slope) < q) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2;
    }

    public static double HydraulicRadius(double d, double ratio)
    {
        var (a, p) = Wet(d, ratio);
        return p > 0 ? a / p : 0;
    }
}

// ------------------------------------------------------------------ esgoto (NBR 9649)

internal sealed record SewerOptions(double PopStart, double PopEnd, double PerCapita, double Return, double K1, double K2,
    double Infiltration, double MinFlow, double Manning, double MaxRatio, double MinTractive, double MinCover,
    double MinDiameter, double MaxVelocity, double DropLimit);

internal sealed record SewerRow(Reach Reach, string Up, string Down, double Length, double Diameter, double Slope,
    double QStart, double QEnd, double QStartUsed, double QEndUsed, double? RatioStart, double? RatioEnd,
    double? VelocityStart, double? VelocityEnd, double? CriticalVelocity, double? Tractive, double MinSlope,
    double? CoverUp, double? CoverDown, bool Ok, string Notes);

internal sealed record SewerResult(IReadOnlyList<SewerRow> Rows, double TotalLength, double RateStart, double RateEnd,
    double Infiltration, IReadOnlyList<string> Problems);

internal static class Sewer
{
    public const double Gravity = 9.81;
    public const double SpecificWeight = 10000; // N/m³, como na NBR 9649

    public static SewerResult Calculate(NetworkGraph g, SewerOptions o)
    {
        var values = new[] { o.PopStart, o.PopEnd, o.PerCapita, o.Return, o.K1, o.K2, o.Infiltration, o.MinFlow, o.Manning,
            o.MaxRatio, o.MinTractive, o.MinCover, o.MinDiameter, o.MaxVelocity, o.DropLimit };
        if (values.Any(v => !double.IsFinite(v) || v < 0) || o.Manning <= 0 || o.MaxRatio <= 0 || o.MaxRatio > 0.9)
            throw new ArgumentException("Confira os parâmetros de esgoto: valores não negativos, n maior que zero e y/D até 0,90.");
        if (g.Reaches.Count == 0) throw new ArgumentException("A rede não tem tubos para calcular.");
        var total = g.Reaches.Sum(r => r.Pipe.Length2D);
        // Taxas de contribuição linear (L/s por metro).
        var rateStart = o.Return * o.K2 * o.PopStart * o.PerCapita / 86400 / total;
        var rateEnd = o.Return * o.K1 * o.K2 * o.PopEnd * o.PerCapita / 86400 / total;
        var infiltration = o.Infiltration / 1000;
        var downStart = new Dictionary<Reach, double>();
        var downEnd = new Dictionary<Reach, double>();
        var rows = new List<SewerRow>();
        var levels = new Dictionary<Reach, (double? Up, double? Down)>();
        foreach (var r in g.Reaches)
        {
            var point = r.Up is not null && g.Structures.TryGetValue(r.Up, out var s) ? s.KnownFlow * 1000 : 0; // m³/s → L/s
            var upStart = g.Into(r.Up).Sum(i => downStart.GetValueOrDefault(i)) + point;
            var upEnd = g.Into(r.Up).Sum(i => downEnd.GetValueOrDefault(i)) + point;
            var qStart = upStart + (rateStart + infiltration) * r.Pipe.Length2D;
            var qEnd = upEnd + (rateEnd + infiltration) * r.Pipe.Length2D;
            downStart[r] = qStart;
            downEnd[r] = qEnd;
            var usedStart = Math.Max(qStart, o.MinFlow);
            var usedEnd = Math.Max(qEnd, o.MinFlow);
            var d = r.Pipe.InnerDiameter;
            var slope = r.Slope;
            var notes = new List<string>();
            double? ratioEnd = null, ratioStart = null, vEnd = null, vStart = null, vc = null, tractive = null;
            if (slope <= 0) notes.Add("declividade nula ou contra o escoamento");
            else
            {
                ratioEnd = Hydraulics.Ratio(usedEnd / 1000, d, o.Manning, slope);
                ratioStart = Hydraulics.Ratio(usedStart / 1000, d, o.Manning, slope);
                if (ratioEnd is double re)
                {
                    vEnd = usedEnd / 1000 / Hydraulics.Wet(d, re).Area;
                    vc = 6 * Math.Sqrt(Gravity * Hydraulics.HydraulicRadius(d, re));
                    var limit = vEnd > vc ? Math.Min(o.MaxRatio, 0.5) : o.MaxRatio;
                    if (re > limit + 1e-9)
                        notes.Add(vEnd > vc ? $"y/D {N(re)} acima de 0,50 (velocidade acima da crítica)" : $"y/D {N(re)} acima de {N(o.MaxRatio)}");
                    if (o.MaxVelocity > 0 && vEnd > o.MaxVelocity) notes.Add($"velocidade {N(vEnd.Value)} m/s acima de {N(o.MaxVelocity)}");
                }
                else notes.Add("vazão final maior que a capacidade do tubo");
                if (ratioStart is double rs)
                {
                    vStart = usedStart / 1000 / Hydraulics.Wet(d, rs).Area;
                    tractive = SpecificWeight * Hydraulics.HydraulicRadius(d, rs) * slope;
                    if (tractive < o.MinTractive - 1e-9) notes.Add($"tensão trativa {N(tractive.Value)} Pa abaixo de {N(o.MinTractive)}");
                }
            }
            var minSlope = 0.0055 * Math.Pow(usedStart, -0.47);
            if (d < o.MinDiameter - 1e-6) notes.Add($"diâmetro {N(d * 1000, 0)} mm abaixo do mínimo");
            double? Cover(double? ground, double invert) => ground is double z ? z - (invert + r.Pipe.InnerDiameter + r.Wall) : null;
            var coverUp = Cover(r.UpGround, r.UpInvert);
            var coverDown = Cover(r.DownGround, r.DownInvert);
            var minCover = new[] { coverUp, coverDown }.Where(c => c.HasValue).Select(c => c!.Value).DefaultIfEmpty(double.NaN).Min();
            if (o.MinCover > 0 && double.IsFinite(minCover) && minCover < o.MinCover - 1e-6) notes.Add($"recobrimento {N(minCover)} m abaixo de {N(o.MinCover)}");
            // Degrau na estrutura de montante: tubo de queda a partir do limite.
            foreach (var i in g.Into(r.Up))
            {
                var drop = i.DownInvert - r.UpInvert;
                if (o.DropLimit > 0 && drop >= o.DropLimit - 1e-6) notes.Add($"degrau de {N(drop)} m em {g.Name(r.Up)}: tubo de queda");
                if (drop < -0.001) notes.Add($"saída de {g.Name(r.Up)} acima da chegada de {i.Pipe.Name}");
            }
            levels[r] = (ratioEnd is double a ? r.UpInvert + a * d : null, ratioEnd is double b ? r.DownInvert + b * d : null);
            // NBR 9649: nível d'água de saída acima de alguma chegada pede verificar o remanso a montante.
            foreach (var i in g.Into(r.Up))
                if (levels.TryGetValue(i, out var li) && li.Down is double naIn && levels[r].Up is double naOut && naOut > naIn + 0.001)
                    notes.Add($"lâmina de saída {N(naOut - naIn, 3)} m acima da chegada de {i.Pipe.Name} em {g.Name(r.Up)}: verificar remanso");
            rows.Add(new SewerRow(r, g.Name(r.Up), g.Name(r.Down), r.Pipe.Length2D, d, slope, qStart, qEnd, usedStart, usedEnd,
                ratioStart, ratioEnd, vStart, vEnd, vc, tractive, minSlope, coverUp, coverDown, notes.Count == 0, string.Join("; ", notes.Distinct())));
        }
        return new SewerResult(rows, total, rateStart, rateEnd, infiltration, g.Problems);
    }

    public static string N(double value, int decimals = 2) => value.ToString("N" + decimals, CultureInfo.GetCultureInfo("pt-BR"));
}

// ------------------------------------------------------------------ drenagem pluvial (método racional)

// Chuva intensa: i = K·TR^a / (t + b)^c, com i em mm/h e t em minutos. InletTime: tempo de entrada
// (tc inicial) das estruturas sem tc próprio. MinDiameter em metros.
internal sealed record StormOptions(double ReturnPeriod, double InletTime, double K, double A, double B, double C,
    double Manning, double MaxRatio, double MinVelocity, double MaxVelocity, double MinCover, double MinDiameter, string Source)
{
    public double Intensity(double minutes) => K * Math.Pow(ReturnPeriod, A) / Math.Pow(minutes + B, C);
}

// Áreas em ha, vazões em L/s, tempos em minutos. Own*: a contribuição da estrutura de montante do trecho.
// Area e RunoffArea (C·A) acumulam tudo o que chega; Tc é o tempo de concentração no início do trecho.
internal sealed record StormRow(Reach Reach, string Up, string Down, double Length, double Diameter, double Slope,
    double OwnArea, double OwnC, double OwnTc, double Area, double RunoffArea, double Tc, double Intensity, double PointFlow,
    double Flow, double Capacity, double? Ratio, double Velocity, double TravelTime, double? CoverUp, double? CoverDown,
    double? Suggested, bool Ok, string Notes)
{
    public double MeanC => Area > 0 ? RunoffArea / Area : 0;
}

internal sealed record StormResult(IReadOnlyList<StormRow> Rows, double TotalArea, double OutletFlow, double LostArea,
    IReadOnlyList<string> Problems);

internal static class Storm
{
    // Diâmetros nominais de tubo de concreto para águas pluviais (NBR 8890), em metros.
    public static readonly double[] Commercial = { 0.30, 0.40, 0.50, 0.60, 0.70, 0.80, 0.90, 1.00, 1.10, 1.20, 1.50, 1.75, 2.00 };

    public static StormResult Calculate(NetworkGraph g, StormOptions o)
    {
        var values = new[] { o.ReturnPeriod, o.InletTime, o.K, o.A, o.B, o.C, o.Manning, o.MaxRatio, o.MinVelocity, o.MaxVelocity, o.MinCover, o.MinDiameter };
        if (values.Any(v => !double.IsFinite(v) || v < 0)) throw new ArgumentException("Confira os parâmetros de drenagem: nenhum valor pode ser negativo.");
        if (o.K <= 0 || o.C <= 0) throw new ArgumentException("Informe a equação de chuva (IDF): K e c maiores que zero.");
        if (o.ReturnPeriod <= 0 || o.InletTime <= 0) throw new ArgumentException("O período de retorno e o tempo de entrada devem ser maiores que zero.");
        if (o.Manning <= 0 || o.MaxRatio <= 0 || o.MaxRatio > Hydraulics.FullRatio)
            throw new ArgumentException($"Use n maior que zero e y/D máximo até {N(Hydraulics.FullRatio, 3)}.");
        if (g.Reaches.Count == 0) throw new ArgumentException("A rede não tem tubos para calcular.");
        var outArea = new Dictionary<Reach, double>();
        var outRunoff = new Dictionary<Reach, double>();
        var outPoint = new Dictionary<Reach, double>();
        var outTime = new Dictionary<Reach, double>();
        var rows = new List<StormRow>();
        foreach (var r in g.Reaches)
        {
            NetStructure? s = r.Up is not null && g.Structures.TryGetValue(r.Up, out var found) ? found : null;
            var ownArea = s is null ? 0 : Math.Max(0, s.CatchArea) / 10000;              // m² → ha
            var ownC = s is null ? 0 : Math.Clamp(s.CatchC, 0, 1);
            var ownTc = s is not null && s.CatchTc > 0 ? s.CatchTc : o.InletTime;
            var incoming = g.Into(r.Up).Where(outArea.ContainsKey).ToList();
            var area = ownArea + incoming.Sum(i => outArea[i]);
            var runoff = ownArea * ownC + incoming.Sum(i => outRunoff[i]);
            var point = (s is null ? 0 : Math.Max(0, s.KnownFlow) * 1000) + incoming.Sum(i => outPoint[i]);
            // tc: o maior tempo entre a própria área e o que chega com área (tc de montante + percurso).
            var tc = ownArea > 0 && ownC > 0 ? ownTc : 0;
            foreach (var i in incoming.Where(i => outRunoff[i] > 0)) tc = Math.Max(tc, outTime[i]);
            if (tc <= 0) tc = o.InletTime;
            var intensity = o.Intensity(tc);
            var flow = runoff * intensity / 360 * 1000 + point;                           // Q = C·i·A/360 (m³/s) → L/s
            var d = r.Pipe.InnerDiameter;
            var slope = r.Slope;
            var notes = new List<string>();
            double capacity = 0, velocity = 0;
            double? ratio = null;
            if (slope <= 0) notes.Add("declividade nula ou contra o escoamento");
            else
            {
                capacity = Hydraulics.Flow(d, o.MaxRatio, o.Manning, slope) * 1000;
                ratio = Hydraulics.Ratio(flow / 1000, d, o.Manning, slope);
                if (ratio is double y)
                {
                    velocity = y > 0 ? flow / 1000 / Hydraulics.Wet(d, y).Area : 0;
                    if (y > o.MaxRatio + 1e-9) notes.Add($"y/D {N(y)} acima de {N(o.MaxRatio)}");
                    if (flow > 0 && o.MinVelocity > 0 && velocity < o.MinVelocity - 1e-9) notes.Add($"velocidade {N(velocity)} m/s abaixo de {N(o.MinVelocity)}");
                    if (o.MaxVelocity > 0 && velocity > o.MaxVelocity + 1e-9) notes.Add($"velocidade {N(velocity)} m/s acima de {N(o.MaxVelocity)}");
                }
                else
                {
                    // Sobrecarga: velocidade de seção plena (Manning) só para o tempo de percurso; o tubo precisa ser revisto.
                    velocity = Hydraulics.Flow(d, 1, o.Manning, slope) / (Math.PI * d * d / 4);
                    notes.Add($"vazão de {N(flow, 0)} L/s maior que a capacidade do tubo ({N(Hydraulics.Flow(d, Hydraulics.FullRatio, o.Manning, slope) * 1000, 0)} L/s)");
                }
            }
            var travel = velocity > 0 ? r.Pipe.Length2D / velocity / 60 : 0;
            if (d < o.MinDiameter - 1e-6) notes.Add($"diâmetro {N(d * 1000, 0)} mm abaixo do mínimo");
            double? Cover(double? ground, double invert) => ground is double z ? z - (invert + d + r.Wall) : null;
            var coverUp = Cover(r.UpGround, r.UpInvert);
            var coverDown = Cover(r.DownGround, r.DownInvert);
            var minCover = new[] { coverUp, coverDown }.Where(c => c.HasValue).Select(c => c!.Value).DefaultIfEmpty(double.NaN).Min();
            if (o.MinCover > 0 && double.IsFinite(minCover) && minCover < o.MinCover - 1e-6) notes.Add($"recobrimento {N(minCover)} m abaixo de {N(o.MinCover)}");
            foreach (var i in g.Into(r.Up))
            {
                if (i.Pipe.InnerDiameter > d + 1e-6) notes.Add($"diâmetro menor que o de {i.Pipe.Name}, que chega a montante");
                if (r.UpInvert > i.DownInvert + 0.001) notes.Add($"saída de {g.Name(r.Up)} acima da chegada de {i.Pipe.Name}");
            }
            // Menor DN comercial que leva a vazão com a lâmina máxima, sem diminuir em relação aos que chegam.
            double? suggested = null;
            if (slope > 0)
            {
                var floor = Math.Max(o.MinDiameter, g.Into(r.Up).Select(i => i.Pipe.InnerDiameter).DefaultIfEmpty(0).Max());
                foreach (var dn in Commercial.Where(x => x >= floor - 0.005))
                    if (Hydraulics.Ratio(flow / 1000, dn, o.Manning, slope) is double y && y <= o.MaxRatio + 1e-9) { suggested = dn; break; }
                if (suggested is null) notes.Add($"nenhum DN até {N(Commercial[^1] * 1000, 0)} mm leva a vazão nesta declividade");
            }
            outArea[r] = area;
            outRunoff[r] = runoff;
            outPoint[r] = point;
            outTime[r] = tc + travel;
            rows.Add(new StormRow(r, g.Name(r.Up), g.Name(r.Down), r.Pipe.Length2D, d, slope, ownArea, ownC, ownTc, area, runoff, tc,
                intensity, point, flow, capacity, ratio, velocity, travel, coverUp, coverDown, suggested, notes.Count == 0, string.Join("; ", notes.Distinct())));
        }
        var problems = g.Problems.ToList();
        var total = g.Structures.Values.Sum(s => Math.Max(0, s.CatchArea)) / 10000;
        // Área em estrutura sem tubo de saída (ponto de lançamento) não entra em nenhum trecho.
        var lost = g.Structures.Values.Where(s => s.CatchArea > 0 && !g.OutOf(s.Handle).Any()).Sum(s => s.CatchArea) / 10000;
        if (lost > 0) problems.Add($"{N(lost, 4)} ha estão em estruturas sem tubo de saída e não entram no cálculo.");
        if (total > 200) problems.Add($"Área total de {N(total)} ha: o método racional costuma ser limitado a bacias pequenas; confira o manual adotado.");
        var ups = g.Reaches.Where(r => r.Up is not null).Select(r => r.Up!).ToHashSet();
        var outlet = rows.Where(x => x.Reach.Down is null || !ups.Contains(x.Reach.Down)).Sum(x => x.Flow);
        return new StormResult(rows, total, outlet, lost, problems);
    }

    public static string N(double value, int decimals = 2) => value.ToString("N" + decimals, CultureInfo.GetCultureInfo("pt-BR"));
}

// ------------------------------------------------------------------ escavação e quantitativos

internal sealed record TrenchOptions(double SideClearance, double MinWidth, double Bedding, double SideSlope, double ShoringDepth);

internal sealed record TrenchRow(Reach Reach, string Up, string Down, double Length, double OuterDiameter, double Width,
    double? DepthUp, double? DepthDown, double? DepthMean, double Excavation, double BeddingVolume, double PipeVolume,
    double Backfill, double ShoringArea, string Band, string Notes);

internal sealed record QuantityRow(string Group, string Item, string Detail, int Count, double Length, double Extra);

internal static class Trench
{
    public static readonly (double Limit, string Name)[] Bands =
    {
        (1.5, "até 1,50 m"), (3.0, "1,50 a 3,00 m"), (4.5, "3,00 a 4,50 m"), (6.0, "4,50 a 6,00 m"), (double.PositiveInfinity, "acima de 6,00 m")
    };

    public static string Band(double depth) => Bands.First(b => depth <= b.Limit + 1e-9).Name;

    public static IReadOnlyList<TrenchRow> Calculate(NetworkGraph g, TrenchOptions o)
    {
        if (new[] { o.SideClearance, o.MinWidth, o.Bedding, o.SideSlope, o.ShoringDepth }.Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("Os parâmetros da vala não podem ser negativos.");
        var rows = new List<TrenchRow>();
        foreach (var r in g.Reaches)
        {
            var p = r.Pipe;
            var length = p.CenterLength > 0 ? p.CenterLength : p.Length2D;
            var width = Math.Max(o.MinWidth, p.OuterDiameter + 2 * o.SideClearance);
            double? Depth(double? ground, double invert) => ground is double z ? z - (invert - r.Wall) + o.Bedding : null;
            var h1 = Depth(r.UpGround, r.UpInvert);
            var h2 = Depth(r.DownGround, r.DownInvert);
            var pipeVolume = Math.PI * p.OuterDiameter * p.OuterDiameter / 4 * length;
            var bedding = width * o.Bedding * length;
            if (h1 is not double a || h2 is not double b)
            {
                rows.Add(new TrenchRow(r, g.Name(r.Up), g.Name(r.Down), length, p.OuterDiameter, width, h1, h2, null,
                    0, bedding, pipeVolume, 0, 0, "—", "sem cota de terreno nas pontas"));
                continue;
            }
            var notes = new List<string>();
            if (a < 0 || b < 0) notes.Add("tubo acima do terreno em uma das pontas");
            a = Math.Max(0, a);
            b = Math.Max(0, b);
            // Profundidade varia linearmente: integral exata da seção trapezoidal B·h + m·h².
            var excavation = length * (width * (a + b) / 2 + o.SideSlope * (a * a + a * b + b * b) / 3);
            var shoring = Shoring(a, b, length, o.ShoringDepth);
            var backfill = Math.Max(0, excavation - pipeVolume - bedding);
            var mean = (a + b) / 2;
            rows.Add(new TrenchRow(r, g.Name(r.Up), g.Name(r.Down), length, p.OuterDiameter, width, a, b, mean,
                excavation, bedding, pipeVolume, backfill, shoring, Band(mean), string.Join("; ", notes)));
        }
        return rows;
    }

    // Área das duas paredes onde a profundidade passa do limite (escoramento).
    private static double Shoring(double a, double b, double length, double limit)
    {
        if (limit <= 0) return length * (a + b);
        var hi = Math.Max(a, b);
        var lo = Math.Min(a, b);
        if (hi <= limit) return 0;
        if (lo >= limit) return length * (a + b);
        var part = length * (hi - limit) / (hi - lo);
        return part * (hi + limit);
    }

    public static IReadOnlyList<QuantityRow> Quantities(NetworkGraph g)
    {
        var rows = new List<QuantityRow>();
        foreach (var pipes in g.Reaches.GroupBy(r => (r.Pipe.SizeName, r.Pipe.Material, Diameter: Math.Round(r.Pipe.InnerDiameter * 1000)))
                     .OrderBy(x => x.Key.Diameter).ThenBy(x => x.Key.SizeName, StringComparer.CurrentCultureIgnoreCase))
            rows.Add(new QuantityRow("Tubos", pipes.Key.SizeName, $"DN {pipes.Key.Diameter:0} mm{(pipes.Key.Material.Length > 0 ? " · " + pipes.Key.Material : "")}",
                pipes.Count(), pipes.Sum(r => r.Pipe.Length3D), pipes.Sum(r => r.Pipe.CenterLength)));
        foreach (var s in g.Structures.Values.GroupBy(s => (s.Kind, s.Description, Band: DepthStep(s.Rim - s.Sump)))
                     .OrderBy(x => x.Key.Kind).ThenBy(x => x.Key.Description, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Key.Band.Order))
            rows.Add(new QuantityRow("Estruturas", $"{s.Key.Kind} · {s.Key.Description}", s.Key.Band.Name, s.Count(), 0,
                s.Sum(x => x.Rim - x.Sump)));
        return rows;
    }

    // Faixas de 0,50 m de altura (tampa ao fundo), como nas composições de PV.
    public static (int Order, string Name) DepthStep(double height)
    {
        if (!double.IsFinite(height) || height <= 0) return (0, "altura não informada");
        var top = Math.Ceiling(height / 0.5 - 1e-9) * 0.5;
        return ((int)Math.Round(top * 2), $"{Sewer.N(Math.Max(0, top - 0.5))} a {Sewer.N(top)} m");
    }
}

// ------------------------------------------------------------------ renomear na ordem do escoamento

internal sealed record RenameOptions(string PvPrefix, string InletPrefix, string PipePrefix, int Start, int Digits);
internal sealed record RenameRow(string Handle, bool IsPipe, string OldName, string NewName);

internal static class Renamer
{
    // Coletor principal primeiro (o caminho mais longo até o ponto de saída), numerado de montante
    // para jusante; depois cada ramal, do mais a montante para o mais a jusante, do mesmo jeito.
    // Bocas de lobo têm numeração própria; cada tubo recebe o número da ordem da sua estrutura de montante.
    public static IReadOnlyList<RenameRow> Plan(NetworkGraph g, RenameOptions o)
    {
        if (o.Start < 0 || o.Digits < 1 || o.Digits > 6) throw new ArgumentException("Início não negativo e de 1 a 6 dígitos.");
        if (new[] { o.PvPrefix, o.InletPrefix, o.PipePrefix }.Any(p => p.IndexOfAny(new[] { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', '=', '`' }) >= 0))
            throw new ArgumentException("Os prefixos não podem ter os caracteres < > / \\ \" : ; ? * | = `.");
        var longest = new Dictionary<string, double>();
        foreach (var r in g.Reaches)
            if (r.Down is not null)
            {
                var up = (r.Up is not null ? longest.GetValueOrDefault(r.Up) : 0) + r.Pipe.Length2D;
                longest[r.Down] = Math.Max(longest.GetValueOrDefault(r.Down), up);
            }
        var order = new List<string>();
        var seen = new HashSet<string>();
        void Number(string root, bool includeRoot)
        {
            var path = new List<string> { root };
            var current = root;
            while (true)
            {
                var next = g.Into(current).Where(r => r.Up is not null && !seen.Contains(r.Up) && !path.Contains(r.Up))
                    .OrderByDescending(r => r.Pipe.Length2D + longest.GetValueOrDefault(r.Up!)).ThenBy(r => r.Pipe.Name).FirstOrDefault();
                if (next is null) break;
                path.Add(next.Up!);
                current = next.Up!;
            }
            path.Reverse(); // de montante para jusante
            if (!includeRoot) path.RemoveAt(path.Count - 1);
            foreach (var s in path) if (seen.Add(s)) order.Add(s);
            foreach (var s in path.Append(root).Distinct())
                foreach (var branch in g.Into(s).Where(r => r.Up is not null && !seen.Contains(r.Up)).OrderBy(r => r.Pipe.Name).ToList())
                    if (!seen.Contains(branch.Up!)) Number(branch.Up!, true);
        }
        var outlets = g.Structures.Values.Where(s => g.OutOf(s.Handle).All(r => r.Down is null) && g.Into(s.Handle).Any())
            .OrderByDescending(s => longest.GetValueOrDefault(s.Handle)).ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        foreach (var outlet in outlets)
            if (!seen.Contains(outlet.Handle)) Number(outlet.Handle, true);
        foreach (var s in g.Structures.Values.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase))
            if (seen.Add(s.Handle)) order.Add(s.Handle);

        string Label(string prefix, int n) => prefix + n.ToString(new string('0', o.Digits), CultureInfo.InvariantCulture);
        var rows = new List<RenameRow>();
        int pv = o.Start, bl = o.Start, tube = o.Start;
        var position = new Dictionary<string, int>();
        foreach (var handle in order)
        {
            var s = g.Structures[handle];
            position[handle] = position.Count;
            var name = s.Kind == "BL" ? Label(o.InletPrefix, bl++) : Label(o.PvPrefix, pv++);
            rows.Add(new RenameRow(handle, false, s.Name, name));
        }
        foreach (var r in g.Reaches.OrderBy(r => r.Up is not null ? position.GetValueOrDefault(r.Up, int.MaxValue) : int.MaxValue)
                     .ThenBy(r => r.Down is not null ? position.GetValueOrDefault(r.Down, int.MaxValue) : int.MaxValue))
            rows.Add(new RenameRow(r.Pipe.Handle, true, r.Pipe.Name, Label(o.PipePrefix, tube++)));
        return rows;
    }
}
