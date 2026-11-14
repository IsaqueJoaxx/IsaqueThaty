using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Escavacao;
using Projeta.Core.Esgoto;
using Projeta.Core.Expressoes;
using Projeta.Core.Hidraulica;
using Projeta.Core.Hidrologia;
using Projeta.Core.Integracao;
using Projeta.Core.Longitudinais;
using Projeta.Core.Modelador;
using Projeta.Core.Redes;
using Projeta.Core.Relatorios;

// ============================================================================
// Testes do núcleo do PROJETA. Uso:  dotnet run [-- pasta_saida]
// Variáveis opcionais: PROJETA_RUNSWMM e PROJETA_RUNEPANET (executáveis dos motores)
// ============================================================================

var saida = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "projeta-testes");
Directory.CreateDirectory(saida);
int ok = 0, falhas = 0;
var log = new List<string>();

void Teste(string nome, Action corpo)
{
    try { corpo(); ok++; Console.WriteLine($"  ok   {nome}"); }
    catch (Exception ex) { falhas++; Console.WriteLine($"  FALHA {nome}: {ex.Message}"); log.Add($"{nome}: {ex}"); }
}
void Igual(double esperado, double obtido, double tol, string oque)
{
    if (double.IsNaN(obtido) || Math.Abs(esperado - obtido) > tol) throw new Exception($"{oque}: esperado {esperado:G6}, obtido {obtido:G6}");
}
void Verdade(bool cond, string oque) { if (!cond) throw new Exception(oque); }

Console.WriteLine("== Expressões");
Teste("aritmética e precedência", () =>
{
    var c = new ContextoDicionario(); c["a"] = 2; c["b"] = 3;
    Igual(14, Expressao.Numero("a + b*4", c), 1e-12, "a+b*4");
    Igual(512, Expressao.Numero("2^3^2", c), 1e-9, "potência à direita");
    Igual(-4, Expressao.Numero("-a^2", c), 1e-12, "-a^2");
    Igual(1, Expressao.Numero("a < b && b >= 3", c), 0, "lógica");
    Igual(5, Expressao.Numero("a > b ? 1 : 5", c), 0, "ternário");
    Igual(7, Expressao.Numero("se(a==2, 7, 9)", c), 0, "se()");
    Igual(Math.PI, Expressao.Numero("pi", c), 1e-12, "pi");
    Igual(2.5, Expressao.Numero("interp(5, 0,0, 10,5)", c), 1e-12, "interp");
    Igual(0.5, Expressao.Numero("round(sin(rad(30)), 6)", c), 1e-12, "sin/rad");
    c["Mat"] = "Concreto";
    Igual(1, Expressao.Numero("Mat == 'concreto'", c), 0, "texto");
});
Teste("erros de sintaxe", () =>
{
    bool erro = false;
    try { Expressao.Numero("2 + * 3", new ContextoDicionario()); } catch (ErroExpressao) { erro = true; }
    Verdade(erro, "deveria acusar erro de sintaxe");
    erro = false;
    try { Expressao.Numero("x + 1", new ContextoDicionario()); } catch (ErroExpressao) { erro = true; }
    Verdade(erro, "deveria acusar variável desconhecida");
});

Console.WriteLine("== Hidrologia");
var projeto = new Projeto();
var eqBH = projeto.Equacao("BH-UFU2024")!;
Teste("IDF Belo Horizonte (generalizada)", () =>
{
    double esperado = 1076.16 * Math.Pow(10, 0.114) / Math.Pow(10 + 9.783, 0.724);
    Igual(esperado, eqBH.Intensidade(10, 10), 1e-9, "i(10,10)");
    Verdade(eqBH.Intensidade(10, 10) > eqBH.Intensidade(10, 30), "i decresce com a duração");
    Verdade(eqBH.Intensidade(25, 10) > eqBH.Intensidade(10, 10), "i cresce com T");
});
Teste("IDF por expressão == generalizada", () =>
{
    var e = new EquacaoChuva { Tipo = TipoEquacaoChuva.Expressao, Formula = "1076.16*T^0.114/(t+9.783)^0.724" };
    Igual(eqBH.Intensidade(5, 15), e.Intensidade(5, 15), 1e-9, "i");
});
Teste("IDF regional RMBH (Pinheiro) — valor plausível", () =>
{
    var e = projeto.Equacao("RMBH-PINHEIRO")!;
    double i = e.Intensidade(10, 60);
    Verdade(i > 30 && i < 120, $"i(10 anos, 60 min) = {i:0.0} mm/h fora do esperado");
});
Teste("hietograma de blocos alternados conserva a altura", () =>
{
    var h = Hietograma.BlocosAlternados(eqBH, 10, 60, 5);
    double total = h.Sum(x => x * 5 / 60);
    Igual(eqBH.Altura(10, 60), total, 1e-6, "altura total");
    int pico = Array.IndexOf(h, h.Max());
    Verdade(pico == 5 || pico == 6, $"pico no centro (posição {pico})");
});
Teste("Kirpich", () => Igual(57 * Math.Pow(0.5 * 0.5 * 0.5 / 10, 0.385), TempoConcentracao.Kirpich(500, 10), 1e-9, "tc"));

