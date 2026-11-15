using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes da locação de campo (PRJLOCACAO).</summary>
internal static class TestesLocacao
{
    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;
        var p = new ParametrosLocacao { NumeroInicial = 101 };

        // tangente de 20 m, curva de 90° com R = 10 m (à esquerda) e tangente de 20 m
        var el = new ElementoLocacao
        {
            Nome = "meio-fio", Codigo = "MF",
            Vertices = new() { new(0, 0), new(20, 0, Math.Tan(Math.PI / 8)), new(30, 10), new(30, 30) },
        };
        var (cx, cy, r, _, sw) = LocacaoCampo.Arco(el.Vertices[1], el.Vertices[2]);
        Ok(Q(cx, 20) && Q(cy, 10) && Q(r, 10) && Q(sw, Math.PI / 2), $"arco pelo bulge: centro ({cx:0.00}; {cy:0.00}), R {r:0.00}");
        var res = LocacaoCampo.Gerar(new[] { el }, p, (x, y) => 100 + 0.01 * x);
        var tipos = res.Pontos.Select(x => x.Tipo).ToList();
        Ok(res.Pontos.Count == 9 && tipos.Count(t => t == "PC") == 1 && tipos.Count(t => t == "PT") == 1 && tipos.Contains("INÍCIO") && tipos.Contains("FIM") && tipos.Contains("CENTRO"),
           $"tangente, curva e tangente: {res.Pontos.Count} pontos ({string.Join(" ", tipos.Select(t => t.Length == 0 ? "·" : t))})");
        var pc = res.Pontos.First(x => x.Tipo == "PC");
        var pt = res.Pontos.First(x => x.Tipo == "PT");
        Ok(Q(pc.X, 20) && Q(pc.Y, 0) && Q(pt.X, 30) && Q(pt.Y, 10) && res.Pontos[0].Numero == 101 && res.Pontos[^1].Numero == 109 && Q(pc.Z ?? 0, 100.2),
           $"PC em (20; 0), PT em (30; 10), numeração de 101 a 109, cota da superfície ({pc.Z:0.000})");
        var c = res.Curvas.Single();
        Ok(Q(c.Raio, 10) && Q(c.Deflexao, 90, 1e-9) && Q(c.Desenvolvimento, 5 * Math.PI, 1e-9) && Q(c.Tangente, 10, 1e-9) && Q(c.Corda, Math.Sqrt(200), 1e-9) && c.PontoPc == pc.Numero && c.PontoPt == pt.Numero && c.Sentido == "à esquerda",
           $"tabela da curva: R {c.Raio:0.00}, Δ {c.Deflexao:0.00}°, D {c.Desenvolvimento:0.000}, T {c.Tangente:0.000}, corda {c.Corda:0.000}");

