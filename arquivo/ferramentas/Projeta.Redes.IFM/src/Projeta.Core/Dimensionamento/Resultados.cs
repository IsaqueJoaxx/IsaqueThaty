using Projeta.Core.Comum;
using Projeta.Core.Redes;

namespace Projeta.Core.Dimensionamento;

public sealed class ResultadoRede
{
    public DateTime Data { get; set; } = DateTime.Now;
    public TipoRede Tipo { get; set; }
    public string Metodo { get; set; } = "";
    public List<ResultadoTrecho> Trechos { get; set; } = new();
    public List<ResultadoNo> Nos { get; set; } = new();
    public List<Mensagem> Mensagens { get; set; } = new();

    public ResultadoTrecho? Trecho(string id) => Trechos.FirstOrDefault(t => t.TrechoId == id);
    public ResultadoNo? No(string id) => Nos.FirstOrDefault(n => n.NoId == id);
    public bool TemErros => Mensagens.Any(m => m.Gravidade == Gravidade.Erro) || Trechos.Any(t => t.Erros.Count > 0);
}

public sealed class ResultadoTrecho
{
    public string TrechoId { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Montante { get; set; } = "";
    public string Jusante { get; set; } = "";
    public double Comprimento { get; set; }

    // hidrologia (drenagem)
    public double AreaLocalHa { get; set; }
    public double AreaAcumuladaHa { get; set; }
    public double SomaCA { get; set; }
    public double Tc { get; set; }
    public double Intensidade { get; set; }

    // esgoto
    public double VazaoInicialLs { get; set; }
    public double VazaoFinalLs { get; set; }
    public double TaxaInicial { get; set; }
    public double TaxaFinal { get; set; }

    /// <summary>Vazão de dimensionamento (m³/s).</summary>
    public double Vazao { get; set; }

    public FormaSecao Forma { get; set; }
    /// <summary>Diâmetro (circular) ou largura (galeria), m.</summary>
    public double Diametro { get; set; }
    /// <summary>Altura interna (m). Para circular = diâmetro.</summary>
    public double Altura { get; set; }
    public int Celulas { get; set; } = 1;
    public double Espessura { get; set; }
    public string Secao => Forma == FormaSecao.Circular ? (Celulas > 1 ? $"{Celulas}xØ{Diametro * 1000:0}" : $"Ø{Diametro * 1000:0}") : (Celulas > 1 ? $"{Celulas}x" : "") + $"{Diametro:0.00}x{Altura:0.00}";

    public double Declividade { get; set; }
    public double CotaTerrenoMontante { get; set; }
    public double CotaTerrenoJusante { get; set; }
    public double CotaFundoMontante { get; set; }
    public double CotaFundoJusante { get; set; }
    public double GeratrizSupMontante => CotaFundoMontante + Altura;
    public double GeratrizSupJusante => CotaFundoJusante + Altura;
    public double RecobrimentoMontante => CotaTerrenoMontante - GeratrizSupMontante - Espessura;
    public double RecobrimentoJusante => CotaTerrenoJusante - GeratrizSupJusante - Espessura;
    public double ProfundidadeMontante => CotaTerrenoMontante - CotaFundoMontante;
    public double ProfundidadeJusante => CotaTerrenoJusante - CotaFundoJusante;

    /// <summary>Lâmina d'água (m) para a vazão de dimensionamento.</summary>
    public double AlturaLamina { get; set; }
    public double LaminaRelativa => Altura > 0 ? AlturaLamina / Altura : double.NaN;
    public double Velocidade { get; set; }
    public double VazaoPlena { get; set; }
    public double VelocidadePlena { get; set; }
    public double TempoPercurso { get; set; }

    // esgoto: verificações adicionais
    public double TensaoTrativa { get; set; }
    public double VelocidadeCritica { get; set; }
    public double LaminaInicial { get; set; }

    public List<string> Avisos { get; set; } = new();
    public List<string> Erros { get; set; } = new();
    public string Situacao => Erros.Count > 0 ? "ERRO" : Avisos.Count > 0 ? "AVISO" : "OK";
    public string Observacoes => string.Join("; ", Erros.Concat(Avisos));
}

public sealed class ResultadoNo
{
    public string NoId { get; set; } = "";
    public string Nome { get; set; } = "";
    public TipoNo Tipo { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Estaca { get; set; } = double.NaN;
    public double Afastamento { get; set; } = double.NaN;
    public double CotaTampa { get; set; }
    public double CotaFundo { get; set; }
    public double Profundidade => CotaTampa - CotaFundo;
    /// <summary>Maior degrau entre um tubo de chegada e o fundo do nó (m).</summary>
    public double DegrauMaximo { get; set; }
    public bool ExigeTuboQueda { get; set; }
    /// <summary>Maior diâmetro/largura conectado (m) — usado para escolher o tamanho do PV.</summary>
    public double MaiorSecao { get; set; }
    public double VazaoSaida { get; set; }
    public List<string> Avisos { get; set; } = new();
}
