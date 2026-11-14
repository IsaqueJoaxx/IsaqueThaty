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
        var h = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None }) { Timeout = TimeSpan.FromSeconds(60) };
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
            // servidor sem suporte a Range: lê até a faixa pedida e descarta o resto
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

/// <summary>
/// Modelo Copernicus DEM GLO-30 (30 m, cobertura mundial, uso livre inclusive comercial com a atribuição abaixo),
/// lido direto dos COGs públicos do programa Copernicus na AWS (registro de dados abertos), por faixas HTTP.
/// É um modelo de SUPERFÍCIE (inclui copas de árvores e prédios), com altitudes ortométricas no geoide EGM2008.
/// </summary>
public static class Copernicus
{
    public const string Atribuicao = "Copernicus DEM GLO-30 © DLR e.V. 2010-2014 e © Airbus Defence and Space GmbH 2014-2018, fornecido no âmbito do programa COPERNICUS pela União Europeia e pela ESA";
    public const string UrlBase = "https://copernicus-dem-30m.s3.amazonaws.com";

    /// <summary>Nome do ladrilho de 1° × 1° cujo canto sudoeste é (latSul, lonOeste).</summary>
    public static string Ladrilho(int latSul, int lonOeste) =>
        $"Copernicus_DSM_COG_10_{(latSul < 0 ? "S" : "N")}{Math.Abs(latSul):00}_00_{(lonOeste < 0 ? "W" : "E")}{Math.Abs(lonOeste):000}_00_DEM";

    public static string Url(string ladrilho) => $"{UrlBase}/{ladrilho}/{ladrilho}.tif";

    public static string PastaCachePadrao => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Projeta", "relevo", "copernicus30");

    /// <summary>
    /// Pontos do modelo dentro do retângulo geográfico (todos os nós da grade de 1"), juntando os ladrilhos necessários.
    /// <paramref name="abrir"/> permite trocar a origem dos ladrilhos (testes ou arquivos locais).
    /// </summary>
    public static RelevoArea Ler(double latMin, double latMax, double lonMin, double lonMax, string? pastaCache = null,
                                 Func<string, IFonteBytes>? abrir = null, Action<string>? progresso = null, int maxPontos = 4_000_000)
    {
        if (latMin > latMax) (latMin, latMax) = (latMax, latMin);
        if (lonMin > lonMax) (lonMin, lonMax) = (lonMax, lonMin);
        if (latMin < -90 || latMax > 90) throw new ArgumentException("Latitude fora de −90..90.");
        pastaCache ??= PastaCachePadrao;
        var r = new RelevoArea { Fonte = "Copernicus DEM GLO-30 (30 m)", Atribuicao = Atribuicao };
        double estimado = (latMax - latMin) * 3600 * (lonMax - lonMin) * 3600;
        if (estimado > maxPontos) throw new ArgumentException($"A área pede cerca de {estimado:N0} pontos; o limite é {maxPontos:N0}. Divida a área.");
        for (int la = (int)Math.Floor(latMin); la <= (int)Math.Floor(latMax); la++)
        {
            for (int lo = (int)Math.Floor(lonMin); lo <= (int)Math.Floor(lonMax); lo++)
            {
                var nome = Ladrilho(la, lo);
                progresso?.Invoke($"Lendo {nome}...");
                IFonteBytes? fonte = null;
                FonteHttp? http = null;
                try
                {
                    if (abrir is not null) fonte = abrir(nome);
                    else fonte = http = new FonteHttp(Url(nome), Path.Combine(pastaCache, nome));
                    using var g = GeoTiff.Abrir(fonte, 32768);
                    var (cA, lA) = g.Pixel(lonMin, latMax);
                    var (cB, lB) = g.Pixel(lonMax, latMin);
                    int c0 = Math.Max(0, (int)Math.Floor(Math.Min(cA, cB))), c1 = Math.Min(g.Largura - 1, (int)Math.Ceiling(Math.Max(cA, cB)));
                    int l0 = Math.Max(0, (int)Math.Floor(Math.Min(lA, lB))), l1 = Math.Min(g.Altura - 1, (int)Math.Ceiling(Math.Max(lA, lB)));
                    if (c0 > c1 || l0 > l1) continue;
                    var z = g.LerJanela(c0, l0, c1 - c0 + 1, l1 - l0 + 1);
                    r.PassoGraus = Math.Max(r.PassoGraus, g.Dy);
                    for (int l = 0; l <= l1 - l0; l++)
                        for (int c = 0; c <= c1 - c0; c++)
                        {
                            var (x, y) = g.Centro(c0 + c, l0 + l);
                            if (y < latMin || y > latMax || x < lonMin || x > lonMax) continue;
                            float v = z[l, c];
                            if (float.IsNaN(v) || v < -1000) continue;
                            r.Pontos.Add(new PontoRelevo(y, x, v));
                        }
                    r.Ladrilhos.Add(nome);
                }
                catch (FileNotFoundException) { r.Ausentes.Add(nome); }
                finally
                {
                    if (http is not null) r.BytesBaixados += http.Baixados;
                    fonte?.Dispose();
                }
            }
        }
        return r;
    }
}
