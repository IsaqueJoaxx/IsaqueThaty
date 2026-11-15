using System.IO.Compression;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;

namespace Projeta.Civil3D.Infra;

/// <summary>
/// Guarda no DWG a geometria de cada interseção e rotatória (para o PRJSINALIZA) e o que a sinalização desenhou: um Xrecord por
/// item no dicionário PROJETA_SINALIZACAO do dicionário de objetos nomeados, com JSON comprimido (GZip + Base64) em blocos.
/// Fica no DWG, então acompanha o arquivo e respeita UNDO/REDO.
/// </summary>
internal static class ArmazenamentoSinal
{
    private const string Dicionario = "PROJETA_SINALIZACAO";
    private const int TamanhoBloco = 1000;

    private static string Chave(string nome)
    {
        var sb = new StringBuilder("S_");
        foreach (char c in nome.Normalize(NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToUpperInvariant(c) : '_');
        }
        return sb.ToString();
    }

    public static List<ItemSinalizacao> Ler(Transaction tr, Database db)
    {
        var r = new List<ItemSinalizacao>();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(Dicionario)) return r;
        var dic = (DBDictionary)tr.GetObject(nod.GetAt(Dicionario), OpenMode.ForRead);
        foreach (DBDictionaryEntry en in dic)
        {
            if (tr.GetObject(en.Value, OpenMode.ForRead) is not Xrecord xr || xr.Data is null) continue;
            var sb = new StringBuilder();
            foreach (TypedValue tv in xr.Data.AsArray())
                if (tv.TypeCode == (short)DxfCode.Text) sb.Append((string)tv.Value);
            try
            {
                using var ms = new MemoryStream(Convert.FromBase64String(sb.ToString()));
                using var gz = new GZipStream(ms, CompressionMode.Decompress);
                using var rd = new StreamReader(gz, Encoding.UTF8);
                if (JsonProjeta.Desserializar<ItemSinalizacao>(rd.ReadToEnd()) is { } it) r.Add(it);
            }
            catch (Exception ex) { Ctx.Msg($"[AVISO] Dados de sinalização '{en.Key}' ilegíveis: {ex.Message}"); }
        }
        return r.OrderBy(x => x.Tipo).ThenBy(x => x.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public static void Gravar(Transaction tr, Database db, ItemSinalizacao item)
    {
        var json = JsonProjeta.Serializar(item, false);
        string b64;
        using (var ms = new MemoryStream())
        {
            using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                gz.Write(bytes, 0, bytes.Length);
            }
            b64 = Convert.ToBase64String(ms.ToArray());
        }
        var valores = new List<TypedValue>();
        for (int i = 0; i < b64.Length; i += TamanhoBloco)
            valores.Add(new TypedValue((int)DxfCode.Text, b64.Substring(i, Math.Min(TamanhoBloco, b64.Length - i))));

        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        DBDictionary dic;
        if (nod.Contains(Dicionario)) dic = (DBDictionary)tr.GetObject(nod.GetAt(Dicionario), OpenMode.ForWrite);
        else
        {
            nod.UpgradeOpen();
            dic = new DBDictionary();
            nod.SetAt(Dicionario, dic);
            tr.AddNewlyCreatedDBObject(dic, true);
        }
        string chave = Chave(item.Nome);
        Xrecord xr;
        if (dic.Contains(chave)) xr = (Xrecord)tr.GetObject(dic.GetAt(chave), OpenMode.ForWrite);
        else
        {
            xr = new Xrecord();
            dic.SetAt(chave, xr);
            tr.AddNewlyCreatedDBObject(xr, true);
        }
        xr.Data = new ResultBuffer(valores.ToArray());
    }
}
