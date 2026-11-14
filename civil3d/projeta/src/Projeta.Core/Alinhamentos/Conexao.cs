namespace Projeta.Core.Alinhamentos;

/// <summary>Ponto (x, y) de um eixo numa estaca e afastamento (positivo à direita, como no Civil 3D).</summary>
public delegate (double X, double Y) PontoNoEixo(double estaca, double afastamento);

/// <summary>Eixo horizontal visto só pela função estaca/afastamento → ponto (vale para alinhamento do Civil, reta ou círculo).</summary>
public sealed class EixoGeometrico
{
    public string Nome { get; init; } = "";
    public double Inicio { get; init; }
    public double Fim { get; init; }
    public PontoNoEixo Ponto { get; init; } = (_, _) => (0, 0);

    /// <summary>Direção (unitária) do eixo na estaca, no sentido crescente.</summary>
    public (double Dx, double Dy) Direcao(double s)
    {
        // passo pequeno: numa curva de raio R a corda de comprimento h desvia h/(2R) rad da tangente
        double h = Math.Max(1e-5, Math.Min(1e-3, (Fim - Inicio) / 4));
        double a = Math.Max(Inicio, s - h), b = Math.Min(Fim, s + h);
        if (b - a < 1e-9) { a = Math.Max(Inicio, b - h); }
        var p = Ponto(a, 0);
        var q = Ponto(b, 0);
        double dx = q.X - p.X, dy = q.Y - p.Y, n = Math.Sqrt(dx * dx + dy * dy);
        return n < 1e-12 ? (1, 0) : (dx / n, dy / n);
    }

    public static EixoGeometrico Reta(string nome, double x0, double y0, double x1, double y1)
    {
        double dx = x1 - x0, dy = y1 - y0, l = Math.Sqrt(dx * dx + dy * dy);
        double ux = dx / l, uy = dy / l;
        // direita = direção girada −90°: (uy, −ux)
        return new EixoGeometrico { Nome = nome, Inicio = 0, Fim = l, Ponto = (s, o) => (x0 + ux * s + uy * o, y0 + uy * s - ux * o) };
    }

    /// <summary>Círculo de raio <paramref name="raio"/> começando no ângulo <paramref name="angIni"/> (rad), anti-horário ou horário.</summary>
    public static EixoGeometrico Circulo(string nome, double cx, double cy, double raio, double angIni = 0, bool antiHorario = true, double voltas = 1)
    {
        double l = 2 * Math.PI * raio * voltas;
        return antiHorario
            ? new EixoGeometrico { Nome = nome, Inicio = 0, Fim = l, Ponto = (s, o) => { double t = angIni + s / raio; return (cx + (raio + o) * Math.Cos(t), cy + (raio + o) * Math.Sin(t)); } }
            : new EixoGeometrico { Nome = nome, Inicio = 0, Fim = l, Ponto = (s, o) => { double t = angIni - s / raio; return (cx + (raio - o) * Math.Cos(t), cy + (raio - o) * Math.Sin(t)); } };
    }
}

/// <summary>Arco circular tangente a dois eixos (ou aos bordos afastados deles).</summary>
public sealed record Concordancia(
    double EstacaA, double EstacaB,
    (double X, double Y) Centro, (double X, double Y) TangenteA, (double X, double Y) TangenteB,
    double Raio, bool AntiHorario, double AnguloCentral, double Desenvolvimento,
    int SentidoA, int SentidoB)
{
    /// <summary>Ponto do meio do arco.</summary>
    public (double X, double Y) Meio
    {
        get
        {
            double a = Math.Atan2(TangenteA.Y - Centro.Y, TangenteA.X - Centro.X) + (AntiHorario ? 1 : -1) * AnguloCentral / 2;
            return (Centro.X + Raio * Math.Cos(a), Centro.Y + Raio * Math.Sin(a));
        }
    }

    /// <summary>"Bulge" da polilinha do AutoCAD para o arco de TangenteA a TangenteB (positivo = anti-horário).</summary>
    public double Bulge => (AntiHorario ? 1 : -1) * Math.Tan(AnguloCentral / 4);
}

