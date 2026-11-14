using System.ComponentModel;
using Projeta.Core.Expressoes;

namespace Projeta.Core.Hidrologia;

public enum TipoEquacaoChuva
{
    /// <summary>i = K·T^a / (t + b)^c — forma mais usada no Brasil.</summary>
    Generalizada,
    /// <summary>Fórmula livre, com variáveis T (anos) e t (minutos) e constantes adicionais.</summary>
    Expressao,
}

/// <summary>
/// Equação de chuvas intensas (IDF). Resultado: intensidade i em mm/h,
/// com T = tempo de retorno (anos) e t = duração (min).
/// </summary>
public sealed class EquacaoChuva
{
    [Category("Identificação"), DisplayName("Id")] public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    [Category("Identificação"), DisplayName("Local")] public string Local { get; set; } = "";
    [Category("Identificação"), DisplayName("UF")] public string Uf { get; set; } = "";
    [Category("Identificação"), DisplayName("Fonte / referência")] public string Fonte { get; set; } = "";
    [Category("Identificação"), DisplayName("Observação")] public string Observacao { get; set; } = "";

    [Category("Modelo"), DisplayName("Tipo")] public TipoEquacaoChuva Tipo { get; set; } = TipoEquacaoChuva.Generalizada;
    [Category("Modelo"), DisplayName("K")] public double K { get; set; }
    [Category("Modelo"), DisplayName("a (expoente de T)")] public double A { get; set; }
    [Category("Modelo"), DisplayName("b (min)")] public double B { get; set; }
    [Category("Modelo"), DisplayName("c (expoente)")] public double C { get; set; }

    [Category("Modelo"), DisplayName("Expressão (tipo Expressão)"),
     Description("Use T (anos) e t (min). Ex.: 1076.16*T^0.114/(t+9.783)^0.724")]
    public string Formula { get; set; } = "";

    [Category("Modelo"), DisplayName("Constantes extras"), Description("Constantes usadas na expressão, ex.: PA=1500"), TypeConverter(typeof(Comum.DicionarioNumerosConverter))]
    public Dictionary<string, double> Constantes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Category("Validade"), DisplayName("Duração mínima (min)")] public double DuracaoMin { get; set; } = 5;
    [Category("Validade"), DisplayName("Duração máxima (min)")] public double DuracaoMax { get; set; } = 1440;
    [Category("Validade"), DisplayName("T máximo (anos)")] public double TMax { get; set; } = 100;

    public string Nome => string.IsNullOrWhiteSpace(Uf) ? Local : $"{Local}/{Uf}";

    /// <summary>Intensidade (mm/h).</summary>
    public double Intensidade(double tempoRetornoAnos, double duracaoMin)
    {
        if (tempoRetornoAnos <= 0) throw new ArgumentOutOfRangeException(nameof(tempoRetornoAnos));
        if (duracaoMin <= 0) throw new ArgumentOutOfRangeException(nameof(duracaoMin));
        switch (Tipo)
        {
            case TipoEquacaoChuva.Generalizada:
                return K * Math.Pow(tempoRetornoAnos, A) / Math.Pow(duracaoMin + B, C);
            default:
                {
                    var ctx = new ContextoDicionario();
                    foreach (var kv in Constantes) ctx[kv.Key] = kv.Value;
                    ctx["TR"] = tempoRetornoAnos;
                    ctx["td"] = duracaoMin;
                    // nomes não diferenciam maiúsculas; "T" (anos) e "t" (min) são tratados à parte
                    var ctxDuplo = new ContextoTt(tempoRetornoAnos, duracaoMin, ctx);
                    return Expressao.Numero(Formula, ctxDuplo);
                }
        }
    }

    /// <summary>Altura de chuva (mm) para a duração.</summary>
    public double Altura(double tempoRetornoAnos, double duracaoMin) => Intensidade(tempoRetornoAnos, duracaoMin) * duracaoMin / 60.0;

    public string Descricao()
    {
        return Tipo == TipoEquacaoChuva.Generalizada
            ? $"i = {K:0.###}·T^{A:0.####} / (t + {B:0.###})^{C:0.####}"
            : "i = " + Formula;
    }

    public override string ToString() => $"{Nome} — {Descricao()}";

    /// <summary>Contexto que diferencia "T" (tempo de retorno) de "t" (duração).</summary>
    private sealed class ContextoTt : IContextoExpressao
    {
        private readonly double _T, _t; private readonly IContextoExpressao _resto;
        public ContextoTt(double T, double t, IContextoExpressao resto) { _T = T; _t = t; _resto = resto; }
        public bool TentarVariavel(string nome, out Valor valor)
        {
            if (nome == "T") { valor = _T; return true; }
            if (nome == "t") { valor = _t; return true; }
            return _resto.TentarVariavel(nome, out valor);
        }
    }
}

/// <summary>Catálogo de equações IDF. As equações abaixo têm fonte publicada; confira a validade para o seu local.</summary>
public static class CatalogoEquacoes
{
    public static List<EquacaoChuva> Padrao() => new()
    {
        new EquacaoChuva
        {
            Id = "BH-UFU2024", Local = "Belo Horizonte", Uf = "MG", Tipo = TipoEquacaoChuva.Generalizada,
            K = 1076.16, A = 0.114, B = 9.783, C = 0.724,
            Fonte = "Atualização de modelos de chuva intensa para municípios do Sudeste e Centro-Oeste do Brasil — Caminhos de Geografia (UFU), eq. 7",
        },
        new EquacaoChuva
        {
            Id = "BSB-UFU2024", Local = "Brasília", Uf = "DF", Tipo = TipoEquacaoChuva.Generalizada,
            K = 816.70, A = 0.081, B = 8.990, C = 0.704,
            Fonte = "Atualização de modelos de chuva intensa para municípios do Sudeste e Centro-Oeste do Brasil — Caminhos de Geografia (UFU), eq. 6",
        },
        new EquacaoChuva
        {
            Id = "LAV-UFU2024", Local = "Lavras", Uf = "MG", Tipo = TipoEquacaoChuva.Generalizada,
            K = 815.55, A = 0.137, B = 9.782, C = 0.724,
            Fonte = "Atualização de modelos de chuva intensa para municípios do Sudeste e Centro-Oeste do Brasil — Caminhos de Geografia (UFU), eq. 8",
        },
        new EquacaoChuva
        {
            Id = "RMBH-PINHEIRO", Local = "RMBH (regional, Pinheiro 1997)", Uf = "MG", Tipo = TipoEquacaoChuva.Expressao,
            Formula = "0.76542*(t/60)^(-0.7059)*PA^0.5360*(interp(t,10,0.932,15,0.933,30,0.936,45,0.932,60,0.930,120,0.931,180,0.930,240,0.930,480,0.929,840,0.921,1440,0.913) - interp(t,10,0.220,15,0.217,30,0.209,45,0.221,60,0.229,120,0.226,180,0.229,240,0.220,480,0.232,840,0.259,1440,0.283)*ln(-ln(1-1/T)))",
            Constantes = new(StringComparer.OrdinalIgnoreCase) { ["PA"] = 1500 },
            DuracaoMin = 10, DuracaoMax = 1440, TMax = 200,
            Fonte = "Equação regional da RMBH (Pinheiro, 1997), citada no relatório IDF de Sarzedo/MG — SGB/CPRM (2022)",
            Observacao = "PA = precipitação média anual do local (mm), lida no mapa de isoietas. Ajuste antes de usar. α e β* interpolados linearmente entre as durações tabeladas.",
        },
    };
}
