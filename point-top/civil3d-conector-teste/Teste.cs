// SOMENTE TESTE AUTOMÁTICO EM CÓPIA DESCARTÁVEL. Nunca carregar em desenho de trabalho:
// PTTESTEMOVER e PTTESTEPI alteram a geometria do eixo. Não faz parte do produto.
using System.IO;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CAlignment = Autodesk.Civil.DatabaseServices.Alignment;
using CAlignmentLine = Autodesk.Civil.DatabaseServices.AlignmentLine;
using CBaseline = Autodesk.Civil.DatabaseServices.Baseline;
using CCorridor = Autodesk.Civil.DatabaseServices.Corridor;
using CProfile = Autodesk.Civil.DatabaseServices.Profile;
using CProfilePVI = Autodesk.Civil.DatabaseServices.ProfilePVI;
using CProfileType = Autodesk.Civil.DatabaseServices.ProfileType;

[assembly: CommandClass(typeof(PointTop.ConectorTeste.Teste))]

namespace PointTop.ConectorTeste;

public sealed class Teste
{
    private const string Saida = @"C:\Users\User\AppData\Local\Temp\pointtop_teste\medidas.jsonl";
    private static readonly double[] Fracoes = { 0.1, 0.3, 0.5, 0.7, 0.9 };

    private static ObjectId Pegar(Editor ed, string pergunta)
    {
        var r = ed.GetEntity(new PromptEntityOptions("\n" + pergunta + ": "));
        return r.Status == PromptStatus.OK ? r.ObjectId : ObjectId.Null;
    }

