using Projeta.Core.Relatorios;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;
using Projeta.Giro.Core.Verificacao;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Auditor de segurança da rotatória (PRJSEGURANCA): trajetória mais rápida (velocidade e deflexão de cada entrada), giro do
// veículo de projeto em todos os movimentos (folga das rodas aos meios-fios, à ilha central e uso da faixa transponível) e
// visibilidade em 3D sobre as superfícies (parada na aproximação e triângulo de visibilidade à esquerda).
// ======================================================================================================================

/// <summary>O que o PRJSEGURANCA desenhou para o item (para substituir na próxima auditoria).</summary>
public sealed class DadosSeguranca
{
    public string Veiculos { get; set; } = "";
    public List<string> Desenhados { get; set; } = new();
}

public sealed class ParametrosSeguranca
{
    public const string Nchrp = "NCHRP 672 (2010) — referência complementar";
    public const string Aashto = "AASHTO (2011) — referência complementar";
    /// <summary>Código do veículo de projeto (biblioteca do PROJETA Giro: VP, CO, O, SR, RE e os do usuário).</summary>
    public string Veiculo { get; set; } = "SR";
    /// <summary>Roda também o carro (VP), para ver se ele precisa da faixa transponível.</summary>
    public bool ConferirVp { get; set; } = true;
    /// <summary>Folga mínima entre a borda do pneu e o meio-fio (m).</summary>
    public double FolgaRoda { get; set; } = 0.60;
    /// <summary>Condução no anel: roda dianteira externa a esta distância do meio-fio externo (m).</summary>
    public double FolgaGuia { get; set; } = 1.00;
    /// <summary>Velocidade de aproximação para a distância de visibilidade de parada (km/h).</summary>
    public double VelocidadeAproximacao { get; set; } = 60;
    public double TempoReacao { get; set; } = 2.5;
    public double Desaceleracao { get; set; } = 3.4;
    /// <summary>Altura do olho do motorista, do objeto (parada) e do veículo que se aproxima (visibilidade à esquerda), em m.</summary>
    public double AlturaOlho { get; set; } = 1.08;
    public double AlturaObjeto { get; set; } = 0.60;
    public double AlturaVeiculo { get; set; } = 1.08;
    /// <summary>Comprimento do caminho do veículo antes e depois do anel (m).</summary>
    public double Trecho { get; set; } = 50;
    public bool ParametrosDoUsuario { get; set; }
}

/// <summary>Um movimento do veículo de projeto: caminho do eixo dianteiro, envoltórias e folgas.</summary>
public sealed class MovimentoGiro
{
    public string Veiculo { get; set; } = "";
    public int Entrada { get; set; }
    public int Saida { get; set; }
    public string Movimento { get; set; } = "";
    public string Nome { get; set; } = "";
    public string? Erro { get; set; }
    public double RaioAnel { get; set; }
    public double RaioConexao { get; set; }
    public List<(double X, double Y)> Caminho { get; set; } = new();
    public List<List<(double X, double Y)>> Envoltoria { get; set; } = new();
    public List<List<(double X, double Y)>> Rodas { get; set; } = new();
    public double LarguraVarrida { get; set; }
    /// <summary>Menor distância das rodas aos meios-fios externos e às ilhas divisórias, sem invasão (m).</summary>
    public double FolgaExterna { get; set; } = double.PositiveInfinity;
    /// <summary>Meio-fio externo invadido pelas rodas (m) e área das ilhas divisórias invadida (m²).</summary>
    public double InvasaoExterna { get; set; }
    public double AreaIlhasDivisorias { get; set; }
    /// <summary>Meio(s)-fio(s) onde está a menor folga ou a invasão externa (ex.: "meio-fio da entrada L").</summary>
    public string OndeExterno { get; set; } = "";
    /// <summary>Menor distância das rodas ao meio-fio da ilha central, sem invasão (m), e área da ilha invadida (m²).</summary>
    public double FolgaIlha { get; set; } = double.PositiveInfinity;
    public double AreaIlhaCentral { get; set; }
    /// <summary>Área das rodas sobre a faixa transponível (m²).</summary>
    public double AreaApron { get; set; }
    /// <summary>Menor distância do centro da rotatória às rodas (m).</summary>
    public double RaioMinimoRodas { get; set; } = double.PositiveInfinity;
    public List<(double X, double Y, string Texto)> Problemas { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
    public string Resultado { get; set; } = VerificacaoRotatoria.Aprovado;
}

/// <summary>Visada conferida em 3D: o observador e os pontos que ele precisa ver.</summary>
public sealed class Visada3D
{
    public int Braco { get; set; }
    public string Tipo { get; set; } = "";
    public double Distancia { get; set; }
    public double AlturaOlho { get; set; }
    public double AlturaAlvo { get; set; }
    public (double X, double Y) Observador { get; set; }
    public List<(double X, double Y)> Alvos { get; set; } = new();
    /// <summary>Menor altura da linha de visada acima da superfície (m); negativa = a superfície tapa a visada.</summary>
    public double FolgaMinima { get; set; } = double.PositiveInfinity;
    public (double X, double Y)? PontoCritico { get; set; }
    public (double X, double Y)? AlvoCritico { get; set; }
    public int AlvosTapados { get; set; }
    /// <summary>Fração dos pontos das visadas com superfície (os sobre ilhas não contam).</summary>
    public double Cobertura { get; set; }
    public int SobreIlhas { get; set; }
    public string Obs { get; set; } = "";
    public string Resultado { get; set; } = "";
}

public sealed class AnaliseSeguranca
{
    public string Item { get; set; } = "";
    public List<string> Veiculos { get; } = new();
    public List<AnaliseBraco> Trajetorias { get; } = new();
    public List<Visada> Visadas2D { get; } = new();
    public List<MovimentoGiro> Movimentos { get; } = new();
    public List<Visada3D> Visadas { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<string> Recomendacoes { get; } = new();
    public List<string> Avisos { get; } = new();
    public string Superficies { get; set; } = "";
}

public static class AuditorRotatoria
{
    private const string Aprovado = VerificacaoRotatoria.Aprovado, Reprovado = VerificacaoRotatoria.Reprovado, Atencao = VerificacaoRotatoria.Atencao;

    /// <summary>Distância de visibilidade de parada (AASHTO, em m): 0,278·V·t + 0,039·V²/a, V em km/h.</summary>
    public static double DistanciaParada(double v, double t, double a) => 0.278 * v * t + 0.039 * v * v / a;

