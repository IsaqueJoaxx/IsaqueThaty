using System.IO.Compression;
using System.Xml.Linq;
using Projeta.Civil3D;

// Testes do núcleo da aba Redes (sem Civil 3D).
var failures = new List<string>();
void Check(bool ok, string what) { if (!ok) failures.Add(what); }
void Near(double a, double b, double tol, string what) => Check(Math.Abs(a - b) <= tol, $"{what}: {a} esperado {b}");

NetStructure S(string h, string name, double rim, double sump, string kind = "PV", double known = 0) =>
    new(h, name, kind, "PV concreto", 0, 0, rim, sump, known, null, null, "");
NetPipe P(string h, string name, string? a, string? b, double len, double d, double invA, double invB, double? gA, double? gB, int flow = 1, double outer = -1) =>
    new(h, name, a, b, len, len, len, d, outer > 0 ? outer : d + 0.02, invA, invB, gA, gB, flow, $"DN {d * 1000:0}", "PVC", null, null, "");

// 1. Manning a seção plena e inversão da lâmina.
{
    double d = 0.3, n = 0.013, s = 0.005;
    var full = 1 / n * Math.PI * d * d / 4 * Math.Pow(d / 4, 2.0 / 3) * Math.Sqrt(s);
    Near(Hydraulics.Flow(d, 1, n, s), full, 1e-9, "Manning seção plena");
    foreach (var ratio in new[] { 0.05, 0.2, 0.5, 0.75, 0.9 })
    {
        var q = Hydraulics.Flow(d, ratio, n, s);
        Near(Hydraulics.Ratio(q, d, n, s) ?? -1, ratio, 1e-6, $"lâmina invertida {ratio}");
    }
    Check(Hydraulics.Ratio(full * 1.2, d, n, s) is null, "sobrecarga deveria dar null");
}

// 2. NBR 9649: a declividade mínima 0,0055·Qi^-0,47 dá tensão trativa perto de 1 Pa (n = 0,013).
foreach (var q in new[] { 1.5, 3.0, 6.0 })
{
    var imin = 0.0055 * Math.Pow(q, -0.47);
    var ratio = Hydraulics.Ratio(q / 1000, 0.15, 0.013, imin)!.Value;
    var sigma = 10000 * Hydraulics.HydraulicRadius(0.15, ratio) * imin;
    Check(sigma > 0.9 && sigma < 1.15, $"tensão na declividade mínima para {q} L/s: {sigma:F3} Pa");
}

// 3. Coletor em série: vazões, taxa linear, ordem.
var serie = new NetworkData("N1", "Serie", "TN",
    new[] { S("s1", "A", 101, 99.3), S("s2", "B", 100.5, 98.3), S("s3", "C", 100, 97.3), S("s4", "D", 99.5, 96.3, "Saída") },
    new[]
    {
        P("p3", "T3", "s3", "s4", 100, 0.15, 97.3, 96.3, 100, 99.5),
        P("p1", "T1", "s1", "s2", 100, 0.15, 99.3, 98.3, 101, 100.5),
        P("p2", "T2", "s3", "s2", 100, 0.15, 97.3, 98.3, 100, 100.5, flow: -1), // desenhado ao contrário
    });
