namespace Projeta.Giro.Core.Geometria;

/// <summary>Funções geométricas 2D: arcos (bulge), polilinhas, distâncias, interseções, polígonos.</summary>
public static class Geo
{
    public const double Eps = 1e-9;

    /// <summary>Ângulo normalizado em (-π, π].</summary>
    public static double AngNorm(double a)
    {
        a = Math.IEEERemainder(a, 2 * Math.PI);
        if (a <= -Math.PI) a += 2 * Math.PI;
        return a;
    }

    /// <summary>Ângulo assinado de a para b (em (-π, π]).</summary>
    public static double AngEntre(V2 a, V2 b) => Math.Atan2(a.Cross(b), a.Dot(b));

    // ------------------------------------------------------------------ arcos

    /// <summary>Centro, raio, ângulo inicial e varredura (positiva = anti-horária) do arco definido por bulge.</summary>
    public static (V2 Centro, double Raio, double AngIni, double Varredura) ArcoDeBulge(V2 a, V2 b, double bulge)
    {
        double theta = 4 * Math.Atan(bulge);
        var c = b - a;
        double corda = c.Comprimento;
        double r = corda / (2 * Math.Sin(Math.Abs(theta) / 2));
        // distância do ponto médio ao centro (com sinal)
        double h = r * Math.Cos(theta / 2);
        var m = (a + b) / 2;
        var n = c.Unit().Esq();
        var centro = m + n * (Math.Sign(theta) * h);
        return (centro, r, (a - centro).Angulo, theta);
    }

    /// <summary>Bulge de um arco de centro <paramref name="c"/> indo de a até b, no sentido dado.</summary>
    public static double BulgeDeArco(V2 c, V2 a, V2 b, bool antiHorario)
    {
        double a0 = (a - c).Angulo, a1 = (b - c).Angulo;
        double sweep = a1 - a0;
        if (antiHorario) { while (sweep <= 0) sweep += 2 * Math.PI; }
        else { while (sweep >= 0) sweep -= 2 * Math.PI; }
        return Math.Tan(sweep / 4);
    }

    public static double ComprimentoSegmento(V2 a, V2 b, double bulge)
    {
        double corda = a.Dist(b);
        if (Math.Abs(bulge) < 1e-12 || corda < Eps) return corda;
        double theta = 4 * Math.Atan(bulge);
        double r = corda / (2 * Math.Sin(Math.Abs(theta) / 2));
        return Math.Abs(r * theta);
    }

    /// <summary>Pontos intermediários (sem extremos, salvo se pedido) de um arco definido por bulge.</summary>
    public static List<V2> PontosArcoBulge(V2 a, V2 b, double bulge, double flecha, bool incluirExtremos)
    {
        var r = new List<V2>();
        var (c, raio, ang0, sweep) = ArcoDeBulge(a, b, bulge);
        int n = NumSegmentosArco(raio, Math.Abs(sweep), flecha);
        if (incluirExtremos) r.Add(a);
        for (int i = 1; i < n; i++) r.Add(c + V2.Polar(ang0 + sweep * i / n, raio));
        if (incluirExtremos) r.Add(b);
        return r;
    }

    /// <summary>Pontos de um arco (centro, raio, ângulo inicial, varredura assinada), incluindo extremos.</summary>
    public static List<V2> PontosArco(V2 c, double raio, double ang0, double sweep, double flecha = 0.01)
    {
        int n = NumSegmentosArco(raio, Math.Abs(sweep), flecha);
        var r = new List<V2>(n + 1);
        for (int i = 0; i <= n; i++) r.Add(c + V2.Polar(ang0 + sweep * i / n, raio));
        return r;
    }

    public static int NumSegmentosArco(double raio, double sweepAbs, double flecha)
    {
        if (raio <= flecha * 2) return Math.Max(2, (int)Math.Ceiling(sweepAbs / (Math.PI / 8)));
        double dAng = 2 * Math.Acos(1 - flecha / raio);
        return Math.Clamp((int)Math.Ceiling(sweepAbs / dAng), 2, 4000);
    }

    // ------------------------------------------------------------------ polilinhas

    public static double Comprimento(IReadOnlyList<V2> p)
    {
        double s = 0;
        for (int i = 1; i < p.Count; i++) s += p[i].Dist(p[i - 1]);
        return s;
    }

