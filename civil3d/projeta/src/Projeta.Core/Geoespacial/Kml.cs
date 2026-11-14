using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Projeta.Core.Geoespacial;

public enum TipoGeometriaKml { Ponto, Linha, Poligono }

/// <summary>Coordenada KML: longitude, latitude (graus) e altitude (m).</summary>
public readonly record struct CoordKml(double Lon, double Lat, double Alt = 0);

public sealed class GeometriaKml
{
    public TipoGeometriaKml Tipo { get; init; }
    /// <summary>Vértices (no polígono, o anel externo, sem repetir o primeiro no fim).</summary>
    public List<CoordKml> Coordenadas { get; init; } = new();
    /// <summary>Anéis internos (furos) do polígono.</summary>
    public List<List<CoordKml>> Furos { get; init; } = new();
}

/// <summary>Um objeto do Google Earth (Placemark).</summary>
public sealed class MarcadorKml
{
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    /// <summary>Caminho das pastas, separado por " / ".</summary>
    public string Pasta { get; set; } = "";
    /// <summary>Cor da linha RRGGBB (null = padrão do Google Earth).</summary>
    public string? Cor { get; set; }
    public double Largura { get; set; } = 2;
    /// <summary>Opacidade do preenchimento dos polígonos, 0 a 1.</summary>
    public double Preenchimento { get; set; } = 0.25;
    /// <summary>true = altitudes absolutas (objeto 3D); false = presos ao chão.</summary>
    public bool Absoluto { get; set; }
    public List<GeometriaKml> Geometrias { get; } = new();
}

/// <summary>Leitura e gravação de KML/KMZ (Google Earth).</summary>
public sealed class DocumentoKml
{
    private static readonly XNamespace K = "http://www.opengis.net/kml/2.2";

    public string Nome { get; set; } = "PROJETA";
    public string Descricao { get; set; } = "";
    public List<MarcadorKml> Itens { get; } = new();

    // ------------------------------------------------------------------ gravação

