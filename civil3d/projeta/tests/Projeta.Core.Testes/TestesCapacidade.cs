using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes de capacidade e fila (PRJCAPACIDADE): fórmulas do HCM, fluxo circulante, rotatória e interseção com PARE.</summary>
internal static class TestesCapacidade
{
    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;
        static List<double[]> Reta(double ax, double ay, double bx, double by) => new() { new[] { ax, ay }, new[] { bx, by } };

        Ok(Q(CalculoCapacidade.CapacidadeEntrada(1, 1, 0), 1380) && Q(CalculoCapacidade.CapacidadeEntrada(1, 1, 500), 1380 * Math.Exp(-0.51), 1e-9),
           $"rotatória de 1 faixa: 1380 ucp/h sem circulante e {CalculoCapacidade.CapacidadeEntrada(1, 1, 500):0.0} com 500 ucp/h");
        double d = CalculoCapacidade.Atraso(800, 400, 0.25), f = CalculoCapacidade.Fila95(800, 400, 0.25);
        Ok(Q(d, 11.413, 0.01) && CalculoCapacidade.NivelServico(d, 0.5) == "B" && Q(f, 2.839, 0.01), $"atraso {d:0.00} s (NS B) e fila de 95 % {f:0.00} veíc com c 800 e v 400");
        Ok(CalculoCapacidade.NivelServico(60, 0.9) == "F" && CalculoCapacidade.NivelServico(5, 1.05) == "F" && CalculoCapacidade.NivelServico(30, 0.8) == "D", "nível de serviço: F acima de 50 s ou com x > 1");
        double cp = CalculoCapacidade.CapacidadePotencial(500, 6.5, 4.0);
        Ok(Q(cp, 475.6, 0.2) && Q(CalculoCapacidade.CapacidadePotencial(0, 7.1, 3.5), 3600 / 3.5, 1e-9), $"capacidade potencial do PARE {cp:0.0} veíc/h (vc 500, tc 6,5, tf 4,0)");

        var vc = CalculoCapacidade.Circulante(4, (o, dd) => o != dd ? 100 : 0);
        var vc2 = CalculoCapacidade.Circulante(4, (o, dd) => o == 3 && dd == 1 ? 200 : 0);
        Ok(vc.All(x => Q(x, 300)) && Q(vc2[0], 200) && Q(vc2[1], 0) && Q(vc2[2], 0) && Q(vc2[3], 0), "fluxo circulante: só os movimentos que passam em frente à entrada (anel anti-horário)");
        var ord = CalculoCapacidade.OrdemAnel(new[] { Math.PI, 0.0, 3 * Math.PI / 2, Math.PI / 2 });   // O, L, S, N fora de ordem
        var vc3 = CalculoCapacidade.Circulante(4, (o, dd) => o == 1 && dd == 0 ? 120 : 0, ord);       // de L para O passa só pelo N
        Ok(ord.SequenceEqual(new[] { 1, 3, 0, 2 }) && Q(vc3[3], 120) && Q(vc3[0] + vc3[1] + vc3[2], 0), "ordem do anel pelo ângulo, com os braços fora de ordem");

