using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;

namespace Projeta.Giro.Core.Cinematica;

/// <summary>Trecho de uma manobra: caminho do ponto de referência (eixo dianteiro; na ré, eixo traseiro).</summary>
public sealed class Trecho
{
    public bool Re { get; set; }
    public List<V2> Pontos { get; set; } = new();
}

/// <summary>Manobra completa (o que fica gravado no desenho para refazer, verificar e animar).</summary>
public sealed class Manobra
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    /// <summary>arco, seguir, gabarito, rotatoria, vaga.</summary>
    public string Modo { get; set; } = "arco";
    public string Nome { get; set; } = "";
    public Veiculo Veiculo { get; set; } = new();
    public double Velocidade { get; set; } = 10;
    public double? Raio { get; set; }
    /// <summary>Posição inicial do eixo dianteiro (veículo alinhado).</summary>
    public V2 Inicio { get; set; }
    public double RumoInicial { get; set; }
    public List<Trecho> Trechos { get; set; } = new();
    /// <summary>Handle da curva seguida (modo seguir), para atualizar quando ela mudar.</summary>
    public string? HandleCaminho { get; set; }
    public bool CaminhoInvertido { get; set; }
    /// <summary>Desenhar o veículo a cada X m (0 = só no início e no fim).</summary>
    public double IntervaloVeiculos { get; set; }
}

public sealed class ResultadoVarredura
{
    public Manobra Manobra { get; init; } = new();
    public List<Pose> Poses { get; init; } = new();
    /// <summary>Caminho efetivamente percorrido pelo eixo dianteiro.</summary>
    public List<V2> CaminhoDianteiro { get; init; } = new();
    public List<List<V2>> Envoltoria { get; init; } = new();
    public List<List<V2>> Rodas { get; init; } = new();
    public List<Mensagem> Avisos { get; init; } = new();
    public double Comprimento { get; init; }
    public double LarguraMaxima { get; set; }
    public double SLarguraMaxima { get; set; }
    public double DeltaMaximo { get; init; }
    public double ArticulacaoMaxima { get; init; }

    /// <summary>Pose mais próxima da distância s.</summary>
    public Pose PoseEm(double s)
    {
        if (Poses.Count == 0) throw new InvalidOperationException("sem poses");
        int a = 0, b = Poses.Count - 1;
        while (b - a > 1) { int m = (a + b) / 2; if (Poses[m].S < s) a = m; else b = m; }
        return Math.Abs(Poses[a].S - s) <= Math.Abs(Poses[b].S - s) ? Poses[a] : Poses[b];
    }
}

public sealed class OpcoesVarredura
{
    /// <summary>Passo da simulação (m).</summary>
    public double Passo { get; set; } = 0.10;
    /// <summary>Espaçamento das posições usadas para montar a envoltória (m).</summary>
    public double PassoEnvoltoria { get; set; } = 0.25;
    public bool CalcularEnvoltoria { get; set; } = true;
    public bool CalcularLargura { get; set; } = true;
}

/// <summary>Simulação do caminho de varredura.</summary>
public static class Varredura
{
    private sealed record Estado(double S, PoseUnidade[] U, bool Re, int Trecho);

