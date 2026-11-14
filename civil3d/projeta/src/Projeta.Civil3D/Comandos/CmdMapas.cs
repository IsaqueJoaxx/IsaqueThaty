using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Relatorios;
using Projeta.Core.Superficies;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJVOLRAPIDO — corte e aterro instantâneo numa região (polilinha ou pontos clicados), com mapa de cores transparente
/// e tabela-resumo no desenho. PRJFAIXAS — faixas de cota ou de declividade de uma superfície, com as cores aplicadas
/// na própria superfície do Civil e tabela de áreas por faixa.
/// </summary>
public sealed class CmdMapas
{
    // ================================================================== PRJVOLRAPIDO

    [CommandMethod("PRJVOLRAPIDO", CommandFlags.Modal)]
    public static void VolumeRapido()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sups = VolumesCivil.Superficies(s.Tr);
            if (sups.Count < 2) { Ctx.Msg("São precisas duas superfícies (base e comparação) no desenho."); return; }
            var nomes = sups.Select(x => x.Nome).ToList();
            var cfg = ConfigGeo.Carregar();
            int pb = cfg.BaseVolume is not null ? nomes.IndexOf(cfg.BaseVolume) : -1;
            if (pb < 0) pb = Math.Max(0, NotaServicoCivil.SuperficiePadrao(nomes, Array.Empty<string>()));
            int ib = s.EscolherDaLista("Superfície BASE (terreno / levantamento anterior)", nomes, pb);
            if (ib < 0) return;
            int pc = cfg.ComparacaoVolume is not null ? nomes.IndexOf(cfg.ComparacaoVolume) : -1;
            if (pc < 0 || pc == ib) pc = Enumerable.Range(0, nomes.Count).First(i => i != ib);
            int ic = s.EscolherDaLista("Superfície de COMPARAÇÃO (projeto / levantamento novo)", nomes, pc);
            if (ic < 0) return;
            if (ic == ib) { Ctx.Msg("Escolha superfícies diferentes."); return; }
            cfg.BaseVolume = nomes[ib]; cfg.ComparacaoVolume = nomes[ic];
            ConfigGeo.Salvar(cfg);

            var modo = s.PedirPalavra("Região [Polilinha = selecionar polígonos fechados / Pontos = clicar os vértices]", new[] { "Polilinha", "Pontos" }, "Polilinha");
            if (modo is null) return;
            List<VolumesCivil.Poligono>? poligonos;
            ObjectId contorno = ObjectId.Null;
            if (modo == "Polilinha") poligonos = VolumesCivil.SelecionarPoligonos(s, "Selecione a(s) polilinha(s) fechada(s) da região");
            else
            {
                var vs = ClicarPoligono(s);
                if (vs is null) return;
                poligonos = new List<VolumesCivil.Poligono> { new("Região clicada", vs, "") };
                // o contorno clicado fica no desenho, na camada do mapa
                var pl = new Polyline();
                for (int i = 0; i < vs.Count; i++) pl.AddVertexAt(i, new Point2d(vs[i].X, vs[i].Y), 0, 0, 0);
                pl.Closed = true;
                pl.SetDatabaseDefaults();
                pl.Layer = Ctx.GarantirLayer(s.Tr, s.Db, VolumesCivil.LayerMapa, 8);
                pl.ColorIndex = 7;
                contorno = Ctx.Adicionar(s.Tr, Ctx.EspacoModelo(s.Tr, s.Db), pl);
            }
            if (poligonos is null || poligonos.Count == 0) return;

            Ctx.Msg("Calculando...");
            var saida = VolumesCivil.Calcular(s.Db, new VolumesCivil.Entrada
            {
                Base = sups[ib].Id, Comparacao = sups[ic].Id, Poligonos = poligonos, Desenho = s.Doc.Name,
            });
            var rel = saida.Relatorio;

