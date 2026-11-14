using Projeta.Core.Alinhamentos;
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
using NS = Projeta.Core.Pavimentacao;

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

Console.WriteLine("== Eixo: apagar PI");
// monta os elementos de um eixo a partir de PIs e curvas (como o Civil 3D desenharia)
List<ElementoEixo> MontarEixo(P2[] pis, CurvaPI?[] curvas)
{
    var t = new List<ElementoEixo>();
    var cur = pis[0];
    for (int k = 1; k < pis.Length - 1; k++)
    {
        var uin = (pis[k] - pis[k - 1]).Unitario();
        var uout = (pis[k + 1] - pis[k]).Unitario();
        double d = Math.Atan2(uin.Cross(uout), uin.Dot(uout));
        if (curvas[k] is not { } c) { t.Add(new(TipoElementoEixo.Reta, cur, pis[k], cur.Distancia(pis[k]))); cur = pis[k]; continue; }
        double T = c.Tangente(d);
        var pc = pis[k] - uin * T;
        var pt = pis[k] + uout * T;
        t.Add(new(TipoElementoEixo.Reta, cur, pc, cur.Distancia(pc)));
        if (c.Tipo == TipoCurvaPI.Arco)
            t.Add(new(TipoElementoEixo.Arco, pc, pt, c.Raio * Math.Abs(d), c.Raio));
        else
        {   // espirais: só os pontos de contato com as tangentes importam para achar o PI
            var m1 = pc + (pt - pc) * 0.3;
            var m2 = pc + (pt - pc) * 0.7;
            t.Add(new(TipoElementoEixo.Espiral, pc, m1, c.L1, 0, c.TipoEspiral));
            t.Add(new(TipoElementoEixo.Arco, m1, m2, 1, c.Raio));
            t.Add(new(TipoElementoEixo.Espiral, m2, pt, c.L2, 0, c.TipoEspiral));
        }
        cur = pt;
    }
    t.Add(new(TipoElementoEixo.Reta, cur, pis[^1], cur.Distancia(pis[^1])));
    return t;
}
bool CurvasCabem(PlanoPIs p)
{
    for (int s = 0; s < p.PIs.Count - 1; s++)
        if (p.Tangente(s) + p.Tangente(s + 1) > p.PIs[s].Distancia(p.PIs[s + 1]) + 1e-6) return false;
    return true;
}
bool Junto(P2 a, P2 b, double tol = 1e-6) => a.Distancia(b) < tol;

