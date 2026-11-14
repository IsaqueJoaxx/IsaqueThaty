using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Projeta.Core.Expressoes;

namespace Projeta.Core.Modelador;

/// <summary>Campo que aceita número ou expressão (texto) no JSON.</summary>
[JsonConverter(typeof(ExprConverter))]
public readonly struct Expr
{
    public string Texto { get; }
    public Expr(string texto) { Texto = texto ?? "0"; }
    public static implicit operator Expr(string s) => new(s);
    public static implicit operator Expr(double d) => new(d.ToString("R", CultureInfo.InvariantCulture));
    public bool Vazio => string.IsNullOrWhiteSpace(Texto);
    public double Avaliar(IContextoExpressao ctx) => Expressao.Numero(Texto, ctx);
    public Valor AvaliarValor(IContextoExpressao ctx) => Expressao.Compilar(Texto).Avaliar(ctx);
    public override string ToString() => Texto;
}

public sealed class ExprConverter : JsonConverter<Expr>
{
    public override Expr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => new Expr(reader.GetDouble().ToString("R", CultureInfo.InvariantCulture)),
            JsonTokenType.String => new Expr(reader.GetString() ?? "0"),
            JsonTokenType.True => new Expr("1"),
            JsonTokenType.False => new Expr("0"),
            JsonTokenType.Null => new Expr("0"),
            _ => throw new JsonException("Esperado número ou expressão"),
        };
    }

    public override void Write(Utf8JsonWriter writer, Expr value, JsonSerializerOptions options)
    {
        if (double.TryParse(value.Texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && !value.Texto.Contains('e', StringComparison.OrdinalIgnoreCase))
            writer.WriteNumberValue(d);
        else writer.WriteStringValue(value.Texto);
    }
}

/// <summary>Parâmetro editável de um dispositivo ou seção-tipo.</summary>
public sealed class ParametroDef
{
    public string Nome { get; set; } = "";
    /// <summary>"numero" (padrão) ou "lista" (valores em Opcoes; o valor numérico é o índice) .</summary>
    public string Tipo { get; set; } = "numero";
    /// <summary>Valor padrão — pode ser uma expressão que usa outros parâmetros (ex.: "se(DiametroMaximo&lt;=0.6, 1.0, 1.2)").</summary>
    public Expr Padrao { get; set; } = new("0");
    public double? Min { get; set; }
    public double? Max { get; set; }
    public List<string>? Opcoes { get; set; }
    public string Unidade { get; set; } = "m";
    public string Descricao { get; set; } = "";
    /// <summary>"rede" = preenchido automaticamente pela rede (Profundidade, CotaFundo, DiametroMaximo...).</summary>
    public string Origem { get; set; } = "";
    public override string ToString() => Nome;
}

public sealed class VariavelDef
{
    public string Nome { get; set; } = "";
    public Expr Expr { get; set; } = new("0");
}

/// <summary>Ponto 2D/3D com coordenadas em expressão.</summary>
public sealed class PontoExpr
{
    public Expr X { get; set; } = new("0");
    public Expr Y { get; set; } = new("0");
    public Expr Z { get; set; } = new("0");
    /// <summary>Se verdadeiro, o elemento é um arco (centro X,Y; Raio; De/Ate em graus; Segmentos).</summary>
    public bool Arco { get; set; }
    public Expr Raio { get; set; } = new("0");
    public Expr De { get; set; } = new("0");
    public Expr Ate { get; set; } = new("0");
    public int Segmentos { get; set; } = 12;
}

/// <summary>Operação de modelagem de sólidos (estilo CSG).</summary>
public sealed class OperacaoSolida
{
    public string Id { get; set; } = "";
    /// <summary>
    /// caixa | cilindro | tronco | esfera | extrusao | revolucao | uniao | subtrair | intersecao | mover | rotacionar | copiar
    /// </summary>
    public string Op { get; set; } = "caixa";
    /// <summary>Só cria a operação se a condição for verdadeira (vazio = sempre).</summary>
    public string? Condicao { get; set; }

