using System.ComponentModel;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Obras;
using Projeta.Sinalizacao.Core.Vertical;
using Cor = Projeta.Sinalizacao.Core.Desenho.Cor;

namespace Projeta.Sinalizacao.UI;

/// <summary>Propriedades editáveis de um objeto (mostradas num PropertyGrid). Aplicar() devolve ao objeto.</summary>
internal abstract class Edicao
{
    protected readonly ObjetoSin O;
    protected Edicao(ObjetoSin o) { O = o; Situacao = o.Situacao; Observacao = o.Obs; }

    [Category("Geral"), DisplayName("ID"), ReadOnly(true)] public string Id => O.Id;
    [Category("Geral"), DisplayName("Tipo"), ReadOnly(true)] public string Tipo => O.Descricao;
    [Category("Geral"), DisplayName("Situação")] public Situacao Situacao { get; set; }
    [Category("Geral"), DisplayName("Observação")] public string Observacao { get; set; }

    public virtual void Aplicar() { O.Situacao = Situacao; O.Obs = Observacao ?? ""; }

    public static Edicao Para(ObjetoSin o) => o.Categoria switch
    {
        Categoria.Placa => new EdPlaca(o),
        Categoria.MarcaLongitudinal => new EdLongitudinal(o),
        Categoria.MarcaTransversal => new EdTransversal(o),
        Categoria.Inscricao => new EdInscricao(o),
        Categoria.Tachas => new EdTachas(o),
        Categoria.Defensa => new EdDefensa(o),
        Categoria.ZonaObra => new EdZona(o),
        _ => new EdGenerica(o),
    };
}

internal sealed class EdGenerica : Edicao
{
    public EdGenerica(ObjetoSin o) : base(o) { Braco = o.Dispositivo?.Braco ?? 0; }
    [Category("Dispositivo"), DisplayName("Braço projetado (m)")] public double Braco { get; set; }
    public override void Aplicar() { base.Aplicar(); if (O.Dispositivo is not null) O.Dispositivo.Braco = Braco; }
}

internal sealed class EdPlaca : Edicao
{
    public EdPlaca(ObjetoSin o) : base(o)
    {
        var p = o.Placa ??= new DadosPlaca();
        Codigos = string.Join(" + ", p.Placas.Select(x => x.Codigo));
        Valores = string.Join(" + ", p.Placas.Select(x => x.Valor ?? "-"));
        Dimensoes = string.Join(" + ", p.Placas.Select(x => Num.M(x.Dimensao)));
        Suporte = p.Suporte; Vao = p.Vao; Chapa = p.Chapa; Pelicula = p.Pelicula; Azimute = Num.Arred(Num.Graus(p.Azimute), 2);
        Obra = p.Placas.Any(x => x.Obra);
    }
    [Category("Placas"), DisplayName("Códigos"), Description("De cima para baixo, separados por +. Ex.: R-19 + R-6a")] public string Codigos { get; set; }
    [Category("Placas"), DisplayName("Valores"), Description("Valor exibido em cada placa (velocidade, altura…), '-' quando não tem.")] public string Valores { get; set; }
    [Category("Placas"), DisplayName("Dimensões (m)"), Description("Diâmetro ou lado de cada placa, separados por +.")] public string Dimensoes { get; set; }
    [Category("Placas"), DisplayName("Obra (fundo laranja)")] public bool Obra { get; set; }
    [Category("Placas"), DisplayName("Chapa")] public string Chapa { get; set; }
    [Category("Placas"), DisplayName("Película")] public string Pelicula { get; set; }
    [Category("Suporte"), DisplayName("Tipo de suporte")] public TipoSuporte Suporte { get; set; }
    [Category("Suporte"), DisplayName("Vão/braço (m)")] public double Vao { get; set; }
    [Category("Suporte"), DisplayName("Face voltada para (graus)")] public double Azimute { get; set; }

    public override void Aplicar()
    {
        base.Aplicar();
        var p = O.Placa!;
        var cods = Codigos.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var vals = Valores.Split('+', StringSplitOptions.TrimEntries);
        var dims = Dimensoes.Split('+', StringSplitOptions.TrimEntries);
        var novas = new List<PlacaItem>();
        for (int i = 0; i < cods.Length; i++)
        {
            var antiga = i < p.Placas.Count ? p.Placas[i] : null;
            var t = Catalogo.Obter(cods[i]);
            var it = new PlacaItem
            {
                Codigo = t?.Codigo ?? cods[i], Modelo = antiga?.Codigo == cods[i] ? antiga?.Modelo : null,
                Dimensao = i < dims.Length ? Num.Ler(dims[i], 0) : 0,
                Valor = i < vals.Length && vals[i] != "-" && vals[i].Length > 0 ? vals[i] : t?.ValorPadrao,
                Obra = Obra && t?.Familia == Familia.Advertencia,
            };
            if (antiga is not null && antiga.Codigo != it.Codigo) it.Dimensao = 0; // nova placa: tamanho padrão
            novas.Add(it);
        }
        if (novas.Count > 0) p.Placas = novas;
        p.Suporte = Suporte; p.Vao = Vao; p.Chapa = Chapa; p.Pelicula = Pelicula; p.Azimute = Num.Rad(Azimute);
    }
}

