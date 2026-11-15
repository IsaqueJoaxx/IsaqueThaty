using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Capacidade e fila (PRJCAPACIDADE): entradas da rotatória (HCM 7ª ed., cap. 22) e movimentos da interseção com PARE
// (HCM 7ª ed., cap. 20, parada em duas aproximações). Capacidade, grau de saturação, atraso de controle, nível de serviço e
// fila de 95 %; faixas recomendadas na rotatória e armazenamento recomendado na faixa de conversão à esquerda.
// ======================================================================================================================

/// <summary>Volumes por movimento guardados no desenho (chave "origem>destino", pelos rumos dos braços), em veíc/h.</summary>
public sealed class DadosCapacidade
{
    public Dictionary<string, double> Movimentos { get; set; } = new();
    public List<string> Desenhados { get; set; } = new();

    public static string Chave(string origem, string destino) => $"{origem}>{destino}";
    public double Total(string origem) => Movimentos.Where(k => k.Key.StartsWith(origem + ">", StringComparison.Ordinal)).Sum(k => k.Value);
}

public sealed class ParametrosCapacidade
{
    public const string Hcm = "HCM 7ª ed. (TRB, 2022)";
    /// <summary>Fator de hora de pico.</summary>
    public double Fhp { get; set; } = 0.92;
    /// <summary>Fração de veículos pesados.</summary>
    public double Pesados { get; set; } = 0.05;
    /// <summary>Período de análise (h).</summary>
    public double Periodo { get; set; } = 0.25;
    /// <summary>Espaço por veículo na fila (m).</summary>
    public double EspacoVeiculo { get; set; } = 7.5;
    /// <summary>Grau de saturação máximo de projeto.</summary>
    public double XProjeto { get; set; } = 0.85;
    public bool ParametrosDoUsuario { get; set; }
}

/// <summary>Resultado de um movimento ou de uma entrada.</summary>
public sealed class ResultadoCapacidade
{
    public string Onde { get; set; } = "";
    public string Movimento { get; set; } = "";
    public int Braco { get; set; }
    public string Controle { get; set; } = "";
    public double Volume { get; set; }
    public double Fluxo { get; set; }
    public double Conflitante { get; set; } = double.NaN;
    public double IntervaloCritico { get; set; } = double.NaN;
    public double IntervaloSeguimento { get; set; } = double.NaN;
    public double Capacidade { get; set; }
    public double X { get; set; }
    public double Atraso { get; set; } = double.NaN;
    public string Nivel { get; set; } = "-";
    public double Fila95 { get; set; }
    public double Fila95m { get; set; }
    public int Faixas { get; set; } = 1;
}

/// <summary>Configuração da rotatória avaliada (faixas no anel e em cada entrada).</summary>
public sealed record AlternativaRotatoria(int FaixasAnel, int[] FaixasEntrada, double XMax, bool Atende);

public sealed class AnaliseCapacidade
{
    public string Item { get; set; } = "";
    public string Tipo { get; set; } = "";
    public List<ResultadoCapacidade> Movimentos { get; } = new();
    public List<ResultadoCapacidade> Aproximacoes { get; } = new();
    public List<AlternativaRotatoria> Alternativas { get; } = new();
    public List<string> Recomendacoes { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<string> Avisos { get; } = new();
    /// <summary>Armazenamento recomendado da faixa de conversão à esquerda (m), por braço da principal; vazio se não há conversão.</summary>
    public Dictionary<int, double> Armazenamento { get; } = new();
    public int FaixasAnelRecomendadas { get; set; }
}

public static class CalculoCapacidade
{
    /// <summary>Tipo do movimento pelo ângulo entre quem chega e quem sai (mão direita).</summary>
    public static string TipoMovimento(PolilinhaEixo origem, PolilinhaEixo destino)
    {
        var (ox, oy) = origem.Direcao(0);
        var (dx, dy) = destino.Direcao(0);
        double a = Math.Atan2(dy, dx) - Math.Atan2(-oy, -ox);
        while (a > Math.PI) a -= 2 * Math.PI;
        while (a <= -Math.PI) a += 2 * Math.PI;
        double g = a * 180 / Math.PI;
        return g > 45 ? "Esquerda" : g < -45 ? "Direita" : "Frente";
    }

    /// <summary>Capacidade potencial (veíc/h): vc·e^(−vc·tc/3600)/(1 − e^(−vc·tf/3600)); 3600/tf sem fluxo conflitante.</summary>
    public static double CapacidadePotencial(double vc, double tc, double tf)
    {
        if (vc < 1e-6) return 3600 / tf;
        return vc * Math.Exp(-vc * tc / 3600) / (1 - Math.Exp(-vc * tf / 3600));
    }