    /// <summary>Nome do movimento pela posição da saída no anel (1 = primeira à direita; n = retorno).</summary>
    public static string NomeMovimento(ModeloRotatoria m, int entrada, int j)
    {
        int n = m.Bracos.Count;
        if (j % n == 0) return "retorno";
        if (j == 1) return "à direita";
        if (j == n - 1) return "à esquerda";
        return (entrada + j) % n == TrajetoriaRapida.SaidaDe(m, entrada, Movimento.Direto) ? "em frente" : $"{j}ª saída";
    }

    // ------------------------------------------------------------------------------------------------ meios-fios

    public sealed record Contornos(List<Obstaculo> Externos, Obstaculo IlhaCentral, Obstaculo? Apron, double RaioIlhaCentral,
                                   List<List<(double X, double Y)>> IlhasDivisorias);

    private static V2 V((double X, double Y) p) => new(p.X, p.Y);
    private static (double X, double Y) T(V2 p) => (p.X, p.Y);

    private static List<V2> Circulo(V2 c, double r)
    {
        var p = Geo.PontosArco(c, r, 0, 2 * Math.PI, 0.005);
        p.RemoveAt(p.Count - 1);
        return p;
    }

    /// <summary>
    /// Meios-fios da rotatória como obstáculos: bordo externo do anel entre as aberturas, curvas das conexões, bordos dos ramos
    /// na zona da rotatória (até o fim do alargamento ou da ilha divisória, o que for mais longe, + 5 m; daí para fora a faixa é
    /// a da via, onde a folga de 0,60 m não se aplica), ilhas divisórias (fechadas), ilha central e o meio-fio da faixa transponível.
    /// </summary>
    public static Contornos MeiosFios(ModeloRotatoria m, double apron, double trecho)
    {
        var ext = new List<Obstaculo>();
        int n = m.Bracos.Count;
        var o = new V2(m.Ox, m.Oy);
        for (int i = 0; i < n; i++)
        {
            var a = m.Bracos[i];
            var b = m.Bracos[(i + 1) % n];
            double de = m.Abertura(a).Ate, ate = m.Abertura(b).De;
            double sw = GeometriaRotatoria.Normalizar(ate - de);
            if (sw < 1e-3 || sw > 2 * Math.PI - 1e-3) continue;
            ext.Add(new Obstaculo { Nome = $"bordo externo do anel ({a.Rumo}–{b.Rumo})", Pontos = Geo.PontosArco(o, m.RExt, de, sw, 0.005) });
        }
        var ilhas = new List<List<(double X, double Y)>>();
        foreach (var b in m.Bracos)
        {
            double zona = new[] { b.SRetencao + 20, b.TemIlha ? b.SIlhaFim : 0, double.IsFinite(b.SAlargFim) ? b.SAlargFim : 0 }.Max() + 5;
            double sFim = Math.Min(Math.Min(zona, b.SRetencao + trecho + 10), b.Eixo.Comprimento);
            foreach (bool entrada in new[] { true, false })
            {
                var pts = new List<V2>();
                var mf = entrada ? b.MeioFioEntrada : b.MeioFioSaida;
                double sTg = entrada ? b.STangEntrada : b.STangSaida;
                if (mf is { } c && !double.IsNaN(sTg))
                {
                    var cc = new V2(c.X, c.Y);
                    var dc = cc - o;
                    var tAnel = o + dc * (m.RExt / dc.Comprimento);
                    var tRamo = V(b.Eixo.Ponto(sTg, (entrada ? 1 : -1) * b.Bordo(sTg, entrada)));
                    double a0 = (tAnel - cc).Angulo, a1 = (tRamo - cc).Angulo;
                    pts.AddRange(Geo.PontosArco(cc, c.R, a0, Geo.AngNorm(a1 - a0), 0.005));
                }
                pts.AddRange(m.BordoRamo(b, entrada, sFim).Select(V));
                var limpo = Geo.Limpar(pts);
                if (limpo.Count >= 2) ext.Add(new Obstaculo { Nome = $"meio-fio da {(entrada ? "entrada" : "saída")} {b.Rumo}", Pontos = limpo });
            }
            if (b.TemIlha)
            {
                var il = m.Ilha(b);
                if (il.Count >= 3)
                {
                    ext.Add(new Obstaculo { Nome = $"ilha divisória {b.Rumo}", Pontos = il.Select(V).ToList(), Fechado = true });
                    ilhas.Add(il);
                }
            }
        }
        double rc = apron > 0.01 && apron < m.RIlha - 0.5 ? m.RIlha - apron : m.RIlha;
        var ilha = new Obstaculo { Nome = "ilha central", Pontos = Circulo(o, rc), Fechado = true };
        Obstaculo? ap = rc < m.RIlha - 1e-6 ? new Obstaculo { Nome = "faixa transponível", Pontos = Circulo(o, m.RIlha), Fechado = true } : null;
        return new Contornos(ext, ilha, ap, rc, ilhas);
    }

    // ------------------------------------------------------------------------------------------------ caminho do veículo

    private sealed record Concordancia(V2 Centro, V2 NaReta, V2 NoAnel);

    /// <summary>
    /// Curva de raio <paramref name="r"/> tangente à reta (ponto <paramref name="p0"/>, direção unitária <paramref name="d"/>, virando
    /// à direita) e tangente por fora ao círculo do caminho no anel (centro O, raio <paramref name="rAnel"/>): curva reversa.
    /// </summary>
    private static Concordancia? Concordar(V2 o, double rAnel, V2 p0, V2 d, double r, bool primeira)
    {
        var nr = new V2(d.Y, -d.X);
        var w = p0 + nr * r - o;
        double bq = w.Dot(d), cq = w.Comprimento2 - (rAnel + r) * (rAnel + r);
        double disc = bq * bq - cq;
        if (disc < 0) return null;
        double t = primeira ? -bq - Math.Sqrt(disc) : -bq + Math.Sqrt(disc);
        var q = p0 + d * t + nr * r;
        var dq = q - o;
        return new Concordancia(q, p0 + d * t, o + dq * (rAnel / dq.Comprimento));
    }

    /// <summary>Arco no sentido horário (giro à direita) de a até b, em volta de c.</summary>
    private static List<V2> ArcoHorario(V2 c, double r, V2 a, V2 b)
    {
        double a0 = (a - c).Angulo, a1 = (b - c).Angulo;
        double sw = -GeometriaRotatoria.Normalizar(a0 - a1);
        return Geo.PontosArco(c, r, a0, sw, 0.005);
    }