            var antigos = VolumesCivil.EntidadesDoMapa(s.Tr, s.Db).Where(id => id != contorno).ToList();
            if (antigos.Count > 0 && s.PedirPalavra($"Apagar o mapa anterior ({antigos.Count} objeto(s) na camada {VolumesCivil.LayerMapa})", new[] { "Sim", "Nao" }, "Sim") == "Sim")
                foreach (var id in antigos) ((Entity)s.Tr.GetObject(id, OpenMode.ForWrite)).Erase();
            int n = VolumesCivil.DesenharMapa(s.Tr, s.Db, saida.Malha, rel);
            // só os retângulos coloridos ficam transparentes (legenda e tabela continuam legíveis)
            var transp = new Autodesk.AutoCAD.Colors.Transparency((byte)Math.Round(255 * 0.6));
            foreach (var id in VolumesCivil.EntidadesDoMapa(s.Tr, s.Db))
                if (s.Tr.GetObject(id, OpenMode.ForRead) is Solid so) { so.UpgradeOpen(); so.Transparency = transp; }

            // tabela-resumo logo abaixo da região
            var todos = poligonos.SelectMany(p => p.Vertices).ToList();
            double x0 = todos.Min(v => v.X), x1 = todos.Max(v => v.X), y0 = todos.Min(v => v.Y), y1 = todos.Max(v => v.Y);
            double h = Math.Clamp(Math.Max(x1 - x0, y1 - y0) / 70, 0.5, 25);
            var tab = Resumo(rel);
            var tid = TabelasDesenho.Inserir(s, tab, new Point3d(x0, y0 - 2 * h, 0), h, "PRJ-VOLUMES-TABELA");
            try
            {
                // a tabela fica por cima da superfície (que pode estar colorida por faixas)
                var ms = Ctx.EspacoModelo(s.Tr, s.Db, OpenMode.ForRead);
                var dot = (DrawOrderTable)s.Tr.GetObject(ms.DrawOrderTableId, OpenMode.ForWrite);
                dot.MoveToTop(new ObjectIdCollection { tid });
            }
            catch { }
            s.Tr.Commit();

