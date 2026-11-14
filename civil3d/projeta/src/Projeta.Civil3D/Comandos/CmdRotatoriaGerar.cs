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
            foreach (var id in ps.Value.GetObjectIds())
            {
                var al = (Alignment)s.Tr.GetObject(id, OpenMode.ForRead);
                if (RotatoriaCivil.Fechado(al)) { avisos.Add($"'{al.Name}' é um eixo fechado (outro anel?) e ficou de fora."); continue; }
                if (al.Name.StartsWith("Rotatória ", StringComparison.OrdinalIgnoreCase) && al.Name.Contains(" - ")) continue; // conexões de uma rotatória anterior
                var bs = Bracos(al, o, rEixo, out double dMin);
                if (bs.Count == 0)
                    avisos.Add($"'{al.Name}' não chega no eixo do anel: passa a {dMin:0.00} m do centro e o eixo do anel fica a {rEixo:0.00} m. Prolongue o eixo até o anel (ou até o centro) e rode de novo.");
                bracos.AddRange(bs);
            }
            bracos = bracos.OrderBy(b => b.Angulo).ToList();
            for (int i = bracos.Count - 1; i > 0; i--)
                if (bracos[i].J.DistanceTo(bracos[i - 1].J) < 0.5) bracos.RemoveAt(i);
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
            Ctx.Adicionar(s.Tr, ms, new Circle(o, Vector3d.ZAxis, rExt) { Layer = lay });
            Ctx.Adicionar(s.Tr, ms, new Circle(o, Vector3d.ZAxis, rIlha) { Layer = lay });
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
            foreach (var br in mod.Bracos)
            {
                if (br.TemIlha) Desenhar(s, ms, mod.Ilha(br), true, layIlha, 256);
                double ate = Math.Min(br.Eixo.Comprimento, br.SAlargFim + 15);
                if (br.MeioFioEntrada is not null) Desenhar(s, ms, mod.BordoRamo(br, true, ate), false, layBordo, 256);
                if (br.MeioFioSaida is not null) Desenhar(s, ms, mod.BordoRamo(br, false, ate), false, layBordo, 256);
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

            // verificações
            var crit = new CriteriosRotatoria { VelMaxEntrada = cfg.VelMaxEntrada, DifVelMax = cfg.DifVelMax };
            var verif = VerificacaoRotatoria.Avaliar(mod, analises, crit);

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
            foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Desenho nas camadas {LayerRotatoria}, {LayerIlha}, {LayerBordo}, {LayerRetencao} e {LayerTrajetoria} (vermelho = direto, azul = à esquerda, verde = à direita). " +
                    "Confira com PRJCASAMENTO › Varios. U desfaz a rotatória inteira.");

            // relatório
            var rel = new RelatorioRotatoria { Titulo = prefixo, Desenho = s.Doc.Name, Modelo = mod, GreideAnel = $"{origemCotas}; erro máx. {erroAnel * 1000:0.0} mm; rampa máx. {Pct(Math.Abs(rampaMax.Rampa))}" };
            rel.Parametros.AddRange(ParametrosDoRelatorio(cfg));
            for (int i = 0; i < bracos.Count; i++)
                rel.Dados.Add(new DadosBraco(bracos[i].Eixo.Name, bracos[i].S, RotatoriaCivil.Est(bracos[i].S), cotas[i], feitas[i, 0]?.C.Desenvolvimento, feitas[i, 1]?.C.Desenvolvimento));
            rel.Analises.AddRange(analises);
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

    private static void Desenhar(Sessao s, BlockTableRecord ms, IReadOnlyList<(double X, double Y)> pts, bool fechada, string layer, short cor)
    {
        if (pts.Count < 2) return;
        var p = new Polyline();
        for (int i = 0; i < pts.Count; i++) p.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        p.Closed = fechada;
        p.SetDatabaseDefaults();
        p.Layer = layer;
        p.ColorIndex = cor;
        Ctx.Adicionar(s.Tr, ms, p);
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
    }

    private static IEnumerable<string> Pendencias() => new[]
    {
        "Giro do veículo de projeto em todos os movimentos (inclusive retorno de 180°) com o PROJETA Giro (GIROSEGUIR / GIROVERIFICAR): largura varrida e folgas ao bordo e à ilha central.",
        "Visibilidade: distância de visibilidade de parada na aproximação e visibilidade à esquerda na entrada.",
        "Confirmar os parâmetros na norma do órgão (DNIT — Manual de Projeto de Interseções, 2005; IPR 740 em travessia urbana): os critérios usados aqui são referências complementares (NCHRP 672, TxDOT, FDOT, MaineDOT).",
        "Sinalização horizontal e vertical (linha de retenção / dê a preferência, faixas de pedestres) conforme o CONTRAN.",
        "Corredor, superfície de projeto e drenagem (montagens próprias; o PRJREDE para a drenagem).",
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
        cfg.Dci = dci; cfg.LarguraAnel = la; cfg.Apron = apr; cfg.FaixaEntrada = fe; cfg.FaixaSaida = fs;
        cfg.IlhaLargura = il; cfg.IlhaComprimento = ic; cfg.IlhaNariz = inz; cfg.IlhaRecuo = ir;
        cfg.MeiaLarguraRamo = mr; cfg.RaioEntrada = re; cfg.RaioSaida = rs; cfg.Caimento = ca / 100;
        if (pk.Status == PromptStatus.OK && !string.IsNullOrEmpty(pk.StringResult)) cfg.CotaAnel = pk.StringResult;
        if (pt.Status == PromptStatus.OK && !string.IsNullOrEmpty(pt.StringResult)) cfg.Trajetorias = pt.StringResult == "Sim";
        cfg.ParametrosDoUsuario = true;
        ConfigRotatoria.Salvar(cfg);
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
                if (EscolherGreide(s, bracos[i].Eixo) is { } g && RotatoriaCivil.CotaRampa(g, bracos[i].S) is { } cr) z[i] = cr.Z;
            if (z.All(v => v is null))
            {
                Ctx.Msg("Nenhum ramo tem greide de projeto no encontro com o anel.");
                var resp = s.PedirPalavra("Cota do anel pelo [Terreno/Fixa]", new[] { "Terreno", "Fixa" }, "Terreno");
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
