using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CivilEntity = Autodesk.Civil.DatabaseServices.Entity;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D;

internal static class VolumeOps
{
    // Sem "Create": a superfície de volume existe só dentro de uma transação que é abortada,
    // então nada fica no desenho. Com "Create": a superfície é gravada com limite e fatores.
    public static VolumeResult Run(Document document, VolumeRequest request)
    {
        var db = document.Database;
        var civil = CivilDocument.GetCivilDocument(db);
        var baseId = Ids.Resolve(db, request.BaseHandle, "o terreno natural");
        var comparisonId = Ids.Resolve(db, request.ComparisonHandle, "o projeto");
        var limitId = request.LimitHandle is null ? ObjectId.Null : Ids.Resolve(db, request.LimitHandle, "a polilinha do limite");

        using var tr = db.TransactionManager.StartTransaction();
        var baseName = ((CivilSurface)tr.GetObject(baseId, OpenMode.ForRead)).Name;
        var comparisonName = ((CivilSurface)tr.GetObject(comparisonId, OpenMode.ForRead)).Name;
        Point3dCollection? polygon = null;
        double? area = null;
        if (!limitId.IsNull)
        {
            var pl = (Polyline)tr.GetObject(limitId, OpenMode.ForRead);
            polygon = Geometry.Polygon(pl);
            area = pl.Area;
        }
        var limitText = request.LimitHandle is null ? "Área comum às duas superfícies" : $"Polilinha {request.LimitHandle}";

        string name;
        if (request.Create)
        {
            var existing = new List<string>();
            foreach (ObjectId id in civil.GetSurfaceIds())
                if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is CivilSurface s) existing.Add(s.Name);
            name = Names.Unique(existing, $"VOL - {baseName} x {comparisonName}");
        }
        else name = "PROJETA_TEMP_" + Guid.NewGuid().ToString("N")[..8];

        var volumeId = TinVolumeSurface.Create(name, baseId, comparisonId);
        var volume = (TinVolumeSurface)tr.GetObject(volumeId, OpenMode.ForWrite);
        double cut, fill;
        if (request.Create)
        {
            volume.CutFactor = request.CutFactor;
            volume.FillFactor = request.FillFactor;
            if (!limitId.IsNull)
                volume.BoundariesDefinition.AddBoundaries(new ObjectIdCollection { limitId }, 0.1, SurfaceBoundaryType.Outer, true);
            volume.Rebuild();
            var p = volume.GetVolumeProperties();
            cut = p.UnadjustedCutVolume;
            fill = p.UnadjustedFillVolume;
            tr.Commit();
        }
        else
        {
            volume.Rebuild();
            if (polygon is not null)
            {
                var b = volume.GetBoundedVolumes(polygon);
                cut = b.Cut;
                fill = b.Fill;
            }
            else
            {
                var p = volume.GetVolumeProperties();
                cut = p.UnadjustedCutVolume;
                fill = p.UnadjustedFillVolume;
            }
            tr.Abort(); // a superfície temporária some com a transação
        }
        return new VolumeResult(baseName, comparisonName, limitText, area, cut, fill,
            request.CutFactor, request.FillFactor, request.Create ? name : null);
    }
}

internal static class Geometry
{
    // Contorno da polilinha em pontos, com arcos divididos em trechos curtos.
    public static Point3dCollection Polygon(Polyline pl)
    {
        var points = new Point3dCollection();
        var n = pl.NumberOfVertices;
        var segments = pl.Closed ? n : n - 1;
        for (var i = 0; i < segments; i++)
        {
            var p = pl.GetPoint2dAt(i);
            points.Add(new Point3d(p.X, p.Y, 0));
            if (pl.GetSegmentType(i) == SegmentType.Arc)
            {
                var arc = pl.GetArcSegment2dAt(i);
                var range = arc.GetInterval();
                const int parts = 24;
                for (var k = 1; k < parts; k++)
                {
                    var q = arc.EvaluatePoint(range.LowerBound + (range.UpperBound - range.LowerBound) * k / parts);
                    points.Add(new Point3d(q.X, q.Y, 0));
                }
            }
        }
        if (!pl.Closed) { var last = pl.GetPoint2dAt(n - 1); points.Add(new Point3d(last.X, last.Y, 0)); }
        if (points.Count > 0) points.Add(points[0]);
        return points;
    }

