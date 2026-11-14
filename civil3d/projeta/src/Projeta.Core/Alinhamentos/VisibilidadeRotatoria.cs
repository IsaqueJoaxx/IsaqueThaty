namespace Projeta.Core.Alinhamentos;

/// <summary>Triângulo de visibilidade de uma entrada: o motorista parado perto da retenção precisa ver os dois fluxos que vêm da esquerda.</summary>
/// <param name="Observador">Motorista na faixa de entrada, a 15 m da linha de retenção.</param>
/// <param name="Circulante">Veículo que já circula no anel, a d2 da entrada (contado pelo meio da pista anelar).</param>
/// <param name="Entrante">Veículo que entra pelo braço anterior (de montante), a d1 da entrada.</param>
/// <param name="PernaCirculante">Caminho do veículo circulante até a entrada (para desenhar a perna do triângulo).</param>
/// <param name="PernaEntrante">Caminho do veículo entrante até a entrada.</param>
/// <param name="Atravessa">O que as visadas cruzam (ilha central, cantos fora da pista, ilhas divisórias).</param>
public sealed record Visada(int Braco, (double X, double Y) Observador, (double X, double Y) Circulante, (double X, double Y) Entrante,
                            double D1, double D2, double VEntrante, double VCirculante,
                            List<(double X, double Y)> PernaCirculante, List<(double X, double Y)> PernaEntrante, List<string> Atravessa);

/// <summary>
/// Visibilidade à esquerda nas entradas (NCHRP 672, seção 6.7.3): d1 = 0,278·tc·Ve (fluxo que entra pelo braço de montante)
/// e d2 = 0,278·tc·Vc (fluxo que já circula), com tc = 5 s e o observador a 15 m da linha de retenção. Velocidades: Ve ≈ média
/// de V1 e V2 do braço de montante e Vc ≈ V4 (trajetória à esquerda), como o NCHRP 672 sugere.
/// </summary>
public static class VisibilidadeRotatoria
{
    public const double TcPadrao = 5.0, RecuoObservador = 15.0;

    public static double Distancia(double velocidadeKmh, double tc = TcPadrao) => 0.278 * tc * velocidadeKmh;

    public static Visada? Calcular(ModeloRotatoria m, int braco, double vEntrante, double vCirculante, double tc = TcPadrao)
    {
        int n = m.Bracos.Count;
        if (n < 2 || double.IsNaN(vEntrante) || double.IsNaN(vCirculante) || double.IsInfinity(vEntrante) || double.IsInfinity(vCirculante)) return null;
        var b = m.Bracos[braco];
        var up = m.Bracos[(braco + n - 1) % n]; // de montante: os veículos dele chegam pela esquerda (sentido anti-horário)
        double rm = (m.RIlha + m.RExt) / 2;
        double Meio(BracoRotatoria x, double s) => (x.MeiaIlha(s) + x.Bordo(s, true)) / 2;

        double so = b.SRetencao + RecuoObservador;
        var obs = b.Eixo.Ponto(so, Meio(b, so));
        var e = b.Eixo.Ponto(b.SRetencao, Meio(b, b.SRetencao));
        double ae = Math.Atan2(e.Y - m.Oy, e.X - m.Ox);
        (double X, double Y) NoAnel(double a) => (m.Ox + rm * Math.Cos(a), m.Oy + rm * Math.Sin(a));

        double d1 = Distancia(vEntrante, tc), d2 = Distancia(vCirculante, tc);

        // circulante: volta pelo anel no sentido horário (de onde ele vem) por d2
        var pc = new List<(double X, double Y)> { e };
        double giro2 = Math.Min(d2 / rm, 2 * Math.PI - 0.01);
        for (int k = 0; k <= 24; k++) pc.Add(NoAnel(ae - giro2 * k / 24));
        var circ = pc[^1];

        // entrante: pelo anel até a entrada de montante e depois para fora na faixa de entrada dela
        var pe = new List<(double X, double Y)> { e };
        var eu = up.Eixo.Ponto(up.SRetencao, Meio(up, up.SRetencao));
        double au = Math.Atan2(eu.Y - m.Oy, eu.X - m.Ox);
        double arco = rm * GeometriaRotatoria.Normalizar(ae - au);
        (double X, double Y) ent;
        if (d1 <= arco || ReferenceEquals(up, b))
        {
            double g = d1 / rm;
            for (int k = 0; k <= 24; k++) pe.Add(NoAnel(ae - g * k / 24));
            ent = pe[^1];
        }
        else
        {
            double g = arco / rm;
            for (int k = 0; k <= 24; k++) pe.Add(NoAnel(ae - g * k / 24));
            double resto = d1 - arco;
            for (double s = 0; s <= resto; s += 1) pe.Add(up.Eixo.Ponto(up.SRetencao + s, Meio(up, up.SRetencao + s)));
            ent = up.Eixo.Ponto(up.SRetencao + resto, Meio(up, up.SRetencao + resto));
            pe.Add(ent);
        }

        var cruza = new List<string>();
        foreach (var (alvo, nome) in new[] { (circ, "circulante"), (ent, "entrante") })
        {
            if (CortaCirculo(obs, alvo, m.Ox, m.Oy, m.RIlha)) cruza.Add($"a visada do {nome} atravessa a ilha central");
            foreach (var x in m.Bracos)
                foreach (var c in new[] { x.MeioFioEntrada, x.MeioFioSaida })
                    if (c is { } cc && CortaCirculo(obs, alvo, cc.X, cc.Y, cc.R) && !cruza.Contains($"a visada do {nome} passa por fora da pista (canto do braço {x.Rumo})"))
                        cruza.Add($"a visada do {nome} passa por fora da pista (canto do braço {x.Rumo})");
            foreach (var x in m.Bracos)
                if (x.TemIlha && CortaPoligono(obs, alvo, m.Ilha(x))) cruza.Add($"a visada do {nome} atravessa a ilha divisória do braço {x.Rumo}");
        }
        return new Visada(braco, obs, circ, ent, d1, d2, vEntrante, vCirculante, pc, pe, cruza);
    }

