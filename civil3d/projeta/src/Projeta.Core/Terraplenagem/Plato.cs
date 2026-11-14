using Projeta.Core.Comum;
using Projeta.Core.Relatorios;
using Projeta.Core.Superficies;

namespace Projeta.Core.Terraplenagem;

/// <summary>Parâmetros de um platô: taludes, caimento do piso e fator de homogeneização.</summary>
public sealed class ParametrosPlato
{
    /// <summary>Talude de corte, em H por 1 V.</summary>
    public double TaludeCorte { get; set; } = 1.5;
    /// <summary>Talude de aterro, em H por 1 V.</summary>
    public double TaludeAterro { get; set; } = 2.0;
    /// <summary>Caimento do piso (m/m), descendo no sentido em que o terreno desce.</summary>
    public double Caimento { get; set; } = 0.005;
    /// <summary>Fator de homogeneização: m³ de corte por m³ de aterro compactado.</summary>
    public double Fh { get; set; } = 1.25;
    /// <summary>Lado da célula da malha de cálculo (m); 0 = automático pela área.</summary>
    public double Celula { get; set; }

    public void Validar()
    {
        if (!(TaludeCorte > 0 && TaludeCorte <= 20) || !(TaludeAterro > 0 && TaludeAterro <= 20))
            throw new ArgumentException("Os taludes devem estar entre 0 e 20 (H por 1 V).");
        if (!(Caimento >= 0 && Caimento <= 0.2)) throw new ArgumentException("O caimento do piso deve estar entre 0 e 20 %.");
        if (!(Fh > 0 && Fh <= 3)) throw new ArgumentException("O fator de homogeneização deve estar entre 0 e 3.");
        if (Celula < 0 || double.IsNaN(Celula)) throw new ArgumentException("O tamanho da célula não pode ser negativo.");
    }

    /// <summary>Célula automática: fina em platô pequeno, mais grossa em platô grande.</summary>
    public static double CelulaPara(double areaPiso) => areaPiso <= 2500 ? 0.5 : areaPiso <= 60000 ? 1.0 : areaPiso <= 250000 ? 2.0 : 4.0;
}

/// <summary>Polígono simples (convexo ou não) em anti-horário, com busca do ponto mais próximo da borda.</summary>
public sealed class Poligono2D
{
    public IReadOnlyList<(double X, double Y)> V { get; }
    public double Area { get; }
    public double Perimetro { get; }
    public double Cx { get; }
    public double Cy { get; }
    private readonly double[] _s;

    public Poligono2D(IEnumerable<(double X, double Y)> pontos)
    {
        var l = new List<(double X, double Y)>();
        foreach (var p in pontos)
            if (l.Count == 0 || Math.Abs(p.X - l[^1].X) + Math.Abs(p.Y - l[^1].Y) > 1e-9) l.Add(p);
        while (l.Count > 1 && Math.Abs(l[0].X - l[^1].X) + Math.Abs(l[0].Y - l[^1].Y) <= 1e-9) l.RemoveAt(l.Count - 1);
        if (l.Count < 3) throw new ArgumentException("O polígono precisa de pelo menos 3 vértices.");
        double a = 0, cx = 0, cy = 0;
        for (int i = 0; i < l.Count; i++)
        {
            var (x0, y0) = l[i]; var (x1, y1) = l[(i + 1) % l.Count];
            double c = x0 * y1 - x1 * y0;
            a += c; cx += (x0 + x1) * c; cy += (y0 + y1) * c;
        }
        a /= 2;
        if (Math.Abs(a) < 1e-9) throw new ArgumentException("O polígono não tem área.");
        Cx = cx / (6 * a); Cy = cy / (6 * a);
        if (a < 0) { l.Reverse(); a = -a; }
        Area = a;
        V = l;
        _s = new double[l.Count + 1];
        for (int i = 0; i < l.Count; i++)
        {
            var (x0, y0) = l[i]; var (x1, y1) = l[(i + 1) % l.Count];
            _s[i + 1] = _s[i] + Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        }
        Perimetro = _s[l.Count];
    }

