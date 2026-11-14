using System.Diagnostics;
using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes da trajetória mais rápida, das ilhas divisórias e dos bordos da rotatória.</summary>
internal static class TestesTrajetoria
{
    /// <summary>Rotatória simétrica de 4 braços retos (DCI 40, pista 6, ilhas de 3,0 → 0,6 m em 30 m, entradas e saídas de 5 m).</summary>
    public static ModeloRotatoria Modelo(int bracos = 4, double rEntrada = 15, double rSaida = 20, double faixa = 5, double ilha = 3, double[]? angulos = null)
    {
        var m = new ModeloRotatoria { Ox = 0, Oy = 0, RExt = 20, RIlha = 14, RecuoIlha = 1 };
        double rEixo = 17;
        for (int k = 0; k < bracos; k++)
        {
            double a = angulos?[k] ?? 2 * Math.PI * k / bracos;
            double ux = Math.Cos(a), uy = Math.Sin(a), vx = -uy, vy = ux;
            var eixo = new PolilinhaEixo(new[] { (rEixo * ux, rEixo * uy), (220 * ux, 220 * uy) });
            double be = ilha / 2 + faixa, bs = ilha / 2 + faixa;
            double de = be + rEntrada, te = Math.Sqrt((20 + rEntrada) * (20 + rEntrada) - de * de);
            double ds = bs + rSaida, ts = Math.Sqrt((20 + rSaida) * (20 + rSaida) - ds * ds);
            var b = new BracoRotatoria
            {
                Nome = $"B{k}", Rumo = GeometriaRotatoria.Rumo(a), Angulo = GeometriaRotatoria.Normalizar(a), Eixo = eixo,
                SRetencao = 20 - rEixo, BordoEntrada = be, BordoSaida = bs, BordoNormal = 3.5,
                MeioFioEntrada = (te * ux + de * vx, te * uy + de * vy, rEntrada), STangEntrada = te - rEixo,
                MeioFioSaida = (ts * ux - ds * vx, ts * uy - ds * vy, rSaida), STangSaida = ts - rEixo,
                SIlhaIni = 21 - rEixo, SIlhaFim = 21 - rEixo + (ilha > 0 ? 30 : 0), IlhaLarguraIni = ilha, IlhaLarguraFim = ilha > 0 ? 0.6 : 0,
            };
            b.SAlargIni = b.SIlhaFim + 5;
            b.SAlargFim = b.SAlargIni + (be - 3.5) * 10;
            m.Bracos.Add(b);
        }
        return m;
    }

