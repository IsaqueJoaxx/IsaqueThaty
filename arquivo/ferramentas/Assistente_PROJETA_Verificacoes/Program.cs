using Projeta.Civil3D;

foreach (var (name, expected) in new[] {
    ("1 — Primitivo", "Terreno natural"), ("2 — Projeto", "Projeto"),
    ("1_Primitivo", "Terreno natural"), (" 2-Projeto", "Projeto"),
    ("10 — Outro", "Não identificado"), ("20 — Outro", "Não identificado"),
    ("Primitivo", "Não identificado"), ("", "Não identificado") })
    if (SurfaceNaming.Role(name) != expected) throw new Exception("Classificação incorreta: " + name);
var candidates = new[] { new SurfaceRow("1 — A", "TinSurface", "A"), new SurfaceRow("1 — B", "TinSurface", "B"), new SurfaceRow("2 — Projeto", "TinSurface", "C") };
if (!SurfaceNaming.Suggestion("1 — A", candidates).Contains("escolher")) throw new Exception("Ambiguidade não indicada");
if (!SurfaceNaming.Suggestion("2 — Projeto", candidates).Contains("sugestão")) throw new Exception("Sugestão ausente");
Console.WriteLine("OK: oito nomes e casos de candidato único/múltiplos candidatos. Sem acesso a desenhos.");

namespace Projeta.Civil3D { public sealed record SurfaceRow(string Name, string Type, string Handle); }
