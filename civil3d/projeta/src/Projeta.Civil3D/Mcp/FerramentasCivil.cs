using System.Diagnostics;
using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Mcp;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Mcp;

/// <summary>Ferramentas de consulta/operação de objetos nativos do Civil 3D (superfícies, alinhamentos, perfis, corredores).</summary>
internal static class FerramentasCivil
{
    public static void Registrar(RegistroFerramentas r)
    {
        r.Adicionar(new FerramentaDelegada("desenho_status", "Status do desenho",
            "Resumo do desenho ativo no Civil 3D: nome e caminho do DWG, se há alterações não salvas, unidades, sistema de coordenadas " +
            "e quantos objetos existem (superfícies, alinhamentos, corredores, redes de tubulação, pontos COGO e redes do PROJETA). " +
            "Use primeiro, para saber em que desenho está trabalhando.",
            () => new Esquema(), Status,
            esquemaSaida: () => new Esquema().Aberto()
                .Texto("desenho", "Nome do arquivo DWG aberto.")
                .Texto("caminho", "Caminho completo do DWG, ou null se nunca foi salvo.", anulavel: true)
                .Booleano("alteracoesNaoSalvas", "true quando há alterações não gravadas.")
                .Texto("unidades", "Unidades de inserção do desenho.")
                .Texto("sistemaCoordenadas", "Código do sistema de coordenadas do desenho, ou null se não houver.", anulavel: true)
                .ObjetoLivre("civil3d", "Contagem por tipo de objeto do Civil 3D.")
                .ListaTextos("redesProjeta", "Redes do PROJETA existentes no desenho.")));

        r.Adicionar(new FerramentaDelegada("civil_listar_objetos", "Listar objetos do Civil 3D",
            "Lista objetos nativos do Civil 3D com os dados principais: superfícies (tipo, cotas mín/máx/média, nº de pontos, área), " +
            "alinhamentos (comprimento, estacas inicial/final, perfis e grupos de seções), corredores (baselines e superfícies), " +
            "redes de tubulação e grupos de pontos. Os nomes retornados são os que as outras ferramentas aceitam.",
            () => new Esquema().ListaTextos("tipos", "Filtrar por tipo: superficies, alinhamentos, corredores, redes_tubulacao, grupos_pontos. Vazio = todos."),
            ListarObjetos,
            esquemaSaida: () => new Esquema().Aberto()
                .ListaObjetos("superficies", "Superfícies: nome, tipo, cotaMin, cotaMax, cotaMedia, pontos, triangulos, area2d_m2.", new Esquema().Aberto())
                .ListaObjetos("alinhamentos", "Alinhamentos: nome, tipo, comprimento_m, estacaInicial/Final (m e formatada), perfis, gruposSecoes.", new Esquema().Aberto())
                .ListaObjetos("corredores", "Corredores: nome, baselines (alinhamento e perfil), superficiesDoCorredor.", new Esquema().Aberto())
                .ListaObjetos("redesTubulacao", "Redes de tubulação: nome, tubos, estruturas.", new Esquema().Aberto())
                .ListaObjetos("gruposPontos", "Grupos de pontos: nome, pontos.", new Esquema().Aberto())));

        r.Adicionar(new FerramentaDelegada("civil_cota_terreno", "Cota do terreno",
            "Cota de uma superfície em pontos dados por coordenadas (x = E, y = N, em m) OU por estacas de um alinhamento (com afastamento opcional, " +
            "positivo à direita). Pontos fora da superfície voltam com cota null. Até 5000 pontos por chamada.",
            () => new Esquema()
                .Texto("superficie", "Nome da superfície. Pode omitir se o desenho tiver só uma.")
                .ListaObjetos("pontos", "Pontos por coordenada.", new Esquema().Numero("x", "E (m)", obrigatorio: true).Numero("y", "N (m)", obrigatorio: true), maxItens: 5000)
                .Texto("alinhamento", "Alinhamento para localizar as estacas.")
                .ListaNumeros("estacas", "Estacas em metros desde o início do alinhamento (ex.: 0, 20, 137.5).", maxItens: 5000)
                .Numero("afastamento", "Afastamento em relação ao eixo, m (positivo = direita).", padrao: 0),
            CotaTerreno,
            esquemaSaida: () => new Esquema().Aberto()
                .Texto("superficie", "Superfície consultada.")
                .ListaObjetos("pontos", "Um item por ponto: x, y, cota (null fora da superfície) e, quando veio de estaca, estaca, estaca_fmt e afastamento.",
                    new Esquema().Aberto()
                        .Numero("x", "Coordenada E (m).").Numero("y", "Coordenada N (m).")
                        .Numero("cota", "Cota na superfície (m); null se o ponto cair fora.", anulavel: true))));

        r.Adicionar(new FerramentaDelegada("civil_perfil_terreno", "Perfil do terreno ao longo do alinhamento",
            "Amostra a superfície ao longo de um alinhamento em intervalo regular (padrão 20 m, mais o início e o fim) e devolve estaca, coordenadas e cota, " +
            "com resumo de cota mínima/máxima, desnível e rampas. Útil para estudar greide sem criar objetos no desenho. Até 5000 amostras.",
            () => new Esquema()
                .Texto("alinhamento", "Nome do alinhamento.", obrigatorio: true)
                .Texto("superficie", "Nome da superfície. Pode omitir se o desenho tiver só uma.")
                .Numero("intervalo", "Intervalo entre amostras (m).", minimo: 0.1, padrao: 20)
                .Numero("estaca_inicial", "Estaca inicial em m (padrão: início do alinhamento).")
                .Numero("estaca_final", "Estaca final em m (padrão: fim do alinhamento).")
                .Numero("afastamento", "Afastamento em relação ao eixo, m (positivo = direita).", padrao: 0),
            PerfilTerreno,
            esquemaSaida: () => new Esquema().Aberto()
                .Texto("alinhamento", "Alinhamento amostrado.")
                .Texto("superficie", "Superfície amostrada.")
                .Numero("afastamento", "Afastamento usado (m).")
                .ObjetoLivre("resumo", "amostras, foraDaSuperficie, cotaMin, cotaMax, desnivelTotal, rampaMedia_pct, rampaMaxSubida_pct, rampaMaxDescida_pct.")
                .ListaObjetos("amostras", "Uma por estaca: estaca, estaca_fmt, x, y, cota.", new Esquema().Aberto())));

        r.Adicionar(new FerramentaDelegada("civil_criar_perfil_terreno", "Criar perfil do terreno",
            "Cria no alinhamento um perfil de terreno (Profile.CreateFromSurface) a partir de uma superfície, com o primeiro estilo de perfil " +
            "e o primeiro conjunto de rótulos do desenho. Altera o DWG (desfaz com U). Não cria profile view.",
            () => new Esquema()
                .Texto("alinhamento", "Nome do alinhamento.", obrigatorio: true)
                .Texto("superficie", "Nome da superfície. Pode omitir se o desenho tiver só uma.")
                .Texto("nome", "Nome do perfil (padrão: '<superfície> - terreno')."),
            CriarPerfilTerreno, somenteLeitura: false));

        r.Adicionar(new FerramentaDelegada("civil_reconstruir_corredores", "Reconstruir corredores",
            "Reconstrói (Rebuild) corredores do desenho — todos ou os informados — e informa o tempo de cada um. Altera o DWG (desfaz com U).",
            () => new Esquema().ListaTextos("corredores", "Nomes dos corredores. Vazio = todos."),
            ReconstruirCorredores, somenteLeitura: false, limite: TimeSpan.FromMinutes(15)));
    }

