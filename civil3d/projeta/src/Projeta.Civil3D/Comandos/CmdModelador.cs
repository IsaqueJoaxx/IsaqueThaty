using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Modelador;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdModelador
{
    private static int _contador;

    /// <summary>Insere um dispositivo avulso (fora de rede) como bloco com planta + sólido, pedindo ponto e rotação.</summary>
    private static void InserirAvulso(Sessao s, DefinicaoDispositivo def, IReadOnlyDictionary<string, double> valores)
    {
        var mod = AvaliadorDispositivo.Avaliar(def, valores);
        foreach (var m in mod.Mensagens) Ctx.Msg((m.Gravidade == Projeta.Core.Comum.Gravidade.Erro ? "[ERRO] " : "") + m);
        if (!mod.Ok) return;
        var p = s.Ed.GetPoint($"\nPonto de inserção de {def.Id} (origem do dispositivo): ");
        if (p.Status != PromptStatus.OK) return;
        var ang = s.Ed.GetAngle(new PromptAngleOptions("\nRotação <0>: ") { AllowNone = true, UseBasePoint = true, BasePoint = p.Value });
        double rot = ang.Status == PromptStatus.OK ? ang.Value : 0;
        var r = InserirDispositivo(s, def, mod, p.Value, rot);
        foreach (var e in r.Erros) Ctx.Msg("[AVISO] " + e);
        foreach (var q in mod.Quantitativos) Ctx.Msg($"  {q.Nome}: {Projeta.Core.Comum.Num.F(q.Valor, 3)} {q.Unidade}");
        Ctx.Msg($"  Volume do sólido no desenho: {Projeta.Core.Comum.Num.F(r.VolumeReal, 3)} m³");
        s.Tr.Commit();
    }

    internal sealed record Insercao(ObjectId Id, double VolumeReal, List<string> Erros);

    /// <summary>Insere o dispositivo avaliado como bloco (sem perguntas). Retorna o volume real dos sólidos.</summary>
    internal static Insercao InserirDispositivo(Sessao s, DefinicaoDispositivo def, ModeloAvaliado mod, Point3d pos, double rot)
    {
        double prof = mod.Valores.TryGetValue("Profundidade", out var pr) ? pr : 0;
        var erros = new List<string>();
        Ctx.GarantirLayer(s.Tr, s.Db, "PRJ-DISPOSITIVOS", 4);
        Ctx.GarantirLayer(s.Tr, s.Db, "PRJ-DISPOSITIVOS-3D", 8);
        string nome = $"PRJ_AVULSO_{Projeta.Core.Comum.Texto.IdSeguro(def.Id, 40)}_{DateTime.Now:HHmmss}_{++_contador}";
        var btrId = DesenhoRede.DefinirBloco(s, nome, mod, prof, true, null, "PRJ-DISPOSITIVOS-3D", erros, def.Id);
        var br = new BlockReference(pos, btrId) { Rotation = rot, Layer = "PRJ-DISPOSITIVOS" };
        var id = Ctx.Adicionar(s.Tr, Ctx.EspacoModelo(s.Tr, s.Db), br);
        double vol = 0;
        var btr = (BlockTableRecord)s.Tr.GetObject(btrId, OpenMode.ForRead);
        foreach (ObjectId eid in btr)
            if (s.Tr.GetObject(eid, OpenMode.ForRead) is Solid3d so) vol += so.MassProperties.Volume;
        return new Insercao(id, vol, erros);
    }

    /// <summary>PRJMODELADOR — editor de dispositivos paramétricos e seções-tipo.</summary>
    [CommandMethod("PRJMODELADOR", CommandFlags.Modal)]
    public static void Modelador() => Abrir(false);

    /// <summary>PRJSECOES — abre o modelador na aba de seções-tipo (dispositivos longitudinais).</summary>
    [CommandMethod("PRJSECOES", CommandFlags.Modal)]
    public static void Secoes() => Abrir(true);

    private static void Abrir(bool secoes) => Operacoes.Rodar("PRJMODELADOR", s =>
    {
        using var f = new FormModelador(Sessao.Bib, secoes);
        var r = AcApp.ShowModalDialog(f);
        Sessao.RecarregarBiblioteca();
        if (r == System.Windows.Forms.DialogResult.OK && f.ParaInserir is not null)
            InserirAvulso(s, f.ParaInserir, f.ValoresTeste);
    });

    /// <summary>PRJDISPOSITIVO — insere um dispositivo da biblioteca, avulso, com parâmetros.</summary>
    [CommandMethod("PRJDISPOSITIVO", CommandFlags.Modal)]
    public static void Dispositivo() => Operacoes.Rodar("PRJDISPOSITIVO", s =>
    {
        var defs = Sessao.Bib.Dispositivos.Values.OrderBy(d => d.Categoria).ThenBy(d => d.Id).ToList();
        int i = s.EscolherDaLista("Dispositivos", defs.Select(d => $"{d.Id} — {d.Nome} [{d.Categoria}]").ToList());
        if (i < 0) return;
        var def = defs[i];
        var padrao = AvaliadorDispositivo.Avaliar(def).Valores;
        using var f = new FormParametros($"Parâmetros de {def.Id}", def.Parametros, new Dictionary<string, double>(), padrao);
        if (AcApp.ShowModalDialog(f) != System.Windows.Forms.DialogResult.OK) return;
        InserirAvulso(s, def, f.Valores);
    });

    /// <summary>PRJBIBLIOTECA — abre a pasta da biblioteca do usuário e recarrega as definições.</summary>
    [CommandMethod("PRJBIBLIOTECA", CommandFlags.Modal)]
    public static void Biblioteca()
    {
        var pasta = Sessao.Bib.PastaUsuario ?? Projeta.Core.Modelador.Biblioteca.PastaPadrao;
        Directory.CreateDirectory(Path.Combine(pasta, "dispositivos"));
        Directory.CreateDirectory(Path.Combine(pasta, "secoes"));
        UiUtil.AbrirNoSistema(pasta);
        Sessao.RecarregarBiblioteca();
        Ctx.Msg($"Biblioteca: {Sessao.Bib.Dispositivos.Count} dispositivos e {Sessao.Bib.Secoes.Count} seções. Pasta: {pasta}");
        foreach (var m in Sessao.Bib.Validar().Where(m => m.Gravidade == Projeta.Core.Comum.Gravidade.Erro)) Ctx.Msg("[ERRO] " + m);
    }
}

