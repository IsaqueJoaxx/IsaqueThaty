using System.Text.Json;
using System.Text.Json.Serialization;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Obras;
using Projeta.Sinalizacao.Core.Vertical;

namespace Projeta.Sinalizacao.Core.Dados;

public enum Categoria { Placa, MarcaLongitudinal, MarcaTransversal, Inscricao, Tachas, Semaforica, RedeSemaforica, Obra, ZonaObra, Defensa }

/// <summary>Situação do dispositivo no projeto.</summary>
public enum Situacao { Implantar, Manter, Remover, Recuperar }

/// <summary>Uma placa dentro de um conjunto (várias placas podem dividir o mesmo suporte).</summary>
public sealed class PlacaItem
{
    public string Codigo { get; set; } = "R-1";
    public double Dimensao { get; set; }
    /// <summary>Largura/altura das placas retangulares (modelador).</summary>
    public double Largura { get; set; }
    public double Altura { get; set; }
    public string? Valor { get; set; }
    public bool Obra { get; set; }
    /// <summary>Modelo do modelador de placas (JSON), para placas de indicação/educativas.</summary>
    public string? Modelo { get; set; }
}

public sealed class DadosPlaca
{
    public List<PlacaItem> Placas { get; set; } = new();
    public string Chapa { get; set; } = "";
    public string Pelicula { get; set; } = "";
    public TipoSuporte Suporte { get; set; } = TipoSuporte.ColunaSimples;
    public double Vao { get; set; }
    public string? MaterialSuporte { get; set; }
    /// <summary>Direção (rad) para a qual a face da placa está voltada (de frente para o tráfego).</summary>
    public double Azimute { get; set; } = Math.PI / 2;
    /// <summary>Afastamento lateral do bordo da pista (m), informativo.</summary>
    public double Afastamento { get; set; }
}

/// <summary>Parâmetros das marcas, inscrições e tachas (campos usados conforme o código).</summary>
public sealed class DadosMarca
{
    public double Velocidade { get; set; } = 60;
    public double Deslocamento { get; set; }
    public double? Largura { get; set; }
    public double? Traco { get; set; }
    public double? Espaco { get; set; }
    public double Fase { get; set; }
    public bool InverterLados { get; set; }
    public Cor? Cor { get; set; }
    public string Material { get; set; } = "";
    // transversais
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Extensao { get; set; }
    public int NumLinhas { get; set; }
    public double V1 { get; set; }
    public double V2 { get; set; }
    public bool FluxoAEsquerda { get; set; } = true;
    // zebrado
    public double AnguloFluxo { get; set; }
    public double LarguraListra { get; set; }
    public double EspacoListra { get; set; }
    public double LarguraContorno { get; set; }
    // inscrições
    public string Texto { get; set; } = "";
    public double AlturaLetra { get; set; }
    public double FatorLargura { get; set; }
    public double EspacoLinhas { get; set; }
    public double Comprimento { get; set; }
    public double Escala { get; set; } = 1;
    // tachas
    public double EspacamentoReta { get; set; }
    public double EspacamentoCurva { get; set; }
}

public sealed class DadosDispositivo
{
    public double Braco { get; set; }
    public double Altura { get; set; }
    public double Comprimento { get; set; }
    public string Descricao { get; set; } = "";
}

