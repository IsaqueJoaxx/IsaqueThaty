using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Core.Horizontal;

public enum Padrao { Continua, Seccionada }
public enum Ritmo { Seccionada, Continuidade, Reversivel }
public enum Espessura { Normal, Especial, Canalizacao, Estacionamento }

/// <summary>Tipo de marca longitudinal (vol. IV do Manual Brasileiro de Sinalização de Trânsito).</summary>
public sealed record TipoLongitudinal(string Codigo, string Nome, Cor Cor, Padrao[] Linhas, Ritmo Ritmo = Ritmo.Seccionada, Espessura Espessura = Espessura.Normal)
{
    public bool Dupla => Linhas.Length == 2;
}

public static class CatalogoLongitudinal
{
    public static readonly IReadOnlyList<TipoLongitudinal> Tipos = new List<TipoLongitudinal>
    {
        new("LFO-1", "Linha simples contínua (fluxos opostos)", Cor.Amarelo, new[] { Padrao.Continua }),
        new("LFO-2", "Linha simples seccionada (fluxos opostos)", Cor.Amarelo, new[] { Padrao.Seccionada }),
        new("LFO-3", "Linha dupla contínua (fluxos opostos)", Cor.Amarelo, new[] { Padrao.Continua, Padrao.Continua }),
        new("LFO-4", "Linha contínua/seccionada (fluxos opostos)", Cor.Amarelo, new[] { Padrao.Seccionada, Padrao.Continua }),
        new("LMS-1", "Linha simples contínua (mesmo sentido)", Cor.Branco, new[] { Padrao.Continua }),
        new("LMS-2", "Linha simples seccionada (mesmo sentido)", Cor.Branco, new[] { Padrao.Seccionada }),
        new("LBO", "Linha de bordo", Cor.Branco, new[] { Padrao.Continua }),
        new("LCO", "Linha de continuidade", Cor.Branco, new[] { Padrao.Seccionada }, Ritmo.Continuidade),
        new("MFE", "Faixa exclusiva — no fluxo", Cor.Branco, new[] { Padrao.Continua }, Espessura: Espessura.Especial),
        new("MFE-CF", "Faixa exclusiva — no contrafluxo (dupla)", Cor.Amarelo, new[] { Padrao.Continua, Padrao.Continua }),
        new("MFP", "Faixa preferencial", Cor.Branco, new[] { Padrao.Continua }, Espessura: Espessura.Especial),
        new("MFR", "Faixa reversível (dupla seccionada)", Cor.Amarelo, new[] { Padrao.Seccionada, Padrao.Seccionada }, Ritmo.Reversivel),
        new("MCI", "Ciclofaixa ao longo da via", Cor.Branco, new[] { Padrao.Continua }, Espessura: Espessura.Especial),
        new("LCA", "Linha de canalização", Cor.Branco, new[] { Padrao.Continua }, Espessura: Espessura.Canalizacao),
        new("LPP", "Linha de proibição de estacionamento/parada", Cor.Amarelo, new[] { Padrao.Continua }, Espessura: Espessura.Estacionamento),
    };