    public bool Dentro(double x, double y)
    {
        bool d = false;
        for (int i = 0, j = V.Count - 1; i < V.Count; j = i++)
            if ((V[i].Y > y) != (V[j].Y > y) && x < (V[j].X - V[i].X) * (y - V[i].Y) / (V[j].Y - V[i].Y) + V[i].X) d = !d;
        return d;
    }

    /// <summary>Ponto da borda mais próximo de (x, y), a distância até ele e a posição dele ao longo da borda.</summary>
    public (double X, double Y, double D, double S) MaisProximo(double x, double y)
    {
        double melhor = double.PositiveInfinity, bx = 0, by = 0, bs = 0;
        for (int i = 0; i < V.Count; i++)
        {
            var (x0, y0) = V[i]; var (x1, y1) = V[(i + 1) % V.Count];
            double ex = x1 - x0, ey = y1 - y0, l2 = ex * ex + ey * ey;
            double t = l2 > 0 ? Math.Clamp(((x - x0) * ex + (y - y0) * ey) / l2, 0, 1) : 0;
            double px = x0 + t * ex, py = y0 + t * ey, d2 = (x - px) * (x - px) + (y - py) * (y - py);
            if (d2 < melhor) { melhor = d2; bx = px; by = py; bs = _s[i] + t * (_s[i + 1] - _s[i]); }
        }
        return (bx, by, Math.Sqrt(melhor), bs);
    }

    /// <summary>Pontos da borda a cada <paramref name="passo"/> (com todos os vértices), com a posição ao longo dela.</summary>
    public List<(double X, double Y, double S)> Densificar(double passo)
    {
        var r = new List<(double, double, double)>();
        for (int i = 0; i < V.Count; i++)
        {
            var (x0, y0) = V[i]; var (x1, y1) = V[(i + 1) % V.Count];
            double len = _s[i + 1] - _s[i];
            int n = Math.Max(1, (int)Math.Ceiling(len / passo - 1e-9));
            for (int k = 0; k < n; k++) { double t = (double)k / n; r.Add((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, _s[i] + len * t)); }
        }
        return r;
    }

    /// <summary>Lados do polígono: início, direção unitária, comprimento e a normal para fora.</summary>
    public IEnumerable<(double X, double Y, double Ex, double Ey, double L, double Nx, double Ny)> Lados()
    {
        for (int i = 0; i < V.Count; i++)
        {
            var (x0, y0) = V[i]; var (x1, y1) = V[(i + 1) % V.Count];
            double l = _s[i + 1] - _s[i];
            if (l <= 0) continue;
            double ex = (x1 - x0) / l, ey = (y1 - y0) / l;
            yield return (x0, y0, ex, ey, l, ey, -ex); // anti-horário: normal para fora = (ey, -ex)
        }
    }

    /// <summary>
    /// Ponto interno mais longe da borda (para o rótulo): o centróide quando ele está dentro e quase tão longe quanto o
    /// melhor ponto; senão, o melhor de uma busca em malha refinada.
    /// </summary>
    public (double X, double Y, double D) Polo()
    {
        double minx = V.Min(v => v.X), maxx = V.Max(v => v.X), miny = V.Min(v => v.Y), maxy = V.Max(v => v.Y);
        double dc = Dentro(Cx, Cy) ? MaisProximo(Cx, Cy).D : -1;
        double bx = Cx, by = Cy, bd = dc, passo = Math.Max(maxx - minx, maxy - miny) / 40;
        for (int rodada = 0; rodada < 4; rodada++)
        {
            double x0 = rodada == 0 ? minx : bx - 2 * passo, x1 = rodada == 0 ? maxx : bx + 2 * passo;
            double y0 = rodada == 0 ? miny : by - 2 * passo, y1 = rodada == 0 ? maxy : by + 2 * passo;
            double cx = bx, cy = by;
            for (double x = x0; x <= x1 + 1e-9; x += passo)
                for (double y = y0; y <= y1 + 1e-9; y += passo)
                {
                    if (!Dentro(x, y)) continue;
                    double d = MaisProximo(x, y).D;
                    if (d > bd + 1e-9) { bd = d; cx = x; cy = y; }
                }
            bx = cx; by = cy; passo /= 4;
        }
        return dc >= 0.9 * bd ? (Cx, Cy, dc) : (bx, by, bd);
    }

