using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projeta.Sinalizacao.Core.Normas;

public enum TipoVia { Urbana, RuralEstrada, RuralRodovia, AreaProtegida }

/// <summary>Valor por faixa de velocidade: vale para v &lt; Ate (km/h). A última faixa deve ter Ate alto.</summary>
public sealed class FaixaVel
{
    public double Ate { get; set; }
    public double Valor { get; set; }
    public FaixaVel() { }
    public FaixaVel(double ate, double valor) { Ate = ate; Valor = valor; }
}

/// <summary>Cadência (traço/espaço) por faixa de velocidade.</summary>
public sealed class Cadencia
{
    public double Ate { get; set; }
    public double Traco { get; set; }
    public double Espaco { get; set; }
    public Cadencia() { }
    public Cadencia(double ate, double traco, double espaco) { Ate = ate; Traco = traco; Espaco = espaco; }
}

/// <summary>
/// Parâmetros normativos usados pelo plugin. Os valores padrão vêm do Manual Brasileiro de Sinalização de
/// Trânsito (CONTRAN, vol. I e IV) quando conferidos; os demais são práticas usuais e estão marcados em
/// <see cref="NaoConferidos"/>. Tudo pode ser alterado no arquivo normas.json (comando SINNORMAS).
/// </summary>
public sealed class Normas
{
    // ------------------------------------------------------------------ placas (vol. I e II)
    /// <summary>Diâmetro das placas circulares adotado por tipo de via (mínimo CONTRAN: 0,40/0,50/0,75/0,30).</summary>
    public Dictionary<TipoVia, double> DiametroCircular { get; set; } = new() { [TipoVia.Urbana] = 0.50, [TipoVia.RuralEstrada] = 0.50, [TipoVia.RuralRodovia] = 0.75, [TipoVia.AreaProtegida] = 0.30 };
    /// <summary>Lado do octógono R-1 (CONTRAN: 0,25/0,35/0,40/0,18).</summary>
    public Dictionary<TipoVia, double> LadoOctogono { get; set; } = new() { [TipoVia.Urbana] = 0.25, [TipoVia.RuralEstrada] = 0.35, [TipoVia.RuralRodovia] = 0.40, [TipoVia.AreaProtegida] = 0.18 };
    /// <summary>Lado do triângulo R-2 (CONTRAN: 0,75/0,75/0,90/0,40).</summary>
    public Dictionary<TipoVia, double> LadoTriangulo { get; set; } = new() { [TipoVia.Urbana] = 0.75, [TipoVia.RuralEstrada] = 0.75, [TipoVia.RuralRodovia] = 0.90, [TipoVia.AreaProtegida] = 0.40 };
    /// <summary>Lado das placas de advertência (quadrado em pé).</summary>
    public Dictionary<TipoVia, double> LadoAdvertencia { get; set; } = new() { [TipoVia.Urbana] = 0.50, [TipoVia.RuralEstrada] = 0.60, [TipoVia.RuralRodovia] = 0.75, [TipoVia.AreaProtegida] = 0.30 };

    public List<double> OpcoesCircular { get; set; } = new() { 0.30, 0.40, 0.50, 0.60, 0.75, 0.90, 1.00, 1.20 };
    public List<double> OpcoesOctogono { get; set; } = new() { 0.18, 0.25, 0.30, 0.35, 0.40, 0.50 };
    public List<double> OpcoesTriangulo { get; set; } = new() { 0.40, 0.75, 0.90, 1.00, 1.20 };
    public List<double> OpcoesAdvertencia { get; set; } = new() { 0.30, 0.45, 0.50, 0.60, 0.75, 0.90, 1.00, 1.20 };

    /// <summary>Orla vermelha e tarja das circulares, em fração do diâmetro (CONTRAN: 0,040 para Ø 0,40).</summary>
    public double OrlaCircularRel { get; set; } = 0.10;
    public double TarjaCircularRel { get; set; } = 0.10;
    /// <summary>R-1: orla interna branca e externa vermelha em fração do lado (CONTRAN: 0,020 e 0,010 para lado 0,25).</summary>
    public double OrlaInternaOctRel { get; set; } = 0.08;
    public double OrlaExternaOctRel { get; set; } = 0.04;
    /// <summary>R-2: orla vermelha em fração do lado (CONTRAN: 0,10 para lado 0,75).</summary>
    public double OrlaTrianguloRel { get; set; } = 0.1333;
    /// <summary>Advertência: orla preta interna e margem amarela externa em fração do lado.</summary>
    public double OrlaPretaAdvRel { get; set; } = 0.04;
    public double MargemAdvRel { get; set; } = 0.02;

