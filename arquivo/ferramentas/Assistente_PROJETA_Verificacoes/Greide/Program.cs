using Projeta.Civil3D;
var options = new GradeOptions(7, 40, 0.2);
var tests = 0;
foreach (var slope in new[] { 0d, 0.02, 0.07, -0.07, 0.20, -0.20 })
{
    var pts = Enumerable.Range(0, 31).Select(i => new GradePoint(i * 10, 100 + i * 10 * slope)).ToArray();
    var result = GradeSolver.Solve(pts, options);
    if (result.Pvis[0].Station != 0 || result.Pvis[^1].Station != 300) throw new Exception("Intervalo perdido");
    for (var i = 1; i < result.Pvis.Count; i++)
    {
        var a = result.Pvis[i - 1]; var b = result.Pvis[i];
        if (100 * Math.Abs((b.Elevation - a.Elevation)/(b.Station-a.Station)) > 7.0000001) throw new Exception("Rampa excedida");
    }
    if (Math.Abs(slope) > .07 && !result.Warnings.Any(w => w.Contains("tolerância"))) throw new Exception("Desvio não avisado");
    if (Math.Abs(slope) > .07 && result.Pvis.Count != 2) throw new Exception("Tangentes colineares redundantes mantidas");
    tests++;
}
void Reject(GradePoint[] pts, GradeOptions opt) { try { GradeSolver.Solve(pts, opt); } catch(ArgumentException) { tests++; return; } throw new Exception("Entrada inválida aceita"); }
var valid = new[] {new GradePoint(0,0),new GradePoint(10,0),new GradePoint(20,0)};
Reject(valid, options with { MaxPercent = 8 });
Reject(valid, options with { MaxPercent = double.NaN });
Reject(valid, options with { MinTangent = 0 });
Reject(new[] {new GradePoint(0,0),new GradePoint(0,1),new GradePoint(20,0)}, options);
Reject(new[] {new GradePoint(0,0),new GradePoint(10,double.NaN),new GradePoint(20,0)}, options);
var kink = Enumerable.Range(0,21).Select(i => new GradePoint(i*10, i<=10 ? i*.7 : (20-i)*.7)).ToArray();
var retained = GradeSolver.Solve(kink, options);
if (retained.Pvis.Count < 3) throw new Exception("Quebra de greide necessária removida");
tests++;
Console.WriteLine($"OK: {tests} cenários. Rampa limitada, extremos preservados, PIVs colineares removidos, quebra necessária mantida e entradas inválidas recusadas. Sem teste normativo ou Civil 3D.");
