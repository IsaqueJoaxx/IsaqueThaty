using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJSINALIZA — sinalização horizontal e vertical das interseções e rotatórias que o PRJINTERSECAO e o PRJROTATORIA guardaram no
/// desenho: linhas de retenção e de dê a preferência, faixas de pedestres, legenda PARE, eixo amarelo, linhas e setas da faixa de
/// conversão, placas R-1, R-2, R-33 / R-24a, verificações e quantitativo em Excel, pelo MBST (CONTRAN, Res. 973/2022). As marcas
/// são polilinhas com hachura sólida (área = quantitativo); as placas, o poste e a placa em planta no tamanho real, mais o símbolo.
/// Rodar de novo substitui a sinalização que o próprio comando desenhou antes.
/// </summary>
public sealed class CmdSinalizacao
{
    internal const string LayerBranca = "PRJ-SINAL-H-BRANCA", LayerAmarela = "PRJ-SINAL-H-AMARELA", LayerPlacas = "PRJ-SINAL-V";

    [CommandMethod("PRJSINALIZA", CommandFlags.Modal)]
    public static void Sinalizar()
    {
        try
        {
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Sinal ?? new ParametrosSinalizacao();
            using var s = Sessao.Abrir();
            var itens = ArmazenamentoSinal.Ler(s.Tr, s.Db);
            if (itens.Count == 0)
            {
                Ctx.Msg("Este desenho não tem interseção nem rotatória do PROJETA guardada para sinalizar. Monte com o PRJINTERSECAO ou o PRJROTATORIA " +
                        "(desta versão em diante eles guardam a geometria no desenho) e rode o PRJSINALIZA de novo.");
                return;
            }
            List<ItemSinalizacao> escolhidos;
            while (true)
            {
                Ctx.Msg(Resumo(p));
                Ctx.Msg("Neste desenho: " + string.Join(", ", itens.Select(x => x.Nome + (x.Desenhados.Count > 0 ? " (já sinalizada)" : ""))));
                var r = s.PedirPalavra("Sinalizar", new[] { "Todas", "Escolher", "Parametros" }, "Todas");
                if (r is null) return;
                if (r == "Parametros") { if (!PedirParametros(s, p)) return; cfg.Sinal = p; ConfigRotatoria.Salvar(cfg); continue; }
                if (r == "Todas") { escolhidos = itens; break; }
                int k = s.EscolherDaLista("Interseção ou rotatória", itens.Select(x => $"{x.Nome} ({x.Tipo})").ToList(), 0);
                if (k < 0) return;
                escolhidos = new List<ItemSinalizacao> { itens[k] };
                break;
            }

            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            var layB = Ctx.GarantirLayer(s.Tr, s.Db, LayerBranca, 7);
            var layA = Ctx.GarantirLayer(s.Tr, s.Db, LayerAmarela, 2);
            var layV = Ctx.GarantirLayer(s.Tr, s.Db, LayerPlacas, 1);
            var resultados = new List<ResultadoSinalizacao>();
            foreach (var it in escolhidos)
            {
                var antigos = new List<ObjectId>();
                foreach (var h in it.Desenhados)
                    if (long.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out long v) && s.Db.TryGetObjectId(new Handle(v), out var oid) && !oid.IsErased)
                        antigos.Add(oid);
                if (antigos.Count > 0)
                {
                    var resp = s.PedirPalavra($"'{it.Nome}' já tem sinalização desenhada ({antigos.Count} objetos). Substituir", new[] { "Sim", "Nao" }, "Sim");
                    if (resp is null) return;
                    if (resp != "Sim") { Ctx.Msg($"'{it.Nome}': ficou a sinalização que já existe."); continue; }
                    foreach (var oid in antigos) s.Tr.GetObject(oid, OpenMode.ForWrite).Erase();
                }
                ResultadoSinalizacao? res = it.Intersecao is not null ? GeradorSinalizacao.Intersecao(it.Nome, it.Intersecao, p, it.Semaforo is not null)
                                          : it.Rotatoria is not null ? GeradorSinalizacao.Rotatoria(it.Nome, it.Rotatoria, p) : null;
                if (res is null) { Ctx.Msg($"'{it.Nome}': sem geometria guardada."); continue; }
                var ids = new List<ObjectId>();
                foreach (var m in res.Marcas) Poligono(s, ms, m.Contorno, m.Cor == "amarela" ? layA : layB, ids);
                foreach (var lg in res.Legendas)
                {
                    var mt = Ctx.Texto("{\\W0.45;" + lg.Texto + "}", new Point3d(lg.X, lg.Y, 0), lg.Altura, lg.Angulo - Math.PI / 2, AttachmentPoint.MiddleCenter);
                    mt.Layer = layB;
                    ids.Add(Ctx.Adicionar(s.Tr, ms, mt));
                }
                foreach (var pl in res.Placas) Placa(s, ms, pl, p.EscalaSimbolo, layV, ids);
                it.Desenhados = ids.Select(i => i.Handle.ToString()).ToList();
                it.Gravado = DateTime.Now;
                ArmazenamentoSinal.Gravar(s.Tr, s.Db, it);
                resultados.Add(res);
            }
            s.Tr.Commit();
            if (resultados.Count == 0) return;

            foreach (var res in resultados)
            {
                double br = res.Marcas.Where(m => m.Cor == "branca").Sum(m => m.Area), am = res.Marcas.Where(m => m.Cor == "amarela").Sum(m => m.Area);
                var partes = res.Marcas.GroupBy(m => m.Codigo).Select(g => $"{g.Key} {g.Sum(m => m.Area):0.00} m²");
                Ctx.Msg($"{res.Item} ({res.Tipo}): pintura branca {br:0.00} m², amarela {am:0.00} m² ({string.Join("; ", partes)}); " +
                        $"{res.Legendas.Count} legenda(s); placas: {(res.Placas.Count == 0 ? "nenhuma" : string.Join(", ", res.Placas.GroupBy(x => x.Codigo).Select(g => $"{g.Count()} {g.Key}")))}.");
            }
            var ver = resultados.SelectMany(r => r.Verificacoes).ToList();
            Ctx.Msg($"Verificações: {ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovada(s), {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADA(S), " +
                    $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção.");
            foreach (var v in ver.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao))
                Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor} (critério {v.Criterio}; {v.Fonte})");
            foreach (var a in resultados.SelectMany(r => r.Avisos)) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Desenho nas camadas {LayerBranca} (pintura branca), {LayerAmarela} (amarela) e {LayerPlacas} (placas: poste, placa em planta no tamanho real e símbolo de {p.EscalaSimbolo:0.##} m). " +
                    "Fica de fora (veja as pendências no Excel): A-12 / A-15, refúgio e rebaixamento das calçadas, zebrado de aproximação das ilhas, R-19 e tachas. U desfaz a sinalização.");

            var rel = new RelatorioSinalizacao { Titulo = "Sinalização - " + string.Join(", ", resultados.Select(r => r.Item)), Desenho = s.Doc.Name };
            rel.Itens.AddRange(resultados);
            rel.Parametros.AddRange(RelatorioSinalizacao.ParametrosDe(p));
            rel.Pendencias.AddRange(RelatorioSinalizacao.PendenciasPadrao());
            if (s.PedirPalavra("Gravar o quantitativo da sinalização em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                string nomeArq = resultados.Count == 1 ? "Sinalizacao_" + Texto.IdSeguro(resultados[0].Item.Replace(' ', '_'), 50) : "Sinalizacao";
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", nomeArq + ".xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Quantitativo gravado: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJSINALIZA: {ex.Message}"); }
    }

    private static string Resumo(ParametrosSinalizacao p) =>
        $"Sinalização (MBST 2022): via {p.TipoVia}, {p.Velocidade:0} km/h; faixas de pedestres: {p.FaixasPedestres}; LRE {p.LarguraLre:0.00} m; LDP {p.LarguraLdp:0.00} m " +
        $"(traço {p.TracoLdp:0.00}); FTP {p.LinhaFtp:0.00}/{p.EspacoFtp:0.00} m × {p.ExtensaoFtp:0.00} m; legenda PARE {(p.LegendaPare ? $"{p.AlturaLegenda:0.00} m" : "não")}; " +
        $"R-1 lado {p.LadoR1:0.00}, R-2 lado {p.LadoR2:0.00}, circulares Ø {p.DiametroCircular:0.00} m; rotatória {p.FaixasEntrada} faixa(s) por entrada, {p.FaixasAnel} no anel" +
        (p.ParametrosDoUsuario ? "." : " [PREMISSA nos valores escolhidos dentro das faixas do manual; troque em Parametros].");

    private static bool PedirParametros(Sessao s, ParametrosSinalizacao p)
    {
        var t = s.PedirPalavra("Tipo de via", new[] { "Urbana", "Estrada", "Rodovia" }, p.TipoVia.ToString());
        if (t is null) return false;
        p.TipoVia = Enum.Parse<TipoViaSinal>(t);
        var v = s.PedirNumero("Velocidade regulamentada das aproximações (km/h)", p.Velocidade);
        if (v is null) return false;
        p.Velocidade = Math.Clamp(v.Value, 10, 120);
        var f = s.PedirPalavra("Faixas de pedestres na interseção", new[] { "Secundarias", "Todas", "Nenhuma" }, p.FaixasPedestres);
        if (f is null) return false;
        p.FaixasPedestres = f;
        var l = s.PedirPalavra("Legenda PARE no pavimento", new[] { "Sim", "Nao" }, p.LegendaPare ? "Sim" : "Nao");
        if (l is null) return false;
        p.LegendaPare = l == "Sim";
        double? Faixa(string msg, double atual, double min, double max)
        {
            var x = s.PedirNumero(msg, atual);
            if (x is null) return null;
            double c = Math.Clamp(x.Value, min, max);
            if (Math.Abs(c - x.Value) > 1e-9) Ctx.Msg($"  fora da faixa do manual: usei {c:0.00}.");
            return c;
        }
        if (Faixa("Faixas por entrada da rotatória (1 ou 2)", p.FaixasEntrada, 1, 2) is not { } fe) return false;
        p.FaixasEntrada = (int)Math.Round(fe);
        if (Faixa("Faixas no anel da rotatória (1 ou 2)", p.FaixasAnel, 1, 2) is not { } fa) return false;
        p.FaixasAnel = (int)Math.Round(fa);
        if (Faixa("Largura da linha de retenção (0,30 a 0,60 m)", p.LarguraLre, 0.30, 0.60) is not { } lre) return false;
        p.LarguraLre = lre;
        if (Faixa("Largura da linha de dê a preferência (0,20 a 0,40 m)", p.LarguraLdp, 0.20, 0.40) is not { } ldp) return false;
        p.LarguraLdp = ldp;
        if (Faixa("Faixa de pedestres: largura das linhas (0,30 a 0,40 m)", p.LinhaFtp, 0.30, 0.40) is not { } lf) return false;
        p.LinhaFtp = lf;
        if (Faixa("Faixa de pedestres: espaço entre as linhas (0,30 a 0,80 m)", p.EspacoFtp, 0.30, 0.80) is not { } ef) return false;
        p.EspacoFtp = ef;
        if (Faixa("Extensão da faixa de pedestres (mínimo 3,00; recomendada 4,00 m)", p.ExtensaoFtp, 3.00, 10.0) is not { } ext) return false;
        p.ExtensaoFtp = ext;
        if (Faixa("Recuo da faixa de pedestres atrás da LDP na rotatória (m)", p.RecuoFtpRotatoria, 0, 30) is not { } rec) return false;
        p.RecuoFtpRotatoria = rec;
        if (Faixa("Comprimento da linha contínua amarela no eixo junto da interseção (m)", p.ComprimentoLfo, 0, 200) is not { } lfo) return false;
        p.ComprimentoLfo = lfo;
        if (Faixa("Tamanho do símbolo das placas no desenho (m)", p.EscalaSimbolo, 0.3, 10) is not { } esc) return false;
        p.EscalaSimbolo = esc;
        p.ParametrosDoUsuario = true;
        return true;
    }

    /// <summary>Marca pintada: polilinha fechada e hachura sólida na mesma camada.</summary>
    private static void Poligono(Sessao s, BlockTableRecord ms, IReadOnlyList<(double X, double Y)> pts, string layer, List<ObjectId> ids)
    {
        if (pts.Count < 3) return;
        var pl = new Polyline();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        pl.Closed = true;
        pl.SetDatabaseDefaults();
        pl.Layer = layer;
        ids.Add(Ctx.Adicionar(s.Tr, ms, pl));
        var h = new Hatch();
        h.SetDatabaseDefaults();
        h.Layer = layer;
        ids.Add(Ctx.Adicionar(s.Tr, ms, h));
        h.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        h.Associative = false;
        var vs = new Point2dCollection();
        var bs = new DoubleCollection();
        foreach (var q in pts) { vs.Add(new Point2d(q.X, q.Y)); bs.Add(0); }
        vs.Add(new Point2d(pts[0].X, pts[0].Y));
        bs.Add(0);
        h.AppendLoop(HatchLoopTypes.Polyline | HatchLoopTypes.External, vs, bs);
        h.EvaluateHatch(true);
    }

    /// <summary>Placa: poste, placa em planta (segmento na largura real, de frente para quem chega), símbolo e linha de chamada.</summary>
    private static void Placa(Sessao s, BlockTableRecord ms, PlacaSinal pl, double escala, string layer, List<ObjectId> ids)
    {
        var poste = new Point3d(pl.X, pl.Y, 0);
        ids.Add(Ctx.Adicionar(s.Tr, ms, new Circle(poste, Vector3d.ZAxis, 0.06) { Layer = layer }));
        double px = -Math.Sin(pl.Angulo), py = Math.Cos(pl.Angulo);
        ids.Add(Ctx.Adicionar(s.Tr, ms, new Line(new Point3d(pl.X - px * pl.Largura / 2, pl.Y - py * pl.Largura / 2, 0),
                                                  new Point3d(pl.X + px * pl.Largura / 2, pl.Y + py * pl.Largura / 2, 0)) { Layer = layer }));
        // símbolo antes do poste (de onde vem o tráfego), com o "para cima" no sentido do tráfego; na ilha central (R-24a), depois dele, dentro da ilha
        double lado = pl.Codigo == "R-24a" ? 1 : -1;
        var c = new Point3d(pl.X + lado * Math.Cos(pl.Angulo) * escala * 0.9, pl.Y + lado * Math.Sin(pl.Angulo) * escala * 0.9, 0);
        ids.Add(Ctx.Adicionar(s.Tr, ms, new Line(c, poste) { Layer = layer, ColorIndex = 8 }));
        var br = new BlockReference(c, Bloco(s, pl.Codigo)) { ScaleFactors = new Scale3d(escala), Rotation = pl.Angulo - Math.PI / 2 };
        br.SetDatabaseDefaults();
        br.Layer = layer;
        ids.Add(Ctx.Adicionar(s.Tr, ms, br));
    }

    /// <summary>Bloco do símbolo da placa (1 m de largura), criado no desenho na primeira vez.</summary>
    private static ObjectId Bloco(Sessao s, string codigo)
    {
        string nome = "PRJ_PLACA_" + codigo.Replace("-", "").ToUpperInvariant();
        var bt = (BlockTable)s.Tr.GetObject(s.Db.BlockTableId, OpenMode.ForRead);
        if (bt.Has(nome)) return bt[nome];
        bt.UpgradeOpen();
        var btr = new BlockTableRecord { Name = nome, Origin = Point3d.Origin };
        var id = bt.Add(btr);
        s.Tr.AddNewlyCreatedDBObject(btr, true);
        void Add(Entity e) { btr.AppendEntity(e); s.Tr.AddNewlyCreatedDBObject(e, true); }
        Polyline Fechada(IEnumerable<(double X, double Y)> pts)
        {
            var p = new Polyline();
            int i = 0;
            foreach (var q in pts) p.AddVertexAt(i++, new Point2d(q.X, q.Y), 0, 0, 0);
            p.Closed = true;
            p.ColorIndex = 1;
            return p;
        }
        MText Txt(string t, double x, double y, double h) => new() { Contents = t, TextHeight = h, Location = new Point3d(x, y, 0), Attachment = AttachmentPoint.MiddleCenter, ColorIndex = 1 };
        switch (codigo)
        {
            case "R-1":
            {
                double r = 0.5 / Math.Cos(Math.PI / 8);
                Add(Fechada(Enumerable.Range(0, 8).Select(k => (r * Math.Cos(Math.PI / 8 + k * Math.PI / 4), r * Math.Sin(Math.PI / 8 + k * Math.PI / 4)))));
                Add(Txt("PARE", 0, 0, 0.24));
                break;
            }
            case "R-2":
                Add(Fechada(new[] { (-0.5, 0.289), (0.5, 0.289), (0.0, -0.577) }));
                Add(Txt("R-2", 0, 0.08, 0.14));
                break;
            case "R-24a":
                Add(new Circle(Point3d.Origin, Vector3d.ZAxis, 0.5) { ColorIndex = 1 });
                Add(new Line(new Point3d(-0.28, 0.05, 0), new Point3d(0.22, 0.05, 0)) { ColorIndex = 7 });
                Add(Fechada(new[] { (0.18, 0.15), (0.32, 0.05), (0.18, -0.05) }));
                Add(Txt("R-24a", 0, -0.22, 0.12));
                break;
            default:
                Add(new Circle(Point3d.Origin, Vector3d.ZAxis, 0.5) { ColorIndex = 1 });
                Add(new Arc(Point3d.Origin, Vector3d.ZAxis, 0.3, Math.PI * 0.15, Math.PI * 1.85) { ColorIndex = 7 });
                Add(Txt(codigo, 0, 0, 0.14));
                break;
        }
        return id;
    }
}
