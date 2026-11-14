using System.ComponentModel;
using System.Text.Json.Serialization;
using Projeta.Core.Comum;
using Projeta.Core.Hidrologia;

namespace Projeta.Core.Redes;

public enum TipoRede { Drenagem, Esgoto, Agua, Generica }

public enum TipoNo
{
    [Description("Poço de visita")] PV,
    [Description("Boca de lobo")] BocaDeLobo,
    [Description("Caixa de ligação/inspeção")] Caixa,
    [Description("Exutório / lançamento")] Exutorio,
    [Description("Junção (água)")] Juncao,
    [Description("Reservatório (água)")] Reservatorio,
    [Description("Tanque (água)")] Tanque,
    [Description("Genérico")] Generico,
}

public enum FormaSecao { Circular, Retangular }

public enum CriterioConcordancia
{
    [Description("Geratriz superior (topo dos tubos)")] GeratrizSuperior,
    [Description("Geratriz inferior (fundo dos tubos)")] GeratrizInferior,
    [Description("Lâmina d'água")] LaminaDagua,
}

/// <summary>Todo o conteúdo do PROJETA salvo dentro do DWG.</summary>
public sealed class Projeto
{
    public int Versao { get; set; } = 1;
    public List<Rede> Redes { get; set; } = new();
    public List<EquacaoChuva> Equacoes { get; set; } = new();
    public List<Longitudinais.DispositivoLongitudinal> Longitudinais { get; set; } = new();
    public Dictionary<string, string> Extras { get; set; } = new();

    public Rede? RedePorId(string? id) => id is null ? null : Redes.FirstOrDefault(r => r.Id == id);
    public Rede? RedePorNome(string nome) => Redes.FirstOrDefault(r => string.Equals(r.Nome, nome, StringComparison.OrdinalIgnoreCase));

    /// <summary>Equações do catálogo padrão + as do projeto (as do projeto prevalecem pelo Id).</summary>
    public List<EquacaoChuva> TodasEquacoes()
    {
        var lista = CatalogoEquacoes.Padrao();
        foreach (var e in Equacoes)
        {
            lista.RemoveAll(x => x.Id == e.Id);
            lista.Add(e);
        }
        return lista;
    }

    public EquacaoChuva? Equacao(string? id) => id is null ? null : TodasEquacoes().FirstOrDefault(e => e.Id == id);

    public static string NovoId() => Guid.NewGuid().ToString("N")[..12];
}

public sealed class Rede
{
    [Category("Identificação"), DisplayName("Id"), ReadOnly(true)] public string Id { get; set; } = Projeto.NovoId();
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "Rede 1";
    [Category("Identificação"), DisplayName("Tipo"), ReadOnly(true)] public TipoRede Tipo { get; set; } = TipoRede.Drenagem;
    [Category("Identificação"), DisplayName("Descrição")] public string Descricao { get; set; } = "";

    [Category("Referência"), DisplayName("Alinhamento (handle)"), Browsable(false)] public string? HandleAlinhamento { get; set; }
    [Category("Referência"), DisplayName("Superfície (handle)"), Browsable(false)] public string? HandleSuperficie { get; set; }
    [Category("Referência"), DisplayName("Alinhamento")] public string? NomeAlinhamento { get; set; }
    [Category("Referência"), DisplayName("Superfície")] public string? NomeSuperficie { get; set; }

    [Category("Numeração"), DisplayName("Prefixo dos nós")] public string PrefixoNo { get; set; } = "PV-";
    [Category("Numeração"), DisplayName("Prefixo das bocas de lobo")] public string PrefixoBocaLobo { get; set; } = "BL-";
    [Category("Numeração"), DisplayName("Prefixo dos trechos")] public string PrefixoTrecho { get; set; } = "T-";
    [Category("Numeração"), DisplayName("Dígitos")] public int Digitos { get; set; } = 2;

    [Category("Dispositivos"), DisplayName("Dispositivo padrão dos PVs")] public string DispositivoPadraoPV { get; set; } = "PV_CIRCULAR";
    [Category("Dispositivos"), DisplayName("Dispositivo padrão das bocas de lobo")] public string DispositivoPadraoBL { get; set; } = "BOCA_LOBO_SIMPLES";
    [Category("Dispositivos"), DisplayName("Abrir furos nos PVs para os tubos")] public bool AbrirFurosNosPVs { get; set; } = true;

