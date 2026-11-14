using System.ComponentModel;
using System.Globalization;

namespace Projeta.Core.Comum;

/// <summary>Permite editar List&lt;double&gt; como texto "0,40; 0,50; 0,60" em PropertyGrid.</summary>
public sealed class ListaNumerosConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? c, Type t) => t == typeof(string) || base.CanConvertFrom(c, t);
    public override bool CanConvertTo(ITypeDescriptorContext? c, Type? t) => t == typeof(string) || base.CanConvertTo(c, t);

    public override object? ConvertFrom(ITypeDescriptorContext? c, CultureInfo? cul, object v)
    {
        if (v is string s)
            return s.Split(new[] { ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Num.Ler(x)).Where(x => !double.IsNaN(x)).ToList();
        return base.ConvertFrom(c, cul, v);
    }

    public override object? ConvertTo(ITypeDescriptorContext? c, CultureInfo? cul, object? v, Type t)
    {
        if (t == typeof(string) && v is IEnumerable<double> l) return string.Join("; ", l.Select(x => x.ToString("0.###", Num.Br)));
        return base.ConvertTo(c, cul, v, t);
    }
}

/// <summary>Permite editar List&lt;string&gt; como texto separado por ';'.</summary>
public sealed class ListaTextoConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? c, Type t) => t == typeof(string) || base.CanConvertFrom(c, t);
    public override bool CanConvertTo(ITypeDescriptorContext? c, Type? t) => t == typeof(string) || base.CanConvertTo(c, t);
    public override object? ConvertFrom(ITypeDescriptorContext? c, CultureInfo? cul, object v) =>
        v is string s ? s.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() : base.ConvertFrom(c, cul, v);
    public override object? ConvertTo(ITypeDescriptorContext? c, CultureInfo? cul, object? v, Type t) =>
        t == typeof(string) && v is IEnumerable<string> l ? string.Join("; ", l) : base.ConvertTo(c, cul, v, t);
}

/// <summary>Permite editar Dictionary&lt;string,double&gt; como "Nome=valor; Nome2=valor2".</summary>
public sealed class DicionarioNumerosConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? c, Type t) => t == typeof(string) || base.CanConvertFrom(c, t);
    public override bool CanConvertTo(ITypeDescriptorContext? c, Type? t) => t == typeof(string) || base.CanConvertTo(c, t);
    public override object? ConvertFrom(ITypeDescriptorContext? c, CultureInfo? cul, object v)
    {
        if (v is not string s) return base.ConvertFrom(c, cul, v);
        var d = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var par in s.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = par.Split('=', 2);
            if (kv.Length == 2 && Num.TentarLer(kv[1], out var x)) d[kv[0].Trim()] = x;
        }
        return d;
    }
    public override object? ConvertTo(ITypeDescriptorContext? c, CultureInfo? cul, object? v, Type t) =>
        t == typeof(string) && v is IDictionary<string, double> d
            ? string.Join("; ", d.Where(kv => !kv.Key.StartsWith("__")).Select(kv => $"{kv.Key}={kv.Value.ToString("0.###", Num.Br)}"))
            : base.ConvertTo(c, cul, v, t);
}
