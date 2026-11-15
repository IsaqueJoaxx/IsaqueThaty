using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Geoespacial;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJINTERFERENCIAS — interferências e desapropriação: o limite da obra (polilinhas fechadas ou a borda de uma superfície, como
/// o topo do corredor, mais um afastamento) cruzado com os lotes (do desenho, de um KML/KMZ ou de um shapefile) e com postes,
/// árvores, redes, muros e edificações. Hachura as áreas a desapropriar, marca as interferências e põe os quadros e o Excel.
/// </summary>
public sealed class CmdInterferencias
{
    internal const string LayerLimite = "PRJ-DESAPROPRIACAO-LIMITE", LayerDesap = "PRJ-DESAPROPRIACAO", LayerInterf = "PRJ-INTERFERENCIAS", LayerLotes = "PRJ-LOTES-IMPORTADOS",
                          LayerQuadros = "PRJ-DESAPROPRIACAO-QUADROS";

    [CommandMethod("PRJINTERFERENCIAS", CommandFlags.Modal)]
    public static void Interferencias()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Interferencias ?? new ParametrosInterferencias();
            using var s = Sessao.Abrir();
            while (true)
            {
                Ctx.Msg(Resumo(p));
                var r = s.PedirPalavra("Interferências e desapropriação", new[] { "Analisar", "Parametros" }, "Analisar");
                if (r is null) return;
                if (r == "Parametros") { if (!PedirParametros(s, p)) return; cfg.Interferencias = p; ConfigRotatoria.Salvar(cfg); continue; }
                break;
            }

            // limite da obra
            var usados = new HashSet<ObjectId>();
            var aneis = new List<IReadOnlyList<(double X, double Y)>>();
            string origemLimite;
            var ml = s.PedirPalavra("Limite da obra", new[] { "Polilinhas", "Superficie" }, "Polilinhas");
            if (ml is null) return;
            if (ml == "Superficie")
            {
                var sups = VolumesCivil.Superficies(s.Tr);
                if (sups.Count == 0) { Ctx.Msg("Não há superfície no desenho."); return; }
                int pp = Math.Max(0, sups.FindIndex(x => x.Nome.Contains("topo", StringComparison.OrdinalIgnoreCase)));
                int k = s.EscolherDaLista("Superfície cuja borda é o limite da obra (ex.: o topo do corredor, com os taludes)", sups.Select(x => x.Nome).ToList(), pp);
                if (k < 0) return;
                aneis.AddRange(Borda(s, sups[k].Id));
                origemLimite = $"borda da superfície '{sups[k].Nome}'";
                if (aneis.Count == 0) { Ctx.Msg("Não consegui ler a borda da superfície."); return; }
            }
            else
            {
                var po = new PromptSelectionOptions { MessageForAdding = "\nSelecione o(s) limite(s) da obra (polilinhas fechadas: offset, faixa de domínio necessária): " };
                var ps = ed.GetSelection(po, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE") }));
                if (ps.Status != PromptStatus.OK) return;
                foreach (var id in ps.Value.GetObjectIds())
                    if (s.Tr.GetObject(id, OpenMode.ForRead) is Curve c && Anel(c) is { } a) { aneis.Add(a); usados.Add(id); }
                origemLimite = $"{aneis.Count} polilinha(s) fechada(s)";
                if (aneis.Count == 0) { Ctx.Msg("Nenhuma polilinha fechada na seleção."); return; }
            }
            var limite = Projeta.Core.Alinhamentos.Interferencias.Limite(aneis, p.Afastamento);

            // lotes
            var lotes = new List<LoteDesapropriacao>();
            var importados = new List<LoteDesapropriacao>();
            string fonteLotes = "nenhum";
            var avisos = new List<string>();
            var mlo = s.PedirPalavra("Lotes", new[] { "Desenho", "Kml", "Shape", "Nenhum" }, "Desenho");
            if (mlo is null) return;
            if (mlo == "Desenho")
            {
                var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os lotes (polilinhas fechadas; o número é o texto de dentro): " },
                                         new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE") }));
                if (ps.Status == PromptStatus.OK)
                {
                    var textos = Textos(s);
                    int n = 0;
                    foreach (var id in ps.Value.GetObjectIds())
                    {
                        if (usados.Contains(id) || s.Tr.GetObject(id, OpenMode.ForRead) is not Curve c || Anel(c) is not { } a) continue;
                        n++;
                        var dentro = textos.Where(t => Projeta.Core.Alinhamentos.Interferencias.Dentro(t.P, new[] { a })).Select(t => t.Texto).ToList();
                        lotes.Add(new LoteDesapropriacao { Id = dentro.Count > 0 ? string.Join(" ", dentro.Take(2)) : $"Lote {n}", Proprietario = dentro.Count > 2 ? string.Join(" ", dentro.Skip(2).Take(2)) : "",
                                                           Origem = $"desenho ({c.Layer})", Aneis = new() { a.ToList() } });
                        usados.Add(id);
                    }
                    fonteLotes = $"{lotes.Count} polilinha(s) do desenho";
                }
            }
            else if (mlo is "Kml" or "Shape")
            {
                var arq = mlo == "Kml" ? UiUtil.AbrirArquivo("Google Earth (*.kmz;*.kml)|*.kmz;*.kml|Todos os arquivos (*.*)|*.*") : UiUtil.AbrirArquivo("Shapefile (*.shp)|*.shp");
                if (arq is null) return;
                SistemaUtm? sis = null;
                if (mlo == "Kml")
                {
                    sis = GeoCivil.Pedir(s);
                    if (sis is null) return;
                    var doc = DocumentoKml.Ler(arq);
                    int n = 0;
                    foreach (var mk in doc.Itens)
                        foreach (var g in mk.Geometrias.Where(g => g.Tipo == TipoGeometriaKml.Poligono))
                        {
                            n++;
                            var a = new List<List<(double X, double Y)>> { g.Coordenadas.Select(c => Utm(c.Lat, c.Lon, sis)).ToList() };
                            a.AddRange(g.Furos.Select(f => f.Select(c => Utm(c.Lat, c.Lon, sis)).ToList()));
                            importados.Add(new LoteDesapropriacao { Id = mk.Nome.Length > 0 ? mk.Nome : $"Lote {n}", Proprietario = Limpo(mk.Descricao), Origem = "KML", Aneis = a });
                        }
                }
                else
                {
                    var feicoes = Projeta.Core.Alinhamentos.Interferencias.LerShapefile(arq).Where(f => f.Tipo == 5).ToList();
                    bool geo = Projeta.Core.Alinhamentos.Interferencias.PareceGeografico(feicoes.SelectMany(f => f.Partes.SelectMany(x => x)));
                    if (geo) { Ctx.Msg("O shapefile está em coordenadas geográficas: converto para UTM."); sis = GeoCivil.Pedir(s); if (sis is null) return; }
                    else avisos.Add("Shapefile em coordenadas planas: considerei que está no mesmo sistema do desenho (confira o .prj).");
                    int n = 0;
                    foreach (var f in feicoes)
                    {
                        n++;
                        var (id, prop) = Projeta.Core.Alinhamentos.Interferencias.IdDosAtributos(f.Atributos, n);
                        var a = f.Partes.Where(x => x.Count >= 3).Select(x => geo ? x.Select(q => Utm(q.Y, q.X, sis!)).ToList() : x).ToList();
                        if (a.Count > 0) importados.Add(new LoteDesapropriacao { Id = id, Proprietario = prop, Origem = "shapefile", Aneis = a });
                    }
                }
                lotes.AddRange(importados);
                fonteLotes = $"{importados.Count} do {(mlo == "Kml" ? "KML" : "shapefile")} {Path.GetFileName(arq)}";
                if (importados.Count == 0) avisos.Add("Nenhum polígono no arquivo.");
            }

            // objetos existentes
            var mo = s.PedirPalavra("Objetos existentes a conferir (postes, árvores, redes, muros, edificações)", new[] { "Tudo", "Selecionar", "Nenhum" }, "Tudo");
            if (mo is null) return;
            var objetos = new List<ObjetoExistente>();
            if (mo != "Nenhum")
            {
                IEnumerable<ObjectId> candidatos;
                bool explicito = mo == "Selecionar";
                if (explicito)
                {
                    var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os objetos existentes: " });
                    if (ps.Status != PromptStatus.OK) return;
                    candidatos = ps.Value.GetObjectIds();
                }
                else
                {
                    var todos = limite.SelectMany(x => x).ToList();
                    double m = p.FaixaAtencao + 5;
                    double x0 = todos.Min(q => q.X) - m, x1 = todos.Max(q => q.X) + m, y0 = todos.Min(q => q.Y) - m, y1 = todos.Max(q => q.Y) + m;
                    var lst = new List<ObjectId>();
                    foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
                    {
                        if (usados.Contains(id)) continue;
                        if (s.Tr.GetObject(id, OpenMode.ForRead) is not Entity e || e.Layer.StartsWith("PRJ-", StringComparison.OrdinalIgnoreCase)) continue;
                        try
                        {
                            var ext = e.GeometricExtents;
                            if (ext.MaxPoint.X < x0 || ext.MinPoint.X > x1 || ext.MaxPoint.Y < y0 || ext.MinPoint.Y > y1) continue;
                        }
                        catch { continue; }
                        lst.Add(id);
                    }
                    candidatos = lst;
                }
                foreach (var id in candidatos) if (Objeto(s, id, explicito) is { } o) objetos.Add(o);
            }

            // desenho anterior
            int antigos = ContarNasCamadas(s, LayerLimite, LayerDesap, LayerInterf, LayerQuadros) + (importados.Count > 0 ? ContarNasCamadas(s, LayerLotes) : 0);
            if (antigos > 0 && s.PedirPalavra($"Apagar o desenho anterior do PRJINTERFERENCIAS ({antigos} objetos nas camadas {LayerDesap}*, {LayerInterf}{(importados.Count > 0 ? ", " + LayerLotes : "")})", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                ApagarNasCamadas(s, LayerLimite, LayerDesap, LayerInterf, LayerQuadros);
                if (importados.Count > 0) ApagarNasCamadas(s, LayerLotes);
            }

            var a2 = Projeta.Core.Alinhamentos.Interferencias.Analisar(limite, lotes, objetos, p);
            a2.Avisos.AddRange(avisos);
            Desenhar(s, a2, importados);

            // quadros
            var pq = ed.GetPoint(new PromptPointOptions("\nCanto superior esquerdo dos quadros <Enter = sem quadros>: ") { AllowNone = true });
            if (pq.Status == PromptStatus.OK)
            {
                var pos = pq.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                double y = pos.Y;
                foreach (var t in new[] { RelatorioInterferencias.QuadroDesapropriacao(a2), RelatorioInterferencias.QuadroInterferencias(a2) })
                {
                    if (t.Linhas.Count == 0) continue;
                    var tid = TabelasDesenho.Inserir(s, t, new Point3d(pos.X, y, 0), 1.2, LayerQuadros);
                    double h = s.Tr.GetObject(tid, OpenMode.ForRead) is Autodesk.AutoCAD.DatabaseServices.Table tb ? tb.Height : 40;
                    y -= h + 5;
                }
            }
            s.Tr.Commit();

            int at = a2.Lotes.Count(x => x.AreaAtingida > 0);
            Ctx.Msg($"Limite da obra: {origemLimite}{(p.Afastamento > 0 ? $" + {p.Afastamento:0.##} m" : "")}, {a2.AreaLimite:N2} m².");
            Ctx.Msg($"Lotes: {a2.Lotes.Count} conferido(s) ({fonteLotes}); {at} atingido(s) ({a2.Lotes.Count(x => x.Situacao == "total")} totalmente); {a2.Lotes.Sum(x => x.AreaAtingida):N2} m² a desapropriar.");
            foreach (var l in a2.Lotes.Where(x => x.AreaAtingida > 0).OrderByDescending(x => x.AreaAtingida).Take(25))
                Ctx.Msg($"  {l.Lote.Id}{(l.Lote.Proprietario.Length > 0 ? $" ({l.Lote.Proprietario})" : "")}: {l.AreaAtingida:N2} m² de {l.Area:N2} m² ({l.Percentual * 100:0.0} %), {l.Situacao}{(l.Obs.Length > 0 ? " — " + l.Obs : "")}");
            Ctx.Msg($"Interferências: {a2.Interferencias.Count(x => x.Situacao == "dentro")} dentro do limite, {a2.Interferencias.Count(x => x.Situacao == "próximo")} a menos de {p.FaixaAtencao:0.##} m (de {objetos.Count} objeto(s) reconhecido(s)).");
            foreach (var g in a2.Interferencias.GroupBy(x => x.Objeto.Tipo).OrderBy(g => g.Key))
                Ctx.Msg($"  {g.Key}: {g.Count(x => x.Situacao == "dentro")} dentro" + (g.Any(x => x.Objeto.Linear) ? $" ({g.Sum(x => x.ComprimentoDentro):N2} m)" : "") + $", {g.Count(x => x.Situacao == "próximo")} próximo(s)");
            foreach (var av in a2.Avisos) Ctx.Msg("AVISO: " + av);
            Ctx.Msg($"Desenho: limite em {LayerLimite}, áreas a desapropriar hachuradas em {LayerDesap}, interferências marcadas em {LayerInterf}" + (importados.Count > 0 ? $", lotes importados em {LayerLotes}." : "."));

            var rel = new RelatorioInterferencias { Desenho = s.Doc.Name, Limite = origemLimite, FonteLotes = fonteLotes, Analise = a2, Parametros = p };
            if (s.PedirPalavra("Gravar o relatório em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Interferencias_e_desapropriacao.xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Planilha gravada: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJINTERFERENCIAS: {ex.Message}"); }
    }

    private static (double X, double Y) Utm(double lat, double lon, SistemaUtm sis) { var u = Projeta.Core.Geoespacial.Utm.ParaUtm(lat, lon, sis); return (u.E, u.N); }

    private static string Limpo(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", " ").Trim();

    /// <summary>Anel de uma polilinha fechada (arcos discretizados a cada 0,5 m); null se aberta.</summary>
    private static List<(double X, double Y)>? Anel(Curve c)
    {
        if (GeoCivil.PontosCurva(c, 0.5) is not { } r || !r.Fechada || r.Pontos.Count < 3) return null;
        return r.Pontos.Select(q => (q.X, q.Y)).ToList();
    }

    /// <summary>Borda da superfície (anéis externos e furos), lida das polilinhas que o Civil extrai e depois apagadas.</summary>
    private static List<IReadOnlyList<(double X, double Y)>> Borda(Sessao s, ObjectId supId)
    {
        var r = new List<IReadOnlyList<(double X, double Y)>>();
        var sup = (Surface)s.Tr.GetObject(supId, OpenMode.ForWrite);
        ObjectIdCollection ids;
        try
        {
            ids = sup switch
            {
                TinSurface tin => tin.ExtractBorder(SurfaceExtractionSettingsType.Plan),
                GridSurface grid => grid.ExtractBorder(SurfaceExtractionSettingsType.Plan),
                _ => throw new InvalidOperationException("tipo de superfície sem extração de borda"),
            };
        }
        catch (System.Exception ex) { Ctx.Msg($"A borda da superfície não foi extraída ({ex.Message})."); return r; }
        foreach (ObjectId id in ids)
        {
            var e = s.Tr.GetObject(id, OpenMode.ForWrite);
            if (e is Curve c)
            {
                var pts = new List<(double X, double Y)>();
                if (c is Polyline3d p3) foreach (ObjectId v in p3) { var pv = (PolylineVertex3d)s.Tr.GetObject(v, OpenMode.ForRead); pts.Add((pv.Position.X, pv.Position.Y)); }
                else if (GeoCivil.PontosCurva(c, 0.5) is { } pc) pts.AddRange(pc.Pontos.Select(q => (q.X, q.Y)));
                if (pts.Count > 3 && Math.Abs(pts[0].X - pts[^1].X) < 1e-6 && Math.Abs(pts[0].Y - pts[^1].Y) < 1e-6) pts.RemoveAt(pts.Count - 1);
                if (pts.Count >= 3) r.Add(pts);
            }
            e.Erase();
        }
        return r;
    }

    private static List<((double X, double Y) P, string Texto)> Textos(Sessao s)
    {
        var r = new List<((double X, double Y), string)>();
        foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
        {
            var e = s.Tr.GetObject(id, OpenMode.ForRead);
            if (e is DBText t && t.TextString.Trim().Length > 0) r.Add(((t.Position.X, t.Position.Y), t.TextString.Trim()));
            else if (e is MText m && m.Text.Trim().Length > 0) r.Add(((m.Location.X, m.Location.Y), m.Text.Trim().Replace("\r", " ").Replace("\n", " ")));
        }
        return r;
    }

    /// <summary>Objeto existente reconhecido (ou qualquer um, se selecionado à mão).</summary>
    private static ObjetoExistente? Objeto(Sessao s, ObjectId id, bool explicito)
    {
        var e = s.Tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (e is null) return null;
        string Tipo(params string?[] t) { var c = Projeta.Core.Alinhamentos.Interferencias.Classificar(t); return c.Length > 0 ? c : explicito ? "Outros" : ""; }
        switch (e)
        {
            case CogoPoint cp:
                {
                    string t = Tipo(cp.RawDescription, cp.FullDescription, cp.Layer);
                    return t.Length == 0 ? null : new ObjetoExistente { Tipo = t, Nome = $"ponto {cp.PointNumber} {cp.RawDescription}".Trim(), Camada = cp.Layer, Pontos = new() { (cp.Easting, cp.Northing) } };
                }
            case Pipe pi:
                {
                    string rede = ""; try { rede = ((Network)s.Tr.GetObject(pi.NetworkId, OpenMode.ForRead)).Name; } catch { }
                    string t = Tipo(rede, pi.Layer, pi.Name);
                    if (t.Length == 0 || t == "Outros") t = $"Rede (Civil 3D{(rede.Length > 0 ? ": " + rede : "")})";
                    return new ObjetoExistente { Tipo = t, Nome = $"tubo {pi.Name}", Camada = pi.Layer, Linear = true, Pontos = new() { (pi.StartPoint.X, pi.StartPoint.Y), (pi.EndPoint.X, pi.EndPoint.Y) } };
                }
            case Structure st:
                {
                    string rede = ""; try { rede = ((Network)s.Tr.GetObject(st.NetworkId, OpenMode.ForRead)).Name; } catch { }
                    string t = Tipo(rede, st.Layer, st.Name);
                    if (t.Length == 0 || t == "Outros") t = $"Rede (Civil 3D{(rede.Length > 0 ? ": " + rede : "")})";
                    return new ObjetoExistente { Tipo = t, Nome = $"estrutura {st.Name}", Camada = st.Layer, Pontos = new() { (st.Location.X, st.Location.Y) } };
                }
            case BlockReference br:
                {
                    string nome = br.IsDynamicBlock && s.Tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead) is BlockTableRecord bd ? bd.Name : br.Name;
                    string t = Tipo(br.Layer, nome);
                    return t.Length == 0 ? null : new ObjetoExistente { Tipo = t, Nome = $"bloco {nome}", Camada = br.Layer, Pontos = new() { (br.Position.X, br.Position.Y) } };
                }
            case DBPoint dp:
                {
                    string t = Tipo(dp.Layer);
                    return t.Length == 0 ? null : new ObjetoExistente { Tipo = t, Nome = "ponto", Camada = dp.Layer, Pontos = new() { (dp.Position.X, dp.Position.Y) } };
                }
            case Circle ci:
                {
                    string t = Tipo(ci.Layer);
                    return t.Length == 0 ? null : new ObjetoExistente { Tipo = t, Nome = $"círculo R {ci.Radius:0.00}", Camada = ci.Layer, Pontos = new() { (ci.Center.X, ci.Center.Y) } };
                }
            case Curve cv when cv is Line or Polyline or Polyline3d or Polyline2d or Arc:
                {
                    string t = Tipo(cv.Layer);
                    if (t.Length == 0) return null;
                    if (GeoCivil.PontosCurva(cv, 1.0) is not { } pc || pc.Pontos.Count < 2) return null;
                    bool fech = pc.Fechada;
                    bool area = t is "Edificação" && fech;
                    return new ObjetoExistente { Tipo = t, Nome = cv.GetType().Name.ToLowerInvariant() switch { "line" => "linha", "arc" => "arco", _ => "polilinha" } + $" {cv.Handle}", Camada = cv.Layer,
                                                 Linear = true, Fechado = fech || area, Pontos = pc.Pontos.Select(q => (q.X, q.Y)).ToList() };
                }
        }
        return null;
    }

    private static int ContarNasCamadas(Sessao s, params string[] camadas)
    {
        int n = 0;
        foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
            if (s.Tr.GetObject(id, OpenMode.ForRead) is Entity e && camadas.Any(c => string.Equals(e.Layer, c, StringComparison.OrdinalIgnoreCase))) n++;
        return n;
    }

    private static void ApagarNasCamadas(Sessao s, params string[] camadas)
    {
        foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
            if (s.Tr.GetObject(id, OpenMode.ForRead) is Entity e && camadas.Any(c => string.Equals(e.Layer, c, StringComparison.OrdinalIgnoreCase)))
            {
                e.UpgradeOpen();
                e.Erase();
            }
    }

    private static void Desenhar(Sessao s, AnaliseInterferencias a, List<LoteDesapropriacao> importados)
    {
        var ms = Ctx.EspacoModelo(s.Tr, s.Db);
        string layL = Ctx.GarantirLayer(s.Tr, s.Db, LayerLimite, 1);
        string layD = Ctx.GarantirLayer(s.Tr, s.Db, LayerDesap, 1);
        string layI = Ctx.GarantirLayer(s.Tr, s.Db, LayerInterf, 6);
        void Pl(IReadOnlyList<(double X, double Y)> pts, bool fechada, string lay, short cor, double larg = 0)
        {
            var pl = new Polyline();
            for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, larg, larg);
            pl.Closed = fechada;
            pl.SetDatabaseDefaults();
            pl.Layer = lay;
            pl.ColorIndex = cor;
            Ctx.Adicionar(s.Tr, ms, pl);
        }
        void Tx(string t, (double X, double Y) q, string lay, short cor, double h, AttachmentPoint ap = AttachmentPoint.MiddleCenter)
        {
            var m = Ctx.Texto(t, new Point3d(q.X, q.Y, 0), h, 0, ap);
            m.Layer = lay;
            m.ColorIndex = cor;
            Ctx.Adicionar(s.Tr, ms, m);
        }
        foreach (var l in a.Limite) Pl(l, true, layL, 1, 0.15);
        if (importados.Count > 0)
        {
            string layLo = Ctx.GarantirLayer(s.Tr, s.Db, LayerLotes, 8);
            foreach (var lo in importados)
            {
                foreach (var an in lo.Aneis) Pl(an, true, layLo, 8);
                Tx(lo.Id, Projeta.Core.Alinhamentos.Interferencias.PontoInterno(lo.Aneis.Cast<IReadOnlyList<(double X, double Y)>>().ToList()), layLo, 8, 1.0);
            }
        }
        foreach (var r in a.Lotes.Where(x => x.AreaAtingida > 0))
        {
            foreach (var poly in r.Atingido)
            {
                Pl(poly, true, layD, 1);
                var h = new Hatch();
                h.SetDatabaseDefaults();
                h.Layer = layD;
                h.ColorIndex = r.Situacao == "total" ? (short)1 : (short)30;
                Ctx.Adicionar(s.Tr, ms, h);
                h.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                h.PatternScale = 0.5;
                h.Associative = false;
                var vs = new Point2dCollection();
                var bs = new DoubleCollection();
                foreach (var q in poly) { vs.Add(new Point2d(q.X, q.Y)); bs.Add(0); }
                vs.Add(new Point2d(poly[0].X, poly[0].Y)); bs.Add(0);
                h.AppendLoop(HatchLoopTypes.Polyline | HatchLoopTypes.External, vs, bs);
                try { h.EvaluateHatch(true); } catch { }
            }
            Tx($"{r.Lote.Id}\\P{r.AreaAtingida:N2} m² ({r.Percentual * 100:0.0} %)", r.Rotulo, layD, 1, 1.0);
        }
        foreach (var i in a.Interferencias)
        {
            short cor = i.Situacao == "dentro" ? (short)1 : (short)2;
            if (i.Objeto.Linear) foreach (var t in i.TrechosDentro) Pl(t, false, layI, cor, 0.3);
            else Ctx.Adicionar(s.Tr, ms, new Circle(new Point3d(i.Ponto.X, i.Ponto.Y, 0), Vector3d.ZAxis, 1.0) { Layer = layI, ColorIndex = cor });
            string rot = i.Objeto.Linear && i.Situacao == "dentro" ? $"{i.Objeto.Tipo}: {i.ComprimentoDentro:N1} m" : i.Situacao == "dentro" ? i.Objeto.Tipo : $"{i.Objeto.Tipo} (a {i.Distancia:0.0} m)";
            Tx(rot, (i.Ponto.X + 1.3, i.Ponto.Y), layI, cor, 0.8, AttachmentPoint.MiddleLeft);
        }
    }

    private static string Resumo(ParametrosInterferencias p) =>
        $"Interferências: afastamento do limite {p.Afastamento:0.##} m, faixa de atenção {p.FaixaAtencao:0.##} m, lote mínimo {p.LoteMinimo:0} m²" + (p.ParametrosDoUsuario ? "." : " [PREMISSA; troque em Parametros].");

    private static bool PedirParametros(Sessao s, ParametrosInterferencias p)
    {
        var a = s.PedirNumero("Afastamento somado ao limite da obra (m)", p.Afastamento);
        if (a is null) return false;
        var f = s.PedirNumero("Faixa de atenção fora do limite (m)", p.FaixaAtencao);
        if (f is null) return false;
        var l = s.PedirNumero("Lote mínimo para o remanescente (m²)", p.LoteMinimo);
        if (l is null) return false;
        p.Afastamento = Math.Clamp(a.Value, 0, 100); p.FaixaAtencao = Math.Clamp(f.Value, 0, 50); p.LoteMinimo = Math.Max(0, l.Value); p.ParametrosDoUsuario = true;
        return true;
    }
}
