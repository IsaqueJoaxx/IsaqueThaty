using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Infra;

namespace Projeta.Giro.Comandos;

/// <summary>Verificação de conflitos, animação e relatório.</summary>
public sealed class CmdVerificacao
{
    [CommandMethod("PROJETA_GIRO", "GIROVERIFICAR", CommandFlags.Modal)]
    public void Verificar() => Operacoes.Executar("GIROVERIFICAR", () =>
    {
        var sel = Operacoes.SelecionarManobra("Selecione a varredura a verificar: ");
        if (sel is null) return;
        var m = sel.Value.M;
        var filtro = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, Curvas.FiltroCurvas) });
        var so = new PromptSelectionOptions { MessageForAdding = "\nSelecione meios-fios, bordas e obstáculos (linhas, polilinhas, arcos, alinhamentos, feature lines): " };
        var rs = Ctx.Ed.GetSelection(so, filtro);
        if (rs.Status != PromptStatus.OK) return;
        var cfg = Config.Atual;
        var rf = Ctx.Ed.GetDistance(new PromptDistanceOptions($"\nFolga lateral exigida (m) <{Num.F(cfg.Folga)}>: ") { AllowNone = true, AllowNegative = false, AllowZero = true, DefaultValue = cfg.Folga, UseDefaultValue = true });
        if (rf.Status != PromptStatus.OK) return;
        cfg.Folga = rf.Value;
        cfg.Salvar();
        List<Core.Verificacao.Obstaculo> obs;
        int ign;
        using (var tr = Ctx.Db.TransactionManager.StartTransaction())
        {
            obs = Curvas.Obstaculos(tr, rs.Value.GetObjectIds(), out ign);
            tr.Commit();
        }
        if (ign > 0) Ctx.Msg($"{ign} objeto(s) ignorado(s) (não são curvas ou fazem parte de varreduras).");
        if (obs.Count == 0) { Ctx.Msg("nenhum obstáculo válido selecionado."); return; }
        Operacoes.Verificar(m, obs, cfg.Folga);
    });

    [CommandMethod("PROJETA_GIRO", "GIROLIMPARVERIF", CommandFlags.Modal)]
    public void LimparVerificacao() => Operacoes.Executar("GIROLIMPARVERIF", () =>
    {
        var sel = Operacoes.SelecionarManobra("Selecione a varredura cuja verificação será apagada: ");
        if (sel is null) return;
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        int n = Desenho.ApagarVerificacao(tr, Ctx.Db, sel.Value.M.Id);
        tr.Commit();
        Ctx.Msg($"{n} objeto(s) de verificação apagados.");
    });

    // ------------------------------------------------------------------ animação

    [CommandMethod("PROJETA_GIRO", "GIROANIMAR", CommandFlags.Modal)]
    public void Animar() => Operacoes.Executar("GIROANIMAR", () =>
    {
        var sel = Operacoes.SelecionarManobra("Selecione a varredura a animar: ");
        if (sel is null) return;
        var m = sel.Value.M;
        var cfg = Config.Atual;
        var o = new PromptDoubleOptions($"\nVelocidade da animação (1 = tempo real) <{Num.F(cfg.FatorAnimacao, 1)}>: ")
        { AllowNone = true, AllowNegative = false, AllowZero = false, DefaultValue = cfg.FatorAnimacao, UseDefaultValue = true };
        var rf = Ctx.Ed.GetDouble(o);
        if (rf.Status != PromptStatus.OK) return;
        cfg.FatorAnimacao = Math.Clamp(rf.Value, 0.1, 50);
        cfg.Salvar();
        var r = Varredura.Simular(m, new OpcoesVarredura { CalcularEnvoltoria = false });
        if (!Ctx.ComInterface) { Ctx.Msg($"animação só com a interface do Civil 3D ({r.Poses.Count} posições calculadas)."); return; }
        Ctx.Msg("Animando — Esc para parar.");
        Executar(r, m.Velocidade, cfg.FatorAnimacao);
    });

    private static void Executar(ResultadoVarredura r, double velKmh, double fator)
    {
        var tm = TransientManager.CurrentTransientManager;
        var vazio = new IntegerCollection();
        var atuais = new List<Entity>();
        var v = r.Manobra.Veiculo;
        const double quadro = 0.04; // s
        double ds = Math.Max(0.02, velKmh / 3.6 * quadro * fator);
        var relogio = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            for (double s = 0; ; s += ds)
            {
                bool ultimo = s >= r.Comprimento;
                var pose = r.PoseEm(Math.Min(s, r.Comprimento));
                foreach (var e in atuais) { tm.EraseTransient(e, vazio); e.Dispose(); }
                atuais.Clear();
                foreach (var e in Desenho.EntidadesVeiculo(v, pose))
                {
                    e.ColorIndex = 1;
                    tm.AddTransient(e, TransientDrawingMode.DirectTopmost, 128, vazio);
                    atuais.Add(e);
                }
                Ctx.Ed.UpdateScreen();
                System.Windows.Forms.Application.DoEvents();
                if (HostApplicationServices.Current.UserBreak()) break;
                long alvo = (long)((s / ds + 1) * quadro * 1000);
                int espera = (int)(alvo - relogio.ElapsedMilliseconds);
                if (espera > 0) Thread.Sleep(espera);
                if (ultimo) { Thread.Sleep(600); break; }
            }
        }
        finally
        {
            foreach (var e in atuais) { tm.EraseTransient(e, vazio); e.Dispose(); }
            Ctx.Ed.UpdateScreen();
        }
    }

    // ------------------------------------------------------------------ relatório

    [CommandMethod("PROJETA_GIRO", "GIRORELATORIO", CommandFlags.Modal)]
    public void Relatorio() => Operacoes.Executar("GIRORELATORIO", () =>
    {
        var sel = Operacoes.SelecionarManobra("Selecione a varredura: ");
        if (sel is null) return;
        var m = sel.Value.M;
        var v = m.Veiculo;
        var r = Varredura.Simular(m);
        var e = v.Estatico();
        Ctx.Msg("──────── PROJETA Giro — relatório da varredura ────────");
        Ctx.Msg($"Manobra {m.Id} ({m.Modo}{(m.Nome.Length > 0 ? ": " + m.Nome : "")}), {m.Trechos.Count} trecho(s), {Num.F(r.Comprimento, 1)} m, velocidade {Num.F(m.Velocidade, 0)} km/h.");
        Ctx.Msg("Veículo: " + Operacoes.Resumo(v));
        Ctx.Msg($"Fonte: {v.Fonte}");
        if (v.RefRaioRodaInterna is double ri)
            Ctx.Msg($"Giro estático × referência: roda externa {Num.F(e.RodaExternaDianteira)} ({Num.F(v.RaioMinimoRodaExterna ?? double.NaN)}) m · roda interna traseira {Num.F(e.RodaInternaTraseira)} ({Num.F(ri)}) m · balanço {Num.F(e.BalancoDianteiro)} ({Num.F(v.RefRaioBalanco ?? double.NaN)}) m");
        Ctx.Msg($"Largura varrida máxima: {Num.F(r.LarguraMaxima)} m a {Num.F(r.SLarguraMaxima, 0)} m do início.");
        Ctx.Msg($"Esterçamento máximo usado: {Num.F(Num.Graus(r.DeltaMaximo), 1)}° de {Num.F(Num.Graus(v.AnguloMaximo), 1)}° disponíveis.");
        if (v.Articulado) Ctx.Msg($"Articulação máxima: {Num.F(Num.Graus(r.ArticulacaoMaxima), 0)}° (limite adotado {Num.F(v.ArticulacaoMaximaGraus, 0)}°).");
        Ctx.Msg($"Área varrida: {Num.F(Core.Geometria.Poligonos.AreaTotal(r.Envoltoria), 1)} m².");
        foreach (var a in r.Avisos) Ctx.Msg("  " + a);
        if (!string.IsNullOrWhiteSpace(v.Observacoes)) Ctx.Msg("Observações do veículo: " + v.Observacoes);
        Ctx.Msg("Rascunho técnico — confira com o gabarito oficial e com o projeto executivo.");
    });
}
