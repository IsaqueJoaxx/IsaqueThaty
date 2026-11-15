using System.Text;
using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes de interferências e desapropriação (PRJINTERFERENCIAS).</summary>
internal static class TestesInterferencias
{
    private static List<(double X, double Y)> Ret(double x0, double y0, double x1, double y1) => new() { (x0, y0), (x1, y0), (x1, y1), (x0, y1) };

    public static void Rodar(Action<bool, string> Ok, string saida)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;

        Ok(Interferencias.Classificar("POSTES") == "Poste" && Interferencias.Classificar("ARV") == "Árvore" && Interferencias.Classificar("REDE AGUA") == "Rede de água" &&
           Interferencias.Classificar("AGUAS PLUVIAIS") == "Drenagem" && Interferencias.Classificar("ESGOTO-EX") == "Rede de esgoto" && Interferencias.Classificar("CERCA") == "Muro / cerca" &&
           Interferencias.Classificar("Edificação") == "Edificação" && Interferencias.Classificar("C-TOPO") == "", "classificação pela camada, bloco e descrição");

        var limite = Interferencias.Limite(new[] { Ret(0, 0, 100, 50) }, 0);
        Ok(limite.Count == 1 && Q(Math.Abs(Interferencias.AreaComSinal(limite[0])), 5000, 0.01), "limite sem afastamento: 5000 m²");
        var lim5 = Interferencias.Limite(new[] { Ret(0, 0, 100, 50) }, 5);
        double a5 = Math.Abs(lim5.Sum(x => Interferencias.AreaComSinal(x)));
        Ok(Math.Abs(a5 - (5000 + 1500 + Math.PI * 25)) < 1, $"afastamento de 5 m com cantos arredondados: {a5:0.0} m²");

        var furo = new LoteDesapropriacao { Aneis = new() { Ret(0, 0, 20, 20), Enumerable.Reverse(Ret(5, 5, 15, 15)).ToList() } };
        Ok(Q(Interferencias.AreaLiquida(furo.Aneis.Cast<IReadOnlyList<(double X, double Y)>>().ToList()), 300, 0.01), "lote com furo: 400 − 100 = 300 m²");

        var lotes = new List<LoteDesapropriacao>
        {
            new() { Id = "A", Aneis = new() { Ret(-20, 20, 20, 40) } },
            new() { Id = "B", Aneis = new() { Ret(40, 10, 60, 20) } },
            new() { Id = "C", Aneis = new() { Ret(110, 0, 130, 20) } },
            new() { Id = "D", Aneis = new() { Ret(90, 0, 110, 12) } },
        };
        var objetos = new List<ObjetoExistente>
        {
            new() { Tipo = "Poste", Nome = "poste 1", Pontos = new() { (10, 10) } },
            new() { Tipo = "Árvore", Nome = "árvore perto", Pontos = new() { (101, 25) } },
            new() { Tipo = "Árvore", Nome = "árvore longe", Pontos = new() { (110, 25) } },
            new() { Tipo = "Rede de água", Nome = "adutora", Linear = true, Pontos = new() { (-20, 25), (120, 25) } },
            new() { Tipo = "Muro / cerca", Nome = "cerca", Linear = true, Pontos = new() { (0, 55), (100, 55) } },
        };
        var p = new ParametrosInterferencias();
        var a = Interferencias.Analisar(limite, lotes, objetos, p);
        var r = a.Lotes.ToDictionary(x => x.Lote.Id);
        Ok(Q(r["A"].AreaAtingida, 400, 0.01) && r["A"].Situacao == "parcial" && Q(r["A"].Percentual, 0.5, 1e-6), $"lote A: {r["A"].AreaAtingida:0.00} m² de {r["A"].Area:0.00} ({r["A"].Situacao})");
        Ok(Q(r["B"].AreaAtingida, 200, 0.01) && r["B"].Situacao == "total" && r["C"].Situacao == "não atingido" && r["C"].AreaAtingida == 0, "lote B atingido por inteiro e C fora");
        Ok(Q(r["D"].AreaAtingida, 120, 0.01) && r["D"].Obs.Contains("lote mínimo"), $"lote D: remanescente de {r["D"].Remanescente:0.00} m² abaixo do lote mínimo");
        var i = a.Interferencias.ToDictionary(x => x.Objeto.Nome);
        Ok(i["poste 1"].Situacao == "dentro" && i["árvore perto"].Situacao == "próximo" && Q(i["árvore perto"].Distancia, 1, 1e-6) && !i.ContainsKey("árvore longe"),
           "poste dentro, árvore a 1 m do limite (próxima) e a outra fora da faixa de atenção");
        Ok(Q(i["adutora"].ComprimentoDentro, 100, 0.01) && i["adutora"].Situacao == "dentro" && !i.ContainsKey("cerca"), $"adutora: {i["adutora"].ComprimentoDentro:0.00} m dentro do limite; cerca a 5 m fica fora");
        var a6 = Interferencias.Analisar(limite, Array.Empty<LoteDesapropriacao>(), objetos, new ParametrosInterferencias { FaixaAtencao = 6 });
        Ok(a6.Interferencias.Any(x => x.Objeto.Nome == "cerca" && x.Situacao == "próximo" && Q(x.Distancia, 5, 1e-6)), "com faixa de atenção de 6 m a cerca entra como próxima");

