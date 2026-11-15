using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;

namespace Projeta.Core.Testes;

/// <summary>Testes da sinalização (PRJSINALIZA): interseção em cruz, faixa de conversão, rotatória, quantitativo e gravação no desenho.</summary>
internal static class TestesSinalizacao
{
    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;
        static List<double[]> Reta(double ax, double ay, double bx, double by) => new() { new[] { ax, ay }, new[] { bx, by } };

        // cruz: principal A (L-O, meia largura 3,5), secundária B (N-S, 3,0)
        var cruz = new ModeloIntersecaoSinal
        {
            Bracos =
            {
                new BracoIntersecaoSinal { Nome = "A", Rumo = "L", Principal = true, Eixo = Reta(0, 0, 100, 0), MeiaLargura = 3.5 },
                new BracoIntersecaoSinal { Nome = "B", Rumo = "N", Eixo = Reta(0, 0, 0, 100), MeiaLargura = 3.0 },
                new BracoIntersecaoSinal { Nome = "A", Rumo = "O", Principal = true, Eixo = Reta(0, 0, -100, 0), MeiaLargura = 3.5 },
                new BracoIntersecaoSinal { Nome = "B", Rumo = "S", Eixo = Reta(0, 0, 0, -100), MeiaLargura = 3.0 },
            },
        };
        var eixos = cruz.Bracos.Select(b => b.Polilinha()).ToList();
        double dN = GeradorSinalizacao.DistanciaTransversal(cruz, eixos, 1, out bool achouN);
        double dL = GeradorSinalizacao.DistanciaTransversal(cruz, eixos, 0, out bool achouL);
        Ok(achouN && achouL && Q(dN, 3.5, 1e-6) && Q(dL, 3.0, 1e-6), $"bordo da via transversal: secundária a {dN:0.000} m, principal a {dL:0.000} m");

        var p = new ParametrosSinalizacao();
        var r = GeradorSinalizacao.Intersecao("Interseção 1", cruz, p);
        var ftp = r.Marcas.Where(m => m.Codigo == "FTP-1").ToList();
        Ok(ftp.Count == 12 && Q(ftp.Sum(m => m.Area), 12 * 0.4 * 4.0, 1e-6), $"faixas de pedestres só nas secundárias: 2 × 6 listras de 0,40 × 4,00 m ({ftp.Count}, {ftp.Sum(m => m.Area):0.00} m²)");
        var lre = r.Marcas.Where(m => m.Codigo == "LRE").ToList();
        var lreN = lre.First(m => m.Onde.Contains("N"));
        double yMin = lreN.Contorno.Min(c => c.Y), xMin = lreN.Contorno.Min(c => c.X), xMax = lreN.Contorno.Max(c => c.X);
        Ok(lre.Count == 2 && Q(yMin, 3.5 + 1 + 4 + 1.6, 1e-6) && Q(lreN.Area, 0.4 * 3.0, 1e-6) && Q(xMin, -3.0, 1e-6) && Q(xMax, 0, 1e-6),
           $"LRE a 1,60 m da faixa, só na mão de quem chega (y {yMin:0.00}, x {xMin:0.00} a {xMax:0.00})");
        var r1 = r.Placas.Where(x => x.Codigo == "R-1").ToList();
        var r1N = r1.First(x => x.Onde.Contains("N"));
        Ok(r1.Count == 2 && Q(r1N.X, -(3.0 + 0.30 + 0.35 * (1 + Math.Sqrt(2)) / 2), 1e-6) && Q(r1N.Y, 10.3, 1e-6) && Q(r1N.Angulo, -Math.PI / 2, 1e-9),
           $"R-1 à direita de quem chega, 0,30 m do bordo ({r1N.X:0.000}; {r1N.Y:0.000})");
        var leg = r.Legendas.First(x => x.Onde.Contains("N"));
        Ok(r.Legendas.Count == 2 && Q(leg.X, -1.5, 1e-6) && Q(leg.Y, 10.5 + 1.6 + 0.8, 1e-6) && Q(leg.Altura, 1.6), $"legenda PARE a 1,60 m antes da LRE ({leg.X:0.00}; {leg.Y:0.00})");
        Ok(r.Verificacoes.Count(v => v.Item.StartsWith("Posição do R-1") && v.Resultado == VerificacaoRotatoria.Aprovado) == 2, "R-1 a 6,8 m do meio-fio da principal: aprovado");
        Ok(r.Marcas.Count(m => m.Codigo == "LFO-1") == 4 && r.Marcas.Where(m => m.Codigo == "LFO-1").All(m => m.Cor == "amarela"), "eixo amarelo contínuo nos 4 braços");

