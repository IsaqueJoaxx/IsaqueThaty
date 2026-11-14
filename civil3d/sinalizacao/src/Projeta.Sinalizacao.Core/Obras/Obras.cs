using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Semaforica;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Core.Obras;

/// <summary>Dispositivos de canalização da sinalização temporária (vol. VII).</summary>
public static class CatalogoObras
{
    public static readonly IReadOnlyList<TipoDispositivo> Itens = new List<TipoDispositivo>
    {
        new("CONE", "Cone (0,75 m)", "un", 0.40, 0.40, 0.75, "Obras"),
        new("TAMBOR", "Tambor/barril canalizador", "un", 0.60, 0.60, 1.00, "Obras"),
        new("CAVALETE", "Cavalete (barreira tipo I)", "un", 1.20, 0.40, 1.00, "Obras"),
        new("BARREIRA", "Barreira plástica tipo New Jersey (2,00 m)", "un", 2.00, 0.50, 0.80, "Obras"),
        new("CILINDRO", "Cilindro delimitador", "un", 0.20, 0.20, 0.75, "Obras"),
        new("BALIZADOR", "Balizador", "un", 0.15, 0.15, 1.00, "Obras"),
        new("TELA", "Tela tapume (m)", "m", 1.00, 0.05, 1.20, "Obras"),
        new("LUZ", "Dispositivo luminoso (pisca)", "un", 0.20, 0.20, 0.30, "Obras"),
    };

    public static TipoDispositivo? Obter(string c) => Itens.FirstOrDefault(i => i.Codigo.Equals(c, StringComparison.OrdinalIgnoreCase));

    private static void Listras(Desenho2D d, double w, double h, int n)
    {
        Pincel.Retangulo(d, -w / 2, -h / 2, w / 2, h / 2, Cor.Branco);
        double p = w / n;
        for (int i = 0; i < n; i += 2) Pincel.Retangulo(d, -w / 2 + i * p, -h / 2, -w / 2 + (i + 1) * p, h / 2, Cor.Laranja);
        d.Linha(new[] { new V2(-w / 2, -h / 2), new V2(w / 2, -h / 2), new V2(w / 2, h / 2), new V2(-w / 2, h / 2) }, Cor.Preto, true);
    }

    /// <summary>Símbolo em planta (m, centrado; comprimento ao longo do fluxo em X).</summary>
    public static Desenho2D Planta(string codigo)
    {
        var d = new Desenho2D();
        switch (codigo.ToUpperInvariant())
        {
            case "CONE":
                d.Disco(V2.Zero, 0.20, Cor.Laranja);
                d.Disco(V2.Zero, 0.12, Cor.Branco);
                d.Disco(V2.Zero, 0.07, Cor.Laranja);
                break;
            case "TAMBOR":
                d.Disco(V2.Zero, 0.30, Cor.Laranja);
                d.Disco(V2.Zero, 0.22, Cor.Branco);
                d.Disco(V2.Zero, 0.15, Cor.Laranja);
                break;
            case "CAVALETE": Listras(d, 1.20, 0.25, 6); break;
            case "BARREIRA": Listras(d, 2.00, 0.50, 4); break;
            case "CILINDRO": d.Disco(V2.Zero, 0.10, Cor.Amarelo); d.Disco(V2.Zero, 0.05, Cor.Preto); break;
            case "BALIZADOR": Pincel.Retangulo(d, -0.075, -0.075, 0.075, 0.075, Cor.Laranja); break;
            case "TELA": Pincel.Retangulo(d, -0.5, -0.025, 0.5, 0.025, Cor.Laranja); break;
            case "LUZ": d.Disco(V2.Zero, 0.10, Cor.Amarelo); Pincel.Anel(d, V2.Zero, 0.16, 0.03, Cor.Laranja); break;
            default:
                Pincel.Retangulo(d, -0.2, -0.2, 0.2, 0.2, Cor.Cinza);
                break;
        }
        return d;
    }
}

/// <summary>Parâmetros de uma zona de obra com interdição de faixa.</summary>
public sealed class ParamZonaObra
{
    /// <summary>Distância (ao longo do caminho, no sentido do tráfego) do início da área de trabalho.</summary>
    public double InicioTrabalho { get; set; } = 150;
    public double ComprimentoTrabalho { get; set; } = 50;
    public double Velocidade { get; set; } = 60;
    public double LarguraFaixa { get; set; } = 3.5;
    /// <summary>Deslocamento do eixo da faixa interditada em relação ao caminho (positivo = esquerda).</summary>
    public double DeslocamentoFaixa { get; set; } = -1.75;
    /// <summary>True se a faixa interditada fica à direita das faixas que continuam abertas.</summary>
    public bool FechaDireita { get; set; } = true;
    public string Dispositivo { get; set; } = "CONE";
    public int NumPlacas { get; set; } = 3;
    public double? TaperManual { get; set; }
}

