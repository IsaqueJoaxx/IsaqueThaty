using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Geoespacial;
using Projeta.Core.Superficies;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// Módulo Geo do PROJETA: relevo de satélite (Copernicus 30 m) para superfície, exportação e importação do Google Earth
/// (KMZ/KML) e georreferenciamento do desenho por pontos conhecidos (SIRGAS 2000 / UTM).
/// </summary>
public sealed class CmdGeo
{
    // ================================================================== PRJRELEVO

    [CommandMethod("PRJRELEVO", CommandFlags.Modal)]
    public static void Relevo()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sis = GeoCivil.Pedir(s);
            if (sis is null) return;
            var cfg = ConfigGeo.Carregar();
            var contorno = PedirArea(s, "Área do relevo");
            if (contorno is null) return;
            if (!GeoCivil.PareceUtm(contorno[0].X, contorno[0].Y))
            {
                Ctx.Msg("A área não está em coordenadas UTM (E entre 100.000 e 900.000 e N até 10.000.000). Georreferencie o desenho com PRJGEORREF antes.");
                return;
            }
            var margem = s.PedirNumero("Margem além da área, em m (o relevo é recortado na área no fim)", cfg.MargemRelevo);
            if (margem is null) return;
            cfg.MargemRelevo = Math.Max(0, margem.Value);
            ConfigGeo.Salvar(cfg);

            var r = GeoCivil.CriarRelevo(s.Tr, s.Db, sis, contorno, cfg.MargemRelevo, Ctx.Msg);
            s.Tr.Commit();

