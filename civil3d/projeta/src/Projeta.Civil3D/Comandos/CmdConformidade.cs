using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Projeta.Core.Superficies;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJCONFORMIDADE — Projeto × obra: compara a superfície executada (levantamento da obra) com a de projeto e marca o que
/// está fora da tolerância: mapa de cores com legenda e pontos críticos no desenho, controle por estaca ao longo de um
/// alinhamento e relatório em Excel (resumo por área, faixas de desvio, por estaca, pontos críticos, origem). U desfaz o mapa.
/// </summary>
public sealed class CmdConformidade
{
    [CommandMethod("PRJCONFORMIDADE", CommandFlags.Modal)]
    public static void Conformidade()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sups = VolumesCivil.Superficies(s.Tr);
            if (sups.Count < 2) { Ctx.Msg("São precisas duas superfícies no desenho: a de projeto e a do levantamento da obra."); return; }
            var nomes = sups.Select(x => x.Nome).ToList();
            var cfg = ConfigConformidade.Carregar();

            int pp = Sugerir(nomes, cfg.Projeto, -1, "projeto", "proj", "greide", "corredor", "final", "acabad", "pavim", "datum");
            int ip = s.EscolherDaLista("Superfície de PROJETO", nomes, Math.Max(pp, 0));
            if (ip < 0) return;
            int po = Sugerir(nomes, cfg.Obra, ip, "obra", "exec", "as built", "as-built", "asbuilt", "cadastro", "medi", "campo");
            if (po < 0) po = Enumerable.Range(0, nomes.Count).FirstOrDefault(i => i != ip);
            int io = s.EscolherDaLista("Superfície da OBRA (levantamento do executado)", nomes, po);
            if (io < 0) return;
            if (io == ip) { Ctx.Msg("Escolha superfícies diferentes para o projeto e a obra."); return; }

            var ta = s.PedirNumero("Tolerância para CIMA - quanto a obra pode ficar acima do projeto (m)", cfg.Acima);
            if (ta is null) return;
            var tb = s.PedirNumero("Tolerância para BAIXO - quanto a obra pode ficar abaixo do projeto (m)", cfg.Abaixo);
            if (tb is null) return;
            if (ta < 0 || tb < 0) { Ctx.Msg("As tolerâncias devem ser zero ou positivas."); return; }
            cfg.Acima = ta.Value; cfg.Abaixo = tb.Value;
            var tol = new ToleranciaObra { Acima = cfg.Acima, Abaixo = cfg.Abaixo, Limites = cfg.Limites.ToList(), PercentualMinimo = cfg.PercentualMinimo };
            tol.Validar();

            var poligonos = new List<VolumesCivil.Poligono>();
            var area = s.PedirPalavra("Área de análise", new[] { "Tudo", "Poligonos" }, cfg.Poligonos ? "Poligonos" : "Tudo");
            if (area is null) return;
            cfg.Poligonos = area == "Poligonos";
            if (cfg.Poligonos)
            {
                var sel = VolumesCivil.SelecionarPoligonos(s, "Selecione os polígonos fechados (trechos, platôs, lotes)");
                if (sel is null) return;
                poligonos = sel;
            }

            var alId = ObjectId.Null;
            var als = CivilUtil.Alinhamentos(s.Tr);
            if (als.Count > 0)
            {
                var itens = new List<string> { "(nenhum - sem controle por estaca)" };
                itens.AddRange(als.Select(a => a.Nome));
                int pa = 0;
                if (cfg.Alinhamento is not null)
                {
                    int k = als.FindIndex(a => string.Equals(a.Nome, cfg.Alinhamento, StringComparison.OrdinalIgnoreCase));
                    if (k >= 0) pa = k + 1;
                }
                int ia = s.EscolherDaLista("Alinhamento para o controle por estaca", itens, pa);
                if (ia < 0) return;
                if (ia > 0)
                {
                    alId = als[ia - 1].Id;
                    var iv = s.PedirNumero("Intervalo entre as estacas do controle (m)", cfg.Intervalo);
                    if (iv is null) return;
                    if (!(iv > 0)) { Ctx.Msg("O intervalo deve ser positivo."); return; }
                    cfg.Intervalo = iv.Value;
                }
                cfg.Alinhamento = ia > 0 ? als[ia - 1].Nome : null;
            }