    // ------------------------------------------------------------------ auxiliares

    internal static JsonNode? N(double v, int casas = 3) => JsonMcp.Numero(v, casas);

    private static void Tentar(Action a)
    {
        try { a(); } catch { }
    }

    internal static Surface AbrirSuperficie(Transaction tr, string? nome)
    {
        var lista = CivilUtil.Superficies(tr);
        if (lista.Count == 0) throw new ErroFerramenta("O desenho não tem superfícies.");
        if (string.IsNullOrWhiteSpace(nome))
        {
            if (lista.Count == 1) return (Surface)tr.GetObject(lista[0].Id, OpenMode.ForRead);
            throw new ErroFerramenta($"Há {lista.Count} superfícies; informe 'superficie': {string.Join(", ", lista.Select(x => $"'{x.Nome}'"))}.");
        }
        var it = lista.FirstOrDefault(x => string.Equals(x.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase));
        if (it.Id.IsNull) throw new ErroFerramenta($"Superfície '{nome}' não encontrada. Existentes: {string.Join(", ", lista.Select(x => $"'{x.Nome}'"))}.");
        return (Surface)tr.GetObject(it.Id, OpenMode.ForRead);
    }

    internal static Alignment AbrirAlinhamento(Transaction tr, string nome)
    {
        var lista = CivilUtil.Alinhamentos(tr);
        if (lista.Count == 0) throw new ErroFerramenta("O desenho não tem alinhamentos.");
        var it = lista.FirstOrDefault(x => string.Equals(x.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase));
        if (it.Id.IsNull)
            throw new ErroFerramenta($"Alinhamento '{nome}' não encontrado. Existentes: {string.Join(", ", lista.Take(40).Select(x => $"'{x.Nome}'"))}{(lista.Count > 40 ? "..." : "")}.");
        return (Alignment)tr.GetObject(it.Id, OpenMode.ForRead);
    }

