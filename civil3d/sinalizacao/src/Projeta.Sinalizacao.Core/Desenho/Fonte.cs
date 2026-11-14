using System.Globalization;
using Projeta.Sinalizacao.Core.Comum;

namespace Projeta.Sinalizacao.Core.Desenho;

public enum Alinhamento { Esquerda, Centro, Direita }

/// <summary>
/// Fonte de traços própria (maiúsculas, algarismos e acentos do português), desenhada numa grade
/// de 6 unidades de altura. Gera polígonos preenchidos — o mesmo resultado no DWG, na prévia e no
/// cálculo de área das legendas, sem depender de fontes instaladas.
/// </summary>
public static class Fonte
{
    private sealed record Glifo(double W, List<List<V2>> Tracos);

    private static readonly Dictionary<char, Glifo> G = new();
    private const double H = 6.0;       // altura de maiúscula na grade
    private const double Gap = 1.35;    // espaço entre letras (grade)

    // traços: "x,y x,y | x,y ..." ; um único ponto = pingo
    private static void Def(char c, double w, string tracos)
    {
        var lista = new List<List<V2>>();
        foreach (var t in tracos.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var pts = new List<V2>();
            foreach (var p in t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var xy = p.Split(',');
                pts.Add(new V2(double.Parse(xy[0], CultureInfo.InvariantCulture), double.Parse(xy[1], CultureInfo.InvariantCulture)));
            }
            if (pts.Count > 0) lista.Add(pts);
        }
        G[c] = new Glifo(w, lista);
    }