/// <summary>
/// Dados gravados em cada objeto do DWG (dicionário de extensão). Guardam tudo o que é preciso para
/// regenerar o desenho e montar os quantitativos sem depender de outras entidades.
/// </summary>
public sealed class ObjetoSin
{
    public int Versao { get; set; } = 1;
    public string Id { get; set; } = "";
    public Categoria Categoria { get; set; }
    public string Codigo { get; set; } = "";
    public Situacao Situacao { get; set; } = Situacao.Implantar;
    public string Obs { get; set; } = "";
    /// <summary>Ponto de inserção (placas e dispositivos) ou referência do bloco das marcas.</summary>
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotacao { get; set; }
    /// <summary>Posição/rotação da referência de bloco quando o objeto foi gerado (para acompanhar MOVE/ROTATE).</summary>
    public double BaseX { get; set; }
    public double BaseY { get; set; }
    public double BaseRot { get; set; }
    /// <summary>Caminho de origem (marcas longitudinais, tachas, defensas, zebrado, redes, zona de obra).</summary>
    public List<Vertice>? Caminho { get; set; }
    public bool CaminhoFechado { get; set; }
    public DadosPlaca? Placa { get; set; }
    public DadosMarca? Marca { get; set; }
    public DadosDispositivo? Dispositivo { get; set; }
    public ParamDefensa? Defensa { get; set; }
    public ParamZonaObra? Zona { get; set; }
    /// <summary>Quantidades calculadas na última geração (chave → valor).</summary>
    public Dictionary<string, double> Quant { get; set; } = new();
    /// <summary>Texto descritivo (nome do tipo) da última geração.</summary>
    public string Descricao { get; set; } = "";

    [JsonIgnore]
    public Caminho? CaminhoObj => Caminho is { Count: >= 2 } ? new Caminho(Caminho, CaminhoFechado) : null;

    private static readonly JsonSerializerOptions Op = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public string ParaJson() => JsonSerializer.Serialize(this, Op);
    public static ObjetoSin? DeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<ObjetoSin>(json, Op); } catch { return null; }
    }

    public ObjetoSin Clonar() => DeJson(ParaJson())!;

    /// <summary>
    /// Aplica a todas as coordenadas guardadas o deslocamento/rotação sofridos pela referência de bloco
    /// (de BaseX/BaseY/BaseRot para a posição atual). Retorna true se algo mudou.
    /// </summary>
    public bool Sincronizar(double x, double y, double rot)
    {
        double dr = Num.NormAng(rot - BaseRot);
        if (Math.Abs(x - BaseX) < 1e-7 && Math.Abs(y - BaseY) < 1e-7 && Math.Abs(dr) < 1e-9) return false;
        var b = new V2(BaseX, BaseY);
        var a = new V2(x, y);
        V2 F(V2 p) => a + (p - b).Girar(dr);
        var q = F(new V2(X, Y)); X = q.X; Y = q.Y;
        Rotacao += dr;
        if (Caminho is not null)
            Caminho = Caminho.Select(v => { var w = F(v.P); return new Vertice(w.X, w.Y, v.Bulge); }).ToList();
        if (Marca is not null)
        {
            var p1 = F(new V2(Marca.X1, Marca.Y1)); var p2 = F(new V2(Marca.X2, Marca.Y2));
            Marca.X1 = p1.X; Marca.Y1 = p1.Y; Marca.X2 = p2.X; Marca.Y2 = p2.Y;
            Marca.AnguloFluxo += dr;
        }
        if (Placa is not null) Placa.Azimute += dr;
        BaseX = x; BaseY = y; BaseRot = rot;
        return true;
    }

    public double Q(string chave) => Quant.TryGetValue(chave, out var v) ? v : 0;

    public static string PrefixoId(Categoria c) => c switch
    {
        Categoria.Placa => "PL",
        Categoria.MarcaLongitudinal => "ML",
        Categoria.MarcaTransversal => "MT",
        Categoria.Inscricao => "IN",
        Categoria.Tachas => "TA",
        Categoria.Semaforica => "SE",
        Categoria.RedeSemaforica => "RS",
        Categoria.Obra => "OB",
        Categoria.ZonaObra => "ZO",
        Categoria.Defensa => "DF",
        _ => "X",
    };

    public static string NomeSituacao(Situacao s) => s switch
    {
        Situacao.Implantar => "Implantar",
        Situacao.Manter => "Manter (existente)",
        Situacao.Remover => "Remover",
        Situacao.Recuperar => "Recuperar",
        _ => s.ToString(),
    };
}
