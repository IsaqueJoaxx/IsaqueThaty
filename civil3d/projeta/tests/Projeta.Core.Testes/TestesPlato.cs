using Projeta.Core.Terraplenagem;

namespace Projeta.Core.Testes;

/// <summary>Testes do platô (Plato.cs) com resultados analíticos.</summary>
internal static class TestesPlato
{
    public static void Rodar(Action<bool, string> Ok, string? saida = null)
    {
        static bool Rel(double a, double b, double tol) => Math.Abs(a - b) <= tol * Math.Abs(b);
        var quad = new Poligono2D(new[] { (0.0, 0.0), (20.0, 0.0), (20.0, 20.0), (0.0, 20.0) });
        Ok(Math.Abs(quad.Area - 400) < 1e-9 && Math.Abs(quad.Perimetro - 80) < 1e-9 && Math.Abs(quad.Cx - 10) < 1e-9, "polígono: área, perímetro, centróide");
        var horario = new Poligono2D(new[] { (0.0, 0.0), (0.0, 20.0), (20.0, 20.0), (20.0, 0.0), (0.0, 0.0) });
        Ok(Poligono2D.AreaDe(horario.V) > 0 && horario.V.Count == 4, "polígono horário vira anti-horário e perde o vértice repetido");
        var (qx, qy, d, s) = quad.MaisProximo(25, 10);
        Ok(Math.Abs(qx - 20) < 1e-9 && Math.Abs(qy - 10) < 1e-9 && Math.Abs(d - 5) < 1e-9 && Math.Abs(s - 30) < 1e-9, "ponto mais próximo");
        Ok(quad.Dentro(10, 10) && !quad.Dentro(25, 10), "dentro/fora");
        var pq = quad.Polo();
        Ok(Math.Abs(pq.X - 10) < 1e-9 && Math.Abs(pq.Y - 10) < 1e-9 && Math.Abs(pq.D - 10) < 1e-9, "polo do quadrado = centro");
        var eleP = new Poligono2D(new[] { (0.0, 0.0), (30.0, 0.0), (30.0, 15.0), (15.0, 15.0), (15.0, 30.0), (0.0, 30.0) }).Polo();
        // maior círculo dentro do L: na diagonal do quadrado do canto, raio d = 15·√2/(1 + √2) (toca x = 0, y = 0 e o canto reentrante)
        double rL = 15 * Math.Sqrt(2) / (1 + Math.Sqrt(2));
        Ok(Math.Abs(eleP.D - rL) < 0.05 && Math.Abs(eleP.X - eleP.Y) < 0.1, $"polo do L longe do canto reentrante ({eleP.X:F2}; {eleP.Y:F2}; d = {eleP.D:F3} x {rL:F3})");

        // 1. terreno plano a 100, aterro de 2 m, talude 1V:2H -> 800 + 2·(80·2 + 2π·16/6) = 1153,51 m³
        var pa = new ParametrosPlato { TaludeAterro = 2, TaludeCorte = 1.5, Caimento = 0.005, Fh = 1 };
        var c1 = new CampoPlato(quad, (x, y) => 100, pa, 0.25, 10);
        Ok(c1.Dx == 0 && c1.Dy == 0 && double.IsNaN(c1.AzimuteCaimento), "terreno plano: piso sem caimento");
        var v1 = c1.Volumes(102);
        double esperado1 = 800 + 2 * (80 * 2 + 2 * Math.PI * 16 / 6);
        Ok(Rel(v1.Aterro, esperado1, 0.01) && v1.Corte < 1e-6, $"aterro quadrado {v1.Aterro:F2} x {esperado1:F2}");
        Ok(Rel(v1.AreaPiso, 400, 0.02) && !v1.TocaBorda, $"área do piso {v1.AreaPiso:F1}");
        var e1 = c1.Encontro(102);
        double area1 = 400 + 80 * 4 + Math.PI * 16;
        Ok(e1.Count == 1 && Rel(Poligono2D.AreaDe(e1[0]), area1, 0.01), $"encontro do quadrado: {e1.Count} laço(s), área {(e1.Count > 0 ? Poligono2D.AreaDe(e1[0]) : 0):F1} x {area1:F1}");
        var (corte1, h1) = c1.Talude(102, 24, 10, 100);
        Ok(!corte1 && Math.Abs(h1 - 2) < 1e-9, "talude de aterro com 2 m");
        var tr1 = c1.Tracos(102, 2, 1);
        Ok(tr1.Count > 20 && tr1.Where((t, i) => i % 2 == 0).All(t => Math.Abs(Math.Sqrt(Math.Pow(t.X1 - t.X0, 2) + Math.Pow(t.Y1 - t.Y0, 2)) - 4) < 0.01), $"tracinhos de 4 m ({tr1.Count})");
        Ok(tr1.All(t => quad.MaisProximo(t.X0, t.Y0).D < 1e-6), "no aterro o tracinho sai da borda do piso");
        var tr2 = c1.Tracos(97, 2, 1); // corte de 3 m, talude 1,5 -> crista a 4,5 m; meio tracinho vai de 4,5 a 2,25 m
        Ok(tr2.Count > 20 && tr2.All(t => t.Corte && Math.Abs(quad.MaisProximo(t.X0, t.Y0).D - 4.5) < 0.01) &&
           tr2.Where((t, i) => i % 2 == 1).All(t => Math.Abs(quad.MaisProximo(t.X1, t.Y1).D - 2.25) < 0.01), "no corte o tracinho sai da crista");

        // 2. corte de 3 m, talude 1V:1,5H -> 1200 + 3·(80·4,5/2 + 2π·4,5²/6)
        var v2 = c1.Volumes(97);
        double esperado2 = 1200 + 3 * (80 * 4.5 / 2 + 2 * Math.PI * 4.5 * 4.5 / 6);
        Ok(Rel(v2.Corte, esperado2, 0.01) && v2.Aterro < 1e-6, $"corte quadrado {v2.Corte:F2} x {esperado2:F2}");

        // 3. polígono em L (canto reentrante), aterro de 1 m, talude 1V:2H: V = A + P·w/2 + (5π/2 − 2)·w²/6
        var ele = new Poligono2D(new[] { (0.0, 0.0), (30.0, 0.0), (30.0, 15.0), (15.0, 15.0), (15.0, 30.0), (0.0, 30.0) });
        var c3 = new CampoPlato(ele, (x, y) => 100, pa, 0.25, 8);
        var v3 = c3.Volumes(101);
        double esperado3 = 675 + 120 * 1 + (5 * Math.PI / 2 - 2) * 4 / 6;
        Ok(Rel(v3.Aterro, esperado3, 0.01), $"aterro em L {v3.Aterro:F2} x {esperado3:F2}");
        var e3 = c3.Encontro(101);
        double area3 = 675 + 120 * 2 + (5 * Math.PI / 2 - 2) * 4 / 2;
        Ok(e3.Count == 1 && Rel(Poligono2D.AreaDe(e3[0]), area3, 0.01), $"encontro do L: área {(e3.Count > 0 ? Poligono2D.AreaDe(e3[0]) : 0):F1} x {area3:F1}");

        // 4. encosta t = 100 + 0,1·y, quadrado centrado; taludes iguais e Fh = 1 -> compensação na cota 100 (simetria)
        var centrado = new Poligono2D(new[] { (-10.0, -10.0), (10.0, -10.0), (10.0, 10.0), (-10.0, 10.0) });
        var ps = new ParametrosPlato { TaludeAterro = 2, TaludeCorte = 2, Caimento = 0.005, Fh = 1 };
        var c4 = new CampoPlato(centrado, (x, y) => 100 + 0.1 * y, ps, 0.25, 12);
        Ok(Math.Abs(c4.Dx) < 1e-6 && Math.Abs(c4.Dy - 1) < 1e-6 && Math.Abs(c4.DeclividadeMedia - 0.1) < 1e-6, $"direção do caimento ({c4.Dx:F3}; {c4.Dy:F3})");
        Ok(Math.Abs(c4.AzimuteCaimento - 180) < 1e-6, $"piso cai para o sul ({c4.AzimuteCaimento:F1}°)");
        Ok(Math.Abs(c4.Piso(100, 0, 10) - c4.Piso(100, 0, -10) - 0.1) < 1e-9, "caimento de 0,5 % em 20 m = 0,10 m");
        double z4 = c4.Compensar();
        Ok(Math.Abs(z4 - 100) < 0.01, $"cota de compensação simétrica {z4:F3}");
        var c5 = new CampoPlato(centrado, (x, y) => 100 + 0.1 * y, new ParametrosPlato { TaludeAterro = 2, TaludeCorte = 2, Caimento = 0.005, Fh = 1.25 }, 0.25, 12);
        double z5 = c5.Compensar();
        var v5 = c5.Volumes(z5);
        Ok(z5 < z4 - 0.01 && Math.Abs(v5.Saldo(1.25)) < 1, $"com Fh 1,25 a cota desce ({z5:F3}) e o saldo zera ({v5.Saldo(1.25):F2})");
        var p5 = c5.PontosTalude(z5).ToList();
        Ok(p5.Count > 100 && p5.All(p => !double.IsNaN(p.Z)), $"pontos de talude ({p5.Count})");

        // 5. fora da superfície
        try { new CampoPlato(quad, (x, y) => double.NaN, pa, 1, 5); Ok(false, "aceitou polígono fora da superfície"); }
        catch (InvalidOperationException) { Ok(true, ""); }

        // 6. relatório
        var rel = new RelatorioPlatos { Terreno = "TN", Parametros = pa };
        rel.Platos.Add(new ResultadoPlato { Nome = "Platô 1", Compensada = true, Cota = 850.21, CorteMalha = 10515, AterroMalha = 8451, Corte = 10514, Aterro = 8451.2, Caimento = 0.005, Azimute = 165 });
        Ok(Math.Abs(rel.Saldo(rel.Platos[0]) - (10514 - 8451.2)) < 1e-6 && rel.Situacao(rel.Platos[0]) == "Sobra (bota-fora)", "saldo com Fh 1 = sobra");
        rel.Parametros = new ParametrosPlato { Fh = 1.25 };
        Ok(rel.Situacao(rel.Platos[0]) == "Equilíbrio", "saldo com Fh 1,25 = equilíbrio");
        var pasta = rel.Planilha();
        if (saida is not null) pasta.Salvar(Path.Combine(saida, "platos_teste.xlsx"));
        Ok(pasta.Folhas[0].Nome == "Platôs" && (string?)pasta.Folhas[0].Valor(5, 1) == "Platô 1 (compensada)", "aba Platôs");
        try { new ParametrosPlato { Caimento = -0.01 }.Validar(); Ok(false, "aceitou caimento negativo"); } catch (ArgumentException) { Ok(true, ""); }
    }
}