        // faixa de conversão à esquerda no braço L da principal (alargada 3,6 m)
        cruz.Bracos[0].Alargamento = 3.6; cruz.Bracos[0].DPlena = 40; cruz.Bracos[0].Teiper = 30; cruz.Bracos[0].Conversao = true; cruz.Bracos[0].DRetencaoConversao = 4.0;
        cruz.Bracos[2].Alargamento = 3.6; cruz.Bracos[2].DPlena = 10; cruz.Bracos[2].Teiper = 30;
        eixos = cruz.Bracos.Select(b => b.Polilinha()).ToList();
        double dN2 = GeradorSinalizacao.DistanciaTransversal(cruz, eixos, 1, out _);
        Ok(Q(dN2, 3.5 + 1.8, 1e-6), $"com o alargamento, o bordo da principal fica a {dN2:0.000} m");
        var r2 = GeradorSinalizacao.Intersecao("Interseção 1", cruz, p);
        int setas = r2.Marcas.Count(m => m.Codigo == "Seta");
        Ok(setas == 6 && r2.Marcas.Any(m => m.Codigo == "LMS-1" && m.Onde.Contains("L")), $"faixa de conversão: linha branca e 2 setas a 10 e 30 m ({setas / 3} setas)");
        var lfoConv = r2.Marcas.Where(m => m.Codigo == "LFO-1" && m.Onde.Contains("braço L")).ToList();
        Ok(lfoConv.Count == 2 && lfoConv[0].Contorno.Max(c => c.X) > 69.9 && lfoConv[0].Contorno.Min(c => c.Y) < -1.8, "linha amarela do lado do fluxo oposto acompanha o teiper");
        cruz.Bracos[0].Alargamento = 0; cruz.Bracos[0].Conversao = false; cruz.Bracos[2].Alargamento = 0;

        // rotatória: 4 braços retos, DCI 40, anel de 6 m, ilha divisória 3,0 → 0,6 m em 30,5 m
        ModeloRotatoriaSinal Rot(double rIlha)
        {
            var m = new ModeloRotatoria { RExt = 20, RIlha = rIlha };
            foreach (double ang in new[] { 0.0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 })
            {
                double c = Math.Cos(ang), s = Math.Sin(ang);
                m.Bracos.Add(new BracoRotatoria
                {
                    Nome = "R" + (int)Math.Round(ang * 180 / Math.PI), Rumo = GeometriaRotatoria.Rumo(ang), Angulo = ang,
                    Eixo = new PolilinhaEixo(new[] { (17 * c, 17 * s), (217 * c, 217 * s) }),
                    SRetencao = 3, BordoEntrada = 6.5, BordoSaida = 6.5, BordoNormal = 3.5,
                    SIlhaIni = 4, SIlhaFim = 34.5, IlhaLarguraIni = 3.0, IlhaLarguraFim = 0.6,
                });
            }
            return ModeloRotatoriaSinal.De(m, 6, 0);
        }
        var rot = Rot(14);
        var rr = GeradorSinalizacao.Rotatoria("Rotatória 1", rot, p);
        var ldp = rr.Marcas.Where(m => m.Codigo == "LDP").ToList();
        double rL = 20 + 1.6 + 0.15;
        bool raioOk = ldp.SelectMany(m => m.Contorno).All(c => { double d = Math.Sqrt(c.X * c.X + c.Y * c.Y); return d >= rL - 0.15 - 1e-6 && d <= rL + 0.15 + 1e-6; });
        Ok(ldp.Count > 0 && raioOk, $"LDP tracejada a 1,60 m do bordo externo do anel ({ldp.Count} traços)");
        var ldpL = ldp.Where(m => m.Onde.Contains("R0")).SelectMany(m => m.Contorno).ToList();
        double yMax = ldpL.Max(c => c.Y), yMinL = ldpL.Min(c => c.Y);
        Ok(yMax <= 6.5 + 0.2 && yMax >= 6.5 - 0.6 && yMinL >= 1.0, $"LDP vai da ilha divisória ao meio-fio da entrada (y {yMinL:0.00} a {yMax:0.00})");
        Ok(rr.Placas.Count(x => x.Codigo == "R-2") == 4 && rr.Placas.Count(x => x.Codigo == "R-24a") == 4 && !rr.Placas.Any(x => x.Codigo == "R-33"),
           "ilha central de raio 14 m: R-2 nas 4 entradas e R-24a na ilha");
        var r24 = rr.Placas.First(x => x.Codigo == "R-24a" && x.Onde.Contains("R0"));
        Ok(Q(r24.X, 14 - 1 - 0.25, 1e-6) && Q(r24.Y, 0, 1e-6) && Q(GeometriaRotatoria.Normalizar(r24.Angulo), Math.PI, 1e-9), $"R-24a na ilha central, de frente para quem entra ({r24.X:0.00}; {r24.Y:0.00})");
        int listras = rr.Marcas.Count(m => m.Codigo == "FTP-1");
        Ok(listras == 4 * 2 * 5, $"faixas de pedestres dos dois lados da ilha divisória ({listras} listras)");
        Ok(rr.Verificacoes.Where(v => v.Item.StartsWith("Posição do R-2")).All(v => v.Resultado == VerificacaoRotatoria.Aprovado), "R-2 junto da LDP: aprovado");
        var rrPeq = GeradorSinalizacao.Rotatoria("Rotatória 2", Rot(10), DuasFaixas(p));
        Ok(rrPeq.Placas.Count(x => x.Codigo == "R-33") == 4 && rrPeq.Placas.Count(x => x.Codigo == "R-2") == 8 && rrPeq.Marcas.Any(m => m.Codigo == "LMS-2"),
           "ilha central de 10 m: R-33 nas entradas; 2 faixas: R-2 também na ilha e linha seccionada no anel");