public sealed class CmdGeral
{
    private static PaletteSet? _painel;

    /// <summary>PRJABA — recria a aba PROJETA na faixa de opções e a deixa na frente.</summary>
    [CommandMethod("PRJABA", CommandFlags.Modal)]
    public static void Aba()
    {
        try
        {
            if (UI.RibbonProjeta.Criar(true)) Ctx.Msg("Aba PROJETA pronta na faixa de opções.");
            else Ctx.Msg("A faixa de opções ainda não está disponível. Ligue-a com RIBBON e rode PRJABA de novo.");
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJABA: {ex.Message}"); }
    }

    /// <summary>PRJPAINEL — mostra o painel lateral com todos os comandos.</summary>
    [CommandMethod("PRJPAINEL", CommandFlags.Modal)]
    public static void Painel()
    {
        if (_painel is null)
        {
            _painel = new PaletteSet("PROJETA • Assistente Civil", new Guid("6F1C1E64-2B1D-4C8E-9A57-6B1E4B9B5A11"))
            {
                MinimumSize = new System.Drawing.Size(250, 400),
                DockEnabled = DockSides.Left | DockSides.Right,
            };
            _painel.Add("Comandos", new PainelProjeta());
        }
        _painel.Visible = true;
    }

    /// <summary>PRJAJUDA — lista os comandos do PROJETA.</summary>
    [CommandMethod("PRJAJUDA", CommandFlags.Modal)]
    public static void Ajuda()
    {
        Ctx.Msg("PROJETA • Assistente Civil — comandos:");
        foreach (var (grupo, itens) in PainelProjeta.Grupos)
        {
            Ctx.Msg($"  [{grupo}]");
            foreach (var (_, cmd, dica) in itens) Ctx.Msg($"    {cmd,-18} {dica}");
        }
        Ctx.Msg("  [Outros]");
        Ctx.Msg("    PRJEXPORTARREDE    Salva uma rede em JSON");
        Ctx.Msg("    PRJIMPORTARREDE    Adiciona uma rede salva em JSON");
        Ctx.Msg("    PRJEXCLUIRREDE     Exclui uma rede e seus desenhos");
        Ctx.Msg("    PRJSECOES          Modelador na aba de seções-tipo");
        Ctx.Msg("    PRJBIBLIOTECA      Abre a pasta da biblioteca do usuário");
        Ctx.Msg("Fluxo típico: PRJREDE → PRJLANCAR → PRJBACIA → PRJCALCULAR → PRJTABELA/PRJRELATORIO → PRJPERFIL.");
        Ctx.Msg("Após mover PVs ou apagar elementos no desenho, rode PRJATUALIZAR.");
    }
}
