using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Geoespacial;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Sistema de coordenadas do desenho (SIRGAS 2000 / UTM), definição da localização geográfica (para o mapa de satélite
/// do próprio Civil 3D) e conversão de objetos do desenho em coordenadas geográficas.
/// </summary>
internal static class GeoCivil
{
    public sealed record Deteccao(SistemaUtm? Sistema, string Origem, string? Codigo);

    /// <summary>Lê o sistema das configurações do Civil 3D ou da localização geográfica do desenho.</summary>
    public static Deteccao Detectar(Transaction tr, Database db)
    {
        string? codigo = null;
        try { codigo = CivilApplication.ActiveDocument.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode; } catch { }
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            var s = SistemaUtm.DeTexto(codigo) ?? DoCodigoAutodesk(codigo);
            if (s is not null) return new Deteccao(s, "configurações do desenho no Civil 3D", codigo);
        }
        try
        {
            var gid = db.GeoDataObject;
            if (!gid.IsNull && tr.GetObject(gid, OpenMode.ForRead) is GeoLocationData g && !string.IsNullOrWhiteSpace(g.CoordinateSystem))
            {
                var s = SistemaUtm.DeTexto(g.CoordinateSystem) ?? DoCodigoAutodesk(g.CoordinateSystem);
                if (s is not null) return new Deteccao(s, "localização geográfica do desenho", g.CoordinateSystem);
                codigo ??= g.CoordinateSystem;
            }
        }
        catch { }
        return new Deteccao(null, "", codigo);
    }

    private static SistemaUtm? DoCodigoAutodesk(string codigo)
    {
        try
        {
            using var cs = GeoCoordinateSystem.Create(codigo);
            return cs.EPSGcode > 0 ? SistemaUtm.DoEpsg(cs.EPSGcode) : null;
        }
        catch { return null; }
    }

    /// <summary>Sistema do desenho; se não estiver definido, pergunta o fuso e o hemisfério (SIRGAS 2000).</summary>
    public static SistemaUtm? Pedir(Sessao s, bool avisarDetectado = true)
    {
        var d = Detectar(s.Tr, s.Db);
        if (d.Sistema is not null)
        {
            if (avisarDetectado) Ctx.Msg($"Sistema do desenho: {d.Sistema} (das {d.Origem}).");
            return d.Sistema;
        }
        if (!string.IsNullOrWhiteSpace(d.Codigo)) Ctx.Msg($"O sistema do desenho ({d.Codigo}) não é SIRGAS 2000 nem WGS 84 / UTM; informe o fuso UTM em SIRGAS 2000.");
        return PedirManual(s);
    }

    public static SistemaUtm? PedirManual(Sessao s, SistemaUtm? sugestao = null)
    {
        var cfg = ConfigGeo.Carregar();
        var fuso = s.PedirNumero("Fuso UTM do desenho em SIRGAS 2000 (MG/SP/RJ/GO/DF: 23; BA/ES: 24; MT/PR/SC/RS: 21-22; PA/AM: 20-22)", sugestao?.Fuso ?? cfg.Fuso);
        if (fuso is null) return null;
        int f = (int)Math.Round(fuso.Value);
        if (f < 1 || f > 60) { Ctx.Msg("Fuso inválido (1 a 60)."); return null; }
        var h = s.PedirPalavra("Hemisfério", new[] { "Sul", "Norte" }, (sugestao?.Sul ?? cfg.Sul) ? "Sul" : "Norte");
        if (h is null) return null;
        var sis = new SistemaUtm(f, h == "Sul");
        cfg.Fuso = f; cfg.Sul = sis.Sul;
        ConfigGeo.Salvar(cfg);
        return sis;
    }

    private static readonly Dictionary<int, string?> _codigos = new();

    /// <summary>Código do sistema na biblioteca de coordenadas da Autodesk (ex.: o equivalente ao EPSG 31983).</summary>
    public static string? CodigoAutodesk(SistemaUtm sis)
    {
        if (sis.Epsg is not { } epsg) return null;
        lock (_codigos) if (_codigos.TryGetValue(epsg, out var c)) return c;
        string hs = sis.Sul ? "S" : "N";
        var candidatos = sis.Datum == SistemaUtm.Wgs84
            ? new[] { $"UTM84-{sis.Fuso}{hs}", $"WGS84.UTM-{sis.Fuso}{hs}" }
            : new[] { $"SIRGAS2000.UTM-{sis.Fuso}{hs}", $"SIRGAS2000-UTM-{sis.Fuso}{hs}", $"UTM-SIRGAS2000-{sis.Fuso}{hs}" };
        string? achado = null;
        foreach (var k in candidatos)
        {
            try
            {
                using var cs = GeoCoordinateSystem.Create(k);
                if (cs.EPSGcode == epsg) { achado = cs.ID; break; }
            }
            catch { }
        }
        if (achado is null)
        {
            try
            {
                foreach (var cs in GeoCoordinateSystem.CreateAll())
                {
                    try { if (achado is null && cs.EPSGcode == epsg) achado = cs.ID; }
                    finally { cs.Dispose(); }
                }
            }
            catch { }
        }
        lock (_codigos) _codigos[epsg] = achado;
        return achado;
    }

    /// <summary>
    /// Grava o sistema nas configurações do Civil 3D e na localização geográfica do desenho (coordenadas de grade:
    /// o desenho já está em UTM). Com isso o mapa de satélite do Civil (GEOMAP) aparece no lugar certo.
    /// Retorna o código usado e a diferença (m) entre a conversão da Autodesk e a do PROJETA num ponto de teste.
    /// </summary>
    public static (string Codigo, double Diferenca) DefinirSistema(Transaction tr, Database db, SistemaUtm sis, Point3d pontoTeste)
    {
        var codigo = CodigoAutodesk(sis) ?? throw new InvalidOperationException($"A biblioteca de coordenadas do Civil 3D não tem {sis} (EPSG {sis.Epsg}).");
        try { CivilApplication.ActiveDocument.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode = codigo; }
        catch (System.Exception ex) { Ctx.Msg($"[AVISO] Não consegui gravar o sistema nas configurações do Civil: {ex.Message}"); }

        GeoLocationData g;
        var gid = ObjectId.Null;
        try { gid = db.GeoDataObject; } catch { }
        if (!gid.IsNull) g = (GeoLocationData)tr.GetObject(gid, OpenMode.ForWrite);
        else
        {
            g = new GeoLocationData { BlockTableRecordId = SymbolUtilityServices.GetBlockModelSpaceId(db) };
            g.PostToDb();
            tr.AddNewlyCreatedDBObject(g, true);
        }
        g.CoordinateSystem = codigo;
        g.TypeOfCoordinates = TypeOfCoordinates.CoordinateTypeGrid;
        g.HorizontalUnits = UnitsValue.Meters;
        g.VerticalUnits = UnitsValue.Meters;
        var p = new Point3d(pontoTeste.X, pontoTeste.Y, 0);
        g.DesignPoint = p;
        g.ReferencePoint = p;
        g.NorthDirectionVector = Vector2d.YAxis;
        try { g.UpdateTransformationMatrix(); } catch { }

        double dif = double.NaN;
        try
        {
            var ll = g.TransformToLonLatAlt(p);
            var (lat, lon) = Utm.ParaGeo(p.X, p.Y, sis);
            var u = Utm.ParaUtm(ll.Y, ll.X, sis);
            dif = Math.Sqrt((u.E - p.X) * (u.E - p.X) + (u.N - p.Y) * (u.N - p.Y));
            if (double.IsNaN(lat)) dif = double.NaN;
        }
        catch { }
        return (codigo, dif);
    }

    /// <summary>O ponto parece estar em coordenadas UTM?</summary>
    public static bool PareceUtm(double x, double y) => x > 100000 && x < 900000 && y > 0 && y < 10000000;

    // ------------------------------------------------------------------ relevo de satélite

    public sealed record Relevo(string Nome, int Pontos, double Zmin, double Zmax, double AreaKm2, List<string> Ladrilhos, long Baixados, ModeloRelevo Modelo);

    /// <summary>O que dizer ao usuário sobre o modelo (tipo, erro esperado, datum vertical).</summary>
    public static string[] Avisos(ModeloRelevo m) => m.Terreno
        ? new[]
        {
            "  ANADEM é modelo de TERRENO: a vegetação foi removida do Copernicus por aprendizado de máquina (ANA/UFRGS). Ainda tem erro vertical de alguns metros e pode ter artefatos em áreas de mata densa; serve para estudo, traçado e anteprojeto, não substitui o levantamento topográfico.",
            "  Altitudes derivadas do Copernicus (geoide EGM2008): podem diferir de decímetros a cerca de 1 m da altitude oficial do IBGE (hgeoHNOR).",
        }
        : new[]
        {
            "  Atenção: é um modelo de SUPERFÍCIE (copas de árvores e prédios entram como relevo), com erro vertical típico de alguns metros. Serve para estudo, traçado e anteprojeto; não substitui o levantamento topográfico.",
            "  Altitudes no geoide EGM2008: podem diferir de decímetros a cerca de 1 m da altitude oficial do IBGE (hgeoHNOR).",
        };

    /// <summary>Retângulo geográfico que contém o retângulo UTM (amostra as bordas, por causa da convergência).</summary>
    public static (double LatMin, double LatMax, double LonMin, double LonMax) LimitesGeo(SistemaUtm sis, double x0, double y0, double x1, double y1)
    {
        double la0 = 90, la1 = -90, lo0 = 180, lo1 = -180;
        for (int i = 0; i <= 10; i++)
            foreach (var (x, y) in new[] { (x0 + (x1 - x0) * i / 10, y0), (x0 + (x1 - x0) * i / 10, y1), (x0, y0 + (y1 - y0) * i / 10), (x1, y0 + (y1 - y0) * i / 10) })
            {
                var (la, lo) = Utm.ParaGeo(x, y, sis);
                la0 = Math.Min(la0, la); la1 = Math.Max(la1, la); lo0 = Math.Min(lo0, lo); lo1 = Math.Max(lo1, lo);
            }
        return (la0, la1, lo0, lo1);
    }

    /// <summary>
    /// Lê o relevo Copernicus 30 m da área (com margem), converte para o UTM do desenho e cria a superfície TIN recortada
    /// pelo contorno. Lança InvalidOperationException com a explicação quando não dá.
    /// </summary>
    public static Relevo CriarRelevo(Transaction tr, Database db, SistemaUtm sis, IReadOnlyList<(double X, double Y)> contorno, double margem, Action<string>? progresso = null, ModeloRelevo? modelo = null)
    {
        modelo ??= ModeloRelevo.Copernicus;
        if (contorno.Count < 3) throw new InvalidOperationException("A área precisa de pelo menos 3 vértices.");
        if (!PareceUtm(contorno[0].X, contorno[0].Y))
            throw new InvalidOperationException("A área não está em coordenadas UTM (E entre 100.000 e 900.000 e N até 10.000.000). Georreferencie o desenho com PRJGEORREF antes.");
        margem = Math.Max(0, margem);
        double x0 = contorno.Min(p => p.X) - margem, x1 = contorno.Max(p => p.X) + margem;
        double y0 = contorno.Min(p => p.Y) - margem, y1 = contorno.Max(p => p.Y) + margem;
        double km2 = (x1 - x0) * (y1 - y0) / 1e6;
        if (km2 > 900) throw new InvalidOperationException($"Área de {km2:0} km² é grande demais para uma superfície só (limite 900 km²). Divida a área.");
        var (latMin, latMax, lonMin, lonMax) = LimitesGeo(sis, x0, y0, x1, y1);
        if (!modelo.Cobre(latMin, latMax, lonMin, lonMax))
            throw new InvalidOperationException($"O {modelo.Nome} não cobre esta área; use o Copernicus (mundial).");
        progresso?.Invoke($"Lendo o relevo {modelo.Nome} de {km2.ToString("0.###", Num.Br)} km² (baixa só os blocos da área; ficam guardados para a próxima vez)...");
        var rel = modelo.Ler(latMin - 1 / 3600.0, latMax + 1 / 3600.0, lonMin - 1 / 3600.0, lonMax + 1 / 3600.0, progresso: m => progresso?.Invoke("  " + m));
        if (rel.Pontos.Count == 0)
            throw new InvalidOperationException(rel.Ausentes.Count > 0 ? $"Não há relevo {modelo.Nome} para esta área (mar ou fora da cobertura)." : "Nenhum ponto de relevo dentro da área.");
        var pts = new Point3dCollection();
        double zmin = double.MaxValue, zmax = double.MinValue;
        foreach (var p in rel.Pontos)
        {
            var u = Utm.ParaUtm(p.Lat, p.Lon, sis);
            if (u.E < x0 || u.E > x1 || u.N < y0 || u.N > y1) continue;
            pts.Add(new Point3d(u.E, u.N, p.Z));
            zmin = Math.Min(zmin, p.Z); zmax = Math.Max(zmax, p.Z);
        }
        if (pts.Count < 3) throw new InvalidOperationException("Pontos de relevo insuficientes dentro da área.");

        var nome = VolumesCivil.NomeLivre(tr, modelo.Terreno ? "PRJ Relevo ANADEM 30 m" : "PRJ Relevo Copernicus 30 m");
        var id = TinSurface.Create(db, nome);
        var tin = (TinSurface)tr.GetObject(id, OpenMode.ForWrite);
        tin.Description = $"{rel.Fonte}, {sis}, {DateTime.Now:dd/MM/yyyy}. " +
                          (modelo.Terreno ? "Modelo de terreno (vegetação removida), altitudes derivadas do Copernicus (EGM2008). " : "Modelo de superfície (inclui vegetação e prédios), altitudes no geoide EGM2008. ") +
                          modelo.Atribuicao + ".";
        tin.AddVertices(pts);
        var lim = new Point2dCollection();
        foreach (var (x, y) in contorno) lim.Add(new Point2d(x, y));
        lim.Add(new Point2d(contorno[0].X, contorno[0].Y));
        tin.BoundariesDefinition.AddBoundaries(lim, 0.1, Autodesk.Civil.SurfaceBoundaryType.Outer, true);
        try { tin.Rebuild(); } catch { }
        return new Relevo(nome, pts.Count, zmin, zmax, km2, rel.Ladrilhos, rel.BytesBaixados, modelo);
    }

    // ------------------------------------------------------------------ objetos do desenho

    /// <summary>Cor RRGGBB efetiva da entidade (resolve DaCamada/DoBloco pela camada).</summary>
    public static string? CorRgb(Transaction tr, Entity e)
    {
        try
        {
            var c = e.Color;
            if (c.IsByLayer || c.IsByBlock || c.IsNone)
                c = ((LayerTableRecord)tr.GetObject(e.LayerId, OpenMode.ForRead)).Color;
            var v = c.ColorValue;
            return $"{v.R:X2}{v.G:X2}{v.B:X2}";
        }
        catch { return null; }
    }

    public static Color Cor(string rgb) => Color.FromRgb(Convert.ToByte(rgb[..2], 16), Convert.ToByte(rgb.Substring(2, 2), 16), Convert.ToByte(rgb.Substring(4, 2), 16));

    /// <summary>Pontos de uma curva (arcos divididos em trechos de até ~<paramref name="passo"/> m), se é fechada e se tem cotas próprias (3D).</summary>
    public static (List<Point3d> Pontos, bool Fechada, bool TresD)? PontosCurva(Curve c, double passo = 2)
    {
        var r = new List<Point3d>();
        bool fechada = false, tresD = false;
        switch (c)
        {
            case Line l:
                r.Add(l.StartPoint); r.Add(l.EndPoint);
                tresD = Math.Abs(l.StartPoint.Z - l.EndPoint.Z) > 1e-9;
                break;
            case Polyline p:
                {
                    fechada = p.Closed || (p.NumberOfVertices > 2 && p.GetPoint2dAt(0).GetDistanceTo(p.GetPoint2dAt(p.NumberOfVertices - 1)) < 1e-6);
                    int nseg = p.Closed ? p.NumberOfVertices : p.NumberOfVertices - 1;
                    for (int i = 0; i < p.NumberOfVertices; i++)
                    {
                        r.Add(p.GetPoint3dAt(i));
                        if (i < nseg && p.GetSegmentType(i) == SegmentType.Arc)
                        {
                            var arco = p.GetArcSegmentAt(i);
                            double len = arco.GetLength(arco.GetParameterOf(arco.StartPoint), arco.GetParameterOf(arco.EndPoint), 1e-9);
                            int k = Math.Clamp((int)Math.Ceiling(len / passo), 2, 720);
                            var pts = arco.GetSamplePoints(k + 1);
                            for (int j = 1; j < pts.Length - 1; j++) r.Add(pts[j].Point);
                        }
                    }
                    if (fechada && !p.Closed && r.Count > 1) r.RemoveAt(r.Count - 1);
                    break;
                }
            case Polyline3d p3:
                {
                    var tr = p3.Database.TransactionManager.TopTransaction;
                    foreach (ObjectId vid in p3)
                        if (tr.GetObject(vid, OpenMode.ForRead) is PolylineVertex3d v && v.VertexType != Vertex3dType.ControlVertex) r.Add(v.Position);
                    fechada = p3.Closed;
                    tresD = true;
                    break;
                }
            default:
                {
                    double ini = c.GetDistanceAtParameter(c.StartParam), fim = c.GetDistanceAtParameter(c.EndParam);
                    double len = fim - ini;
                    int n = Math.Clamp((int)Math.Ceiling(len / passo), 8, 5000);
                    fechada = c.Closed;
                    int ultimo = fechada ? n - 1 : n;
                    for (int i = 0; i <= ultimo; i++) r.Add(c.GetPointAtDist(Math.Min(len, len * i / n) + ini));
                    tresD = c is Spline && r.Any(q => Math.Abs(q.Z - r[0].Z) > 1e-9);
                    break;
                }
        }
        if (r.Count < 2) return null;
        return (r, fechada, tresD);
    }

    /// <summary>Pontos de um alinhamento do Civil a cada <paramref name="passo"/> m (mais o fim).</summary>
    public static List<Point3d> PontosAlinhamento(Alignment al, double passo = 5)
    {
        var r = new List<Point3d>();
        double a = al.StartingStation, b = al.EndingStation;
        int n = Math.Clamp((int)Math.Ceiling((b - a) / passo), 1, 20000);
        for (int i = 0; i <= n; i++)
        {
            double st = Math.Min(b, a + (b - a) * i / n);
            if (CivilUtil.PontoNaEstaca(al, st, 0, out var x, out var y)) r.Add(new Point3d(x, y, 0));
        }
        return r;
    }

    /// <summary>Nome de camada válido no AutoCAD.</summary>
    public static string NomeCamada(string nome)
    {
        var n = Regex.Replace(nome, @"[<>/\\"":;?*|,=`]", "-").Trim();
        if (n.Length == 0) n = "KML";
        return n.Length > 200 ? n[..200] : n;
    }
}

/// <summary>Últimas escolhas dos comandos de geo em %APPDATA%\Projeta\geo.json.</summary>
internal sealed class ConfigGeo
{
    public int Fuso { get; set; } = 23;
    public bool Sul { get; set; } = true;
    public string? BaseVolume { get; set; }
    public string? ComparacaoVolume { get; set; }
    public bool KmzPoligonos { get; set; } = true;
    public string KmzAltitudes { get; set; } = "Solo";
    public string KmzSuperficie { get; set; } = "Contorno";
    public string KmlCotas { get; set; } = "Kml";
    public double MargemRelevo { get; set; } = 60;
    public string ModeloRelevo { get; set; } = "anadem";
    public int FaixasQuantidade { get; set; } = 8;

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "geo.json");

    public static ConfigGeo Carregar()
    {
        try { if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigGeo>(File.ReadAllText(Arquivo)) is { } c) return c; }
        catch { }
        return new ConfigGeo();
    }

    public static void Salvar(ConfigGeo c)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!); File.WriteAllText(Arquivo, JsonProjeta.Serializar(c)); }
        catch { }
    }
}
