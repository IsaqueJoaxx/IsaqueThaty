using System.Text;
using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Escavacao;
using Projeta.Core.Redes;

namespace Projeta.Core.Relatorios;

/// <summary>Tabela genérica (usada para Table do AutoCAD, CSV e HTML).</summary>
public sealed class TabelaDados
{
    public string Titulo { get; set; } = "";
    public List<string> Colunas { get; set; } = new();
    public List<string[]> Linhas { get; set; } = new();
    /// <summary>Situação de cada linha (OK/AVISO/ERRO) para colorir.</summary>
    public List<string> Situacao { get; set; } = new();
    public List<string> Rodape { get; set; } = new();

    public string ParaCsv(char sep = ';')
    {
        var sb = new StringBuilder();
        if (Titulo.Length > 0) sb.AppendLine(Csv.Escapar(Titulo, sep));
        sb.AppendLine(Csv.Linha(Colunas, sep));
        foreach (var l in Linhas) sb.AppendLine(Csv.Linha(l, sep));
        foreach (var r in Rodape) sb.AppendLine(Csv.Escapar(r, sep));
        return sb.ToString();
    }

    public string ParaHtml()
    {
        var sb = new StringBuilder();
        if (Titulo.Length > 0) sb.Append("<h2>").Append(Texto.Html(Titulo)).AppendLine("</h2>");
        sb.AppendLine("<div class=\"rolagem\"><table><thead><tr>");
        foreach (var c in Colunas) sb.Append("<th>").Append(Texto.Html(c)).Append("</th>");
        sb.AppendLine("</tr></thead><tbody>");
        for (int i = 0; i < Linhas.Count; i++)
        {
            var cls = i < Situacao.Count ? Situacao[i] switch { "ERRO" => " class=\"erro\"", "AVISO" => " class=\"aviso\"", _ => "" } : "";
            sb.Append("<tr").Append(cls).Append('>');
            foreach (var v in Linhas[i]) sb.Append("<td>").Append(Texto.Html(v)).Append("</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table></div>");
        foreach (var r in Rodape) sb.Append("<p class=\"nota\">").Append(Texto.Html(r)).AppendLine("</p>");
        return sb.ToString();
    }
}

public static class GeradorTabelas
{
    public static TabelaDados Dimensionamento(Rede rede, ResultadoRede res)
    {
        var t = new TabelaDados { Titulo = $"Planilha de dimensionamento — {rede.Nome}" };
        bool esg = rede.Tipo == TipoRede.Esgoto;
        if (!esg)
        {
            t.Colunas.AddRange(new[] { "Trecho", "Montante", "Jusante", "L (m)", "Área acum. (ha)", "ΣC·A (ha)", "tc (min)", "i (mm/h)", "Q (L/s)",
                "Seção", "I (%)", "CT mont.", "CT jus.", "CF mont.", "CF jus.", "Prof. mont.", "Prof. jus.", "Recobr. mín.", "y/D", "v (m/s)", "Q plena (L/s)", "Situação", "Observações" });
            foreach (var r in res.Trechos)
            {
                t.Linhas.Add(new[]
                {
                    r.Nome, r.Montante, r.Jusante, Num.F(r.Comprimento), Num.F(r.AreaAcumuladaHa, 3), Num.F(r.SomaCA, 3), Num.F(r.Tc, 2), Num.F(r.Intensidade, 1),
                    Num.F(r.Vazao * 1000, 1), r.Secao, Num.F(r.Declividade * 100, 3), Num.F(r.CotaTerrenoMontante, 3), Num.F(r.CotaTerrenoJusante, 3),
                    Num.F(r.CotaFundoMontante, 3), Num.F(r.CotaFundoJusante, 3), Num.F(r.ProfundidadeMontante), Num.F(r.ProfundidadeJusante),
                    Num.F(Math.Min(r.RecobrimentoMontante, r.RecobrimentoJusante)), Num.F(r.LaminaRelativa), Num.F(r.Velocidade), Num.F(r.VazaoPlena * 1000, 1),
                    r.Situacao, r.Observacoes,
                });
                t.Situacao.Add(r.Situacao);
            }
        }
        else
        {
            t.Colunas.AddRange(new[] { "Trecho", "Montante", "Jusante", "L (m)", "Qi (L/s)", "Qf (L/s)", "DN (mm)", "I (m/m)", "CT mont.", "CT jus.", "CF mont.", "CF jus.",
                "Prof. mont.", "Prof. jus.", "y/D final", "v final (m/s)", "v crítica (m/s)", "σ inicial (Pa)", "Situação", "Observações" });
            foreach (var r in res.Trechos)
            {
                t.Linhas.Add(new[]
                {
                    r.Nome, r.Montante, r.Jusante, Num.F(r.Comprimento), Num.F(r.VazaoInicialLs, 3), Num.F(r.VazaoFinalLs, 3), (r.Diametro * 1000).ToString("0"),
                    Num.F(r.Declividade, 5), Num.F(r.CotaTerrenoMontante, 3), Num.F(r.CotaTerrenoJusante, 3), Num.F(r.CotaFundoMontante, 3), Num.F(r.CotaFundoJusante, 3),
                    Num.F(r.ProfundidadeMontante), Num.F(r.ProfundidadeJusante), Num.F(r.LaminaRelativa), Num.F(r.Velocidade), Num.F(r.VelocidadeCritica),
                    Num.F(r.TensaoTrativa), r.Situacao, r.Observacoes,
                });
                t.Situacao.Add(r.Situacao);
            }
        }
        t.Rodape.Add(res.Metodo);
        return t;
    }