    public static double AreaDe(IReadOnlyList<(double X, double Y)> p)
    {
        double a = 0;
        for (int i = 0; i < p.Count; i++) { var (x0, y0) = p[i]; var (x1, y1) = p[(i + 1) % p.Count]; a += x0 * y1 - x1 * y0; }
        return a / 2;
    }
}

/// <summary>Volumes e áreas do platô numa cota, pela malha.</summary>
public readonly record struct ResumoMalhaPlato(double Corte, double Aterro, double AreaPiso, double AreaTaludeCorte, double AreaTaludeAterro,
                                                bool TocaBorda, int NosSemTerreno)
{
    public double Saldo(double fh) => Corte - fh * Aterro;
}

/// <summary>
/// Malha de cálculo de um platô: piso plano (com caimento) dentro do polígono e, fora dele, o talude que sai do ponto
/// mais próximo da borda até encontrar o terreno (cone nos cantos convexos, encontro dos planos nos reentrantes).
/// O terreno vem de uma função (NaN fora da superfície), lida uma vez por nó da malha.
/// </summary>
public sealed class CampoPlato
{
    public Poligono2D Pol { get; }
    public ParametrosPlato P { get; }
    public double X0 { get; }
    public double Y0 { get; }
    public double Celula { get; }
    public int Nx { get; }
    public int Ny { get; }
    /// <summary>Direção (unitária) em que o piso sobe: a do terreno subindo. (0, 0) = piso sem caimento.</summary>
    public double Dx { get; }
    public double Dy { get; }
    public double TerrenoMin { get; }
    public double TerrenoMax { get; }
    public int NosPisoSemTerreno { get; }
    /// <summary>Declividade do plano médio do terreno sob o piso (m/m).</summary>
    public double DeclividadeMedia { get; }

    private readonly Func<double, double, double> _terreno;
    private readonly double[] _t, _qx, _qy, _d, _tq;
    private readonly bool[] _dentro;
    private readonly double[] _bs, _bt;

    public CampoPlato(Poligono2D pol, Func<double, double, double> terreno, ParametrosPlato p, double celula, double margem)
    {
        if (!(celula > 0)) throw new ArgumentException("A célula deve ser positiva.");
        Pol = pol; P = p; Celula = celula; _terreno = terreno;
        double minx = pol.V.Min(v => v.X) - margem, maxx = pol.V.Max(v => v.X) + margem;
        double miny = pol.V.Min(v => v.Y) - margem, maxy = pol.V.Max(v => v.Y) + margem;
        X0 = Math.Floor(minx / celula) * celula; Y0 = Math.Floor(miny / celula) * celula;
        Nx = (int)Math.Ceiling((maxx - X0) / celula) + 1; Ny = (int)Math.Ceiling((maxy - Y0) / celula) + 1;
        if ((long)Nx * Ny > 6_000_000) throw new ArgumentException($"Malha grande demais ({Nx} × {Ny} nós); aumente a célula.");

        // terreno ao longo da borda, para decidir corte ou aterro pelo ponto mais próximo
        var dens = pol.Densificar(Math.Min(0.5, celula / 2));
        _bs = dens.Select(q => q.S).Append(pol.Perimetro).ToArray();
        _bt = dens.Select(q => terreno(q.X, q.Y)).ToArray();
        _bt = _bt.Append(_bt[0]).ToArray();

        int n = Nx * Ny;
        _t = new double[n]; _qx = new double[n]; _qy = new double[n]; _d = new double[n]; _tq = new double[n]; _dentro = new bool[n];
        double sx = 0, sy = 0, sz = 0, sxx = 0, syy = 0, sxy = 0, sxz = 0, syz = 0; int m = 0, semT = 0;
        double tmin = double.PositiveInfinity, tmax = double.NegativeInfinity;
        for (int j = 0; j < Ny; j++)
            for (int i = 0; i < Nx; i++)
            {
                int k = j * Nx + i;
                double x = X0 + i * celula, y = Y0 + j * celula;
                double t = terreno(x, y);
                _t[k] = t;
                if (pol.Dentro(x, y))
                {
                    _dentro[k] = true;
                    if (double.IsNaN(t)) { semT++; continue; }
                    double u = x - pol.Cx, v = y - pol.Cy;
                    sx += u; sy += v; sz += t; sxx += u * u; syy += v * v; sxy += u * v; sxz += u * t; syz += v * t; m++;
                    if (t < tmin) tmin = t;
                    if (t > tmax) tmax = t;
                }
                else
                {
                    var (qx, qy, d, s) = pol.MaisProximo(x, y);
                    _qx[k] = qx; _qy[k] = qy; _d[k] = d; _tq[k] = TerrenoNaBorda(s);
                }
            }
        if (m < 3) throw new InvalidOperationException("O polígono não está sobre a superfície do terreno.");
        NosPisoSemTerreno = semT;
        TerrenoMin = tmin; TerrenoMax = tmax;
        // plano do terreno por mínimos quadrados: t = a + b·u + c·v (u, v relativos ao centróide)
        double mx = sx / m, my = sy / m, mz = sz / m;
        double cxx = sxx / m - mx * mx, cyy = syy / m - my * my, cxy = sxy / m - mx * my, cxz = sxz / m - mx * mz, cyz = syz / m - my * mz;
        double det = cxx * cyy - cxy * cxy;
        double b = 0, c = 0;
        if (Math.Abs(det) > 1e-12) { b = (cxz * cyy - cyz * cxy) / det; c = (cyz * cxx - cxz * cxy) / det; }
        double g = Math.Sqrt(b * b + c * c);
        if (g > 1e-4 && p.Caimento > 0) { Dx = b / g; Dy = c / g; }
        DeclividadeMedia = g;
    }

