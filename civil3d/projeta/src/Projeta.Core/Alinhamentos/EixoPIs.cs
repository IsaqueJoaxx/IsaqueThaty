// Apagar PI de um alinhamento: geometria pura (sem Civil 3D), testada em Projeta.Core.Testes.
//
// O eixo é lido como uma sequência de elementos (reta, arco, espiral) na ordem do estaqueamento.
// Os PIs são as interseções das tangentes consecutivas; o grupo de curvas entre duas tangentes
// vira a curva daquele PI (sem curva, arco simples ou espiral-curva-espiral).
using Projeta.Core.Comum;

namespace Projeta.Core.Alinhamentos;

public enum TipoElementoEixo { Reta, Arco, Espiral }

/// <summary>Subelemento do alinhamento como o Civil 3D entrega (TipoEspiral = SpiralType do Civil 3D).</summary>
public sealed record ElementoEixo(TipoElementoEixo Tipo, P2 Ini, P2 Fim, double Comprimento,
                                  double Raio = 0, int TipoEspiral = 0);

public enum TipoCurvaPI { Arco, EspiralCurvaEspiral }

public sealed record CurvaPI(TipoCurvaPI Tipo, double Raio, double L1 = 0, double L2 = 0, int TipoEspiral = 0)
{
    /// <summary>Distância do PI ao início da curva (TS/PC) para a deflexão dada, em rad.</summary>
    public double Tangente(double deflexao)
    {
        double t = Math.Tan(Math.Abs(deflexao) / 2);
        if (Tipo == TipoCurvaPI.Arco) return Raio * t;
        double L = Math.Max(L1, L2); // conservador para espirais assimétricas
        double p = L * L / (24 * Raio);
        double k = L / 2 - L * L * L / (240 * Raio * Raio);
        return (Raio + p) * t + k;
    }

    public CurvaPI Escalada(double f) => this with { Raio = Raio * f, L1 = L1 * f, L2 = L2 * f };

    public string Descricao => Tipo == TipoCurvaPI.Arco
        ? $"curva R = {Raio:0.###} m"
        : $"espiral-curva-espiral R = {Raio:0.###} m, Le = {L1:0.##} / {L2:0.##} m";
}

/// <summary>Eixo como lista de PIs (com as pontas) e a curva de cada PI (null nas pontas e em PI sem curva).</summary>
public sealed class PlanoPIs
{
    public List<P2> PIs { get; } = new();
    public List<CurvaPI?> Curvas { get; } = new();

    public PlanoPIs Copia()
    {
        var p = new PlanoPIs();
        p.PIs.AddRange(PIs);
        p.Curvas.AddRange(Curvas);
        return p;
    }

    /// <summary>Deflexão com sinal no PI k (rad), + = à esquerda.</summary>
    public double Deflexao(int k)
    {
        var a = (PIs[k] - PIs[k - 1]).Unitario();
        var b = (PIs[k + 1] - PIs[k]).Unitario();
        return Math.Atan2(a.Cross(b), a.Dot(b));
    }

    public double Tangente(int k) =>
        k >= 1 && k <= PIs.Count - 2 && Curvas[k] is { } c ? c.Tangente(Deflexao(k)) : 0.0;

    /// <summary>PI interno mais próximo do ponto (-1 se não houver).</summary>
    public int MaisProximo(P2 p)
    {
        int melhor = -1;
        double d = double.MaxValue;
        for (int k = 1; k < PIs.Count - 1; k++)
        {
            double dk = p.Distancia(PIs[k]);
            if (dk < d) { d = dk; melhor = k; }
        }
        return melhor;
    }
}

public static class EixoPIs
{
    private const double TolColinear = 1e-9;

    public static PlanoPIs Extrair(IReadOnlyList<ElementoEixo> elementos)
    {
        if (elementos.Count == 0)
            throw new InvalidOperationException("O alinhamento não tem elementos.");
        if (elementos[0].Tipo != TipoElementoEixo.Reta || elementos[^1].Tipo != TipoElementoEixo.Reta)
            throw new InvalidOperationException(
                "O eixo começa ou termina em curva/espiral; esta versão só trabalha com eixos que começam e terminam em tangente.");

        var tang = new List<(P2 Ini, P2 Fim)>();
        var grupos = new List<List<ElementoEixo>>();
        var atual = new List<ElementoEixo>();
        foreach (var t in elementos)
        {
            if (t.Tipo != TipoElementoEixo.Reta) { atual.Add(t); continue; }
            if (t.Ini.Distancia(t.Fim) < 1e-9) continue; // reta de comprimento zero
            if (tang.Count > 0)
            {
                var (ai, af) = tang[^1];
                var da = (af - ai).Unitario();
                var db = (t.Fim - t.Ini).Unitario();
                if (atual.Count == 0 && Math.Abs(da.Cross(db)) < TolColinear && da.Dot(db) > 0)
                {
                    tang[^1] = (ai, t.Fim); // retas colineares (vértice sobrando) viram uma só
                    continue;
                }
                grupos.Add(atual);
                atual = new List<ElementoEixo>();
            }
            tang.Add((t.Ini, t.Fim));
        }
        if (tang.Count == 0)
            throw new InvalidOperationException("O alinhamento não tem tangentes com comprimento.");

        var plano = new PlanoPIs();
        plano.PIs.Add(tang[0].Ini);
        plano.Curvas.Add(null);
        for (int i = 0; i < grupos.Count; i++)
        {
            var (a0, a1) = tang[i];
            var (b0, b1) = tang[i + 1];
            var pi = Intersecao(a0, a1 - a0, b0, b1 - b0)
                     ?? throw new InvalidOperationException(
                         $"As tangentes do PI {i + 1} são paralelas (curva reversa sem tangente entre elas?); esta versão não refaz esse caso.");
            plano.PIs.Add(pi);
            plano.Curvas.Add(CurvaDoGrupo(grupos[i], i + 1));
        }
        plano.PIs.Add(tang[^1].Fim);
        plano.Curvas.Add(null);
        return plano;
    }