    public static TabelaDados NotaServicoNos(Rede rede, ResultadoRede res, double intervaloEstaca = 20)
    {
        var t = new TabelaDados { Titulo = $"Nota de serviço de PVs e caixas — {rede.Nome}" };
        t.Colunas.AddRange(new[] { "Nó", "Tipo", "Estaca", "Afast. (m)", "E (m)", "N (m)", "Cota tampa", "Cota fundo", "Profundidade", "Degrau máx.", "Dispositivo", "Observações" });
        foreach (var rn in res.Nos.OrderBy(n => n.Nome, StringComparer.OrdinalIgnoreCase))
        {
            var no = rede.NoPorId(rn.NoId);
            t.Linhas.Add(new[]
            {
                rn.Nome, rn.Tipo.ToString(), double.IsNaN(rn.Estaca) ? "-" : Num.Estaca(rn.Estaca, intervaloEstaca), Num.F(rn.Afastamento),
                Num.F(rn.X, 3), Num.F(rn.Y, 3), Num.F(rn.CotaTampa, 3), Num.F(rn.CotaFundo, 3), Num.F(rn.Profundidade), Num.F(rn.DegrauMaximo),
                no?.DispositivoId ?? "", string.Join("; ", rn.Avisos),
            });
            t.Situacao.Add(rn.Avisos.Count > 0 ? "AVISO" : "OK");
        }
        return t;
    }