    /// <summary>Grava .kml ou .kmz (pela extensão do arquivo).</summary>
    public void Salvar(string arquivo)
    {
        var xml = ParaTexto();
        if (arquivo.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(arquivo)) File.Delete(arquivo);
            using var z = ZipFile.Open(arquivo, ZipArchiveMode.Create);
            var e = z.CreateEntry("doc.kml", CompressionLevel.Optimal);
            using var s = new StreamWriter(e.Open(), new UTF8Encoding(false));
            s.Write(xml);
        }
        else File.WriteAllText(arquivo, xml, new UTF8Encoding(false));
    }

    public string ParaTexto()
    {
        var doc = new XElement(K + "Document", new XElement(K + "name", Nome));
        if (Descricao.Length > 0) doc.Add(new XElement(K + "description", Descricao));
        // estilos compartilhados
        var estilos = new Dictionary<string, string>();
        string Estilo(MarcadorKml m)
        {
            var cor = (m.Cor ?? "FFFF00").ToUpperInvariant();
            var chave = $"{cor}|{m.Largura.ToString("0.#", CultureInfo.InvariantCulture)}|{Math.Round(m.Preenchimento * 255)}";
            if (estilos.TryGetValue(chave, out var id)) return id;
            id = "e" + (estilos.Count + 1);
            estilos[chave] = id;
            string bgr = cor.Length == 6 ? cor.Substring(4, 2) + cor.Substring(2, 2) + cor[..2] : "00FFFF";
            int alfa = (int)Math.Clamp(Math.Round(m.Preenchimento * 255), 0, 255);
            doc.Add(new XElement(K + "Style", new XAttribute("id", id),
                new XElement(K + "IconStyle", new XElement(K + "color", "ff" + bgr), new XElement(K + "scale", "0.9")),
                new XElement(K + "LabelStyle", new XElement(K + "scale", "0.8")),
                new XElement(K + "LineStyle", new XElement(K + "color", "ff" + bgr), new XElement(K + "width", m.Largura.ToString("0.#", CultureInfo.InvariantCulture))),
                new XElement(K + "PolyStyle", new XElement(K + "color", alfa.ToString("x2") + bgr), new XElement(K + "fill", alfa > 0 ? "1" : "0"), new XElement(K + "outline", "1"))));
            return id;
        }
        var pastas = new Dictionary<string, XElement>();
        XElement Pasta(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho)) return doc;
            if (pastas.TryGetValue(caminho, out var p)) return p;
            var partes = caminho.Split(" / ");
            var pai = partes.Length > 1 ? Pasta(string.Join(" / ", partes[..^1])) : doc;
            p = new XElement(K + "Folder", new XElement(K + "name", partes[^1]));
            pai.Add(p);
            pastas[caminho] = p;
            return p;
        }
        foreach (var m in Itens)
        {
            if (m.Geometrias.Count == 0) continue;
            var pm = new XElement(K + "Placemark", new XElement(K + "name", m.Nome));
            if (m.Descricao.Length > 0) pm.Add(new XElement(K + "description", m.Descricao));
            pm.Add(new XElement(K + "styleUrl", "#" + Estilo(m)));
            var geos = m.Geometrias.Select(g => Geometria(g, m.Absoluto)).ToList();
            pm.Add(geos.Count == 1 ? geos[0] : new XElement(K + "MultiGeometry", geos));
            Pasta(m.Pasta).Add(pm);
        }
        var kml = new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement(K + "kml", doc));
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = false }))
            kml.Save(w);
        return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"UTF-8\"");
    }

    private static string Coords(IEnumerable<CoordKml> c, bool fechar)
    {
        var l = c.ToList();
        if (fechar && l.Count > 0 && (l[0].Lon != l[^1].Lon || l[0].Lat != l[^1].Lat)) l.Add(l[0]);
        return string.Join(" ", l.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Lon:0.#########},{p.Lat:0.#########},{p.Alt:0.###}")));
    }

    private static XElement Geometria(GeometriaKml g, bool absoluto)
    {
        var modo = new XElement(K + "altitudeMode", absoluto ? "absolute" : "clampToGround");
        switch (g.Tipo)
        {
            case TipoGeometriaKml.Ponto:
                return new XElement(K + "Point", modo, new XElement(K + "coordinates", Coords(g.Coordenadas.Take(1), false)));
            case TipoGeometriaKml.Linha:
                return new XElement(K + "LineString", new XElement(K + "tessellate", absoluto ? "0" : "1"), modo, new XElement(K + "coordinates", Coords(g.Coordenadas, false)));
            default:
                var p = new XElement(K + "Polygon", new XElement(K + "tessellate", absoluto ? "0" : "1"), modo,
                    new XElement(K + "outerBoundaryIs", new XElement(K + "LinearRing", new XElement(K + "coordinates", Coords(g.Coordenadas, true)))));
                foreach (var f in g.Furos)
                    p.Add(new XElement(K + "innerBoundaryIs", new XElement(K + "LinearRing", new XElement(K + "coordinates", Coords(f, true)))));
                return p;
        }
    }

    // ------------------------------------------------------------------ leitura

    /// <summary>Lê .kml ou .kmz.</summary>
    public static DocumentoKml Ler(string arquivo)
    {
        if (arquivo.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase) || EhZip(arquivo))
        {
            using var z = ZipFile.OpenRead(arquivo);
            var e = z.Entries.FirstOrDefault(x => x.FullName.Equals("doc.kml", StringComparison.OrdinalIgnoreCase))
                    ?? z.Entries.FirstOrDefault(x => x.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidDataException("O KMZ não tem nenhum arquivo .kml dentro.");
            using var s = new StreamReader(e.Open(), Encoding.UTF8);
            return LerTexto(s.ReadToEnd());
        }
        return LerTexto(File.ReadAllText(arquivo, Encoding.UTF8));
    }

    private static bool EhZip(string arquivo)
    {
        try
        {
            using var f = File.OpenRead(arquivo);
            return f.Length > 4 && f.ReadByte() == 'P' && f.ReadByte() == 'K';
        }
        catch { return false; }
    }

    public static DocumentoKml LerTexto(string xml)
    {
        var x = XDocument.Parse(xml, LoadOptions.None);
        var d = new DocumentoKml();
        var raiz = x.Root ?? throw new InvalidDataException("KML vazio.");
        var docEl = raiz.Descendants().FirstOrDefault(e => e.Name.LocalName == "Document") ?? raiz;
        d.Nome = Filho(docEl, "name") ?? "KML";

        // estilos: id → cor RRGGBB (Style e StyleMap "normal")
        var cores = new Dictionary<string, string>();
        foreach (var st in raiz.Descendants().Where(e => e.Name.LocalName == "Style" && e.Attribute("id") is not null))
        {
            var cor = st.Elements().FirstOrDefault(e => e.Name.LocalName == "LineStyle")?.Elements().FirstOrDefault(e => e.Name.LocalName == "color")?.Value
                      ?? st.Elements().FirstOrDefault(e => e.Name.LocalName == "PolyStyle")?.Elements().FirstOrDefault(e => e.Name.LocalName == "color")?.Value
                      ?? st.Elements().FirstOrDefault(e => e.Name.LocalName == "IconStyle")?.Elements().FirstOrDefault(e => e.Name.LocalName == "color")?.Value;
            if (Rgb(cor) is { } rgb) cores[st.Attribute("id")!.Value] = rgb;
        }
        foreach (var sm in raiz.Descendants().Where(e => e.Name.LocalName == "StyleMap" && e.Attribute("id") is not null))
        {
            var normal = sm.Elements().Where(e => e.Name.LocalName == "Pair")
                .FirstOrDefault(p => Filho(p, "key") == "normal");
            var url = normal is null ? null : Filho(normal, "styleUrl");
            if (url is not null && cores.TryGetValue(url.TrimStart('#'), out var c)) cores[sm.Attribute("id")!.Value] = c;
        }

        foreach (var pm in raiz.Descendants().Where(e => e.Name.LocalName == "Placemark"))
        {
            var m = new MarcadorKml
            {
                Nome = Filho(pm, "name") ?? "",
                Descricao = Filho(pm, "description") ?? "",
                Pasta = string.Join(" / ", pm.Ancestors().Where(a => a.Name.LocalName == "Folder").Reverse().Select(a => Filho(a, "name") ?? "Pasta")),
            };
            var su = Filho(pm, "styleUrl");
            if (su is not null && cores.TryGetValue(su.TrimStart('#'), out var cor)) m.Cor = cor;
            var inline = pm.Elements().FirstOrDefault(e => e.Name.LocalName == "Style");
            if (inline is not null)
            {
                var c2 = inline.Descendants().FirstOrDefault(e => e.Name.LocalName == "color")?.Value;
                if (Rgb(c2) is { } rgb) m.Cor = rgb;
            }
            foreach (var g in pm.Elements()) LerGeometria(g, m);
            if (m.Geometrias.Count > 0) d.Itens.Add(m);
        }
        return d;
    }

    private static void LerGeometria(XElement g, MarcadorKml m)
    {
        string n = g.Name.LocalName;
        bool abs = Filho(g, "altitudeMode") is "absolute" or "relativeToGround";
        switch (n)
        {
            case "MultiGeometry":
                foreach (var f in g.Elements()) LerGeometria(f, m);
                break;
            case "Point":
                {
                    var c = LerCoords(Filho(g, "coordinates"));
                    if (c.Count > 0) { m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Ponto, Coordenadas = { c[0] } }); m.Absoluto |= abs; }
                    break;
                }
            case "LineString":
                {
                    var c = LerCoords(Filho(g, "coordinates"));
                    if (c.Count >= 2) { m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Linha, Coordenadas = c }); m.Absoluto |= abs; }
                    break;
                }
            case "LinearRing":
                {
                    var c = Anel(LerCoords(Filho(g, "coordinates")));
                    if (c.Count >= 3) { m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Poligono, Coordenadas = c }); m.Absoluto |= abs; }
                    break;
                }
            case "Polygon":
                {
                    var ext = g.Elements().FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs")?.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates")?.Value;
                    var c = Anel(LerCoords(ext));
                    if (c.Count < 3) break;
                    var geo = new GeometriaKml { Tipo = TipoGeometriaKml.Poligono, Coordenadas = c };
                    foreach (var ib in g.Elements().Where(e => e.Name.LocalName == "innerBoundaryIs"))
                    {
                        var f = Anel(LerCoords(ib.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates")?.Value));
                        if (f.Count >= 3) geo.Furos.Add(f);
                    }
                    m.Geometrias.Add(geo);
                    m.Absoluto |= abs;
                    break;
                }
            case "Track":
                {
                    // gx:Track: <gx:coord>lon lat alt</gx:coord>
                    var c = g.Elements().Where(e => e.Name.LocalName == "coord")
                        .Select(e => e.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        .Where(p => p.Length >= 2)
                        .Select(p => new CoordKml(D(p[0]), D(p[1]), p.Length > 2 ? D(p[2]) : 0))
                        .Where(p => !double.IsNaN(p.Lon) && !double.IsNaN(p.Lat)).ToList();
                    if (c.Count >= 2) { m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Linha, Coordenadas = c }); m.Absoluto |= abs; }
                    break;
                }
        }
    }

    private static List<CoordKml> Anel(List<CoordKml> c)
    {
        if (c.Count > 1 && c[0].Lon == c[^1].Lon && c[0].Lat == c[^1].Lat) c.RemoveAt(c.Count - 1);
        return c;
    }

    private static double D(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

    /// <summary>"lon,lat[,alt] lon,lat[,alt] ..." (separados por espaço ou quebra de linha).</summary>
    public static List<CoordKml> LerCoords(string? texto)
    {
        var r = new List<CoordKml>();
        if (string.IsNullOrWhiteSpace(texto)) return r;
        foreach (var t in texto.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var p = t.Split(',');
            if (p.Length < 2) continue;
            double lon = D(p[0]), lat = D(p[1]), alt = p.Length > 2 ? D(p[2]) : 0;
            if (double.IsNaN(lon) || double.IsNaN(lat)) continue;
            r.Add(new CoordKml(lon, lat, double.IsNaN(alt) ? 0 : alt));
        }
        return r;
    }

    private static string? Filho(XElement e, string nome) => e.Elements().FirstOrDefault(x => x.Name.LocalName == nome)?.Value?.Trim();

    /// <summary>Cor KML "aabbggrr" → "RRGGBB".</summary>
    private static string? Rgb(string? kml)
    {
        if (string.IsNullOrWhiteSpace(kml)) return null;
        var c = kml.Trim().TrimStart('#');
        if (c.Length == 8) return (c.Substring(6, 2) + c.Substring(4, 2) + c.Substring(2, 2)).ToUpperInvariant();
        if (c.Length == 6) return (c.Substring(4, 2) + c.Substring(2, 2) + c[..2]).ToUpperInvariant();
        return null;
    }
}
