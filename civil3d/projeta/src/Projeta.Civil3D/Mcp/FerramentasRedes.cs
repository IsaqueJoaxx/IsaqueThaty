using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Projeta.Civil3D.Comandos;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Escavacao;
using Projeta.Core.Redes;
using Projeta.Core.Relatorios;
using Projeta.Mcp;
using TextoUtil = Projeta.Core.Comum.Texto;

namespace Projeta.Civil3D.Mcp;

/// <summary>
/// Ferramentas sobre as redes do PROJETA (drenagem, esgoto, água) guardadas no DWG.
/// Reaproveitam exatamente o que os comandos PRJ* fazem: Sessao, Operacoes.Atualizar, GeradorTabelas, RelatorioCompleto.
/// </summary>
internal static class FerramentasRedes
{
    private static readonly string[] Tabelas = { "dimensionamento", "nos", "bacias", "materiais", "vala", "forcados", "parametros", "mensagens" };

    /// <summary>Propriedades da rede (fora do grupo de parâmetros) que podem ser alteradas.</summary>
    private static readonly string[] PropsRede =
    {
        nameof(Rede.Nome), nameof(Rede.Descricao), nameof(Rede.AlturaTexto), nameof(Rede.Gerar3D), nameof(Rede.AbrirFurosNosPVs),
        nameof(Rede.DispositivoPadraoPV), nameof(Rede.DispositivoPadraoBL), nameof(Rede.PrefixoNo), nameof(Rede.PrefixoBocaLobo),
        nameof(Rede.PrefixoTrecho), nameof(Rede.Digitos),
    };

