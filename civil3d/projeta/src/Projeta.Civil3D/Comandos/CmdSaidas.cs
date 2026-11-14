using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Escavacao;
using Projeta.Core.Redes;
using Projeta.Core.Relatorios;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdSaidas
{
    internal static ParametrosVala ParametrosVala(Projeto p) =>
        p.Extras.TryGetValue("vala", out var j) ? JsonProjeta.Desserializar<ParametrosVala>(j) ?? new() : new();

    internal static TabelaDados TabelaBacias(Rede rede)
    {
        var t = new TabelaDados { Titulo = $"Bacias de contribuição — {rede.Nome}" };
        t.Colunas.AddRange(new[] { "Bacia", "Nó", "Área (ha)", "C", "C·A (ha)", "Método tc", "tc (min)", "Talvegue (m)", "Desnível (m)" });
        foreach (var b in rede.Bacias)
        {
            t.Linhas.Add(new[] { b.Nome, rede.NoPorId(b.NoId)?.Nome ?? "-", Num.F(b.Area, 4), Num.F(b.C, 2), Num.F(b.Area * b.C, 4), b.MetodoTc.ToString(),
                Num.F(b.Tc(), 2), Num.F(b.ComprimentoTalvegue, 1), Num.F(b.DesnivelTalvegue, 2) });
            t.Situacao.Add("OK");
        }
        t.Linhas.Add(new[] { "TOTAL", "", Num.F(rede.Bacias.Sum(b => b.Area), 4), "", Num.F(rede.Bacias.Sum(b => b.Area * b.C), 4), "", "", "", "" });
        t.Situacao.Add("OK");
        return t;
    }

    /// <summary>Relatório HTML completo (parâmetros, bacias, dimensionamento, nós, materiais e valas).</summary>
    internal static string RelatorioCompleto(Rede rede, Projeto projeto)
    {
        var res = rede.Resultado ?? throw new InvalidOperationException("Rede não calculada.");
        var blocos = new List<string>();
        if (rede.Bacias.Count > 0) blocos.Add(TabelaBacias(rede).ParaHtml());
        blocos.Add(GeradorTabelas.Dimensionamento(rede, res).ParaHtml());
        blocos.Add(GeradorTabelas.NotaServicoNos(rede, res).ParaHtml());
        blocos.Add(GeradorTabelas.ResumoMateriais(rede, res).ParaHtml());
        blocos.Add(GeradorTabelas.Vala(rede, CalculoVala.Calcular(res, ParametrosVala(projeto))).ParaHtml());
        return RelatorioHtml.Documento($"Rede {rede.Nome} ({rede.Tipo})", blocos, res.Mensagens, RelatorioHtml.ParametrosRede(rede, projeto));
    }

    /// <summary>PRJTABELA — insere uma tabela (dimensionamento, nós, materiais, valas ou bacias) no desenho.</summary>
    [CommandMethod("PRJTABELA", CommandFlags.Modal)]
    public static void Tabela() => Operacoes.Rodar("PRJTABELA", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Resultado is not null);
        if (rede?.Resultado is null) return;
        var tipo = s.PedirPalavra("Tabela", new[] { "Dimensionamento", "Nos", "Materiais", "Vala", "Bacias" }, "Dimensionamento");
        if (tipo is null) return;
        var dados = tipo switch
        {
            "Nos" => GeradorTabelas.NotaServicoNos(rede, rede.Resultado),
            "Materiais" => GeradorTabelas.ResumoMateriais(rede, rede.Resultado),
            "Vala" => GeradorTabelas.Vala(rede, CalculoVala.Calcular(rede.Resultado, ParametrosVala(s.Projeto))),
            "Bacias" => TabelaBacias(rede),
            _ => GeradorTabelas.Dimensionamento(rede, rede.Resultado),
        };
        var h = s.PedirNumero("Altura do texto", rede.AlturaTexto * 0.8);
        if (h is null) return;
        var p = s.Ed.GetPoint("\nPonto de inserção (canto superior esquerdo): ");
        if (p.Status != PromptStatus.OK) return;
        var id = TabelasDesenho.Inserir(s, dados, p.Value, h.Value, DesenhoRede.Layer(rede, "TABELA"));
        if (!rede.HandlesAuxiliares.TryGetValue("tabelas", out var l)) rede.HandlesAuxiliares["tabelas"] = l = new();
        l.Add(Ctx.HandleDe(id));
        s.SalvarEConfirmar();
    });

    /// <summary>PRJRELATORIO — gera o relatório HTML completo da rede e abre no navegador.</summary>
    [CommandMethod("PRJRELATORIO", CommandFlags.Modal)]
    public static void Relatorio() => Operacoes.Rodar("PRJRELATORIO", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Resultado is not null);
        if (rede is null) return;
        var arq = UiUtil.SalvarArquivo("HTML|*.html", Texto.IdSeguro(rede.Nome, 60) + "_relatorio.html");
        if (arq is null) return;
        File.WriteAllText(arq, RelatorioCompleto(rede, s.Projeto));
        UiUtil.AbrirNoSistema(arq);
        Ctx.Msg("Relatório salvo em " + arq);
    });

    /// <summary>PRJNOTACAIXAS — nota de serviço de PVs/caixas em CSV (Excel).</summary>
    [CommandMethod("PRJNOTACAIXAS", CommandFlags.Modal)]
    public static void NotaCaixas() => Operacoes.Rodar("PRJNOTACAIXAS", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Resultado is not null);
        if (rede?.Resultado is null) return;
        var arq = UiUtil.SalvarArquivo("CSV (Excel)|*.csv", Texto.IdSeguro(rede.Nome, 60) + "_nota_pvs.csv");
        if (arq is null) return;
        File.WriteAllText(arq, GeradorTabelas.NotaServicoNos(rede, rede.Resultado).ParaCsv(), new System.Text.UTF8Encoding(true));
        UiUtil.AbrirNoSistema(arq);
    });

    /// <summary>PRJPERFIL — projeta tubos e PVs da rede em um profile view.</summary>
    [CommandMethod("PRJPERFIL", CommandFlags.Modal)]
    public static void Perfil() => Operacoes.Rodar("PRJPERFIL", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Resultado is not null);
        if (rede is null) return;
        var pv = CivilUtil.PedirProfileView(s);
        if (pv is null) return;
        var tol = s.PedirNumero("Afastamento máximo dos nós em relação ao alinhamento do perfil (m)", 2.0);
        if (tol is null) return;
        int n = ProjecaoPerfil.Projetar(s, rede, pv, tol.Value);
        s.SalvarEConfirmar();
        Ctx.Msg(n == 0 ? "Nenhum trecho com os dois nós próximos ao alinhamento do perfil." : $"{n} trecho(s) projetado(s). Rode de novo após recalcular para atualizar.");
    });

    /// <summary>PRJINTERFERENCIA — verifica cruzamentos e folgas verticais entre redes.</summary>
    [CommandMethod("PRJINTERFERENCIA", CommandFlags.Modal)]
    public static void Interferencias() => Operacoes.Rodar("PRJINTERFERENCIA", s =>
    {
        var redes = s.Projeto.Redes.Where(r => r.Resultado is not null).ToList();
        if (redes.Count == 0) { Ctx.Msg("Calcule as redes antes (PRJCALCULAR)."); return; }
        var folga = s.PedirNumero("Folga vertical mínima entre tubos (m)", 0.20);
        if (folga is null) return;
        bool mesma = s.PedirPalavra("Verificar também dentro da mesma rede", new[] { "Sim", "Nao" }, "Nao") == "Sim";
        var lista = VerificadorInterferencias.Verificar(redes, folga.Value, mesma);
        // marcações antigas
        if (s.Projeto.Extras.TryGetValue("interferencias", out var antigas))
            foreach (var h in antigas.Split(',', StringSplitOptions.RemoveEmptyEntries)) Ctx.Apagar(s.Tr, s.Db, h);
        const string layer = "PRJ-INTERFERENCIA";
        Ctx.GarantirLayer(s.Tr, s.Db, layer, 1);
        var ms = Ctx.EspacoModelo(s.Tr, s.Db);
        var handles = new List<string>();
        foreach (var i in lista)
        {
            var c = new Circle(new Point3d(i.Ponto.X, i.Ponto.Y, 0), Vector3d.ZAxis, 1.5) { Layer = layer };
            handles.Add(Ctx.HandleDe(Ctx.Adicionar(s.Tr, ms, c)));
            var t = Ctx.Texto(Ctx.MTextLinhas($"{i.TrechoA} x {i.TrechoB}", $"folga {Num.F(i.Folga)} m"), new Point3d(i.Ponto.X + 1.8, i.Ponto.Y + 1.8, 0), 0.8);
            t.Layer = layer;
            handles.Add(Ctx.HandleDe(Ctx.Adicionar(s.Tr, ms, t)));
        }
        s.Projeto.Extras["interferencias"] = string.Join(",", handles);
        Ctx.Msg($"{lista.Count} interferência(s) encontrada(s) (marcadas no layer {layer}).");
        if (lista.Count > 0)
        {
            var tab = GeradorTabelas.Interferencias(lista);
            using var f = new FormTabelas("PROJETA — Interferências", new[] { tab });
            if (AcApp.ShowModalDialog(f) == System.Windows.Forms.DialogResult.Yes)
            {
                var p = s.Ed.GetPoint("\nPonto de inserção da tabela: ");
                if (p.Status == PromptStatus.OK) TabelasDesenho.Inserir(s, tab, p.Value, 0.8, layer);
            }
        }
        s.SalvarEConfirmar();
    });

    /// <summary>PRJESCAVACAO — volumes de escavação de valas por trecho e faixa de profundidade.</summary>
    [CommandMethod("PRJESCAVACAO", CommandFlags.Modal)]
    public static void Escavacao() => Operacoes.Rodar("PRJESCAVACAO", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Resultado is not null);
        if (rede?.Resultado is null) return;
        var p = ParametrosVala(s.Projeto);
        using (var fp = new FormPropriedades("PROJETA — Parâmetros de vala", ("Vala", p)))
            if (AcApp.ShowModalDialog(fp) != System.Windows.Forms.DialogResult.OK) return;
        s.Projeto.Extras["vala"] = JsonProjeta.Serializar(p, false);
        var q = CalculoVala.Calcular(rede.Resultado, p);
        var tab = GeradorTabelas.Vala(rede, q);
        using var f = new FormTabelas($"PROJETA — Escavação — {rede.Nome}", new[] { tab });
        if (AcApp.ShowModalDialog(f) == System.Windows.Forms.DialogResult.Yes)
        {
            var pt = s.Ed.GetPoint("\nPonto de inserção da tabela: ");
            if (pt.Status == PromptStatus.OK) TabelasDesenho.Inserir(s, tab, pt.Value, rede.AlturaTexto * 0.8, DesenhoRede.Layer(rede, "TABELA"));
        }
        s.SalvarEConfirmar();
    });
}
