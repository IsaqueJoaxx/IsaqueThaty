using Projeta.Core.Alinhamentos;
using Projeta.Giro.Core.Veiculos;

namespace Projeta.Core.Testes;

/// <summary>Testes do auditor de segurança da rotatória (PRJSEGURANCA): caminho do veículo, giro, folgas e visibilidade 3D.</summary>
internal static class TestesSeguranca
{
    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;

        double dvp = AuditorRotatoria.DistanciaParada(60, 2.5, 3.4);
        Ok(Q(dvp, 0.278 * 60 * 2.5 + 0.039 * 3600 / 3.4, 1e-9) && dvp > 80 && dvp < 86, $"distância de visibilidade de parada a 60 km/h: {dvp:0.0} m");

        // rotatória de 4 braços, DCI 40 m, pista anelar de 6 m
        ModeloRotatoria Modelo()
        {
            var m = new ModeloRotatoria { RExt = 20, RIlha = 14 };
            foreach (double ang in new[] { 0.0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 })
            {
                var b = new BracoRotatoria { Nome = "R" + (int)Math.Round(ang * 180 / Math.PI), Rumo = GeometriaRotatoria.Rumo(ang), Angulo = ang,
                    Eixo = new PolilinhaEixo(new[] { (17 * Math.Cos(ang), 17 * Math.Sin(ang)), (217 * Math.Cos(ang), 217 * Math.Sin(ang)) }), SRetencao = 3, BordoEntrada = 5, BordoSaida = 5 };
                // meios-fios das conexões com raio de 15 m, tangentes ao bordo externo do anel e ao bordo do ramo
                foreach (int lado in new[] { +1, -1 })
                {
                    var cc = GeometriaRotatoria.ChuteCentroConexao(17 * Math.Cos(ang), 17 * Math.Sin(ang), Math.Cos(ang), Math.Sin(ang), lado, 5 + 15, 0, 0, 20 + 15);
                    double st = b.Eixo.Projetar(cc.X, cc.Y).S;
                    if (lado > 0) { b.MeioFioEntrada = (cc.X, cc.Y, 15); b.STangEntrada = st; } else { b.MeioFioSaida = (cc.X, cc.Y, 15); b.STangSaida = st; }
                }
                m.Bracos.Add(b);
            }
            return m;
        }
        var m = Modelo();
        var bib = Biblioteca.Carregar(null);
        var vp = bib.Obter("VP")!;
        var sr = bib.Obter("SR")!;
        Ok(vp is not null && sr is not null && sr.Articulado, "veículos do DNIT na biblioteca do PROJETA Giro (VP e SR)");

        // caminho do eixo dianteiro: todos os movimentos de uma entrada
        double rAnel = 20 - 1.0 - sr.BitolaDianteira / 2;
        bool todos = true;
        string falha = "";
        for (int j = 1; j <= 4; j++)
        {
            int s = j % 4;
            var c = AuditorRotatoria.CaminhoGuia(m, 0, s, rAnel, 15 + 20 - rAnel, 15 + 20 - rAnel, 50, out var erro);
            if (c is null) { todos = false; falha += $" j{j}: {erro}"; continue; }
            double passoMax = 0; for (int i = 1; i < c.Count; i++) passoMax = Math.Max(passoMax, Math.Sqrt(Math.Pow(c[i].X - c[i - 1].X, 2) + Math.Pow(c[i].Y - c[i - 1].Y, 2)));
            var ini = c[0]; var fim = c[^1];
            double rIni = Math.Sqrt(ini.X * ini.X + ini.Y * ini.Y), rFim = Math.Sqrt(fim.X * fim.X + fim.Y * fim.Y);
            if (passoMax > 0.6 || rIni < 60 || rFim < 60) { todos = false; falha += $" j{j}: passo {passoMax:0.00}, início a {rIni:0} m, fim a {rFim:0} m"; }
            double rMinCam = c.Min(p => Math.Sqrt(p.X * p.X + p.Y * p.Y));
            if (j >= 2 && Math.Abs(rMinCam - rAnel) > 0.05) { todos = false; falha += $" j{j}: raio no anel {rMinCam:0.00} ≠ {rAnel:0.00}"; }
        }
        Ok(todos, "caminho do veículo em todos os movimentos da entrada L: meio da faixa, curva de entrada, anel e curva de saída" + falha);
        var cDir = AuditorRotatoria.CaminhoGuia(m, 0, 1, rAnel, 14, 14, 50, out _)!;
        // a primeira parte vem do leste pela faixa de entrada (y > 0 no braço L) e a saída vai para o norte pela faixa de saída (x < 0 no braço N)
        Ok(cDir[0].Y > 1 && cDir[^1].X > 1, $"faixa de entrada à esquerda do eixo do braço e de saída à direita (início {cDir[0].X:0.0},{cDir[0].Y:0.0}; fim {cDir[^1].X:0.0},{cDir[^1].Y:0.0})");