    private double TerrenoNaBorda(double s)
    {
        int i = Array.BinarySearch(_bs, s);
        if (i >= 0) return _bt[Math.Min(i, _bt.Length - 1)];
        i = ~i;
        if (i <= 0) return _bt[0];
        if (i >= _bs.Length) return _bt[^1];
        double t = (s - _bs[i - 1]) / (_bs[i] - _bs[i - 1]);
        double a = _bt[i - 1], b = _bt[i];
        if (double.IsNaN(a)) return b;
        if (double.IsNaN(b)) return a;
        return a + (b - a) * t;
    }

    /// <summary>Cota do piso em (x, y) para a cota <paramref name="z0"/> no centróide.</summary>
    public double Piso(double z0, double x, double y) => z0 + P.Caimento * ((x - Pol.Cx) * Dx + (y - Pol.Cy) * Dy);

    /// <summary>Azimute (graus, a partir do norte, horário) para onde o piso cai; NaN sem caimento.</summary>
    public double AzimuteCaimento => Dx == 0 && Dy == 0 ? double.NaN : (Math.Atan2(-Dx, -Dy) * 180 / Math.PI + 360) % 360;

    public double X(int i) => X0 + i * Celula;
    public double Y(int j) => Y0 + j * Celula;

    /// <summary>Cota final e classe do nó: 0 piso, 1 talude de corte, 2 talude de aterro, 3 terreno, −1 sem dado.</summary>
    public (double Z, int Classe) Final(double z0, int k)
    {
        int i = k % Nx, j = k / Nx;
        if (_dentro[k]) return (Piso(z0, X(i), Y(j)), 0);
        double t = _t[k];
        if (double.IsNaN(t)) return (double.NaN, -1);
        double zb = Piso(z0, _qx[k], _qy[k]);
        double tq = double.IsNaN(_tq[k]) ? t : _tq[k];
        if (tq > zb) { double zs = zb + _d[k] / P.TaludeCorte; return zs < t ? (zs, 1) : (t, 3); }
        else { double zs = zb - _d[k] / P.TaludeAterro; return zs > t ? (zs, 2) : (t, 3); }
    }