var g = NetworkGraph.Build(serie);
Check(string.Join(",", g.Reaches.Select(r => r.Pipe.Name)) == "T1,T2,T3", "ordem de montante para jusante: " + string.Join(",", g.Reaches.Select(r => r.Pipe.Name)));
Check(g.Problems.Count == 0, "série sem problemas: " + string.Join(" | ", g.Problems));
var opt = new SewerOptions(300, 600, 150, 0.8, 1.2, 1.5, 0.1, 1.5, 0.013, 0.75, 1.0, 0.9, 0.15, 5, 0.5);
var sewer = Sewer.Calculate(g, opt);
Near(sewer.Rows[^1].QEnd, 1.5 + 0.03, 1e-9, "Qf no fim do coletor");
Near(sewer.Rows[^1].QStart, 0.8 * 1.5 * 300 * 150 / 86400 + 0.03, 1e-9, "Qi no fim do coletor");
Near(sewer.Rows[0].QEnd, (1.5 + 0.03) / 3, 1e-9, "Qf no primeiro trecho");
Near(sewer.Rows[0].Slope, 0.01, 1e-12, "declividade");
Check(sewer.Rows.All(r => r.Ok), "série atende: " + string.Join(" | ", sewer.Rows.Select(r => r.Notes)));
// Recobrimento: terreno 101, GI 99,3, DN 0,15 + parede 0,01 → 101 − (99,3 + 0,15 + 0,01) = 1,54.
Near(sewer.Rows[0].CoverUp ?? -1, 1.54, 1e-9, "recobrimento a montante");

// 4. Ramais, vazão pontual, degrau e tensão baixa.
var arvore = new NetworkData("N2", "Arvore", "TN",
    new[]
    {
        S("a1", "A1", 105, 103), S("a2", "A2", 104, 102), S("b1", "B1", 104.5, 102.5, known: 0.002),
        S("j", "J", 103, 101), S("o", "O", 102, 100, "Saída"), S("x", "X", 103.5, 102.8, "BL")
    },
    new[]
    {
        P("pa1", "PA1", "a1", "a2", 80, 0.15, 103, 102.2, 105, 104),
        P("pa2", "PA2", "a2", "j", 80, 0.15, 102.2, 101.4, 104, 103),
        P("pb1", "PB1", "b1", "j", 50, 0.15, 102.5, 102.0, 104.5, 103), // chega 0,6 m acima: tubo de queda
        P("pj", "PJ", "j", "o", 120, 0.15, 101.4, 101.34, 103, 102),    // I = 0,0005: tensão baixa
        P("px", "PX", "x", "j", 20, 0.15, 102.8, 102.6, 103.5, 103),
    });
var ga = NetworkGraph.Build(arvore);
var ra = Sewer.Calculate(ga, opt with { PopStart = 0, PopEnd = 0, Infiltration = 0 });
var pj = ra.Rows.Single(r => r.Reach.Pipe.Name == "PJ");
Near(pj.QEnd, 2.0, 1e-9, "vazão pontual de 2 L/s chega ao trecho final");
Check(pj.Notes.Contains("tensão trativa"), "trecho quase plano deveria acusar tensão trativa: " + pj.Notes);
Check(pj.Notes.Contains("tubo de queda"), "degrau de 0,6 m deveria pedir tubo de queda: " + pj.Notes);
Check(!pj.Ok, "trecho PJ deveria ficar para revisar");

// 5. Anel: avisa.
var anel = new NetworkData("N3", "Anel", "", new[] { S("1", "1", 10, 8), S("2", "2", 10, 8), S("3", "3", 10, 8) },
    new[] { P("q1", "Q1", "1", "2", 10, 0.15, 9, 8.9, 10, 10), P("q2", "Q2", "2", "3", 10, 0.15, 8.9, 8.8, 10, 10), P("q3", "Q3", "3", "1", 10, 0.15, 8.8, 8.7, 10, 10) });
Check(NetworkGraph.Build(anel).Problems.Any(p => p.Contains("circuito")), "anel deveria ser apontado");

