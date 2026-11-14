using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivilAssembly = Autodesk.Civil.DatabaseServices.Assembly;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D;

public sealed record SurfaceInfo(string Name, string Type, string Handle, bool IsVolume)
{
    public int? Points { get; init; }
    public int? Triangles { get; init; }
    public double? Area2D { get; init; }
    public double? Area3D { get; init; }
    public double? MinZ { get; init; }
    public double? MeanZ { get; init; }
    public double? MaxZ { get; init; }
    public double? SizeX { get; init; }
    public double? SizeY { get; init; }
    public double? Cut { get; init; }
    public double? Fill { get; init; }
    public double? Net { get; init; }
    public double? CutFactor { get; init; }
    public double? FillFactor { get; init; }
    public string? BaseName { get; init; }
    public string? ComparisonName { get; init; }
}

public sealed record ProfileInfo(string Name, string Handle, string Kind, bool IsLayout, double Start, double End);

public sealed record AlignmentInfo(string Name, string Handle, double Start, double End, double Length, bool IsOffset,
    IReadOnlyList<ProfileInfo> Profiles);

public sealed record AssemblyInfo(string Name, string Handle);

public sealed record DrawingSnapshot(string Drawing, string DrawingPath, DateTime CapturedAt, string Unit,
    IReadOnlyList<SurfaceInfo> Surfaces, IReadOnlyList<AlignmentInfo> Alignments, IReadOnlyList<AssemblyInfo> Assemblies);

// Leitura única do desenho ativo. Só leitura: transação curta, OpenMode.ForRead, sem Commit.
// Apenas textos e números saem daqui; nenhum objeto da base fica guardado no painel.
internal static class DrawingReader
{
    public static DrawingSnapshot Read(Document document)
    {
        var db = document.Database;
        var civil = CivilDocument.GetCivilDocument(db);
        var surfaces = new List<SurfaceInfo>();
        var alignments = new List<AlignmentInfo>();
        var assemblies = new List<AssemblyInfo>();
        using (var tr = db.TransactionManager.StartOpenCloseTransaction())
        {
            var surfaceNames = new Dictionary<ObjectId, string>();
            var volumeLinks = new List<(int Index, ObjectId Base, ObjectId Comparison)>();
            foreach (ObjectId id in civil.GetSurfaceIds())
            {
                if (id.IsNull || id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not CivilSurface surface) continue;
                surfaceNames[id] = surface.Name;
                var info = ReadSurface(surface, id, out var baseId, out var comparisonId);
                if (!baseId.IsNull) volumeLinks.Add((surfaces.Count, baseId, comparisonId));
                surfaces.Add(info);
            }
            foreach (var link in volumeLinks)
                surfaces[link.Index] = surfaces[link.Index] with
                {
                    BaseName = surfaceNames.GetValueOrDefault(link.Base, "?"),
                    ComparisonName = surfaceNames.GetValueOrDefault(link.Comparison, "?")
                };

            var alignmentIds = new HashSet<ObjectId>();
            foreach (ObjectId id in civil.GetAlignmentIds()) alignmentIds.Add(id);
            foreach (ObjectId id in civil.GetSitelessAlignmentIds()) alignmentIds.Add(id);
            foreach (var id in alignmentIds)
            {
                if (id.IsNull || id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not Alignment alignment) continue;
                alignments.Add(ReadAlignment(alignment, id, tr));
            }

            foreach (ObjectId id in civil.AssemblyCollection)
            {
                if (id.IsNull || id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not CivilAssembly assembly) continue;
                assemblies.Add(new AssemblyInfo(assembly.Name, id.Handle.ToString()));
            }
            // Sem Commit: nada é alterado.
        }
        var byName = StringComparer.CurrentCultureIgnoreCase;
        return new DrawingSnapshot(System.IO.Path.GetFileName(document.Name), document.Name, DateTime.Now, Fmt.Unit(db.Insunits),
            surfaces.OrderBy(s => s.Name, byName).ToArray(),
            alignments.OrderBy(a => a.Name, byName).ToArray(),
            assemblies.OrderBy(a => a.Name, byName).ToArray());
    }

