using System.ComponentModel;
using System.Text.Json.Serialization;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;

namespace Projeta.Giro.Core.Veiculos;

public enum TipoUnidade
{
    [Description("Unidade motriz (caminhão, ônibus, cavalo mecânico, automóvel)")] Motriz,
    [Description("Semirreboque (apoiado no pino-rei)")] Semirreboque,
    [Description("Dolly / barra de tração")] Dolly,
    [Description("Reboque (apoiado no dolly)")] Reboque,
}

/// <summary>Eixo de uma unidade. X no referencial da unidade (origem no ponto guia, x para a frente).</summary>
public sealed class Eixo
{
    [DisplayName("Posição X (m)"), Description("Posição longitudinal no referencial da unidade: 0 = ponto guia (eixo dianteiro ou engate), negativo para trás.")]
    public double X { get; set; }

    [DisplayName("Bitola externa (m)"), Description("Distância entre as faces externas dos pneus do eixo.")]
    public double Bitola { get; set; } = 2.6;

    [DisplayName("Rodado duplo")] public bool Duplo { get; set; }
    [DisplayName("Largura do pneu (m)")] public double LarguraPneu { get; set; } = 0.30;
    [DisplayName("Diâmetro da roda (m)")] public double DiametroRoda { get; set; } = 1.00;

    [JsonIgnore, Browsable(false)] public double LarguraRodado => Duplo ? 2 * LarguraPneu + 0.05 : LarguraPneu;
    public override string ToString() => $"x = {Num.F(X)} m, bitola {Num.F(Bitola)} m{(Duplo ? ", duplo" : "")}";
}

/// <summary>
/// Unidade de um veículo (corpo rígido). Referencial próprio: origem no ponto guia — eixo dianteiro (direcional) da unidade motriz,
/// ou ponto de engate (pino-rei, olhal da barra, centro da mesa giratória) dos reboques; eixo x para a frente.
/// O eixo efetivo (traseiro) fica em x = −EntreEixos. O engate da unidade seguinte fica em x = −(EntreEixos + Engate).
/// </summary>
public sealed class Unidade
{
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "Unidade";
    [Category("Identificação"), DisplayName("Tipo")] public TipoUnidade Tipo { get; set; } = TipoUnidade.Motriz;

    [Category("Dimensões"), DisplayName("Largura (m)")] public double Largura { get; set; } = 2.6;
    [Category("Dimensões"), DisplayName("Balanço dianteiro (m)"), Description("Da frente da carroceria até o ponto guia (eixo dianteiro ou engate).")]
    public double BalancoDianteiro { get; set; } = 1.2;
    [Category("Dimensões"), DisplayName("Entre-eixos efetivo (m)"), Description("Do ponto guia ao eixo traseiro efetivo (centro do conjunto em tandem).")]
    public double EntreEixos { get; set; } = 6.1;
    [Category("Dimensões"), DisplayName("Balanço traseiro (m)"), Description("Do eixo traseiro efetivo até a traseira da carroceria.")]
    public double BalancoTraseiro { get; set; } = 1.8;
    [Category("Engate"), DisplayName("Posição do engate da unidade seguinte (m)"), Description("Distância do eixo efetivo até o engate da próxima unidade: positivo = atrás do eixo, negativo = à frente.")]
    public double Engate { get; set; }
    [Category("Dimensões"), DisplayName("Desenhar como barra"), Description("Unidade sem carroceria (barra de tração do dolly).")]
    public bool Barra { get; set; }

    [Category("Vão livre vertical"), DisplayName("Vão livre entre eixos (m)")] public double VaoLivreEntreEixos { get; set; } = 0.30;
    [Category("Vão livre vertical"), DisplayName("Vão livre na frente (m)")] public double VaoLivreDianteiro { get; set; } = 0.40;
    [Category("Vão livre vertical"), DisplayName("Vão livre na traseira (m)")] public double VaoLivreTraseiro { get; set; } = 0.50;

    [Category("Eixos"), DisplayName("Eixos")] public List<Eixo> Eixos { get; set; } = new();

    [JsonIgnore, Browsable(false)] public double XFrente => BalancoDianteiro;
    [JsonIgnore, Browsable(false)] public double XTras => -(EntreEixos + BalancoTraseiro);
    [JsonIgnore, Browsable(false)] public double XEngate => -(EntreEixos + Engate);
    [JsonIgnore, Browsable(false)] public double BitolaMax => Eixos.Count == 0 ? Largura : Eixos.Max(e => e.Bitola);
    public override string ToString() => $"{Nome} ({Tipo})";
}