    // primitivas
    public Expr? Comprimento { get; set; }   // X (caixa)
    public Expr? Largura { get; set; }       // Y (caixa)
    public Expr? Altura { get; set; }        // Z / altura de extrusão / altura do cilindro
    public Expr? Raio { get; set; }
    public Expr? RaioTopo { get; set; }
    /// <summary>Ponto de base: caixa = centro da face inferior; cilindro = centro da base; esfera = centro.</summary>
    public PontoExpr? Base { get; set; }
    /// <summary>Direção do eixo do cilindro/tronco (padrão Z).</summary>
    public PontoExpr? Eixo { get; set; }
    /// <summary>Perfil 2D para extrusão (no plano indicado) ou revolução (x = raio, y = z).</summary>
    public List<PontoExpr>? Perfil { get; set; }
    /// <summary>Plano do perfil de extrusão: XY (extrude em Z), XZ (extrude em Y), YZ (extrude em X).</summary>
    public string Plano { get; set; } = "XY";
    public Expr? Angulo { get; set; }

    // booleanas e transformações
    public string? De { get; set; }
    public List<string>? Com { get; set; }
    public string? Alvo { get; set; }
    public PontoExpr? Deslocamento { get; set; }
}

public sealed class ElementoPlanta
{
    /// <summary>circulo | linha | polilinha | retangulo | arco | texto</summary>
    public string Tipo { get; set; } = "circulo";
    public string? Condicao { get; set; }
    public PontoExpr? Centro { get; set; }
    public Expr? Raio { get; set; }
    public Expr? Largura { get; set; }
    public Expr? Altura { get; set; }
    public List<PontoExpr>? Pontos { get; set; }
    public bool Fechada { get; set; }
    public Expr? De { get; set; }
    public Expr? Ate { get; set; }
    public string? Texto { get; set; }
    /// <summary>"contorno" (padrão), "tampa", "eixo" — usado para escolher layer/estilo.</summary>
    public string Camada { get; set; } = "contorno";
}

public sealed class QuantitativoDef
{
    public string Nome { get; set; } = "";
    public string Unidade { get; set; } = "";
    /// <summary>Expressão; pode usar volume(id) e area_planta().</summary>
    public Expr Expr { get; set; } = new("0");
}

/// <summary>Definição paramétrica de um dispositivo (PV, caixa, boca de lobo, ala...).</summary>
public sealed class DefinicaoDispositivo
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    /// <summary>PV | BocaDeLobo | Caixa | Exutorio | Bueiro | Generico</summary>
    public string Categoria { get; set; } = "PV";
    public string Descricao { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Versao { get; set; } = "1.0";
    /// <summary>Alinhar o dispositivo com o tubo de saída (ou de chegada, no exutório).</summary>
    public bool AlinharComTubo { get; set; } = true;
    /// <summary>Distância (m) do centro até a face interna da parede, onde começam os furos dos tubos. Expressão.</summary>
    public Expr? RaioFuros { get; set; }
    public List<ParametroDef> Parametros { get; set; } = new();
    public List<VariavelDef> Variaveis { get; set; } = new();
    public List<OperacaoSolida> Solidos { get; set; } = new();
    /// <summary>Ids dos sólidos de saída (vazio = o último).</summary>
    public List<string> Saida { get; set; } = new();
    public List<ElementoPlanta> Planta { get; set; } = new();
    public List<QuantitativoDef> Quantitativos { get; set; } = new();

    public override string ToString() => $"{Id} — {Nome}";
}

/// <summary>Seção-tipo de dispositivo longitudinal (valeta, sarjeta, meio-fio, canaleta...).</summary>
public sealed class SecaoTipo
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "Valeta";
    public string Descricao { get; set; } = "";
    public List<ParametroDef> Parametros { get; set; } = new();
    public List<VariavelDef> Variaveis { get; set; } = new();
    /// <summary>Contorno molhado: da borda esquerda, pelo fundo, até a borda direita. Origem (0,0) = ponto de referência no eixo do dispositivo.</summary>
    public List<PontoExpr> ContornoHidraulico { get; set; } = new();
    /// <summary>Polígono fechado do material (concreto).</summary>
    public List<PontoExpr> Concreto { get; set; } = new();
    /// <summary>Polígono fechado da escavação (opcional; se vazio usa o retângulo envolvente do concreto).</summary>
    public List<PontoExpr> Escavacao { get; set; } = new();
    public override string ToString() => $"{Id} — {Nome}";
}
