using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Explorar))]

namespace Projeta.Reconstrutor;

public class Explorar
{
    [CommandMethod("PRJSUBPARAMS", CommandFlags.Modal)]
    public void SubParams()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var cd = CivilApplication.ActiveDocument;
        var nomes = new[] { "BasicLane", "BasicShoulder", "BasicBarrier", "LinkWidthAndSlope", "DaylightStandard", "DaylightGeneral", "BasicSideSlopeCutDitch", "LinkSlopeToSurface" };
        var res = new List<object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            double x = 0;
            foreach (var n in nomes)
            {
                try
                {
                    var id = cd.SubassemblyCollection.ImportStockSubassembly("T_" + n, "Subassembly." + n, new Point3d(x, 0, 0)); x += 50;
                    var sa = (Subassembly)tr.GetObject(id, OpenMode.ForRead);
                    var d = new Dictionary<string, object>();
                    foreach (var p in sa.ParamsDouble) d["D:" + p.Key + "|" + p.DisplayName] = p.Value;
                    foreach (var p in sa.ParamsLong) d["L:" + p.Key + "|" + p.DisplayName] = p.Value;
                    foreach (var p in sa.ParamsString) d["S:" + p.Key + "|" + p.DisplayName] = p.Value;
                    foreach (var p in sa.ParamsBool) d["B:" + p.Key + "|" + p.DisplayName] = p.Value;
                    var pts = new List<string>();
                    foreach (Autodesk.Civil.DatabaseServices.Point pt in sa.Points) pts.Add(string.Join(",", pt.Codes));
                    res.Add(new { n, side = sa.HasSide ? sa.Side.ToString() : "-", d, pts });
                }
                catch (System.Exception ex) { res.Add(new { n, erro = ex.ToString() }); }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_sub.json"), JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJSUBPARAMS ok\n");
    }
}
