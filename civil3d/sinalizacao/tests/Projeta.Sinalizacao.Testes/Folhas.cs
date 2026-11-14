using System.Globalization;
using System.Text;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Vertical;
using Nm = Projeta.Sinalizacao.Core.Normas;

namespace Projeta.Sinalizacao.Testes;

/// <summary>Folhas SVG de conferência visual (catálogo de placas, marcas).</summary>
public static class Folhas
{
    public static string Catalogo(IEnumerable<TipoPlaca> tipos, int colunas = 8, double celula = 150)
    {
        var n = new Nm.Normas();
        var lista = tipos.ToList();
        int linhas = (lista.Count + colunas - 1) / colunas;
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        double W = colunas * celula, H = linhas * (celula + 26);
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{W}\" height=\"{H}\"><rect width=\"100%\" height=\"100%\" fill=\"#d8dde3\"/>");
        for (int i = 0; i < lista.Count; i++)
        {
            var t = lista[i];
            double dim = GeradorPlaca.DimensaoPadrao(t, n, Nm.TipoVia.Urbana);
            var d = GeradorPlaca.Gerar(t, dim, null, n);
            var cx = d.Limites();
            double tam = Math.Max(cx.Largura, cx.Altura);
            double k = (celula - 24) / tam;
            double ox = (i % colunas) * celula + celula / 2, oy = (i / colunas) * (celula + 26) + celula / 2;
            string X(double x) => (ox + (x - cx.Centro.X) * k).ToString("0.##", inv);
            string Y(double y) => (oy - (y - cx.Centro.Y) * k).ToString("0.##", inv);
            d.SvgItens(sb, X, Y, k);
            sb.Append($"<text x=\"{ox.ToString(inv)}\" y=\"{(oy + celula / 2 + 8).ToString(inv)}\" font-family=\"Arial\" font-size=\"13\" text-anchor=\"middle\">{t.Codigo}</text>");
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string Desenho(Desenho2D d, double largura = 1200, string fundo = "#3a3f44")
        => d.Svg(largura, 0.5, fundo);
}
