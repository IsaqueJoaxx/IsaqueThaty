using System.Reflection;
// uso: apidump <pastaAcad> <Tipo1,Tipo2,...> [filtroMembro]
var dir = args[0];
var wanted = args[1].Split(',');
var memberFilter = args.Length > 2 ? args[2].ToLowerInvariant() : null;
var paths = Directory.GetFiles(dir, "*.dll").Concat(Directory.GetFiles(Path.Combine(dir, "C3D"), "*.dll"))
    .Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
var resolver = new PathAssemblyResolver(paths);
using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
string[] asms = { "acdbmgd.dll", "accoremgd.dll", "acmgd.dll", "C3D\\AeccDbMgd.dll", "C3D\\AeccLandMgd.dll" };
foreach (var a in asms)
{
    var p = Path.Combine(dir, a);
    if (!File.Exists(p)) continue;
    Assembly asm;
    try { asm = mlc.LoadFromAssemblyPath(p); } catch { continue; }
    Type[] types;
    try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray()!; }
    foreach (var t in types)
    {
        if (!wanted.Any(w => t.FullName == w || t.Name == w)) continue;
        Console.WriteLine($"=== {t.FullName} : {SafeBase(t)}  [{a}]");
        foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (memberFilter != null && !m.Name.ToLowerInvariant().Contains(memberFilter)) continue;
            try
            {
                switch (m)
                {
                    case MethodInfo mi when !mi.IsSpecialName:
                        Console.WriteLine($"  M {(mi.IsStatic ? "static " : "")}{Short(mi.ReturnType)} {mi.Name}({string.Join(", ", mi.GetParameters().Select(x => Short(x.ParameterType) + " " + x.Name))})");
                        break;
                    case PropertyInfo pi:
                        Console.WriteLine($"  P {Short(pi.PropertyType)} {pi.Name} {(pi.CanWrite ? "{get;set;}" : "{get;}")}");
                        break;
                    case ConstructorInfo ci:
                        Console.WriteLine($"  C ({string.Join(", ", ci.GetParameters().Select(x => Short(x.ParameterType) + " " + x.Name))})");
                        break;
                    case FieldInfo fi when t.IsEnum:
                        if (fi.IsStatic) Console.WriteLine($"  E {fi.Name}");
                        break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"  ? {m.Name} ({ex.GetType().Name})"); }
        }
    }
}
static string SafeBase(Type t) { try { return t.BaseType?.Name ?? ""; } catch { return "?"; } }
static string Short(Type t) { try { return t.IsGenericType ? t.Name.Split('`')[0] + "<" + string.Join(",", t.GetGenericArguments().Select(Short)) + ">" : t.Name; } catch { return "?"; } }
