using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projeta.Giro.Core.Comum;

/// <summary>Formatação e leitura de números (vírgula ou ponto decimal).</summary>
public static class Num
{
    public static readonly CultureInfo Br = CriarBr();
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static CultureInfo CriarBr()
    {
        var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.NumberDecimalSeparator = ",";
        c.NumberFormat.NumberGroupSeparator = ".";
        return c;
    }

    public static string F(double v, int casas = 2) =>
        double.IsFinite(v) ? v.ToString("F" + casas, Br) : "-";

    public static bool TentarLer(string? s, out double v)
    {
        v = double.NaN;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().Replace(" ", "");
        if (s.Contains(',') && s.Contains('.'))
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.')) s = s.Replace(".", "").Replace(',', '.');
            else s = s.Replace(",", "");
        }
        else if (s.Contains(',')) s = s.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Float, Inv, out v) && double.IsFinite(v);
    }

    /// <summary>Estaca no padrão de 20 m: 12+5,30.</summary>
    public static string Estaca(double m, double intervalo = 20)
    {
        if (!double.IsFinite(m)) return "-";
        int e = (int)Math.Floor(m / intervalo + 1e-9);
        double r = Math.Max(0, m - e * intervalo);
        return $"{e}+{r.ToString("F2", Br)}";
    }

    public static double Rad(double graus) => graus * Math.PI / 180.0;
    public static double Graus(double rad) => rad * 180.0 / Math.PI;
}

public static class JsonGiro
{
    public static readonly JsonSerializerOptions Opcoes = Criar(true);
    public static readonly JsonSerializerOptions Compacto = Criar(false);

    private static JsonSerializerOptions Criar(bool indentar)
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = indentar,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public static string Serializar<T>(T obj, bool indentar = true) => JsonSerializer.Serialize(obj, indentar ? Opcoes : Compacto);
    public static T? Ler<T>(string json) => JsonSerializer.Deserialize<T>(json, Opcoes);
    public static T Clonar<T>(T obj) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj, Compacto), Opcoes)!;
}

public enum Gravidade { Info, Aviso, Erro }

public sealed record Mensagem(Gravidade Gravidade, string Texto)
{
    public override string ToString() => Gravidade switch
    {
        Gravidade.Erro => "[ERRO] ",
        Gravidade.Aviso => "[AVISO] ",
        _ => "",
    } + Texto;
}

/// <summary>Erro de dado de entrada (mensagem pronta para o usuário).</summary>
public sealed class ErroGiro : Exception
{
    public ErroGiro(string msg) : base(msg) { }
}
