using Projeta.Core.Comum;
using Projeta.Core.Relatorios;

namespace Projeta.Core.Superficies;

/// <summary>Uma faixa do mapa de cores de uma superfície (cota em m ou declividade em %).</summary>
public sealed class FaixaMapa
{
    public double De { get; set; }
    public double Ate { get; set; } = double.PositiveInfinity;
    /// <summary>Cor RRGGBB.</summary>
    public string Cor { get; set; } = "808080";
    /// <summary>Nome da classe (ex.: "Suave ondulado"); vazio = só o intervalo.</summary>
    public string Classe { get; set; } = "";
    /// <summary>Área em planta (m²) calculada.</summary>
    public double Area { get; set; }
}

/// <summary>
/// Faixas de cota e de declividade de uma superfície TIN com as áreas EXATAS de cada faixa: cada triângulo é cortado
/// pelos planos das cotas-limite (cota) ou cai inteiro numa classe pela sua inclinação (declividade).
/// </summary>
public static class MapaFaixas
{
    /// <summary>Classes de relevo da EMBRAPA (declividade %).</summary>
    public static readonly (double Ate, string Nome)[] Embrapa =
    {
        (3, "Plano"), (8, "Suave ondulado"), (20, "Ondulado"), (45, "Forte ondulado"), (75, "Montanhoso"), (double.PositiveInfinity, "Escarpado"),
    };

    /// <summary>Faixas de cota com passo "redondo" (1, 2, 2,5, 5 × 10ⁿ) para cerca de <paramref name="quantidade"/> faixas.</summary>
    public static List<FaixaMapa> Cotas(double zmin, double zmax, int quantidade = 8)
    {
        if (!(zmax > zmin)) zmax = zmin + 1;
        quantidade = Math.Max(1, quantidade);
        // passo "redondo" que dá o número de faixas mais perto do pedido
        double bruto = (zmax - zmin) / quantidade;
        double p10 = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
        double melhor = PassoRedondo(bruto);
        int dif = int.MaxValue;
        foreach (var k in new[] { 0.1, 1, 10 })
            foreach (var m in new[] { 1, 2, 2.5, 5 })
            {
                double passo = m * k * p10;
                int n = (int)Math.Ceiling(zmax / passo - 1e-9) - (int)Math.Floor(zmin / passo + 1e-9);
                if (n > 200 || n < 1) continue;
                int d = Math.Abs(n - quantidade);
                if (d < dif || (d == dif && passo > melhor)) { dif = d; melhor = passo; }
            }
        return CotasComPasso(zmin, zmax, melhor);
    }

    public static List<FaixaMapa> CotasComPasso(double zmin, double zmax, double passo)
    {
        if (!(passo > 0)) throw new ArgumentException("O passo das faixas deve ser positivo.");
        if ((zmax - zmin) / passo > 200) throw new ArgumentException("Passo pequeno demais: mais de 200 faixas.");
        double ini = Math.Floor(zmin / passo + 1e-9) * passo;
        var l = new List<FaixaMapa>();
        for (double z = ini; z < zmax - 1e-9 || l.Count == 0; z += passo)
            l.Add(new FaixaMapa { De = Math.Round(z, 6), Ate = Math.Round(z + passo, 6) });
        var cores = Rampa(l.Count, "1B5E20", "66BB6A", "D4E157", "FFEE58", "FFA726", "A1662F", "6D4C41");
        for (int i = 0; i < l.Count; i++) l[i].Cor = cores[i];
        return l;
    }

    /// <summary>Faixas de declividade (%). Sem limites = classes da EMBRAPA.</summary>
    public static List<FaixaMapa> Declividades(IReadOnlyList<double>? limites = null)
    {
        var l = new List<FaixaMapa>();
        if (limites is null || limites.Count == 0)
        {
            var cores = new[] { "2E7D32", "9CCC65", "FFEE58", "FFA726", "E53935", "6A1B9A" };
            double de = 0;
            for (int i = 0; i < Embrapa.Length; i++)
            {
                l.Add(new FaixaMapa { De = de, Ate = Embrapa[i].Ate, Classe = Embrapa[i].Nome, Cor = cores[i] });
                de = Embrapa[i].Ate;
            }
            return l;
        }
        var lim = limites.Where(v => v > 0).Distinct().OrderBy(v => v).ToList();
        double a = 0;
        foreach (var v in lim) { l.Add(new FaixaMapa { De = a, Ate = v }); a = v; }
        l.Add(new FaixaMapa { De = a });
        var c = Rampa(l.Count, "2E7D32", "9CCC65", "FFEE58", "FFA726", "E53935", "6A1B9A");
        for (int i = 0; i < l.Count; i++) l[i].Cor = c[i];
        return l;
    }

    public static double PassoRedondo(double bruto)
    {
        if (!(bruto > 0)) return 1;
        double p = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
        foreach (var m in new[] { 1, 2, 2.5, 5, 10 })
            if (m * p >= bruto - 1e-12) return m * p;
        return 10 * p;
    }