    public static ResultadoVarredura Simular(Manobra m, OpcoesVarredura? op = null)
    {
        op ??= new OpcoesVarredura();
        var v = m.Veiculo;
        var avisos = new List<Mensagem>();
        foreach (var e in v.Validar().Where(x => x.Gravidade == Gravidade.Erro)) throw new ErroGiro("veículo inválido: " + e.Texto);

        var est = new List<Estado>();
        var U = Seguidor.Alinhado(v, m.Inicio, m.RumoInicial);
        double s = 0;
        est.Add(new Estado(0, U, m.Trechos.FirstOrDefault()?.Re ?? false, 0));
        double l = v.Motriz.EntreEixos;

        for (int t = 0; t < m.Trechos.Count; t++)
        {
            var tr = m.Trechos[t];
            if (tr.Pontos.Count < 2) continue;
            if (tr.Re && v.Articulado)
            {
                avisos.Add(new(Gravidade.Erro, $"trecho {t + 1}: marcha à ré com reboque não é simulada nesta versão — trecho ignorado."));
                continue;
            }
            var refAtual = tr.Re ? U[0].Ponto(-l) : U[0].Guia;
            var bruto = new List<V2>(tr.Pontos);
            if (bruto[0].Dist(refAtual) > 0.02) bruto.Insert(0, refAtual);
            var pts = Geo.Reamostrar(bruto, op.Passo);
            for (int k = 1; k < pts.Count; k++)
            {
                double ds = pts[k].Dist(pts[k - 1]);
                if (ds < 1e-9) continue;
                U = tr.Re ? Seguidor.RePeloEixoTraseiro(v, pts[k], (pts[k] - pts[k - 1]).Angulo) : Seguidor.Avancar(v, U, pts[k]);
                s += ds;
                est.Add(new Estado(s, U, tr.Re, t));
            }
        }

        // esterçamento: suavizado numa janela de 1 m (o caminho vem discretizado em cordas)
        var deltas = new double[est.Count];
        const double janela = 1.0;
        int meia = Math.Max(1, (int)Math.Round(janela / 2 / op.Passo));
        for (int i = 0; i < est.Count; i++)
        {
            // janela simétrica dentro do mesmo trecho (assimétrica só se não houver vizinho de um dos lados)
            int ka = 0, kb = 0;
            while (ka < meia && i - ka - 1 >= 0 && est[i - ka - 1].Trecho == est[i].Trecho) ka++;
            while (kb < meia && i + kb + 1 < est.Count && est[i + kb + 1].Trecho == est[i].Trecho) kb++;
            int k = Math.Min(ka, kb);
            int a = i - (k > 0 ? k : Math.Min(ka, 1)), b = i + (k > 0 ? k : Math.Min(kb, 1));
            if (b == a) { if (i > 0) deltas[i] = deltas[i - 1]; continue; }
            if (!est[i].Re)
            {
                var d = est[b].U[0].Guia - est[a].U[0].Guia;
                if (d.Comprimento < 1e-9) { deltas[i] = i > 0 ? deltas[i - 1] : 0; continue; }
                deltas[i] = Geo.AngNorm(d.Angulo - est[i].U[0].Rumo);
            }
            else
            {
                double dth = Geo.AngNorm(est[b].U[0].Rumo - est[a].U[0].Rumo);
                double ds = est[b].S - est[a].S;
                deltas[i] = ds < 1e-9 ? 0 : Math.Atan(-l * dth / ds);
            }
        }

        var poses = new List<Pose>(est.Count);
        for (int i = 0; i < est.Count; i++) poses.Add(new Pose { S = est[i].S, U = est[i].U, Re = est[i].Re, Delta = deltas[i] });

        // avisos
        double dMax = v.AnguloMaximo;
        double taxa = Autoconducao.TaxaPorMetro(v, m.Velocidade);
        Faixas(poses, p => Math.Abs(p.Delta) > dMax + Num.Rad(0.5),
            (a, b, pk) => avisos.Add(new(Gravidade.Aviso,
                $"esterçamento de {Num.F(Num.Graus(Math.Abs(pk.Delta)), 1)}° acima do máximo do veículo ({Num.F(Num.Graus(dMax), 1)}°) entre {Num.F(a, 1)} m e {Num.F(b, 1)} m — o raio do caminho está menor que o mínimo.")),
            p => Math.Abs(p.Delta));
        if (!double.IsInfinity(taxa))
        {
            var taxaReal = new double[poses.Count];
            for (int i = 0; i < poses.Count; i++)
            {
                int a = Math.Max(0, i - meia), b = Math.Min(poses.Count - 1, i + meia);
                double ds = poses[b].S - poses[a].S;
                taxaReal[i] = ds < 1e-9 ? 0 : Math.Abs(poses[b].Delta - poses[a].Delta) / ds;
            }
            int idx = 0;
            var mapa = poses.ToDictionary(p => p, _ => idx++);
            Faixas(poses, p => taxaReal[mapa[p]] > taxa * 1.25 && Math.Abs(p.Delta) > Num.Rad(1),
                (a, b, _) => avisos.Add(new(Gravidade.Aviso,
                    $"volante girando mais rápido que o possível a {Num.F(m.Velocidade, 0)} km/h entre {Num.F(a, 1)} m e {Num.F(b, 1)} m (mudança brusca de curvatura no caminho).")),
                p => taxaReal[mapa[p]]);
        }
        double artMax = 0;
        if (v.Articulado)
        {
            double lim = Num.Rad(v.ArticulacaoMaximaGraus);
            for (int i = 1; i < v.Unidades.Count; i++)
            {
                int ui = i;
                Faixas(poses, p => Math.Abs(p.Articulacao(ui)) > lim,
                    (a, b, pk) => avisos.Add(new(Gravidade.Aviso,
                        $"articulação {v.Unidades[ui - 1].Nome} × {v.Unidades[ui].Nome} chega a {Num.F(Num.Graus(Math.Abs(pk.Articulacao(ui))), 0)}° (limite {Num.F(v.ArticulacaoMaximaGraus, 0)}°) entre {Num.F(a, 1)} m e {Num.F(b, 1)} m.")),
                    p => Math.Abs(p.Articulacao(ui)));
                artMax = Math.Max(artMax, poses.Max(p => Math.Abs(p.Articulacao(ui))));
            }
        }

        var r = new ResultadoVarredura
        {
            Manobra = m,
            Poses = poses,
            CaminhoDianteiro = Geo.Limpar(poses.Select(p => p.EixoDianteiro)),
            Avisos = avisos,
            Comprimento = s,
            DeltaMaximo = poses.Count == 0 ? 0 : poses.Max(p => Math.Abs(p.Delta)),
            ArticulacaoMaxima = artMax,
        };
        if (op.CalcularEnvoltoria && poses.Count > 1)
        {
            var (env, rodas) = Envoltorias(v, poses, op.PassoEnvoltoria);
            r.Envoltoria.AddRange(env);
            r.Rodas.AddRange(rodas);
            if (op.CalcularLargura) Largura(r);
        }
        return r;
    }

