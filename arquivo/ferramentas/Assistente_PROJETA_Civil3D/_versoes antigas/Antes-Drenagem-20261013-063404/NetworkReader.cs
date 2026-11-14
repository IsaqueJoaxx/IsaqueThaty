using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D;

internal sealed record NetworkInfo(string Name, string Handle, int Pipes, int Structures);

// Leitura das redes de tubulação. Só leitura (transação de abrir e fechar, sem passo de desfazer).
internal static class NetworkReader
{
    public static IReadOnlyList<NetworkInfo> List(Database db)
    {
        var civil = CivilDocument.GetCivilDocument(db);
        var list = new List<NetworkInfo>();
        using var tr = db.TransactionManager.StartOpenCloseTransaction();
        foreach (ObjectId id in civil.GetPipeNetworkIds())
        {
            if (id.IsNull || id.IsErased) continue;
            if (tr.GetObject(id, OpenMode.ForRead) is Network n)
                list.Add(new NetworkInfo(n.Name, id.Handle.ToString(), n.GetPipeIds().Count, n.GetStructureIds().Count));
        }
        return list.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public static NetworkData Read(Database db, string handle)
    {
        if (db.Insunits != UnitsValue.Meters) throw new InvalidOperationException("As redes são calculadas com o desenho em metros.");
        var id = Ids.Resolve(db, handle, "a rede");
        using var tr = db.TransactionManager.StartOpenCloseTransaction();
        var network = (Network)tr.GetObject(id, OpenMode.ForRead);
        CivilSurface? Open(ObjectId sid)
        {
            try { return sid.IsNull || sid.IsErased ? null : tr.GetObject(sid, OpenMode.ForRead) as CivilSurface; }
            catch { return null; }
        }
        var networkSurface = Open(SafeId(() => network.ReferenceSurfaceId));
        static double? Ground(CivilSurface? s, Point3d p)
        {
            if (s is null) return null;
            try { var z = s.FindElevationAtXY(p.X, p.Y); return double.IsFinite(z) ? z : null; }
            catch { return null; }
        }
        var structures = new List<NetStructure>();
        foreach (ObjectId sid in network.GetStructureIds())
        {
            if (sid.IsErased || tr.GetObject(sid, OpenMode.ForRead) is not Structure s) continue;
            double? station = null, offset = null;
            var alignment = "";
            try
            {
                if (!s.RefAlignmentId.IsNull && !s.RefAlignmentId.IsErased)
                {
                    alignment = s.RefAlignmentName ?? "";
                    station = s.Station;
                    offset = s.Offset;
                }
            }
            catch { station = offset = null; }
            structures.Add(new NetStructure(sid.Handle.ToString(), s.Name, Kind(s), Describe(s), s.Location.X, s.Location.Y,
                s.RimElevation, s.SumpElevation, SafeDouble(() => s.KnownFlow), station, offset, alignment));
        }
        var rims = structures.ToDictionary(s => s.Handle, s => s.Rim);
        var pipes = new List<NetPipe>();
        foreach (ObjectId pid in network.GetPipeIds())
        {
            if (pid.IsErased || tr.GetObject(pid, OpenMode.ForRead) is not Pipe p) continue;
            var start = Handle(SafeId(() => p.StartStructureId));
            var end = Handle(SafeId(() => p.EndStructureId));
            var surface = Open(SafeId(() => p.RefSurfaceId)) ?? networkSurface;
            double? Rim(string? h) => h is not null && rims.TryGetValue(h, out var z) ? z : null;
            var inner = p.InnerDiameterOrWidth;
            var height = SafeDouble(() => p.InnerHeight);
            var half = (height > 0 ? height : inner) / 2;
            var flow = SafeFlow(p) switch { FlowDirectionType.StartToEnd => 1, FlowDirectionType.EndToStart => -1, _ => 0 };
            double? station = null, offset = null;
            var alignment = "";
            try
            {
                if (!p.RefAlignmentId.IsNull && !p.RefAlignmentId.IsErased)
                {
                    alignment = p.RefAlignmentName ?? "";
                    station = p.StartStation;
                    offset = p.StartOffset;
                }
            }
            catch { station = offset = null; }
            pipes.Add(new NetPipe(pid.Handle.ToString(), p.Name, start, end, p.Length2D, p.Length3D,
                SafeDouble(() => p.Length2DCenterToCenter), inner, p.OuterDiameterOrWidth,
                p.StartPoint.Z - half, p.EndPoint.Z - half,
                Ground(surface, p.StartPoint) ?? Rim(start), Ground(surface, p.EndPoint) ?? Rim(end),
                flow, p.PartSizeName ?? "", SafeText(() => p.Material), station, offset, alignment));
        }
        return new NetworkData(handle, network.Name, networkSurface?.Name ?? "", structures, pipes);
    }

    // Boca de lobo/caixa de captação, ponto de lançamento ou poço de visita.
    private static string Kind(Structure s)
    {
        var type = PipeNetworkStructureType.UndefinedType;
        try { type = s.StructureType; } catch { }
        switch (type)
        {
            case PipeNetworkStructureType.GrateInlet or PipeNetworkStructureType.CurbInlet or PipeNetworkStructureType.CombinationInlet:
                return "BL";
            case PipeNetworkStructureType.Outfall:
                return "Saída";
            case PipeNetworkStructureType.Manhole:
                return "PV";
        }
        var text = $"{SafeText(() => s.PartFamilyName)} {SafeText(() => s.PartSizeName)} {SafeText(() => s.PartDescription)} {s.Name}".ToUpperInvariant();
        if (new[] { "BOCA", "BL ", "BL-", "GRELHA", "INLET", "CAPTA" }.Any(text.Contains)) return "BL";
        if (new[] { "LANÇ", "LANC", "OUTFALL", "DESCARGA", "DISSIPA" }.Any(text.Contains)) return "Saída";
        return "PV";
    }

    private static string Describe(Structure s)
    {
        foreach (var text in new[] { SafeText(() => s.PartSizeName), SafeText(() => s.PartDescription), SafeText(() => s.PartFamilyName) })
            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
        return "estrutura";
    }

    private static string? Handle(ObjectId id) => id.IsNull || id.IsErased ? null : id.Handle.ToString();
    private static ObjectId SafeId(Func<ObjectId> get) { try { return get(); } catch { return ObjectId.Null; } }
    private static double SafeDouble(Func<double> get) { try { var v = get(); return double.IsFinite(v) ? v : 0; } catch { return 0; } }
    private static string SafeText(Func<string?> get) { try { return get() ?? ""; } catch { return ""; } }
    private static FlowDirectionType SafeFlow(Pipe p) { try { return p.FlowDirection; } catch { return FlowDirectionType.Bidirectional; } }
}