    /// <summary>Negativo dentro da área de terraplenagem (piso + taludes), positivo fora; o zero é o encontro com o terreno.</summary>
    public double G(double z0, int k)
    {
        if (_dentro[k]) return -1;
        double t = _t[k];
        if (double.IsNaN(t)) return 1;
        double zb = Piso(z0, _qx[k], _qy[k]);
        double tq = double.IsNaN(_tq[k]) ? t : _tq[k];
        return tq > zb ? zb + _d[k] / P.TaludeCorte - t : t - (zb - _d[k] / P.TaludeAterro);
    }

    public bool Dentro(int k) => _dentro[k];
    public double Terreno(int k) => _t[k];

    public ResumoMalhaPlato Volumes(double z0)
    {
        double a1 = Celula * Celula, corte = 0, aterro = 0, piso = 0, tc = 0, ta = 0; bool borda = false; int semT = 0;
        for (int j = 0; j < Ny; j++)
            for (int i = 0; i < Nx; i++)
            {
                int k = j * Nx + i;
                var (z, cl) = Final(z0, k);
                if (cl == 0) piso += a1;
                double t = _t[k];
                if (double.IsNaN(t) || double.IsNaN(z)) { if (cl == 0) semT++; continue; }
                double dz = z - t;
                if (dz < 0) corte -= dz * a1; else aterro += dz * a1;
                if (cl == 1) tc += a1; else if (cl == 2) ta += a1;
                if ((cl == 1 || cl == 2) && (i == 0 || j == 0 || i == Nx - 1 || j == Ny - 1)) borda = true;
            }
        return new ResumoMalhaPlato(corte, aterro, piso, tc, ta, borda, semT);
    }

    /// <summary>Cota no centróide que zera corte − Fh × aterro (bisseção; o saldo cai quando a cota sobe).</summary>
    public double Compensar()
    {
        double lo = TerrenoMin - 30, hi = TerrenoMax + 30;
        double flo = Volumes(lo).Saldo(P.Fh), fhi = Volumes(hi).Saldo(P.Fh);
        if (!(flo > 0) || !(fhi < 0)) throw new InvalidOperationException("Não achei a cota de compensação entre o terreno mais baixo e o mais alto (−30 / +30 m).");
        for (int it = 0; it < 60 && hi - lo > 1e-4; it++)
        {
            double mid = (lo + hi) / 2;
            if (Volumes(mid).Saldo(P.Fh) > 0) lo = mid; else hi = mid;
        }
        return Math.Round((lo + hi) / 2, 3);
    }

