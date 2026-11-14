namespace Projeta.Civil3D;

internal readonly record struct JunctionPoint(double X, double Y)
{
    public static JunctionPoint operator +(JunctionPoint a, JunctionPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static JunctionPoint operator -(JunctionPoint a, JunctionPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static JunctionPoint operator *(JunctionPoint a, double b) => new(a.X * b, a.Y * b);
    public double Length => Math.Sqrt(X * X + Y * Y);
    public static double Dot(JunctionPoint a, JunctionPoint b) => a.X * b.X + a.Y * b.Y;
    public static double Cross(JunctionPoint a, JunctionPoint b) => a.X * b.Y - a.Y * b.X;
}

internal sealed record JunctionArc(JunctionPoint Start, JunctionPoint End, double Bulge, double Length);

internal static class JunctionGeometry
{
    // Interseção das retas suporte; os cliques escolhem as duas semirretas.
    public static JunctionArc Solve(JunctionPoint a0, JunctionPoint a1, JunctionPoint pickA,
        JunctionPoint b0, JunctionPoint b1, JunctionPoint pickB, double radius)
    {
        if (!double.IsFinite(radius) || radius <= 0) throw new ArgumentException("Informe um raio positivo.");
        var da = a1 - a0; var db = b1 - b0;
        if (da.Length < 1e-6 || db.Length < 1e-6) throw new ArgumentException("Trecho sem comprimento.");
        da *= 1 / da.Length; db *= 1 / db.Length;
        var cross = JunctionPoint.Cross(da, db);
        if (Math.Abs(cross) < 1e-6) throw new ArgumentException("Os trechos são paralelos ou coincidentes.");
        var intersection = a0 + da * (JunctionPoint.Cross(b0 - a0, db) / cross);
        var sideA = JunctionPoint.Dot(pickA - intersection, da);
        var sideB = JunctionPoint.Dot(pickB - intersection, db);
        if (Math.Abs(sideA) < .001 || Math.Abs(sideB) < .001)
            throw new ArgumentException("Clique em cada eixo afastado do cruzamento, no lado desejado.");
        var ua = da * Math.Sign(sideA); var ub = db * Math.Sign(sideB);
        var angle = Math.Acos(Math.Clamp(JunctionPoint.Dot(ua, ub), -1, 1));
        var distance = radius / Math.Tan(angle / 2);
        var start = intersection + ua * distance; var end = intersection + ub * distance;
        CheckInside(a0, a1, start); CheckInside(b0, b1, end);
        var sweep = (Math.PI - angle) * -Math.Sign(JunctionPoint.Cross(ua, ub));
        return new(start, end, Math.Tan(sweep / 4), radius * Math.Abs(sweep));
    }

    private static void CheckInside(JunctionPoint a, JunctionPoint b, JunctionPoint p)
    {
        var d = b - a; var length = d.Length; var along = JunctionPoint.Dot(p - a, d) / length;
        if (along < -1e-6 || along > length + 1e-6)
            throw new ArgumentException("O raio não cabe nos trechos retos selecionados. Reduza o raio ou escolha outros trechos.");
    }

    public static double Grade(double startZ, double endZ, double length)
    {
        var grade = 100 * (endZ - startZ) / length;
        if (!double.IsFinite(grade) || Math.Abs(grade) > 7 + 1e-8)
            throw new ArgumentException("A ligação das cotas excede 7% de rampa. Revise raio ou greides.");
        return grade;
    }
}
