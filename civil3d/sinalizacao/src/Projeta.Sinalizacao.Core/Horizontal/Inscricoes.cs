using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Core.Horizontal;

/// <summary>Setas, legendas e símbolos no pavimento (vol. IV, cap. 9).</summary>
public static class Inscricoes
{
    public sealed record TipoSeta(string Codigo, string Nome);

    public static readonly IReadOnlyList<TipoSeta> Setas = new List<TipoSeta>
    {
        new("PEM-SF", "Seta siga em frente"),
        new("PEM-VE", "Seta vire à esquerda"),
        new("PEM-VD", "Seta vire à direita"),
        new("PEM-SFVE", "Seta siga em frente ou vire à esquerda"),
        new("PEM-SFVD", "Seta siga em frente ou vire à direita"),
        new("PEM-RE", "Seta retorno à esquerda"),
        new("PEM-RD", "Seta retorno à direita"),
        new("PEM-VEVD", "Seta vire à esquerda ou à direita"),
        new("MOF-E", "Seta de mudança obrigatória de faixa (à esquerda)"),
        new("MOF-D", "Seta de mudança obrigatória de faixa (à direita)"),
    };

    public sealed record TipoSimbolo(string Codigo, string Nome, double Largura, double Altura);

    public static readonly IReadOnlyList<TipoSimbolo> Simbolos = new List<TipoSimbolo>
    {
        new("SIP", "Símbolo dê a preferência", 1.20, 3.60),
        new("SIB", "Símbolo de bicicleta (ciclofaixa)", 1.40, 1.00),
        new("SID", "Símbolo de deficiente físico", 1.20, 1.20),
        new("SIS", "Símbolo de serviço de saúde (cruz)", 1.20, 1.20),
    };

    /// <summary>
    /// Seta no referencial local: base em (0,0), apontando para +Y (sentido do tráfego), comprimento L.
    /// </summary>
    public static ResultadoPeca Seta(string codigo, double comprimento)
    {
        var r = new ResultadoPeca { Codigo = codigo.ToUpperInvariant(), Comprimento = comprimento };
        r.Descricao = Setas.FirstOrDefault(s => s.Codigo == r.Codigo)?.Nome ?? codigo;
        double k = comprimento / 5.0;
        bool dir = r.Codigo.EndsWith("VD") || r.Codigo.EndsWith("RD") || r.Codigo == "MOF-D";
        var d = new Desenho2D();
        double esp = 0.24, lc = 0.80, cc = 1.40;
        V2 P(double x, double y) => new(x, y);
        switch (r.Codigo.Replace("VD", "VE").Replace("RD", "RE").Replace("MOF-D", "MOF-E"))
        {
            case "PEM-SF":
                Pincel.Seta(d, new[] { P(0, 0), P(0, 5) }, esp, Cor.Branco, lc, cc);
                break;
            case "PEM-VE":
            {
                var pts = new List<V2> { P(0.45, 0), P(0.45, 2.7) };
                pts.AddRange(Pincel.Arco(P(-0.45, 2.7), 0.9, 0, 90, 12).Skip(1));
                pts.Add(P(-1.85, 3.6));
                Pincel.Seta(d, pts, esp, Cor.Branco, lc, cc * 0.85);
                break;
            }
            case "PEM-SFVE":
            {
                Pincel.Seta(d, new[] { P(0.45, 0), P(0.45, 5) }, esp, Cor.Branco, lc, cc);
                var pts = new List<V2> { P(0.45, 1.4) };
                pts.AddRange(Pincel.Arco(P(-0.45, 1.4), 0.9, 0, 90, 12).Skip(1));
                pts.Add(P(-1.75, 2.3));
                Pincel.Seta(d, pts, esp, Cor.Branco, lc, cc * 0.85);
                break;
            }
            case "PEM-VEVE": // vire à esquerda ou à direita (o Replace converte VD → VE)
            {
                Pincel.Tracos(d, new[] { P(0, 0), P(0, 2.4) }, esp, Cor.Branco);
                foreach (int sx in new[] { -1, 1 })
                {
                    var pts = new List<V2> { P(0, 2.4) };
                    pts.AddRange(Pincel.Arco(P(sx * 0.9, 2.4), 0.9, sx < 0 ? 0 : 180, 90, 12).Skip(1));
                    pts.Add(P(sx * 2.1, 3.3));
                    Pincel.Seta(d, pts, esp, Cor.Branco, lc, cc * 0.85);
                }
                break;
            }
            case "PEM-RE":
            {
                var pts = new List<V2> { P(0.55, 0), P(0.55, 3.3) };
                pts.AddRange(Pincel.Arco(P(-0.30, 3.3), 0.85, 0, 180, 18).Skip(1));
                pts.Add(P(-1.15, 1.5));
                Pincel.Seta(d, pts, esp, Cor.Branco, lc, cc * 0.85);
                break;
            }
            case "MOF-E":
            {
                var pts = new List<V2>();
                for (int i = 0; i <= 20; i++)
                {
                    double t = i / 20.0;
                    double y = t * 4.2;
                    double x = 0.6 - 1.2 * (3 * t * t - 2 * t * t * t);
                    pts.Add(P(x, y));
                }
                pts.Add(P(-0.6, 5.0));
                Pincel.Seta(d, pts, esp, Cor.Branco, lc, cc);
                break;
            }
            default:
                Pincel.Seta(d, new[] { P(0, 0), P(0, 5) }, esp, Cor.Branco, lc, cc);
                break;
        }
        var final = d.Transformado(k, 0, null, dir);
        r.Desenho.Juntar(final);
        r.Area = final.AreaPintada();
        r.Pecas = 1;
        return r;
    }

