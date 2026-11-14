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

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJINTERSECAO — interseção em nível (T, cruzamento ou mais braços): clica perto do cruzamento e seleciona os eixos; o
/// centro vai para o encontro dos eixos, os braços saem onde cada eixo cruza o círculo de busca (confirma o número e
/// prolonga eixo curto), as esquinas são curvas de raio R tangentes aos bordos dos dois braços vizinhos, com greide casando
/// com os dois; bordos das vias, triângulos de visibilidade das aproximações com PARE, verificações (ângulo entre os eixos,
/// cota dos greides no cruzamento, rampas das aproximações, esquinas), montagens do corredor, corredor (existente ou novo)
/// e relatório em Excel. Não mexe nos eixos (a não ser no prolongamento que você aceitar).
/// </summary>
public sealed partial class CmdRotatoria
{
    internal const string LayerIntersecao = "PRJ-INTERSECAO";
    internal const string LayerIntersecaoBordo = "PRJ-INTERSECAO-BORDO";
    internal const string LayerIntersecaoVisib = "PRJ-INTERSECAO-VISIBILIDADE";
    internal const string LayerIntersecaoMont = "PRJ-INTERSECAO-MONTAGENS";

    private const double AnguloMinimoIntersecao = 60, RampaAproximacaoMax = 0.03, ComprimentoAproximacao = 20, TrechoForaIntersecao = 30;

    [CommandMethod("PRJINTERSECAO", CommandFlags.Modal)]
    public static void Intersecao()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            Point3d clique;
            while (true)
            {
                Ctx.Msg(ResumoIntersecao(cfg));
                var ppo = new PromptPointOptions("\nClique perto do cruzamento dos eixos");
                ppo.Keywords.Add("Parametros");
                ppo.AppendKeywordsToMessage = true;
                var pr = ed.GetPoint(ppo);
                if (pr.Status == PromptStatus.Keyword) { if (!PedirParametrosIntersecao(ed, cfg)) return; continue; }
                if (pr.Status != PromptStatus.OK) return;
                var wc = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                clique = new Point3d(wc.X, wc.Y, 0);
                break;
            }
            var f = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT") });
            var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os eixos das vias que se encontram: " }, f);
            if (ps.Status != PromptStatus.OK) return;

            using var s = Sessao.Abrir();
            var civ = CivilApplication.ActiveDocument;
            var avisos = new List<string>();
            var eixos = new List<Alignment>();
            foreach (var id in ps.Value.GetObjectIds())
            {
                var al = (Alignment)s.Tr.GetObject(id, OpenMode.ForRead);
                if (RotatoriaCivil.Fechado(al)) { avisos.Add($"'{al.Name}' é um eixo fechado e ficou de fora."); continue; }
                if ((al.Name.StartsWith("Interseção ", StringComparison.OrdinalIgnoreCase) || al.Name.StartsWith("Rotatória ", StringComparison.OrdinalIgnoreCase)) && al.Name.Contains(" - ")) continue;
                eixos.Add(al);
            }
            if (eixos.Count == 0) { Ctx.Msg("Nenhum eixo de via na seleção."); return; }

            // centro: o encontro de dois eixos (cruzamento ou ponta) mais perto do clique
            var o = clique;
            string origemCentro = "o ponto clicado (os eixos não se encontram perto dele)";
            double melhor = 40;
            for (int i = 0; i < eixos.Count; i++)
                for (int j = i + 1; j < eixos.Count; j++)
                    foreach (var e in RotatoriaCivil.Encontros(s.Tr, eixos[i], eixos[j], 20))
                    {
                        var p = e.Tipo == "cruzamento" || e.Tipo == "ponta com ponta" ? e.P : RotatoriaCivil.Ponto(e.Principal, e.SPrincipal, 0);
                        double d = p.DistanceTo(clique);
                        if (d >= melhor) continue;
                        melhor = d;
                        o = new Point3d(p.X, p.Y, 0);
                        origemCentro = $"{e.Tipo} de '{e.Ponta.Name}' em '{e.Principal.Name}'";
                    }

            double wMax = Math.Max(cfg.MeiaLarguraRamo, eixos.Max(a => cfg.Bordo(a.Name)));
            double rBusca = wMax + cfg.RaioEsquina + 5;

