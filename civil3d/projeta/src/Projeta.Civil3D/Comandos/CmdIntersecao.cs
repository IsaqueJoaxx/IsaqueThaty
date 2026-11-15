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
    internal const string LayerIntersecaoConv = "PRJ-INTERSECAO-CONVERSAO";
    internal const string LayerIntersecaoIlha = "PRJ-INTERSECAO-ILHA";

    private const double AnguloMinimoIntersecao = 60, RampaAproximacaoMax = 0.03, ComprimentoAproximacao = 20, TrechoForaIntersecao = 30;

    /// <summary>Linha de retenção da faixa de conversão: a 1 m do bordo da via transversal [PREMISSA].</summary>
    private const double RetencaoConversao = 1.0;

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
            var principaisIdx = Enumerable.Range(0, n).Where(i => bracos[i].Eixo.ObjectId == principal.ObjectId).ToList();
            var secundariosIdx = Enumerable.Range(0, n).Where(i => bracos[i].Eixo.ObjectId != principal.ObjectId).ToList();

            // faixa de conversão à esquerda: nas aproximações da principal que têm via secundária à esquerda de quem chega
            var temConversao = new bool[n];
            foreach (int i in principaisIdx)
                temConversao[i] = secundariosIdx.Any(k => GeometriaIntersecao.AnguloAntiHorario(bracos[k].Angulo, bracos[i].Angulo) is var d
                                                          && d > 10 * Math.PI / 180 && d < 170 * Math.PI / 180);
            bool conversao = false;
            double convDes = 0, convTeiper = 0;
            int vTabela = 0;
            if (temConversao.Any(x => x))
            {
                var r = s.PedirPalavra("Faixa de conversão à esquerda na via principal", new[] { "Sim", "Nao" }, "Nao");
                if (r is null) return;
                conversao = r == "Sim";
                (convDes, convTeiper, vTabela) = GeometriaIntersecao.DesaceleracaoDnit(cfg.VelPrincipal);
                if (cfg.ConvDesaceleracao > 0) convDes = cfg.ConvDesaceleracao;
                if (cfg.ConvTeiper > 0) convTeiper = cfg.ConvTeiper;
            }
            double alarg = conversao ? cfg.ConvLargura : 0;
            // meia largura junto do centro: na principal, com metade do alargamento de cada lado
            double W0(int i) => w[bracos[i].Eixo.ObjectId] + (bracos[i].Eixo.ObjectId == principal.ObjectId ? alarg / 2 : 0);

            // interseção em T: o lado da principal sem via transversal tem meio-fio contínuo (direita/esquerda do sentido do eixo)
            string? ladoContinuo = null;
            if (principaisIdx.Count >= 2 && secundariosIdx.Count > 0)
            {
                var (pdx, pdy) = RotatoriaCivil.Direcao(principal, RotatoriaCivil.EstacaPerto(principal, o));
                double Lado(int k) => pdx * Math.Sin(bracos[k].Angulo) - pdy * Math.Cos(bracos[k].Angulo);   // + = à esquerda do eixo
                bool aEsquerda = secundariosIdx.Any(k => Lado(k) > 0.1), aDireita = secundariosIdx.Any(k => Lado(k) < -0.1);
                ladoContinuo = !aDireita ? "direita" : !aEsquerda ? "esquerda" : null;
            }

            // ilha canalizadora nas esquinas (giro à direita com pista de conversão)
            string modoIlha = "Nao";
            {
                var r = s.PedirPalavra($"Ilha canalizadora nas esquinas (giro à direita: bordo interno R {cfg.CanalRaio:0.##} m, pista de {cfg.CanalLargura:0.##} m)",
                                       new[] { "Nao", "Todas", "Escolher" }, "Nao");
                if (r is null) return;
                modoIlha = r;
            }

            string prefixo = PrefixoIntersecao(s.Tr, civ);
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);

            // esquinas: curva de raio R tangente ao bordo anti-horário do braço i e ao bordo horário do braço seguinte
            var esquinas = new List<(int I, int J, Conectada? C, double Alfa)>();
            var raios = new List<double>();
            var ilhas = new List<IlhaCanalizadora?>();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                if (n == 2 && i == 1) break;
                var bi = bracos[i];
                var bj = bracos[j];
                double alfa = GeometriaIntersecao.AnguloAntiHorario(bi.Angulo, bj.Angulo);
                if (n == 2 && alfa > Math.PI) { (bi, bj) = (bj, bi); (i, j) = (1, 0); alfa = 2 * Math.PI - alfa; }
                if (bi.Eixo.ObjectId == bj.Eixo.ObjectId || alfa >= 175 * Math.PI / 180 || alfa < 10 * Math.PI / 180) { if (n == 2) break; continue; }
                double wi = W0(i), wj = W0(j);
                bool comIlha = modoIlha == "Todas";
                if (modoIlha == "Escolher")
                {
                    var r = s.PedirPalavra($"Ilha canalizadora na esquina {bi.Rumo}-{bj.Rumo}", new[] { "Sim", "Nao" }, "Sim");
                    if (r is null) return;
                    comIlha = r == "Sim";
                }
                double raio = comIlha ? cfg.CanalRaio : cfg.RaioEsquina;
                if (GeometriaIntersecao.CentroEsquina(bi.Angulo, bj.Angulo, wi, wj, raio) is not { } cc) continue;
                var centro = new Point3d(o.X + cc.X, o.Y + cc.Y, 0);
                var pA = RotatoriaCivil.Ponto(bi.Eixo, RotatoriaCivil.EstacaPerto(bi.Eixo, centro), 0);
                var pB = RotatoriaCivil.Ponto(bj.Eixo, RotatoriaCivil.EstacaPerto(bj.Eixo, centro), 0);
                var feito = Conectar(s, cfg, bi.Eixo.ObjectId, pA, bj.Eixo.ObjectId, pB, raio, $"{prefixo} - esquina {bi.Rumo}-{bj.Rumo}", wi, wj);
                if (feito is null) avisos.Add($"Esquina {bi.Rumo}-{bj.Rumo}: o raio de {raio:0.##} m não coube entre '{bi.Eixo.Name}' e '{bj.Eixo.Name}'.");
                esquinas.Add((i, j, feito, alfa));
                raios.Add(raio);
                ilhas.Add(comIlha && feito is not null && GeometriaIntersecao.Ilha(bi.Angulo, bj.Angulo, wi, wj, raio, cfg.CanalLargura, cfg.CanalRecuo) is { } il
                    ? il with { Entre = $"{bi.Rumo}-{bj.Rumo}" } : null);
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

            // alargamento da principal: largura plena até o fim do armazenamento e da desaceleração (aproximação com conversão) ou
            // até a esquina (o braço do outro lado do T: transição de saída), depois o teiper
            var dPlena = new double[n];
            double dRetencao = (secundariosIdx.Count > 0 ? secundariosIdx.Max(k => w[bracos[k].Eixo.ObjectId]) : 0) + RetencaoConversao;
            if (conversao)
                foreach (int i in principaisIdx)
                    dPlena[i] = temConversao[i] ? Math.Max(tang[i], dRetencao + cfg.ConvArmazenamento + convDes) : tang[i];
            bool alargado(int i) => conversao && bracos[i].Eixo.ObjectId == principal.ObjectId;
            // até onde vão os bordos e as regiões da interseção em cada braço (distância do centro)
            var alcance = new double[n];
            for (int i = 0; i < n; i++) alcance[i] = Math.Max(tang[i] + TrechoForaIntersecao, alargado(i) ? dPlena[i] + convTeiper + 10 : 0);

            // bordos de cada braço (do centro até 30 m depois da esquina ou do teiper): desenho e alvo de largura
            var layB = Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecaoBordo, 30);
            var bordos = new List<ObjectId>[n];
            var sCentro = new double[n];
            for (int i = 0; i < n; i++)
            {
                var b = bracos[i];
                bordos[i] = new List<ObjectId>();
                sCentro[i] = RotatoriaCivil.EstacaPerto(b.Eixo, o);
                double sFim = EstacaNaDistancia(b, sCentro[i], o, alcance[i]);
                double wi = w[b.Eixo.ObjectId];
                foreach (int lado in new[] { +1, -1 })
                {
                    var pts = new List<(double X, double Y)>();
                    int m = Math.Max(2, (int)Math.Ceiling(Math.Abs(sFim - sCentro[i])));
                    for (int k = 0; k <= m; k++)
                    {
                        double st = sCentro[i] + (sFim - sCentro[i]) * k / m;
                        double wk = alargado(i) ? GeometriaIntersecao.MeiaLarguraAlargada(wi, alarg, dPlena[i], convTeiper, Math.Abs(st - sCentro[i])) : wi;
                        var p = RotatoriaCivil.Ponto(b.Eixo, st, lado * wk);
                        pts.Add((p.X, p.Y));
                    }
                    var id = Desenhar(s, ms, pts, false, layB, 256);
                    if (!id.IsNull) bordos[i].Add(id);
                }
            }

            // faixa de conversão: contorno (centrada no eixo, da retenção ao fim do teiper) e o resumo de cada braço alargado
            var conversoes = new List<ConversaoIntersecao>();
            if (conversao)
            {
                var layC = Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecaoConv, 4);
                foreach (int i in principaisIdx)
                {
                    var b = bracos[i];
                    double lim = b.SentidoFora > 0 ? b.Eixo.EndingStation : b.Eixo.StartingStation;
                    double St(double d) => b.SentidoFora > 0 ? Math.Min(lim, sCentro[i] + d) : Math.Max(lim, sCentro[i] - d);
                    double dFim = dPlena[i] + convTeiper;
                    double falta = dFim - Math.Abs(lim - sCentro[i]);
                    if (falta > 0.01)
                        avisos.Add($"Faixa de conversão: '{b.Eixo.Name}' acaba {falta:0.0} m antes do fim do teiper do braço {b.Rumo}; o alargamento ficou cortado na ponta do eixo.");
                    if (!temConversao[i])
                    {
                        conversoes.Add(new ConversaoIntersecao(b.Rumo, b.Eixo.Name, "transição de saída (sem conversão à esquerda neste braço)", alarg, 0, 0, convTeiper,
                            dPlena[i], RotatoriaCivil.Est(sCentro[i]), RotatoriaCivil.Est(St(dFim))));
                        continue;
                    }
                    var cont = new List<(double X, double Y)>();
                    int m = Math.Max(4, (int)Math.Ceiling((dFim - dRetencao) / 2));
                    for (int k = 0; k <= m; k++)
                    {
                        double d = dRetencao + (dFim - dRetencao) * k / m;
                        var p = RotatoriaCivil.Ponto(b.Eixo, St(d), GeometriaIntersecao.MeiaLarguraAlargada(0, alarg, dPlena[i], convTeiper, d));
                        cont.Add((p.X, p.Y));
                    }
                    for (int k = m; k >= 0; k--)
                    {
                        double d = dRetencao + (dFim - dRetencao) * k / m;
                        var p = RotatoriaCivil.Ponto(b.Eixo, St(d), -GeometriaIntersecao.MeiaLarguraAlargada(0, alarg, dPlena[i], convTeiper, d));
                        cont.Add((p.X, p.Y));
                    }
                    Desenhar(s, ms, cont, true, layC, 4);
                    double dMeio = (dRetencao + dPlena[i]) / 2;
                    var (tx0, ty0) = RotatoriaCivil.Direcao(b.Eixo, St(dMeio));
                    double giroTx = Math.Atan2(ty0, tx0);
                    if (giroTx > Math.PI / 2) giroTx -= Math.PI; else if (giroTx < -Math.PI / 2) giroTx += Math.PI;
                    var tc = Ctx.Texto($"conversão à esquerda: {cfg.ConvArmazenamento:0} + {convDes:0} m, teiper {convTeiper:0} m",
                                       RotatoriaCivil.Ponto(b.Eixo, St(dMeio), 0), 0.6, giroTx, AttachmentPoint.MiddleCenter);
                    tc.Layer = layC;
                    Ctx.Adicionar(s.Tr, ms, tc);
                    conversoes.Add(new ConversaoIntersecao(b.Rumo, b.Eixo.Name, "faixa de conversão à esquerda (aproximação)", alarg, cfg.ConvArmazenamento, convDes, convTeiper,
                        dPlena[i], RotatoriaCivil.Est(St(dRetencao)), RotatoriaCivil.Est(St(dFim))));
                }
            }

            // ilhas canalizadoras: contorno da ilha e bordo da pista de conversão do lado da ilha
            var ilhaIds = new List<ObjectId>();
            var layI = ilhas.Any(x => x is not null) ? Ctx.GarantirLayer(s.Tr, s.Db, LayerIntersecaoIlha, 3) : "";
            foreach (var ilha in ilhas)
            {
                var idIlha = ObjectId.Null;
                if (ilha is not null)
                {
                    if (ilha.BordoGiro.Count > 1) Desenhar(s, ms, ilha.BordoGiro.Select(p => (o.X + p.X, o.Y + p.Y)).ToList(), false, layI, 3);
                    if (ilha.Cabe)
                    {
                        idIlha = Desenhar(s, ms, ilha.Contorno.Select(p => (o.X + p.X, o.Y + p.Y)).ToList(), true, layI, 3);
                        double gx = ilha.Contorno.Average(p => p.X), gy = ilha.Contorno.Average(p => p.Y);
                        var ti = Ctx.Texto($"ilha {ilha.Area:0.0} m²", new Point3d(o.X + gx, o.Y + gy, 0), 0.5, 0, AttachmentPoint.MiddleCenter);
                        ti.Layer = layI;
                        Ctx.Adicionar(s.Tr, ms, ti);
                    }
                }
                ilhaIds.Add(idIlha);
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
                double s0 = EstacaNaDistancia(b, sCentro[i], o, wP + alarg / 2 + GeometriaIntersecao.RecuoMotorista);
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
            foreach (var il in ilhas)
            {
                if (il is null) continue;
                string onde = $"esquina {il.Entre}";
                if (!il.Cabe)
                {
                    verif.Add(new Verificacao("Ilha canalizadora", onde, $"não cabe: pista de conversão de {il.LarguraGiro:0.00} m com bordo interno de raio {il.RaioGiro:0.##} m encosta nos bordos das vias",
                        "a ilha cabe entre a pista de conversão e os bordos das vias", "geometria: aumente o raio ou diminua a largura da pista de conversão (Parametros)", VerificacaoRotatoria.Reprovado));
                    continue;
                }
                verif.Add(new Verificacao("Ilha canalizadora - área", onde, $"{il.Area:0.00} m²", $"≥ {cfg.CanalAreaMin:0.0} m²",
                    "AASHTO Green Book (50 ft² urbano, 75 ft² rural, 100 ft² preferível) — referência complementar [A CONFIRMAR]",
                    il.Area >= cfg.CanalAreaMin - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                double ladoMin = Math.Min(Math.Min(il.LadoA, il.LadoB), il.LadoArco);
                verif.Add(new Verificacao("Ilha canalizadora - lados", onde, $"{il.LadoA:0.00} / {il.LadoB:0.00} / {il.LadoArco:0.00} m (vértices sem arredondar)", $"≥ {cfg.CanalLadoMin:0.0} m",
                    "AASHTO Green Book (12 ft depois de arredondar os vértices com 0,3 a 0,6 m) — referência complementar [A CONFIRMAR]",
                    ladoMin >= cfg.CanalLadoMin - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                verif.Add(new Verificacao("Pista de conversão à direita", onde, $"largura {il.LarguraGiro:0.00} m, bordo interno R {il.RaioGiro:0.##} m, ilha afastada {il.Recuo:0.00} m dos bordos",
                    "giro do veículo de projeto", "DNIT (2024), Manual de Projeto de Acessos, Tabela 10 (90°: pista de 4,2 / 5,4 / 6,0 m nas classes A / B / C) e p. 41 (afastamento de 0,60 m); conferir com o PROJETA Giro [A CONFIRMAR]",
                    VerificacaoRotatoria.Informativo));
            }
            string fonteConv = (cfg.ConvDesaceleracao > 0 || cfg.ConvTeiper > 0 ? "desaceleração / teiper informados pelo usuário" : $"DNIT (2024), Manual de Projeto de Acessos, Tabela 6 (V {vTabela} km/h, desaceleração até a parada; usada por analogia na conversão à esquerda) [A CONFIRMAR]")
                + $"; largura {(cfg.IntersecaoExtrasDoUsuario ? "informada pelo usuário" : "DNIT (2024), p. 47 (faixa de giro de 3,60 m)")}; armazenamento {(cfg.IntersecaoExtrasDoUsuario ? "informado pelo usuário" : "[PREMISSA] 2 veículos de 7,5 m, sem estudo de tráfego")}";
            foreach (var cv in conversoes)
                verif.Add(new Verificacao("Faixa de conversão à esquerda", $"{cv.Braco} ({cv.Eixo})",
                    cv.Armazenamento > 0 ? $"largura {cv.Largura:0.00} m; armazenamento {cv.Armazenamento:0.0} + desaceleração {cv.Desaceleracao:0.0} m; teiper {cv.Teiper:0.0} m (est. {cv.Inicio} a {cv.Fim})"
                                         : $"transição de saída: largura plena até {cv.DistanciaPlena:0.0} m do centro, teiper {cv.Teiper:0.0} m (até a est. {cv.Fim})",
                    "comprimentos da tabela e armazenamento pela fila do estudo de tráfego", fonteConv, VerificacaoRotatoria.Informativo));
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
                var defs = MontagensRotatoria.DefinirIntersecao(cfg, distintos.Select(a => (a.Name, w[a.ObjectId])), canalizada: ilhaIds.Any(id => !id.IsNull),
                    continuo: ladoContinuo is null ? null : (principal.Name, wP, ladoContinuo));
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
                trechos = TrechosIntersecao(s, o, bracos, sCentro, tang, wP + alarg / 2, principal, esquinas, ladoEsquina, bordos, alcance, conversao, ilhas, ilhaIds, ladoContinuo);
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

            // geometria guardada no desenho para a sinalização (PRJSINALIZA)
            try
            {
                var modSinal = new ModeloIntersecaoSinal { Ox = o.X, Oy = o.Y };
                for (int iSin = 0; iSin < n; iSin++)
                {
                    var bSin = bracos[iSin];
                    double limSin = bSin.SentidoFora > 0 ? bSin.Eixo.EndingStation : bSin.Eixo.StartingStation;
                    double compSin = Math.Min(Math.Abs(limSin - sCentro[iSin]), alcance[iSin] + 60);
                    if (compSin < 1) continue;
                    var ptsSin = new List<double[]>();
                    int nSin = Math.Max(2, (int)Math.Ceiling(compSin));
                    for (int kSin = 0; kSin <= nSin; kSin++)
                    {
                        var qSin = RotatoriaCivil.Ponto(bSin.Eixo, sCentro[iSin] + bSin.SentidoFora * compSin * kSin / nSin, 0);
                        ptsSin.Add(new[] { qSin.X, qSin.Y });
                    }
                    modSinal.Bracos.Add(new BracoIntersecaoSinal
                    {
                        Nome = bSin.Eixo.Name, Rumo = bSin.Rumo, Principal = bSin.Eixo.ObjectId == principal.ObjectId, Eixo = ptsSin,
                        MeiaLargura = w[bSin.Eixo.ObjectId], Alargamento = alargado(iSin) ? alarg : 0, DPlena = dPlena[iSin], Teiper = convTeiper,
                        Conversao = conversao && temConversao[iSin], DRetencaoConversao = dRetencao,
                    });
                }
                ArmazenamentoSinal.Gravar(s.Tr, s.Db, new ItemSinalizacao { Nome = prefixo, Tipo = "Interseção", Intersecao = modSinal });
            }
            catch (System.Exception ex) { avisos.Add($"Sinalização: a geometria não foi guardada no desenho ({ex.Message}); o PRJSINALIZA não vai achar esta interseção."); }            s.Tr.Commit();

            // mensagens
            string premissa = cfg.IntersecaoParametrosDoUsuario ? "" : " [PREMISSA: valores de partida, troque em Parametros]";
            Ctx.Msg($"{prefixo} criada no centro ({o.X:0.000}; {o.Y:0.000}) — {origemCentro}: {n} braços, via principal '{principal.Name}', raio de esquina {cfg.RaioEsquina:0.##} m" + (ilhas.Any(x => x is not null) ? $" ({cfg.CanalRaio:0.##} m nas esquinas canalizadas)" : "") + $", V da principal {cfg.VelPrincipal:0} km/h{premissa}.");
            foreach (var cv in conversoes)
                Ctx.Msg(cv.Armazenamento > 0
                    ? $"  conversão à esquerda no braço {cv.Braco}: faixa de {cv.Largura:0.00} m, armazenamento {cv.Armazenamento:0} m + desaceleração {cv.Desaceleracao:0} m + teiper {cv.Teiper:0} m (est. {cv.Inicio} a {cv.Fim})"
                    : $"  braço {cv.Braco}: só a transição de saída do alargamento (teiper {cv.Teiper:0} m até a est. {cv.Fim})");
            foreach (var il in ilhas.Where(x => x is not null))
                Ctx.Msg(il!.Cabe ? $"  ilha canalizadora {il.Entre}: {il.Area:0.0} m², lados {il.LadoA:0.0} / {il.LadoB:0.0} / {il.LadoArco:0.0} m, pista de conversão {il.LarguraGiro:0.0} m com bordo interno R {il.RaioGiro:0.##} m"
                                 : $"  ilha canalizadora {il.Entre}: NÃO coube (aumente o raio ou diminua a largura da pista de conversão)");
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
            Ctx.Msg($"Desenho nas camadas {LayerIntersecaoBordo} (bordos), {LayerIntersecaoVisib} (visadas)" + (conversoes.Count > 0 ? $", {LayerIntersecaoConv} (faixa de conversão)" : "")
                    + (ilhas.Any(x => x is not null) ? $", {LayerIntersecaoIlha} (ilhas)" : "") + ". Confira com PRJCASAMENTO › Varios. U desfaz a interseção inteira.");
            Ctx.Msg("Sinalização horizontal e vertical desta interseção: PRJSINALIZA.");

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
                rel.Esquinas.Add(new EsquinaIntersecao(c?.Nome ?? $"{prefixo} - esquina {bracos[i].Rumo}-{bracos[j].Rumo}", $"{bracos[i].Rumo}-{bracos[j].Rumo}" + (ilhas[e] is not null ? " (canalizada)" : ""),
                    raios[e], c?.C.Desenvolvimento, alfa * 180 / Math.PI, lado, c?.ComGreide ?? false, c is not null));
            }
            rel.Visadas.AddRange(visadas);
            rel.Conversoes.AddRange(conversoes);
            rel.Ilhas.AddRange(ilhas.Where(x => x is not null)!);
            rel.Parametros.AddRange(ParametrosExtras(cfg, conversoes.Count > 0, convDes, convTeiper, vTabela, ilhas.Any(x => x is not null), dRetencao));
            rel.Verificacoes.AddRange(verif);
            rel.Corredor.AddRange(trechos.Select(t => new RegiaoCorredor(t.Eixo, t.De, t.Ate, $"{prefixo} - {t.Montagem}", t.Obs)));
            rel.Montagens.AddRange(montagens.Select(m => $"{m.Nome}: {m.Uso}"));
            rel.Avisos.AddRange(avisos);
            rel.Pendencias.AddRange(PendenciasIntersecao(conversoes.Count > 0, ilhas.Any(x => x is not null)));
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
    /// centro da curva e a faixa até o bordo mais perto das vias. Com faixa de conversão, as faixas da principal vão até os
    /// bordos alargados; com ilha canalizadora, a esquina tem, no trecho da ilha, a pista de conversão até o contorno da ilha.
    /// </summary>
    private static List<TrechoCorredor> TrechosIntersecao(Sessao s, Point3d o, List<Braco> bracos, double[] sCentro, double[] tang, double wP,
        Alignment principal, List<(int I, int J, Conectada? C, double Alfa)> esquinas, List<string> ladoEsquina, List<ObjectId>[] bordos,
        double[] alcance, bool conversao, List<IlhaCanalizadora?> ilhas, List<ObjectId> ilhaIds, string? ladoContinuo = null)
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
            string montagem = ladoContinuo is null ? $"Pista só faixas - {principal.Name}" : $"Pista só faixas com meio-fio à {ladoContinuo} - {principal.Name}";
            string obs = "via principal dentro da interseção (o meio-fio ali é o das esquinas" + (ladoContinuo is null ? ")" : $"; à {ladoContinuo}, o lado sem via transversal, meio-fio contínuo)");
            if (conversao)
                // com a faixa de conversão, uma região por braço, cada uma com os bordos alargados do seu braço (com os quatro
                // bordos numa região só, o Civil perde o passeio depois do centro)
                foreach (int i in principais)
                    Add(principal, sCentro[i], EstacaNaDistancia(bracos[i], sCentro[i], o, tang[i]), montagem, obs + $"; braço {bracos[i].Rumo}: faixas até os bordos alargados", bordos[i]);
            else
            {
                var ests = principais.Select(i => EstacaNaDistancia(bracos[i], sCentro[i], o, tang[i])).ToList();
                if (principais.Count == 1) ests.Add(sCentro[principais[0]]);
                Add(principal, ests.Min(), ests.Max(), montagem, obs);
            }
        }
        for (int i = 0; i < n; i++)
        {
            var b = bracos[i];
            double sT = EstacaNaDistancia(b, sCentro[i], o, tang[i]);
            if (b.Eixo.ObjectId != principal.ObjectId)
                Add(b.Eixo, EstacaNaDistancia(b, sCentro[i], o, wP), sT, $"Pista só faixas - {b.Eixo.Name}", $"braço {b.Rumo}: do bordo da via principal às esquinas");
            bool alargado = conversao && b.Eixo.ObjectId == principal.ObjectId;
            Add(b.Eixo, sT, EstacaNaDistancia(b, sCentro[i], o, alcance[i]), $"Pista completa - {b.Eixo.Name}",
                $"braço {b.Rumo}: a partir das esquinas (depois segue o corredor da via)" + (alargado ? "; faixas até os bordos alargados (conversão e teiper)" : ""),
                alargado ? bordos[i] : null);
        }
        for (int e = 0; e < esquinas.Count; e++)
        {
            var (i, j, c, _) = esquinas[e];
            if (c is null || s.Tr.GetObject(c.Id, OpenMode.ForRead) is not Alignment ca) continue;
            string lado = ladoEsquina[e];
            string nome = $"esquina {bracos[i].Rumo}-{bracos[j].Rumo}";
            if (e < ilhas.Count && ilhas[e] is { Cabe: true } il && !ilhaIds[e].IsNull)
            {
                // trecho da ilha: projeção das pontas da ilha no eixo da esquina (radial: o eixo é um arco concêntrico)
                double s1 = RotatoriaCivil.EstacaPerto(ca, new Point3d(o.X + il.PontaA.X, o.Y + il.PontaA.Y, 0));
                double s2 = RotatoriaCivil.EstacaPerto(ca, new Point3d(o.X + il.PontaB.X, o.Y + il.PontaB.Y, 0));
                if (s1 > s2) (s1, s2) = (s2, s1);
                Add(ca, ca.StartingStation, s1, $"Esquina - passeio à {lado}", $"{nome}: antes da ilha canalizadora; faixa até o bordo mais perto das vias", bordos[i].Concat(bordos[j]), maisPerto: true);
                Add(ca, s1, s2, $"Esquina canalizada - passeio à {lado}", $"{nome}: trecho da ilha canalizadora; pista de conversão até o contorno da ilha e meio-fio da ilha (o topo da ilha fica fora do corredor)",
                    new[] { ilhaIds[e] }.Concat(bordos[i]).Concat(bordos[j]), maisPerto: true);
                Add(ca, s2, ca.EndingStation, $"Esquina - passeio à {lado}", $"{nome}: depois da ilha canalizadora; faixa até o bordo mais perto das vias", bordos[i].Concat(bordos[j]), maisPerto: true);
                continue;
            }
            Add(ca, ca.StartingStation, ca.EndingStation, $"Esquina - passeio à {lado}", $"{nome}: meio-fio, passeio e talude à {lado}; faixa até o bordo mais perto das vias",
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
        $"; conversão {c.ConvLargura:0.00} m, armazenamento {c.ConvArmazenamento:0} m" + (c.ConvDesaceleracao > 0 ? $", desaceleração {c.ConvDesaceleracao:0} m" : "") + (c.ConvTeiper > 0 ? $", teiper {c.ConvTeiper:0} m" : "") +
        $"; ilha: bordo interno R {c.CanalRaio:0.##} m, pista {c.CanalLargura:0.##} m" +
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
        var pk = new PromptKeywordOptions("\nAjustar a faixa de conversão e a ilha canalizadora") { AllowNone = true };
        pk.Keywords.Add("Sim");
        pk.Keywords.Add("Nao");
        pk.Keywords.Default = "Nao";
        var rk = ed.GetKeywords(pk);
        if (rk.Status == PromptStatus.Cancel) return false;
        if (rk.Status == PromptStatus.OK && rk.StringResult == "Sim")
        {
            if (N("Largura da faixa de conversão à esquerda (m)", cfg.ConvLargura) is not { } cl) return false;
            if (N("Armazenamento da faixa de conversão: veículos na fila × 7,5 m (m)", cfg.ConvArmazenamento, zero: true) is not { } carm) return false;
            if (N("Desaceleração (m; 0 = Tabela 6 do DNIT pela V da principal)", cfg.ConvDesaceleracao, zero: true) is not { } cd) return false;
            if (N("Teiper (m; 0 = Tabela 6 do DNIT pela V da principal)", cfg.ConvTeiper, zero: true) is not { } ct) return false;
            if (N("Ilha canalizadora: raio do bordo interno da pista de conversão (m)", cfg.CanalRaio) is not { } cr) return false;
            if (N("Largura da pista de conversão à direita (m)", cfg.CanalLargura) is not { } cg) return false;
            if (N("Afastamento da ilha aos bordos das vias (m)", cfg.CanalRecuo, zero: true) is not { } ce) return false;
            if (N("Área mínima da ilha (m²)", cfg.CanalAreaMin, zero: true) is not { } ca2) return false;
            if (N("Lado mínimo da ilha (m)", cfg.CanalLadoMin, zero: true) is not { } cm) return false;
            cfg.ConvLargura = cl; cfg.ConvArmazenamento = carm; cfg.ConvDesaceleracao = cd; cfg.ConvTeiper = ct;
            cfg.CanalRaio = cr; cfg.CanalLargura = cg; cfg.CanalRecuo = ce; cfg.CanalAreaMin = ca2; cfg.CanalLadoMin = cm;
            cfg.IntersecaoExtrasDoUsuario = true;
        }
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

    /// <summary>Parâmetros da faixa de conversão e da ilha canalizadora (só os que foram usados).</summary>
    private static IEnumerable<ParametroRotatoria> ParametrosExtras(ConfigRotatoria c, bool conversao, double desac, double teiper, int vTabela, bool ilha, double dRetencao)
    {
        string U(string fonte) => c.IntersecaoExtrasDoUsuario ? "informado pelo usuário" : fonte;
        if (conversao)
        {
            yield return new("Faixa de conversão: largura", c.ConvLargura.ToString("0.00"), "m", U("DNIT (2024), Manual de Projeto de Acessos de Áreas Lindeiras, p. 47 (faixa de giro de 3,60 m; 3,00 m em locais restritos)"));
            yield return new("Faixa de conversão: armazenamento", c.ConvArmazenamento.ToString("0.0"), "m", U("[PREMISSA] 2 veículos de 7,5 m; sem estudo de tráfego — com estudo, fila × 7,5 m"));
            string t6 = $"DNIT (2024), Manual de Projeto de Acessos, Tabela 6 (V {vTabela} km/h, saída a 0 km/h), por analogia na conversão à esquerda [A CONFIRMAR]";
            yield return new("Faixa de conversão: desaceleração", desac.ToString("0.0"), "m", c.ConvDesaceleracao > 0 ? "informado pelo usuário" : t6);
            yield return new("Faixa de conversão: teiper", teiper.ToString("0.0"), "m", c.ConvTeiper > 0 ? "informado pelo usuário" : t6);
            yield return new("Linha de retenção da conversão", dRetencao.ToString("0.00"), "m do centro", $"[PREMISSA] {RetencaoConversao:0.0} m antes do bordo da via transversal");
        }
        if (ilha)
        {
            yield return new("Ilha canalizadora: raio do bordo interno da pista de conversão", c.CanalRaio.ToString("0.00"), "m", U("[PREMISSA] curva simples; conferir com a Tabela 9 / 10 do Manual de Acessos (DNIT, 2024) e o giro do veículo"));
            yield return new("Ilha canalizadora: largura da pista de conversão", c.CanalLargura.ToString("0.00"), "m", U("DNIT (2024), Manual de Acessos, Tabela 10 (90°, classe B: 5,4 m)"));
            yield return new("Ilha canalizadora: afastamento aos bordos das vias", c.CanalRecuo.ToString("0.00"), "m", U("DNIT (2024), Manual de Acessos, p. 41 (afastamento mínimo de 0,60 m)"));
            yield return new("Ilha canalizadora: área mínima", c.CanalAreaMin.ToString("0.0"), "m²", U("AASHTO Green Book (50 ft² urbano; 75 ft² rural) — referência complementar [A CONFIRMAR]"));
            yield return new("Ilha canalizadora: lado mínimo", c.CanalLadoMin.ToString("0.0"), "m", U("AASHTO Green Book (12 ft depois de arredondar) — referência complementar [A CONFIRMAR]"));
        }
    }

    private static IEnumerable<string> PendenciasIntersecao(bool conversao = false, bool ilha = false) => new[]
    {
        conversao ? "Faixa de conversão: armazenamento pela fila do estudo de tráfego (veículos × 7,5 m), sinalização (setas, linhas de canalização e de retenção) e confirmação dos comprimentos na norma do órgão." : "",
        ilha ? "Ilhas canalizadoras: arredondar os vértices (0,3 a 0,6 m) e conferir de novo os lados; modelar o topo das ilhas (fica fora do corredor); giro do veículo na pista de conversão." : "",
        "Giro do veículo de projeto em todas as conversões com o PROJETA Giro (GIROSEGUIR / GIROVERIFICAR): largura varrida e folga ao meio-fio das esquinas.",
        "Visibilidade: distância de visibilidade de parada nas aproximações e conferência dos triângulos de visibilidade (desenhados) contra os obstáculos reais (muros, vegetação, postes, placas).",
        "Confirmar os parâmetros na norma do órgão (DNIT — Manual de Projeto de Interseções, 2005; IPR 740 em travessia urbana): os critérios usados aqui são referências complementares (AASHTO).",
        "Sinalização horizontal e vertical (PARE / dê a preferência, faixas de pedestres, linhas de retenção) conforme o CONTRAN" + (conversao || ilha ? "." : "; ilha canalizadora e faixa de conversão (Sim no comando), se o tráfego pedir."),
        "Drenagem das esquinas (sarjetas e bocas de lobo nos pontos baixos) com o PRJREDE.",
        "Interferências (postes, árvores, acessos de lote, redes, faixa de domínio) e desapropriação, se houver.",
    }.Where(p => p.Length > 0);
}
