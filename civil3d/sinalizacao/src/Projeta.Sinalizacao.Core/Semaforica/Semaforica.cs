using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Core.Semaforica;

/// <summary>Item de sinalização semafórica (vol. V) ou dispositivo de obra (vol. VII) representado por símbolo em planta.</summary>
public sealed record TipoDispositivo(string Codigo, string Nome, string Unidade, double Largura, double Profundidade, double Altura, string Grupo);

public static class CatalogoSemaforica
{
    public static readonly IReadOnlyList<TipoDispositivo> Itens = new List<TipoDispositivo>
    {
        new("GFV-200", "Grupo focal veicular 3 focos Ø 200 mm", "un", 0.30, 0.25, 0.80, "Semafórica"),
        new("GFV-300", "Grupo focal veicular 3 focos Ø 300 mm", "un", 0.40, 0.30, 1.05, "Semafórica"),
        new("GFP-200", "Grupo focal de pedestres 2 focos Ø 200 mm", "un", 0.30, 0.25, 0.55, "Semafórica"),
        new("GFC-200", "Grupo focal de ciclistas 3 focos Ø 200 mm", "un", 0.30, 0.25, 0.80, "Semafórica"),
        new("BOT", "Botoeira para pedestres", "un", 0.15, 0.12, 0.25, "Semafórica"),
        new("COL-S", "Coluna simples", "un", 0.11, 0.11, 4.00, "Semafórica"),
        new("COL-BP", "Coluna com braço projetado", "un", 0.14, 0.14, 6.00, "Semafórica"),
        new("CTR", "Controlador semafórico", "un", 0.60, 0.40, 1.20, "Semafórica"),
        new("CXP", "Caixa de passagem", "un", 0.50, 0.50, 0.50, "Semafórica"),
    };

    public static readonly IReadOnlyList<(string Codigo, string Nome)> Redes = new List<(string, string)>
    {
        ("REDE-SUB", "Rede subterrânea (duto PEAD + cabo)"),
        ("REDE-AER", "Rede aérea (cabo)"),
    };

    public static TipoDispositivo? Obter(string c) => Itens.FirstOrDefault(i => i.Codigo.Equals(c, StringComparison.OrdinalIgnoreCase))
        ?? Obras.CatalogoObras.Itens.FirstOrDefault(i => i.Codigo.Equals(c, StringComparison.OrdinalIgnoreCase));

    /// <summary>Símbolo em planta (m, centrado na origem, "frente" voltada para +Y).</summary>
    public static Desenho2D Planta(string codigo, double braco = 4.6)
    {
        var d = new Desenho2D();
        switch (codigo.ToUpperInvariant())
        {
            case "GFV-200":
            case "GFV-300":
            case "GFC-200":
            {
                double w = codigo.EndsWith("300") ? 0.40 : 0.30, h = w * 2.7;
                Pincel.Retangulo(d, -h / 2, -w / 2, h / 2, w / 2, Cor.Preto);
                var cores = codigo.StartsWith("GFC") ? new[] { Cor.Vermelho, Cor.Amarelo, Cor.Verde } : new[] { Cor.Vermelho, Cor.Amarelo, Cor.Verde };
                for (int i = 0; i < 3; i++) d.Disco(new V2(-h / 3 + i * h / 3, 0), w * 0.36, cores[i]);
                // aba indicando a face
                d.Area(new[] { new V2(-0.12, w / 2), new V2(0.12, w / 2), new V2(0, w / 2 + 0.2) }, Cor.Preto);
                if (codigo.StartsWith("GFC")) Figuras.Bicicleta(d, new V2(0, -w / 2 - 0.25), 0.2, Cor.Preto);
                break;
            }
            case "GFP-200":
            {
                double w = 0.30, h = 0.62;
                Pincel.Retangulo(d, -h / 2, -w / 2, h / 2, w / 2, Cor.Preto);
                d.Disco(new V2(-h / 4, 0), w * 0.36, Cor.Vermelho);
                d.Disco(new V2(h / 4, 0), w * 0.36, Cor.Verde);
                d.Area(new[] { new V2(-0.12, w / 2), new V2(0.12, w / 2), new V2(0, w / 2 + 0.2) }, Cor.Preto);
                break;
            }
            case "BOT":
                Pincel.Retangulo(d, -0.15, -0.15, 0.15, 0.15, Cor.Preto);
                Fonte.Desenhar(d, "B", new V2(0, -0.09), 0.18, Cor.Branco, Alinhamento.Centro);
                break;
            case "COL-S":
                d.Disco(V2.Zero, 0.15, Cor.Preto);
                d.Disco(V2.Zero, 0.08, Cor.Branco);
                break;
            case "COL-BP":
                d.Disco(V2.Zero, 0.18, Cor.Preto);
                d.Disco(V2.Zero, 0.10, Cor.Branco);
                Pincel.Tracos(d, new[] { new V2(0.18, 0), new V2(braco, 0) }, 0.10, Cor.Preto);
                break;
            case "CTR":
                Pincel.Retangulo(d, -0.30, -0.20, 0.30, 0.20, Cor.Preto);
                Fonte.Desenhar(d, "CTR", new V2(0, -0.07), 0.14, Cor.Branco, Alinhamento.Centro);
                break;
            case "CXP":
                Pincel.Tracos(d, new[] { new V2(-0.25, -0.25), new V2(0.25, -0.25), new V2(0.25, 0.25), new V2(-0.25, 0.25) }, 0.05, Cor.Preto, fechado: true);
                Pincel.Tracos(d, new[] { new V2(-0.25, -0.25), new V2(0.25, 0.25) }, 0.04, Cor.Preto);
                Pincel.Tracos(d, new[] { new V2(-0.25, 0.25), new V2(0.25, -0.25) }, 0.04, Cor.Preto);
                break;
            default:
                return Obras.CatalogoObras.Planta(codigo);
        }
        return d;
    }
}