/// <summary>
/// Conexão entre dois eixos (curb return / concordância de ramo de rotatória): o centro do arco fica a (afastamento + raio)
/// de cada eixo, do lado escolhido; as estacas de tangência saem por Newton sobre as funções dos eixos, então vale para
/// tangentes, curvas e espirais do Civil 3D.
/// </summary>
public static class Conexao
{
    /// <param name="ladoA">+1 = o arco fica à direita de A; −1 = à esquerda.</param>
    /// <param name="afastA">Distância do eixo A ao bordo onde o arco encosta (0 = no eixo).</param>
    /// <returns>null quando não há arco desse raio que toque os dois dentro dos limites dos eixos.</returns>
    public static Concordancia? Calcular(EixoGeometrico a, double estacaA0, int ladoA, double afastA,
                                         EixoGeometrico b, double estacaB0, int ladoB, double afastB, double raio)
    {
        if (!(raio > 0)) throw new ArgumentException("O raio deve ser positivo.");
        double oa = ladoA * (afastA + raio), ob = ladoB * (afastB + raio);
        double sa = Math.Clamp(estacaA0, a.Inicio, a.Fim), sb = Math.Clamp(estacaB0, b.Inicio, b.Fim);
        double passoMax = Math.Max(5, raio);
        bool ok = false;
        for (int it = 0; it < 80; it++)
        {
            var pa = a.Ponto(sa, oa);
            var pb = b.Ponto(sb, ob);
            double fx = pa.X - pb.X, fy = pa.Y - pb.Y;
            if (Math.Sqrt(fx * fx + fy * fy) < 1e-8) { ok = true; break; }
            const double h = 1e-3;
            double ha = sa + h <= a.Fim ? h : -h, hb = sb + h <= b.Fim ? h : -h;
            var pa2 = a.Ponto(sa + ha, oa);
            var pb2 = b.Ponto(sb + hb, ob);
            double j11 = (pa2.X - pa.X) / ha, j21 = (pa2.Y - pa.Y) / ha;
            double j12 = -(pb2.X - pb.X) / hb, j22 = -(pb2.Y - pb.Y) / hb;
            double det = j11 * j22 - j12 * j21;
            if (Math.Abs(det) < 1e-12) return null;
            double dsa = -(j22 * fx - j12 * fy) / det, dsb = -(-j21 * fx + j11 * fy) / det;
            double maior = Math.Max(Math.Abs(dsa), Math.Abs(dsb));
            if (maior > passoMax) { dsa *= passoMax / maior; dsb *= passoMax / maior; }
            sa = Math.Clamp(sa + dsa, a.Inicio, a.Fim);
            sb = Math.Clamp(sb + dsb, b.Inicio, b.Fim);
        }
        if (!ok) return null;
        var c = a.Ponto(sa, oa);
        var ta = a.Ponto(sa, ladoA * afastA);
        var tb = b.Ponto(sb, ladoB * afastB);
        double angA = Math.Atan2(ta.Y - c.Y, ta.X - c.X), angB = Math.Atan2(tb.Y - c.Y, tb.X - c.X);
        double delta = angB - angA;
        while (delta > Math.PI) delta -= 2 * Math.PI;
        while (delta <= -Math.PI) delta += 2 * Math.PI;
        bool anti = delta > 0;
        double central = Math.Abs(delta);
        // direção do arco na saída (em TA) e na chegada (em TB): tangente = raio girado ±90°
        (double X, double Y) Tang((double X, double Y) p) { double rx = p.X - c.X, ry = p.Y - c.Y, n = Math.Sqrt(rx * rx + ry * ry); return anti ? (-ry / n, rx / n) : (ry / n, -rx / n); }
        var t0 = Tang(ta);
        var t1 = Tang(tb);
        var da = a.Direcao(sa);
        var db = b.Direcao(sb);
        int sentA = t0.X * da.Dx + t0.Y * da.Dy >= 0 ? 1 : -1;
        int sentB = t1.X * db.Dx + t1.Y * db.Dy >= 0 ? 1 : -1;
        return new Concordancia(sa, sb, c, ta, tb, raio, anti, central, raio * central, sentA, sentB);
    }
}

/// <summary>Greide da conexão: sai com a cota e a rampa de um pai e chega com as do outro.</summary>
public static class PerfilConexao
{
    /// <summary>PVI do greide (Curva = comprimento da parábola simétrica; 0 = sem curva).</summary>
    public sealed record Pvi(double Estaca, double Cota, double Curva);

