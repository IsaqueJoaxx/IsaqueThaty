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
/// PRJSEMAFORO — plano semafórico de tempo fixo de uma interseção montada pelo PRJINTERSECAO: um estágio por via, entreverdes
/// (amarelo e vermelho geral), ciclo ótimo de Webster, verdes, pedestres, grau de saturação, atraso e fila (MBST Vol. V); desenha
/// os grupos focais (coluna depois do cruzamento com braço projetado, pedestres nas pontas das faixas), os laços (opcional), o
/// diagrama de estágios e o diagrama de tempos, e grava o Excel. Depois, o PRJSINALIZA põe retenção em todas as aproximações.
/// </summary>
public sealed class CmdSemaforo
{
    internal const string Layer = "PRJ-SEMAFORO", LayerDiagrama = "PRJ-SEMAFORO-DIAGRAMA", LayerLacos = "PRJ-SEMAFORO-LACOS";

    [CommandMethod("PRJSEMAFORO", CommandFlags.Modal)]
    public static void Semaforo()
    {
        try
        {
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Semaforo ?? new ParametrosSemaforo();
            var ps = cfg.Sinal ?? new ParametrosSinalizacao();
            using var s = Sessao.Abrir();
            var itens = ArmazenamentoSinal.Ler(s.Tr, s.Db).Where(x => x.Intersecao is not null).ToList();
            if (itens.Count == 0)
            {
                Ctx.Msg("Este desenho não tem interseção do PROJETA guardada. Monte com o PRJINTERSECAO (desta versão em diante ele guarda a geometria) e rode o PRJSEMAFORO de novo.");
                return;
            }
            while (true)
            {
                Ctx.Msg(Resumo(p));
                var r = s.PedirPalavra("Semáforo", new[] { "Calcular", "Parametros" }, "Calcular");
                if (r is null) return;
                if (r == "Parametros") { if (!PedirParametros(s, p)) return; cfg.Semaforo = p; ConfigRotatoria.Salvar(cfg); continue; }
                break;
            }
            int k = itens.Count == 1 ? 0 : s.EscolherDaLista("Interseção", itens.Select(x => x.Nome + (x.Semaforo is not null ? $" (semáforo, ciclo {x.Semaforo.Ciclo:0} s)" : "")).ToList(), 0);
            if (k < 0) return;
            var it = itens[k];
            var m = it.Intersecao!;
            var dados = it.Semaforo ?? new DadosSemaforo();
            var vols = new List<double>();
            Ctx.Msg($"{it.Nome}: informe a contagem da hora de pico em ucp/h (sem contagem, os valores sugeridos são só um ponto de partida [PREMISSA]).");
            foreach (var b in m.Bracos)
            {
                double padrao = dados.Volumes.TryGetValue(b.Rumo, out var vv) ? vv : b.Principal ? 600 : 250;
                var v = s.PedirNumero($"Volume que chega pelo braço {b.Rumo} ('{b.Nome}') na hora de pico (ucp/h)", padrao);
                if (v is null) return;
                vols.Add(v.Value);
                dados.Volumes[b.Rumo] = v.Value;
            }
            var plano = CalculoSemaforico.Calcular(it.Nome, m, vols, p, ps);

            var antigos = new List<ObjectId>();
            foreach (var h in dados.Desenhados)
                if (long.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out long hv) && s.Db.TryGetObjectId(new Handle(hv), out var oid) && !oid.IsErased)
                    antigos.Add(oid);
            if (antigos.Count > 0)
            {
                var resp = s.PedirPalavra($"'{it.Nome}' já tem semáforo desenhado ({antigos.Count} objetos). Substituir", new[] { "Sim", "Nao" }, "Sim");
                if (resp != "Sim") return;
                foreach (var oid in antigos) s.Tr.GetObject(oid, OpenMode.ForWrite).Erase();
            }

            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            var lay = Ctx.GarantirLayer(s.Tr, s.Db, Layer, 3);
            var layD = Ctx.GarantirLayer(s.Tr, s.Db, LayerDiagrama, 7);
            var layL = Ctx.GarantirLayer(s.Tr, s.Db, LayerLacos, 6);
            var ids = new List<ObjectId>();
            double esc = ps.EscalaSimbolo;
            // grupos focais
            foreach (var c in plano.Cabecas)
            {
                bool ped = c.Tipo == "pedestres";
                var pc = new Point3d(c.X, c.Y, 0);
                if (c.Tipo.Contains("coluna") || ped)
                    ids.Add(Ctx.Adicionar(s.Tr, ms, new Circle(new Point3d(c.PosteX, c.PosteY, 0), Vector3d.ZAxis, 0.15) { Layer = lay }));
                if (c.Tipo.Contains("projetado"))
                    ids.Add(Ctx.Adicionar(s.Tr, ms, new Line(new Point3d(c.PosteX, c.PosteY, 0), pc) { Layer = lay }));
                var br = new BlockReference(pc, Bloco(s, ped)) { ScaleFactors = new Scale3d(esc), Rotation = c.Angulo + Math.PI / 2 };
                br.SetDatabaseDefaults();
                br.Layer = lay;
                ids.Add(Ctx.Adicionar(s.Tr, ms, br));
                if (!c.Tipo.Contains("projetado"))
                {
                    var tx = Ctx.Texto(c.Grupo, new Point3d(c.X + Math.Cos(c.Angulo) * esc * 0.9, c.Y + Math.Sin(c.Angulo) * esc * 0.9, 0), 0.6 * esc / 1.5, 0, AttachmentPoint.MiddleCenter);
                    tx.Layer = lay;
                    ids.Add(Ctx.Adicionar(s.Tr, ms, tx));
                }
            }
            foreach (var l in plano.Lacos)
            {
                var pl = new Polyline();
                for (int i = 0; i < l.Contorno.Count; i++) pl.AddVertexAt(i, new Point2d(l.Contorno[i].X, l.Contorno[i].Y), 0, 0, 0);
                pl.Closed = true;
                pl.SetDatabaseDefaults();
                pl.Layer = layL;
                ids.Add(Ctx.Adicionar(s.Tr, ms, pl));
            }
            Diagramas(s, ms, it.Nome, m, plano, layD, ids);

            dados.Ciclo = plano.Ciclo;
            dados.Desenhados = ids.Select(i => i.Handle.ToString()).ToList();
            it.Semaforo = dados;
            ArmazenamentoSinal.Gravar(s.Tr, s.Db, it);
            s.Tr.Commit();

            Ctx.Msg($"{it.Nome}: ciclo {plano.Ciclo:0} s (Webster {(double.IsInfinity(plano.CicloOtimo) ? "sem solução, Σy ≥ 1" : $"{plano.CicloOtimo:0.0} s")}; Σy {plano.Y:0.000}; tempo perdido {plano.TempoPerdido:0} s).");
            foreach (var e in plano.Estagios)
                Ctx.Msg($"  estágio {e.Numero} ({e.Via}): verde {e.Verde:0} s + amarelo {e.Amarelo:0} s + vermelho geral {e.VermelhoGeral:0} s — grupos {string.Join(", ", e.Grupos)}");
            foreach (var a in plano.Aproximacoes)
                Ctx.Msg($"  {a.Grupo} braço {a.Rumo} ({a.Nome}): {a.Volume:0} ucp/h, {a.Faixas} faixa(s), x = {a.GrauSaturacao:0.00}, " +
                        (double.IsNaN(a.Atraso) ? "saturada" : $"atraso {a.Atraso:0.0} s/veíc") + $", fila {a.Fila:0} m por faixa");
            foreach (var g in plano.Grupos.Where(x => x.Pedestre))
                Ctx.Msg($"  {g.Nome} {g.Descricao}: verde {g.Verde:0} s + vermelho intermitente {g.VermelhoIntermitente:0} s");
            var ver = plano.Verificacoes;
            Ctx.Msg($"Verificações: {ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovada(s), {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADA(S), " +
                    $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção.");
            foreach (var v in ver.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao))
                Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor} (critério {v.Criterio}; {v.Fonte})");
            foreach (var a in plano.Avisos) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Desenho nas camadas {Layer} (grupos focais: coluna depois do cruzamento com braço projetado; pedestres nas pontas das faixas), {LayerDiagrama} (diagramas de estágios e de tempos, à direita da interseção)" +
                    (plano.Lacos.Count > 0 ? $" e {LayerLacos} (laços)" : "") + ". Rode o PRJSINALIZA de novo: com semáforo ele põe retenção e faixa de pedestres em todas as aproximações, sem PARE e sem R-1.");

            var rel = new RelatorioSemaforo { Titulo = $"Plano semafórico - {it.Nome}", Desenho = s.Doc.Name, Plano = plano };
            rel.Parametros.AddRange(RelatorioSemaforo.ParametrosDe(p));
            rel.Pendencias.AddRange(RelatorioSemaforo.PendenciasPadrao());
            if (s.PedirPalavra("Gravar o plano semafórico em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Semaforo_" + Texto.IdSeguro(it.Nome.Replace(' ', '_'), 50) + ".xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Plano gravado: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJSEMAFORO: {ex.Message}"); }
    }

    private static string Resumo(ParametrosSemaforo p) =>
        $"Semáforo de tempo fixo (MBST Vol. V, Webster): {p.Velocidade:0} km/h, rampa {p.Rampa * 100:0.#} %, fluxo de saturação {p.FluxoSaturacaoFaixa:0} ucp/h por faixa, " +
        $"verde de segurança {p.VerdeSeguranca:0} s, verde de pedestres {p.VerdePedestre:0} s a {p.VelocidadePedestre:0.0} m/s, ciclo até {p.CicloMaximo:0} s, x até {p.GrauSaturacaoMax:0.00}, " +
        $"laços {(p.Lacos ? "sim" : "não")}" + (p.ParametrosDoUsuario ? "." : " [PREMISSA nos valores adotados; troque em Parametros].");

    private static bool PedirParametros(Sessao s, ParametrosSemaforo p)
    {
        double? Faixa(string msg, double atual, double min, double max)
        {
            var x = s.PedirNumero(msg, atual, min < 0);
            if (x is null) return null;
            double c = Math.Clamp(x.Value, min, max);
            if (Math.Abs(c - x.Value) > 1e-9) Ctx.Msg($"  fora da faixa: usei {c:0.##}.");
            return c;
        }
        if (Faixa("Velocidade regulamentada das aproximações (km/h)", p.Velocidade, 10, 120) is not { } vel) return false;
        p.Velocidade = vel;
        if (Faixa("Rampa das aproximações (%, + subindo)", p.Rampa * 100, -15, 15) is not { } ram) return false;
        p.Rampa = ram / 100;
        if (Faixa("Fluxo de saturação por faixa (ucp/h; o manual cita 1600 a 2000)", p.FluxoSaturacaoFaixa, 1000, 2500) is not { } fs) return false;
        p.FluxoSaturacaoFaixa = fs;
        if (Faixa("Largura de faixa para contar as faixas (m)", p.LarguraFaixa, 2.5, 5) is not { } lf) return false;
        p.LarguraFaixa = lf;
        if (Faixa("Verde de segurança veicular (s; 10 a 20)", p.VerdeSeguranca, 10, 30) is not { } vs) return false;
        p.VerdeSeguranca = vs;
        if (Faixa("Verde de pedestres (s; mínimo 4, recomendado 7)", p.VerdePedestre, 4, 60) is not { } vp) return false;
        p.VerdePedestre = vp;
        if (Faixa("Velocidade de travessia dos pedestres (m/s)", p.VelocidadePedestre, 0.5, 1.5) is not { } vt) return false;
        p.VelocidadePedestre = vt;
        if (Faixa("Ciclo máximo (s)", p.CicloMaximo, 40, 240) is not { } cm) return false;
        p.CicloMaximo = cm;
        if (Faixa("Grau de saturação máximo (0,75 a 0,90 recomendado)", p.GrauSaturacaoMax, 0.5, 1) is not { } gx) return false;
        p.GrauSaturacaoMax = gx;
        var l = s.PedirPalavra("Laços detectores no desenho", new[] { "Sim", "Nao" }, p.Lacos ? "Sim" : "Nao");
        if (l is null) return false;
        p.Lacos = l == "Sim";
        p.ParametrosDoUsuario = true;
        return true;
    }

    /// <summary>Diagrama de estágios (um croqui por estágio) e diagrama de tempos (barras por grupo), à direita da interseção.</summary>
    private static void Diagramas(Sessao s, BlockTableRecord ms, string nome, ModeloIntersecaoSinal m, PlanoSemaforico pl, string lay, List<ObjectId> ids)
    {
        const double sx = 0.4, linha = 2.0, barra = 1.2;
        // caixa dos dois diagramas, no maior vão entre os braços, com o canto mais perto do centro a 35 m dele
        double larg = 8 + Math.Max(pl.Ciclo * sx, pl.Estagios.Count * 16), alt = 26 + (pl.Grupos.Count + 2) * linha;
        var angs = m.Bracos.Select(b => { var (dx, dy) = b.Polilinha().Direcao(0); return GeometriaRotatoria.Normalizar(Math.Atan2(dy, dx)); }).OrderBy(a => a).ToList();
        double vao = -1, meio = Math.PI / 4;
        for (int i = 0; i < angs.Count; i++)
        {
            double a0 = angs[i], a1 = i + 1 < angs.Count ? angs[i + 1] : angs[0] + 2 * Math.PI;
            if (a1 - a0 > vao) { vao = a1 - a0; meio = (a0 + a1) / 2; }
        }
        double qx = m.Ox + 35 * Math.Cos(meio), qy = m.Oy + 35 * Math.Sin(meio);
        double xEsq = Math.Cos(meio) >= 0 ? qx : qx - larg, yBaixo = Math.Sin(meio) >= 0 ? qy : qy - alt;
        double x0 = xEsq + 8, y0 = yBaixo + (pl.Grupos.Count + 2) * linha;
        void Txt(string t, double x, double y, double h, AttachmentPoint ap, short cor = 7)
        {
            var mt = Ctx.Texto(t, new Point3d(x, y, 0), h, 0, ap);
            mt.Layer = lay;
            mt.ColorIndex = cor;
            ids.Add(Ctx.Adicionar(s.Tr, ms, mt));
        }
        void Ln(double ax, double ay, double bx, double by, short cor)
        {
            ids.Add(Ctx.Adicionar(s.Tr, ms, new Line(new Point3d(ax, ay, 0), new Point3d(bx, by, 0)) { Layer = lay, ColorIndex = cor }));
        }
        void Barra(double t0, double t1, double y, short cor, bool listrada)
        {
            if (t1 - t0 < 1e-6) return;
            var pts = new[] { (x0 + t0 * sx, y), (x0 + t1 * sx, y), (x0 + t1 * sx, y + barra), (x0 + t0 * sx, y + barra) };
            var pol = new Polyline();
            for (int i = 0; i < 4; i++) pol.AddVertexAt(i, new Point2d(pts[i].Item1, pts[i].Item2), 0, 0, 0);
            pol.Closed = true;
            pol.SetDatabaseDefaults();
            pol.Layer = lay;
            pol.ColorIndex = cor;
            ids.Add(Ctx.Adicionar(s.Tr, ms, pol));
            if (listrada)
            {
                // vermelho intermitente: contorno vermelho com listras verticais
                for (double xl = x0 + t0 * sx + 0.25; xl < x0 + t1 * sx - 0.05; xl += 0.3) Ln(xl, y, xl, y + barra, cor);
                return;
            }
            var h = new Hatch();
            h.SetDatabaseDefaults();
            h.Layer = lay;
            h.ColorIndex = cor;
            ids.Add(Ctx.Adicionar(s.Tr, ms, h));
            if (listrada) { h.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31"); h.PatternScale = 0.03; }   // (não usado: listras desenhadas acima)
            else h.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            h.Associative = false;
            var vs = new Point2dCollection();
            var bs = new DoubleCollection();
            foreach (var q in pts) { vs.Add(new Point2d(q.Item1, q.Item2)); bs.Add(0); }
            vs.Add(new Point2d(pts[0].Item1, pts[0].Item2));
            bs.Add(0);
            h.AppendLoop(HatchLoopTypes.Polyline | HatchLoopTypes.External, vs, bs);
            h.EvaluateHatch(true);
        }

        double c = pl.Ciclo;
        // diagrama de estágios
        var eixos = m.Bracos.Select(b => b.Polilinha()).ToList();
        double yE = y0 + 15;
        Txt($"{nome} – diagrama de estágios", x0, yE + 9, 1.0, AttachmentPoint.BottomLeft);
        for (int k = 0; k < pl.Estagios.Count; k++)
        {
            var e = pl.Estagios[k];
            double cx = x0 + 7 + k * 16, cy = yE;
            for (int i = 0; i < m.Bracos.Count; i++)
            {
                var (ux, uy) = eixos[i].Direcao(0);
                double nx = -uy, ny = ux;
                Ln(cx, cy, cx + ux * 6, cy + uy * 6, 8);
                if (pl.Aproximacoes[i].Estagio == e.Numero)
                {
                    // seta de quem chega pelo braço (na mão direita dele)
                    double ox = cx + nx * 0.8, oy = cy + ny * 0.8;
                    Ln(ox + ux * 5.5, oy + uy * 5.5, ox + ux * 1.2, oy + uy * 1.2, 3);
                    Ln(ox + ux * 1.2, oy + uy * 1.2, ox + ux * 2.0 + nx * 0.5, oy + uy * 2.0 + ny * 0.5, 3);
                    Ln(ox + ux * 1.2, oy + uy * 1.2, ox + ux * 2.0 - nx * 0.5, oy + uy * 2.0 - ny * 0.5, 3);
                }
                if (pl.Grupos.Any(g => g.Pedestre && g.Braco == i && g.Estagio == e.Numero))
                    for (int t = -2; t < 2; t++)
                        Ln(cx + ux * 3.6 + nx * (t * 0.6 + 0.1), cy + uy * 3.6 + ny * (t * 0.6 + 0.1), cx + ux * 3.6 + nx * (t * 0.6 + 0.4), cy + uy * 3.6 + ny * (t * 0.6 + 0.4), 3);
            }
            Txt($"E{e.Numero} – {e.Via}\\P{e.Verde:0} s + {e.Amarelo:0} + {e.VermelhoGeral:0} s", cx, cy - 7, 0.7, AttachmentPoint.TopCenter);
        }

        // diagrama de tempos
        Txt($"{nome} – diagrama de tempos (ciclo {c:0} s)", x0, y0 + 1.5, 1.0, AttachmentPoint.BottomLeft);
        for (int j = 0; j < pl.Grupos.Count; j++)
        {
            var g = pl.Grupos[j];
            double y = y0 - (j + 1) * linha;
            Txt(g.Nome + (g.Pedestre ? " (ped.)" : $" {m.Bracos[g.Braco].Rumo}"), x0 - 0.5, y + barra / 2, 0.6, AttachmentPoint.MiddleRight);
            double a = g.InicioVerde, b = a + g.Verde, fim = b + (g.Pedestre ? g.VermelhoIntermitente : g.Amarelo);
            Barra(0, a, y, 1, false);
            Barra(a, b, y, 3, false);
            Barra(b, Math.Min(fim, c), y, g.Pedestre ? (short)1 : (short)2, g.Pedestre);
            Barra(Math.Min(fim, c), c, y, 1, false);
        }
        double yb = y0 - (pl.Grupos.Count + 1) * linha + barra - 0.3;
        Ln(x0, yb, x0 + c * sx, yb, 7);
        for (double t = 0; t <= c + 1e-9; t += 10)
        {
            Ln(x0 + t * sx, yb, x0 + t * sx, yb - 0.4, 7);
            Txt($"{t:0}", x0 + t * sx, yb - 0.6, 0.5, AttachmentPoint.TopCenter);
        }
        foreach (var e in pl.Estagios)
        {
            Ln(x0 + e.Inicio * sx, y0 + 0.8, x0 + e.Inicio * sx, yb, 8);
            Txt($"E{e.Numero}", x0 + (e.Inicio + e.Duracao / 2) * sx, y0 + 0.9, 0.6, AttachmentPoint.BottomCenter);
        }
        Txt("verde · amarelo · vermelho · vermelho intermitente (pedestres, listrado) — tempos em s", x0, yb - 1.6, 0.5, AttachmentPoint.TopLeft, 8);
    }

    /// <summary>Bloco do grupo focal (veicular: 3 focos; pedestres: 2), 1 m de largura, com a frente para +Y.</summary>
    private static ObjectId Bloco(Sessao s, bool pedestre)
    {
        string nome = pedestre ? "PRJ_SEMAFORO_PED" : "PRJ_SEMAFORO_VEIC";
        var bt = (BlockTable)s.Tr.GetObject(s.Db.BlockTableId, OpenMode.ForRead);
        if (bt.Has(nome)) return bt[nome];
        bt.UpgradeOpen();
        var btr = new BlockTableRecord { Name = nome, Origin = Point3d.Origin };
        var id = bt.Add(btr);
        s.Tr.AddNewlyCreatedDBObject(btr, true);
        void Add(Entity e) { btr.AppendEntity(e); s.Tr.AddNewlyCreatedDBObject(e, true); }
        double meia = pedestre ? 0.32 : 0.5;
        var caixa = new Polyline();
        caixa.AddVertexAt(0, new Point2d(-meia, -0.18), 0, 0, 0);
        caixa.AddVertexAt(1, new Point2d(meia, -0.18), 0, 0, 0);
        caixa.AddVertexAt(2, new Point2d(meia, 0.18), 0, 0, 0);
        caixa.AddVertexAt(3, new Point2d(-meia, 0.18), 0, 0, 0);
        caixa.Closed = true;
        caixa.ColorIndex = 7;
        Add(caixa);
        // frente (lado dos focos) para +Y
        Add(new Line(new Point3d(-meia, 0.24, 0), new Point3d(meia, 0.24, 0)) { ColorIndex = 7 });
        var focos = pedestre ? new (double X, short Cor)[] { (-0.15, 1), (0.15, 3) } : new (double X, short Cor)[] { (-0.32, 1), (0, 2), (0.32, 3) };
        foreach (var (x, cor) in focos) Add(new Circle(new Point3d(x, 0, 0), Vector3d.ZAxis, 0.12) { ColorIndex = cor });
        return id;
    }
}
