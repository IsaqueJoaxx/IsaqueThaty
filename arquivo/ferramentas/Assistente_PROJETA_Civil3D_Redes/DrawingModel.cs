using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivilAssembly = Autodesk.Civil.DatabaseServices.Assembly;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D;

public sealed record SurfaceInfo(string Name, string Type, string Handle, bool IsVolume)
{
    public int? Revision { get; init; }
    public bool HasDetails { get; init; }
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

// MissingPkt: arquivos .pkt (Subassembly Composer) que o Civil 3D não achou no caminho gravado na seção tipo.
// OutdatedPkt: submontagens cujo PKT mudou depois de inseridas (o Civil 3D pede para atualizar).
public sealed record AssemblyInfo(string Name, string Handle)
{
    public IReadOnlyList<string> MissingPkt { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OutdatedPkt { get; init; } = Array.Empty<string>();
}

public sealed record DrawingSnapshot(string Drawing, string DrawingPath, DateTime CapturedAt, string Unit,
    IReadOnlyList<SurfaceInfo> Surfaces, IReadOnlyList<AlignmentInfo> Alignments, IReadOnlyList<AssemblyInfo> Assemblies)
{
    public IReadOnlyList<StyleInfo> GradeStyles { get; init; } = Array.Empty<StyleInfo>();
    public IReadOnlyList<StyleInfo> GradeLabelSets { get; init; } = Array.Empty<StyleInfo>();
}

public sealed record StyleInfo(string Name, string Handle);

// Leitura do desenho ativo. Só leitura: transação curta, OpenMode.ForRead, sem Commit.
// Apenas textos e números saem daqui; nenhum objeto da base fica guardado no painel.
// Read é leve e roda depois de cada comando; a ficha completa de uma superfície (áreas,
// triângulos, volumes) vem de ReadDetails, só para a superfície escolhida na lista.
internal static class DrawingReader
{
    public static DrawingSnapshot Read(Document document)
    {
        var db = document.Database;
        var civil = CivilDocument.GetCivilDocument(db);
        var surfaces = new List<SurfaceInfo>();
        var alignments = new List<AlignmentInfo>();
        var assemblies = new List<AssemblyInfo>();
        var gradeStyles = new List<StyleInfo>();
        var gradeLabels = new List<StyleInfo>();
        using (var tr = db.TransactionManager.StartOpenCloseTransaction())
        {
            foreach (ObjectId id in civil.GetSurfaceIds())
            {
                if (id.IsNull || id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not CivilSurface surface) continue;
                var info = new SurfaceInfo(surface.Name, TypeLabel(surface), id.Handle.ToString(), surface is TinVolumeSurface or GridVolumeSurface);
                try { info = info with { Revision = surface.GetGeneralProperties().RevisionNumber }; } catch { }
                surfaces.Add(info);
            }

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
            foreach (ObjectId id in civil.Styles.ProfileStyles)
                if (!id.IsNull && !id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Autodesk.Civil.DatabaseServices.Styles.StyleBase style)
                    gradeStyles.Add(new StyleInfo(style.Name, id.Handle.ToString()));
            foreach (ObjectId id in civil.Styles.LabelSetStyles.ProfileLabelSetStyles)
                if (!id.IsNull && !id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Autodesk.Civil.DatabaseServices.Styles.StyleBase style)
                    gradeLabels.Add(new StyleInfo(style.Name, id.Handle.ToString()));
            // Sem Commit: nada é alterado.
        }
        var pkt = ReadPktIssues(db, civil);
        for (var i = 0; i < assemblies.Count; i++)
            if (pkt.TryGetValue(assemblies[i].Handle, out var issue))
                assemblies[i] = assemblies[i] with { MissingPkt = issue.Missing.ToArray(), OutdatedPkt = issue.Outdated.ToArray() };
        var byName = StringComparer.CurrentCultureIgnoreCase;
        return new DrawingSnapshot(System.IO.Path.GetFileName(document.Name), document.Name, DateTime.Now, Fmt.Unit(db.Insunits),
            surfaces.OrderBy(s => s.Name, byName).ToArray(),
            alignments.OrderBy(a => a.Name, byName).ToArray(),
            assemblies.OrderBy(a => a.Name, byName).ToArray())
        {
            GradeStyles = gradeStyles.OrderBy(s => s.Name, byName).ToArray(),
            GradeLabelSets = gradeLabels.OrderBy(s => s.Name, byName).ToArray()
        };
    }

    // Submontagens do Subassembly Composer com problema, por seção tipo (chave: handle da seção tipo).
    // OpenCloseTransaction, como o resto da leitura: uma transação comum aberta fora de comando
    // (na releitura automática) grava um passo vazio no desfazer, e o U do usuário passa a desfazer
    // esses passos vazios em vez do último comando. Visto no teste da Prévia 08.
    public static Dictionary<string, (SortedSet<string> Missing, SortedSet<string> Outdated)> ReadPktIssues(Database db, CivilDocument civil)
    {
        var result = new Dictionary<string, (SortedSet<string> Missing, SortedSet<string> Outdated)>();
        try
        {
            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            foreach (ObjectId id in civil.SubassemblyCollection)
            {
                try
                {
                    if (id.IsNull || id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not Subassembly sub) continue;
                    if (!sub.IsFromSubassemblyComposer || !sub.HasParentAssembly) continue;
                    var status = sub.Status;
                    if (status == SubassemblyStatus.UpToDate) continue;
                    var key = sub.AssemblyId.Handle.ToString();
                    if (!result.TryGetValue(key, out var sets))
                        result[key] = sets = (new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase), new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase));
                    (status == SubassemblyStatus.FileNotFound ? sets.Missing : sets.Outdated).Add(PktName(sub));
                }
                catch { }
            }
            tr.Abort();
        }
        catch { }
        return result;
    }

    public static string PktName(Subassembly sub)
    {
        try
        {
            var file = System.IO.Path.GetFileName(sub.GeometryGenerator.ProjectOrAssemblyName ?? "");
            if (!string.IsNullOrEmpty(file)) return file;
        }
        catch { }
        return sub.Name;
    }

    // Ficha completa de uma superfície. Áreas e volumes podem demorar em superfícies grandes,
    // por isso só são lidos para a superfície escolhida, e não a cada comando.
    public static SurfaceInfo ReadDetails(Document document, string handle)
    {
        var db = document.Database;
        var id = Ids.Resolve(db, handle, "a superfície");
        using var tr = db.TransactionManager.StartOpenCloseTransaction();
        var surface = (CivilSurface)tr.GetObject(id, OpenMode.ForRead);
        var info = ReadSurface(surface, id, out var baseId, out var comparisonId);
        string NameOf(ObjectId other)
        {
            try { return other.IsNull || other.IsErased ? "—" : ((CivilSurface)tr.GetObject(other, OpenMode.ForRead)).Name; }
            catch { return "?"; }
        }
        if (!baseId.IsNull) info = info with { BaseName = NameOf(baseId), ComparisonName = NameOf(comparisonId) };
        return info with { HasDetails = true };
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
