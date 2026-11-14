using System.Net;
using System.Net.Http.Headers;

namespace Projeta.Core.Geoespacial;

/// <summary>
/// Lê um arquivo na internet por faixas de bytes (HTTP Range), guardando cada faixa baixada numa pasta de cache:
/// a segunda vez que a mesma área é pedida não baixa nada.
/// </summary>
public sealed class FonteHttp : IFonteBytes
{
    private static readonly HttpClient _http = Criar();
    private readonly string _url, _cache;

    public FonteHttp(string url, string pastaCache)
    {
        _url = url;
        _cache = pastaCache;
        Directory.CreateDirectory(_cache);
    }

    private static HttpClient Criar()
    {
        var h = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None }) { Timeout = TimeSpan.FromSeconds(90) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("PROJETA-Civil3D/1.0");
        return h;
    }

    /// <summary>Bytes baixados da internet nesta fonte (o que veio do cache não conta).</summary>
    public long Baixados { get; private set; }

    public byte[] Ler(long pos, int n)
    {
        if (n <= 0) return Array.Empty<byte>();
        var arq = Path.Combine(_cache, $"{pos}_{n}.bin");
        if (File.Exists(arq)) return File.ReadAllBytes(arq);
        using var req = new HttpRequestMessage(HttpMethod.Get, _url);
        req.Headers.Range = new RangeHeaderValue(pos, pos + n - 1);
        using var resp = _http.Send(req, HttpCompletionOption.ResponseHeadersRead);
        if (resp.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable) return Array.Empty<byte>();
        if (resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.Forbidden)
            throw new FileNotFoundException("Arquivo não existe no servidor: " + _url);
        resp.EnsureSuccessStatusCode();
        using var s = resp.Content.ReadAsStream();
        using var ms = new MemoryStream();
        if (resp.StatusCode == HttpStatusCode.PartialContent) s.CopyTo(ms);
        else
        {
            // servidor sem leitura por partes: só aceita se o pedaço está no começo do arquivo (não baixa arquivos enormes inteiros)
            if (pos + n > 16 * 1024 * 1024) throw new InvalidOperationException("O servidor não aceita leitura por partes (HTTP Range): " + _url);
            var buf = new byte[81920];
            long lidos = 0;
            int k;
            while (lidos < pos + n && (k = s.Read(buf, 0, buf.Length)) > 0)
            {
                long ini = Math.Max(pos - lidos, 0), fim = Math.Min(k, pos + n - lidos);
                if (fim > ini) ms.Write(buf, (int)ini, (int)(fim - ini));
                lidos += k;
            }
        }
        var b = ms.ToArray();
        Baixados += b.Length;
        try { File.WriteAllBytes(arq, b); } catch { }
        return b;
    }

    public void Dispose() { }
}

/// <summary>Ponto de relevo em coordenadas geográficas.</summary>
public readonly record struct PontoRelevo(double Lat, double Lon, double Z);

/// <summary>Resultado da leitura de relevo de uma área.</summary>
public sealed class RelevoArea
{
    public List<PontoRelevo> Pontos { get; } = new();
    public List<string> Ladrilhos { get; } = new();
    public List<string> Ausentes { get; } = new();
    public long BytesBaixados { get; set; }
    public double PassoGraus { get; set; }
    public string Fonte { get; set; } = "";
    public string Atribuicao { get; set; } = "";
}

/// <summary>Ladrilho de um modelo de relevo: nome e o retângulo geográfico que ele "possui" (sem a borda de sobreposição).</summary>
public readonly record struct LadrilhoRelevo(string Nome, double LatMin, double LatMax, double LonMin, double LonMax);

/// <summary>
/// Modelo digital de elevação público distribuído em ladrilhos GeoTIFF/COG geográficos (WGS 84), lido pela internet por
/// faixas HTTP: só os blocos internos que cobrem a área são baixados, e ficam guardados numa pasta de cache.
/// </summary>
public sealed class ModeloRelevo
{
    public string Id { get; init; } = "";
    public string Nome { get; init; } = "";
    /// <summary>true = modelo de TERRENO (MDT, sem a vegetação); false = de SUPERFÍCIE (MDS).</summary>
    public bool Terreno { get; init; }
    public string Atribuicao { get; init; } = "";
    /// <summary>Retângulo de cobertura (lat mín, lat máx, lon mín, lon máx).</summary>
    public (double LatMin, double LatMax, double LonMin, double LonMax) Cobertura { get; init; } = (-90, 90, -180, 180);
    public Func<double, double, double, double, IEnumerable<LadrilhoRelevo>> Ladrilhos { get; init; } = (_, _, _, _) => Array.Empty<LadrilhoRelevo>();
    public Func<string, string> Url { get; init; } = n => n;

    public string PastaCache => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Projeta", "relevo", Id);

    public bool Cobre(double latMin, double latMax, double lonMin, double lonMax) =>
        latMin >= Cobertura.LatMin && latMax <= Cobertura.LatMax && lonMin >= Cobertura.LonMin && lonMax <= Cobertura.LonMax;

