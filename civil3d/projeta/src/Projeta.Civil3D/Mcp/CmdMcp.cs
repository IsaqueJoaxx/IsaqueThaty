using System.Text;
using System.Text.Json.Nodes;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Infra;
using Projeta.Mcp;

[assembly: CommandClass(typeof(Projeta.Civil3D.Mcp.CmdMcp))]

namespace Projeta.Civil3D.Mcp;

public sealed class CmdMcp
{
    /// <summary>PRJMCP — liga/desliga a ponte com o Claude, mostra o status e configura o início automático.</summary>
    [CommandMethod("PRJMCP", CommandFlags.Modal)]
    public static void Ponte()
    {
        var cfg = ConfigMcp.Ler();
        MostrarStatus(cfg);
        var padrao = PonteCivil.Ativa ? "Status" : "Iniciar";
        var pko = new PromptKeywordOptions($"\nPROJETA MCP [Iniciar/Parar/Status/Automatico/Ferramentas] <{padrao}>: ",
            "Iniciar Parar Status Automatico Ferramentas") { AllowNone = true };
        var r = Ctx.Ed.GetKeywords(pko);
        if (r.Status != PromptStatus.OK && r.Status != PromptStatus.None) return;
        var op = r.Status == PromptStatus.None ? padrao : r.StringResult;
        try
        {
            switch (op)
            {
                case "Iniciar":
                    if (PonteCivil.Ativa) { Ctx.Msg("A ponte já está ativa."); break; }
                    PonteCivil.Iniciar();
                    Ctx.Msg($"Ponte MCP ativa (porta {PonteCivil.Info!.Porta}) — {PonteCivil.Registro.Quantidade} ferramentas disponíveis para o Claude.");
                    if (!InstalacaoClaude.Instalado(InstalacaoClaude.ConfigPadrao))
                        Ctx.Msg("[AVISO] O Claude Desktop ainda não conhece o PROJETA: rode instalador\\instalar-mcp.ps1 e reinicie o Claude Desktop.");
                    break;
                case "Parar":
                    PonteCivil.Parar();
                    Ctx.Msg("Ponte MCP desligada — o Claude não tem mais acesso a este Civil 3D.");
                    break;
                case "Automatico":
                    cfg.IniciarAutomatico = !cfg.IniciarAutomatico;
                    cfg.Salvar();
                    Ctx.Msg(cfg.IniciarAutomatico
                        ? "Início automático LIGADO: a ponte liga sozinha quando o Civil 3D carregar o PROJETA."
                        : "Início automático DESLIGADO: use PRJMCP → Iniciar quando quiser usar o Claude.");
                    if (cfg.IniciarAutomatico && !PonteCivil.Ativa) { PonteCivil.Iniciar(); Ctx.Msg("Ponte iniciada agora também."); }
                    break;
                case "Ferramentas":
                    foreach (var f in PonteCivil.Registro.Todas)
                        Ctx.Msg($"  {f.Nome,-30} {(f.AlteraDesenho ? "ALTERA O DWG" : f.SomenteLeitura ? "leitura" : "grava arquivo"),-13} {f.Titulo}");
                    break;
                default:
                    break; // Status já mostrado
            }
        }
        catch (System.Exception ex) { Ctx.Msg("[ERRO] PRJMCP: " + ex.Message); }
    }

    private static void MostrarStatus(ConfigMcp cfg)
    {
        if (PonteCivil.Ativa)
        {
            var i = PonteCivil.Info!;
            Ctx.Msg($"Ponte MCP ATIVA desde {i.Iniciado:HH:mm} — porta {i.Porta}, {PonteCivil.Registro.Quantidade} ferramentas, {i.Aplicativo}, PROJETA {i.Versao}.");
        }
        else Ctx.Msg("Ponte MCP desligada.");
        Ctx.Msg($"Início automático: {(cfg.IniciarAutomatico ? "ligado" : "desligado")}. Arquivos da ponte: {Locais.Pasta}");
        Ctx.Msg($"Claude Desktop configurado: {(InstalacaoClaude.Instalado(InstalacaoClaude.ConfigPadrao) ? "sim" : "não (rode instalador\\instalar-mcp.ps1)")}.");
    }