    /// <summary>
    /// Caminho do eixo dianteiro: meio da faixa de entrada desde <paramref name="trecho"/> antes da retenção, curva à direita de raio
    /// <paramref name="rEntrada"/> para o anel, volta no anel com raio <paramref name="rAnel"/> (anti-horário), curva à direita de raio
    /// <paramref name="rSaida"/> para a faixa de saída e meio dela até <paramref name="trecho"/> além. Null (com o motivo) quando as
    /// curvas não cabem.
    /// </summary>
    public static List<(double X, double Y)>? CaminhoGuia(ModeloRotatoria m, int entrada, int saida, double rAnel, double rEntrada, double rSaida, double trecho, out string? erro)
    {
        erro = null;
        var be = m.Bracos[entrada];
        var bs = m.Bracos[saida];
        var o = new V2(m.Ox, m.Oy);
        double VEnt(double s) => (be.MeiaIlha(s) + be.Bordo(s, true)) / 2;
        double VSai(double s) => -(bs.MeiaIlha(s) + bs.Bordo(s, false)) / 2;
        double fimE = Math.Min(be.SRetencao + trecho, be.Eixo.Comprimento), fimS = Math.Min(bs.SRetencao + trecho, bs.Eixo.Comprimento);

        // entrada: reta pelo meio da faixa junto da retenção, no sentido de quem chega
        double s1 = be.SRetencao + 1, s2 = Math.Min(be.SRetencao + 15, fimE);
        var a = V(be.Eixo.Ponto(s1, VEnt(s1)));
        var dIn = a - V(be.Eixo.Ponto(s2, VEnt(s2)));
        if (dIn.Comprimento < 1) { erro = $"o ramo {be.Rumo} é curto demais"; return null; }
        dIn /= dIn.Comprimento;
        var ce = Concordar(o, rAnel, a, dIn, rEntrada, true);
        // saída: reta pelo meio da faixa de saída, no sentido de quem sai
        double x1 = bs.SRetencao + 1, x2 = Math.Min(bs.SRetencao + 15, fimS);
        var c = V(bs.Eixo.Ponto(x1, VSai(x1)));
        var dOut = V(bs.Eixo.Ponto(x2, VSai(x2))) - c;
        if (dOut.Comprimento < 1) { erro = $"o ramo {bs.Rumo} é curto demais"; return null; }
        dOut /= dOut.Comprimento;
        var cs = Concordar(o, rAnel, c, dOut, rSaida, false);
        if (ce is null) { erro = $"a curva de entrada de raio {rEntrada:0.0} m não liga a faixa {be.Rumo} ao anel"; return null; }
        if (cs is null) { erro = $"a curva de saída de raio {rSaida:0.0} m não liga o anel à faixa {bs.Rumo}"; return null; }

        double t2 = (ce.NoAnel - o).Angulo, t3 = (cs.NoAnel - o).Angulo;
        double sw = GeometriaRotatoria.Normalizar(t3 - t2);
        double esperado = entrada == saida ? 2 * Math.PI : GeometriaRotatoria.Normalizar(bs.Angulo - be.Angulo);
        if (sw > esperado + 0.2) { erro = $"as curvas de entrada ({rEntrada:0.0} m) e de saída ({rSaida:0.0} m) se sobrepõem no anel"; return null; }
        var arcoE = ArcoHorario(ce.Centro, rEntrada, ce.NaReta, ce.NoAnel);
        var arcoS = ArcoHorario(cs.Centro, rSaida, cs.NoAnel, cs.NaReta);
        if (Geo.Comprimento(arcoE) > Math.PI * rEntrada || Geo.Comprimento(arcoS) > Math.PI * rSaida) { erro = "curva de concordância com mais de 180°"; return null; }

        var r = new List<V2>();
        double sT1 = be.Eixo.Projetar(ce.NaReta.X, ce.NaReta.Y).S;
        for (double s = fimE; s > sT1 + 1; s -= 1) r.Add(V(be.Eixo.Ponto(s, VEnt(s))));
        r.AddRange(arcoE);
        if (sw > 1e-4) r.AddRange(Geo.PontosArco(o, rAnel, t2, sw, 0.005));
        r.AddRange(arcoS);
        double sT4 = bs.Eixo.Projetar(cs.NaReta.X, cs.NaReta.Y).S;
        for (double s = sT4 + 1; s <= fimS; s += 1) r.Add(V(bs.Eixo.Ponto(s, VSai(s))));
        return Geo.Reamostrar(Geo.Limpar(r, 0.01), 0.5).Select(T).ToList();
    }

    /// <summary>Nome do obstáculo mais perto do ponto.</summary>
    private static string MaisPerto(V2 q, IEnumerable<Obstaculo> obs)
    {
        string nome = ""; double melhor = double.PositiveInfinity;
        foreach (var o in obs)
        {
            var pts = o.Fechado ? o.Pontos.Append(o.Pontos[0]).ToList() : o.Pontos;
            for (int i = 1; i < pts.Count; i++)
            {
                double d = Geo.DistPontoSegmento(q, pts[i - 1], pts[i]);
                if (d < melhor) { melhor = d; nome = o.Nome; }
            }
        }
        return nome;
    }

    private static (double X, double Y) Meio(IReadOnlyList<V2> p) => p.Count == 0 ? (0, 0) : T(Geo.PontoNaDistancia(p, Geo.Acumulado(p), Geo.Comprimento(p) / 2).P);
    private static (double X, double Y) Centroide(IReadOnlyList<V2> p) => p.Count == 0 ? (0, 0) : (p.Average(q => q.X), p.Average(q => q.Y));