    /// <summary>
    /// Pontos do modelo dentro do retângulo geográfico (os centros de todos os pixels), juntando os ladrilhos necessários.
    /// Cada ponto vem só do ladrilho que "possui" aquela posição, então as bordas sobrepostas não duplicam pontos.
    /// <paramref name="abrir"/> permite trocar a origem dos ladrilhos (testes ou arquivos locais).
    /// </summary>
    public RelevoArea Ler(double latMin, double latMax, double lonMin, double lonMax, string? pastaCache = null,
                          Func<string, IFonteBytes>? abrir = null, Action<string>? progresso = null, int maxPontos = 4_000_000)
    {
        if (latMin > latMax) (latMin, latMax) = (latMax, latMin);
        if (lonMin > lonMax) (lonMin, lonMax) = (lonMax, lonMin);
        if (latMin < -90 || latMax > 90) throw new ArgumentException("Latitude fora de −90..90.");
        pastaCache ??= PastaCache;
        var r = new RelevoArea { Fonte = Nome, Atribuicao = Atribuicao };
        double estimado = (latMax - latMin) * 3700 * (lonMax - lonMin) * 3700;
        if (estimado > maxPontos) throw new ArgumentException($"A área pede cerca de {estimado:N0} pontos; o limite é {maxPontos:N0}. Divida a área.");
        foreach (var lad in Ladrilhos(latMin, latMax, lonMin, lonMax))
        {
            progresso?.Invoke($"Lendo {lad.Nome}...");
            IFonteBytes? fonte = null;
            FonteHttp? http = null;
            try
            {
                if (abrir is not null) fonte = abrir(lad.Nome);
                else fonte = http = new FonteHttp(Url(lad.Nome), Path.Combine(pastaCache, lad.Nome));
                using var g = GeoTiff.Abrir(fonte, 32768);
                // só a parte da área que este ladrilho possui
                double a0 = Math.Max(latMin, lad.LatMin), a1 = Math.Min(latMax, lad.LatMax), o0 = Math.Max(lonMin, lad.LonMin), o1 = Math.Min(lonMax, lad.LonMax);
                if (a0 > a1 || o0 > o1) continue;
                var (cA, lA) = g.Pixel(o0, a1);
                var (cB, lB) = g.Pixel(o1, a0);
                int c0 = Math.Max(0, (int)Math.Floor(Math.Min(cA, cB))), c1 = Math.Min(g.Largura - 1, (int)Math.Ceiling(Math.Max(cA, cB)));
                int l0 = Math.Max(0, (int)Math.Floor(Math.Min(lA, lB))), l1 = Math.Min(g.Altura - 1, (int)Math.Ceiling(Math.Max(lA, lB)));
                if (c0 > c1 || l0 > l1) continue;
                var z = g.LerJanela(c0, l0, c1 - c0 + 1, l1 - l0 + 1);
                r.PassoGraus = Math.Max(r.PassoGraus, g.Dy);
                bool ultimaLat = lad.LatMax >= Cobertura.LatMax, ultimaLon = lad.LonMax >= Cobertura.LonMax;
                for (int l = 0; l <= l1 - l0; l++)
                    for (int c = 0; c <= c1 - c0; c++)
                    {
                        var (x, y) = g.Centro(c0 + c, l0 + l);
                        if (y < latMin || y > latMax || x < lonMin || x > lonMax) continue;
                        // posse semiaberta [mín, máx): o limite comum fica com um ladrilho só
                        if (y < lad.LatMin || (y >= lad.LatMax && !(ultimaLat && y <= lad.LatMax))) continue;
                        if (x < lad.LonMin || (x >= lad.LonMax && !(ultimaLon && x <= lad.LonMax))) continue;
                        float v = z[l, c];
                        if (float.IsNaN(v) || v < -1000) continue;
                        r.Pontos.Add(new PontoRelevo(y, x, v));
                    }
                r.Ladrilhos.Add(lad.Nome);
            }
            catch (FileNotFoundException) { r.Ausentes.Add(lad.Nome); }
            finally
            {
                if (http is not null) r.BytesBaixados += http.Baixados;
                fonte?.Dispose();
            }
        }
        return r;
    }

    // ------------------------------------------------------------------ modelos disponíveis

