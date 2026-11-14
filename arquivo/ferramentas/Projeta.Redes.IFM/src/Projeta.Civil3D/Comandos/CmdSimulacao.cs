using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Integracao;
using Projeta.Core.Redes;
using Projeta.Core.Relatorios;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdSimulacao
{
    private static string PastaTemp(Rede r)
    {
        var p = Path.Combine(Path.GetTempPath(), "Projeta", Texto.IdSeguro(r.Nome, 40));
        Directory.CreateDirectory(p);
        return p;
    }

    private static OpcoesSwmm? PedirOpcoesSwmm(Sessao s)
    {
        var op = s.Projeto.Extras.TryGetValue("swmm", out var j) ? JsonProjeta.Desserializar<OpcoesSwmm>(j) ?? new() : new();
        using var f = new FormPropriedades("PROJETA — IFM — Opções do SWMM", ("SWMM", op));
        if (AcApp.ShowModalDialog(f) != System.Windows.Forms.DialogResult.OK) return null;
        s.Projeto.Extras["swmm"] = JsonProjeta.Serializar(op, false);
        return op;
    }

    /// <summary>Localiza o executável do motor (config → pede ao usuário).</summary>
    private static string? Motor(bool swmm)
    {
        var cfg = Config.Atual;
        var atual = swmm ? cfg.RunSwmm : cfg.RunEpanet;
        if (!string.IsNullOrEmpty(atual) && File.Exists(atual)) return atual;
        Ctx.Msg(swmm
            ? "Indique o executável runswmm.exe (vem com o EPA SWMM 5.2, na pasta de instalação; ou compile do código aberto em github.com/USEPA/Stormwater-Management-Model)."
            : "Indique o executável runepanet.exe (vem com o EPANET 2.2, na pasta de instalação; ou compile do código aberto em github.com/OpenWaterAnalytics/EPANET).");
        var arq = UiUtil.AbrirArquivo(swmm ? "runswmm.exe|runswmm*.exe|Executáveis|*.exe" : "runepanet.exe|runepanet*.exe|Executáveis|*.exe");
        if (arq is null) return null;
        if (swmm) cfg.RunSwmm = arq; else cfg.RunEpanet = arq;
        cfg.Salvar();
        return arq;
    }

    private static Rede? RedeGravidadeCalculada(Sessao s)
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Tipo is TipoRede.Drenagem or TipoRede.Esgoto);
        if (rede is null) return null;
        if (rede.Resultado is null) Operacoes.Atualizar(s, rede, redesenhar: false);
        return rede;
    }

    /// <summary>PRJSWMMEXPORTAR — gera o arquivo .inp do SWMM (rede dimensionada + bacias + chuva de projeto).</summary>
    [CommandMethod("PRJSWMMEXPORTAR", CommandFlags.Modal)]
    public static void SwmmExportar() => Operacoes.Rodar("PRJSWMMEXPORTAR", s =>
    {
        var rede = RedeGravidadeCalculada(s);
        if (rede is null) return;
        var op = PedirOpcoesSwmm(s);
        if (op is null) return;
        var arq = UiUtil.SalvarArquivo("SWMM (*.inp)|*.inp", Texto.IdSeguro(rede.Nome, 60) + ".inp");
        if (arq is null) return;
        File.WriteAllText(arq, SwmmInp.Gerar(rede, s.Projeto, op));
        Ctx.Msg("Arquivo do SWMM salvo em " + arq);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJSWMMCALCULAR — exporta, executa o SWMM e mostra os resultados comparados ao dimensionamento.</summary>
    [CommandMethod("PRJSWMMCALCULAR", CommandFlags.Modal)]
    public static void SwmmCalcular() => Operacoes.Rodar("PRJSWMMCALCULAR", s =>
    {
        var rede = RedeGravidadeCalculada(s);
        if (rede?.Resultado is null) return;
        var op = PedirOpcoesSwmm(s);
        if (op is null) return;
        var exe = Motor(true);
        if (exe is null) return;
        var pasta = PastaTemp(rede);
        var inp = Path.Combine(pasta, "rede.inp"); var rpt = Path.Combine(pasta, "rede.rpt"); var outb = Path.Combine(pasta, "rede.out");
        File.WriteAllText(inp, SwmmInp.Gerar(rede, s.Projeto, op, out var ids));
        Ctx.Msg("Executando o SWMM...");
        var ex = MotorExterno.Executar(exe, inp, rpt, outb);
        if (!File.Exists(rpt)) { Ctx.Msg("[ERRO] O SWMM não gerou relatório. " + ex.Saida + ex.Erro); return; }
        var r = ResultadoSwmm.Ler(File.ReadAllText(rpt));
        var inv = ids.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
        string Nome(string idSwmm, Func<string, string?> porChave) => inv.TryGetValue(idSwmm, out var ch) ? porChave(ch) ?? idSwmm : idSwmm;

        var tl = new TabelaDados { Titulo = "SWMM — trechos (comparação com o dimensionamento)" };
        tl.Colunas.AddRange(new[] { "Trecho", "Seção", "Q dimensionamento (L/s)", "Q máx. SWMM (L/s)", "Hora", "v máx. (m/s)", "Q/Qplena", "y/D máx.", "Situação" });
        foreach (var l in r.Links)
        {
            var t = inv.TryGetValue(l.Nome, out var ch) && ch.StartsWith("T") ? rede.TrechoPorId(ch[1..]) : null;
            var rt = t is null ? null : rede.Resultado.Trecho(t.Id);
            bool cheio = l.MaxPlenaLamina >= 0.999;
            tl.Linhas.Add(new[] { t?.Nome ?? l.Nome, rt?.Secao ?? "", rt is null ? "-" : Num.F(rt.Vazao * 1000, 1), Num.F(l.VazaoMax * 1000, 1), l.HoraMax,
                Num.F(l.VelMax), Num.F(l.MaxPlenaVazao), Num.F(l.MaxPlenaLamina), cheio ? "SOBRECARGA" : "OK" });
            tl.Situacao.Add(cheio ? "ERRO" : l.MaxPlenaLamina > (rede.Tipo == TipoRede.Esgoto ? rede.Esgoto.LaminaMaxima : rede.Drenagem.LaminaMaxima) ? "AVISO" : "OK");
        }
        var tn = new TabelaDados { Titulo = "SWMM — nós" };
        tn.Colunas.AddRange(new[] { "Nó", "Tipo", "Prof. média (m)", "Prof. máx. (m)", "HGL máx.", "Hora", "Alagamento (h)", "Vazão alagada máx. (L/s)" });
        foreach (var n in r.Nos)
        {
            var nome = Nome(n.Nome, ch => ch.StartsWith("N") ? rede.NoPorId(ch[1..])?.Nome : null);
            var al = r.Alagamentos.FirstOrDefault(a => a.Nome == n.Nome);
            tn.Linhas.Add(new[] { nome, n.Tipo, Num.F(n.ProfMedia), Num.F(n.ProfMax), Num.F(n.HglMax, 3), n.HoraMax, al is null ? "-" : Num.F(al.Horas), al is null ? "-" : Num.F(al.VazaoMax * 1000, 1) });
            tn.Situacao.Add(al is null ? "OK" : "ERRO");
        }
        var msgs = new List<string>
        {
            $"Erro de continuidade — escoamento superficial: {Num.F(r.ErroContinuidadeEscoamento)}%; roteamento: {Num.F(r.ErroContinuidadeRoteamento)}%",
            $"{r.Alagamentos.Count} nó(s) com alagamento. Arquivos em {pasta}",
        };
        msgs.AddRange(r.Erros); msgs.AddRange(r.Avisos.Take(20));
        foreach (var m in msgs.Take(4)) Ctx.Msg(m);
        using var f = new FormTabelas($"PROJETA — IFM — Resultados do SWMM — {rede.Nome}", new[] { tl, tn }, msgs, permitirInserir: false);
        AcApp.ShowModalDialog(f);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJSWMMIMPORTAR — cria uma rede a partir de um arquivo .inp do SWMM.</summary>
    [CommandMethod("PRJSWMMIMPORTAR", CommandFlags.Modal)]
    public static void SwmmImportar() => Operacoes.Rodar("PRJSWMMIMPORTAR", s =>
    {
        var arq = UiUtil.AbrirArquivo("SWMM (*.inp)|*.inp");
        if (arq is null) return;
        var tipo = Operacoes.LerTipo(s.PedirPalavra("Tipo da rede", new[] { "Drenagem", "Esgoto" }, "Drenagem"));
        if (tipo is null) return;
        var rede = Operacoes.NovaRede(s.Projeto, Path.GetFileNameWithoutExtension(arq), tipo.Value);
        Ctx.Msgs(SwmmInp.Importar(File.ReadAllText(arq), rede));
        var surf = CivilUtil.EscolherSuperficie(s);
        if (surf is not null) { rede.HandleSuperficie = surf.Handle.ToString(); rede.NomeSuperficie = surf.Name; }
        Operacoes.Atualizar(s, rede, sincronizar: false);
        s.SalvarEConfirmar();
    });

    // ------------------------------------------------------------------ EPANET

    /// <summary>PRJEPANETEXPORTAR — gera o arquivo .inp do EPANET da rede de água.</summary>
    [CommandMethod("PRJEPANETEXPORTAR", CommandFlags.Modal)]
    public static void EpanetExportar() => Operacoes.Rodar("PRJEPANETEXPORTAR", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede de água", r => r.Tipo == TipoRede.Agua);
        if (rede is null) return;
        CivilUtil.AmostrarTerreno(s, rede);
        var arq = UiUtil.SalvarArquivo("EPANET (*.inp)|*.inp", Texto.IdSeguro(rede.Nome, 60) + ".inp");
        if (arq is null) return;
        File.WriteAllText(arq, EpanetInp.Gerar(rede, out _));
        Ctx.Msg("Arquivo do EPANET salvo em " + arq);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJEPANETCALCULAR — executa o EPANET (com ajuste de diâmetros por velocidade, se ativado) e mostra pressões e vazões.</summary>
    [CommandMethod("PRJEPANETCALCULAR", CommandFlags.Modal)]
    public static void EpanetCalcular() => Operacoes.Rodar("PRJEPANETCALCULAR", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede de água", r => r.Tipo == TipoRede.Agua);
        if (rede is null) return;
        if (!rede.Nos.Any(n => n.Tipo is TipoNo.Reservatorio or TipoNo.Tanque)) { Ctx.Msg("[ERRO] A rede precisa de ao menos um reservatório ou tanque (PRJNO → Reservatorio)."); return; }
        var exe = Motor(false);
        if (exe is null) return;
        Ctx.Msgs(Desenho.DesenhoRede.Sincronizar(s, rede), false);
        CivilUtil.AmostrarTerreno(s, rede);
        var pasta = PastaTemp(rede);
        var inp = Path.Combine(pasta, "rede.inp"); var rpt = Path.Combine(pasta, "rede.rpt");
        ResultadoEpanet? r = null;
        Dictionary<string, string> ids = new();
        int alterados = 0;
        for (int it = 0; it < 12; it++)
        {
            File.WriteAllText(inp, EpanetInp.Gerar(rede, out ids));
            MotorExterno.Executar(exe, inp, rpt);
            r = ResultadoEpanet.Ler(File.ReadAllText(rpt));
            if (r.Erros.Count > 0 || !rede.Agua.DimensionarPorVelocidade) break;
            int k = r.AjustarDiametros(rede, ids);
            alterados += k;
            if (k == 0) break;
        }
        if (r is null) return;
        if (alterados > 0) Ctx.Msg($"{alterados} ajuste(s) de diâmetro por velocidade.");
        var inv = ids.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
        var p = rede.Agua;
        var tn = new TabelaDados { Titulo = "EPANET — nós" };
        tn.Colunas.AddRange(new[] { "Nó", "Cota (m)", "Demanda (L/s)", "Carga (m)", "Pressão (mca)", "Situação" });
        foreach (var n in r.Nos)
        {
            var no = inv.TryGetValue(n.Nome, out var ch) ? rede.NoPorId(ch[1..]) : null;
            bool res = no?.Tipo is TipoNo.Reservatorio or TipoNo.Tanque;
            string sit = res ? "OK" : n.Pressao < p.PressaoMinima ? "PRESSÃO BAIXA" : n.Pressao > p.PressaoMaxima ? "PRESSÃO ALTA" : "OK";
            tn.Linhas.Add(new[] { no?.Nome ?? n.Nome, no is null ? "-" : Num.F(no.CotaTampa, 3), Num.F(n.Demanda, 3), Num.F(n.Carga, 2), Num.F(n.Pressao, 2), sit });
            tn.Situacao.Add(sit == "OK" ? "OK" : "AVISO");
        }
        var tl = new TabelaDados { Titulo = "EPANET — tubos" };
        tl.Colunas.AddRange(new[] { "Tubo", "DN (mm)", "L (m)", "Vazão (L/s)", "Velocidade (m/s)", "Perda (m/km)", "Situação" });
        foreach (var l in r.Links)
        {
            var t = inv.TryGetValue(l.Nome, out var ch) ? rede.TrechoPorId(ch[1..]) : null;
            string sit = Math.Abs(l.Velocidade) > p.VelocidadeMaxima ? "VELOCIDADE ALTA" : "OK";
            tl.Linhas.Add(new[] { t?.Nome ?? l.Nome, t is null ? "-" : ((t.DiametroForcado ?? p.DiametroPadrao) * 1000).ToString("0"), t is null ? "-" : Num.F(rede.ComprimentoHorizontal(t)),
                Num.F(l.Vazao, 3), Num.F(l.Velocidade), Num.F(l.PerdaUnitaria, 3), sit });
            tl.Situacao.Add(sit == "OK" ? "OK" : "AVISO");
        }
        var msgs = new List<string> { $"Demanda total distribuída: {Num.F(EpanetInp.Demandas(rede).Values.Sum(), 2)} L/s. Arquivos em {pasta}" };
        msgs.AddRange(r.Erros); msgs.AddRange(r.Avisos);
        foreach (var m in msgs.Take(4)) Ctx.Msg(m);
        Desenho.DesenhoRede.Redesenhar(s, rede);
        using var f = new FormTabelas($"PROJETA — IFM — Resultados do EPANET — {rede.Nome}", new[] { tn, tl }, msgs, permitirInserir: false);
        AcApp.ShowModalDialog(f);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJEPANETIMPORTAR — cria uma rede de água a partir de um arquivo .inp do EPANET.</summary>
    [CommandMethod("PRJEPANETIMPORTAR", CommandFlags.Modal)]
    public static void EpanetImportar() => Operacoes.Rodar("PRJEPANETIMPORTAR", s =>
    {
        var arq = UiUtil.AbrirArquivo("EPANET (*.inp)|*.inp");
        if (arq is null) return;
        var rede = Operacoes.NovaRede(s.Projeto, Path.GetFileNameWithoutExtension(arq), TipoRede.Agua);
        Ctx.Msgs(EpanetInp.Importar(File.ReadAllText(arq), rede));
        Operacoes.Atualizar(s, rede, sincronizar: false, calcular: false);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJPOPULACAO — projeção populacional e aplicação na rede de esgoto/água.</summary>
    [CommandMethod("PRJPOPULACAO", CommandFlags.Modal)]
    public static void Populacao() => Operacoes.Rodar("PRJPOPULACAO", s =>
    {
        using var f = new FormPopulacao();
        if (AcApp.ShowModalDialog(f) != System.Windows.Forms.DialogResult.OK || f.PopulacaoEscolhida is null) return;
        var rede = s.EscolherRede("Rede que receberá a população", r => r.Tipo is TipoRede.Esgoto or TipoRede.Agua);
        if (rede is null) return;
        double pop = Math.Round(f.PopulacaoEscolhida.Value);
        if (rede.Tipo == TipoRede.Agua) rede.Agua.PopulacaoFinal = pop;
        else
        {
            var qual = s.PedirPalavra("Aplicar como população", new[] { "Final", "Inicial" }, "Final");
            if (qual == "Inicial") rede.Esgoto.PopulacaoInicial = pop; else if (qual == "Final") rede.Esgoto.PopulacaoFinal = pop; else return;
        }
        s.SalvarEConfirmar();
        Ctx.Msg($"População {pop:0} aplicada à rede {rede.Nome}. Recalcule a rede.");
    });

    /// <summary>PRJMOTORES — configura os caminhos dos executáveis do SWMM e do EPANET.</summary>
    [CommandMethod("PRJMOTORES", CommandFlags.Modal)]
    public static void Motores() => Operacoes.Rodar("PRJMOTORES", s =>
    {
        var cfg = Config.Atual;
        Ctx.Msg($"runswmm: {cfg.RunSwmm ?? "(não configurado)"}");
        Ctx.Msg($"runepanet: {cfg.RunEpanet ?? "(não configurado)"}");
        var q = s.PedirPalavra("Configurar", new[] { "Swmm", "Epanet", "Nada" }, "Nada");
        if (q == "Swmm") { cfg.RunSwmm = null; Motor(true); }
        else if (q == "Epanet") { cfg.RunEpanet = null; Motor(false); }
    });
}