    [Category("Desenho"), DisplayName("Altura do texto (m)")] public double AlturaTexto { get; set; } = 1.0;
    [Category("Desenho"), DisplayName("Gerar sólidos 3D")] public bool Gerar3D { get; set; } = true;
    [Category("Desenho"), DisplayName("Prefixo de layers")] public string PrefixoLayer { get; set; } = "PRJ-DRE";

    [Browsable(false)] public ParametrosDrenagem Drenagem { get; set; } = new();
    [Browsable(false)] public ParametrosEsgoto Esgoto { get; set; } = new();
    [Browsable(false)] public ParametrosAgua Agua { get; set; } = new();

    [Browsable(false)] public List<No> Nos { get; set; } = new();
    [Browsable(false)] public List<Trecho> Trechos { get; set; } = new();
    [Browsable(false)] public List<Bacia> Bacias { get; set; } = new();

    [Browsable(false)] public Dimensionamento.ResultadoRede? Resultado { get; set; }

    /// <summary>Handles de entidades auxiliares (tabelas, projeções em perfil...).</summary>
    [Browsable(false)] public Dictionary<string, List<string>> HandlesAuxiliares { get; set; } = new();

    public No? NoPorId(string? id) => id is null ? null : Nos.FirstOrDefault(n => n.Id == id);
    public No? NoPorNome(string nome) => Nos.FirstOrDefault(n => string.Equals(n.Nome, nome, StringComparison.OrdinalIgnoreCase));
    public Trecho? TrechoPorId(string? id) => id is null ? null : Trechos.FirstOrDefault(t => t.Id == id);
    public Bacia? BaciaPorId(string? id) => id is null ? null : Bacias.FirstOrDefault(b => b.Id == id);

    public IEnumerable<Trecho> TrechosQueChegam(string noId) => Trechos.Where(t => t.NoJusanteId == noId);
    public IEnumerable<Trecho> TrechosQueSaem(string noId) => Trechos.Where(t => t.NoMontanteId == noId);

    public double ComprimentoHorizontal(Trecho t)
    {
        if (t.ComprimentoForcado is > 0) return t.ComprimentoForcado.Value;
        var a = NoPorId(t.NoMontanteId); var b = NoPorId(t.NoJusanteId);
        if (a is null || b is null) return 0;
        return new P2(a.X, a.Y).Distancia(new P2(b.X, b.Y));
    }

    public No NovoNo(TipoNo tipo, double x, double y)
    {
        var n = new No
        {
            Tipo = tipo, X = x, Y = y,
            DispositivoId = tipo == TipoNo.BocaDeLobo ? DispositivoPadraoBL : tipo == TipoNo.Exutorio ? "EXUTORIO" : DispositivoPadraoPV,
        };
        n.Nome = ProximoNome(tipo == TipoNo.BocaDeLobo ? PrefixoBocaLobo : PrefixoNo, Nos.Select(k => k.Nome));
        Nos.Add(n);
        return n;
    }

    public Trecho NovoTrecho(No montante, No jusante)
    {
        var t = new Trecho { NoMontanteId = montante.Id, NoJusanteId = jusante.Id };
        t.Nome = ProximoNome(PrefixoTrecho, Trechos.Select(k => k.Nome));
        Trechos.Add(t);
        return t;
    }

    public void RemoverNo(string noId)
    {
        Nos.RemoveAll(n => n.Id == noId);
        Trechos.RemoveAll(t => t.NoMontanteId == noId || t.NoJusanteId == noId);
        foreach (var b in Bacias.Where(b => b.NoId == noId)) b.NoId = null;
    }

    public string ProximoNome(string prefixo, IEnumerable<string> existentes)
    {
        var usados = new HashSet<string>(existentes, StringComparer.OrdinalIgnoreCase);
        for (int i = 1; ; i++)
        {
            var nome = prefixo + i.ToString(new string('0', Math.Max(1, Digitos)));
            if (!usados.Contains(nome)) return nome;
        }
    }
}

