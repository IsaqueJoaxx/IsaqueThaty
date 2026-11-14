using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using CivDb = Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Resumo))]

namespace Projeta.Reconstrutor;

public class Acc
{
    public int n; public double comp; public double x0 = double.MaxValue, y0 = double.MaxValue, z0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue, z1 = double.MinValue;
    public void Ext(Extents3d e) { x0 = Math.Min(x0, e.MinPoint.X); y0 = Math.Min(y0, e.MinPoint.Y); z0 = Math.Min(z0, e.MinPoint.Z); x1 = Math.Max(x1, e.MaxPoint.X); y1 = Math.Max(y1, e.MaxPoint.Y); z1 = Math.Max(z1, e.MaxPoint.Z); }
    public object Js() => new { n, comp = Math.Round(comp, 2), ext = x0 == double.MaxValue ? null : new[] { Math.Round(x0, 2), Math.Round(y0, 2), Math.Round(z0, 2), Math.Round(x1, 2), Math.Round(y1, 2), Math.Round(z1, 2) } };
}

public class Resumo
{
    public static string Saida(string padrao) => Environment.GetEnvironmentVariable("PRJ_SAIDA") ?? Path.Combine(Path.GetTempPath(), padrao);

    public static string Tipo(Entity e) => e is ProxyEntity pe ? "Proxy:" + pe.OriginalClassName : e.GetType().Name;

    [CommandMethod("PRJRESUMO", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var camadas = new SortedDictionary<string, SortedDictionary<string, Acc>>();
        var blocosUso = new SortedDictionary<string, Acc>();
        var blocosDef = new Dictionary<string, SortedDictionary<string, int>>();
        var textos = new SortedDictionary<string, List<object>>();
        var layouts = new List<object>();
        var civil = new Dictionary<string, object>();
        var extras = new List<string>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId bid in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
                if (btr.IsFromExternalReference) extras.Add("XREF:" + btr.Name + " -> " + btr.PathName);
                if (btr.IsLayout && btr.Name != BlockTableRecord.ModelSpace)
                {
                    int c = 0; foreach (ObjectId _ in btr) c++;
                    string ln = btr.Name;
                    try { ln = ((Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead)).LayoutName; } catch { }
                    layouts.Add(new { nome = ln, entidades = c });
                }
            }
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                Entity e;
                try { e = tr.GetObject(id, OpenMode.ForRead) as Entity; } catch { continue; }
                if (e == null) continue;
                string tipo = Tipo(e);
                if (!camadas.TryGetValue(e.Layer, out var porTipo)) camadas[e.Layer] = porTipo = new SortedDictionary<string, Acc>();
                if (!porTipo.TryGetValue(tipo, out var a)) porTipo[tipo] = a = new Acc();
                a.n++;
                try { a.Ext(e.GeometricExtents); } catch { }
                if (e is Curve cv) { try { a.comp += cv.GetDistanceAtParameter(cv.EndParam) - cv.GetDistanceAtParameter(cv.StartParam); } catch { } }
                if (e is BlockReference br)
                {
                    string bn = br.Name;
                    try { if (br.IsDynamicBlock) bn = ((BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name; } catch { }
                    if (!blocosUso.TryGetValue(bn, out var ab)) blocosUso[bn] = ab = new Acc();
                    ab.n++; try { ab.Ext(br.GeometricExtents); } catch { }
                    if (!blocosDef.ContainsKey(bn))
                    {
                        var d = new SortedDictionary<string, int>();
                        try
                        {
                            var def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                            foreach (ObjectId sid in def)
                            {
                                try { var se = tr.GetObject(sid, OpenMode.ForRead) as Entity; if (se == null) continue; string k = se.Layer + "|" + Tipo(se); d[k] = d.TryGetValue(k, out var v) ? v + 1 : 1; } catch { }
                            }
                        }
                        catch { }
                        blocosDef[bn] = d;
                    }
                }
                string txt = null; Point3d pos = Point3d.Origin; double h = 0, rot = 0;
                if (e is DBText t) { txt = t.TextString; pos = t.Position; h = t.Height; rot = t.Rotation; }
                else if (e is MText mt) { txt = mt.Text; pos = mt.Location; h = mt.TextHeight; rot = mt.Rotation; }
                if (txt != null)
                {
                    if (!textos.TryGetValue(e.Layer, out var lt)) textos[e.Layer] = lt = new List<object>();
                    if (lt.Count < 30) lt.Add(new { t = txt.Length > 120 ? txt[..120] : txt, x = Math.Round(pos.X, 2), y = Math.Round(pos.Y, 2), h = Math.Round(h, 2), r = Math.Round(rot * 180 / Math.PI, 1) });
                }
            }
            try
            {
                var cd = CivilApplication.ActiveDocument;
                if (cd != null)
                {
                    civil["cs"] = cd.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode;
                    var sups = new List<object>();
                    foreach (ObjectId sid in cd.GetSurfaceIds())
                    {
                        try
                        {
                            var s = (CivDb.Surface)tr.GetObject(sid, OpenMode.ForRead);
                            var p = s.GetGeneralProperties();
                            sups.Add(new { s.Name, tipo = s.GetType().Name, pontos = p.NumberOfPoints, zmin = Math.Round(p.MinimumElevation, 2), zmax = Math.Round(p.MaximumElevation, 2), x0 = Math.Round(p.MinimumCoordinateX, 1), y0 = Math.Round(p.MinimumCoordinateY, 1), x1 = Math.Round(p.MaximumCoordinateX, 1), y1 = Math.Round(p.MaximumCoordinateY, 1) });
                        }
                        catch (System.Exception ex) { sups.Add("erro:" + ex.Message); }
                    }
                    civil["superficies"] = sups;
                    var als = new List<object>();
                    foreach (ObjectId aid in cd.GetAlignmentIds())
                    {
                        try
                        {
                            var al = (CivDb.Alignment)tr.GetObject(aid, OpenMode.ForRead);
                            var perfis = new List<string>();
                            foreach (ObjectId pid in al.GetProfileIds()) { try { var pr = (CivDb.Profile)tr.GetObject(pid, OpenMode.ForRead); perfis.Add(pr.Name + ":" + pr.ProfileType); } catch { } }
                            als.Add(new { al.Name, comp = Math.Round(al.Length, 2), e0 = Math.Round(al.StartingStation, 2), e1 = Math.Round(al.EndingStation, 2), tipo = al.AlignmentType.ToString(), perfis });
                        }
                        catch { }
                    }
                    civil["alinhamentos"] = als;
                    var cors = new List<string>();
                    foreach (ObjectId cid in cd.CorridorCollection) { try { cors.Add(((CivDb.Corridor)tr.GetObject(cid, OpenMode.ForRead)).Name); } catch { } }
                    civil["corredores"] = cors;
                }
            }
            catch (System.Exception ex) { civil["erro"] = ex.Message; }
            tr.Commit();
        }
        var res = new
        {
            arquivo = db.Filename,
            extmin = new[] { db.Extmin.X, db.Extmin.Y }, extmax = new[] { db.Extmax.X, db.Extmax.Y },
            camadas = camadas.ToDictionary(k => k.Key, k => k.Value.ToDictionary(t => t.Key, t => t.Value.Js())),
            blocos = blocosUso.ToDictionary(k => k.Key, k => new { uso = k.Value.Js(), conteudo = blocosDef.TryGetValue(k.Key, out var d) ? d : null }),
            textos, layouts, civil, extras
        };
        File.WriteAllText(Saida("prj_resumo.json"), JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = false }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJRESUMO ok\n");
    }

}
