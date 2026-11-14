// Testes da geometria do Apagar PI (sem Civil 3D).
using Projeta.ApagarPI;

int falhas = 0;
void Ok(bool cond, string msg)
{
    Console.WriteLine((cond ? "ok    " : "FALHOU ") + msg);
    if (!cond) falhas++;
}
bool Perto(V2 a, V2 b, double tol = 1e-6) => V2.Dist(a, b) < tol;

// Monta os trechos de um eixo a partir de PIs e curvas (como o Civil 3D desenharia).
List<Trecho> Montar(V2[] pis, Curva?[] curvas)
{
    var t = new List<Trecho>();
    var cur = pis[0];
    for (int k = 1; k < pis.Length - 1; k++)
    {
        var uin = (pis[k] - pis[k - 1]).Unit;
        var uout = (pis[k + 1] - pis[k]).Unit;
        double d = Math.Atan2(V2.Cruz(uin, uout), V2.Esc(uin, uout));
        if (curvas[k] is not { } c) { t.Add(new(TipoTrecho.Reta, cur, pis[k], V2.Dist(cur, pis[k]))); cur = pis[k]; continue; }
        double T = c.Tangente(d);
        var pc = pis[k] - uin * T;
        var pt = pis[k] + uout * T;
        t.Add(new(TipoTrecho.Reta, cur, pc, V2.Dist(cur, pc)));
        if (c.Tipo == TipoCurva.Arco)
            t.Add(new(TipoTrecho.Arco, pc, pt, c.Raio * Math.Abs(d), c.Raio));
        else
        {   // espirais: só os pontos de contato com as tangentes importam para achar o PI
            var m1 = pc + (pt - pc) * 0.3;
            var m2 = pc + (pt - pc) * 0.7;
            t.Add(new(TipoTrecho.Espiral, pc, m1, c.L1, 0, c.TipoEspiral));
            t.Add(new(TipoTrecho.Arco, m1, m2, 1, c.Raio));
            t.Add(new(TipoTrecho.Espiral, m2, pt, c.L2, 0, c.TipoEspiral));
        }
        cur = pt;
    }
    t.Add(new(TipoTrecho.Reta, cur, pis[^1], V2.Dist(cur, pis[^1])));
    return t;
}

bool Cabe(Plano p)
{
    for (int s = 0; s < p.PIs.Count - 1; s++)
        if (p.Tangente(s) + p.Tangente(s + 1) > V2.Dist(p.PIs[s], p.PIs[s + 1]) + 1e-6) return false;
    return true;
}

// 1) eixo só com retas (polilinha): PIs = vértices
{
    var pis = new V2[] { new(0, 0), new(100, 0), new(150, 50), new(250, 50), new(300, 0) };
    var pl = Geometria.Extrair(Montar(pis, new Curva?[5]));
    Ok(pl.PIs.Count == 5 && pis.Zip(pl.PIs).All(z => Perto(z.First, z.Second)), "só retas: PIs = vértices");
    var log = new List<string>();
    var n = Geometria.Apagar(pl, 2, log);
    Ok(n.PIs.Count == 4 && Perto(n.PIs[1], new(100, 0)) && Perto(n.PIs[2], new(250, 50)), "só retas: apagou o PI 2");
}

// 2) tangente-curva-tangente: recupera PIs e raios; apaga e as curvas vizinhas cabem
{
    var pis = new V2[] { new(0, 0), new(200, 0), new(300, 80), new(450, 60), new(600, 150) };
    var cs = new Curva?[] { null, new(TipoCurva.Arco, 80), new(TipoCurva.Arco, 60), new(TipoCurva.Arco, 90), null };
    var pl = Geometria.Extrair(Montar(pis, cs));
    Ok(pis.Zip(pl.PIs).All(z => Perto(z.First, z.Second, 1e-6)), "arcos: PIs recuperados");
    Ok(pl.Curvas.Zip(cs).All(z => z.First?.Raio == z.Second?.Raio || Math.Abs((z.First?.Raio ?? 0) - (z.Second?.Raio ?? 0)) < 1e-9), "arcos: raios recuperados");
    var log = new List<string>();
    var n = Geometria.Apagar(pl, 2, log);
    Ok(n.PIs.Count == 4 && Perto(n.PIs[1], new(200, 0)) && Perto(n.PIs[2], new(450, 60)), "arcos: PI 2 apagado");
    Ok(n.Curvas[1]!.Raio == 80 && n.Curvas[2]!.Raio == 90 && Cabe(n), "arcos: vizinhos mantêm R 80 e 90 e cabem");
}

