// Apagar PIV de um greide: geometria pura (sem Civil 3D), testada em Projeta.Core.Testes.
//
// O greide é lido como a lista de PIVs (estaca, cota) com a curva vertical de cada um. Apagar um PIV
// liga os vizinhos por uma rampa só; as curvas dos vizinhos ficam como estavam se couberem nas novas
// rampas e são reduzidas até caber se não couberem. As demais curvas não mudam.
// Sem dependências além do .NET: o mesmo arquivo entra no plugin avulso (NETLOAD) do PRJAPAGARPIV.
namespace Projeta.Core.Alinhamentos;

public enum TipoCurvaVertical { Parabola, ParabolaAssimetrica, Circular }

/// <summary>
/// Curva vertical de um PIV. Parábola: L1 = L2 = L/2. Assimétrica: L1 antes e L2 depois do PIV.
/// Circular: Raio (as projeções saem do raio e da variação de rampa).
/// </summary>
public sealed record CurvaVertical(TipoCurvaVertical Tipo, double L1 = 0, double L2 = 0, double Raio = 0)
{
    public static CurvaVertical Parabola(double comprimento) => new(TipoCurvaVertical.Parabola, comprimento / 2, comprimento / 2);
    public static CurvaVertical Assimetrica(double l1, double l2) => new(TipoCurvaVertical.ParabolaAssimetrica, l1, l2);
    public static CurvaVertical Circular(double raio) => new(TipoCurvaVertical.Circular, Raio: raio);

    /// <summary>Extensão horizontal da curva antes do PIV, para as rampas gi (chegada) e gf (saída), em m/m.</summary>
    public double Antes(double gi, double gf) => Tipo == TipoCurvaVertical.Circular ? TangenteCircular(gi, gf) : L1;

    /// <summary>Extensão horizontal da curva depois do PIV.</summary>
    public double Depois(double gi, double gf) => Tipo == TipoCurvaVertical.Circular ? TangenteCircular(gi, gf) : L2;

    private double TangenteCircular(double gi, double gf) => Raio * Math.Tan(Math.Abs(Math.Atan(gf) - Math.Atan(gi)) / 2);

    public CurvaVertical Escalada(double f) => this with { L1 = L1 * f, L2 = L2 * f, Raio = Raio * f };

    /// <summary>Curva pequena demais para valer a pena (vira PIV sem curva).</summary>
    public bool Desprezivel => Tipo == TipoCurvaVertical.Circular ? Raio < 1.0 : L1 + L2 < 1.0;

    public string Descricao => Tipo switch
    {
        TipoCurvaVertical.Parabola => $"parábola L = {L1 + L2:0.##} m",
        TipoCurvaVertical.ParabolaAssimetrica => $"parábola assimétrica L1 = {L1:0.##} m, L2 = {L2:0.##} m",
        _ => $"curva circular R = {Raio:0.##} m",
    };
}

/// <summary>Greide como lista de PIVs (com o início e o fim) e a curva vertical de cada PIV.</summary>
public sealed class PlanoGreide
{
    public List<double> Estacas { get; } = new();
    public List<double> Cotas { get; } = new();
    public List<CurvaVertical?> Curvas { get; } = new();

    public int Count => Estacas.Count;

    public void Adicionar(double estaca, double cota, CurvaVertical? curva = null)
    {
        Estacas.Add(estaca);
        Cotas.Add(cota);
        Curvas.Add(curva);
    }

    public void Remover(int k)
    {
        Estacas.RemoveAt(k);
        Cotas.RemoveAt(k);
        Curvas.RemoveAt(k);
    }

    public PlanoGreide Copia()
    {
        var p = new PlanoGreide();
        for (int i = 0; i < Count; i++) p.Adicionar(Estacas[i], Cotas[i], Curvas[i]);
        return p;
    }

    /// <summary>Rampa do segmento s (entre o PIV s e o s+1), em m/m.</summary>
    public double Rampa(int s) => (Cotas[s + 1] - Cotas[s]) / (Estacas[s + 1] - Estacas[s]);

