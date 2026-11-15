using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;
using Projeta.Giro.Core.Veiculos;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJSEGURANCA — auditor de segurança de uma rotatória montada pelo PRJROTATORIA: trajetória mais rápida (velocidade e deflexão
/// de cada entrada, NCHRP 672), giro do veículo de projeto em todos os movimentos (núcleo do PROJETA Giro: folgas das rodas aos
/// meios-fios, à ilha central e uso da faixa transponível) e visibilidade em 3D sobre as superfícies (parada na aproximação e
/// triângulo à esquerda). Desenha as envoltórias, as visadas e as marcas dos problemas, e grava o Excel com REPROVADO onde não atende.
/// </summary>
public sealed class CmdSeguranca
{
    internal const string LayerGiro = "PRJ-SEGURANCA-GIRO", LayerCaminho = "PRJ-SEGURANCA-CAMINHO", LayerVisibilidade = "PRJ-SEGURANCA-VISIBILIDADE",
                          LayerProblemas = "PRJ-SEGURANCA-PROBLEMAS";

    /// <summary>Pasta dos veículos do usuário do PROJETA Giro (os mesmos que o GIROVEICULO grava).</summary>
    private static string PastaVeiculos => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "Giro", "veiculos");

    [CommandMethod("PRJSEGURANCA", CommandFlags.Modal)]
    public static void Seguranca()
    {
        try
        {
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Seguranca ?? new ParametrosSeguranca();
            var bib = Biblioteca.Carregar(PastaVeiculos);
            foreach (var msg in bib.Mensagens) Ctx.Msg("AVISO (veículos do usuário): " + msg);
            using var s = Sessao.Abrir();
            var itens = ArmazenamentoSinal.Ler(s.Tr, s.Db).Where(x => x.Rotatoria is not null).ToList();
            if (itens.Count == 0)
            {
                Ctx.Msg("Este desenho não tem rotatória do PROJETA guardada. Monte com o PRJROTATORIA (desta versão em diante ele guarda a geometria) e rode o PRJSEGURANCA de novo.");
                return;
            }
            while (true)
            {
                Ctx.Msg(Resumo(p));
                var r = s.PedirPalavra("Auditor de segurança da rotatória", new[] { "Auditar", "Parametros" }, "Auditar");
                if (r is null) return;
                if (r == "Parametros") { if (!PedirParametros(s, p)) return; cfg.Seguranca = p; ConfigRotatoria.Salvar(cfg); continue; }
                break;
            }
            int k = itens.Count == 1 ? 0 : s.EscolherDaLista("Rotatória", itens.Select(x => x.Nome).ToList(), 0);
            if (k < 0) return;
            var it = itens[k];

            // veículo de projeto (biblioteca do PROJETA Giro) e o carro, para a faixa transponível
            var veics = bib.Ordenados.ToList();
            int iv = Math.Max(0, veics.FindIndex(v => string.Equals(v.Id, p.Veiculo, StringComparison.OrdinalIgnoreCase)));
            int kv = s.EscolherDaLista("Veículo de projeto", veics.Select(v => $"{v.Id} — {v.Nome}, {v.Comprimento:0.0} m").ToList(), iv);
            if (kv < 0) return;
            var lista = new List<Veiculo> { veics[kv] };
            if (p.ConferirVp && !string.Equals(veics[kv].Id, "VP", StringComparison.OrdinalIgnoreCase) && bib.Obter("VP") is { } vp) lista.Add(vp);

            // superfícies da visibilidade 3D: projeto (pavimento acabado) onde houver, terreno no resto
            var sups = VolumesCivil.Superficies(s.Tr);
            var terrenos = CorredorProjeta.SuperficiesDeTerreno(s.Tr);
            var nomes = new List<string> { "(nenhuma)" };
            nomes.AddRange(sups.Select(x => x.Nome));
            int pp = sups.FindIndex(x => x.Nome.Contains("topo", StringComparison.OrdinalIgnoreCase) && x.Nome.Contains(it.Nome, StringComparison.OrdinalIgnoreCase));
            if (pp < 0) pp = sups.FindIndex(x => x.Nome.Contains("topo", StringComparison.OrdinalIgnoreCase) && !terrenos.Any(t => t.Id == x.Id));
            int kp = s.EscolherDaLista("Superfície do projeto (pavimento acabado; a cota vem dela onde houver)", nomes, pp + 1);
            if (kp < 0) return;
            int pt = terrenos.Count > 0 ? sups.FindIndex(x => x.Id == terrenos[0].Id) : -1;
            int kt = s.EscolherDaLista("Superfície do terreno (onde o projeto não cobre)", nomes, pt + 1);
            if (kt < 0) return;
            var supProj = kp > 0 ? s.Tr.GetObject(sups[kp - 1].Id, OpenMode.ForRead) as Surface : null;
            var supTer = kt > 0 ? s.Tr.GetObject(sups[kt - 1].Id, OpenMode.ForRead) as Surface : null;
            Func<double, double, double?>? z = supProj is null && supTer is null ? null : (x, y) => Cota(supProj, x, y) ?? Cota(supTer, x, y);
            string supTxt = $"projeto: {(kp > 0 ? nomes[kp] : "nenhuma")}; terreno: {(kt > 0 ? nomes[kt] : "nenhuma")}";

            var crit = new CriteriosRotatoria { VelMaxEntrada = cfg.VelMaxEntrada, DifVelMax = cfg.DifVelMax };
            Ctx.Msg($"Auditando '{it.Nome}' com o {string.Join(" e o ", lista.Select(v => v.Id))}...");
            var a = AuditorRotatoria.Auditar(it.Nome, it.Rotatoria!, p, crit, lista, z, m => Ctx.Msg("  " + m));
            a.Superficies = supTxt;
            var modelo = it.Rotatoria!.Modelo();

            // substitui o desenho anterior
            var dados = it.Seguranca ?? new DadosSeguranca();
            var antigos = new List<ObjectId>();
            foreach (var h in dados.Desenhados)
                if (long.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out long hv) && s.Db.TryGetObjectId(new Handle(hv), out var oid) && !oid.IsErased)
                    antigos.Add(oid);
            if (antigos.Count > 0)
            {
                var resp = s.PedirPalavra($"'{it.Nome}' já tem uma auditoria desenhada ({antigos.Count} objetos). Substituir", new[] { "Sim", "Nao" }, "Sim");
                if (resp != "Sim") return;
                foreach (var oid in antigos) s.Tr.GetObject(oid, OpenMode.ForWrite).Erase();
            }
            var ids = Desenhar(s, a);
            dados.Veiculos = string.Join(", ", lista.Select(v => v.Id));
            dados.Desenhados = ids.Select(i => i.Handle.ToString()).ToList();
            it.Seguranca = dados;
            ArmazenamentoSinal.Gravar(s.Tr, s.Db, it);
            s.Tr.Commit();

            // mensagens
            var ver = a.Verificacoes;
            Ctx.Msg($"{it.Nome}: {ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovada(s), {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADA(S), " +
                    $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção.");
            foreach (var (nome, filtro) in Grupos())
            {
                var l = ver.Where(filtro).ToList();
                Ctx.Msg($"  {nome}: {l.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovada(s), {l.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} reprovada(s), {l.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção");
            }
            foreach (var an in a.Trajetorias)
                Ctx.Msg($"  trajetória mais rápida, braço {modelo.Bracos[an.Braco].Rumo}: V1 {V(an.R1)}, V2 {V(an.R2)}, V3 {V(an.R3)}, V4 {V(an.R4)}, V5 {V(an.R5)} km/h");
            foreach (var g in a.Movimentos.GroupBy(x => x.Veiculo))
            {
                var ok = g.Where(x => x.Erro is null).ToList();
                Ctx.Msg($"  giro do {g.Key}: {g.Count(x => x.Resultado == VerificacaoRotatoria.Reprovado)} de {g.Count()} movimento(s) REPROVADO(S)" +
                        (ok.Count > 0 ? $"; menor folga ao meio-fio externo {F(ok.Min(x => x.FolgaExterna))} m, à ilha central {F(ok.Min(x => x.FolgaIlha))} m, largura varrida até {ok.Max(x => x.LarguraVarrida):0.00} m" : ""));
            }
            int mostradas = 0;
            foreach (var v in ver.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao))
            {
                if (mostradas++ >= 30) { Ctx.Msg("  ... (o resto está no Excel)"); break; }
                Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor}");
            }
            foreach (var rec in a.Recomendacoes) Ctx.Msg("RECOMENDAÇÃO: " + rec);
            foreach (var av in a.Avisos) Ctx.Msg("AVISO: " + av);
            Ctx.Msg($"Desenho: envoltórias nas camadas {LayerGiro}-<veículo>-<movimento> (verde à direita, azul em frente, magenta à esquerda, laranja retorno; rodas em cinza), caminhos em {LayerCaminho}, " +
                    $"visadas em {LayerVisibilidade} (verde livre, vermelho tapada, amarelo sem superfície) e os problemas marcados em {LayerProblemas}.");

            var rel = new RelatorioSeguranca { Titulo = $"Auditoria de segurança - {it.Nome}", Desenho = s.Doc.Name, Analise = a, Modelo = modelo };
            rel.Parametros.AddRange(RelatorioSeguranca.ParametrosDe(p, crit, string.Join(", ", lista.Select(v => $"{v.Id} ({v.Nome})")), supTxt));
            rel.Pendencias.AddRange(RelatorioSeguranca.PendenciasPadrao());
            if (s.PedirPalavra("Gravar a auditoria em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Seguranca_" + Texto.IdSeguro(it.Nome.Replace(' ', '_'), 50) + ".xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Planilha gravada: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJSEGURANCA: {ex.Message}"); }
    }

    private static IEnumerable<(string, Func<Verificacao, bool>)> Grupos() => new (string, Func<Verificacao, bool>)[]
    {
        ("trajetória mais rápida e geometria", v => !v.Item.StartsWith("Giro", StringComparison.Ordinal) && !v.Item.StartsWith("Visibilidade em 3D", StringComparison.Ordinal)),
        ("giro do veículo de projeto", v => v.Item.StartsWith("Giro", StringComparison.Ordinal)),
        ("visibilidade em 3D", v => v.Item.StartsWith("Visibilidade em 3D", StringComparison.Ordinal)),
    };

    private static string V(RaioCritico? r) => r is null ? "-" : r.Velocidade.ToString("0");
    private static string F(double v) => double.IsInfinity(v) ? "∞" : v.ToString("0.00");

    private static double? Cota(Surface? sup, double x, double y)
    {
        if (sup is null) return null;
        try
        {
            double z = sup.FindElevationAtXY(x, y);
            return double.IsNaN(z) || z < -1e5 ? null : z;
        }
        catch { return null; }
    }

    private static short CorMovimento(string mov) => mov switch { "à direita" => 3, "em frente" => 5, "à esquerda" => 6, "retorno" => 30, _ => 4 };
    private static short CorResultado(string r) => r == VerificacaoRotatoria.Aprovado ? (short)3 : r == VerificacaoRotatoria.Reprovado ? (short)1 : (short)2;

    private static List<ObjectId> Desenhar(Sessao s, AnaliseSeguranca a)
    {
        var ids = new List<ObjectId>();
        var ms = Ctx.EspacoModelo(s.Tr, s.Db);
        string layCam = Ctx.GarantirLayer(s.Tr, s.Db, LayerCaminho, 8);
        string layVis = Ctx.GarantirLayer(s.Tr, s.Db, LayerVisibilidade, 6);
        string layProb = Ctx.GarantirLayer(s.Tr, s.Db, LayerProblemas, 1);
        ObjectId Pl(IReadOnlyList<(double X, double Y)> pts, bool fechada, string lay, short cor, double largura = 0)
        {
            var pl = new Polyline();
            for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, largura, largura);
            pl.Closed = fechada;
            pl.SetDatabaseDefaults();
            pl.Layer = lay;
            pl.ColorIndex = cor;
            return Ctx.Adicionar(s.Tr, ms, pl);
        }
        ObjectId Tx(string t, double x, double y, string lay, short cor, double h = 0.8)
        {
            var tx = Ctx.Texto(t, new Point3d(x, y, 0), h, 0, AttachmentPoint.MiddleLeft);
            tx.Layer = lay;
            tx.ColorIndex = cor;
            return Ctx.Adicionar(s.Tr, ms, tx);
        }

        // envoltórias (cor da camada) e rodas (cinza) numa camada por veículo e tipo de movimento, para isolar; caminho do eixo dianteiro
        foreach (var g in a.Movimentos.GroupBy(x => (x.Veiculo, x.Movimento)))
        {
            short cor = CorMovimento(g.Key.Movimento);
            string tipo = g.Key.Movimento switch { "à direita" => "DIREITA", "em frente" => "FRENTE", "à esquerda" => "ESQUERDA", "retorno" => "RETORNO", _ => "OUTRAS" };
            string lay = Ctx.GarantirLayer(s.Tr, s.Db, $"{LayerGiro}-{Texto.IdSeguro(g.Key.Veiculo, 20)}-{tipo}", cor);
            foreach (var mv in g)
            {
                foreach (var e in mv.Envoltoria.Where(e => e.Count >= 3)) ids.Add(Pl(e, true, lay, 256));
                foreach (var r in mv.Rodas.Where(r => r.Count >= 3)) ids.Add(Pl(r, true, lay, 8));
                if (mv.Caminho.Count >= 2) ids.Add(Pl(mv.Caminho, false, layCam, cor));
            }
        }

        // problemas: os do mesmo tipo a até 6 m um do outro viram uma marca só, com a contagem
        var grupos = new List<(double X, double Y, string Texto, int N)>();
        foreach (var mv in a.Movimentos)
            foreach (var (x, y, t) in mv.Problemas)
            {
                int i = grupos.FindIndex(g => g.Texto == t && (g.X - x) * (g.X - x) + (g.Y - y) * (g.Y - y) < 36);
                if (i < 0) grupos.Add((x, y, t, 1));
                else grupos[i] = grupos[i] with { N = grupos[i].N + 1 };
            }
        foreach (var (x, y, t, n) in grupos)
        {
            var c = new Circle(new Point3d(x, y, 0), Vector3d.ZAxis, 1.2) { Layer = layProb, ColorIndex = 1 };
            ids.Add(Ctx.Adicionar(s.Tr, ms, c));
            ids.Add(Tx(t + (n > 1 ? $" ({n} movimentos)" : ""), x + 1.6, y, layProb, 1));
        }

        // visadas: linha até o alvo crítico (ou o último), cor pelo resultado; X onde a superfície tapa
        foreach (var v in a.Visadas)
        {
            var alvo = v.AlvoCritico ?? (v.Alvos.Count > 0 ? v.Alvos[^1] : v.Observador);
            short cor = CorResultado(v.Resultado);
            ids.Add(Pl(new[] { v.Observador, alvo }, false, layVis, cor));
            if (v.Resultado == VerificacaoRotatoria.Reprovado && v.PontoCritico is { } pc)
            {
                ids.Add(Pl(new[] { (pc.X - 1, pc.Y - 1), (pc.X + 1, pc.Y + 1) }, false, layProb, 1, 0.15));
                ids.Add(Pl(new[] { (pc.X - 1, pc.Y + 1), (pc.X + 1, pc.Y - 1) }, false, layProb, 1, 0.15));
                ids.Add(Tx($"visada tapada ({v.Tipo}): passa {v.FolgaMinima:0.00} m", pc.X + 1.4, pc.Y, layProb, 1));
            }
        }
        return ids;
    }

    private static string Resumo(ParametrosSeguranca p) =>
        $"Auditor: veículo {p.Veiculo}{(p.ConferirVp ? " (e o VP)" : "")}, rodas a ≥ {p.FolgaRoda:0.00} m dos meios-fios, roda dianteira a {p.FolgaGuia:0.00} m do meio-fio externo no anel, " +
        $"aproximação a {p.VelocidadeAproximacao:0} km/h, olho {p.AlturaOlho:0.00} m, objeto {p.AlturaObjeto:0.00} m" + (p.ParametrosDoUsuario ? "." : " [PREMISSA; troque em Parametros].");

    private static bool PedirParametros(Sessao s, ParametrosSeguranca p)
    {
        double? Faixa(string msg, double atual, double min, double max)
        {
            var x = s.PedirNumero(msg, atual);
            if (x is null) return null;
            double c = Math.Clamp(x.Value, min, max);
            if (Math.Abs(c - x.Value) > 1e-9) Ctx.Msg($"  fora da faixa: usei {c:0.##}.");
            return c;
        }
        var vp = s.PedirPalavra("Rodar também o carro (VP) para conferir a faixa transponível", new[] { "Sim", "Nao" }, p.ConferirVp ? "Sim" : "Nao");
        if (vp is null) return false;
        if (Faixa("Folga mínima das rodas aos meios-fios (m)", p.FolgaRoda, 0, 2) is not { } fr) return false;
        if (Faixa("Roda dianteira externa até o meio-fio externo no anel (m)", p.FolgaGuia, 0.3, 3) is not { } fg) return false;
        if (Faixa("Velocidade de aproximação para a visibilidade de parada (km/h)", p.VelocidadeAproximacao, 20, 120) is not { } va) return false;
        if (Faixa("Altura do olho do motorista (m)", p.AlturaOlho, 0.8, 2.5) is not { } ho) return false;
        if (Faixa("Altura do objeto na pista (visibilidade de parada, m)", p.AlturaObjeto, 0, 1.5) is not { } hb) return false;
        if (Faixa("Altura do veículo que se aproxima (visibilidade à esquerda, m)", p.AlturaVeiculo, 0.5, 2.5) is not { } hv) return false;
        p.ConferirVp = vp == "Sim"; p.FolgaRoda = fr; p.FolgaGuia = fg; p.VelocidadeAproximacao = va; p.AlturaOlho = ho; p.AlturaObjeto = hb; p.AlturaVeiculo = hv;
        p.ParametrosDoUsuario = true;
        return true;
    }
}