internal sealed class EdLongitudinal : Edicao
{
    public EdLongitudinal(ObjetoSin o) : base(o)
    {
        var m = o.Marca ??= new DadosMarca();
        Codigo = o.Codigo; Velocidade = m.Velocidade; Deslocamento = m.Deslocamento; Largura = m.Largura; Traco = m.Traco; Espaco = m.Espaco;
        Fase = m.Fase; Inverter = m.InverterLados; Material = m.Material;
    }
    [Category("Marca"), DisplayName("Código"), Description("LFO-1…4, LMS-1/2, LBO, LCO, MFE, MFE-CF, MFP, MFR, MCI, LCA, LPP")] public string Codigo { get; set; }
    [Category("Marca"), DisplayName("Velocidade (km/h)")] public double Velocidade { get; set; }
    [Category("Marca"), DisplayName("Deslocamento lateral (m)")] public double Deslocamento { get; set; }
    [Category("Marca"), DisplayName("Largura (m) — vazio = norma")] public double? Largura { get; set; }
    [Category("Marca"), DisplayName("Traço (m) — vazio = norma")] public double? Traco { get; set; }
    [Category("Marca"), DisplayName("Espaço (m) — vazio = norma")] public double? Espaco { get; set; }
    [Category("Marca"), DisplayName("Fase (m)")] public double Fase { get; set; }
    [Category("Marca"), DisplayName("Inverter lados (LFO-4)")] public bool Inverter { get; set; }
    [Category("Marca"), DisplayName("Material")] public string Material { get; set; }
    public override void Aplicar()
    {
        base.Aplicar();
        if (Core.Horizontal.CatalogoLongitudinal.Obter(Codigo) is { } t) O.Codigo = t.Codigo;
        var m = O.Marca!;
        m.Velocidade = Velocidade; m.Deslocamento = Deslocamento; m.Largura = Largura; m.Traco = Traco; m.Espaco = Espaco; m.Fase = Fase; m.InverterLados = Inverter; m.Material = Material;
    }
}

internal sealed class EdTransversal : Edicao
{
    public EdTransversal(ObjetoSin o) : base(o)
    {
        var m = o.Marca ??= new DadosMarca();
        Largura = m.Largura; Extensao = m.Extensao; Espaco = m.Espaco; V1 = m.V1; V2 = m.V2; NumLinhas = m.NumLinhas; Material = m.Material;
        LarguraListra = m.LarguraListra; EspacoListra = m.EspacoListra; Contorno = m.LarguraContorno; Cor = m.Cor ?? Cor.Branco;
    }
    [Category("Marca"), DisplayName("Largura das linhas (m) — vazio = norma")] public double? Largura { get; set; }
    [Category("Marca"), DisplayName("Extensão (m)")] public double Extensao { get; set; }
    [Category("Marca"), DisplayName("Espaço (m) — vazio = norma")] public double? Espaco { get; set; }
    [Category("LRV"), DisplayName("Velocidade inicial (km/h)")] public double V1 { get; set; }
    [Category("LRV"), DisplayName("Velocidade final (km/h)")] public double V2 { get; set; }
    [Category("LRV"), DisplayName("Número de linhas")] public int NumLinhas { get; set; }
    [Category("Zebrado"), DisplayName("Largura das listras (m)")] public double LarguraListra { get; set; }
    [Category("Zebrado"), DisplayName("Espaço entre listras (m)")] public double EspacoListra { get; set; }
    [Category("Zebrado"), DisplayName("Contorno (m)")] public double Contorno { get; set; }
    [Category("Zebrado"), DisplayName("Cor")] public Cor Cor { get; set; }
    [Category("Marca"), DisplayName("Material")] public string Material { get; set; }
    public override void Aplicar()
    {
        base.Aplicar();
        var m = O.Marca!;
        m.Largura = Largura; m.Extensao = Extensao; m.Espaco = Espaco; m.V1 = V1; m.V2 = V2; m.NumLinhas = NumLinhas; m.Material = Material;
        m.LarguraListra = LarguraListra; m.EspacoListra = EspacoListra; m.LarguraContorno = Contorno;
        if (O.Codigo == "ZPA") m.Cor = Cor;
    }
}

