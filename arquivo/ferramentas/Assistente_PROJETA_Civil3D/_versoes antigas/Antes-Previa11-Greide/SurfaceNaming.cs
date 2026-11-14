using System.Text.RegularExpressions;

namespace Projeta.Civil3D;

internal static class SurfaceNaming
{
    public const string Unidentified = "Não identificado";
    public const string Ground = "Terreno natural";
    public const string Design = "Projeto";
    public const string Volume = "Superfície de volume";

    // Zeros à esquerda são aceitos (01, 002). Um separador ou o fim do nome continua obrigatório:
    // 10, 20 e 012 não viram as funções 1 e 2.
    private static readonly Regex RolePattern =
        new(@"^0*([12])(?:\s|[-–—_.:]|$)", RegexOptions.CultureInvariant);

    public static string Role(string name)
    {
        var match = RolePattern.Match(name.TrimStart());
        return match.Success ? (match.Groups[1].Value == "1" ? Ground : Design) : Unidentified;
    }

    // Superfícies de volume não concorrem a terreno natural nem a projeto.
    // Texto curto para caber na coluna; as candidatas aparecem na própria lista.
    public static string Suggestion(SurfaceInfo row, IReadOnlyList<SurfaceInfo> rows)
    {
        if (row.IsVolume) return Volume;
        var role = Role(row.Name);
        if (role == Unidentified) return role;
        var candidates = rows.Count(other => !other.IsVolume && Role(other.Name) == role);
        return candidates > 1 ? role + " · escolher" : role;
    }

    // Primeira superfície (que não seja de volume) com a função pedida, para pré-selecionar nas abas.
    public static SurfaceInfo? First(IReadOnlyList<SurfaceInfo> rows, string role) =>
        rows.FirstOrDefault(s => !s.IsVolume && Role(s.Name) == role);
}
