using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Alinhamentos))]

namespace Projeta.Reconstrutor;

public class Alinhamentos
{
    static double R(double v) => Math.Round(v, 4);
    static double[] P(Point2d p) => new[] { R(p.X), R(p.Y) };

    public static object Sub(AlignmentSubEntity s)
    {
        switch (s)
        {
            case AlignmentSubEntityLine l: return new { t = "L", e0 = R(l.StartStation), e1 = R(l.EndStation), a = P(l.StartPoint), b = P(l.EndPoint) };
            case AlignmentSubEntityArc a: return new { t = "A", e0 = R(a.StartStation), e1 = R(a.EndStation), a = P(a.StartPoint), b = P(a.EndPoint), c = P(a.CenterPoint), r = R(a.Radius), cw = a.Clockwise };
            case AlignmentSubEntitySpiral sp: return new { t = "S", e0 = R(sp.StartStation), e1 = R(sp.EndStation), a = P(sp.StartPoint), b = P(sp.EndPoint), r0 = R(sp.RadiusIn), r1 = R(sp.RadiusOut), len = R(sp.Length), A = R(sp.A), tipo = sp.SpiralDefinition.ToString() };
            default: return new { t = s.GetType().Name };
        }
    }

    [CommandMethod("PRJALIN", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var cd = CivilApplication.ActiveDocument;
        var lista = new List<object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (ObjectId aid in cd.GetAlignmentIds())
            {
                var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
                var ents = new List<object>();
                for (int i = 0; i < al.Entities.Count; i++)
                {
                    var ae = al.Entities.GetEntityByOrder(i);
                    var subs = new List<object>();
                    for (int k = 0; k < ae.SubEntityCount; k++) subs.Add(Sub(ae[k]));
                    ents.Add(new { tipo = ae.EntityType.ToString(), restricao = ae.Constraint1.ToString(), subs });
                }
                var perfis = new List<object>();
                foreach (ObjectId pid in al.GetProfileIds())
                {
                    var pr = (Profile)tr.GetObject(pid, OpenMode.ForRead);
                    var pvis = new List<object>();
                    if (pr.ProfileType != ProfileType.EG) foreach (ProfilePVI v in pr.PVIs) pvis.Add(new { e = R(v.RawStation), z = R(v.Elevation), lcv = R(v.VerticalCurve?.Length ?? 0) });
                    perfis.Add(new { pr.Name, tipo = pr.ProfileType.ToString(), pvis });
                }
                lista.Add(new { al.Name, layer = al.Layer, comp = R(al.Length), e0 = R(al.StartingStation), ents, perfis, estilo = al.StyleName });
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_alin.json"), JsonSerializer.Serialize(lista));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJALIN ok {lista.Count}\n");
    }
}