    public static TabelaDados Vala(Rede rede, List<QuantitativoVala> q)
    {
        var t = new TabelaDados { Titulo = $"Quantitativos de escavação de valas — {rede.Nome}" };
        t.Colunas.AddRange(new[] { "Trecho", "Seção", "L (m)", "Largura (m)", "Prof. média (m)", "Escavação (m³)", CalculoVala.Faixas[0], CalculoVala.Faixas[1], CalculoVala.Faixas[2], CalculoVala.Faixas[3],
            "Berço (m³)", "Reaterro (m³)", "Bota-fora (m³)", "Escoramento (m²)" });
        foreach (var x in q)
        {
            t.Linhas.Add(new[] { x.Trecho, x.Secao, Num.F(x.Comprimento), Num.F(x.LarguraFundo), Num.F(x.ProfundidadeMedia), Num.F(x.Escavacao),
                Num.F(x.PorFaixa[0]), Num.F(x.PorFaixa[1]), Num.F(x.PorFaixa[2]), Num.F(x.PorFaixa[3]), Num.F(x.Berco), Num.F(x.Reaterro), Num.F(x.BotaFora), Num.F(x.Escoramento) });
            t.Situacao.Add("OK");
        }
        t.Linhas.Add(new[] { "TOTAL", "", Num.F(q.Sum(x => x.Comprimento)), "", "", Num.F(q.Sum(x => x.Escavacao)),
            Num.F(q.Sum(x => x.PorFaixa[0])), Num.F(q.Sum(x => x.PorFaixa[1])), Num.F(q.Sum(x => x.PorFaixa[2])), Num.F(q.Sum(x => x.PorFaixa[3])),
            Num.F(q.Sum(x => x.Berco)), Num.F(q.Sum(x => x.Reaterro)), Num.F(q.Sum(x => x.BotaFora)), Num.F(q.Sum(x => x.Escoramento)) });
        t.Situacao.Add("OK");
        return t;
    }

    public static TabelaDados Interferencias(List<Interferencia> lista)
    {
        var t = new TabelaDados { Titulo = "Interferências entre redes" };
        t.Colunas.AddRange(new[] { "Rede A", "Trecho A", "Rede B", "Trecho B", "E", "N", "Topo inferior", "Fundo superior", "Folga (m)", "Descrição" });
        foreach (var i in lista)
        {
            t.Linhas.Add(new[] { i.RedeA, i.TrechoA, i.RedeB, i.TrechoB, Num.F(i.Ponto.X, 3), Num.F(i.Ponto.Y, 3), Num.F(i.TopoInferior, 3), Num.F(i.FundoSuperior, 3), Num.F(i.Folga), i.Descricao });
            t.Situacao.Add(i.Folga < 0 ? "ERRO" : "AVISO");
        }
        return t;
    }

