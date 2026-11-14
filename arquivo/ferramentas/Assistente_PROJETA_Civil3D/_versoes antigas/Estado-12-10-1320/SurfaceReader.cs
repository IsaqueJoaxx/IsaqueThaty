using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D;

public sealed record SurfaceRow(string Name, string Type, string Handle, bool IsVolume);
public sealed record SurfaceSnapshot(string Drawing, string DrawingPath, DateTime CapturedAt, IReadOnlyList<SurfaceRow> Rows);

internal static class SurfaceReader
{
    // Invoked only from an AutoCAD command in document context, never a palette callback.
    // Only plain strings leave this scope. No DBObject or ObjectId is retained by the UI.
    public static SurfaceSnapshot Read(Document document)
    {
        var database = document.Database;
        var civil = CivilDocument.GetCivilDocument(database);
        var rows = new List<SurfaceRow>();
        using (var transaction = database.TransactionManager.StartOpenCloseTransaction())
        {
            foreach (ObjectId id in civil.GetSurfaceIds())
            {
                if (id.IsNull || id.IsErased) continue;
                if (transaction.GetObject(id, OpenMode.ForRead) is CivilSurface surface)
                    rows.Add(new SurfaceRow(surface.Name, TypeLabel(surface), id.Handle.ToString(),
                        surface is TinVolumeSurface or GridVolumeSurface));
            }
            // No Commit: this operation opens objects for reading only.
        }
        // System.IO explícito: projetos com WPF não importam System.IO automaticamente.
        return new SurfaceSnapshot(System.IO.Path.GetFileName(document.Name), document.Name, DateTime.Now,
            rows.OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase).ToArray());
    }

    // Volume primeiro: as classes de volume derivam das classes TIN e grade.
    private static string TypeLabel(CivilSurface surface) => surface switch
    {
        TinVolumeSurface => "Volume TIN",
        GridVolumeSurface => "Volume grade",
        TinSurface => "TIN",
        GridSurface => "Grade",
        _ => surface.GetType().Name
    };
}
