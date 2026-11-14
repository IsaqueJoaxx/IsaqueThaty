using System.Text.RegularExpressions;

namespace Projeta.Civil3D;

internal static class SurfaceNaming
{
    // A separator or end is required: 10 and 20 must not match roles 1 and 2.
    public static string Role(string name)
    {
        var match = Regex.Match(name.TrimStart(), @"^([12])(?:\s|[-–—_.:]|$)");
        return match.Success ? (match.Groups[1].Value == "1" ? "Terreno natural" : "Projeto") : "Não identificado";
    }

    public static string Suggestion(string name, IEnumerable<SurfaceRow> rows)
    {
        var role = Role(name);
        if (role == "Não identificado") return role;
        return rows.Count(row => Role(row.Name) == role) > 1
            ? role + " — escolher entre candidatos"
            : role + " — sugestão pelo nome";
    }
}