            var mapa = s.PedirPalavra("Desenhar o mapa de desvios e os pontos críticos", new[] { "Sim", "Nao" }, cfg.Mapa ? "Sim" : "Nao");
            if (mapa is null) return;
            cfg.Mapa = mapa == "Sim";
            cfg.Projeto = nomes[ip];
            cfg.Obra = nomes[io];

            Ctx.Msg("Comparando a obra com o projeto...");
            var saida = ConformidadeCivil.Calcular(s.Db, new ConformidadeCivil.Entrada
            {
                Projeto = sups[ip].Id, Obra = sups[io].Id, Poligonos = poligonos, Tolerancia = tol, Alinhamento = alId,
                Intervalo = cfg.Intervalo, Afastamentos = cfg.Afastamentos, Pontos = cfg.Pontos, Desenho = s.Doc.Name,
            });
            var rel = saida.Relatorio;

            if (cfg.Mapa && rel.Geral.Area > 0)
            {
                var antigos = ConformidadeCivil.EntidadesDoMapa(s.Tr, s.Db);
                if (antigos.Count > 0)
                {
                    var apagar = s.PedirPalavra($"Apagar o mapa anterior ({antigos.Count} objeto(s) na camada {ConformidadeCivil.LayerMapa})", new[] { "Sim", "Nao" }, "Sim");
                    if (apagar == "Sim")
                        foreach (var id in antigos) ((Entity)s.Tr.GetObject(id, OpenMode.ForWrite)).Erase();
                }
                int n = ConformidadeCivil.DesenharMapa(s.Tr, s.Db, saida.Malha, rel);
                Ctx.Msg($"Mapa: {n} retângulo(s) e {rel.Criticos.Count} ponto(s) crítico(s) na camada {ConformidadeCivil.LayerMapa}, com legenda à direita.");
                s.Tr.Commit();
            }
            ConfigConformidade.Salvar(cfg);

            Ctx.Msg($"--- Projeto × obra: projeto {rel.Projeto} × obra {rel.Obra} (tolerância {rel.Tolerancia.Descricao}, malha {F(rel.Celula)} m) ---");
            foreach (var a in rel.Linhas)
                Ctx.Msg($"  {a.Nome}: {V(a.Area)} m² comparados; dentro {P(a.PctDentro)} %, acima {P(a.PctAcima)} %, abaixo {P(a.PctAbaixo)} %; " +
                        $"desvio médio {D(a.Media)} m, EMQ {D3(a.Emq)} m, de {D(a.Minimo)} a {D(a.Maximo)} m — {a.Situacao(rel.Tolerancia.PercentualMinimo)}");
            var t = rel.Linhas[^1];
            Ctx.Msg($"  Volume acima do projeto (cortar) {V(t.VolumeAcima)} m³; abaixo do projeto (aterrar) {V(t.VolumeAbaixo)} m³.");
            if (rel.Criticos.Count > 0)
            {
                Ctx.Msg($"  Pontos críticos ({rel.Criticos.Count}):");
                foreach (var p in rel.Criticos.Take(5))
                    Ctx.Msg($"    P{p.Numero}: desvio {D(p.Desvio)} m em E={p.X.ToString("0.000", Num.Br)} N={p.Y.ToString("0.000", Num.Br)}" +
                            (double.IsNaN(p.Estaca) ? "" : $" (estaca {NotaServico.FormatarEstaca(p.Estaca)}, afast. {p.Afastamento.ToString("0.00", Num.Br)} m)"));
                if (rel.Criticos.Count > 5) Ctx.Msg($"    ... e mais {rel.Criticos.Count - 5} na planilha.");
            }
            if (rel.Estacas is { } ce && ce.Linhas.Count > 0)
            {
                int fora = ce.Linhas.Count(l => ce.Resumo(l, rel.Tolerancia).Fora > 0);
                Ctx.Msg($"  Controle por estaca ({ce.Alinhamento}): {ce.Linhas.Count} estaca(s) × {ce.Afastamentos.Length} afastamento(s); {fora} estaca(s) com ponto fora da tolerância.");
            }
            foreach (var a in rel.Avisos) Ctx.Msg("  [AVISO] " + a);
            if (!(rel.Geral.Area > 0)) return;