    /// <summary>Agrupa poses consecutivas que atendem a condição e chama o relato com (s inicial, s final, pose de pico).</summary>
    private static void Faixas(List<Pose> poses, Func<Pose, bool> cond, Action<double, double, Pose> relato, Func<Pose, double> pico)
    {
        int i = 0, n = 0;
        while (i < poses.Count && n < 6)
        {
            if (!cond(poses[i])) { i++; continue; }
            int j = i;
            var pk = poses[i];
            while (j + 1 < poses.Count && cond(poses[j + 1])) { j++; if (pico(poses[j]) > pico(pk)) pk = poses[j]; }
            relato(poses[i].S, poses[j].S, pk);
            n++;
            i = j + 1;
        }
    }

    /// <summary>Índices das poses usadas na envoltória (a cada passo, sempre incluindo a última).</summary>
    public static List<int> Amostras(List<Pose> poses, double passo)
    {
        var idx = new List<int> { 0 };
        double ult = poses[0].S;
        for (int i = 1; i < poses.Count; i++)
        {
            bool mudouSentido = poses[i].Re != poses[i - 1].Re;
            if (poses[i].S - ult >= passo - 1e-9 || mudouSentido || i == poses.Count - 1)
            {
                if (mudouSentido && idx[^1] != i - 1) idx.Add(i - 1);
                idx.Add(i);
                ult = poses[i].S;
            }
        }
        return idx;
    }

    /// <summary>
    /// Continua a partir das posições atuais percorrendo um trecho (sem montar envoltória) — usado na prévia interativa.
    /// Retorna as poses (a primeira é a atual).
    /// </summary>
    public static List<Pose> Percorrer(Veiculo v, PoseUnidade[] inicio, Trecho tr, double passo = 0.10)
    {
        var r = new List<Pose> { new Pose { U = inicio, Re = tr.Re } };
        if (tr.Pontos.Count < 2 || (tr.Re && v.Articulado)) return r;
        var U = inicio;
        double l = v.Motriz.EntreEixos, s = 0;
        var refAtual = tr.Re ? U[0].Ponto(-l) : U[0].Guia;
        var bruto = new List<V2>(tr.Pontos);
        if (bruto[0].Dist(refAtual) > 0.02) bruto.Insert(0, refAtual);
        var pts = Geo.Reamostrar(bruto, passo);
        for (int k = 1; k < pts.Count; k++)
        {
            double ds = pts[k].Dist(pts[k - 1]);
            if (ds < 1e-9) continue;
            double dir = (pts[k] - pts[k - 1]).Angulo;
            U = tr.Re ? Seguidor.RePeloEixoTraseiro(v, pts[k], dir) : Seguidor.Avancar(v, U, pts[k]);
            s += ds;
            double delta = tr.Re ? 0 : Geo.AngNorm(dir - U[0].Rumo);
            r.Add(new Pose { S = s, U = U, Re = tr.Re, Delta = delta });
        }
        return r;
    }

