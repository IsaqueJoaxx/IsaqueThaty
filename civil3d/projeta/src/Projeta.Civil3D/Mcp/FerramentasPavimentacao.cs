using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Comandos;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Projeta.Mcp;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Mcp;

/// <summary>Ferramentas de pavimentação: nota de serviço a partir do corredor.</summary>
internal static class FerramentasPavimentacao
{
    public static void Registrar(RegistroFerramentas r)
    {
        r.Adicionar(new FerramentaDelegada("civil_nota_servico", "Nota de serviço do corredor",
            "Gera a nota de serviço de pavimentação (.xlsx, layout E-NS) de um corredor do Civil 3D: uma linha a cada 'intervalo' m " +
            "(padrão 10), nos pontos notáveis do eixo (PC/PT, TS/SC/CS/ST, PCC/PRC) e do greide (BVC/EVC/PIV) e no início/fim de cada região; " +
            "colunas do eixo (estaca 20 m, cota de projeto do greide da baseline, cota do terreno, cota vermelha) e, de cada lado, afastamento, cota e " +
            "inclinação dos elementos da seção, achados pelos códigos de ponto do corredor (padrão: Bordo Pista=ETW, Drenagem=Flowline_Gutter, " +
            "Topo Meio Fio=Back_Curb|Top_Curb, Passeio=Sidewalk_Out). Estacas que não são seções calculadas do corredor são interpoladas " +
            "(afastamento e altura sobre o greide) e listadas na aba Origem. Não altera o desenho. Use somente_codigos=true primeiro para ver " +
            "os códigos que o corredor tem e ajustar 'elementos' se o assembly usar códigos próprios.",
            () => new Esquema()
                .Texto("corredor", "Nome do corredor. Pode omitir se o desenho tiver só um.")
                .Texto("baseline", "Nome da baseline ou do alinhamento dela. Pode omitir se o corredor tiver só uma.")
                .Texto("superficie", "Superfície do terreno. Omitida: a mais provável (fora as superfícies do corredor). 'nenhuma': cota do terreno em branco.")
                .Texto("arquivo", "Caminho do .xlsx. Padrão: pasta 'relatorios' ao lado do DWG, <alinhamento>_nota_servico.xlsx.")
                .Numero("intervalo", "Distância entre as linhas regulares (m).", minimo: 0.1, padrao: 10)
                .Numero("estaca_inicial", "Estaca inicial em metros (padrão: início do corredor).")
                .Numero("estaca_final", "Estaca final em metros (padrão: fim do corredor).")
                .ListaObjetos("elementos", "Elementos da seção, do eixo para fora (iguais nos dois lados). Omitido: os da última configuração do PRJNOTASERVICO.",
                    new Esquema()
                        .Texto("nome", "Título do grupo de colunas (ex.: Bordo Pista).", obrigatorio: true)
                        .ListaTextos("codigos", "Códigos de ponto em ordem de preferência (ex.: ETW).", obrigatorio: true)
                        .Booleano("inclinacao", "Inclui a coluna Incl. (%).", false), maxItens: 12)
                .Booleano("terraplenagem", "Inclui banquetas e offset (crista do corte / pé do aterro) de cada lado. Padrão: a última configuração (normalmente sim).")
                .ListaTextos("codigos_banqueta", "Códigos dos pontos de banqueta (aceitam * e ?). Do eixo para fora, cada par = início e fim de uma banqueta. Padrão: *BERMA*, *Ban*, *Bench*.")
                .ListaTextos("codigos_offset", "Códigos do offset (aceitam * e ?); vale o mais externo. Sem eles, o ponto mais externo sobre o terreno. Padrão: *OFFSET*, Daylight*, Corte*33-*, Aterro*33-*.")
                .Booleano("somente_codigos", "true: só lista os códigos de ponto do corredor e como os elementos casam com eles, sem gerar a planilha.", false),
            Gerar, limite: TimeSpan.FromMinutes(5),
            esquemaSaida: () => new Esquema().Aberto()
                .Texto("arquivo", "Planilha gravada (ausente com somente_codigos).", anulavel: true)
                .ListaTextos("codigosEncontrados", "Códigos de ponto existentes no corredor.")
                .ListaTextos("avisos", "Avisos (corredor desatualizado, elementos sem ponto, estacas fora do greide/terreno...).")));
    }