Teste("eixo só com retas: PIs = vértices e apagar o do meio", () =>
{
    var pis = new P2[] { new(0, 0), new(100, 0), new(150, 50), new(250, 50), new(300, 0) };
    var pl = EixoPIs.Extrair(MontarEixo(pis, new CurvaPI?[5]));
    Verdade(pl.PIs.Count == 5 && pis.Zip(pl.PIs).All(z => Junto(z.First, z.Second)), "PIs = vértices");
    var novo = EixoPIs.Apagar(pl, 2, new List<string>());
    Verdade(novo.PIs.Count == 4 && Junto(novo.PIs[1], new(100, 0)) && Junto(novo.PIs[2], new(250, 50)), "PI 2 apagado");
});
Teste("eixo com arcos: PIs e raios recuperados; vizinhos que cabem mantêm o raio", () =>
{
    var pis = new P2[] { new(0, 0), new(200, 0), new(300, 80), new(450, 60), new(600, 150) };
    var cs = new CurvaPI?[] { null, new(TipoCurvaPI.Arco, 80), new(TipoCurvaPI.Arco, 60), new(TipoCurvaPI.Arco, 90), null };
    var pl = EixoPIs.Extrair(MontarEixo(pis, cs));
    Verdade(pis.Zip(pl.PIs).All(z => Junto(z.First, z.Second)), "PIs recuperados");
    Verdade(pl.Curvas.Zip(cs).All(z => Math.Abs((z.First?.Raio ?? 0) - (z.Second?.Raio ?? 0)) < 1e-9), "raios recuperados");
    var novo = EixoPIs.Apagar(pl, 2, new List<string>());
    Verdade(novo.PIs.Count == 4 && Junto(novo.PIs[1], new(200, 0)) && Junto(novo.PIs[2], new(450, 60)), "PI 2 apagado");
    Verdade(novo.Curvas[1]!.Raio == 80 && novo.Curvas[2]!.Raio == 90 && CurvasCabem(novo), "vizinhos com R 80 e 90 cabem");
});
Teste("apagar PI: curva vizinha que não cabe é reduzida, a que não é vizinha fica", () =>
{
    var pl = new PlanoPIs();
    pl.PIs.AddRange(new P2[] { new(0, 0), new(100, 0), new(150, 20), new(200, 80), new(300, 80), new(400, 0) });
    pl.Curvas.AddRange(new CurvaPI?[] { null, new(TipoCurvaPI.Arco, 250), new(TipoCurvaPI.Arco, 250), new(TipoCurvaPI.Arco, 250), new(TipoCurvaPI.Arco, 50), null });
    var rel = new List<string>();
    var novo = EixoPIs.Apagar(pl, 2, rel);
    Verdade(CurvasCabem(novo), "todas as curvas cabem");
    Verdade(novo.Curvas[1]!.Raio < 250 && novo.Curvas[2]!.Raio < 250, "vizinhos reduzidos");
    Verdade(novo.Curvas[3]!.Raio == 50, "PI que não é vizinho manteve R 50");
    Verdade(rel.Count(l => l.Contains("não cabia")) == 2, "as duas mudanças relatadas");
});
Teste("apagar PI: vizinhos que ficam alinhados saem também", () =>
{
    var pis = new P2[] { new(0, 0), new(200, 0), new(260, 40), new(320, 0), new(520, 0) };
    var cs = new CurvaPI?[] { null, new(TipoCurvaPI.Arco, 60), new(TipoCurvaPI.Arco, 60), new(TipoCurvaPI.Arco, 60), null };
    var rel = new List<string>();
    var novo = EixoPIs.Apagar(EixoPIs.Extrair(MontarEixo(pis, cs)), 2, rel);
    Verdade(novo.PIs.Count == 2 && Junto(novo.PIs[0], new(0, 0)) && Junto(novo.PIs[1], new(520, 0)), "sobrou uma reta só");
    Verdade(rel.Count(l => l.Contains("alinhado")) == 2, "os dois PIs extras relatados");
});
Teste("eixo: espiral-curva-espiral reconhecida no PI certo", () =>
{
    var pis = new P2[] { new(0, 0), new(300, 0), new(500, 150), new(800, 150) };
    var cs = new CurvaPI?[] { null, new(TipoCurvaPI.EspiralCurvaEspiral, 150, 40, 40, 0), new(TipoCurvaPI.Arco, 120), null };
    var pl = EixoPIs.Extrair(MontarEixo(pis, cs));
    Verdade(pl.Curvas[1] is { Tipo: TipoCurvaPI.EspiralCurvaEspiral, Raio: 150, L1: 40, L2: 40 } && Junto(pl.PIs[1], pis[1]), "SCS no PI 1");
});
Teste("eixo de polilinha: vértice no meio da reta não vira PI", () =>
{
    var t = new List<ElementoEixo>
    {
        new(TipoElementoEixo.Reta, new(0, 0), new(50, 0), 50),
        new(TipoElementoEixo.Reta, new(50, 0), new(100, 0), 50),
        new(TipoElementoEixo.Reta, new(100, 0), new(150, 50), 70.71),
    };
    var pl = EixoPIs.Extrair(t);
    Verdade(pl.PIs.Count == 3 && Junto(pl.PIs[1], new(100, 0)), "um PI só");
});
Teste("eixo que começa em curva é recusado com mensagem", () =>
{
    var t = new List<ElementoEixo> { new(TipoElementoEixo.Arco, new(0, 0), new(10, 1), 10, 50), new(TipoElementoEixo.Reta, new(10, 1), new(100, 10), 90) };
    try { EixoPIs.Extrair(t); throw new Exception("devia recusar"); }
    catch (InvalidOperationException ex) { Verdade(ex.Message.Contains("começa"), "mensagem"); }
});
Teste("apagar PI: clique escolhe o PI interno mais próximo", () =>
{
    var pl = new PlanoPIs();
    pl.PIs.AddRange(new P2[] { new(0, 0), new(100, 0), new(200, 50), new(300, 0) });
    pl.Curvas.AddRange(new CurvaPI?[4]);
    Verdade(pl.MaisProximo(new(190, 45)) == 2 && pl.MaisProximo(new(-5, 0)) == 1, "PI mais próximo");
});