        // a mesma geometria discretizada a cada 1 m (como os bordos desenhados): vira tangente, arco e tangente de novo
        var disc = new List<VerticeVia>();
        for (int i = 0; i < 20; i++) disc.Add(new(i, 0));
        int na = 16;
        for (int i = 0; i < na; i++) { double t = -Math.PI / 2 + Math.PI / 2 * i / na; disc.Add(new(20 + 10 * Math.Cos(t), 10 + 10 * Math.Sin(t))); }
        for (int i = 0; i <= 20; i++) disc.Add(new(30, 10 + i));
        var simp = LocacaoCampo.Simplificar(disc);
        Ok(simp.Count == 4 && Q(simp[1].X, 20) && Q(simp[1].Y, 0) && Q(simp[1].Bulge, Math.Tan(Math.PI / 8), 1e-6) && Q(simp[2].X, 30) && Q(simp[2].Y, 10) && Q(simp[3].Y, 30),
           $"polilinha discretizada a cada 1 m ({disc.Count} vértices) vira {simp.Count}: tangente, arco R 10 e tangente [{string.Join(" ", simp.Select(q => $"({q.X:0.###};{q.Y:0.###};{q.Bulge:0.####})"))}]");
        var rd = LocacaoCampo.Gerar(new[] { new ElementoLocacao { Nome = "mf", Codigo = "MF", Vertices = disc } }, p);
        Ok(rd.Pontos.Count == 9 && rd.Curvas.Count == 1 && Q(rd.Curvas[0].Raio, 10, 1e-3), $"locação da discretizada igual à da original: {rd.Pontos.Count} pontos e 1 curva");
        var oct = new List<VerticeVia>();
        for (int i = 0; i < 8; i++) oct.Add(new(3 * Math.Cos(i * Math.PI / 4), 3 * Math.Sin(i * Math.PI / 4)));
        Ok(LocacaoCampo.Simplificar(oct).Count == 8, "octógono (deflexão de 45°) não vira arco");

        // dois meios-fios que se encontram: um ponto só
        var a = new ElementoLocacao { Nome = "a", Codigo = "MF", Vertices = new() { new(0, 0), new(10, 0) } };
        var b = new ElementoLocacao { Nome = "b", Codigo = "MF", Vertices = new() { new(10, 0), new(10, 10) } };
        var rab = LocacaoCampo.Gerar(new[] { a, b }, new ParametrosLocacao());
        Ok(rab.Pontos.Count == 3 && rab.Pontos.Any(x => x.Tipo == "FIM/INÍCIO" && Q(x.X, 10) && Q(x.Y, 0)), $"ponto comum a dois elementos vira um só ({string.Join(", ", rab.Pontos.Select(x => x.Tipo))})");

        // ilha fechada (retângulo) e círculo
        var ilha = new ElementoLocacao { Nome = "ilha", Codigo = "ILHA", Fechado = true, Vertices = new() { new(0, 0), new(4, 0), new(4, 2), new(0, 2) } };
        var circ = new ElementoLocacao { Nome = "ilha central", Codigo = "ILHA", Circulo = (50, 50, 10) };
        var rf = LocacaoCampo.Gerar(new[] { ilha, circ }, new ParametrosLocacao { IntervaloCurva = 5 });
        Ok(rf.Pontos.Count(x => x.Tipo == "V") == 4 && rf.Pontos.Count(x => x.Elemento == "ilha central") == 14 && rf.Pontos.Any(x => x.Tipo == "CENTRO" && Q(x.X, 50)),
           $"ilha fechada com 4 vértices e círculo de R 10 com {rf.Pontos.Count(x => x.Elemento == "ilha central") - 1} pontos + centro");

        // CSV e quadros
        string csv = LocacaoCampo.Csv(res.Pontos, p);
        var l1 = csv.Split("\r\n")[0];
        Ok(l1 == "101,0.000,0.000,100.000,MF INÍCIO", $"CSV PNEZD: '{l1}'");
        string csv2 = LocacaoCampo.Csv(res.Pontos.Take(1), new ParametrosLocacao { Formato = "PENZD" });
        Ok(csv2.StartsWith("101,0.000,0.000,", StringComparison.Ordinal) && LocacaoCampo.Csv(rab.Pontos, new ParametrosLocacao()).Split("\r\n")[0].EndsWith(",,MF INÍCIO", StringComparison.Ordinal),
           "PENZD e cota vazia quando não há superfície");
        var muitos = Enumerable.Range(1, 120).Select(i => new PontoLocacao(i, i, i, null, "MF", "", "x", "")).ToList();
        var qd = LocacaoCampo.Quadros(muitos, 50, 3);
        Ok(qd.Count == 3 && qd[2].Linhas.Count == 20 && qd[0].Titulo.Contains("1/3"), "120 pontos em 3 quadros de até 50 linhas");
        Ok(LocacaoCampo.CodigoDaCamada("PRJ-ROTATORIA-BORDO") == "MF" && LocacaoCampo.CodigoDaCamada("PRJ-INTERSECAO-ILHA") == "ILHA" && LocacaoCampo.CodigoDaCamada("C-EIXO") == "EIXO",
           "códigos pela camada (MF, ILHA, EIXO)");

        var rel = new RelatorioLocacao { Resultado = res, Parametros = p, Desenho = "teste.dwg", Superficie = "Topo" };
        string arq = Path.Combine(saida, "locacao.xlsx");
        rel.Salvar(arq);
        Ok(new FileInfo(arq).Length > 3000, "nota de locação gravada");
    }
}