    /// <summary>
    /// Contornos do encontro dos taludes com o terreno (marching squares no zero de G), em anti-horário,
    /// do maior para o menor. O primeiro é o limite da terraplenagem; os outros, ilhas de terreno dentro dela.
    /// </summary>
    public List<List<(double X, double Y)>> Encontro(double z0)
    {
        var g = new double[Nx * Ny];
        for (int k = 0; k < g.Length; k++) { g[k] = G(z0, k); if (g[k] == 0) g[k] = 1e-12; }
        // pontos nas arestas: horizontal (i,j)-(i+1,j) = 2k, vertical (i,j)-(i,j+1) = 2k+1
        var pts = new Dictionary<long, (double X, double Y)>();
        (double, double) Ponto(long id)
        {
            if (pts.TryGetValue(id, out var p)) return p;
            long k = id / 2; int i = (int)(k % Nx), j = (int)(k / Nx);
            int k2 = id % 2 == 0 ? (int)k + 1 : (int)k + Nx;
            double a = g[k], b = g[k2], t = a / (a - b);
            p = id % 2 == 0 ? (X(i) + t * Celula, Y(j)) : (X(i), Y(j) + t * Celula);
            pts[id] = p;
            return p;
        }
        var viz = new Dictionary<long, List<long>>();
        void Seg(long a, long b)
        {
            if (!viz.TryGetValue(a, out var la)) viz[a] = la = new List<long>(2);
            if (!viz.TryGetValue(b, out var lb)) viz[b] = lb = new List<long>(2);
            la.Add(b); lb.Add(a);
        }
        for (int j = 0; j < Ny - 1; j++)
            for (int i = 0; i < Nx - 1; i++)
            {
                int k0 = j * Nx + i, k1 = k0 + 1, k3 = k0 + Nx, k2 = k3 + 1;
                int idx = (g[k0] < 0 ? 1 : 0) | (g[k1] < 0 ? 2 : 0) | (g[k2] < 0 ? 4 : 0) | (g[k3] < 0 ? 8 : 0);
                if (idx == 0 || idx == 15) continue;
                long e0 = 2L * k0, e1 = 2L * k1 + 1, e2 = 2L * k3, e3 = 2L * k0 + 1; // baixo, direita, cima, esquerda
                bool centro = (g[k0] + g[k1] + g[k2] + g[k3]) / 4 < 0;
                switch (idx)
                {
                    case 1: case 14: Seg(e3, e0); break;
                    case 2: case 13: Seg(e0, e1); break;
                    case 3: case 12: Seg(e3, e1); break;
                    case 4: case 11: Seg(e1, e2); break;
                    case 6: case 9: Seg(e0, e2); break;
                    case 7: case 8: Seg(e3, e2); break;
                    case 5: if (centro) { Seg(e0, e1); Seg(e2, e3); } else { Seg(e3, e0); Seg(e1, e2); } break;
                    case 10: if (centro) { Seg(e3, e0); Seg(e1, e2); } else { Seg(e0, e1); Seg(e2, e3); } break;
                }
            }
        var usados = new HashSet<long>();
        var lacos = new List<List<(double X, double Y)>>();
        foreach (var ini in viz.Keys)
        {
            if (usados.Contains(ini)) continue;
            var l = new List<(double X, double Y)>();
            long ant = -1, cur = ini;
            while (cur >= 0 && usados.Add(cur))
            {
                l.Add(Ponto(cur));
                long prox = -1;
                foreach (var nb in viz[cur]) if (nb != ant && !usados.Contains(nb)) { prox = nb; break; }
                ant = cur; cur = prox;
            }
            if (l.Count >= 3) lacos.Add(l);
        }
        foreach (var l in lacos) if (Poligono2D.AreaDe(l) < 0) l.Reverse();
        return lacos.OrderByDescending(l => Math.Abs(Poligono2D.AreaDe(l))).ToList();
    }

    /// <summary>Para um ponto do encontro com o terreno: se é corte, e a altura do talude ali (terreno − piso na borda).</summary>
    public (bool Corte, double Altura) Talude(double z0, double x, double y, double terreno)
    {
        var (qx, qy, _, _) = Pol.MaisProximo(x, y);
        double zb = Piso(z0, qx, qy);
        return (terreno > zb, Math.Abs(terreno - zb));
    }

    /// <summary>
    /// Tracinhos de talude: da borda, para fora, até o terreno, a cada <paramref name="passo"/> m, longe dos cantos
    /// (a menos de <paramref name="folgaCanto"/> m de um vértice não há tracinho). Comprimento alternado inteiro / metade.
    /// </summary>
    public List<(double X0, double Y0, double X1, double Y1, bool Corte)> Tracos(double z0, double passo, double folgaCanto)
    {
        var r = new List<(double, double, double, double, bool)>();
        int n = 0;
        foreach (var (x0, y0, ex, ey, l, nx, ny) in Pol.Lados())
            for (double s = folgaCanto; s <= l - folgaCanto + 1e-9; s += passo)
            {
                double bx = x0 + ex * s, by = y0 + ey * s, zb = Piso(z0, bx, by), tb = _terreno(bx, by);
                if (double.IsNaN(tb)) continue;
                bool corte = tb > zb;
                double k = corte ? 1 / P.TaludeCorte : -1 / P.TaludeAterro;
                double F(double d) { double t = _terreno(bx + nx * d, by + ny * d); return double.IsNaN(t) ? double.NaN : t - (zb + k * d); }
                double f0 = tb - zb;
                if (Math.Abs(f0) < 0.05) continue;
                double a = 0, b = 0.25, fb = F(b);
                while (b < 400 && !double.IsNaN(fb) && (fb > 0) == (f0 > 0)) { a = b; b += 0.25; fb = F(b); }
                if (double.IsNaN(fb) || b >= 400) continue;
                for (int it = 0; it < 40; it++) { double mm = (a + b) / 2, fm = F(mm); if (double.IsNaN(fm)) break; if ((fm > 0) == (f0 > 0)) a = mm; else b = mm; }
                double D = (a + b) / 2, dd = D * (n++ % 2 == 0 ? 1 : 0.5);
                if (dd < 0.2) continue;
                // o tracinho sai do alto do talude: no corte, da crista (no terreno) para o piso; no aterro, da borda do piso para fora
                if (corte) r.Add((bx + nx * D, by + ny * D, bx + nx * (D - dd), by + ny * (D - dd), true));
                else r.Add((bx, by, bx + nx * dd, by + ny * dd, false));
            }
        return r;
    }