    public static double[] Acumulado(IReadOnlyList<V2> p)
    {
        var a = new double[p.Count];
        for (int i = 1; i < p.Count; i++) a[i] = a[i - 1] + p[i].Dist(p[i - 1]);
        return a;
    }

    /// <summary>Remove pontos repetidos consecutivos.</summary>
    public static List<V2> Limpar(IEnumerable<V2> pts, double tol = 1e-6)
    {
        var r = new List<V2>();
        foreach (var p in pts)
            if (r.Count == 0 || r[^1].Dist(p) > tol) r.Add(p);
        return r;
    }

    /// <summary>Reamostra a polilinha em passos iguais (o último passo ajusta para cair no fim).</summary>
    public static List<V2> Reamostrar(IReadOnlyList<V2> p, double passo)
    {
        var limpo = Limpar(p);
        var r = new List<V2>();
        if (limpo.Count == 0) return r;
        r.Add(limpo[0]);
        if (limpo.Count == 1) return r;
        double total = Comprimento(limpo);
        int n = Math.Max(1, (int)Math.Ceiling(total / passo - 1e-9));
        double ds = total / n;
        int seg = 1; double base0 = 0;
        for (int k = 1; k < n; k++)
        {
            double alvo = k * ds;
            while (seg < limpo.Count - 1 && base0 + limpo[seg].Dist(limpo[seg - 1]) < alvo)
            {
                base0 += limpo[seg].Dist(limpo[seg - 1]);
                seg++;
            }
            double l = limpo[seg].Dist(limpo[seg - 1]);
            double t = l < Eps ? 0 : (alvo - base0) / l;
            r.Add(V2.Lerp(limpo[seg - 1], limpo[seg], Math.Clamp(t, 0, 1)));
        }
        r.Add(limpo[^1]);
        return r;
    }

    /// <summary>Ponto e direção (unitária) na distância s ao longo da polilinha.</summary>
    public static (V2 P, V2 T) PontoNaDistancia(IReadOnlyList<V2> p, double[] acum, double s)
    {
        if (p.Count == 1) return (p[0], new V2(1, 0));
        s = Math.Clamp(s, 0, acum[^1]);
        int i = Array.BinarySearch(acum, s);
        if (i < 0) i = ~i;
        i = Math.Clamp(i, 1, p.Count - 1);
        double l = acum[i] - acum[i - 1];
        double t = l < Eps ? 0 : (s - acum[i - 1]) / l;
        return (V2.Lerp(p[i - 1], p[i], t), (p[i] - p[i - 1]).Unit());
    }

    /// <summary>Simplificação de Douglas-Peucker (mantém extremos).</summary>
    public static List<V2> Simplificar(IReadOnlyList<V2> p, double tol)
    {
        if (p.Count < 3) return p.ToList();
        var manter = new bool[p.Count];
        manter[0] = manter[^1] = true;
        var pilha = new Stack<(int, int)>();
        pilha.Push((0, p.Count - 1));
        while (pilha.Count > 0)
        {
            var (i0, i1) = pilha.Pop();
            double dmax = -1; int imax = -1;
            for (int k = i0 + 1; k < i1; k++)
            {
                double d = DistPontoSegmento(p[k], p[i0], p[i1]);
                if (d > dmax) { dmax = d; imax = k; }
            }
            if (imax >= 0 && dmax > tol)
            {
                manter[imax] = true;
                pilha.Push((i0, imax));
                pilha.Push((imax, i1));
            }
        }
        var r = new List<V2>();
        for (int k = 0; k < p.Count; k++) if (manter[k]) r.Add(p[k]);
        return r;
    }

    // ------------------------------------------------------------------ distâncias e interseções

    public static double DistPontoSegmento(V2 p, V2 a, V2 b) => p.Dist(ProjetarNoSegmento(p, a, b));

    public static V2 ProjetarNoSegmento(V2 p, V2 a, V2 b)
    {
        var ab = b - a;
        double l2 = ab.Comprimento2;
        if (l2 < Eps * Eps) return a;
        double t = Math.Clamp((p - a).Dot(ab) / l2, 0, 1);
        return a + ab * t;
    }

