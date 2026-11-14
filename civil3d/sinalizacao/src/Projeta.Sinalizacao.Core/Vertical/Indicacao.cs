using System.Text.Json;
using System.Text.Json.Serialization;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Core.Vertical;

public enum SetaIndicacao { Nenhuma, Esquerda, Direita, Frente, DiagonalEsquerda, DiagonalDireita }

public sealed class LinhaIndicacao
{
    public string Texto { get; set; } = "";
    public SetaIndicacao Seta { get; set; }
    /// <summary>Altura da letra desta linha em relação à altura padrão (1 = igual).</summary>
    public double Escala { get; set; } = 1.0;
    public LinhaIndicacao() { }
    public LinhaIndicacao(string texto, SetaIndicacao seta = SetaIndicacao.Nenhuma, double escala = 1) { Texto = texto; Seta = seta; Escala = escala; }
}

/// <summary>
/// Placa retangular montada por texto (indicação, educativa, serviços, obras): o tamanho sai da altura de
/// letra e do texto. Equivale ao "modelador de placas": define, pré-visualiza, calcula a área e salva na biblioteca.
/// </summary>
public sealed class ModeloPlaca
{
    public string Nome { get; set; } = "Indicação";
    public string Codigo { get; set; } = "I-01";
    public Familia Familia { get; set; } = Familia.Indicacao;
    public Cor Fundo { get; set; } = Cor.Verde;
    public Cor? CorTexto { get; set; }
    public bool Orla { get; set; } = true;
    /// <summary>Altura da letra maiúscula (m).</summary>
    public double AlturaLetra { get; set; } = 0.15;
    /// <summary>Margem em múltiplos da altura de letra.</summary>
    public double Margem { get; set; } = 0.8;
    /// <summary>Espaço entre linhas em múltiplos da altura de letra.</summary>
    public double EntreLinhas { get; set; } = 0.6;
    public double FatorLargura { get; set; } = 0.9;
    public Alinhamento Alinhamento { get; set; } = Alinhamento.Centro;
    /// <summary>Arredonda largura e altura para cima neste passo (m).</summary>
    public double Arredondamento { get; set; } = 0.05;
    public List<LinhaIndicacao> Linhas { get; set; } = new();

    [JsonIgnore]
    public Cor CorDoTexto => CorTexto ?? (Fundo is Cor.Branco or Cor.Amarelo or Cor.Laranja ? Cor.Preto : Cor.Branco);

    public static ModeloPlaca Exemplo() => new()
    {
        Nome = "Orientação de destino", Codigo = "I-01", Fundo = Cor.Verde, AlturaLetra = 0.15,
        Linhas = { new("CENTRO", SetaIndicacao.Frente), new("RODOVIÁRIA", SetaIndicacao.Esquerda), new("HOSPITAL", SetaIndicacao.Direita) },
    };

    private static readonly JsonSerializerOptions Op = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public string ParaJson() => JsonSerializer.Serialize(this, Op);
    public static ModeloPlaca? DeJson(string json) { try { return JsonSerializer.Deserialize<ModeloPlaca>(json, Op); } catch { return null; } }
}

public sealed record LayoutPlaca(Desenho2D Desenho, double Largura, double Altura)
{
    public double Area => Largura * Altura;
}

public static class GeradorIndicacao
{
    public static LayoutPlaca Montar(ModeloPlaca m)
    {
        double h = Math.Max(0.02, m.AlturaLetra);
        double margem = m.Margem * h;
        double seta = 1.25 * h; // largura do bloco da seta
        var linhas = m.Linhas.Where(l => !string.IsNullOrWhiteSpace(l.Texto) || l.Seta != SetaIndicacao.Nenhuma).ToList();
        if (linhas.Count == 0) linhas.Add(new LinhaIndicacao("TEXTO"));

        var medidas = linhas.Select(l =>
        {
            double hl = h * l.Escala;
            double wt = Fonte.Largura(l.Texto.ToUpper(Num.Br), hl, m.FatorLargura);
            double ws = l.Seta == SetaIndicacao.Nenhuma ? 0 : seta * l.Escala + (wt > 0 ? 0.6 * hl : 0);
            return (hl, wt, ws);
        }).ToList();

        double wConteudo = medidas.Max(x => x.wt + x.ws);
        double hConteudo = medidas.Sum(x => x.hl) + (linhas.Count - 1) * m.EntreLinhas * h;
        double W = Num.ArredCima(wConteudo + 2 * margem, m.Arredondamento);
        double H = Num.ArredCima(hConteudo + 2 * margem, m.Arredondamento);

        var d = new Desenho2D();
        double x0 = -W / 2, y0 = -H / 2;
        Pincel.Retangulo(d, x0, y0, x0 + W, y0 + H, m.Fundo);
        var corT = m.CorDoTexto;
        if (m.Orla)
        {
            double e = Math.Max(0.015, 0.12 * h), r = Math.Max(0.02, 0.2 * h);
            var c = new List<V2> { new(x0 + r, y0 + r), new(x0 + W - r, y0 + r), new(x0 + W - r, y0 + H - r), new(x0 + r, y0 + H - r) };
            Pincel.Tracos(d, c, e, corT, fechado: true);
        }

        double y = y0 + H - margem - (H - 2 * margem - hConteudo) / 2;
        for (int i = 0; i < linhas.Count; i++)
        {
            var l = linhas[i];
            var (hl, wt, ws) = medidas[i];
            y -= hl;
            bool setaEsq = l.Seta is SetaIndicacao.Esquerda or SetaIndicacao.DiagonalEsquerda or SetaIndicacao.Frente;
            double larguraLinha = wt + ws;
            double xLinha = m.Alinhamento switch
            {
                Alinhamento.Esquerda => x0 + margem,
                Alinhamento.Direita => x0 + W - margem - larguraLinha,
                _ => -larguraLinha / 2,
            };
            if (l.Seta != SetaIndicacao.Nenhuma)
            {
                double xs = setaEsq ? xLinha : xLinha + larguraLinha - seta * l.Escala;
                DesenharSeta(d, l.Seta, new V2(xs + seta * l.Escala / 2, y + hl / 2), seta * l.Escala, corT);
            }
            double xt = setaEsq && l.Seta != SetaIndicacao.Nenhuma ? xLinha + ws : xLinha;
            Fonte.Desenhar(d, l.Texto.ToUpper(Num.Br), new V2(xt, y), hl, corT, Alinhamento.Esquerda, m.FatorLargura, 0.15);
            y -= m.EntreLinhas * h;
        }
        return new LayoutPlaca(d, W, H);
    }

    private static void DesenharSeta(Desenho2D d, SetaIndicacao s, V2 c, double tam, Cor cor)
    {
        double ang = s switch
        {
            SetaIndicacao.Esquerda => Math.PI,
            SetaIndicacao.Direita => 0,
            SetaIndicacao.Frente => Math.PI / 2,
            SetaIndicacao.DiagonalEsquerda => 3 * Math.PI / 4,
            SetaIndicacao.DiagonalDireita => Math.PI / 4,
            _ => 0,
        };
        var u = V2.Polar(ang);
        var a = c - u * (tam * 0.48);
        var b = c + u * (tam * 0.48);
        Pincel.Seta(d, new[] { a, b }, tam * 0.16, cor, tam * 0.55, tam * 0.42);
    }
}
