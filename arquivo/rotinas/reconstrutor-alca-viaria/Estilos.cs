using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Estilos))]

namespace Projeta.Reconstrutor;

// PRJESTILOS: lista estilos do desenho e quais estilos cada corredor / montagem / superficie / alinhamento usa.
// PRJAPLICAESTILOS: importa estilos de um modelo (PRJ_TPL) e aplica conforme PRJ_ESTILOS (JSON). Rodar em COPIA.
public class Estilos
{
    static string Nome(Transaction tr, ObjectId id) { try { return id.IsNull ? "" : ((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name; } catch { return "?"; } }
    static List<string> Nomes(Transaction tr, System.Collections.IEnumerable col) { var l = new List<string>(); foreach (var o in col) if (o is ObjectId id) l.Add(Nome(tr, id)); return l; }

    [CommandMethod("PRJESTILOS", CommandFlags.Modal)]
    public void Listar()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument; var st = cd.Styles;
        var res = new Dictionary<string, object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            res["codeSet"] = Nomes(tr, st.CodeSetStyles); res["corridor"] = Nomes(tr, st.CorridorStyles); res["assembly"] = Nomes(tr, st.AssemblyStyles);
            res["surface"] = Nomes(tr, st.SurfaceStyles); res["alignment"] = Nomes(tr, st.AlignmentStyles); res["profile"] = Nomes(tr, st.ProfileStyles);
            var cors = new List<object>();
            foreach (ObjectId id in cd.CorridorCollection)
            {
                var c = (Corridor)tr.GetObject(id, OpenMode.ForRead);
                string cs = ""; try { cs = Nome(tr, c.CodeSetStyleId); } catch (System.Exception ex) { cs = "erro " + ex.Message; }
                var sups = new List<object>(); foreach (CorridorSurface s in c.CorridorSurfaces) { try { var ts = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(s.SurfaceId, OpenMode.ForRead); sups.Add(new { s.Name, estilo = ts.StyleName, layer = ts.Layer }); } catch { } }
                cors.Add(new { c.Name, estilo = c.StyleName, codeSet = cs, layer = c.Layer, sups });
            }
            res["corredores"] = cors;
            var asms = new List<object>();
            foreach (ObjectId id in cd.AssemblyCollection)
            {
                try { var a = (Assembly)tr.GetObject(id, OpenMode.ForRead); string cs = ""; try { cs = Nome(tr, a.CodeSetStyleId); } catch { } asms.Add(new { a.Name, estilo = a.StyleName, codeSet = cs, layer = a.Layer }); } catch { }
            }
            res["montagens"] = asms;
            var als = new List<object>();
            foreach (ObjectId id in cd.GetAlignmentIds())
            {
                var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                var prs = new List<object>(); foreach (ObjectId pid in a.GetProfileIds()) { var p = (Profile)tr.GetObject(pid, OpenMode.ForRead); prs.Add(new { p.Name, estilo = p.StyleName, layer = p.Layer }); }
                als.Add(new { a.Name, estilo = a.StyleName, layer = a.Layer, prs });
            }
            res["alinhamentos"] = als;
            var sups2 = new List<object>(); foreach (ObjectId id in cd.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead); sups2.Add(new { s.Name, estilo = s.StyleName, layer = s.Layer }); }
            res["superficies"] = sups2;
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_estilos.json"), JsonSerializer.Serialize(res));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJESTILOS ok\n");
    }
}
