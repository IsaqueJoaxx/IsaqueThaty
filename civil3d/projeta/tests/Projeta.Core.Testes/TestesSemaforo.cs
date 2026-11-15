using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes do plano semafórico (PRJSEMAFORO): entreverdes, Webster, verdes, grupos focais, laços e sinalização com semáforo.</summary>
internal static class TestesSemaforo
{
    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;
        static List<double[]> Reta(double ax, double ay, double bx, double by) => new() { new[] { ax, ay }, new[] { bx, by } };

        Ok(CalculoSemaforico.Amarelo(40) == 3 && CalculoSemaforico.Amarelo(60) == 4 && CalculoSemaforico.Amarelo(80) == 5 && CalculoSemaforico.Amarelo(100) == 5,
           "amarelo: 3 s a 40 km/h, 4 s a 60, 5 s a 80 e no máximo 5 s");
        Ok(CalculoSemaforico.Amarelo(60, -0.05) == 5, "amarelo maior na descida (60 km/h, -5 %: 4,3 s → 5 s)");
        Ok(CalculoSemaforico.VermelhoGeral(13.6, 40) == 2 && CalculoSemaforico.VermelhoGeral(0, 100) == 1, "vermelho geral (d2 + 5)/v, no mínimo 1 s");
        Ok(Q(CalculoSemaforico.CicloWebster(10, 0.68), 62.5, 1e-9), "ciclo ótimo de Webster bate com o exemplo do manual (Tp 10 s, Σy 0,68 → 62,5 s)");
        double at = CalculoSemaforico.AtrasoWebster(60, 0.5, 0.8, 0.25);
        Ok(Q(at, 16.55, 0.05), $"atraso de Webster (c 60 s, λ 0,5, x 0,8, q 0,25 veíc/s → {at:0.00} s)");

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
        var ps = new ParametrosSinalizacao();
        var p = new ParametrosSemaforo();
        var plano = CalculoSemaforico.Calcular("Interseção 1", cruz, new double[] { 600, 250, 550, 200 }, p, ps);
        Ok(plano.Estagios.Count == 2 && plano.Estagios[0].Via == "A" && Q(plano.TempoPerdido, 10) && Q(plano.Y, 600.0 / 1800 + 250.0 / 1800, 1e-9),
           $"2 estágios (principal primeiro), tempo perdido {plano.TempoPerdido:0} s, Σy {plano.Y:0.000}");
        Ok(Q(plano.CicloOtimo, 20 / (1 - plano.Y), 1e-9) && plano.Ciclo == 39 && plano.Estagios[0].Verde == 19 && plano.Estagios[1].Verde == 10,
           $"Webster {plano.CicloOtimo:0.0} s; verde mínimo de 10 s na secundária leva o ciclo a {plano.Ciclo:0} s (verdes {plano.Estagios[0].Verde:0} / {plano.Estagios[1].Verde:0})");
        var gL = plano.Aproximacoes[0];
        Ok(Q(gL.GrauSaturacao, 600 / (1800 * 19.0 / 39), 1e-9) && !double.IsNaN(gL.Atraso) && gL.Fila > 0, $"grau de saturação da principal {gL.GrauSaturacao:0.000}, atraso {gL.Atraso:0.0} s, fila {gL.Fila:0} m");
        var pedN = plano.Grupos.First(g => g.Pedestre && g.Braco == 1);
        Ok(plano.Grupos.Count == 8 && pedN.Estagio == 1 && pedN.VermelhoIntermitente == 6 && pedN.Verde == 24 - 6 - 1,
           $"pedestres que atravessam a secundária andam com a principal: verde {pedN.Verde:0} s + intermitente {pedN.VermelhoIntermitente:0} s");
        var colN = plano.Cabecas.First(c => c.Grupo == "G2" && c.Tipo.Contains("coluna"));
        Ok(plano.Cabecas.Count == 16 && Q(colN.X, -3.8, 1e-6) && Q(colN.Y, -4.5, 1e-6) && Q(colN.Distancia, 10.1 + 4.5, 1e-6),
           $"coluna do braço N depois do cruzamento, à direita, a 0,80 m do meio-fio ({colN.X:0.00}; {colN.Y:0.00}), {colN.Distancia:0.0} m da retenção");
        Ok(plano.Verificacoes.Where(v => v.Item.StartsWith("Grupo focal veicular")).All(v => v.Resultado == VerificacaoRotatoria.Aprovado), "grupos focais a 13–30 m da retenção e visada abaixo de 20°");
        Ok(plano.Verificacoes.Where(v => v.Item == "Grau de saturação").All(v => v.Resultado == VerificacaoRotatoria.Aprovado), "graus de saturação abaixo de 0,90");