public sealed record ItemZona(string Tipo, string Codigo, double S, double Desloc, string Papel, string? Valor = null);

public sealed class ResultadoZona
{
    public List<ItemZona> Itens { get; } = new();
    public double Taper { get; set; }
    public double ZonaProtecao { get; set; }
    public double EspacamentoTaper { get; set; }
    public double EspacamentoLongitudinal { get; set; }
    public double DistanciaPlacas { get; set; }
    public double InicioSinalizacao { get; set; }
    public double FimSinalizacao { get; set; }
    public List<string> Avisos { get; } = new();
    public int Contar(string tipo) => Itens.Count(i => i.Tipo == tipo);
}

/// <summary>
/// Layout de interdição de faixa: placas de pré-sinalização, taper de canalização, zona de proteção,
/// área de trabalho com dispositivos e taper de saída. Distâncias medidas ao longo do caminho.
/// </summary>
public static class ZonaObra
{
    public static ResultadoZona Calcular(double comprimentoCaminho, ParamZonaObra p, N n)
    {
        var r = new ResultadoZona();
        double v = Math.Max(20, p.Velocidade);
        r.Taper = p.TaperManual ?? Math.Max(n.TaperMinimo, n.FatorTaper * v * p.LarguraFaixa);
        r.ZonaProtecao = N.PorVelocidade(n.ZonaProtecao, v);
        r.EspacamentoTaper = Math.Max(2, v / Math.Max(1, n.DivisorEspacamentoCones));
        r.EspacamentoLongitudinal = r.EspacamentoTaper * n.FatorEspacamentoLongitudinal;
        r.DistanciaPlacas = Math.Max(n.DistanciaMinimaPlacasObra, n.FatorDistanciaPlacasObra * v);

        double w0 = p.InicioTrabalho, w1 = p.InicioTrabalho + p.ComprimentoTrabalho;
        double b0 = w0 - r.ZonaProtecao;
        double t0 = b0 - r.Taper;
        double sinal = p.FechaDireita ? 1 : -1; // lado das faixas abertas em relação à faixa fechada
        double bordaExterna = p.DeslocamentoFaixa - sinal * p.LarguraFaixa / 2;
        double bordaInterna = p.DeslocamentoFaixa + sinal * p.LarguraFaixa / 2;

        // taper de entrada
        int nt = Math.Max(2, (int)Math.Round(r.Taper / r.EspacamentoTaper));
        for (int i = 0; i <= nt; i++)
        {
            double f = i / (double)nt;
            r.Itens.Add(new ItemZona("DISP", p.Dispositivo, t0 + f * r.Taper, bordaExterna + f * (bordaInterna - bordaExterna), "Taper de entrada"));
        }
        // zona de proteção + área de trabalho
        for (double s = b0 + r.EspacamentoLongitudinal; s < w1 - 1e-6; s += r.EspacamentoLongitudinal)
            r.Itens.Add(new ItemZona("DISP", p.Dispositivo, s, bordaInterna, s < w0 ? "Zona de proteção" : "Área de trabalho"));
        // taper de saída
        int ns = Math.Max(2, (int)Math.Round(n.TaperSaida / r.EspacamentoTaper));
        for (int i = 0; i <= ns; i++)
        {
            double f = i / (double)ns;
            r.Itens.Add(new ItemZona("DISP", p.Dispositivo, w1 + f * n.TaperSaida, bordaInterna + f * (bordaExterna - bordaInterna), "Taper de saída"));
        }
        // placas de pré-sinalização (da mais distante para a mais próxima), no lado da faixa fechada
        double ladoPlaca = bordaExterna - sinal * 0.8;
        var seq = new List<(string cod, string? val, string papel)>
        {
            ("A-24", null, "Obras"),
            ("R-19", Math.Max(30, Math.Round((v - 20) / 10) * 10).ToString(Num.Inv), "Velocidade reduzida"),
            (p.FechaDireita ? "A-21c" : "A-21b", null, "Estreitamento de pista"),
        };
        int np = Math.Clamp(p.NumPlacas, 1, seq.Count);
        for (int i = 0; i < np; i++)
        {
            var (cod, val, papel) = seq[i];
            double s = t0 - (np - i) * r.DistanciaPlacas;
            r.Itens.Add(new ItemZona("PLACA", cod, s, ladoPlaca, papel, val));
        }
        r.InicioSinalizacao = t0 - np * r.DistanciaPlacas;
        r.FimSinalizacao = w1 + n.TaperSaida;
        if (r.InicioSinalizacao < 0) r.Avisos.Add($"A pré-sinalização começa {Num.F(-r.InicioSinalizacao, 1)} m antes do início do caminho: estenda o caminho ou mova a obra.");
        if (r.FimSinalizacao > comprimentoCaminho) r.Avisos.Add($"O taper de saída passa {Num.F(r.FimSinalizacao - comprimentoCaminho, 1)} m do fim do caminho.");
        return r;
    }
}
