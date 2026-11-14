using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Projeta.Core.Superficies;
using Projeta.Core.Terraplenagem;
using Projeta.Mcp;

namespace Projeta.Civil3D.Mcp;

/// <summary>Ferramentas de superfícies: volumes de corte e aterro entre duas superfícies.</summary>
internal static class FerramentasSuperficies
{
    public static void Registrar(RegistroFerramentas r)
    {
        r.Adicionar(new FerramentaDelegada("civil_volumes_superficies", "Volumes entre superfícies",
            "Corte e aterro entre duas superfícies do Civil 3D (base = terreno ou levantamento anterior; comparação = projeto ou levantamento novo), " +
            "pelo cálculo exato da superfície de volume TIN (criada só para o cálculo e desfeita, a menos que manter_superficie=true). " +
            "Na sobreposição inteira ou dentro de polígonos fechados (por handle ou por camada). Aplica fatores de corte (empolamento) e de aterro; " +
            "saldo = corte × fator − aterro × fator (positivo = sobra/bota-fora, negativo = falta/empréstimo). Devolve volumes por área e por faixa de altura " +
            "e grava a planilha .xlsx; com mapa=true desenha o mapa de cores com legenda na camada PRJ-VOLUMES-MAPA (desfaz com U).",
            () => new Esquema()
                .Texto("base", "Superfície base (terreno / levantamento anterior).", obrigatorio: true)
                .Texto("comparacao", "Superfície de comparação (projeto / levantamento novo).", obrigatorio: true)
                .ListaTextos("poligonos", "Handles de polilinhas/círculos fechados que delimitam as áreas de cálculo.", maxItens: 500)
                .Texto("camada_poligonos", "Alternativa: usa todas as curvas fechadas desta camada como áreas de cálculo.")
                .Numero("fator_corte", "Fator do corte (empolamento).", minimo: 0.01, padrao: 1)
                .Numero("fator_aterro", "Fator do aterro.", minimo: 0.01, padrao: 1)
                .Numero("celula", "Lado da célula da malha do mapa e das faixas (m). Padrão: automático.", minimo: 0.1)
                .Booleano("mapa", "Desenha o mapa de corte e aterro com legenda (altera o DWG).", false)
                .Booleano("manter_superficie", "Mantém a superfície de volume no desenho.", false)
                .Texto("arquivo", "Caminho do .xlsx. Padrão: pasta 'relatorios' ao lado do DWG; vazio = 'nenhum' não grava."),
            Calcular, somenteLeitura: false, limite: TimeSpan.FromMinutes(10),
            esquemaSaida: () => new Esquema().Aberto()
                .ListaObjetos("areas", "Por área: nome, area2d_m2, corte_m3, aterro_m3, saldo_m3, situacao.", new Esquema().Aberto())
                .ListaObjetos("faixas", "Por faixa de altura: faixa, area_m2, volume_m3.", new Esquema().Aberto())
                .Texto("arquivo", "Planilha gravada.", anulavel: true)));

        r.Adicionar(new FerramentaDelegada("civil_projeto_obra", "Projeto × obra (conformidade)",
            "Compara a superfície executada (levantamento da obra) com a de projeto e marca o que está fora da tolerância. " +
            "Desvio = cota da obra − cota do projeto (positivo = obra acima do projeto, falta cortar; negativo = abaixo, falta aterrar), " +
            "pela superfície de volume TIN do Civil 3D (criada só para o cálculo e desfeita). Devolve por área: % dentro/acima/abaixo da tolerância, " +
            "desvio médio, desvio-padrão, EMQ, máximo e mínimo, volumes acima/abaixo do projeto e situação (conforme quando ≥ percentual_minimo da área está dentro); " +
            "área por faixa de desvio; pontos críticos (maior excesso de cada região, com cotas e estaca); e, com alinhamento, o controle por estaca em afastamentos fixos. " +
            "Grava a planilha .xlsx; com mapa=true desenha o mapa de desvios, a legenda e os pontos críticos na camada PRJ-CONFORMIDADE-MAPA (desfaz com U).",
            () => new Esquema()
                .Texto("projeto", "Superfície de projeto.", obrigatorio: true)
                .Texto("obra", "Superfície do levantamento da obra executada.", obrigatorio: true)
                .Numero("tolerancia_acima", "Quanto a obra pode ficar acima do projeto (m).", minimo: 0, maximo: 5, padrao: 0.05)
                .Numero("tolerancia_abaixo", "Quanto a obra pode ficar abaixo do projeto (m).", minimo: 0, maximo: 5, padrao: 0.05)
                .ListaNumeros("limites", "Limites (m) das faixas de desvio além da tolerância. Padrão: 0,10 e 0,20.", maxItens: 8)
                .Numero("percentual_minimo", "Área dentro da tolerância (%) para a área ser conforme.", minimo: 0, maximo: 100, padrao: 95)
                .ListaTextos("poligonos", "Handles de curvas fechadas que delimitam as áreas de análise (trechos, platôs).", maxItens: 500)
                .Texto("camada_poligonos", "Alternativa: usa todas as curvas fechadas desta camada como áreas de análise.")
                .Texto("alinhamento", "Alinhamento do controle por estaca (opcional).")
                .Numero("estaca_inicial", "Estaca inicial do controle (m). Padrão: início do alinhamento.")
                .Numero("estaca_final", "Estaca final do controle (m). Padrão: fim do alinhamento.")
                .Numero("intervalo", "Intervalo entre as estacas do controle (m).", minimo: 0.5, padrao: 20)
                .ListaNumeros("afastamentos", "Afastamentos do controle (m, negativo = esquerda). Padrão: automático pela faixa coberta.", maxItens: 61)
                .Inteiro("pontos", "Máximo de pontos críticos.", minimo: 0, maximo: 200, padrao: 20)
                .Numero("celula", "Lado da célula da malha (m). Padrão: automático.", minimo: 0.1)
                .Booleano("mapa", "Desenha o mapa de desvios, a legenda e os pontos críticos (altera o DWG).", false)
                .Texto("arquivo", "Caminho do .xlsx. Padrão: pasta 'relatorios' ao lado do DWG; 'nenhum' não grava."),
            ProjetoObra, somenteLeitura: false, limite: TimeSpan.FromMinutes(10),
            esquemaSaida: () => new Esquema().Aberto()
                .ListaObjetos("areas", "Por área: nome, area_m2, dentro_pct, acima_pct, abaixo_pct, media_m, desvio_padrao_m, emq_m, maximo_m, minimo_m, volume_acima_m3, volume_abaixo_m3, situacao.", new Esquema().Aberto())
                .ListaObjetos("faixas", "Por faixa de desvio: faixa, area_m2, pct.", new Esquema().Aberto())
                .ListaObjetos("criticos", "Pontos críticos: n, x, y, estaca, afastamento_m, cota_projeto, cota_obra, desvio_m.", new Esquema().Aberto())
                .Texto("arquivo", "Planilha gravada.", anulavel: true)));

        r.Adicionar(new FerramentaDelegada("civil_platos", "Platôs (terraplenagem)",
            "Projeta um platô em cada polígono fechado: piso plano na cota que compensa corte e aterro (corte = aterro × fh), numa cota fixa ou na elevação " +
            "da polilinha, com caimento descendo no sentido em que o terreno desce, e taludes de corte e de aterro até o terreno. Cria a superfície de cada " +
            "platô e uma superfície final (terreno + platôs), desenha o piso, a crista do corte (vermelho), o pé do aterro (verde), os tracinhos de talude, " +
            "as cotas e a seta do caimento (camadas PRJ-PLATO-*), calcula os volumes exatos pela superfície de volume TIN dentro de cada área de " +
            "terraplenagem e grava a planilha .xlsx (aba Platôs e as abas de volumes). Altera o DWG (desfaz com U).",
            () => new Esquema()
                .Texto("terreno", "Superfície do terreno natural.", obrigatorio: true)
                .ListaTextos("poligonos", "Handles das polilinhas/círculos fechados dos platôs.", maxItens: 100)
                .Texto("camada_poligonos", "Alternativa: usa todas as curvas fechadas desta camada como platôs.")
                .Texto("cota", "'compensar' (padrão), 'polilinha' (elevação da polilinha; sem elevação compensa) ou um número = cota fixa do piso no centro (m).")
                .Numero("fh", "Fator de homogeneização (m³ de corte por m³ de aterro compactado).", minimo: 0.01, maximo: 3, padrao: 1.25)
                .Numero("caimento_pct", "Caimento do piso (%), descendo como o terreno; 0 = em nível.", minimo: 0, maximo: 20, padrao: 0.5)
                .Numero("talude_corte", "Talude de corte (H por 1 V).", minimo: 0.01, maximo: 20, padrao: 1.5)
                .Numero("talude_aterro", "Talude de aterro (H por 1 V).", minimo: 0.01, maximo: 20, padrao: 2)
                .Numero("celula", "Lado da célula da malha de cálculo (m). Padrão: automático pela área.", minimo: 0.1)
                .Booleano("mapa", "Desenha também o mapa de corte e aterro (camada PRJ-VOLUMES-MAPA).", false)
                .Texto("arquivo", "Caminho do .xlsx. Padrão: pasta 'relatorios' ao lado do DWG; 'nenhum' não grava."),
            Platos, somenteLeitura: false, limite: TimeSpan.FromMinutes(15),
            esquemaSaida: () => new Esquema().Aberto()
                .ListaObjetos("platos", "Por platô: nome, cota_m, compensada, cota_min_m, cota_max_m, caimento_pct, azimute_caimento, area_piso_m2, area_terraplenagem_m2, corte_m3, aterro_m3, saldo_m3, situacao, altura_corte_m, altura_aterro_m.", new Esquema().Aberto())
                .Texto("superficie_final", "Superfície final criada.")
                .Texto("arquivo", "Planilha gravada.", anulavel: true)));

        r.Adicionar(new FerramentaDelegada("civil_volumes_secoes", "Volumes por seções (cubação)",
            "Nota de cubação a partir de um grupo de linhas de amostragem (sample lines) do Civil 3D. Origem 'materiais': lê áreas e volumes da " +
            "lista de materiais do próprio Civil (com os fatores e a correção de curva dela). Origem 'superficies': calcula, em cada seção, as áreas de " +
            "corte (terreno acima do projeto) e aterro entre duas fontes amostradas (terreno e projeto: superfície TIN ou do corredor), na faixa em que " +
            "as duas existem, e os volumes pelas áreas médias com fatores de corte e de aterro; se o grupo tiver lista de materiais, confere as áreas " +
            "com as do Civil. Devolve por estaca: áreas, volumes, compensação lateral e ordenada de Brückner; totais e saldo; grava a planilha .xlsx. " +
            "Sem 'grupo' nem 'alinhamento', lista os grupos, listas de materiais e fontes disponíveis.",
            () => new Esquema()
                .Texto("alinhamento", "Alinhamento do grupo de seções.")
                .Texto("grupo", "Nome do grupo de seções. Padrão: o único do alinhamento.")
                .Texto("origem", "'materiais' (padrão quando o grupo tem lista de materiais) ou 'superficies'.")
                .Texto("lista", "Nome da lista de materiais. Padrão: a primeira.")
                .Texto("terreno", "Fonte do terreno (nome da superfície amostrada), na origem 'superficies'.")
                .Texto("projeto", "Fonte do projeto (superfície do corredor ou TIN), na origem 'superficies'.")
                .Booleano("amostrar", "Na origem 'superficies', amostra no grupo a superfície que ainda não for amostrada (altera o DWG).", false)
                .Numero("fator_corte", "Fator do corte (origem 'superficies').", minimo: 0.01, maximo: 3, padrao: 1)
                .Numero("fator_aterro", "Fator do aterro / homogeneização (origem 'superficies').", minimo: 0.01, maximo: 3, padrao: 1.25)
                .Numero("estaca_inicial", "Estaca inicial (m). Padrão: a primeira seção.")
                .Numero("estaca_final", "Estaca final (m). Padrão: a última seção.")
                .Booleano("incluir_estacas", "Devolve a tabela por estaca no resultado.", true)
                .Texto("arquivo", "Caminho do .xlsx. Padrão: pasta 'relatorios' ao lado do DWG; 'nenhum' não grava."),
            VolumesSecoes, somenteLeitura: false, limite: TimeSpan.FromMinutes(10),
            esquemaSaida: () => new Esquema().Aberto()
                .ListaObjetos("estacas", "Por estaca: estaca, area_corte_m2, area_aterro_m2, volume_corte_m3, volume_aterro_m3, compensacao_m3, bruckner_m3, obs.", new Esquema().Aberto())
                .Texto("arquivo", "Planilha gravada.", anulavel: true)));
    }

