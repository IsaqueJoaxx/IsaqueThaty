using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Superficies;
using Projeta.Core.Terraplenagem;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJPLATO — platôs em polígonos fechados: cota que compensa corte e aterro (ou fixa, ou a elevação da polilinha), piso
/// com caimento descendo como o terreno, taludes de corte e aterro até o terreno, superfície de cada platô e superfície
/// final, crista, pé, tracinhos e cotas no desenho, volumes exatos e planilha. U desfaz tudo.
/// </summary>
public sealed class CmdPlato
{
    [CommandMethod("PRJPLATO", CommandFlags.Modal)]
    public static void Plato()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sups = VolumesCivil.Superficies(s.Tr);
            if (sups.Count == 0) { Ctx.Msg("Não há superfície de terreno no desenho."); return; }
            var nomes = sups.Select(x => x.Nome).ToList();
            var cfg = ConfigPlato.Carregar();

            int pt = cfg.Terreno is null ? -1 : nomes.FindIndex(n => string.Equals(n, cfg.Terreno, StringComparison.OrdinalIgnoreCase));
            if (pt < 0) pt = NotaServicoCivil.SuperficiePadrao(nomes, Array.Empty<string>());
            int it = s.EscolherDaLista("Superfície do TERRENO", nomes, Math.Max(pt, 0));
            if (it < 0) return;

            var filtro = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE,CIRCLE,ELLIPSE,SPLINE") });
            var r = s.Ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os polígonos fechados dos platôs: " }, filtro);
            if (r.Status != PromptStatus.OK) return;
            var sel = new List<(List<(double X, double Y)> V, string Handle, double Elev)>();
            int abertos = 0;
            foreach (var id in r.Value.GetObjectIds())
            {
                if (s.Tr.GetObject(id, OpenMode.ForRead) is not Curve c) continue;
                var v = VolumesCivil.Vertices(c);
                if (v is null) { abertos++; continue; }
                sel.Add((v, id.Handle.ToString(), c is Polyline pl ? pl.Elevation : double.NaN));
            }
            if (abertos > 0) Ctx.Msg($"{abertos} curva(s) aberta(s) ignorada(s).");
            if (sel.Count == 0) { Ctx.Msg("Nenhum polígono fechado selecionado."); return; }

            var modo = s.PedirPalavra("Cota do piso", new[] { "Compensar", "Fixa", "Polilinha" }, cfg.Modo);
            if (modo is null) return;
            cfg.Modo = modo;
            double fixa = double.NaN;
            if (modo == "Fixa")
            {
                var z = s.PedirNumero("Cota do piso no centro do platô (m)", cfg.CotaFixa);
                if (z is null) return;
                fixa = cfg.CotaFixa = z.Value;
            }
            bool algumCompensa = modo == "Compensar" || (modo == "Polilinha" && sel.Any(x => double.IsNaN(x.Elev) || x.Elev == 0));
            if (modo == "Polilinha" && algumCompensa)
                Ctx.Msg("Polígono sem elevação (ou que não é polilinha) fica com a cota que compensa corte e aterro.");
            var par = new ParametrosPlato { TaludeCorte = cfg.TaludeCorte, TaludeAterro = cfg.TaludeAterro, Caimento = cfg.CaimentoPct / 100, Fh = cfg.Fh };
            var fh = s.PedirNumero("Fator de homogeneização (m³ de corte por m³ de aterro compactado)", cfg.Fh);
            if (fh is null) return;
            var cai = s.PedirNumero("Caimento do piso (%), descendo como o terreno; 0 = em nível", cfg.CaimentoPct);
            if (cai is null) return;
            var tc = s.PedirNumero("Talude de CORTE (H por 1 V)", cfg.TaludeCorte);
            if (tc is null) return;
            var ta = s.PedirNumero("Talude de ATERRO (H por 1 V)", cfg.TaludeAterro);
            if (ta is null) return;
            cfg.Fh = fh.Value; cfg.CaimentoPct = cai.Value; cfg.TaludeCorte = tc.Value; cfg.TaludeAterro = ta.Value;
            par = new ParametrosPlato { TaludeCorte = cfg.TaludeCorte, TaludeAterro = cfg.TaludeAterro, Caimento = cfg.CaimentoPct / 100, Fh = cfg.Fh };
            try { par.Validar(); } catch (ArgumentException ex) { Ctx.Msg(ex.Message); return; }
            var mapa = s.PedirPalavra("Desenhar o mapa de corte e aterro", new[] { "Sim", "Nao" }, cfg.Mapa ? "Sim" : "Nao");
            if (mapa is null) return;
            cfg.Mapa = mapa == "Sim";
            cfg.Terreno = nomes[it];

