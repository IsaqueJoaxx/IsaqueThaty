using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Montagens))]

namespace Projeta.Reconstrutor;

// PRJMONTAGENS -> lista montagens, subassemblies (lado, nome, parametros numericos) e corredores (regioes)
public class Montagens
{
    [CommandMethod("PRJMONTAGENS", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var cd = CivilApplication.ActiveDocument;
        var res = new List<object>(); var cors = new List<object>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (ObjectId aid in cd.AssemblyCollection)
            {
                try
                {
                    var asm = (Assembly)tr.GetObject(aid, OpenMode.ForRead);
                    var subs = new List<object>();
                    foreach (AssemblyGroup g in asm.Groups)
                        foreach (ObjectId sid in g.GetSubassemblyIds())
                        {
                            var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead);
                            var d = new Dictionary<string, double>();
                            foreach (var p in sa.ParamsDouble) d[p.Key] = Math.Round(p.Value, 4);
                            var s = new Dictionary<string, string>();
                            foreach (var p in sa.ParamsString) s[p.Key] = p.Value;
                            subs.Add(new { sa.Name, lado = sa.HasSide ? sa.Side.ToString() : "-", macro = sa.GeometryGenerator?.MacroOrClassName, d, s });
                        }
                    res.Add(new { asm.Name, layer = asm.Layer, subs });
                }
                catch (System.Exception ex) { res.Add(new { erro = ex.Message }); }
            }
            foreach (ObjectId cid in cd.CorridorCollection)
            {
                try
                {
                    var c = (Corridor)tr.GetObject(cid, OpenMode.ForRead);
                    var bls = new List<object>();
                    foreach (Baseline bl in c.Baselines)
                    {
                        var regs = new List<object>();
                        foreach (BaselineRegion r in bl.BaselineRegions) regs.Add(new { r.Name, e0 = Math.Round(r.StartStation, 2), e1 = Math.Round(r.EndStation, 2), montagem = ((Assembly)tr.GetObject(r.AssemblyId, OpenMode.ForRead)).Name });
                        string al = ""; try { al = ((Alignment)tr.GetObject(bl.AlignmentId, OpenMode.ForRead)).Name; } catch { }
                        bls.Add(new { bl.Name, al, regs });
                    }
                    cors.Add(new { c.Name, layer = c.Layer, bls });
                }
                catch (System.Exception ex) { cors.Add(new { erro = ex.Message }); }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_montagens.json"), JsonSerializer.Serialize(new { montagens = res, corredores = cors }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJMONTAGENS ok {res.Count} {cors.Count}\n");
    }
}