        // quantitativo, planilha e gravação (JSON) com infinito e NaN
        var quant = RelatorioSinalizacao.Quantitativo(new[] { r, rr });
        double brancas = r.Marcas.Concat(rr.Marcas).Where(m => m.Cor == "branca").Sum(m => m.Area);
        Ok(Q(quant.Where(l => l.Unidade == "m²" && l.Cor == "branca").Sum(l => l.Quantidade), brancas, 1e-9) && quant.Any(l => l.Codigo == "Suporte" && l.Quantidade == r.Placas.Count + rr.Placas.Count),
           $"quantitativo soma {brancas:0.00} m² de pintura branca e um suporte por placa");
        var rel = new RelatorioSinalizacao { Titulo = "Sinalização - teste" };
        rel.Itens.Add(r); rel.Itens.Add(rr);
        rel.Parametros.AddRange(RelatorioSinalizacao.ParametrosDe(p));
        rel.Pendencias.AddRange(RelatorioSinalizacao.PendenciasPadrao());
        string arq = Path.Combine(saida, "sinalizacao.xlsx");
        rel.Salvar(arq);
        Ok(File.Exists(arq) && new FileInfo(arq).Length > 3000, "planilha do quantitativo gravada");
        var item = new ItemSinalizacao { Nome = "Rotatória 1", Tipo = "Rotatória", Rotatoria = rot, Desenhados = { "2A0", "2A1" } };
        var volta = JsonProjeta.Desserializar<ItemSinalizacao>(JsonProjeta.Serializar(item, false))!;
        var rv = GeradorSinalizacao.Rotatoria("Rotatória 1", volta.Rotatoria!, p);
        Ok(volta.Desenhados.Count == 2 && double.IsPositiveInfinity(volta.Rotatoria!.Bracos[0].SAlargIni) && double.IsNaN(volta.Rotatoria.Bracos[0].STangEntrada)
           && rv.Marcas.Count == rr.Marcas.Count && Q(rv.Marcas.Sum(m => m.Area), rr.Marcas.Sum(m => m.Area), 1e-9), "modelo guardado em JSON volta igual (infinito e NaN incluídos)");
    }

    private static ParametrosSinalizacao DuasFaixas(ParametrosSinalizacao p, int faixas = 2)
    {
        var c = JsonProjeta.Clonar(p);
        c.FaixasEntrada = faixas;
        c.FaixasAnel = faixas;
        return c;
    }
}