Console.WriteLine("== Greide: apagar PIV");
CurvaVertical? Parab(double l) => CurvaVertical.Parabola(l);
PlanoGreide Greide(params (double Est, double Cota, CurvaVertical? Curva)[] pivs)
{
    var g = new PlanoGreide();
    foreach (var (e, c, cv) in pivs) g.Adicionar(e, c, cv);
    return g;
}
bool CurvasVerticaisCabem(PlanoGreide g)
{
    for (int s = 0; s < g.Count - 1; s++)
        if (g.Depois(s) + g.Antes(s + 1) > g.Estacas[s + 1] - g.Estacas[s] + 1e-6) return false;
    return true;
}
Teste("greide sem curvas: apagar o PIV liga os vizinhos por uma rampa", () =>
{
    var g = Greide((0, 100, null), (100, 102, null), (200, 101, null), (300, 103, null));
    var novo = GreidePVIs.Apagar(g, 2, new List<string>());
    Verdade(novo.Count == 3 && novo.Estacas[1] == 100 && novo.Estacas[2] == 300, "PIVs restantes");
    Igual(0.005, novo.Rampa(1), 1e-12, "rampa nova 0,5%");
});
Teste("greide: parábolas vizinhas que cabem mantêm o comprimento", () =>
{
    var g = Greide((0, 100, null), (200, 104, Parab(80)), (300, 101, Parab(60)), (500, 105, Parab(100)), (700, 100, null));
    var novo = GreidePVIs.Apagar(g, 2, new List<string>());
    Verdade(novo.Count == 4 && novo.Curvas[1]!.L1 + novo.Curvas[1]!.L2 == 80 && novo.Curvas[2]!.L1 + novo.Curvas[2]!.L2 == 100, "L 80 e 100 mantidos");
    Verdade(CurvasVerticaisCabem(novo), "cabem");
});
Teste("greide: curva vizinha que não cabe é reduzida, a que não é vizinha fica", () =>
{
    var g = Greide((0, 100, null), (100, 103, Parab(120)), (150, 101, Parab(100)), (200, 104, Parab(120)), (300, 100, Parab(60)), (400, 103, null));
    var rel = new List<string>();
    var novo = GreidePVIs.Apagar(g, 2, rel);
    Verdade(CurvasVerticaisCabem(novo), "todas cabem");
    Verdade(novo.Curvas[1]!.L1 + novo.Curvas[1]!.L2 < 120 && novo.Curvas[2]!.L1 + novo.Curvas[2]!.L2 < 120, "vizinhas reduzidas");
    Verdade(novo.Curvas[3]!.L1 + novo.Curvas[3]!.L2 == 60, "a não vizinha manteve L 60");
    Verdade(rel.Count(l => l.Contains("não cabia")) == 2, "as duas mudanças relatadas");
});
Teste("greide: PIV que fica na mesma rampa sai também", () =>
{
    var g = Greide((0, 100, null), (100, 101, Parab(40)), (150, 103, Parab(40)), (200, 102, Parab(40)), (300, 103, null));
    var rel = new List<string>();
    var novo = GreidePVIs.Apagar(g, 2, rel);
    Verdade(novo.Count == 2 && novo.Estacas[0] == 0 && novo.Estacas[1] == 300, "sobrou uma rampa só");
    Verdade(rel.Count(l => l.Contains("mesma rampa")) == 2, "os dois PIVs extras relatados");
});
Teste("greide: curva circular e assimétrica ocupam o trecho certo", () =>
{
    var c = CurvaVertical.Circular(1000);
    Igual(1000 * Math.Tan((Math.Atan(0.02) + Math.Atan(0.03)) / 2), c.Antes(0.02, -0.03), 1e-9, "T da circular");
    var a = CurvaVertical.Assimetrica(30, 50);
    Verdade(a.Antes(0.01, -0.01) == 30 && a.Depois(0.01, -0.01) == 50, "L1 antes, L2 depois");
});
Teste("greide com PIVs fora de ordem é recusado", () =>
{
    var g = Greide((0, 100, null), (100, 101, null), (100, 102, null), (200, 100, null));
    try { GreidePVIs.Apagar(g, 1, new List<string>()); throw new Exception("devia recusar"); }
    catch (InvalidOperationException) { }
});