    public static void Registrar(RegistroFerramentas r)
    {
        r.Adicionar(new FerramentaDelegada("projeta_listar_redes", "Listar redes do PROJETA",
            "Lista as redes do PROJETA gravadas no desenho (drenagem, esgoto, água, genérica) com nº de nós, trechos e bacias, superfície e alinhamento " +
            "de referência, extensão e o estado do último cálculo (data, trechos com erro/aviso, profundidade máxima).",
            () => new Esquema(), ListarRedes));

        r.Adicionar(new FerramentaDelegada("projeta_detalhar_rede", "Detalhar rede do PROJETA",
            "Devolve uma tabela da rede: 'dimensionamento' (por trecho: seção, declividade, cotas de terreno e fundo, profundidades, recobrimento, Q, v, y/D, " +
            "situação e mensagens), 'nos' (nota de serviço dos PVs/caixas), 'bacias', 'materiais', 'vala' (escavação por faixa), " +
            "'forcados' (valores forçados pelo usuário em trechos e nós), 'parametros' (critérios de cálculo editáveis com projeta_alterar_parametros) " +
            "ou 'mensagens' do último cálculo. Valores refletem o ÚLTIMO cálculo — recalcule se o desenho mudou.",
            () => new Esquema()
                .Texto("rede", "Nome da rede. Pode omitir se houver só uma.")
                .Texto("tabela", "Qual tabela.", valores: Tabelas, padrao: "dimensionamento")
                .Inteiro("limite", "Máximo de linhas.", minimo: 1, padrao: 500),
            DetalharRede));

        r.Adicionar(new FerramentaDelegada("projeta_calcular_rede", "Calcular rede do PROJETA",
            "Equivale ao PRJATUALIZAR: lê as alterações do desenho, amostra o terreno na superfície da rede, dimensiona (método racional na drenagem, " +
            "NBR 9649 no esgoto) e redesenha tubos, PVs, rótulos e sólidos. Água só é sincronizada/redesenhada (o cálculo é pelo EPANET, fora daqui). " +
            "Grava no DWG (desfaz com U). Devolve o resumo e os trechos com erro/aviso.",
            () => new Esquema()
                .Texto("rede", "Nome da rede. Pode omitir se houver só uma.")
                .Booleano("todas", "Calcular todas as redes do desenho.", padrao: false)
                .Booleano("redesenhar", "Redesenhar a rede depois do cálculo.", padrao: true),
            CalcularRede, somenteLeitura: false));

        r.Adicionar(new FerramentaDelegada("projeta_alterar_parametros", "Alterar parâmetros da rede",
            "Altera critérios de cálculo da rede e recalcula. As chaves são os nomes das propriedades mostrados em projeta_detalhar_rede com tabela='parametros' " +
            "(ex. drenagem: TempoRetorno, EquacaoId, TcMinimo, Manning, LaminaMaxima, VelocidadeMinima, VelocidadeMaxima, RecobrimentoMinimo, " +
            "DeclividadeMinima, DeclividadeMaxima, Criterio, Diametros, DiametroMinimo, PermitirGalerias; esgoto: PopulacaoInicial, PopulacaoFinal, " +
            "ConsumoPerCapita, CoefRetorno, K1, K2, TaxaInfiltracao...). Também aceita dados da rede: Nome, Descricao, AlturaTexto, Gerar3D, " +
            "DispositivoPadraoPV, DispositivoPadraoBL. Declividades em m/m. Sem 'valores', só mostra os atuais. Grava no DWG (desfaz com U).",
            () => new Esquema()
                .Texto("rede", "Nome da rede. Pode omitir se houver só uma.")
                .ObjetoLivre("valores", "Parâmetro → novo valor, ex.: {\"TempoRetorno\": 25, \"RecobrimentoMinimo\": 1.2}.")
                .Booleano("recalcular", "Recalcular depois de alterar.", padrao: true)
                .Booleano("redesenhar", "Redesenhar depois de recalcular.", padrao: true),
            AlterarParametros, somenteLeitura: false));

        var itemTrecho = new Esquema()
            .Texto("trecho", "Nome do trecho (ex.: T-05).", obrigatorio: true)
            .Numero("diametro_m", "Diâmetro (ou largura da galeria) forçado, m. null remove.", anulavel: true)
            .Numero("altura_m", "Altura forçada da galeria, m. null remove.", anulavel: true)
            .Inteiro("celulas", "Nº de células/linhas em paralelo. null volta a 1.", minimo: 1, maximo: 10, anulavel: true)
            .Texto("forma", "Forma da seção.", valores: new[] { "Circular", "Retangular" })
            .Numero("declividade_m_m", "Declividade forçada em m/m (0.01 = 1%). null remove.", anulavel: true)
            .Numero("comprimento_m", "Comprimento forçado, m. null remove.", anulavel: true)
            .Numero("cota_fundo_montante", "Cota de fundo forçada na montante, m. null remove.", anulavel: true)
            .Numero("cota_fundo_jusante", "Cota de fundo forçada na jusante, m. null remove.", anulavel: true)
            .Numero("manning", "Coeficiente de Manning do trecho. null volta ao padrão da rede.", anulavel: true)
            .Texto("material", "Material do tubo.");
        var itemNo = new Esquema()
            .Texto("no", "Nome do nó (ex.: PV-03).", obrigatorio: true)
            .Numero("cota_tampa", "Cota da tampa forçada, m. null volta à cota da superfície.", anulavel: true)
            .Numero("cota_fundo", "Cota de fundo forçada (exutório), m. null remove.", anulavel: true)
            .Numero("rebaixo_m", "Rebaixo do fundo abaixo do tubo mais baixo, m. null zera.", anulavel: true)
            .Texto("dispositivo", "Id do dispositivo do modelador (ex.: PV_CIRCULAR).")
            .Numero("vazao_adicional_ls", "Drenagem: vazão pontual somada no nó, L/s. null zera.", anulavel: true)
            .Numero("contribuicao_singular_ls", "Esgoto: contribuição concentrada, L/s. null zera.", anulavel: true)
            .Numero("demanda_ls", "Água: demanda no nó, L/s. null zera.", anulavel: true);
        r.Adicionar(new FerramentaDelegada("projeta_editar_elementos", "Forçar valores em trechos e nós",
            "Força ou libera valores em trechos e nós — o mesmo que editar as colunas azuis da planilha do PRJCALCULAR — e recalcula. " +
            "Campo ausente = não mexe; campo com null = remove o valor forçado (volta ao cálculo automático). " +
            "Use projeta_detalhar_rede (tabela 'forcados') para ver o que já está forçado. Grava no DWG (desfaz com U).",
            () => new Esquema()
                .Texto("rede", "Nome da rede. Pode omitir se houver só uma.")
                .ListaObjetos("trechos", "Alterações por trecho.", itemTrecho, maxItens: 500)
                .ListaObjetos("nos", "Alterações por nó.", itemNo, maxItens: 500)
                .Booleano("recalcular", "Recalcular depois de alterar.", padrao: true)
                .Booleano("redesenhar", "Redesenhar depois de recalcular.", padrao: true),
            EditarElementos, somenteLeitura: false));

        r.Adicionar(new FerramentaDelegada("projeta_relatorio", "Gerar relatório da rede",
            "Gera o relatório da rede calculada em arquivo: 'html' (relatório completo do PRJRELATORIO: parâmetros, bacias, dimensionamento, PVs, materiais, valas) " +
            "ou 'csv' (planilha de dimensionamento + nota de serviço dos PVs, separador ';' para o Excel). Padrão: pasta 'relatorios' ao lado do DWG. " +
            "Não altera o desenho.",
            () => new Esquema()
                .Texto("rede", "Nome da rede. Pode omitir se houver só uma.")
                .Texto("formato", "Formato do arquivo.", valores: new[] { "html", "csv" }, padrao: "html")
                .Texto("pasta", "Pasta de saída (opcional).")
                .Booleano("abrir", "Abrir o arquivo no programa padrão depois de gerar.", padrao: false),
            Relatorio, somenteLeitura: false, alteraDesenho: false));
    }

    // ------------------------------------------------------------------ auxiliares

    private static JsonNode? N(double v, int casas = 3) => JsonMcp.Numero(v, casas);
    private static JsonNode? N(double? v, int casas = 3) => JsonMcp.Numero(v, casas);

    private static Rede EscolherRede(Sessao s, string? nome)
    {
        var redes = s.Projeto.Redes;
        if (redes.Count == 0) throw new ErroFerramenta("Este desenho não tem redes do PROJETA (crie com PRJREDE).");
        var lista = string.Join(", ", redes.Select(r => $"'{r.Nome}' ({r.Tipo})"));
        if (string.IsNullOrWhiteSpace(nome))
        {
            if (redes.Count == 1) return redes[0];
            throw new ErroFerramenta($"Há {redes.Count} redes; informe 'rede': {lista}.");
        }
        return s.Projeto.RedePorNome(nome.Trim()) ?? s.Projeto.RedePorId(nome.Trim())
               ?? throw new ErroFerramenta($"Rede '{nome}' não encontrada. Redes: {lista}.");
    }

    private static ResultadoRede ExigirResultado(Rede r) =>
        r.Resultado ?? throw new ErroFerramenta($"A rede '{r.Nome}' ainda não foi calculada. Use projeta_calcular_rede.");