    /// <summary>Resumo de materiais: extensão de tubos por seção e quantidade de nós por dispositivo.</summary>
    public static TabelaDados ResumoMateriais(Rede rede, ResultadoRede res)
    {
        var t = new TabelaDados { Titulo = $"Resumo de materiais — {rede.Nome}" };
        t.Colunas.AddRange(new[] { "Item", "Unidade", "Quantidade" });
        foreach (var g in res.Trechos.GroupBy(r => r.Secao).OrderBy(g => g.First().Diametro * g.First().Altura))
        {
            t.Linhas.Add(new[] { $"Tubulação {g.Key}", "m", Num.F(g.Sum(x => x.Comprimento)) });
            t.Situacao.Add("OK");
        }
        foreach (var g in rede.Nos.GroupBy(n => n.DispositivoId).OrderBy(g => g.Key))
        {
            var profs = g.Select(n => res.No(n.Id)?.Profundidade ?? double.NaN).Where(x => !double.IsNaN(x)).ToList();
            string faixa = profs.Count > 0 ? $" (prof. {Num.F(profs.Min())} a {Num.F(profs.Max())} m)" : "";
            t.Linhas.Add(new[] { $"{g.Key}{faixa}", "un", g.Count().ToString() });
            t.Situacao.Add("OK");
        }
        return t;
    }
}

public static class RelatorioHtml
{
    public static string Documento(string titulo, IEnumerable<string> blocosHtml, IEnumerable<Mensagem>? mensagens = null, IEnumerable<(string, string)>? parametros = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        sb.Append("<title>").Append(Texto.Html(titulo)).AppendLine("</title>");
        sb.AppendLine("""
<style>
 body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1d2433;background:#fff}
 h1{font-size:20px;margin:0 0 4px} h2{font-size:16px;margin:24px 0 8px;color:#2a4a7f}
 .sub{color:#667;margin-bottom:16px;font-size:13px}
 table{border-collapse:collapse;font-size:12px;white-space:nowrap}
 th,td{border:1px solid #c9d1e0;padding:3px 6px;text-align:right}
 th{background:#eaf0fa;position:sticky;top:0} td:first-child,th:first-child{text-align:left}
 tr.aviso td{background:#fff7e0} tr.erro td{background:#fde4e4}
 .rolagem{overflow-x:auto;max-width:100%} .nota{font-size:12px;color:#555}
 ul.msg{font-size:13px} li.Erro{color:#b00020} li.Aviso{color:#a06000}
 dl{display:grid;grid-template-columns:max-content auto;gap:2px 16px;font-size:13px} dt{color:#556}
</style></head><body>
""");
        sb.Append("<h1>").Append(Texto.Html(titulo)).AppendLine("</h1>");
        sb.Append("<div class=\"sub\">Gerado pelo PROJETA • Assistente Civil em ").Append(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).AppendLine("</div>");
        if (parametros is not null)
        {
            sb.AppendLine("<h2>Parâmetros</h2><dl>");
            foreach (var (k, v) in parametros) sb.Append("<dt>").Append(Texto.Html(k)).Append("</dt><dd>").Append(Texto.Html(v)).AppendLine("</dd>");
            sb.AppendLine("</dl>");
        }
        if (mensagens is not null && mensagens.Any())
        {
            sb.AppendLine("<h2>Mensagens</h2><ul class=\"msg\">");
            foreach (var m in mensagens) sb.Append("<li class=\"").Append(m.Gravidade).Append("\">").Append(Texto.Html(m.ToString())).AppendLine("</li>");
            sb.AppendLine("</ul>");
        }
        foreach (var b in blocosHtml) sb.AppendLine(b);
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static IEnumerable<(string, string)> ParametrosRede(Rede rede, Projeto projeto)
    {
        if (rede.Tipo == TipoRede.Esgoto)
        {
            var p = rede.Esgoto;
            yield return ("Populações (inicial / final)", $"{p.PopulacaoInicial:0} / {p.PopulacaoFinal:0} hab");
            yield return ("Consumo per capita", $"{p.ConsumoPerCapita:0} L/hab.dia");
            yield return ("C / K1 / K2", $"{Num.F(p.CoefRetorno)} / {Num.F(p.K1)} / {Num.F(p.K2)}");
            yield return ("Infiltração", $"{Num.F(p.TaxaInfiltracao, 3)} L/s.km");
            yield return ("Manning", Num.F(p.Manning, 3));
            yield return ("y/D máx.", Num.F(p.LaminaMaxima));
            yield return ("Recobrimento mínimo", $"{Num.F(p.RecobrimentoMinimo)} m");
            yield return ("Critério", p.Criterio.ToString());
        }
        else
        {
            var p = rede.Drenagem;
            var eq = projeto.Equacao(p.EquacaoId);
            yield return ("Equação de chuva", eq is null ? p.EquacaoId : $"{eq.Nome}: {eq.Descricao()}");
            yield return ("Fonte da equação", eq?.Fonte ?? "-");
            yield return ("Tempo de retorno", $"{p.TempoRetorno:0.#} anos");
            yield return ("tc mínimo", $"{p.TcMinimo:0.#} min");
            yield return ("Manning", Num.F(p.Manning, 3));
            yield return ("y/D máx.", Num.F(p.LaminaMaxima));
            yield return ("Velocidades (mín / máx)", $"{Num.F(p.VelocidadeMinima)} / {Num.F(p.VelocidadeMaxima)} m/s");
            yield return ("Recobrimento mínimo", $"{Num.F(p.RecobrimentoMinimo)} m");
            yield return ("Declividades (mín / máx)", $"{Num.F(p.DeclividadeMinima * 100, 2)}% / {Num.F(p.DeclividadeMaxima * 100, 2)}%");
            yield return ("Critério de concordância", p.Criterio.ToString());
        }
    }
}