// 6. Vala: largura, profundidade, volume, talude, escoramento.
{
    var vala = new NetworkData("N4", "Vala", "", new[] { S("u", "U", 100, 97.5), S("d", "D", 100, 97.4) },
        new[] { P("v", "V", "u", "d", 50, 0.18, 98, 98, 100, 100, outer: 0.20) });
    var gv = NetworkGraph.Build(vala);
    var t0 = Trench.Calculate(gv, new TrenchOptions(0.30, 0.60, 0.10, 0, 1.25)).Single();
    Near(t0.Width, 0.80, 1e-12, "largura");
    Near(t0.DepthUp ?? 0, 2.11, 1e-9, "profundidade");
    Near(t0.Excavation, 50 * 0.8 * 2.11, 1e-9, "escavação sem talude");
    Near(t0.ShoringArea, 2 * 2.11 * 50, 1e-9, "escoramento das duas paredes");
    Near(t0.Backfill, 50 * 0.8 * 2.11 - Math.PI * 0.04 / 4 * 50 - 0.8 * 0.1 * 50, 1e-9, "reaterro");
    Check(t0.Band == "1,50 a 3,00 m", "faixa: " + t0.Band);
    var t1 = Trench.Calculate(gv, new TrenchOptions(0.30, 0.60, 0.10, 0.5, 1.25)).Single();
    Near(t1.Excavation, 50 * (0.8 * 2.11 + 0.5 * 2.11 * 2.11), 1e-9, "escavação com talude");
    // Profundidade variando de 1 a 2 m com limite 1,25: 37,5 m escorados, média (2 + 1,25)/2 nas duas paredes.
    var var = new NetworkData("N5", "Var", "", new[] { S("u", "U", 100, 97.5), S("d", "D", 100, 97.4) },
        new[] { P("v", "V", "u", "d", 50, 0.18, 99.11, 98.11, 100, 100, outer: 0.20) });
    var tv = Trench.Calculate(NetworkGraph.Build(var), new TrenchOptions(0.30, 0.60, 0.10, 0, 1.25)).Single();
    Near(tv.DepthUp ?? 0, 1.0, 1e-9, "profundidade montante");
    Near(tv.DepthDown ?? 0, 2.0, 1e-9, "profundidade jusante");
    Near(tv.ShoringArea, 37.5 * (2 + 1.25), 1e-9, "escoramento parcial");
    var q = Trench.Quantities(NetworkGraph.Build(arvore));
    Check(q.Any(x => x.Group == "Tubos" && x.Count == 5), "quantitativo de tubos");
    Check(q.Where(x => x.Group == "Estruturas").Sum(x => x.Count) == 6, "quantitativo de estruturas");
}

// 7. Renomear: coletor mais longo primeiro, depois ramais; boca de lobo com numeração própria.
{
    var plan = Renamer.Plan(ga, new RenameOptions("PV-", "BL-", "TR-", 1, 2));
    var names = plan.Where(r => !r.IsPipe).ToDictionary(r => r.OldName, r => r.NewName);
    Check(names["A1"] == "PV-01" && names["A2"] == "PV-02" && names["J"] == "PV-03" && names["O"] == "PV-04", "coletor principal: " + string.Join(",", names));
    Check(names["B1"] == "PV-05" && names["X"] == "BL-01", "ramal e boca de lobo: " + string.Join(",", names));
    var pipes = plan.Where(r => r.IsPipe).ToDictionary(r => r.OldName, r => r.NewName);
    Check(pipes["PA1"] == "TR-01" && pipes["PA2"] == "TR-02" && pipes["PJ"] == "TR-03", "tubos: " + string.Join(",", pipes));
    Check(plan.Select(r => r.NewName).Distinct().Count() == plan.Count, "nomes novos únicos");
}

// 8. Planilha: abre como zip e cada XML é válido.
{
    var path = Path.Combine(Path.GetTempPath(), "projeta-redes-teste.xlsx");
    Xlsx.Write(path, new[]
    {
        new XlsxSheet("Esgoto", new[] { "Trecho", "Q (L/s)", "y/D", "Obs" },
            new List<object?[]> { new object?[] { "T1", 1.5, 0.2345, "ok & <teste>" }, new object?[] { "T2", null, 0.5, "" } },
            new[] { -1, 2, 3, -1 }, new[] { "Nota 1" }),
        new XlsxSheet("Esgoto", new[] { "A" }, new List<object?[]> { new object?[] { 1 } }, new[] { 0 })
    });
    using var zip = ZipFile.OpenRead(path);
    foreach (var e in zip.Entries) using (var s = e.Open()) XDocument.Load(s);
    Check(zip.Entries.Any(e => e.FullName == "xl/worksheets/sheet2.xml"), "duas abas");
    Console.WriteLine("planilha de teste: " + path);
}