    /// <summary>
    /// PRJMCPTESTE — executa as ferramentas do Claude neste desenho, sem o Claude (autoteste).
    /// Leitura: só consulta. Completo: também calcula, força e libera um diâmetro, muda e restaura um parâmetro,
    /// gera relatório e cria um perfil de terreno — use numa cópia do desenho (ou desfaça depois).
    /// O resultado completo vai para %TEMP%\projeta-mcp-teste.json.
    /// </summary>
    [CommandMethod("PRJMCPTESTE", CommandFlags.Modal)]
    public static void Teste()
    {
        var modo = PedirModo();
        if (modo is null) return;
        var reg = PonteCivil.Registro;
        var log = new JsonArray();
        int ok = 0, falhas = 0;

        ResultadoFerramenta Chamar(string nome, JsonObject args, Func<ResultadoFerramenta, string?>? verificar = null, bool esperaErro = false)
        {
            var f = reg.Obter(nome) ?? throw new InvalidOperationException("ferramenta não registrada: " + nome);
            var r = ExecutorCivil.ExecutarAqui(f, new Argumentos((JsonObject)args.DeepClone()), emComando: true);
            string? problema = esperaErro
                ? (r.Erro ? null : "deveria ter dado erro")
                : (r.Erro ? r.Texto : verificar?.Invoke(r));
            if (problema is null) ok++; else falhas++;
            var primeira = r.Texto.Split('\n')[0];
            Ctx.Msg($"  {(problema is null ? "ok   " : "FALHA")} {nome} {JsonMcp.Texto(args)} — {(problema ?? (primeira.Length > 140 ? primeira[..137] + "..." : primeira))}");
            log.Add(new JsonObject
            {
                ["ferramenta"] = nome, ["argumentos"] = args.DeepClone(), ["ok"] = problema is null, ["problema"] = problema,
                ["erro"] = r.Erro, ["texto"] = r.Texto, ["dados"] = r.Dados?.DeepClone(),
            });
            return r;
        }
        static JsonArray? Arr(ResultadoFerramenta r, string k) => r.Dados?[k] as JsonArray;
        static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

        Ctx.Msg($"=== PRJMCPTESTE ({modo}) — {reg.Quantidade} ferramentas ===");
        Chamar("desenho_status", new JsonObject());
        var objs = Chamar("civil_listar_objetos", new JsonObject());
        var redes = Chamar("projeta_listar_redes", new JsonObject());
        Chamar("projeta_detalhar_rede", new JsonObject { ["rede"] = "rede que não existe" }, esperaErro: true);
        Chamar("civil_listar_objetos", new JsonObject { ["tipos"] = new JsonArray("superficie_errada") }, esperaErro: true);

        var sup = Str(Arr(objs, "superficies")?.FirstOrDefault()?["nome"]);
        var alObj = Arr(objs, "alinhamentos")?.FirstOrDefault();
        var al = Str(alObj?["nome"]);
        var primeiraRede = Arr(redes, "redes")?.FirstOrDefault(r => Str(r?["tipo"]) != "Agua");
        var nomeRede = Str(primeiraRede?["nome"]);

        if (sup is not null && al is not null)
        {
            double ini = alObj!["estacaInicial"]!.GetValue<double>(), fim = alObj["estacaFinal"]!.GetValue<double>();
            Chamar("civil_perfil_terreno", new JsonObject { ["alinhamento"] = al, ["superficie"] = sup, ["intervalo"] = 20 },
                r => (Arr(r, "amostras")?.Count ?? 0) >= 2 ? null : "esperava ao menos 2 amostras");
            Chamar("civil_cota_terreno", new JsonObject { ["superficie"] = sup, ["alinhamento"] = al, ["estacas"] = new JsonArray(ini, (ini + fim) / 2), ["afastamento"] = 0 },
                r => Arr(r, "pontos")?.Count == 2 ? null : "esperava 2 pontos");
            Chamar("civil_cota_terreno", new JsonObject { ["superficie"] = sup, ["alinhamento"] = al, ["estacas"] = new JsonArray(fim + 1000) }, esperaErro: true);
        }
        else Ctx.Msg("  (sem superfície e alinhamento: testes de terreno pulados)");

        if (nomeRede is not null)
        {
            foreach (var t in new[] { "parametros", "forcados", "nos", "bacias" })
                Chamar("projeta_detalhar_rede", new JsonObject { ["rede"] = nomeRede, ["tabela"] = t });
            if (primeiraRede?["calculada"]?.GetValue<bool>() == true)
                foreach (var t in new[] { "dimensionamento", "materiais", "vala", "mensagens" })
                    Chamar("projeta_detalhar_rede", new JsonObject { ["rede"] = nomeRede, ["tabela"] = t });
        }
        else Ctx.Msg("  (sem rede de drenagem/esgoto do PROJETA: testes de rede pulados)");

        if (modo == "Completo")
        {
            if (nomeRede is not null)
            {
                var calc = Chamar("projeta_calcular_rede", new JsonObject { ["rede"] = nomeRede },
                    r => r.Dados?["redes"]?[0]?["trechos"] is not null ? null : "resumo sem trechos");
                var dim = Chamar("projeta_detalhar_rede", new JsonObject { ["rede"] = nomeRede, ["tabela"] = "dimensionamento" });
                var par = Chamar("projeta_detalhar_rede", new JsonObject { ["rede"] = nomeRede, ["tabela"] = "parametros" });
                var t0 = Arr(dim, "trechos")?.FirstOrDefault();
                var listaD = par.Dados?["valores"]?["parametros"]?["Diametros"] as JsonArray;
                if (t0 is not null && listaD is not null)
                {
                    var nomeT = Str(t0["trecho"])!;
                    double d0 = t0["diametro_m"]!.GetValue<double>();
                    var maiores = listaD.Select(x => x!.GetValue<double>()).Where(x => x > d0 + 1e-9).ToList();
                    double d1 = maiores.Count > 0 ? maiores.Min() : d0;
                    Chamar("projeta_editar_elementos", new JsonObject { ["rede"] = nomeRede, ["trechos"] = new JsonArray(new JsonObject { ["trecho"] = nomeT, ["diametro_m"] = d1 }) },
                        r => Math.Abs((r.Dados?["trechos"]?[0]?["diametro_m"]?.GetValue<double>() ?? -1) - d1) < 1e-6 ? null : $"diâmetro de {nomeT} deveria ser {d1}");
                    Chamar("projeta_editar_elementos", new JsonObject { ["rede"] = nomeRede, ["trechos"] = new JsonArray(new JsonObject { ["trecho"] = nomeT, ["diametro_m"] = null }) },
                        r => r.Dados?["mudancas"]?[0]?["depois"] is null ? null : "o diâmetro forçado deveria ter sido removido");
                    Chamar("projeta_editar_elementos", new JsonObject { ["rede"] = nomeRede, ["trechos"] = new JsonArray(new JsonObject { ["trecho"] = nomeT, ["declividade_m_m"] = 5 }) }, esperaErro: true);
                }
                var grupo = par.Dados?["valores"]?["parametros"];
                var chave = grupo?["TempoRetorno"] is not null ? "TempoRetorno" : grupo?["PopulacaoFinal"] is not null ? "PopulacaoFinal" : null;
                if (chave is not null)
                {
                    double v0 = grupo![chave]!.GetValue<double>();
                    Chamar("projeta_alterar_parametros", new JsonObject { ["rede"] = nomeRede, ["valores"] = new JsonObject { [chave] = v0 + 5 } },
                        r => Math.Abs((r.Dados?["mudancas"]?[0]?["depois"]?.GetValue<double>() ?? -1) - (v0 + 5)) < 1e-9 ? null : $"{chave} deveria ser {v0 + 5}");
                    Chamar("projeta_alterar_parametros", new JsonObject { ["rede"] = nomeRede, ["valores"] = new JsonObject { [chave] = v0 } });
                }
                Chamar("projeta_alterar_parametros", new JsonObject { ["rede"] = nomeRede, ["valores"] = new JsonObject { ["ParametroInexistente"] = 1 } }, esperaErro: true);
                var pasta = Path.Combine(Path.GetTempPath(), "projeta-mcp-relatorio");
                Chamar("projeta_relatorio", new JsonObject { ["rede"] = nomeRede, ["formato"] = "html", ["pasta"] = pasta },
                    r => Arr(r, "arquivos")?.All(a => File.Exists(Str(a))) == true ? null : "arquivo não gerado");
                Chamar("projeta_relatorio", new JsonObject { ["rede"] = nomeRede, ["formato"] = "csv", ["pasta"] = pasta },
                    r => Arr(r, "arquivos")?.Count == 2 ? null : "esperava 2 CSVs");
            }
            if (sup is not null && al is not null)
                Chamar("civil_criar_perfil_terreno", new JsonObject { ["alinhamento"] = al, ["superficie"] = sup, ["nome"] = "PRJMCP teste " + DateTime.Now.ToString("HHmmss") });
            if ((Arr(objs, "corredores")?.Count ?? 0) > 0)
                Chamar("civil_reconstruir_corredores", new JsonObject());
        }

        var arq = Path.Combine(Path.GetTempPath(), "projeta-mcp-teste.json");
        try { File.WriteAllText(arq, new JsonObject { ["modo"] = modo, ["ok"] = ok, ["falhas"] = falhas, ["chamadas"] = log }.ToJsonString(JsonMcp.Indentado), new UTF8Encoding(false)); }
        catch (System.Exception ex) { Ctx.Msg("Não foi possível gravar " + arq + ": " + ex.Message); }
        Ctx.Msg($"=== PRJMCPTESTE: {ok} ok, {falhas} falha(s). Detalhes em {arq} ===");
    }

    private static string? PedirModo()
    {
        var pko = new PromptKeywordOptions("\nModo do teste [Leitura/Completo] <Leitura>: ", "Leitura Completo") { AllowNone = true };
        var r = Ctx.Ed.GetKeywords(pko);
        if (r.Status == PromptStatus.None) return "Leitura";
        return r.Status == PromptStatus.OK ? r.StringResult : null;
    }
}