            var tot = rel.Areas.Count > 1 ? rel.Total : rel.Areas[0];
            string V(double v) => double.IsNaN(v) ? "-" : v.ToString("#,##0.00", Num.Br);
            Ctx.Msg($"--- Volume: {rel.Comparacao} − {rel.Base} ---");
            Ctx.Msg($"  Área {V(tot.Area2D)} m²  |  corte {V(tot.Corte)} m³  |  aterro {V(tot.Aterro)} m³  |  saldo {V(rel.Saldo(tot))} m³ — {rel.SituacaoDe(tot)}");
            Ctx.Msg($"  Mapa de corte e aterro ({n} retângulos coloridos por faixa de altura, 40 % transparentes) e legenda na camada {VolumesCivil.LayerMapa}; tabela-resumo na PRJ-VOLUMES-TABELA. U desfaz. Para Excel e fatores de empolamento use PRJVOLUMES.");
            foreach (var a in rel.Avisos) Ctx.Msg("  [AVISO] " + a);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJVOLRAPIDO: " + ex.Message); }
    }

    internal static TabelaDados Resumo(RelatorioVolumes rel)
    {
        string V(double v) => double.IsNaN(v) ? "-" : v.ToString("#,##0", Num.Br);
        var t = new TabelaDados { Titulo = $"CORTE E ATERRO: {rel.Comparacao} − {rel.Base}" };
        t.Colunas = new List<string> { "Região", "Área (m²)", "Corte (m³)", "Aterro (m³)", "Saldo (m³)", "Situação" };
        foreach (var a in rel.Areas) t.Linhas.Add(new[] { a.Nome, V(a.Area2D), V(a.Corte), V(a.Aterro), V(rel.Saldo(a)), rel.SituacaoDe(a) });
        if (rel.Areas.Count > 1) { var x = rel.Total; t.Linhas.Add(new[] { "TOTAL", V(x.Area2D), V(x.Corte), V(x.Aterro), V(rel.Saldo(x)), rel.SituacaoDe(x) }); }
        return t;
    }

    /// <summary>Clica os vértices de um polígono (Enter fecha); linha elástica a partir do último ponto.</summary>
    private static List<(double X, double Y)>? ClicarPoligono(Sessao s)
    {
        var ucs = s.Ed.CurrentUserCoordinateSystem;
        var l = new List<(double X, double Y)>();
        Point3d ultimo = Point3d.Origin;
        while (true)
        {
            var po = new PromptPointOptions(l.Count < 3 ? $"\nVértice {l.Count + 1} da região: " : $"\nVértice {l.Count + 1} <Enter = fechar>: ") { AllowNone = l.Count >= 3 };
            if (l.Count > 0) { po.UseBasePoint = true; po.BasePoint = ultimo; }
            var r = s.Ed.GetPoint(po);
            if (r.Status == PromptStatus.None && l.Count >= 3) break;
            if (r.Status != PromptStatus.OK) return null;
            ultimo = r.Value;
            var w = r.Value.TransformBy(ucs);
            l.Add((w.X, w.Y));
        }
        return Malha.AreaPoligono(l) > 1e-6 ? l : null;
    }

    internal static void Transparencia(Sessao s, string camada, int porcento)
    {
        try
        {
            var lt = (LayerTable)s.Tr.GetObject(s.Db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(camada)) return;
            var ltr = (LayerTableRecord)s.Tr.GetObject(lt[camada], OpenMode.ForWrite);
            ltr.Transparency = new Autodesk.AutoCAD.Colors.Transparency((byte)Math.Round(255 * (100 - Math.Clamp(porcento, 0, 90)) / 100.0));
        }
        catch { }
    }

    // ================================================================== PRJFAIXAS

    [CommandMethod("PRJFAIXAS", CommandFlags.Modal)]
    public static void Faixas()
    {
        try
        {
            using var s = Sessao.Abrir();
            var tins = new List<(ObjectId Id, string Nome)>();
            foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
                if (s.Tr.GetObject(id, OpenMode.ForRead) is TinSurface t) tins.Add((id, t.Name));
            if (tins.Count == 0) { Ctx.Msg("Não há superfície TIN no desenho."); return; }
            int i = tins.Count == 1 ? 0 : s.EscolherDaLista("Superfície", tins.Select(x => x.Nome).ToList());
            if (i < 0) return;
            var tin = (TinSurface)s.Tr.GetObject(tins[i].Id, OpenMode.ForRead);
            var tipo = s.PedirPalavra("Faixas de", new[] { "Cotas", "Declividades" }, "Cotas");
            if (tipo is null) return;
            bool cotas = tipo == "Cotas";
            var cfg = ConfigGeo.Carregar();

            var tri = new List<(P3, P3, P3)>();
            foreach (TinSurfaceTriangle t in tin.GetTriangles(false))
            {
                var a = t.Vertex1.Location; var b = t.Vertex2.Location; var c = t.Vertex3.Location;
                tri.Add((new P3(a.X, a.Y, a.Z), new P3(b.X, b.Y, b.Z), new P3(c.X, c.Y, c.Z)));
            }
            if (tri.Count == 0) { Ctx.Msg("A superfície não tem triângulos visíveis."); return; }
            double zmin = tri.Min(t => Math.Min(t.Item1.Z, Math.Min(t.Item2.Z, t.Item3.Z))), zmax = tri.Max(t => Math.Max(t.Item1.Z, Math.Max(t.Item2.Z, t.Item3.Z)));

            List<FaixaMapa> faixas;
            if (cotas)
            {
                var m = s.PedirPalavra($"Cotas de {zmin.ToString("0.00", Num.Br)} a {zmax.ToString("0.00", Num.Br)} m: dividir por [Quantidade/Passo]", new[] { "Quantidade", "Passo" }, "Quantidade");
                if (m is null) return;
                if (m == "Quantidade")
                {
                    var q = s.PedirNumero("Quantas faixas (aproximado; o passo é arredondado)", cfg.FaixasQuantidade);
                    if (q is null) return;
                    cfg.FaixasQuantidade = Math.Clamp((int)Math.Round(q.Value), 2, 40);
                    faixas = MapaFaixas.Cotas(zmin, zmax, cfg.FaixasQuantidade);
                }
                else
                {
                    var p = s.PedirNumero("Passo das faixas (m)", MapaFaixas.PassoRedondo((zmax - zmin) / 8));
                    if (p is null || !(p > 0)) return;
                    faixas = MapaFaixas.CotasComPasso(zmin, zmax, p.Value);
                }
                ConfigGeo.Salvar(cfg);
            }
            else
            {
                var m = s.PedirPalavra("Classes [Embrapa = 0-3-8-20-45-75 % / Limites = digitar]", new[] { "Embrapa", "Limites" }, "Embrapa");
                if (m is null) return;
                if (m == "Embrapa") faixas = MapaFaixas.Declividades();
                else
                {
                    var txt = s.PedirTexto("Limites em % separados por ponto e vírgula", "5; 10; 15; 30");
                    if (txt is null) return;
                    var lim = txt.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Num.Ler(x)).Where(v => v > 0).ToList();
                    if (lim.Count == 0) { Ctx.Msg("Nenhum limite válido."); return; }
                    faixas = MapaFaixas.Declividades(lim);
                }
            }

            double total = cotas ? MapaFaixas.AreasPorCota(tri, faixas) : MapaFaixas.AreasPorDeclividade(tri, faixas);
            double smax = cotas ? 0 : tri.Select(t => MapaFaixas.Declividade(t.Item1, t.Item2, t.Item3)).Where(v => !double.IsNaN(v)).DefaultIfEmpty(0).Max();

            // cores na própria superfície (análise do Civil) e estilo que mostra só as faixas e o contorno
            var aplicar = s.PedirPalavra("Colorir a superfície no desenho (troca o estilo para um que mostra as faixas)", new[] { "Sim", "Nao" }, "Sim");
            if (aplicar is null) return;
            string? estiloAntigo = null;
            if (aplicar == "Sim")
            {
                tin.UpgradeOpen();
                if (cotas)
                {
                    var dados = faixas.Select((f, k) => new SurfaceAnalysisElevationData(k == 0 ? Math.Min(f.De, zmin) : f.De, k == faixas.Count - 1 ? Math.Max(f.Ate, zmax) : f.Ate, GeoCivil.Cor(f.Cor))).ToArray();
                    tin.Analysis.SetElevationData(dados);
                }
                else
                {
                    var dados = faixas.Select(f => new SurfaceAnalysisSlopeData(f.De / 100, double.IsPositiveInfinity(f.Ate) ? Math.Max(smax, f.De + 1) / 100 : f.Ate / 100, GeoCivil.Cor(f.Cor))).ToArray();
                    tin.Analysis.SetSlopeData(dados);
                }
                try { estiloAntigo = ((StyleBase)s.Tr.GetObject(tin.StyleId, OpenMode.ForRead)).Name; } catch { }
                tin.StyleId = EstiloFaixas(s, cotas);
            }

            // tabela à direita da superfície, com a cor de cada faixa
            var g = tin.GetGeneralProperties();
            double h = Math.Clamp(Math.Max(g.MaximumCoordinateX - g.MinimumCoordinateX, g.MaximumCoordinateY - g.MinimumCoordinateY) / 70, 0.5, 25);
            string unidade = cotas ? "m" : "%";
            var dadosTab = MapaFaixas.Tabela(cotas ? $"FAIXAS DE COTA: {tin.Name}" : $"DECLIVIDADE: {tin.Name}", faixas, unidade);
            dadosTab.Colunas.Insert(0, "Cor");
            for (int k = 0; k < dadosTab.Linhas.Count; k++) dadosTab.Linhas[k] = new[] { "" }.Concat(dadosTab.Linhas[k]).ToArray();
            const string camada = "PRJ-FAIXAS";
            // à direita da superfície e das tabelas de faixas que já estiverem lá
            double xt = g.MaximumCoordinateX + 4 * h;
            foreach (ObjectId oid in Ctx.EspacoModelo(s.Tr, s.Db, OpenMode.ForRead))
            {
                if (s.Tr.GetObject(oid, OpenMode.ForRead) is not Autodesk.AutoCAD.DatabaseServices.Table velha || !string.Equals(velha.Layer, camada, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var ext = velha.GeometricExtents;
                    if (ext.MaxPoint.X > xt - 2 * h && ext.MinPoint.Y < g.MaximumCoordinateY + h && ext.MaxPoint.Y > g.MinimumCoordinateY) xt = Math.Max(xt, ext.MaxPoint.X + 3 * h);
                }
                catch { }
            }
            var tid = TabelasDesenho.Inserir(s, dadosTab, new Point3d(xt, g.MaximumCoordinateY, 0), h, camada);
            var tb = (Autodesk.AutoCAD.DatabaseServices.Table)s.Tr.GetObject(tid, OpenMode.ForWrite);
            for (int k = 0; k < faixas.Count; k++)
            {
                var cel = tb.Cells[k + 2, 0];
                cel.BackgroundColor = GeoCivil.Cor(faixas[k].Cor);
                cel.IsBackgroundColorNone = false;
            }
            tb.Columns[0].Width = 4 * h;
            tb.GenerateLayout();
            s.Tr.Commit();

            Ctx.Msg($"--- {(cotas ? "Faixas de cota" : "Declividade")} de '{tin.Name}' ({tri.Count:N0} triângulos, área {(total / 10000).ToString("#,##0.00", Num.Br)} ha) ---");
            foreach (var f in faixas)
                Ctx.Msg($"  {MapaFaixas.Intervalo(f, unidade)}{(f.Classe.Length > 0 ? " — " + f.Classe : "")}: {(f.Area / 10000).ToString("#,##0.00", Num.Br)} ha ({(total > 0 ? f.Area / total * 100 : 0).ToString("0.0", Num.Br)} %)");
            Ctx.Msg("  Áreas em planta exatas (cada triângulo cortado nas cotas-limite)." + (cotas ? "" : " Declividade de cada triângulo, em %."));
            if (estiloAntigo is not null) Ctx.Msg($"  Estilo trocado de '{estiloAntigo}' para '{(cotas ? "PRJ Faixas de cota" : "PRJ Faixas de declividade")}'. U desfaz; ou volte o estilo nas propriedades da superfície.");
            Ctx.Msg($"  Tabela na camada {camada}, à direita da superfície.");
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJFAIXAS: " + ex.Message); }
    }

    /// <summary>Estilo de superfície que mostra só as faixas (cota ou declividade) e o contorno.</summary>
    private static ObjectId EstiloFaixas(Sessao s, bool cotas)
    {
        var nome = cotas ? "PRJ Faixas de cota" : "PRJ Faixas de declividade";
        var col = CivilApplication.ActiveDocument.Styles.SurfaceStyles;
        var id = col.Contains(nome) ? col[nome] : col.Add(nome);
        var st = (SurfaceStyle)s.Tr.GetObject(id, OpenMode.ForWrite);
        var mostrar = cotas ? SurfaceDisplayStyleType.Elevations : SurfaceDisplayStyleType.Slopes;
        foreach (SurfaceDisplayStyleType t in Enum.GetValues(typeof(SurfaceDisplayStyleType)))
        {
            bool vis = t == mostrar || t == SurfaceDisplayStyleType.Boundary;
            try { st.GetDisplayStylePlan(t).Visible = vis; } catch { }
            try { st.GetDisplayStyleModel(t).Visible = vis; } catch { }
        }
        return id;
    }
}