    private static object ObjParametros(Rede r) => r.Tipo switch
    {
        TipoRede.Esgoto => r.Esgoto,
        TipoRede.Agua => r.Agua,
        _ => r.Drenagem,
    };

    private static JsonNode? ParaJson(object? v) =>
        v is null ? null : JsonSerializer.SerializeToNode(v, v.GetType(), JsonProjeta.Compacto);

    private static JsonObject LinhaTrecho(ResultadoTrecho t, TipoRede tipo)
    {
        var o = new JsonObject
        {
            ["trecho"] = t.Nome, ["montante"] = t.Montante, ["jusante"] = t.Jusante,
            ["L_m"] = N(t.Comprimento, 2), ["secao"] = t.Secao, ["forma"] = t.Forma.ToString(),
            ["diametro_m"] = N(t.Diametro), ["altura_m"] = N(t.Altura), ["celulas"] = t.Celulas,
            ["declividade_m_m"] = N(t.Declividade, 5),
            ["Q_ls"] = N(t.Vazao * 1000, 2), ["v_m_s"] = N(t.Velocidade, 2), ["y_D"] = N(t.LaminaRelativa, 3), ["Qplena_ls"] = N(t.VazaoPlena * 1000, 1),
            ["CT_mont"] = N(t.CotaTerrenoMontante), ["CT_jus"] = N(t.CotaTerrenoJusante),
            ["CF_mont"] = N(t.CotaFundoMontante), ["CF_jus"] = N(t.CotaFundoJusante),
            ["prof_mont_m"] = N(t.ProfundidadeMontante, 2), ["prof_jus_m"] = N(t.ProfundidadeJusante, 2),
            ["recobrimento_min_m"] = N(Math.Min(t.RecobrimentoMontante, t.RecobrimentoJusante), 2),
            ["situacao"] = t.Situacao,
        };
        if (tipo == TipoRede.Esgoto)
        {
            o["Qi_ls"] = N(t.VazaoInicialLs); o["Qf_ls"] = N(t.VazaoFinalLs);
            o["tensao_trativa_Pa"] = N(t.TensaoTrativa, 2); o["v_critica_m_s"] = N(t.VelocidadeCritica, 2);
        }
        else
        {
            o["area_acum_ha"] = N(t.AreaAcumuladaHa, 4); o["soma_CA_ha"] = N(t.SomaCA, 4);
            o["tc_min"] = N(t.Tc, 2); o["i_mm_h"] = N(t.Intensidade, 1);
        }
        if (t.Erros.Count > 0) o["erros"] = JsonMcp.Textos(t.Erros);
        if (t.Avisos.Count > 0) o["avisos"] = JsonMcp.Textos(t.Avisos);
        return o;
    }

    private static JsonObject LinhaNo(ResultadoNo n, Rede rede)
    {
        var no = rede.NoPorId(n.NoId);
        var o = new JsonObject
        {
            ["no"] = n.Nome, ["tipo"] = n.Tipo.ToString(),
            ["estaca"] = N(n.Estaca, 2), ["estaca_fmt"] = double.IsNaN(n.Estaca) ? null : Num.Estaca(n.Estaca), ["afastamento"] = N(n.Afastamento, 2),
            ["x"] = N(n.X), ["y"] = N(n.Y), ["cota_tampa"] = N(n.CotaTampa), ["cota_fundo"] = N(n.CotaFundo),
            ["profundidade_m"] = N(n.Profundidade, 2), ["degrau_max_m"] = N(n.DegrauMaximo, 2), ["exige_tubo_queda"] = n.ExigeTuboQueda,
            ["maior_secao_m"] = N(n.MaiorSecao), ["dispositivo"] = no?.DispositivoId,
        };
        if (n.Avisos.Count > 0) o["avisos"] = JsonMcp.Textos(n.Avisos);
        return o;
    }

    private static JsonObject TabelaJson(TabelaDados t, int limite)
    {
        var linhas = new JsonArray();
        foreach (var l in t.Linhas.Take(limite))
        {
            var o = new JsonObject();
            for (int i = 0; i < t.Colunas.Count && i < l.Length; i++) o[t.Colunas[i]] = l[i];
            linhas.Add(o);
        }
        return new JsonObject
        {
            ["titulo"] = t.Titulo, ["colunas"] = JsonMcp.Textos(t.Colunas), ["linhas"] = linhas,
            ["total"] = t.Linhas.Count, ["rodape"] = JsonMcp.Textos(t.Rodape),
        };
    }

    private static JsonObject ParametrosJson(Rede rede, Projeto p)
    {
        var o = new JsonObject
        {
            ["rede"] = new JsonObject(PropsRede.Select(n => KeyValuePair.Create(n, ParaJson(typeof(Rede).GetProperty(n)!.GetValue(rede))))),
            ["grupo"] = rede.Tipo switch { TipoRede.Esgoto => "Esgoto", TipoRede.Agua => "Agua", _ => "Drenagem" },
            ["parametros"] = ParaJson(ObjParametros(rede)),
        };
        if (rede.Tipo is TipoRede.Drenagem or TipoRede.Generica)
            o["equacoesDeChuva"] = JsonMcp.Array(p.TodasEquacoes(), e => new JsonObject { ["id"] = e.Id, ["nome"] = e.Nome });
        o["criterios"] = JsonMcp.Textos(Enum.GetNames<CriterioConcordancia>());
        return o;
    }