public sealed class No
{
    [Category("Identificação"), DisplayName("Id"), ReadOnly(true)] public string Id { get; set; } = Projeto.NovoId();
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "";
    [Category("Identificação"), DisplayName("Tipo")] public TipoNo Tipo { get; set; } = TipoNo.PV;

    [Category("Locação"), DisplayName("X (E)")] public double X { get; set; }
    [Category("Locação"), DisplayName("Y (N)")] public double Y { get; set; }
    [Category("Locação"), DisplayName("Rotação (graus)")] public double Rotacao { get; set; }
    [Category("Locação"), DisplayName("Cota do terreno (superfície)"), ReadOnly(true)] public double CotaTerreno { get; set; } = double.NaN;
    [Category("Locação"), DisplayName("Estaca"), ReadOnly(true)] public double Estaca { get; set; } = double.NaN;
    [Category("Locação"), DisplayName("Afastamento"), ReadOnly(true)] public double Afastamento { get; set; } = double.NaN;

    [Category("Cotas forçadas"), DisplayName("Cota da tampa forçada")] public double? CotaTampaForcada { get; set; }
    [Category("Cotas forçadas"), DisplayName("Cota de fundo forçada (exutório)")] public double? CotaFundoForcada { get; set; }
    [Category("Cotas forçadas"), DisplayName("Rebaixo do fundo (m)"), Description("Profundidade extra do fundo abaixo do tubo mais baixo (poço/caixa de areia)")]
    public double Rebaixo { get; set; }

    [Category("Dispositivo"), DisplayName("Dispositivo (modelador)")] public string DispositivoId { get; set; } = "PV_CIRCULAR";
    [Category("Dispositivo"), DisplayName("Parâmetros do dispositivo"), TypeConverter(typeof(DicionarioNumerosConverter))] public Dictionary<string, double> ParametrosDispositivo { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Category("Vazões"), DisplayName("Vazão adicional (L/s)"), Description("Drenagem: vazão pontual somada no nó")] public double VazaoAdicional { get; set; }
    [Category("Vazões"), DisplayName("Contribuição singular (L/s)"), Description("Esgoto: contribuição concentrada")] public double ContribuicaoSingular { get; set; }
    [Category("Vazões"), DisplayName("Demanda base (L/s)"), Description("Água: demanda no nó (somada à distribuída)")] public double Demanda { get; set; }
    [Category("Vazões"), DisplayName("Carga / nível (m)"), Description("Água: nível do reservatório ou cota do tanque")] public double Carga { get; set; }

    [Browsable(false)] public string? HandleBloco { get; set; }
    [Browsable(false)] public string? HandleRotulo { get; set; }
    [Browsable(false)] public string? HandleSolido { get; set; }

    [JsonIgnore, Browsable(false)] public P2 XY => new(X, Y);

    public double CotaTampa => CotaTampaForcada ?? CotaTerreno;
    public override string ToString() => Nome;
}

public sealed class Trecho
{
    [Category("Identificação"), DisplayName("Id"), ReadOnly(true)] public string Id { get; set; } = Projeto.NovoId();
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "";
    [Category("Conexão"), DisplayName("Nó de montante"), ReadOnly(true)] public string NoMontanteId { get; set; } = "";
    [Category("Conexão"), DisplayName("Nó de jusante"), ReadOnly(true)] public string NoJusanteId { get; set; } = "";

    [Category("Seção"), DisplayName("Forma")] public FormaSecao Forma { get; set; } = FormaSecao.Circular;
    [Category("Seção"), DisplayName("Diâmetro / largura forçado (m)")] public double? DiametroForcado { get; set; }
    [Category("Seção"), DisplayName("Altura forçada (galeria, m)")] public double? AlturaForcada { get; set; }
    [Category("Seção"), DisplayName("Nº de células/linhas")] public int Celulas { get; set; } = 1;
    [Category("Seção"), DisplayName("Material")] public string Material { get; set; } = "Concreto";
    [Category("Seção"), DisplayName("Manning (vazio = padrão da rede)")] public double? Manning { get; set; }
    [Category("Seção"), DisplayName("C Hazen-Williams (água)")] public double? RugosidadeHW { get; set; }