Console.WriteLine("== Hidráulica");
Teste("Manning — seção plena circular", () =>
{
    var s = new SecaoCircular(0.6);
    double a = Math.PI * 0.09, r = 0.15;
    double q = 1 / 0.013 * a * Math.Pow(r, 2.0 / 3) * Math.Sqrt(0.01);
    Igual(q, Manning.VazaoPlena(s, 0.013, 0.01), 1e-9, "Qp");
    Igual(q, Manning.Vazao(s, 0.6, 0.013, 0.01), 1e-6, "Q(y=D)");
});
Teste("altura normal (ida e volta)", () =>
{
    var s = new SecaoCircular(0.8);
    foreach (var yd in new[] { 0.1, 0.3, 0.5, 0.75, 0.9 })
    {
        double q = Manning.Vazao(s, yd * 0.8, 0.015, 0.005);
        Igual(yd * 0.8, Manning.AlturaNormal(s, q, 0.015, 0.005), 1e-5, $"y/D={yd}");
    }
    Verdade(double.IsNaN(Manning.AlturaNormal(s, 10, 0.015, 0.005)), "vazão acima da capacidade deve dar NaN");
});
Teste("seção poligonal == trapézio analítico", () =>
{
    var s = SecaoPoligonalAberta.Trapezoidal(0.5, 1.5, 1.5, 0.6);
    double y = 0.4;
    Igual((0.5 + 1.5 * y) * y, s.Area(y), 1e-9, "área");
    Igual(0.5 + 2 * y * Math.Sqrt(1 + 2.25), s.PerimetroMolhado(y), 1e-9, "perímetro");
    Igual(0.5 + 2 * 1.5 * y, s.LarguraSuperficial(y), 1e-9, "largura superficial");
});
Teste("declividade para velocidade", () =>
{
    var s = new SecaoCircular(0.5);
    double sv = Manning.DeclividadeParaVelocidade(s, 0.08, 0.013, 1.0);
    double y = Manning.AlturaNormal(s, 0.08, 0.013, sv);
    Igual(1.0, Manning.Velocidade(s, 0.08, y), 1e-3, "v");
});