Console.WriteLine("== Nota de serviço (pavimentação)");
Teste("estaca 20 m com arredondamento", () =>
{
    Verdade(NS.NotaServico.FormatarEstaca(24.775) == "1+4,775", NS.NotaServico.FormatarEstaca(24.775));
    Verdade(NS.NotaServico.FormatarEstaca(39.9996) == "2+0,000", "39,9996 deve virar 2+0,000: " + NS.NotaServico.FormatarEstaca(39.9996));
    Verdade(NS.NotaServico.FormatarEstaca(230.17) == "11+10,170", NS.NotaServico.FormatarEstaca(230.17));
    Verdade(NS.NotaServico.FormatarEstaca(0) == "0+0,000", "zero");
});
Teste("pontos notáveis horizontais", () =>
{
    var t = new List<NS.TrechoEixo>
    {
        new(NS.TipoTrechoEixo.Reta, 0, 24.775), new(NS.TipoTrechoEixo.Curva, 24.775, 35.141, true),
        new(NS.TipoTrechoEixo.Reta, 35.141, 54.913), new(NS.TipoTrechoEixo.Curva, 54.913, 65.373, true),
        new(NS.TipoTrechoEixo.Curva, 65.373, 80, false), new(NS.TipoTrechoEixo.Espiral, 80, 90),
        new(NS.TipoTrechoEixo.Curva, 90, 100, false), new(NS.TipoTrechoEixo.Espiral, 100, 110), new(NS.TipoTrechoEixo.Reta, 110, 150),
    };
    var r = string.Join(" ", NS.NotaServico.PontosNotaveisHorizontais(t).Select(p => $"{p.Rotulo}@{p.Estaca:0.###}"));
    Verdade(r == "PC@24.775 PT@35.141 PC@54.913 PRC@65.373 CS@80 SC@90 CS@100 ST@110", r);
});
Teste("estacas: grade de 10 m + notáveis + regiões", () =>
{
    var e = NS.NotaServico.Estacas(20, 230.17, 10,
        new[] { new NS.PontoNotavel(24.775, "PC"), new NS.PontoNotavel(35.141, "PT") },
        new[] { new NS.PontoNotavel(24.775, "BVC"), new NS.PontoNotavel(40.0002, "EVC") },
        new[] { 29.958, 109.99, 110.0, 20.0 });
    var txt = string.Join(" ", e.Take(7).Select(x => $"{NS.NotaServico.FormatarEstaca(x.Estaca)}{(x.H + x.V).Length switch { 0 => "", _ => "[" + x.H + "|" + x.V + "]" }}"));
    Verdade(txt == "1+0,000 1+4,775[PC|BVC] 1+9,958 1+10,000 1+15,141[PT|] 2+0,000[|EVC] 2+10,000", txt);
    Verdade(e.Any(x => Math.Abs(x.Estaca - 109.99) < 1e-9) && e.Any(x => Math.Abs(x.Estaca - 110) < 1e-9), "5+9,990 e 5+10,000 ficam separadas");
    Verdade(Math.Abs(e[^1].Estaca - 230.17) < 1e-9 && e.Count == 27, $"fim e quantidade ({e.Count})");
});
Teste("inclinações da seção (lado direito do modelo E-NS-04-PD)", () =>
{
    var lado = new NS.PontoSecao?[] { new(9.5, 955.625), new(10.0, 955.550), new(10.121, 955.730), new(12.374, 955.809) };
    Igual(-3.0, NS.NotaServico.Inclinacao(lado, 0, 955.910), 0.01, "bordo");
    Igual(-15.0, NS.NotaServico.Inclinacao(lado, 1, 955.910), 0.01, "drenagem");
    Igual(3.506, NS.NotaServico.Inclinacao(lado, 3, 955.910), 0.01, "passeio a partir do meio-fio");
    var semBordo = new NS.PontoSecao?[] { null, new(-0.121, 955.752) };
    Igual(0.18 / 0.121 * 100, NS.NotaServico.Inclinacao(semBordo, 1, 955.572), 1e-6, "sem o anterior, mede do eixo");
});
Teste("interpolação acompanha o greide", () =>
{
    // seção constante (bordo 3,5 m a -2%) e greide em curva vertical: a cota do bordo segue o greide exato
    var p = NS.NotaServico.Interpolar(15, 10, new NS.PontoSecao(3.5, 99.93), 100, 20, new NS.PontoSecao(3.5, 100.93), 101, 100.6);
    Igual(3.5, p!.Value.Afastamento, 1e-12, "afastamento");
    Igual(100.53, p.Value.Cota, 1e-9, "cota = greide − 0,07");
    Verdade(NS.NotaServico.Interpolar(15, 10, null, 100, 20, new NS.PontoSecao(1, 1), 101, 100.5) is null, "ausente de um lado → vazio");
});
Teste("planilha xlsx no layout E-NS", () =>
{
    var ns = new NS.NotaServico { Titulo = "E-NS-04-PD" };
    NS.PontoSecao? P(double a, double c) => new NS.PontoSecao(a, c);
    void L(double e, string h, string v, double proj, double terr, NS.PontoSecao?[] esq, NS.PontoSecao?[] dir) =>
        ns.Linhas.Add(new NS.LinhaNota { Estaca = e, NotavelH = h, NotavelV = v, CotaProjeto = proj, CotaTerreno = terr, Esquerdo = esq, Direito = dir });
    L(20, "", "", 955.572, 955.572, new[] { null, null, P(-0.121, 955.752), null }, new[] { P(9.866, 955.375), P(10.366, 955.300), P(10.487, 955.480), P(12.075, 955.520) });
    L(24.775, "PC", "BVC", 955.724, 955.753, new[] { null, null, P(-0.121, 955.904), null }, new[] { P(9.502, 955.534), P(10.002, 955.459), P(10.122, 955.639), P(12.289, 955.669) });
    L(29.958, "", "", 955.910, 955.945, new[] { P(-0.156, 955.914), null, P(-0.276, 956.094), null }, new[] { P(9.5, 955.625), P(10, 955.550), P(10.121, 955.730), P(12.374, 955.809) });
    L(30, "", "", 955.911, 955.947, new[] { P(-0.158, 955.916), null, P(-0.279, 956.096), null }, new[] { P(9.5, 955.626), P(10, 955.551), P(10.121, 955.731), P(12.374, 955.811) });
    L(35.141, "PT", "", 956.143, 956.131, new[] { P(-0.583, 956.160), null, P(-0.703, 956.340), null }, new[] { P(9.5, 955.858), P(10, 955.783), P(10.121, 955.963), P(12.001, 956.000) });
    L(40, "", "EVC", 956.404, 956.305, new[] { P(-1.108, 956.437), null, P(-1.229, 956.617), null }, new[] { P(9.5, 956.119), P(10, 956.044), P(10.121, 956.224), P(14.472, 956.311) });
    L(90, "", "", 959.009, 958.630, new[] { P(-3.3, 959.108), null, P(-3.421, 959.288), null }, new[] { P(9.5, 958.724), null, null, null });
    L(120, "", "", 960.299, 960.207, new[] { P(-3.3, 960.398), null, P(-3.421, 960.578), null }, new[] { P(9.5, 960.014), P(10, 959.939), P(10.121, 960.119), null });
    ns.Linhas[6].Interpolada = true;
    ns.Origem.Add(("Corredor", "teste"));
    var (esq, dir) = ns.Colunas();
    Verdade(string.Join(",", esq) == "2,0" && string.Join(",", dir) == "0,1,2,3", $"colunas {string.Join(",", esq)} | {string.Join(",", dir)}");
    var pasta = ns.Planilha();
    var f = pasta.Folhas[0];
    Verdade(f.UltimaColuna == 22, $"22 colunas como o modelo (A..V), saiu {f.UltimaColuna}");
    Verdade((string?)f.Valor(2, 1) == "E-NS-04-PD - Estaca 1+0,000 a 6+0,000", "subtítulo: " + f.Valor(2, 1));
    Verdade((string?)f.Valor(6, 6) == "1+0,000" && (string?)f.Valor(7, 7) == "PC" && (string?)f.Valor(7, 8) == "BVC", "eixo");
    Verdade(f.Valor(6, 11) is double z && z == 0, "cota vermelha zero");
    Verdade(f.Valor(6, 3) is null && f.Valor(8, 3) is double a && a == -0.156, "bordo esquerdo vazio na 1ª linha e -0,156 na 3ª");
    Igual(3.0, (double)f.Valor(8, 5)!, 0.5, "inclinação esquerda ~3%");
    var arq = Path.Combine(saida, "nota_servico_teste.xlsx");
    pasta.Salvar(arq);
    using var zip = System.IO.Compression.ZipFile.OpenRead(arq);
    var nomes = zip.Entries.Select(x => x.FullName).ToHashSet();
    Verdade(nomes.SetEquals(new[] { "[Content_Types].xml", "_rels/.rels", "docProps/core.xml", "docProps/app.xml", "xl/workbook.xml",
        "xl/_rels/workbook.xml.rels", "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml", "xl/styles.xml" }), "partes do pacote");
    using var sr = new StreamReader(zip.GetEntry("xl/workbook.xml")!.Open());
    Verdade(sr.ReadToEnd().Contains("'Nota de Serviço'!$1:$5"), "títulos de impressão");
});

