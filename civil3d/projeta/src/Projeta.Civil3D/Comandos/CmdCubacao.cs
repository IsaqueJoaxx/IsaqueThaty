using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Terraplenagem;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJCUBACAO — volumes por seções (nota de cubação) a partir de um grupo de linhas de amostragem do Civil 3D: pela lista
/// de materiais do Civil ou pelas áreas entre duas superfícies amostradas, com compensação lateral, Brückner e Excel.
/// </summary>
public sealed class CmdCubacao
{
    [CommandMethod("PRJCUBACAO", CommandFlags.Modal)]
    public static void Cubacao()
    {
        try
        {
            using var s = Sessao.Abrir();
            Rodar(s);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJCUBACAO: " + ex.Message); }
    }

    /// <summary>O comando em si; também chamado pelo PRJVOLUMES na opção Seções.</summary>
    internal static void Rodar(Sessao s)
    {
        var grupos = CubacaoCivil.Grupos(s.Tr);
        if (grupos.Count == 0)
        {
            Ctx.Msg("Não há grupos de seções (linhas de amostragem) no desenho. Crie as sample lines no Civil e rode de novo.");
            return;
        }
        var cfg = ConfigCubacao.Carregar();
        int pg = Math.Max(0, grupos.FindIndex(g => g.Rotulo == cfg.Grupo));
        int ig = grupos.Count == 1 ? 0 : s.EscolherDaLista("Grupo de seções", grupos.Select(g => g.Rotulo).ToList(), pg);
        if (ig < 0) return;
        var grupo = grupos[ig];
        cfg.Grupo = grupo.Rotulo;

        var listas = CubacaoCivil.Listas(s.Tr, grupo.Id);
        var origens = listas.Select(l => $"Lista de materiais do Civil: {l.Nome} - {l.Materiais}").ToList();
        origens.Add("Superfícies amostradas (cálculo PROJETA, áreas médias)");
        int po = cfg.Origem is null ? 0 : Math.Max(0, origens.FindIndex(o => o == cfg.Origem));
        int io = origens.Count == 1 ? 0 : s.EscolherDaLista("De onde vêm as áreas", origens, po);
        if (io < 0) return;
        cfg.Origem = origens[io];
        bool doCivil = io < listas.Count;

        bool alterou = false;
        CubacaoCivil.Entrada e;
        var trecho = s.PedirPalavra("Estacas", new[] { "Todas", "Trecho" }, "Todas");
        if (trecho is null) return;
        double ini = double.NaN, fim = double.NaN;
        if (trecho == "Trecho")
        {
            var a = s.PedirNumero("Estaca inicial, em metros (ex.: 200 = 10+0)", 0);
            if (a is null) return;
            var b = s.PedirNumero("Estaca final, em metros", a.Value + 1000);
            if (b is null) return;
            if (b <= a) { Ctx.Msg("A estaca final deve ser maior que a inicial."); return; }
            ini = a.Value; fim = b.Value;
        }

        if (doCivil)
            e = new CubacaoCivil.Entrada { Grupo = grupo.Id, Lista = listas[io].Guid, EstacaInicial = ini, EstacaFinal = fim };
        else
        {
            var fontes = CubacaoCivil.Fontes(s.Tr, grupo.Id);
            if (fontes.Count < 2) { Ctx.Msg("São precisas duas superfícies (terreno e projeto) para calcular pelas seções."); return; }
            var rotulos = fontes.Select(f => f.Rotulo).ToList();
            var nomes = fontes.Select(f => f.Nome).ToList();
            int pt = cfg.Terreno is null ? -1 : nomes.FindIndex(n => n == cfg.Terreno);
            if (pt < 0) pt = Math.Max(0, NotaServicoCivil.SuperficiePadrao(nomes, Array.Empty<string>()));
            int it = s.EscolherDaLista("Seção do TERRENO", rotulos, pt);
            if (it < 0) return;
            int pp = cfg.Projeto is null ? -1 : nomes.FindIndex(n => n == cfg.Projeto);
            if (pp < 0 || pp == it) pp = Math.Max(0, fontes.FindIndex(f => f.Tipo == SectionSourceType.CorridorSurface && f.Amostrada));
            if (pp == it) pp = Enumerable.Range(0, fontes.Count).First(i => i != it);
            int ip = s.EscolherDaLista("Seção do PROJETO", rotulos, pp);
            if (ip < 0) return;
            if (ip == it) { Ctx.Msg("Escolha fontes diferentes para terreno e projeto."); return; }
            foreach (var f in new[] { fontes[it], fontes[ip] }.Where(f => !f.Amostrada))
            {
                if (!CubacaoCivil.Amostrar(s.Tr, grupo.Id, f.Id)) { Ctx.Msg($"Não consegui amostrar '{f.Nome}' no grupo; amostre pelo Civil (Propriedades do grupo de seções)."); return; }
                Ctx.Msg($"'{f.Nome}' passou a ser amostrada no grupo {grupo.Nome}.");
                alterou = true;
            }
            cfg.Terreno = fontes[it].Nome; cfg.Projeto = fontes[ip].Nome;
            var fc = s.PedirNumero("Fator do corte (1 = volume geométrico)", cfg.FatorCorte);
            if (fc is null) return;
            var fa = s.PedirNumero("Fator do aterro / homogeneização (m³ de corte por m³ de aterro)", cfg.FatorAterro);
            if (fa is null) return;
            if (!(fc > 0) || !(fa > 0)) { Ctx.Msg("Os fatores devem ser positivos."); return; }
            cfg.FatorCorte = fc.Value; cfg.FatorAterro = fa.Value;
            e = new CubacaoCivil.Entrada { Grupo = grupo.Id, Terreno = fontes[it].Id, Projeto = fontes[ip].Id, FatorCorte = fc.Value, FatorAterro = fa.Value, EstacaInicial = ini, EstacaFinal = fim };
        }

        var cub = CubacaoCivil.Calcular(s.Tr, e);
        s.Tr.Commit();
        ConfigCubacao.Salvar(cfg);

        string F(double v) => v.ToString("#,##0", Num.Br);
        string X(double v) => v.ToString("0.###", Num.Br);
        Ctx.Msg($"--- Cubação: {cub.Alinhamento} / {cub.Grupo} ({cub.Linhas.Count} seções, {cub.Estaca(cub.Linhas[0].Estaca)} a {cub.Estaca(cub.Linhas[^1].Estaca)}) ---");
        Ctx.Msg($"  {cub.Origem}");
        Ctx.Msg($"  Corte {F(cub.TotalCorte)} m³" + (Math.Abs(cub.FatorCorte - 1) > 1e-9 ? $" (× {X(cub.FatorCorte)} = {F(cub.TotalCorteFator)})" : "") +
                $"; aterro {F(cub.TotalAterro)} m³ (× {X(cub.FatorAterro)} = {F(cub.TotalAterroFator)}); compensação lateral {F(cub.TotalCompensacao)} m³");
        Ctx.Msg($"  Saldo final (Brückner) {F(cub.Saldo)} m³ — {cub.Situacao}");
        if (cub.Conferencia.Count > 0)
        {
            var (d, est) = cub.MaiorDiferenca();
            Ctx.Msg($"  Conferência com a lista {cub.ConferenciaNome} do Civil: maior diferença de área {d.ToString("0.000", Num.Br)} m²" + (double.IsNaN(est) ? "" : $" (estaca {cub.Estaca(est)})"));
        }
        foreach (var a in cub.Avisos) Ctx.Msg("  [AVISO] " + a);
        if (alterou) Ctx.Msg("  (U desfaz a amostragem feita agora.)");

        var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", Texto.IdSeguro($"cubacao_{cub.Alinhamento}", 80) + ".xlsx");
        if (arq is null) return;
        cub.Salvar(arq);
        Ctx.Msg($"Nota de cubação gravada: {arq}");
        if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
    }
}

/// <summary>Últimas escolhas do PRJCUBACAO em %APPDATA%\Projeta\cubacao.json.</summary>
internal sealed class ConfigCubacao
{
    public string? Grupo { get; set; }
    public string? Origem { get; set; }
    public string? Terreno { get; set; }
    public string? Projeto { get; set; }
    public double FatorCorte { get; set; } = 1;
    public double FatorAterro { get; set; } = 1.25;

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "cubacao.json");

    public static ConfigCubacao Carregar()
    {
        try { if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigCubacao>(File.ReadAllText(Arquivo)) is { } c) return c; }
        catch { }
        return new ConfigCubacao();
    }

    public static void Salvar(ConfigCubacao c)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!); File.WriteAllText(Arquivo, JsonProjeta.Serializar(c)); }
        catch { }
    }
}