        // rotatória de 4 braços (a mesma dos testes de sinalização)
        var m = new ModeloRotatoria { RExt = 20, RIlha = 14 };
        foreach (double ang in new[] { 0.0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 })
            m.Bracos.Add(new BracoRotatoria { Nome = "R" + (int)Math.Round(ang * 180 / Math.PI), Rumo = GeometriaRotatoria.Rumo(ang), Angulo = ang,
                Eixo = new PolilinhaEixo(new[] { (17 * Math.Cos(ang), 17 * Math.Sin(ang)), (217 * Math.Cos(ang), 217 * Math.Sin(ang)) }), SRetencao = 3, BordoEntrada = 6.5, BordoSaida = 6.5 });
        var rot = ModeloRotatoriaSinal.De(m, 6, 0);
        var p = new ParametrosCapacidade();
        Dictionary<(int, int), double> Igual(double v) { var x = new Dictionary<(int, int), double>(); for (int o = 0; o < 4; o++) for (int dd = 0; dd < 4; dd++) if (o != dd) x[(o, dd)] = v; return x; }
        var rr = CalculoCapacidade.Rotatoria("Rotatória 1", rot, Igual(150), p, 1, 1);
        double fhv = 1 / 1.05, pce = 150 / 0.92 / fhv, xEsp = 3 * pce / (1380 * Math.Exp(-1.02e-3 * 3 * pce));
        Ok(rr.FaixasAnelRecomendadas == 1 && rr.Aproximacoes.All(a => Q(a.X, xEsp, 1e-9)) && rr.Alternativas[0].Atende,
           $"450 veíc/h por entrada: anel de 1 faixa basta (x = {rr.Aproximacoes[0].X:0.000}, NS {rr.Aproximacoes[0].Nivel}, fila {rr.Aproximacoes[0].Fila95m:0} m)");
        var rr2 = CalculoCapacidade.Rotatoria("Rotatória 1", rot, Igual(250), p, 1, 1);
        Ok(rr2.Alternativas.Any(a => a.Atende) && rr2.Aproximacoes.All(a => a.X > p.XProjeto) && rr2.Recomendacoes.Count >= 1,
           $"750 veíc/h por entrada: 1 faixa não atende (x = {rr2.Aproximacoes[0].X:0.00}); recomenda {rr2.Recomendacoes[0]}");
        var rr3 = CalculoCapacidade.Rotatoria("Rotatória 1", rot, Igual(350), p, 1, 1);
        Ok(!rr3.Alternativas.Any(a => a.Atende) && rr3.Recomendacoes[0].Contains("não atende") && rr3.Aproximacoes.All(a => a.Nivel == "F"),
           "1050 veíc/h por entrada: nem 2 × 2 atende; NS F");

