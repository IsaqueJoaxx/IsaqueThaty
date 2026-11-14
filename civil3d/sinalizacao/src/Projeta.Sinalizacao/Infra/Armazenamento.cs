using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Sinalizacao.Core.Dados;

namespace Projeta.Sinalizacao.Infra;

/// <summary>Grava/lê textos (JSON) no dicionário de extensão das entidades e no dicionário do desenho.</summary>
internal static class Armazenamento
{
    public const string ChaveObjeto = "PROJETA_SIN";
    public const string Chave3D = "PROJETA_SIN_3D";
    private const int Pedaco = 240;

    private static ResultBuffer Buffer(string texto)
    {
        var rb = new ResultBuffer();
        for (int i = 0; i < texto.Length; i += Pedaco)
            rb.Add(new TypedValue((int)DxfCode.Text, texto.Substring(i, Math.Min(Pedaco, texto.Length - i))));
        if (texto.Length == 0) rb.Add(new TypedValue((int)DxfCode.Text, ""));
        return rb;
    }

    private static string Texto(ResultBuffer? rb)
    {
        if (rb is null) return "";
        var sb = new StringBuilder();
        foreach (var tv in rb.AsArray()) if (tv.TypeCode == (int)DxfCode.Text) sb.Append(tv.Value as string);
        return sb.ToString();
    }

    private static void GravarEm(Transaction tr, DBDictionary dict, string chave, string texto)
    {
        if (dict.Contains(chave))
        {
            var xr = (Xrecord)tr.GetObject(dict.GetAt(chave), OpenMode.ForWrite);
            xr.Data = Buffer(texto);
        }
        else
        {
            if (!dict.IsWriteEnabled) dict.UpgradeOpen();
            var xr = new Xrecord { Data = Buffer(texto) };
            dict.SetAt(chave, xr);
            tr.AddNewlyCreatedDBObject(xr, true);
        }
    }

    public static void GravarTexto(Transaction tr, DBObject obj, string chave, string texto)
    {
        if (obj.ExtensionDictionary.IsNull)
        {
            if (!obj.IsWriteEnabled) obj.UpgradeOpen();
            obj.CreateExtensionDictionary();
        }
        var dict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForWrite);
        GravarEm(tr, dict, chave, texto);
    }

    public static string? LerTexto(Transaction tr, DBObject obj, string chave)
    {
        if (obj.ExtensionDictionary.IsNull) return null;
        var dict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForRead);
        if (!dict.Contains(chave)) return null;
        var xr = (Xrecord)tr.GetObject(dict.GetAt(chave), OpenMode.ForRead);
        return Texto(xr.Data);
    }

    public static string? LerTextoNod(Transaction tr, Database db, string chave)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(chave)) return null;
        var xr = (Xrecord)tr.GetObject(nod.GetAt(chave), OpenMode.ForRead);
        return Texto(xr.Data);
    }

    public static void GravarTextoNod(Transaction tr, Database db, string chave, string texto)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
        GravarEm(tr, nod, chave, texto);
    }

    // ------------------------------------------------------------------ objetos de sinalização

    public static ObjetoSin? Ler(Transaction tr, DBObject obj) => ObjetoSin.DeJson(LerTexto(tr, obj, ChaveObjeto));

    public static void Gravar(Transaction tr, DBObject obj, ObjetoSin o) => GravarTexto(tr, obj, ChaveObjeto, o.ParaJson());

    /// <summary>Todos os objetos de sinalização do espaço do modelo (referência de bloco + dados).</summary>
    public static List<(ObjectId Id, ObjetoSin Obj)> Todos(Transaction tr, Database db)
    {
        var l = new List<(ObjectId, ObjetoSin)>();
        var ms = Ctx.EspacoModelo(tr, db, OpenMode.ForRead);
        foreach (ObjectId id in ms)
        {
            if (!id.ObjectClass.IsDerivedFrom(Autodesk.AutoCAD.Runtime.RXObject.GetClass(typeof(BlockReference)))) continue;
            var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
            if (br.ExtensionDictionary.IsNull) continue;
            var o = Ler(tr, br);
            if (o is not null) l.Add((id, o));
        }
        return l;
    }
}
