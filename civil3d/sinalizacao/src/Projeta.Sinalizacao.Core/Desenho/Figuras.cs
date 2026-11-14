using Projeta.Sinalizacao.Core.Comum;

namespace Projeta.Sinalizacao.Core.Desenho;

/// <summary>
/// Pictogramas simplificados (silhuetas) usados nas placas e nas inscrições do pavimento.
/// Coordenadas normalizadas: a figura ocupa aproximadamente x ∈ [-1, 1], y ∈ [-1, 1] em torno de <c>c</c>,
/// multiplicada pela escala <c>s</c>. São representações para projeto (planta), não arte-final de fabricação.
/// </summary>
public static class Figuras
{
    private static V2 P(V2 c, double s, double x, double y, bool esp = false) => c + new V2(esp ? -x * s : x * s, y * s);
    private static List<V2> L(V2 c, double s, bool e, params double[] xy)
    {
        var l = new List<V2>(xy.Length / 2);
        for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(P(c, s, xy[i], xy[i + 1], e));
        return l;
    }

    public static void Bicicleta(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.10 * s;
        Pincel.Anel(d, P(c, s, -0.58, -0.28, espelhar), 0.36 * s, e * 0.9, cor, 32);
        Pincel.Anel(d, P(c, s, 0.58, -0.28, espelhar), 0.36 * s, e * 0.9, cor, 32);
        Pincel.Tracos(d, L(c, s, espelhar, -0.58, -0.28, -0.05, -0.28, -0.22, 0.30, -0.58, -0.28), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.22, 0.30, 0.40, 0.30), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.05, -0.28, 0.40, 0.30), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.58, -0.28, 0.36, 0.48, 0.18, 0.52), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.34, 0.40, -0.10, 0.40), e * 1.2, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.22, 0.30, -0.22, 0.40), e, cor);
    }

    public static void Ciclista(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        Bicicleta(d, c + new V2(0, -0.12 * s), s * 0.9, cor, espelhar);
        double e = 0.11 * s;
        d.Disco(P(c, s, 0.22, 0.86, espelhar), 0.13 * s, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.18, 0.30, 0.10, 0.66, 0.28, 0.40, 0.30, 0.30), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.18, 0.30, 0.10, -0.05, -0.04, -0.33), e, cor);
    }

    public static void Pessoa(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.15 * s;
        d.Disco(P(c, s, 0.06, 0.80, espelhar), 0.16 * s, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.02, 0.55, -0.06, 0.02), e * 1.25, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.06, 0.02, 0.26, -0.42, 0.34, -0.92), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.06, 0.02, -0.28, -0.40, -0.48, -0.90), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.02, 0.48, 0.30, 0.18, 0.38, -0.04), e * 0.85, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.02, 0.48, -0.26, 0.20, -0.38, -0.02), e * 0.85, cor);
    }

    public static void Crianca(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        Pessoa(d, c, s * 0.8, cor, espelhar);
        // mochila
        d.Area(L(c, s, espelhar, -0.34, 0.04, -0.10, 0.04, -0.10, 0.34, -0.34, 0.34), cor);
    }

    public static void Trabalhador(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.14 * s;
        d.Disco(P(c, s, 0.18, 0.62, espelhar), 0.15 * s, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.08, 0.42, -0.30, -0.02), e * 1.3, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.30, -0.02, -0.08, -0.40, -0.12, -0.85), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.30, -0.02, -0.50, -0.42, -0.62, -0.85), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.04, 0.34, 0.30, 0.05, 0.42, -0.12), e * 0.85, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.10, 0.30, 0.62, -0.55), e * 0.6, cor);
        d.Area(L(c, s, espelhar, 0.52, -0.52, 0.76, -0.66, 0.66, -0.80, 0.44, -0.66), cor);
        d.Area(L(c, s, espelhar, 0.40, -0.95, 1.00, -0.95, 0.80, -0.70), cor);
    }

    public static void CarroLateral(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        d.Area(L(c, s, espelhar, -1.0, -0.22, 1.0, -0.22, 1.0, 0.10, 0.55, 0.16, 0.30, 0.50, -0.45, 0.50, -0.70, 0.14, -1.0, 0.10), cor);
        foreach (double x in new[] { -0.58, 0.58 })
        {
            d.Disco(P(c, s, x, -0.24, espelhar), 0.22 * s, cor);
            d.Disco(P(c, s, x, -0.24, espelhar), 0.08 * s, Cor.Branco);
        }
        d.Area(L(c, s, espelhar, -0.40, 0.20, -0.08, 0.20, -0.08, 0.42, -0.30, 0.42), Cor.Branco);
        d.Area(L(c, s, espelhar, 0.02, 0.20, 0.40, 0.20, 0.24, 0.42, 0.02, 0.42), Cor.Branco);
    }

    public static void CarroTraseira(Desenho2D d, V2 c, double s, Cor cor)
    {
        d.Area(L(c, s, false, -0.48, -0.30, 0.48, -0.30, 0.48, 0.12, 0.36, 0.48, -0.36, 0.48, -0.48, 0.12), cor);
        Pincel.Retangulo(d, c.X - 0.46 * s, c.Y - 0.48 * s, c.X - 0.28 * s, c.Y - 0.28 * s, cor);
        Pincel.Retangulo(d, c.X + 0.28 * s, c.Y - 0.48 * s, c.X + 0.46 * s, c.Y - 0.28 * s, cor);
        d.Area(L(c, s, false, -0.30, 0.16, 0.30, 0.16, 0.24, 0.40, -0.24, 0.40), Cor.Branco);
    }

    public static void Caminhao(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        d.Area(L(c, s, espelhar, -1.0, -0.20, 0.30, -0.20, 0.30, 0.58, -1.0, 0.58), cor);
        d.Area(L(c, s, espelhar, 0.36, -0.20, 1.0, -0.20, 1.0, 0.18, 0.86, 0.44, 0.36, 0.44), cor);
        d.Area(L(c, s, espelhar, 0.52, 0.16, 0.82, 0.16, 0.74, 0.34, 0.52, 0.34), Cor.Branco);
        foreach (double x in new[] { -0.72, -0.36, 0.72 })
        {
            d.Disco(P(c, s, x, -0.24, espelhar), 0.19 * s, cor);
            d.Disco(P(c, s, x, -0.24, espelhar), 0.07 * s, Cor.Branco);
        }
    }

    public static void Onibus(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        d.Area(L(c, s, espelhar, -1.0, -0.22, 1.0, -0.22, 1.0, 0.50, 0.9, 0.58, -1.0, 0.58), cor);
        for (int i = 0; i < 5; i++)
        {
            double x0 = -0.88 + i * 0.34;
            d.Area(L(c, s, espelhar, x0, 0.20, x0 + 0.26, 0.20, x0 + 0.26, 0.46, x0, 0.46), Cor.Branco);
        }
        foreach (double x in new[] { -0.60, 0.60 })
        {
            d.Disco(P(c, s, x, -0.24, espelhar), 0.19 * s, cor);
            d.Disco(P(c, s, x, -0.24, espelhar), 0.07 * s, Cor.Branco);
        }
    }

    public static void Moto(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.10 * s;
        Pincel.Anel(d, P(c, s, -0.62, -0.30, espelhar), 0.32 * s, e, cor, 32);
        Pincel.Anel(d, P(c, s, 0.62, -0.30, espelhar), 0.32 * s, e, cor, 32);
        d.Area(L(c, s, espelhar, -0.55, -0.10, 0.25, -0.10, 0.40, 0.18, -0.05, 0.22, -0.20, 0.10, -0.62, 0.10), cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.62, -0.30, 0.42, 0.30, 0.30, 0.42), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.62, -0.30, -0.30, -0.10), e, cor);
        d.Disco(P(c, s, -0.02, 0.78, espelhar), 0.14 * s, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.08, 0.60, -0.20, 0.20, 0.10, 0.02), e * 1.3, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.06, 0.52, 0.30, 0.40), e, cor);
    }

    public static void Trator(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.10 * s;
        Pincel.Anel(d, P(c, s, -0.45, -0.32, espelhar), 0.46 * s, e * 1.6, cor, 40);
        d.Disco(P(c, s, -0.45, -0.32, espelhar), 0.12 * s, cor);
        Pincel.Anel(d, P(c, s, 0.62, -0.50, espelhar), 0.26 * s, e * 1.3, cor, 32);
        d.Area(L(c, s, espelhar, -0.10, -0.25, 0.90, -0.25, 0.90, 0.12, -0.10, 0.12), cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.62, 0.16, -0.62, 0.80, -0.06, 0.80, -0.06, 0.16), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.60, 0.12, 0.60, 0.52), e, cor);
    }

    public static void Carroca(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.09 * s;
        Pincel.Anel(d, P(c, s, -0.55, -0.40, espelhar), 0.28 * s, e, cor, 32);
        d.Area(L(c, s, espelhar, -1.0, -0.10, -0.05, -0.10, -0.05, 0.20, -1.0, 0.20), cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.05, 0.0, 0.40, 0.0), e, cor);
        // cavalo
        d.Area(L(c, s, espelhar, 0.30, -0.10, 0.82, -0.10, 0.86, 0.22, 0.30, 0.22), cor);
        d.Area(L(c, s, espelhar, 0.74, 0.12, 0.94, 0.62, 1.04, 0.56, 0.90, 0.10), cor);
        d.Area(L(c, s, espelhar, 0.90, 0.50, 1.10, 0.44, 1.12, 0.56, 0.94, 0.66), cor);
        foreach (double x in new[] { 0.36, 0.48, 0.70, 0.80 })
            Pincel.Tracos(d, L(c, s, espelhar, x, -0.10, x, -0.68), e, cor);
    }

    public static void CarroDeMao(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.10 * s;
        d.Area(L(c, s, espelhar, -0.70, -0.05, 0.40, -0.05, 0.55, 0.40, -0.85, 0.40), cor);
        Pincel.Anel(d, P(c, s, -0.15, -0.45, espelhar), 0.30 * s, e, cor, 32);
        Pincel.Tracos(d, L(c, s, espelhar, 0.40, 0.10, 1.00, 0.55), e, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.15, -0.45, -0.15, -0.05), e, cor);
    }

    public static void Vaca(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.11 * s;
        d.Area(L(c, s, espelhar, -0.75, -0.05, 0.50, -0.05, 0.55, 0.42, -0.75, 0.42), cor);
        foreach (double x in new[] { -0.68, -0.48, 0.30, 0.46 }) Pincel.Tracos(d, L(c, s, espelhar, x, -0.05, x, -0.62), e, cor);
        d.Area(L(c, s, espelhar, 0.50, 0.25, 0.95, 0.45, 1.00, 0.20, 0.62, 0.05), cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.62, 0.40, 0.66, 0.62), e * 0.7, cor);
        Pincel.Tracos(d, L(c, s, espelhar, -0.75, 0.38, -0.92, 0.0), e * 0.6, cor);
    }

    public static void Cervo(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        double e = 0.09 * s;
        d.Area(L(c, s, espelhar, -0.70, -0.02, 0.35, -0.02, 0.40, 0.32, -0.70, 0.32), cor);
        foreach (double x in new[] { -0.62, -0.46, 0.20, 0.32 }) Pincel.Tracos(d, L(c, s, espelhar, x, -0.02, x, -0.70), e, cor);
        d.Area(L(c, s, espelhar, 0.28, 0.20, 0.50, 0.70, 0.66, 0.66, 0.44, 0.18), cor);
        d.Area(L(c, s, espelhar, 0.50, 0.66, 0.84, 0.62, 0.84, 0.74, 0.56, 0.80), cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.56, 0.78, 0.48, 1.0, 0.36, 1.06), e * 0.6, cor);
        Pincel.Tracos(d, L(c, s, espelhar, 0.62, 0.80, 0.70, 1.02, 0.84, 1.08), e * 0.6, cor);
    }

    public static void Trem(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        d.Area(L(c, s, espelhar, -1.0, -0.20, 0.70, -0.20, 1.0, 0.05, 1.0, 0.30, 0.15, 0.30, 0.15, 0.62, -0.40, 0.62, -0.40, 0.30, -1.0, 0.30), cor);
        d.Area(L(c, s, espelhar, 0.40, 0.30, 0.56, 0.30, 0.60, 0.62, 0.36, 0.62), cor);
        d.Area(L(c, s, espelhar, -0.28, 0.38, 0.02, 0.38, 0.02, 0.54, -0.28, 0.54), Cor.Branco);
        foreach (double x in new[] { -0.72, -0.30, 0.12, 0.54 }) d.Disco(P(c, s, x, -0.30, espelhar), 0.16 * s, cor);
    }

    public static void Bonde(Desenho2D d, V2 c, double s, Cor cor)
    {
        d.Area(L(c, s, false, -0.90, -0.30, 0.90, -0.30, 0.90, 0.40, -0.90, 0.40), cor);
        for (int i = 0; i < 4; i++)
        {
            double x0 = -0.78 + i * 0.40;
            d.Area(L(c, s, false, x0, 0.05, x0 + 0.30, 0.05, x0 + 0.30, 0.30, x0, 0.30), Cor.Branco);
        }
        Pincel.Tracos(d, L(c, s, false, 0, 0.40, -0.25, 0.80, 0.25, 0.95), 0.08 * s, cor);
        Pincel.Tracos(d, L(c, s, false, -1.0, -0.45, 1.0, -0.45), 0.06 * s, cor);
    }

    public static void Aviao(Desenho2D d, V2 c, double s, Cor cor)
    {
        d.Area(L(c, s, false, -0.10, -0.85, 0.10, -0.85, 0.10, 0.80, 0.0, 0.95, -0.10, 0.80), cor);
        d.Area(L(c, s, false, -0.95, 0.0, -0.10, 0.35, 0.10, 0.35, 0.95, 0.0, 0.95, -0.12, 0.10, 0.08, -0.10, 0.08, -0.95, -0.12), cor);
        d.Area(L(c, s, false, -0.38, -0.85, -0.10, -0.62, 0.10, -0.62, 0.38, -0.85, 0.38, -0.95, -0.38, -0.95), cor);
    }

    public static void Buzina(Desenho2D d, V2 c, double s, Cor cor)
    {
        d.Area(L(c, s, false, -0.65, -0.14, -0.15, -0.14, 0.75, -0.55, 0.75, 0.55, -0.15, 0.14, -0.65, 0.14), cor);
        d.Disco(c + new V2(-0.72 * s, 0), 0.24 * s, cor);
    }

    public static void PneuCorrente(Desenho2D d, V2 c, double s, Cor cor)
    {
        Pincel.Anel(d, c, 0.62 * s, 0.24 * s, cor, 40);
        d.Disco(c, 0.12 * s, cor);
        for (int i = 0; i < 10; i++)
        {
            double a = 2 * Math.PI * i / 10;
            Pincel.Tracos(d, new[] { c + V2.Polar(a, 0.40 * s), c + V2.Polar(a, 0.86 * s) }, 0.07 * s, cor);
        }
    }

    public static void CadeiraDeRodas(Desenho2D d, V2 c, double s, Cor cor)
    {
        double e = 0.14 * s;
        d.Disco(c + new V2(-0.05 * s, 0.82 * s), 0.15 * s, cor);
        Pincel.Tracos(d, L(c, s, false, -0.12, 0.56, -0.18, 0.02, 0.36, 0.02, 0.52, -0.52, 0.72, -0.48), e, cor);
        Pincel.Tracos(d, L(c, s, false, -0.14, 0.36, 0.30, 0.36), e * 0.8, cor);
        Pincel.Anel(d, c + new V2(-0.10 * s, -0.36 * s), 0.46 * s, e * 0.8, cor, 40);
    }

    public static void Semaforo(Desenho2D d, V2 c, double s)
    {
        d.Area(L(c, s, false, -0.36, -0.95, 0.36, -0.95, 0.36, 0.95, -0.36, 0.95), Cor.Preto);
        d.Disco(c + new V2(0, 0.58 * s), 0.23 * s, Cor.Vermelho);
        d.Disco(c, 0.23 * s, Cor.Amarelo);
        d.Disco(c + new V2(0, -0.58 * s), 0.23 * s, Cor.Verde);
    }

    public static void Biruta(Desenho2D d, V2 c, double s, Cor cor, bool espelhar = false)
    {
        Pincel.Tracos(d, L(c, s, espelhar, -0.70, -0.95, -0.70, 0.75), 0.10 * s, cor);
        d.Area(L(c, s, espelhar, -0.65, 0.40, 0.80, 0.25, 0.80, 0.05, -0.65, 0.80), cor);
        d.Area(L(c, s, espelhar, -0.10, 0.36, 0.20, 0.33, 0.20, 0.48, -0.10, 0.58), Cor.Branco);
    }

    public static void Lombada(Desenho2D d, V2 c, double s, Cor cor)
    {
        var pts = new List<V2> { c + new V2(-0.95 * s, -0.25 * s) };
        for (int i = 0; i <= 24; i++)
        {
            double x = -0.55 + 1.1 * i / 24.0;
            pts.Add(c + new V2(x * s, (-0.25 + 0.42 * Math.Sin(Math.PI * i / 24.0)) * s));
        }
        pts.Add(c + new V2(0.95 * s, -0.25 * s));
        pts.Add(c + new V2(0.95 * s, -0.42 * s));
        pts.Add(c + new V2(-0.95 * s, -0.42 * s));
        d.Area(pts, cor);
    }

    public static void Depressao(Desenho2D d, V2 c, double s, Cor cor)
    {
        var pts = new List<V2> { c + new V2(-0.95 * s, 0.10 * s) };
        for (int i = 0; i <= 24; i++)
        {
            double x = -0.55 + 1.1 * i / 24.0;
            pts.Add(c + new V2(x * s, (0.10 - 0.40 * Math.Sin(Math.PI * i / 24.0)) * s));
        }
        pts.Add(c + new V2(0.95 * s, 0.10 * s));
        pts.Add(c + new V2(0.95 * s, -0.45 * s));
        pts.Add(c + new V2(-0.95 * s, -0.45 * s));
        d.Area(pts, cor);
    }

    public static void PistaIrregular(Desenho2D d, V2 c, double s, Cor cor)
    {
        var pts = new List<V2> { c + new V2(-0.95 * s, -0.25 * s) };
        for (int i = 0; i <= 40; i++)
        {
            double x = -0.8 + 1.6 * i / 40.0;
            pts.Add(c + new V2(x * s, (-0.25 + 0.28 * Math.Abs(Math.Sin(2 * Math.PI * i / 40.0))) * s));
        }
        pts.Add(c + new V2(0.95 * s, -0.25 * s));
        pts.Add(c + new V2(0.95 * s, -0.42 * s));
        pts.Add(c + new V2(-0.95 * s, -0.42 * s));
        d.Area(pts, cor);
    }
}