        // shapefile: grava dois lotes e lê de volta
        string shp = Path.Combine(saida, "lotes_teste.shp");
        EscreverShapefile(shp, new() { Ret(-20, 20, 20, 40), Ret(40, 10, 60, 20) }, new() { ("L-01", "JOSÉ DA SILVA"), ("L-02", "MARIA") });
        var fe = Interferencias.LerShapefile(shp);
        var (id0, prop0) = Interferencias.IdDosAtributos(fe[0].Atributos, 1);
        Ok(fe.Count == 2 && fe[0].Tipo == 5 && fe[0].Partes[0].Count == 4 && id0 == "L-01" && prop0 == "JOSÉ DA SILVA" && !Interferencias.PareceGeografico(new[] { (600000.0, 7800000.0) }),
           $"shapefile lido: {fe.Count} polígonos, lote '{id0}', proprietário '{prop0}'");
        Ok(Interferencias.PareceGeografico(new[] { (-44.0, -19.9) }), "coordenadas geográficas reconhecidas");

        var rel = new RelatorioInterferencias { Analise = a, Parametros = p, Limite = "teste", FonteLotes = "teste", Desenho = "teste.dwg" };
        string arq = Path.Combine(saida, "interferencias.xlsx");
        rel.Salvar(arq);
        var qd = RelatorioInterferencias.QuadroDesapropriacao(a);
        Ok(new FileInfo(arq).Length > 3000 && qd.Linhas.Count == 4 && qd.Linhas[^1][0] == "TOTAL", "planilha e quadro de desapropriação (3 lotes + total)");
    }

    /// <summary>Shapefile de polígonos (tipo 5) com .dbf (LOTE, PROPRIET) — só para o teste.</summary>
    private static void EscreverShapefile(string shp, List<List<(double X, double Y)>> polis, List<(string Lote, string Prop)> atr)
    {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            void BE(int v) { w.Write(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }); }
            BE(9994); for (int k = 0; k < 5; k++) BE(0); BE(0); w.Write(1000); w.Write(5);
            for (int k = 0; k < 8; k++) w.Write(0.0);
            int rec = 1;
            foreach (var pl in polis)
            {
                var pts = pl.Append(pl[0]).ToList();
                int cont = 4 + 32 + 4 + 4 + 4 + 16 * pts.Count;
                BE(rec++); BE(cont / 2);
                w.Write(5);
                w.Write(pts.Min(q => q.X)); w.Write(pts.Min(q => q.Y)); w.Write(pts.Max(q => q.X)); w.Write(pts.Max(q => q.Y));
                w.Write(1); w.Write(pts.Count); w.Write(0);
                foreach (var q in pts) { w.Write(q.X); w.Write(q.Y); }
            }
            w.Flush();
            var b = ms.ToArray();
            int words = b.Length / 2;
            b[24] = (byte)(words >> 24); b[25] = (byte)(words >> 16); b[26] = (byte)(words >> 8); b[27] = (byte)words;
            File.WriteAllBytes(shp, b);
        }
        var campos = new[] { ("LOTE", 10), ("PROPRIET", 20) };
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            short cab = (short)(32 + 32 * campos.Length + 1), tam = (short)(1 + campos.Sum(c => c.Item2));
            w.Write((byte)3); w.Write((byte)126); w.Write((byte)1); w.Write((byte)1);
            w.Write(atr.Count); w.Write(cab); w.Write(tam); w.Write(new byte[20]);
            foreach (var (n, t) in campos)
            {
                var nb = new byte[11]; Encoding.ASCII.GetBytes(n).CopyTo(nb, 0); w.Write(nb);
                w.Write((byte)'C'); w.Write(new byte[4]); w.Write((byte)t); w.Write((byte)0); w.Write(new byte[14]);
            }
            w.Write((byte)0x0D);
            foreach (var (l, pr) in atr)
            {
                w.Write((byte)' ');
                w.Write(Encoding.Latin1.GetBytes(l.PadRight(10)[..10]));
                w.Write(Encoding.Latin1.GetBytes(pr.PadRight(20)[..20]));
            }
            w.Write((byte)0x1A);
            File.WriteAllBytes(Path.ChangeExtension(shp, ".dbf"), ms.ToArray());
        }
    }
}
