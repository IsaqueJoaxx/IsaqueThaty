using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Suavizar))]

namespace Projeta.Reconstrutor;

// Ferramentas para suavizar tracados (rodar sempre em COPIA do DWG):
//  PRJINFO    -> exporta pontos (DBPoint) e hachuras (associatividade, contornos) do espaco modelo
//  PRJAPLICAR -> aplica as edicoes do JSON em PRJ_EDITS:
//                {h, op:"pl", v:[[x,y,bulge],...]} | {h, op:"move", dx, dy} | {h, op:"hatch", loops:[{t, v:[[x,y,b],...]}]}
public class Suavizar
{
    static double R(double v) => Math.Round(v, 4);

    static List<double[]> Loop(HatchLoop lp)
    {
        var v = new List<double[]>();
        if (lp.IsPolyline)
        {
            foreach (BulgeVertex bv in lp.Polyline) v.Add(new[] { R(bv.Vertex.X), R(bv.Vertex.Y), Math.Round(bv.Bulge, 8) });
            return v;
        }
        foreach (Curve2d c in lp.Curves)
        {
            if (c is LineSegment2d ls) v.Add(new[] { R(ls.StartPoint.X), R(ls.StartPoint.Y), 0 });
            else if (c is CircularArc2d ca)
            {
                double sw = ca.EndAngle - ca.StartAngle; if (ca.IsClockWise) sw = -sw;
                v.Add(new[] { R(ca.StartPoint.X), R(ca.StartPoint.Y), Math.Round(Math.Tan(sw / 4), 8) });
            }
            else
            {
                var iv = c.GetInterval(); int n = 40;
                for (int k = 0; k < n; k++) { var p = c.EvaluatePoint(iv.LowerBound + (iv.UpperBound - iv.LowerBound) * k / n); v.Add(new[] { R(p.X), R(p.Y), 0 }); }
            }
        }
        return v;
    }

    [CommandMethod("PRJINFO", CommandFlags.Modal)]
    public void Info()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var pts = new List<object>(); var hs = new List<object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                Entity e; try { e = tr.GetObject(id, OpenMode.ForRead) as Entity; } catch { continue; }
                if (e is DBPoint p) pts.Add(new { h = e.Handle.ToString(), l = e.Layer, p = new[] { R(p.Position.X), R(p.Position.Y), R(p.Position.Z) } });
                else if (e is Hatch ha)
                {
                    var assoc = new List<string>();
                    try { foreach (ObjectId a in ha.GetAssociatedObjectIds()) assoc.Add(a.Handle.ToString()); } catch { }
                    double[] ext = null; try { var x = ha.GeometricExtents; ext = new[] { R(x.MinPoint.X), R(x.MinPoint.Y), R(x.MaxPoint.X), R(x.MaxPoint.Y) }; } catch { }
                    var loops = new List<object>();
                    if (ha.PatternName.EndsWith("SOLID") && ext != null && (ext[2] - ext[0] > 50 || ext[3] - ext[1] > 50))
                        for (int i = 0; i < ha.NumberOfLoops; i++) { try { var lp = ha.GetLoopAt(i); loops.Add(new { t = (int)ha.LoopTypeAt(i), pl = lp.IsPolyline, v = Loop(lp) }); } catch (System.Exception ex) { loops.Add(new { erro = ex.Message }); } }
                    hs.Add(new { h = e.Handle.ToString(), l = e.Layer, ha.Associative, n = ha.NumberOfLoops, assoc, ext, pat = ha.PatternName, loops });
                }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_info.json"), JsonSerializer.Serialize(new { pontos = pts, hachuras = hs }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJINFO ok {pts.Count} pontos {hs.Count} hachuras\n");
    }

    static double[] Arr(JsonElement v) => v.EnumerateArray().Select(x => x.GetDouble()).ToArray();

    [CommandMethod("PRJAPLICAR", CommandFlags.Modal)]
    public void Aplicar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var ed = AcApp.DocumentManager.MdiActiveDocument?.Editor;
        var doc = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_EDITS"))).RootElement;
        int ok = 0, falha = 0; var log = new List<string>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (var op in doc.EnumerateArray())
            {
                string hs = op.GetProperty("h").GetString();
                try
                {
                    var id = db.GetObjectId(false, new Handle(Convert.ToInt64(hs, 16)), 0);
                    var e = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    string tipo = op.GetProperty("op").GetString();
                    if (tipo == "pl")
                    {
                        var pl = (Polyline)e;
                        int n0 = pl.NumberOfVertices;
                        foreach (var v in op.GetProperty("v").EnumerateArray())
                        {
                            var a = Arr(v);
                            pl.AddVertexAt(pl.NumberOfVertices, new Point2d(a[0], a[1]), a.Length > 2 ? a[2] : 0, 0, 0);
                        }
                        for (int k = 0; k < n0; k++) pl.RemoveVertexAt(0);
                    }
                    else if (tipo == "move")
                    {
                        e.TransformBy(Matrix3d.Displacement(new Vector3d(op.GetProperty("dx").GetDouble(), op.GetProperty("dy").GetDouble(), 0)));
                    }
                    else if (tipo == "hatch")
                    {
                        var ha = (Hatch)e;
                        ha.Associative = false;
                        int n0 = ha.NumberOfLoops;
                        foreach (var lp in op.GetProperty("loops").EnumerateArray())
                        {
                            var hl = new HatchLoop((HatchLoopTypes)lp.GetProperty("t").GetInt32() | HatchLoopTypes.Polyline);
                            foreach (var v in lp.GetProperty("v").EnumerateArray()) { var a = Arr(v); hl.Polyline.Add(new BulgeVertex(new Point2d(a[0], a[1]), a.Length > 2 ? a[2] : 0)); }
                            ha.AppendLoop(hl);
                        }
                        for (int k = 0; k < n0; k++) ha.RemoveLoopAt(0);
                        ha.EvaluateHatch(true);
                    }
                    ok++;
                }
                catch (System.Exception ex) { falha++; log.Add(hs + ": " + ex.Message); }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_aplicar.json"), JsonSerializer.Serialize(new { ok, falha, log }));
        ed?.WriteMessage($"\nPRJAPLICAR ok={ok} falha={falha}\n");
    }
}