    private static SurfaceInfo ReadSurface(CivilSurface surface, ObjectId id, out ObjectId baseId, out ObjectId comparisonId)
    {
        baseId = ObjectId.Null;
        comparisonId = ObjectId.Null;
        var info = new SurfaceInfo(surface.Name, TypeLabel(surface), id.Handle.ToString(),
            surface is TinVolumeSurface or GridVolumeSurface);
        // Cada propriedade é lida à parte: uma superfície vazia ou desatualizada não derruba a leitura das demais.
        try
        {
            var g = surface.GetGeneralProperties();
            info = info with { Points = g.NumberOfPoints };
            if (g.NumberOfPoints > 0)
                info = info with
                {
                    MinZ = g.MinimumElevation, MeanZ = g.MeanElevation, MaxZ = g.MaximumElevation,
                    SizeX = g.MaximumCoordinateX - g.MinimumCoordinateX, SizeY = g.MaximumCoordinateY - g.MinimumCoordinateY
                };
        }
        catch { }
        try
        {
            TerrainSurfaceProperties? t = surface switch
            {
                TinSurface tin => tin.GetTerrainProperties(),
                GridSurface grid => grid.GetTerrainProperties(),
                _ => null
            };
            if (t is not null && (info.Points ?? 0) > 0) info = info with { Area2D = t.SurfaceArea2D, Area3D = t.SurfaceArea3D };
        }
        catch { }
        try
        {
            int? triangles = surface switch
            {
                TinSurface tin => tin.GetTinProperties().NumberOfTriangles,
                TinVolumeSurface vol => vol.GetTinProperties().NumberOfTriangles,
                _ => null
            };
            info = info with { Triangles = triangles };
        }
        catch { }
        try
        {
            VolumeSurfaceProperties? v = surface switch
            {
                TinVolumeSurface vol => vol.GetVolumeProperties(),
                GridVolumeSurface vol => vol.GetVolumeProperties(),
                _ => null
            };
            if (v is not null)
            {
                info = info with
                {
                    Cut = v.AdjustedCutVolume, Fill = v.AdjustedFillVolume, Net = v.AdjustedNetVolume,
                    CutFactor = v.CutFactor, FillFactor = v.FillFactor
                };
                baseId = v.BaseSurface;
                comparisonId = v.ComparisonSurface;
            }
        }
        catch { }
        return info;
    }

    private static AlignmentInfo ReadAlignment(Alignment alignment, ObjectId id, Transaction tr)
    {
        var profiles = new List<ProfileInfo>();
        foreach (ObjectId pid in alignment.GetProfileIds())
        {
            if (pid.IsNull || pid.IsErased || tr.GetObject(pid, OpenMode.ForRead) is not Profile profile) continue;
            double start = double.NaN, end = double.NaN;
            try { start = profile.StartingStation; end = profile.EndingStation; } catch { }
            profiles.Add(new ProfileInfo(profile.Name, pid.Handle.ToString(), KindLabel(profile.ProfileType),
                profile.ProfileType == ProfileType.FG, start, end));
        }
        return new AlignmentInfo(alignment.Name, id.Handle.ToString(), alignment.StartingStation, alignment.EndingStation,
            alignment.Length, alignment.IsOffsetAlignment, profiles.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToArray());
    }

    // Volume primeiro, por segurança, caso uma versão futura derive volume de TIN ou grade.
    private static string TypeLabel(CivilSurface surface) => surface switch
    {
        TinVolumeSurface => "Volume TIN",
        GridVolumeSurface => "Volume grade",
        TinSurface => "TIN",
        GridSurface => "Grade",
        _ => surface.GetType().Name
    };

    private static string KindLabel(ProfileType type) => type switch
    {
        ProfileType.FG => "Projeto",
        ProfileType.EG => "Terreno",
        ProfileType.Superimposed => "Sobreposto",
        ProfileType.CorridorFeature => "Corredor",
        ProfileType.OffsetProfile => "Deslocado",
        ProfileType.File => "Arquivo",
        ProfileType.CurbReturnProfile => "Meio-fio",
        _ => type.ToString()
    };
}
