using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Projeta.Core.Comum;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D.Infra;

/// <summary>Atalhos para documento, banco de dados, editor, layers e handles.</summary>
internal static class Ctx
{
    // Core.Application existe no Civil 3D completo e no console (accoreconsole)
    public static Document Doc => AcCore.DocumentManager.MdiActiveDocument;
    public static Database Db => Doc.Database;
    public static Editor Ed => Doc.Editor;

    public static void Msg(string s) => Ed.WriteMessage("\n" + s);

    public static void Msgs(IEnumerable<Mensagem> msgs, bool incluirInfo = true)
    {
        foreach (var m in msgs)
        {
            if (m.Gravidade == Gravidade.Info && !incluirInfo) continue;
            string pre = m.Gravidade switch { Gravidade.Erro => "[ERRO] ", Gravidade.Aviso => "[AVISO] ", _ => "" };
            Msg(pre + m);
        }
    }

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

    public static string HandleDe(ObjectId id) => id.Handle.ToString();

    public static T? Abrir<T>(Transaction tr, Database db, string? handle, OpenMode modo = OpenMode.ForRead) where T : DBObject
    {
        var id = IdDeHandle(db, handle);
        if (id.IsNull) return null;
        return tr.GetObject(id, modo, false, true) as T;
    }

    public static void Apagar(Transaction tr, Database db, string? handle)
    {
        var id = IdDeHandle(db, handle);
        if (id.IsNull) return;
        var o = tr.GetObject(id, OpenMode.ForWrite, false, true);
        if (!o.IsErased) o.Erase();
    }

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

    public static string GarantirLayer(Transaction tr, Database db, string nome, short corAci, LineWeight? espessura = null)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(nome)) return nome;
        lt.UpgradeOpen();
        var ltr = new LayerTableRecord { Name = nome, Color = Color.FromColorIndex(ColorMethod.ByAci, corAci) };
        if (espessura.HasValue) ltr.LineWeight = espessura.Value;
        lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        return nome;
    }

    public static MText Texto(string conteudo, Point3d pos, double altura, double rotacao = 0, AttachmentPoint anc = AttachmentPoint.BottomLeft)
    {
        var t = new MText();
        t.SetDatabaseDefaults();
        t.Contents = conteudo;
        t.Location = pos;
        t.TextHeight = altura;
        t.Rotation = rotacao;
        t.Attachment = anc;
        return t;
    }

    /// <summary>Converte "\n" em quebra de parágrafo do MText.</summary>
    public static string MTextLinhas(params string[] linhas) => string.Join("\\P", linhas.Select(l => l.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}")));

    public static Point3d P(double x, double y, double z = 0) => new(x, y, z);
}