Teste("códigos com curinga e migração da configuração", () =>
{
    Verdade(NS.ConfigNotaServico.Casa("CORTE_16_OFFSET_DIREITO", "*OFFSET*"), "*OFFSET*");
    Verdade(NS.ConfigNotaServico.Casa("CorteDir33-10", "Corte*33-*") && !NS.ConfigNotaServico.Casa("CorteBanDir-10", "Corte*33-*"), "Corte*33-*");
    Verdade(NS.ConfigNotaServico.Casa("CorteBanEsq-8", "*Ban*") && NS.ConfigNotaServico.Casa("BERMA_8_CORTE_ESQUERDO", "*BERMA*"), "banquetas");
    Verdade(!NS.ConfigNotaServico.Casa("BORDO_BASE", "*Ban*") && NS.ConfigNotaServico.Casa("etw", "ETW"), "sem curinga = igual, sem maiúsculas");
    var c = new NS.ConfigNotaServico();
    c.Elementos[0].Codigos = new() { "ETW" };
    c.Elementos[2].Codigos = new() { "MeuMeioFio" };
    c.Migrar();
    Verdade(c.Elementos[0].Codigos.Contains("BORDO_PISTA"), "padrão antigo atualizado");
    Verdade(c.Elementos[2].Codigos.Count == 1, "lista do usuário mantida");
});
Teste("banquetas e offset na planilha (corte à direita, aterro à esquerda)", () =>
{
    var ns = new NS.NotaServico { Titulo = "TESTE" };
    NS.PontoSecao? P(double a, double c) => new NS.PontoSecao(a, c);
    var esq = new[] { P(-3.3, 475.616), P(-3.8, 475.541), P(-3.962, 475.721), null };
    var dir = new[] { P(3.3, 576.413), P(3.8, 576.338), P(3.962, 576.518), null };
    ns.Linhas.Add(new NS.LinhaNota
    {
        Estaca = 100, CotaProjeto = 475.715, CotaTerreno = 470, Esquerdo = esq, Direito = dir,
        BanquetasEsquerdo = new[] { new NS.PontoSecao(-15.962, 467.721), new NS.PontoSecao(-19.962, 468.121), new NS.PontoSecao(-31.962, 460.121), new NS.PontoSecao(-35.962, 460.521) },
        OffsetEsquerdo = P(-36.997, 459.831),
        BanquetasDireito = new[] { new NS.PontoSecao(13.962, 591.443), new NS.PontoSecao(17.962, 591.043) },
        OffsetDireito = P(20.5, 595.2),
    });
    ns.Linhas.Add(new NS.LinhaNota { Estaca = 110, CotaProjeto = 476, CotaTerreno = 476.2, Esquerdo = esq, Direito = dir, OffsetDireito = P(4.1, 576.7) });
    Verdade(NS.NotaServico.CorteAterro(ns.Linhas[0].OffsetEsquerdo!.Value, esq, 475.715) == "A", "esquerdo em aterro");
    Verdade(NS.NotaServico.CorteAterro(ns.Linhas[0].OffsetDireito!.Value, dir, 475.715) == "C", "direito em corte");
    var f = ns.Planilha().Folhas[0];
    // esquerdo: Offset(3) + 2 banquetas(8) + Topo MF(2) + Drenagem(3) + Bordo(3) = 19 ; eixo 6 ; direito: 8 + 1 banqueta(4) + Offset(3) = 15
    Verdade(f.UltimaColuna == 40, $"40 colunas, saiu {f.UltimaColuna}");
    Verdade((string?)f.Valor(4, 1) == "Offset" && (string?)f.Valor(4, 4) == "Banqueta 2" && (string?)f.Valor(4, 8) == "Banqueta 1", "esquerdo espelhado");
    Verdade(f.Valor(6, 1) is double oa && oa == -36.997 && (string?)f.Valor(6, 3) == "A", "offset esquerdo e C/A");
    Verdade(f.Valor(6, 4) is double b2 && b2 == -31.962 && f.Valor(6, 8) is double b1 && b1 == -15.962, "banquetas na ordem");
    Verdade((string?)f.Valor(4, 34) == "Banqueta 1" && (string?)f.Valor(4, 38) == "Offset" && (string?)f.Valor(6, 40) == "C", "direito");
    Verdade(f.Valor(7, 34) is null && f.Valor(7, 38) is double o2 && o2 == 4.1, "linha sem banqueta fica em branco");
    Verdade(f.Papel == 8, "A3 quando passa de 30 colunas");
    ns.Config.Terraplenagem = false;
    Verdade(ns.Planilha().Folhas[0].UltimaColuna == 22, "sem terraplenagem volta às 22 colunas");
});

