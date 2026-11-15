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
/// PRJCAPACIDADE — capacidade e fila de uma rotatória (PRJROTATORIA) ou de uma interseção com PARE (PRJINTERSECAO), pelo HCM 7ª ed.:
/// volumes por movimento (guardados no desenho), capacidade, grau de saturação, atraso, nível de serviço e fila de 95 % de cada
/// entrada / movimento; faixas recomendadas na rotatória e armazenamento recomendado na faixa de conversão. Desenha a fila de 95 %
/// em cada aproximação (cor pelo nível de serviço) com o rótulo, e grava o Excel.
/// </summary>
public sealed class CmdCapacidade
{
    internal const string Layer = "PRJ-CAPACIDADE";

    [CommandMethod("PRJCAPACIDADE", CommandFlags.Modal)]
    public static void Capacidade()
    {
        try
        {
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Capacidade ?? new ParametrosCapacidade();
            var ps = cfg.Sinal ?? new ParametrosSinalizacao();
            using var s = Sessao.Abrir();
            var itens = ArmazenamentoSinal.Ler(s.Tr, s.Db);
            if (itens.Count == 0)
            {
                Ctx.Msg("Este desenho não tem interseção nem rotatória do PROJETA guardada. Monte com o PRJINTERSECAO ou o PRJROTATORIA (desta versão em diante eles guardam a geometria) e rode o PRJCAPACIDADE de novo.");
                return;
            }
            while (true)
            {
                Ctx.Msg(Resumo(p));
                var r = s.PedirPalavra("Capacidade e fila", new[] { "Calcular", "Parametros" }, "Calcular");
                if (r is null) return;
                if (r == "Parametros") { if (!PedirParametros(s, p)) return; cfg.Capacidade = p; ConfigRotatoria.Salvar(cfg); continue; }
                break;
            }
            int k = itens.Count == 1 ? 0 : s.EscolherDaLista("Interseção ou rotatória", itens.Select(x => $"{x.Nome} ({x.Tipo})").ToList(), 0);
            if (k < 0) return;
            var it = itens[k];
            bool rotatoria = it.Rotatoria is not null;
            if (!rotatoria && it.Intersecao is null) { Ctx.Msg($"'{it.Nome}': sem geometria guardada."); return; }
            if (it.Semaforo is not null) Ctx.Msg($"'{it.Nome}' tem semáforo: o desempenho com semáforo está no PRJSEMAFORO; aqui a análise é com PARE nas secundárias.");

            // volumes por movimento
            var rumos = rotatoria ? it.Rotatoria!.Bracos.Select(b => b.Rumo).ToList() : it.Intersecao!.Bracos.Select(b => b.Rumo).ToList();
            var nomes = rotatoria ? it.Rotatoria!.Bracos.Select(b => b.Nome).ToList() : it.Intersecao!.Bracos.Select(b => b.Nome).ToList();
            var eixos = rotatoria ? it.Rotatoria!.Modelo().Bracos.Select(b => b.Eixo).ToList() : it.Intersecao!.Bracos.Select(b => b.Polilinha()).ToList();
            int n = rumos.Count;
            var dados = it.Capacidade ?? new DadosCapacidade();
            var vols = new Dictionary<(int O, int D), double>();
            var tabela = new List<(string, string, string, double)>();
            Ctx.Msg($"{it.Nome}: informe a contagem da hora de pico por movimento, em veíc/h (sem contagem, os valores sugeridos são só ponto de partida [PREMISSA]).");
            var ordem = rotatoria ? CalculoCapacidade.OrdemAnel(it.Rotatoria!.Bracos.Select(b => b.Angulo).ToList()) : Enumerable.Range(0, n).ToArray();
            var posAnel = new int[n];
            for (int q = 0; q < n; q++) posAnel[ordem[q]] = q;
            for (int o = 0; o < n; o++)
                for (int j = 1; j < n; j++)
                {
                    int d = rotatoria ? ordem[(posAnel[o] + j) % n] : j <= o ? j - 1 : j;
                    if (!rotatoria && d == o) continue;
                    string tipo = rotatoria ? $"{j}ª saída" : CalculoCapacidade.TipoMovimento(eixos[o], eixos[d]);
                    string chave = DadosCapacidade.Chave(rumos[o], rumos[d]);
                    double padrao = dados.Movimentos.TryGetValue(chave, out var vv) ? vv
                                  : rotatoria ? 100 : (it.Intersecao!.Bracos[o].Principal && tipo == "Frente" ? 400 : 50);
                    var v = s.PedirNumero($"Do braço {rumos[o]} ('{nomes[o]}') para o {rumos[d]} ({tipo.ToLowerInvariant()}), veíc/h", padrao);
                    if (v is null) return;
                    vols[(o, d)] = v.Value;
                    dados.Movimentos[chave] = v.Value;
                    tabela.Add((rumos[o], rumos[d], tipo, v.Value));
                }

            AnaliseCapacidade a;
            int anelAtual = 1, entradaAtual = 1;
            if (rotatoria)
            {
                var rm = it.Rotatoria!;
                anelAtual = rm.LarguraAnel >= 8.5 ? 2 : 1;
                var b0 = rm.Bracos.FirstOrDefault();
                entradaAtual = b0 is not null && b0.BordoEntrada - b0.IlhaLarguraIni / 2 >= 7.0 ? 2 : 1;
                a = CalculoCapacidade.Rotatoria(it.Nome, rm, vols, p, anelAtual, entradaAtual);
                a.Avisos.Add($"Geometria considerada: {anelAtual} faixa(s) no anel (largura {rm.LarguraAnel:0.0} m) e {entradaAtual} por entrada [PREMISSA: 2 faixas a partir de 8,5 m no anel e 7,0 m na entrada].");
            }
            else a = CalculoCapacidade.Pare(it.Nome, it.Intersecao!, vols, p, cfg.ConvArmazenamento);

            // desenho: fila de 95 % em cada aproximação
            var antigos = new List<ObjectId>();
            foreach (var h in dados.Desenhados)
                if (long.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out long hv) && s.Db.TryGetObjectId(new Handle(hv), out var oid) && !oid.IsErased)
                    antigos.Add(oid);
            if (antigos.Count > 0)
            {
                var resp = s.PedirPalavra($"'{it.Nome}' já tem filas desenhadas ({antigos.Count} objetos). Substituir", new[] { "Sim", "Nao" }, "Sim");
                if (resp != "Sim") return;
                foreach (var oid in antigos) s.Tr.GetObject(oid, OpenMode.ForWrite).Erase();
            }
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            var lay = Ctx.GarantirLayer(s.Tr, s.Db, Layer, 3);
            var ids = new List<ObjectId>();
            foreach (var ap in a.Aproximacoes)
            {
                var e = eixos[ap.Braco];
                double s0, sRot;
                Func<double, double> v, vRotulo;
                if (rotatoria)
                {
                    var b = it.Rotatoria!.Modelo().Bracos[ap.Braco];
                    double rr = it.Rotatoria!.RExt + ParametrosSinalizacao.DistLdp + ps.LarguraLdp;
                    v = st => (b.MeiaIlha(st) + GeradorSinalizacao.BordaRamo(b, st, true)) / 2;
                    vRotulo = st => GeradorSinalizacao.BordaRamo(b, st, true) + 2.0;
                    double ox = it.Rotatoria.Ox, oy = it.Rotatoria.Oy;
                    s0 = GeoSinal.Bissecao(st => { var q = e.Ponto(st, v(st)); return Math.Sqrt((q.X - ox) * (q.X - ox) + (q.Y - oy) * (q.Y - oy)) - rr; }, 0, 150);
                    if (double.IsNaN(s0)) continue;
                    sRot = s0 + 2;
                }
                else
                {
                    var m = it.Intersecao!;
                    var b = m.Bracos[ap.Braco];
                    var pos = GeradorSinalizacao.Posicoes(m, m.Bracos.Select(x => x.Polilinha()).ToList(), ap.Braco, ps, false);
                    bool conv = ap.Movimento.StartsWith("conversão");
                    s0 = pos.Retencao + (conv ? 0 : ps.LarguraLre);
                    double w = b.MeiaLargura;
                    v = conv && b.Conversao ? _ => 0.0 : _ => w / 2;
                    vRotulo = st => b.MeiaLarguraEm(st) + 2.0;
                    // depois do fim da curva da esquina do lado da aproximação (o centro do meio-fio projetado no eixo)
                    sRot = s0 + 2;
                    foreach (var q in m.Esquinas)
                    {
                        var (sq, vq) = e.Projetar(q[0], q[1]);
                        if (vq > 0 && sq > 0 && vq < b.MeiaLarguraEm(sq) + q[2] + 3) sRot = Math.Max(sRot, sq + 1);
                    }
                }
                // a fila desenhada para no fim do eixo guardado (e em 300 m); o rótulo traz o comprimento calculado
                double limite = Math.Max(5, Math.Min(300, e.Comprimento - s0 - 5));
                double fila = double.IsNaN(ap.Fila95m) || double.IsInfinity(ap.Fila95m) ? limite : ap.Fila95m;
                double comp = Math.Clamp(fila, 1, limite);
                bool cortada = fila > limite + 1e-6;
                var pl = new Polyline();
                int np = Math.Clamp((int)Math.Ceiling(comp / 2), 2, 200);
                for (int i = 0; i <= np; i++)
                {
                    double st = s0 + comp * i / np;
                    var q = e.Ponto(st, v(st));
                    pl.AddVertexAt(i, new Point2d(q.X, q.Y), 0, 0.8, 0.8);
                }
                pl.SetDatabaseDefaults();
                pl.Layer = lay;
                pl.ColorIndex = Cor(ap.Nivel);
                ids.Add(Ctx.Adicionar(s.Tr, ms, pl));
                // rótulo por fora do bordo, do lado da aproximação, a partir da linha de retenção e correndo para fora
                var pt = e.Ponto(sRot, vRotulo(sRot));
                var (dx, dy) = e.Direcao(sRot);
                double giro = Math.Atan2(dy, dx);
                var anexo = AttachmentPoint.MiddleLeft;
                if (giro > Math.PI / 2) { giro -= Math.PI; anexo = AttachmentPoint.MiddleRight; }
                else if (giro < -Math.PI / 2) { giro += Math.PI; anexo = AttachmentPoint.MiddleRight; }
                string rotulo = $"{a.Item} {rumos[ap.Braco]}: x {CalculoCapacidade.TxtX(ap.X)} · NS {ap.Nivel} · {CalculoCapacidade.Txt(ap.Atraso, "0", 999)} s · fila 95% {fila:0} m"
                                 + (cortada ? $" (desenhada até {comp:0} m)" : "")
                                 + (a.Armazenamento.TryGetValue(ap.Braco, out var arm) && ap.Movimento.StartsWith("conversão") ? $" (armazenamento {arm:0} m)" : "");
                var tx = Ctx.Texto(rotulo, new Point3d(pt.X, pt.Y, 0), 1.0, giro, anexo);
                tx.Layer = lay;
                tx.ColorIndex = Cor(ap.Nivel);
                ids.Add(Ctx.Adicionar(s.Tr, ms, tx));
            }
            dados.Desenhados = ids.Select(i => i.Handle.ToString()).ToList();
            it.Capacidade = dados;
            ArmazenamentoSinal.Gravar(s.Tr, s.Db, it);
            s.Tr.Commit();

            Ctx.Msg($"{it.Nome} ({a.Tipo}), HCM 7ª ed.:");
            foreach (var ap in a.Aproximacoes)
                Ctx.Msg($"  {ap.Onde} – {ap.Movimento}: {ap.Fluxo:0} veíc/h para {ap.Capacidade:0}, x = {CalculoCapacidade.TxtX(ap.X)}, atraso {CalculoCapacidade.TxtAtraso(ap.Atraso)} s, NS {ap.Nivel}, fila de 95 % {ap.Fila95:0.0} veíc ({ap.Fila95m:0} m)");
            foreach (var mv in a.Movimentos)
                Ctx.Msg($"    {mv.Onde} {mv.Movimento}: conflitante {mv.Conflitante:0}, tc {mv.IntervaloCritico:0.0} s, capacidade {mv.Capacidade:0}, x = {CalculoCapacidade.TxtX(mv.X)}, NS {mv.Nivel}");
            foreach (var rec in a.Recomendacoes) Ctx.Msg("RECOMENDAÇÃO: " + rec);
            var ver = a.Verificacoes;
            Ctx.Msg($"Verificações: {ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovada(s), {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADA(S), " +
                    $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção.");
            foreach (var v in ver.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao))
                Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor}");
            foreach (var av in a.Avisos) Ctx.Msg("AVISO: " + av);
            Ctx.Msg($"Fila de 95 % desenhada em cada aproximação na camada {Layer} (verde NS A-B, amarelo C-D, laranja E, vermelho F).");