            // braços: onde cada eixo cruza o círculo de busca; confere o número e prolonga eixo curto
            var bracos = new List<Braco>();
            var foraDoCirculo = new List<Alignment>();
            foreach (var al in eixos)
            {
                var bs = Bracos(al, o, rBusca, out double dMin);
                if (bs.Count == 0)
                {
                    foraDoCirculo.Add(al);
                    avisos.Add($"'{al.Name}' não chega na interseção: passa a {dMin:0.00} m do centro (os braços são contados a {rBusca:0.00} m).");
                }
                bracos.AddRange(bs);
            }
            bracos = Ordenar(bracos);
            string rumos = bracos.Count == 0 ? "nenhum" : string.Join(", ", bracos.Select(b => $"{b.Rumo} ({b.Eixo.Name})"));
            var pio = new PromptIntegerOptions($"\nBraços encontrados: {bracos.Count} — {rumos}. Quantos braços tem a interseção")
            { AllowNone = true, AllowZero = false, AllowNegative = false, DefaultValue = Math.Max(1, bracos.Count), UseDefaultValue = true };
            var pin = ed.GetInteger(pio);
            if (pin.Status != PromptStatus.OK && pin.Status != PromptStatus.None) return;
            int esperado = pin.Status == PromptStatus.OK ? pin.Value : Math.Max(1, bracos.Count);
            if (esperado > bracos.Count)
                foreach (var al in foraDoCirculo)
                {
                    if (Prolongamento(al, o, rBusca) is not { } pr) continue;
                    var resp = s.PedirPalavra($"'{al.Name}' para a {pr.Falta:0.0} m do círculo da interseção, apontando para ela. Prolongar o {(pr.NoFim ? "fim" : "início")} dele {pr.Comprimento:0.0} m em reta, até o cruzamento",
                                              new[] { "Sim", "Nao" }, "Sim");
                    if (resp is null) return;
                    if (resp != "Sim") continue;
                    al.UpgradeOpen();
                    if (!Prolongar(s, al, pr, avisos)) continue;
                    avisos.RemoveAll(a => a.StartsWith($"'{al.Name}' não chega", StringComparison.Ordinal));
                    bracos.AddRange(Bracos(al, o, rBusca, out _));
                    bracos = Ordenar(bracos);
                    if (bracos.Count >= esperado) break;
                }
            if (esperado != bracos.Count)
            {
                foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
                Ctx.Msg($"Achei {bracos.Count} braço(s) e você informou {esperado}.");
                if (bracos.Count < 2 || s.PedirPalavra($"Continuar com {bracos.Count} braço(s)", new[] { "Sim", "Nao" }, "Nao") != "Sim")
                {
                    Ctx.Msg("Nada foi criado. Selecione todos os eixos (um eixo que atravessa a interseção conta como dois braços) e rode de novo.");
                    return;
                }
            }
            if (bracos.Count < 2) { foreach (var a in avisos) Ctx.Msg("AVISO: " + a); Ctx.Msg("Uma interseção precisa de pelo menos dois braços: nada foi criado."); return; }
            int n = bracos.Count;

            // meia largura de cada eixo (lembrada pelo nome) e a via principal
            var distintos = bracos.Select(b => b.Eixo).DistinctBy(a => a.ObjectId).ToList();
            var w = new Dictionary<ObjectId, double>();
            foreach (var al in distintos)
            {
                double padrao = cfg.Bordo(al.Name) > 0 ? cfg.Bordo(al.Name) : cfg.MeiaLarguraRamo;
                var v = s.PedirNumero($"Meia largura de '{al.Name}' (do eixo ao bordo, m)", padrao);
                if (v is null) return;
                w[al.ObjectId] = v.Value;
                cfg.Bordos[al.Name] = v.Value;
            }
            ConfigRotatoria.Salvar(cfg);
            var ordem = distintos.OrderByDescending(a => bracos.Count(b => b.Eixo.ObjectId == a.ObjectId)).ThenByDescending(a => a.Length).ToList();
            var principal = ordem[0];
            if (ordem.Count > 1)
            {
                int k = s.EscolherDaLista("Via principal (preferencial; as outras param no PARE)", ordem.Select(a => a.Name).ToList(), 0);
                if (k < 0) return;
                principal = ordem[k];
            }
            double wP = w[principal.ObjectId];