    /// <summary>
    /// Sai de z0 com a rampa <paramref name="g0"/> e chega em z1 com a rampa <paramref name="g1"/>, sempre (é o que faz a
    /// conexão casar com os dois pais): se as duas tangentes se cruzam dentro do trecho, um PVI no cruzamento com a
    /// parábola que cabe; se não (rampas paralelas, ou cruzamento fora), dois PVIs a 1/3 e 2/3 com uma parábola em cada.
    /// Só vira rampa única quando as duas rampas já são a reta de z0 a z1. Rampas em fração (0,02 = 2 %).
    /// </summary>
    public static List<Pvi> Calcular(double comprimento, double z0, double g0, double z1, double g1)
    {
        if (!(comprimento > 0)) throw new ArgumentException("Comprimento da conexão inválido.");
        double l = comprimento, reta = (z1 - z0) / l;
        var r = new List<Pvi> { new(0, z0, 0) };
        if (Math.Abs(g0 - reta) > 1e-6 || Math.Abs(g1 - reta) > 1e-6)
        {
            double dg = g0 - g1;
            double x = Math.Abs(dg) > 1e-9 ? (z1 - z0 - g1 * l) / dg : double.NaN;
            if (x > 0.02 * l && x < 0.98 * l) r.Add(new Pvi(x, z0 + g0 * x, 2 * Math.Min(x, l - x) * 0.98));
            else
            {
                double x1 = l / 3, x2 = 2 * l / 3, c = l / 3 * 0.98;
                r.Add(new Pvi(x1, z0 + g0 * x1, c));
                r.Add(new Pvi(x2, z1 - g1 * (l - x2), c));
            }
        }
        r.Add(new Pvi(l, z1, 0));
        return r;
    }

    /// <summary>Cota no greide (tangentes entre os PVIs e parábolas simétricas nos PVIs internos).</summary>
    public static double Cota(IReadOnlyList<Pvi> p, double s)
    {
        for (int i = 1; i < p.Count - 1; i++)
        {
            var v = p[i];
            if (v.Curva <= 0) continue;
            double ini = v.Estaca - v.Curva / 2, fim = v.Estaca + v.Curva / 2;
            if (s <= ini || s >= fim) continue;
            double g0 = (v.Cota - p[i - 1].Cota) / (v.Estaca - p[i - 1].Estaca), g1 = (p[i + 1].Cota - v.Cota) / (p[i + 1].Estaca - v.Estaca);
            double x = s - ini;
            return v.Cota - g0 * v.Curva / 2 + g0 * x + (g1 - g0) * x * x / (2 * v.Curva);
        }
        int k = 0;
        while (k < p.Count - 2 && s > p[k + 1].Estaca) k++;
        return p[k].Cota + (p[k + 1].Cota - p[k].Cota) * (s - p[k].Estaca) / (p[k + 1].Estaca - p[k].Estaca);
    }
}

/// <summary>Avaliação altimétrica de um encontro entre dois eixos.</summary>
public static class Casamento
{
    public sealed record Avaliacao(double CotaAAjustada, double Dz, double RampaANaDirecaoB, double Di, bool CotaCasa, bool RampaComparavel, bool RampaCasa);

    /// <param name="angulo">Ângulo entre as direções de A e B no encontro (rad).</param>
    /// <param name="afastamento">Distância do ponto ao eixo de A (m).</param>
    /// <param name="caimento">Caimento transversal de A do eixo até o ponto (fração; −0,02 = desce 2 %).</param>
    public static Avaliacao Avaliar(double zA, double gA, double zB, double gB, double angulo, double afastamento, double caimento,
                                    double tolCota = 0.01, double tolRampa = 0.005)
    {
        double zAa = zA + caimento * Math.Abs(afastamento);
        double gDir = gA * Math.Cos(angulo);
        double dz = zB - zAa, di = gB - gDir;
        bool comparavel = Math.Abs(Math.Cos(angulo)) >= Math.Cos(Math.PI / 4);
        return new Avaliacao(zAa, dz, gDir, di, Math.Abs(dz) <= tolCota, comparavel, !comparavel || Math.Abs(di) <= tolRampa);
    }
}