        // giro do SR sem faixa transponível: a carreta invade a ilha central (14 m)
        var p = new ParametrosSeguranca();
        var k = AuditorRotatoria.MeiosFios(m, 0, p.Trecho);
        var g = AuditorRotatoria.Simular(m, k, sr, 0, 3, p);
        Ok(g.Erro is null && g.Envoltoria.Count > 0 && g.AreaIlhaCentral > 1 && g.Resultado == VerificacaoRotatoria.Reprovado && g.RaioMinimoRodas < 14,
           $"SR à esquerda sem faixa transponível: rodas a {g.RaioMinimoRodas:0.0} m do centro, {g.AreaIlhaCentral:0.0} m² sobre a ilha central → {g.Resultado} (largura varrida {g.LarguraVarrida:0.00} m)");
        var gv = AuditorRotatoria.Simular(m, k, vp, 0, 2, p);
        Ok(gv.Erro is null && gv.AreaIlhaCentral < 0.001 && gv.FolgaIlha >= 0.6 && gv.RaioMinimoRodas > 14.6 && gv.FolgaExterna >= 0.6 && gv.Resultado == VerificacaoRotatoria.Aprovado,
           $"VP em frente: rodas a {gv.FolgaIlha:0.00} m da ilha e {gv.FolgaExterna:0.00} m do meio-fio externo → {gv.Resultado}");

        // com faixa transponível de 5 m (ilha central com 9 m): o SR passa e usa a faixa
        var ms5 = ModeloRotatoriaSinal.De(m, 6, 5);
        var k5 = AuditorRotatoria.MeiosFios(ms5.Modelo(), 5, p.Trecho);
        var g5 = AuditorRotatoria.Simular(ms5.Modelo(), k5, sr, 0, 3, p);
        Ok(g5.Erro is null && g5.AreaIlhaCentral < 0.001 && g5.AreaApron > 1 && g5.FolgaIlha >= 0.6,
           $"SR à esquerda com faixa transponível de 5 m: ilha central livre (folga {g5.FolgaIlha:0.00} m), {g5.AreaApron:0.0} m² na faixa, meio-fio externo a {g5.FolgaExterna:0.00} m (invade {g5.InvasaoExterna:0.0} m, ilhas {g5.AreaIlhasDivisorias:0.0} m²) → {g5.Resultado} {string.Join("; ", g5.Avisos)}");

        // visibilidade 3D: plano, morro no meio e morro sobre a ilha (ignorado)
        double? Plano(double x, double y) => 100;
        double? Morro(double x, double y) => 100 + 2.5 * Math.Exp(-((x - 25) * (x - 25) + y * y) / 9);
        var alvos = new List<(double X, double Y)> { (40, 0), (50, 0) };
        var v1 = AuditorRotatoria.Conferir(0, "teste", 50, (0, 0), alvos, 1.08, 0.6, Plano);
        var v2 = AuditorRotatoria.Conferir(0, "teste", 50, (0, 0), alvos, 1.08, 0.6, Morro);
        var v3 = AuditorRotatoria.Conferir(0, "teste", 50, (0, 0), alvos, 1.08, 0.6, Morro, (x, y) => Math.Abs(x - 25) < 6);
        Ok(v1.Resultado == VerificacaoRotatoria.Aprovado && v1.FolgaMinima > 0.6 && v1.Cobertura > 0.99, $"visada sobre terreno plano: folga mínima {v1.FolgaMinima:0.00} m");
        Ok(v2.Resultado == VerificacaoRotatoria.Reprovado && v2.FolgaMinima < -1 && v2.AlvosTapados == 2 && v2.PontoCritico is { } pc && Math.Abs(pc.X - 25) < 1,
           $"morro de 2,5 m no meio: a visada passa {v2.FolgaMinima:0.00} m (abaixo da superfície) em x = {v2.PontoCritico?.X:0.0}");
        Ok(v3.Resultado == VerificacaoRotatoria.Aprovado && v3.SobreIlhas > 0, "o trecho sobre a ilha não conta");
        double? Fora(double x, double y) => x < 30 ? 100 : null;
        var v4 = AuditorRotatoria.Conferir(0, "teste", 50, (0, 0), alvos, 1.08, 0.6, Fora);
        Ok(v4.Resultado == VerificacaoRotatoria.Atencao, $"superfície que não cobre o alvo: {v4.Resultado} ({v4.Obs})");

        // auditoria completa (SR + VP), terreno plano
        var a = AuditorRotatoria.Auditar("Rotatória 1", ModeloRotatoriaSinal.De(m, 6, 0), p, new CriteriosRotatoria(), new[] { sr, vp }, Plano);
        int giros = a.Verificacoes.Count(x => x.Item.StartsWith("Giro")), vis = a.Verificacoes.Count(x => x.Item.StartsWith("Visibilidade em 3D"));
        Ok(a.Movimentos.Count == 32 && giros == 32 && vis == 12 && a.Trajetorias.Count == 4,
           $"auditoria: {a.Movimentos.Count} movimentos, {giros} verificações de giro, {vis} de visibilidade 3D, {a.Verificacoes.Count} no total");
        Ok(a.Recomendacoes.Any(r => r.Contains("faixa transponível") && r.Contains("SR")), "recomendação: faixa transponível para o SR — " + a.Recomendacoes.FirstOrDefault(r => r.Contains("SR")));
        Ok(a.Visadas.All(x => x.Resultado == VerificacaoRotatoria.Aprovado), $"terreno plano: todas as visadas livres ({string.Join(", ", a.Visadas.Where(x => x.Resultado != VerificacaoRotatoria.Aprovado).Select(x => x.Tipo + " " + x.Obs))})");

        var rel = new RelatorioSeguranca { Analise = a, Modelo = m, Desenho = "teste.dwg" };
        rel.Parametros.AddRange(RelatorioSeguranca.ParametrosDe(p, new CriteriosRotatoria(), "SR, VP", "plano"));
        rel.Pendencias.AddRange(RelatorioSeguranca.PendenciasPadrao());
        string arq = Path.Combine(saida, "seguranca.xlsx");
        rel.Salvar(arq);
        Ok(new FileInfo(arq).Length > 5000, "planilha da auditoria gravada");
    }
}