internal sealed class EdInscricao : Edicao
{
    public EdInscricao(ObjetoSin o) : base(o)
    {
        var m = o.Marca ??= new DadosMarca();
        Texto = m.Texto; AlturaLetra = m.AlturaLetra; Comprimento = m.Comprimento; Escala = m.Escala; Material = m.Material; Velocidade = m.Velocidade;
    }
    [Category("Inscrição"), DisplayName("Texto (legendas; | quebra linha)")] public string Texto { get; set; }
    [Category("Inscrição"), DisplayName("Altura das letras (m)")] public double AlturaLetra { get; set; }
    [Category("Inscrição"), DisplayName("Comprimento da seta (m)")] public double Comprimento { get; set; }
    [Category("Inscrição"), DisplayName("Escala do símbolo")] public double Escala { get; set; }
    [Category("Inscrição"), DisplayName("Velocidade (km/h)")] public double Velocidade { get; set; }
    [Category("Inscrição"), DisplayName("Material")] public string Material { get; set; }
    public override void Aplicar()
    {
        base.Aplicar();
        var m = O.Marca!;
        m.Texto = Texto; m.AlturaLetra = AlturaLetra; m.Comprimento = Comprimento; m.Escala = Escala; m.Material = Material; m.Velocidade = Velocidade;
    }
}

internal sealed class EdTachas : Edicao
{
    public EdTachas(ObjetoSin o) : base(o)
    {
        var m = o.Marca ??= new DadosMarca();
        Deslocamento = m.Deslocamento; Reta = m.EspacamentoReta; Curva = m.EspacamentoCurva; Cor = m.Cor ?? Cor.Amarelo;
    }
    [Category("Tachas"), DisplayName("Deslocamento lateral (m)")] public double Deslocamento { get; set; }
    [Category("Tachas"), DisplayName("Espaçamento em tangente (m) — 0 = norma")] public double Reta { get; set; }
    [Category("Tachas"), DisplayName("Espaçamento em curva (m) — 0 = norma")] public double Curva { get; set; }
    [Category("Tachas"), DisplayName("Cor")] public Cor Cor { get; set; }
    public override void Aplicar()
    {
        base.Aplicar();
        var m = O.Marca!;
        m.Deslocamento = Deslocamento; m.EspacamentoReta = Reta; m.EspacamentoCurva = Curva; m.Cor = Cor;
    }
}

internal sealed class EdDefensa : Edicao
{
    public EdDefensa(ObjetoSin o) : base(o)
    {
        var p = o.Defensa ??= new ParamDefensa();
        TipoDefensa = p.Tipo; Inicio = p.TerminalInicio; Fim = p.TerminalFim; TrafegoAEsquerda = p.LadoTrafego >= 0;
    }
    [Category("Defensa"), DisplayName("Tipo")] public TipoDefensa TipoDefensa { get; set; }
    [Category("Defensa"), DisplayName("Terminal no início")] public TipoTerminal Inicio { get; set; }
    [Category("Defensa"), DisplayName("Terminal no fim")] public TipoTerminal Fim { get; set; }
    [Category("Defensa"), DisplayName("Tráfego à esquerda da linha")] public bool TrafegoAEsquerda { get; set; }
    public override void Aplicar()
    {
        base.Aplicar();
        var p = O.Defensa!;
        p.Tipo = TipoDefensa; p.TerminalInicio = Inicio; p.TerminalFim = Fim; p.LadoTrafego = TrafegoAEsquerda ? 1 : -1;
    }
}

internal sealed class EdZona : Edicao
{
    public EdZona(ObjetoSin o) : base(o) { Z = (o.Zona ??= new ParamZonaObra()); }
    private readonly ParamZonaObra Z;
    [Category("Zona de obra"), DisplayName("Início da área de trabalho (m ao longo do eixo)")] public double Inicio { get => Z.InicioTrabalho; set => Z.InicioTrabalho = value; }
    [Category("Zona de obra"), DisplayName("Comprimento da área de trabalho (m)")] public double Comprimento { get => Z.ComprimentoTrabalho; set => Z.ComprimentoTrabalho = value; }
    [Category("Zona de obra"), DisplayName("Velocidade (km/h)")] public double Velocidade { get => Z.Velocidade; set => Z.Velocidade = value; }
    [Category("Zona de obra"), DisplayName("Largura da faixa (m)")] public double Largura { get => Z.LarguraFaixa; set => Z.LarguraFaixa = value; }
    [Category("Zona de obra"), DisplayName("Deslocamento da faixa (m)")] public double Deslocamento { get => Z.DeslocamentoFaixa; set => Z.DeslocamentoFaixa = value; }
    [Category("Zona de obra"), DisplayName("Faixa fechada à direita")] public bool Direita { get => Z.FechaDireita; set => Z.FechaDireita = value; }
    [Category("Zona de obra"), DisplayName("Dispositivo")] public string Dispositivo { get => Z.Dispositivo; set => Z.Dispositivo = value?.ToUpperInvariant() ?? "CONE"; }
    [Category("Zona de obra"), DisplayName("Placas de pré-sinalização")] public int Placas { get => Z.NumPlacas; set => Z.NumPlacas = Math.Clamp(value, 1, 3); }
    [Category("Zona de obra"), DisplayName("Taper manual (m) — vazio = calculado")] public double? Taper { get => Z.TaperManual; set => Z.TaperManual = value; }
}