Console.WriteLine("== Drenagem — rede de exemplo");
Rede CriarRedeDrenagem()
{
    var r = new Rede { Nome = "Drenagem Teste", Tipo = TipoRede.Drenagem };
    // coletor principal descendo ~1,2 % e um ramal
    var pts = new (string n, double x, double y, double z, TipoNo t)[]
    {
        ("A", 0, 0, 850.00, TipoNo.PV), ("B", 60, 0, 849.40, TipoNo.PV), ("C", 120, 0, 848.60, TipoNo.PV),
        ("D", 180, 0, 847.70, TipoNo.PV), ("E", 240, 0, 846.50, TipoNo.Exutorio),
        ("R1", 60, 70, 850.20, TipoNo.PV), ("R2", 120, 50, 849.30, TipoNo.PV),
        ("BL1", 2, 5, 850.05, TipoNo.BocaDeLobo),
    };
    foreach (var p in pts) { var n = r.NovoNo(p.t, p.x, p.y); n.Nome = p.n; n.CotaTerreno = p.z; }
    void T(string a, string b) => r.NovoTrecho(r.NoPorNome(a)!, r.NoPorNome(b)!);
    T("BL1", "A"); T("A", "B"); T("B", "C"); T("C", "D"); T("D", "E"); T("R1", "R2"); T("R2", "C");
    void B(string no, double ha, double c) => r.Bacias.Add(new Bacia { Nome = "B-" + no, NoId = r.NoPorNome(no)!.Id, AreaHa = ha, C = c });
    B("BL1", 0.6, 0.75); B("A", 1.2, 0.70); B("B", 1.5, 0.65); B("C", 0.9, 0.70); B("D", 2.0, 0.60); B("R1", 1.1, 0.70); B("R2", 0.8, 0.70);
    return r;
}
var drenagem = CriarRedeDrenagem();
projeto.Redes.Add(drenagem);
ResultadoRede resD = null!;
Teste("dimensionamento sem erros", () =>
{
    resD = DimensionadorGravidade.Calcular(drenagem, projeto);
    drenagem.Resultado = resD;
    var erros = resD.Mensagens.Where(m => m.Gravidade == Gravidade.Erro).Select(m => m.ToString()).Concat(resD.Trechos.SelectMany(t => t.Erros.Select(e => t.Nome + ": " + e))).ToList();
    Verdade(erros.Count == 0, "erros: " + string.Join(" | ", erros));
    Verdade(resD.Trechos.Count == 7, "7 trechos");
});
Teste("restrições atendidas em todos os trechos", () =>
{
    var p = drenagem.Drenagem;
    foreach (var t in resD.Trechos)
    {
        Verdade(t.LaminaRelativa <= p.LaminaMaxima + 1e-6, $"{t.Nome}: y/D {t.LaminaRelativa:0.000}");
        Verdade(t.Velocidade <= p.VelocidadeMaxima + 1e-6, $"{t.Nome}: v {t.Velocidade:0.00}");
        Verdade(t.RecobrimentoMontante >= p.RecobrimentoMinimo - 1e-3 && t.RecobrimentoJusante >= p.RecobrimentoMinimo - 1e-3, $"{t.Nome}: recobrimento");
        Verdade(t.Declividade >= p.DeclividadeMinima - 1e-9, $"{t.Nome}: declividade mínima");
        Igual(t.CotaFundoMontante - t.Declividade * t.Comprimento, t.CotaFundoJusante, 1e-6, $"{t.Nome}: coerência de cotas");
    }
});
Teste("diâmetros não diminuem e geratriz superior concordante", () =>
{
    var top = new Topologia(drenagem);
    foreach (var t in drenagem.Trechos)
    {
        var r = resD.Trecho(t.Id)!;
        foreach (var e in top.Chegam(t.NoMontanteId))
        {
            var re = resD.Trecho(e.Id)!;
            Verdade(r.Diametro >= re.Diametro - 1e-9 || r.Forma != re.Forma, $"{t.Nome} reduz diâmetro em relação a {e.Nome}");
            Verdade(r.CotaFundoMontante + r.Altura <= re.CotaFundoJusante + re.Altura + 1e-6, $"{t.Nome}: geratriz superior acima da de {e.Nome}");
        }
    }
});
Teste("vazão acumula para jusante e tc cresce", () =>
{
    var cd = resD.Trechos.First(t => t.Montante == "C");
    var bc = resD.Trechos.First(t => t.Montante == "B");
    Verdade(cd.SomaCA > bc.SomaCA, "ΣCA deve crescer");
    Verdade(cd.Tc >= bc.Tc, "tc deve crescer");
    double sca = drenagem.Bacias.Where(b => b.Nome != "B-D").Sum(b => b.C * b.Area);
    Igual(sca, cd.SomaCA, 1e-9, "ΣCA em C-D");
});
Teste("diâmetro forçado e cota forçada respeitados", () =>
{
    var r2 = JsonProjeta.Clonar(drenagem);
    var t = r2.Trechos.First(x => r2.NoPorId(x.NoMontanteId)!.Nome == "D");
    t.DiametroForcado = 1.20;
    var rr = DimensionadorGravidade.Calcular(r2, projeto);
    Igual(1.20, rr.Trecho(t.Id)!.Diametro, 1e-12, "D forçado");
    var t2 = r2.Trechos.First(x => r2.NoPorId(x.NoMontanteId)!.Nome == "A");
    t2.CotaMontanteForcada = 847.0;
    rr = DimensionadorGravidade.Calcular(r2, projeto);
    Igual(847.0, rr.Trecho(t2.Id)!.CotaFundoMontante, 1e-9, "cota forçada");
});
Teste("ciclo é detectado", () =>
{
    var r2 = JsonProjeta.Clonar(drenagem);
    r2.NovoTrecho(r2.NoPorNome("E")!, r2.NoPorNome("A")!);
    var rr = DimensionadorGravidade.Calcular(r2, projeto);
    Verdade(rr.Mensagens.Any(m => m.Gravidade == Gravidade.Erro && m.Texto.Contains("ciclo")), "deveria acusar ciclo");
});
Teste("numeração por coletor", () =>
{
    var r2 = JsonProjeta.Clonar(drenagem);
    Numeracao.Numerar(r2);
    var nomes = r2.Nos.Select(n => n.Nome).ToList();
    Verdade(nomes.Distinct().Count() == nomes.Count, "nomes únicos");
    // coletor principal: BL1 (boca de lobo) -> A -> B -> C -> D -> E
    Verdade(r2.Nos.First(n => n.X == 0 && n.Y == 0).Nome == "PV-01", "A deve ser PV-01 (cabeceira do coletor principal após a BL)");
    Verdade(r2.Nos.First(n => n.Tipo == TipoNo.BocaDeLobo).Nome == "BL-01", "boca de lobo numerada com prefixo próprio");
});
Teste("interferências entre redes", () =>
{
    var outra = new Rede { Nome = "Esgoto cruzando", Tipo = TipoRede.Esgoto };
    var a = outra.NovoNo(TipoNo.PV, 90, -30); a.CotaTerreno = 849.0;
    var b = outra.NovoNo(TipoNo.PV, 90, 30); b.CotaTerreno = 849.0;
    outra.NovoTrecho(a, b);
    outra.Esgoto.PopulacaoInicial = 50; outra.Esgoto.PopulacaoFinal = 80;
    outra.Resultado = DimensionadorGravidade.Calcular(outra, projeto);
    var lista = VerificadorInterferencias.Verificar(new[] { drenagem, outra }, 5.0);
    Verdade(lista.Count == 1, $"esperado 1 cruzamento, obtido {lista.Count}");
    Igual(90, lista[0].Ponto.X, 1e-6, "x do cruzamento");
});

