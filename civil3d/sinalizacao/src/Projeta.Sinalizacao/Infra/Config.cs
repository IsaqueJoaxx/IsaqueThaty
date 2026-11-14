using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Normas;

namespace Projeta.Sinalizacao.Infra;

/// <summary>
/// Padrões do projeto (equivalente ao "Definir padrões"): escala, tamanhos de exibição, tipo de via, materiais.
/// Ficam gravados no DWG; "Salvar como padrão" grava também para novos desenhos.
/// </summary>
internal sealed class Config
{
    [Category("1. Desenho"), DisplayName("Escala (1:N)"), Description("Denominador da escala de plotagem (500 = 1:500). Define o tamanho dos símbolos e textos.")]
    public double Escala { get; set; } = 500;

    [Category("1. Desenho"), DisplayName("Placa no papel (mm)"), Description("Tamanho com que uma placa de 0,50 m aparece no papel. As demais mantêm a proporção.")]
    public double TamanhoPlacaPapel { get; set; } = 9;

    [Category("1. Desenho"), DisplayName("Texto no papel (mm)"), Description("Altura dos rótulos (ID e código) no papel.")]
    public double AlturaTextoPapel { get; set; } = 1.8;

    [Category("1. Desenho"), DisplayName("Ampliação de dispositivos"), Description("Fator de ampliação dos símbolos de cones, semáforos e outros dispositivos em planta.")]
    public double FatorDispositivos { get; set; } = 2.5;

    [Category("1. Desenho"), DisplayName("Ampliação de tachas"), Description("Fator de ampliação das tachas em planta.")]
    public double FatorTachas { get; set; } = 3;

    [Category("1. Desenho"), DisplayName("Ampliação das placas da zona de obra")]
    public double FatorPlacasZona { get; set; } = 6;

    [Category("1. Desenho"), DisplayName("Rotular marcas"), Description("Escreve o código das marcas horizontais (LFO-2, FTP-1…) ao lado do desenho.")]
    public bool RotularMarcas { get; set; } = true;

    [Category("2. Projeto"), DisplayName("Tipo de via"), Description("Define as dimensões padrão das placas (CONTRAN) e alturas de montagem.")]
    public TipoVia Via { get; set; } = TipoVia.Urbana;

    [Category("2. Projeto"), DisplayName("Velocidade padrão (km/h)")]
    public double Velocidade { get; set; } = 50;

    [Category("2. Projeto"), DisplayName("Estaqueamento (m)"), Description("20 = estacas de 20 m (12+5,600); 1000 = quilômetro (1+245,600).")]
    public double PassoEstaca { get; set; } = 20;

    [Category("2. Projeto"), DisplayName("Situação padrão")]
    public Situacao SituacaoPadrao { get; set; } = Situacao.Implantar;

    [Category("3. Materiais"), DisplayName("Chapa")]
    public string Chapa { get; set; } = "Aço galvanizado #18";

    [Category("3. Materiais"), DisplayName("Película")]
    public string Pelicula { get; set; } = "Retrorrefletiva tipo III";

    [Category("3. Materiais"), DisplayName("Material do suporte")]
    public string MaterialSuporte { get; set; } = "Tubo aço galvanizado Ø 2 1/2\"";

    [Category("3. Materiais"), DisplayName("Material de pintura")]
    public string MaterialPintura { get; set; } = "Tinta acrílica a base de água";

    [Category("4. Normas"), DisplayName("Arquivo de normas"), Description("normas.json com larguras, cadências, tamanhos e espaçamentos. Vazio = padrão do plugin. Edite com SINNORMAS.")]
    public string ArquivoNormas { get; set; } = "";

    [Browsable(false)]
    public Dictionary<string, int> Contadores { get; set; } = new();

    // ------------------------------------------------------------------ derivados
    [JsonIgnore, Browsable(false)] public double TextoModelo => AlturaTextoPapel / 1000.0 * Escala;
    /// <summary>Fator de ampliação das faces de placa (placa de 0,50 m → TamanhoPlacaPapel no papel).</summary>
    [JsonIgnore, Browsable(false)] public double FatorPlaca => TamanhoPlacaPapel / 1000.0 * Escala / 0.50;
    [JsonIgnore, Browsable(false)] public double MmModelo => Escala / 1000.0;

    public Exibicao Exibicao() => new() { FatorDispositivos = FatorDispositivos, FatorPlacasZona = FatorPlacasZona, FatorTachas = FatorTachas, Via = Via };

    // ------------------------------------------------------------------ persistência
    public static string PastaUsuario => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "Sinalizacao");
    public static string ArquivoPadroes => Path.Combine(PastaUsuario, "padroes.json");
    public static string ArquivoNormasPadrao => Path.Combine(PastaUsuario, "normas.json");
    private const string Chave = "PROJETA_SIN_CONFIG";

    private static readonly JsonSerializerOptions Op = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public string ParaJson() => JsonSerializer.Serialize(this, Op);
    public static Config? DeJson(string? s) { if (string.IsNullOrWhiteSpace(s)) return null; try { return JsonSerializer.Deserialize<Config>(s, Op); } catch { return null; } }

    public static Config PadraoUsuario()
    {
        try { if (File.Exists(ArquivoPadroes)) return DeJson(File.ReadAllText(ArquivoPadroes)) ?? new Config(); } catch { }
        return new Config();
    }

    public void SalvarComoPadrao()
    {
        Directory.CreateDirectory(PastaUsuario);
        var c = DeJson(ParaJson())!;
        c.Contadores = new();
        File.WriteAllText(ArquivoPadroes, c.ParaJson());
    }

    public static Config Ler(Transaction tr, Database db)
    {
        var s = Armazenamento.LerTextoNod(tr, db, Chave);
        return DeJson(s) ?? PadraoUsuario();
    }

    public void Gravar(Transaction tr, Database db) => Armazenamento.GravarTextoNod(tr, db, Chave, ParaJson());

    /// <summary>Próximo ID da categoria (PL-001, ML-002…), guardado no próprio DWG.</summary>
    public string ProximoId(Categoria c)
    {
        string p = ObjetoSin.PrefixoId(c);
        Contadores.TryGetValue(p, out int n);
        n++;
        Contadores[p] = n;
        return $"{p}-{n:000}";
    }

    // ------------------------------------------------------------------ normas (cache por arquivo)
    private static Core.Normas.Normas? _normas;
    private static string? _normasArq;
    private static DateTime _normasData;

    public Core.Normas.Normas Normas()
    {
        string arq = string.IsNullOrWhiteSpace(ArquivoNormas) ? ArquivoNormasPadrao : ArquivoNormas;
        DateTime dt = File.Exists(arq) ? File.GetLastWriteTimeUtc(arq) : DateTime.MinValue;
        if (_normas is null || _normasArq != arq || _normasData != dt)
        {
            _normas = Core.Normas.Normas.Carregar(arq, out var erro);
            if (erro is not null) try { Ctx.Msg("[NORMAS] " + erro); } catch { }
            _normasArq = arq; _normasData = dt;
        }
        _normas.ChapaPadrao = Chapa;
        _normas.PeliculaPadrao = Pelicula;
        _normas.MaterialSuportePadrao = MaterialSuporte;
        _normas.MaterialPinturaPadrao = MaterialPintura;
        return _normas;
    }
}