            string prefixo = PrefixoIntersecao(s.Tr, civ);
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);

            // esquinas: curva de raio R tangente ao bordo anti-horário do braço i e ao bordo horário do braço seguinte
            var esquinas = new List<(int I, int J, Conectada? C, double Alfa)>();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                if (n == 2 && i == 1) break;
                var bi = bracos[i];
                var bj = bracos[j];
                double alfa = GeometriaIntersecao.AnguloAntiHorario(bi.Angulo, bj.Angulo);
                if (n == 2 && alfa > Math.PI) { (bi, bj) = (bj, bi); (i, j) = (1, 0); alfa = 2 * Math.PI - alfa; }
                if (bi.Eixo.ObjectId == bj.Eixo.ObjectId || alfa >= 175 * Math.PI / 180 || alfa < 10 * Math.PI / 180) { if (n == 2) break; continue; }
                double wi = w[bi.Eixo.ObjectId], wj = w[bj.Eixo.ObjectId];
                if (GeometriaIntersecao.CentroEsquina(bi.Angulo, bj.Angulo, wi, wj, cfg.RaioEsquina) is not { } cc) continue;
                var centro = new Point3d(o.X + cc.X, o.Y + cc.Y, 0);
                var pA = RotatoriaCivil.Ponto(bi.Eixo, RotatoriaCivil.EstacaPerto(bi.Eixo, centro), 0);
                var pB = RotatoriaCivil.Ponto(bj.Eixo, RotatoriaCivil.EstacaPerto(bj.Eixo, centro), 0);
                var feito = Conectar(s, cfg, bi.Eixo.ObjectId, pA, bj.Eixo.ObjectId, pB, cfg.RaioEsquina, $"{prefixo} - esquina {bi.Rumo}-{bj.Rumo}", wi, wj);
                if (feito is null) avisos.Add($"Esquina {bi.Rumo}-{bj.Rumo}: o raio de {cfg.RaioEsquina:0.##} m não coube entre '{bi.Eixo.Name}' e '{bj.Eixo.Name}'.");
                esquinas.Add((i, j, feito, alfa));
                if (n == 2) break;
            }

            // lado do passeio de cada esquina: o do centro da curva (o canto fora da pista); lido antes do Commit
            var ladoEsquina = esquinas.Select(e => e.C is not null && s.Tr.GetObject(e.C.Id, OpenMode.ForRead) is Alignment ca
                && ca.Entities.Cast<AlignmentEntity>().OfType<AlignmentArc>().FirstOrDefault()?.Clockwise == true ? "direita" : "esquerda").ToList();

            // distância da esquina mais longe em cada braço (até ali a via fica "só faixas")
            var tang = new double[n];
            for (int i = 0; i < n; i++) tang[i] = w.Values.Max() + cfg.RaioEsquina * 0.5;
            foreach (var (i, j, c, _) in esquinas)
            {
                if (c is null) continue;
                tang[i] = Math.Max(tang[i], Math.Sqrt(Math.Pow(c.C.TangenteA.X - o.X, 2) + Math.Pow(c.C.TangenteA.Y - o.Y, 2)));
                tang[j] = Math.Max(tang[j], Math.Sqrt(Math.Pow(c.C.TangenteB.X - o.X, 2) + Math.Pow(c.C.TangenteB.Y - o.Y, 2)));
            }

            // bordos de cada braço (do centro até 30 m depois da esquina): desenho e alvo de largura das esquinas
            var layB = Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecaoBordo, 30);
            var bordos = new List<ObjectId>[n];
            var sCentro = new double[n];
            for (int i = 0; i < n; i++)
            {
                var b = bracos[i];
                bordos[i] = new List<ObjectId>();
                sCentro[i] = RotatoriaCivil.EstacaPerto(b.Eixo, o);
                double sFim = EstacaNaDistancia(b, sCentro[i], o, tang[i] + TrechoForaIntersecao);
                double wi = w[b.Eixo.ObjectId];
                foreach (int lado in new[] { +1, -1 })
                {
                    var pts = new List<(double X, double Y)>();
                    int m = Math.Max(2, (int)Math.Ceiling(Math.Abs(sFim - sCentro[i])));
                    for (int k = 0; k <= m; k++)
                    {
                        double st = sCentro[i] + (sFim - sCentro[i]) * k / m;
                        var p = RotatoriaCivil.Ponto(b.Eixo, st, lado * wi);
                        pts.Add((p.X, p.Y));
                    }
                    var id = Desenhar(s, ms, pts, false, layB, 256);
                    if (!id.IsNull) bordos[i].Add(id);
                }
            }

            // visibilidade das aproximações com PARE (as das vias que não são a principal)
            var layV = Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecaoVisib, 6);
            double dVis = GeometriaIntersecao.DistanciaVisibilidade(cfg.VelPrincipal);
            var visadas = new List<VisadaIntersecao>();
            var bracosPrincipais = Enumerable.Range(0, n).Where(i => bracos[i].Eixo.ObjectId == principal.ObjectId).ToList();
            for (int i = 0; i < n; i++)
            {
                if (bracos[i].Eixo.ObjectId == principal.ObjectId || bracosPrincipais.Count == 0) continue;
                var b = bracos[i];
                double wi = w[b.Eixo.ObjectId];
                double s0 = EstacaNaDistancia(b, sCentro[i], o, wP + GeometriaIntersecao.RecuoMotorista);
                var p0 = RotatoriaCivil.Ponto(b.Eixo, s0, 0);
                var (dx, dy) = RotatoriaCivil.Direcao(b.Eixo, s0);
                dx *= b.SentidoFora; dy *= b.SentidoFora;
                var obs = (X: p0.X - dy * wi / 2, Y: p0.Y + dx * wi / 2);
                var situacoes = new List<string>();
                foreach (int k in bracosPrincipais)
                {
                    var pol = PolilinhaDoBraco(bracos[k], dVis + 20, out _);
                    var alvo = pol.Ponto(Math.Max(0, dVis - rBusca), wP / 2);
                    Desenhar(s, ms, new[] { obs, alvo }, false, layV, 6);
                    var cortes = esquinas.Where(e => e.C is not null && GeometriaIntersecao.SegmentoCortaCirculo(obs, alvo, e.C!.C.Centro, e.C.C.Raio)).Select(e => $"{bracos[e.I].Rumo}-{bracos[e.J].Rumo}").ToList();
                    situacoes.Add($"vindo do braço {bracos[k].Rumo}: " + (cortes.Count == 0 ? "a visada fica sobre a pista" : $"a visada passa pela esquina {string.Join(", ", cortes)} (fora da pista: a área tem de ficar livre)"));
                }
                var tx = Ctx.Texto($"visibilidade {b.Rumo}: {dVis:0.0} m", new Point3d(obs.X, obs.Y, 0), 0.8);
                tx.Layer = layV;
                Ctx.Adicionar(s.Tr, ms, tx);
                visadas.Add(new VisadaIntersecao($"{b.Rumo} ({b.Eixo.Name})", dVis, cfg.VelPrincipal, GeometriaIntersecao.TempoPadrao, string.Join("; ", situacoes)));
            }

            // verificações
            var verif = new List<Verificacao>();
            // cada via secundária contra a principal (duas secundárias em sequência não se cruzam)
            foreach (var B in distintos.Where(x => x.ObjectId != principal.ObjectId))
                {
                    var A = principal;
                    double sa = RotatoriaCivil.EstacaPerto(A, o), sb = RotatoriaCivil.EstacaPerto(B, o);
                    var (ax, ay) = RotatoriaCivil.Direcao(A, sa);
                    var (bx, by) = RotatoriaCivil.Direcao(B, sb);
                    // ângulo entre as retas dos dois eixos no cruzamento, de 0° (paralelos) a 90° (perpendiculares)
                    double angulo = Math.Acos(Math.Clamp(Math.Abs(ax * bx + ay * by), 0, 1)) * 180 / Math.PI;
                    verif.Add(new Verificacao("Ângulo entre os eixos", $"'{A.Name}' × '{B.Name}'", $"{angulo:0.0}°", $"≥ {AnguloMinimoIntersecao:0}°",
                        "AASHTO Green Book (eixos que se cruzam a menos de 60° pedem correção) — referência complementar [A CONFIRMAR no DNIT]",
                        angulo >= AnguloMinimoIntersecao - 1e-6 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                    var gA = EscolherGreide(s, A);
                    var gB = EscolherGreide(s, B);
                    if (gA is null || gB is null) { verif.Add(new Verificacao("Cota dos greides no cruzamento", $"'{A.Name}' × '{B.Name}'", "sem greide de projeto em um dos eixos", $"|Δz| ≤ {cfg.TolCota:0.000} m", "geometria", VerificacaoRotatoria.Informativo)); continue; }
                    double da = RotatoriaCivil.Ponto(A, sa, 0).DistanceTo(o), db = RotatoriaCivil.Ponto(B, sb, 0).DistanceTo(o);
                    if (RotatoriaCivil.CotaRampa(gA, sa) is { } za && RotatoriaCivil.CotaRampa(gB, sb) is { } zb)
                    {
                        double dz = zb.Z - za.Z;
                        bool noCentro = da < 0.5 && db < 0.5;
                        verif.Add(new Verificacao("Cota dos greides no cruzamento", $"'{A.Name}' × '{B.Name}'",
                            $"'{A.Name}' {za.Z:0.000} | '{B.Name}' {zb.Z:0.000} → Δz {Sn(dz, 3)} m" + (noCentro ? "" : $" (pontos a {da:0.00} e {db:0.00} m do centro)"),
                            $"|Δz| ≤ {cfg.TolCota:0.000} m", "geometria: os greides têm de se encontrar na mesma cota (acerte com PRJCASAMENTO)",
                            !noCentro ? VerificacaoRotatoria.Informativo : Math.Abs(dz) <= cfg.TolCota + 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                    }
                }
            var rampas = new double?[n];
            for (int i = 0; i < n; i++)
            {
                var b = bracos[i];
                if (EscolherGreide(s, b.Eixo) is not { } g) continue;
                double s1 = EstacaNaDistancia(b, sCentro[i], o, tang[i]), s2 = EstacaNaDistancia(b, sCentro[i], o, tang[i] + ComprimentoAproximacao);
                if (Math.Abs(s2 - s1) < 1 || RotatoriaCivil.CotaRampa(g, s1) is not { } z1 || RotatoriaCivil.CotaRampa(g, s2) is not { } z2) continue;
                double rampa = (z1.Z - z2.Z) / Math.Abs(s2 - s1);   // + = sobe chegando na interseção
                rampas[i] = rampa;
                verif.Add(new Verificacao("Rampa na aproximação (20 m antes da esquina)", $"{b.Rumo} ({b.Eixo.Name})", $"{Sn(rampa * 100, 2)} % (+ = sobe chegando)",
                    $"|i| ≤ {RampaAproximacaoMax * 100:0} %", "AASHTO Green Book (greides nas aproximações até cerca de 3 %) — referência complementar [A CONFIRMAR]",
                    Math.Abs(rampa) <= RampaAproximacaoMax + 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Atencao));
            }
            foreach (var (i, j, c, alfa) in esquinas)
                verif.Add(c is null
                    ? new Verificacao("Esquina", $"{bracos[i].Rumo}-{bracos[j].Rumo}", $"o raio de {cfg.RaioEsquina:0.##} m não coube", "a curva cabe entre os bordos", "geometria", VerificacaoRotatoria.Reprovado)
                    : new Verificacao("Esquina", $"{bracos[i].Rumo}-{bracos[j].Rumo}", $"R {c.C.Raio:0.00} m, {c.C.Desenvolvimento:0.00} m de curva, ângulo {alfa * 180 / Math.PI:0.0}°{(c.ComGreide ? "" : ", sem greide")}",
                        "giro do veículo de projeto", "conferir com o PROJETA Giro (GIROSEGUIR / GIROVERIFICAR) [A CONFIRMAR]", VerificacaoRotatoria.Informativo));
            foreach (var v in visadas)
                verif.Add(new Verificacao("Visibilidade de cruzamento (aproximação com PARE)", v.Aproximacao, $"{v.Distancia:0.0} m ao longo da via principal; {v.Situacao}",
                    "triângulo livre de obstáculos", $"d = 0,278·V·tg, V {v.Velocidade:0} km/h, tg {v.Tempo:0.0} s (AASHTO, caso B1) — referência complementar [A CONFIRMAR]",
                    v.Situacao.Contains("esquina", StringComparison.Ordinal) ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Aprovado));

            // montagens, trechos do corredor e corredor
            var montagens = new List<MontagensRotatoria.Criada>();
            var trechos = new List<TrechoCorredor>();
            var msgsCorredor = new List<string>();
            string? nomeCorredor = null;
            int regioes = 0;
            if (cfg.Montagens)
            {
                var defs = MontagensRotatoria.DefinirIntersecao(cfg, distintos.Select(a => (a.Name, w[a.ObjectId])));
                const double passo = 40;
                var org = new Point3d(o.X - (defs.Count - 1) * passo / 2, o.Y - rBusca - 80, 0);
                montagens = MontagensRotatoria.Criar(civ, s.Tr, prefixo, defs, org, passo, avisos);
                var layM = Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecaoMont, 7);
                foreach (var m in montagens)
                {
                    var tm = Ctx.Texto(m.Tipo, new Point3d(m.Local.X, m.Local.Y - 3, 0), 1.0, 0, AttachmentPoint.TopCenter);
                    tm.Layer = layM;
                    Ctx.Adicionar(s.Tr, ms, tm);
                }
                trechos = TrechosIntersecao(s, o, bracos, sCentro, tang, wP, principal, esquinas, ladoEsquina, bordos);
                if (CorredorProjeta.Escolher(s, civ, prefixo) is { } corrId)
                {
                    var sup = CorredorProjeta.Superficie(s);
                    if (sup.IsNull) avisos.Add("Corredor: não há superfície de terreno no desenho; os taludes ficaram sem alvo (defina depois nas regiões).");
                    Ctx.Msg("Montando o corredor...");
                    msgsCorredor = CorredorProjeta.Inserir(s.Tr, corrId, trechos, al => EscolherGreide(s, al)?.ObjectId, montagens.ToDictionary(m => m.Tipo, m => m.Id),
                                                           sup, prefixo, recortar: true, out regioes);
                    nomeCorredor = ((Corridor)s.Tr.GetObject(corrId, OpenMode.ForRead)).Name;
                }
            }

            var lay = Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecao, 30);
            var rot = Ctx.Texto(prefixo, new Point3d(o.X, o.Y - wMax - 2, 0), 1.2, 0, AttachmentPoint.TopCenter);
            rot.Layer = lay;
            Ctx.Adicionar(s.Tr, ms, rot);
            s.Tr.Commit();

            // mensagens
            string premissa = cfg.IntersecaoParametrosDoUsuario ? "" : " [PREMISSA: valores de partida, troque em Parametros]";
            Ctx.Msg($"{prefixo} criada no centro ({o.X:0.000}; {o.Y:0.000}) — {origemCentro}: {n} braços, via principal '{principal.Name}', raio de esquina {cfg.RaioEsquina:0.##} m, V da principal {cfg.VelPrincipal:0} km/h{premissa}.");
            foreach (var b in bracos) Ctx.Msg($"  braço {b.Rumo,-2} '{b.Eixo.Name}' (meia largura {w[b.Eixo.ObjectId]:0.00} m)");
            foreach (var (i, j, c, alfa) in esquinas)
                Ctx.Msg($"  esquina {bracos[i].Rumo}-{bracos[j].Rumo} ({alfa * 180 / Math.PI:0.0}°): " + (c is null ? "NÃO coube" : $"R {c.C.Raio:0.##} m, {c.C.Desenvolvimento:0.00} m{(c.ComGreide ? ", greide casando com os dois" : ", sem greide")}"));
            int ap = verif.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado);
            Ctx.Msg($"Verificações: {ap} aprovada(s), {verif.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADA(S), {verif.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção.");
            foreach (var v in verif.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao))
                Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor} (critério {v.Criterio}; {v.Fonte})");
            if (montagens.Count > 0)
            {
                Ctx.Msg($"Montagens do corredor: {montagens.Count} criadas abaixo da interseção (camada {LayerIntersecaoMont}). Trechos de cada eixo:");
                foreach (var g in trechos.GroupBy(t => t.Eixo))
                    Ctx.Msg($"  {g.Key}: " + string.Join("; ", g.Select(t => $"{RotatoriaCivil.Est(t.De)} a {RotatoriaCivil.Est(t.Ate)} → {t.Montagem}")));
            }
            if (nomeCorredor is not null)
            {
                Ctx.Msg($"Corredor '{nomeCorredor}': {regioes} região(ões) da interseção, com as montagens, os alvos de largura e o talude na superfície do terreno.");
                foreach (var m in msgsCorredor) Ctx.Msg("  corredor: " + m);
            }
            foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Desenho nas camadas {LayerIntersecaoBordo} (bordos) e {LayerIntersecaoVisib} (visadas). Confira com PRJCASAMENTO › Varios. U desfaz a interseção inteira.");

            // relatório
            var rel = new RelatorioIntersecao { Titulo = prefixo, Desenho = s.Doc.Name, Ox = o.X, Oy = o.Y, Principal = principal.Name };
            rel.Parametros.AddRange(ParametrosIntersecao(cfg, distintos, w));
            for (int i = 0; i < n; i++)
            {
                double alfa = GeometriaIntersecao.AnguloAntiHorario(bracos[i].Angulo, bracos[(i + 1) % n].Angulo) * 180 / Math.PI;
                rel.Bracos.Add(new DadosBracoIntersecao(bracos[i].Rumo, bracos[i].Eixo.Name, RotatoriaCivil.Est(bracos[i].S), alfa, w[bracos[i].Eixo.ObjectId], rampas[i], tang[i], bracos[i].Eixo.ObjectId == principal.ObjectId));
            }
            for (int e = 0; e < esquinas.Count; e++)
            {
                var (i, j, c, alfa) = esquinas[e];
                string lado = c is null ? "-" : ladoEsquina[e];
                rel.Esquinas.Add(new EsquinaIntersecao(c?.Nome ?? $"{prefixo} - esquina {bracos[i].Rumo}-{bracos[j].Rumo}", $"{bracos[i].Rumo}-{bracos[j].Rumo}", cfg.RaioEsquina, c?.C.Desenvolvimento, alfa * 180 / Math.PI, lado, c?.ComGreide ?? false, c is not null));
            }
            rel.Visadas.AddRange(visadas);
            rel.Verificacoes.AddRange(verif);
            rel.Corredor.AddRange(trechos.Select(t => new RegiaoCorredor(t.Eixo, t.De, t.Ate, $"{prefixo} - {t.Montagem}", t.Obs)));
            rel.Montagens.AddRange(montagens.Select(m => $"{m.Nome}: {m.Uso}"));
            rel.Avisos.AddRange(avisos);
            rel.Pendencias.AddRange(PendenciasIntersecao());
            if (s.PedirPalavra("Gravar o relatório da interseção em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
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
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJINTERSECAO: {ex.Message}"); }
    }

    /// <summary>
    /// Trechos do corredor da interseção: a via principal passa só com as faixas entre as esquinas; as outras começam no bordo
    /// da principal só com as faixas até as esquinas; depois das esquinas, pista completa; as esquinas com o passeio do lado do
    /// centro da curva e a faixa até o bordo mais perto das vias.
    /// </summary>
    private static List<TrechoCorredor> TrechosIntersecao(Sessao s, Point3d o, List<Braco> bracos, double[] sCentro, double[] tang, double wP,
        Alignment principal, List<(int I, int J, Conectada? C, double Alfa)> esquinas, List<string> ladoEsquina, List<ObjectId>[] bordos)
    {
        var r = new List<TrechoCorredor>();
        void Add(Alignment al, double a, double b, string montagem, string obs, IEnumerable<ObjectId>? alvos = null, bool maisPerto = false)
        {
            if (Math.Abs(b - a) < 0.05) return;
            r.Add(new TrechoCorredor(al.ObjectId, al.Name, Math.Min(a, b), Math.Max(a, b), montagem, obs)
            { AlvosLargura = alvos?.Where(id => !id.IsNull).ToList() ?? new List<ObjectId>(), AlvoMaisPerto = maisPerto });
        }
        int n = bracos.Count;
        var principais = Enumerable.Range(0, n).Where(i => bracos[i].Eixo.ObjectId == principal.ObjectId).ToList();
        if (principais.Count > 0)
        {
            var ests = principais.Select(i => EstacaNaDistancia(bracos[i], sCentro[i], o, tang[i])).ToList();
            if (principais.Count == 1) ests.Add(sCentro[principais[0]]);
            Add(principal, ests.Min(), ests.Max(), $"Pista só faixas - {principal.Name}", "via principal dentro da interseção (o meio-fio ali é o das esquinas)");
        }
        for (int i = 0; i < n; i++)
        {
            var b = bracos[i];
            double sT = EstacaNaDistancia(b, sCentro[i], o, tang[i]);
            if (b.Eixo.ObjectId != principal.ObjectId)
                Add(b.Eixo, EstacaNaDistancia(b, sCentro[i], o, wP), sT, $"Pista só faixas - {b.Eixo.Name}", $"braço {b.Rumo}: do bordo da via principal às esquinas");
            Add(b.Eixo, sT, EstacaNaDistancia(b, sCentro[i], o, tang[i] + TrechoForaIntersecao), $"Pista completa - {b.Eixo.Name}",
                $"braço {b.Rumo}: a partir das esquinas (depois segue o corredor da via)");
        }
        for (int e = 0; e < esquinas.Count; e++)
        {
            var (i, j, c, _) = esquinas[e];
            if (c is null || s.Tr.GetObject(c.Id, OpenMode.ForRead) is not Alignment ca) continue;
            string lado = ladoEsquina[e];
            Add(ca, ca.StartingStation, ca.EndingStation, $"Esquina - passeio à {lado}", $"esquina {bracos[i].Rumo}-{bracos[j].Rumo}: meio-fio, passeio e talude à {lado}; faixa até o bordo mais perto das vias",
                bordos[i].Concat(bordos[j]), maisPerto: true);
        }
        return r;
    }

    /// <summary>Estaca do eixo do braço a uma distância do centro (bisseção do centro para fora; para no fim do eixo).</summary>
    private static double EstacaNaDistancia(Braco b, double sCentro, Point3d o, double d)
    {
        double lim = b.SentidoFora > 0 ? b.Eixo.EndingStation : b.Eixo.StartingStation;
        double D(double st) => RotatoriaCivil.Ponto(b.Eixo, st, 0).DistanceTo(o);
        if (D(lim) <= d) return lim;
        double a = sCentro, c = lim;
        if (D(a) >= d) return a;
        for (int k = 0; k < 60; k++) { double m = (a + c) / 2; if (D(m) < d) a = m; else c = m; }
        return (a + c) / 2;
    }

    /// <summary>"Interseção n": o primeiro número sem alinhamento com esse prefixo.</summary>
    private static string PrefixoIntersecao(Transaction tr, CivilDocument civ)
    {
        var nomes = new List<string>();
        foreach (ObjectId id in civ.GetAlignmentIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Alignment al) nomes.Add(al.Name);
        for (int k = 1; ; k++)
        {
            string p = $"Interseção {k}";
            if (!nomes.Any(x => x.StartsWith(p + " - ", StringComparison.OrdinalIgnoreCase))) return p;
        }
    }

    private static string ResumoIntersecao(ConfigRotatoria c) =>
        $"Interseção: raio de esquina {c.RaioEsquina:0.##} m, meia largura padrão {c.MeiaLarguraRamo:0.##} m, caimento {Pct(c.Caimento, 1)}, V da via principal {c.VelPrincipal:0} km/h, " +
        (c.Montagens ? $"montagens (revestimento {c.Revestimento:0.00}, base {c.Base:0.00}, sub-base {c.SubBase:0.00}, passeio {c.Passeio:0.00} m)" : "sem montagens") +
        (c.IntersecaoParametrosDoUsuario ? "" : " [PREMISSA]") + ".";

    private static bool PedirParametrosIntersecao(Editor ed, ConfigRotatoria cfg)
    {
        double? N(string m, double v, bool neg = false, bool zero = false)
        {
            var r = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = neg, AllowZero = zero, DefaultValue = v, UseDefaultValue = true });
            return r.Status == PromptStatus.OK ? r.Value : r.Status == PromptStatus.None ? v : null;
        }
        if (N("Raio das esquinas (m)", cfg.RaioEsquina) is not { } re) return false;
        if (N("Meia largura padrão das vias, do eixo ao bordo (m)", cfg.MeiaLarguraRamo) is not { } mr) return false;
        if (N("Caimento transversal do eixo até o bordo (%; negativo = desce)", cfg.Caimento * 100, neg: true, zero: true) is not { } ca) return false;
        if (N("Velocidade de projeto da via principal (km/h)", cfg.VelPrincipal) is not { } vp) return false;
        if (!PedirMontagens(ed, cfg)) return false;
        cfg.RaioEsquina = re; cfg.MeiaLarguraRamo = mr; cfg.Caimento = ca / 100; cfg.VelPrincipal = vp;
        cfg.IntersecaoParametrosDoUsuario = true;
        ConfigRotatoria.Salvar(cfg);
        return true;
    }

    private static IEnumerable<ParametroRotatoria> ParametrosIntersecao(ConfigRotatoria c, List<Alignment> eixos, Dictionary<ObjectId, double> w)
    {
        string P(string referencia) => c.IntersecaoParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA] valor de partida" + (referencia.Length > 0 ? " — " + referencia : "");
        yield return new("Raio das esquinas", c.RaioEsquina.ToString("0.00"), "m", P("pelo veículo de projeto e pela norma do órgão (DNIT, Manual de Projeto de Interseções) [A CONFIRMAR]"));
        foreach (var al in eixos) yield return new($"Meia largura de '{al.Name}'", w[al.ObjectId].ToString("0.00"), "m", "informado pelo usuário");
        yield return new("Caimento transversal", (c.Caimento * 100).ToString("0.0"), "%", P(""));
        yield return new("Velocidade de projeto da via principal", c.VelPrincipal.ToString("0"), "km/h", P("[A CONFIRMAR]"));
        yield return new("Tempo de travessia tg (visibilidade)", GeometriaIntersecao.TempoPadrao.ToString("0.0"), "s", "AASHTO caso B1 (conversão à esquerda partindo do PARE, carro) — referência complementar [A CONFIRMAR]");
        yield return new("Motorista parado antes do bordo da principal", GeometriaIntersecao.RecuoMotorista.ToString("0.0"), "m", "AASHTO (14,5 ft)");
        yield return new("Ângulo mínimo entre os eixos", AnguloMinimoIntersecao.ToString("0"), "°", "AASHTO Green Book — referência complementar [A CONFIRMAR no DNIT]");
        yield return new("Rampa máxima nas aproximações", (RampaAproximacaoMax * 100).ToString("0.0"), "%", "AASHTO Green Book (cerca de 3 %) — referência complementar [A CONFIRMAR]");
        if (!c.Montagens) yield break;
        yield return new("Montagens: revestimento / base / sub-base", $"{c.Revestimento:0.00} / {c.Base:0.00} / {c.SubBase:0.00}", "m", P("estrutura a dimensionar (DNIT / órgão) [A CONFIRMAR]"));
        yield return new("Montagens: meio-fio / sarjeta / passeio", $"{c.MeioFioAltura:0.00} / {c.SarjetaLargura:0.00} / {c.Passeio:0.00}", "m", P("padrão do órgão [A CONFIRMAR]"));
        yield return new("Montagens: talude de corte / aterro (H:V)", $"{c.TaludeCorte:0.##}:1 / {c.TaludeAterro:0.##}:1", "", P("estudo geotécnico [A CONFIRMAR]"));
    }

    private static IEnumerable<string> PendenciasIntersecao() => new[]
    {
        "Giro do veículo de projeto em todas as conversões com o PROJETA Giro (GIROSEGUIR / GIROVERIFICAR): largura varrida e folga ao meio-fio das esquinas.",
        "Visibilidade: distância de visibilidade de parada nas aproximações e conferência dos triângulos de visibilidade (desenhados) contra os obstáculos reais (muros, vegetação, postes, placas).",
        "Confirmar os parâmetros na norma do órgão (DNIT — Manual de Projeto de Interseções, 2005; IPR 740 em travessia urbana): os critérios usados aqui são referências complementares (AASHTO).",
        "Sinalização horizontal e vertical (PARE / dê a preferência, faixas de pedestres, linhas de retenção) conforme o CONTRAN; ilha canalizadora e faixa de conversão, se o tráfego pedir.",
        "Drenagem das esquinas (sarjetas e bocas de lobo nos pontos baixos) com o PRJREDE.",
        "Interferências (postes, árvores, acessos de lote, redes, faixa de domínio) e desapropriação, se houver.",
    };
}
