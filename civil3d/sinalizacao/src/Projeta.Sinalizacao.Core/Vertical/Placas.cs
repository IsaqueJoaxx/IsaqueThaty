using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Normas;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Core.Vertical;

public enum Familia { Regulamentacao, Advertencia, Obras, Indicacao, Educativa, ServicosAuxiliares }
public enum Forma { Circular, Octogonal, Triangular, Losango, Retangular, CruzSantoAndre }

/// <summary>Tipo de placa do catálogo (código CONTRAN, nome, forma e pictograma).</summary>
public sealed class TipoPlaca
{
    public string Codigo { get; init; } = "";
    public string Nome { get; init; } = "";
    public Familia Familia { get; init; }
    public Forma Forma { get; init; }
    /// <summary>Tarja vermelha diagonal (proibição).</summary>
    public bool Proibicao { get; init; }
    /// <summary>Valor variável exibido na placa (velocidade, altura, peso…). Null = sem valor.</summary>
    public string? ValorPadrao { get; init; }
    /// <summary>Desenha o pictograma no referencial normalizado.</summary>
    public Action<Pic>? Picto { get; init; }
    /// <summary>Usa o pictograma de outro código espelhado (variante a/b).</summary>
    public string? EspelhoDe { get; init; }
    public string Rotulo => $"{Codigo} — {Nome}";
}

/// <summary>Contexto de desenho de pictograma: unidade normalizada → metros (K).</summary>
public sealed class Pic
{
    public Desenho2D D { get; } = new();
    public double K { get; init; }
    public string Valor { get; init; } = "";
    public Cor Cor { get; set; } = Cor.Preto;
    public double E { get; set; } = 0.18;
    public V2 P(double x, double y) => new(x * K, y * K);

    private List<V2> Pts(double[] xy)
    {
        var l = new List<V2>(xy.Length / 2);
        for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(P(xy[i], xy[i + 1]));
        return l;
    }

    public void Tr(params double[] xy) => Pincel.Tracos(D, Pts(xy), E * K, Cor);
    public void TrE(double esp, params double[] xy) => Pincel.Tracos(D, Pts(xy), esp * K, Cor);
    public void Seta(params double[] xy) => Pincel.Seta(D, Pts(xy), E * K, Cor, E * K * 3.0, E * K * 2.5);
    public void SetaL(IList<V2> ptsNorm) => Pincel.Seta(D, ptsNorm.Select(p => p * K).ToList(), E * K, Cor, E * K * 3.0, E * K * 2.5);
    public void TrL(IList<V2> ptsNorm) => Pincel.Tracos(D, ptsNorm.Select(p => p * K).ToList(), E * K, Cor);
    public void Ar(params double[] xy) => D.Area(Pts(xy), Cor);
    public void ArC(Cor c, params double[] xy) => D.Area(Pts(xy), c);
    public void Disco(double x, double y, double r, Cor? c = null) => D.Disco(P(x, y), r * K, c ?? Cor);

    /// <summary>Texto centrado em (x, yCentro) cabendo em larguraMax × alturaMax (normalizados).</summary>
    public void Texto(string s, double x, double yCentro, double alturaMax, double larguraMax = 1.9, double fator = 1.0, Cor? c = null)
    {
        if (string.IsNullOrEmpty(s)) return;
        double h = alturaMax;
        double w = Fonte.Largura(s, h, fator);
        if (w > larguraMax) h *= larguraMax / w;
        Fonte.Desenhar(D, s, P(x, yCentro - h / 2), h * K, c ?? Cor, Alinhamento.Centro, fator, 0.15);
    }

    public void Fig(Action<Desenho2D, V2, double, Cor, bool> f, double x, double y, double s, bool espelhar = false) => f(D, P(x, y), s * K, Cor, espelhar);

    public static List<V2> Arco(double cx, double cy, double r, double a0, double a1, int n = 16) => Pincel.Arco(new V2(cx, cy), r, a0, a1, n);
}

/// <summary>Gera a face das placas a partir do catálogo e das proporções do CONTRAN.</summary>
public static class GeradorPlaca
{
    /// <summary>Dimensão padrão da placa para o tipo de via.</summary>
    public static double DimensaoPadrao(TipoPlaca t, N n, TipoVia via) => t.Forma switch
    {
        Forma.Circular => n.DiametroCircular[via],
        Forma.Octogonal => n.LadoOctogono[via],
        Forma.Triangular => n.LadoTriangulo[via],
        Forma.Losango => n.LadoAdvertencia[via],
        Forma.CruzSantoAndre => via == TipoVia.Urbana ? 1.20 : 1.50,
        _ => 1.0,
    };

