using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Horizontal;
using Projeta.Sinalizacao.Core.Vertical;

namespace Projeta.Sinalizacao.Core.Quantitativos;

/// <summary>Localização de um objeto em relação a um eixo (alinhamento) e coordenadas.</summary>
public sealed record Localizacao(string Eixo = "", double? Estaca = null, double? EstacaFim = null, double? Afastamento = null, double N = 0, double E = 0)
{
    public string Lado => Afastamento is null ? "" : Afastamento < -0.01 ? "LD" : Afastamento > 0.01 ? "LE" : "EIXO";
}

public sealed record ItemQuant(ObjetoSin Obj, Localizacao Loc);

/// <summary>Monta a pasta de quantitativos (resumo + detalhamento por tipo).</summary>
public static class PlanilhaQuantitativos
{
    private static Celula T(string s) => new(s);
    private static Celula Nm(double v) => new(Num.Arred(v, 3), Estilo.Numero2);
    private static Celula Nm3(double v) => new(Num.Arred(v, 3), Estilo.Numero3);
    private static Celula I(double v) => new(Math.Round(v), Estilo.Inteiro);
    private static Celula H(string s) => new(s, Estilo.Cabecalho);
    private static Celula Est(double? s, double passo) => s is null ? new Celula(null) : new Celula(Estaca.Formatar(s.Value, passo));
    private static Celula Opc(double? v) => v is null ? new Celula(null) : Nm(v.Value);

    public static string FamiliaDe(PlacaItem p)
    {
        if (!string.IsNullOrWhiteSpace(p.Modelo)) return Nome(ModeloPlaca.DeJson(p.Modelo!)?.Familia ?? Vertical.Familia.Indicacao);
        var t = Catalogo.Obter(p.Codigo);
        if (t is null) return "Outras";
        return p.Obra ? "Obras (temporária)" : Nome(t.Familia);
    }

    public static string Nome(Vertical.Familia f) => f switch
    {
        Vertical.Familia.Regulamentacao => "Regulamentação",
        Vertical.Familia.Advertencia => "Advertência",
        Vertical.Familia.Obras => "Obras (temporária)",
        Vertical.Familia.Indicacao => "Indicação",
        Vertical.Familia.Educativa => "Educativa",
        Vertical.Familia.ServicosAuxiliares => "Serviços auxiliares",
        _ => f.ToString(),
    };