    private static string NomeDe(Transaction tr, ObjectId id)
    {
        if (id.IsNull) return "";
        try
        {
            return tr.GetObject(id, OpenMode.ForRead) switch
            {
                Alignment a => a.Name,
                Profile p => p.Name,
                Surface s => s.Name,
                _ => "",
            };
        }
        catch { return ""; }
    }

    // ------------------------------------------------------------------ ferramentas

    private static ResultadoFerramenta Status(Argumentos a)
    {
        var doc = Ctx.Doc;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        bool modificado = false;
        Tentar(() => modificado = Convert.ToInt32(AcCore.GetSystemVariable("DBMOD")) != 0);
        var o = new JsonObject
        {
            ["desenho"] = Path.GetFileName(doc.Name),
            ["caminho"] = doc.IsNamedDrawing ? db.Filename : null,
            ["salvoEmDisco"] = doc.IsNamedDrawing,
            ["alteracoesNaoSalvas"] = modificado,
            ["unidades"] = db.Insunits.ToString(),
            ["aplicativo"] = PonteCivil.NomeAplicativo(),
            ["pluginProjeta"] = PonteCivil.VersaoPlugin(),
        };
        Tentar(() => o["sistemaCoordenadas"] = civ.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode);
        var cont = new JsonObject();
        Tentar(() => cont["superficies"] = civ.GetSurfaceIds().Count);
        Tentar(() => cont["alinhamentos"] = civ.GetAlignmentIds().Count);
        Tentar(() => cont["corredores"] = civ.CorridorCollection.Count);
        Tentar(() => cont["redesTubulacao"] = civ.GetPipeNetworkIds().Count);
        Tentar(() => cont["pontosCogo"] = civ.CogoPoints.Count);
        o["civil3d"] = cont;
        using (var s = Sessao.Abrir())
        {
            o["redesProjeta"] = JsonMcp.Array(s.Projeto.Redes, r => JsonValue.Create($"{r.Nome} ({r.Tipo}, {r.Nos.Count} nós, {r.Trechos.Count} trechos)"));
            o["dispositivosLongitudinais"] = s.Projeto.Longitudinais.Count;
        }
        var texto = $"Desenho {o["desenho"]}{(modificado ? " (com alterações não salvas)" : "")}, unidades {db.Insunits}. " +
                    $"Civil 3D: {string.Join(", ", cont.Select(kv => $"{kv.Value} {kv.Key}"))}. Redes do PROJETA: {((JsonArray)o["redesProjeta"]!).Count}.";
        return ResultadoFerramenta.Ok(texto, o);
    }

