using Projeta.Core.Superficies;

namespace Projeta.Core.Testes;

/// <summary>Testes do Projeto × obra (Conformidade.cs).</summary>
internal static class TestesConformidade
{
    public static void Rodar(Action<bool, string> Ok, string? saida = null)
    {
        static bool Perto(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;

        // faixas padrão
        var t = new ToleranciaObra();
        t.Validar();
        var fx = t.Faixas();
        Ok(fx.Count == 7, $"7 faixas ({fx.Count})");
        Ok(fx[0].Sinal == 1 && Perto(fx[0].De, 0.20) && double.IsPositiveInfinity(fx[0].Ate), "faixa 0 = acima > 0,20");
        Ok(fx[3].Sinal == 0 && Perto(fx[3].De, 0.05) && Perto(fx[3].Ate, 0.05), "faixa 3 = dentro");
        Ok(fx[6].Sinal == -1 && Perto(fx[6].De, 0.20), "faixa 6 = abaixo > 0,20");
        Ok(fx[2].Rotulo == "Acima do projeto (cortar): 0,05 a 0,10 m", fx[2].Rotulo);
        Ok(fx[3].Rotulo == "Dentro da tolerância (-0,05 a +0,05 m)", fx[3].Rotulo);
        Ok(fx[6].RotuloCurto == "Abaixo > 0,20 m", fx[6].RotuloCurto);
        Ok(fx.Select(f => f.Cor).Distinct().Count() == 7, "cores distintas");
        Ok(ToleranciaObra.Indice(fx, 0.05) == 3 && ToleranciaObra.Indice(fx, -0.05) == 3 && ToleranciaObra.Indice(fx, 0) == 3, "limites inclusive");
        Ok(ToleranciaObra.Indice(fx, 0.0501) == 2, "0,0501 → acima 0,05-0,10");
        Ok(ToleranciaObra.Indice(fx, 0.15) == 1, "0,15 → acima 0,10-0,20");
        Ok(ToleranciaObra.Indice(fx, 0.5) == 0, "0,5 → acima > 0,20");
        Ok(ToleranciaObra.Indice(fx, -0.08) == 4, "-0,08 → abaixo 0,05-0,10");
        Ok(ToleranciaObra.Indice(fx, -0.3) == 6, "-0,3 → abaixo > 0,20");
        Ok(ToleranciaObra.Indice(fx, double.NaN) == -1, "NaN → -1");
        Ok(t.Dentro(0.05) && !t.Dentro(0.0502) && Perto(t.Excesso(0.15), 0.10) && Perto(t.Excesso(-0.08), 0.03) && t.Excesso(0.01) == 0, "Dentro/Excesso");
        Ok(t.Descricao == "+0,05 / -0,05 m", t.Descricao);

        // assimétrica: nada acima, 3 cm abaixo
        var ta = new ToleranciaObra { Acima = 0, Abaixo = 0.03, Limites = new() { 0.1, 0.02, 0.02, -1 } };
        ta.Validar();
        var fa = ta.Faixas();
        Ok(ta.Limites.SequenceEqual(new[] { 0.02, 0.1 }), "limites ordenados e limpos");
        Ok(fa.Count(f => f.Sinal == 1) == 3 && fa.Count(f => f.Sinal == -1) == 2, "assimétrica: 3 acima, 2 abaixo");
        Ok(fa[ToleranciaObra.Indice(fa, 0.001)].Sinal == 1 && ToleranciaObra.Indice(fa, 0) == ToleranciaObra.IndiceDentro(fa), "assimétrica: 0 dentro, +1 mm acima");
        Ok(fa[ToleranciaObra.Indice(fa, -0.03)].Sinal == 0 && fa[ToleranciaObra.Indice(fa, -0.031)].Sinal == -1, "assimétrica: -3 cm dentro");
        try { new ToleranciaObra { Acima = -0.01 }.Validar(); Ok(false, "tolerância negativa aceita"); } catch (ArgumentException) { Ok(true, ""); }

        // malha 100 × 100 m: x<20 → +0,15; 20≤x<30 → -0,08; resto +0,01
        double Dif(double x, double y) => x < 20 ? 0.15 : x < 30 ? -0.08 : 0.01;
        var faixas = t.Faixas();
        var r = Conformidade.Calcular(0, 0, 100, 100, 1, Dif, null, faixas);
        var g = r.Geral;
        Ok(r.Linhas == 100 && r.Colunas == 100, "malha 100×100");
        Ok(Perto(g.Area, 10000) && Perto(g.AreaAcima, 2000) && Perto(g.AreaAbaixo, 1000) && Perto(g.AreaDentro, 7000), $"áreas {g.Area} {g.AreaAcima} {g.AreaAbaixo} {g.AreaDentro}");
        Ok(Perto(g.PctDentro, 70) && Perto(g.PctAcima, 20) && Perto(g.PctAbaixo, 10), "percentuais");
        Ok(Perto(g.Media, 0.029), $"média {g.Media}");
        double var2 = (0.15 * 0.15 * 2000 + 0.08 * 0.08 * 1000 + 0.01 * 0.01 * 7000) / 10000;
        Ok(Perto(g.Emq, Math.Sqrt(var2)) && Perto(g.DesvioPadrao, Math.Sqrt(var2 - 0.029 * 0.029)), "EMQ e desvio-padrão");
        Ok(Perto(g.VolumeAcima, 370) && Perto(g.VolumeAbaixo, 80), $"volumes {g.VolumeAcima} {g.VolumeAbaixo}");
        Ok(Perto(g.Maximo, 0.15) && Perto(g.Minimo, -0.08), "máx/mín");
        Ok(g.Situacao(95) == "Não conforme" && g.Situacao(70) == "Conforme" && new EstatisticaDesvio().Situacao(95) == "Sem dados", "situação");
        Ok(Perto(faixas[1].Area, 2000) && Perto(faixas[4].Area, 1000) && Perto(faixas[3].Area, 7000) && Perto(faixas[1].Volume, 300), "áreas por faixa");
        Ok(g.Nome == "Sobreposição das superfícies", g.Nome);

        // por áreas: metade esquerda e direita
        var areas = new List<(string, Func<double, double, bool>)> { ("Esquerda", (x, y) => x < 50), ("Direita", (x, y) => x >= 50) };
        var r2 = Conformidade.Calcular(0, 0, 100, 100, 1, Dif, areas, t.Faixas());
        Ok(r2.Areas.Count == 2 && Perto(r2.Areas[0].PctDentro, 40) && Perto(r2.Areas[1].PctDentro, 100), "por área");
        Ok(r2.Areas[1].Situacao(95) == "Conforme" && r2.Geral.Nome == "Total" && Perto(r2.Geral.Area, 10000), "total das áreas");
        var r3 = Conformidade.Calcular(0, 0, 100, 100, 1, Dif, new List<(string, Func<double, double, bool>)> { ("Só o canto", (x, y) => x < 10 && y < 10) }, t.Faixas());
        Ok(Perto(r3.Geral.Area, 100) && Perto(r3.Areas[0].PctAcima, 100), "só células dentro das áreas");

        // fora das superfícies
        var r4 = Conformidade.Calcular(0, 0, 100, 100, 2, (x, y) => y > 50 ? double.NaN : 0, null, t.Faixas());
        Ok(Perto(r4.Geral.Area, 5000) && float.IsNaN(r4.Desvio[40, 0]) && r4.Faixa[40, 0] == -1, "NaN fora");

        // pontos críticos: pico de -0,40 em (55,5; 55,5)
        double Dif2(double x, double y) => Math.Abs(x - 55.5) < 0.1 && Math.Abs(y - 55.5) < 0.1 ? -0.40 : Dif(x, y);
        var r5 = Conformidade.Calcular(0, 0, 100, 100, 1, Dif2, null, t.Faixas());
        var cr = Conformidade.Criticos(r5, t, 5, 10);
        Ok(cr.Count == 5, $"5 críticos ({cr.Count})");
        Ok(cr[0].Numero == 1 && Perto(cr[0].X, 55.5) && Perto(cr[0].Y, 55.5) && Perto(cr[0].Desvio, -0.40, 1e-6) && Perto(cr[0].Excesso, 0.35, 1e-6), "P1 no pico");
        Ok(r5.Faixa[55, 55] == 6 && cr[0].Faixa == 6, "faixa do pico");
        bool afastados = true;
        for (int i = 0; i < cr.Count; i++) for (int j = i + 1; j < cr.Count; j++)
            if (Math.Sqrt(Math.Pow(cr[i].X - cr[j].X, 2) + Math.Pow(cr[i].Y - cr[j].Y, 2)) < 10 - 1e-9) afastados = false;
        Ok(afastados, "críticos afastados 10 m");
        Ok(cr.Skip(1).All(p => Perto(p.Excesso, 0.10, 1e-6)), "demais críticos na faixa +0,15");
        Ok(Conformidade.Criticos(Conformidade.Calcular(0, 0, 10, 10, 1, (x, y) => 0.01, null, t.Faixas()), t, 5, 10).Count == 0, "sem críticos dentro da tolerância");

        // afastamentos e estacas
        var af = Conformidade.Afastamentos(-12.3, 12.3);
        Ok(af.Length == 25 && af[0] == -12 && af[^1] == 12 && af.Contains(0), $"afastamentos ±12,3 ({af.Length})");
        var af2 = Conformidade.Afastamentos(-150, 150);
        Ok(af2.Length == 31 && af2[1] - af2[0] == 10, $"afastamentos ±150 passo 10 ({af2.Length})");
        var af3 = Conformidade.Afastamentos(3.2, 3.4);
        Ok(af3.Length == 1, $"faixa estreita ({string.Join(";", af3)})");
        Ok(Conformidade.Afastamentos(double.NaN, 1).Length == 0, "sem faixa");
        Ok(Conformidade.Estacas(5, 65, 20).SequenceEqual(new[] { 5.0, 20, 40, 60, 65 }), string.Join(";", Conformidade.Estacas(5, 65, 20)));
        Ok(Conformidade.Estacas(0, 40, 20).SequenceEqual(new[] { 0.0, 20, 40 }), string.Join(";", Conformidade.Estacas(0, 40, 20)));

        var ce = new ControleEstacas { Alinhamento = "Eixo", Afastamentos = new[] { -2.0, 0, 2 } };
        var lc = new LinhaControle { Estaca = 40, Desvios = new[] { 0.1, 0.0, double.NaN } };
        ce.Linhas.Add(lc);
        ce.Linhas.Add(new LinhaControle { Estaca = 60, Desvios = new[] { 0.01, -0.02, 0.03 } });
        var rs = ce.Resumo(lc, t);
        Ok(rs.Pontos == 2 && rs.Fora == 1 && Perto(rs.Media, 0.05) && Perto(rs.Maximo, 0.1) && rs.AfMaximo == -2 && rs.Minimo == 0 && rs.AfMinimo == 0, "resumo da estaca");
        Ok(rs.Situacao == "1 ponto(s) fora" && ce.Resumo(ce.Linhas[1], t).Situacao == "Conforme", "situação da estaca");

        // planilha
        var rel = new RelatorioConformidade { Projeto = "PROJ", Obra = "OBRA", Tolerancia = t, Geral = r5.Geral, Faixas = t.Faixas(), Celula = 1, Estacas = ce };
        cr.ForEach(p => rel.Criticos.Add(p));
        var pasta = rel.Planilha();
        if (saida is not null) pasta.Salvar(Path.Combine(saida, "conformidade_teste.xlsx"));
        Ok(pasta.Folhas.Select(f => f.Nome).SequenceEqual(new[] { "Resumo", "Faixas de desvio", "Por estaca", "Pontos críticos", "Origem" }), string.Join(",", pasta.Folhas.Select(f => f.Nome)));
        var pe = pasta.Folhas[2];
        Ok((string?)pe.Valor(6, 1) == "2+0,000" && (double?)pe.Valor(6, 2) == 0.1 && (string?)pe.Valor(6, 4) == "-", "por estaca: linha 6");
        Ok((string?)pasta.Folhas[0].Valor(6, 13) == "Não conforme" && rel.Situacao == "Não conforme", "resumo: situação");
        Ok(rel.Linhas.Count == 1, "uma linha sem áreas");
    }
}
