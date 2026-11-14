using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Superficies;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJVOLUMES — corte e aterro entre duas superfícies (terreno × projeto, levantamento anterior × novo), na sobreposição
/// inteira ou dentro de polígonos fechados (lotes, platôs, trechos), com fatores de corte e de aterro, mapa de cores com
/// legenda no desenho e relatório em Excel (volumes por área, faixas de altura, origem). U desfaz o mapa.
/// </summary>
public sealed class CmdVolumes
{
    [CommandMethod("PRJVOLUMES", CommandFlags.Modal)]
    public static void Volumes()
    {
        try
        {
            using var s = Sessao.Abrir();
            var metodo = s.PedirPalavra("Calcular por [superfícies inteiras / seções do Civil (cubação)]", new[] { "Superficies", "Secoes" }, "Superficies");
            if (metodo is null) return;
            if (metodo == "Secoes") { CmdCubacao.Rodar(s); return; }
            var sups = VolumesCivil.Superficies(s.Tr);
            if (sups.Count < 2) { Ctx.Msg("São precisas duas superfícies (base e comparação) no desenho."); return; }
            var nomes = sups.Select(x => x.Nome).ToList();
            var cfg = ConfigVolumes.Carregar();

            int pb = NotaServicoCivil.SuperficiePadrao(nomes, Array.Empty<string>());
            int ib = s.EscolherDaLista("Superfície BASE (terreno / levantamento anterior)", nomes, Math.Max(pb, 0));
            if (ib < 0) return;
            int pc = Enumerable.Range(0, nomes.Count).FirstOrDefault(i => i != ib);
            int ic = s.EscolherDaLista("Superfície de COMPARAÇÃO (projeto / levantamento novo)", nomes, pc);
            if (ic < 0) return;
            if (ic == ib) { Ctx.Msg("Escolha superfícies diferentes para base e comparação."); return; }

            var poligonos = new List<VolumesCivil.Poligono>();
            var area = s.PedirPalavra("Área de cálculo", new[] { "Tudo", "Poligonos" }, cfg.Poligonos ? "Poligonos" : "Tudo");
            if (area is null) return;
            cfg.Poligonos = area == "Poligonos";
            if (cfg.Poligonos)
            {
                var sel = VolumesCivil.SelecionarPoligonos(s, "Selecione os polígonos fechados (lotes, platôs, áreas)");
                if (sel is null) return;
                poligonos = sel;
            }

            var fc = s.PedirNumero("Fator do corte (empolamento; 1 = volume geométrico)", cfg.FatorCorte);
            if (fc is null) return;
            var fa = s.PedirNumero("Fator do aterro (1 = volume geométrico)", cfg.FatorAterro);
            if (fa is null) return;
            if (!(fc > 0) || !(fa > 0)) { Ctx.Msg("Os fatores devem ser positivos."); return; }
            cfg.FatorCorte = fc.Value; cfg.FatorAterro = fa.Value;

            var mapa = s.PedirPalavra("Desenhar o mapa de corte e aterro", new[] { "Sim", "Nao" }, cfg.Mapa ? "Sim" : "Nao");
            if (mapa is null) return;
            cfg.Mapa = mapa == "Sim";
            var manter = s.PedirPalavra("Manter a superfície de volume no desenho", new[] { "Sim", "Nao" }, "Nao");
            if (manter is null) return;

            Ctx.Msg("Calculando os volumes...");
            var saida = VolumesCivil.Calcular(s.Db, new VolumesCivil.Entrada
            {
                Base = sups[ib].Id, Comparacao = sups[ic].Id, Poligonos = poligonos,
                FatorCorte = cfg.FatorCorte, FatorAterro = cfg.FatorAterro, ManterSuperficie = manter == "Sim", Desenho = s.Doc.Name,
            });
            var rel = saida.Relatorio;

            bool alterou = saida.SuperficieVolume is not null;
            if (cfg.Mapa)
            {
                var antigos = VolumesCivil.EntidadesDoMapa(s.Tr, s.Db);
                if (antigos.Count > 0)
                {
                    var apagar = s.PedirPalavra($"Apagar o mapa anterior ({antigos.Count} objeto(s) na camada {VolumesCivil.LayerMapa})", new[] { "Sim", "Nao" }, "Sim");
                    if (apagar == "Sim")
                        foreach (var id in antigos) ((Entity)s.Tr.GetObject(id, OpenMode.ForWrite)).Erase();
                }
                int n = VolumesCivil.DesenharMapa(s.Tr, s.Db, saida.Malha, rel);
                Ctx.Msg($"Mapa: {n} retângulo(s) na camada {VolumesCivil.LayerMapa}, com legenda à direita.");
                alterou = true;
            }
            if (alterou) s.Tr.Commit();
            ConfigVolumes.Salvar(cfg);

            Ctx.Msg($"--- Volumes: base {rel.Base} × comparação {rel.Comparacao} (fatores corte {F(rel.FatorCorte)}, aterro {F(rel.FatorAterro)}) ---");
            foreach (var a in rel.Areas.Concat(rel.Areas.Count > 1 ? new[] { rel.Total } : Array.Empty<AreaVolume>()))
                Ctx.Msg($"  {a.Nome}: área {V(a.Area2D)} m², corte {V(a.Corte)} m³, aterro {V(a.Aterro)} m³, saldo {V(rel.Saldo(a))} m³ — {rel.SituacaoDe(a)}");
            if (saida.SuperficieVolume is not null) Ctx.Msg($"  Superfície de volume mantida: {saida.SuperficieVolume}");
            foreach (var a in rel.Avisos) Ctx.Msg("  [AVISO] " + a);

            var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", Texto.IdSeguro($"volumes_{rel.Base}_x_{rel.Comparacao}", 80) + ".xlsx");
            if (arq is null) return;
            rel.Salvar(arq);
            Ctx.Msg($"Relatório gravado: {arq}");
            if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJVOLUMES: " + ex.Message); }
    }

    private static string F(double v) => v.ToString("0.###", Num.Br);
    private static string V(double v) => double.IsNaN(v) ? "-" : v.ToString("#,##0.00", Num.Br);
}

/// <summary>Últimas escolhas do PRJVOLUMES em %APPDATA%\Projeta\volumes.json.</summary>
internal sealed class ConfigVolumes
{
    public double FatorCorte { get; set; } = 1;
    public double FatorAterro { get; set; } = 1;
    public bool Mapa { get; set; } = true;
    public bool Poligonos { get; set; }

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "volumes.json");

    public static ConfigVolumes Carregar()
    {
        try { if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigVolumes>(File.ReadAllText(Arquivo)) is { } c) return c; }
        catch { }
        return new ConfigVolumes();
    }

    public static void Salvar(ConfigVolumes c)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!);
            File.WriteAllText(Arquivo, JsonProjeta.Serializar(c));
        }
        catch { }
    }
}