    public static TipoLongitudinal? Obter(string codigo) =>
        Tipos.FirstOrDefault(t => string.Equals(t.Codigo, codigo?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>Parâmetros de uma marca longitudinal lançada sobre um caminho.</summary>
public sealed class ParamLongitudinal
{
    public string Codigo { get; set; } = "LFO-2";
    public double Velocidade { get; set; } = 60;
    /// <summary>Deslocamento do eixo da marca em relação ao caminho (positivo = esquerda).</summary>
    public double Deslocamento { get; set; }
    public double? Largura { get; set; }
    public double? Traco { get; set; }
    public double? Espaco { get; set; }
    /// <summary>Distância do início do caminho até o primeiro traço.</summary>
    public double Fase { get; set; }
    /// <summary>LFO-4: troca o lado da linha seccionada.</summary>
    public bool InverterLados { get; set; }
    public Cor? Cor { get; set; }
    /// <summary>Recorte: começa/termina a marca nestas distâncias ao longo do caminho (opcional).</summary>
    public double? Inicio { get; set; }
    public double? Fim { get; set; }
}

public sealed class ResultadoMarca
{
    public Desenho2D Desenho { get; } = new();
    public string Codigo { get; init; } = "";
    public Cor Cor { get; init; }
    public double ComprimentoEixo { get; set; }
    /// <summary>Soma dos comprimentos pintados de todas as linhas (m).</summary>
    public double ComprimentoPintado { get; set; }
    public double Area { get; set; }
    public double Largura { get; set; }
    public double Traco { get; set; }
    public double Espaco { get; set; }
    public int NumTracos { get; set; }
    public string Cadencia => Traco > 0 ? $"{Num.F0(Traco)}:{Num.F0(Espaco)}" : "contínua";
}

public static class GeradorLongitudinal
{
    public static double LarguraPadrao(N n, TipoLongitudinal t, double v) => t.Espessura switch
    {
        Espessura.Especial => n.LarguraLinhaEspecial,
        Espessura.Canalizacao => n.LarguraLCA,
        Espessura.Estacionamento => n.LarguraLPP,
        _ => N.PorVelocidade(n.LarguraLinha, v),
    };

    public static (double traco, double espaco) CadenciaPadrao(N n, TipoLongitudinal t, double v)
    {
        var tab = t.Ritmo switch { Ritmo.Continuidade => n.CadenciaContinuidade, Ritmo.Reversivel => n.CadenciaReversivel, _ => n.CadenciaSeccionada };
        var c = N.CadenciaPara(tab, v);
        return (c.Traco, c.Espaco);
    }

    public static ResultadoMarca Gerar(Caminho caminho, ParamLongitudinal p, N normas)
    {
        var tipo = CatalogoLongitudinal.Obter(p.Codigo) ?? throw new ArgumentException($"Marca longitudinal desconhecida: {p.Codigo}");
        var cor = p.Cor ?? tipo.Cor;
        double w = p.Largura ?? LarguraPadrao(normas, tipo, p.Velocidade);
        var (tPad, ePad) = CadenciaPadrao(normas, tipo, p.Velocidade);
        double traco = p.Traco ?? tPad, espaco = p.Espaco ?? ePad;
        bool temSecc = tipo.Linhas.Contains(Padrao.Seccionada);
        var r = new ResultadoMarca
        {
            Codigo = tipo.Codigo, Cor = cor, Largura = w,
            Traco = temSecc ? traco : 0, Espaco = temSecc ? espaco : 0,
        };
        double s0 = Math.Max(0, p.Inicio ?? 0), s1 = Math.Min(caminho.Comprimento, p.Fim ?? caminho.Comprimento);
        if (s1 - s0 < 1e-6) return r;
        r.ComprimentoEixo = s1 - s0;

        var linhas = tipo.Linhas.ToArray();
        if (p.InverterLados && linhas.Length == 2) Array.Reverse(linhas);
        double[] offs = linhas.Length == 2
            ? new[] { p.Deslocamento + (normas.DistanciaLinhasDuplas + w) / 2, p.Deslocamento - (normas.DistanciaLinhasDuplas + w) / 2 }
            : new[] { p.Deslocamento };

        for (int k = 0; k < linhas.Length; k++)
        {
            if (linhas[k] == Padrao.Continua)
            {
                // divide trechos muito longos para manter polígonos leves
                const double maxPeca = 200.0;
                for (double a = s0; a < s1 - 1e-6; a += maxPeca)
                {
                    double b = Math.Min(s1, a + maxPeca);
                    Adicionar(r, caminho.Faixa(a, b, offs[k], w), cor, b - a);
                }
            }
            else
            {
                if (traco <= 0) traco = 1;
                double ciclo = traco + Math.Max(0, espaco);
                double a = s0 + (p.Fase % ciclo + ciclo) % ciclo;
                // traço parcial no início, se a fase o cortar
                if (a - espaco > s0 + 0.05 && p.Fase > 0) Adicionar(r, caminho.Faixa(s0, a - espaco, offs[k], w), cor, a - espaco - s0, conta: true);
                for (; a < s1 - 0.05; a += ciclo)
                {
                    double b = Math.Min(s1, a + traco);
                    if (b - a < 0.05) break;
                    Adicionar(r, caminho.Faixa(a, b, offs[k], w), cor, b - a, conta: true);
                }
            }
        }
        return r;
    }

    private static void Adicionar(ResultadoMarca r, List<V2> poli, Cor cor, double comp, bool conta = false)
    {
        r.Desenho.Area(poli, cor);
        r.ComprimentoPintado += comp;
        r.Area += Poligonos.Area(poli);
        if (conta) r.NumTracos++;
    }
}