    [Category("Perfil forçado"), DisplayName("Comprimento forçado (m)")] public double? ComprimentoForcado { get; set; }
    [Category("Perfil forçado"), DisplayName("Declividade forçada (m/m)")] public double? DeclividadeForcada { get; set; }
    [Category("Perfil forçado"), DisplayName("Cota de fundo montante forçada")] public double? CotaMontanteForcada { get; set; }
    [Category("Perfil forçado"), DisplayName("Cota de fundo jusante forçada")] public double? CotaJusanteForcada { get; set; }

    [Category("Esgoto"), DisplayName("Contribui em marcha")] public bool Contribui { get; set; } = true;

    [Browsable(false)] public string? HandleLinha { get; set; }
    [Browsable(false)] public string? HandleRotulo { get; set; }
    [Browsable(false)] public string? HandleSolido { get; set; }
    public override string ToString() => Nome;
}

public sealed class Bacia
{
    [Category("Identificação"), DisplayName("Id"), ReadOnly(true)] public string Id { get; set; } = Projeto.NovoId();
    [Category("Identificação"), DisplayName("Nome")] public string Nome { get; set; } = "";
    [Category("Conexão"), DisplayName("Nó de lançamento (Id)"), ReadOnly(true)] public string? NoId { get; set; }

    [Category("Hidrologia"), DisplayName("Área (ha)")] public double AreaHa { get; set; }
    [Category("Hidrologia"), DisplayName("Área forçada (ha)")] public double? AreaForcadaHa { get; set; }
    [Category("Hidrologia"), DisplayName("Coeficiente de escoamento C")] public double C { get; set; } = 0.70;
    [Category("Hidrologia"), DisplayName("Método do tc")] public MetodoTc MetodoTc { get; set; } = MetodoTc.Informado;
    [Category("Hidrologia"), DisplayName("tc informado (min)")] public double TcInformado { get; set; } = 10;
    [Category("Hidrologia"), DisplayName("Comprimento do talvegue (m)")] public double ComprimentoTalvegue { get; set; }
    [Category("Hidrologia"), DisplayName("Desnível do talvegue (m)")] public double DesnivelTalvegue { get; set; }
    [Category("Hidrologia"), DisplayName("Impermeabilidade (%) — SWMM"), Description("Vazio = estimado a partir de C")] public double? Impermeabilidade { get; set; }

    [Browsable(false)] public List<P2> Contorno { get; set; } = new();
    [Browsable(false)] public string? HandleContorno { get; set; }
    [Browsable(false)] public string? HandleTalvegue { get; set; }
    [Browsable(false)] public string? HandleRotulo { get; set; }

    [JsonIgnore, Browsable(false)] public double Area => AreaForcadaHa ?? AreaHa;

    /// <summary>Tempo de concentração da bacia (min).</summary>
    public double Tc() => TempoConcentracao.Calcular(MetodoTc, ComprimentoTalvegue, DesnivelTalvegue, TcInformado);

    /// <summary>Impermeabilidade estimada a partir de C (relação linear entre C=0,20 → 0% e C=0,90 → 100%).</summary>
    public double ImpermeabilidadeEstimada() => Impermeabilidade ?? Math.Clamp((C - 0.20) / 0.70 * 100.0, 0, 100);
    public override string ToString() => Nome;
}

// ---------------------------------------------------------------- parâmetros

public sealed class ParametrosDrenagem
{
    [Category("Chuva"), DisplayName("Equação de chuva (Id)")] public string EquacaoId { get; set; } = "BH-UFU2024";
    [Category("Chuva"), DisplayName("Tempo de retorno (anos)")] public double TempoRetorno { get; set; } = 10;
    [Category("Chuva"), DisplayName("tc mínimo / de entrada (min)")] public double TcMinimo { get; set; } = 10;

    [Category("Hidráulica"), DisplayName("Manning n")] public double Manning { get; set; } = 0.015;
    [Category("Hidráulica"), DisplayName("Lâmina máxima y/D")] public double LaminaMaxima { get; set; } = 0.85;
    [Category("Hidráulica"), DisplayName("Velocidade mínima (m/s)")] public double VelocidadeMinima { get; set; } = 0.75;
    [Category("Hidráulica"), DisplayName("Velocidade máxima (m/s)")] public double VelocidadeMaxima { get; set; } = 5.0;