    public double Antes(int k) => k >= 1 && k <= Count - 2 && Curvas[k] is { } c ? c.Antes(Rampa(k - 1), Rampa(k)) : 0.0;

    public double Depois(int k) => k >= 1 && k <= Count - 2 && Curvas[k] is { } c ? c.Depois(Rampa(k - 1), Rampa(k)) : 0.0;

    /// <summary>Confere se as estacas crescem (greide contínuo, um PIV por estaca).</summary>
    public void Validar()
    {
        if (Count < 2) throw new InvalidOperationException("O greide precisa de pelo menos dois PIVs.");
        for (int i = 0; i < Count - 1; i++)
            if (Estacas[i + 1] - Estacas[i] < 1e-6)
                throw new InvalidOperationException($"PIVs fora de ordem ou repetidos perto da estaca {Estacas[i]:0.000}.");
    }
}

public static class GreidePVIs
{
    /// <summary>
    /// Remove o PIV k. As curvas dos dois PIVs vizinhos mantêm o comprimento (ou o raio) se couberem
    /// nas novas rampas; se não couberem, são reduzidas até caber. As outras curvas não mudam.
    /// O que mudou vai para <paramref name="log"/>.
    /// </summary>
    public static PlanoGreide Apagar(PlanoGreide origem, int k, List<string> log)
    {
        origem.Validar();
        if (k <= 0 || k >= origem.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(k), "Só PIVs internos podem ser apagados (o início e o fim do greide ficam).");
        var p = origem.Copia();
        p.Remover(k);

        // PIV que ficou com a mesma rampa antes e depois também sai
        for (int i = 1; i < p.Count - 1; i++)
        {
            if (Math.Abs(p.Rampa(i - 1) - p.Rampa(i)) < 1e-9)
            {
                log.Add($"o PIV da estaca {p.Estacas[i]:0.000} ficou na mesma rampa e também saiu");
                p.Remover(i);
                i--;
            }
        }

        var vizinhos = new[] { origem.Estacas[k - 1], origem.Estacas[k + 1] };
        bool EhVizinho(double est) => vizinhos.Any(v => Math.Abs(v - est) < 1e-6);

        for (int iter = 0; iter < 500; iter++)
        {
            int pior = -1;
            double razao = 1.0;
            for (int s = 0; s < p.Count - 1; s++)
            {
                double disp = p.Estacas[s + 1] - p.Estacas[s];
                double usa = p.Depois(s) + p.Antes(s + 1);
                if (usa > disp + 1e-6 && disp / usa < razao) { razao = disp / usa; pior = s; }
            }
            if (pior < 0) break;
            bool mexeu = false;
            foreach (int i in new[] { pior, pior + 1 })
            {
                if (i < 1 || i > p.Count - 2 || p.Curvas[i] is not { } c || !EhVizinho(p.Estacas[i])) continue;
                var nova = c.Escalada(Math.Max(0.5, razao * 0.999));
                p.Curvas[i] = nova.Desprezivel ? null : nova;
                mexeu = true;
            }
            if (!mexeu)
            {
                log.Add("aviso: há curvas verticais que não cabem entre PIVs que não são vizinhos do apagado (já era assim antes)");
                break;
            }
        }

        foreach (var v in vizinhos)
        {
            int io = origem.Estacas.FindIndex(x => Math.Abs(x - v) < 1e-6);
            int i = p.Estacas.FindIndex(x => Math.Abs(x - v) < 1e-6);
            if (io < 0 || i < 0) continue;
            var antes = origem.Curvas[io];
            var depois = p.Curvas[i];
            if (antes == depois || antes is null) continue;
            log.Add(depois is null
                ? $"PIV da estaca {v:0.000}: curva vertical removida (não cabia nas novas rampas)"
                : $"PIV da estaca {v:0.000}: {antes.Descricao} → {depois.Descricao} (não cabia nas novas rampas)");
        }
        return p;
    }
}
