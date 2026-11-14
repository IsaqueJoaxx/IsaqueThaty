using System.Text.Json;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Desenhar))]

namespace Projeta.Reconstrutor;

// PRJDESENHAR: le PRJ_DESENHO (JSON) e cria entidades no espaco modelo (rodar em COPIA do DWG).
// {camadas:[{nome,cor,lt}], novos:[{tipo:"pl"|"circ", layer, v:[[x,y,b]], fechada, c:[x,y], r, lt, lts}], props:[{h, layer, lt, lts, bylayer}]}
public class Desenhar
{
    static ObjectId Lt(Database db, Transaction tr, string nome)
    {
        var lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
        if (!lt.Has(nome)) { try { db.LoadLineTypeFile(nome, "acadiso.lin"); } catch { } }
        lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
        return lt.Has(nome) ? lt[nome] : db.ContinuousLinetype;
    }

    [CommandMethod("PRJDESENHAR", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase;
        var doc = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_DESENHO"))).RootElement;
        int n = 0, p = 0; var log = new List<string>(); var criados = new List<string>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
            foreach (var c in doc.GetProperty("camadas").EnumerateArray())
            {
                string nome = c.GetProperty("nome").GetString();
                if (lt.Has(nome)) continue;
                var ltr = new LayerTableRecord { Name = nome, Color = Color.FromColorIndex(ColorMethod.ByAci, (short)c.GetProperty("cor").GetInt32()) };
                if (c.TryGetProperty("lt", out var l) && l.ValueKind == JsonValueKind.String) ltr.LinetypeObjectId = Lt(db, tr, l.GetString());
                if (c.TryGetProperty("naoplota", out var np) && np.GetBoolean()) ltr.IsPlottable = false;
                lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
            }
            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
            foreach (var e in doc.GetProperty("novos").EnumerateArray())
            {
                try
                {
                    Entity ent;
                    string tipo = e.GetProperty("tipo").GetString();
                    if (tipo == "circ")
                    {
                        var c = e.GetProperty("c").EnumerateArray().Select(x => x.GetDouble()).ToArray();
                        ent = new Circle(new Point3d(c[0], c[1], 0), Vector3d.ZAxis, e.GetProperty("r").GetDouble());
                    }
                    else
                    {
                        var pl = new Polyline(); int i = 0;
                        foreach (var v in e.GetProperty("v").EnumerateArray()) { var a = v.EnumerateArray().Select(x => x.GetDouble()).ToArray(); pl.AddVertexAt(i++, new Point2d(a[0], a[1]), a.Length > 2 ? a[2] : 0, 0, 0); }
                        pl.Closed = e.TryGetProperty("fechada", out var f) && f.GetBoolean();
                        if (e.TryGetProperty("w", out var w)) pl.ConstantWidth = w.GetDouble();
                        ent = pl;
                    }
                    ent.SetDatabaseDefaults(db);
                    ent.Layer = e.GetProperty("layer").GetString();
                    ent.ColorIndex = 256;
                    if (e.TryGetProperty("lt", out var l) && l.ValueKind == JsonValueKind.String) ent.LinetypeId = Lt(db, tr, l.GetString());
                    if (e.TryGetProperty("lts", out var s)) ent.LinetypeScale = s.GetDouble();
                    ms.AppendEntity(ent); tr.AddNewlyCreatedDBObject(ent, true); n++; criados.Add(ent.Handle.ToString());
                }
                catch (System.Exception ex) { log.Add("novo: " + ex.Message); }
            }
            if (doc.TryGetProperty("props", out var props))
                foreach (var op in props.EnumerateArray())
                {
                    try
                    {
                        var id = db.GetObjectId(false, new Handle(Convert.ToInt64(op.GetProperty("h").GetString(), 16)), 0);
                        var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        if (op.TryGetProperty("layer", out var la)) ent.Layer = la.GetString();
                        if (op.TryGetProperty("bylayer", out var bl) && bl.GetBoolean()) { ent.ColorIndex = 256; ent.Linetype = "ByLayer"; }
                        if (op.TryGetProperty("lt", out var l) && l.ValueKind == JsonValueKind.String) ent.LinetypeId = Lt(db, tr, l.GetString());
                        if (op.TryGetProperty("lts", out var s)) ent.LinetypeScale = s.GetDouble();
                        p++;
                    }
                    catch (System.Exception ex) { log.Add("props: " + ex.Message); }
                }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_desenhar.json"), JsonSerializer.Serialize(new { novos = n, props = p, criados, log, ltscale = db.Ltscale, msltscale = db.MsLtScale }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJDESENHAR novos={n} props={p} erros={log.Count}\n");
    }
}
