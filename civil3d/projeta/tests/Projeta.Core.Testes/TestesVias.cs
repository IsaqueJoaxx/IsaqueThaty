using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes das vias de loteamento em lote (PRJVIAS): curvas nos vértices, encontros entre ruas e greide proposto.</summary>
internal static class TestesVias
{
    public static void Rodar(Action<bool, string> Ok)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;

        // vértice a 90° (vira à esquerda), raio 30: tangentes a 30 m do vértice e bulge = tg(22,5°)
        var av = new List<string>();
        var v = GeometriaVias.Arredondar(new[] { new VerticeVia(0, 0), new VerticeVia(100, 0), new VerticeVia(100, 100) }, 30, av, "Rua 01");
        Ok(v.Count == 4 && Q(v[1].X, 70) && Q(v[1].Y, 0) && Q(v[1].Bulge, Math.Tan(Math.PI / 8)) && Q(v[2].X, 100) && Q(v[2].Y, 30) && av.Count == 0,
           $"curva de raio 30 no vértice ({string.Join("; ", v)})");
        var v2 = GeometriaVias.Arredondar(new[] { new VerticeVia(0, 0), new VerticeVia(100, 0), new VerticeVia(100, -100) }, 30, av, "Rua 02");
        Ok(Q(v2[1].Bulge, -Math.Tan(Math.PI / 8)), "vira à direita: bulge negativo");
        av.Clear();
        var v3 = GeometriaVias.Arredondar(new[] { new VerticeVia(0, 0), new VerticeVia(20, 0), new VerticeVia(20, 100) }, 30, av, "Rua 03");
        Ok(Q(v3[1].X, 1) && Q(v3[2].Y, 19) && av.Count == 1, $"raio que não cabe vira o maior possível ({av.FirstOrDefault()})");

        // encontros: cruzamento no meio, T a 1 m e ponta com ponta
        var ruas = new List<IReadOnlyList<(double X, double Y, double S)>>
        {
            new[] { (0.0, 0.0, 0.0), (200.0, 0.0, 200.0) },
            new[] { (100.0, -100.0, 0.0), (100.0, 100.0, 200.0) },
            new[] { (150.0, 50.0, 0.0), (150.0, 1.0, 49.0) },
            new[] { (200.0, 0.0, 0.0), (300.0, 0.0, 100.0) },
        };
        var cz = GeometriaVias.Cruzamentos(ruas, 2);
        Ok(cz.Count == 3, $"três encontros ({string.Join("; ", cz)})");
        Ok(cz.Any(c => c.A == 0 && c.B == 1 && c.Tipo == "cruzamento" && Q(c.EstacaA, 100) && Q(c.EstacaB, 100) && Q(c.X, 100) && Q(c.Y, 0)), "cruzamento no meio das duas ruas");
        Ok(cz.Any(c => c.A == 0 && c.B == 2 && c.Tipo == "T" && Q(c.EstacaA, 150) && Q(c.EstacaB, 49) && Q(c.X, 150) && Q(c.Y, 0)), "rua que para a 1 m da outra vira T");
        Ok(cz.Any(c => c.A == 0 && c.B == 3 && c.Tipo == "ponta com ponta" && Q(c.EstacaA, 200) && Q(c.EstacaB, 0)), "rua que continua na ponta da outra");