    /// <summary>Simula um movimento: caminho, varredura (PROJETA Giro) e folgas das rodas aos meios-fios.</summary>
    public static MovimentoGiro Simular(ModeloRotatoria m, Contornos k, Veiculo v, int entrada, int j, ParametrosSeguranca p)
    {
        int n = m.Bracos.Count;
        int saida = (entrada + j) % n;
        var mv = new MovimentoGiro
        {
            Veiculo = v.Id, Entrada = entrada, Saida = saida, Movimento = NomeMovimento(m, entrada, j), Nome = $"{m.Bracos[entrada].Rumo}→{m.Bracos[saida].Rumo}",
        };
        double rMin = Autoconducao.RaioMinimo(v, false);
        double rAnel = m.RExt - p.FolgaGuia - v.BitolaDianteira / 2;
        mv.RaioAnel = rAnel;
        if (rAnel < rMin * 1.02)
        {
            mv.Erro = $"o caminho no anel teria raio {rAnel:0.0} m, menor que o mínimo do {v.Id} ({rMin:0.0} m): o veículo não contorna a ilha";
            mv.Resultado = Reprovado;
            return mv;
        }
        // curvas de entrada e de saída paralelas aos meios-fios das conexões (raio do meio-fio + a distância do caminho ao bordo
        // externo no anel), nunca abaixo do raio mínimo do veículo; se não couberem, vão diminuindo
        double afast = m.RExt - rAnel, rLim = 1.05 * rMin;
        double rE0 = Math.Max(rLim, (m.Bracos[entrada].MeioFioEntrada?.R ?? 15) + afast);
        double rS0 = Math.Max(rLim, (m.Bracos[saida].MeioFioSaida?.R ?? 15) + afast);
        List<(double X, double Y)>? cam = null;
        string? erro = null;
        foreach (double f in new[] { 1.0, 0.85, 0.7, 0.55, 0.4 })
        {
            double re = Math.Max(rLim, rE0 * f), rs = Math.Max(rLim, rS0 * f);
            cam = CaminhoGuia(m, entrada, saida, rAnel, re, rs, p.Trecho, out erro);
            if (cam is not null) { mv.RaioConexao = Math.Min(re, rs); break; }
            if (re <= rLim + 1e-9 && rs <= rLim + 1e-9) break;
        }
        if (cam is null || cam.Count < 3)
        {
            mv.Erro = $"não há caminho para o {v.Id}: {erro}";
            mv.Resultado = Reprovado;
            return mv;
        }
        mv.Caminho = cam;
        var pts = cam.Select(V).ToList();
        var man = new Manobra
        {
            Modo = "seguir", Nome = mv.Nome, Veiculo = v, Velocidade = 10, Inicio = pts[0], RumoInicial = (pts[1] - pts[0]).Angulo,
            Trechos = new List<Trecho> { new() { Pontos = pts } },
        };
        var res = Varredura.Simular(man, new OpcoesVarredura { Passo = 0.2, PassoEnvoltoria = 0.5 });
        mv.Envoltoria = res.Envoltoria.Select(q => q.Select(T).ToList()).ToList();
        mv.Rodas = res.Rodas.Select(q => q.Select(T).ToList()).ToList();
        mv.LarguraVarrida = res.LarguraMaxima;
        // a taxa de giro do volante fica de fora: o caminho é feito de retas e arcos (sem curvas de transição) e o salto de curvatura
        // entre eles não é um defeito da rotatória
        foreach (var a in res.Avisos.Where(x => !x.Texto.Contains("volante girando"))) mv.Avisos.Add(a.Texto);
        var rodas = (res.Rodas.Count > 0 ? res.Rodas : res.Envoltoria).Cast<IReadOnlyList<V2>>().ToList();
        if (rodas.Count == 0) { mv.Erro = "a varredura não gerou envoltória"; mv.Resultado = Reprovado; return mv; }
        var o = new V2(m.Ox, m.Oy);
        mv.RaioMinimoRodas = rodas.SelectMany(q => q).Min(q => q.Dist(o));

        var ce = Conflitos.Verificar(rodas, k.Externos, p.FolgaRoda);
        mv.InvasaoExterna = ce.ComprimentoInvadido;
        mv.AreaIlhasDivisorias = ce.AreaInvadida;
        if (ce.TemConflito) mv.FolgaExterna = 0;
        else mv.FolgaExterna = ce.DistanciaMinima;
        // textos curtos: no desenho os problemas do mesmo tipo e do mesmo lugar se juntam (os números ficam na planilha)
        foreach (var x in ce.Invasoes) { var q = Meio(x); mv.Problemas.Add((q.X, q.Y, $"{v.Id}: rodas sobre o meio-fio")); }
        foreach (var x in ce.AreasInvadidas) { var q = Centroide(x); mv.Problemas.Add((q.X, q.Y, $"{v.Id}: rodas sobre a ilha divisória")); }
        if (!ce.TemConflito && ce.DistanciaMinima < p.FolgaRoda)
            mv.Problemas.Add((ce.PontoDistanciaMinima.X, ce.PontoDistanciaMinima.Y, $"{v.Id}: rodas a menos de {p.FolgaRoda:0.00} m do meio-fio"));
        var ondes = ce.Invasoes.Select(x => Geo.PontoNaDistancia(x, Geo.Acumulado(x), Geo.Comprimento(x) / 2).P)
                      .Concat(ce.AreasInvadidas.Select(x => new V2(x.Average(q => q.X), x.Average(q => q.Y)))).ToList();
        if (!ce.TemConflito && double.IsFinite(ce.DistanciaMinima)) ondes.Add(ce.PontoDistanciaMinima);
        mv.OndeExterno = string.Join(", ", ondes.Select(q => MaisPerto(q, k.Externos)).Where(x => x.Length > 0).Distinct());

        var ci = Conflitos.Verificar(rodas, new[] { k.IlhaCentral }, p.FolgaRoda);
        mv.AreaIlhaCentral = ci.AreaInvadida;
        mv.FolgaIlha = ci.TemConflito ? 0 : ci.DistanciaMinima;
        if (ci.TemConflito)
            foreach (var x in ci.AreasInvadidas) { var q = Centroide(x); mv.Problemas.Add((q.X, q.Y, $"{v.Id}: rodas sobre a ilha central")); }
        else if (ci.DistanciaMinima < p.FolgaRoda)
            mv.Problemas.Add((ci.PontoDistanciaMinima.X, ci.PontoDistanciaMinima.Y, $"{v.Id}: rodas a menos de {p.FolgaRoda:0.00} m da ilha central"));
        if (k.Apron is { } ap)
            mv.AreaApron = Math.Max(0, Conflitos.Verificar(rodas, new[] { ap }, 0).AreaInvadida - mv.AreaIlhaCentral);

        bool vp = string.Equals(v.Id, "VP", StringComparison.OrdinalIgnoreCase);
        if (mv.InvasaoExterna > 0.005 || mv.AreaIlhasDivisorias > 0.001 || mv.AreaIlhaCentral > 0.001 || mv.FolgaExterna < p.FolgaRoda - 1e-6 || mv.FolgaIlha < p.FolgaRoda - 1e-6)
            mv.Resultado = Reprovado;
        else if ((vp && mv.AreaApron > 0.05) || mv.Avisos.Count > 0)
            mv.Resultado = Atencao;
        return mv;
    }

    // ------------------------------------------------------------------------------------------------ visibilidade 3D

