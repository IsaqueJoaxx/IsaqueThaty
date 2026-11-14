using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Inviabilidade))]

#nullable enable
namespace Projeta.Reconstrutor;

// Comandos de medicao (somente leitura) para o estudo de inviabilidade
public class Inviabilidade
{
    static double R(double v) => double.IsNaN(v) || double.IsInfinity(v) ? -9999 : Math.Round(v, 3);
    static void Msg(string s) => AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + s + "\n");

    // PRJPERFAMOSTRA: amostra greides e superficies ao longo dos alinhamentos (passo PRJ_PASSO, offsets PRJ_OFFS, superficies PRJ_SUPS)
    [CommandMethod("PRJPERFAMOSTRA", CommandFlags.Modal)]
    public void PerfAmostra()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        double passo = double.TryParse(Environment.GetEnvironmentVariable("PRJ_PASSO"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pp) ? pp : 1.0;
        var offs = (Environment.GetEnvironmentVariable("PRJ_OFFS") ?? "-3,0,3").Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var supNomes = (Environment.GetEnvironmentVariable("PRJ_SUPS") ?? "PRIMITIVA").Split('|');
        var alNomes = (Environment.GetEnvironmentVariable("PRJ_ALINS") ?? "").Split('|').Where(s => s != "").ToHashSet();
        var res = new List<object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var sups = new List<(string, Surface)>();
            foreach (ObjectId sid in cd.GetSurfaceIds())
            {
                var s = tr.GetObject(sid, OpenMode.ForRead) as Surface; if (s != null && supNomes.Contains(s.Name)) sups.Add((s.Name, s));
            }
            foreach (ObjectId aid in cd.GetAlignmentIds())
            {
                var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
                if (alNomes.Count > 0 && !alNomes.Contains(al.Name)) continue;
                var perfis = new List<Profile>();
                foreach (ObjectId pid in al.GetProfileIds()) perfis.Add((Profile)tr.GetObject(pid, OpenMode.ForRead));
                var linhas = new List<object>();
                for (double st = al.StartingStation; st <= al.EndingStation + 1e-6; st += passo)
                {
                    double s0 = Math.Min(st, al.EndingStation);
                    var pts = new List<object>();
                    foreach (var o in offs)
                    {
                        double x = 0, y = 0;
                        try { al.PointLocation(s0, o, ref x, ref y); } catch { continue; }
                        var zs = new Dictionary<string, double>();
                        foreach (var (n, s) in sups) { try { zs[n] = R(s.FindElevationAtXY(x, y)); } catch { zs[n] = -9999; } }
                        pts.Add(new { o, x = R(x), y = R(y), zs });
                    }
                    var zp = new Dictionary<string, double>();
                    foreach (var pr in perfis) { try { zp[pr.Name] = R(pr.ElevationAt(s0)); } catch { } }
                    linhas.Add(new { e = R(s0), pts, zp });
                }
                res.Add(new { al.Name, comp = R(al.Length), linhas });
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_perfamostra.json"), JsonSerializer.Serialize(res));
        Msg($"PRJPERFAMOSTRA ok {res.Count}");
    }

    static object? Val(object? v)
    {
        switch (v)
        {
            case null: return null;
            case double d: return R(d);
            case float f: return R(f);
            case int or long or short or bool or string: return v;
            case Enum e: return e.ToString();
            case Point3d p: return new[] { R(p.X), R(p.Y), R(p.Z) };
            case Point2d p2: return new[] { R(p2.X), R(p2.Y) };
            case Vector3d vv: return new[] { R(vv.X), R(vv.Y), R(vv.Z) };
            case ObjectId oid: return oid.IsNull ? null : oid.Handle.ToString();
            case Extents3d ex: return new[] { R(ex.MinPoint.X), R(ex.MinPoint.Y), R(ex.MinPoint.Z), R(ex.MaxPoint.X), R(ex.MaxPoint.Y), R(ex.MaxPoint.Z) };
            default: return null;
        }
    }

    static Dictionary<string, object?> Props(object o, int prof)
    {
        var d = new Dictionary<string, object?>();
        foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            object? v; try { v = p.GetValue(o); } catch { continue; }
            var s = Val(v);
            if (s != null) { d[p.Name] = s; continue; }
            if (v == null || prof <= 0) continue;
            var tn = v.GetType().Namespace ?? "";
            if (tn.StartsWith("Autodesk.Civil") && !(v is System.Collections.IEnumerable) && !(v is DBObject))
            {
                try { d[p.Name] = Props(v, prof - 1); } catch { }
            }
            else if (v is System.Collections.IEnumerable en && !(v is string) && tn.StartsWith("Autodesk.Civil"))
            {
                var l = new List<object?>(); int k = 0;
                try { foreach (var it in en) { if (k++ > 60) break; var sv = Val(it); l.Add(sv ?? (it != null && prof > 1 ? Props(it, prof - 2) : it?.GetType().Name)); } } catch { }
                d[p.Name] = l;
            }
        }
        return d;
    }

    // PRJPONTEOBJ: propriedades (reflexao) dos objetos de ponte do Civil 3D e de outros tipos em PRJ_TIPOS
    [CommandMethod("PRJPONTEOBJ", CommandFlags.Modal)]
    public void PonteObj()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var rx = new Regex(Environment.GetEnvironmentVariable("PRJ_TIPOS") ?? "^Bridge");
        var res = new List<object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                if (!rx.IsMatch(id.ObjectClass.DxfName) && !rx.IsMatch(id.ObjectClass.Name)) { var t = tr.GetObject(id, OpenMode.ForRead); if (!rx.IsMatch(t.GetType().Name)) continue; }
                var e = tr.GetObject(id, OpenMode.ForRead) as Entity; if (e == null) continue;
                object? ext = null; try { ext = Val(e.GeometricExtents); } catch { }
                Dictionary<string, object?> pr; try { pr = Props(e, 2); } catch (System.Exception ex) { pr = new() { ["erro"] = ex.Message }; }
                res.Add(new { tipo = e.GetType().FullName, h = e.Handle.ToString(), e.Layer, ext, pr });
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_ponteobj.json"), JsonSerializer.Serialize(res));
        Msg($"PRJPONTEOBJ ok {res.Count}");
    }

    // PRJTOPOMUNDO: curvas e textos de blocos (recursivo) em coordenadas do mundo, filtrados por camada (PRJ_CAMADAS regex) e caixa (PRJ_BBOX x0,y0,x1,y1)
    [CommandMethod("PRJTOPOMUNDO", CommandFlags.Modal)]
    public void TopoMundo()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var rx = new Regex(Environment.GetEnvironmentVariable("PRJ_CAMADAS") ?? ".");
        var bb = (Environment.GetEnvironmentVariable("PRJ_BBOX") ?? "").Split(',').Where(s => s != "").Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        bool Dentro(Point3d p) => bb.Length < 4 || (p.X >= bb[0] && p.Y >= bb[1] && p.X <= bb[2] && p.Y <= bb[3]);
        var res = new List<object>(); int n = 0;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            void Andar(BlockTableRecord btr, Matrix3d xf, string layPai, string caminho, int prof)
            {
                foreach (ObjectId id in btr)
                {
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity; if (e == null) continue;
                    string lay = e.Layer == "0" && layPai != "" ? layPai : e.Layer;
                    if (e is BlockReference br)
                    {
                        if (rx.IsMatch(lay)) { try { var ex = br.GeometricExtents; var a = ex.MinPoint.TransformBy(xf); var b2 = ex.MaxPoint.TransformBy(xf); var p0 = br.Position.TransformBy(xf); if (Dentro(p0) || Dentro(a)) res.Add(new { lay, tipo = "B", nome = br.Name, p = new[] { R(p0.X), R(p0.Y), R(p0.Z) }, ext = new[] { R(a.X), R(a.Y), R(b2.X), R(b2.Y) }, caminho }); } catch { } }
                        if (prof < 5) { var sub = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead); Andar(sub, xf * br.BlockTransform, lay, caminho + "/" + sub.Name, prof + 1); }
                        continue;
                    }
                    if (!rx.IsMatch(lay)) continue;
                    try
                    {
                        if (e is DBText t) { var p = t.Position.TransformBy(xf); if (Dentro(p)) { res.Add(new { lay, tipo = "T", txt = t.TextString, p = new[] { R(p.X), R(p.Y), R(p.Z) }, caminho }); n++; } continue; }
                        if (e is MText m) { var p = m.Location.TransformBy(xf); if (Dentro(p)) { res.Add(new { lay, tipo = "T", txt = m.Text, p = new[] { R(p.X), R(p.Y), R(p.Z) }, caminho }); n++; } continue; }
                        if (e is Curve c)
                        {
                            var cw = (Curve)c.GetTransformedCopy(xf);
                            var pts = new List<double[]>();
                            if (cw is Polyline pl && pl.NumberOfVertices > 0 && !pl.HasBulges) { for (int i = 0; i < pl.NumberOfVertices; i++) { var q = pl.GetPoint3dAt(i); pts.Add(new[] { R(q.X), R(q.Y), R(q.Z) }); } if (pl.Closed) { var q = pl.GetPoint3dAt(0); pts.Add(new[] { R(q.X), R(q.Y), R(q.Z) }); } }
                            else
                            {
                                double L = cw.GetDistanceAtParameter(cw.EndParam); int k = Math.Max(1, (int)Math.Ceiling(L / 0.5));
                                for (int i = 0; i <= k; i++) { var q = cw.GetPointAtDist(Math.Min(L, L * i / k)); pts.Add(new[] { R(q.X), R(q.Y), R(q.Z) }); }
                            }
                            if (pts.Any(q => Dentro(new Point3d(q[0], q[1], 0)))) { res.Add(new { lay, tipo = cw.GetType().Name, pts, caminho }); n++; }
                            cw.Dispose();
                        }
                    }
                    catch { }
                }
            }
            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            Andar(ms, Matrix3d.Identity, "", "", 0);
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_topomundo.json"), JsonSerializer.Serialize(res));
        Msg($"PRJTOPOMUNDO ok {n}");
    }
    // PRJXDICT: conteudo dos dicionarios de extensao (XRecords, recursivo) dos objetos em PRJ_HANDLES (separados por |)
    [CommandMethod("PRJXDICT", CommandFlags.Modal)]
    public void XDict()
    {
        var db = HostApplicationServices.WorkingDatabase; var res = new List<object>();
        var hs = (Environment.GetEnvironmentVariable("PRJ_HANDLES") ?? "").Split('|').Where(x => x != "");
        using (var tr = db.TransactionManager.StartTransaction())
        {
            object Ler(ObjectId id, int prof)
            {
                var o = tr.GetObject(id, OpenMode.ForRead);
                if (o is Xrecord xr) { var l = new List<string>(); if (xr.Data != null) foreach (TypedValue tv in xr.Data) { var v = tv.Value; l.Add(tv.TypeCode + "=" + (v is Point3d p ? $"{R(p.X)},{R(p.Y)},{R(p.Z)}" : v is double dd ? R(dd).ToString(System.Globalization.CultureInfo.InvariantCulture) : v?.ToString())); if (l.Count > 400) break; } return l; }
                if (o is DBDictionary dd2 && prof < 6) { var m = new Dictionary<string, object>(); foreach (DBDictionaryEntry en in dd2) { try { m[en.Key] = Ler(en.Value, prof + 1); } catch (System.Exception ex) { m[en.Key] = "erro " + ex.Message; } } return m; }
                return o.GetType().Name;
            }
            foreach (var h in hs)
            {
                try
                {
                    var id = db.GetObjectId(false, new Handle(Convert.ToInt64(h, 16)), 0); var o = tr.GetObject(id, OpenMode.ForRead);
                    res.Add(new { h, tipo = o.GetType().Name, x = o.ExtensionDictionary.IsNull ? null : Ler(o.ExtensionDictionary, 0) });
                }
                catch (System.Exception ex) { res.Add(new { h, erro = ex.Message }); }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_xdict.json"), JsonSerializer.Serialize(res));
        Msg($"PRJXDICT ok {res.Count}");
    }
    // PRJPSETS: dados dos PropertySets (AEC) dos objetos em PRJ_HANDLES, por reflexao
    [CommandMethod("PRJPSETS", CommandFlags.Modal)]
    public void PSets()
    {
        var db = HostApplicationServices.WorkingDatabase; var res = new List<object>();
        var hs = (Environment.GetEnvironmentVariable("PRJ_HANDLES") ?? "").Split('|').Where(x => x != "");
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (var h in hs)
            {
                var id = db.GetObjectId(false, new Handle(Convert.ToInt64(h, 16)), 0); var o = tr.GetObject(id, OpenMode.ForRead);
                var sets = new List<object>();
                if (!o.ExtensionDictionary.IsNull)
                {
                    var xd = (DBDictionary)tr.GetObject(o.ExtensionDictionary, OpenMode.ForRead);
                    if (xd.Contains("AEC_PROPERTY_SETS"))
                    {
                        var pd = (DBDictionary)tr.GetObject(xd.GetAt("AEC_PROPERTY_SETS"), OpenMode.ForRead);
                        foreach (DBDictionaryEntry en in pd)
                        {
                            var ps = tr.GetObject(en.Value, OpenMode.ForRead); var t = ps.GetType(); var dados = new Dictionary<string, object?>(); string defn = "";
                            try { var did = (ObjectId)t.GetProperty("PropertySetDefinition")!.GetValue(ps)!; var def = tr.GetObject(did, OpenMode.ForRead); defn = (string)(def.GetType().GetProperty("Name")?.GetValue(def) ?? "");
                                  var defs = def.GetType().GetProperty("Definitions")?.GetValue(def) as System.Collections.IEnumerable;
                                  var gd = t.GetMethod("GetAt", new[] { typeof(int) });
                                  if (defs != null) foreach (var pdef in defs) { var nm = pdef.GetType().GetProperty("Name")?.GetValue(pdef)?.ToString() ?? "?"; var pid = (int)(pdef.GetType().GetProperty("Id")?.GetValue(pdef) ?? -1); object? v = null; try { v = gd?.Invoke(ps, new object[] { pid }); } catch (System.Exception ex) { v = "erro " + (ex.InnerException?.Message ?? ex.Message); } dados[nm] = Val(v) ?? v?.ToString(); }
                            } catch (System.Exception ex) { dados["_erro"] = ex.Message; }
                            sets.Add(new { def = defn, dados });
                        }
                    }
                }
                res.Add(new { h, tipo = o.GetType().Name, sets });
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_psets.json"), JsonSerializer.Serialize(res));
        Msg($"PRJPSETS ok {res.Count}");
    }
}