Console.WriteLine("== Volumes entre superfícies");
Teste("faixas de altura, polígono e célula", () =>
{
    var fx = Projeta.Core.Superficies.Malha.Faixas();
    Verdade(fx.Count == 10 && fx[0].Corte && !fx[5].Corte, "5 faixas de corte + 5 de aterro");
    Verdade(Projeta.Core.Superficies.Malha.Indice(fx, -0.3) == 0 && Projeta.Core.Superficies.Malha.Indice(fx, -2.5) == 3 && Projeta.Core.Superficies.Malha.Indice(fx, 4) == 9, "índices");
    Verdade(Projeta.Core.Superficies.Malha.Indice(fx, 0.005) == -1, "neutro");
    Verdade(fx[4].Rotulo == "Corte acima de 3,00 m" && fx[6].Rotulo == "Aterro 0,50 a 1,00 m", fx[4].Rotulo + " | " + fx[6].Rotulo);
    var q = new List<(double, double)> { (0, 0), (10, 0), (10, 10), (0, 10) };
    Igual(100, Projeta.Core.Superficies.Malha.AreaPoligono(q), 1e-9, "área");
    Verdade(Projeta.Core.Superficies.Malha.Dentro(q, 5, 5) && !Projeta.Core.Superficies.Malha.Dentro(q, 11, 5), "dentro/fora");
    Igual(1, Projeta.Core.Superficies.Malha.TamanhoCelula(50000), 0, "célula 1 m para 5 ha");
    Igual(5, Projeta.Core.Superficies.Malha.TamanhoCelula(1_000_000), 0, "célula 5 m para 100 ha");
});
Teste("malha: plano inclinado dá corte e aterro exatos", () =>
{
    var fx = Projeta.Core.Superficies.Malha.Faixas();
    // diferença d = x - 5 num quadrado 10 × 10: 125 m³ de corte (x < 5) e 125 m³ de aterro
    var m = Projeta.Core.Superficies.Malha.Calcular(0, 0, 10, 10, 0.5, (x, y) => x - 5, null, fx);
    Igual(125, m.Corte, 1e-6, "corte"); Igual(125, m.Aterro, 1e-6, "aterro"); Igual(100, m.Area, 1e-9, "área");
    Verdade(m.Linhas == 20 && m.Colunas == 20, "20 × 20 células");
    Igual(125, fx.Where(x => x.Corte).Sum(x => x.Volume), 1e-6, "faixas somam o corte");
    var c = Projeta.Core.Superficies.Malha.Corridas(m.Faixa);
    Verdade(c.Count == 20 * 10, $"10 trechos por linha (5 faixas de corte e 5 de aterro), saiu {c.Count / 20.0}");
    var dentro = Projeta.Core.Superficies.Malha.Calcular(0, 0, 10, 10, 0.5, (x, y) => -1, (x, y) => x < 5, fx);
    Igual(50, dentro.Corte, 1e-6, "só metade dentro do polígono");
});
Teste("relatório de volumes (saldo e planilha)", () =>
{
    var r = new Projeta.Core.Superficies.RelatorioVolumes { Base = "PRIMITIVA", Comparacao = "PROJETO", FatorCorte = 1.25, FatorAterro = 1.3, Celula = 1 };
    r.Areas.Add(new Projeta.Core.Superficies.AreaVolume { Nome = "Lote 1", Area2D = 360, Corte = 500, Aterro = 100 });
    r.Areas.Add(new Projeta.Core.Superficies.AreaVolume { Nome = "Lote 2", Area2D = 400, Corte = 50, Aterro = 300 });
    r.Faixas = Projeta.Core.Superficies.Malha.Faixas();
    Igual(495, r.Saldo(r.Areas[0]), 1e-9, "saldo lote 1 = 625 - 130");
    Verdade(Projeta.Core.Superficies.RelatorioVolumes.Situacao(r.Saldo(r.Areas[1])) == "Falta (empréstimo)", "lote 2 falta");
    Verdade(Projeta.Core.Superficies.RelatorioVolumes.Situacao(-50, 10564) == "Equilíbrio"
            && Projeta.Core.Superficies.RelatorioVolumes.Situacao(-120, 10564) == "Falta (empréstimo)", "tolerância de 1 % no equilíbrio");
    var p = r.Planilha();
    Verdade(p.Folhas.Select(x => x.Nome).SequenceEqual(new[] { "Volumes", "Faixas de altura", "Origem" }), "abas");
    Verdade((string?)p.Folhas[0].Valor(8, 1) == "Total" && p.Folhas[0].Valor(8, 3) is double tc && tc == 550, "linha de total");
    p.Salvar(Path.Combine(saida, "volumes_teste.xlsx"));
});