    static Fonte()
    {
        Def('A', 4, "0,0 2,6 4,0 | 0.7,2.1 3.3,2.1");
        Def('B', 4, "0,0 0,6 3,6 4,5 4,4 3,3 0,3 | 3,3 4,2 4,1 3,0 0,0");
        Def('C', 4, "4,5 3,6 1,6 0,5 0,1 1,0 3,0 4,1");
        Def('D', 4, "0,0 0,6 2.6,6 4,4.6 4,1.4 2.6,0 0,0");
        Def('E', 3.6, "3.6,6 0,6 0,0 3.6,0 | 0,3 2.8,3");
        Def('F', 3.6, "3.6,6 0,6 0,0 | 0,3 2.8,3");
        Def('G', 4, "4,5 3,6 1,6 0,5 0,1 1,0 3,0 4,1 4,2.8 2.2,2.8");
        Def('H', 4, "0,0 0,6 | 4,0 4,6 | 0,3 4,3");
        Def('I', 0, "0,0 0,6");
        Def('J', 3.6, "0,1 1,0 2.6,0 3.6,1 3.6,6");
        Def('K', 4, "0,0 0,6 | 4,6 0,2 | 1.4,3.4 4,0");
        Def('L', 3.6, "0,6 0,0 3.6,0");
        Def('M', 4.6, "0,0 0,6 2.3,2.6 4.6,6 4.6,0");
        Def('N', 4, "0,0 0,6 4,0 4,6");
        Def('O', 4, "1,0 0,1 0,5 1,6 3,6 4,5 4,1 3,0 1,0");
        Def('P', 4, "0,0 0,6 3,6 4,5 4,4 3,3 0,3");
        Def('Q', 4, "1,0 0,1 0,5 1,6 3,6 4,5 4,1 3,0 1,0 | 2.6,1.6 4,0");
        Def('R', 4, "0,0 0,6 3,6 4,5 4,4 3,3 0,3 | 2.2,3 4,0");
        Def('S', 4, "4,5 3,6 1,6 0,5 0,4 1,3 3,3 4,2 4,1 3,0 1,0 0,1");
        Def('T', 4, "0,6 4,6 | 2,6 2,0");
        Def('U', 4, "0,6 0,1 1,0 3,0 4,1 4,6");
        Def('V', 4, "0,6 2,0 4,6");
        Def('W', 5.4, "0,6 1.3,0 2.7,4.2 4.1,0 5.4,6");
        Def('X', 4, "0,0 4,6 | 0,6 4,0");
        Def('Y', 4, "0,6 2,3 4,6 | 2,3 2,0");
        Def('Z', 4, "0,6 4,6 0,0 4,0");
        Def('0', 4, "1,0 0,1 0,5 1,6 3,6 4,5 4,1 3,0 1,0");
        Def('1', 2.2, "0,4.6 1.6,6 1.6,0");
        Def('2', 4, "0,5 1,6 3,6 4,5 4,4 0,0 4,0");
        Def('3', 4, "0,5 1,6 3,6 4,5 4,4 3,3 1.6,3 | 3,3 4,2 4,1 3,0 1,0 0,1");
        Def('4', 4, "3,0 3,6 0,2 4,2");
        Def('5', 4, "4,6 0,6 0,3.4 3,3.4 4,2.4 4,1 3,0 1,0 0,1");
        Def('6', 4, "4,5 3,6 1,6 0,5 0,1 1,0 3,0 4,1 4,2.4 3,3.4 0,3.4");
        Def('7', 4, "0,6 4,6 1.6,0");
        Def('8', 4, "1,3 0,4 0,5 1,6 3,6 4,5 4,4 3,3 1,3 0,2 0,1 1,0 3,0 4,1 4,2 3,3");
        Def('9', 4, "0,1 1,0 3,0 4,1 4,5 3,6 1,6 0,5 0,3.6 1,2.6 4,2.6");
        Def('-', 3, "0.3,2.8 2.7,2.8");
        Def('.', 0.4, "0.2,0.2");
        Def(',', 0.6, "0.5,0.3 0,-0.9");
        Def(':', 0.4, "0.2,0.2 | 0.2,3.6");
        Def('/', 3, "0,0 3,6");
        Def('%', 4, "0,0 4,6 | 0,4.6 0,6 1.2,6 1.2,4.6 0,4.6 | 2.8,0 2.8,1.4 4,1.4 4,0 2.8,0");
        Def('(', 1.6, "1.6,6.4 0.4,5 0.4,1 1.6,-0.4");
        Def(')', 1.6, "0,6.4 1.2,5 1.2,1 0,-0.4");
        Def('+', 4, "0,3 4,3 | 2,1 2,5");
        Def('\'', 0.4, "0.2,6 0.2,4.6");
        Def('"', 1.4, "0.2,6 0.2,4.6 | 1.2,6 1.2,4.6");
        Def('°', 1.8, "0.6,6 0,5.4 0.6,4.8 1.2,4.8 1.8,5.4 1.2,6 0.6,6");
        Def(' ', 2.4, "");
        // minúsculas usadas em unidades (km/h, t, m)
        Def('k', 3, "0,0 0,6 | 3,4 0,1.5 | 1.1,2.4 3,0");
        Def('m', 4.6, "0,0 0,4 | 0,3 1,4 1.6,4 2.3,3.3 2.3,0 | 2.3,3.3 3,4 3.6,4 4.6,3 4.6,0");
        Def('h', 3, "0,0 0,6 | 0,3 1,4 2.2,4 3,3.2 3,0");
        Def('t', 2.4, "1,6 1,0.8 1.6,0 2.4,0 | 0,4 2.4,4");
    }

    private static readonly Dictionary<char, (char Base, char Acento)> Compostos = new()
    {
        ['Á'] = ('A', '´'), ['À'] = ('A', '`'), ['Â'] = ('A', '^'), ['Ã'] = ('A', '~'),
        ['É'] = ('E', '´'), ['Ê'] = ('E', '^'), ['Í'] = ('I', '´'),
        ['Ó'] = ('O', '´'), ['Ô'] = ('O', '^'), ['Õ'] = ('O', '~'), ['Ú'] = ('U', '´'), ['Ü'] = ('U', '¨'),
        ['Ç'] = ('C', '¸'),
    };