    [CommandMethod("PTTESTEMEDIR", CommandFlags.Modal)]
    public void Medir()
    {
        try
        {
            var doc = App.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var etiqueta = ed.GetString("\nEtiqueta: ").StringResult;
            var idE = Pegar(ed, "Eixo");
            var idC = Pegar(ed, "Corredor");
            using var tr = doc.Database.TransactionManager.StartOpenCloseTransaction();
            var a = (CAlignment)tr.GetObject(idE, OpenMode.ForRead);
            var c = (CCorridor)tr.GetObject(idC, OpenMode.ForRead);
            double x0 = 0, y0 = 0;
            a.PointLocation(a.StartingStation, 0, ref x0, ref y0);
            var greides = new Dictionary<string, List<double[]>>();
            CProfile? terreno = null, greide = null;
            foreach (ObjectId id in a.GetProfileIds())
            {
                var p = (CProfile)tr.GetObject(id, OpenMode.ForRead);
                if (p.ProfileType == CProfileType.EG) terreno = p;
                if (p.ProfileType != CProfileType.FG) continue;
                greide = p;
                var l = new List<double[]>();
                foreach (CProfilePVI pvi in p.PVIs) l.Add(new[] { pvi.RawStation, pvi.Elevation });
                greides[p.Name] = l;
            }
            var amostras = new List<object>();
            foreach (CBaseline b in c.Baselines)
            {
                if (b.AlignmentId != idE) continue;
                var ests = b.SortedStations();
                foreach (var f in Fracoes)
                {
                    var s = ests[(int)((ests.Length - 1) * f)];
                    var aa = b.GetAppliedAssemblyAtStation(s);
                    double minO = double.MaxValue, maxO = double.MinValue, zMin = 0, zMax = 0, xMin = 0, yMin = 0;
                    var n = 0;
                    foreach (Autodesk.Civil.DatabaseServices.CalculatedLink l in aa.Links)
                        foreach (Autodesk.Civil.DatabaseServices.CalculatedPoint q in l.CalculatedPoints)
                        {
                            var w = q.StationOffsetElevationToBaseline;
                            n++;
                            if (w.Y < minO) { minO = w.Y; zMin = w.Z; xMin = q.XYZ.X; yMin = q.XYZ.Y; }
                            if (w.Y > maxO) { maxO = w.Y; zMax = w.Z; }
                        }
                    double ax = 0, ay = 0;
                    a.PointLocation(s, 0, ref ax, ref ay);
                    amostras.Add(new
                    {
                        estaca = s, eixoX = ax, eixoY = ay, pontos = n,
                        offEsq = minO, zEsq = zMin, offDir = maxO, zDir = zMax, xEsq = xMin, yEsq = yMin,
                        terreno = terreno?.ElevationAt(s), greide = greide?.ElevationAt(s),
                    });
                }
            }
            File.AppendAllText(Saida, JsonSerializer.Serialize(new { etiqueta, comprimento = a.Length, x0, y0, fim = a.EndingStation, desatualizado = c.IsOutOfDate, greides, amostras }) + Environment.NewLine);
            ed.WriteMessage("\nPTTESTEMEDIR " + etiqueta + " ok | desatualizado=" + c.IsOutOfDate + " | comprimento=" + a.Length + "\n");
        }
        catch (System.Exception ex) { App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPTTESTEMEDIR falhou: " + ex + "\n"); }
    }

    [CommandMethod("PTTESTEMOVER", CommandFlags.Modal)]
    public void Mover()
    {
        try
        {
            var doc = App.DocumentManager.MdiActiveDocument;
            var id = Pegar(doc.Editor, "Eixo");
            using var tr = doc.Database.TransactionManager.StartTransaction();
            var a = (CAlignment)tr.GetObject(id, OpenMode.ForWrite);
            a.TransformBy(Matrix3d.Displacement(new Vector3d(5, 0, 0)));
            tr.Commit();
            doc.Editor.WriteMessage("\nPTTESTEMOVER: eixo deslocado 5 m em X\n");
        }
        catch (System.Exception ex) { App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPTTESTEMOVER falhou: " + ex + "\n"); }
    }

    // Aproxima a edição real: desloca uma ponta de tangente no trecho do corredor, como quem arrasta um ponto.
    [CommandMethod("PTTESTEPI", CommandFlags.Modal)]
    public void MoverPonto()
    {
        var ed = App.DocumentManager.MdiActiveDocument.Editor;
        try
        {
            var doc = App.DocumentManager.MdiActiveDocument;
            var id = Pegar(ed, "Eixo");
            using var tr = doc.Database.TransactionManager.StartTransaction();
            var a = (CAlignment)tr.GetObject(id, OpenMode.ForWrite);
            var tipos = new List<string>();
            for (var i = 0; i < a.Entities.Count; i++)
            {
                var ent = a.Entities.GetEntityByOrder(i);
                tipos.Add(ent.EntityType + "@" + (ent is Autodesk.Civil.DatabaseServices.AlignmentCurve k ? Math.Round(k.StartStation) : -1));
            }
            ed.WriteMessage("\nPTTESTEPI: elementos " + a.Entities.Count + ": " + string.Join(" ", tipos.Take(30)) + "\n");
            var editaveis = typeof(CAlignmentLine).GetProperties().Where(q => q.CanWrite).Select(q => q.Name);
            ed.WriteMessage("\nPTTESTEPI: propriedades graváveis da tangente: " + string.Join(",", editaveis) + "\n");
            var antes = a.Length;
            for (var i = 0; i < a.Entities.Count; i++)
            {
                if (a.Entities.GetEntityByOrder(i) is not CAlignmentLine linha) continue;
                if (linha.StartStation < 300 || linha.EndStation > 2300) continue;
                try
                {
                    var p = linha.PassThroughPoint2;
                    linha.PassThroughPoint2 = new Point2d(p.X + 6, p.Y + 4);
                    a.Update();
                    tr.Commit();
                    ed.WriteMessage("\nPTTESTEPI: tangente em " + Math.Round(linha.StartStation) + " alterada (6;4) | comprimento " + antes + " -> " + a.Length + "\n");
                    return;
                }
                catch (System.Exception ex) { ed.WriteMessage("\nPTTESTEPI: tangente recusou: " + ex.Message + "\n"); }
            }
            ed.WriteMessage("\nPTTESTEPI: nenhuma tangente aceitou a edição\n");
        }
        catch (System.Exception ex) { ed.WriteMessage("\nPTTESTEPI falhou: " + ex + "\n"); }
    }
}