    /// <summary>Rampa de cores interpolada entre as paradas (RRGGBB).</summary>
    public static string[] Rampa(int n, params string[] paradas)
    {
        if (n <= 0) return Array.Empty<string>();
        if (paradas.Length == 0) paradas = new[] { "00FF00", "FF0000" };
        var r = new string[n];
        for (int i = 0; i < n; i++)
        {
            double t = n == 1 ? 0 : (double)i / (n - 1) * (paradas.Length - 1);
            int k = Math.Min((int)Math.Floor(t), paradas.Length - 2);
            double f = paradas.Length == 1 ? 0 : t - k;
            if (paradas.Length == 1) { r[i] = paradas[0]; continue; }
            int C(string s, int o) => Convert.ToInt32(s.Substring(o, 2), 16);
            int Mix(int o) => (int)Math.Round(C(paradas[k], o) + (C(paradas[k + 1], o) - C(paradas[k], o)) * f);
            r[i] = $"{Mix(0):X2}{Mix(2):X2}{Mix(4):X2}";
        }
        return r;
    }

    /// <summary>Área em planta da parte do triângulo com cota entre a e b.</summary>
    public static double AreaEntreCotas(P3 p, P3 q, P3 r, double a, double b)
    {
        var poli = new List<P3> { p, q, r };
        poli = Recortar(poli, v => v.Z - a);
        if (poli.Count < 3) return 0;
        poli = Recortar(poli, v => b - v.Z);
        if (poli.Count < 3) return 0;
        double s = 0;
        for (int i = 0; i < poli.Count; i++)
        {
            var u = poli[i]; var w = poli[(i + 1) % poli.Count];
            s += u.X * w.Y - w.X * u.Y;
        }
        return Math.Abs(s) / 2;
    }

    /// <summary>Sutherland–Hodgman: mantém a parte com f(v) ≥ 0.</summary>
    private static List<P3> Recortar(List<P3> poli, Func<P3, double> f)
    {
        var r = new List<P3>(poli.Count + 2);
        for (int i = 0; i < poli.Count; i++)
        {
            var u = poli[i]; var w = poli[(i + 1) % poli.Count];
            double fu = f(u), fw = f(w);
            if (fu >= 0) r.Add(u);
            if ((fu >= 0) != (fw >= 0))
            {
                double t = fu / (fu - fw);
                r.Add(new P3(u.X + (w.X - u.X) * t, u.Y + (w.Y - u.Y) * t, u.Z + (w.Z - u.Z) * t));
            }
        }
        return r;
    }

    /// <summary>Declividade do plano do triângulo, em % (NaN se o triângulo é vertical/degenerado em planta).</summary>
    public static double Declividade(P3 p, P3 q, P3 r)
    {
        var n = (q - p).Cross(r - p);
        if (Math.Abs(n.Z) < 1e-12) return double.NaN;
        return Math.Sqrt(n.X * n.X + n.Y * n.Y) / Math.Abs(n.Z) * 100;
    }

