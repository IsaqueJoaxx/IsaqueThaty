using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Projeta.Giro.Core.Geometria;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Giro.Infra;

/// <summary>Atalhos para documento, banco de dados, editor, layers e entidades.</summary>
internal static class Ctx
{
    public static Document Doc => AcCore.DocumentManager.MdiActiveDocument;
    public static Database Db => Doc.Database;
    public static Editor Ed => Doc.Editor;

    public static void Msg(string s) => Ed.WriteMessage("\n" + s);

    public static BlockTableRecord EspacoModelo(Transaction tr, Database db, OpenMode modo = OpenMode.ForWrite)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        return (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], modo);
    }

    public static ObjectId Adicionar(Transaction tr, BlockTableRecord btr, Entity e)
    {
        var id = btr.AppendEntity(e);
        tr.AddNewlyCreatedDBObject(e, true);
        return id;
    }

    public static string GarantirLayer(Transaction tr, Database db, string nome, short corAci, LineWeight? espessura = null, bool plotar = true)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(nome)) return nome;
        lt.UpgradeOpen();
        var ltr = new LayerTableRecord { Name = nome, Color = Color.FromColorIndex(ColorMethod.ByAci, corAci), IsPlottable = plotar };
        if (espessura.HasValue) ltr.LineWeight = espessura.Value;
        lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        return nome;
    }

    public static MText Texto(string conteudo, Point3d pos, double altura, double rotacao = 0, AttachmentPoint anc = AttachmentPoint.MiddleCenter)
    {
        var t = new MText();
        t.SetDatabaseDefaults();
        t.Contents = conteudo.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\n", "\\P");
        t.Location = pos;
        t.TextHeight = altura;
        t.Rotation = rotacao;
        t.Attachment = anc;
        return t;
    }

    public static Polyline Pl(IReadOnlyList<V2> pts, bool fechada)
    {
        var pl = new Polyline();
        pl.SetDatabaseDefaults();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        pl.Closed = fechada;
        return pl;
    }

    public static Point3d P(V2 v, double z = 0) => new(v.X, v.Y, z);
    public static Point2d P2(V2 v) => new(v.X, v.Y);
    public static V2 V(Point3d p) => new(p.X, p.Y);

    public static ObjectId IdDeHandle(Database db, string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle)) return ObjectId.Null;
        try
        {
            long v = Convert.ToInt64(handle, 16);
            if (db.TryGetObjectId(new Handle(v), out var id) && !id.IsNull && !id.IsErased) return id;
        }
        catch (FormatException) { }
        catch (Autodesk.AutoCAD.Runtime.Exception) { }
        return ObjectId.Null;
    }

    /// <summary>Ponto do usuário (UCS) convertido para o sistema universal.</summary>
    public static Point3d Wcs(Point3d p) => p.TransformBy(Ed.CurrentUserCoordinateSystem);

    /// <summary>True quando rodando no AutoCAD/Civil 3D com interface (não no accoreconsole).</summary>
    public static bool ComInterface => _comInterface ??= !System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("accoreconsole", StringComparison.OrdinalIgnoreCase);
    private static bool? _comInterface;
}