    public static IReadOnlyList<double> Opcoes(TipoPlaca t, N n) => t.Forma switch
    {
        Forma.Circular => n.OpcoesCircular,
        Forma.Octogonal => n.OpcoesOctogono,
        Forma.Triangular => n.OpcoesTriangulo,
        Forma.Losango => n.OpcoesAdvertencia,
        Forma.CruzSantoAndre => new List<double> { 1.20, 1.50 },
        _ => new List<double>(),
    };

    /// <summary>Descrição da dimensão: "Ø 0,50", "L 0,25", "0,60 × 0,60" …</summary>
    public static string TextoDimensao(Forma f, double dim, double altura = 0) => f switch
    {
        Forma.Circular => $"Ø {Num.M(dim)}",
        Forma.Octogonal => $"L {Num.M(dim)}",
        Forma.Triangular => $"L {Num.M(dim)}",
        Forma.Losango => $"{Num.M(dim)} × {Num.M(dim)}",
        Forma.CruzSantoAndre => $"{Num.M(dim)} (cruz)",
        _ => $"{Num.M(dim)} × {Num.M(altura)}",
    };

    /// <summary>Área da chapa (m²).</summary>
    public static double Area(Forma f, double dim, double altura = 0) => f switch
    {
        Forma.Circular => Math.PI * dim * dim / 4,
        Forma.Octogonal => 2 * (1 + Math.Sqrt(2)) * dim * dim,
        Forma.Triangular => Math.Sqrt(3) / 4 * dim * dim,
        Forma.Losango => dim * dim,
        Forma.CruzSantoAndre => 2 * dim * (dim * 0.15) - Math.Pow(dim * 0.15, 2),
        _ => dim * altura,
    };

    /// <summary>Altura total da face (para o comprimento do suporte).</summary>
    public static double AlturaFace(Forma f, double dim, double altura = 0) => f switch
    {
        Forma.Circular => dim,
        Forma.Octogonal => dim * (1 + Math.Sqrt(2)),
        Forma.Triangular => dim * Math.Sqrt(3) / 2,
        Forma.Losango => dim * Math.Sqrt(2),
        Forma.CruzSantoAndre => dim * Math.Sqrt(0.5) + dim * 0.15,
        _ => altura,
    };

    public static double LarguraFace(Forma f, double dim, double largura = 0) => f switch
    {
        Forma.Circular => dim,
        Forma.Octogonal => dim * (1 + Math.Sqrt(2)),
        Forma.Triangular => dim,
        Forma.Losango => dim * Math.Sqrt(2),
        Forma.CruzSantoAndre => dim * Math.Sqrt(0.5) + dim * 0.15,
        _ => largura,
    };

    /// <summary>
    /// Desenha a face da placa em metros, centrada na origem.
    /// <paramref name="obra"/> troca o fundo amarelo da advertência por laranja (sinalização temporária).
    /// </summary>
    public static Desenho2D Gerar(TipoPlaca t, double dim, string? valor, N n, bool obra = false)
    {
        var d = new Desenho2D();
        string v = string.IsNullOrWhiteSpace(valor) ? t.ValorPadrao ?? "" : valor!;
        switch (t.Forma)
        {
            case Forma.Circular: Circular(d, t, dim, v, n); break;
            case Forma.Octogonal: Octogonal(d, dim, n); break;
            case Forma.Triangular: Triangular(d, dim, n); break;
            case Forma.Losango: Losango(d, t, dim, v, n, obra); break;
            case Forma.CruzSantoAndre: Cruz(d, dim); break;
            default:
                Pincel.Retangulo(d, -dim / 2, -dim / 4, dim / 2, dim / 4, Cor.Branco);
                d.Linha(new[] { new V2(-dim / 2, -dim / 4), new V2(dim / 2, -dim / 4), new V2(dim / 2, dim / 4), new V2(-dim / 2, dim / 4) }, Cor.Preto, true);
                break;
        }
        return d;
    }

    private static Pic Picto(TipoPlaca t, double k, string valor)
    {
        var p = new Pic { K = k, Valor = valor };
        if (t.EspelhoDe is not null)
        {
            var baseT = Catalogo.Obter(t.EspelhoDe);
            baseT?.Picto?.Invoke(p);
            var esp = p.D.Transformado(1, 0, null, espelharX: true);
            var q = new Pic { K = k, Valor = valor };
            q.D.Juntar(esp);
            return q;
        }
        t.Picto?.Invoke(p);
        return p;
    }

