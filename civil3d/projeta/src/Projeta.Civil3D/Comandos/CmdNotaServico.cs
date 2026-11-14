using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJNOTASERVICO — nota de serviço de pavimentação a partir de um corredor: uma linha a cada 10 m (configurável),
/// nos pontos notáveis do eixo (PC/PT, TS/SC/CS/ST) e do greide (BVC/EVC) e nas mudanças de região, com
/// cota de projeto/terreno/vermelha e afastamento, cota e inclinação dos pontos da seção (bordo, drenagem,
/// meio-fio, passeio) dos dois lados. Grava .xlsx no layout E-NS. Não altera o desenho.
/// </summary>
public sealed class CmdNotaServico
{
    [CommandMethod("PRJNOTASERVICO", CommandFlags.Modal)]
    public static void Gerar()
    {
        try
        {
            using var s = Sessao.Abrir();
            var cor = EscolherCorredor(s);
            if (cor is null) return;
            var bls = NotaServicoCivil.Baselines(cor);
            if (bls.Count == 0) { Ctx.Msg($"O corredor {cor.Name} não tem baseline por alinhamento."); return; }
            Baseline bl;
            if (bls.Count == 1) bl = bls[0];
            else
            {
                int i = s.EscolherDaLista("Baselines", bls.Select(b => NotaServicoCivil.NomeBaseline(s.Tr, b)).ToList());
                if (i < 0) return;
                bl = bls[i];
            }
            var al = (Alignment)s.Tr.GetObject(bl.AlignmentId, OpenMode.ForRead);
            var regioes = NotaServicoCivil.Regioes(bl);
            double bIni = regioes.Count > 0 ? regioes.Min(r => r.Inicio) : al.StartingStation;
            double bFim = regioes.Count > 0 ? regioes.Max(r => r.Fim) : al.EndingStation;

            var terreno = EscolherTerreno(s, cor);
            var cfg = ConfigNotaArquivo.Carregar();
            double? ini = null, fim = null;

            Ctx.Msg($"Lendo os códigos de ponto do corredor {cor.Name}...");
            var codigos = NotaServicoCivil.CodigosDaBaseline(bl);
            while (true)
            {
                Ctx.Msg($"--- Nota de serviço: {al.Name}, {NotaServico.FormatarEstaca(ini ?? bIni)} a {NotaServico.FormatarEstaca(fim ?? bFim)}, " +
                        $"linhas a cada {cfg.Intervalo.ToString("0.###", Num.Br)} m ---");
                Ctx.Msg("Códigos no corredor: " + (codigos.Count == 0 ? "(nenhum — o corredor foi construído?)" : string.Join(", ", codigos.Keys)));
                string Achados(IEnumerable<string> padroes)
                {
                    var l = codigos.Keys.Where(k => padroes.Any(p => ConfigNotaServico.Casa(k, p))).ToList();
                    return l.Count == 0 ? "" : string.Join(", ", l.Take(6)) + (l.Count > 6 ? $" (+{l.Count - 6})" : "");
                }
                foreach (var e in cfg.Elementos)
                {
                    var achado = e.Codigos.Select(c => Achados(new[] { c })).FirstOrDefault(a => a.Length > 0);
                    Ctx.Msg($"  {e.Nome,-14} ← {string.Join(" | ", e.Codigos)}  {(achado is null ? "[NÃO ENCONTRADO]" : $"[usa {achado}]")}");
                }
                if (cfg.Terraplenagem)
                {
                    var ab = Achados(cfg.CodigosBanqueta);
                    var ao = Achados(cfg.CodigosOffset);
                    Ctx.Msg($"  {"Banquetas",-14} ← {string.Join(" | ", cfg.CodigosBanqueta)}  {(ab.Length == 0 ? "[nenhuma nas seções de amostra]" : $"[usa {ab}]")}");
                    Ctx.Msg($"  {"Offset",-14} ← {string.Join(" | ", cfg.CodigosOffset)}  " +
                            (ao.Length > 0 ? $"[usa {ao}]" : cfg.OffsetPeloTerreno ? "[pelo terreno: ponto externo sobre a superfície]" : "[NÃO ENCONTRADO]"));
                }
                else Ctx.Msg("  Terraplenagem (banquetas e offset): desligada");
                var op = s.PedirPalavra("Opção", new[] { "Gerar", "Codigos", "Intervalo", "Faixa", "Terraplenagem", "Sair" }, "Gerar");
                if (op is null || op == "Sair") return;
                if (op == "Gerar") break;
                if (op == "Intervalo")
                {
                    var v = s.PedirNumero("Intervalo entre linhas (m)", cfg.Intervalo);
                    if (v is > 0) cfg.Intervalo = v.Value;
                }
                else if (op == "Terraplenagem")
                {
                    var t = s.PedirPalavra("Incluir banquetas e offset", new[] { "Sim", "Nao" }, cfg.Terraplenagem ? "Sim" : "Nao");
                    if (t is not null) cfg.Terraplenagem = t == "Sim";
                }
                else if (op == "Faixa")
                {
                    var a = s.PedirNumero($"Estaca inicial em metros ({NotaServico.FormatarEstaca(bIni)} = {bIni.ToString("0.###", Num.Br)})", ini ?? bIni);
                    if (a is null) continue;
                    var b = s.PedirNumero($"Estaca final em metros ({NotaServico.FormatarEstaca(bFim)} = {bFim.ToString("0.###", Num.Br)})", fim ?? bFim);
                    if (b is null) continue;
                    if (b <= a) { Ctx.Msg("A estaca final deve ser maior que a inicial."); continue; }
                    ini = a; fim = b;
                }
                else if (op == "Codigos")
                {
                    // lista numerada: aceita o nome do código (com * e ?) ou o número dele na lista
                    var nomes = codigos.Keys.ToList();
                    Ctx.Msg("Códigos no corredor (digite o nome ou o número; Enter mantém o atual):");
                    for (int i = 0; i < nomes.Count; i += 4)
                        Ctx.Msg("  " + string.Join("", nomes.Skip(i).Take(4).Select((c, j) => $"{i + j + 1,3}) {c,-24}")));
                    List<string>? Ler(string pergunta, List<string> atual)
                    {
                        var t = s.PedirTexto(pergunta + " (separe com ;)", string.Join(";", atual));
                        if (t is null) return null;
                        var l = Lista(t).Select(x => int.TryParse(x, out var k) && k >= 1 && k <= nomes.Count ? nomes[k - 1] : x).ToList();
                        if (l.Count == 0 || l.SequenceEqual(atual)) return atual;
                        if (!nomes.Any(c => l.Any(p => ConfigNotaServico.Casa(c, p))))
                        {
                            Ctx.Msg($"  Nenhum ponto do corredor tem o código {string.Join(" / ", l)}: mantido {string.Join(" | ", atual)}.");
                            return atual;
                        }
                        return l;
                    }
                    foreach (var e in cfg.Elementos)
                    {
                        var l = Ler($"Códigos para '{e.Nome}' (em ordem de preferência)", e.Codigos);
                        if (l is null) break;
                        e.Codigos = l;
                    }
                    if (cfg.Terraplenagem)
                    {
                        if (Ler("Códigos das banquetas (aceita * e ?)", cfg.CodigosBanqueta) is { } lb) cfg.CodigosBanqueta = lb;
                        if (Ler("Códigos do offset (aceita * e ?)", cfg.CodigosOffset) is { } lo) cfg.CodigosOffset = lo;
                    }
                }
            }

            var nomeArq = Texto.IdSeguro(al.Name, 60) + "_nota_servico.xlsx";
            var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", nomeArq);
            if (arq is null) return;

            Ctx.Msg("Montando a nota de serviço...");
            var lida = NotaServicoCivil.Montar(s.Tr, cor, bl, terreno, cfg, ini, fim, s.Doc.Name);
            lida.Nota.Salvar(arq);
            ConfigNotaArquivo.Salvar(cfg);

            var ns = lida.Nota;
            Ctx.Msg($"Nota de serviço gravada: {arq}");
            Ctx.Msg($"  {ns.Linhas.Count} linhas de {ns.Estaca(ns.EstacaInicial)} a {ns.Estaca(ns.EstacaFinal)}; {lida.SecoesLidas} seções do corredor lidas" +
                    (lida.Interpoladas > 0 ? $"; {lida.Interpoladas} estaca(s) interpoladas entre seções (lista na aba Origem)." : "."));
            if (cfg.Terraplenagem)
                Ctx.Msg($"  Terraplenagem: {lida.LinhasComOffset} linha(s) com offset, {lida.LinhasComBanquetas} com banquetas.");
            foreach (var a in ns.Avisos) Ctx.Msg("  [AVISO] " + a);
            var abrir = s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim");
            if (abrir == "Sim") UiUtil.AbrirNoSistema(arq);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJNOTASERVICO: " + ex.Message); }
    }

    private static List<string> Lista(string t) => t.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static Corridor? EscolherCorredor(Sessao s)
    {
        var lista = NotaServicoCivil.Corredores(s.Tr);
        if (lista.Count == 0) { Ctx.Msg("O desenho não tem corredores."); return null; }
        if (lista.Count == 1)
        {
            Ctx.Msg($"Corredor: {lista[0].Nome}");
            return (Corridor)s.Tr.GetObject(lista[0].Id, OpenMode.ForRead);
        }
        var peo = new PromptEntityOptions("\nSelecione o corredor ou [Lista]: ", "Lista");
        peo.SetRejectMessage("\nIsso não é um corredor.");
        peo.AddAllowedClass(typeof(Corridor), true);
        peo.AllowNone = true;
        var r = s.Ed.GetEntity(peo);
        if (r.Status == PromptStatus.OK) return (Corridor)s.Tr.GetObject(r.ObjectId, OpenMode.ForRead);
        if (r.Status != PromptStatus.Keyword && r.Status != PromptStatus.None) return null;
        int i = s.EscolherDaLista("Corredores", lista.Select(x => x.Nome).ToList());
        return i < 0 ? null : (Corridor)s.Tr.GetObject(lista[i].Id, OpenMode.ForRead);
    }

    private static Surface? EscolherTerreno(Sessao s, Corridor cor)
    {
        var lista = CivilUtil.Superficies(s.Tr);
        if (lista.Count == 0) { Ctx.Msg("Sem superfícies: a cota do terreno fica em branco."); return null; }
        var nomes = lista.Select(x => x.Nome).ToList();
        int padrao = NotaServicoCivil.SuperficiePadrao(nomes, NotaServicoCivil.SuperficiesDoCorredor(cor));
        nomes.Add("(nenhuma)");
        int i = s.EscolherDaLista("Superfície do terreno (cota do terreno)", nomes, Math.Max(padrao, 0));
        if (i < 0 || i >= lista.Count) return null;
        return (Surface)s.Tr.GetObject(lista[i].Id, OpenMode.ForRead);
    }
}

/// <summary>Configuração da nota de serviço em %APPDATA%\Projeta\nota_servico.json (vale para os próximos desenhos).</summary>
internal static class ConfigNotaArquivo
{
    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "nota_servico.json");

    public static ConfigNotaServico Carregar()
    {
        try
        {
            if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigNotaServico>(File.ReadAllText(Arquivo)) is { } c)
            {
                c.Migrar();
                c.Validar();
                return c;
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[AVISO] Configuração da nota de serviço ignorada ({ex.Message}); usando a padrão."); }
        return new ConfigNotaServico();
    }

    public static void Salvar(ConfigNotaServico c)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!);
            File.WriteAllText(Arquivo, JsonProjeta.Serializar(c));
        }
        catch { }
    }
}