/// <summary>Veículo de projeto: uma unidade motriz seguida de zero ou mais unidades rebocadas.</summary>
public sealed class Veiculo
{
    [Category("Identificação"), DisplayName("Código")] public string Id { get; set; } = "NOVO";
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "Novo veículo";
    [Category("Identificação"), DisplayName("Fonte")] public string Fonte { get; set; } = "";
    [Category("Identificação"), DisplayName("Observações")] public string Observacoes { get; set; } = "";

    [Category("Giro"), DisplayName("Raio mín. da roda externa dianteira (m)"),
     Description("Valor de projeto (ex.: DNIT IPR-718, Tabela 6). Calibra o esterçamento máximo quando o ângulo não é informado.")]
    public double? RaioMinimoRodaExterna { get; set; }
    [Category("Giro"), DisplayName("Esterçamento máximo (graus)"), Description("Opcional: se informado, prevalece sobre o raio mínimo.")]
    public double? AnguloMaximoGraus { get; set; }
    [Category("Giro"), DisplayName("Tempo batente a batente (s)"), Description("Tempo para girar o volante de um batente ao outro. Define as transições de esterçamento.")]
    public double TempoBatente { get; set; } = 6.0;

    [Category("Giro"), DisplayName("Articulação máxima (graus)"),
     Description("Ângulo máximo entre unidades antes de contato cabine/carroceria (L dobrado). Valor padrão é premissa — confira com o fabricante.")]
    public double ArticulacaoMaximaGraus { get; set; } = 80;

    [Category("Referência"), DisplayName("Raio mín. roda interna traseira — referência (m)")] public double? RefRaioRodaInterna { get; set; }
    [Category("Referência"), DisplayName("Raio máx. do balanço dianteiro — referência (m)")] public double? RefRaioBalanco { get; set; }
    [Category("Referência"), DisplayName("Comprimento total — referência (m)")] public double? RefComprimento { get; set; }

    [Browsable(false)] public List<Unidade> Unidades { get; set; } = new();

    [JsonIgnore, Browsable(false)] public Unidade Motriz => Unidades[0];
    [JsonIgnore, Browsable(false)] public bool Articulado => Unidades.Count > 1;

    /// <summary>Bitola externa do eixo direcional (x = 0 da unidade motriz).</summary>
    [JsonIgnore, Browsable(false)]
    public double BitolaDianteira => Motriz.Eixos.Count == 0 ? Motriz.Largura
        : Motriz.Eixos.OrderBy(e => Math.Abs(e.X)).First().Bitola;

    /// <summary>Esterçamento máximo do eixo dianteiro (rad), equivalente de bicicleta.</summary>
    [JsonIgnore, Browsable(false)]
    public double AnguloMaximo
    {
        get
        {
            if (AnguloMaximoGraus is > 0 and < 89) return Num.Rad(AnguloMaximoGraus.Value);
            double l = Motriz.EntreEixos;
            if (RaioMinimoRodaExterna is double r && r > l)
            {
                double r1 = Math.Sqrt(r * r - l * l) - BitolaDianteira / 2;
                if (r1 > 0.1) return Math.Atan(l / r1);
            }
            return Num.Rad(35);
        }
    }

    /// <summary>Raio mínimo da trajetória do centro do eixo dianteiro (m).</summary>
    [JsonIgnore, Browsable(false)] public double RaioMinimoDianteiro => Motriz.EntreEixos / Math.Sin(AnguloMaximo);

    /// <summary>Comprimento total em linha reta (m).</summary>
    [JsonIgnore, Browsable(false)]
    public double Comprimento
    {
        get
        {
            double xFrente = Motriz.XFrente, xTras = double.MaxValue, base0 = 0;
            for (int i = 0; i < Unidades.Count; i++)
            {
                var u = Unidades[i];
                xTras = Math.Min(xTras, base0 + u.XTras);
                if (i > 0) xFrente = Math.Max(xFrente, base0 + u.XFrente);
                base0 += u.XEngate;
            }
            return xFrente - xTras;
        }
    }

    [JsonIgnore, Browsable(false)] public double Largura => Unidades.Max(u => u.Largura);