    public static bool IsClosed(Polyline pl) =>
        pl.NumberOfVertices >= 3 && (pl.Closed || pl.GetPoint2dAt(0).GetDistanceTo(pl.GetPoint2dAt(pl.NumberOfVertices - 1)) < 1e-6);
}

internal static class SectionOps
{
    public static string Run(Document document, SectionRequest request)
    {
        var db = document.Database;
        var ed = document.Editor;
        var alignmentId = Ids.Resolve(db, request.AlignmentHandle, "o eixo");
        var wanted = request.SurfaceHandles.Select(h => Ids.Resolve(db, h, "uma das superfícies")).ToHashSet();

        Point3d? insert = null;
        if (request.Views)
        {
            var answer = ed.GetPoint(new PromptPointOptions("\nPROJETA: clique o ponto de inserção das vistas de seção: "));
            if (answer.Status != PromptStatus.OK) return "Cancelado: nenhum ponto de inserção. Nada foi criado.";
            insert = answer.Value;
        }

        ObjectId groupId;
        string groupName, alignmentName;
        int created = 0, failed = 0, sampled = 0;
        double from, to;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
            alignmentName = alignment.Name;
            from = Math.Max(request.Start, alignment.StartingStation);
            to = Math.Min(request.End, alignment.EndingStation);
            var existing = new List<string>();
            foreach (ObjectId id in alignment.GetSampleLineGroupIds())
                if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is CivilEntity e) existing.Add(e.Name);
            groupName = Names.Unique(existing, "PROJETA SL");
            groupId = SampleLineGroup.Create(groupName, alignmentId);

            var used = new List<string>();
            foreach (var station in SectionsPage.Stations(from, to, request.Interval))
            {
                try
                {
                    double x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                    alignment.PointLocation(station, -request.Left, ref x1, ref y1);
                    alignment.PointLocation(station, request.Right, ref x2, ref y2);
                    var name = Names.Unique(used, alignment.GetStationStringWithEquations(station));
                    used.Add(name);
                    SampleLine.Create(name, groupId, new Point2dCollection { new Point2d(x1, y1), new Point2d(x2, y2) });
                    created++;
                }
                catch { failed++; }
            }
            var group = (SampleLineGroup)tr.GetObject(groupId, OpenMode.ForWrite);
            foreach (SectionSource source in group.GetSectionSources())
                if (wanted.Contains(source.SourceId)) { source.IsSampled = true; sampled++; }
            tr.Commit();
        }

        var views = 0;
        if (insert is Point3d point && created > 0)
        {
            using var tr = db.TransactionManager.StartTransaction();
            var group = (SampleLineGroup)tr.GetObject(groupId, OpenMode.ForWrite);
            using var range = new SectionViewGroupCreationRangeOptions(groupId);
            var placement = new SectionViewGroupCreationPlacementOptions();
            placement.UseDraftPlacement();
            var viewGroup = group.SectionViewGroups.Add(point, from, to, range, placement);
            views = viewGroup.GetSectionViewIds().Count;
            tr.Commit();
        }

        return $"Grupo \"{groupName}\" criado no eixo {alignmentName}: {created} linhas de amostragem" +
               (failed > 0 ? $" ({failed} estacas não puderam ser criadas)" : "") +
               $", {sampled} superfície(s) amostrada(s)" + (request.Views ? $", {views} vistas de seção" : "") +
               ". Para desfazer: U.";
    }
}

internal static class CorridorOps
{
    public static string Run(Document document, CorridorRequest request)
    {
        var db = document.Database;
        var civil = CivilDocument.GetCivilDocument(db);
        var assemblyId = Ids.Resolve(db, request.AssemblyHandle, "a seção tipo");
        var targetId = request.TargetHandle is null ? ObjectId.Null : Ids.Resolve(db, request.TargetHandle, "a superfície alvo");
        var report = new List<string>();
        var createdNames = new List<string>();
        var corridors = new List<Corridor>();

        using var tr = db.TransactionManager.StartTransaction();
        var existing = new List<string>();
        foreach (ObjectId id in civil.CorridorCollection)
            if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Corridor c) existing.Add(c.Name);