    /// <summary>Atraso de controle (s/veíc): 3600/c + 900T[x − 1 + √((x−1)² + (3600/c)x/(450T))] + 5·min(x, 1).</summary>
    public static double Atraso(double c, double v, double t)
    {
        if (c <= 0) return double.PositiveInfinity;
        double x = v / c;
        return 3600 / c + 900 * t * (x - 1 + Math.Sqrt((x - 1) * (x - 1) + 3600 / c * x / (450 * t))) + 5 * Math.Min(x, 1);
    }

    /// <summary>Fila de 95 % (veíc): 900T[x − 1 + √((1−x)² + (3600/c)x/(150T))]·c/3600.</summary>
    public static double Fila95(double c, double v, double t)
    {
        if (c <= 1e-9) return v * t;   // sem capacidade: tudo o que chega no período fica na fila
        double x = v / c;
        return 900 * t * (x - 1 + Math.Sqrt((1 - x) * (1 - x) + 3600 / c * x / (150 * t))) * c / 3600;
    }

    /// <summary>Número para texto: "-" sem valor e "&gt; teto" quando passa do teto (ou não há capacidade).</summary>
    public static string Txt(double v, string formato, double teto) =>
        double.IsNaN(v) ? "-" : double.IsInfinity(v) || v > teto ? "> " + teto.ToString(formato, System.Globalization.CultureInfo.InvariantCulture) : v.ToString(formato, System.Globalization.CultureInfo.InvariantCulture);
    public static string TxtX(double x) => Txt(x, "0.00", 9.99);
    public static string TxtAtraso(double d) => Txt(d, "0.0", 999);

    /// <summary>Nível de serviço pelo atraso (A ≤ 10, B ≤ 15, C ≤ 25, D ≤ 35, E ≤ 50 s) e F com x &gt; 1.</summary>
    public static string NivelServico(double atraso, double x) =>
        x > 1 + 1e-9 || double.IsNaN(atraso) || atraso > 50 ? "F" : atraso <= 10 ? "A" : atraso <= 15 ? "B" : atraso <= 25 ? "C" : atraso <= 35 ? "D" : "E";

    /// <summary>
    /// Capacidade de uma faixa de entrada da rotatória (ucp/h) pelo fluxo circulante vc (ucp/h): 1 faixa × 1 no anel 1380·e^(−1,02·10⁻³·vc);
    /// 2 faixas × 1 no anel 1420·e^(−0,91·10⁻³·vc); 1 faixa × 2 no anel 1420·e^(−0,85·10⁻³·vc); 2 × 2: direita 1420·e^(−0,85·10⁻³·vc) e
    /// esquerda 1350·e^(−0,92·10⁻³·vc).
    /// </summary>
    public static double CapacidadeEntrada(int faixasEntrada, int faixasAnel, double vc, bool esquerda = false)
    {
        if (faixasAnel <= 1) return faixasEntrada <= 1 ? 1380 * Math.Exp(-1.02e-3 * vc) : 1420 * Math.Exp(-0.91e-3 * vc);
        if (faixasEntrada <= 1) return 1420 * Math.Exp(-0.85e-3 * vc);
        return esquerda ? 1350 * Math.Exp(-0.92e-3 * vc) : 1420 * Math.Exp(-0.85e-3 * vc);
    }