    private static ResultadoFerramenta Gerar(Argumentos a)
    {
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var cors = NotaServicoCivil.Corredores(tr);
        if (cors.Count == 0) throw new ErroFerramenta("O desenho não tem corredores.");
        var nomeCor = a.Texto("corredor");
        (ObjectId Id, string Nome) it;
        if (string.IsNullOrWhiteSpace(nomeCor))
        {
            if (cors.Count > 1) throw new ErroFerramenta($"Há {cors.Count} corredores; informe 'corredor': {string.Join(", ", cors.Select(c => $"'{c.Nome}'"))}.");
            it = cors[0];
        }
        else
        {
            it = cors.FirstOrDefault(c => string.Equals(c.Nome, nomeCor.Trim(), StringComparison.OrdinalIgnoreCase));
            if (it.Id.IsNull) throw new ErroFerramenta($"Corredor '{nomeCor}' não encontrado. Existentes: {string.Join(", ", cors.Select(c => $"'{c.Nome}'"))}.");
        }
        var cor = (Corridor)tr.GetObject(it.Id, OpenMode.ForRead);

        var bls = NotaServicoCivil.Baselines(cor);
        if (bls.Count == 0) throw new ErroFerramenta($"O corredor {cor.Name} não tem baseline por alinhamento.");
        var nomeBl = a.Texto("baseline");
        Baseline bl;
        if (string.IsNullOrWhiteSpace(nomeBl))
        {
            if (bls.Count > 1) throw new ErroFerramenta($"O corredor {cor.Name} tem {bls.Count} baselines; informe 'baseline': " +
                                                        string.Join("; ", bls.Select(b => NotaServicoCivil.NomeBaseline(tr, b))) + ".");
            bl = bls[0];
        }
        else
        {
            bl = bls.FirstOrDefault(b => string.Equals(b.Name, nomeBl.Trim(), StringComparison.OrdinalIgnoreCase)
                                         || NotaServicoCivil.NomeBaseline(tr, b).Contains($"eixo {nomeBl.Trim()},", StringComparison.OrdinalIgnoreCase))
                 ?? throw new ErroFerramenta($"Baseline '{nomeBl}' não encontrada. Existentes: {string.Join("; ", bls.Select(b => NotaServicoCivil.NomeBaseline(tr, b)))}.");
        }

        var cfg = ConfigNotaArquivo.Carregar();
        if (a.Numero("intervalo") is { } iv) cfg.Intervalo = iv;
        var els = a.ListaObjetos("elementos");
        if (els.Count > 0)
            cfg.Elementos = els.Select(e => new ElementoSecao(e.TextoObrigatorio("nome"), e.Booleano("inclinacao", false), e.ListaTextos("codigos").ToArray())).ToList();
        if (a.Tem("terraplenagem")) cfg.Terraplenagem = a.Booleano("terraplenagem", true);
        if (a.ListaTextos("codigos_banqueta") is { Count: > 0 } cb) cfg.CodigosBanqueta = cb.ToList();
        if (a.ListaTextos("codigos_offset") is { Count: > 0 } co) cfg.CodigosOffset = co.ToList();
        try { cfg.Validar(); }
        catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }

        if (a.Booleano("somente_codigos", false))
        {
            var cods = NotaServicoCivil.CodigosDaBaseline(bl);
            var map = new JsonArray();
            var linhas = new List<string>();
            foreach (var e in cfg.Elementos)
            {
                var usa = e.Codigos.SelectMany(c => cods.Keys.Where(k => ConfigNotaServico.Casa(k, c))).FirstOrDefault();
                map.Add(new JsonObject { ["elemento"] = e.Nome, ["codigos"] = JsonMcp.Array(e.Codigos, c => JsonValue.Create(c)), ["usa"] = usa });
                linhas.Add($"{e.Nome} ← {(usa ?? "NÃO ENCONTRADO")}");
            }
            if (cfg.Terraplenagem)
            {
                var b = cods.Keys.Where(k => cfg.CodigosBanqueta.Any(p => ConfigNotaServico.Casa(k, p))).ToList();
                var o = cods.Keys.Where(k => cfg.CodigosOffset.Any(p => ConfigNotaServico.Casa(k, p))).ToList();
                map.Add(new JsonObject { ["elemento"] = "Banquetas", ["codigos"] = JsonMcp.Array(cfg.CodigosBanqueta, c => JsonValue.Create(c)), ["usa"] = string.Join(", ", b) });
                map.Add(new JsonObject { ["elemento"] = "Offset", ["codigos"] = JsonMcp.Array(cfg.CodigosOffset, c => JsonValue.Create(c)), ["usa"] = string.Join(", ", o) });
                linhas.Add($"Banquetas ← {(b.Count == 0 ? "nenhuma nas seções de amostra" : string.Join(", ", b))}");
                linhas.Add($"Offset ← {(o.Count == 0 ? (cfg.OffsetPeloTerreno ? "pelo terreno" : "NÃO ENCONTRADO") : string.Join(", ", o))}");
            }
            return ResultadoFerramenta.Ok(
                $"Corredor {cor.Name}, baseline {bl.Name}: códigos {string.Join(", ", cods.Keys)}. Elementos: {string.Join("; ", linhas)}.",
                new JsonObject
                {
                    ["corredor"] = cor.Name, ["baseline"] = NotaServicoCivil.NomeBaseline(tr, bl),
                    ["codigosEncontrados"] = JsonMcp.Array(cods.Keys, c => JsonValue.Create(c)),
                    ["elementos"] = map,
                    ["avisos"] = new JsonArray(),
                });
        }

        Surface? sup = null;
        var nomeSup = a.Texto("superficie")?.Trim();
        if (string.Equals(nomeSup, "nenhuma", StringComparison.OrdinalIgnoreCase)) sup = null;
        else if (!string.IsNullOrWhiteSpace(nomeSup)) sup = FerramentasCivil.AbrirSuperficie(tr, nomeSup);
        else
        {
            var l = CivilUtil.Superficies(tr);
            int k = NotaServicoCivil.SuperficiePadrao(l.Select(x => x.Nome).ToList(), NotaServicoCivil.SuperficiesDoCorredor(cor));
            if (k >= 0) sup = (Surface)tr.GetObject(l[k].Id, OpenMode.ForRead);
        }

        NotaServicoCivil.Leitura lida;
        try { lida = NotaServicoCivil.Montar(tr, cor, bl, sup, cfg, a.Numero("estaca_inicial"), a.Numero("estaca_final"), Ctx.Doc.Name); }
        catch (InvalidOperationException ex) { throw new ErroFerramenta(ex.Message); }
        catch (ArgumentException ex) { throw new ErroArgumento(ex.Message); }
        var ns = lida.Nota;

        var arq = a.Texto("arquivo")?.Trim();
        if (string.IsNullOrWhiteSpace(arq))
        {
            var pasta = Ctx.Doc.IsNamedDrawing && !string.IsNullOrEmpty(Ctx.Db.Filename)
                ? Path.Combine(Path.GetDirectoryName(Ctx.Db.Filename)!, "relatorios")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PROJETA");
            arq = Path.Combine(pasta, Texto.IdSeguro(ns.Titulo, 60) + "_nota_servico.xlsx");
        }
        if (!arq.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) arq += ".xlsx";
        try { ns.Salvar(arq); }
        catch (IOException ex) { throw new ErroFerramenta(ex.Message); }
        catch (UnauthorizedAccessException ex) { throw new ErroFerramenta($"Sem permissão para gravar {arq}: {ex.Message}"); }