Console.WriteLine("== Esgoto (NBR 9649)");
Rede esgoto = null!;
Teste("rede de esgoto atende NBR 9649", () =>
{
    esgoto = new Rede { Nome = "Esgoto Teste", Tipo = TipoRede.Esgoto, PrefixoNo = "PV-E" };
    esgoto.Esgoto.PopulacaoInicial = 1200; esgoto.Esgoto.PopulacaoFinal = 2000;
    double z = 860;
    No? ant = null;
    for (int i = 0; i < 6; i++)
    {
        var n = esgoto.NovoNo(i == 5 ? TipoNo.Exutorio : TipoNo.PV, i * 70, 10); n.CotaTerreno = z - i * 0.35;
        if (ant is not null) esgoto.NovoTrecho(ant, n);
        ant = n;
    }
    var lateral = esgoto.NovoNo(TipoNo.PV, 140, 90); lateral.CotaTerreno = 860.2;
    esgoto.NovoTrecho(lateral, esgoto.Nos[2]);
    var rr = DimensionadorGravidade.Calcular(esgoto, projeto);
    esgoto.Resultado = rr;
    var erros = rr.Trechos.SelectMany(t => t.Erros.Select(e => t.Nome + ": " + e)).ToList();
    Verdade(erros.Count == 0, string.Join(" | ", erros));
    foreach (var t in rr.Trechos)
    {
        Verdade(t.Diametro >= 0.15 - 1e-9, "DN mínimo 150");
        Verdade(t.LaminaRelativa <= 0.75 + 1e-6, $"{t.Nome}: y/D {t.LaminaRelativa:0.00}");
        double qi = Math.Max(t.VazaoInicialLs, 1.5);
        Verdade(t.Declividade >= 0.0055 * Math.Pow(qi, -0.47) - 1e-9, $"{t.Nome}: Imín");
    }
    double qTot = rr.Trechos.Where(t => t.Jusante == esgoto.Nos[5].Nome).Sum(t => t.VazaoFinalLs);
    double esperado = esgoto.Esgoto.VazaoDomesticaFinal() + esgoto.Esgoto.TaxaInfiltracao / 1000 * esgoto.Trechos.Sum(esgoto.ComprimentoHorizontal);
    Igual(esperado, qTot, 1e-6, "vazão final no exutório");
});
Teste("projeção populacional", () =>
{
    var a = ProjecaoPopulacional.Aritmetico(2000, 10000, 2020, 14000, 2040);
    Igual(18000, a.Populacao, 1e-9, "aritmético");
    var g = ProjecaoPopulacional.Geometrico(2000, 10000, 2020, 14000, 2040);
    Igual(14000.0 * 14000 / 10000, g.Populacao, 1e-6, "geométrico");
    var l = ProjecaoPopulacional.Logistico(2000, 10000, 2010, 13000, 2020, 15000, 2040);
    Verdade(l.Valido && l.Populacao > 15000 && l.Populacao < ProjecaoPopulacional.Saturacao(10000, 13000, 15000), "logístico entre P2 e Ps");
    Igual(15000, ProjecaoPopulacional.Logistico(2000, 10000, 2010, 13000, 2020, 15000, 2020).Populacao, 1e-6, "logístico passa por P2");
    var d = ProjecaoPopulacional.TaxaDecrescente(2000, 10000, 2010, 13000, 2020, 15000, 2020);
    Igual(15000, d.Populacao, 1e-6, "taxa decrescente passa por P2");
});