    /// <summary>Soma nas faixas as áreas em planta por cota. A primeira e a última faixa recebem o que estiver abaixo/acima.</summary>
    public static double AreasPorCota(IEnumerable<(P3 A, P3 B, P3 C)> triangulos, IList<FaixaMapa> faixas)
    {
        foreach (var f in faixas) f.Area = 0;
        double total = 0;
        foreach (var (a, b, c) in triangulos)
        {
            double zmin = Math.Min(a.Z, Math.Min(b.Z, c.Z)), zmax = Math.Max(a.Z, Math.Max(b.Z, c.Z));
            if (zmax - zmin < 1e-9)
            {
                // triângulo plano: vai inteiro para a faixa [De, Até) da sua cota (não conta duas vezes no limite)
                double ap = Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) / 2;
                int k = faixas.Count - 1;
                for (int i = 0; i < faixas.Count; i++)
                    if (zmin < faixas[i].Ate) { k = i; break; }
                faixas[k].Area += ap;
                total += ap;
                continue;
            }
            for (int i = 0; i < faixas.Count; i++)
            {
                double de = i == 0 ? double.NegativeInfinity : faixas[i].De;
                double ate = i == faixas.Count - 1 ? double.PositiveInfinity : faixas[i].Ate;
                if (zmax < de || zmin > ate) continue;
                double ar = AreaEntreCotas(a, b, c, double.IsNegativeInfinity(de) ? zmin - 1 : de, double.IsPositiveInfinity(ate) ? zmax + 1 : ate);
                faixas[i].Area += ar;
                total += ar;
            }
        }
        return total;
    }

    /// <summary>Soma nas faixas as áreas em planta por declividade (%).</summary>
    public static double AreasPorDeclividade(IEnumerable<(P3 A, P3 B, P3 C)> triangulos, IList<FaixaMapa> faixas)
    {
        foreach (var f in faixas) f.Area = 0;
        double total = 0;
        foreach (var (a, b, c) in triangulos)
        {
            double s = Declividade(a, b, c);
            if (double.IsNaN(s)) continue;
            double ar = Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) / 2;
            int k = faixas.Count - 1;
            for (int i = 0; i < faixas.Count; i++)
                if (s < faixas[i].Ate) { k = i; break; }
            faixas[k].Area += ar;
            total += ar;
        }
        return total;
    }

    public static string Intervalo(FaixaMapa f, string unidade, int casas = 2)
    {
        string N(double v) => v.ToString("0." + new string('#', casas), Num.Br);
        if (double.IsPositiveInfinity(f.Ate)) return $"acima de {N(f.De)} {unidade}";
        return $"{N(f.De)} a {N(f.Ate)} {unidade}";
    }

    /// <summary>Tabela da legenda: faixa, classe, área (m² e ha) e porcentagem.</summary>
    public static TabelaDados Tabela(string titulo, IList<FaixaMapa> faixas, string unidade)
    {
        double total = faixas.Sum(f => f.Area);
        var t = new TabelaDados { Titulo = titulo };
        bool classes = faixas.Any(f => f.Classe.Length > 0);
        t.Colunas = classes ? new List<string> { "Faixa", "Classe", "Área (m²)", "Área (ha)", "%" } : new List<string> { "Faixa", "Área (m²)", "Área (ha)", "%" };
        foreach (var f in faixas)
        {
            var linha = new List<string> { Intervalo(f, unidade) };
            if (classes) linha.Add(f.Classe);
            linha.Add(f.Area.ToString("#,##0", Num.Br));
            linha.Add((f.Area / 10000).ToString("#,##0.00", Num.Br));
            linha.Add(total > 0 ? (f.Area / total * 100).ToString("0.0", Num.Br) : "-");
            t.Linhas.Add(linha.ToArray());
        }
        var tot = new List<string> { "Total" };
        if (classes) tot.Add("");
        tot.Add(total.ToString("#,##0", Num.Br));
        tot.Add((total / 10000).ToString("#,##0.00", Num.Br));
        tot.Add(total > 0 ? "100,0" : "-");
        t.Linhas.Add(tot.ToArray());
        return t;
    }
}

/// <summary>Contorno de uma malha de triângulos: arestas usadas por um só triângulo, encadeadas em anéis fechados.</summary>
public static class ContornoTin
{
    /// <summary>Anéis do contorno (externo e furos), cada um sem repetir o primeiro vértice; o maior vem primeiro.</summary>
    public static List<List<P3>> Aneis(IEnumerable<(P3 A, P3 B, P3 C)> triangulos, double tolerancia = 1e-6)
    {
        var pontos = new Dictionary<(long, long), int>();
        var lista = new List<P3>();
        int Id(P3 p)
        {
            var k = ((long)Math.Round(p.X / tolerancia), (long)Math.Round(p.Y / tolerancia));
            if (pontos.TryGetValue(k, out var i)) return i;
            pontos[k] = lista.Count;
            lista.Add(p);
            return lista.Count - 1;
        }
        // aresta orientada de cada triângulo (sentido anti-horário): a aresta de contorno não tem a gêmea invertida
        var arestas = new Dictionary<(int, int), int>();
        foreach (var (a, b, c) in triangulos)
        {
            int ia = Id(a), ib = Id(b), ic = Id(c);
            if (ia == ib || ib == ic || ia == ic) continue;
            double s = (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
            if (Math.Abs(s) < 1e-12) continue;
            if (s < 0) (ib, ic) = (ic, ib);
            foreach (var (u, v) in new[] { (ia, ib), (ib, ic), (ic, ia) })
                arestas[(u, v)] = arestas.TryGetValue((u, v), out var n) ? n + 1 : 1;
        }
        var proxima = new Dictionary<int, List<int>>();
        foreach (var ((u, v), _) in arestas)
            if (!arestas.ContainsKey((v, u)))
            {
                if (!proxima.TryGetValue(u, out var l)) proxima[u] = l = new List<int>();
                l.Add(v);
            }
        var aneis = new List<List<P3>>();
        while (proxima.Count > 0)
        {
            int ini = proxima.Keys.First(), atual = ini;
            var anel = new List<P3>();
            for (int guarda = 0; guarda < 10_000_000; guarda++)
            {
                anel.Add(lista[atual]);
                if (!proxima.TryGetValue(atual, out var l) || l.Count == 0) break;
                int prox = l[^1];
                l.RemoveAt(l.Count - 1);
                if (l.Count == 0) proxima.Remove(atual);
                atual = prox;
                if (atual == ini) break;
            }
            if (anel.Count >= 3) aneis.Add(anel);
        }
        static double Area(List<P3> p)
        {
            double s = 0;
            for (int i = 0; i < p.Count; i++) { var u = p[i]; var w = p[(i + 1) % p.Count]; s += u.X * w.Y - w.X * u.Y; }
            return Math.Abs(s) / 2;
        }
        return aneis.OrderByDescending(Area).ToList();
    }
}