    /// <summary>
    /// Copernicus DEM GLO-30 (30 m, mundial, uso livre inclusive comercial com a atribuição), dos COGs públicos do programa
    /// Copernicus na AWS. Modelo de SUPERFÍCIE (copas de árvores e prédios), altitudes no geoide EGM2008.
    /// Ladrilhos de 1° × 1° nomeados pelo canto sudoeste, pixel como ponto (a linha de cima é a latitude inteira de cima).
    /// </summary>
    public static readonly ModeloRelevo Copernicus = new()
    {
        Id = "copernicus30",
        Nome = "Copernicus DEM GLO-30 (30 m)",
        Terreno = false,
        Atribuicao = Geoespacial.Copernicus.Atribuicao,
        Ladrilhos = (la0, la1, lo0, lo1) =>
        {
            var l = new List<LadrilhoRelevo>();
            for (int la = (int)Math.Floor(la0); la <= (int)Math.Floor(la1); la++)
                for (int lo = (int)Math.Floor(lo0); lo <= (int)Math.Floor(lo1); lo++)
                    l.Add(new LadrilhoRelevo(Geoespacial.Copernicus.Ladrilho(la, lo), la - 1e-9, la + 1 + 1e-9, lo, lo + 1));
            return l;
        },
        Url = Geoespacial.Copernicus.Url,
    };

    /// <summary>
    /// ANADEM v1.0 — Modelo Digital de TERRENO da América do Sul (ANA/UFRGS), o Copernicus GLO-30 com o viés da vegetação
    /// removido; ~30 m, ladrilhos COG por zona da grade MGRS (6° × 8°, com pequena borda de sobreposição), no servidor da ANA.
    /// </summary>
    public static readonly ModeloRelevo Anadem = new()
    {
        Id = "anadem",
        Nome = "ANADEM v1.0 (30 m, terreno)",
        Terreno = true,
        Atribuicao = "ANADEM v1.0 – Modelo Digital de Terreno para a América do Sul (ANA/UFRGS; Laipelt et al., 2024, Remote Sensing 16(13):2321), derivado do Copernicus DEM GLO-30",
        Cobertura = (-56.6, 14.1, -82.6, -34.7),
        Ladrilhos = (la0, la1, lo0, lo1) =>
        {
            var l = new List<LadrilhoRelevo>();
            int z0 = SistemaUtm.FusoDe(lo0), z1 = SistemaUtm.FusoDe(lo1);
            int b0 = Math.Clamp((int)Math.Floor((la0 + 80) / 8), 0, 19), b1 = Math.Clamp((int)Math.Floor((la1 + 80) / 8), 0, 19);
            for (int b = b0; b <= b1; b++)
                for (int z = z0; z <= z1; z++)
                    l.Add(new LadrilhoRelevo(AnademLadrilho(z, b), -80 + 8 * b, -80 + 8 * (b + 1), -180 + 6 * (z - 1), -180 + 6 * z));
            return l;
        },
        Url = n => $"https://metadados.snirh.gov.br/files/anadem_v1_tiles/{n}.tif",
    };

    private const string BandasMgrs = "CDEFGHJKLMNPQRSTUVWX";

    /// <summary>Nome do ladrilho ANADEM da zona MGRS (fuso + letra da faixa de 8° de latitude).</summary>
    public static string AnademLadrilho(int fuso, int banda) => $"anadem_v1_{fuso}{BandasMgrs[Math.Clamp(banda, 0, 19)]}";

    /// <summary>Ladrilho ANADEM que contém o ponto.</summary>
    public static string AnademLadrilhoDe(double lat, double lon) => AnademLadrilho(SistemaUtm.FusoDe(lon), (int)Math.Floor((lat + 80) / 8));

    public static IReadOnlyList<ModeloRelevo> Todos => new[] { Anadem, Copernicus };

    public static ModeloRelevo? DoId(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : Todos.FirstOrDefault(m => m.Id.StartsWith(id.Trim(), StringComparison.OrdinalIgnoreCase) || m.Nome.StartsWith(id.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>Atalhos do Copernicus DEM GLO-30 (nomes e endereços dos ladrilhos).</summary>
public static class Copernicus
{
    public const string Atribuicao = "Copernicus DEM GLO-30 © DLR e.V. 2010-2014 e © Airbus Defence and Space GmbH 2014-2018, fornecido no âmbito do programa COPERNICUS pela União Europeia e pela ESA";
    public const string UrlBase = "https://copernicus-dem-30m.s3.amazonaws.com";

    /// <summary>Nome do ladrilho de 1° × 1° cujo canto sudoeste é (latSul, lonOeste).</summary>
    public static string Ladrilho(int latSul, int lonOeste) =>
        $"Copernicus_DSM_COG_10_{(latSul < 0 ? "S" : "N")}{Math.Abs(latSul):00}_00_{(lonOeste < 0 ? "W" : "E")}{Math.Abs(lonOeste):000}_00_DEM";

    public static string Url(string ladrilho) => $"{UrlBase}/{ladrilho}/{ladrilho}.tif";

    public static string PastaCachePadrao => ModeloRelevo.Copernicus.PastaCache;

    public static RelevoArea Ler(double latMin, double latMax, double lonMin, double lonMax, string? pastaCache = null,
                                 Func<string, IFonteBytes>? abrir = null, Action<string>? progresso = null, int maxPontos = 4_000_000)
        => ModeloRelevo.Copernicus.Ler(latMin, latMax, lonMin, lonMax, pastaCache, abrir, progresso, maxPontos);
}
