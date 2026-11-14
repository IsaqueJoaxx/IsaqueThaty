using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;

namespace Projeta.Civil3D;

internal sealed record GradeRequest(string DrawingPath, string AlignmentHandle, string ProfileHandle, double Step, GradeOptions Options);

internal static class GradeReader
{
    public static GradePreview Read(Document document, GradeRequest request)
    {
        if (!Actions.SameDrawing(document, request.DrawingPath)) throw new InvalidOperationException("O desenho ativo mudou. Atualize a leitura.");
        if (document.Database.Insunits != UnitsValue.Meters) throw new InvalidOperationException("Esta prévia requer unidades em metros.");
        if (!double.IsFinite(request.Step) || request.Step <= 0) throw new ArgumentException("Passo inválido.");
        var db = document.Database;
        using var tr = db.TransactionManager.StartOpenCloseTransaction();
        var axisId = Ids.Resolve(db, request.AlignmentHandle, "o eixo");
        var profileId = Ids.Resolve(db, request.ProfileHandle, "o perfil do terreno");
        var axis = (Alignment)tr.GetObject(axisId, OpenMode.ForRead);
        var profile = (Profile)tr.GetObject(profileId, OpenMode.ForRead);
        if (profile.AlignmentId != axisId || profile.ProfileType != ProfileType.EG)
            throw new InvalidOperationException("Escolha um perfil de terreno pertencente ao eixo selecionado.");
        var start = Math.Max(axis.StartingStation, profile.StartingStation);
        var end = Math.Min(axis.EndingStation, profile.EndingStation);
        if (!double.IsFinite(start) || !double.IsFinite(end) || end <= start)
            throw new InvalidOperationException("O perfil não tem intervalo de leitura válido.");
        var intervals = Math.Ceiling((end - start) / request.Step);
        if (intervals < 2 || intervals > 599)
            throw new InvalidOperationException("Ajuste o passo para obter de 3 a 600 amostras. O passo não será aumentado automaticamente.");
        var points = new List<GradePoint>();
        for (var i = 0; i <= (int)intervals; i++)
        {
            var station = Math.Min(start + i * request.Step, end);
            // A missing/invalid elevation aborts the entire preview rather than hiding a gap.
            points.Add(new GradePoint(station, profile.ElevationAt(station)));
        }
        var signature = string.Join(";", points.Select(p => p.Station.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            + ":" + p.Elevation.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
        return GradeSolver.Solve(points, request.Options) with { SourceSignature = signature };
    }
}