// 3) curva vizinha que não cabe é reduzida; curva que não é vizinha não muda
{
    var pl = new Plano();
    var pis = new V2[] { new(0, 0), new(100, 0), new(150, 20), new(200, 80), new(300, 80), new(400, 0) };
    var cs = new Curva?[] { null, new(TipoCurva.Arco, 250), new(TipoCurva.Arco, 250), new(TipoCurva.Arco, 250), new(TipoCurva.Arco, 50), null };
    pl.PIs.AddRange(pis); pl.Curvas.AddRange(cs);
    var log = new List<string>();
    var n = Geometria.Apagar(pl, 2, log);
    Ok(Cabe(n), "redução: todas as curvas cabem depois de apagar");
    Ok(n.Curvas[1]!.Raio < 250 && n.Curvas[2]!.Raio < 250, $"redução: vizinhos reduzidos (R = {n.Curvas[1]!.Raio:0.0} e {n.Curvas[2]!.Raio:0.0})");
    Ok(n.Curvas[3]!.Raio == 50, "redução: PI que não é vizinho manteve R 50");
    Ok(log.Count(l => l.Contains("não cabia")) == 2, "redução: as duas mudanças foram relatadas");
}

// 4) apagar o PI do meio deixa os vizinhos alinhados: eles saem também
{
    var pis = new V2[] { new(0, 0), new(200, 0), new(260, 40), new(320, 0), new(520, 0) };
    var cs = new Curva?[] { null, new(TipoCurva.Arco, 60), new(TipoCurva.Arco, 60), new(TipoCurva.Arco, 60), null };
    var pl = Geometria.Extrair(Montar(pis, cs));
    var log = new List<string>();
    var n = Geometria.Apagar(pl, 2, log);
    Ok(n.PIs.Count == 2 && Perto(n.PIs[0], new(0, 0)) && Perto(n.PIs[1], new(520, 0)), "alinhados: sobrou uma reta só");
    Ok(log.Count(l => l.Contains("alinhado")) == 2, "alinhados: os dois PIs extras foram relatados");
}

// 5) espiral-curva-espiral é reconhecida
{
    var pis = new V2[] { new(0, 0), new(300, 0), new(500, 150), new(800, 150) };
    var cs = new Curva?[] { null, new(TipoCurva.EspiralCurvaEspiral, 150, 40, 40, 0), new(TipoCurva.Arco, 120), null };
    var pl = Geometria.Extrair(Montar(pis, cs));
    var c = pl.Curvas[1];
    Ok(c is { Tipo: TipoCurva.EspiralCurvaEspiral, Raio: 150, L1: 40, L2: 40 } && Perto(pl.PIs[1], pis[1], 1e-6), "espiral: SCS reconhecida no PI certo");
}

// 6) vértices sobrando numa reta (eixo vindo de polilinha) somem
{
    var t = new List<Trecho>
    {
        new(TipoTrecho.Reta, new(0, 0), new(50, 0), 50),
        new(TipoTrecho.Reta, new(50, 0), new(100, 0), 50),
        new(TipoTrecho.Reta, new(100, 0), new(150, 50), 70.71),
    };
    var pl = Geometria.Extrair(t);
    Ok(pl.PIs.Count == 3 && Perto(pl.PIs[1], new(100, 0)), "colineares: vértice no meio da reta não vira PI");
}

// 7) eixo que começa em curva é recusado com mensagem
{
    var t = new List<Trecho> { new(TipoTrecho.Arco, new(0, 0), new(10, 1), 10, 50), new(TipoTrecho.Reta, new(10, 1), new(100, 10), 90) };
    try { Geometria.Extrair(t); Ok(false, "começa em curva: devia recusar"); }
    catch (InvalidOperationException ex) { Ok(ex.Message.Contains("começa"), "começa em curva: recusado com mensagem"); }
}

// 8) PI mais próximo do clique
{
    var pl = new Plano();
    pl.PIs.AddRange(new V2[] { new(0, 0), new(100, 0), new(200, 50), new(300, 0) });
    pl.Curvas.AddRange(new Curva?[4]);
    Ok(pl.MaisProximo(new(190, 45)) == 2 && pl.MaisProximo(new(-5, 0)) == 1, "clique: escolhe o PI interno mais próximo");
}

Console.WriteLine(falhas == 0 ? "\nTodos os testes passaram." : $"\n{falhas} teste(s) falharam.");
return falhas == 0 ? 0 : 1;