    /// <summary>
    /// Legenda no referencial local (base da primeira linha em y=0, centrada em x=0, lida por quem vem de −Y).
    /// Linhas separadas por '|' — a primeira é a mais próxima do condutor.
    /// </summary>
    public static ResultadoPeca Legenda(string texto, double alturaLetra, double fatorLargura, double espacoLinhas)
    {
        var r = new ResultadoPeca { Codigo = "LEG", Descricao = $"Legenda \"{texto.Replace('|', ' ')}\"", Comprimento = alturaLetra };
        var linhas = texto.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim().ToUpper(Num.Br)).Where(l => l.Length > 0).ToList();
        double y = 0;
        foreach (var l in linhas)
        {
            r.Area += Fonte.Desenhar(r.Desenho, l, new V2(0, y), alturaLetra, Cor.Branco, Alinhamento.Centro, fatorLargura, 0.16);
            y += alturaLetra + espacoLinhas;
            r.Pecas += l.Replace(" ", "").Length;
        }
        r.Comprimento = linhas.Count == 0 ? 0 : y - espacoLinhas;
        return r;
    }

    /// <summary>Símbolo no referencial local (centrado na origem, "em pé" para quem vem de −Y).</summary>
    public static ResultadoPeca Simbolo(string codigo, double escala = 1.0)
    {
        var tipo = Simbolos.FirstOrDefault(s => s.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)) ?? Simbolos[0];
        var r = new ResultadoPeca { Codigo = tipo.Codigo, Descricao = tipo.Nome, Comprimento = tipo.Altura * escala };
        var d = new Desenho2D();
        switch (tipo.Codigo)
        {
            case "SIP":
            {
                // triângulo vazado com a ponta voltada para o condutor que se aproxima
                double b = tipo.Largura / 2, h = tipo.Altura, e = 0.20;
                var ext = new[] { new V2(-b, h / 2), new V2(0, -h / 2), new V2(b, h / 2) };
                Pincel.Tracos(d, ext, e, Cor.Branco, fechado: true);
                break;
            }
            case "SIB":
                Figuras.Bicicleta(d, V2.Zero, 0.70, Cor.Branco);
                break;
            case "SID":
                Figuras.CadeiraDeRodas(d, V2.Zero, 0.55, Cor.Branco);
                break;
            case "SIS":
                Pincel.Retangulo(d, -0.18, -0.60, 0.18, 0.60, Cor.Branco);
                Pincel.Retangulo(d, -0.60, -0.18, 0.60, 0.18, Cor.Branco);
                break;
        }
        var f = d.Transformado(escala);
        r.Desenho.Juntar(f);
        r.Area = f.AreaPintada();
        r.Pecas = 1;
        return r;
    }
}

/// <summary>Tachas e tachões ao longo de um caminho.</summary>
public static class Tachas
{
    public sealed record TipoTacha(string Codigo, string Nome, double Comprimento, double Largura);

    public static readonly IReadOnlyList<TipoTacha> Tipos = new List<TipoTacha>
    {
        new("TACHA-MONO", "Tacha monodirecional", 0.10, 0.10),
        new("TACHA-BI", "Tacha bidirecional", 0.10, 0.10),
        new("TACHAO-MONO", "Tachão monodirecional", 0.25, 0.15),
        new("TACHAO-BI", "Tachão bidirecional", 0.25, 0.15),
    };

    /// <summary>Distâncias ao longo do caminho, com espaçamento menor nos trechos em curva (R &lt; raioCurva).</summary>
    public static List<double> Posicoes(Caminho c, double espReta, double espCurva, double raioCurva, double inicio = 0)
    {
        var l = new List<double>();
        if (espReta <= 0) return l;
        double s = Math.Max(0, inicio);
        while (s <= c.Comprimento + 1e-6)
        {
            l.Add(s);
            double esp = c.Raio(s) < raioCurva ? espCurva : espReta;
            s += Math.Max(0.1, esp);
        }
        return l;
    }

    /// <summary>Planta de uma tacha (retângulo centrado, comprimento no sentido do fluxo).</summary>
    public static Desenho2D Planta(string codigo, Cor cor, double fatorExibicao = 1)
    {
        var t = Tipos.FirstOrDefault(x => x.Codigo == codigo) ?? Tipos[0];
        var d = new Desenho2D();
        double a = t.Comprimento * fatorExibicao / 2, b = t.Largura * fatorExibicao / 2;
        d.Area(new[] { new V2(-a, -b), new V2(a, -b), new V2(a, b), new V2(-a, b) }, cor);
        return d;
    }
}
