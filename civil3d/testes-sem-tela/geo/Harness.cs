using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Table = Autodesk.AutoCAD.DatabaseServices.Table;

[assembly: CommandClass(typeof(GeoHarness.Cmd))]

namespace GeoHarness;

/// <summary>Preparação e conferência do teste sem tela do módulo Geo (fora do repositório).</summary>
public sealed class Cmd
{
    static readonly CultureInfo I = CultureInfo.InvariantCulture;

    [CommandMethod("PRJGEOPREP")]
    public static void Prep()
    {
        var db = AcCore.DocumentManager.MdiActiveDocument.Database;
        using var tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
        if (!lt.Has("LOCAL")) { var l = new LayerTableRecord { Name = "LOCAL", Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1) }; lt.Add(l); tr.AddNewlyCreatedDBObject(l, true); }
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        var pl = new Polyline();
        var v = new[] { (0.0, 0.0), (100.0, 0.0), (100.0, 50.0), (0.0, 50.0) };
        for (int i = 0; i < v.Length; i++) pl.AddVertexAt(i, new Point2d(v[i].Item1, v[i].Item2), 0, 0, 0);
        pl.Closed = true; pl.Layer = "LOCAL";
        ms.AppendEntity(pl); tr.AddNewlyCreatedDBObject(pl, true);
        var t = new DBText { Position = new Point3d(10, 20, 0), Height = 5, TextString = "Galpao", Layer = "LOCAL" };
        ms.AppendEntity(t); tr.AddNewlyCreatedDBObject(t, true);
        tr.Commit();
        AcCore.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPREP OK");
    }

    [CommandMethod("PRJGEOPLANO")]
    public static void Plano()
    {
        var db = AcCore.DocumentManager.MdiActiveDocument.Database;
        using var tr = db.TransactionManager.StartTransaction();
        var id = TinSurface.Create(db, "Plano 850");
        var s = (TinSurface)tr.GetObject(id, OpenMode.ForWrite);
        var p = new Point3dCollection { new Point3d(610900, 7795900, 850), new Point3d(613100, 7795900, 850), new Point3d(613100, 7797600, 850), new Point3d(610900, 7797600, 850), new Point3d(612000, 7796750, 850) };
        s.AddVertices(p);
        tr.Commit();
        AcCore.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPLANO OK");
    }

    [CommandMethod("PRJGEOCHECK")]
    public static void Check()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var sb = new StringBuilder();
        using var tr = db.TransactionManager.StartTransaction();
        try { sb.AppendLine("civil_cs=" + CivilApplication.ActiveDocument.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode); } catch (System.Exception ex) { sb.AppendLine("civil_cs ERRO " + ex.Message); }
        try
        {
            var gid = db.GeoDataObject;
            if (gid.IsNull) sb.AppendLine("geo=nenhum");
            else
            {
                var g = (GeoLocationData)tr.GetObject(gid, OpenMode.ForRead);
                var ll = g.TransformToLonLatAlt(new Point3d(611172.0613, 7796931.7837, 0));
                sb.AppendLine($"geo_cs={g.CoordinateSystem} tipo={g.TypeOfCoordinates} ll={ll.X.ToString("0.0000000", I)},{ll.Y.ToString("0.0000000", I)}");
            }
        }
        catch (System.Exception ex) { sb.AppendLine("geo ERRO " + ex.Message); }
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
        var camadas = new SortedDictionary<string, int>();
        foreach (ObjectId id in ms)
        {
            var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
            var k = e.Layer + "|" + e.GetType().Name;
            camadas[k] = camadas.TryGetValue(k, out var n) ? n + 1 : 1;
            if (e is Polyline p && e.Layer == "LOCAL")
                for (int i = 0; i < p.NumberOfVertices; i++) sb.AppendLine($"local_v{i}={p.GetPoint2dAt(i).X.ToString("0.0000", I)},{p.GetPoint2dAt(i).Y.ToString("0.0000", I)}");
            if (e is DBText t && e.Layer == "LOCAL") sb.AppendLine($"texto={t.Position.X.ToString("0.000", I)},{t.Position.Y.ToString("0.000", I)} rot={(t.Rotation * 180 / Math.PI).ToString("0.000", I)}");
            if (e is Table tb) sb.AppendLine($"tabela [{e.Layer}] {tb.Rows.Count}x{tb.Columns.Count}: {tb.Cells[0, 0].TextString} | {string.Join(" ; ", Enumerable.Range(0, tb.Columns.Count).Select(c => tb.Cells[Math.Min(2, tb.Rows.Count - 1), c].TextString))} | ultima: {string.Join(" ; ", Enumerable.Range(0, tb.Columns.Count).Select(c => tb.Cells[tb.Rows.Count - 1, c].TextString))}");
            if (e is CogoPoint cp) sb.AppendLine($"cogo {cp.PointNumber} nome={cp.PointName} desc={cp.RawDescription} xyz={cp.Location.X.ToString("0.000", I)},{cp.Location.Y.ToString("0.000", I)},{cp.Location.Z.ToString("0.000", I)} [{cp.Layer}]");
        }
        foreach (var kv in camadas) sb.AppendLine($"ent {kv.Key} = {kv.Value}");
        foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
        {
            var s = (Surface)tr.GetObject(id, OpenMode.ForRead);
            var g = s.GetGeneralProperties();
            string est = "";
            try { est = ((Autodesk.Civil.DatabaseServices.Styles.StyleBase)tr.GetObject(s.StyleId, OpenMode.ForRead)).Name; } catch { }
            double zc = double.NaN;
            try { zc = s.FindElevationAtXY(612000, 7796750); } catch { }
            sb.AppendLine($"sup {s.Name} [{s.GetType().Name}] pontos={g.NumberOfPoints} z={g.MinimumElevation.ToString("0.00", I)}..{g.MaximumElevation.ToString("0.00", I)} x={g.MinimumCoordinateX.ToString("0.0", I)}..{g.MaximumCoordinateX.ToString("0.0", I)} y={g.MinimumCoordinateY.ToString("0.0", I)}..{g.MaximumCoordinateY.ToString("0.0", I)} estilo={est} zcentro={zc.ToString("0.000", I)} desc={s.Description}");
            try { foreach (var a in s.Analysis.GetElevationData()) sb.AppendLine($"   cota {a.MinimumElevation.ToString("0.###", I)}..{a.MaximumElevation.ToString("0.###", I)} cor={a.Scheme.ColorValue.R},{a.Scheme.ColorValue.G},{a.Scheme.ColorValue.B}"); } catch (System.Exception ex) { sb.AppendLine("   cota ERRO " + ex.Message); }
            try { foreach (var a in s.Analysis.GetSlopeData()) sb.AppendLine($"   decliv {a.MinimumSlope.ToString("0.####", I)}..{a.MaximumSlope.ToString("0.####", I)} cor={a.Scheme.ColorValue.R},{a.Scheme.ColorValue.G},{a.Scheme.ColorValue.B}"); } catch (System.Exception ex) { sb.AppendLine("   decliv ERRO " + ex.Message); }
        }
        // cotas de referência (arquivo E N Z gerado pelo GDAL), se houver
        var refs = @"C:\Temp\geo\pontos_ref.txt";
        if (File.Exists(refs))
        {
            Surface? rel = null;
            foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
                if (tr.GetObject(id, OpenMode.ForRead) is Surface s && s.Name.StartsWith("PRJ Relevo")) rel = s;
            foreach (var linha in File.ReadAllLines(refs))
            {
                var p = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 3 || rel is null) continue;
                double e = double.Parse(p[0], I), n = double.Parse(p[1], I), z = double.Parse(p[2], I);
                double zs = double.NaN;
                try { zs = rel.FindElevationAtXY(e, n); } catch { }
                sb.AppendLine($"ref {e.ToString("0.000", I)} {n.ToString("0.000", I)} gdal={z.ToString("0.000", I)} superficie={zs.ToString("0.000", I)} dif={(zs - z).ToString("0.0000", I)}");
            }
        }
        tr.Commit();
        File.WriteAllText(@"C:\Temp\geo\geo_check.txt", sb.ToString());
        doc.Editor.WriteMessage("\nCHECK OK");
    }
}