    /// <summary>Índices dos braços na ordem do anel (anti-horário, mão direita): pelo ângulo crescente.</summary>
    public static int[] OrdemAnel(IReadOnlyList<double> angulos) =>
        Enumerable.Range(0, angulos.Count).OrderBy(i => ((angulos[i] % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI)).ToArray();

    /// <summary>
    /// Fluxo circulante em frente a cada entrada: os movimentos que passam pela entrada no anel anti-horário. <paramref name="ordem"/>
    /// é a sequência dos braços no anel (padrão 0, 1, ..., n−1).
    /// </summary>
    public static double[] Circulante(int n, Func<int, int, double> fluxo, int[]? ordem = null)
    {
        ordem ??= Enumerable.Range(0, n).ToArray();
        var pos = new int[n];
        for (int k = 0; k < n; k++) pos[ordem[k]] = k;
        var vc = new double[n];
        for (int o = 0; o < n; o++)
            for (int d = 0; d < n; d++)
            {
                double q = fluxo(o, d);
                if (q <= 0) continue;
                // de o até d no sentido anti-horário: passa nas entradas seguintes até antes de d (no retorno, d = o, passa em todas as outras)
                for (int k = (pos[o] + 1) % n; k != pos[d]; k = (k + 1) % n) vc[ordem[k]] += q;
            }
        return vc;
    }

    private static double FatorPesados(double p) => 1 / (1 + p * (2 - 1));

    /// <summary>Rotatória: cada entrada com 1 faixa ou com as faixas pedidas; avalia as configurações e recomenda a menor que atende.</summary>
    public static AnaliseCapacidade Rotatoria(string nome, ModeloRotatoriaSinal ms, IReadOnlyDictionary<(int O, int D), double> volumes, ParametrosCapacidade p,
                                              int faixasAnelAtual, int faixasEntradaAtual)
    {
        var r = new AnaliseCapacidade { Item = nome, Tipo = "Rotatória" };
        int n = ms.Bracos.Count;
        double fhv = FatorPesados(p.Pesados);
        double Pce(int o, int d) => volumes.TryGetValue((o, d), out var v) ? v / p.Fhp / fhv : 0;
        var vc = Circulante(n, Pce, OrdemAnel(ms.Bracos.Select(b => b.Angulo).ToList()));
        var entrada = Enumerable.Range(0, n).Select(o => Enumerable.Range(0, n).Sum(d => Pce(o, d))).ToArray();

        double XMax(int anel, int faixas, int i)
        {
            if (faixas <= 1) return entrada[i] / CapacidadeEntrada(1, anel, vc[i]);
            double cR = CapacidadeEntrada(2, anel, vc[i]), cL = CapacidadeEntrada(2, anel, vc[i], esquerda: true);
            return Math.Max(entrada[i] / 2 / cR, entrada[i] / 2 / cL);   // fluxo dividido igualmente entre as faixas [PREMISSA]
        }
        foreach (int anel in new[] { 1, 2 })
        {
            var fx = new int[n];
            for (int i = 0; i < n; i++) fx[i] = XMax(anel, 1, i) <= p.XProjeto + 1e-9 ? 1 : 2;
            double xm = Enumerable.Range(0, n).Select(i => XMax(anel, fx[i], i)).DefaultIfEmpty(0).Max();
            r.Alternativas.Add(new AlternativaRotatoria(anel, fx, xm, xm <= p.XProjeto + 1e-9));
        }
        var ok = r.Alternativas.FirstOrDefault(a => a.Atende);
        r.FaixasAnelRecomendadas = ok?.FaixasAnel ?? 2;

        // resultado da configuração atual
        for (int i = 0; i < n; i++)
        {
            var b = ms.Bracos[i];
            string onde = $"entrada {b.Rumo} ({b.Nome})";
            int fe = Math.Max(1, faixasEntradaAtual);
            double vol = Enumerable.Range(0, n).Sum(d => volumes.TryGetValue((i, d), out var v) ? v : 0);
            double vVeh = entrada[i] * fhv;
            double cap, x, atraso, fila;
            if (fe <= 1)
            {
                cap = CapacidadeEntrada(1, faixasAnelAtual, vc[i]) * fhv;
                x = vVeh / cap; atraso = Atraso(cap, vVeh, p.Periodo); fila = Fila95(cap, vVeh, p.Periodo);
            }
            else
            {
                double cR = CapacidadeEntrada(2, faixasAnelAtual, vc[i]) * fhv, cL = CapacidadeEntrada(2, faixasAnelAtual, vc[i], true) * fhv;
                double xR = vVeh / 2 / cR, xL = vVeh / 2 / cL;
                cap = cR + cL; x = Math.Max(xR, xL);
                atraso = (Atraso(cR, vVeh / 2, p.Periodo) + Atraso(cL, vVeh / 2, p.Periodo)) / 2;
                fila = Math.Max(Fila95(cR, vVeh / 2, p.Periodo), Fila95(cL, vVeh / 2, p.Periodo));
            }
            var res = new ResultadoCapacidade
            {
                Onde = onde, Movimento = "entrada", Braco = i, Controle = "Dê a preferência (rotatória)", Volume = vol, Fluxo = vVeh, Conflitante = vc[i] * fhv,
                Capacidade = cap, X = x, Atraso = atraso, Nivel = NivelServico(atraso, x), Fila95 = fila, Fila95m = Math.Ceiling(fila - 1e-9) * p.EspacoVeiculo, Faixas = fe,
            };
            r.Aproximacoes.Add(res);
            r.Verificacoes.Add(new Verificacao("Grau de saturação da entrada", $"{nome}, {onde}",
                $"x = {TxtX(x)} ({vVeh:0} veíc/h para {cap:0} veíc/h; circulante {vc[i] * fhv:0} veíc/h; {fe} faixa(s) na entrada, {faixasAnelAtual} no anel)",
                $"≤ {p.XProjeto:0.00} (0,90 é a capacidade prática)", $"{ParametrosCapacidade.Hcm}, cap. 22",
                x <= p.XProjeto + 1e-9 ? VerificacaoRotatoria.Aprovado : x <= 1 ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
            r.Verificacoes.Add(new Verificacao("Nível de serviço da entrada", $"{nome}, {onde}",
                $"NS {res.Nivel}: atraso {TxtAtraso(atraso)} s/veíc; fila de 95 % {fila:0.0} veíc ({res.Fila95m:0} m)", "NS D ou melhor [PREMISSA]", $"{ParametrosCapacidade.Hcm}, cap. 22 (Exhibit 22-8)",
                res.Nivel is "A" or "B" or "C" or "D" ? VerificacaoRotatoria.Aprovado : res.Nivel == "E" ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
        }
        if (ok is null)
            r.Recomendacoes.Add($"Nem com 2 faixas no anel e nas entradas o grau de saturação fica abaixo de {p.XProjeto:0.00} (máximo {r.Alternativas.Last().XMax:0.00}): a rotatória de até 2 faixas não atende a demanda; avalie outra solução (desnível, semáforo, rotatória maior) [HCM: método vale até 2 faixas].");
        else
        {
            var fx = ok.FaixasEntrada;
            r.Recomendacoes.Add($"Anel com {ok.FaixasAnel} faixa(s); entradas: " + string.Join(", ", Enumerable.Range(0, n).Select(i => $"{ms.Bracos[i].Rumo} {fx[i]}")) +
                                $" faixa(s) (x máximo {ok.XMax:0.00} ≤ {p.XProjeto:0.00}).");
            if (ok.FaixasAnel != faixasAnelAtual || fx.Any(f => f != faixasEntradaAtual))
                r.Recomendacoes.Add($"A geometria montada tem {faixasAnelAtual} faixa(s) no anel e {faixasEntradaAtual} por entrada: ajuste a largura do anel e das entradas (Parametros do PRJROTATORIA) e monte de novo.");
        }
        return r;
    }

    /// <summary>
    /// Interseção com PARE nas secundárias (HCM cap. 20): fluxo conflitante, intervalos crítico e de seguimento, capacidade potencial,
    /// impedância (ranks 2 a 4), faixa compartilhada na secundária, atraso, nível de serviço e fila; conversão à esquerda da principal com
    /// o armazenamento recomendado pela fila de 95 %.
    /// </summary>
    public static AnaliseCapacidade Pare(string nome, ModeloIntersecaoSinal m, IReadOnlyDictionary<(int O, int D), double> volumes, ParametrosCapacidade p, double armazenamentoAtual)
    {
        var r = new AnaliseCapacidade { Item = nome, Tipo = "Interseção com PARE" };
        var eixos = m.Bracos.Select(b => b.Polilinha()).ToList();
        int n = m.Bracos.Count;
        bool emT = n == 3;
        double V(int o, int d) => volumes.TryGetValue((o, d), out var v) ? v / p.Fhp : 0;
        string Tipo(int o, int d) => o == d ? "Retorno" : TipoMovimento(eixos[o], eixos[d]);
        double Mov(int o, string tipo) => Enumerable.Range(0, n).Where(d => d != o && Tipo(o, d) == tipo).Sum(d => V(o, d));
        int Destino(int o, string tipo) => Enumerable.Range(0, n).FirstOrDefault(d => d != o && Tipo(o, d) == tipo, -1);
        var maiores = Enumerable.Range(0, n).Where(i => m.Bracos[i].Principal).ToList();
        var menores = Enumerable.Range(0, n).Where(i => !m.Bracos[i].Principal).ToList();
        double tcHv = 1.0 * p.Pesados, tfHv = 0.9 * p.Pesados;
        const double T = 0.25;
        double t = p.Periodo > 0 ? p.Periodo : T;

        // rank 2: conversão à esquerda da principal (oposta: frente + direita da outra aproximação da principal)
        var p0 = new Dictionary<(int, string), double>();
        var cm = new Dictionary<(int, string), double>();
        foreach (int k in maiores)
        {
            double v = Mov(k, "Esquerda");
            if (v <= 0) continue;
            int opp = maiores.FirstOrDefault(j => j != k, -1);
            double vc = opp < 0 ? 0 : Mov(opp, "Frente") + Mov(opp, "Direita");
            double tc = 4.1 + tcHv, tf = 2.2 + tfHv;
            double c = CapacidadePotencial(vc, tc, tf);
            cm[(k, "Esquerda")] = c;
            p0[(k, "Esquerda")] = Math.Max(0, 1 - v / c);
            AdicionarMovimento(r, m, k, "Esquerda (da principal)", "Preferencial", v * p.Fhp, v, vc, tc, tf, c, p, t);
            if (m.Bracos[k].Conversao)
            {
                double fila = Fila95(c, v, t);
                double rec = Math.Max(15, Math.Ceiling(Math.Ceiling(fila - 1e-9) * p.EspacoVeiculo / 5 - 1e-9) * 5);
                r.Armazenamento[k] = rec;
                r.Verificacoes.Add(new Verificacao("Armazenamento da faixa de conversão à esquerda", $"{nome}, braço {m.Bracos[k].Rumo} ({m.Bracos[k].Nome})",
                    $"fila de 95 % {fila:0.0} veíc × {p.EspacoVeiculo:0.0} m = {Math.Ceiling(fila - 1e-9) * p.EspacoVeiculo:0.0} m; recomendado {rec:0} m; atual {armazenamentoAtual:0} m",
                    "armazenamento ≥ fila de 95 % (mínimo de 15 m [PREMISSA])", $"{ParametrosCapacidade.Hcm}, cap. 20 (fila de 95 %)",
                    armazenamentoAtual >= rec - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
            }
        }

        // secundárias: direita (rank 2), frente (rank 3), esquerda (rank 4)
        // duas passadas: a esquerda da secundária depende da fila livre da frente e da direita da secundária oposta
        foreach (int passo in new[] { 1, 2 })
        foreach (int i in menores)
        {
            // aproximação da principal mais perto (a que vira à direita para dentro deste braço) e a mais longe
            int perto = maiores.FirstOrDefault(k => Destino(k, "Direita") == i, -1);
            int longe = maiores.FirstOrDefault(k => k != perto, -1);
            double L(int k) => k < 0 ? 0 : Mov(k, "Esquerda");
            double Tt(int k) => k < 0 ? 0 : Mov(k, "Frente");
            double R(int k) => k < 0 ? 0 : Mov(k, "Direita");
            int oposta = menores.FirstOrDefault(j => j != i, -1);
            double pL(int k) => k >= 0 && p0.TryGetValue((k, "Esquerda"), out var x) ? x : 1;

            // direita: entra na corrente da principal que segue para o mesmo lado (a mais perto vista do outro lado: a que segue em frente para o destino da direita)
            double vR = Mov(i, "Direita");
            int dR = Destino(i, "Direita");
            int kR = maiores.FirstOrDefault(k => Destino(k, "Frente") == dR, perto);
            double vcR = Tt(kR) + 0.5 * (kR >= 0 && Destino(kR, "Direita") == i ? V(kR, i) : 0);
            double tcR = 6.2 + tcHv, tfR = 3.3 + tfHv;
            double cR = CapacidadePotencial(vcR, tcR, tfR);
            p0[(i, "Direita")] = Math.Max(0, 1 - vR / cR);

            // frente
            double vT = Mov(i, "Frente");
            double vcT = 2 * L(perto) + Tt(perto) + 0.5 * R(perto) + 2 * L(longe) + Tt(longe) + R(longe);
            double tcT = 6.5 + tcHv, tfT = 4.0 + tfHv;
            double cT = CapacidadePotencial(vcT, tcT, tfT) * pL(perto) * pL(longe);
            p0[(i, "Frente")] = cT > 0 ? Math.Max(0, 1 - vT / cT) : 0;
            if (passo == 1) continue;

            // esquerda
            double vL = Mov(i, "Esquerda");
            double vcL = 2 * L(perto) + Tt(perto) + 0.5 * R(perto) + 2 * L(longe) + Tt(longe) + 0.5 * R(longe)
                         + (oposta >= 0 ? 0.5 * Mov(oposta, "Direita") + 0.5 * Mov(oposta, "Frente") : 0);
            double tcL = 7.1 + tcHv - (emT ? 0.7 : 0), tfL = 3.5 + tfHv;
            double cpL = CapacidadePotencial(vcL, tcL, tfL);
            double p2 = pL(perto) * pL(longe) * (oposta >= 0 && p0.TryGetValue((oposta, "Frente"), out var pt) ? pt : 1);
            double p1 = 0.65 * p2 - p2 / (p2 + 3) + 0.6 * Math.Sqrt(p2);
            double cLm = cpL * (oposta >= 0 ? p1 * (p0.TryGetValue((oposta, "Direita"), out var pr) ? pr : 1) : pL(perto) * pL(longe));

            string onde = $"braço {m.Bracos[i].Rumo} ({m.Bracos[i].Nome})";
            if (vR > 0) AdicionarMovimento(r, m, i, "Direita", "PARE", vR * p.Fhp, vR, vcR, tcR, tfR, cR, p, t);
            if (vT > 0) AdicionarMovimento(r, m, i, "Frente", "PARE", vT * p.Fhp, vT, vcT, tcT, tfT, cT, p, t);
            if (vL > 0) AdicionarMovimento(r, m, i, "Esquerda", "PARE", vL * p.Fhp, vL, vcL, tcL, tfL, cLm, p, t);

            // faixa única compartilhada na secundária [PREMISSA]
            double vTot = vR + vT + vL;
            double soma = (vR > 0 ? vR / cR : 0) + (vT > 0 ? vT / cT : 0) + (vL > 0 ? vL / cLm : 0);
            if (vTot <= 0) continue;
            double cSh = soma > 0 && !double.IsInfinity(soma) ? vTot / soma : 0;
            double x = cSh > 0 ? vTot / cSh : double.PositiveInfinity, atraso = Atraso(cSh, vTot, t), fila = Fila95(cSh, vTot, t);
            var ap = new ResultadoCapacidade
            {
                Onde = onde, Movimento = "aproximação (faixa compartilhada)", Braco = i, Controle = "PARE", Volume = vTot * p.Fhp, Fluxo = vTot, Capacidade = cSh,
                X = x, Atraso = atraso, Nivel = NivelServico(atraso, x), Fila95 = fila, Fila95m = Math.Ceiling(fila - 1e-9) * p.EspacoVeiculo,
            };
            r.Aproximacoes.Add(ap);
            r.Verificacoes.Add(new Verificacao("Grau de saturação da aproximação com PARE", $"{nome}, {onde}",
                $"x = {TxtX(x)} ({vTot:0} veíc/h para {cSh:0} veíc/h na faixa compartilhada)", $"≤ {p.XProjeto:0.00}", $"{ParametrosCapacidade.Hcm}, cap. 20",
                x <= p.XProjeto + 1e-9 ? VerificacaoRotatoria.Aprovado : x <= 1 ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
            r.Verificacoes.Add(new Verificacao("Nível de serviço da aproximação com PARE", $"{nome}, {onde}",
                $"NS {ap.Nivel}: atraso {TxtAtraso(atraso)} s/veíc; fila de 95 % {fila:0.0} veíc ({ap.Fila95m:0} m)", "NS D ou melhor [PREMISSA]", $"{ParametrosCapacidade.Hcm}, cap. 20",
                ap.Nivel is "A" or "B" or "C" or "D" ? VerificacaoRotatoria.Aprovado : ap.Nivel == "E" ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
        }
        foreach (int k in maiores.Where(k => Mov(k, "Esquerda") > 0 && cm.ContainsKey((k, "Esquerda"))))
        {
            var mv = r.Movimentos.First(x => x.Braco == k && x.Movimento.StartsWith("Esquerda"));
            r.Aproximacoes.Add(new ResultadoCapacidade
            {
                Onde = $"braço {m.Bracos[k].Rumo} ({m.Bracos[k].Nome})", Movimento = m.Bracos[k].Conversao ? "conversão à esquerda (faixa exclusiva)" : "conversão à esquerda (faixa compartilhada [PREMISSA: sem bloqueio])",
                Braco = k, Controle = "Preferencial", Volume = mv.Volume, Fluxo = mv.Fluxo, Capacidade = mv.Capacidade, X = mv.X, Atraso = mv.Atraso, Nivel = mv.Nivel,
                Fila95 = mv.Fila95, Fila95m = mv.Fila95m,
            });
        }
        if (r.Aproximacoes.Any(a => a.X > 1))
            r.Recomendacoes.Add("Há aproximação acima da capacidade: avalie semáforo (PRJSEMAFORO), rotatória (PRJROTATORIA) ou faixas exclusivas de conversão.");
        else if (r.Aproximacoes.Any(a => a.X > p.XProjeto))
            r.Recomendacoes.Add($"Há aproximação acima de x = {p.XProjeto:0.00}: margem pequena; avalie faixa exclusiva de conversão ou outro controle.");
        foreach (var (k, rec) in r.Armazenamento)
            r.Recomendacoes.Add($"Faixa de conversão à esquerda do braço {m.Bracos[k].Rumo}: armazenamento de {rec:0} m (atual {armazenamentoAtual:0} m).");
        return r;
    }

    private static void AdicionarMovimento(AnaliseCapacidade r, ModeloIntersecaoSinal m, int i, string mov, string controle, double vol, double v, double vc, double tc, double tf, double c,
                                           ParametrosCapacidade p, double t)
    {
        double x = c > 0 ? v / c : double.PositiveInfinity;
        double atraso = Atraso(c, v, t), fila = Fila95(c, v, t);
        r.Movimentos.Add(new ResultadoCapacidade
        {
            Onde = $"braço {m.Bracos[i].Rumo} ({m.Bracos[i].Nome})", Movimento = mov, Braco = i, Controle = controle, Volume = vol, Fluxo = v, Conflitante = vc,
            IntervaloCritico = tc, IntervaloSeguimento = tf, Capacidade = c, X = x, Atraso = atraso, Nivel = NivelServico(atraso, x), Fila95 = fila,
            Fila95m = Math.Ceiling(fila - 1e-9) * p.EspacoVeiculo,
        });
    }
}

/// <summary>Planilha do PRJCAPACIDADE.</summary>
public sealed class RelatorioCapacidade
{
    public string Titulo { get; set; } = "Capacidade e fila";
    public string Desenho { get; set; } = "";
    public AnaliseCapacidade Analise { get; set; } = new();
    public List<(string Origem, string Destino, string Tipo, double Volume)> Volumes { get; } = new();
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<string> Pendencias { get; } = new();

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";

    public static List<ParametroRotatoria> ParametrosDe(ParametrosCapacidade p, string tipo)
    {
        string pre = p.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA]";
        string h = ParametrosCapacidade.Hcm;
        var l = new List<ParametroRotatoria>
        {
            new("Fator de hora de pico (FHP)", $"{p.Fhp:0.00}", "", $"{h}, eq. 22-8 / cap. 20 (v = V/FHP); valor {pre}"),
            new("Veículos pesados", $"{p.Pesados * 100:0.#}", "%", pre),
            new("Período de análise", $"{p.Periodo:0.00}", "h", $"{h} (padrão de 15 min)"),
            new("Espaço por veículo na fila", $"{p.EspacoVeiculo:0.0}", "m", pre),
            new("Grau de saturação máximo de projeto", $"{p.XProjeto:0.00}", "", $"{pre} (0,85 usual em rotatória; 0,90 é a capacidade prática)"),
            new("Atraso de controle", "3600/c + 900T[x − 1 + √((x−1)² + (3600/c)x/(450T))] + 5·min(x,1)", "s/veíc", $"{h}, eq. 22-17 (e cap. 20)"),
            new("Fila de 95 %", "900T[x − 1 + √((1−x)² + (3600/c)x/(150T))]·c/3600", "veíc", $"{h} (cap. 20 e 22) [A CONFIRMAR na edição]"),
            new("Nível de serviço", "A ≤ 10; B ≤ 15; C ≤ 25; D ≤ 35; E ≤ 50; F > 50 s ou x > 1", "s/veíc", $"{h}, Exhibit 22-8 / cap. 20"),
        };
        if (tipo == "Rotatória")
        {
            l.Add(new("Capacidade: 1 faixa na entrada e 1 no anel", "1380·e^(−1,02·10⁻³·vc)", "ucp/h", $"{h}, eq. 22-1"));
            l.Add(new("Capacidade: 2 faixas na entrada e 1 no anel", "1420·e^(−0,91·10⁻³·vc)", "ucp/h", $"{h}, cap. 22 [A CONFIRMAR na edição]"));
            l.Add(new("Capacidade: 1 faixa na entrada e 2 no anel", "1420·e^(−0,85·10⁻³·vc)", "ucp/h", $"{h}, cap. 22 [A CONFIRMAR na edição]"));
            l.Add(new("Capacidade: 2 × 2 (direita / esquerda)", "1420·e^(−0,85·10⁻³·vc) / 1350·e^(−0,92·10⁻³·vc)", "ucp/h", $"{h}, cap. 22 [A CONFIRMAR na edição]"));
            l.Add(new("Equivalência de pesados", "2,0", "ucp", $"{h}, Exhibit 22-11"));
            l.Add(new("Divisão do fluxo nas entradas de 2 faixas", "igual", "", "[PREMISSA]"));
        }
        else
        {
            l.Add(new("Intervalo crítico base (esquerda principal / direita / frente / esquerda secundária)", "4,1 / 6,2 / 6,5 / 7,1", "s", $"{h}, cap. 20 (principal de 2 faixas)"));
            l.Add(new("Intervalo de seguimento base", "2,2 / 3,3 / 4,0 / 3,5", "s", $"{h}, cap. 20"));
            l.Add(new("Ajustes de pesados (crítico / seguimento)", "1,0·P / 0,9·P", "s", $"{h}, cap. 20 [A CONFIRMAR]"));
            l.Add(new("Ajuste da esquerda da secundária em T", "−0,7", "s", $"{h}, cap. 20 [A CONFIRMAR]"));
            l.Add(new("Capacidade potencial", "vc·e^(−vc·tc/3600)/(1 − e^(−vc·tf/3600))", "veíc/h", $"{h}, cap. 20"));
            l.Add(new("Secundária", "uma faixa compartilhada (direita, frente e esquerda)", "", "[PREMISSA]"));
        }
        return l;
    }

    public static IEnumerable<string> PendenciasPadrao(string tipo) => new List<string>
    {
        "Contagem classificada por movimento na hora de pico (e projeção para o ano de projeto): sem isso o resultado é só estimativa.",
        "Pedestres e ciclistas não reduzem a capacidade neste cálculo (HCM cap. 20 e 22 têm os fatores) [A CONFIRMAR em travessias movimentadas].",
        tipo == "Rotatória"
            ? "Fluxo desigual entre as faixas, faixa de giro livre (bypass) e entrada alargada (flare) não estão modelados."
            : "Bloqueio da faixa compartilhada da principal pela conversão à esquerda, travessia em duas etapas e semáforos próximos não estão modelados.",
        "Calibração local: o HCM é norte-americano; intervalos críticos e capacidade podem diferir no Brasil (confira com o DNIT / órgão).",
    };

    public PastaXlsx Planilha()
    {
        var a = Analise;
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n0 = new EstiloCelula { Formato = "0" };
        var n1 = new EstiloCelula { Formato = "0.0" };
        var n2 = new EstiloCelula { Formato = "0.00" };
        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };
        EstiloCelula Ns(string ns) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = ns is "A" or "B" or "C" ? Verde : ns is "D" or "E" ? Amarelo : Vermelho };
        void Num(FolhaXlsx f, int l, int c, double v, EstiloCelula e) { if (double.IsNaN(v) || double.IsInfinity(v)) f.Texto(l, c, "-", txt); else f.Numero(l, c, v, e); }

        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string x, string y) { fr.Texto(lr, 1, x, txt with { Negrito = true }); fr.Texto(lr, 2, y, q); lr++; }
        Par("Item", $"{a.Item} ({a.Tipo})");
        foreach (var ap in a.Aproximacoes) Par(ap.Onde, $"{ap.Movimento}: x = {CalculoCapacidade.TxtX(ap.X)}, NS {ap.Nivel}, atraso {CalculoCapacidade.TxtAtraso(ap.Atraso)} s/veíc, fila de 95 % {ap.Fila95m:0} m");
        foreach (var rec in a.Recomendacoes) Par("Recomendação", rec);
        var ver = a.Verificacoes;
        Par("Verificações", $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} reprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção");
        Par("Método", ParametrosCapacidade.Hcm);
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        foreach (var av in a.Avisos) { fr.Texto(lr, 1, "Aviso", txt with { Negrito = true }); fr.Texto(lr, 2, av, q); lr++; }
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 30); fr.Largura(2, 100);

        var fv0 = pasta.NovaFolha("Volumes");
        Cab(fv0, 1, ("De", 10), ("Para", 10), ("Movimento", 12), ("Volume (veíc/h)", 12));
        int l0 = 2;
        foreach (var (o, d, tp, v) in Volumes) { fv0.Texto(l0, 1, o, txt); fv0.Texto(l0, 2, d, txt); fv0.Texto(l0, 3, tp, txt); fv0.Numero(l0, 4, v, n0); l0++; }

        void Tabela(string nomeFolha, List<ResultadoCapacidade> lista)
        {
            var f = pasta.NovaFolha(nomeFolha);
            Cab(f, 1, ("Onde", 28), ("Movimento", 26), ("Controle", 14), ("Volume (veíc/h)", 10), ("Fluxo (veíc/h)", 10), ("Conflitante (veíc/h)", 11), ("tc (s)", 7), ("tf (s)", 7),
                ("Capacidade (veíc/h)", 11), ("x", 7), ("Atraso (s/veíc)", 10), ("NS", 6), ("Fila 95 % (veíc)", 10), ("Fila 95 % (m)", 10), ("Faixas", 7));
            int l = 2;
            foreach (var x in lista)
            {
                f.Texto(l, 1, x.Onde, q); f.Texto(l, 2, x.Movimento, q); f.Texto(l, 3, x.Controle, q); f.Numero(l, 4, x.Volume, n0); f.Numero(l, 5, x.Fluxo, n0);
                Num(f, l, 6, x.Conflitante, n0); Num(f, l, 7, x.IntervaloCritico, n1); Num(f, l, 8, x.IntervaloSeguimento, n1); Num(f, l, 9, x.Capacidade, n0); Num(f, l, 10, x.X, n2);
                Num(f, l, 11, x.Atraso, n1); f.Texto(l, 12, x.Nivel, Ns(x.Nivel)); Num(f, l, 13, x.Fila95, n1); Num(f, l, 14, x.Fila95m, n0); f.Numero(l, 15, x.Faixas, n0);
                l++;
            }
        }
        Tabela("Aproximações", a.Aproximacoes);
        if (a.Movimentos.Count > 0) Tabela("Movimentos", a.Movimentos);

        if (a.Alternativas.Count > 0)
        {
            var fa = pasta.NovaFolha("Alternativas");
            Cab(fa, 1, ("Faixas no anel", 10), ("Faixas por entrada", 40), ("x máximo", 10), ("Atende", 10));
            int la = 2;
            foreach (var al in a.Alternativas)
            {
                fa.Numero(la, 1, al.FaixasAnel, n0); fa.Texto(la, 2, string.Join(" / ", al.FaixasEntrada), txt); fa.Numero(la, 3, al.XMax, n2);
                fa.Texto(la, 4, al.Atende ? "sim" : "não", Res(al.Atende ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                la++;
            }
        }

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 46), ("Valor", 40), ("Unidade", 9), ("Fonte", 60));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, q); fp.Texto(lp, 2, p.Valor, q); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 30), ("Onde", 36), ("Valor", 54), ("Critério", 30), ("Fonte", 40), ("Resultado", 13));
        int lv = 2;
        foreach (var v in ver) { fv.Texto(lv, 1, v.Item, q); fv.Texto(lv, 2, v.Braco, q); fv.Texto(lv, 3, v.Valor, q); fv.Texto(lv, 4, v.Criterio, q); fv.Texto(lv, 5, v.Fonte, q); fv.Texto(lv, 6, v.Resultado, Res(v.Resultado)); lv++; }

        var fd = pasta.NovaFolha("Pendências");
        Cab(fd, 1, ("Pendência", 110));
        int ld = 2;
        foreach (var p in Pendencias) { fd.Texto(ld, 1, p, q); ld++; }
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
