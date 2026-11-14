using System.Text.Json.Nodes;

namespace Projeta.Mcp;

/// <summary>
/// Biblioteca de prompts do MCP (prompts/list e prompts/get). São roteiros prontos que o cliente
/// mostra ao usuário — no Claude Desktop aparecem como comandos; em clientes com biblioteca de
/// prompts (o Autodesk Assistant tem uma aba dessas) entram na lista junto com os do próprio produto.
/// O prompt não executa nada: devolve a mensagem que o modelo recebe, e aí ele chama as ferramentas.
/// </summary>
public static class Prompts
{
    private sealed record Arg(string Nome, string Descricao, bool Obrigatorio);

    private sealed record Modelo(string Nome, string Titulo, string Descricao, Arg[] Args, Func<Func<string, string?>, string> Texto);

    private static readonly Modelo[] Lista =
    {
        new("inventario_desenho", "Inventário do desenho",
            "Tabela com tudo que existe no DWG aberto: superfícies, alinhamentos, corredores, redes e grupos de pontos.",
            Array.Empty<Arg>(),
            _ => "Use desenho_status e depois civil_listar_objetos no desenho aberto do Civil 3D. " +
                 "Monte uma tabela com: tipo de objeto, nome, quantidade e as informações principais de cada um " +
                 "(superfícies: cota mínima, máxima e número de pontos; alinhamentos: comprimento e estacas; " +
                 "corredores: linhas-base e superfícies; redes: número de tubos e estruturas). " +
                 "Feche com uma linha dizendo o que chama atenção no desenho."),

        new("perfil_do_terreno", "Perfil do terreno ao longo de um eixo",
            "Amostra a superfície ao longo de um alinhamento e resume cotas, desnível e rampas, sem criar nada no desenho.",
            new[]
            {
                new Arg("alinhamento", "Nome do alinhamento, exatamente como está no desenho.", true),
                new Arg("intervalo", "Intervalo entre amostras em metros (padrão 20).", false),
                new Arg("superficie", "Nome da superfície (opcional se houver só uma).", false),
            },
            g => $"Rode civil_perfil_terreno no alinhamento '{g("alinhamento")}'" +
                 (g("superficie") is { Length: > 0 } s ? $", superfície '{s}'" : "") +
                 $", intervalo de {g("intervalo") ?? "20"} m. " +
                 "Apresente o resumo (cota mínima e máxima, desnível, rampa média e rampas máximas de subida e descida) " +
                 "e aponte os trechos onde a rampa do terreno passa de 10%. Não crie objetos no desenho."),

        new("conferir_rampas", "Conferir as rampas de um greide",
            "Lê os perfis de projeto de um alinhamento e compara as rampas com o limite de norma que você indicar.",
            new[]
            {
                new Arg("alinhamento", "Nome do alinhamento.", true),
                new Arg("rampa_maxima", "Rampa máxima admitida em % (ex.: 10).", false),
            },
            g => $"No desenho aberto, liste os perfis do alinhamento '{g("alinhamento")}' com civil_listar_objetos " +
                 $"e levante as rampas de cada trecho do greide de projeto. Compare com o limite de {g("rampa_maxima") ?? "10"}% " +
                 "e monte uma tabela com: trecho (estaca inicial e final), rampa em %, extensão e se atende ou não. " +
                 "Liste primeiro os trechos que não atendem."),

        new("cotas_em_estacas", "Cotas do terreno em estacas",
            "Devolve a cota do terreno em uma lista de estacas de um alinhamento.",
            new[]
            {
                new Arg("alinhamento", "Nome do alinhamento.", true),
                new Arg("estacas", "Estacas em metros, separadas por vírgula (ex.: 0, 20, 40).", true),
                new Arg("afastamento", "Afastamento do eixo em metros, positivo à direita (padrão 0).", false),
            },
            g => $"Use civil_cota_terreno no alinhamento '{g("alinhamento")}' nas estacas {g("estacas")}" +
                 $" com afastamento de {g("afastamento") ?? "0"} m. " +
                 "Apresente uma tabela com estaca, coordenadas E e N e cota, e avise se alguma estaca cair fora da superfície."),

        new("relatorio_de_rede", "Relatório de uma rede do PROJETA",
            "Resumo de uma rede de drenagem, esgoto ou água: nós, trechos, declividades e pontos de atenção.",
            new[] { new Arg("rede", "Nome da rede, como aparece em projeta_listar_redes.", false) },
            g => (g("rede") is { Length: > 0 } r
                     ? $"Gere o relatório da rede '{r}' com projeta_relatorio."
                     : "Liste as redes com projeta_listar_redes e gere o relatório da rede mais recente com projeta_relatorio.") +
                 " Resuma: quantidade de nós e trechos, extensão total, declividades mínima e máxima, " +
                 "e aponte trechos fora de faixa (declividade baixa demais, recobrimento insuficiente ou velocidade fora do intervalo)."),

        new("diagnostico_conexao", "Diagnóstico da conexão com o Civil 3D",
            "Use quando uma ferramenta responder que o Civil 3D não respondeu.",
            Array.Empty<Arg>(),
            _ => "Rode projeta_conexao e depois desenho_status. " +
                 "Se não conectar, explique em passos simples o que o usuário deve fazer: abrir o Civil 3D com o plugin PROJETA carregado, " +
                 "rodar o comando PRJMCP, e conferir se há comando em andamento ou caixa de diálogo aberta (Esc). " +
                 "Mostre o que cada verificação retornou."),
    };

    public static JsonArray Catalogo()
    {
        var a = new JsonArray();
        foreach (var m in Lista)
        {
            var args = new JsonArray();
            foreach (var p in m.Args)
                args.Add(new JsonObject { ["name"] = p.Nome, ["description"] = p.Descricao, ["required"] = p.Obrigatorio });
            a.Add(new JsonObject
            {
                ["name"] = m.Nome,
                ["title"] = m.Titulo,
                ["description"] = m.Descricao,
                ["arguments"] = args,
            });
        }
        return a;
    }

    /// <summary>Monta a mensagem do prompt pedido. Lança quando o nome não existe ou falta argumento obrigatório.</summary>
    public static JsonObject Obter(string? nome, JsonObject? argumentos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("params.name ausente.");
        var m = Lista.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.Ordinal))
                ?? throw new ArgumentException($"Prompt '{nome}' não existe. Disponíveis: {string.Join(", ", Lista.Select(x => x.Nome))}.");

        string? Valor(string chave)
        {
            var v = argumentos?[chave];
            if (v is null) return null;
            var s = v is JsonValue jv && jv.TryGetValue<string>(out var txt) ? txt : v.ToJsonString().Trim('"');
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        foreach (var p in m.Args)
            if (p.Obrigatorio && Valor(p.Nome) is null)
                throw new ArgumentException($"O prompt '{m.Nome}' precisa do argumento '{p.Nome}' ({p.Descricao}).");

        return new JsonObject
        {
            ["description"] = m.Descricao,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonObject { ["type"] = "text", ["text"] = m.Texto(Valor) },
                },
            },
        };
    }
}