    private static void Circular(Desenho2D d, TipoPlaca t, double D, string v, N n)
    {
        double R = D / 2;
        double orla = n.OrlaCircularRel * D;
        double tarja = n.TarjaCircularRel * D;
        if (t.Codigo == "R-3")
        {
            // sentido proibido: fundo vermelho, faixa branca, orla branca
            d.Disco(V2.Zero, R, Cor.Branco);
            d.Disco(V2.Zero, R * 0.94, Cor.Vermelho);
            Pincel.Retangulo(d, -R * 0.68, -R * 0.17, R * 0.68, R * 0.17, Cor.Branco);
            return;
        }
        d.Disco(V2.Zero, R, Cor.Vermelho);
        double ri = R - orla;
        d.Disco(V2.Zero, ri, Cor.Branco);
        var p = Picto(t, 0.27 * D, v);
        d.Juntar(p.D);
        if (t.Proibicao)
        {
            d.Area(Poligonos.Retangulo(V2.Zero, new V2(1, -1), 2 * ri, tarja), Cor.Vermelho);
            if (t.Codigo == "R-6c") d.Area(Poligonos.Retangulo(V2.Zero, new V2(1, 1), 2 * ri, tarja), Cor.Vermelho);
        }
    }

    private static List<V2> Octogono(double raioInscrito)
    {
        double rc = raioInscrito / Math.Cos(Math.PI / 8);
        return Poligonos.PoligonoRegular(V2.Zero, rc, 8, Math.PI / 8);
    }

    private static void Octogonal(Desenho2D d, double L, N n)
    {
        double ri = L * (1 + Math.Sqrt(2)) / 2; // apótema
        double oe = n.OrlaExternaOctRel * L, oi = n.OrlaInternaOctRel * L;
        d.Area(Octogono(ri), Cor.Vermelho);
        d.Area(Octogono(ri - oe), Cor.Branco);
        d.Area(Octogono(ri - oe - oi), Cor.Vermelho);
        double larguraUtil = 2 * (ri - oe - oi) * 0.86;
        double h = Math.Min(0.55 * L, larguraUtil / Fonte.Largura("PARE", 1, 0.95, 0.17));
        Fonte.Desenhar(d, "PARE", new V2(0, -h / 2), h, Cor.Branco, Alinhamento.Centro, 0.95, 0.17);
    }

    private static void Triangular(Desenho2D d, double L, N n)
    {
        double H = L * Math.Sqrt(3) / 2;
        var tri = new List<V2> { new(-L / 2, H / 3), new(0, -2 * H / 3), new(L / 2, H / 3) };
        d.Area(tri, Cor.Vermelho);
        double r = H / 3, e = n.OrlaTrianguloRel * L;
        double f = (r - e) / r;
        d.Area(tri.Select(p => p * f).ToList(), Cor.Branco);
    }

    private static void Losango(Desenho2D d, TipoPlaca t, double L, string v, N n, bool obra)
    {
        double a = L / Math.Sqrt(2);
        double m = n.MargemAdvRel * L, o = n.OrlaPretaAdvRel * L;
        Cor fundo = obra || t.Familia == Familia.Obras ? Cor.Laranja : Cor.Amarelo;
        List<V2> Los(double h) => new() { new(0, h), new(-h, 0), new(0, -h), new(h, 0) };
        d.Area(Los(a), fundo);
        d.Area(Los(a - m * Math.Sqrt(2)), Cor.Preto);
        d.Area(Los(a - (m + o) * Math.Sqrt(2)), fundo);
        var p = Picto(t, 0.42 * L, v);
        d.Juntar(p.D);
    }

    private static void Cruz(Desenho2D d, double comp)
    {
        double w = comp * 0.15;
        foreach (var dir in new[] { new V2(1, 1), new V2(1, -1) })
        {
            d.Area(Poligonos.Retangulo(V2.Zero, dir, comp, w), Cor.Vermelho);
        }
        foreach (var dir in new[] { new V2(1, 1), new V2(1, -1) })
            d.Area(Poligonos.Retangulo(V2.Zero, dir, comp - w * 0.4, w * 0.6), Cor.Branco);
    }

    /// <summary>Contorno externo simples da placa (para a chapa nos sólidos 3D).</summary>
    public static List<V2> Contorno(Forma f, double dim, double largura = 0, double altura = 0)
    {
        switch (f)
        {
            case Forma.Circular: return Poligonos.Circulo(V2.Zero, dim / 2, 40);
            case Forma.Octogonal: return Octogono(dim * (1 + Math.Sqrt(2)) / 2);
            case Forma.Triangular:
            {
                double H = dim * Math.Sqrt(3) / 2;
                return new List<V2> { new(-dim / 2, H / 3), new(0, -2 * H / 3), new(dim / 2, H / 3) };
            }
            case Forma.Losango:
            {
                double a = dim / Math.Sqrt(2);
                return new List<V2> { new(0, a), new(-a, 0), new(0, -a), new(a, 0) };
            }
            case Forma.CruzSantoAndre:
            {
                double h = (dim * Math.Sqrt(0.5) + dim * 0.15) / 2;
                return new List<V2> { new(-h, -h), new(h, -h), new(h, h), new(-h, h) };
            }
            default:
                return new List<V2> { new(-largura / 2, -altura / 2), new(largura / 2, -altura / 2), new(largura / 2, altura / 2), new(-largura / 2, altura / 2) };
        }
    }
}