    public List<Mensagem> Validar()
    {
        var m = new List<Mensagem>();
        if (Unidades.Count == 0) { m.Add(new(Gravidade.Erro, "o veículo não tem unidades.")); return m; }
        if (Motriz.Tipo != TipoUnidade.Motriz) m.Add(new(Gravidade.Aviso, "a primeira unidade deveria ser do tipo Motriz."));
        for (int i = 0; i < Unidades.Count; i++)
        {
            var u = Unidades[i];
            if (u.EntreEixos <= 0.1) m.Add(new(Gravidade.Erro, $"{u.Nome}: entre-eixos deve ser maior que zero."));
            if (u.Largura <= 0) m.Add(new(Gravidade.Erro, $"{u.Nome}: largura inválida."));
            if (u.BalancoDianteiro < 0 || u.BalancoTraseiro < 0) m.Add(new(Gravidade.Erro, $"{u.Nome}: balanços não podem ser negativos."));
            if (i > 0 && u.Tipo == TipoUnidade.Motriz) m.Add(new(Gravidade.Aviso, $"{u.Nome}: só a primeira unidade pode ser motriz."));
        }
        double a = Num.Graus(AnguloMaximo);
        if (a < 5 || a > 70) m.Add(new(Gravidade.Aviso, $"esterçamento máximo de {Num.F(a, 1)}° parece fora da faixa usual."));
        if (TempoBatente < 0) m.Add(new(Gravidade.Erro, "tempo batente a batente não pode ser negativo."));
        return m;
    }

    /// <summary>Raios do giro estático com esterçamento máximo (para comparar com os gabaritos de referência).</summary>
    public RaiosEstaticos Estatico(double? angulo = null)
    {
        double d = angulo ?? AnguloMaximo;
        var m = Motriz;
        double l = m.EntreEixos;
        double r = l / Math.Tan(d); // raio do centro do eixo traseiro efetivo
        double b = BitolaDianteira;
        double rodaExt = Math.Sqrt(l * l + Math.Pow(r + b / 2, 2));
        double balanco = Math.Sqrt(Math.Pow(l + m.BalancoDianteiro, 2) + Math.Pow(r + m.Largura / 2, 2));
        double rEixo = r;
        double rodaInt = RodaInterna(m, r);
        var raiosEixos = new List<double> { r };
        for (int i = 1; i < Unidades.Count; i++)
        {
            double e = Unidades[i - 1].Engate;
            double rEngate = Math.Sqrt(rEixo * rEixo + e * e);
            double li = Unidades[i].EntreEixos;
            if (rEngate <= li) { rEixo = 0; raiosEixos.Add(0); rodaInt = 0; break; }
            rEixo = Math.Sqrt(rEngate * rEngate - li * li);
            raiosEixos.Add(rEixo);
            rodaInt = RodaInterna(Unidades[i], rEixo);
        }
        return new RaiosEstaticos(Num.Graus(d), l / Math.Sin(d), rodaExt, balanco, rodaInt, raiosEixos, balanco - rodaInt);
    }

    private static double RodaInterna(Unidade u, double rEixo)
    {
        if (u.Eixos.Count == 0) return rEixo - u.Largura / 2;
        // centro de giro sobre a linha do eixo efetivo (x = -EntreEixos), a rEixo para o lado interno
        return u.Eixos.Min(e => Math.Sqrt(Math.Pow(e.X + u.EntreEixos, 2) + Math.Pow(rEixo - e.Bitola / 2, 2)));
    }

    public override string ToString() => $"{Id} — {Nome}";
}

/// <param name="Angulo">Esterçamento usado (graus).</param>
/// <param name="EixoDianteiro">Raio do centro do eixo dianteiro (m).</param>
/// <param name="RodaExternaDianteira">Raio da face externa da roda externa dianteira (m).</param>
/// <param name="BalancoDianteiro">Raio do canto externo do balanço dianteiro (m).</param>
/// <param name="RodaInternaTraseira">Raio da face interna da roda interna traseira da última unidade (m).</param>
/// <param name="EixosEfetivos">Raios dos eixos efetivos de cada unidade (m).</param>
/// <param name="LarguraVarrida">Largura da faixa varrida no giro estático (m).</param>
public sealed record RaiosEstaticos(double Angulo, double EixoDianteiro, double RodaExternaDianteira, double BalancoDianteiro,
    double RodaInternaTraseira, List<double> EixosEfetivos, double LarguraVarrida);