    /// <summary>
    /// Confere em 3D as linhas de visada do observador (olho a <paramref name="hOlho"/> acima da superfície) até cada alvo (a
    /// <paramref name="hAlvo"/> acima): a linha não pode passar abaixo da superfície. Pontos onde <paramref name="ignorar"/> é
    /// verdadeiro (sobre ilhas) e o primeiro e o último metro de cada linha não contam.
    /// </summary>
    public static Visada3D Conferir(int braco, string tipo, double distancia, (double X, double Y) obs, IReadOnlyList<(double X, double Y)> alvos,
                                    double hOlho, double hAlvo, Func<double, double, double?> z, Func<double, double, bool>? ignorar = null, double passo = 0.5)
    {
        var r = new Visada3D { Braco = braco, Tipo = tipo, Distancia = distancia, Observador = obs, Alvos = alvos.ToList(), AlturaOlho = hOlho, AlturaAlvo = hAlvo };
        if (z(obs.X, obs.Y) is not { } z0)
        {
            r.Resultado = Atencao;
            r.Obs = "a superfície não cobre o observador";
            return r;
        }
        int total = 0, com = 0;
        foreach (var a in alvos)
        {
            if (z(a.X, a.Y) is not { } z1) continue;
            double ya = z0 + hOlho, yb = z1 + hAlvo;
            double l = Math.Sqrt((a.X - obs.X) * (a.X - obs.X) + (a.Y - obs.Y) * (a.Y - obs.Y));
            int nk = Math.Max(2, (int)Math.Ceiling(l / passo));
            bool tapado = false;
            for (int i = 1; i < nk; i++)
            {
                double t = (double)i / nk, sd = t * l;
                if (sd < 1 || l - sd < 1) continue;
                double x = obs.X + t * (a.X - obs.X), y = obs.Y + t * (a.Y - obs.Y);
                total++;
                if (ignorar?.Invoke(x, y) == true) { r.SobreIlhas++; continue; }
                if (z(x, y) is not { } zs) continue;
                com++;
                double folga = ya + t * (yb - ya) - zs;
                if (folga < 0) tapado = true;
                if (folga < r.FolgaMinima) { r.FolgaMinima = folga; r.PontoCritico = (x, y); r.AlvoCritico = a; }
            }
            if (tapado) r.AlvosTapados++;
        }
        int validos = total - r.SobreIlhas;
        r.Cobertura = validos <= 0 ? 0 : (double)com / validos;
        if (double.IsInfinity(r.FolgaMinima)) { r.Resultado = Atencao; r.Obs = "a superfície não cobre as visadas"; }
        else if (r.FolgaMinima < 0) r.Resultado = Reprovado;
        else if (r.Cobertura < 0.8) { r.Resultado = Atencao; r.Obs = $"a superfície cobre só {r.Cobertura * 100:0} % das visadas"; }
        else r.Resultado = Aprovado;
        return r;
    }

    private static List<(double X, double Y)> Amostrar(IReadOnlyList<(double X, double Y)> p, double passo)
    {
        var v = Geo.Reamostrar(p.Select(V).ToList(), passo);
        return v.Select(T).ToList();
    }

    // ------------------------------------------------------------------------------------------------ auditoria

    /// <summary>Auditoria completa da rotatória guardada; <paramref name="z"/> = cota da superfície (null = sem visibilidade 3D).</summary>
    public static AnaliseSeguranca Auditar(string nome, ModeloRotatoriaSinal ms, ParametrosSeguranca p, CriteriosRotatoria crit, IReadOnlyList<Veiculo> veiculos,
                                           Func<double, double, double?>? z, Action<string>? progresso = null)
    {
        var a = new AnaliseSeguranca { Item = nome };
        var m = ms.Modelo();
        int n = m.Bracos.Count;
        if (n < 2) { a.Avisos.Add("A rotatória tem menos de dois braços."); return a; }

        // 1. trajetória mais rápida: velocidade e deflexão de cada entrada
        progresso?.Invoke("Trajetórias mais rápidas (R1 a R5)...");
        for (int i = 0; i < n; i++) a.Trajetorias.Add(TrajetoriaRapida.Analisar(m, i));
        for (int i = 0; i < n; i++)
        {
            int up = (i + n - 1) % n;
            if (VisibilidadeRotatoria.Velocidades(a.Trajetorias, up) is { } vv && VisibilidadeRotatoria.Calcular(m, i, vv.VEntrante, vv.VCirculante) is { } vi)
                a.Visadas2D.Add(vi);
        }
        a.Verificacoes.AddRange(VerificacaoRotatoria.Avaliar(m, a.Trajetorias, crit, a.Visadas2D));

        // 2. giro do veículo de projeto em todos os movimentos
        var k = MeiosFios(m, ms.Apron, p.Trecho);
        foreach (var v in veiculos)
        {
            a.Veiculos.Add($"{v.Id} ({v.Nome})");
            progresso?.Invoke($"Giro do {v.Id} em {n * n} movimentos...");
            for (int e = 0; e < n; e++)
                for (int j = 1; j <= n; j++)
                {
                    MovimentoGiro mv;
                    try { mv = Simular(m, k, v, e, j, p); }
                    catch (Exception ex) { mv = new MovimentoGiro { Veiculo = v.Id, Entrada = e, Saida = (e + j) % n, Movimento = NomeMovimento(m, e, j), Nome = $"{m.Bracos[e].Rumo}→{m.Bracos[(e + j) % n].Rumo}", Erro = ex.Message, Resultado = Reprovado }; }
                    a.Movimentos.Add(mv);
                    a.Verificacoes.Add(VerificacaoGiro(mv, p, ms.Apron));
                }
        }
        Recomendar(a, m, ms, p);

        // 3. visibilidade em 3D
        if (z is null) a.Avisos.Add("Sem superfície escolhida: a visibilidade em 3D não foi conferida.");
        else
        {
            progresso?.Invoke("Visibilidade em 3D...");
            var k2 = k;
            bool Ignorar(double x, double y)
            {
                double dx = x - m.Ox, dy = y - m.Oy;
                if (dx * dx + dy * dy < k2.RaioIlhaCentral * k2.RaioIlhaCentral) return true;
                foreach (var il in k2.IlhasDivisorias) if (Geo.DentroPoligono(new V2(x, y), il.Select(V).ToList())) return true;
                return false;
            }
            double dvp = DistanciaParada(p.VelocidadeAproximacao, p.TempoReacao, p.Desaceleracao);
            for (int i = 0; i < n; i++)
            {
                var b = m.Bracos[i];
                double VEnt(double s) => (b.MeiaIlha(s) + b.Bordo(s, true)) / 2;
                // parada na aproximação: o motorista a DVP da retenção vê a faixa até a retenção
                double so = b.SRetencao + dvp;
                string obs = "";
                if (so > b.Eixo.Comprimento - 1) { so = b.Eixo.Comprimento - 1; obs = $"o eixo do ramo acaba antes: conferido a {so - b.SRetencao:0} m"; }
                var alvos = new List<(double X, double Y)>();
                for (double s = b.SRetencao; s <= so - 5 + 1e-9; s += 5) alvos.Add(b.Eixo.Ponto(s, VEnt(s)));
                var vp = Conferir(i, "parada na aproximação", dvp, b.Eixo.Ponto(so, VEnt(so)), alvos, p.AlturaOlho, p.AlturaObjeto, z, Ignorar);
                if (obs.Length > 0) vp.Obs = (vp.Obs.Length > 0 ? vp.Obs + "; " : "") + obs;
                a.Visadas.Add(vp);
                if (a.Visadas2D.FirstOrDefault(x => x.Braco == i) is { } vi)
                {
                    a.Visadas.Add(Conferir(i, "à esquerda — veículo que entra (d1)", vi.D1, vi.Observador, Amostrar(vi.PernaEntrante, 3), p.AlturaOlho, p.AlturaVeiculo, z, Ignorar));
                    a.Visadas.Add(Conferir(i, "à esquerda — veículo que circula (d2)", vi.D2, vi.Observador, Amostrar(vi.PernaCirculante, 3), p.AlturaOlho, p.AlturaVeiculo, z, Ignorar));
                }
            }
            foreach (var x in a.Visadas) a.Verificacoes.Add(VerificacaoVisada(x, m, p));
        }
        return a;
    }