    [Category("Geometria"), DisplayName("Recobrimento mínimo (m)")] public double RecobrimentoMinimo { get; set; } = 1.00;
    [Category("Geometria"), DisplayName("Profundidade máxima de aviso (m)")] public double ProfundidadeMaxima { get; set; } = 4.50;
    [Category("Geometria"), DisplayName("Declividade mínima (m/m)")] public double DeclividadeMinima { get; set; } = 0.005;
    [Category("Geometria"), DisplayName("Declividade máxima (m/m)")] public double DeclividadeMaxima { get; set; } = 0.10;
    [Category("Geometria"), DisplayName("Critério de concordância nos PVs")] public CriterioConcordancia Criterio { get; set; } = CriterioConcordancia.GeratrizSuperior;
    [Category("Geometria"), DisplayName("Degrau mínimo no PV (m)")] public double DegrauMinimo { get; set; } = 0.0;
    [Category("Geometria"), DisplayName("Espessura de parede relativa (e/D)")] public double EspessuraRelativa { get; set; } = 0.10;
    [Category("Geometria"), DisplayName("Espessura mínima de parede (m)")] public double EspessuraMinima { get; set; } = 0.05;

    [Category("Seções"), DisplayName("Diâmetros comerciais (m)"), TypeConverter(typeof(ListaNumerosConverter))] public List<double> Diametros { get; set; } = new() { 0.40, 0.50, 0.60, 0.80, 1.00, 1.20, 1.50 };
    [Category("Seções"), DisplayName("Diâmetro mínimo (m)")] public double DiametroMinimo { get; set; } = 0.40;
    [Category("Seções"), DisplayName("Permitir galerias retangulares")] public bool PermitirGalerias { get; set; } = true;
    [Category("Seções"), DisplayName("Galerias (b x h, m)"), TypeConverter(typeof(ListaTextoConverter))] public List<string> Galerias { get; set; } = new() { "1.50x1.50", "2.00x1.50", "2.00x2.00", "2.50x2.00", "2.50x2.50", "3.00x2.50", "3.00x3.00" };

    public double Espessura(double d) => Math.Max(EspessuraMinima, EspessuraRelativa * d);
}

public sealed class ParametrosEsgoto
{
    [Category("População"), DisplayName("População inicial (hab)")] public double PopulacaoInicial { get; set; } = 1000;
    [Category("População"), DisplayName("População final (hab)")] public double PopulacaoFinal { get; set; } = 1500;
    [Category("Consumo"), DisplayName("Consumo per capita (L/hab.dia)")] public double ConsumoPerCapita { get; set; } = 160;
    [Category("Consumo"), DisplayName("Coeficiente de retorno C")] public double CoefRetorno { get; set; } = 0.80;
    [Category("Consumo"), DisplayName("K1 (dia de maior consumo)")] public double K1 { get; set; } = 1.2;
    [Category("Consumo"), DisplayName("K2 (hora de maior consumo)")] public double K2 { get; set; } = 1.5;
    [Category("Consumo"), DisplayName("Taxa de infiltração (L/s.km)")] public double TaxaInfiltracao { get; set; } = 0.10;
    [Category("Consumo"), DisplayName("Vazão mínima de cálculo (L/s)")] public double VazaoMinima { get; set; } = 1.5;

    [Category("Hidráulica"), DisplayName("Manning n")] public double Manning { get; set; } = 0.013;
    [Category("Hidráulica"), DisplayName("Lâmina máxima y/D")] public double LaminaMaxima { get; set; } = 0.75;
    [Category("Hidráulica"), DisplayName("Tensão trativa mínima (Pa)")] public double TensaoTrativaMinima { get; set; } = 1.0;
    [Category("Hidráulica"), DisplayName("Velocidade máxima (m/s)")] public double VelocidadeMaxima { get; set; } = 5.0;