    /// <summary>Interseção própria de dois segmentos (t e u em [0,1]).</summary>
    public static bool IntersecaoSegmentos(V2 a, V2 b, V2 c, V2 d, out V2 ponto)
    {
        ponto = default;
        var r = b - a; var s = d - c;
        double den = r.Cross(s);
        if (Math.Abs(den) < 1e-14) return false;
        var ac = c - a;
        double t = ac.Cross(s) / den, u = ac.Cross(r) / den;
        if (t < -1e-12 || t > 1 + 1e-12 || u < -1e-12 || u > 1 + 1e-12) return false;
        ponto = a + r * t;
        return true;
    }

    /// <summary>Interseção de duas retas infinitas (p + t·u e q + s·v).</summary>
    public static bool IntersecaoRetas(V2 p, V2 u, V2 q, V2 v, out V2 ponto)
    {
        ponto = default;
        double den = u.Cross(v);
        if (Math.Abs(den) < 1e-14) return false;
        double t = (q - p).Cross(v) / den;
        ponto = p + u * t;
        return true;
    }

    public static double DistSegmentos(V2 a, V2 b, V2 c, V2 d)
    {
        if (IntersecaoSegmentos(a, b, c, d, out _)) return 0;
        return Math.Min(Math.Min(DistPontoSegmento(a, c, d), DistPontoSegmento(b, c, d)),
                        Math.Min(DistPontoSegmento(c, a, b), DistPontoSegmento(d, a, b)));
    }

    // ------------------------------------------------------------------ polígonos

    /// <summary>Área com sinal (positiva = anti-horário).</summary>
    public static double Area(IReadOnlyList<V2> p)
    {
        double s = 0;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += p[j].X * p[i].Y - p[i].X * p[j].Y;
        return s / 2;
    }

    public static bool DentroPoligono(V2 q, IReadOnlyList<V2> p)
    {
        bool dentro = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
        {
            if ((p[i].Y > q.Y) != (p[j].Y > q.Y) &&
                q.X < (p[j].X - p[i].X) * (q.Y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)
                dentro = !dentro;
        }
        return dentro;
    }

    /// <summary>Fecho convexo (cadeia monótona), anti-horário.</summary>
    public static List<V2> FechoConvexo(IEnumerable<V2> pts)
    {
        var p = pts.Distinct().OrderBy(v => v.X).ThenBy(v => v.Y).ToList();
        if (p.Count < 3) return p;
        var h = new V2[2 * p.Count];
        int k = 0;
        for (int i = 0; i < p.Count; i++)
        {
            while (k >= 2 && (h[k - 1] - h[k - 2]).Cross(p[i] - h[k - 2]) <= 0) k--;
            h[k++] = p[i];
        }
        for (int i = p.Count - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && (h[k - 1] - h[k - 2]).Cross(p[i] - h[k - 2]) <= 0) k--;
            h[k++] = p[i];
        }
        return h.Take(k - 1).ToList();
    }

    public static (V2 Min, V2 Max) Caixa(IEnumerable<V2> pts)
    {
        double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
        foreach (var p in pts)
        {
            if (p.X < x0) x0 = p.X; if (p.Y < y0) y0 = p.Y;
            if (p.X > x1) x1 = p.X; if (p.Y > y1) y1 = p.Y;
        }
        return (new V2(x0, y0), new V2(x1, y1));
    }

    /// <summary>Retângulo (4 vértices, anti-horário) de um corpo: origem o, rumo h, de x1 (trás) a x0 (frente), largura w centrada.</summary>
    public static V2[] Retangulo(V2 o, double h, double xFrente, double xTras, double w, double yCentro = 0)
    {
        double y0 = yCentro - w / 2, y1 = yCentro + w / 2;
        return new[]
        {
            V2.Local(o, h, xTras, y0), V2.Local(o, h, xFrente, y0),
            V2.Local(o, h, xFrente, y1), V2.Local(o, h, xTras, y1),
        };
    }

    /// <summary>Arredonda as coordenadas (para gravar no desenho sem inflar o JSON).</summary>
    public static List<V2> Arredondar(IEnumerable<V2> pts, int casas = 4) =>
        Limpar(pts.Select(p => new V2(Math.Round(p.X, casas), Math.Round(p.Y, casas))), Math.Pow(10, -casas));
}