    public static List<Aba> Montar(IList<ItemQuant> itens, string titulo, double passoEstaca, Normas.Normas normas, Normas.TipoVia via)
    {
        var abas = new List<Aba>();
        string sit(ObjetoSin o) => ObjetoSin.NomeSituacao(o.Situacao);
        var placas = itens.Where(i => i.Obj.Categoria == Categoria.Placa).ToList();
        var longit = itens.Where(i => i.Obj.Categoria == Categoria.MarcaLongitudinal).ToList();
        var transv = itens.Where(i => i.Obj.Categoria is Categoria.MarcaTransversal or Categoria.Inscricao).ToList();
        var tachas = itens.Where(i => i.Obj.Categoria == Categoria.Tachas).ToList();
        var sema = itens.Where(i => i.Obj.Categoria is Categoria.Semaforica or Categoria.RedeSemaforica).ToList();
        var obras = itens.Where(i => i.Obj.Categoria is Categoria.Obra or Categoria.ZonaObra).ToList();
        var defs = itens.Where(i => i.Obj.Categoria == Categoria.Defensa).ToList();

        // ============================================================ RESUMO
        var r = new Aba("Resumo");
        r.Larguras.AddRange(new double[] { 48, 26, 22, 14, 10 });
        r.Linha(new Celula(titulo, Estilo.Titulo));
        r.Linha(T($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} — PROJETA Sinalização"));
        r.Vazia();

        void Secao(string nome, string[] cab, IEnumerable<Celula[]> linhas)
        {
            var l = linhas.ToList();
            if (l.Count == 0) return;
            r.Linha(new Celula(nome, Estilo.Subtitulo));
            r.Linha(cab.Select(H).ToArray());
            foreach (var x in l) r.Linha(x);
            r.Vazia();
        }

        // placas por família/chapa/película/situação
        var linhasPlacas = placas.SelectMany(i => (i.Obj.Placa?.Placas ?? new()).Select(p => (i, p)))
            .Select(x =>
            {
                var f = Motor.Face(x.p, normas, via);
                return (fam: FamiliaDe(x.p), chapa: x.i.Obj.Placa!.Chapa, pel: x.i.Obj.Placa.Pelicula, sit: sit(x.i.Obj), area: f.area);
            })
            .GroupBy(x => (x.fam, x.chapa, x.pel, x.sit))
            .OrderBy(g => g.Key.sit).ThenBy(g => g.Key.fam)
            .Select(g => new Celula[] { T($"Placa de {g.Key.fam.ToLowerInvariant()} — {g.Key.chapa} / {g.Key.pel}"), T(g.Key.sit), T("m²"), Nm(g.Sum(x => x.area)), I(g.Count()) });
        Secao("SINALIZAÇÃO VERTICAL — PLACAS", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Peças" }, linhasPlacas);

        var linhasSup = placas.Where(i => i.Obj.Placa!.Suporte != TipoSuporte.Existente)
            .GroupBy(i => (tipo: Suportes.Nome(i.Obj.Placa!.Suporte), mat: i.Obj.Placa.MaterialSuporte ?? normas.MaterialSuportePadrao, s: sit(i.Obj)))
            .OrderBy(g => g.Key.s).ThenBy(g => g.Key.tipo)
            .Select(g => new Celula[] { T($"Suporte — {g.Key.tipo} ({g.Key.mat})"), T(g.Key.s), T("m"), Nm(g.Sum(i => i.Obj.Q("suporteTotal"))), I(g.Count()) });
        Secao("SINALIZAÇÃO VERTICAL — SUPORTES", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Peças" }, linhasSup);

        // pintura por cor/material/situação
        var pintura = longit.Concat(transv)
            .GroupBy(i => (cor: Cores.Nome((Cor)(int)i.Obj.Q("cor")), mat: i.Obj.Marca?.Material ?? "", s: sit(i.Obj)))
            .OrderBy(g => g.Key.s).ThenBy(g => g.Key.cor)
            .Select(g => new Celula[] { T($"Pintura de sinalização horizontal {g.Key.cor} — {g.Key.mat}"), T(g.Key.s), T("m²"), Nm(g.Sum(i => i.Obj.Q("area"))), I(g.Count()) });
        Secao("SINALIZAÇÃO HORIZONTAL — PINTURA (ÁREA)", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Objetos" }, pintura);

        var porCodigo = longit
            .GroupBy(i => (c: i.Obj.Codigo, s: sit(i.Obj)))
            .OrderBy(g => g.Key.c)
            .Select(g => new Celula[] { T($"{g.Key.c} — {CatalogoLongitudinal.Obter(g.Key.c)?.Nome ?? g.Key.c} (extensão)"), T(g.Key.s), T("m"), Nm(g.Sum(i => i.Obj.Q("comp"))), Nm(g.Sum(i => i.Obj.Q("area"))) })
            .Concat(transv.GroupBy(i => (c: i.Obj.Codigo, s: sit(i.Obj))).OrderBy(g => g.Key.c)
                .Select(g => new Celula[] { T($"{g.Key.c} — {g.First().Obj.Descricao}"), T(g.Key.s), T("un"), I(g.Count()), Nm(g.Sum(i => i.Obj.Q("area"))) }));
        Secao("SINALIZAÇÃO HORIZONTAL — POR TIPO", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Área (m²)" }, porCodigo);

        var ltachas = tachas.GroupBy(i => (d: i.Obj.Descricao, s: sit(i.Obj))).OrderBy(g => g.Key.d)
            .Select(g => new Celula[] { T(g.Key.d), T(g.Key.s), T("un"), I(g.Sum(i => i.Obj.Q("un"))), I(g.Count()) });
        Secao("DISPOSITIVOS AUXILIARES — TACHAS E TACHÕES", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Trechos" }, ltachas);

        var lsema = sema.GroupBy(i => (d: i.Obj.Descricao, s: sit(i.Obj), red: i.Obj.Categoria == Categoria.RedeSemaforica)).OrderBy(g => g.Key.red).ThenBy(g => g.Key.d)
            .Select(g => g.Key.red
                ? new Celula[] { T(g.Key.d), T(g.Key.s), T("m"), Nm(g.Sum(i => i.Obj.Q("comp"))), I(g.Count()) }
                : new Celula[] { T(g.Key.d), T(g.Key.s), T("un"), I(g.Count()), I(g.Count()) });
        Secao("SINALIZAÇÃO SEMAFÓRICA", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Objetos" }, lsema);

        var lobras = obras.Where(i => i.Obj.Categoria == Categoria.Obra).GroupBy(i => (d: i.Obj.Descricao, s: sit(i.Obj))).OrderBy(g => g.Key.d)
            .Select(g => new Celula[] { T(g.Key.d), T(g.Key.s), T("un"), I(g.Count()), I(g.Count()) })
            .Concat(obras.Where(i => i.Obj.Categoria == Categoria.ZonaObra).SelectMany(i => new[]
            {
                new Celula[] { T($"{i.Obj.Id} — {i.Obj.Descricao}"), T(sit(i.Obj)), T("un"), I(i.Obj.Q("dispositivos")), T("dispositivos") },
                new Celula[] { T($"{i.Obj.Id} — placas de pré-sinalização"), T(sit(i.Obj)), T("m²"), Nm(i.Obj.Q("areaPlacas")), I(i.Obj.Q("placas")) },
            }));
        Secao("SINALIZAÇÃO DE OBRAS (TEMPORÁRIA)", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Peças" }, lobras);

        var ldef = defs.GroupBy(i => (d: i.Obj.Descricao, s: sit(i.Obj))).OrderBy(g => g.Key.d)
            .SelectMany(g => new[]
            {
                new Celula[] { T(g.Key.d), T(g.Key.s), T("m"), Nm(g.Sum(i => i.Obj.Q("comp"))), I(g.Count()) },
                new Celula[] { T($"   postes"), T(g.Key.s), T("un"), I(g.Sum(i => i.Obj.Q("postes"))), new Celula(null) },
                new Celula[] { T($"   terminais"), T(g.Key.s), T("un"), I(g.Sum(i => i.Obj.Q("terminais"))), new Celula(null) },
            });
        Secao("DISPOSITIVOS DE SEGURANÇA — DEFENSAS E BARREIRAS", new[] { "Descrição", "Situação", "Unidade", "Quantidade", "Trechos" }, ldef);
        abas.Add(r);

        // ============================================================ DETALHAMENTO
        Aba Det(string nome, string[] cab, double[] larg, IEnumerable<Celula[]> linhas)
        {
            var a = new Aba(nome);
            a.Larguras.AddRange(larg);
            a.Linha(cab.Select(H).ToArray());
            a.LinhaCabecalho = 1;
            a.NumColunasFiltro = cab.Length;
            foreach (var l in linhas) a.Linha(l);
            return a;
        }

        if (placas.Count > 0)
        {
            abas.Add(Det("Placas", new[] { "ID", "Código", "Nome", "Família", "Situação", "Dimensão", "Área (m²)", "Chapa", "Película", "Suporte", "Eixo", "Estaca", "Lado", "Afast. (m)", "N (Y)", "E (X)" },
                new double[] { 9, 10, 40, 16, 16, 14, 10, 22, 24, 22, 14, 12, 6, 10, 14, 14 },
                placas.OrderBy(i => i.Obj.Id).SelectMany(i => i.Obj.Placa!.Placas.Select(p =>
                {
                    var f = Motor.Face(p, normas, via);
                    string dim = GeradorPlaca.TextoDimensao(f.forma, f.forma == Forma.Retangular ? f.largura : p.Dimensao, f.altura);
                    string cod = string.IsNullOrWhiteSpace(p.Modelo) ? p.Codigo : (ModeloPlaca.DeJson(p.Modelo!)?.Codigo ?? "IND");
                    if (!string.IsNullOrWhiteSpace(p.Valor)) cod += $" ({p.Valor})";
                    return new Celula[] { T(i.Obj.Id), T(cod), T(f.nome), T(FamiliaDe(p)), T(sit(i.Obj)), T(dim), Nm3(f.area), T(i.Obj.Placa.Chapa), T(i.Obj.Placa.Pelicula),
                        T(Suportes.Nome(i.Obj.Placa.Suporte)), T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), T(i.Loc.Lado), Opc(i.Loc.Afastamento), Nm3(i.Loc.N), Nm3(i.Loc.E) };
                }))));

            abas.Add(Det("Suportes", new[] { "ID", "Tipo", "Situação", "Nº colunas", "Comp. coluna (m)", "Viga/braço (m)", "Total (m)", "Material", "Placas", "Eixo", "Estaca", "Lado", "N (Y)", "E (X)" },
                new double[] { 9, 26, 16, 10, 14, 13, 11, 28, 22, 14, 12, 6, 14, 14 },
                placas.OrderBy(i => i.Obj.Id).Select(i => new Celula[]
                {
                    T(i.Obj.Id), T(Suportes.Nome(i.Obj.Placa!.Suporte)), T(sit(i.Obj)), I(i.Obj.Q("suporteColunas")), Nm(i.Obj.Q("suporteComprColuna")),
                    Nm(i.Obj.Q("suporteViga")), Nm(i.Obj.Q("suporteTotal")), T(i.Obj.Placa.Suporte == TipoSuporte.Existente ? "—" : i.Obj.Placa.MaterialSuporte ?? normas.MaterialSuportePadrao),
                    T(i.Obj.Codigo), T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), T(i.Loc.Lado), Nm3(i.Loc.N), Nm3(i.Loc.E),
                })));
        }

        if (longit.Count > 0)
            abas.Add(Det("Marcas longitudinais", new[] { "ID", "Código", "Descrição", "Situação", "Cor", "Largura (m)", "Cadência (m)", "Extensão (m)", "Comp. pintado (m)", "Área (m²)", "Material", "Eixo", "Est. início", "Est. fim", "Lado" },
                new double[] { 9, 9, 40, 16, 10, 11, 12, 12, 14, 11, 28, 14, 12, 12, 6 },
                longit.OrderBy(i => i.Obj.Id).Select(i => new Celula[]
                {
                    T(i.Obj.Id), T(i.Obj.Codigo), T(i.Obj.Descricao), T(sit(i.Obj)), T(Cores.Nome((Cor)(int)i.Obj.Q("cor"))), Nm(i.Obj.Q("largura")),
                    T(i.Obj.Q("traco") > 0 ? $"{Num.F0(i.Obj.Q("traco"))}/{Num.F0(i.Obj.Q("espaco"))}" : "contínua"), Nm(i.Obj.Q("comp")), Nm(i.Obj.Q("compPintado")),
                    Nm(i.Obj.Q("area")), T(i.Obj.Marca?.Material ?? ""), T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), Est(i.Loc.EstacaFim, passoEstaca), T(i.Loc.Lado),
                })));

        if (transv.Count > 0)
            abas.Add(Det("Transversais e inscrições", new[] { "ID", "Código", "Descrição", "Situação", "Cor", "Área (m²)", "Comp. ref. (m)", "Peças", "Material", "Eixo", "Estaca", "Lado", "N (Y)", "E (X)" },
                new double[] { 9, 9, 44, 16, 10, 11, 13, 8, 28, 14, 12, 6, 14, 14 },
                transv.OrderBy(i => i.Obj.Id).Select(i => new Celula[]
                {
                    T(i.Obj.Id), T(i.Obj.Codigo), T(i.Obj.Descricao), T(sit(i.Obj)), T(Cores.Nome((Cor)(int)i.Obj.Q("cor"))), Nm(i.Obj.Q("area")), Nm(i.Obj.Q("comp")),
                    I(i.Obj.Q("pecas")), T(i.Obj.Marca?.Material ?? ""), T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), T(i.Loc.Lado), Nm3(i.Loc.N), Nm3(i.Loc.E),
                })));

        if (tachas.Count > 0)
            abas.Add(Det("Tachas", new[] { "ID", "Descrição", "Situação", "Quantidade", "Espaçamento (m)", "Extensão (m)", "Eixo", "Est. início", "Est. fim", "Lado" },
                new double[] { 9, 36, 16, 11, 14, 12, 14, 12, 12, 6 },
                tachas.OrderBy(i => i.Obj.Id).Select(i => new Celula[]
                {
                    T(i.Obj.Id), T(i.Obj.Descricao), T(sit(i.Obj)), I(i.Obj.Q("un")), Nm(i.Obj.Q("esp")), Nm(i.Obj.Q("comp")), T(i.Loc.Eixo),
                    Est(i.Loc.Estaca, passoEstaca), Est(i.Loc.EstacaFim, passoEstaca), T(i.Loc.Lado),
                })));

        if (sema.Count > 0)
            abas.Add(Det("Semafórica", new[] { "ID", "Código", "Descrição", "Situação", "Quantidade", "Unidade", "Eixo", "Estaca", "Lado", "N (Y)", "E (X)" },
                new double[] { 9, 10, 40, 16, 11, 8, 14, 12, 6, 14, 14 },
                sema.OrderBy(i => i.Obj.Id).Select(i =>
                {
                    bool rede = i.Obj.Categoria == Categoria.RedeSemaforica;
                    return new Celula[] { T(i.Obj.Id), T(i.Obj.Codigo), T(i.Obj.Descricao), T(sit(i.Obj)), rede ? Nm(i.Obj.Q("comp")) : I(1), T(rede ? "m" : "un"),
                        T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), T(i.Loc.Lado), Nm3(i.Loc.N), Nm3(i.Loc.E) };
                })));

        if (obras.Count > 0)
            abas.Add(Det("Obras", new[] { "ID", "Código", "Descrição", "Situação", "Dispositivos (un)", "Placas (un)", "Placas (m²)", "Taper (m)", "Zona de proteção (m)", "Extensão (m)", "Eixo", "Estaca", "N (Y)", "E (X)" },
                new double[] { 9, 10, 40, 16, 14, 11, 11, 10, 16, 12, 14, 12, 14, 14 },
                obras.OrderBy(i => i.Obj.Id).Select(i =>
                {
                    bool zona = i.Obj.Categoria == Categoria.ZonaObra;
                    return new Celula[] { T(i.Obj.Id), T(zona ? i.Obj.Zona?.Dispositivo ?? "" : i.Obj.Codigo), T(i.Obj.Descricao), T(sit(i.Obj)),
                        I(zona ? i.Obj.Q("dispositivos") : 1), I(i.Obj.Q("placas")), Nm(i.Obj.Q("areaPlacas")), Opc(zona ? i.Obj.Q("taper") : null),
                        Opc(zona ? i.Obj.Q("protecao") : null), Opc(zona ? i.Obj.Q("extensao") : null), T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), Nm3(i.Loc.N), Nm3(i.Loc.E) };
                })));

        if (defs.Count > 0)
            abas.Add(Det("Defensas", new[] { "ID", "Tipo", "Situação", "Comprimento (m)", "Terminais (m)", "Postes (un)", "Lâminas (un)", "Terminais (un)", "Terminal início", "Terminal fim", "Eixo", "Est. início", "Est. fim", "Lado" },
                new double[] { 9, 38, 16, 14, 13, 11, 12, 13, 22, 22, 14, 12, 12, 6 },
                defs.OrderBy(i => i.Obj.Id).Select(i => new Celula[]
                {
                    T(i.Obj.Id), T(i.Obj.Descricao), T(sit(i.Obj)), Nm(i.Obj.Q("comp")), Nm(i.Obj.Q("compTerminais")), I(i.Obj.Q("postes")), I(i.Obj.Q("laminas")),
                    I(i.Obj.Q("terminais")), T(GeradorDefensa.Nome(i.Obj.Defensa?.TerminalInicio ?? TipoTerminal.Nenhum)), T(GeradorDefensa.Nome(i.Obj.Defensa?.TerminalFim ?? TipoTerminal.Nenhum)),
                    T(i.Loc.Eixo), Est(i.Loc.Estaca, passoEstaca), Est(i.Loc.EstacaFim, passoEstaca), T(i.Loc.Lado),
                })));

        return abas;
    }
}