        // interseção com PARE: principal A (L-O), secundária B (N-S)
        var cruz = new ModeloIntersecaoSinal
        {
            Bracos =
            {
                new BracoIntersecaoSinal { Nome = "A", Rumo = "L", Principal = true, Eixo = Reta(0, 0, 100, 0), MeiaLargura = 3.5, Conversao = true, Alargamento = 3.6, DPlena = 40, Teiper = 30, DRetencaoConversao = 4 },
                new BracoIntersecaoSinal { Nome = "B", Rumo = "N", Eixo = Reta(0, 0, 0, 100), MeiaLargura = 3.0 },
                new BracoIntersecaoSinal { Nome = "A", Rumo = "O", Principal = true, Eixo = Reta(0, 0, -100, 0), MeiaLargura = 3.5 },
                new BracoIntersecaoSinal { Nome = "B", Rumo = "S", Eixo = Reta(0, 0, 0, -100), MeiaLargura = 3.0 },
            },
        };
        var ex = cruz.Bracos.Select(b => b.Polilinha()).ToList();
        Ok(CalculoCapacidade.TipoMovimento(ex[0], ex[1]) == "Direita" && CalculoCapacidade.TipoMovimento(ex[0], ex[3]) == "Esquerda" && CalculoCapacidade.TipoMovimento(ex[0], ex[2]) == "Frente",
           "quem chega pelo leste: norte = direita, sul = esquerda, oeste = em frente");
        var vols = new Dictionary<(int, int), double>
        {
            [(0, 2)] = 500, [(0, 1)] = 50, [(0, 3)] = 80,
            [(2, 0)] = 450, [(2, 3)] = 60, [(2, 1)] = 70,
            [(1, 3)] = 40, [(1, 2)] = 50, [(1, 0)] = 60,
            [(3, 1)] = 30, [(3, 0)] = 40, [(3, 2)] = 50,
        };
        var rp = CalculoCapacidade.Pare("Interseção 1", cruz, vols, p, 15);
        var mN = rp.Movimentos.Where(x => x.Braco == 1).ToDictionary(x => x.Movimento);
        double vcLeste = (450 + 60) / 0.92;
        var esqL = rp.Movimentos.First(x => x.Braco == 0 && x.Movimento.StartsWith("Esquerda"));
        Ok(Q(esqL.Conflitante, vcLeste, 1e-9) && Q(esqL.IntervaloCritico, 4.1 + 0.05, 1e-9), $"esquerda da principal: conflitante = frente + direita da oposta ({esqL.Conflitante:0} veíc/h)");
        var rs = CalculoCapacidade.Pare("Interseção 1", cruz, new Dictionary<(int, int), double>(vols) { [(3, 1)] = 40, [(3, 0)] = 60, [(3, 2)] = 50, [(1, 3)] = 40, [(1, 2)] = 60, [(1, 0)] = 50, [(0, 1)] = 50, [(0, 3)] = 50, [(2, 3)] = 50, [(2, 1)] = 50, [(0, 2)] = 450 }, p, 15);
        var eN = rs.Movimentos.First(x => x.Braco == 1 && x.Movimento == "Esquerda"); var eS = rs.Movimentos.First(x => x.Braco == 3 && x.Movimento == "Esquerda");
        Ok(Q(eN.Capacidade, eS.Capacidade, 1e-6), $"cruzamento simétrico: esquerda da secundária N e S com a mesma capacidade ({eN.Capacidade:0.0} e {eS.Capacidade:0.0} veíc/h) nas duas passadas");
        var sat = CalculoCapacidade.Pare("Interseção 1", cruz, new Dictionary<(int, int), double>(Enumerable.Range(0, 4).SelectMany(o => Enumerable.Range(0, 4).Where(dd => dd != o).Select(dd => KeyValuePair.Create((o, dd), 900.0)))), p, 15);
        Ok(sat.Aproximacoes.All(a => !double.IsNaN(a.Fila95) && !double.IsInfinity(a.Fila95)) && sat.Aproximacoes.Any(a => a.Nivel == "F"), "demanda muito acima da capacidade: fila finita (chegadas do período) e NS F");
        Ok(mN["Esquerda"].Capacidade < mN["Frente"].Capacidade && mN["Frente"].Capacidade < mN["Direita"].Capacidade,
           $"secundária N: capacidade esquerda {mN["Esquerda"].Capacidade:0} < frente {mN["Frente"].Capacidade:0} < direita {mN["Direita"].Capacidade:0} veíc/h");
        double vcDirN = 500 / 0.92 + 0.5 * 50 / 0.92;   // segue para o oeste junto com quem vem do leste em frente (500) e metade de quem vira à direita para o N (50)
        Ok(Q(mN["Direita"].Conflitante, vcDirN, 1e-9), $"direita da secundária N entra na corrente que vai para o oeste: {mN["Direita"].Conflitante:0} veíc/h");
        var apN = rp.Aproximacoes.First(x => x.Braco == 1);
        double cSh = apN.Fluxo / mN.Values.Sum(x => x.Fluxo / x.Capacidade);
        Ok(Q(apN.Capacidade, cSh, 1e-9) && apN.X > 1 && apN.Nivel == "F" && rp.Recomendacoes.Any(r => r.Contains("semáforo")) && rp.Armazenamento.ContainsKey(0) && rp.Armazenamento[0] >= 15,
           $"aproximação N (faixa compartilhada Σv/Σ(v/c)): x {apN.X:0.00}, NS {apN.Nivel} — a esquerda da secundária cruza 1000 veíc/h e a recomendação aponta semáforo ou rotatória; conversão do braço L: {rp.Armazenamento[0]:0} m");
        var cheia = new Dictionary<(int, int), double>(vols) { [(0, 3)] = 400, [(0, 2)] = 900, [(2, 0)] = 900 };
        var rc = CalculoCapacidade.Pare("Interseção 1", cruz, cheia, p, 15);
        Ok(rc.Armazenamento[0] > 15 && rc.Verificacoes.Any(v => v.Item.StartsWith("Armazenamento") && v.Resultado == VerificacaoRotatoria.Reprovado),
           $"conversão carregada: armazenamento recomendado {rc.Armazenamento[0]:0} m e o de 15 m reprovado");

        var rel = new RelatorioCapacidade { Titulo = "Capacidade - teste", Analise = rp };
        rel.Parametros.AddRange(RelatorioCapacidade.ParametrosDe(p, rp.Tipo));
        rel.Pendencias.AddRange(RelatorioCapacidade.PendenciasPadrao(rp.Tipo));
        string arq = Path.Combine(saida, "capacidade.xlsx");
        rel.Salvar(arq);
        var rel2 = new RelatorioCapacidade { Titulo = "Capacidade - rotatória", Analise = rr2 };
        rel2.Parametros.AddRange(RelatorioCapacidade.ParametrosDe(p, rr2.Tipo));
        string arq2 = Path.Combine(saida, "capacidade_rot.xlsx");
        rel2.Salvar(arq2);
        Ok(File.Exists(arq) && new FileInfo(arq).Length > 3000 && File.Exists(arq2), "planilhas de capacidade gravadas");
    }
}