    private static ResultadoFerramenta VolumesSecoes(Argumentos a)
    {
        var db = Ctx.Db;
        Cubacao cub;
        bool alterou = false;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var grupos = CubacaoCivil.Grupos(tr);
            if (grupos.Count == 0) throw new ErroFerramenta("O desenho não tem grupos de seções (sample lines).");
            var nomeAl = a.Texto("alinhamento")?.Trim();
            var nomeG = a.Texto("grupo")?.Trim();
            var cand = grupos.Where(g => (string.IsNullOrEmpty(nomeAl) || string.Equals(g.NomeAlinhamento, nomeAl, StringComparison.OrdinalIgnoreCase)) &&
                                         (string.IsNullOrEmpty(nomeG) || string.Equals(g.Nome, nomeG, StringComparison.OrdinalIgnoreCase))).ToList();
            if (cand.Count != 1)
            {
                var lista = new JsonArray();
                foreach (var g in grupos)
                    lista.Add(new JsonObject
                    {
                        ["alinhamento"] = g.NomeAlinhamento, ["grupo"] = g.Nome, ["secoes"] = g.Secoes,
                        ["listas_materiais"] = JsonMcp.Array(CubacaoCivil.Listas(tr, g.Id), l => JsonValue.Create($"{l.Nome}: {l.Materiais}")),
                        ["fontes"] = JsonMcp.Array(CubacaoCivil.Fontes(tr, g.Id), f => JsonValue.Create(f.Rotulo)),
                    });
                tr.Commit();
                var msg = cand.Count == 0 ? "Nenhum grupo com esse alinhamento/nome." : "Há mais de um grupo; informe 'alinhamento' e 'grupo'.";
                return ResultadoFerramenta.Ok($"{msg} Grupos: " + string.Join("; ", grupos.Select(g => g.Rotulo)) + ".", new JsonObject { ["grupos"] = lista });
            }
            var grupo = cand[0];
            var listas = CubacaoCivil.Listas(tr, grupo.Id);
            var origem = (a.Texto("origem") ?? (listas.Count > 0 ? "materiais" : "superficies")).Trim().ToLowerInvariant();
            double ini = a.Numero("estaca_inicial", double.NaN), fim = a.Numero("estaca_final", double.NaN);
            CubacaoCivil.Entrada e;
            if (origem.StartsWith("mat"))
            {
                if (listas.Count == 0) throw new ErroFerramenta($"O grupo {grupo.Nome} não tem lista de materiais; use origem 'superficies'.");
                var nl = a.Texto("lista")?.Trim();
                var l = string.IsNullOrEmpty(nl) ? listas[0] : listas.FirstOrDefault(x => string.Equals(x.Nome, nl, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ErroFerramenta($"Lista '{nl}' não encontrada. Existentes: {string.Join(", ", listas.Select(x => $"'{x.Nome}'"))}.");
                e = new CubacaoCivil.Entrada { Grupo = grupo.Id, Lista = l.Guid, EstacaInicial = ini, EstacaFinal = fim };
            }
            else if (origem.StartsWith("sup"))
            {
                var fontes = CubacaoCivil.Fontes(tr, grupo.Id);
                CubacaoCivil.Fonte Achar(string campo)
                {
                    var n = a.TextoObrigatorio(campo).Trim();
                    return fontes.FirstOrDefault(f => string.Equals(f.Nome, n, StringComparison.OrdinalIgnoreCase))
                           ?? fontes.FirstOrDefault(f => f.Nome.StartsWith(n, StringComparison.OrdinalIgnoreCase))
                           ?? throw new ErroFerramenta($"Fonte '{n}' não encontrada. Disponíveis: {string.Join(", ", fontes.Select(f => $"'{f.Rotulo}'"))}.");
                }
                var t = Achar("terreno");
                var p = Achar("projeto");
                if (t.Id == p.Id) throw new ErroArgumento("terreno e projeto devem ser fontes diferentes.");
                foreach (var f in new[] { t, p }.Where(f => !f.Amostrada))
                {
                    if (!a.Booleano("amostrar", false)) throw new ErroFerramenta($"'{f.Nome}' não é amostrada no grupo {grupo.Nome}; use amostrar=true ou amostre pelo Civil.");
                    if (!CubacaoCivil.Amostrar(tr, grupo.Id, f.Id)) throw new ErroFerramenta($"Não consegui amostrar '{f.Nome}' no grupo.");
                    alterou = true;
                }
                e = new CubacaoCivil.Entrada
                {
                    Grupo = grupo.Id, Terreno = t.Id, Projeto = p.Id, FatorCorte = a.Numero("fator_corte", 1), FatorAterro = a.Numero("fator_aterro", 1.25),
                    EstacaInicial = ini, EstacaFinal = fim,
                };
            }
            else throw new ErroArgumento("origem deve ser 'materiais' ou 'superficies'.");
            try { cub = CubacaoCivil.Calcular(tr, e); }
            catch (InvalidOperationException ex) { throw new ErroFerramenta(ex.Message); }
            catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }
            tr.Commit();
        }

        string? arq = a.Texto("arquivo")?.Trim();
        if (string.Equals(arq, "nenhum", StringComparison.OrdinalIgnoreCase)) arq = null;
        else
        {
            if (string.IsNullOrWhiteSpace(arq))
            {
                var pasta = Ctx.Doc.IsNamedDrawing && !string.IsNullOrEmpty(db.Filename)
                    ? Path.Combine(Path.GetDirectoryName(db.Filename)!, "relatorios")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PROJETA");
                arq = Path.Combine(pasta, Texto.IdSeguro($"cubacao_{cub.Alinhamento}", 80) + ".xlsx");
            }
            if (!arq.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) arq += ".xlsx";
            try { cub.Salvar(arq); }
            catch (IOException ex) { throw new ErroFerramenta(ex.Message); }
        }

        static JsonNode? N(double v) => FerramentasCivil.N(v, 2);
        JsonArray? estacas = null;
        if (a.Booleano("incluir_estacas", true))
        {
            estacas = new JsonArray();
            foreach (var l in cub.Linhas)
                estacas.Add(new JsonObject
                {
                    ["estaca"] = cub.Estaca(l.Estaca), ["m"] = N(l.Estaca), ["area_corte_m2"] = N(l.AreaCorte), ["area_aterro_m2"] = N(l.AreaAterro),
                    ["volume_corte_m3"] = N(l.VolumeCorte), ["volume_aterro_m3"] = N(l.VolumeAterro), ["compensacao_m3"] = N(l.Compensacao),
                    ["bruckner_m3"] = N(l.Bruckner), ["obs"] = l.Obs.Length == 0 ? null : l.Obs,
                });
        }
        JsonObject? conf = null;
        if (cub.Conferencia.Count > 0)
        {
            var (d, est) = cub.MaiorDiferenca();
            conf = new JsonObject { ["lista"] = cub.ConferenciaNome, ["maior_diferenca_area_m2"] = FerramentasCivil.N(d, 3), ["estaca"] = double.IsNaN(est) ? null : cub.Estaca(est) };
        }
        var texto = $"Cubação {cub.Alinhamento} / {cub.Grupo}, {cub.Linhas.Count} seções ({cub.Origem}): corte {cub.TotalCorte.ToString("#,##0", Num.Br)} m³, " +
                    $"aterro {cub.TotalAterro.ToString("#,##0", Num.Br)} m³ (× {cub.FatorAterro.ToString("0.###", Num.Br)} = {cub.TotalAterroFator.ToString("#,##0", Num.Br)}), " +
                    $"compensação lateral {cub.TotalCompensacao.ToString("#,##0", Num.Br)} m³, saldo {cub.Saldo.ToString("#,##0", Num.Br)} m³ ({cub.Situacao})" +
                    (conf is null ? "" : $"; maior diferença de área para o Civil {cub.MaiorDiferenca().Diferenca.ToString("0.000", Num.Br)} m²") +
                    (alterou ? "; superfície passou a ser amostrada no grupo" : "") + (arq is null ? "." : $"; planilha {arq}.") +
                    (cub.Avisos.Count > 0 ? " Avisos: " + string.Join(" ", cub.Avisos) : "");
        return ResultadoFerramenta.Ok(texto, new JsonObject
        {
            ["alinhamento"] = cub.Alinhamento, ["grupo"] = cub.Grupo, ["origem"] = cub.Origem, ["volumes_do_civil"] = cub.VolumesDoCivil,
            ["fator_corte"] = cub.FatorCorte, ["fator_aterro"] = cub.FatorAterro, ["secoes"] = cub.Linhas.Count,
            ["corte_m3"] = N(cub.TotalCorte), ["aterro_m3"] = N(cub.TotalAterro), ["corte_fator_m3"] = N(cub.TotalCorteFator), ["aterro_fator_m3"] = N(cub.TotalAterroFator),
            ["compensacao_lateral_m3"] = N(cub.TotalCompensacao), ["saldo_m3"] = N(cub.Saldo), ["situacao"] = cub.Situacao,
            ["conferencia"] = conf, ["estacas"] = estacas, ["arquivo"] = arq, ["avisos"] = JsonMcp.Array(cub.Avisos, x => JsonValue.Create(x)),
        });
    }