    private static string F(double v, string f = "0.00") => double.IsInfinity(v) ? "∞" : v.ToString(f);

    public static Verificacao VerificacaoGiro(MovimentoGiro mv, ParametrosSeguranca p, double apron)
    {
        const string fonte = "AASHTO (2011): 0,6 m entre a borda do pneu e o meio-fio; FDOT FDM 213 pede 1,5 ft e TxDOT RDM 14.4.5, 1 ft — referências complementares";
        string valor;
        if (mv.Erro is not null) valor = mv.Erro;
        else
        {
            var partes = new List<string>();
            partes.Add(mv.InvasaoExterna > 0.005 || mv.AreaIlhasDivisorias > 0.001
                ? $"rodas INVADEM {(mv.OndeExterno.Length > 0 ? "o " + mv.OndeExterno : "o meio-fio externo/ilha divisória")} ({mv.InvasaoExterna:0.0} m de meio-fio, {mv.AreaIlhasDivisorias:0.0} m² de ilha)"
                : $"rodas a {F(mv.FolgaExterna)} m do meio-fio externo e das ilhas divisórias" + (mv.OndeExterno.Length > 0 ? $" (a menor no {mv.OndeExterno})" : ""));
            partes.Add(mv.AreaIlhaCentral > 0.001 ? $"rodas INVADEM a ilha central ({mv.AreaIlhaCentral:0.0} m²)" : $"a {F(mv.FolgaIlha)} m da ilha central");
            if (apron > 0.01) partes.Add(mv.AreaApron > 0.05 ? $"usa {mv.AreaApron:0.0} m² da faixa transponível" : "não usa a faixa transponível");
            partes.Add($"largura varrida {mv.LarguraVarrida:0.00} m");
            if (mv.Avisos.Count > 0) partes.Add(string.Join("; ", mv.Avisos));
            valor = string.Join("; ", partes);
        }
        string crit = $"rodas a ≥ {p.FolgaRoda:0.00} m de todo meio-fio, sem invadir" + (string.Equals(mv.Veiculo, "VP", StringComparison.OrdinalIgnoreCase) && apron > 0.01 ? "; o carro não precisa da faixa transponível" : "");
        return new Verificacao($"Giro do {mv.Veiculo} — {mv.Movimento}", mv.Nome, valor, crit, fonte, mv.Resultado);
    }

    public static Verificacao VerificacaoVisada(Visada3D x, ModeloRotatoria m, ParametrosSeguranca p)
    {
        var b = m.Bracos[x.Braco];
        string fonte = x.Tipo.StartsWith("parada")
            ? $"DVP = 0,278·V·t + 0,039·V²/a (AASHTO), V = {p.VelocidadeAproximacao:0} km/h [PREMISSA]; olho {p.AlturaOlho:0.00} m e objeto {p.AlturaObjeto:0.00} m [A CONFIRMAR na norma do órgão]"
            : $"NCHRP 672 6.7.3 (d = 0,278·tc·V, tc = 5 s); olho e veículo a {p.AlturaOlho:0.00} m (AASHTO, 3,5 ft) — referências complementares";
        string valor = double.IsInfinity(x.FolgaMinima)
            ? x.Obs
            : $"{x.Distancia:0.0} m; a visada passa no mínimo {x.FolgaMinima:0.00} m acima da superfície" + (x.FolgaMinima < 0 ? $" — {x.AlvosTapados} de {x.Alvos.Count} alvo(s) tapado(s)" : "") +
              (x.SobreIlhas > 0 ? "; trecho sobre ilhas não medido" : "") + (x.Obs.Length > 0 ? "; " + x.Obs : "");
        return new Verificacao($"Visibilidade em 3D — {x.Tipo}", VerificacaoRotatoria.Nome(b), valor, "linha de visada sempre acima da superfície (talude, corte, aterro)", fonte, x.Resultado);
    }

