using System.IO.Compression;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;

namespace Projeta.Giro.Infra;

/// <summary>
/// Guarda as manobras no desenho: dicionário PROJETA_GIRO do NOD (um Xrecord por manobra, JSON compactado),
/// e marca cada entidade desenhada com XData (PROJETA_GIRO → "M:id" ou "V:id") e um grupo por manobra.
/// </summary>
internal static class Armazenamento
{
    public const string App = "PROJETA_GIRO";
    private const int Pedaco = 240;

    public static void RegistrarApp(Transaction tr, Database db)
    {
        var t = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (t.Has(App)) return;
        t.UpgradeOpen();
        var r = new RegAppTableRecord { Name = App };
        t.Add(r);
        tr.AddNewlyCreatedDBObject(r, true);
    }

    public static void Marcar(Entity e, string marca) =>
        e.XData = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, App), new TypedValue((int)DxfCode.ExtendedDataAsciiString, marca));

    public static string? Marca(Entity e)
    {
        using var rb = e.GetXDataForApplication(App);
        if (rb is null) return null;
        foreach (var tv in rb.AsArray()) if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString) return tv.Value as string;
        return null;
    }

    private static string Compactar(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal)) gz.Write(bytes, 0, bytes.Length);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string Descompactar(string b64)
    {
        using var ms = new MemoryStream(Convert.FromBase64String(b64));
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var r = new StreamReader(gz, Encoding.UTF8);
        return r.ReadToEnd();
    }

    private static DBDictionary Dicionario(Transaction tr, Database db, bool criar)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (nod.Contains(App)) return (DBDictionary)tr.GetObject(nod.GetAt(App), criar ? OpenMode.ForWrite : OpenMode.ForRead);
        if (!criar) return null!;
        nod.UpgradeOpen();
        var d = new DBDictionary();
        nod.SetAt(App, d);
        tr.AddNewlyCreatedDBObject(d, true);
        return d;
    }

    public static void Gravar(Transaction tr, Database db, Manobra m)
    {
        var d = Dicionario(tr, db, true);
        var texto = Compactar(JsonGiro.Serializar(m, false));
        var rb = new ResultBuffer();
        for (int i = 0; i < texto.Length; i += Pedaco)
            rb.Add(new TypedValue((int)DxfCode.Text, texto.Substring(i, Math.Min(Pedaco, texto.Length - i))));
        if (d.Contains(m.Id))
        {
            var xr = (Xrecord)tr.GetObject(d.GetAt(m.Id), OpenMode.ForWrite);
            xr.Data = rb;
        }
        else
        {
            var xr = new Xrecord { Data = rb };
            d.SetAt(m.Id, xr);
            tr.AddNewlyCreatedDBObject(xr, true);
        }
    }

    public static Manobra? Ler(Transaction tr, Database db, string id)
    {
        var d = Dicionario(tr, db, false);
        if (d is null || !d.Contains(id)) return null;
        var xr = (Xrecord)tr.GetObject(d.GetAt(id), OpenMode.ForRead);
        var sb = new StringBuilder();
        foreach (var tv in xr.Data.AsArray()) if (tv.TypeCode == (int)DxfCode.Text) sb.Append(tv.Value as string);
        try { return JsonGiro.Ler<Manobra>(Descompactar(sb.ToString())); }
        catch { return null; }
    }

    public static void Remover(Transaction tr, Database db, string id)
    {
        var d = Dicionario(tr, db, false);
        if (d is null || !d.Contains(id)) return;
        d.UpgradeOpen();
        var xid = d.GetAt(id);
        d.Remove(id);
        tr.GetObject(xid, OpenMode.ForWrite).Erase();
    }

    public static List<string> Ids(Transaction tr, Database db)
    {
        var d = Dicionario(tr, db, false);
        var l = new List<string>();
        if (d is null) return l;
        foreach (DBDictionaryEntry e in d) l.Add(e.Key);
        return l;
    }

    // ------------------------------------------------------------------ grupos

    public static string NomeGrupo(string prefixo, string id) => $"{prefixo}_{id}";

    public static void Agrupar(Transaction tr, Database db, string nome, string descricao, IEnumerable<ObjectId> ids)
    {
        var gd = (DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite);
        Group g;
        if (gd.Contains(nome)) g = (Group)tr.GetObject(gd.GetAt(nome), OpenMode.ForWrite);
        else
        {
            g = new Group(descricao, true);
            gd.SetAt(nome, g);
            tr.AddNewlyCreatedDBObject(g, true);
        }
        var col = new ObjectIdCollection();
        foreach (var id in ids) if (!id.IsNull && !g.Has((Entity)tr.GetObject(id, OpenMode.ForRead))) col.Add(id);
        if (col.Count > 0) g.Append(col);
    }

    /// <summary>Apaga as entidades marcadas com a marca dada (e o grupo, se houver).</summary>
    public static int ApagarMarcadas(Transaction tr, Database db, string marca, string? grupo)
    {
        int n = 0;
        var ms = Ctx.EspacoModelo(tr, db, OpenMode.ForRead);
        foreach (ObjectId id in ms)
        {
            if (id.IsErased) continue;
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity e) continue;
            if (Marca(e) != marca) continue;
            e.UpgradeOpen();
            e.Erase();
            n++;
        }
        if (grupo is not null)
        {
            var gd = (DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForRead);
            if (gd.Contains(grupo))
            {
                var g = tr.GetObject(gd.GetAt(grupo), OpenMode.ForWrite);
                gd.UpgradeOpen();
                gd.Remove(grupo);
                g.Erase();
            }
        }
        return n;
    }
}
