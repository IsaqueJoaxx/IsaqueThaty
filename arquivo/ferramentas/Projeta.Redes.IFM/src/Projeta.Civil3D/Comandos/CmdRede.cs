using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Redes;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdRede
{
    /// <summary>PRJREDE — cria uma rede (drenagem, esgoto, água ou genérica).</summary>
    [CommandMethod("PRJREDE", CommandFlags.Modal)]
    public static void CriarRede() => Operacoes.Rodar("PRJREDE", s =>
    {
        var tipo = Operacoes.LerTipo(s.PedirPalavra("Tipo de rede", new[] { "Drenagem", "Esgoto", "Agua", "Generica" }, "Drenagem"));
        if (tipo is null) return;
        string sugestao = tipo switch { TipoRede.Esgoto => "Esgoto", TipoRede.Agua => "Agua", TipoRede.Generica => "Rede", _ => "Drenagem" } + $" {s.Projeto.Redes.Count + 1}";
        var nome = s.PedirTexto("Nome da rede", sugestao);
        if (nome is null) return;
        var rede = Operacoes.NovaRede(s.Projeto, nome, tipo.Value);

        var surf = CivilUtil.EscolherSuperficie(s);
        if (surf is not null) { rede.HandleSuperficie = surf.Handle.ToString(); rede.NomeSuperficie = surf.Name; }
        var al = CivilUtil.EscolherAlinhamento(s, "Alinhamento de referência (estacas)");
        if (al is not null) { rede.HandleAlinhamento = al.Handle.ToString(); rede.NomeAlinhamento = al.Name; }

        if (!EditarOpcoes(s, rede)) { Ctx.Msg("*Cancelado*"); return; }
        s.SalvarEConfirmar();
        Ctx.Msg($"Rede '{rede.Nome}' ({rede.Tipo}) criada. Use PRJLANCAR para lançar os nós e tubos.");
    });

    internal static bool EditarOpcoes(Sessao s, Rede rede)
    {
        var abas = new List<(string, object)> { ("Rede", rede) };
        switch (rede.Tipo)
        {
            case TipoRede.Esgoto: abas.Add(("Esgoto (NBR 9649)", rede.Esgoto)); break;
            case TipoRede.Agua: abas.Add(("Água (EPANET)", rede.Agua)); break;
            default: abas.Add(("Drenagem", rede.Drenagem)); break;
        }
        using var f = new FormPropriedades($"PROJETA — IFM — Opções da rede {rede.Nome}", abas.ToArray());
        return AcApp.ShowModalDialog(f) == System.Windows.Forms.DialogResult.OK;
    }

    /// <summary>PRJOPCOES — parâmetros de cálculo e desenho da rede; troca de superfície/alinhamento.</summary>
    [CommandMethod("PRJOPCOES", CommandFlags.Modal)]
    public static void Opcoes() => Operacoes.Rodar("PRJOPCOES", s =>
    {
        var rede = s.EscolherRede();
        if (rede is null) return;
        while (true)
        {
            var op = s.PedirPalavra($"Superfície: {rede.NomeSuperficie ?? "-"}; alinhamento: {rede.NomeAlinhamento ?? "-"}. Alterar", new[] { "Superficie", "Alinhamento", "Parametros" }, "Parametros");
            if (op is null) return;
            if (op == "Superficie")
            {
                var surf = CivilUtil.EscolherSuperficie(s);
                rede.HandleSuperficie = surf?.Handle.ToString(); rede.NomeSuperficie = surf?.Name;
                continue;
            }
            if (op == "Alinhamento")
            {
                var al = CivilUtil.EscolherAlinhamento(s, "Alinhamento de referência");
                rede.HandleAlinhamento = al?.Handle.ToString(); rede.NomeAlinhamento = al?.Name;
                continue;
            }
            break;
        }
        if (!EditarOpcoes(s, rede)) return;
        if (s.PedirPalavra("Recalcular e redesenhar agora", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            Operacoes.Atualizar(s, rede);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJEQUACOES — catálogo de equações IDF e equações próprias do projeto.</summary>
    [CommandMethod("PRJEQUACOES", CommandFlags.Modal)]
    public static void Equacoes() => Operacoes.Rodar("PRJEQUACOES", s =>
    {
        var drenagens = s.Projeto.Redes.Where(r => r.Tipo == TipoRede.Drenagem).ToList();
        using var f = new FormEquacoes(s.Projeto, drenagens.FirstOrDefault()?.Drenagem.EquacaoId);
        var r = AcApp.ShowModalDialog(f);
        if (r == System.Windows.Forms.DialogResult.OK && f.EquacaoEscolhida is not null && drenagens.Count > 0)
        {
            var rede = s.EscolherRede("Rede que usará a equação", x => x.Tipo == TipoRede.Drenagem);
            if (rede is not null)
            {
                rede.Drenagem.EquacaoId = f.EquacaoEscolhida;
                Ctx.Msg($"Rede {rede.Nome} usará a equação {s.Projeto.Equacao(f.EquacaoEscolhida)?.Nome}. Rode PRJCALCULAR.");
            }
        }
        s.SalvarEConfirmar();
    });

    /// <summary>PRJEXPORTARREDE — salva uma rede em JSON (para levar a outro desenho).</summary>
    [CommandMethod("PRJEXPORTARREDE", CommandFlags.Modal)]
    public static void ExportarRede() => Operacoes.Rodar("PRJEXPORTARREDE", s =>
    {
        var rede = s.EscolherRede();
        if (rede is null) return;
        var arq = UiUtil.SalvarArquivo("Rede PROJETA (*.json)|*.json", Texto.IdSeguro(rede.Nome, 60) + ".json");
        if (arq is null) return;
        File.WriteAllText(arq, JsonProjeta.Serializar(rede));
        Ctx.Msg("Rede salva em " + arq);
    });

    /// <summary>PRJIMPORTARREDE — adiciona uma rede salva em JSON e a desenha.</summary>
    [CommandMethod("PRJIMPORTARREDE", CommandFlags.Modal)]
    public static void ImportarRede() => Operacoes.Rodar("PRJIMPORTARREDE", s =>
    {
        var arq = UiUtil.AbrirArquivo("Rede PROJETA (*.json)|*.json");
        if (arq is null) return;
        var rede = JsonProjeta.Desserializar<Rede>(File.ReadAllText(arq)) ?? throw new System.Exception("Arquivo inválido");
        rede.Id = Projeto.NovoId();
        foreach (var n in rede.Nos) { n.HandleBloco = n.HandleRotulo = n.HandleSolido = null; }
        foreach (var t in rede.Trechos) { t.HandleLinha = t.HandleRotulo = t.HandleSolido = null; }
        foreach (var b in rede.Bacias) { b.HandleContorno = b.HandleRotulo = b.HandleTalvegue = null; }
        rede.HandlesAuxiliares.Clear();
        rede.HandleSuperficie = rede.HandleAlinhamento = null;
        if (s.Projeto.Redes.Any(r => string.Equals(r.Nome, rede.Nome, StringComparison.OrdinalIgnoreCase))) rede.Nome += " (importada)";
        s.Projeto.Redes.Add(rede);
        var surf = CivilUtil.EscolherSuperficie(s);
        if (surf is not null) { rede.HandleSuperficie = surf.Handle.ToString(); rede.NomeSuperficie = surf.Name; }
        Operacoes.Atualizar(s, rede, sincronizar: false);
        s.SalvarEConfirmar();
        Ctx.Msg($"Rede '{rede.Nome}' importada.");
    });

    /// <summary>PRJEXCLUIRREDE — remove uma rede inteira (modelo e desenho).</summary>
    [CommandMethod("PRJEXCLUIRREDE", CommandFlags.Modal)]
    public static void ExcluirRede() => Operacoes.Rodar("PRJEXCLUIRREDE", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede a excluir");
        if (rede is null) return;
        if (s.PedirPalavra($"Excluir a rede '{rede.Nome}' e seus desenhos", new[] { "Sim", "Nao" }, "Nao") != "Sim") return;
        Desenho.DesenhoRede.ApagarGraficos(s, rede);
        foreach (var lista in rede.HandlesAuxiliares.Values) foreach (var h in lista) Ctx.Apagar(s.Tr, s.Db, h);
        s.Projeto.Redes.Remove(rede);
        s.SalvarEConfirmar();
        Ctx.Msg("Rede excluída (os contornos das bacias foram mantidos).");
    });
}