    /// <summary>Recomendações a partir dos giros: faixa transponível necessária e meios-fios externos invadidos.</summary>
    private static void Recomendar(AnaliseSeguranca a, ModeloRotatoria m, ModeloRotatoriaSinal ms, ParametrosSeguranca p)
    {
        foreach (var g in a.Movimentos.GroupBy(x => x.Veiculo))
        {
            var ilha = g.Where(x => x.Erro is null && (x.AreaIlhaCentral > 0.001 || x.FolgaIlha < p.FolgaRoda - 1e-6)).ToList();
            bool vp = string.Equals(g.Key, "VP", StringComparison.OrdinalIgnoreCase);
            if (ilha.Count > 0)
            {
                double rMin = ilha.Min(x => x.RaioMinimoRodas);
                double rCentral = ms.Apron > 0.01 && ms.Apron < m.RIlha - 0.5 ? m.RIlha - ms.Apron : m.RIlha;
                double precisa = m.RIlha - (rMin - p.FolgaRoda);
                if (vp) a.Recomendacoes.Add($"O carro (VP) chega a menos de {p.FolgaRoda:0.00} m da ilha central em {ilha.Count} movimento(s): reveja a largura do anel e o raio das entradas.");
                else if (precisa >= m.RIlha - 0.5)
                    a.Recomendacoes.Add($"O {g.Key} não cabe no anel: as rodas chegam a {rMin:0.0} m do centro, e a ilha central teria de ter menos de {Math.Max(0, rMin - p.FolgaRoda):0.0} m de raio — aumente o DCI ou adote outro veículo de projeto.");
                else
                    a.Recomendacoes.Add($"O {g.Key} invade ou encosta na ilha central em {ilha.Count} movimento(s) (rodas a {rMin:0.0} m do centro; ilha com {rCentral:0.0} m de raio): faça a faixa transponível com pelo menos {Math.Ceiling(precisa * 10) / 10:0.0} m de largura (meio-fio montável a {m.RIlha:0.0} m do centro) e a ilha com {Math.Max(0, rMin - p.FolgaRoda):0.0} m de raio.");
            }
            var ext = g.Where(x => x.Erro is null && (x.InvasaoExterna > 0.005 || x.AreaIlhasDivisorias > 0.001 || x.FolgaExterna < p.FolgaRoda - 1e-6)).ToList();
            if (ext.Count > 0)
                a.Recomendacoes.Add($"O {g.Key} passa a menos de {p.FolgaRoda:0.00} m do meio-fio externo ou das ilhas divisórias em {ext.Count} movimento(s) ({string.Join(", ", ext.Select(x => $"{x.Nome} {x.Movimento}").Distinct().Take(6))}): abra o raio da conexão, alargue a entrada/saída ou recue a ponta da ilha divisória.");
            var err = g.Where(x => x.Erro is not null).ToList();
            if (err.Count > 0) a.Recomendacoes.Add($"{g.Key}: {err.Count} movimento(s) sem caminho possível ({err[0].Erro}).");
            if (vp && ms.Apron > 0.01 && g.Any(x => x.AreaApron > 0.05))
                a.Recomendacoes.Add("O carro (VP) usa a faixa transponível: ela deve ser só para os veículos longos; reveja a largura do anel.");
        }
    }
}

/// <summary>Planilha do PRJSEGURANCA.</summary>
public sealed class RelatorioSeguranca
{
    public string Titulo { get; set; } = "Auditoria de segurança da rotatória";
    public string Desenho { get; set; } = "";
    public AnaliseSeguranca Analise { get; set; } = new();
    public ModeloRotatoria? Modelo { get; set; }
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<string> Pendencias { get; } = new();

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";

    public static List<ParametroRotatoria> ParametrosDe(ParametrosSeguranca p, CriteriosRotatoria c, string veiculos, string superficies)
    {
        string pre = p.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA]";
        return new List<ParametroRotatoria>
        {
            new("Veículo(s) de projeto", veiculos, "", "biblioteca do PROJETA Giro (DNIT IPR-718: VP, CO, O, SR, RE) — escolha conforme o órgão"),
            new("Folga mínima roda – meio-fio", $"{p.FolgaRoda:0.00}", "m", "AASHTO (2011), 0,6 m entre a borda do pneu e o meio-fio; FDOT 1,5 ft e TxDOT 1 ft — referências complementares"),
            new("Condução no anel", $"roda dianteira externa a {p.FolgaGuia:0.00} m do meio-fio externo", "", $"{pre} caminho padrão; confira os críticos no PROJETA Giro"),
            new("Curvas de entrada e saída do caminho", "paralelas ao meio-fio da conexão (raio do meio-fio + afastamento do caminho no anel), reduzidas se não couberem, nunca abaixo de 1,05× o raio mínimo do eixo dianteiro", "", "[PREMISSA]"),
            new("Velocidade de manobra", "10", "km/h", "PROJETA Giro [PREMISSA]; a taxa de giro do volante não é conferida (caminho de retas e arcos, sem transição)"),
            new("Velocidade máxima de entrada V1", $"{c.VelMaxEntrada:0}", "km/h", "NCHRP 672 / TxDOT RDM 14.4.5 — referência complementar"),
            new("Diferença máxima V1 × V2 e V1 × V4", $"{c.DifVelMax:0}", "km/h", "referência complementar (ver relatório do PRJROTATORIA)"),
            new("Distância de visibilidade de parada", "0,278·V·t + 0,039·V²/a", "m", "AASHTO (SSD = 1,47·V·t + 1,075·V²/a em pés) — referência complementar"),
            new("Velocidade de aproximação", $"{p.VelocidadeAproximacao:0}", "km/h", pre),
            new("Tempo de percepção e reação / desaceleração", $"{p.TempoReacao:0.0} s / {p.Desaceleracao:0.0} m/s²", "", "AASHTO [A CONFIRMAR]"),
            new("Altura do olho", $"{p.AlturaOlho:0.00}", "m", "AASHTO 3,5 ft [A CONFIRMAR na norma do órgão; o DNIT usa 1,10 m]"),
            new("Altura do objeto (parada)", $"{p.AlturaObjeto:0.00}", "m", "AASHTO 2 ft [A CONFIRMAR; o DNIT usa 0,15 m]"),
            new("Altura do veículo (visibilidade à esquerda)", $"{p.AlturaVeiculo:0.00}", "m", "AASHTO 3,5 ft (olho e objeto na visibilidade de interseção)"),
            new("Visibilidade à esquerda", "d = 0,278·tc·V, tc = 5 s, observador a 15 m da retenção", "", "NCHRP 672, 6.7.3"),
            new("Superfícies da visibilidade 3D", superficies, "", "escolhidas no comando (projeto onde houver; terreno no resto)"),
        };
    }

    public static IEnumerable<string> PendenciasPadrao() => new[]
    {
        "O caminho do veículo é uma condução padrão (meio da faixa na entrada e na saída, roda dianteira externa a 1 m do meio-fio externo no anel); confira os movimentos críticos com o PROJETA Giro (GIROSEGUIR) no caminho que o motorista faria.",
        "Ilha central e ilhas divisórias não entram na visibilidade 3D: paisagismo, muros, placas, postes e vegetação no triângulo de visibilidade são conferidos no projeto e em campo.",
        "Visibilidade de parada na pista anelar e até as travessias de pedestres na saída (NCHRP 672, 6.7.2) não está incluída.",
        "Veículos fora da biblioteca (bitrem, rodotrem, ônibus articulado): crie no PROJETA Giro (GIROVEICULO) e rode de novo.",
        "Critérios de referência complementar (NCHRP 672 / AASHTO / DOTs americanos): confirme na norma do órgão (DNIT, DER, prefeitura).",
    };

    public PastaXlsx Planilha()
    {
        var a = Analise;
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n1 = new EstiloCelula { Formato = "0.0" };
        var n2 = new EstiloCelula { Formato = "0.00" };
        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };
        void Num(FolhaXlsx f, int l, int c, double v, EstiloCelula e) { if (double.IsNaN(v) || double.IsInfinity(v)) f.Texto(l, c, "-", txt); else f.Numero(l, c, v, e); }
        string Rumo(int i) => Modelo is { } m && i >= 0 && i < m.Bracos.Count ? VerificacaoRotatoria.Nome(m.Bracos[i]) : i.ToString();