    private static JsonObject ResumoCalculo(Rede r)
    {
        var o = new JsonObject { ["rede"] = r.Nome, ["tipo"] = r.Tipo.ToString(), ["nos"] = r.Nos.Count, ["trechos"] = r.Trechos.Count };
        var res = r.Resultado;
        if (r.Tipo == TipoRede.Agua || res is null)
        {
            o["observacao"] = r.Tipo == TipoRede.Agua
                ? "Rede de água: sincronizada e redesenhada; o dimensionamento é pelo EPANET (PRJEPANETCALCULAR)."
                : "Nada a calcular (rede sem trechos).";
            return o;
        }
        o["metodo"] = res.Metodo;
        o["trechosComErro"] = res.Trechos.Count(t => t.Erros.Count > 0);
        o["trechosComAviso"] = res.Trechos.Count(t => t.Erros.Count == 0 && t.Avisos.Count > 0);
        o["extensao_m"] = N(res.Trechos.Sum(t => t.Comprimento), 2);
        if (res.Nos.Count > 0) o["profundidadeMaxima_m"] = N(res.Nos.Max(n => n.Profundidade), 2);
        o["secoes"] = JsonMcp.Array(res.Trechos.GroupBy(t => t.Secao).OrderBy(g => g.First().Diametro * g.First().Altura),
            g => new JsonObject { ["secao"] = g.Key, ["extensao_m"] = N(g.Sum(t => t.Comprimento), 1) });
        o["problemas"] = JsonMcp.Array(res.Trechos.Where(t => t.Erros.Count > 0 || t.Avisos.Count > 0).Take(40),
            t => new JsonObject { ["trecho"] = t.Nome, ["situacao"] = t.Situacao, ["observacoes"] = t.Observacoes });
        var msgs = res.Mensagens.Where(m => m.Gravidade != Gravidade.Info).Take(30).ToList();
        if (msgs.Count > 0) o["mensagens"] = JsonMcp.Array(msgs, m => JsonValue.Create($"[{m.Gravidade}] {m}"));
        return o;
    }

    private static string TextoResumo(Rede r)
    {
        var res = r.Resultado;
        if (r.Tipo == TipoRede.Agua || res is null) return $"{r.Nome}: sincronizada e redesenhada.";
        int e = res.Trechos.Count(t => t.Erros.Count > 0), w = res.Trechos.Count(t => t.Erros.Count == 0 && t.Avisos.Count > 0);
        return $"{r.Nome}: {res.Trechos.Count} trechos — {e} com erro, {w} com aviso.";
    }

    // ------------------------------------------------------------------ ferramentas

    private static ResultadoFerramenta ListarRedes(Argumentos a)
    {
        using var s = Sessao.Abrir();
        var arr = new JsonArray();
        foreach (var r in s.Projeto.Redes)
        {
            var res = r.Resultado;
            var it = new JsonObject
            {
                ["nome"] = r.Nome, ["tipo"] = r.Tipo.ToString(), ["descricao"] = r.Descricao,
                ["nos"] = r.Nos.Count, ["trechos"] = r.Trechos.Count, ["bacias"] = r.Bacias.Count,
                ["superficie"] = r.NomeSuperficie, ["alinhamento"] = r.NomeAlinhamento,
                ["extensao_m"] = N(r.Trechos.Sum(t => r.ComprimentoHorizontal(t)), 2),
                ["calculada"] = res is not null,
            };
            if (res is not null)
            {
                it["calculadaEm"] = res.Data.ToString("dd/MM/yyyy HH:mm");
                it["trechosComErro"] = res.Trechos.Count(t => t.Erros.Count > 0);
                it["trechosComAviso"] = res.Trechos.Count(t => t.Erros.Count == 0 && t.Avisos.Count > 0);
                if (res.Nos.Count > 0) it["profundidadeMaxima_m"] = N(res.Nos.Max(n => n.Profundidade), 2);
            }
            arr.Add(it);
        }
        var texto = arr.Count == 0
            ? "Este desenho não tem redes do PROJETA."
            : $"{arr.Count} rede(s): " + string.Join("; ", s.Projeto.Redes.Select(r =>
                $"{r.Nome} ({r.Tipo}, {r.Nos.Count} nós, {r.Trechos.Count} trechos, {(r.Resultado is null ? "não calculada" : "calculada")})")) + ".";
        return ResultadoFerramenta.Ok(texto, new JsonObject { ["redes"] = arr, ["dispositivosLongitudinais"] = s.Projeto.Longitudinais.Count });
    }