    private static ResultadoFerramenta Platos(Argumentos a)
    {
        var db = Ctx.Db;
        var par = new ParametrosPlato
        {
            Fh = a.Numero("fh", 1.25), Caimento = a.Numero("caimento_pct", 0.5) / 100,
            TaludeCorte = a.Numero("talude_corte", 1.5), TaludeAterro = a.Numero("talude_aterro", 2), Celula = a.Numero("celula", 0),
        };
        try { par.Validar(); } catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }
        var cotaTxt = (a.Texto("cota") ?? "compensar").Trim();
        bool polilinha = string.Equals(cotaTxt, "polilinha", StringComparison.OrdinalIgnoreCase);
        double fixa = double.NaN;
        if (!polilinha && !string.Equals(cotaTxt, "compensar", StringComparison.OrdinalIgnoreCase) && cotaTxt.Length > 0)
        {
            if (!double.TryParse(cotaTxt.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fixa))
                throw new ErroArgumento("cota deve ser 'compensar', 'polilinha' ou um número.");
        }

        PlatoCivil.Saida s;
        RelatorioPlatos rel;
        int solidos = 0;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var sups = VolumesCivil.Superficies(tr);
            var tn = AcharSuperficie(sups, a, "terreno");
            var lidos = LerPoligonos(a, tr, db);
            if (lidos.Count == 0) throw new ErroArgumento("Informe os polígonos dos platôs (poligonos ou camada_poligonos).");
            var polis = new List<PlatoCivil.Poligono>();
            for (int i = 0; i < lidos.Count; i++)
            {
                double cota = fixa;
                if (polilinha)
                {
                    var id = Ctx.IdDeHandle(db, lidos[i].Handle);
                    cota = !id.IsNull && tr.GetObject(id, OpenMode.ForRead) is Polyline pl && pl.Elevation != 0 ? pl.Elevation : double.NaN;
                }
                polis.Add(new PlatoCivil.Poligono($"Platô {i + 1}", lidos[i].Vertices, lidos[i].Handle, cota));
            }
            try { s = PlatoCivil.Executar(tr, db, new PlatoCivil.Entrada { Terreno = tn, Poligonos = polis, Parametros = par, Desenho = Ctx.Doc.Name }); }
            catch (InvalidOperationException ex) { throw new ErroFerramenta(ex.Message); }
            catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }
            rel = s.Relatorio;
            if (a.Booleano("mapa", false) && s.Volumes is { } sv)
            {
                foreach (var id in VolumesCivil.EntidadesDoMapa(tr, db)) ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
                solidos = VolumesCivil.DesenharMapa(tr, db, sv.Malha, sv.Relatorio);
            }
            tr.Commit();
        }

        string? arq = a.Texto("arquivo")?.Trim();
        if (string.Equals(arq, "nenhum", StringComparison.OrdinalIgnoreCase)) arq = null;
        else
        {
            if (string.IsNullOrWhiteSpace(arq))
            {
                var pasta = Ctx.Doc.IsNamedDrawing && !string.IsNullOrEmpty(db.Filename)
                    ? Path.Combine(Path.GetDirectoryName(db.Filename)!, "relatorios")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PROJETA");
                arq = Path.Combine(pasta, Texto.IdSeguro($"platos_{rel.Terreno}", 80) + ".xlsx");
            }
            if (!arq.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) arq += ".xlsx";
            try { rel.Salvar(arq, s.Volumes?.Relatorio); }
            catch (IOException ex) { throw new ErroFerramenta(ex.Message); }
        }

        static JsonNode? N(double v, int d = 2) => FerramentasCivil.N(v, d);
        var platos = new JsonArray();
        foreach (var p in rel.Platos)
            platos.Add(new JsonObject
            {
                ["nome"] = p.Nome, ["cota_m"] = N(p.Cota, 3), ["compensada"] = p.Compensada, ["cota_min_m"] = N(p.CotaMin, 3), ["cota_max_m"] = N(p.CotaMax, 3),
                ["caimento_pct"] = N(p.Caimento * 100), ["azimute_caimento"] = N(p.Azimute, 0),
                ["area_piso_m2"] = N(p.AreaPiso), ["area_terraplenagem_m2"] = N(p.AreaTerraplenagem),
                ["corte_m3"] = N(RelatorioPlatos.C(p)), ["aterro_m3"] = N(RelatorioPlatos.A(p)), ["saldo_m3"] = N(rel.Saldo(p)), ["situacao"] = rel.Situacao(p),
                ["volumes_exatos"] = !double.IsNaN(p.Corte), ["altura_corte_m"] = N(p.AlturaCorte), ["altura_aterro_m"] = N(p.AlturaAterro),
                ["crista_m"] = N(p.ExtensaoCrista, 1), ["pe_m"] = N(p.ExtensaoPe, 1), ["handle"] = p.Handle,
            });
        var texto = string.Join(" ", rel.Platos.Select(p =>
                        $"{p.Nome}: piso {p.Cota.ToString("0.00", Num.Br)}{(p.Compensada ? " (compensada)" : "")}, corte {RelatorioPlatos.C(p).ToString("#,##0", Num.Br)} m³, " +
                        $"aterro {RelatorioPlatos.A(p).ToString("#,##0", Num.Br)} m³, saldo {rel.Saldo(p).ToString("#,##0", Num.Br)} m³ ({rel.Situacao(p)}).")) +
                    $" Superfície final {s.SuperficieFinal}" + (solidos > 0 ? $"; mapa com {solidos} retângulos" : "") + (arq is null ? "." : $"; planilha {arq}.") +
                    (rel.Avisos.Count > 0 ? " Avisos: " + string.Join(" ", rel.Avisos) : "");
        return ResultadoFerramenta.Ok(texto, new JsonObject
        {
            ["terreno"] = rel.Terreno, ["fh"] = par.Fh, ["talude_corte"] = par.TaludeCorte, ["talude_aterro"] = par.TaludeAterro,
            ["platos"] = platos, ["superficies_plato"] = JsonMcp.Array(s.SuperficiesPlato, x => JsonValue.Create(x)),
            ["superficie_final"] = s.SuperficieFinal, ["arquivo"] = arq, ["avisos"] = JsonMcp.Array(rel.Avisos, x => JsonValue.Create(x)),
        });
    }

    private static ResultadoFerramenta Calcular(Argumentos a)
    {
        var db = Ctx.Db;
        VolumesCivil.Entrada e;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var sups = VolumesCivil.Superficies(tr);
            ObjectId Achar(string campo)
            {
                var n = a.TextoObrigatorio(campo).Trim();
                var it = sups.FirstOrDefault(x => string.Equals(x.Nome, n, StringComparison.OrdinalIgnoreCase));
                if (it.Id.IsNull) throw new ErroFerramenta($"Superfície '{n}' não encontrada. Existentes: {string.Join(", ", sups.Select(x => $"'{x.Nome}'"))}.");
                return it.Id;
            }
            var b = Achar("base");
            var c = Achar("comparacao");
            var pols = new List<VolumesCivil.Poligono>();
            void Usar(ObjectId id)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Curve cv) return;
                var v = VolumesCivil.Vertices(cv);
                if (v is not null) pols.Add(new VolumesCivil.Poligono($"Polígono {pols.Count + 1} ({cv.Layer})", v, id.Handle.ToString()));
            }
            foreach (var h in a.ListaTextos("poligonos"))
            {
                var id = Ctx.IdDeHandle(db, h);
                if (id.IsNull) throw new ErroArgumento($"handle '{h}' não encontrado no desenho.");
                Usar(id);
            }
            var camada = a.Texto("camada_poligonos")?.Trim();
            if (!string.IsNullOrEmpty(camada))
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                foreach (ObjectId id in ms)
                    if (tr.GetObject(id, OpenMode.ForRead) is Curve cv && string.Equals(cv.Layer, camada, StringComparison.OrdinalIgnoreCase)) Usar(id);
                if (pols.Count == 0) throw new ErroFerramenta($"Nenhuma curva fechada na camada '{camada}'.");
            }
            if ((a.ListaTextos("poligonos").Count > 0 || !string.IsNullOrEmpty(camada)) && pols.Count == 0)
                throw new ErroFerramenta("Nenhum dos objetos informados é uma curva fechada.");
            e = new VolumesCivil.Entrada
            {
                Base = b, Comparacao = c, Poligonos = pols,
                FatorCorte = a.Numero("fator_corte", 1), FatorAterro = a.Numero("fator_aterro", 1),
                Celula = a.Numero("celula", 0), ManterSuperficie = a.Booleano("manter_superficie", false), Desenho = Ctx.Doc.Name,
            };
            tr.Commit();
        }

        VolumesCivil.Saida s;
        try { s = VolumesCivil.Calcular(db, e); }
        catch (InvalidOperationException ex) { throw new ErroFerramenta(ex.Message); }
        catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }
        var rel = s.Relatorio;

        int solidos = 0;
        if (a.Booleano("mapa", false))
        {
            using var tr = db.TransactionManager.StartTransaction();
            foreach (var id in VolumesCivil.EntidadesDoMapa(tr, db)) ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
            solidos = VolumesCivil.DesenharMapa(tr, db, s.Malha, rel);
            tr.Commit();
        }

        string? arq = a.Texto("arquivo")?.Trim();
        if (string.Equals(arq, "nenhum", StringComparison.OrdinalIgnoreCase)) arq = null;
        else
        {
            if (string.IsNullOrWhiteSpace(arq))
            {
                var pasta = Ctx.Doc.IsNamedDrawing && !string.IsNullOrEmpty(db.Filename)
                    ? Path.Combine(Path.GetDirectoryName(db.Filename)!, "relatorios")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PROJETA");
                arq = Path.Combine(pasta, Texto.IdSeguro($"volumes_{rel.Base}_x_{rel.Comparacao}", 80) + ".xlsx");
            }
            if (!arq.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) arq += ".xlsx";
            try { rel.Salvar(arq); }
            catch (IOException ex) { throw new ErroFerramenta(ex.Message); }
        }

        var areas = new JsonArray();
        foreach (var x in rel.Areas.Concat(rel.Areas.Count > 1 ? new[] { rel.Total } : Array.Empty<AreaVolume>()))
            areas.Add(new JsonObject
            {
                ["nome"] = x.Nome, ["area2d_m2"] = FerramentasCivil.N(x.Area2D, 2), ["corte_m3"] = FerramentasCivil.N(x.Corte, 2),
                ["aterro_m3"] = FerramentasCivil.N(x.Aterro, 2), ["saldo_m3"] = FerramentasCivil.N(rel.Saldo(x), 2),
                ["situacao"] = rel.SituacaoDe(x),
            });
        var faixas = new JsonArray();
        foreach (var f in rel.Faixas)
            faixas.Add(new JsonObject { ["faixa"] = f.Rotulo, ["area_m2"] = FerramentasCivil.N(f.Area, 1), ["volume_m3"] = FerramentasCivil.N(f.Volume, 1) });
        var t = rel.Total;
        var texto = $"Base {rel.Base} × comparação {rel.Comparacao}: corte {t.Corte.ToString("#,##0.0", Num.Br)} m³, aterro {t.Aterro.ToString("#,##0.0", Num.Br)} m³, " +
                    $"saldo {rel.Saldo(t).ToString("#,##0.0", Num.Br)} m³ ({rel.SituacaoDe(t)}) em {rel.Areas.Count} área(s)" +
                    (solidos > 0 ? $"; mapa com {solidos} retângulos" : "") + (arq is null ? "." : $"; planilha {arq}.") +
                    (rel.Avisos.Count > 0 ? " Avisos: " + string.Join(" ", rel.Avisos) : "");
        return ResultadoFerramenta.Ok(texto, new JsonObject
        {
            ["base"] = rel.Base, ["comparacao"] = rel.Comparacao, ["fatorCorte"] = rel.FatorCorte, ["fatorAterro"] = rel.FatorAterro,
            ["celula_m"] = rel.Celula, ["diferencaMalha_pct"] = FerramentasCivil.N(rel.DiferencaMalha(), 2),
            ["areas"] = areas, ["faixas"] = faixas, ["arquivo"] = arq, ["superficieVolume"] = s.SuperficieVolume,
            ["avisos"] = JsonMcp.Array(rel.Avisos, x => JsonValue.Create(x)),
        });
    }

    /// <summary>Superfície pelo nome (sem diferenciar maiúsculas); erro com a lista das existentes.</summary>
    private static ObjectId AcharSuperficie(List<(ObjectId Id, string Nome)> sups, Argumentos a, string campo)
    {
        var n = a.TextoObrigatorio(campo).Trim();
        var it = sups.FirstOrDefault(x => string.Equals(x.Nome, n, StringComparison.OrdinalIgnoreCase));
        if (it.Id.IsNull) throw new ErroFerramenta($"Superfície '{n}' não encontrada. Existentes: {string.Join(", ", sups.Select(x => $"'{x.Nome}'"))}.");
        return it.Id;
    }

    /// <summary>Curvas fechadas pelos argumentos 'poligonos' (handles) e 'camada_poligonos'.</summary>
    private static List<VolumesCivil.Poligono> LerPoligonos(Argumentos a, Transaction tr, Database db)
    {
        var pols = new List<VolumesCivil.Poligono>();
        void Usar(ObjectId id)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Curve cv) return;
            var v = VolumesCivil.Vertices(cv);
            if (v is not null) pols.Add(new VolumesCivil.Poligono($"Polígono {pols.Count + 1} ({cv.Layer})", v, id.Handle.ToString()));
        }
        foreach (var h in a.ListaTextos("poligonos"))
        {
            var id = Ctx.IdDeHandle(db, h);
            if (id.IsNull) throw new ErroArgumento($"handle '{h}' não encontrado no desenho.");
            Usar(id);
        }
        var camada = a.Texto("camada_poligonos")?.Trim();
        if (!string.IsNullOrEmpty(camada))
        {
            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            foreach (ObjectId id in ms)
                if (tr.GetObject(id, OpenMode.ForRead) is Curve cv && string.Equals(cv.Layer, camada, StringComparison.OrdinalIgnoreCase)) Usar(id);
            if (pols.Count == 0) throw new ErroFerramenta($"Nenhuma curva fechada na camada '{camada}'.");
        }
        if ((a.ListaTextos("poligonos").Count > 0 || !string.IsNullOrEmpty(camada)) && pols.Count == 0)
            throw new ErroFerramenta("Nenhum dos objetos informados é uma curva fechada.");
        return pols;
    }

    private static ResultadoFerramenta ProjetoObra(Argumentos a)
    {
        var db = Ctx.Db;
        ConformidadeCivil.Entrada e;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var sups = VolumesCivil.Superficies(tr);
            var p = AcharSuperficie(sups, a, "projeto");
            var o = AcharSuperficie(sups, a, "obra");
            var alId = ObjectId.Null;
            var nomeAl = a.Texto("alinhamento")?.Trim();
            if (!string.IsNullOrEmpty(nomeAl))
            {
                var als = CivilUtil.Alinhamentos(tr);
                var it = als.FirstOrDefault(x => string.Equals(x.Nome, nomeAl, StringComparison.OrdinalIgnoreCase));
                if (it.Id.IsNull) throw new ErroFerramenta($"Alinhamento '{nomeAl}' não encontrado. Existentes: {string.Join(", ", als.Select(x => $"'{x.Nome}'"))}.");
                alId = it.Id;
            }
            var lim = a.ListaNumeros("limites");
            e = new ConformidadeCivil.Entrada
            {
                Projeto = p, Obra = o, Poligonos = LerPoligonos(a, tr, db),
                Tolerancia = new ToleranciaObra
                {
                    Acima = a.Numero("tolerancia_acima", 0.05), Abaixo = a.Numero("tolerancia_abaixo", 0.05),
                    Limites = lim.Count > 0 ? lim.ToList() : new List<double> { 0.10, 0.20 },
                    PercentualMinimo = a.Numero("percentual_minimo", 95),
                },
                Alinhamento = alId,
                EstacaInicial = a.Numero("estaca_inicial", double.NaN), EstacaFinal = a.Numero("estaca_final", double.NaN),
                Intervalo = a.Numero("intervalo", 20), Afastamentos = a.ListaNumeros("afastamentos"),
                Pontos = a.Inteiro("pontos", 20), Celula = a.Numero("celula", 0), Desenho = Ctx.Doc.Name,
            };
            tr.Commit();
        }

        ConformidadeCivil.Saida s;
        try { s = ConformidadeCivil.Calcular(db, e); }
        catch (InvalidOperationException ex) { throw new ErroFerramenta(ex.Message); }
        catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }
        var rel = s.Relatorio;
        if (!(rel.Geral.Area > 0)) throw new ErroFerramenta(string.Join(" ", rel.Avisos));

        int solidos = 0;
        if (a.Booleano("mapa", false))
        {
            using var tr = db.TransactionManager.StartTransaction();
            foreach (var id in ConformidadeCivil.EntidadesDoMapa(tr, db)) ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
            solidos = ConformidadeCivil.DesenharMapa(tr, db, s.Malha, rel);
            tr.Commit();
        }

        string? arq = a.Texto("arquivo")?.Trim();
        if (string.Equals(arq, "nenhum", StringComparison.OrdinalIgnoreCase)) arq = null;
        else
        {
            if (string.IsNullOrWhiteSpace(arq))
            {
                var pasta = Ctx.Doc.IsNamedDrawing && !string.IsNullOrEmpty(db.Filename)
                    ? Path.Combine(Path.GetDirectoryName(db.Filename)!, "relatorios")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PROJETA");
                arq = Path.Combine(pasta, Texto.IdSeguro($"projeto_x_obra_{rel.Projeto}_x_{rel.Obra}", 80) + ".xlsx");
            }
            if (!arq.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) arq += ".xlsx";
            try { rel.Salvar(arq); }
            catch (IOException ex) { throw new ErroFerramenta(ex.Message); }
        }

        static JsonNode? N3(double v) => FerramentasCivil.N(v, 3);
        var areas = new JsonArray();
        foreach (var x in rel.Linhas)
            areas.Add(new JsonObject
            {
                ["nome"] = x.Nome, ["area_m2"] = FerramentasCivil.N(x.Area, 2), ["dentro_pct"] = FerramentasCivil.N(x.PctDentro, 2),
                ["acima_pct"] = FerramentasCivil.N(x.PctAcima, 2), ["abaixo_pct"] = FerramentasCivil.N(x.PctAbaixo, 2),
                ["media_m"] = N3(x.Media), ["desvio_padrao_m"] = N3(x.DesvioPadrao), ["emq_m"] = N3(x.Emq),
                ["maximo_m"] = N3(x.Maximo), ["minimo_m"] = N3(x.Minimo),
                ["volume_acima_m3"] = FerramentasCivil.N(x.VolumeAcima, 2), ["volume_abaixo_m3"] = FerramentasCivil.N(x.VolumeAbaixo, 2),
                ["situacao"] = x.Situacao(rel.Tolerancia.PercentualMinimo),
            });
        double areaTot = rel.Faixas.Sum(f => f.Area);
        var faixas = new JsonArray();
        foreach (var f in rel.Faixas)
            faixas.Add(new JsonObject { ["faixa"] = f.Rotulo, ["area_m2"] = FerramentasCivil.N(f.Area, 1), ["pct"] = FerramentasCivil.N(areaTot > 0 ? f.Area / areaTot * 100 : double.NaN, 2) });
        var criticos = new JsonArray();
        foreach (var q in rel.Criticos)
            criticos.Add(new JsonObject
            {
                ["n"] = q.Numero, ["x"] = N3(q.X), ["y"] = N3(q.Y),
                ["estaca"] = double.IsNaN(q.Estaca) ? null : NotaServico.FormatarEstaca(q.Estaca), ["afastamento_m"] = FerramentasCivil.N(q.Afastamento, 2),
                ["cota_projeto"] = N3(q.CotaProjeto), ["cota_obra"] = N3(q.CotaObra), ["desvio_m"] = N3(q.Desvio),
            });
        JsonObject? estacas = null;
        if (rel.Estacas is { } ce)
            estacas = new JsonObject
            {
                ["alinhamento"] = ce.Alinhamento, ["estacas"] = ce.Linhas.Count,
                ["estacas_com_ponto_fora"] = ce.Linhas.Count(l => ce.Resumo(l, rel.Tolerancia).Fora > 0),
                ["afastamentos"] = JsonMcp.Array(ce.Afastamentos, x => JsonValue.Create(x)),
            };
        var g = rel.Linhas[^1];
        var texto = $"Projeto {rel.Projeto} × obra {rel.Obra} (tolerância {rel.Tolerancia.Descricao}): {g.PctDentro.ToString("0.0", Num.Br)} % da área dentro, " +
                    $"{g.PctAcima.ToString("0.0", Num.Br)} % acima e {g.PctAbaixo.ToString("0.0", Num.Br)} % abaixo; desvio médio {g.Media.ToString("+0.000;-0.000;0.000", Num.Br)} m, " +
                    $"EMQ {g.Emq.ToString("0.000", Num.Br)} m — {g.Situacao(rel.Tolerancia.PercentualMinimo)}; {rel.Criticos.Count} ponto(s) crítico(s)" +
                    (estacas is null ? "" : $"; controle por estaca com {rel.Estacas!.Linhas.Count} estacas") +
                    (solidos > 0 ? $"; mapa com {solidos} retângulos" : "") + (arq is null ? "." : $"; planilha {arq}.") +
                    (rel.Avisos.Count > 0 ? " Avisos: " + string.Join(" ", rel.Avisos) : "");
        return ResultadoFerramenta.Ok(texto, new JsonObject
        {
            ["projeto"] = rel.Projeto, ["obra"] = rel.Obra,
            ["tolerancia"] = new JsonObject { ["acima_m"] = rel.Tolerancia.Acima, ["abaixo_m"] = rel.Tolerancia.Abaixo, ["percentual_minimo"] = rel.Tolerancia.PercentualMinimo },
            ["celula_m"] = rel.Celula, ["situacao"] = rel.Situacao, ["volumes_exatos"] = rel.VolumesExatos,
            ["areas"] = areas, ["faixas"] = faixas, ["criticos"] = criticos, ["controle_estacas"] = estacas,
            ["arquivo"] = arq, ["avisos"] = JsonMcp.Array(rel.Avisos, x => JsonValue.Create(x)),
        });
    }
}