    [Category("Geometria"), DisplayName("Recobrimento mínimo (m)")] public double RecobrimentoMinimo { get; set; } = 0.90;
    [Category("Geometria"), DisplayName("Profundidade máxima de aviso (m)")] public double ProfundidadeMaxima { get; set; } = 4.00;
    [Category("Geometria"), DisplayName("Declividade mínima absoluta (m/m)")] public double DeclividadeMinimaAbsoluta { get; set; } = 0.0045;
    [Category("Geometria"), DisplayName("Critério de concordância")] public CriterioConcordancia Criterio { get; set; } = CriterioConcordancia.GeratrizSuperior;
    [Category("Geometria"), DisplayName("Degrau mínimo no PV (m)")] public double DegrauMinimo { get; set; } = 0.0;
    [Category("Geometria"), DisplayName("Degrau que exige tubo de queda (m)")] public double DegrauTuboQueda { get; set; } = 0.50;
    [Category("Geometria"), DisplayName("Espessura de parede relativa (e/D)")] public double EspessuraRelativa { get; set; } = 0.08;
    [Category("Geometria"), DisplayName("Espessura mínima de parede (m)")] public double EspessuraMinima { get; set; } = 0.02;

    [Category("Seções"), DisplayName("Diâmetros comerciais (m)"), TypeConverter(typeof(ListaNumerosConverter))] public List<double> Diametros { get; set; } = new() { 0.15, 0.20, 0.25, 0.30, 0.35, 0.40, 0.45, 0.50, 0.60 };
    [Category("Seções"), DisplayName("Diâmetro mínimo (m)")] public double DiametroMinimo { get; set; } = 0.15;

    public double Espessura(double d) => Math.Max(EspessuraMinima, EspessuraRelativa * d);

    /// <summary>Vazão doméstica inicial (L/s): C·K2·q·Pi/86400.</summary>
    public double VazaoDomesticaInicial() => CoefRetorno * K2 * ConsumoPerCapita * PopulacaoInicial / 86400.0;
    /// <summary>Vazão doméstica final (L/s): C·K1·K2·q·Pf/86400.</summary>
    public double VazaoDomesticaFinal() => CoefRetorno * K1 * K2 * ConsumoPerCapita * PopulacaoFinal / 86400.0;
}

public sealed class ParametrosAgua
{
    [Category("Demanda"), DisplayName("População final (hab)")] public double PopulacaoFinal { get; set; } = 1500;
    [Category("Demanda"), DisplayName("Consumo per capita (L/hab.dia)")] public double ConsumoPerCapita { get; set; } = 200;
    [Category("Demanda"), DisplayName("K1")] public double K1 { get; set; } = 1.2;
    [Category("Demanda"), DisplayName("K2")] public double K2 { get; set; } = 1.5;
    [Category("Demanda"), DisplayName("Distribuir demanda pelo comprimento dos tubos")] public bool DistribuirPorComprimento { get; set; } = true;

    [Category("Hidráulica"), DisplayName("C Hazen-Williams padrão")] public double RugosidadeHW { get; set; } = 140;
    [Category("Hidráulica"), DisplayName("Pressão dinâmica mínima (mca)")] public double PressaoMinima { get; set; } = 10;
    [Category("Hidráulica"), DisplayName("Pressão estática máxima (mca)")] public double PressaoMaxima { get; set; } = 40;
    [Category("Hidráulica"), DisplayName("Velocidade máxima (m/s)")] public double VelocidadeMaxima { get; set; } = 2.0;
    [Category("Hidráulica"), DisplayName("Diâmetro padrão (m)")] public double DiametroPadrao { get; set; } = 0.10;
    [Category("Hidráulica"), DisplayName("Diâmetros comerciais (m)"), TypeConverter(typeof(ListaNumerosConverter))] public List<double> Diametros { get; set; } = new() { 0.05, 0.075, 0.10, 0.15, 0.20, 0.25, 0.30, 0.40 };
    [Category("Hidráulica"), DisplayName("Dimensionar diâmetros por velocidade")] public bool DimensionarPorVelocidade { get; set; } = true;

    /// <summary>Vazão máxima horária total (L/s): K1·K2·q·P/86400.</summary>
    public double DemandaTotal() => K1 * K2 * ConsumoPerCapita * PopulacaoFinal / 86400.0;
}