    private static ResultadoFerramenta DetalharRede(Argumentos a)
    {
        var tabela = (a.Texto("tabela") ?? "dimensionamento").Trim().ToLowerInvariant();
        if (!Tabelas.Contains(tabela)) throw new ErroArgumento($"'tabela' deve ser uma de: {string.Join(", ", Tabelas)}.");
        int lim = a.Inteiro("limite", 500);
        using var s = Sessao.Abrir();
        var rede = EscolherRede(s, a.Texto("rede"));
        var o = new JsonObject { ["rede"] = rede.Nome, ["tipo"] = rede.Tipo.ToString(), ["tabela"] = tabela };
        string texto;
        switch (tabela)
        {
            case "dimensionamento":
            {
                var res = ExigirResultado(rede);
                o["calculadaEm"] = res.Data.ToString("dd/MM/yyyy HH:mm");
                o["metodo"] = res.Metodo;
                o["trechos"] = JsonMcp.Array(res.Trechos.Take(lim), t => LinhaTrecho(t, rede.Tipo));
                o["total"] = res.Trechos.Count;
                texto = $"Dimensionamento de {rede.Nome} ({res.Trechos.Count} trechos, calculado em {res.Data:dd/MM/yyyy HH:mm}). {TextoResumo(rede)}";
                break;
            }
            case "nos":
            {
                if (rede.Resultado is { } res)
                {
                    o["nos"] = JsonMcp.Array(res.Nos.OrderBy(n => n.Nome, StringComparer.OrdinalIgnoreCase).Take(lim), n => LinhaNo(n, rede));
                    texto = $"Nota de serviço de {res.Nos.Count} nós de {rede.Nome} (último cálculo).";
                }
                else
                {
                    o["nos"] = JsonMcp.Array(rede.Nos.Take(lim), n => new JsonObject
                    {
                        ["no"] = n.Nome, ["tipo"] = n.Tipo.ToString(), ["x"] = N(n.X), ["y"] = N(n.Y),
                        ["cota_terreno"] = N(n.CotaTerreno), ["cota_tampa_forcada"] = N(n.CotaTampaForcada), ["dispositivo"] = n.DispositivoId,
                    });
                    texto = $"{rede.Nos.Count} nós de {rede.Nome} (rede ainda não calculada — sem cotas de fundo).";
                }
                o["total"] = rede.Nos.Count;
                break;
            }
            case "bacias":
                o["tabelaDados"] = TabelaJson(CmdSaidas.TabelaBacias(rede), lim);
                texto = $"{rede.Bacias.Count} bacia(s) de {rede.Nome}, área total {Num.F(rede.Bacias.Sum(b => b.Area), 4)} ha.";
                break;
            case "materiais":
                o["tabelaDados"] = TabelaJson(GeradorTabelas.ResumoMateriais(rede, ExigirResultado(rede)), lim);
                texto = $"Resumo de materiais de {rede.Nome}.";
                break;
            case "vala":
            {
                var q = CalculoVala.Calcular(ExigirResultado(rede), CmdSaidas.ParametrosVala(s.Projeto));
                o["tabelaDados"] = TabelaJson(GeradorTabelas.Vala(rede, q), lim);
                texto = $"Escavação de valas de {rede.Nome}: {Num.F(q.Sum(x => x.Escavacao), 1)} m³ no total.";
                break;
            }
            case "forcados":
            {
                o["trechos"] = JsonMcp.Array(rede.Trechos.Where(t => t.DiametroForcado.HasValue || t.AlturaForcada.HasValue || t.DeclividadeForcada.HasValue
                        || t.ComprimentoForcado.HasValue || t.CotaMontanteForcada.HasValue || t.CotaJusanteForcada.HasValue || t.Manning.HasValue || t.Celulas != 1),
                    t => new JsonObject
                    {
                        ["trecho"] = t.Nome, ["diametro_m"] = N(t.DiametroForcado), ["altura_m"] = N(t.AlturaForcada), ["celulas"] = t.Celulas,
                        ["declividade_m_m"] = N(t.DeclividadeForcada, 5), ["comprimento_m"] = N(t.ComprimentoForcado),
                        ["cota_fundo_montante"] = N(t.CotaMontanteForcada), ["cota_fundo_jusante"] = N(t.CotaJusanteForcada), ["manning"] = N(t.Manning, 4),
                    });
                o["nos"] = JsonMcp.Array(rede.Nos.Where(n => n.CotaTampaForcada.HasValue || n.CotaFundoForcada.HasValue || n.Rebaixo != 0
                        || n.VazaoAdicional != 0 || n.ContribuicaoSingular != 0 || n.Demanda != 0),
                    n => new JsonObject
                    {
                        ["no"] = n.Nome, ["cota_tampa"] = N(n.CotaTampaForcada), ["cota_fundo"] = N(n.CotaFundoForcada), ["rebaixo_m"] = N(n.Rebaixo),
                        ["vazao_adicional_ls"] = N(n.VazaoAdicional), ["contribuicao_singular_ls"] = N(n.ContribuicaoSingular), ["demanda_ls"] = N(n.Demanda),
                    });
                o["dispositivosDisponiveis"] = JsonMcp.Textos(Sessao.Bib.Dispositivos.Keys.OrderBy(k => k));
                texto = $"{((JsonArray)o["trechos"]!).Count} trecho(s) e {((JsonArray)o["nos"]!).Count} nó(s) com valores forçados em {rede.Nome}.";
                break;
            }
            case "parametros":
                o["valores"] = ParametrosJson(rede, s.Projeto);
                texto = $"Parâmetros de {rede.Nome} ({rede.Tipo}). Altere com projeta_alterar_parametros usando os mesmos nomes.";
                break;
            default: // mensagens
            {
                var res = ExigirResultado(rede);
                o["mensagens"] = JsonMcp.Array(res.Mensagens.Take(lim), m => new JsonObject { ["gravidade"] = m.Gravidade.ToString(), ["objeto"] = m.Objeto, ["texto"] = m.Texto });
                texto = $"{res.Mensagens.Count} mensagem(ns) do último cálculo de {rede.Nome}.";
                break;
            }
        }
        return ResultadoFerramenta.Ok(texto, o);
    }