        // greide proposto: terreno reto = só as pontas; lombada = PIV no alto; cota fixa no encontro
        var reto = Enumerable.Range(0, 41).Select(i => (S: i * 5.0, Z: 100 + 0.05 * i * 5.0)).ToList();
        var g1 = GeometriaVias.GreideProposto(reto, Array.Empty<(double, double)>(), 0.3, 20);
        Ok(g1.Count == 2 && Q(g1[0].Cota, 100) && Q(g1[1].Cota, 110), $"terreno reto: greide só com as pontas ({g1.Count} PIVs)");
        var lomba = Enumerable.Range(0, 41).Select(i => (S: i * 5.0, Z: 100 + 3 * Math.Sin(Math.PI * i * 5.0 / 200))).ToList();
        var g2 = GeometriaVias.GreideProposto(lomba, Array.Empty<(double, double)>(), 0.3, 20);
        Ok(g2.Count >= 3 && g2.Any(p => Q(p.Estaca, 100) && Q(p.Cota, 103)), $"lombada: PIV no alto ({string.Join("; ", g2.Select(p => $"{p.Estaca:0}/{p.Cota:0.00}"))})");
        bool dentro = lomba.All(t => Math.Abs(t.Z - Cota(g2, t.S)) <= 0.3 + 0.35);
        Ok(dentro, "greide perto do terreno em todo o trecho");
        var g3 = GeometriaVias.GreideProposto(reto, new[] { (100.0, 104.0) }, 0.3, 20);
        Ok(g3.Any(p => p.Fixo && Q(p.Estaca, 100) && Q(p.Cota, 104)), "cota fixa no encontro com outra rua");
        var g4 = GeometriaVias.GreideProposto(reto, new[] { (0.4, 99.5) }, 0.3, 20);
        Ok(Q(g4[0].Estaca, 0) && Q(g4[0].Cota, 99.5) && g4[0].Fixo, "cota fixa na ponta substitui a do terreno");

        // curvas verticais: L = K·A, encurtada para caber
        var cv = GeometriaVias.CurvasVerticais(new[] { new PivVia(0, 100), new PivVia(100, 110), new PivVia(200, 100) }, 6, 10);
        Ok(Q(cv[1].Curva, 120) && cv[0].Curva == 0 && cv[2].Curva == 0, $"K 6 × A 20 % = 120 m ({cv[1].Curva})");
        var cv2 = GeometriaVias.CurvasVerticais(new[] { new PivVia(0, 100), new PivVia(100, 110), new PivVia(200, 100) }, 10, 10);
        Ok(Q(cv2[1].Curva, 190), $"curva encurtada para caber ({cv2[1].Curva})");
        var cv3 = GeometriaVias.CurvasVerticais(new[] { new PivVia(0, 100), new PivVia(100, 110, 0, true), new PivVia(150, 106), new PivVia(200, 100) }, 6, 10);
        Ok(cv3[1].Curva == 0 && cv3[2].Curva > 0 && cv3[2].Curva / 2 <= 0.95 * 50 + 1e-9, $"sem curva no PIV fixo do encontro ({cv3[1].Curva}; {cv3[2].Curva})");
        var rp = GeometriaVias.Rampas(new[] { new PivVia(0, 100), new PivVia(100, 104), new PivVia(200, 110) });
        Ok(rp.Count == 2 && Q(rp[0].Rampa, 0.04) && Q(rp[1].Rampa, 0.06), "rampas entre PIVs");

        // relatório
        var rel = new RelatorioVias { Desenho = "teste.dwg", Superficie = "Terreno" };
        rel.Ruas.Add(new RuaVia("Rua 01", "polilinha", 200, 1, 30, 100, 110, 0.04, 0.06, 3, "proposto", "Vias do loteamento"));
        rel.Encontros.Add(new EncontroVia("Rua 01", "5+0,00", "Rua 02", "5+0,00", 100, 0, 104, "cruzamento"));
        rel.Pivs.Add(new PivRelatorio("Rua 01", "5+0,00", 104, 0, 0.04, "cota do encontro com a Rua 02"));
        rel.Verificacoes.Add(new Verificacao("Rampa máxima", "Rua 01", "6,00 %", "≤ 15 %", "teste", VerificacaoRotatoria.Aprovado));
        string arq = Path.Combine(Path.GetTempPath(), "projeta-testes", "vias.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(arq)!);
        rel.Salvar(arq);
        Ok(new FileInfo(arq).Length > 3000, "planilha das vias gravada");
    }

    private static double Cota(IReadOnlyList<PivVia> p, double s)
    {
        for (int i = 0; i + 1 < p.Count; i++)
            if (s >= p[i].Estaca - 1e-9 && s <= p[i + 1].Estaca + 1e-9)
                return p[i].Cota + (p[i + 1].Cota - p[i].Cota) * (s - p[i].Estaca) / (p[i + 1].Estaca - p[i].Estaca);
        return p[^1].Cota;
    }
}
