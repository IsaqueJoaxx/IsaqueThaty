using System.ComponentModel;
using Projeta.Core.Comum;
using Projeta.Core.Hidraulica;
using Projeta.Core.Hidrologia;
using Projeta.Core.Modelador;

namespace Projeta.Core.Longitudinais;

public enum OrigemCota
{
    [Description("Perfil (greide) do alinhamento")] Perfil,
    [Description("Superfície (terreno)")] Superficie,
}

/// <summary>Dispositivo longitudinal (valeta, sarjeta, meio-fio, canaleta) lançado ao longo de um alinhamento.</summary>
public sealed class DispositivoLongitudinal
{
    [Category("Identificação"), DisplayName("Id"), ReadOnly(true)] public string Id { get; set; } = Redes.Projeto.NovoId();
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "VL-01";
    [Category("Seção"), DisplayName("Seção-tipo")] public string SecaoId { get; set; } = "VALETA_TRAPEZOIDAL";
    [Category("Seção"), DisplayName("Parâmetros da seção"), TypeConverter(typeof(DicionarioNumerosConverter))] public Dictionary<string, double> Parametros { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    [Category("Seção"), DisplayName("Espelhar seção (lado esquerdo)")] public bool Espelhar { get; set; }

    [Category("Referência"), Browsable(false)] public string? HandleAlinhamento { get; set; }
    [Category("Referência"), DisplayName("Alinhamento"), ReadOnly(true)] public string? NomeAlinhamento { get; set; }
    [Category("Referência"), Browsable(false)] public string? HandlePerfil { get; set; }
    [Category("Referência"), DisplayName("Perfil"), ReadOnly(true)] public string? NomePerfil { get; set; }
    [Category("Referência"), Browsable(false)] public string? HandleSuperficie { get; set; }
    [Category("Referência"), DisplayName("Superfície"), ReadOnly(true)] public string? NomeSuperficie { get; set; }
    [Category("Referência"), DisplayName("Cota a partir de")] public OrigemCota OrigemCota { get; set; } = OrigemCota.Perfil;

    [Category("Locação"), DisplayName("Estaca inicial (m)")] public double EstacaInicial { get; set; }
    [Category("Locação"), DisplayName("Estaca final (m)")] public double EstacaFinal { get; set; }
    [Category("Locação"), DisplayName("Afastamento (m, + direita)")] public double Afastamento { get; set; } = 4.0;
    [Category("Locação"), DisplayName("Desnível em relação à referência (m)")] public double Desnivel { get; set; }
    [Category("Locação"), DisplayName("Intervalo de amostragem (m)")] public double Intervalo { get; set; } = 5.0;

    [Category("Hidráulica"), DisplayName("Manning n")] public double Manning { get; set; } = 0.015;
    [Category("Hidráulica"), DisplayName("Lâmina máxima (fração da altura)")] public double LaminaMaxima { get; set; } = 0.80;
    [Category("Hidráulica"), DisplayName("Largura de contribuição (m)")] public double LarguraContribuicao { get; set; } = 10.0;
    [Category("Hidráulica"), DisplayName("Coeficiente de escoamento C")] public double C { get; set; } = 0.80;
    [Category("Hidráulica"), DisplayName("Equação de chuva (Id)")] public string EquacaoId { get; set; } = "BH-UFU2024";
    [Category("Hidráulica"), DisplayName("Tempo de retorno (anos)")] public double TempoRetorno { get; set; } = 10;
    [Category("Hidráulica"), DisplayName("Duração / tc (min)")] public double Tc { get; set; } = 5;

    [Browsable(false)] public List<string> HandlesDesenho { get; set; } = new();
    [Browsable(false)] public ResultadoLongitudinal? Resultado { get; set; }
    public override string ToString() => Nome;
}

/// <summary>Amostra ao longo do eixo do dispositivo.</summary>
public sealed record AmostraLongitudinal(double Estaca, double X, double Y, double Z, double DirX, double DirY);

public sealed class SegmentoLongitudinal
{
    public double EstacaInicial { get; set; }
    public double EstacaFinal { get; set; }
    public double Comprimento { get; set; }
    public double Declividade { get; set; }
    public double Capacidade { get; set; }
    public double Velocidade { get; set; }
    public double ComprimentoCritico { get; set; }
}

public sealed class ResultadoLongitudinal
{
    public double ComprimentoHorizontal { get; set; }
    public double Comprimento3D { get; set; }
    public double AreaConcreto { get; set; }
    public double VolumeConcreto { get; set; }
    public double VolumeEscavacao { get; set; }
    public double AreaRevestida { get; set; }
    public double DeclividadeMinima { get; set; }
    public double DeclividadeMaxima { get; set; }
    public double CapacidadeMinima { get; set; }
    public double ComprimentoCriticoMinimo { get; set; }
    public double VazaoPorMetro { get; set; }
    public List<SegmentoLongitudinal> Segmentos { get; set; } = new();
    public List<Mensagem> Mensagens { get; set; } = new();
}

public static class CalculoLongitudinal
{
    /// <summary>
    /// Calcula quantitativos e verificação hidráulica a partir das amostras (estaca, x, y, cota do fundo).
    /// A capacidade usa Manning com a lâmina máxima; o comprimento crítico usa q = C·i·Lc/3,6e6 (m³/s por metro).
    /// </summary>
    public static ResultadoLongitudinal Calcular(DispositivoLongitudinal d, AvaliadorDispositivo.SecaoAvaliada secao,
        IReadOnlyList<AmostraLongitudinal> amostras, EquacaoChuva? eq)
    {
        var r = new ResultadoLongitudinal { AreaConcreto = secao.AreaConcreto };
        r.Mensagens.AddRange(secao.Mensagens);
        if (amostras.Count < 2) { r.Mensagens.Add(new(Gravidade.Erro, "Menos de duas amostras no trecho")); return r; }

        double i = eq is null ? double.NaN : eq.Intensidade(d.TempoRetorno, Math.Max(d.Tc, 1));
        r.VazaoPorMetro = double.IsNaN(i) ? double.NaN : d.C * i * d.LarguraContribuicao / 3.6e6;
        var hid = secao.Hidraulica;
        double yMax = hid is null ? 0 : hid.Altura * d.LaminaMaxima;
        double sMin = double.MaxValue, sMax = double.MinValue, capMin = double.MaxValue, lcMin = double.MaxValue;

        for (int k = 0; k < amostras.Count - 1; k++)
        {
            var a = amostras[k]; var b = amostras[k + 1];
            double lh = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            if (lh < 1e-6) continue;
            double dz = b.Z - a.Z;
            double l3 = Math.Sqrt(lh * lh + dz * dz);
            r.ComprimentoHorizontal += lh;
            r.Comprimento3D += l3;
            double s = Math.Abs(dz) / lh;
            sMin = Math.Min(sMin, s); sMax = Math.Max(sMax, s);
            var seg = new SegmentoLongitudinal { EstacaInicial = a.Estaca, EstacaFinal = b.Estaca, Comprimento = l3, Declividade = s };
            if (hid is not null && yMax > 0)
            {
                double sCalc = Math.Max(s, 1e-4);
                seg.Capacidade = Manning.Vazao(hid, yMax, d.Manning, sCalc);
                seg.Velocidade = seg.Capacidade / Math.Max(hid.Area(yMax), 1e-9);
                seg.ComprimentoCritico = double.IsNaN(r.VazaoPorMetro) || r.VazaoPorMetro <= 0 ? double.NaN : seg.Capacidade / r.VazaoPorMetro;
                capMin = Math.Min(capMin, seg.Capacidade);
                if (!double.IsNaN(seg.ComprimentoCritico)) lcMin = Math.Min(lcMin, seg.ComprimentoCritico);
            }
            r.Segmentos.Add(seg);
        }
        r.VolumeConcreto = secao.AreaConcreto * r.Comprimento3D;
        r.VolumeEscavacao = secao.AreaEscavacao * r.Comprimento3D;
        r.AreaRevestida = secao.PerimetroRevestido * r.Comprimento3D;
        r.DeclividadeMinima = sMin == double.MaxValue ? 0 : sMin;
        r.DeclividadeMaxima = sMax == double.MinValue ? 0 : sMax;
        r.CapacidadeMinima = capMin == double.MaxValue ? double.NaN : capMin;
        r.ComprimentoCriticoMinimo = lcMin == double.MaxValue ? double.NaN : lcMin;

        if (hid is not null && r.DeclividadeMinima < 0.0025)
            r.Mensagens.Add(new(Gravidade.Aviso, $"Declividade mínima de {r.DeclividadeMinima * 100:0.00}% — risco de empoçamento"));
        if (hid is not null && !double.IsNaN(r.ComprimentoCriticoMinimo) && r.ComprimentoCriticoMinimo < r.ComprimentoHorizontal)
            r.Mensagens.Add(new(Gravidade.Aviso, $"Comprimento crítico {r.ComprimentoCriticoMinimo:0.0} m menor que a extensão ({r.ComprimentoHorizontal:0.0} m): prever saídas/descidas d'água"));
        var vMax = r.Segmentos.Select(sg => sg.Velocidade).DefaultIfEmpty(0).Max();
        if (vMax > 4.5) r.Mensagens.Add(new(Gravidade.Aviso, $"Velocidade de até {vMax:0.00} m/s com a seção cheia — verifique dissipação/revestimento"));
        return r;
    }

    /// <summary>
    /// Transforma um ponto da seção (offset, cota relativa) em coordenada 3D, dada a amostra (ponto do eixo do dispositivo).
    /// X da seção cresce para a direita do sentido do alinhamento (ou para a esquerda, se espelhada).
    /// </summary>
    public static P3 PontoSecao(AmostraLongitudinal a, P2 pontoSecao, bool espelhar)
    {
        var dir = new P2(a.DirX, a.DirY).Unitario();
        var direita = new P2(dir.Y, -dir.X); // perpendicular à direita
        double off = espelhar ? -pontoSecao.X : pontoSecao.X;
        return new P3(a.X + direita.X * off, a.Y + direita.Y * off, a.Z + pontoSecao.Y);
    }
}