            if (a.Armazenamento.Count > 0)
            {
                double rec = a.Armazenamento.Values.Max();
                if (Math.Abs(rec - cfg.ConvArmazenamento) > 0.01 &&
                    s.PedirPalavra($"Usar {rec:0} m de armazenamento na faixa de conversão nas próximas interseções (hoje {cfg.ConvArmazenamento:0} m; monte de novo com o PRJINTERSECAO)", new[] { "Sim", "Nao" }, "Nao") == "Sim")
                {
                    cfg = ConfigRotatoria.Carregar();
                    cfg.ConvArmazenamento = rec;
                    cfg.IntersecaoExtrasDoUsuario = true;
                    ConfigRotatoria.Salvar(cfg);
                    Ctx.Msg($"Armazenamento da faixa de conversão: {rec:0} m (PRJINTERSECAO › Parametros).");
                }
            }

            var rel = new RelatorioCapacidade { Titulo = $"Capacidade e fila - {it.Nome}", Desenho = s.Doc.Name, Analise = a };
            rel.Volumes.AddRange(tabela);
            rel.Parametros.AddRange(RelatorioCapacidade.ParametrosDe(p, a.Tipo));
            rel.Pendencias.AddRange(RelatorioCapacidade.PendenciasPadrao(a.Tipo));
            if (s.PedirPalavra("Gravar a capacidade e as filas em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Capacidade_" + Texto.IdSeguro(it.Nome.Replace(' ', '_'), 50) + ".xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Planilha gravada: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJCAPACIDADE: {ex.Message}"); }
    }

    private static short Cor(string nivel) => nivel switch { "A" or "B" => 3, "C" or "D" => 2, "E" => 30, _ => 1 };

    private static string Resumo(ParametrosCapacidade p) =>
        $"Capacidade e fila (HCM 7ª ed.): FHP {p.Fhp:0.00}, pesados {p.Pesados * 100:0.#} %, período {p.Periodo * 60:0} min, {p.EspacoVeiculo:0.0} m por veículo na fila, x de projeto {p.XProjeto:0.00}" +
        (p.ParametrosDoUsuario ? "." : " [PREMISSA; troque em Parametros].");

    private static bool PedirParametros(Sessao s, ParametrosCapacidade p)
    {
        double? Faixa(string msg, double atual, double min, double max)
        {
            var x = s.PedirNumero(msg, atual);
            if (x is null) return null;
            double c = Math.Clamp(x.Value, min, max);
            if (Math.Abs(c - x.Value) > 1e-9) Ctx.Msg($"  fora da faixa: usei {c:0.##}.");
            return c;
        }
        if (Faixa("Fator de hora de pico (0,70 a 1,00)", p.Fhp, 0.7, 1.0) is not { } fhp) return false;
        p.Fhp = fhp;
        if (Faixa("Veículos pesados (%)", p.Pesados * 100, 0, 60) is not { } pes) return false;
        p.Pesados = pes / 100;
        if (Faixa("Período de análise (min)", p.Periodo * 60, 15, 60) is not { } per) return false;
        p.Periodo = per / 60;
        if (Faixa("Espaço por veículo na fila (m)", p.EspacoVeiculo, 5, 15) is not { } esp) return false;
        p.EspacoVeiculo = esp;
        if (Faixa("Grau de saturação máximo de projeto (0,85 usual)", p.XProjeto, 0.5, 1.0) is not { } xp) return false;
        p.XProjeto = xp;
        p.ParametrosDoUsuario = true;
        return true;
    }
}