    // Acentos montados direto com números (sem passar por texto, que dependeria da cultura do Windows).
    private static List<List<V2>> Acento(char a, double w)
    {
        double cx = w / 2;
        List<V2> L(params double[] xy) { var l = new List<V2>(); for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new V2(xy[i], xy[i + 1])); return l; }
        return a switch
        {
            '´' => new() { L(cx - 0.5, 6.9, cx + 0.6, 7.9) },
            '`' => new() { L(cx + 0.5, 6.9, cx - 0.6, 7.9) },
            '^' => new() { L(cx - 1.1, 6.9, cx, 7.8, cx + 1.1, 6.9) },
            '~' => new() { L(cx - 1.5, 7.0, cx - 0.7, 7.7, cx + 0.3, 7.1, cx + 1.2, 7.7) },
            '¨' => new() { L(cx - 0.8, 7.3), L(cx + 0.8, 7.3) },
            '¸' => new() { L(cx, 0, cx, -0.8, cx - 0.8, -1.3) },
            _ => new(),
        };
    }

    private static Glifo Obter(char c)
    {
        if (G.TryGetValue(c, out var g)) return g;
        c = char.ToUpper(c, new CultureInfo("pt-BR"));
        if (G.TryGetValue(c, out g)) return g;
        if (Compostos.TryGetValue(c, out var comp) && G.TryGetValue(comp.Base, out var b))
        {
            var tr = b.Tracos.Select(t => t.ToList()).ToList();
            tr.AddRange(Acento(comp.Acento, b.W));
            var ng = new Glifo(b.W, tr);
            G[c] = ng;
            return ng;
        }
        return G['-'];
    }

    /// <summary>Espessura padrão do traço como fração da altura.</summary>
    public const double EspessuraPadrao = 0.15;

    /// <summary>Largura do texto para altura de maiúscula h e fator de largura f.</summary>
    public static double Largura(string texto, double h, double fatorLargura = 1, double espRel = EspessuraPadrao)
    {
        if (string.IsNullOrEmpty(texto)) return 0;
        double k = h / H;
        double t = espRel * h;
        double w = 0;
        foreach (var c in texto) w += Obter(c).W * k * fatorLargura + Gap * k * fatorLargura;
        w -= Gap * k * fatorLargura;
        return w + t; // meia espessura de cada lado
    }

    /// <summary>
    /// Desenha texto como áreas. A origem é o ponto de inserção na linha de base (conforme o alinhamento).
    /// Retorna a área pintada estimada (comprimento dos traços × espessura).
    /// </summary>
    public static double Desenhar(Desenho2D d, string texto, V2 origem, double h, Cor cor,
        Alinhamento alin = Alinhamento.Esquerda, double fatorLargura = 1, double espRel = EspessuraPadrao, double rotacao = 0)
    {
        if (string.IsNullOrEmpty(texto) || h <= 0) return 0;
        double k = h / H;
        double t = espRel * h;
        double wTot = Largura(texto, h, fatorLargura, espRel);
        double x0 = alin switch { Alinhamento.Centro => -wTot / 2, Alinhamento.Direita => -wTot, _ => 0 } + t / 2;
        double area = 0;
        double x = x0;
        foreach (var c in texto)
        {
            var g = Obter(c);
            foreach (var tr in g.Tracos)
            {
                var pts = tr.Select(p => origem + new V2(x + p.X * k * fatorLargura, p.Y * k).Girar(rotacao)).ToList();
                double comp = Pincel.Tracos(d, Encolher(pts, origem, rotacao, k, t), t, cor);
                area += comp * t;
            }
            x += g.W * k * fatorLargura + Gap * k * fatorLargura;
        }
        return area;
    }

    // Mantém o traço dentro da altura nominal: y=0 → t/2 e y=h → h − t/2 (no referencial do texto).
    private static List<V2> Encolher(List<V2> pts, V2 origem, double rot, double k, double t)
    {
        double h = H * k;
        if (h <= t) return pts;
        double f = (h - t) / h;
        return pts.Select(p =>
        {
            var loc = (p - origem).Girar(-rot);
            loc = new V2(loc.X, t / 2 + loc.Y * f);
            return origem + loc.Girar(rot);
        }).ToList();
    }

    /// <summary>Área pintada estimada sem desenhar.</summary>
    public static double AreaEstimada(string texto, double h, double fatorLargura = 1, double espRel = EspessuraPadrao)
    {
        var d = new Desenho2D();
        return Desenhar(d, texto, V2.Zero, h, Cor.Branco, Alinhamento.Esquerda, fatorLargura, espRel);
    }
}