        var sinal = GeradorSinalizacao.Intersecao("Interseção 1", cruz, ps, semaforo: true);
        Ok(sinal.Marcas.Count(m => m.Codigo == "LRE") == 4 && sinal.Placas.Count == 0 && sinal.Legendas.Count == 0 && sinal.Marcas.Count(m => m.Codigo == "FTP-1") == 4 * 6 + 0
           || sinal.Marcas.Count(m => m.Codigo == "LRE") == 4 && sinal.Placas.Count == 0 && sinal.Legendas.Count == 0,
           $"com semáforo: retenção nas 4 aproximações, sem PARE e sem R-1 ({sinal.Marcas.Count(m => m.Codigo == "FTP-1")} listras de faixa)");
        var lreN = sinal.Marcas.First(m => m.Codigo == "LRE" && m.Onde.Contains("N"));
        Ok(Q(lreN.Contorno.Min(c => c.Y), plano.Aproximacoes[1].DRetencao, 1e-6), "a retenção desenhada é a mesma usada no plano");

        p.Lacos = true;
        var comLacos = CalculoSemaforico.Calcular("Interseção 1", cruz, new double[] { 600, 250, 550, 200 }, p, ps);
        Ok(comLacos.Lacos.Count == 4 && comLacos.Lacos.All(l => Q(l.Area, 1.5 * (l.Onde.Contains("A") ? 2.5 : 2.0), 1e-6)), $"laços: um por faixa, 1,5 m ({comLacos.Lacos.Count})");
        p.Lacos = false;

        var saturado = CalculoSemaforico.Calcular("Interseção 1", cruz, new double[] { 1500, 1200, 1500, 1200 }, p, ps);
        Ok(double.IsInfinity(saturado.CicloOtimo) && saturado.Ciclo >= 120 - 1e-9 && saturado.Verificacoes.First(v => v.Item == "Ciclo").Resultado == VerificacaoRotatoria.Reprovado,
           $"demanda acima da capacidade (Σy {saturado.Y:0.00}): ciclo {saturado.Ciclo:0} s e REPROVADO");

        // esquina NE (principal 3,5 e secundária 3,0 m, raio 12): o ponto no asfalto alargado vai para a calçada, 0,80 m atrás do meio-fio
        var comEsquina = new ModeloIntersecaoSinal { Esquinas = { new[] { 15.0, 15.5, 12.0 } } };
        var nc = comEsquina.NaCalcada((4.0, 4.3), 0.8);
        double dc = Math.Sqrt((nc.X - 15) * (nc.X - 15) + (nc.Y - 15.5) * (nc.Y - 15.5));
        var longe = comEsquina.NaCalcada((60, 4.3), 0.8);
        Ok(Q(dc, 11.2, 1e-9) && longe == (60.0, 4.3), $"coluna levada para a calçada da esquina ({nc.X:0.00}; {nc.Y:0.00}); longe da esquina fica onde está");

        var rel = new RelatorioSemaforo { Titulo = "Semáforo - teste", Plano = plano };
        rel.Parametros.AddRange(RelatorioSemaforo.ParametrosDe(p));
        rel.Pendencias.AddRange(RelatorioSemaforo.PendenciasPadrao());
        string arq = Path.Combine(saida, "semaforo.xlsx");
        rel.Salvar(arq);
        Ok(File.Exists(arq) && new FileInfo(arq).Length > 3000, "planilha do plano semafórico gravada");
    }
}