    // montagem (vol. I, item de posicionamento)
    public double AlturaLivreUrbana { get; set; } = 2.10;
    public double AlturaLivreRural { get; set; } = 1.20;
    public double AlturaLivreSuspensaUrbana { get; set; } = 4.60;
    public double AlturaLivreSuspensaRural { get; set; } = 5.50;
    public double AfastamentoLateralUrbano { get; set; } = 0.30;
    public double AfastamentoLateralRural { get; set; } = 1.20;
    public double EngastamentoSuporte { get; set; } = 0.60;

    // placas de indicação (modelador)
    public List<FaixaVel> AlturaLetraIndicacao { get; set; } = new() { new(61, 0.125), new(81, 0.150), new(999, 0.200) };

    // ------------------------------------------------------------------ marcas longitudinais (vol. IV)
    /// <summary>Largura das linhas (LFO, LMS, LBO): 0,10 m para v &lt; 80 e 0,15 m para v ≥ 80.</summary>
    public List<FaixaVel> LarguraLinha { get; set; } = new() { new(80, 0.10), new(999, 0.15) };
    /// <summary>Cadência das linhas seccionadas LFO-2 / LMS-2 / trecho seccionado da LFO-4.</summary>
    public List<Cadencia> CadenciaSeccionada { get; set; } = new() { new(60, 2, 4), new(80, 3, 6), new(999, 4, 12) };
    /// <summary>Cadência da LCO (linha de continuidade): 1:1 com 1 m (v ≤ 60) e 2 m (v &gt; 60).</summary>
    public List<Cadencia> CadenciaContinuidade { get; set; } = new() { new(61, 1, 1), new(999, 2, 2) };
    /// <summary>Faixa reversível (MFR) — dupla seccionada.</summary>
    public List<Cadencia> CadenciaReversivel { get; set; } = new() { new(80, 2, 4), new(999, 4, 8) };
    /// <summary>Distância entre as linhas duplas (LFO-3, LFO-4, MFR): 0,10 a 0,15 m.</summary>
    public double DistanciaLinhasDuplas { get; set; } = 0.10;
    /// <summary>Largura das linhas de faixa exclusiva/preferencial e ciclofaixa (0,20 a 0,30 m).</summary>
    public double LarguraLinhaEspecial { get; set; } = 0.20;
    public double LarguraLCA { get; set; } = 0.15;
    public double LarguraLPP { get; set; } = 0.10;

    // ------------------------------------------------------------------ marcas transversais
    public double LarguraLRE { get; set; } = 0.40;          // 0,30 a 0,60
    public double LarguraLDP { get; set; } = 0.30;          // 0,20 a 0,40
    public double TracoLDP { get; set; } = 0.50;
    public double EspacoLDP { get; set; } = 0.50;
    public double FTP1Largura { get; set; } = 0.40;         // 0,30 a 0,40
    public double FTP1Espaco { get; set; } = 0.60;          // 0,30 a 0,80
    public double FTP1Extensao { get; set; } = 4.00;        // mín. 3,00, recomendada 4,00
    public double FTP2Largura { get; set; } = 0.50;         // 0,40 a 0,60
    public double FTP2Extensao { get; set; } = 4.00;
    public List<FaixaVel> LarguraLRV { get; set; } = new() { new(60, 0.20), new(81, 0.30), new(999, 0.40) };
    public double ZPALinha { get; set; } = 0.40;            // 0,30 a 0,50 (circulação)
    public double ZPAEspaco { get; set; } = 1.50;           // 1,10 a 3,50
    public double ZPAContorno { get; set; } = 0.15;
    public double MCCLado { get; set; } = 0.50;             // 0,40 a 0,60

    // ------------------------------------------------------------------ inscrições
    /// <summary>Comprimento das setas PEM/MOF (urbana: 5,00 m até 80 km/h, 7,50 m acima).</summary>
    public List<FaixaVel> ComprimentoSetaUrbana { get; set; } = new() { new(81, 5.00), new(999, 7.50) };
    public List<FaixaVel> ComprimentoSetaRural { get; set; } = new() { new(60, 5.00), new(999, 7.50) };
    public List<FaixaVel> AlturaLegenda { get; set; } = new() { new(81, 1.60), new(999, 2.40) };
    /// <summary>Fator de largura das letras no pavimento (letras alongadas).</summary>
    public double FatorLarguraLegenda { get; set; } = 0.42;

    // ------------------------------------------------------------------ tachas
    public double EspacamentoTachasReta { get; set; } = 16.0;
    public double EspacamentoTachasCurva { get; set; } = 8.0;
    public double RaioCurvaTachas { get; set; } = 600.0;