    public static void Rodar(Action<bool, string> Ok)
    {
        static bool Q(double a, double b, double tol) => Math.Abs(a - b) <= tol;

        // velocidades (NCHRP 672, ajuste métrico) e consistência com a fórmula em pés/mph
        Ok(Q(TrajetoriaRapida.Velocidade(30, true), 8.7602 * Math.Pow(30, 0.3861), 1e-9) && Q(TrajetoriaRapida.Velocidade(30, true), 1.609344 * 3.4415 * Math.Pow(30 / 0.3048, 0.3861), 0.05)
           && Q(TrajetoriaRapida.Velocidade(30, false), 1.609344 * 3.4614 * Math.Pow(30 / 0.3048, 0.3673), 0.05), "velocidade × raio (e = ±0,02) bate com a forma em pés/mph");

        // polilinha: estaca/afastamento ida e volta
        var pl = new PolilinhaEixo(new[] { (0.0, 0.0), (10.0, 0.0), (10.0, 10.0) });
        var pt = pl.Ponto(15, 2);
        var pr = pl.Projetar(pt.X, pt.Y);
        Ok(Q(pt.X, 8, 1e-9) && Q(pt.Y, 5, 1e-9) && Q(pr.S, 15, 1e-9) && Q(pr.V, 2, 1e-9) && Q(pl.Projetar(-5, 1).S, -5, 1e-9), "polilinha: ponto por estaca/afastamento e projeção (esquerda +)");

        // passo implícito: reta continua reta, quina vira curva
        var reta = Enumerable.Range(0, 30).Select(i => ((double)i, 0.0)).ToList();
        TrajetoriaRapida.PassoImplicito(reta, 10);
        var quina = Enumerable.Range(0, 41).Select(i => i <= 20 ? ((double)i, 0.0) : (20.0, (double)(i - 20))).ToList();
        double antes = quina.Select((p, i) => Math.Abs(p.Item1 - 15) + Math.Abs(p.Item2 - 5)).Min();
        for (int k = 0; k < 50; k++) TrajetoriaRapida.PassoImplicito(quina, 10);
        double depois = quina.Select((p, i) => Math.Abs(p.Item1 - 15) + Math.Abs(p.Item2 - 5)).Min();
        Ok(reta.All(p => Math.Abs(p.Item2) < 1e-9) && depois < antes && quina[0] == (0, 0) && quina[^1] == (20, 20), "flexão: reta fica reta, quina é arredondada, pontas fixas");

        // ilha divisória e bordos
        var m = Modelo();
        var ilha = m.Ilha(m.Bracos[0]);
        double area = 0;
        for (int i = 0; i < ilha.Count; i++) { var a = ilha[i]; var b = ilha[(i + 1) % ilha.Count]; area += a.X * b.Y - b.X * a.Y; }
        area = Math.Abs(area) / 2;
        double rMin = ilha.Min(p => Math.Sqrt(p.X * p.X + p.Y * p.Y));
        double xMax = ilha.Max(p => p.X), yMax = ilha.Max(p => Math.Abs(p.Y));
        Ok(area > 30 && area < 60 && rMin > 20.99 && Q(yMax, 1.5, 0.01) && Q(xMax, 17 + 34, 0.01), $"ilha divisória: área {area:F1} m², começa no recuo do anel (r ≥ {rMin:F2}), 3,0 m de largura e nariz na estaca certa");
        var bordo = m.BordoRamo(m.Bracos[0], true, 80);
        Ok(Q(bordo[0].Y, 6.5, 1e-9) && Q(bordo[^1].Y, 3.5, 1e-9) && Q(bordo[0].X, 17 + m.Bracos[0].STangEntrada, 1e-9), "bordo do ramo: da tangência da conexão, alargado junto da rotatória e normal depois");
        var ret = m.Retencao(m.Bracos[0]);
        Ok(ret.All(p => Q(Math.Sqrt(p.X * p.X + p.Y * p.Y), 20, 1e-9)) && Q(ret[0].Y, 1.5, 0.01) && ret[^1].Y > 6, "linha de retenção no bordo externo, da ilha ao meio-fio da entrada");

        // trajetória mais rápida
        var sw = Stopwatch.StartNew();
        var an = TrajetoriaRapida.Analisar(m, 0);
        double ms = sw.Elapsed.TotalMilliseconds;
        var d = an.Direto!;
        Ok(d.Violacao < 0.01 && an.Esquerda!.Violacao < 0.01 && an.Direita!.Violacao < 0.01,
           $"trajetórias respeitam os afastamentos (violação {d.Violacao:F4} / {an.Esquerda.Violacao:F4} / {an.Direita.Violacao:F4} m; {d.Iteracoes}/{an.Esquerda.Iteracoes}/{an.Direita.Iteracoes} iterações; {ms:F0} ms)");
        var topo = d.Pontos.OrderBy(p => Math.Abs(p.X)).First();
        Ok(topo.Y > 15.4 && d.Pontos.All(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) >= 15.49), $"direto do leste para o oeste passa por cima (norte) da ilha central, a ≥ 1,5 m dela (y = {topo.Y:F2})");
        Ok(an.R1 is not null && an.R2 is not null && an.R3 is not null && an.R4 is not null && an.R5 is not null, "R1 a R5 encontrados");
        string resumo = $"R1 {an.R1?.Raio:F1} ({an.R1?.Velocidade:F0} km/h), R2 {an.R2?.Raio:F1} ({an.R2?.Velocidade:F0}), R3 {an.R3?.Raio:F1} ({an.R3?.Velocidade:F0}), R4 {an.R4?.Raio:F1} ({an.R4?.Velocidade:F0}), R5 {an.R5?.Raio:F1} ({an.R5?.Velocidade:F0})";
        Ok(an.R2!.Raio > 15 && an.R2.Raio < 80 && an.R1!.Raio > 10 && an.R1.Raio < 200 && an.R4!.Raio > 15 && an.R4.Raio < an.R2.Raio + 5 && an.R5!.Raio > 10, "raios plausíveis: " + resumo);
        var an1 = TrajetoriaRapida.Analisar(m, 1);
        Ok(Q(an1.R1!.Raio, an.R1.Raio, an.R1.Raio * 0.03) && Q(an1.R2!.Raio, an.R2.Raio, an.R2.Raio * 0.03) && Q(an1.R4!.Raio, an.R4.Raio, an.R4.Raio * 0.03),
           $"braços simétricos dão os mesmos raios (R1 {an1.R1.Raio:F1} × {an.R1.Raio:F1})");
        // raio de entrada maior deixa a entrada mais rápida (ou igual)
        var mg = Modelo(rEntrada: 18);
        var ang = TrajetoriaRapida.Analisar(mg, 0);
        Ok(ang.R5 is not null && ang.R5.Raio >= an.R5.Raio - 1, $"raio de conexão de entrada maior → R5 maior ({ang.R5?.Raio:F1} × {an.R5.Raio:F1})");
        // sem ilha divisória, a trajetória respeita o eixo da via
        var ms0 = Modelo(ilha: 0);
        var an0 = TrajetoriaRapida.Analisar(ms0, 0);
        Ok(an0.Direto!.Violacao < 0.01 && an0.R1 is not null, $"sem ilha divisória: R1 {an0.R1?.Raio:F1} m, a trajetória fica a 1,5 m do eixo da via");
        // verificações e planilha
        var crit = new CriteriosRotatoria();
        var analises = Enumerable.Range(0, m.Bracos.Count).Select(i => TrajetoriaRapida.Analisar(m, i)).ToList();
        var ver = VerificacaoRotatoria.Avaliar(m, analises, crit);
        var v1 = ver.First(v => v.Item.StartsWith("Velocidade de entrada"));
        var fol = ver.First(v => v.Item.StartsWith("Folga"));
        Ok(ver.Count >= 4 * 8 && v1.Resultado == VerificacaoRotatoria.Aprovado && fol.Resultado == VerificacaoRotatoria.Aprovado
           && ver.All(v => v.Fonte.Length > 0), $"verificações: {ver.Count} itens, V1 {v1.Valor} aprovado, folga {fol.Valor}");
        Ok(Math.Abs(VerificacaoRotatoria.DesvioDoCentro(m, m.Bracos[0])) < 1e-9, "eixo radial: desvio zero em relação ao centro");
        var torto = new BracoRotatoria { Eixo = new PolilinhaEixo(new[] { (17.0, 3.0), (200.0, 3.0) }), SRetencao = 3 };
        var torto2 = new BracoRotatoria { Eixo = new PolilinhaEixo(new[] { (17.0, -3.0), (200.0, -3.0) }), SRetencao = 3 };
        Ok(Math.Abs(VerificacaoRotatoria.DesvioDoCentro(m, torto) - 3) < 1e-9 && Math.Abs(VerificacaoRotatoria.DesvioDoCentro(m, torto2) + 3) < 1e-9, "braço a leste: eixo 3 m ao norte passa à direita do centro (quem chega vai para oeste), 3 m ao sul passa à esquerda");
        var mf = Modelo(rEntrada: 40, rSaida: 40);
        var fs = VerificacaoRotatoria.Folga(mf, 0);
        Ok(fs is < 0, $"raios grandes demais: folga negativa ({fs:F2} m)");
        var rel = new RelatorioRotatoria { Titulo = "Rotatória teste", Modelo = m, GreideAnel = "plano" };
        rel.Analises.AddRange(analises); rel.Verificacoes.AddRange(ver);
        rel.Parametros.Add(new ParametroRotatoria("DCI", "40,00", "m", "[PREMISSA]"));
        rel.Pendencias.Add("Giro do veículo de projeto.");
        var pasta = rel.Planilha();
        var bytes = pasta.Bytes();
        Ok(pasta.Folhas.Count == 6 && bytes.Length > 3000 && pasta.Folhas.First(f => f.Nome == "Trajetórias").Valor(2, 2) is double, $"planilha com {pasta.Folhas.Count} abas ({bytes.Length} bytes)");

        // 3 braços (T): direto existe
        var mt = Modelo(bracos: 3, angulos: new[] { 0.0, Math.PI / 2, Math.PI });
        var at = TrajetoriaRapida.Analisar(mt, 0);
        Ok(at.Direto is not null && at.Direto.Saida == 2 && at.R2 is not null && at.Direto.Violacao < 0.01, $"rotatória em T: direto do leste sai no oeste (R2 {at.R2?.Raio:F1})");
    }
}
