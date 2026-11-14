using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Longitudinais;
using Projeta.Core.Relatorios;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdLongitudinal
{
    private static bool Configurar(Sessao s, DispositivoLongitudinal d, Alignment al)
    {
        var perfis = CivilUtil.Perfis(s.Tr, al).Select(p => (p.Id.Handle.ToString(), p.Nome)).ToList();
        var superficies = CivilUtil.Superficies(s.Tr).Select(p => (p.Id.Handle.ToString(), p.Nome)).ToList();
        var equacoes = s.Projeto.TodasEquacoes().Select(e => (e.Id, e.Nome)).ToList();
        if (perfis.Count == 0 && d.OrigemCota == OrigemCota.Perfil) d.OrigemCota = OrigemCota.Superficie;
        using var f = new FormLongitudinal(d, Sessao.Bib, perfis, superficies, equacoes);
        return AcApp.ShowModalDialog(f) == System.Windows.Forms.DialogResult.OK;
    }

    private static void Gerar(Sessao s, DispositivoLongitudinal d)
    {
        var erros = DesenhoLongitudinal.Gerar(s, d);
        foreach (var e in erros.Take(10)) Ctx.Msg("[AVISO] " + e);
        var r = d.Resultado;
        if (r is null) return;
        Ctx.Msgs(r.Mensagens.Where(m => m.Gravidade != Gravidade.Info));
        Ctx.Msg($"{d.Nome}: L = {Num.F(r.Comprimento3D)} m; concreto {Num.F(r.VolumeConcreto, 3)} m³; escavação {Num.F(r.VolumeEscavacao, 3)} m³; " +
                $"I = {Num.F(r.DeclividadeMinima * 100, 2)}% a {Num.F(r.DeclividadeMaxima * 100, 2)}%" +
                (double.IsNaN(r.CapacidadeMinima) ? "" : $"; Qcap mín. {Num.F(r.CapacidadeMinima * 1000, 1)} L/s; L crítico {Num.F(r.ComprimentoCriticoMinimo, 1)} m"));
    }

    /// <summary>PRJLONGITUDINAL — lança valeta, sarjeta, meio-fio ou canaleta ao longo de um alinhamento.</summary>
    [CommandMethod("PRJLONGITUDINAL", CommandFlags.Modal)]
    public static void Criar() => Operacoes.Rodar("PRJLONGITUDINAL", s =>
    {
        var al = CivilUtil.EscolherAlinhamento(s, "Selecione o alinhamento", false);
        if (al is null) return;
        var nome = s.Projeto.Longitudinais.Count == 0 ? "VL-01" : $"VL-{s.Projeto.Longitudinais.Count + 1:00}";
        var d = new DispositivoLongitudinal
        {
            Nome = nome, HandleAlinhamento = al.Handle.ToString(), NomeAlinhamento = al.Name,
            EstacaInicial = al.StartingStation, EstacaFinal = al.EndingStation,
        };
        var perfis = CivilUtil.Perfis(s.Tr, al);
        if (perfis.Count > 0) { d.HandlePerfil = perfis[0].Id.Handle.ToString(); d.NomePerfil = perfis[0].Nome; }
        var ultimo = s.Projeto.Longitudinais.LastOrDefault();
        if (ultimo is not null) { d.SecaoId = ultimo.SecaoId; d.Parametros = new(ultimo.Parametros, StringComparer.OrdinalIgnoreCase); d.Afastamento = ultimo.Afastamento; d.Espelhar = ultimo.Espelhar; }
        if (!Configurar(s, d, al)) return;
        s.Projeto.Longitudinais.Add(d);
        Gerar(s, d);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJLONGEDITAR — edita e regenera um dispositivo longitudinal.</summary>
    [CommandMethod("PRJLONGEDITAR", CommandFlags.Modal)]
    public static void Editar() => Operacoes.Rodar("PRJLONGEDITAR", s =>
    {
        var it = s.PedirItem("Selecione o dispositivo longitudinal");
        if (it?.Longitudinal is null) { Ctx.Msg("Selecione um dispositivo longitudinal (sólido, contorno ou rótulo)."); return; }
        var d = it.Longitudinal;
        var al = Ctx.Abrir<Alignment>(s.Tr, s.Db, d.HandleAlinhamento);
        if (al is null) { Ctx.Msg("O alinhamento original não existe mais."); return; }
        if (!Configurar(s, d, al)) return;
        Gerar(s, d);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJLONGQUANT — quadro de quantitativos e verificação hidráulica dos dispositivos longitudinais.</summary>
    [CommandMethod("PRJLONGQUANT", CommandFlags.Modal)]
    public static void Quantitativos() => Operacoes.Rodar("PRJLONGQUANT", s =>
    {
        var lista = s.Projeto.Longitudinais.Where(d => d.Resultado is not null).ToList();
        if (lista.Count == 0) { Ctx.Msg("Nenhum dispositivo longitudinal no desenho."); return; }
        var t = new TabelaDados { Titulo = "Dispositivos longitudinais — quantitativos e capacidade" };
        t.Colunas.AddRange(new[] { "Dispositivo", "Seção", "Alinhamento", "Estaca inicial", "Estaca final", "Afast. (m)", "L (m)", "Concreto (m³)", "Escavação (m³)", "Área revestida (m²)", "I mín. (%)", "I máx. (%)", "Q cap. mín. (L/s)", "L crítico (m)", "Observações" });
        foreach (var d in lista)
        {
            var r = d.Resultado!;
            var nomeSec = Sessao.Bib.Secoes.TryGetValue(d.SecaoId, out var sec) ? sec.Nome : d.SecaoId;
            t.Linhas.Add(new[] { d.Nome, nomeSec, d.NomeAlinhamento ?? "", Num.Estaca(d.EstacaInicial), Num.Estaca(d.EstacaFinal), Num.F(d.Afastamento),
                Num.F(r.Comprimento3D), Num.F(r.VolumeConcreto, 3), Num.F(r.VolumeEscavacao, 3), Num.F(r.AreaRevestida, 2),
                Num.F(r.DeclividadeMinima * 100), Num.F(r.DeclividadeMaxima * 100), double.IsNaN(r.CapacidadeMinima) ? "-" : Num.F(r.CapacidadeMinima * 1000, 1),
                double.IsNaN(r.ComprimentoCriticoMinimo) ? "-" : Num.F(r.ComprimentoCriticoMinimo, 1), string.Join("; ", r.Mensagens.Where(m => m.Gravidade != Gravidade.Info).Select(m => m.Texto)) });
            t.Situacao.Add(r.Mensagens.Any(m => m.Gravidade == Gravidade.Aviso) ? "AVISO" : "OK");
        }
        t.Linhas.Add(new[] { "TOTAL", "", "", "", "", "", Num.F(lista.Sum(d => d.Resultado!.Comprimento3D)), Num.F(lista.Sum(d => d.Resultado!.VolumeConcreto), 3),
            Num.F(lista.Sum(d => d.Resultado!.VolumeEscavacao), 3), Num.F(lista.Sum(d => d.Resultado!.AreaRevestida), 2), "", "", "", "", "" });
        t.Situacao.Add("OK");
        using var f = new FormTabelas("PROJETA — IFM — Dispositivos longitudinais", new[] { t });
        if (AcApp.ShowModalDialog(f) == System.Windows.Forms.DialogResult.Yes)
        {
            var p = s.Ed.GetPoint("\nPonto de inserção da tabela: ");
            if (p.Status == PromptStatus.OK) { TabelasDesenho.Inserir(s, t, p.Value, 0.8, DesenhoLongitudinal.LayerTexto); s.Tr.Commit(); }
        }
    });
}
