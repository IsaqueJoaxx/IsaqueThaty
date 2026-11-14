using System.Globalization;
using System.Text;
using Projeta.Sinalizacao.Core.Comum;

namespace Projeta.Sinalizacao.Core.Desenho;

/// <summary>Cores da sinalização (CONTRAN / NBR) com equivalentes RGB e ACI.</summary>
public enum Cor { Branco, Preto, Vermelho, Amarelo, Verde, Azul, Laranja, Marrom, Cinza, VermelhoCiclo }

public static class Cores
{
    public static (byte R, byte G, byte B) Rgb(Cor c) => c switch
    {
        Cor.Branco => (255, 255, 255),
        Cor.Preto => (0, 0, 0),
        Cor.Vermelho => (204, 0, 0),
        Cor.Amarelo => (255, 204, 0),
        Cor.Verde => (0, 120, 60),
        Cor.Azul => (0, 70, 160),
        Cor.Laranja => (255, 120, 0),
        Cor.Marrom => (110, 60, 30),
        Cor.Cinza => (128, 128, 128),
        Cor.VermelhoCiclo => (190, 40, 40),
        _ => (128, 128, 128),
    };

    /// <summary>Cor de índice do AutoCAD (usada nas marcas do pavimento e layers).</summary>
    public static short Aci(Cor c) => c switch
    {
        Cor.Branco => 7,
        Cor.Preto => 250,
        Cor.Vermelho => 1,
        Cor.Amarelo => 2,
        Cor.Verde => 3,
        Cor.Azul => 5,
        Cor.Laranja => 30,
        Cor.Marrom => 34,
        Cor.Cinza => 8,
        Cor.VermelhoCiclo => 14,
        _ => 7,
    };

    public static string Hex(Cor c) { var (r, g, b) = Rgb(c); return $"#{r:X2}{g:X2}{b:X2}"; }

    public static string Nome(Cor c) => c switch
    {
        Cor.Branco => "branca", Cor.Preto => "preta", Cor.Vermelho => "vermelha", Cor.Amarelo => "amarela",
        Cor.Verde => "verde", Cor.Azul => "azul", Cor.Laranja => "laranja", Cor.Marrom => "marrom",
        Cor.Cinza => "cinza", Cor.VermelhoCiclo => "vermelha (ciclofaixa)", _ => c.ToString(),
    };
}

/// <summary>Primitiva de desenho vetorial.</summary>
public abstract record Prim(Cor Cor);

/// <summary>Área preenchida (polígono fechado).</summary>
public sealed record PArea(List<V2> Pts, Cor Cor) : Prim(Cor);

/// <summary>Círculo preenchido.</summary>
public sealed record PDisco(V2 C, double R, Cor Cor) : Prim(Cor);

/// <summary>Linha fina (contorno).</summary>
public sealed record PLinha(List<V2> Pts, bool Fechada, Cor Cor) : Prim(Cor);

/// <summary>
/// Desenho vetorial independente do AutoCAD. As primitivas são pintadas na ordem
/// (a última fica por cima). É convertido em entidades do DWG e também em SVG (prévia/testes).
/// </summary>
public sealed class Desenho2D
{
    public List<Prim> Itens { get; } = new();

    public Desenho2D Add(Prim p) { Itens.Add(p); return this; }
    public Desenho2D Area(IEnumerable<V2> pts, Cor cor) { var l = pts.ToList(); if (l.Count >= 3) Itens.Add(new PArea(l, cor)); return this; }
    public Desenho2D Disco(V2 c, double r, Cor cor) { if (r > 0) Itens.Add(new PDisco(c, r, cor)); return this; }
    public Desenho2D Linha(IEnumerable<V2> pts, Cor cor, bool fechada = false) { var l = pts.ToList(); if (l.Count >= 2) Itens.Add(new PLinha(l, fechada, cor)); return this; }
    public Desenho2D Juntar(Desenho2D outro) { Itens.AddRange(outro.Itens); return this; }

    public Caixa Limites()
    {
        var c = Caixa.Vazia;
        foreach (var p in Itens)
        {
            switch (p)
            {
                case PArea a: foreach (var v in a.Pts) c = c.Incluir(v); break;
                case PLinha l: foreach (var v in l.Pts) c = c.Incluir(v); break;
                case PDisco d: c = c.Incluir(d.C - new V2(d.R, d.R)).Incluir(d.C + new V2(d.R, d.R)); break;
            }
        }
        return c;
    }

