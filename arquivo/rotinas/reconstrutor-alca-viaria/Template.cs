using System.Reflection;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Assembly = Autodesk.Civil.DatabaseServices.Assembly;
using Shape = Autodesk.Civil.DatabaseServices.Shape;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.TemplateInsp))]

namespace Projeta.Reconstrutor;

public class TemplateInsp
{
    static object Props(object o)
    {
        if (o == null) return null;
        var d = new Dictionary<string, string> { ["_tipo"] = o.GetType().FullName };
        foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            try { var v = p.GetValue(o); if (v == null || v is string || v.GetType().IsPrimitive || v is Enum) d[p.Name] = v?.ToString(); } catch { }
        }
        return d;
    }

    static List<string> Nomes(Transaction tr, dynamic col)
    {
        var l = new List<string>();
        try { foreach (ObjectId id in col) { try { var o = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Styles.StyleBase; l.Add(o?.Name ?? "?"); } catch { } } } catch (System.Exception ex) { l.Add("erro " + ex.Message); }
        return l;
    }

    [CommandMethod("PRJTEMPLATE", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        var res = new Dictionary<string, object>();
        using var tr = db.TransactionManager.StartTransaction();
        var st = cd.Styles;
        res["codeSet"] = Nomes(tr, st.CodeSetStyles);
        res["profileView"] = Nomes(tr, st.ProfileViewStyles);
        res["bandSets"] = Nomes(tr, st.ProfileViewBandSetStyles);
        res["profile"] = Nomes(tr, st.ProfileStyles);
        res["alignment"] = Nomes(tr, st.AlignmentStyles);
        res["corridor"] = Nomes(tr, st.CorridorStyles);
        res["assembly"] = Nomes(tr, st.AssemblyStyles);
        res["surface"] = Nomes(tr, st.SurfaceStyles);
        res["sectionView"] = Nomes(tr, st.SectionViewStyles);
        res["alLabelSets"] = Nomes(tr, st.LabelSetStyles.AlignmentLabelSetStyles);
        res["prLabelSets"] = Nomes(tr, st.LabelSetStyles.ProfileLabelSetStyles);
        var asms = new List<object>();
        foreach (ObjectId aid in cd.AssemblyCollection)
        {
            var a = (Assembly)tr.GetObject(aid, OpenMode.ForRead);
            var grupos = new List<object>();
            foreach (AssemblyGroup g in a.Groups)
            {
                var subs = new List<object>();
                foreach (ObjectId sid in g.GetSubassemblyIds())
                {
                    var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead);
                    var pd = new Dictionary<string, object>();
                    foreach (var p in sa.ParamsDouble) pd["D:" + p.Key] = Math.Round(p.Value, 4);
                    foreach (var p in sa.ParamsLong) pd["L:" + p.Key] = p.Value;
                    foreach (var p in sa.ParamsString) pd["S:" + p.Key] = p.Value;
                    foreach (var p in sa.ParamsBool) pd["B:" + p.Key] = p.Value;
                    var pts = new List<string>(); foreach (Autodesk.Civil.DatabaseServices.Point pt in sa.Points) pts.Add(string.Join("|", pt.Codes));
                    var lks = new List<string>(); foreach (Link lk in sa.Links) lks.Add(string.Join("|", lk.Codes));
                    var shs = new List<string>(); try { foreach (Shape sh in sa.Shapes) shs.Add(string.Join("|", sh.Codes)); } catch { }
                    subs.Add(new { sa.Name, lado = sa.HasSide ? sa.Side.ToString() : "-", composer = sa.IsFromSubassemblyComposer, gen = Props(sa.GeometryGenerator), origem = new[] { Math.Round(sa.Origin.X, 2), Math.Round(sa.Origin.Y, 2) }, hook = sa.SubassemblyHookTo.IsNull ? "" : ((Subassembly)tr.GetObject(sa.SubassemblyHookTo, OpenMode.ForRead)).Name, idx = sa.PointIndexHookTo, pd, pts, lks, shs });
                }
                grupos.Add(new { g.Name, subs });
            }
            asms.Add(new { a.Name, loc = new[] { Math.Round(a.Location.X, 2), Math.Round(a.Location.Y, 2) }, tipo = a.Type.ToString(), estilo = a.StyleName, grupos });
        }
        res["montagens"] = asms;
        // layouts e viewports
        var lays = new List<object>();
        var dic = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
        foreach (DBDictionaryEntry de in dic)
        {
            var lo = (Layout)tr.GetObject(de.Value, OpenMode.ForRead);
            var btr = (BlockTableRecord)tr.GetObject(lo.BlockTableRecordId, OpenMode.ForRead);
            var vps = new List<object>(); var txt = new List<string>();
            foreach (ObjectId id in btr)
            {
                var e = tr.GetObject(id, OpenMode.ForRead);
                if (e is Viewport vp) vps.Add(new { centro = new[] { Math.Round(vp.ViewCenter.X, 1), Math.Round(vp.ViewCenter.Y, 1) }, alt = Math.Round(vp.ViewHeight, 1), larg = Math.Round(vp.ViewHeight * vp.Width / Math.Max(vp.Height, 1e-9), 1), alvo = new[] { Math.Round(vp.ViewTarget.X, 1), Math.Round(vp.ViewTarget.Y, 1) }, tw = Math.Round(vp.TwistAngle, 3) });
                else if (e is MText mt && txt.Count < 60) txt.Add(mt.Text);
                else if (e is DBText t && txt.Count < 60) txt.Add(t.TextString);
            }
            lays.Add(new { nome = lo.LayoutName, vps, txt });
        }
        res["layouts"] = lays;
        // textos do espaco modelo perto das montagens
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
        var textos = new List<object>(); var cont = new Dictionary<string, int>();
        foreach (ObjectId id in ms)
        {
            var e = tr.GetObject(id, OpenMode.ForRead) as Entity; if (e == null) continue;
            var k = e.GetType().Name; cont[k] = cont.TryGetValue(k, out var c) ? c + 1 : 1;
            if (e is MText mt) textos.Add(new { t = mt.Text, x = Math.Round(mt.Location.X, 1), y = Math.Round(mt.Location.Y, 1), l = e.Layer });
            else if (e is DBText t) textos.Add(new { t = t.TextString, x = Math.Round(t.Position.X, 1), y = Math.Round(t.Position.Y, 1), l = e.Layer });
        }
        res["ms_tipos"] = cont; res["ms_textos"] = textos.Take(400).ToList();
        tr.Commit();
        File.WriteAllText(Resumo.Saida("prj_tpl.json"), JsonSerializer.Serialize(res, new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJTEMPLATE ok\n");
    }
}