        Corridor? shared = null;
        var targetsSet = 0;
        foreach (var item in request.Items)
        {
            var alignmentId = Ids.Resolve(db, item.AlignmentHandle, "um dos eixos");
            var profileId = Ids.Resolve(db, item.ProfileHandle, "um dos greides");
            var alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
            var profile = (Profile)tr.GetObject(profileId, OpenMode.ForRead);
            var start = Math.Max(alignment.StartingStation, profile.StartingStation);
            var end = Math.Min(alignment.EndingStation, profile.EndingStation);
            if (end - start < 0.01) { report.Add($"{alignment.Name}: o greide não cobre o eixo, pulado"); continue; }

            Corridor corridor;
            if (request.Single && shared is not null) corridor = shared;
            else
            {
                var name = Names.Unique(existing, request.Single ? request.BaseName : $"{request.BaseName} - {alignment.Name}");
                existing.Add(name);
                corridor = (Corridor)tr.GetObject(civil.CorridorCollection.Add(name), OpenMode.ForWrite);
                corridors.Add(corridor);
                createdNames.Add(name);
                if (request.Single) shared = corridor;
            }

            var baselineNames = new List<string>();
            foreach (Baseline b in corridor.Baselines) baselineNames.Add(b.Name);
            var baseline = corridor.Baselines.Add(Names.Unique(baselineNames, alignment.Name), alignmentId, profileId);
            var region = baseline.BaselineRegions.Add("RG - " + alignment.Name, assemblyId, start, end);
            var setting = region.AppliedAssemblySetting;
            setting.FrequencyAlongTangents = request.Frequency;
            setting.FrequencyAlongCurves = request.Frequency;
            setting.FrequencyAlongSpirals = request.Frequency;
            setting.FrequencyAlongProfileCurves = request.Frequency;
            if (!targetId.IsNull)
            {
                using var targets = region.GetTargets();
                foreach (SubassemblyTargetInfo t in targets)
                    if (t.TargetType == SubassemblyLogicalNameType.Surface)
                    {
                        t.TargetIds = new ObjectIdCollection { targetId };
                        targetsSet++;
                    }
                region.SetTargets(targets);
            }
            report.Add($"{alignment.Name} → {profile.Name} ({Fmt.N(start)} a {Fmt.N(end)} m)");
        }
        foreach (var corridor in corridors) corridor.Rebuild();
        tr.Commit();

        if (createdNames.Count == 0) return "Nenhum corredor criado. " + string.Join("; ", report);
        return $"Criado(s): {string.Join(", ", createdNames)}. " + string.Join("; ", report) +
               (targetId.IsNull ? "" : $". Alvos de superfície ligados: {targetsSet}") + ". Para desfazer: U.";
    }
}

internal static class ZoomOps
{
    // Vai até o objeto no Modelo e o deixa selecionado (a paleta Propriedades mostra seus dados).
    public static string Run(Document document, string handle)
    {
        var db = document.Database;
        var ed = document.Editor;
        var id = Ids.Resolve(db, handle, "a superfície");
        Extents3d extents;
        string name;
        using (var tr = db.TransactionManager.StartOpenCloseTransaction())
        {
            var entity = (Autodesk.AutoCAD.DatabaseServices.Entity)tr.GetObject(id, OpenMode.ForRead);
            name = entity is CivilEntity ce ? ce.Name : handle;
            extents = entity.GeometricExtents;
        }
        if (Convert.ToInt32(AcadApplication.GetSystemVariable("TILEMODE")) == 0)
            AcadApplication.SetSystemVariable("TILEMODE", 1);

        using (var view = ed.GetCurrentView())
        {
            var toWorld = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target)
                          * Matrix3d.Displacement(view.Target - Point3d.Origin)
                          * Matrix3d.PlaneToWorld(view.ViewDirection);
            extents.TransformBy(toWorld.Inverse());
            var w = extents.MaxPoint.X - extents.MinPoint.X;
            var h = extents.MaxPoint.Y - extents.MinPoint.Y;
            view.Width = Math.Max(w, 1) * 1.1;
            view.Height = Math.Max(h, 1) * 1.1;
            view.CenterPoint = new Point2d((extents.MinPoint.X + extents.MaxPoint.X) / 2, (extents.MinPoint.Y + extents.MaxPoint.Y) / 2);
            ed.SetCurrentView(view);
        }
        ed.SetImpliedSelection(new[] { id });
        return $"{name} em vista e selecionada.";
    }
}
