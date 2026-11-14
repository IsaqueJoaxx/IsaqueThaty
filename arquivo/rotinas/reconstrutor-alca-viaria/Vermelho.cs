using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Vermelho))]

namespace Projeta.Reconstrutor;

public class Vermelho
{
    // Lista curvas vermelhas (cor da entidade ou da camada) e curvas com espessura >= 0.5 mm
    [CommandMethod("PRJVERMELHO", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var res = new List<object>();
        using var tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
        foreach (ObjectId id in ms)
        {
            if (!id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Curve)))) continue;
            if (id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Xline))) || id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Ray)))) continue;
            if (!id.ObjectClass.Name.StartsWith("AcDb")) continue;
            if (tr.GetObject(id, OpenMode.ForRead) is not Curve c) continue;
            try {
            var ltr = (LayerTableRecord)tr.GetObject(c.LayerId, OpenMode.ForRead);
            var cor = c.Color.IsByLayer ? ltr.Color : c.Color;
            System.Drawing.Color rgb = System.Drawing.Color.Black; try { rgb = cor.ColorValue; } catch { }
            bool verm = rgb.R > 180 && rgb.G < 90 && rgb.B < 90;
            var lw = c.LineWeight == LineWeight.ByLayer ? ltr.LineWeight : c.LineWeight;
            bool grossa = (int)lw >= 50;
            double len = 0; try { len = c.GetDistanceAtParameter(c.EndParam); } catch { }
            var pref = new Point3d(664033.748, 7746072.927, 0);
            bool liga = c.StartPoint.DistanceTo(pref) < 10 || c.EndPoint.DistanceTo(pref) < 10 || new Point2d(c.StartPoint.X, c.StartPoint.Y).GetDistanceTo(new Point2d(pref.X, pref.Y)) < 10 || new Point2d(c.EndPoint.X, c.EndPoint.Y).GetDistanceTo(new Point2d(pref.X, pref.Y)) < 10;
            if (!((verm && len > 100) || (grossa && len > 100) || liga)) continue;
            object geo = null;
            if (c is Polyline pl)
            {
                var v = new List<double[]>();
                for (int i = 0; i < pl.NumberOfVertices; i++) { var p = pl.GetPoint2dAt(i); v.Add(new[] { p.X, p.Y, pl.GetBulgeAt(i) }); }
                geo = v;
            }
            else
            {
                var v = new List<double[]>();
                int n = (int)Math.Ceiling(len / 2.0);
                for (int i = 0; i <= n; i++) { var p = c.GetPointAtDist(len * i / n); v.Add(new[] { p.X, p.Y, p.Z }); }
                geo = v;
            }
            res.Add(new { h = c.Handle.ToString(), tipo = c.GetType().Name, camada = c.Layer, cor = cor.ToString(), lw = lw.ToString(), comp = Math.Round(len, 3), ini = new[] { c.StartPoint.X, c.StartPoint.Y }, fim = new[] { c.EndPoint.X, c.EndPoint.Y }, geo });
            } catch { }
        }
        tr.Commit();
        File.WriteAllText(Resumo.Saida("prj_verm.json"), JsonSerializer.Serialize(res));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJVERMELHO ok {res.Count}\n");
    }
}