            var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", Texto.IdSeguro($"projeto_x_obra_{rel.Projeto}_x_{rel.Obra}", 80) + ".xlsx");
            if (arq is null) return;
            rel.Salvar(arq);
            Ctx.Msg($"Relatório gravado: {arq}");
            if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJCONFORMIDADE: " + ex.Message); }
    }

    /// <summary>Índice da última superfície usada ou da primeira cujo nome tem uma das dicas (fora <paramref name="exceto"/>); −1 se nenhuma.</summary>
    internal static int Sugerir(IList<string> nomes, string? ultimo, int exceto, params string[] dicas)
    {
        if (ultimo is not null)
            for (int i = 0; i < nomes.Count; i++)
                if (i != exceto && string.Equals(nomes[i], ultimo, StringComparison.OrdinalIgnoreCase)) return i;
        foreach (var d in dicas)
            for (int i = 0; i < nomes.Count; i++)
                if (i != exceto && nomes[i].Contains(d, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private static string F(double v) => v.ToString("0.###", Num.Br);
    private static string V(double v) => double.IsNaN(v) ? "-" : v.ToString("#,##0.00", Num.Br);
    private static string P(double v) => double.IsNaN(v) ? "-" : v.ToString("0.0", Num.Br);
    private static string D(double v) => double.IsNaN(v) ? "-" : v.ToString("+0.000;-0.000;0.000", Num.Br);
    private static string D3(double v) => double.IsNaN(v) ? "-" : v.ToString("0.000", Num.Br);
}

/// <summary>Últimas escolhas do PRJCONFORMIDADE em %APPDATA%\Projeta\conformidade.json.</summary>
internal sealed class ConfigConformidade
{
    public double Acima { get; set; } = 0.05;
    public double Abaixo { get; set; } = 0.05;
    /// <summary>Limites (m) das faixas além da tolerância.</summary>
    public List<double> Limites { get; set; } = new() { 0.10, 0.20 };
    /// <summary>Área dentro da tolerância (%) para a área ser conforme.</summary>
    public double PercentualMinimo { get; set; } = 95;
    public double Intervalo { get; set; } = 20;
    /// <summary>Afastamentos fixos do controle por estaca (m, negativo = esquerda); vazio = automático.</summary>
    public List<double> Afastamentos { get; set; } = new();
    public int Pontos { get; set; } = 20;
    public bool Mapa { get; set; } = true;
    public bool Poligonos { get; set; }
    public string? Alinhamento { get; set; }
    public string? Projeto { get; set; }
    public string? Obra { get; set; }

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "conformidade.json");

    public static ConfigConformidade Carregar()
    {
        try
        {
            if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigConformidade>(File.ReadAllText(Arquivo)) is { } c)
            {
                c.Limites ??= new() { 0.10, 0.20 };
                c.Afastamentos ??= new();
                if (!(c.Intervalo > 0)) c.Intervalo = 20;
                if (!(c.PercentualMinimo >= 0 && c.PercentualMinimo <= 100)) c.PercentualMinimo = 95;
                c.Pontos = Math.Clamp(c.Pontos, 0, 200);
                return c;
            }
        }
        catch { }
        return new ConfigConformidade();
    }

    public static void Salvar(ConfigConformidade c)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!);
            File.WriteAllText(Arquivo, JsonProjeta.Serializar(c));
        }
        catch { }
    }
}
