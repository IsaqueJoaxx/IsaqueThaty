using System.Text.Json;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Geometria))]

namespace Projeta.Reconstrutor;

public class Geometria
{
    static double R(double v) => Math.Round(v, 4);

    public static object Ent(Transaction tr, Entity e, Matrix3d? xf)
    {
        string tipo = Resumo.Tipo(e);
        object g = null;
        try
        {
            switch (e)
            {
                case Polyline pl:
                    {
                        var v = new List<double[]>();
                        for (int i = 0; i < pl.NumberOfVertices; i++) { var p = pl.GetPoint3dAt(i); if (xf.HasValue) p = p.TransformBy(xf.Value); v.Add(new[] { R(p.X), R(p.Y), R(pl.GetBulgeAt(i)) }); }
                        g = new { v, z = R(pl.Elevation), fechada = pl.Closed };
                        break;
                    }
                case Polyline3d p3:
                    {
                        var v = new List<double[]>();
                        foreach (ObjectId vid in p3) { var vx = (PolylineVertex3d)tr.GetObject(vid, OpenMode.ForRead); var p = vx.Position; if (xf.HasValue) p = p.TransformBy(xf.Value); v.Add(new[] { R(p.X), R(p.Y), R(p.Z) }); }
                        g = new { v };
                        break;
                    }
                case Line ln:
                    {
                        var a = ln.StartPoint; var b = ln.EndPoint; if (xf.HasValue) { a = a.TransformBy(xf.Value); b = b.TransformBy(xf.Value); }
                        g = new { a = new[] { R(a.X), R(a.Y), R(a.Z) }, b = new[] { R(b.X), R(b.Y), R(b.Z) } };
                        break;
                    }
                case Arc ar:
                    g = new { c = new[] { R(ar.Center.X), R(ar.Center.Y), R(ar.Center.Z) }, r = R(ar.Radius), a0 = R(ar.StartAngle), a1 = R(ar.EndAngle) };
                    break;
                case Circle ci:
                    g = new { c = new[] { R(ci.Center.X), R(ci.Center.Y), R(ci.Center.Z) }, r = R(ci.Radius) };
                    break;
                case DBText t:
                    g = new { t = t.TextString, p = new[] { R(t.Position.X), R(t.Position.Y) }, al = new[] { R(t.AlignmentPoint.X), R(t.AlignmentPoint.Y) }, h = R(t.Height), rot = R(t.Rotation) };
                    break;
                case MText mt:
                    g = new { t = mt.Text, p = new[] { R(mt.Location.X), R(mt.Location.Y) }, h = R(mt.TextHeight), rot = R(mt.Rotation), at = (int)mt.Attachment };
                    break;
                case BlockReference br:
                    {
                        var at = new List<string>();
                        foreach (ObjectId aid in br.AttributeCollection) { try { var ar2 = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead); at.Add(ar2.Tag + "=" + ar2.TextString); } catch { } }
                        g = new { nome = br.Name, p = new[] { R(br.Position.X), R(br.Position.Y), R(br.Position.Z) }, rot = R(br.Rotation), esc = R(br.ScaleFactors.X), at };
                        break;
                    }
                case Face f:
                    {
                        var v = new List<double[]>();
                        for (short i = 0; i < 4; i++) { var p = f.GetVertexAt(i); v.Add(new[] { R(p.X), R(p.Y), R(p.Z) }); }
                        g = new { v };
                        break;
                    }
            }
        }
        catch (System.Exception ex) { g = new { erro = ex.Message }; }
        return new { h = e.Handle.ToString(), l = e.Layer, tipo, g };
    }

    // Exporta as entidades do espaco modelo cujas camadas casam com PRJ_CAMADAS (regex).
    // PRJ_BLOCOS (regex): tambem entra nas definicoes desses blocos (com a transformacao da insercao).
    [CommandMethod("PRJGEOM", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var reCam = new Regex(Environment.GetEnvironmentVariable("PRJ_CAMADAS") ?? ".*", RegexOptions.IgnoreCase);
        var blk = Environment.GetEnvironmentVariable("PRJ_BLOCOS");
        var reBlk = string.IsNullOrEmpty(blk) ? null : new Regex(blk, RegexOptions.IgnoreCase);
        var lista = new List<object>();
        using var prog = new StreamWriter(Resumo.Saida("x") + ".prog") { AutoFlush = true };
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                Entity e;
                try { e = tr.GetObject(id, OpenMode.ForRead) as Entity; } catch { continue; }
                if (e == null) continue;
                prog.WriteLine(e.Handle + " " + e.GetType().Name + " " + e.Layer);
                if (e is BlockReference br && reBlk != null && reBlk.IsMatch(br.Name))
                {
                    var def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                    foreach (ObjectId sid in def)
                    {
                        prog.WriteLine("  sub " + sid.Handle);
                        try { var se = tr.GetObject(sid, OpenMode.ForRead) as Entity; if (se != null && reCam.IsMatch(se.Layer)) lista.Add(new { bloco = br.Name, e = Ent(tr, se, br.BlockTransform) }); } catch { }
                    }
                    continue;
                }
                if (reCam.IsMatch(e.Layer)) lista.Add(Ent(tr, e, null));
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_geom.json"), JsonSerializer.Serialize(lista));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJGEOM ok {lista.Count}\n");
    }
}