    /// <summary>Envoltória da carroceria e das rodas: união dos fechos convexos de posições consecutivas de cada peça.</summary>
    public static (List<List<V2>> Corpo, List<List<V2>> Rodas) Envoltorias(Veiculo v, List<Pose> poses, double passo, bool comRodas = true)
    {
        var idx = Amostras(poses, passo);
        var corpos = new List<IReadOnlyList<V2>>();
        var rodas = new List<IReadOnlyList<V2>>();
        List<V2[]>? cAnt = null, rAnt = null;
        foreach (int i in idx)
        {
            var p = poses[i];
            var c = new List<V2[]>();
            var rr = new List<V2[]>();
            for (int k = 0; k < v.Unidades.Count; k++)
            {
                c.AddRange(Corpo.Carroceria(v.Unidades[k], p.U[k]));
                if (comRodas) rr.AddRange(Corpo.Rodas(v, k, p.U[k], p.Delta));
            }
            if (cAnt is null) { corpos.AddRange(c); rodas.AddRange(rr); }
            else
            {
                for (int k = 0; k < c.Count && k < cAnt.Count; k++) corpos.Add(Geo.FechoConvexo(c[k].Concat(cAnt[k])));
                for (int k = 0; k < rr.Count && k < rAnt!.Count; k++) rodas.Add(Geo.FechoConvexo(rr[k].Concat(rAnt[k])));
            }
            cAnt = c; rAnt = rr;
        }
        var env = Poligonos.UniaoEmLotes(corpos).Select(q => Poligonos.SimplificarFechado(q, 0.003)).Where(q => q.Count >= 3).ToList();
        var rod = rodas.Count == 0 ? new List<List<V2>>() : Poligonos.UniaoEmLotes(rodas).Select(q => Poligonos.SimplificarFechado(q, 0.003)).Where(q => q.Count >= 3).ToList();
        return (env, rod);
    }

    /// <summary>Largura varrida máxima, medida na normal ao caminho do eixo dianteiro a cada metro.</summary>
    private static void Largura(ResultadoVarredura r)
    {
        var cam = r.CaminhoDianteiro;
        if (cam.Count < 2 || r.Envoltoria.Count == 0) return;
        var acum = Geo.Acumulado(cam);
        double total = acum[^1];
        double meio = r.Manobra.Veiculo.Comprimento + 5;
        double melhor = 0, sMelhor = 0;
        var normais = new List<IReadOnlyList<V2>>();
        var centros = new List<(V2 P, double S)>();
        for (double s = 0.5; s < total; s += 1.0)
        {
            var (p, t) = Geo.PontoNaDistancia(cam, acum, s);
            var nrm = t.Esq();
            normais.Add(new[] { p - nrm * meio, p + nrm * meio });
            centros.Add((p, s));
        }
        // todas as normais num só recorte; cada pedaço vale para a estação cujo eixo ele contém
        var pedacos = Poligonos.LinhasDentro(normais, r.Envoltoria);
        const double cel = 5.0;
        var grade = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < centros.Count; i++)
        {
            var k = ((int)Math.Floor(centros[i].P.X / cel), (int)Math.Floor(centros[i].P.Y / cel));
            if (!grade.TryGetValue(k, out var l)) grade[k] = l = new List<int>();
            l.Add(i);
        }
        foreach (var pc in pedacos)
        {
            if (pc.Count < 2) continue;
            var a = pc[0];
            var b = pc[^1];
            double w = a.Dist(b);
            if (w <= melhor) continue;
            int x0 = (int)Math.Floor(Math.Min(a.X, b.X) / cel), x1 = (int)Math.Floor(Math.Max(a.X, b.X) / cel);
            int y0 = (int)Math.Floor(Math.Min(a.Y, b.Y) / cel), y1 = (int)Math.Floor(Math.Max(a.Y, b.Y) / cel);
            for (int gx = x0; gx <= x1 && w > melhor; gx++)
                for (int gy = y0; gy <= y1 && w > melhor; gy++)
                {
                    if (!grade.TryGetValue((gx, gy), out var l)) continue;
                    foreach (int i in l)
                    {
                        if (Geo.DistPontoSegmento(centros[i].P, a, b) > 0.02) continue;
                        // o pedaço precisa estar sobre a normal desta estação (não sobre a de uma vizinha que cruza o eixo)
                        var n = normais[i];
                        if (Geo.DistPontoSegmento(a, n[0], n[1]) > 0.02 || Geo.DistPontoSegmento(b, n[0], n[1]) > 0.02) continue;
                        melhor = w; sMelhor = centros[i].S;
                        break;
                    }
                }
        }
        r.LarguraMaxima = melhor;
        r.SLarguraMaxima = sMelhor;
    }
}
