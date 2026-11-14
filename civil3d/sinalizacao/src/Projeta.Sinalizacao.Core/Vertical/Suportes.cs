using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Normas;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Core.Vertical;

public enum TipoSuporte
{
    ColunaSimples, ColunaDupla, ColunaTripla, BracoProjetado, BracoProjetadoDuplo,
    SemiPorticoSimples, SemiPorticoDuplo, Portico, Existente,
}

/// <summary>Quantitativo de um suporte: comprimento de colunas e de vigas/braços.</summary>
public sealed record QuantSuporte(TipoSuporte Tipo, int NumColunas, double ComprimentoColuna, double ComprimentoViga, string Material)
{
    public double ComprimentoTotal => NumColunas * ComprimentoColuna + ComprimentoViga;
}

public static class Suportes
{
    public static string Nome(TipoSuporte t) => t switch
    {
        TipoSuporte.ColunaSimples => "Coluna simples",
        TipoSuporte.ColunaDupla => "Coluna dupla",
        TipoSuporte.ColunaTripla => "Coluna tripla",
        TipoSuporte.BracoProjetado => "Braço projetado",
        TipoSuporte.BracoProjetadoDuplo => "Braço projetado duplo",
        TipoSuporte.SemiPorticoSimples => "Semipórtico simples",
        TipoSuporte.SemiPorticoDuplo => "Semipórtico duplo",
        TipoSuporte.Portico => "Pórtico",
        TipoSuporte.Existente => "Fixação em poste/estrutura existente",
        _ => t.ToString(),
    };

    public static bool Suspenso(TipoSuporte t) => t is TipoSuporte.BracoProjetado or TipoSuporte.BracoProjetadoDuplo
        or TipoSuporte.SemiPorticoSimples or TipoSuporte.SemiPorticoDuplo or TipoSuporte.Portico;

    public static TipoSuporte? Ler(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        string k = Normalizar(s);
        foreach (TipoSuporte t in Enum.GetValues<TipoSuporte>())
            if (Normalizar(Nome(t)) == k || Normalizar(t.ToString()) == k) return t;
        if (k.Contains("PORTICO") && k.Contains("SEMI")) return k.Contains("DUPLO") ? TipoSuporte.SemiPorticoDuplo : TipoSuporte.SemiPorticoSimples;
        if (k.Contains("PORTICO")) return TipoSuporte.Portico;
        if (k.Contains("BRACO")) return k.Contains("DUPLO") ? TipoSuporte.BracoProjetadoDuplo : TipoSuporte.BracoProjetado;
        if (k.Contains("TRIPLA")) return TipoSuporte.ColunaTripla;
        if (k.Contains("DUPLA")) return TipoSuporte.ColunaDupla;
        if (k.Contains("COLUNA") || k.Contains("SIMPLES")) return TipoSuporte.ColunaSimples;
        if (k.Contains("OUTRO") || k.Contains("EXIST") || k.Contains("POSTE")) return TipoSuporte.Existente;
        return null;
    }

    private static string Normalizar(string s)
    {
        var n = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in n)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    /// <summary>
    /// Comprimento do suporte. Colunas: altura livre + altura das placas + folga de 0,10 m + engastamento.
    /// Suspensos: altura livre suspensa + placas + engastamento; viga = vão informado (ou braço padrão).
    /// </summary>
    public static QuantSuporte Calcular(TipoSuporte t, double alturaPlacas, N n, TipoVia via, double vao = 0, string? material = null)
    {
        string mat = material ?? n.MaterialSuportePadrao;
        bool urb = via == TipoVia.Urbana;
        double livre = Suspenso(t) ? (urb ? n.AlturaLivreSuspensaUrbana : n.AlturaLivreSuspensaRural) : n.AlturaLivre(via);
        double col = Num.Arred(livre + alturaPlacas + 0.10 + n.EngastamentoSuporte, 2);
        return t switch
        {
            TipoSuporte.ColunaSimples => new(t, 1, col, 0, mat),
            TipoSuporte.ColunaDupla => new(t, 2, col, 0, mat),
            TipoSuporte.ColunaTripla => new(t, 3, col, 0, mat),
            TipoSuporte.BracoProjetado => new(t, 1, col, vao > 0 ? vao : n.ComprimentoBracoProjetado, "Coluna e braço em aço galvanizado"),
            TipoSuporte.BracoProjetadoDuplo => new(t, 1, col, 2 * (vao > 0 ? vao : n.ComprimentoBracoProjetado), "Coluna e braços em aço galvanizado"),
            TipoSuporte.SemiPorticoSimples => new(t, 1, col, vao > 0 ? vao : 6.0, "Estrutura metálica treliçada/tubular"),
            TipoSuporte.SemiPorticoDuplo => new(t, 2, col, vao > 0 ? vao : 6.0, "Estrutura metálica treliçada/tubular"),
            TipoSuporte.Portico => new(t, 2, col, vao > 0 ? vao : 12.0, "Estrutura metálica treliçada/tubular"),
            _ => new(t, 0, 0, 0, "Existente"),
        };
    }
}