Console.WriteLine("== Escavação, tabelas e relatórios");
Teste("volumes de vala", () =>
{
    var q = CalculoVala.Calcular(resD, new ParametrosVala());
    Verdade(q.Count == resD.Trechos.Count, "um quantitativo por trecho");
    foreach (var x in q)
    {
        Verdade(x.Escavacao > 0 && x.Reaterro >= 0, x.Trecho);
        Igual(x.Escavacao, x.PorFaixa.Sum(), 1e-6, $"{x.Trecho}: soma das faixas");
    }
});
Teste("tabelas e relatório HTML", () =>
{
    var t = GeradorTabelas.Dimensionamento(drenagem, resD);
    Verdade(t.Linhas.Count == 7 && t.Linhas.All(l => l.Length == t.Colunas.Count), "linhas x colunas");
    var html = RelatorioHtml.Documento("Teste", new[] { t.ParaHtml(), GeradorTabelas.NotaServicoNos(drenagem, resD).ParaHtml(), GeradorTabelas.ResumoMateriais(drenagem, resD).ParaHtml() },
        resD.Mensagens, RelatorioHtml.ParametrosRede(drenagem, projeto));
    File.WriteAllText(Path.Combine(saida, "relatorio_drenagem.html"), html);
    File.WriteAllText(Path.Combine(saida, "planilha_drenagem.csv"), t.ParaCsv());
    Verdade(html.Contains("<table>") && html.Contains("Belo Horizonte"), "conteúdo do HTML");
});
Teste("CSV: exportar e importar de volta", () =>
{
    var csv = RedeCsv.Exportar(drenagem);
    var r2 = new Rede { Nome = "Importada" };
    var m = RedeCsv.Importar(csv, r2);
    Verdade(!m.Any(x => x.Gravidade == Gravidade.Erro), string.Join("; ", m));
    Verdade(r2.Nos.Count == drenagem.Nos.Count && r2.Trechos.Count == drenagem.Trechos.Count && r2.Bacias.Count == drenagem.Bacias.Count, "contagens");
    var r3 = new Rede { Nome = "Modelo" };
    var m3 = RedeCsv.Importar(RedeCsv.Modelo(), r3);
    Verdade(!m3.Any(x => x.Gravidade == Gravidade.Erro) && r3.Trechos.Count == 4, "modelo de importação");
    var rr = DimensionadorGravidade.Calcular(r3, projeto);
    Verdade(!rr.TemErros, "modelo dimensionável: " + string.Join("; ", rr.Trechos.SelectMany(x => x.Erros)));
    File.WriteAllText(Path.Combine(saida, "modelo_importacao.csv"), RedeCsv.Modelo());
});
Teste("projeto serializa/desserializa (JSON)", () =>
{
    var json = JsonProjeta.Serializar(projeto);
    var p2 = JsonProjeta.Desserializar<Projeto>(json)!;
    Verdade(p2.Redes.Count == projeto.Redes.Count && p2.Redes[0].Trechos.Count == 7, "contagens");
    Verdade(p2.Redes[0].Resultado!.Trechos.Count == 7, "resultado preservado");
    Verdade(double.IsNaN(new No().CotaTerreno), "NaN padrão");
    var p3 = JsonProjeta.Desserializar<Projeto>(JsonProjeta.Serializar(new Projeto { Redes = { new Rede { Nos = { new No() } } } }))!;
    Verdade(double.IsNaN(p3.Redes[0].Nos[0].CotaTerreno), "NaN preservado no JSON");
});