            var polis = new List<PlatoCivil.Poligono>();
            for (int i = 0; i < sel.Count; i++)
            {
                double cota = modo == "Fixa" ? fixa : modo == "Polilinha" && !double.IsNaN(sel[i].Elev) && sel[i].Elev != 0 ? sel[i].Elev : double.NaN;
                polis.Add(new PlatoCivil.Poligono($"Platô {i + 1}", sel[i].V, sel[i].Handle, cota));
            }
            Ctx.Msg($"Calculando {polis.Count} platô(s)...");
            var saida = PlatoCivil.Executar(s.Tr, s.Db, new PlatoCivil.Entrada { Terreno = sups[it].Id, Poligonos = polis, Parametros = par, Desenho = s.Doc.Name });
            var rel = saida.Relatorio;
            if (cfg.Mapa && saida.Volumes is { } sv)
            {
                var antigos = VolumesCivil.EntidadesDoMapa(s.Tr, s.Db);
                if (antigos.Count > 0 && s.PedirPalavra($"Apagar o mapa anterior ({antigos.Count} objeto(s) na camada {VolumesCivil.LayerMapa})", new[] { "Sim", "Nao" }, "Sim") == "Sim")
                    foreach (var id in antigos) ((Entity)s.Tr.GetObject(id, OpenMode.ForWrite)).Erase();
                VolumesCivil.DesenharMapa(s.Tr, s.Db, sv.Malha, sv.Relatorio);
            }
            s.Tr.Commit();
            ConfigPlato.Salvar(cfg);

            Ctx.Msg($"--- Platôs sobre {rel.Terreno} (taludes {F(par.TaludeCorte)}H:1V corte, {F(par.TaludeAterro)}H:1V aterro, Fh {F(par.Fh)}) ---");
            foreach (var p in rel.Platos)
                Ctx.Msg($"  {p.Nome}: piso {p.Cota.ToString("0.00", Num.Br)} no centro{(p.Compensada ? " (compensada)" : "")}, de {p.CotaMin.ToString("0.00", Num.Br)} a {p.CotaMax.ToString("0.00", Num.Br)}; " +
                        $"corte {V(RelatorioPlatos.C(p))} m³, aterro {V(RelatorioPlatos.A(p))} m³ (× {F(par.Fh)} = {V(RelatorioPlatos.A(p) * par.Fh)}), saldo {V(rel.Saldo(p))} m³ — {rel.Situacao(p)}; " +
                        $"taludes até {p.AlturaCorte.ToString("0.0", Num.Br)} m (corte) e {p.AlturaAterro.ToString("0.0", Num.Br)} m (aterro)");
            Ctx.Msg($"  Superfícies: {string.Join(", ", saida.SuperficiesPlato)} e a final {saida.SuperficieFinal}.");
            foreach (var a in rel.Avisos) Ctx.Msg("  [AVISO] " + a);

            var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", Texto.IdSeguro($"platos_{rel.Terreno}", 80) + ".xlsx");
            if (arq is null) return;
            rel.Salvar(arq, saida.Volumes?.Relatorio);
            Ctx.Msg($"Relatório gravado: {arq}");
            if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJPLATO: " + ex.Message); }
    }

    private static string F(double v) => v.ToString("0.###", Num.Br);
    private static string V(double v) => double.IsNaN(v) ? "-" : v.ToString("#,##0", Num.Br);
}

/// <summary>Últimas escolhas do PRJPLATO em %APPDATA%\Projeta\platos.json.</summary>
internal sealed class ConfigPlato
{
    public string Modo { get; set; } = "Compensar";
    public double CotaFixa { get; set; }
    public double Fh { get; set; } = 1.25;
    public double CaimentoPct { get; set; } = 0.5;
    public double TaludeCorte { get; set; } = 1.5;
    public double TaludeAterro { get; set; } = 2.0;
    public bool Mapa { get; set; } = true;
    public string? Terreno { get; set; }

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "platos.json");

    public static ConfigPlato Carregar()
    {
        try
        {
            if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigPlato>(File.ReadAllText(Arquivo)) is { } c)
            {
                if (c.Modo is not ("Compensar" or "Fixa" or "Polilinha")) c.Modo = "Compensar";
                return c;
            }
        }
        catch { }
        return new ConfigPlato();
    }

    public static void Salvar(ConfigPlato c)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!);
            File.WriteAllText(Arquivo, JsonProjeta.Serializar(c));
        }
        catch { }
    }
}