        var ver = a.Verificacoes;
        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string x, string y) { fr.Texto(lr, 1, x, txt with { Negrito = true }); fr.Texto(lr, 2, y, q); lr++; }
        Par("Rotatória", a.Item);
        Par("Veículos", string.Join(", ", a.Veiculos));
        Par("Verificações", $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} REPROVADAS, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção");
        foreach (var g in new[] { ("Trajetória e geometria", (Func<Verificacao, bool>)(v => !v.Item.StartsWith("Giro") && !v.Item.StartsWith("Visibilidade em 3D"))),
                                  ("Giro do veículo de projeto", v => v.Item.StartsWith("Giro")), ("Visibilidade em 3D", v => v.Item.StartsWith("Visibilidade em 3D")) })
        {
            var l = ver.Where(g.Item2).ToList();
            Par(g.Item1, $"{l.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovadas, {l.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} reprovadas, {l.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção");
        }
        foreach (var r in a.Recomendacoes) Par("Recomendação", r);
        foreach (var av in a.Avisos) Par("Aviso", av);
        Par("Superfícies", a.Superficies);
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 28); fr.Largura(2, 110);

        var ft = pasta.NovaFolha("Trajetórias");
        Cab(ft, 1, ("Braço", 18), ("R1 (m)", 8), ("V1 (km/h)", 9), ("R2 (m)", 8), ("V2 (km/h)", 9), ("R3 (m)", 8), ("V3 (km/h)", 9), ("R4 (m)", 8), ("V4 (km/h)", 9),
            ("R5 (m)", 8), ("V5 (km/h)", 9), ("Problemas", 60));
        int lt = 2;
        foreach (var an in a.Trajetorias)
        {
            ft.Texto(lt, 1, Rumo(an.Braco), txt);
            int c = 2;
            foreach (var rc in new[] { an.R1, an.R2, an.R3, an.R4, an.R5 })
            {
                if (rc is null) { ft.Texto(lt, c++, "-", txt); ft.Texto(lt, c++, "-", txt); }
                else { ft.Numero(lt, c++, rc.Raio, n1); ft.Numero(lt, c++, rc.Velocidade, n1); }
            }
            ft.Texto(lt, c, string.Join("; ", an.Problemas), q);
            lt++;
        }

        var fg = pasta.NovaFolha("Giro");
        Cab(fg, 1, ("Veículo", 9), ("Movimento", 13), ("De → para", 12), ("Raio no anel (m)", 9), ("Raio das curvas (m)", 9), ("Largura varrida (m)", 9),
            ("Folga ao meio-fio externo (m)", 11), ("Meio-fio invadido (m)", 9), ("Ilha divisória invadida (m²)", 10), ("Folga à ilha central (m)", 10),
            ("Ilha central invadida (m²)", 10), ("Faixa transponível usada (m²)", 10), ("Menor raio das rodas (m)", 10), ("Onde / avisos", 50), ("Resultado", 13));
        int lg = 2;
        foreach (var mv in a.Movimentos)
        {
            fg.Texto(lg, 1, mv.Veiculo, txt); fg.Texto(lg, 2, mv.Movimento, txt); fg.Texto(lg, 3, mv.Nome, txt);
            Num(fg, lg, 4, mv.RaioAnel, n1); Num(fg, lg, 5, mv.RaioConexao, n1); Num(fg, lg, 6, mv.LarguraVarrida, n2);
            Num(fg, lg, 7, mv.FolgaExterna, n2); Num(fg, lg, 8, mv.InvasaoExterna, n2); Num(fg, lg, 9, mv.AreaIlhasDivisorias, n2);
            Num(fg, lg, 10, mv.FolgaIlha, n2); Num(fg, lg, 11, mv.AreaIlhaCentral, n2); Num(fg, lg, 12, mv.AreaApron, n2); Num(fg, lg, 13, mv.RaioMinimoRodas, n2);
            fg.Texto(lg, 14, mv.Erro ?? string.Join("; ", new[] { mv.OndeExterno.Length > 0 ? "externo: " + mv.OndeExterno : "" }.Concat(mv.Avisos).Where(x => x.Length > 0)), q);
            fg.Texto(lg, 15, mv.Resultado, Res(mv.Resultado));
            lg++;
        }

        if (a.Visadas.Count > 0)
        {
            var fv3 = pasta.NovaFolha("Visibilidade 3D");
            Cab(fv3, 1, ("Braço", 18), ("Visada", 34), ("Distância (m)", 10), ("Olho (m)", 8), ("Alvo (m)", 8), ("Alvos", 7), ("Alvos tapados", 9),
                ("Menor folga acima da superfície (m)", 12), ("Cobertura da superfície", 10), ("Observação", 44), ("Resultado", 13));
            int lv = 2;
            foreach (var x in a.Visadas)
            {
                fv3.Texto(lv, 1, Rumo(x.Braco), txt); fv3.Texto(lv, 2, x.Tipo, q); Num(fv3, lv, 3, x.Distancia, n1); Num(fv3, lv, 4, x.AlturaOlho, n2); Num(fv3, lv, 5, x.AlturaAlvo, n2);
                fv3.Numero(lv, 6, x.Alvos.Count, txt); fv3.Numero(lv, 7, x.AlvosTapados, txt); Num(fv3, lv, 8, x.FolgaMinima, n2);
                fv3.Numero(lv, 9, x.Cobertura, txt with { Formato = "0%" }); fv3.Texto(lv, 10, x.Obs, q); fv3.Texto(lv, 11, x.Resultado, Res(x.Resultado));
                lv++;
            }
        }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 32), ("Onde", 22), ("Valor", 70), ("Critério", 34), ("Fonte", 50), ("Resultado", 13));
        int lw = 2;
        foreach (var v in ver) { fv.Texto(lw, 1, v.Item, q); fv.Texto(lw, 2, v.Braco, q); fv.Texto(lw, 3, v.Valor, q); fv.Texto(lw, 4, v.Criterio, q); fv.Texto(lw, 5, v.Fonte, q); fv.Texto(lw, 6, v.Resultado, Res(v.Resultado)); lw++; }

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 40), ("Valor", 44), ("Unidade", 9), ("Fonte", 70));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, q); fp.Texto(lp, 2, p.Valor, q); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        var fd = pasta.NovaFolha("Pendências");
        Cab(fd, 1, ("Pendência", 120));
        int ld = 2;
        foreach (var p in Pendencias) { fd.Texto(ld, 1, p, q); ld++; }
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