    /// <summary>Aplica transformação afim (escala uniforme, espelho em X, rotação e translação, nessa ordem).</summary>
    public Desenho2D Transformado(double escala = 1, double rotacao = 0, V2? translacao = null, bool espelharX = false)
    {
        var t = translacao ?? V2.Zero;
        V2 F(V2 p)
        {
            var q = new V2(espelharX ? -p.X : p.X, p.Y) * escala;
            return q.Girar(rotacao) + t;
        }
        var r = new Desenho2D();
        foreach (var p in Itens)
        {
            switch (p)
            {
                case PArea a:
                    var pts = a.Pts.Select(F).ToList();
                    if (espelharX) pts.Reverse();
                    r.Itens.Add(new PArea(pts, a.Cor));
                    break;
                case PLinha l: r.Itens.Add(new PLinha(l.Pts.Select(F).ToList(), l.Fechada, l.Cor)); break;
                case PDisco d: r.Itens.Add(new PDisco(F(d.C), d.R * Math.Abs(escala), d.Cor)); break;
            }
        }
        return r;
    }

    /// <summary>Troca uma cor por outra (ex.: advertência amarela → laranja nas obras).</summary>
    public Desenho2D TrocarCor(Cor de, Cor para)
    {
        var r = new Desenho2D();
        foreach (var p in Itens) r.Itens.Add(p.Cor == de ? p with { Cor = para } : p);
        return r;
    }

    /// <summary>Soma das áreas preenchidas de uma cor (aproximada: sobreposições contam em dobro).</summary>
    public double AreaPintada(Cor? cor = null)
    {
        double s = 0;
        foreach (var p in Itens)
        {
            if (cor.HasValue && p.Cor != cor.Value) continue;
            if (p is PArea a) s += Poligonos.Area(a.Pts);
            else if (p is PDisco d) s += Math.PI * d.R * d.R;
        }
        return s;
    }

    /// <summary>SVG para prévia (y para cima, como no CAD).</summary>
    public string Svg(double larguraPx = 200, double margem = 0.02, string? fundo = null, Caixa? caixa = null)
    {
        var c = (caixa ?? Limites()).Expandir(Math.Max(margem, 1e-6));
        if (c.EhVazia || c.Largura <= 0) c = new Caixa(-1, -1, 1, 1);
        double k = larguraPx / c.Largura;
        double h = c.Altura * k;
        var inv = CultureInfo.InvariantCulture;
        string X(double x) => ((x - c.XMin) * k).ToString("0.##", inv);
        string Y(double y) => ((c.YMax - y) * k).ToString("0.##", inv);
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{larguraPx.ToString("0", inv)}\" height=\"{h.ToString("0", inv)}\" viewBox=\"0 0 {larguraPx.ToString("0.##", inv)} {h.ToString("0.##", inv)}\">");
        if (fundo is not null) sb.Append($"<rect width=\"100%\" height=\"100%\" fill=\"{fundo}\"/>");
        SvgItens(sb, X, Y, k);
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Escreve as primitivas como elementos SVG (para compor folhas de catálogo).</summary>
    public void SvgItens(StringBuilder sb, Func<double, string> X, Func<double, string> Y, double k)
    {
        var inv = CultureInfo.InvariantCulture;
        foreach (var p in Itens)
        {
            string cor = Cores.Hex(p.Cor);
            switch (p)
            {
                case PArea a:
                    sb.Append("<path d=\"M");
                    sb.Append(string.Join(" L", a.Pts.Select(v => X(v.X) + " " + Y(v.Y))));
                    sb.Append($" Z\" fill=\"{cor}\" stroke=\"{cor}\" stroke-width=\"0.3\" stroke-linejoin=\"round\"/>");
                    break;
                case PDisco d:
                    sb.Append($"<circle cx=\"{X(d.C.X)}\" cy=\"{Y(d.C.Y)}\" r=\"{(d.R * k).ToString("0.##", inv)}\" fill=\"{cor}\"/>");
                    break;
                case PLinha l:
                    sb.Append("<path d=\"M");
                    sb.Append(string.Join(" L", l.Pts.Select(v => X(v.X) + " " + Y(v.Y))));
                    sb.Append(l.Fechada ? " Z" : "");
                    sb.Append($"\" fill=\"none\" stroke=\"{cor}\" stroke-width=\"1\"/>");
                    break;
            }
        }
    }
}