        var (esq, dir) = ns.Colunas();
        var amostra = new JsonArray();
        foreach (var l in ns.Linhas.Take(8))
        {
            JsonObject Lado(PontoSecao?[] pts, List<int> idx)
            {
                var o = new JsonObject();
                foreach (var i in idx)
                    if (pts[i] is { } p)
                        o[cfg.Elementos[i].Nome] = new JsonObject
                        {
                            ["afastamento"] = FerramentasCivil.N(p.Afastamento), ["cota"] = FerramentasCivil.N(p.Cota),
                            ["inclinacao_pct"] = FerramentasCivil.N(NotaServico.Inclinacao(pts, i, l.CotaProjeto), 2),
                        };
                return o;
            }
            amostra.Add(new JsonObject
            {
                ["estaca"] = ns.Estaca(l.Estaca), ["notavelH"] = l.NotavelH, ["notavelV"] = l.NotavelV,
                ["cotaProjeto"] = FerramentasCivil.N(l.CotaProjeto), ["cotaTerreno"] = FerramentasCivil.N(l.CotaTerreno),
                ["cotaVermelha"] = FerramentasCivil.N(l.CotaVermelha), ["interpolada"] = l.Interpolada,
                ["esquerdo"] = Lado(l.Esquerdo, esq), ["direito"] = Lado(l.Direito, dir),
                ["offsetEsquerdo"] = Pt(l.OffsetEsquerdo, NotaServico.CorteAterro), ["offsetDireito"] = Pt(l.OffsetDireito, NotaServico.CorteAterro),
                ["banquetasEsquerdo"] = JsonMcp.Array(l.BanquetasEsquerdo, p => (JsonNode?)new JsonObject { ["afastamento"] = FerramentasCivil.N(p.Afastamento), ["cota"] = FerramentasCivil.N(p.Cota) }),
                ["banquetasDireito"] = JsonMcp.Array(l.BanquetasDireito, p => (JsonNode?)new JsonObject { ["afastamento"] = FerramentasCivil.N(p.Afastamento), ["cota"] = FerramentasCivil.N(p.Cota) }),
            });
            JsonObject? Pt(PontoSecao? p, Func<PontoSecao, IReadOnlyList<PontoSecao?>, double, string> ca) =>
                p is { } q ? new JsonObject
                {
                    ["afastamento"] = FerramentasCivil.N(q.Afastamento), ["cota"] = FerramentasCivil.N(q.Cota),
                    ["corteAterro"] = ca(q, q.Afastamento < 0 ? l.Esquerdo : l.Direito, l.CotaProjeto),
                } : null;
        }

        var dados = new JsonObject
        {
            ["arquivo"] = arq,
            ["corredor"] = cor.Name,
            ["baseline"] = NotaServicoCivil.NomeBaseline(tr, bl),
            ["terreno"] = sup?.Name,
            ["linhas"] = ns.Linhas.Count,
            ["estacaInicial"] = ns.Estaca(ns.EstacaInicial),
            ["estacaFinal"] = ns.Estaca(ns.EstacaFinal),
            ["secoesLidas"] = lida.SecoesLidas,
            ["interpoladas"] = lida.Interpoladas,
            ["linhasComOffset"] = lida.LinhasComOffset,
            ["linhasComBanquetas"] = lida.LinhasComBanquetas,
            ["colunasEsquerdo"] = JsonMcp.Array(esq, i => JsonValue.Create(cfg.Elementos[i].Nome)),
            ["colunasDireito"] = JsonMcp.Array(dir, i => JsonValue.Create(cfg.Elementos[i].Nome)),
            ["codigosEncontrados"] = JsonMcp.Array(lida.Codigos.Keys, c => JsonValue.Create(c)),
            ["elementosSemPonto"] = JsonMcp.Array(lida.ElementosSemPonto, c => JsonValue.Create(c)),
            ["avisos"] = JsonMcp.Array(ns.Avisos, c => JsonValue.Create(c)),
            ["amostra"] = amostra,
        };
        var texto = $"Nota de serviço gravada em {arq}: {ns.Linhas.Count} linhas de {ns.Estaca(ns.EstacaInicial)} a {ns.Estaca(ns.EstacaFinal)} " +
                    $"({lida.SecoesLidas} seções do corredor, {lida.Interpoladas} interpolada(s)); terreno {(sup?.Name ?? "nenhum")}." +
                    (ns.Avisos.Count > 0 ? " Avisos: " + string.Join(" ", ns.Avisos) : "");
        return ResultadoFerramenta.Ok(texto, dados);
    }
}