Console.WriteLine("== Modelador e biblioteca");
var bib = Biblioteca.Carregar(Path.Combine(saida, "biblioteca_vazia"));
Teste("biblioteca padrão carrega e valida", () =>
{
    Verdade(bib.Dispositivos.Count >= 6 && bib.Secoes.Count >= 6, $"{bib.Dispositivos.Count} dispositivos / {bib.Secoes.Count} seções");
    var m = bib.Validar().Where(x => x.Gravidade == Gravidade.Erro).ToList();
    Verdade(m.Count == 0, string.Join(" | ", m));
});
Teste("biblioteca de exemplo do usuário (revolução e arcos)", () =>
{
    var dir = AppContext.BaseDirectory;
    string? raiz = dir;
    while (raiz is not null && !Directory.Exists(Path.Combine(raiz, "biblioteca", "dispositivos"))) raiz = Path.GetDirectoryName(raiz);
    Verdade(raiz is not null, "pasta biblioteca/ não encontrada");
    var b2 = Biblioteca.Carregar(Path.Combine(raiz!, "biblioteca"));
    Verdade(b2.Mensagens.Count == 0, string.Join("; ", b2.Mensagens));
    var m = AvaliadorDispositivo.Avaliar(b2.Dispositivos["PV_CONICO"], new Dictionary<string, double> { ["Profundidade"] = 3.0, ["DiametroMaximo"] = 0.6 });
    Verdade(m.Ok && m.Saida.Count == 1, string.Join("; ", m.Mensagens));
    double v = m.Saida[0].VolumeEstimado();
    Verdade(v > 0.5 && v < 5, $"volume de revolução {v:0.000} m³");
    var s = AvaliadorDispositivo.AvaliarSecao(b2.Secoes["MEIA_CANA_COM_ABAS"]);
    Verdade(s.Ok && s.AreaConcreto > 0.05 && s.Hidraulica!.Area(0.2) > 0, $"meia-cana com abas {s.AreaConcreto:0.000}");
    Verdade(b2.Validar().All(x => x.Gravidade != Gravidade.Erro), "validação geral");
});
Teste("PV circular: chaminé condicional e volumes", () =>
{
    var raso = AvaliadorDispositivo.Avaliar(bib.Dispositivos["PV_CIRCULAR"], new Dictionary<string, double> { ["Profundidade"] = 1.4, ["DiametroMaximo"] = 0.4 });
    var fundo = AvaliadorDispositivo.Avaliar(bib.Dispositivos["PV_CIRCULAR"], new Dictionary<string, double> { ["Profundidade"] = 3.5, ["DiametroMaximo"] = 0.8 });
    Verdade(raso.Ok && fundo.Ok, string.Join(";", raso.Mensagens.Concat(fundo.Mensagens)));
    Verdade(!raso.Solidos.ContainsKey("chamine") && fundo.Solidos.ContainsKey("chamine"), "chaminé só no PV profundo");
    Igual(1.2, fundo.Valores["DiametroInterno"], 1e-12, "Di automático para Ø800");
    Verdade(fundo.Saida[0].VolumeEstimado() > raso.Saida[0].VolumeEstimado(), "PV profundo tem mais concreto");
    Verdade(fundo.Quantitativos.Any(q => q.Nome.StartsWith("Concreto") && q.Valor > 0.5), "quantitativo de concreto");
});
Teste("nomes que só diferem por maiúsculas são acusados", () =>
{
    var t = new SecaoTipo { Id = "X", Parametros = { new ParametroDef { Nome = "h", Padrao = 0.1 }, new ParametroDef { Nome = "H", Padrao = 0.5 } },
        Concreto = { new PontoExpr { X = "0", Y = "0" }, new PontoExpr { X = "1", Y = "0" }, new PontoExpr { X = "1", Y = "h" } } };
    Verdade(!AvaliadorDispositivo.AvaliarSecao(t).Ok, "deveria acusar h/H");
    var laco = new List<P2> { new(0, 0), new(1, 1), new(1, 0), new(0, 1) };
    Verdade(Geo.AutoIntersecta(laco), "polígono em laço");
});
Teste("valor informado prevalece sobre o padrão", () =>
{
    var m = AvaliadorDispositivo.Avaliar(bib.Dispositivos["PV_CIRCULAR"], new Dictionary<string, double> { ["DiametroInterno"] = 2.0 });
    Igual(2.0, m.Valores["DiametroInterno"], 0, "Di");
    Igual(2.2, m.Valores["De"], 1e-12, "De");
});
Teste("seções-tipo: áreas de concreto coerentes", () =>
{
    var s = AvaliadorDispositivo.AvaliarSecao(bib.Secoes["CANALETA_RETANGULAR"], new Dictionary<string, double> { ["b"] = 0.4, ["h"] = 0.4, ["e"] = 0.1 });
    Igual(0.6 * 0.5 - 0.4 * 0.4, s.AreaConcreto, 1e-9, "U retangular");
    var tz = AvaliadorDispositivo.AvaliarSecao(bib.Secoes["VALETA_TRAPEZOIDAL"]);
    Verdade(tz.Ok && tz.AreaConcreto > 0.05 && tz.AreaConcreto < 0.2, $"trapezoidal {tz.AreaConcreto:0.000}");
    var tri = AvaliadorDispositivo.AvaliarSecao(bib.Secoes["VALETA_TRIANGULAR"]);
    // revestimento ≈ perímetro molhado × e (aproximação de primeira ordem)
    Igual(tri.PerimetroRevestido * 0.08, tri.AreaConcreto, 0.02, "triangular ≈ perímetro × e");
    var mc = AvaliadorDispositivo.AvaliarSecao(bib.Secoes["MEIA_CANA"]);
    Igual(Math.PI / 2 * (0.25 * 0.25 - 0.2 * 0.2), mc.AreaConcreto, 0.002, "meia-cana");
    var mfc = AvaliadorDispositivo.AvaliarSecao(bib.Secoes["MEIO_FIO_SARJETA"]);
    Verdade(mfc.Ok && mfc.Hidraulica is not null && mfc.Hidraulica.Area(0.1) > 0, "MFC hidráulica: " + string.Join("; ", mfc.Mensagens));
    Igual(0.15 * 0.45 + 0.30 * 0.10, mfc.AreaConcreto, 1e-9, "MFC: guia + sarjeta");
    Verdade(!Geo.AutoIntersecta(mfc.Concreto), "MFC sem autointerseção");
    foreach (var sx in bib.Secoes.Values) Verdade(!Geo.AutoIntersecta(AvaliadorDispositivo.AvaliarSecao(sx).Concreto), $"{sx.Id} se cruza");
});
Teste("longitudinal: quantitativos e comprimento crítico", () =>
{
    var d = new DispositivoLongitudinal { SecaoId = "MEIO_FIO_SARJETA", LarguraContribuicao = 8, C = 0.9 };
    var sec = AvaliadorDispositivo.AvaliarSecao(bib.Secoes[d.SecaoId]);
    var am = Enumerable.Range(0, 21).Select(k => new AmostraLongitudinal(k * 10, k * 10, 0, 850 - k * 10 * 0.02, 1, 0)).ToList();
    var r = CalculoLongitudinal.Calcular(d, sec, am, eqBH);
    Igual(200, r.ComprimentoHorizontal, 1e-9, "comprimento");
    Igual(0.02, r.DeclividadeMinima, 1e-9, "declividade");
    Verdade(r.CapacidadeMinima > 0 && r.ComprimentoCriticoMinimo > 0, "capacidade/comprimento crítico");
    Igual(sec.AreaConcreto * r.Comprimento3D, r.VolumeConcreto, 1e-9, "volume");
    var p = CalculoLongitudinal.PontoSecao(am[0], new P2(1, 0.5), false);
    Igual(-1, p.Y, 1e-12, "lado direito de um eixo para +X é -Y");
});