    /// <summary>Nós da faixa dos taludes (fora do piso, antes do encontro com o terreno), com a cota do talude.</summary>
    public IEnumerable<(double X, double Y, double Z)> PontosTalude(double z0)
    {
        for (int k = 0; k < _t.Length; k++)
        {
            if (_dentro[k]) continue;
            var (z, cl) = Final(z0, k);
            if (cl == 1 || cl == 2) yield return (X(k % Nx), Y(k / Nx), z);
        }
    }
}

/// <summary>Resultado de um platô para o relatório.</summary>
public sealed class ResultadoPlato
{
    public string Nome { get; set; } = "";
    public bool Compensada { get; set; }
    /// <summary>Cota do piso no centróide.</summary>
    public double Cota { get; set; }
    public double CotaMin { get; set; }
    public double CotaMax { get; set; }
    public double Caimento { get; set; }
    public double Azimute { get; set; } = double.NaN;
    public double AreaPiso { get; set; }
    public double AreaTerraplenagem { get; set; }
    public double AreaTaludeCorte { get; set; }
    public double AreaTaludeAterro { get; set; }
    public double AlturaCorte { get; set; }
    public double AlturaAterro { get; set; }
    public double ExtensaoCrista { get; set; }
    public double ExtensaoPe { get; set; }
    /// <summary>Volumes exatos (superfície de volume) — NaN se não calculados; aí valem os da malha.</summary>
    public double Corte { get; set; } = double.NaN;
    public double Aterro { get; set; } = double.NaN;
    public double CorteMalha { get; set; }
    public double AterroMalha { get; set; }
    public string Handle { get; set; } = "";
}

/// <summary>Relatório dos platôs: aba Platôs e, depois, as abas do relatório de volumes.</summary>
public sealed class RelatorioPlatos
{
    public string Terreno { get; set; } = "";
    public ParametrosPlato Parametros { get; set; } = new();
    public List<ResultadoPlato> Platos { get; } = new();
    public List<string> Avisos { get; } = new();

    public double Saldo(ResultadoPlato p) => C(p) - Parametros.Fh * A(p);
    public string Situacao(ResultadoPlato p) => RelatorioVolumes.Situacao(Saldo(p), Math.Max(C(p), Parametros.Fh * A(p)));
    public static double C(ResultadoPlato p) => double.IsNaN(p.Corte) ? p.CorteMalha : p.Corte;
    public static double A(ResultadoPlato p) => double.IsNaN(p.Aterro) ? p.AterroMalha : p.Aterro;