    // ------------------------------------------------------------------ obras (sinalização temporária)
    /// <summary>Taper: L = FatorTaper · v · d (CONTRAN, marca de transição de largura: l = 0,5·v·d).</summary>
    public double FatorTaper { get; set; } = 0.5;
    public double TaperMinimo { get; set; } = 30.0;
    /// <summary>Espaçamento dos cones no taper: v / DivisorEspacamentoCones (60 km/h → 6 m).</summary>
    public double DivisorEspacamentoCones { get; set; } = 10.0;
    public double FatorEspacamentoLongitudinal { get; set; } = 2.0;
    public List<FaixaVel> ZonaProtecao { get; set; } = new() { new(41, 20), new(51, 30), new(61, 40), new(71, 55), new(81, 70), new(91, 90), new(101, 110), new(999, 130) };
    public double TaperSaida { get; set; } = 30.0;
    public double FatorDistanciaPlacasObra { get; set; } = 1.5;
    public double DistanciaMinimaPlacasObra { get; set; } = 30.0;

    // ------------------------------------------------------------------ defensas
    public double EspacamentoPostesSemimaleavel { get; set; } = 4.0;
    public double EspacamentoPostesRigida { get; set; } = 2.0;
    public double ComprimentoLamina { get; set; } = 4.0;
    public double ComprimentoTerminalAbatido { get; set; } = 12.0;
    public double ComprimentoTerminalAncorado { get; set; } = 4.0;
    public double AlturaDefensa { get; set; } = 0.75;

    // ------------------------------------------------------------------ semafórica
    public double AlturaColunaSemaforo { get; set; } = 6.0;
    public double ComprimentoBracoProjetado { get; set; } = 4.6;
    public double AlturaLivreGrupoFocal { get; set; } = 4.5;

    // ------------------------------------------------------------------ materiais padrão
    public string ChapaPadrao { get; set; } = "Aço galvanizado #18";
    public string PeliculaPadrao { get; set; } = "Retrorrefletiva tipo III";
    public string MaterialSuportePadrao { get; set; } = "Tubo aço galvanizado Ø 2 1/2\"";
    public string MaterialPinturaPadrao { get; set; } = "Tinta acrílica a base de água";

    /// <summary>Itens cujos valores padrão não foram conferidos diretamente no manual (prática usual).</summary>
    [JsonIgnore]
    public static readonly string[] NaoConferidos =
    {
        "LadoAdvertencia / OrlaPretaAdvRel / MargemAdvRel (dimensões da advertência)",
        "AlturaLetraIndicacao (altura de letras das placas de indicação)",
        "AlturaLegenda / FatorLarguraLegenda (legendas no pavimento)",
        "EspacamentoTachas* (espaçamento de tachas)",
        "FatorTaper (adota a fórmula l = 0,5·v·d da marca de transição de largura do vol. IV), espaçamento de cones, ZonaProtecao e distância entre placas de obra",
        "Defensas (espaçamento de postes, lâmina e terminais)",
        "Semafórica (altura de coluna, braço projetado)",
        "EngastamentoSuporte e materiais padrão",
    };

    // ------------------------------------------------------------------ utilidades
    public static double PorVelocidade(List<FaixaVel> tabela, double v)
    {
        foreach (var f in tabela.OrderBy(f => f.Ate)) if (v < f.Ate) return f.Valor;
        return tabela.Count > 0 ? tabela.OrderBy(f => f.Ate).Last().Valor : 0;
    }

    public static Cadencia CadenciaPara(List<Cadencia> tabela, double v)
    {
        foreach (var c in tabela.OrderBy(f => f.Ate)) if (v < c.Ate) return c;
        return tabela.OrderBy(f => f.Ate).Last();
    }

    public double AlturaLivre(TipoVia via) => via == TipoVia.Urbana ? AlturaLivreUrbana : AlturaLivreRural;

    private static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string ParaJson() => JsonSerializer.Serialize(this, Opcoes);
    public static Normas DeJson(string json) => JsonSerializer.Deserialize<Normas>(json, Opcoes) ?? new Normas();

    /// <summary>Carrega do arquivo (se existir e for válido); senão, os padrões. Mensagem de erro em <paramref name="erro"/>.</summary>
    public static Normas Carregar(string? caminho, out string? erro)
    {
        erro = null;
        if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return new Normas();
        try { return DeJson(File.ReadAllText(caminho)); }
        catch (Exception ex) { erro = $"normas.json inválido ({ex.Message}); usando os padrões."; return new Normas(); }
    }

    public void Salvar(string caminho)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(caminho, ParaJson());
    }
}
