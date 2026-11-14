using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Veiculos;

namespace Projeta.Giro.Infra;

/// <summary>Preferências do usuário (valem para todos os desenhos): %APPDATA%\Projeta\Giro\config.json.</summary>
internal sealed class Config
{
    public string VeiculoAtual { get; set; } = "SR";
    /// <summary>Velocidade de manobra (km/h) [PREMISSA: 10 km/h para conversões em baixa velocidade].</summary>
    public double Velocidade { get; set; } = 10;
    /// <summary>Folga lateral exigida na verificação (m) [PREMISSA].</summary>
    public double Folga { get; set; } = 0.50;
    /// <summary>Desenhar o veículo a cada X m ao longo da varredura (0 = só início e fim).</summary>
    public double IntervaloVeiculos { get; set; } = 0;
    public bool DesenharRodas { get; set; } = true;
    /// <summary>Fator de tempo da animação (1 = tempo real).</summary>
    public double FatorAnimacao { get; set; } = 3;

    public static string Pasta => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "Giro");
    public static string PastaVeiculos => Path.Combine(Pasta, "veiculos");
    private static string Arquivo => Path.Combine(Pasta, "config.json");

    private static Config? _atual;
    public static Config Atual => _atual ??= Ler();

    private static Config Ler()
    {
        try { if (File.Exists(Arquivo)) return JsonGiro.Ler<Config>(File.ReadAllText(Arquivo)) ?? new Config(); }
        catch { }
        return new Config();
    }

    public void Salvar()
    {
        try
        {
            Directory.CreateDirectory(Pasta);
            File.WriteAllText(Arquivo, JsonGiro.Serializar(this));
        }
        catch { }
    }

    private static Biblioteca? _bib;
    public static Biblioteca Biblioteca => _bib ??= Biblioteca.Carregar(PastaVeiculos);
    public static void RecarregarBiblioteca() => _bib = null;

    public Veiculo Veiculo() => Biblioteca.Obter(VeiculoAtual) ?? Biblioteca.Ordenados.First();
}