    private static ResultadoFerramenta CalcularRede(Argumentos a)
    {
        bool redesenhar = a.Booleano("redesenhar", true);
        using var s = Sessao.Abrir();
        var redes = a.Booleano("todas", false) ? s.Projeto.Redes.ToList() : new List<Rede> { EscolherRede(s, a.Texto("rede")) };
        if (redes.Count == 0) throw new ErroFerramenta("Este desenho não tem redes do PROJETA.");
        foreach (var r in redes)
        {
            Ctx.Msg($"--- {r.Nome} ---");
            Operacoes.Atualizar(s, r, sincronizar: true, redesenhar: redesenhar, calcular: true);
        }
        s.SalvarEConfirmar();
        return ResultadoFerramenta.Ok(string.Join(" ", redes.Select(TextoResumo)) + " Gravado no desenho (desfaz com U).",
            new JsonObject { ["redes"] = JsonMcp.Array(redes, ResumoCalculo) });
    }

    private static (object Alvo, PropertyInfo Prop)? LocalizarPropriedade(Rede rede, string chave)
    {
        var alvo = ObjParametros(rede);
        var p = alvo.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => x.CanWrite && string.Equals(x.Name, chave, StringComparison.OrdinalIgnoreCase));
        if (p is not null) return (alvo, p);
        var nr = PropsRede.FirstOrDefault(n => string.Equals(n, chave, StringComparison.OrdinalIgnoreCase));
        return nr is null ? null : (rede, typeof(Rede).GetProperty(nr)!);
    }

    internal static object? Converter(JsonNode? valor, Type tipo, string nome)
    {
        var nt = Nullable.GetUnderlyingType(tipo);
        if (valor is null)
        {
            if (nt is not null || !tipo.IsValueType) return null;
            throw new ErroArgumento($"'{nome}' não aceita null.");
        }
        var t = nt ?? tipo;
        if (t == typeof(double)) return Argumentos.LerNumero(valor, nome);
        if (t == typeof(int))
        {
            var d = Argumentos.LerNumero(valor, nome);
            if (Math.Abs(d - Math.Round(d)) > 1e-9) throw new ErroArgumento($"'{nome}' deve ser inteiro.");
            return (int)Math.Round(d);
        }
        if (t == typeof(bool)) return new Argumentos(new JsonObject { ["v"] = valor.DeepClone() }, nome + ".").Booleano("v", false);
        if (t == typeof(string)) return new Argumentos(new JsonObject { ["v"] = valor.DeepClone() }).Texto("v");
        if (t.IsEnum)
        {
            var sv = new Argumentos(new JsonObject { ["v"] = valor.DeepClone() }).Texto("v") ?? "";
            if (Enum.TryParse(t, sv.Trim(), true, out var e) && Enum.IsDefined(t, e!)) return e;
            throw new ErroArgumento($"'{nome}' deve ser um de: {string.Join(", ", Enum.GetNames(t))}.");
        }
        if (t == typeof(List<double>))
        {
            if (valor is not JsonArray arr) throw new ErroArgumento($"'{nome}' deve ser uma lista de números.");
            var l = arr.Select((x, i) => x is null ? throw new ErroArgumento($"'{nome}[{i}]' é nulo.") : Argumentos.LerNumero(x, $"{nome}[{i}]")).ToList();
            if (l.Any(x => x <= 0)) throw new ErroArgumento($"'{nome}' só aceita valores positivos.");
            return l.Distinct().OrderBy(x => x).ToList();
        }
        if (t == typeof(List<string>))
        {
            if (valor is not JsonArray arr) throw new ErroArgumento($"'{nome}' deve ser uma lista de textos.");
            return arr.Select((x, i) => new Argumentos(new JsonObject { ["v"] = x?.DeepClone() }).Texto("v") ?? throw new ErroArgumento($"'{nome}[{i}]' é nulo.")).ToList();
        }
        throw new ErroArgumento($"'{nome}' ({t.Name}) não pode ser alterado por esta ferramenta.");
    }

    private static ResultadoFerramenta AlterarParametros(Argumentos a)
    {
        using var s = Sessao.Abrir();
        var rede = EscolherRede(s, a.Texto("rede"));
        var valores = a.Objeto("valores");
        if (valores is null || valores.Count == 0)
            return ResultadoFerramenta.Ok($"Nenhum valor informado. Parâmetros atuais de {rede.Nome}:", ParametrosJson(rede, s.Projeto));

        var mudancas = new JsonArray();
        foreach (var (chave, valor) in valores.ToList())
        {
            var loc = LocalizarPropriedade(rede, chave);
            if (loc is null)
            {
                var nomes = ObjParametros(rede).GetType().GetProperties().Where(p => p.CanWrite).Select(p => p.Name).Concat(PropsRede);
                throw new ErroArgumento($"parâmetro desconhecido '{chave}'. Disponíveis: {string.Join(", ", nomes)}.");
            }
            var (alvo, prop) = loc.Value;
            var novo = Converter(valor, prop.PropertyType, prop.Name);
            if (prop.Name == nameof(Rede.Nome))
            {
                var n = ((string?)novo ?? "").Trim();
                if (n.Length == 0) throw new ErroArgumento("'Nome' não pode ser vazio.");
                if (s.Projeto.Redes.Any(r => r != rede && string.Equals(r.Nome, n, StringComparison.OrdinalIgnoreCase))) throw new ErroArgumento($"já existe outra rede chamada '{n}'.");
                novo = n;
            }
            if (prop.Name == nameof(ParametrosDrenagem.EquacaoId) && s.Projeto.Equacao((string?)novo) is null)
                throw new ErroArgumento($"equação '{novo}' não existe. Disponíveis: {string.Join(", ", s.Projeto.TodasEquacoes().Select(e => e.Id))}.");
            if (prop.PropertyType == typeof(double) && novo is double d && d < 0)
                throw new ErroArgumento($"'{prop.Name}' não pode ser negativo.");
            var antes = ParaJson(prop.GetValue(alvo));
            prop.SetValue(alvo, novo);
            mudancas.Add(new JsonObject { ["parametro"] = prop.Name, ["antes"] = antes, ["depois"] = ParaJson(prop.GetValue(alvo)) });
        }

        bool recalc = a.Booleano("recalcular", true);
        if (recalc) Operacoes.Atualizar(s, rede, sincronizar: true, redesenhar: a.Booleano("redesenhar", true), calcular: true);
        s.SalvarEConfirmar();
        var dados = new JsonObject { ["rede"] = rede.Nome, ["mudancas"] = mudancas };
        if (recalc) dados["calculo"] = ResumoCalculo(rede);
        var desc = string.Join("; ", mudancas.Select(m => $"{m!["parametro"]}: {JsonMcp.Texto(m["antes"])} → {JsonMcp.Texto(m["depois"])}"));
        return ResultadoFerramenta.Ok($"{rede.Nome}: {desc}.{(recalc ? " " + TextoResumo(rede) : " (não recalculada)")} Gravado no desenho (desfaz com U).", dados);
    }

    private static ResultadoFerramenta EditarElementos(Argumentos a)
    {
        var itensT = a.ListaObjetos("trechos");
        var itensN = a.ListaObjetos("nos");
        if (itensT.Count == 0 && itensN.Count == 0) throw new ErroArgumento("informe 'trechos' e/ou 'nos'.");
        using var s = Sessao.Abrir();
        var rede = EscolherRede(s, a.Texto("rede"));
        var mudancas = new JsonArray();
        var trechosEditados = new List<Trecho>();
        var nosEditados = new List<No>();

        void CampoNum(Argumentos it, string chave, string elemento, Func<double?> obter, Action<double?> definir, Func<double, string?>? validar = null)
        {
            if (!it.Tem(chave)) return;
            double? novo = it.EhNulo(chave) ? null : it.Numero(chave);
            if (novo.HasValue && validar?.Invoke(novo.Value) is { } erro) throw new ErroArgumento($"{elemento}.{chave}: {erro}");
            var antes = obter();
            definir(novo);
            mudancas.Add(new JsonObject { ["elemento"] = elemento, ["campo"] = chave, ["antes"] = N(antes, 5), ["depois"] = N(obter(), 5) });
        }
        void CampoTxt(Argumentos it, string chave, string elemento, Func<string> obter, Action<string> definir, Func<string, string?>? validar = null)
        {
            if (!it.Tem(chave) || it.EhNulo(chave)) return;
            var novo = it.TextoObrigatorio(chave);
            if (validar?.Invoke(novo) is { } erro) throw new ErroArgumento($"{elemento}.{chave}: {erro}");
            var antes = obter();
            definir(novo);
            mudancas.Add(new JsonObject { ["elemento"] = elemento, ["campo"] = chave, ["antes"] = antes, ["depois"] = obter() });
        }
        string? Positivo(double v) => v > 0 ? null : "deve ser maior que zero";

        foreach (var it in itensT)
        {
            var nome = it.TextoObrigatorio("trecho");
            var t = rede.Trechos.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase) || x.Id == nome)
                    ?? throw new ErroFerramenta($"Trecho '{nome}' não existe em {rede.Nome}. Trechos: {string.Join(", ", rede.Trechos.Take(60).Select(x => x.Nome))}{(rede.Trechos.Count > 60 ? "..." : "")}.");
            CampoNum(it, "diametro_m", t.Nome, () => t.DiametroForcado, v => t.DiametroForcado = v, v => v > 0 && v <= 10 ? null : "use um valor entre 0 e 10 m");
            CampoNum(it, "altura_m", t.Nome, () => t.AlturaForcada, v => t.AlturaForcada = v, Positivo);
            CampoNum(it, "celulas", t.Nome, () => t.Celulas, v => t.Celulas = (int)Math.Round(v ?? 1), v => v >= 1 && v <= 10 && Math.Abs(v - Math.Round(v)) < 1e-9 ? null : "inteiro de 1 a 10");
            CampoNum(it, "declividade_m_m", t.Nome, () => t.DeclividadeForcada, v => t.DeclividadeForcada = v, v => v > 0 && v < 1 ? null : "em m/m, entre 0 e 1 (ex.: 0.01 = 1%)");
            CampoNum(it, "comprimento_m", t.Nome, () => t.ComprimentoForcado, v => t.ComprimentoForcado = v, Positivo);
            CampoNum(it, "cota_fundo_montante", t.Nome, () => t.CotaMontanteForcada, v => t.CotaMontanteForcada = v);
            CampoNum(it, "cota_fundo_jusante", t.Nome, () => t.CotaJusanteForcada, v => t.CotaJusanteForcada = v);
            CampoNum(it, "manning", t.Nome, () => t.Manning, v => t.Manning = v, v => v >= 0.008 && v <= 0.1 ? null : "entre 0,008 e 0,1");
            CampoTxt(it, "material", t.Nome, () => t.Material, v => t.Material = v);
            CampoTxt(it, "forma", t.Nome, () => t.Forma.ToString(), v => t.Forma = Enum.Parse<FormaSecao>(v, true),
                v => Enum.TryParse<FormaSecao>(v, true, out _) ? null : "Circular ou Retangular");
            trechosEditados.Add(t);
        }

        foreach (var it in itensN)
        {
            var nome = it.TextoObrigatorio("no");
            var n = rede.Nos.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase) || x.Id == nome)
                    ?? throw new ErroFerramenta($"Nó '{nome}' não existe em {rede.Nome}. Nós: {string.Join(", ", rede.Nos.Take(60).Select(x => x.Nome))}{(rede.Nos.Count > 60 ? "..." : "")}.");
            CampoNum(it, "cota_tampa", n.Nome, () => n.CotaTampaForcada, v => n.CotaTampaForcada = v);
            CampoNum(it, "cota_fundo", n.Nome, () => n.CotaFundoForcada, v => n.CotaFundoForcada = v);
            CampoNum(it, "rebaixo_m", n.Nome, () => n.Rebaixo, v => n.Rebaixo = v ?? 0, v => v >= 0 && v <= 5 ? null : "entre 0 e 5 m");
            CampoNum(it, "vazao_adicional_ls", n.Nome, () => n.VazaoAdicional, v => n.VazaoAdicional = v ?? 0, v => v >= 0 ? null : "não pode ser negativa");
            CampoNum(it, "contribuicao_singular_ls", n.Nome, () => n.ContribuicaoSingular, v => n.ContribuicaoSingular = v ?? 0, v => v >= 0 ? null : "não pode ser negativa");
            CampoNum(it, "demanda_ls", n.Nome, () => n.Demanda, v => n.Demanda = v ?? 0, v => v >= 0 ? null : "não pode ser negativa");
            CampoTxt(it, "dispositivo", n.Nome, () => n.DispositivoId, v => n.DispositivoId = Sessao.Bib.Dispositivos.Keys.First(k => string.Equals(k, v, StringComparison.OrdinalIgnoreCase)),
                v => Sessao.Bib.Dispositivos.Keys.Any(k => string.Equals(k, v, StringComparison.OrdinalIgnoreCase)) ? null
                    : "dispositivo inexistente. Disponíveis: " + string.Join(", ", Sessao.Bib.Dispositivos.Keys.OrderBy(k => k)));
            nosEditados.Add(n);
        }

        bool recalc = a.Booleano("recalcular", true);
        if (recalc) Operacoes.Atualizar(s, rede, sincronizar: true, redesenhar: a.Booleano("redesenhar", true), calcular: true);
        s.SalvarEConfirmar();

        var dados = new JsonObject { ["rede"] = rede.Nome, ["mudancas"] = mudancas };
        if (recalc && rede.Resultado is { } res)
        {
            dados["trechos"] = JsonMcp.Array(trechosEditados.Distinct().Select(t => res.Trecho(t.Id)).Where(x => x is not null), x => LinhaTrecho(x!, rede.Tipo));
            dados["nos"] = JsonMcp.Array(nosEditados.Distinct().Select(n => res.No(n.Id)).Where(x => x is not null), x => LinhaNo(x!, rede));
            dados["calculo"] = ResumoCalculo(rede);
        }
        return ResultadoFerramenta.Ok($"{mudancas.Count} alteração(ões) em {rede.Nome}.{(recalc ? " " + TextoResumo(rede) : " (não recalculada)")} Gravado no desenho (desfaz com U).", dados);
    }

    private static string PastaPadrao()
    {
        var doc = Ctx.Doc;
        if (doc.IsNamedDrawing && Path.GetDirectoryName(doc.Database.Filename) is { Length: > 0 } dir) return Path.Combine(dir, "relatorios");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Projeta", "relatorios");
    }

    private static ResultadoFerramenta Relatorio(Argumentos a)
    {
        var formato = (a.Texto("formato") ?? "html").Trim().ToLowerInvariant();
        if (formato is not ("html" or "csv")) throw new ErroArgumento("'formato' deve ser html ou csv.");
        using var s = Sessao.Abrir();
        var rede = EscolherRede(s, a.Texto("rede"));
        var res = ExigirResultado(rede);
        var pasta = string.IsNullOrWhiteSpace(a.Texto("pasta")) ? PastaPadrao() : a.Texto("pasta")!.Trim();
        Directory.CreateDirectory(pasta);
        var baseNome = TextoUtil.IdSeguro(rede.Nome, 60);
        var arquivos = new List<string>();
        if (formato == "html")
        {
            var arq = Path.Combine(pasta, baseNome + "_relatorio.html");
            File.WriteAllText(arq, CmdSaidas.RelatorioCompleto(rede, s.Projeto), new UTF8Encoding(false));
            arquivos.Add(arq);
        }
        else
        {
            var bom = new UTF8Encoding(true);
            var dim = Path.Combine(pasta, baseNome + "_dimensionamento.csv");
            File.WriteAllText(dim, GeradorTabelas.Dimensionamento(rede, res).ParaCsv(), bom);
            var nos = Path.Combine(pasta, baseNome + "_nota_pvs.csv");
            File.WriteAllText(nos, GeradorTabelas.NotaServicoNos(rede, res).ParaCsv(), bom);
            arquivos.Add(dim); arquivos.Add(nos);
        }
        if (a.Booleano("abrir", false)) UiUtil.AbrirNoSistema(arquivos[0]);
        return ResultadoFerramenta.Ok($"Relatório de {rede.Nome} (cálculo de {res.Data:dd/MM/yyyy HH:mm}) gravado em: {string.Join(" e ", arquivos)}.",
            new JsonObject { ["arquivos"] = JsonMcp.Textos(arquivos) });
    }
}