    private static ResultadoFerramenta ListarObjetos(Argumentos a)
    {
        var validos = new[] { "superficies", "alinhamentos", "corredores", "redes_tubulacao", "grupos_pontos" };
        var tipos = a.ListaTextos("tipos").Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
        var invalidos = tipos.Where(t => !validos.Contains(t)).ToList();
        if (invalidos.Count > 0) throw new ErroArgumento($"tipo(s) desconhecido(s): {string.Join(", ", invalidos)}. Use: {string.Join(", ", validos)}.");
        bool Quer(string t) => tipos.Count == 0 || tipos.Contains(t);

        var civ = CivilApplication.ActiveDocument;
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var o = new JsonObject();
        var resumo = new List<string>();

        if (Quer("superficies"))
        {
            var arr = new JsonArray();
            foreach (ObjectId id in civ.GetSurfaceIds())
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Surface s) continue;
                var it = new JsonObject { ["nome"] = s.Name, ["tipo"] = s.GetType().Name, ["descricao"] = s.Description };
                Tentar(() =>
                {
                    var gp = s.GetGeneralProperties();
                    it["cotaMin"] = N(gp.MinimumElevation);
                    it["cotaMax"] = N(gp.MaximumElevation);
                    it["cotaMedia"] = N(gp.MeanElevation);
                    it["pontos"] = gp.NumberOfPoints;
                });
                if (s is TinSurface tin)
                    Tentar(() =>
                    {
                        it["triangulos"] = tin.GetTinProperties().NumberOfTriangles;
                        it["area2d_m2"] = N(tin.GetTerrainProperties().SurfaceArea2D, 1);
                    });
                arr.Add(it);
            }
            o["superficies"] = arr;
            resumo.Add($"{arr.Count} superfície(s)");
        }

        if (Quer("alinhamentos"))
        {
            var arr = new JsonArray();
            foreach (ObjectId id in civ.GetAlignmentIds())
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Alignment al) continue;
                var perfis = new JsonArray();
                foreach (ObjectId pid in al.GetProfileIds())
                    if (tr.GetObject(pid, OpenMode.ForRead) is Profile p)
                        perfis.Add(new JsonObject
                        {
                            ["nome"] = p.Name, ["tipo"] = p.ProfileType.ToString(),
                            ["estacaInicial"] = N(p.StartingStation), ["estacaFinal"] = N(p.EndingStation),
                        });
                var grupos = new JsonArray();
                foreach (ObjectId gid in al.GetSampleLineGroupIds())
                    if (tr.GetObject(gid, OpenMode.ForRead) is SampleLineGroup g)
                        grupos.Add(new JsonObject { ["nome"] = g.Name, ["secoes"] = g.GetSampleLineIds().Count });
                arr.Add(new JsonObject
                {
                    ["nome"] = al.Name,
                    ["tipo"] = al.AlignmentType.ToString(),
                    ["comprimento_m"] = N(al.Length, 2),
                    ["estacaInicial"] = N(al.StartingStation),
                    ["estacaFinal"] = N(al.EndingStation),
                    ["estacaInicial_fmt"] = Num.Estaca(al.StartingStation),
                    ["estacaFinal_fmt"] = Num.Estaca(al.EndingStation),
                    ["perfis"] = perfis,
                    ["gruposSecoes"] = grupos,
                });
            }
            o["alinhamentos"] = arr;
            resumo.Add($"{arr.Count} alinhamento(s)");
        }

        if (Quer("corredores"))
        {
            var arr = new JsonArray();
            foreach (ObjectId id in civ.CorridorCollection)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Corridor c) continue;
                var bls = new JsonArray();
                foreach (Baseline b in c.Baselines)
                {
                    var bl = new JsonObject();
                    Tentar(() => bl["nome"] = b.Name);
                    try { bl["alinhamento"] = NomeDe(tr, b.AlignmentId); bl["perfil"] = NomeDe(tr, b.ProfileId); }
                    catch { bl["tipo"] = "feature line"; } // baseline por feature line não tem alinhamento/perfil
                    bls.Add(bl);
                }
                arr.Add(new JsonObject { ["nome"] = c.Name, ["baselines"] = bls, ["superficiesDoCorredor"] = c.CorridorSurfaces.Count });
            }
            o["corredores"] = arr;
            resumo.Add($"{arr.Count} corredor(es)");
        }

        if (Quer("redes_tubulacao"))
        {
            var arr = new JsonArray();
            foreach (ObjectId id in civ.GetPipeNetworkIds())
                if (tr.GetObject(id, OpenMode.ForRead) is Network n)
                    arr.Add(new JsonObject { ["nome"] = n.Name, ["tubos"] = n.GetPipeIds().Count, ["estruturas"] = n.GetStructureIds().Count });
            o["redesTubulacao"] = arr;
            resumo.Add($"{arr.Count} rede(s) de tubulação");
        }

        if (Quer("grupos_pontos"))
        {
            var arr = new JsonArray();
            foreach (ObjectId id in civ.PointGroups)
                if (tr.GetObject(id, OpenMode.ForRead) is PointGroup g)
                    arr.Add(new JsonObject { ["nome"] = g.Name, ["pontos"] = g.PointsCount });
            o["gruposPontos"] = arr;
            resumo.Add($"{arr.Count} grupo(s) de pontos");
        }

        return ResultadoFerramenta.Ok(string.Join(", ", resumo) + ".", o);
    }

    private static ResultadoFerramenta CotaTerreno(Argumentos a)
    {
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var surf = AbrirSuperficie(tr, a.Texto("superficie"));
        var pontos = new List<(double X, double Y, double? Estaca, double? Afast)>();
        foreach (var p in a.ListaObjetos("pontos")) pontos.Add((p.NumeroObrigatorio("x"), p.NumeroObrigatorio("y"), null, null));

        var estacas = a.ListaNumeros("estacas");
        if (estacas.Count > 0)
        {
            var nomeAl = a.Texto("alinhamento");
            if (string.IsNullOrWhiteSpace(nomeAl)) throw new ErroArgumento("'alinhamento' é obrigatório quando há 'estacas'.");
            var al = AbrirAlinhamento(tr, nomeAl);
            double off = a.Numero("afastamento", 0);
            foreach (var e in estacas)
            {
                if (e < al.StartingStation - 1e-6 || e > al.EndingStation + 1e-6 || !CivilUtil.PontoNaEstaca(al, e, off, out var x, out var y))
                    throw new ErroFerramenta($"Estaca {Num.F(e)} fora do alinhamento {al.Name} ({Num.F(al.StartingStation)} a {Num.F(al.EndingStation)} m).");
                pontos.Add((x, y, e, off));
            }
        }
        if (pontos.Count == 0) throw new ErroArgumento("informe 'pontos' ou 'alinhamento' + 'estacas'.");
        if (pontos.Count > 5000) throw new ErroArgumento("máximo de 5000 pontos por chamada.");

        var arr = new JsonArray();
        int fora = 0;
        foreach (var p in pontos)
        {
            var z = CivilUtil.Cota(surf, p.X, p.Y);
            if (double.IsNaN(z)) fora++;
            var it = new JsonObject { ["x"] = N(p.X), ["y"] = N(p.Y), ["cota"] = N(z) };
            if (p.Estaca.HasValue)
            {
                it["estaca"] = N(p.Estaca.Value);
                it["estaca_fmt"] = Num.Estaca(p.Estaca.Value);
                it["afastamento"] = N(p.Afast ?? 0);
            }
            arr.Add(it);
        }
        var texto = pontos.Count == 1 && fora == 0
            ? $"Cota na superfície {surf.Name}: {Num.F(CivilUtil.Cota(surf, pontos[0].X, pontos[0].Y), 3)} m."
            : $"{pontos.Count} ponto(s) na superfície {surf.Name}{(fora > 0 ? $"; {fora} fora da superfície (cota null)" : "")}.";
        return ResultadoFerramenta.Ok(texto, new JsonObject { ["superficie"] = surf.Name, ["pontos"] = arr });
    }

    private static ResultadoFerramenta PerfilTerreno(Argumentos a)
    {
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var al = AbrirAlinhamento(tr, a.TextoObrigatorio("alinhamento"));
        var surf = AbrirSuperficie(tr, a.Texto("superficie"));
        double passo = a.Numero("intervalo", 20);
        if (passo <= 0) throw new ErroArgumento("'intervalo' deve ser positivo.");
        double ini = Math.Max(a.Numero("estaca_inicial") ?? al.StartingStation, al.StartingStation);
        double fim = Math.Min(a.Numero("estaca_final") ?? al.EndingStation, al.EndingStation);
        if (fim <= ini) throw new ErroArgumento($"trecho vazio: estacas {Num.F(ini)} a {Num.F(fim)} (alinhamento vai de {Num.F(al.StartingStation)} a {Num.F(al.EndingStation)}).");
        double off = a.Numero("afastamento", 0);

        var estacas = new List<double> { ini };
        for (double e = (Math.Floor(ini / passo + 1e-9) + 1) * passo; e < fim - 1e-6; e += passo) estacas.Add(e);
        estacas.Add(fim);
        if (estacas.Count > 5000) throw new ErroArgumento($"seriam {estacas.Count} amostras; aumente o 'intervalo' (máximo 5000).");

        var arr = new JsonArray();
        var validos = new List<(double E, double Z)>();
        foreach (var e in estacas)
        {
            double z = double.NaN, x = double.NaN, y = double.NaN;
            if (CivilUtil.PontoNaEstaca(al, e, off, out x, out y)) z = CivilUtil.Cota(surf, x, y);
            if (!double.IsNaN(z)) validos.Add((e, z));
            arr.Add(new JsonObject { ["estaca"] = N(e), ["estaca_fmt"] = Num.Estaca(e), ["x"] = N(x), ["y"] = N(y), ["cota"] = N(z) });
        }

        var resumo = new JsonObject { ["amostras"] = estacas.Count, ["foraDaSuperficie"] = estacas.Count - validos.Count };
        string texto = $"{estacas.Count} amostras de {Num.Estaca(ini)} a {Num.Estaca(fim)} a cada {Num.F(passo, 1)} m no alinhamento {al.Name}, superfície {surf.Name}";
        if (validos.Count >= 2)
        {
            var rampas = validos.Zip(validos.Skip(1), (p, q) => (q.Z - p.Z) / (q.E - p.E)).ToList();
            double zmin = validos.Min(v => v.Z), zmax = validos.Max(v => v.Z);
            double media = (validos[^1].Z - validos[0].Z) / (validos[^1].E - validos[0].E);
            resumo["cotaMin"] = N(zmin); resumo["cotaMax"] = N(zmax);
            resumo["desnivelTotal"] = N(validos[^1].Z - validos[0].Z);
            resumo["rampaMedia_pct"] = N(media * 100, 2);
            resumo["rampaMaxSubida_pct"] = N(rampas.Max() * 100, 2);
            resumo["rampaMaxDescida_pct"] = N(rampas.Min() * 100, 2);
            texto += $": cotas {Num.F(zmin)} a {Num.F(zmax)} m, rampa média {Num.F(media * 100)}%, máx. {Num.F(rampas.Max() * 100)}% / {Num.F(rampas.Min() * 100)}%";
        }
        if (validos.Count < estacas.Count) texto += $"; {estacas.Count - validos.Count} amostra(s) fora da superfície";
        return ResultadoFerramenta.Ok(texto + ".", new JsonObject { ["alinhamento"] = al.Name, ["superficie"] = surf.Name, ["afastamento"] = off, ["resumo"] = resumo, ["amostras"] = arr });
    }

    private static ResultadoFerramenta CriarPerfilTerreno(Argumentos a)
    {
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var al = AbrirAlinhamento(tr, a.TextoObrigatorio("alinhamento"));
        var surf = AbrirSuperficie(tr, a.Texto("superficie"));
        var nome = string.IsNullOrWhiteSpace(a.Texto("nome")) ? $"{surf.Name} - terreno" : a.Texto("nome")!.Trim();
        if (CivilUtil.Perfis(tr, al).Any(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            throw new ErroFerramenta($"O alinhamento {al.Name} já tem um perfil '{nome}'. Informe outro 'nome'.");
        var civ = CivilApplication.ActiveDocument;
        var estilos = civ.Styles.ProfileStyles;
        var rotulos = civ.Styles.LabelSetStyles.ProfileLabelSetStyles;
        if (estilos.Count == 0) throw new ErroFerramenta("O desenho não tem estilos de perfil.");
        var id = Profile.CreateFromSurface(nome, al.ObjectId, surf.ObjectId, al.LayerId, estilos[0], rotulos.Count > 0 ? rotulos[0] : ObjectId.Null);
        var p = (Profile)tr.GetObject(id, OpenMode.ForRead);
        var dados = new JsonObject
        {
            ["perfil"] = p.Name, ["alinhamento"] = al.Name, ["superficie"] = surf.Name,
            ["estacaInicial"] = N(p.StartingStation), ["estacaFinal"] = N(p.EndingStation), ["estilo"] = NomeEstilo(tr, estilos[0]),
        };
        tr.Commit();
        return ResultadoFerramenta.Ok($"Perfil '{p.Name}' criado no alinhamento {al.Name} a partir da superfície {surf.Name} " +
                                      $"({Num.Estaca(p.StartingStation)} a {Num.Estaca(p.EndingStation)}). Desfaz com U no Civil 3D.", dados);
    }

    private static string NomeEstilo(Transaction tr, ObjectId id)
    {
        try { return tr.GetObject(id, OpenMode.ForRead) is Autodesk.Civil.DatabaseServices.Styles.StyleBase s ? s.Name : ""; }
        catch { return ""; }
    }

    private static ResultadoFerramenta ReconstruirCorredores(Argumentos a)
    {
        var civ = CivilApplication.ActiveDocument;
        var pedidos = a.ListaTextos("corredores").Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var todos = new List<(ObjectId Id, string Nome)>();
        foreach (ObjectId id in civ.CorridorCollection)
            if (tr.GetObject(id, OpenMode.ForRead) is Corridor c) todos.Add((id, c.Name));
        if (todos.Count == 0) throw new ErroFerramenta("O desenho não tem corredores.");
        var alvo = pedidos.Count == 0 ? todos : pedidos.Select(n =>
        {
            var it = todos.FirstOrDefault(t => string.Equals(t.Nome, n, StringComparison.OrdinalIgnoreCase));
            if (it.Id.IsNull) throw new ErroFerramenta($"Corredor '{n}' não encontrado. Existentes: {string.Join(", ", todos.Select(t => $"'{t.Nome}'"))}.");
            return it;
        }).ToList();

        var arr = new JsonArray();
        foreach (var (id, nome) in alvo)
        {
            var sw = Stopwatch.StartNew();
            var c = (Corridor)tr.GetObject(id, OpenMode.ForWrite);
            c.Rebuild();
            arr.Add(new JsonObject { ["corredor"] = nome, ["tempo_ms"] = sw.ElapsedMilliseconds });
        }
        tr.Commit();
        return ResultadoFerramenta.Ok($"{alvo.Count} corredor(es) reconstruído(s): {string.Join(", ", alvo.Select(x => x.Nome))}. Desfaz com U no Civil 3D.",
            new JsonObject { ["corredores"] = arr });
    }
}
