using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Lista propriedades e métodos públicos de tipos de uma DLL, só lendo os metadados (sem carregar).
var dll = args[0];
var wanted = args.Skip(1).ToHashSet();
using var pe = new PEReader(File.OpenRead(dll));
var md = pe.GetMetadataReader();
var provider = new Names(md);
foreach (var th in md.TypeDefinitions)
{
    var t = md.GetTypeDefinition(th);
    var full = md.GetString(t.Namespace) + "." + md.GetString(t.Name);
    if (wanted.Count > 0 && !wanted.Contains(full) && !wanted.Contains(md.GetString(t.Name))) continue;
    var baseName = t.BaseType.IsNil ? "" : provider.Name(t.BaseType);
    Console.WriteLine($"== {full} : {baseName}");
    foreach (var ph in t.GetProperties())
    {
        var p = md.GetPropertyDefinition(ph);
        var acc = p.GetAccessors();
        bool Public(MethodDefinitionHandle h) => !h.IsNil && (md.GetMethodDefinition(h).Attributes & System.Reflection.MethodAttributes.Public) != 0;
        if (!Public(acc.Getter) && !Public(acc.Setter)) continue;
        var sig = p.DecodeSignature(provider, null);
        Console.WriteLine($"  P {sig.ReturnType} {md.GetString(p.Name)} {(Public(acc.Getter) ? "get" : "")} {(Public(acc.Setter) ? "set" : "")}");
    }
    foreach (var mh in t.GetMethods())
    {
        var m = md.GetMethodDefinition(mh);
        if ((m.Attributes & System.Reflection.MethodAttributes.Public) == 0) continue;
        var name = md.GetString(m.Name);
        if (name.StartsWith("get_") || name.StartsWith("set_") || name.StartsWith("add_") || name.StartsWith("remove_")) continue;
        var sig = m.DecodeSignature(provider, null);
        var st = (m.Attributes & System.Reflection.MethodAttributes.Static) != 0 ? "static " : "";
        Console.WriteLine($"  M {st}{sig.ReturnType} {name}({string.Join(", ", sig.ParameterTypes)})");
    }
}

internal sealed class Names(MetadataReader md) : ISignatureTypeProvider<string, object?>
{
    public string Name(EntityHandle h) => h.Kind switch
    {
        HandleKind.TypeDefinition => md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)h).Name),
        HandleKind.TypeReference => md.GetString(md.GetTypeReference((TypeReferenceHandle)h).Name),
        HandleKind.TypeSpecification => "spec",
        _ => "?"
    };
    public string GetArrayType(string e, ArrayShape s) => e + "[,]";
    public string GetByReferenceType(string e) => "ref " + e;
    public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr";
    public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object? c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object? c, int i) => "!" + i;
    public string GetModifiedType(string m, string u, bool r) => u;
    public string GetPinnedType(string e) => e;
    public string GetPointerType(string e) => e + "*";
    public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
    public string GetSZArrayType(string e) => e + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => r.GetString(r.GetTypeDefinition(h).Name);
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => r.GetString(r.GetTypeReference(h).Name);
    public string GetTypeFromSpecification(MetadataReader r, object? c, TypeSpecificationHandle h, byte k) => "spec";
}
