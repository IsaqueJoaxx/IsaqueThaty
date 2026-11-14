using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJROTATORIA — monta a rotatória de uma vez: clica no centro, seleciona os eixos dos ramos e sai o anel (eixo fechado
/// anti-horário, mão direita) com o greide passando nas cotas dos braços; as conexões de entrada e de saída de cada braço
/// (meios-fios tangentes ao bordo externo do anel e aos bordos do ramo, com greide casando com os dois); as ilhas
/// divisórias, os bordos dos ramos com o alargamento, as linhas de retenção, o bordo externo, a ilha central e a faixa
/// transponível; a trajetória mais rápida de cada braço (R1 a R5, NCHRP 672) e a tabela de verificações (aprovado /
/// reprovado / atenção, cada uma com a fonte), com relatório em Excel. Não mexe nos eixos dos ramos.
/// </summary>
public sealed partial class CmdRotatoria
{
    internal const string LayerRotatoria = "PRJ-ROTATORIA";
    internal const string LayerIlha = "PRJ-ROTATORIA-ILHA";
    internal const string LayerBordo = "PRJ-ROTATORIA-BORDO";
    internal const string LayerRetencao = "PRJ-ROTATORIA-RETENCAO";
    internal const string LayerTrajetoria = "PRJ-ROTATORIA-TRAJETORIA";
    internal const string LayerVisibilidade = "PRJ-ROTATORIA-VISIBILIDADE";
    internal const string LayerMontagens = "PRJ-ROTATORIA-MONTAGENS";

    /// <summary>Braço da rotatória: onde um eixo de ramo cruza o eixo do anel.</summary>
    private sealed record Braco(Alignment Eixo, double S, Point3d J, double Angulo, int SentidoFora, string Rumo);

    [CommandMethod("PRJROTATORIA", CommandFlags.Modal)]
    public static void Rotatoria()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            Point3d o;
            while (true)
            {
                Ctx.Msg(ResumoParametros(cfg));
                var ppo = new PromptPointOptions("\nClique no centro da rotatória");
                ppo.Keywords.Add("Parametros");
                ppo.AppendKeywordsToMessage = true;
                var pr = ed.GetPoint(ppo);
                if (pr.Status == PromptStatus.Keyword) { if (!PedirParametros(ed, cfg)) return; continue; }
                if (pr.Status != PromptStatus.OK) return;
                var w = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                o = new Point3d(w.X, w.Y, 0);
                break;
            }
            double rExt = cfg.Dci / 2, rEixo = rExt - cfg.LarguraAnel / 2, rIlha = rExt - cfg.LarguraAnel;
            if (rIlha < 1) { Ctx.Msg($"Com DCI {cfg.Dci:0.##} m e pista anelar de {cfg.LarguraAnel:0.##} m não sobra ilha central. Ajuste em Parametros."); return; }
            bool comIlha = cfg.IlhaLargura > 0 && cfg.IlhaComprimento > 0;
            double meiaIlha = comIlha ? cfg.IlhaLargura / 2 : 0;
            double bordoE = meiaIlha + cfg.FaixaEntrada, bordoS = meiaIlha + cfg.FaixaSaida;

            var f = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT") });
            var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os eixos dos ramos que chegam na rotatória: " }, f);
            if (ps.Status != PromptStatus.OK) return;

            using var s = Sessao.Abrir();
            var civ = CivilApplication.ActiveDocument;
            var avisos = new List<string>();
            var bracos = new List<Braco>();
            var foraDoAnel = new List<Alignment>();
            foreach (var id in ps.Value.GetObjectIds())
            {
                var al = (Alignment)s.Tr.GetObject(id, OpenMode.ForRead);
                if (RotatoriaCivil.Fechado(al)) { avisos.Add($"'{al.Name}' é um eixo fechado (outro anel?) e ficou de fora."); continue; }
                if (al.Name.StartsWith("Rotatória ", StringComparison.OrdinalIgnoreCase) && al.Name.Contains(" - ")) continue; // conexões de uma rotatória anterior
                var bs = Bracos(al, o, rEixo, out double dMin);
                if (bs.Count == 0)
                {
                    foraDoAnel.Add(al);
                    avisos.Add($"'{al.Name}' não chega no eixo do anel: passa a {dMin:0.00} m do centro e o eixo do anel fica a {rEixo:0.00} m. Prolongue o eixo até o anel (ou até o centro) e rode de novo.");
                }
                bracos.AddRange(bs);
            }
            bracos = Ordenar(bracos);

            // número de braços: confere com o que ele espera e oferece prolongar os eixos que param antes do anel
            int nAchados = bracos.Count;
            string rumos = bracos.Count == 0 ? "nenhum" : string.Join(", ", bracos.Select(b => $"{b.Rumo} ({b.Eixo.Name})"));
            var pio = new PromptIntegerOptions($"\nBraços encontrados: {nAchados} — {rumos}. Quantos braços tem a rotatória")
            { AllowNone = true, AllowZero = false, AllowNegative = false, DefaultValue = Math.Max(1, nAchados), UseDefaultValue = true };
            var pin = ed.GetInteger(pio);
            if (pin.Status != PromptStatus.OK && pin.Status != PromptStatus.None) return;
            int esperado = pin.Status == PromptStatus.OK ? pin.Value : Math.Max(1, nAchados);
            if (esperado > bracos.Count)
                foreach (var al in foraDoAnel)
                {
                    if (Prolongamento(al, o, rEixo) is not { } pr) continue;
                    var resp = s.PedirPalavra($"'{al.Name}' para a {pr.Falta:0.0} m do anel, apontando para a rotatória. Prolongar o {(pr.NoFim ? "fim" : "início")} dele {pr.Comprimento:0.0} m em reta, até o centro",
                                              new[] { "Sim", "Nao" }, "Sim");
                    if (resp is null) return;
                    if (resp != "Sim") continue;
                    al.UpgradeOpen();
                    if (!Prolongar(s, al, pr, avisos)) continue;
                    avisos.RemoveAll(a => a.StartsWith($"'{al.Name}' não chega", StringComparison.Ordinal));
                    bracos.AddRange(Bracos(al, o, rEixo, out _));
                    bracos = Ordenar(bracos);
                    if (bracos.Count >= esperado) break;
                }
            if (esperado != bracos.Count)
            {
                foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
                string porque = esperado > bracos.Count
                    ? (foraDoAnel.Count == 0 ? "Os outros eixos não foram selecionados ou não chegam perto da rotatória." : "Veja os avisos acima: os eixos que não chegam no anel ficaram de fora.")
                    : "Algum eixo cruza o anel mais de uma vez ou há eixo a mais na seleção.";
                Ctx.Msg($"Achei {bracos.Count} braço(s) e você informou {esperado}. {porque}");
                if (bracos.Count == 0 || s.PedirPalavra($"Continuar com {bracos.Count} braço(s)", new[] { "Sim", "Nao" }, "Nao") != "Sim")
                {
                    Ctx.Msg("Nada foi criado. Selecione todos os eixos dos ramos (um eixo que atravessa a rotatória conta como dois braços) e rode de novo.");
                    return;
                }
            }
            if (bracos.Count == 0) { foreach (var a in avisos) Ctx.Msg("AVISO: " + a); Ctx.Msg("Nenhum braço: nada foi criado."); return; }

            // cotas do anel nos braços
            var cotas = CotasDosBracos(s, cfg, bracos, avisos, out string origemCotas);
            if (cotas is null) return;

            // eixo do anel, começando no meio do maior vão entre os braços
            string prefixo = Prefixo(s.Tr, civ);
            double emenda = GeometriaRotatoria.AnguloEmenda(bracos.Select(b => b.Angulo).ToList());
            var pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(o.X + rEixo * Math.Cos(emenda), o.Y + rEixo * Math.Sin(emenda)), 1, 0, 0);
            pl.AddVertexAt(1, new Point2d(o.X - rEixo * Math.Cos(emenda), o.Y - rEixo * Math.Sin(emenda)), 1, 0, 0);
            pl.Closed = true;
            pl.SetDatabaseDefaults();
            var plId = Ctx.Adicionar(s.Tr, Ctx.EspacoModelo(s.Tr, s.Db), pl);
            var ref0 = bracos[0].Eixo;
            ObjectId rotulos = ObjectId.Null;
            try { if (civ.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count > 0) rotulos = civ.Styles.LabelSetStyles.AlignmentLabelSetStyles[0]; } catch { }
            string nomeAnel = RotatoriaCivil.NomeUnico(s.Tr, civ, $"{prefixo} - Anel");
            var anelId = Alignment.Create(civ, new PolylineOptions { PlineId = plId, AddCurvesBetweenTangents = false, EraseExistingEntities = true },
                                          nomeAnel, ObjectId.Null, ref0.LayerId, ref0.StyleId, rotulos);
            var anel = (Alignment)s.Tr.GetObject(anelId, OpenMode.ForRead);
            double st0 = anel.StartingStation, comp = anel.EndingStation - anel.StartingStation;

            // greide do anel passando nas cotas dos braços
            var alvos = new List<PerfilAnel.Alvo>();
            for (int i = 0; i < bracos.Count; i++)
            {
                double sa = RotatoriaCivil.EstacaAfast(anel, bracos[i].J) is { } so ? so.S : st0 + rEixo * GeometriaRotatoria.Normalizar(bracos[i].Angulo - emenda);
                alvos.Add(new PerfilAnel.Alvo(sa - st0, cotas[i]));
            }
            var pvis = PerfilAnel.Calcular(comp, alvos);
            var gRef = bracos.Select(b => RotatoriaCivil.Greides(s.Tr, b.Eixo).FirstOrDefault()).FirstOrDefault(g => g is not null);
            ObjectId estiloPerfil = gRef?.StyleId ?? ObjectId.Null, rotPerfil = ObjectId.Null;
            try { if (estiloPerfil.IsNull && civ.Styles.ProfileStyles.Count > 0) estiloPerfil = civ.Styles.ProfileStyles[0]; } catch { }
            try { if (civ.Styles.LabelSetStyles.ProfileLabelSetStyles.Count > 0) rotPerfil = civ.Styles.LabelSetStyles.ProfileLabelSetStyles[0]; } catch { }
            var pid = Profile.CreateByLayout(nomeAnel, anelId, gRef?.LayerId ?? anel.LayerId, estiloPerfil, rotPerfil);
            var pf = (Profile)s.Tr.GetObject(pid, OpenMode.ForWrite);
            for (int i = 0; i + 1 < pvis.Count; i++)
                pf.Entities.AddFixedTangent(new Point2d(st0 + pvis[i].Estaca, pvis[i].Cota), new Point2d(st0 + pvis[i + 1].Estaca, pvis[i + 1].Cota));
            for (int i = 1; i + 1 < pvis.Count; i++)
                if (pvis[i].Curva > 0.05 && AcharPVI(pf, st0 + pvis[i].Estaca) is { } pvi)
                    pf.Entities.AddFreeSymmetricParabolaByPVIAndCurveLength(pvi, pvis[i].Curva);
            GreideEscolhido[nomeAnel] = pf.Name;
            double erroAnel = alvos.Max(a => Math.Abs(pf.ElevationAt(st0 + a.Estaca) - a.Cota));
            var rampaMax = PerfilAnel.RampaMaxima(pvis);

            // anel desenhado: bordo externo, ilha central (meio-fio) e faixa transponível
            var lay = Ctx.GarantirLayer(s.Tr, s.Db, LayerRotatoria, 30);
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            // bordo externo e ilha central como polilinhas (servem de alvo de largura no corredor)
            var bordoExternoId = Ctx.Adicionar(s.Tr, ms, CirculoPolilinha(o, rExt, lay));
            Ctx.Adicionar(s.Tr, ms, CirculoPolilinha(o, rIlha, lay));
            if (cfg.Apron > 0 && cfg.Apron < rIlha - 0.5) Ctx.Adicionar(s.Tr, ms, new Circle(o, Vector3d.ZAxis, rIlha - cfg.Apron) { Layer = lay, ColorIndex = 8 });
            var rotulo = Ctx.Texto(Ctx.MTextLinhas(prefixo, $"DCI {cfg.Dci:0.##} m"), o, Math.Clamp(rIlha / 6, 0.5, 3), 0, AttachmentPoint.MiddleCenter);
            rotulo.Layer = lay;
            Ctx.Adicionar(s.Tr, ms, rotulo);

            // conexões de entrada (lado anti-horário do braço) e de saída (lado horário), encostadas nos bordos com a ilha
            var feitas = new Conectada?[bracos.Count, 2];
            var linhas = new List<string>();
            for (int i = 0; i < bracos.Count; i++)
            {
                var b = bracos[i];
                var (dx, dy) = RotatoriaCivil.Direcao(b.Eixo, b.S);
                double ux = dx * b.SentidoFora, uy = dy * b.SentidoFora;
                var partes = new List<string>();
                foreach (int lado in new[] { +1, -1 })
                {
                    double r = lado > 0 ? cfg.RaioEntrada : cfg.RaioSaida;
                    double bordo = lado > 0 ? bordoE : bordoS;
                    string tipo = lado > 0 ? "entrada" : "saída";
                    var (cx, cy) = GeometriaRotatoria.ChuteCentroConexao(b.J.X, b.J.Y, ux, uy, lado, bordo + r, o.X, o.Y, rExt + r);
                    var pc = new Point3d(cx, cy, 0);
                    var pB = RotatoriaCivil.Ponto(b.Eixo, RotatoriaCivil.EstacaPerto(b.Eixo, pc), 0);
                    var pA = RotatoriaCivil.Ponto(anel, RotatoriaCivil.EstacaPerto(anel, pc), 0);
                    var feito = Conectar(s, cfg, anelId, pA, b.Eixo.ObjectId, pB, r, $"{prefixo} - {b.Eixo.Name} {b.Rumo} - {tipo}", cfg.LarguraAnel / 2, bordo);
                    feitas[i, lado > 0 ? 0 : 1] = feito;
                    if (feito is null) { partes.Add($"{tipo} R {r:0.##}: NÃO coube"); avisos.Add($"Braço {b.Rumo} ('{b.Eixo.Name}'): a conexão de {tipo} de R {r:0.##} m não coube (o eixo do ramo não vai longe o bastante para fora, ou o raio é grande demais)."); continue; }
                    partes.Add($"{tipo} R {r:0.##} ({feito.C.Desenvolvimento:0.00} m{(feito.ComGreide ? "" : ", sem greide")})");
                }
                linhas.Add($"  braço {b.Rumo,-2} '{b.Eixo.Name}' est. {RotatoriaCivil.Est(b.S)}: cota {cotas[i]:0.000} — {string.Join("; ", partes)}");
            }

            // modelo plano da rotatória (ilhas, bordos, retenção e trajetórias)
            var mod = new ModeloRotatoria { Ox = o.X, Oy = o.Y, RExt = rExt, RIlha = rIlha, RecuoIlha = cfg.IlhaRecuo };
            for (int i = 0; i < bracos.Count; i++)
            {
                var b = bracos[i];
                var eixo = PolilinhaDoBraco(b, 200, out bool prolongado);
                if (prolongado) avisos.Add($"Braço {b.Rumo} ('{b.Eixo.Name}'): o eixo do ramo acaba antes de 200 m da rotatória; para a ilha, os bordos e a trajetória ele foi prolongado em reta.");
                var br = new BracoRotatoria
                {
                    Nome = b.Eixo.Name, Rumo = b.Rumo, Angulo = b.Angulo, Eixo = eixo,
                    BordoEntrada = bordoE, BordoSaida = bordoS, BordoNormal = cfg.MeiaLarguraRamo,
                };
                br.SRetencao = EstacaNoRaio(eixo, o, rExt);
                if (feitas[i, 0] is { } fe) { br.MeioFioEntrada = (fe.C.Centro.X, fe.C.Centro.Y, fe.C.Raio); br.STangEntrada = eixo.Projetar(fe.C.TangenteB.X, fe.C.TangenteB.Y).S; }
                if (feitas[i, 1] is { } fs) { br.MeioFioSaida = (fs.C.Centro.X, fs.C.Centro.Y, fs.C.Raio); br.STangSaida = eixo.Projetar(fs.C.TangenteB.X, fs.C.TangenteB.Y).S; }
                if (comIlha)
                {
                    br.SIlhaIni = EstacaNoRaio(eixo, o, rExt + cfg.IlhaRecuo);
                    br.SIlhaFim = br.SIlhaIni + cfg.IlhaComprimento;
                    br.IlhaLarguraIni = cfg.IlhaLargura;
                    br.IlhaLarguraFim = Math.Min(cfg.IlhaNariz, cfg.IlhaLargura);
                }
                double fimLarga = comIlha ? br.SIlhaFim + 5 : Math.Max(double.IsNaN(br.STangEntrada) ? 0 : br.STangEntrada, double.IsNaN(br.STangSaida) ? 0 : br.STangSaida) + 10;
                br.SAlargIni = fimLarga;
                br.SAlargFim = fimLarga + Math.Max(0, Math.Max(bordoE, bordoS) - cfg.MeiaLarguraRamo) * 10;
                mod.Bracos.Add(br);
            }

            var layIlha = Ctx.GarantirLayer(s.Tr, s.Db, LayerIlha, 40);
            var layBordo = Ctx.GarantirLayer(s.Tr, s.Db, LayerBordo, 30);
            var layRet = Ctx.GarantirLayer(s.Tr, s.Db, LayerRetencao, 7);
            var bordosDoBraco = new List<ObjectId>[mod.Bracos.Count];
            for (int i = 0; i < mod.Bracos.Count; i++)
            {
                var br = mod.Bracos[i];
                bordosDoBraco[i] = new List<ObjectId>();
                if (br.TemIlha) Desenhar(s, ms, mod.Ilha(br), true, layIlha, 256);
                double ate = Math.Min(br.Eixo.Comprimento, br.SAlargFim + 15);
                if (br.MeioFioEntrada is not null) bordosDoBraco[i].Add(Desenhar(s, ms, mod.BordoRamo(br, true, ate), false, layBordo, 256));
                if (br.MeioFioSaida is not null) bordosDoBraco[i].Add(Desenhar(s, ms, mod.BordoRamo(br, false, ate), false, layBordo, 256));
                bordosDoBraco[i].RemoveAll(id => id.IsNull);
                Desenhar(s, ms, mod.Retencao(br), false, layRet, 256);
            }

            // trajetórias mais rápidas
            var analises = new List<AnaliseBraco>();
            if (cfg.Trajetorias && mod.Bracos.Count >= 2)
            {
                Ctx.Msg("Calculando as trajetórias mais rápidas (R1 a R5)...");
                var layTr = Ctx.GarantirLayer(s.Tr, s.Db, LayerTrajetoria, 1);
                double ht = Math.Clamp(rIlha / 18, 0.4, 1.5);
                for (int i = 0; i < mod.Bracos.Count; i++)
                {
                    var an = TrajetoriaRapida.Analisar(mod, i);
                    analises.Add(an);
                    foreach (var (t, cor) in new[] { (an.Direto, (short)1), (an.Esquerda, (short)5), (an.Direita, (short)3) })
                        if (t is not null) Desenhar(s, ms, t.Pontos, false, layTr, cor);
                    foreach (var rc in new[] { an.R1, an.R2, an.R3, an.R4, an.R5 })
                    {
                        if (rc is null) continue;
                        var tx = Ctx.Texto($"{rc.Nome} {rc.Raio:0.0} m · {rc.Velocidade:0} km/h", new Point3d(rc.Ponto.X, rc.Ponto.Y, 0), ht);
                        tx.Layer = layTr;
                        tx.ColorIndex = (short)(rc.Nome is "R4" ? 5 : rc.Nome is "R5" ? 3 : 1);
                        Ctx.Adicionar(s.Tr, ms, tx);
                    }
                }
            }

            // visibilidade à esquerda em cada entrada (pelas velocidades do braço de montante)
            var visadas = new List<Visada>();
            if (analises.Count > 0)
            {
                var layVis = Ctx.GarantirLayer(s.Tr, s.Db, LayerVisibilidade, 6);
                double hv = Math.Clamp(rIlha / 20, 0.35, 1.2);
                for (int i = 0; i < mod.Bracos.Count; i++)
                {
                    int up = (i + mod.Bracos.Count - 1) % mod.Bracos.Count;
                    if (VisibilidadeRotatoria.Velocidades(analises, up) is not { } vv) continue;
                    if (VisibilidadeRotatoria.Calcular(mod, i, vv.VEntrante, vv.VCirculante) is not { } vi) continue;
                    visadas.Add(vi);
                    Desenhar(s, ms, vi.PernaCirculante, false, layVis, 6);
                    Desenhar(s, ms, vi.PernaEntrante, false, layVis, 4);
                    Desenhar(s, ms, new[] { vi.Observador, vi.Circulante }, false, layVis, 6);
                    Desenhar(s, ms, new[] { vi.Observador, vi.Entrante }, false, layVis, 4);
                    var tv = Ctx.Texto($"visibilidade: d1 {vi.D1:0.0} m · d2 {vi.D2:0.0} m", new Point3d(vi.Observador.X, vi.Observador.Y, 0), hv);
                    tv.Layer = layVis;
                    Ctx.Adicionar(s.Tr, ms, tv);
                }
            }

            // verificações
            var crit = new CriteriosRotatoria { VelMaxEntrada = cfg.VelMaxEntrada, DifVelMax = cfg.DifVelMax };
            var verif = VerificacaoRotatoria.Avaliar(mod, analises, crit, visadas);

            // montagens do corredor (subassemblies padrão do Civil) e trechos de cada eixo
            var montagens = new List<MontagensRotatoria.Criada>();
            var corredor = new List<RegiaoCorredor>();
            var msgsCorredor = new List<string>();
            string? nomeCorredor = null;
            int regioesCorredor = 0;
            if (cfg.Montagens)
            {
                var defs = MontagensRotatoria.Definir(cfg);
                const double passo = 40;
                var org = new Point3d(o.X - (defs.Count - 1) * passo / 2, o.Y - rExt - 90, 0);
                montagens = MontagensRotatoria.Criar(civ, s.Tr, prefixo, defs, org, passo, avisos);
                var layM = Ctx.GarantirLayer(s.Tr, s.Db, LayerMontagens, 7);
                foreach (var m in montagens)
                {
                    var tm = Ctx.Texto(m.Tipo, new Point3d(m.Local.X, m.Local.Y - 3, 0), 1.0, 0, AttachmentPoint.TopCenter);
                    tm.Layer = layM;
                    Ctx.Adicionar(s.Tr, ms, tm);
                }
                var titulo = Ctx.Texto($"{prefixo} - montagens do corredor", new Point3d(o.X, org.Y + 18, 0), 2.0, 0, AttachmentPoint.BottomCenter);
                titulo.Layer = layM;
                Ctx.Adicionar(s.Tr, ms, titulo);
                var trechos = GuiaDoCorredor(s, anel, o, rExt, st0, comp, bracos, alvos.Select(a => st0 + a.Estaca).ToList(), feitas, mod, comIlha, bordoExternoId, bordosDoBraco, avisos);
                corredor = trechos.Select(t => new RegiaoCorredor(t.Eixo, t.De, t.Ate, $"{prefixo} - {t.Montagem}", t.Obs)).ToList();
                if (CorredorProjeta.Escolher(s, civ, prefixo) is { } corrId)
                {
                    var sup = CorredorProjeta.Superficie(s);
                    if (sup.IsNull) avisos.Add("Corredor: não há superfície de terreno no desenho; os taludes ficaram sem alvo (defina depois nas regiões).");
                    Ctx.Msg("Montando o corredor...");
                    msgsCorredor = CorredorProjeta.Inserir(s.Tr, corrId, trechos, al => EscolherGreide(s, al)?.ObjectId, montagens.ToDictionary(m => m.Tipo, m => m.Id),
                                                           sup, prefixo, recortar: true, out regioesCorredor);
                    nomeCorredor = ((Corridor)s.Tr.GetObject(corrId, OpenMode.ForRead)).Name;
                }
            }

            s.Tr.Commit();

            string premissa = cfg.ParametrosDoUsuario ? "" : " [PREMISSA: valores de partida, troque em Parametros]";
            Ctx.Msg($"{prefixo} criada no centro ({o.X:0.000}; {o.Y:0.000}): DCI {cfg.Dci:0.##} m, pista anelar {cfg.LarguraAnel:0.##} m → eixo do anel R {rEixo:0.00} m, ilha central R {rIlha:0.00} m; " +
                    $"entrada {cfg.FaixaEntrada:0.##} m e saída {cfg.FaixaSaida:0.##} m{(comIlha ? $", ilha divisória {cfg.IlhaLargura:0.##} × {cfg.IlhaComprimento:0.##} m" : ", sem ilha divisória")}; caimento {Pct(cfg.Caimento, 1)}{premissa}.");
            foreach (var l in linhas) Ctx.Msg(l);
            Ctx.Msg($"  greide do anel ({origemCotas}): passa nas cotas dos braços (erro máx. {erroAnel * 1000:0.0} mm); rampa máxima {Pct(Math.Abs(rampaMax.Rampa))} " +
                    $"entre as est. {RotatoriaCivil.Est(st0 + rampaMax.De)} e {RotatoriaCivil.Est(st0 + rampaMax.Ate)}.");
            foreach (var an in analises)
            {
                string R(RaioCritico? r) => r is null ? "-" : $"{r.Raio:0.0} m/{r.Velocidade:0} km/h";
                Ctx.Msg($"  trajetória mais rápida, braço {mod.Bracos[an.Braco].Rumo}: R1 {R(an.R1)}, R2 {R(an.R2)}, R3 {R(an.R3)}, R4 {R(an.R4)}, R5 {R(an.R5)}");
            }
            int ap = verif.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado);
            var ruins = verif.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao).ToList();
            Ctx.Msg($"Verificações: {ap} aprovada(s), {verif.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADA(S), {verif.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção.");
            foreach (var v in ruins) Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor} (critério {v.Criterio}; {v.Fonte})");
            if (montagens.Count > 0)
            {
                Ctx.Msg($"Montagens do corredor: {montagens.Count} criadas abaixo da rotatória ({string.Join("; ", montagens.Select(m => m.Tipo))}) — subassemblies padrão do Civil, " +
                        $"revestimento {cfg.Revestimento:0.00} + base {cfg.Base:0.00} + sub-base {cfg.SubBase:0.00} m, meio-fio {cfg.MeioFioAltura:0.00} m, sarjeta {cfg.SarjetaLargura:0.00} m, passeio {cfg.Passeio:0.00} m, " +
                        $"talude {cfg.TaludeCorte:0.##}:1 no corte e {cfg.TaludeAterro:0.##}:1 no aterro{(cfg.ParametrosDoUsuario ? "" : " [PREMISSA]")}. Trechos de cada eixo:");
                foreach (var g in corredor.GroupBy(c => c.Baseline))
                    Ctx.Msg($"  {g.Key}: " + string.Join("; ", g.Select(c => $"{RotatoriaCivil.Est(c.De)} a {RotatoriaCivil.Est(c.Ate)} → {c.Montagem.Replace(prefixo + " - ", "")}")));
            }
            if (nomeCorredor is not null)
            {
                Ctx.Msg($"Corredor '{nomeCorredor}': {regioesCorredor} região(ões) da rotatória, com as montagens, os alvos de largura e o talude na superfície do terreno.");
                foreach (var m in msgsCorredor) Ctx.Msg("  corredor: " + m);
            }
            foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Desenho nas camadas {LayerRotatoria}, {LayerIlha}, {LayerBordo}, {LayerRetencao}, {LayerTrajetoria} (vermelho = direto, azul = à esquerda, verde = à direita) e {LayerVisibilidade} (triângulos de visibilidade: magenta = circulante, ciano = entrante). " +
                    "Confira com PRJCASAMENTO › Varios. U desfaz a rotatória inteira.");

            // relatório
            var rel = new RelatorioRotatoria { Titulo = prefixo, Desenho = s.Doc.Name, Modelo = mod, GreideAnel = $"{origemCotas}; erro máx. {erroAnel * 1000:0.0} mm; rampa máx. {Pct(Math.Abs(rampaMax.Rampa))}" };
            rel.Parametros.AddRange(ParametrosDoRelatorio(cfg));
            for (int i = 0; i < bracos.Count; i++)
                rel.Dados.Add(new DadosBraco(bracos[i].Eixo.Name, bracos[i].S, RotatoriaCivil.Est(bracos[i].S), cotas[i], feitas[i, 0]?.C.Desenvolvimento, feitas[i, 1]?.C.Desenvolvimento));
            rel.Analises.AddRange(analises);
            rel.Visadas.AddRange(visadas);
            rel.Corredor.AddRange(corredor);
            rel.Montagens.AddRange(montagens.Select(m => $"{m.Nome}: {m.Uso}"));
            rel.Verificacoes.AddRange(verif);
            rel.Avisos.AddRange(avisos);
            rel.Pendencias.AddRange(Pendencias());
            if (s.PedirPalavra("Gravar o relatório da rotatória em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", Texto.IdSeguro(prefixo.Replace(' ', '_'), 60) + ".xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Relatório gravado: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJROTATORIA: {ex.Message}"); }
    }

    private static Polyline CirculoPolilinha(Point3d o, double r, string layer)
    {
        var p = new Polyline();
        p.AddVertexAt(0, new Point2d(o.X + r, o.Y), 1, 0, 0);
        p.AddVertexAt(1, new Point2d(o.X - r, o.Y), 1, 0, 0);
        p.Closed = true;
        p.SetDatabaseDefaults();
        p.Layer = layer;
        return p;
    }

    /// <summary>
    /// Trechos do corredor: anel (aberturas dos braços entre as tangências das conexões = só pista; o resto com meio-fio e
    /// passeio), ramos (junto do anel só pista, depois ilha divisória, depois pista completa) e conexões (passeio do lado do
    /// centro da curva, que é o canto fora da pista).
    /// </summary>
    private static List<TrechoCorredor> GuiaDoCorredor(Sessao s, Alignment anel, Point3d o, double rExt, double st0, double comp,
        List<Braco> bracos, IReadOnlyList<double> sAnel, Conectada?[,] feitas, ModeloRotatoria mod, bool comIlha,
        ObjectId bordoExterno, IReadOnlyList<List<ObjectId>> bordosDoBraco, List<string> avisos)
    {
        double Mod(double x) => ((x % comp) + comp) % comp;
        var r = new List<TrechoCorredor>();
        void Add(Alignment eixo, double a, double b, string montagem, string obs, IEnumerable<ObjectId>? alvos = null, bool maisPerto = false)
        {
            if (Math.Abs(b - a) < 0.01) return;
            r.Add(new TrechoCorredor(eixo.ObjectId, eixo.Name, Math.Min(a, b), Math.Max(a, b), montagem, obs)
            {
                AlvosLargura = alvos?.Where(id => !id.IsNull).ToList() ?? new List<ObjectId>(),
                AlvoMaisPerto = maisPerto,
            });
        }
        List<ObjectId> Conexoes(int i) => new[] { feitas[i, 0]?.Id, feitas[i, 1]?.Id }.Where(id => id is { IsNull: false }).Select(id => id!.Value).ToList();

        // anel: aberturas dos braços (entre as tangências das duas conexões) só com pista; o resto com meio-fio e passeio
        var ints = new List<(double A, double B, string Rumo)>();
        for (int i = 0; i < bracos.Count; i++)
        {
            var ss = new List<double>();
            for (int k = 0; k < 2; k++)
                if (feitas[i, k] is { } f && s.Tr.GetObject(f.Id, OpenMode.ForRead) is Alignment ca)
                {
                    var p0 = RotatoriaCivil.Ponto(ca, ca.StartingStation, 0);
                    var p1 = RotatoriaCivil.Ponto(ca, ca.EndingStation, 0);
                    var p = Math.Abs(p0.DistanceTo(o) - rExt) < Math.Abs(p1.DistanceTo(o) - rExt) ? p0 : p1;
                    if (RotatoriaCivil.EstacaAfast(anel, p) is { } so) ss.Add(so.S);
                }
            if (ss.Count < 2) { avisos.Add($"Corredor: a abertura do braço {bracos[i].Rumo} no anel não foi calculada (falta conexão); acerte as regiões do anel à mão."); continue; }
            double sj = sAnel[i], f1 = Mod(ss[0] - sj), f2 = Mod(ss[1] - sj);
            double a = Mod(sj - (comp - Math.Max(f1, f2)) - st0), b = Mod(sj + Math.Min(f1, f2) - st0);
            if (a <= b) ints.Add((a, b, bracos[i].Rumo));
            else { ints.Add((a, comp, bracos[i].Rumo)); ints.Add((0, b, bracos[i].Rumo)); }
        }
        double pos = 0;
        foreach (var (a, b, rumo) in ints.OrderBy(x => x.A))
        {
            if (a > pos) Add(anel, st0 + pos, st0 + a, "Anel - pista, meio-fio e passeio", "entre dois braços: bordo externo com meio-fio");
            Add(anel, st0 + Math.Max(a, pos), st0 + b, "Anel - só pista", $"abertura do braço {rumo} (entre as conexões)");
            pos = Math.Max(pos, b);
        }
        if (pos < comp) Add(anel, st0 + pos, st0 + comp, "Anel - pista, meio-fio e passeio", "entre dois braços: bordo externo com meio-fio");

        // ramos: junto do anel só pista (até as tangências), ilha divisória, alargamento e seção normal
        for (int i = 0; i < bracos.Count && i < mod.Bracos.Count; i++)
        {
            var b = bracos[i];
            var br = mod.Bracos[i];
            double Sm(double x) => b.S + b.SentidoFora * x;
            double sBordo = EstacaNoRaio(br.Eixo, o, rExt);
            double sTang = new[] { br.STangEntrada, br.STangSaida }.Where(v => !double.IsNaN(v)).DefaultIfEmpty(sBordo).Max();
            sTang = Math.Max(sTang, sBordo);
            Add(b.Eixo, Sm(sBordo), Sm(sTang), "Ramo junto do anel - só pista",
                $"braço {b.Rumo}: da borda do anel às tangências das conexões; faixas até os eixos das conexões", Conexoes(i));
            double depois = sTang;
            if (comIlha && br.TemIlha && br.SIlhaFim > sTang)
            {
                Add(b.Eixo, Sm(sTang), Sm(br.SIlhaFim), "Ramo com ilha divisória",
                    $"braço {b.Rumo}: até o nariz da ilha; faixas até os bordos (PRJ-ROTATORIA-BORDO)", bordosDoBraco[i]);
                depois = br.SIlhaFim;
            }
            if (br.SAlargFim > depois)
                Add(b.Eixo, Sm(depois), Sm(br.SAlargFim), "Pista completa", $"braço {b.Rumo}: alargamento; faixas até os bordos (PRJ-ROTATORIA-BORDO)", bordosDoBraco[i]);
            Add(b.Eixo, Sm(Math.Max(depois, br.SAlargFim)), Sm(Math.Max(depois, br.SAlargFim) + 20), "Pista completa",
                $"braço {b.Rumo}: seção normal daqui para fora (depois segue o corredor da via)");
        }

        // conexões: passeio do lado do centro da curva (o canto fora da pista); faixa até o bordo do anel ou do ramo, o que estiver mais perto
        for (int i = 0; i < bracos.Count; i++)
            for (int k = 0; k < 2; k++)
                if (feitas[i, k] is { } f && s.Tr.GetObject(f.Id, OpenMode.ForRead) is Alignment ca)
                {
                    var arco = ca.Entities.Cast<AlignmentEntity>().OfType<AlignmentArc>().FirstOrDefault();
                    string lado = arco is not null && arco.Clockwise ? "direita" : "esquerda";
                    var alvos = new List<ObjectId> { bordoExterno };
                    if (i < bordosDoBraco.Count) alvos.AddRange(bordosDoBraco[i]);
                    Add(ca, ca.StartingStation, ca.EndingStation, $"Conexão - passeio à {lado}",
                        $"meio-fio, passeio e talude à {lado} (fora da curva); faixa até o bordo do anel ou do ramo", alvos, maisPerto: true);
                }
        return r;
    }

    private static ObjectId Desenhar(Sessao s, BlockTableRecord ms, IReadOnlyList<(double X, double Y)> pts, bool fechada, string layer, short cor)
    {
        if (pts.Count < 2) return ObjectId.Null;
        var p = new Polyline();
        for (int i = 0; i < pts.Count; i++) p.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        p.Closed = fechada;
        p.SetDatabaseDefaults();
        p.Layer = layer;
        p.ColorIndex = cor;
        return Ctx.Adicionar(s.Tr, ms, p);
    }

    /// <summary>O eixo do ramo do encontro para fora, amostrado a cada 1 m (até <paramref name="comprimento"/> ou o fim do eixo).</summary>
    private static PolilinhaEixo PolilinhaDoBraco(Braco b, double comprimento, out bool prolongado)
    {
        var pts = new List<(double X, double Y)>();
        double ini = b.Eixo.StartingStation, fim = b.Eixo.EndingStation;
        double disp = b.SentidoFora > 0 ? fim - b.S : b.S - ini;
        double l = Math.Min(comprimento, disp);
        prolongado = disp < comprimento - 1e-6;
        int n = Math.Max(2, (int)Math.Ceiling(l));
        for (int k = 0; k <= n; k++)
        {
            var p = RotatoriaCivil.Ponto(b.Eixo, b.S + b.SentidoFora * l * k / n, 0);
            pts.Add((p.X, p.Y));
        }
        return new PolilinhaEixo(pts);
    }

    /// <summary>Estaca no eixo do braço onde ele atinge a distância <paramref name="r"/> do centro (bisseção; o braço sai do anel).</summary>
    private static double EstacaNoRaio(PolilinhaEixo e, Point3d o, double r)
    {
        double D(double s) { var p = e.Ponto(s, 0); return Math.Sqrt((p.X - o.X) * (p.X - o.X) + (p.Y - o.Y) * (p.Y - o.Y)); }
        double a = 0, b = Math.Max(e.Comprimento, r * 3);
        if (D(a) >= r) return 0;
        for (int i = 0; i < 80; i++) { double m = (a + b) / 2; if (D(m) < r) a = m; else b = m; }
        return (a + b) / 2;
    }

    private static IEnumerable<ParametroRotatoria> ParametrosDoRelatorio(ConfigRotatoria c)
    {
        string P(string referencia) => c.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA] valor de partida" + (referencia.Length > 0 ? " — " + referencia : "");
        string V(double v) => v.ToString("0.00");
        yield return new("DCI (diâmetro do círculo inscrito)", V(c.Dci), "m", P("sem estudo de tráfego; dimensionar pelo veículo de projeto e pela norma do órgão [A CONFIRMAR]"));
        yield return new("Largura da pista anelar", V(c.LarguraAnel), "m", P("TxDOT RDM: 18 a 20 ft (5,5 a 6,1 m) em rotatória de uma faixa"));
        yield return new("Faixa transponível (apron)", V(c.Apron), "m", P(""));
        yield return new("Largura de entrada", V(c.FaixaEntrada), "m", P("TxDOT RDM: 16 a 20 ft (4,9 a 6,1 m)"));
        yield return new("Largura de saída", V(c.FaixaSaida), "m", P(""));
        yield return new("Ilha divisória: largura junto do anel", V(c.IlhaLargura), "m", P("FDOT FDM 213: refúgio ≥ 6 ft (1,8 m)"));
        yield return new("Ilha divisória: comprimento", V(c.IlhaComprimento), "m", P("FDOT FDM 213: ≥ 100 ft (30,5 m)"));
        yield return new("Ilha divisória: largura no nariz", V(c.IlhaNariz), "m", P(""));
        yield return new("Ilha divisória: recuo do bordo do anel", V(c.IlhaRecuo), "m", P(""));
        yield return new("Meia largura normal dos ramos", V(c.MeiaLarguraRamo), "m", P("alargamento até os bordos da rotatória com taper 1:10 [PREMISSA]"));
        yield return new("Raio da conexão de entrada", V(c.RaioEntrada), "m", P(""));
        yield return new("Raio da conexão de saída", V(c.RaioSaida), "m", P(""));
        yield return new("Caimento transversal", (c.Caimento * 100).ToString("0.0"), "%", P("FHWA HIF-20-080: 2 % para fora da ilha central é típico"));
        yield return new("Cota do anel", c.CotaAnel, "", "escolha do comando (Greides / Terreno / Fixa)");
        yield return new("Afastamento da trajetória a meios-fios, ilhas e eixo da via", "1.50", "m", "NCHRP 672 (método da trajetória mais rápida)");
        yield return new("Início e fim da trajetória antes/depois da retenção", "50.00", "m", "NCHRP 672 (pelo menos 50 m, método britânico)");
        yield return new("Velocidade máxima de entrada V1", c.VelMaxEntrada.ToString("0"), "km/h", "TxDOT RDM 14.4.5 / NCHRP 672: 25 mph em uma faixa — referência complementar");
        yield return new("Diferença máxima V1–V2 e V1–V4", c.DifVelMax.ToString("0"), "km/h", "AASHTO via TORUS (20 km/h); o TxDOT usa 10 mph (16 km/h)");
        yield return new("Visibilidade à esquerda: tempo crítico tc", VisibilidadeRotatoria.TcPadrao.ToString("0.0"), "s", "NCHRP 672, 6.7.3 (d = 0,278·tc·V) — referência complementar [A CONFIRMAR na norma do órgão]");
        yield return new("Visibilidade à esquerda: observador antes da retenção", VisibilidadeRotatoria.RecuoObservador.ToString("0.00"), "m", "NCHRP 672, 6.7.3 (15 m)");
        if (!c.Montagens) yield break;
        yield return new("Montagens: revestimento / base / sub-base", $"{c.Revestimento:0.00} / {c.Base:0.00} / {c.SubBase:0.00}", "m", P("estrutura a dimensionar (DNIT / órgão) [A CONFIRMAR]"));
        yield return new("Montagens: meio-fio (altura / largura / profundidade)", $"{c.MeioFioAltura:0.00} / {c.MeioFioLargura:0.00} / {c.MeioFioProfundidade:0.00}", "m", P("padrão do órgão [A CONFIRMAR]"));
        yield return new("Montagens: sarjeta (largura / caimento)", $"{c.SarjetaLargura:0.00} m / {c.SarjetaCaimento * 100:0.0} %", "", P("padrão do órgão [A CONFIRMAR]"));
        yield return new("Montagens: passeio (largura / caimento / espessura)", $"{c.Passeio:0.00} m / {c.PasseioCaimento * 100:0.0} % / {c.PasseioEspessura:0.00} m", "", P("padrão do órgão [A CONFIRMAR]"));
        yield return new("Montagens: talude de corte / aterro (H:V)", $"{c.TaludeCorte:0.##}:1 / {c.TaludeAterro:0.##}:1", "", P("estudo geotécnico [A CONFIRMAR]"));
    }

    private static IEnumerable<string> Pendencias() => new[]
    {
        "Giro do veículo de projeto em todos os movimentos (inclusive retorno de 180°) com o PROJETA Giro (GIROSEGUIR / GIROVERIFICAR): largura varrida e folgas ao bordo e à ilha central.",
        "Visibilidade: distância de visibilidade de parada na aproximação, na pista anelar e nas travessias; conferir os triângulos de visibilidade à esquerda (desenhados) contra os obstáculos reais (paisagismo da ilha, postes, placas, muros).",
        "Confirmar os parâmetros na norma do órgão (DNIT — Manual de Projeto de Interseções, 2005; IPR 740 em travessia urbana): os critérios usados aqui são referências complementares (NCHRP 672, TxDOT, FDOT, MaineDOT).",
        "Sinalização horizontal e vertical (linha de retenção / dê a preferência, faixas de pedestres) conforme o CONTRAN.",
        "Corredor: as montagens saem prontas (subassemblies padrão do Civil, valores [PREMISSA] até você trocar em Parametros ou nas propriedades das peças); falta montar o corredor com os trechos da aba Corredor, os alvos de largura (bordos, ilhas, conexões) e a superfície do terreno nos taludes. Depois, a superfície de projeto e a drenagem (PRJREDE).",
        "Interferências (postes, árvores, acessos de lote, redes, faixa de domínio) e desapropriação, se houver.",
    };

    private static string ResumoParametros(ConfigRotatoria c) =>
        $"Rotatória: DCI {c.Dci:0.##} m, pista anelar {c.LarguraAnel:0.##} m{(c.Apron > 0 ? $" (+ faixa transponível {c.Apron:0.##} m)" : "")}, entrada {c.FaixaEntrada:0.##} m, saída {c.FaixaSaida:0.##} m, " +
        (c.IlhaLargura > 0 && c.IlhaComprimento > 0 ? $"ilha divisória {c.IlhaLargura:0.##} × {c.IlhaComprimento:0.##} m, " : "sem ilha divisória, ") +
        $"raio de entrada {c.RaioEntrada:0.##} m e de saída {c.RaioSaida:0.##} m, ramos com {c.MeiaLarguraRamo:0.##} m do eixo ao bordo, caimento {Pct(c.Caimento, 1)}, cota do anel pelos {c.CotaAnel.ToLowerInvariant()}" +
        (c.ParametrosDoUsuario ? "." : " [PREMISSA: valores de partida].");

    private static bool PedirParametros(Editor ed, ConfigRotatoria cfg)
    {
        double? N(string m, double v, bool neg = false, bool zero = false)
        {
            var r = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = neg, AllowZero = zero, DefaultValue = v, UseDefaultValue = true });
            return r.Status == PromptStatus.OK ? r.Value : r.Status == PromptStatus.None ? v : null;
        }
        if (N("Diâmetro do círculo inscrito, DCI (m)", cfg.Dci) is not { } dci) return false;
        if (N("Largura da pista anelar (m)", cfg.LarguraAnel) is not { } la) return false;
        if (N("Faixa transponível em volta da ilha central (m; 0 = sem)", cfg.Apron, zero: true) is not { } apr) return false;
        if (N("Largura da entrada, da ilha divisória ao meio-fio (m)", cfg.FaixaEntrada) is not { } fe) return false;
        if (N("Largura da saída (m)", cfg.FaixaSaida) is not { } fs) return false;
        if (N("Largura da ilha divisória junto do anel (m; 0 = sem ilha)", cfg.IlhaLargura, zero: true) is not { } il) return false;
        double ic = cfg.IlhaComprimento, inz = cfg.IlhaNariz, ir = cfg.IlhaRecuo;
        if (il > 0)
        {
            if (N("Comprimento da ilha divisória (m)", cfg.IlhaComprimento) is not { } v1) return false;
            if (N("Largura da ilha no nariz (m)", cfg.IlhaNariz, zero: true) is not { } v2) return false;
            if (N("Recuo da ilha em relação ao bordo do anel (m)", cfg.IlhaRecuo, zero: true) is not { } v3) return false;
            ic = v1; inz = v2; ir = v3;
        }
        if (N("Distância do eixo ao bordo nos ramos, longe da rotatória (m)", cfg.MeiaLarguraRamo) is not { } mr) return false;
        if (N("Raio de entrada (m)", cfg.RaioEntrada) is not { } re) return false;
        if (N("Raio de saída (m)", cfg.RaioSaida) is not { } rs) return false;
        if (N("Caimento transversal do eixo até o bordo (%; negativo = desce)", cfg.Caimento * 100, neg: true, zero: true) is not { } ca) return false;
        var pko = new PromptKeywordOptions("\nCota do anel nos braços [Greides/Terreno/Fixa]", "Greides Terreno Fixa") { AllowNone = true };
        pko.Keywords.Default = cfg.CotaAnel;
        var pk = ed.GetKeywords(pko);
        if (pk.Status == PromptStatus.Cancel || pk.Status == PromptStatus.Error) return false;
        var pkt = new PromptKeywordOptions("\nCalcular a trajetória mais rápida (R1 a R5) [Sim/Nao]", "Sim Nao") { AllowNone = true };
        pkt.Keywords.Default = cfg.Trajetorias ? "Sim" : "Nao";
        var pt = ed.GetKeywords(pkt);
        if (pt.Status == PromptStatus.Cancel || pt.Status == PromptStatus.Error) return false;
        if (!PedirMontagens(ed, cfg)) return false;
        cfg.Dci = dci; cfg.LarguraAnel = la; cfg.Apron = apr; cfg.FaixaEntrada = fe; cfg.FaixaSaida = fs;
        cfg.IlhaLargura = il; cfg.IlhaComprimento = ic; cfg.IlhaNariz = inz; cfg.IlhaRecuo = ir;
        cfg.MeiaLarguraRamo = mr; cfg.RaioEntrada = re; cfg.RaioSaida = rs; cfg.Caimento = ca / 100;
        if (pk.Status == PromptStatus.OK && !string.IsNullOrEmpty(pk.StringResult)) cfg.CotaAnel = pk.StringResult;
        if (pt.Status == PromptStatus.OK && !string.IsNullOrEmpty(pt.StringResult)) cfg.Trajetorias = pt.StringResult == "Sim";
        cfg.ParametrosDoUsuario = true;
        ConfigRotatoria.Salvar(cfg);
        return true;
    }

    /// <summary>Perguntas das montagens do corredor (rotatória, interseção e vias). false = cancelou.</summary>
    internal static bool PedirMontagens(Editor ed, ConfigRotatoria cfg)
    {
        double? N(string m, double v, bool neg = false, bool zero = false)
        {
            var r = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = neg, AllowZero = zero, DefaultValue = v, UseDefaultValue = true });
            return r.Status == PromptStatus.OK ? r.Value : r.Status == PromptStatus.None ? v : null;
        }
        var pkm = new PromptKeywordOptions("\nCriar as montagens do corredor (subassemblies) [Sim/Nao]", "Sim Nao") { AllowNone = true };
        pkm.Keywords.Default = cfg.Montagens ? "Sim" : "Nao";
        var pm = ed.GetKeywords(pkm);
        if (pm.Status == PromptStatus.Cancel || pm.Status == PromptStatus.Error) return false;
        bool montagens = pm.Status == PromptStatus.OK && !string.IsNullOrEmpty(pm.StringResult) ? pm.StringResult == "Sim" : cfg.Montagens;
        double rv = cfg.Revestimento, bs = cfg.Base, sbb = cfg.SubBase, mh = cfg.MeioFioAltura, ml = cfg.MeioFioLargura,
               sj = cfg.SarjetaLargura, pa = cfg.Passeio, tco = cfg.TaludeCorte, tat = cfg.TaludeAterro;
        if (montagens)
        {
            if (N("Espessura do revestimento (m)", rv) is not { } v1) return false;
            if (N("Espessura da base (m)", bs, zero: true) is not { } v2) return false;
            if (N("Espessura da sub-base (m)", sbb, zero: true) is not { } v3) return false;
            if (N("Altura do meio-fio acima da sarjeta (m)", mh) is not { } v4) return false;
            if (N("Largura do meio-fio (m)", ml) is not { } v5) return false;
            if (N("Largura da sarjeta (m)", sj) is not { } v6) return false;
            if (N("Largura do passeio (m)", pa) is not { } v7) return false;
            if (N("Talude de corte (H para 1 V)", tco) is not { } v8) return false;
            if (N("Talude de aterro (H para 1 V)", tat) is not { } v9) return false;
            rv = v1; bs = v2; sbb = v3; mh = v4; ml = v5; sj = v6; pa = v7; tco = v8; tat = v9;
        }
        cfg.Montagens = montagens;
        cfg.Revestimento = rv; cfg.Base = bs; cfg.SubBase = sbb; cfg.MeioFioAltura = mh; cfg.MeioFioLargura = ml;
        cfg.MeioFioProfundidade = Math.Max(cfg.MeioFioProfundidade, mh + 0.10);
        cfg.SarjetaLargura = sj; cfg.Passeio = pa; cfg.TaludeCorte = tco; cfg.TaludeAterro = tat;
        return true;
    }

    /// <summary>Braços em ordem de ângulo, sem repetir o mesmo ponto.</summary>
    private static List<Braco> Ordenar(List<Braco> bracos)
    {
        var r = bracos.OrderBy(b => b.Angulo).ToList();
        for (int i = r.Count - 1; i > 0; i--)
            if (r[i].J.DistanceTo(r[i - 1].J) < 0.5) r.RemoveAt(i);
        return r;
    }

    /// <summary>Ponta de um eixo que para antes do anel mas aponta para a rotatória: quanto falta e quanto prolongar até o ponto mais perto do centro.</summary>
    private sealed record Prolongamento2(bool NoFim, double Comprimento, double Falta);

    private static Prolongamento2? Prolongamento(Alignment al, Point3d o, double r)
    {
        Prolongamento2? melhor = null;
        foreach (bool fim in new[] { true, false })
        {
            double st = fim ? al.EndingStation : al.StartingStation;
            var p = RotatoriaCivil.Ponto(al, st, 0);
            var (dx, dy) = RotatoriaCivil.Direcao(al, st);
            if (!fim) { dx = -dx; dy = -dy; }
            double t = (o.X - p.X) * dx + (o.Y - p.Y) * dy;          // até o pé da perpendicular pelo centro
            double perp = Math.Abs((o.X - p.X) * dy - (o.Y - p.Y) * dx);
            double falta = p.DistanceTo(o) - r;
            if (t <= 0 || falta <= 0 || perp > r * 0.9 || falta > 300) continue;
            if (melhor is null || falta < melhor.Falta) melhor = new Prolongamento2(fim, t, falta);
        }
        return melhor;
    }

    /// <summary>
    /// Prolonga o eixo em reta tangente (no fim: as estacas não mudam; no início: o ponto de referência fica no início antigo,
    /// com a mesma estaca) e estende os greides de projeto com a rampa da ponta.
    /// </summary>
    private static bool Prolongar(Sessao s, Alignment al, Prolongamento2 pr, List<string> avisos)
    {
        double s0 = al.StartingStation, s1 = al.EndingStation;
        var p0 = RotatoriaCivil.Ponto(al, s0, 0);
        var greides = RotatoriaCivil.Greides(s.Tr, al).Select(g => (G: g, Ponta: RotatoriaCivil.CotaRampa(g, pr.NoFim ? Math.Min(s1, g.EndingStation) : Math.Max(s0, g.StartingStation)))).ToList();
        try
        {
            int n = al.Entities.Count;
            if (pr.NoFim) al.Entities.AddFixedLine(al.Entities.GetEntityByOrder(n - 1).EntityId, pr.Comprimento);
            else
            {
                al.Entities.AddFloatingLine(pr.Comprimento, al.Entities.GetEntityByOrder(0).EntityId);
                try { al.ReferencePoint = new Point2d(p0.X, p0.Y); al.ReferencePointStation = s0; }
                catch (System.Exception e) { avisos.Add($"'{al.Name}': as estacas podem ter mudado com o prolongamento no início ({e.Message}); confira os greides."); }
            }
        }
        catch (System.Exception e) { avisos.Add($"Não consegui prolongar '{al.Name}': {e.Message}"); return false; }
        double novo = pr.NoFim ? al.EndingStation : al.StartingStation;
        string sobreGreides = "";
        foreach (var (g, ponta) in greides)
        {
            if (ponta is not { } cr) continue;
            try
            {
                g.UpgradeOpen();
                double sp = pr.NoFim ? g.EndingStation : g.StartingStation;
                g.PVIs.AddPVI(novo, cr.Z + cr.G * (novo - sp));
                sobreGreides += $" O greide '{g.Name}' seguiu com a rampa da ponta ({cr.G * 100:0.00} %).";
            }
            catch (System.Exception e) { sobreGreides += $" O greide '{g.Name}' não foi estendido ({e.Message}): a cota do braço sai da rampa da ponta [A CONFIRMAR]."; }
        }
        avisos.Add($"'{al.Name}' foi prolongado {pr.Comprimento:0.00} m em reta no {(pr.NoFim ? "fim" : "início (estacas do trecho existente mantidas)")} até o centro da rotatória.{sobreGreides}");
        return true;
    }

    /// <summary>Onde o eixo cruza o círculo do eixo do anel (amostrado a cada 0,5 m e refinado por bisseção).</summary>
    private static List<Braco> Bracos(Alignment al, Point3d o, double r, out double dMin)
    {
        var res = new List<Braco>();
        double ini = al.StartingStation, fim = al.EndingStation, l = fim - ini;
        int n = (int)Math.Clamp(Math.Ceiling(l / 0.5), 10, 40000);
        double h = l / n;
        double dm = double.MaxValue;
        double F(double s)
        {
            double d = RotatoriaCivil.Ponto(al, s, 0).DistanceTo(o);
            dm = Math.Min(dm, d);
            return d - r;
        }
        double s0 = ini, f0 = F(ini);
        for (int k = 1; k <= n; k++)
        {
            double s1 = k == n ? fim : ini + k * h, f1 = F(s1);
            if (f0 * f1 < 0 || (f1 == 0 && f0 != 0))
            {
                double a = s0, b = s1, fa = f0;
                for (int it = 0; it < 60 && b - a > 1e-9; it++)
                {
                    double m = (a + b) / 2, fmv = F(m);
                    if (fa * fmv <= 0) b = m; else { a = m; fa = fmv; }
                }
                double sj = (a + b) / 2;
                var j = RotatoriaCivil.Ponto(al, sj, 0);
                double ang = GeometriaRotatoria.Normalizar(Math.Atan2(j.Y - o.Y, j.X - o.X));
                int sentido = f1 > f0 ? 1 : -1;
                res.Add(new Braco(al, sj, j, ang, sentido, GeometriaRotatoria.Rumo(ang)));
            }
            s0 = s1; f0 = f1;
        }
        dMin = dm;
        return res;
    }

    /// <summary>Cota do eixo do anel em cada braço (greides dos ramos, superfície ou fixa). null = cancelou.</summary>
    private static double[]? CotasDosBracos(Sessao s, ConfigRotatoria cfg, List<Braco> bracos, List<string> avisos, out string origem)
    {
        var z = new double?[bracos.Count];
        string modo = cfg.CotaAnel;
        origem = "";
        if (modo == "Greides")
        {
            for (int i = 0; i < bracos.Count; i++)
            {
                if (EscolherGreide(s, bracos[i].Eixo) is not { } g) continue;
                if (RotatoriaCivil.CotaRampa(g, bracos[i].S) is { } cr) { z[i] = cr.Z; continue; }
                // braço além do fim do greide (eixo prolongado): segue a rampa da ponta
                double sp = Math.Clamp(bracos[i].S, g.StartingStation, g.EndingStation);
                if (Math.Abs(bracos[i].S - sp) <= 300 && RotatoriaCivil.CotaRampa(g, sp) is { } ce)
                {
                    z[i] = ce.Z + ce.G * (bracos[i].S - sp);
                    avisos.Add($"Braço {bracos[i].Rumo} ('{bracos[i].Eixo.Name}'): o greide '{g.Name}' acaba a {Math.Abs(bracos[i].S - sp):0.0} m do anel; a cota do braço seguiu a rampa da ponta ({ce.G * 100:0.00} %) [A CONFIRMAR].");
                }
            }
            if (z.All(v => v is null))
            {
                Ctx.Msg("Nenhum ramo tem greide de projeto no encontro com o anel.");
                var resp = s.PedirPalavra("Cota do anel pelo", new[] { "Terreno", "Fixa" }, "Terreno");
                if (resp is null) return null;
                modo = resp;
            }
            else origem = "cotas dos greides dos ramos";
        }
        if (modo == "Terreno")
        {
            var sups = VolumesCivil.Superficies(s.Tr);
            if (sups.Count == 0) { Ctx.Msg("Não há superfície no desenho; use a cota fixa."); modo = "Fixa"; }
            else
            {
                int k = sups.Count == 1 ? 0 : s.EscolherDaLista("Superfície para a cota do anel", sups.Select(x => x.Nome).ToList(), 0);
                if (k < 0) return null;
                var sup = (Surface)s.Tr.GetObject(sups[k].Id, OpenMode.ForRead);
                for (int i = 0; i < bracos.Count; i++)
                    try { z[i] = sup.FindElevationAtXY(bracos[i].J.X, bracos[i].J.Y); } catch { }
                origem = $"superfície '{sups[k].Nome}' nos braços";
                if (z.All(v => v is null)) { Ctx.Msg($"A superfície '{sups[k].Nome}' não cobre os braços; use a cota fixa."); modo = "Fixa"; }
            }
        }
        if (modo == "Fixa")
        {
            var c = s.PedirNumero("Cota do anel (m)", cfg.CotaFixa);
            if (c is null) return null;
            cfg.CotaFixa = c.Value;
            ConfigRotatoria.Salvar(cfg);
            for (int i = 0; i < z.Length; i++) z[i] = c.Value;
            origem = $"cota fixa {c.Value:0.000}";
        }
        double media = z.Where(v => v is not null).Average(v => v!.Value);
        for (int i = 0; i < z.Length; i++)
            if (z[i] is null)
            {
                z[i] = media;
                avisos.Add($"Braço {bracos[i].Rumo} ('{bracos[i].Eixo.Name}'): sem cota no encontro (sem greide ou fora da superfície); usei a média dos outros, {media:0.000} [A CONFIRMAR].");
            }
        return z.Select(v => v!.Value).ToArray();
    }

    /// <summary>"Rotatória n": o primeiro número sem alinhamento com esse prefixo.</summary>
    private static string Prefixo(Transaction tr, CivilDocument civ)
    {
        var nomes = new List<string>();
        foreach (ObjectId id in civ.GetAlignmentIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Alignment al) nomes.Add(al.Name);
        for (int n = 1; ; n++)
        {
            string p = $"Rotatória {n}";
            if (!nomes.Any(x => x.StartsWith(p + " - ", StringComparison.OrdinalIgnoreCase))) return p;
        }
    }
}