    /// <summary>O segmento a–b passa por dentro do círculo?</summary>
    public static bool CortaCirculo((double X, double Y) a, (double X, double Y) b, double cx, double cy, double r)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
        double t = l2 > 0 ? Math.Clamp(((cx - a.X) * dx + (cy - a.Y) * dy) / l2, 0, 1) : 0;
        double px = a.X + t * dx - cx, py = a.Y + t * dy - cy;
        return px * px + py * py < r * r;
    }

    /// <summary>O segmento a–b cruza o contorno do polígono (ou fica dentro dele)?</summary>
    public static bool CortaPoligono((double X, double Y) a, (double X, double Y) b, IReadOnlyList<(double X, double Y)> p)
    {
        if (p.Count < 3) return false;
        static double Cr((double X, double Y) o, (double X, double Y) u, (double X, double Y) v) => (u.X - o.X) * (v.Y - o.Y) - (u.Y - o.Y) * (v.X - o.X);
        for (int i = 0; i < p.Count; i++)
        {
            var c = p[i]; var d = p[(i + 1) % p.Count];
            double d1 = Cr(a, b, c), d2 = Cr(a, b, d), d3 = Cr(c, d, a), d4 = Cr(c, d, b);
            if (((d1 > 0) != (d2 > 0)) && ((d3 > 0) != (d4 > 0))) return true;
        }
        // dentro: teste do raio a partir do ponto médio
        var mid = ((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        bool dentro = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            if ((p[i].Y > mid.Item2) != (p[j].Y > mid.Item2) && mid.Item1 < (p[j].X - p[i].X) * (mid.Item2 - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X) dentro = !dentro;
        return dentro;
    }

    /// <summary>Velocidades do NCHRP 672 para as pernas: entrante = média de V1 e V2 do braço de montante; circulante = V4 dele.</summary>
    public static (double VEntrante, double VCirculante)? Velocidades(IReadOnlyList<AnaliseBraco> analises, int bracoDeMontante)
    {
        var a = analises.FirstOrDefault(x => x.Braco == bracoDeMontante);
        if (a is null) return null;
        double? ve = a.R1 is { } r1 && a.R2 is { } r2 ? (r1.Velocidade + r2.Velocidade) / 2 : a.R1?.Velocidade ?? a.R2?.Velocidade;
        double? vc = a.R4?.Velocidade ?? a.R2?.Velocidade;
        if (ve is null || vc is null) return null;
        return (ve.Value, vc.Value);
    }
}
