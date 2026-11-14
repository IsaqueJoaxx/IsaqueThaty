using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Redes;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdCalculo
{
    /// <summary>PRJCALCULAR — dimensiona a rede e abre a planilha dinâmica (editável).</summary>
    [CommandMethod("PRJCALCULAR", CommandFlags.Modal)]
    public static void Calcular() => Operacoes.Rodar("PRJCALCULAR", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Tipo != TipoRede.Agua);
        if (rede is null) return;
        Operacoes.Atualizar(s, rede, redesenhar: false);
        if (rede.Resultado is null) { Ctx.Msg("Nada a calcular."); return; }
        using var f = new FormPlanilha(rede, s.Projeto);
        if (AcApp.ShowModalDialog(f) != System.Windows.Forms.DialogResult.OK) { Ctx.Msg("Planilha fechada sem aplicar."); return; }
        var erros = DesenhoRede.Redesenhar(s, rede);
        foreach (var e in erros.Take(10)) Ctx.Msg("[DESENHO] " + e);
        s.SalvarEConfirmar();
        Ctx.Msg("Resultados aplicados ao desenho.");
    });

    /// <summary>PRJATUALIZAR — lê alterações do desenho, amostra o terreno, recalcula e redesenha.</summary>
    [CommandMethod("PRJATUALIZAR", CommandFlags.Modal)]
    public static void Atualizar() => Operacoes.Rodar("PRJATUALIZAR", s =>
    {
        if (s.Projeto.Redes.Count == 0) { Ctx.Msg("Nenhuma rede no desenho."); return; }
        List<Rede> redes;
        if (s.Projeto.Redes.Count > 1 && s.PedirPalavra("Atualizar", new[] { "Uma", "Todas" }, "Todas") == "Todas") redes = s.Projeto.Redes.ToList();
        else { var r = s.EscolherRede(); if (r is null) return; redes = new() { r }; }
        foreach (var r in redes)
        {
            Ctx.Msg($"--- {r.Nome} ---");
            Operacoes.Atualizar(s, r);
        }
        s.SalvarEConfirmar();
    });

    /// <summary>PRJNUMERAR — renumera nós (por coletor, de montante para jusante) e trechos.</summary>
    [CommandMethod("PRJNUMERAR", CommandFlags.Modal)]
    public static void Numerar() => Operacoes.Rodar("PRJNUMERAR", s =>
    {
        var rede = s.EscolherRede();
        if (rede is null) return;
        var ini = s.PedirNumero("Número inicial", 1);
        if (ini is null) return;
        var modo = s.PedirPalavra("Nome dos trechos", new[] { "Sequencial", "PeloNos" }, "Sequencial");
        if (modo is null) return;
        Numeracao.Numerar(rede, (int)ini.Value, 1, modo == "PeloNos");
        Operacoes.Atualizar(s, rede);
        s.SalvarEConfirmar();
        Ctx.Msg("Rede renumerada.");
    });
}