Console.WriteLine("== Projeto × obra");
Teste("projeto × obra: faixas, malha, críticos, estacas e planilha", () =>
{
    var erros = new List<string>();
    int n = 0;
    Projeta.Core.Testes.TestesConformidade.Rodar((c, m) => { n++; if (!c) erros.Add(m); }, saida);
    Verdade(erros.Count == 0, $"{erros.Count} de {n} verificações falharam: {string.Join("; ", erros)}");
});

Console.WriteLine("== Platôs");
Teste("platô: volumes analíticos (quadrado, L), compensação, encontro, relatório", () =>
{
    var erros = new List<string>();
    int n = 0;
    Projeta.Core.Testes.TestesPlato.Rodar((c, m) => { n++; if (!c) erros.Add(m); }, saida);
    Verdade(erros.Count == 0, $"{erros.Count} de {n} verificações falharam: {string.Join("; ", erros)}");
});

Console.WriteLine("== Cubação");
Teste("cubação: áreas exatas por seção, áreas médias, compensação lateral, Brückner, planilha", () =>
{
    var erros = new List<string>();
    int n = 0;
    Projeta.Core.Testes.TestesCubacao.Rodar((c, m) => { n++; if (!c) erros.Add(m); }, saida);
    Verdade(erros.Count == 0, $"{erros.Count} de {n} verificações falharam: {string.Join("; ", erros)}");
});

Console.WriteLine("== Geo (UTM, coordenadas, GeoTIFF, relevo, KML, faixas)");
Teste("geo: UTM x PROJ, GMS, Helmert, GeoTIFF/LZW/GDAL, relevo, KML/KMZ, faixas de cota e declividade", () =>
{
    var erros = new List<string>();
    int n = 0;
    Projeta.Core.Testes.TestesGeo.Rodar((c, m) => { n++; if (!c) erros.Add(m); }, saida);
    Console.WriteLine($"       {n} verificações");
    Verdade(erros.Count == 0, $"{erros.Count} de {n} verificações falharam: {string.Join(" | ", erros)}");
});

Console.WriteLine();
Console.WriteLine($"Resultado: {ok} ok, {falhas} falha(s). Arquivos em {saida}");
if (falhas > 0) { File.WriteAllLines(Path.Combine(saida, "falhas.log"), log); Environment.Exit(1); }
