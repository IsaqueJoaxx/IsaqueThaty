using System.IO.Compression;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Core.Comum;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Infra;

/// <summary>
/// Guarda o projeto (redes, equações, dispositivos longitudinais) dentro do DWG, num Xrecord do
/// dicionário de objetos nomeados (chave PROJETA_DADOS), como JSON comprimido (GZip + Base64) em blocos.
/// Por ficar no DWG, o projeto acompanha o arquivo e respeita UNDO/REDO.
/// </summary>
internal static class Armazenamento
{
    private const string Chave = "PROJETA_DADOS";
    private const int TamanhoBloco = 1000;

    public static Projeto Carregar(Transaction tr, Database db)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(Chave)) return new Projeto();
        var xr = tr.GetObject(nod.GetAt(Chave), OpenMode.ForRead) as Xrecord;
        if (xr?.Data is null) return new Projeto();
        var sb = new StringBuilder();
        foreach (TypedValue tv in xr.Data.AsArray())
            if (tv.TypeCode == (short)DxfCode.Text) sb.Append((string)tv.Value);
        if (sb.Length == 0) return new Projeto();
        try
        {
            var bytes = Convert.FromBase64String(sb.ToString());
            using var ms = new MemoryStream(bytes);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var rd = new StreamReader(gz, Encoding.UTF8);
            return JsonProjeta.Desserializar<Projeto>(rd.ReadToEnd()) ?? new Projeto();
        }
        catch (Exception ex)
        {
            Ctx.Msg("[ERRO] Não foi possível ler os dados do PROJETA neste desenho: " + ex.Message);
            return new Projeto();
        }
    }

    public static void Salvar(Transaction tr, Database db, Projeto projeto)
    {
        var json = JsonProjeta.Serializar(projeto, false);
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
        Xrecord xr;
        if (nod.Contains(Chave))
        {
            xr = (Xrecord)tr.GetObject(nod.GetAt(Chave), OpenMode.ForWrite);
        }
        else
        {
            nod.UpgradeOpen();
            xr = new Xrecord();
            nod.SetAt(Chave, xr);
            tr.AddNewlyCreatedDBObject(xr, true);
        }
        xr.Data = new ResultBuffer(valores.ToArray());
    }

    /// <summary>Exporta o projeto como JSON legível (backup / troca entre desenhos).</summary>
    public static string ExportarJson(Projeto p) => JsonProjeta.Serializar(p, true);
}

/// <summary>Configurações do usuário (fora do DWG): caminhos dos motores, pasta da biblioteca.</summary>
internal sealed class Config
{
    public string? RunSwmm { get; set; }
    public string? RunEpanet { get; set; }
    public string? PastaBiblioteca { get; set; }
    public string? UltimaPasta { get; set; }
    public double AlturaTextoPadrao { get; set; } = 1.0;

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "config.json");
    private static Config? _atual;

    public static Config Atual
    {
        get
        {
            if (_atual is not null) return _atual;
            try { _atual = File.Exists(Arquivo) ? JsonProjeta.Desserializar<Config>(File.ReadAllText(Arquivo)) : null; }
            catch { _atual = null; }
            return _atual ??= new Config();
        }
    }

    public void Salvar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!);
        File.WriteAllText(Arquivo, JsonProjeta.Serializar(this));
    }
}