    public PastaXlsx Planilha(RelatorioVolumes? volumes = null)
    {
        var pasta = new PastaXlsx { Titulo = $"Platôs sobre {Terreno}" };
        var titulo = new EstiloCelula { Tamanho = 16, Negrito = true, Fundo = "D9D9D9", Horizontal = AlinhamentoH.Centro };
        var sub = titulo with { Tamanho = 10, Negrito = false };
        var cab = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula { Tamanho = 10 };
        var n2 = new EstiloCelula { Tamanho = 10, Formato = "#,##0.00" };
        var n3 = n2 with { Formato = "0.000" };
        var n1 = n2 with { Formato = "#,##0.0" };
        var n0 = n2 with { Formato = "0" };
        var f = pasta.NovaFolha("Platôs");
        string[] cols = { "Platô", "Cota do piso no centro (m)", "Cota mín. do piso (m)", "Cota máx. do piso (m)", "Caimento (%)", "Cai para (azimute °)",
                          "Área do piso (m²)", "Área de terraplenagem (m²)", "Talude de corte (m²)", "Talude de aterro (m²)",
                          "Altura máx. de corte (m)", "Altura máx. de aterro (m)", "Crista do corte (m)", "Pé do aterro (m)",
                          "Corte (m³)", "Aterro (m³)", $"Aterro × {F(Parametros.Fh)} (m³)", "Saldo (m³)", "Situação" };
        double[] larg = { 21, 11, 10, 10, 9, 10, 11, 14, 11, 11, 10, 10, 10, 10, 12, 12, 12, 11, 17 };
        int n = cols.Length;
        f.Mesclar(1, 1, 1, n, "Platôs - terraplenagem", titulo);
        f.Mesclar(2, 1, 2, n, $"Terreno: {Terreno} · taludes {F(Parametros.TaludeCorte)}H:1V no corte e {F(Parametros.TaludeAterro)}H:1V no aterro · " +
                              $"fator de homogeneização {F(Parametros.Fh)} · saldo = corte − aterro × {F(Parametros.Fh)}", sub);
        f.Altura(1, 30); f.Altura(2, 18); f.Altura(4, 44);
        for (int c = 0; c < n; c++) { f.Texto(4, c + 1, cols[c], cab); f.Largura(c + 1, larg[c]); }
        int lin = 5;
        foreach (var p in Platos)
        {
            f.Texto(lin, 1, p.Nome + (p.Compensada ? " (compensada)" : ""), txt);
            f.Numero(lin, 2, Math.Round(p.Cota, 3), n3);
            f.Numero(lin, 3, Math.Round(p.CotaMin, 3), n3);
            f.Numero(lin, 4, Math.Round(p.CotaMax, 3), n3);
            f.Numero(lin, 5, Math.Round(p.Caimento * 100, 2), n2);
            f.Numero(lin, 6, double.IsNaN(p.Azimute) ? double.NaN : Math.Round(p.Azimute), n0);
            f.Numero(lin, 7, Math.Round(p.AreaPiso, 2), n2);
            f.Numero(lin, 8, Math.Round(p.AreaTerraplenagem, 2), n2);
            f.Numero(lin, 9, Math.Round(p.AreaTaludeCorte, 1), n1);
            f.Numero(lin, 10, Math.Round(p.AreaTaludeAterro, 1), n1);
            f.Numero(lin, 11, Math.Round(p.AlturaCorte, 2), n2);
            f.Numero(lin, 12, Math.Round(p.AlturaAterro, 2), n2);
            f.Numero(lin, 13, Math.Round(p.ExtensaoCrista, 1), n1);
            f.Numero(lin, 14, Math.Round(p.ExtensaoPe, 1), n1);
            f.Numero(lin, 15, Math.Round(C(p), 2), n2);
            f.Numero(lin, 16, Math.Round(A(p), 2), n2);
            f.Numero(lin, 17, Math.Round(A(p) * Parametros.Fh, 2), n2);
            f.Numero(lin, 18, Math.Round(Saldo(p), 2), n2);
            f.Texto(lin, 19, Situacao(p), txt);
            lin++;
        }
        lin++;
        var nota = new EstiloCelula { Tamanho = 9, Italico = true, Borda = false };
        f.Texto(lin++, 1, "Áreas em planta. Volumes " + (Platos.All(p => !double.IsNaN(p.Corte)) ? "exatos, pela superfície de volume do Civil 3D (abas seguintes)." : "pela malha de cálculo."), nota);
        f.Texto(lin++, 1, "Cota compensada: a cota do piso no centro que zera corte − aterro × fator de homogeneização. O piso cai para o azimute indicado.", nota);
        foreach (var a in Avisos) f.Texto(lin++, 1, "Aviso: " + a, nota);
        f.CongelarLinhas = 4;
        f.TituloImpressaoAte = 4;
        f.Paisagem = true;
        f.CaberNaLargura = true;
        f.Papel = 8;
        volumes?.Planilha(pasta);
        return pasta;
    }

    public void Salvar(string arquivo, RelatorioVolumes? volumes = null) => Planilha(volumes).Salvar(arquivo);

    private static string F(double v) => v.ToString("0.###", Num.Br);
}