            Ctx.Msg($"Superfície '{r.Nome}': {r.Pontos:N0} pontos da grade de 1\" (~30 m), cotas de {r.Zmin.ToString("0.0", Num.Br)} a {r.Zmax.ToString("0.0", Num.Br)} m, recortada na área escolhida.");
            Ctx.Msg($"  Blocos: {string.Join(", ", r.Ladrilhos)}; baixados agora {(r.Baixados / 1024.0).ToString("0", Num.Br)} kB (cache em {Copernicus.PastaCachePadrao}).");
            Ctx.Msg("  Atenção: é um modelo de SUPERFÍCIE (copas de árvores e prédios entram como relevo), com erro vertical típico de alguns metros. Serve para estudo, traçado e anteprojeto; não substitui o levantamento topográfico.");
            Ctx.Msg("  Altitudes no geoide EGM2008: podem diferir de decímetros a cerca de 1 m da altitude oficial do IBGE (hgeoHNOR).");
            Ctx.Msg("  Fonte: " + Copernicus.Atribuicao + ".");
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJRELEVO: " + ex.Message); }
    }

    /// <summary>Área por polilinha fechada ou por janela (em coordenadas do mundo).</summary>
    internal static List<(double X, double Y)>? PedirArea(Sessao s, string titulo)
    {
        var modo = s.PedirPalavra(titulo, new[] { "Polilinha", "Janela" }, "Polilinha");
        if (modo is null) return null;
        if (modo == "Polilinha")
        {
            var peo = new PromptEntityOptions("\nSelecione a polilinha fechada da área: ");
            peo.SetRejectMessage("\nSelecione uma polilinha, círculo, elipse ou spline fechada.");
            peo.AddAllowedClass(typeof(Curve), false);
            var r = s.Ed.GetEntity(peo);
            if (r.Status != PromptStatus.OK) return null;
            var v = VolumesCivil.Vertices((Curve)s.Tr.GetObject(r.ObjectId, OpenMode.ForRead));
            if (v is null) Ctx.Msg("A curva escolhida não é fechada.");
            return v;
        }
        var ucs = s.Ed.CurrentUserCoordinateSystem;
        var p1 = s.Ed.GetPoint("\nPrimeiro canto da área: ");
        if (p1.Status != PromptStatus.OK) return null;
        var p2 = s.Ed.GetCorner("\nCanto oposto: ", p1.Value);
        if (p2.Status != PromptStatus.OK) return null;
        var a = p1.Value.TransformBy(ucs);
        var b = p2.Value.TransformBy(ucs);
        double xa = Math.Min(a.X, b.X), xb = Math.Max(a.X, b.X), ya = Math.Min(a.Y, b.Y), yb = Math.Max(a.Y, b.Y);
        if (xb - xa < 1 || yb - ya < 1) { Ctx.Msg("Janela pequena demais."); return null; }
        return new List<(double X, double Y)> { (xa, ya), (xb, ya), (xb, yb), (xa, yb) };
    }

    // ================================================================== PRJKMZ (exportar para o Google Earth)

    [CommandMethod("PRJKMZ", CommandFlags.Modal | CommandFlags.UsePickSet)]
    public static void ExportarKmz()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sis = GeoCivil.Pedir(s);
            if (sis is null) return;
            var cfg = ConfigGeo.Carregar();
            var sel = s.Ed.SelectImplied();
            ObjectId[] ids;
            if (sel.Status == PromptStatus.OK && sel.Value.Count > 0) ids = sel.Value.GetObjectIds();
            else
            {
                var r = s.Ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione o que vai para o Google Earth (linhas, polilinhas, eixos, pontos COGO, blocos, textos, superfícies): " });
                if (r.Status != PromptStatus.OK) return;
                ids = r.Value.GetObjectIds();
            }
            var alt = s.PedirPalavra("Altitudes [Solo = em cima do terreno do Google Earth / Absolutas = cotas do desenho nos objetos 3D]", new[] { "Solo", "Absolutas" }, cfg.KmzAltitudes);
            if (alt is null) return;
            var pol = s.PedirPalavra("Curvas fechadas como áreas preenchidas", new[] { "Sim", "Nao" }, cfg.KmzPoligonos ? "Sim" : "Nao");
            if (pol is null) return;
            bool temSup = ids.Any(i => s.Tr.GetObject(i, OpenMode.ForRead) is TinSurface);
            string sup = cfg.KmzSuperficie;
            if (temSup)
            {
                sup = s.PedirPalavra("Superfícies [Contorno / Triangulos coloridos por cota, em 3D]", new[] { "Contorno", "Triangulos" }, cfg.KmzSuperficie) ?? "";
                if (sup.Length == 0) return;
            }
            cfg.KmzAltitudes = alt; cfg.KmzPoligonos = pol == "Sim"; cfg.KmzSuperficie = sup;
            ConfigGeo.Salvar(cfg);
            bool abs = alt == "Absolutas";

            var doc = new DocumentoKml
            {
                Nome = Path.GetFileNameWithoutExtension(s.Doc.Name),
                Descricao = $"Exportado pelo PROJETA de {Path.GetFileName(s.Doc.Name)} ({sis}) em {DateTime.Now:dd/MM/yyyy HH:mm}.",
            };
            int fora = 0, ignorados = 0;
            var contagem = new Dictionary<string, int>();
            void Contar(string k) => contagem[k] = contagem.TryGetValue(k, out var n) ? n + 1 : 1;
            CoordKml C(Point3d p) { var (la, lo) = Utm.ParaGeo(p.X, p.Y, sis); return new CoordKml(lo, la, p.Z); }
            bool Valido(Point3d p) { if (GeoCivil.PareceUtm(p.X, p.Y)) return true; fora++; return false; }
            MarcadorKml Novo(Entity e, string nome, string desc = "") => new() { Nome = nome, Descricao = desc, Pasta = e.Layer, Cor = GeoCivil.CorRgb(s.Tr, e) ?? "FFFF00" };
            void Ponto(Entity e, Point3d p, string nome, string desc, bool tem3D)
            {
                if (!Valido(p)) return;
                var m = Novo(e, nome, desc);
                m.Absoluto = abs && tem3D;
                m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Ponto, Coordenadas = { C(p) } });
                doc.Itens.Add(m);
            }
            void Linha(Entity e, string nome, List<Point3d> pts, bool fechada, bool tem3D)
            {
                if (pts.Count < 2 || !Valido(pts[0])) return;
                var m = Novo(e, nome);
                m.Absoluto = abs && tem3D;
                m.Largura = 2.5;
                if (fechada && cfg.KmzPoligonos && pts.Count >= 3)
                    m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Poligono, Coordenadas = pts.Select(C).ToList() });
                else
                {
                    var c = pts.Select(C).ToList();
                    if (fechada) c.Add(c[0]);
                    m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Linha, Coordenadas = c });
                }
                doc.Itens.Add(m);
            }

            var erros = new Dictionary<string, int>();
            var espacoModelo = SymbolUtilityServices.GetBlockModelSpaceId(s.Db);
            foreach (var id in ids)
            {
                if (s.Tr.GetObject(id, OpenMode.ForRead) is not Entity e) continue;
                if (e.OwnerId != espacoModelo) { ignorados++; continue; }   // layouts (folhas) não vão para o mapa
                try { Exportar(e); }
                catch (System.Exception ex)
                {
                    var k = $"{e.GetType().Name}: {ex.Message}";
                    erros[k] = erros.TryGetValue(k, out var n) ? n + 1 : 1;
                }
            }
            void Exportar(Entity e)
            {
                switch (e)
                {
                    case CogoPoint cp:
                        Ponto(cp, cp.Location, string.IsNullOrWhiteSpace(cp.PointName) ? cp.PointNumber.ToString(CultureInfo.InvariantCulture) : cp.PointName,
                              $"{cp.RawDescription} | cota {cp.Elevation.ToString("0.000", Num.Br)} m", true);
                        Contar("pontos COGO");
                        break;
                    case DBPoint dp:
                        Ponto(dp, dp.Position, "Ponto", $"cota {dp.Position.Z.ToString("0.000", Num.Br)} m", Math.Abs(dp.Position.Z) > 1e-9);
                        Contar("pontos");
                        break;
                    case Autodesk.AutoCAD.DatabaseServices.Table:
                        ignorados++;
                        break;
                    case BlockReference br:
                        {
                            string nome = br.Name;
                            try { if (br.IsDynamicBlock) nome = ((BlockTableRecord)s.Tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name; } catch { }
                            Ponto(br, br.Position, nome, $"camada {br.Layer}", Math.Abs(br.Position.Z) > 1e-9);
                            Contar("blocos");
                            break;
                        }
                    case DBText t:
                        Ponto(t, t.Position, t.TextString, "", false);
                        Contar("textos");
                        break;
                    case MText mt:
                        Ponto(mt, mt.Location, mt.Text.Replace("\r", " ").Replace("\n", " "), "", false);
                        Contar("textos");
                        break;
                    case Alignment al:
                        Linha(al, al.Name, GeoCivil.PontosAlinhamento(al), false, false);
                        Contar("alinhamentos");
                        break;
                    case FeatureLine fl:
                        {
                            var pc = fl.GetPoints(FeatureLinePointType.AllPoints);
                            var l = new List<Point3d>();
                            foreach (Point3d p in pc) l.Add(p);
                            Linha(fl, string.IsNullOrWhiteSpace(fl.Name) ? "Feature line" : fl.Name, l, fl.Closed, true);
                            Contar("feature lines");
                            break;
                        }
                    case TinSurface tin:
                        ExportarSuperficie(s, doc, tin, sis, sup == "Triangulos", abs, ref fora);
                        Contar("superfícies");
                        break;
                    case Curve cv when cv is Line or Arc or Circle or Polyline or Polyline2d or Polyline3d or Spline or Ellipse:
                        {
                            var r = GeoCivil.PontosCurva(cv);
                            if (r is null) { ignorados++; break; }
                            bool tem3D = r.Value.TresD || (cv is Polyline pl && Math.Abs(pl.Elevation) > 1e-9);
                            Linha(cv, cv.GetType().Name switch { "Polyline" or "Polyline2d" or "Polyline3d" => "Polilinha", "Line" => "Linha", "Arc" => "Arco", "Circle" => "Círculo", _ => "Curva" } + $" ({cv.Layer})",
                                  r.Value.Pontos, r.Value.Fechada, tem3D);
                            Contar("linhas e polilinhas");
                            break;
                        }
                    default:
                        ignorados++;
                        break;
                }
            }
            if (doc.Itens.Count == 0)
            {
                Ctx.Msg(fora > 0 ? "Os objetos não estão em coordenadas UTM; georreferencie o desenho com PRJGEORREF." : "Nada para exportar na seleção.");
                return;
            }
            var arq = UiUtil.SalvarArquivo("Google Earth (*.kmz)|*.kmz|KML (*.kml)|*.kml", Texto.IdSeguro(doc.Nome, 80) + ".kmz");
            if (arq is null) return;
            doc.Salvar(arq);
            Ctx.Msg($"Google Earth: {doc.Itens.Count} objeto(s) gravados em {arq} ({string.Join(", ", contagem.Select(k => $"{k.Value} {k.Key}"))}).");
            Ctx.Msg($"  Coordenadas convertidas de {sis} para latitude/longitude (WGS 84 ≈ SIRGAS 2000). Pastas = camadas do desenho.");
            if (fora > 0) Ctx.Msg($"  [AVISO] {fora} objeto(s) fora de coordenadas UTM ficaram de fora.");
            if (ignorados > 0) Ctx.Msg($"  {ignorados} objeto(s) de tipos não exportados (hachuras, cotas, sólidos...) ficaram de fora.");
            foreach (var (k, n) in erros) Ctx.Msg($"  [AVISO] {n} objeto(s) não exportados — {k}");
            if (s.PedirPalavra("Abrir no Google Earth", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJKMZ: " + ex.Message); }
    }

    private static void ExportarSuperficie(Sessao s, DocumentoKml doc, TinSurface tin, SistemaUtm sis, bool triangulos, bool abs, ref int fora)
    {
        var tri = new List<(P3, P3, P3)>();
        foreach (TinSurfaceTriangle t in tin.GetTriangles(false))
        {
            var a = t.Vertex1.Location; var b = t.Vertex2.Location; var c = t.Vertex3.Location;
            tri.Add((new P3(a.X, a.Y, a.Z), new P3(b.X, b.Y, b.Z), new P3(c.X, c.Y, c.Z)));
        }
        if (tri.Count == 0) return;
        if (!GeoCivil.PareceUtm(tri[0].Item1.X, tri[0].Item1.Y)) { fora++; return; }
        CoordKml C(P3 p) { var (la, lo) = Utm.ParaGeo(p.X, p.Y, sis); return new CoordKml(lo, la, p.Z); }
        string pasta = "Superfície " + tin.Name;
        if (triangulos && tri.Count > 60000)
        {
            Ctx.Msg($"  [AVISO] '{tin.Name}' tem {tri.Count:N0} triângulos (limite 60.000 para o Google Earth): vai só o contorno.");
            triangulos = false;
        }
        if (!triangulos)
        {
            var aneis = ContornoTin.Aneis(tri);
            if (aneis.Count == 0) return;
            var m = new MarcadorKml { Nome = tin.Name, Descricao = $"Contorno da superfície {tin.Name}", Pasta = pasta, Cor = GeoCivil.CorRgb(s.Tr, tin) ?? "FFFF00", Preenchimento = 0.15, Largura = 2 };
            // o maior anel é o externo; os outros, por dentro dele, são furos
            var geo = new GeometriaKml { Tipo = TipoGeometriaKml.Poligono, Coordenadas = aneis[0].Select(C).ToList() };
            foreach (var f in aneis.Skip(1)) geo.Furos.Add(f.Select(C).ToList());
            m.Geometrias.Add(geo);
            m.Absoluto = abs;
            doc.Itens.Add(m);
            return;
        }
        double zmin = tri.Min(t => Math.Min(t.Item1.Z, Math.Min(t.Item2.Z, t.Item3.Z))), zmax = tri.Max(t => Math.Max(t.Item1.Z, Math.Max(t.Item2.Z, t.Item3.Z)));
        var faixas = MapaFaixas.Cotas(zmin, zmax, 10);
        var grupos = new Dictionary<int, MarcadorKml>();
        foreach (var (a, b, c) in tri)
        {
            double zm = (a.Z + b.Z + c.Z) / 3;
            int k = faixas.Count - 1;
            for (int i = 0; i < faixas.Count; i++) if (zm < faixas[i].Ate) { k = i; break; }
            if (!grupos.TryGetValue(k, out var m))
            {
                m = new MarcadorKml { Nome = $"{tin.Name}: {MapaFaixas.Intervalo(faixas[k], "m")}", Pasta = pasta, Cor = faixas[k].Cor, Preenchimento = 0.85, Largura = 0, Absoluto = true };
                grupos[k] = m;
                doc.Itens.Add(m);
            }
            m.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Poligono, Coordenadas = { C(a), C(b), C(c) } });
        }
    }

    // ================================================================== PRJKML (importar do Google Earth)

    [CommandMethod("PRJKML", CommandFlags.Modal)]
    public static void ImportarKml()
    {
        try
        {
            using var s = Sessao.Abrir();
            var arq = UiUtil.AbrirArquivo("Google Earth (*.kmz;*.kml)|*.kmz;*.kml|Todos os arquivos (*.*)|*.*");
            if (arq is null) return;
            var doc = DocumentoKml.Ler(arq);
            if (doc.Itens.Count == 0) { Ctx.Msg("O arquivo não tem marcadores, caminhos nem polígonos."); return; }
            var todas = doc.Itens.SelectMany(i => i.Geometrias).SelectMany(g => g.Coordenadas).ToList();
            double latM = todas.Average(c => c.Lat), lonM = todas.Average(c => c.Lon);
            var det = GeoCivil.Detectar(s.Tr, s.Db);
            SistemaUtm? sis = det.Sistema;
            if (sis is null)
            {
                var sug = SistemaUtm.DoPonto(latM, lonM);
                Ctx.Msg($"O desenho não tem sistema de coordenadas definido; pelos dados do arquivo o fuso é {sug.Fuso}{(sug.Sul ? "S" : "N")}.");
                sis = GeoCivil.PedirManual(s, sug);
                if (sis is null) return;
            }
            else Ctx.Msg($"Sistema do desenho: {sis} (das {det.Origem}).");
            double dist = Math.Abs(Utm.NormalizarLon(lonM - sis.MeridianoCentral));
            if (dist > 4.5) Ctx.Msg($"[AVISO] Os dados estão a {dist.ToString("0.0", Num.Br)}° do meridiano central do fuso {sis.Fuso}: provavelmente são de outro fuso; a deformação será grande.");

            var cfg = ConfigGeo.Carregar();
            bool temAlt = doc.Itens.Any(i => i.Absoluto) && todas.Any(c => Math.Abs(c.Alt) > 1e-6);
            var sups = CivilUtil.Superficies(s.Tr);
            var opcoes = sups.Count > 0 ? new[] { "Zero", "Kml", "Superficie" } : new[] { "Zero", "Kml" };
            var padrao = opcoes.Contains(cfg.KmlCotas) ? cfg.KmlCotas : (temAlt ? "Kml" : "Zero");
            var cotas = s.PedirPalavra("Cotas [Zero / Kml = altitudes do arquivo" + (sups.Count > 0 ? " / Superficie = projetar numa superfície" : "") + "]", opcoes, padrao);
            if (cotas is null) return;
            cfg.KmlCotas = cotas;
            ConfigGeo.Salvar(cfg);
            Surface? sup = null;
            if (cotas == "Superficie")
            {
                sup = CivilUtil.EscolherSuperficie(s, false);
                if (sup is null) return;
            }
            bool tresD = cotas != "Zero";
            double Z(double e, double n, double alt) => cotas switch
            {
                "Kml" => alt,
                "Superficie" => CivilUtil.Cota(sup, e, n) is var z && !double.IsNaN(z) ? z : 0,
                _ => 0,
            };
            Point3d P(CoordKml c)
            {
                var u = Utm.ParaUtm(c.Lat, c.Lon, sis);
                return new Point3d(u.E, u.N, Z(u.E, u.N, c.Alt));
            }

            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            CogoPointCollection? cogo = null;
            try { cogo = CivilApplication.ActiveDocument.CogoPoints; } catch { }
            int nPontos = 0, nLinhas = 0, nAreas = 0;
            double ex0 = double.MaxValue, ey0 = double.MaxValue, ex1 = double.MinValue, ey1 = double.MinValue;
            void Ext(Point3d p) { ex0 = Math.Min(ex0, p.X); ey0 = Math.Min(ey0, p.Y); ex1 = Math.Max(ex1, p.X); ey1 = Math.Max(ey1, p.Y); }
            void Estilo(Entity e, string camada, string? cor)
            {
                e.Layer = camada;
                if (cor is not null) e.Color = GeoCivil.Cor(cor);
            }
            ObjectId Curva(List<Point3d> pts, bool fechada, string camada, string? cor)
            {
                Entity e;
                if (tresD)
                {
                    var pc = new Point3dCollection();
                    foreach (var p in pts) pc.Add(p);
                    e = new Polyline3d(Poly3dType.SimplePoly, pc, fechada);
                }
                else
                {
                    var pl = new Polyline();
                    for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
                    pl.Closed = fechada;
                    e = pl;
                }
                e.SetDatabaseDefaults();
                Estilo(e, camada, cor);
                foreach (var p in pts) Ext(p);
                return Ctx.Adicionar(s.Tr, ms, e);
            }

            foreach (var it in doc.Itens)
            {
                var camada = Ctx.GarantirLayer(s.Tr, s.Db, GeoCivil.NomeCamada("KML-" + (it.Pasta.Length > 0 ? it.Pasta.Replace(" / ", "-") : doc.Nome)), 7);
                foreach (var g in it.Geometrias)
                {
                    switch (g.Tipo)
                    {
                        case TipoGeometriaKml.Ponto:
                            {
                                var p = P(g.Coordenadas[0]);
                                Ext(p);
                                bool feito = false;
                                if (cogo is not null)
                                {
                                    try
                                    {
                                        var pid = cogo.Add(p, it.Nome, true);
                                        var cp = (CogoPoint)s.Tr.GetObject(pid, OpenMode.ForWrite);
                                        cp.Layer = camada;
                                        if (!string.IsNullOrWhiteSpace(it.Nome)) try { cp.PointName = it.Nome; } catch { }
                                        feito = true;
                                    }
                                    catch { }
                                }
                                if (!feito)
                                {
                                    var dp = new DBPoint(p);
                                    dp.SetDatabaseDefaults();
                                    Estilo(dp, camada, it.Cor);
                                    Ctx.Adicionar(s.Tr, ms, dp);
                                    if (it.Nome.Length > 0)
                                    {
                                        var t = Ctx.Texto(Ctx.MTextLinhas(it.Nome), new Point3d(p.X + 1, p.Y + 1, p.Z), 1.5);
                                        t.Layer = camada;
                                        Ctx.Adicionar(s.Tr, ms, t);
                                    }
                                }
                                nPontos++;
                                break;
                            }
                        case TipoGeometriaKml.Linha:
                            Curva(g.Coordenadas.Select(P).ToList(), false, camada, it.Cor);
                            nLinhas++;
                            break;
                        default:
                            Curva(g.Coordenadas.Select(P).ToList(), true, camada, it.Cor);
                            foreach (var f in g.Furos) Curva(f.Select(P).ToList(), true, camada, it.Cor);
                            nAreas++;
                            break;
                    }
                }
            }
            s.Tr.Commit();
            Ctx.Msg($"Google Earth → desenho: {nPontos} ponto(s){(cogo is not null ? " COGO" : "")}, {nLinhas} caminho(s), {nAreas} polígono(s) de '{doc.Nome}', em {sis}, camadas KML-*.");
            if (cotas == "Zero") Ctx.Msg("  Cotas zeradas (curvas 2D). Rode de novo com Cotas = Superficie para projetar numa superfície.");
            if (ex1 > ex0 && s.PedirPalavra("Aproximar a vista dos objetos importados", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                double mx = Math.Max(10, (ex1 - ex0) * 0.05), my = Math.Max(10, (ey1 - ey0) * 0.05);
                string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
                s.Doc.SendStringToExecute($"_.ZOOM _W {F(ex0 - mx)},{F(ey0 - my)} {F(ex1 + mx)},{F(ey1 + my)} ", true, false, false);
            }
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJKML: " + ex.Message); }
    }

    // ================================================================== PRJGEORREF

    [CommandMethod("PRJGEORREF", CommandFlags.Modal)]
    public static void Georreferenciar()
    {
        try
        {
            using var s = Sessao.Abrir();
            var modo = s.PedirPalavra("Georreferenciar [Pontos = clicar pontos e digitar as coordenadas reais / Sistema = o desenho já está em UTM, só definir o sistema]", new[] { "Pontos", "Sistema" }, "Pontos");
            if (modo is null) return;
            var det = GeoCivil.Detectar(s.Tr, s.Db);
            if (modo == "Sistema")
            {
                if (det.Sistema is not null) Ctx.Msg($"Hoje o desenho está em {det.Sistema} (das {det.Origem}).");
                var sis0 = GeoCivil.PedirManual(s, det.Sistema);
                if (sis0 is null) return;
                var ext = Extensao(s);
                if (ext is null || !GeoCivil.PareceUtm(ext.Value.X, ext.Value.Y))
                    Ctx.Msg("[AVISO] O desenho não parece estar em coordenadas UTM (E 100.000–900.000, N até 10.000.000). Para levar um desenho local para o UTM, use a opção Pontos.");
                var (cod0, dif0) = GeoCivil.DefinirSistema(s.Tr, s.Db, sis0, ext ?? new Point3d(500000, 7500000, 0));
                s.Tr.Commit();
                Fim(s, sis0, cod0, dif0);
                return;
            }

            SistemaUtm? sis = det.Sistema;
            if (sis is not null) Ctx.Msg($"Sistema de destino: {sis} (das {det.Origem}).");
            var ucs = s.Ed.CurrentUserCoordinateSystem;
            var origem = new List<(double X, double Y)>();
            var destino = new List<(double X, double Y)>();
            double zSoma = 0;
            for (int i = 1; ; i++)
            {
                var po = new PromptPointOptions(i <= 2 ? $"\nPonto {i} no desenho: " : $"\nPonto {i} no desenho <Enter = terminar>: ") { AllowNone = i > 2 };
                if (i > 1) { po.UseBasePoint = true; po.BasePoint = new Point3d(origem[^1].X, origem[^1].Y, 0).TransformBy(ucs.Inverse()); }
                var pr = s.Ed.GetPoint(po);
                if (pr.Status == PromptStatus.None && i > 2) break;
                if (pr.Status != PromptStatus.OK) return;
                var pw = pr.Value.TransformBy(ucs);
                CoordenadaDigitada? cd = null;
                while (cd is null)
                {
                    var ps = new PromptStringOptions($"\nCoordenada REAL do ponto {i} (lat, long como no Google Maps — ex.: -19.9208, -43.9378 — ou E N em UTM): ") { AllowSpaces = true };
                    var rs = s.Ed.GetString(ps);
                    if (rs.Status != PromptStatus.OK) return;
                    cd = CoordenadaDigitada.Ler(rs.StringResult);
                    if (cd is null) Ctx.Msg("Não entendi a coordenada. Exemplos: -19.9208, -43.9378 | 19°55'15\"S 43°56'16\"W | 611172,06 7796931,78");
                }
                double e, n;
                if (cd.Value.Geografica)
                {
                    if (sis is null)
                    {
                        sis = SistemaUtm.DoPonto(cd.Value.Lat, cd.Value.Lon);
                        Ctx.Msg($"Sistema de destino pelo ponto digitado: {sis}.");
                    }
                    var u = Utm.ParaUtm(cd.Value.Lat, cd.Value.Lon, sis);
                    e = u.E; n = u.N;
                    Ctx.Msg($"  ponto {i}: {Angulos.Gms(cd.Value.Lat, true)} {Angulos.Gms(cd.Value.Lon, false)} → E {e.ToString("0.000", Num.Br)}  N {n.ToString("0.000", Num.Br)}");
                }
                else
                {
                    sis ??= GeoCivil.PedirManual(s);
                    if (sis is null) return;
                    e = cd.Value.E; n = cd.Value.N;
                }
                origem.Add((pw.X, pw.Y));
                destino.Add((e, n));
                zSoma += pw.Z;
            }
            if (sis is null) return;

            var livre = Transformacao2D.Ajustar(origem, destino, false);
            var fixa = Transformacao2D.Ajustar(origem, destino, true);
            string N3(double v) => v.ToString("0.000", Num.Br);
            Ctx.Msg($"--- Ajuste com {origem.Count} pontos ---");
            Ctx.Msg($"  Escala livre: escala {livre.Escala.ToString("0.000000", Num.Br)}, rotação {livre.Rotacao.ToString("0.0000", Num.Br)}°, erro médio {N3(livre.Rms)} m (máx. {N3(livre.Residuos.Max())} m)");
            Ctx.Msg($"  Escala fixa (1): rotação {fixa.Rotacao.ToString("0.0000", Num.Br)}°, erro médio {N3(fixa.Rms)} m (máx. {N3(fixa.Residuos.Max())} m)");
            if (origem.Count == 2) Ctx.Msg("  Com 2 pontos a escala livre sempre fecha exata; use 3 ou mais para conferir.");
            string sugestao = Math.Abs(livre.Escala - 1) > 0.01 ? "Livre" : "Fixa";
            if (sugestao == "Livre") Ctx.Msg($"  A escala ({livre.Escala.ToString("0.######", Num.Br)}) está longe de 1: o desenho deve estar em outra unidade (mm, cm, pés) — use Livre.");
            var esc = s.PedirPalavra("Escala", new[] { "Fixa", "Livre" }, sugestao);
            if (esc is null) return;
            var t = esc == "Livre" ? livre : fixa;
            var alvo = s.PedirPalavra("Mover [Todos = tudo do espaço do modelo / Selecao]", new[] { "Todos", "Selecao" }, "Todos");
            if (alvo is null) return;
            IEnumerable<ObjectId> ids;
            if (alvo == "Selecao")
            {
                var r = s.Ed.GetSelection();
                if (r.Status != PromptStatus.OK) return;
                ids = r.Value.GetObjectIds();
            }
            else
            {
                var l = new List<ObjectId>();
                foreach (ObjectId id in Ctx.EspacoModelo(s.Tr, s.Db, OpenMode.ForRead)) l.Add(id);
                ids = l;
            }
            // semelhança uniforme também em Z; perto de escala 1 a cota fica presa na média dos pontos clicados
            double sc = t.Escala;
            double zref = Math.Abs(sc - 1) < 0.01 ? zSoma / origem.Count : 0;
            var m = new Matrix3d(new[]
            {
                t.A, -t.B, 0, t.Tx,
                t.B, t.A, 0, t.Ty,
                0, 0, sc, (1 - sc) * zref,
                0, 0, 0, 1,
            });
            int ok = 0, falhas = 0;
            var tipos = new HashSet<string>();
            foreach (var id in ids)
            {
                try
                {
                    var en = (Entity)s.Tr.GetObject(id, OpenMode.ForWrite, false, true);
                    en.TransformBy(m);
                    ok++;
                }
                catch { falhas++; try { tipos.Add(s.Tr.GetObject(id, OpenMode.ForRead).GetType().Name); } catch { } }
            }
            var (pa, pb) = t.Aplicar(origem[0].X, origem[0].Y);
            var (cod, dif) = GeoCivil.DefinirSistema(s.Tr, s.Db, sis, new Point3d(pa, pb, 0));
            s.Tr.Commit();
            Ctx.Msg($"Desenho georreferenciado: {ok} objeto(s) movidos para {sis}" + (falhas > 0 ? $"; {falhas} não aceitaram a transformação ({string.Join(", ", tipos)})" : "") + ". U desfaz.");
            Fim(s, sis, cod, dif);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJGEORREF: " + ex.Message); }
    }

    private static Point3d? Extensao(Sessao s)
    {
        try
        {
            var a = s.Db.Extmin; var b = s.Db.Extmax;
            if (a.X > b.X) return null;
            return new Point3d((a.X + b.X) / 2, (a.Y + b.Y) / 2, 0);
        }
        catch { return null; }
    }

    private static void Fim(Sessao s, SistemaUtm sis, string codigo, double dif)
    {
        Ctx.Msg($"Sistema do desenho definido: {sis} (código {codigo} na biblioteca da Autodesk).");
        if (!double.IsNaN(dif)) Ctx.Msg($"  Conferência Autodesk × PROJETA na conversão para lat/long: {dif.ToString("0.000", Num.Br)} m.");
        Ctx.Msg("  Imagem de satélite: GEOMAP → Aérea (precisa estar conectado na conta Autodesk); para gravar a imagem no desenho use GEOMAPIMAGE.");
        if (s.PedirPalavra("Ligar o mapa de satélite agora (GEOMAP Aérea)", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            s.Doc.SendStringToExecute("_.GEOMAP _A ", true, false, false);
    }
}
