using Projeta.Core.Alinhamentos;
using Projeta.Giro.Core.Geometria;

namespace Projeta.Core.Testes;

/// <summary>Testes do comparador de alternativas (PRJALTERNATIVAS).</summary>
internal static class TestesAlternativas
{
    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol) => Math.Abs(a - b) <= tol;

        var c3 = ComparadorAlternativas.ConflitosConvencional(3);
        var c4 = ComparadorAlternativas.ConflitosConvencional(4);
        var r4 = ComparadorAlternativas.ConflitosRotatoria(4);
        Ok(c3 == (3, 3, 3) && c4 == (16, 8, 8) && r4 == (0, 4, 4) && ComparadorAlternativas.ConflitosRotatoria(3) == (0, 3, 3),
           "pontos de conflito: T 9, cruzamento 32, rotatória 8 e 6 (FHWA)");

        var o = new V2(1000, 2000);
        var esq = ComparadorAlternativas.Esquina(o, 0, 3.5, Math.PI / 2, 3.5, 10, out var cc);
        double aEsq = esq is null ? 0 : Math.Abs(Geo.Area(esq));
        Ok(esq is not null && Q(aEsq, 100 - Math.PI * 25, 0.3) && Q(cc.X, 1013.5, 1e-6) && Q(cc.Y, 2013.5, 1e-6), $"concordância da esquina a 90° com R 10: {aEsq:0.00} m² (100 − 25π)");
        Ok(ComparadorAlternativas.Esquina(o, 0, 3.5, Math.PI, 3.5, 10, out _) is null, "sem concordância entre braços alinhados");
        var cx = ComparadorAlternativas.ConexaoAnel(o, 0, 20, 6, 15, 1);
        Ok(cx is not null && cx.Count > 5 && Q(cx[0].Dist(o), 20, 1e-6) && Q(cx[^1].Dist(o), 20, 0.01), "concordância do bordo do braço com o anel começa e termina no anel");

        // cruzamento de 4 braços a 90°, principal L-O
        var c = new CenarioAlternativas { Nome = "teste", Origem = "teste", Ox = o.X, Oy = o.Y };
        string[] rumos = { "L", "N", "O", "S" };
        for (int i = 0; i < 4; i++) c.Bracos.Add(new BracoAlternativa { Nome = "via " + rumos[i], Rumo = rumos[i], Angulo = i * Math.PI / 2, MeiaLargura = 3.5, Principal = i % 2 == 0 });
        var p = new ParametrosAlternativas();
        var pc = new ParametrosCapacidade();
        Func<double, double, double?> terreno = (x, y) => 100 + 0.01 * (x - o.X) - 0.02 * (y - o.Y);
        var r = ComparadorAlternativas.Gerar(c, p, pc, terreno);
        var a = r.Alternativas.ToDictionary(x => x.Codigo);
        Ok(r.Alternativas.Count == 5 && r.Alternativas.All(x => x.Aplicavel), "as cinco alternativas montadas: " + string.Join(", ", r.Alternativas.Select(x => x.Nome + (x.Aplicavel ? "" : " (" + x.Motivo + ")"))));
        double z = r.Zona, esperado = 2 * 2 * z * 7 - 49 + 4 * (144 - Math.PI * 36);
        Ok(Q(a["PARE"].AreaPavimento, esperado, 2.0), $"PARE: pavimento {a["PARE"].AreaPavimento:0.0} m² (esperado {esperado:0.0})");
        double mf = 8 * (z - 15.5) + 4 * Math.PI / 2 * 12;
        Ok(Q(a["PARE"].MeioFio, mf, 1.0), $"PARE: meio-fio {a["PARE"].MeioFio:0.0} m (esperado {mf:0.0})");
        Ok(a["PARE"].AreaAdicional < 1.0, $"PARE dentro da via existente com passeio: área adicional {a["PARE"].AreaAdicional:0.0} m²");
        Ok(a["CANALIZADA"].Ilhas.Count == 4 && a["CANALIZADA"].AreaIlhas > 4 * p.IlhaMinima && a["CANALIZADA"].AreaPavimento > a["PARE"].AreaPavimento,
           $"canalizada: {a["CANALIZADA"].Ilhas.Count} ilhas de giro ({a["CANALIZADA"].AreaIlhas:0.0} m²) e pavimento {a["CANALIZADA"].AreaPavimento:0} m²");
        Ok(Q(a["MINI"].AreaTransponivel, Math.PI * 36, 0.5) && a["ROT1"].Ilhas.Count == 5 && a["ROT2"].Ilhas.Count == 5,
           $"mini com ilha transponível de {a["MINI"].AreaTransponivel:0.0} m²; rotatórias com 4 ilhas divisórias + a central");
        Ok(a["ROT2"].AreaOcupada > a["ROT1"].AreaOcupada && a["ROT1"].AreaOcupada > a["MINI"].AreaOcupada && a["ROT2"].AreaAdicional > a["ROT1"].AreaAdicional,
           $"áreas ocupadas: mini {a["MINI"].AreaOcupada:0}, 1 faixa {a["ROT1"].AreaOcupada:0}, 2 faixas {a["ROT2"].AreaOcupada:0} m²");
        double eTot = p.Revestimento + p.Base + p.SubBase;
        Ok(a["PARE"].Corte is { } ct && Q(ct, a["PARE"].AreaOcupada * eTot, a["PARE"].AreaOcupada * eTot * 0.03) && (a["PARE"].Aterro ?? 1) < 1e-6,
           $"terraplenagem no terreno plano = caixa do pavimento: corte {a["PARE"].Corte:0.0} m³ (≈ {a["PARE"].AreaOcupada * eTot:0.0})");
        Ok(a["PARE"].Conflitos == 32 && a["ROT1"].Conflitos == 8 && a["ROT2"].Conflitos == 12 && a["MINI"].Cruzamento == 0, "conflitos nas alternativas");
        Ok(!r.ComVolumes && r.Alternativas.All(x => x.Situacao == "sem volumes") && r.Recomendada is not null, $"sem volumes: recomendada {r.Recomendada?.Nome} ({r.Recomendada?.Pontuacao:0} pontos)");

        // com volumes e preços
        void V(int oo, int d, double v) => c.Volumes[(oo, d)] = v;
        V(0, 2, 600); V(2, 0, 600); V(0, 1, 80); V(0, 3, 80); V(2, 1, 80); V(2, 3, 80);
        V(1, 3, 150); V(3, 1, 150); V(1, 0, 80); V(1, 2, 80); V(3, 0, 80); V(3, 2, 80);
        foreach (var pr in p.Precos) pr.Preco = 1;
        var r2 = ComparadorAlternativas.Gerar(c, p, pc, terreno);
        var b = r2.Alternativas.ToDictionary(x => x.Codigo);
        Ok(r2.ComVolumes && r2.ComPrecos && b.Values.All(x => x.CustoCompleto && x.Custo > 0) && r2.Criterios.Count == 5,
           $"com volumes, preços e terreno: 5 critérios; custo do PARE {b["PARE"].Custo:N0}");
        Ok(b.Values.All(x => !double.IsNaN(x.XMax)) && b["ROT2"].XMax < b["ROT1"].XMax && b["PARE"].XMax > b["ROT1"].XMax,
           $"x máximo: PARE {CalculoCapacidade.TxtX(b["PARE"].XMax)}, 1 faixa {CalculoCapacidade.TxtX(b["ROT1"].XMax)}, 2 faixas {CalculoCapacidade.TxtX(b["ROT2"].XMax)}");
        Ok(r2.Recomendada?.Codigo == "ROT2" && r2.Recomendada.Posicao == 1 && b.Values.All(x => x.Pontuacao >= 0 && x.Pontuacao <= 100) && b.Values.Select(x => x.Posicao).OrderBy(x => x).SequenceEqual(new[] { 1, 2, 3, 4, 5 }) && r2.Recomendada is not null && r2.Recomendada.Situacao != "não atende",
           $"ranking: " + string.Join(", ", b.Values.OrderBy(x => x.Posicao).Select(x => $"{x.Posicao}ª {x.Nome} {x.Pontuacao:0} ({x.Situacao})")));

        // T: PARE com 3 ramos
        var t = new CenarioAlternativas { Nome = "T", Ox = 0, Oy = 0 };
        t.Bracos.Add(new BracoAlternativa { Rumo = "L", Angulo = 0, Principal = true });
        t.Bracos.Add(new BracoAlternativa { Rumo = "N", Angulo = Math.PI / 2 });
        t.Bracos.Add(new BracoAlternativa { Rumo = "O", Angulo = Math.PI, Principal = true });
        var rt = ComparadorAlternativas.Gerar(t, new ParametrosAlternativas(), pc, null);
        var at = rt.Alternativas.ToDictionary(x => x.Codigo);
        Ok(at.Values.All(x => x.Aplicavel) && at["PARE"].Conflitos == 9 && at["ROT1"].Conflitos == 6 && !rt.ComTerreno, "interseção em T: 9 conflitos no PARE e 6 na rotatória");
        var cinco = new CenarioAlternativas { Nome = "5", Ox = 0, Oy = 0 };
        for (int i = 0; i < 5; i++) cinco.Bracos.Add(new BracoAlternativa { Rumo = i.ToString(), Angulo = i * 2 * Math.PI / 5, Principal = i < 2 });
        var r5 = ComparadorAlternativas.Gerar(cinco, new ParametrosAlternativas(), pc, null);
        Ok(!r5.Alternativas.First(x => x.Codigo == "PARE").Aplicavel && r5.Alternativas.First(x => x.Codigo == "ROT1").Aplicavel, "5 ramos: PARE não se aplica, rotatória sim");

        // preços em CSV
        string csv = Path.Combine(saida, "precos_teste.csv");
        File.WriteAllText(csv, "item;descrição;unidade;preço;fonte\nPAV-CBUQ;CBUQ faixa C;t;1.234,56;SICRO MG 07/2026\nMEIO-FIO;Meio-fio;m;45,10;SINAPI 94273\nDESAP;Desapropriação;m²;;\n", new System.Text.UTF8Encoding(false));
        var lp = RelatorioAlternativas.LerPrecos(csv);
        Ok(lp.Count == 3 && Q(lp[0].Preco ?? 0, 1234.56, 1e-9) && Q(lp[1].Preco ?? 0, 45.10, 1e-9) && lp[2].Preco is null && lp[0].Fonte == "SICRO MG 07/2026",
           "CSV de preços com decimal brasileiro e preço em branco");
        Ok(RelatorioAlternativas.ModeloPrecos(new ParametrosAlternativas()).Split('\n').Count(x => x.Contains(';')) == 11, "modelo do CSV de preços com os 10 itens");

        var rel = new RelatorioAlternativas { Comparacao = r2, Desenho = "teste.dwg", Superficie = "plano" };
        string arq = Path.Combine(saida, "alternativas.xlsx");
        rel.Salvar(arq);
        var quadro = ComparadorAlternativas.Quadro(r2);
        Ok(new FileInfo(arq).Length > 5000 && quadro.Colunas.Count == 6 && quadro.Linhas.Count >= 15, $"planilha de decisão e quadro ({quadro.Linhas.Count} linhas)");
    }
}
