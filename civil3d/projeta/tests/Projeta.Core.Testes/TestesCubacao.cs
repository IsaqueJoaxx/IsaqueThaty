using Projeta.Core.Terraplenagem;

namespace Projeta.Core.Testes;

/// <summary>Testes da cubação por seções (Cubacao.cs) com resultados exatos.</summary>
internal static class TestesCubacao
{
    public static void Rodar(Action<bool, string> Ok, string? saida = null)
    {
        static bool Q(double a, double b, double tol = 1e-9) => Math.Abs(a - b) <= tol;

        // linha recortada na faixa válida, com interpolação nas pontas
        var ln = new LinhaSecao(new[] { (10.0, 5.0), (-10.0, 3.0), (0.0, 4.0), (double.NaN, 1.0) }, -5, 5);
        Ok(ln.P.Count == 3 && Q(ln.Inicio, -5) && Q(ln.Fim, 5) && Q(ln.Cota(-5), 3.5) && Q(ln.Cota(5), 4.5) && double.IsNaN(ln.Cota(6)), "linha de seção recortada e interpolada");
        try { new LinhaSecao(new[] { (0.0, 1.0), (1.0, 1.0) }, 2, 3); Ok(false, "aceitou faixa vazia"); } catch (ArgumentException) { Ok(true, ""); }

        // plataforma 2 m acima do terreno plano, 10 m de largura: aterro 20 m², faixa comum só a da plataforma
        var tn = new LinhaSecao(new[] { (-10.0, 10.0), (10.0, 10.0) });
        var pl = new LinhaSecao(new[] { (-5.0, 12.0), (5.0, 12.0) });
        var a1 = Secoes.Areas(tn, pl);
        Ok(Q(a1.Aterro, 20) && Q(a1.Corte, 0) && Q(a1.Inicio, -5) && Q(a1.Fim, 5) && Q(a1.Largura, 10), $"aterro de plataforma ({a1.Aterro})");

        // terreno inclinado z = x cruzando o projeto em nível z = 0 entre -4 e 4: corte 8 e aterro 8
        var a2 = Secoes.Areas(new LinhaSecao(new[] { (-6.0, -6.0), (6.0, 6.0) }), new LinhaSecao(new[] { (-4.0, 0.0), (4.0, 0.0) }));
        Ok(Q(a2.Corte, 8) && Q(a2.Aterro, 8), $"cruzamento dentro do intervalo ({a2.Corte}; {a2.Aterro})");

        // seção mista típica: terreno em V, projeto com taludes (pontos que não coincidem): conferido por trapézios finos
        var tv = new LinhaSecao(new[] { (-20.0, 106.0), (0.0, 100.0), (20.0, 104.0) });
        var pj = new LinhaSecao(new[] { (-12.0, 106.0), (-6.0, 103.0), (6.0, 102.8), (11.0, 103.5) });
        var a3 = Secoes.Areas(tv, pj);
        double cN = 0, aN = 0, dx = 1e-4;
        for (double x = -12 + dx / 2; x < 11; x += dx) { double d = tv.Cota(x) - pj.Cota(x); if (d > 0) cN += d * dx; else aN -= d * dx; }
        Ok(Q(a3.Corte, cN, 1e-4) && Q(a3.Aterro, aN, 1e-4), $"seção mista exata ({a3.Corte:F4} x {cN:F4}; {a3.Aterro:F4} x {aN:F4})");
        // projeto com buraco no meio (vão / região escondida): só conta onde as duas linhas existem
        var furado = LinhaSecao.DeTrechos(new[] { (6.0, 1.0, 2.0, 1.0), (-6.0, 1.0, -2.0, 1.0) });
        var a4 = Secoes.Areas(new LinhaSecao(new[] { (-10.0, 0.0), (10.0, 0.0) }), furado);
        Ok(Q(a4.Aterro, 8) && Q(a4.Corte, 0) && Q(a4.Inicio, -6) && Q(a4.Fim, 6) && double.IsNaN(furado.Cota(0)) && Q(furado.Cota(4), 1), $"buraco no projeto fica de fora ({a4.Aterro})");
        var tFurado = LinhaSecao.DeTrechos(new[] { (-10.0, 3.0, -1.0, 3.0), (1.0, 3.0, 10.0, 3.0) });
        var a5 = Secoes.Areas(tFurado, new LinhaSecao(new[] { (-4.0, 1.0), (4.0, 1.0) }));
        Ok(Q(a5.Corte, 12), $"buraco no terreno fica de fora ({a5.Corte})");
        var sem = Secoes.Areas(tv, new LinhaSecao(new[] { (30.0, 1.0), (40.0, 1.0) }));
        Ok(sem.Corte == 0 && sem.Aterro == 0 && double.IsNaN(sem.Inicio), "sem faixa comum = área zero");

        // volumes por áreas médias, compensação lateral e Brückner (Fh 1,25), estacas fora de ordem
        var c = Cubacao.PorAreas(new[]
        {
            ("E2", 40.0, 0.0, 8.0, ""), ("E0", 0.0, 10.0, 0.0, ""), ("E1", 20.0, 20.0, 0.0, ""),
        }, 1, 1.25);
        var L = c.Linhas;
        Ok(L[0].Nome == "E0" && Q(L[1].VolumeCorte, 300) && Q(L[2].VolumeCorte, 200) && Q(L[2].VolumeAterro, 80) && Q(L[2].AterroFator, 100), "volumes por áreas médias");
        Ok(Q(L[1].Compensacao, 0) && Q(L[2].Compensacao, 100) && Q(L[2].CorteExcedente, 100) && Q(L[2].AterroReceber, 0), "compensação lateral");
        Ok(Q(L[1].Bruckner, 300) && Q(L[2].Bruckner, 400) && Q(c.Saldo, 400) && c.Situacao == "Sobra (bota-fora)", $"Brückner ({L[2].Bruckner})");
        Ok(Q(c.TotalCorte, 500) && Q(c.TotalAterroFator, 100) && Q(c.TotalCompensacao, 100), "totais");
        var c2 = Cubacao.PorAreas(new[] { ("A", 0.0, 1.0, 0.0, ""), ("B", 20.0, 0.0, 1.0, "") }, 1.3, 1);
        Ok(Q(c2.Linhas[1].CorteFator, 13) && Q(c2.Linhas[1].AterroFator, 10) && Q(c2.Saldo, 3), "fator do corte");
        try { Cubacao.PorAreas(new[] { ("A", 0.0, 1.0, 0.0, "") }, 1, 1); Ok(false, "aceitou uma seção só"); } catch (ArgumentException) { Ok(true, ""); }

        // conferência e planilha
        c.Alinhamento = "Eixo 1"; c.Grupo = "Seções"; c.Origem = "superfícies TN × projeto";
        c.Conferencia.Add((20, 20.05, 0)); c.Conferencia.Add((40, 0, 8)); c.ConferenciaNome = "Lista 1";
        var (dif, est) = c.MaiorDiferenca();
        Ok(Q(dif, 0.05, 1e-9) && Q(est, 20), "maior diferença da conferência");
        var pasta = c.Planilha();
        if (saida is not null) pasta.Salvar(Path.Combine(saida, "cubacao_teste.xlsx"));
        var f = pasta.Folhas[0];
        Ok(f.Nome == "Cubação" && (string?)f.Valor(6, 1) == "0+0,00" && (string?)f.Valor(8, 1) == "2+0,00", "aba Cubação e estacas");
        Ok(pasta.Folhas.Count == 3 && pasta.Folhas[1].Nome == "Conferência Civil" && pasta.Folhas[2].Nome == "Origem", "abas de conferência e origem");
        Ok(Equals(f.Valor(8, 11), 400.0) || Equals(f.Valor(8, 11), 400d), $"Brückner na planilha ({f.Valor(8, 11)})");
    }
}