// 9. Drenagem pluvial (método racional).
{
    NetStructure A(string h, string name, double rim, double sump, double area, double c, double tc = 0, double known = 0, string kind = "BL") =>
        new(h, name, kind, "BL", 0, 0, rim, sump, known, null, null, "", area, c, tc);
    double I(double t) => 1000 * Math.Pow(10, 0.1) / Math.Pow(t + 10, 0.8);   // K 1000, a 0,1, b 10, c 0,8, TR 10
    var so = new StormOptions(10, 10, 1000, 0.1, 10, 0.8, 0.015, 0.85, 0.6, 5, 1.0, 0.4, "teste");
    Near(so.Intensity(10), 114.598, 0.01, "intensidade IDF em 10 min");

    // Um trecho: 1 ha, C 0,8, tempo de entrada 10 min → Q = 0,8·114,6·1/360 = 254,7 L/s.
    var um = new NetworkData("D1", "Um", "", new[] { A("a", "A", 102, 99, 10000, 0.8), A("o", "O", 101, 98, 0, 0, kind: "Saída") },
        new[] { P("p", "P1", "a", "o", 100, 0.6, 99.0, 98.0, 102, 101) });
    var r1 = Storm.Calculate(NetworkGraph.Build(um), so).Rows.Single();
    Near(r1.Flow, 0.8 * I(10) / 360 * 1000, 1e-6, "Q de um trecho");
    Near(r1.Flow, 254.66, 0.05, "Q de um trecho (valor)");
    Near(r1.Tc, 10, 1e-12, "tc inicial");
    Check(r1.Ratio is double y1 && y1 < 0.85 && r1.Ok, "DN 600 a 1% leva 255 L/s: " + r1.Notes);
    Near(r1.TravelTime, 100 / r1.Velocity / 60, 1e-9, "tempo de percurso");

    // Série: tc a jusante = máx(tc próprio 5 min, 10 + percurso); C·A soma.
    var serieP = new NetworkData("D2", "Serie", "", new[]
        { A("a", "A", 103, 101, 10000, 0.8), A("b", "B", 102, 100, 5000, 0.6, tc: 5), A("o", "O", 101, 99, 0, 0, kind: "Saída") },
        new[] { P("p1", "P1", "a", "b", 100, 0.6, 101.0, 100.0, 103, 102), P("p2", "P2", "b", "o", 100, 0.6, 100.0, 99.0, 102, 101) });
    var rs = Storm.Calculate(NetworkGraph.Build(serieP), so).Rows;
    var t2 = 10 + rs[0].TravelTime;
    Near(rs[1].Tc, t2, 1e-9, "tc a jusante soma o percurso");
    Near(rs[1].Area, 1.5, 1e-12, "área acumulada");
    Near(rs[1].RunoffArea, 1.1, 1e-12, "C·A acumulado");
    Near(rs[1].Flow, 1.1 * I(t2) / 360 * 1000, 1e-6, "Q a jusante com a intensidade do tc acumulado");

    // Junção: ramal sem área (tubo longo) não aumenta o tc; vazão pontual entra; DN diminuindo é apontado.
    var junta = new NetworkData("D3", "Junta", "", new[]
        {
            A("a", "A", 103, 101, 20000, 0.7), A("x", "X", 104, 101.5, 0, 0, known: 0.05), A("j", "J", 102, 99.8, 0, 0, kind: "PV"),
            A("o", "O", 101, 98, 3000, 0.9, kind: "Saída")
        },
        new[]
        {
            P("pa", "PA", "a", "j", 50, 0.6, 101.0, 100.5, 103, 102), P("px", "PX", "x", "j", 400, 0.4, 101.5, 100.6, 104, 102),
            P("pj", "PJ", "j", "o", 80, 0.5, 100.4, 99.6, 102, 101)
        });
    var rj = Storm.Calculate(NetworkGraph.Build(junta), so);
    var pa = rj.Rows.Single(r => r.Reach.Pipe.Name == "PA");
    var pjr = rj.Rows.Single(r => r.Reach.Pipe.Name == "PJ");
    Near(pjr.Tc, 10 + pa.TravelTime, 1e-9, "tc só pelo ramal com área");
    Near(pjr.Flow, 1.4 * I(pjr.Tc) / 360 * 1000 + 50, 1e-6, "Q na junção com vazão pontual de 50 L/s");
    Check(pjr.Notes.Contains("diâmetro menor que o de PA"), "DN diminuindo para jusante: " + pjr.Notes);
    Check(pjr.Suggested is double sj && sj >= 0.6 - 1e-9, "DN sugerido não diminui: " + pjr.Suggested);
    Near(rj.LostArea, 0.3, 1e-12, "área no lançamento não entra");
    Check(rj.Problems.Any(p => p.Contains("sem tubo de saída")), "aviso de área no lançamento");
    Near(rj.OutletFlow, pjr.Flow, 1e-9, "vazão no lançamento");
    var px = rj.Rows.Single(r => r.Reach.Pipe.Name == "PX");
    Near(px.Flow, 50, 1e-9, "ramal só com vazão pontual");

    // DN sugerido: 4 ha, C 0,9 a 0,5%: DN 400 não leva, sugere o menor comercial que leva.
    var grande = new NetworkData("D4", "Grande", "", new[] { A("a", "A", 103, 100, 40000, 0.9), A("o", "O", 103, 99, 0, 0, kind: "Saída") },
        new[] { P("p", "P", "a", "o", 100, 0.4, 100.0, 99.5, 103, 103) });
    var rg = Storm.Calculate(NetworkGraph.Build(grande), so).Rows.Single();
    Check(!rg.Ok && rg.Notes.Contains("capacidade"), "DN 400 sobrecarregado: " + rg.Notes);
    var expected = Storm.Commercial.First(d => d >= 0.4 && Hydraulics.Ratio(rg.Flow / 1000, d, 0.015, 0.005) is double v && v <= 0.85);
    Near(rg.Suggested ?? 0, expected, 1e-12, "DN sugerido");
    var before = Storm.Commercial[Array.IndexOf(Storm.Commercial, expected) - 1];
    Check(Hydraulics.Ratio(rg.Flow / 1000, before, 0.015, 0.005) is not double w || w > 0.85, "o DN comercial anterior ao sugerido não atende");

    // Sem equação de chuva: recusa.
    try { Storm.Calculate(NetworkGraph.Build(um), so with { K = 0 }); failures.Add("K zero deveria ser recusado"); }
    catch (ArgumentException) { }

    // Catálogo de estudos de chuva do SGB embutido.
    var all = RainCatalog.Bundled();
    Check(all.Count > 700, "catálogo SGB: " + all.Count);
    Check(all.Any(s => RainCatalog.Normalize(s.Municipio) == "BELO HORIZONTE" && s.Uf == "MG" && RainCatalog.IsOfficial(s.Url)), "estudo de Belo Horizonte no catálogo");
    Console.WriteLine($"drenagem: Q um trecho {r1.Flow:F2} L/s; série tc {rs[1].Tc:F2} min Q {rs[1].Flow:F1} L/s; junção Q {pjr.Flow:F1} L/s; DN sugerido {rg.Suggested * 1000:F0} mm para {rg.Flow:F0} L/s");
}

Console.WriteLine(failures.Count == 0 ? "TODOS OS TESTES PASSARAM" : "FALHAS:\n" + string.Join("\n", failures));
return failures.Count == 0 ? 0 : 1;