Console.WriteLine("== SWMM / EPANET");
string? runSwmm = Environment.GetEnvironmentVariable("PROJETA_RUNSWMM");
string? runEpanet = Environment.GetEnvironmentVariable("PROJETA_RUNEPANET");
Teste("INP do SWMM (drenagem) gerado e simulado", () =>
{
    var inp = SwmmInp.Gerar(drenagem, projeto, new OpcoesSwmm(), out var ids);
    var arq = Path.Combine(saida, "drenagem.inp");
    File.WriteAllText(arq, inp);
    Verdade(inp.Contains("[CONDUITS]") && inp.Contains("CHUVA_PROJETO"), "seções");
    if (runSwmm is null) { Console.WriteLine("       (PROJETA_RUNSWMM não definido — simulação pulada)"); return; }
    var rpt = Path.ChangeExtension(arq, ".rpt");
    var ex = MotorExterno.Executar(runSwmm, arq, rpt, Path.ChangeExtension(arq, ".out"));
    var r = ResultadoSwmm.Ler(File.ReadAllText(rpt));
    Verdade(r.Erros.Count == 0, "erros do SWMM: " + string.Join(" | ", r.Erros));
    Verdade(r.Links.Count == 7 && r.Nos.Count == 8, $"resultados: {r.Links.Count} links, {r.Nos.Count} nós");
    Verdade(Math.Abs(r.ErroContinuidadeRoteamento) < 5, $"continuidade {r.ErroContinuidadeRoteamento}");
    var ult = r.Links.First(l => l.Nome == ids["T" + drenagem.Trechos.First(t => drenagem.NoPorId(t.NoJusanteId)!.Nome == "E").Id]);
    Console.WriteLine($"       SWMM: vazão máx. no último trecho {ult.VazaoMax:0.000} m³/s (racional: {resD.Trechos.Last().Vazao:0.000}); alagamentos: {r.Alagamentos.Count}");
    Verdade(ult.VazaoMax > 0, "vazão no exutório");
});
Teste("INP do SWMM (esgoto) gerado e simulado", () =>
{
    var arq = Path.Combine(saida, "esgoto.inp");
    File.WriteAllText(arq, SwmmInp.Gerar(esgoto, projeto, new OpcoesSwmm { Roteamento = "DYNWAVE" }));
    if (runSwmm is null) return;
    var rpt = Path.ChangeExtension(arq, ".rpt");
    MotorExterno.Executar(runSwmm, arq, rpt);
    var r = ResultadoSwmm.Ler(File.ReadAllText(rpt));
    Verdade(r.Erros.Count == 0, "erros: " + string.Join(" | ", r.Erros));
    Verdade(r.Links.Count == esgoto.Trechos.Count, "links");
});
Teste("SWMM: importar o INP gerado", () =>
{
    var r2 = new Rede { Nome = "Importada SWMM" };
    var m = SwmmInp.Importar(File.ReadAllText(Path.Combine(saida, "drenagem.inp")), r2);
    Verdade(!m.Any(x => x.Gravidade == Gravidade.Erro), string.Join("; ", m));
    Verdade(r2.Nos.Count == 8 && r2.Trechos.Count == 7, "contagens");
    var t = r2.Trechos.First();
    Verdade(t.CotaMontanteForcada.HasValue && t.DiametroForcado > 0, "cotas e diâmetros");
});
Rede agua = null!;
Teste("EPANET: rede de água exportada e simulada", () =>
{
    agua = new Rede { Nome = "Agua Teste", Tipo = TipoRede.Agua };
    var res = agua.NovoNo(TipoNo.Reservatorio, 0, 0); res.Nome = "RES"; res.CotaTerreno = 900; res.Carga = 905;
    var nos = new List<No>();
    for (int i = 0; i < 4; i++) for (int j = 0; j < 3; j++)
    { var n = agua.NovoNo(TipoNo.Juncao, 100 + i * 80, j * 80); n.CotaTerreno = 870 - i * 2 - j; nos.Add(n); }
    agua.NovoTrecho(res, nos[0]);
    for (int i = 0; i < 4; i++) for (int j = 0; j < 3; j++)
    {
        var n = nos[i * 3 + j];
        if (i < 3) agua.NovoTrecho(n, nos[(i + 1) * 3 + j]);
        if (j < 2) agua.NovoTrecho(n, nos[i * 3 + j + 1]);
    }
    agua.Agua.PopulacaoFinal = 6000;
    agua.Trechos[0].DiametroForcado = 0.20;
    var arq = Path.Combine(saida, "agua.inp");
    var inp = EpanetInp.Gerar(agua, out var ids);
    File.WriteAllText(arq, inp);
    Igual(agua.Agua.DemandaTotal(), EpanetInp.Demandas(agua).Values.Sum(), 1e-9, "demanda distribuída = total");
    if (runEpanet is null) { Console.WriteLine("       (PROJETA_RUNEPANET não definido — simulação pulada)"); return; }
    ResultadoEpanet r = null!;
    for (int it = 0; it < 6; it++)
    {
        var rpt = Path.ChangeExtension(arq, ".rpt");
        File.WriteAllText(arq, EpanetInp.Gerar(agua, out ids));
        MotorExterno.Executar(runEpanet, arq, rpt);
        r = ResultadoEpanet.Ler(File.ReadAllText(rpt));
        Verdade(r.Erros.Count == 0, "erros: " + string.Join(" | ", r.Erros));
        if (r.AjustarDiametros(agua, ids) == 0) break;
    }
    Verdade(r.Nos.Count == 13 && r.Links.Count == agua.Trechos.Count, $"resultados {r.Nos.Count} nós / {r.Links.Count} tubos");
    Verdade(r.Links.All(l => Math.Abs(l.Velocidade) <= agua.Agua.VelocidadeMaxima + 1e-6), "velocidades após ajuste");
    double pmin = r.Nos.Where(n => n.Nome != "RES").Min(n => n.Pressao);
    Console.WriteLine($"       EPANET: pressão mínima {pmin:0.0} mca; vazão saindo do reservatório {Math.Abs(r.Nos.First(n => n.Nome == "RES").Demanda):0.00} L/s (demanda total {agua.Agua.DemandaTotal():0.00})");
    Igual(agua.Agua.DemandaTotal(), Math.Abs(r.Nos.First(n => n.Nome == "RES").Demanda), 0.05, "balanço de massa");
});
Teste("EPANET: importar o INP gerado", () =>
{
    var r2 = new Rede { Nome = "Importada EPANET", Tipo = TipoRede.Agua };
    var m = EpanetInp.Importar(File.ReadAllText(Path.Combine(saida, "agua.inp")), r2);
    Verdade(!m.Any(x => x.Gravidade == Gravidade.Erro), string.Join("; ", m));
    Verdade(r2.Nos.Count == agua.Nos.Count && r2.Trechos.Count == agua.Trechos.Count, "contagens");
    Igual(agua.Agua.DemandaTotal(), r2.Nos.Sum(n => n.Demanda), 1e-3, "demandas");
});

Console.WriteLine();
Console.WriteLine($"Resultado: {ok} ok, {falhas} falha(s). Arquivos em {saida}");
if (falhas > 0) { File.WriteAllLines(Path.Combine(saida, "falhas.log"), log); Environment.Exit(1); }