    private static CurvaPI? CurvaDoGrupo(List<ElementoEixo> g, int indicePI)
    {
        if (g.Count == 0) return null;
        if (g.Count == 1 && g[0].Tipo == TipoElementoEixo.Arco)
            return new CurvaPI(TipoCurvaPI.Arco, g[0].Raio);
        if (g.Count == 3 && g[0].Tipo == TipoElementoEixo.Espiral && g[1].Tipo == TipoElementoEixo.Arco && g[2].Tipo == TipoElementoEixo.Espiral)
            return new CurvaPI(TipoCurvaPI.EspiralCurvaEspiral, g[1].Raio, g[0].Comprimento, g[2].Comprimento, g[0].TipoEspiral);
        throw new InvalidOperationException(
            $"No PI {indicePI} há {string.Join(" + ", g.Select(t => t.Tipo.ToString().ToLowerInvariant()))} sem tangente entre eles; " +
            "esta versão só refaz curva simples ou espiral-curva-espiral.");
    }

    private static P2? Intersecao(P2 p, P2 d, P2 q, P2 e)
    {
        double den = d.Cross(e);
        if (Math.Abs(den) < 1e-12 * d.Comprimento * e.Comprimento) return null;
        double t = (q - p).Cross(e) / den;
        return p + d * t;
    }

    /// <summary>
    /// Remove o PI k. As curvas dos dois PIs vizinhos mantêm o raio (e as espirais) se couberem
    /// na nova tangente; se não couberem, são reduzidas até caber. As outras curvas não mudam.
    /// O que mudou vai para <paramref name="log"/>.
    /// </summary>
    public static PlanoPIs Apagar(PlanoPIs origem, int k, List<string> log)
    {
        if (k <= 0 || k >= origem.PIs.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(k), "Só PIs internos podem ser apagados.");
        var p = origem.Copia();
        p.PIs.RemoveAt(k);
        p.Curvas.RemoveAt(k);

        // PI vizinho que ficou alinhado (deflexão ~0) também sai
        for (int i = 1; i < p.PIs.Count - 1; i++)
        {
            if (Math.Abs(p.Deflexao(i)) < 1e-7)
            {
                log.Add($"o PI em E={p.PIs[i].X:0.000}, N={p.PIs[i].Y:0.000} ficou alinhado com a nova tangente e também saiu");
                p.PIs.RemoveAt(i);
                p.Curvas.RemoveAt(i);
                i--;
            }
        }

        var vizinhos = new[] { origem.PIs[k - 1], origem.PIs[k + 1] };
        bool EhVizinho(P2 v) => vizinhos.Any(w => w.Distancia(v) < 1e-9);

        for (int iter = 0; iter < 500; iter++)
        {
            int pior = -1;
            double razao = 1.0;
            for (int s = 0; s < p.PIs.Count - 1; s++)
            {
                double disp = p.PIs[s].Distancia(p.PIs[s + 1]);
                double usa = p.Tangente(s) + p.Tangente(s + 1);
                if (usa > disp + 1e-6 && disp / usa < razao) { razao = disp / usa; pior = s; }
            }
            if (pior < 0) break;
            bool mexeu = false;
            foreach (int i in new[] { pior, pior + 1 })
            {
                if (i < 1 || i > p.PIs.Count - 2 || p.Curvas[i] is not { } c || !EhVizinho(p.PIs[i])) continue;
                var nova = c.Escalada(Math.Max(0.5, razao * 0.999));
                p.Curvas[i] = nova.Raio < 1.0 ? null : nova;
                mexeu = true;
            }
            if (!mexeu)
            {
                log.Add("aviso: há um trecho com tangente menor que as curvas que não é vizinho do PI apagado (já era assim antes)");
                break;
            }
        }

        foreach (var v in vizinhos)
        {
            int io = origem.PIs.FindIndex(x => x.Distancia(v) < 1e-9);
            int i = p.PIs.FindIndex(x => x.Distancia(v) < 1e-9);
            if (io < 0 || i < 0) continue;
            var antes = origem.Curvas[io];
            var depois = p.Curvas[i];
            if (antes == depois || antes is null) continue;
            log.Add(depois is null
                ? $"PI em E={v.X:0.000}, N={v.Y:0.000}: curva removida (não cabia na nova tangente)"
                : $"PI em E={v.X:0.000}, N={v.Y:0.000}: R {antes.Raio:0.###} → {depois.Raio:0.###} m (não cabia na nova tangente)");
        }
        return p;
    }
}
