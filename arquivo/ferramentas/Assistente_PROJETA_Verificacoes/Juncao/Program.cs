using Projeta.Civil3D;

var count = 0;
void Near(double actual, double expected, double tolerance = 1e-7)
{
    if (Math.Abs(actual - expected) > tolerance) throw new Exception($"Esperado {expected}, obtido {actual}.");
    count++;
}
void Reject(Action action)
{
    try { action(); } catch (ArgumentException) { count++; return; }
    throw new Exception("Deveria rejeitar os dados.");
}
JunctionPoint P(double x, double y) => new(x, y);

// Quatro quadrantes, duas orientações de cada eixo e coordenadas topográficas.
foreach (var ox in new[] { 0d, 600000d })
foreach (var sx in new[] { -1, 1 })
foreach (var sy in new[] { -1, 1 })
foreach (var reverseA in new[] { false, true })
foreach (var reverseB in new[] { false, true })
{
    var origin = P(ox, 7800000);
    var a0 = origin + P(-100, 0); var a1 = origin + P(100, 0);
    var b0 = origin + P(0, -100); var b1 = origin + P(0, 100);
    if (reverseA) (a0, a1) = (a1, a0);
    if (reverseB) (b0, b1) = (b1, b0);
    var arc = JunctionGeometry.Solve(a0, a1, origin + P(50 * sx, 0), b0, b1, origin + P(0, 50 * sy), 12);
    Near(arc.Start.X, ox + sx * 12); Near(arc.Start.Y, origin.Y);
    Near(arc.End.X, ox); Near(arc.End.Y, origin.Y + sy * 12);
    Near(arc.Length, 6 * Math.PI);
    Near(arc.Bulge, -sx * sy * Math.Tan(Math.PI / 8));
}
// Interseção oblíqua: distância ao PI e comprimento analíticos.
var oblique = JunctionGeometry.Solve(P(-100, 0), P(100, 0), P(50, 0),
    P(-50, -50 * Math.Sqrt(3)), P(50, 50 * Math.Sqrt(3)), P(25, 25 * Math.Sqrt(3)), 10);
Near(oblique.Start.X, 10 * Math.Sqrt(3)); Near(oblique.Length, 20 * Math.PI / 3);
// O raio não cabe; paralelas; clique sobre PI; raio inválido; rampa excessiva.
Reject(() => JunctionGeometry.Solve(P(-5, 0), P(5, 0), P(2, 0), P(0, -5), P(0, 5), P(0, 2), 12));
Reject(() => JunctionGeometry.Solve(P(-10, 0), P(10, 0), P(2, 0), P(-10, 2), P(10, 2), P(2, 2), 1));
Reject(() => JunctionGeometry.Solve(P(-10, 0), P(10, 0), P(0, 0), P(0, -10), P(0, 10), P(0, 2), 1));
Reject(() => JunctionGeometry.Solve(P(-10, 0), P(10, 0), P(2, 0), P(0, -10), P(0, 10), P(0, 2), double.NaN));
Reject(() => JunctionGeometry.Grade(100, 108, 100));
Near(JunctionGeometry.Grade(100, 107, 100), 7);
Near(JunctionGeometry.Grade(107, 100, 100), -7);
Console.WriteLine($"JUNÇÃO — IFM: {count} verificações aprovadas.");
