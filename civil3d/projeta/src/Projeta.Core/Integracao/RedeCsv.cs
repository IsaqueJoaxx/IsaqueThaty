using System.Text;
using Projeta.Core.Comum;
using Projeta.Core.Redes;

namespace Projeta.Core.Integracao;

/// <summary>
/// Importação/exportação de redes em planilha (CSV com ';', aberto direto no Excel).
/// Cada linha começa com o tipo do registro:
/// <code>
/// PV;nome;X;Y;[cota tampa];[dispositivo]
/// BL;nome;X;Y;[cota tampa];[dispositivo]
/// CX;nome;X;Y;[cota tampa];[dispositivo]
/// EXUTORIO;nome;X;Y;[cota tampa];[cota de lançamento]
/// TRECHO;nome;montante;jusante;[diâmetro m];[declividade m/m];[cota fundo mont.];[cota fundo jus.]
/// BACIA;nome;nó;área (ha);C;[tc min]
/// </code>
/// Linhas iniciadas por # ou com tipo desconhecido (ex.: cabeçalho) são ignoradas.
/// </summary>
public static class RedeCsv
{
    public static List<Mensagem> Importar(string texto, Rede rede)
    {
        var msgs = new List<Mensagem>();
        char sep = Csv.DetectarSeparador(texto);
        var linhas = Csv.Ler(texto, sep);
        int ln = 0;
        var trechosPendentes = new List<(int linha, string[] c)>();
        var baciasPendentes = new List<(int linha, string[] c)>();
        foreach (var c in linhas)
        {
            ln++;
            if (c.Length == 0 || c[0].TrimStart().StartsWith('#')) continue;
            string tipo = c[0].Trim().ToUpperInvariant();
            string Campo(int i) => i < c.Length ? c[i].Trim() : "";
            double Numero(int i) => Num.Ler(Campo(i));
            switch (tipo)
            {
                case "PV": case "NO": case "BL": case "CX": case "EXUTORIO": case "EX":
                    {
                        var tn = tipo switch { "BL" => TipoNo.BocaDeLobo, "CX" => TipoNo.Caixa, "EXUTORIO" or "EX" => TipoNo.Exutorio, _ => TipoNo.PV };
                        double x = Numero(2), y = Numero(3);
                        if (double.IsNaN(x) || double.IsNaN(y)) { msgs.Add(new(Gravidade.Erro, $"Linha {ln}: coordenadas inválidas")); continue; }
                        var no = rede.NoPorNome(Campo(1));
                        if (no is null) { no = rede.NovoNo(tn, x, y); if (Campo(1).Length > 0) no.Nome = Campo(1); }
                        else { no.X = x; no.Y = y; no.Tipo = tn; }
                        double ct = Numero(4);
                        if (!double.IsNaN(ct)) no.CotaTampaForcada = ct;
                        if (tn == TipoNo.Exutorio) { double zl = Numero(5); if (!double.IsNaN(zl)) no.CotaFundoForcada = zl; }
                        else if (Campo(5).Length > 0) no.DispositivoId = Campo(5);
                        break;
                    }
                case "TRECHO": case "TUBO": trechosPendentes.Add((ln, c)); break;
                case "BACIA": baciasPendentes.Add((ln, c)); break;
            }
        }
        foreach (var (linha, c) in trechosPendentes)
        {
            string Campo(int i) => i < c.Length ? c[i].Trim() : "";
            double? Opc(int i) => Num.TentarLer(Campo(i), out var v) ? v : null;
            var a = rede.NoPorNome(Campo(2)); var b = rede.NoPorNome(Campo(3));
            if (a is null || b is null) { msgs.Add(new(Gravidade.Erro, $"Linha {linha}: nó '{(a is null ? Campo(2) : Campo(3))}' não encontrado")); continue; }
            var t = rede.Trechos.FirstOrDefault(k => k.NoMontanteId == a.Id && k.NoJusanteId == b.Id) ?? rede.NovoTrecho(a, b);
            if (Campo(1).Length > 0) t.Nome = Campo(1);
            t.DiametroForcado = Opc(4);
            t.DeclividadeForcada = Opc(5);
            t.CotaMontanteForcada = Opc(6);
            t.CotaJusanteForcada = Opc(7);
        }
        foreach (var (linha, c) in baciasPendentes)
        {
            string Campo(int i) => i < c.Length ? c[i].Trim() : "";
            var no = rede.NoPorNome(Campo(2));
            if (no is null) { msgs.Add(new(Gravidade.Erro, $"Linha {linha}: nó '{Campo(2)}' da bacia não encontrado")); continue; }
            var bac = rede.Bacias.FirstOrDefault(k => string.Equals(k.Nome, Campo(1), StringComparison.OrdinalIgnoreCase));
            if (bac is null) { bac = new Bacia { Nome = Campo(1).Length > 0 ? Campo(1) : $"B-{rede.Bacias.Count + 1:00}" }; rede.Bacias.Add(bac); }
            bac.NoId = no.Id;
            bac.AreaHa = Num.Ler(Campo(3), 0);
            bac.C = Num.Ler(Campo(4), bac.C);
            if (Num.TentarLer(Campo(5), out var tc)) { bac.TcInformado = tc; bac.MetodoTc = Hidrologia.MetodoTc.Informado; }
        }
        msgs.Add(new(Gravidade.Info, $"Importados: {rede.Nos.Count} nós, {rede.Trechos.Count} trechos, {rede.Bacias.Count} bacias."));
        return msgs;
    }

    public static string Exportar(Rede rede)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PROJETA — rede " + rede.Nome + " (" + rede.Tipo + ")");
        sb.AppendLine("# TIPO;NOME;X;Y;COTA_TAMPA;DISPOSITIVO / COTA_LANCAMENTO");
        foreach (var n in rede.Nos)
        {
            string tipo = n.Tipo switch { TipoNo.BocaDeLobo => "BL", TipoNo.Caixa => "CX", TipoNo.Exutorio => "EXUTORIO", _ => "PV" };
            string ultimo = n.Tipo == TipoNo.Exutorio ? (n.CotaFundoForcada is double z ? Num.F(z, 3) : "") : n.DispositivoId;
            sb.AppendLine(Csv.Linha(new[] { tipo, n.Nome, Num.F(n.X, 3), Num.F(n.Y, 3), n.CotaTampaForcada is double ct ? Num.F(ct, 3) : (double.IsNaN(n.CotaTerreno) ? "" : Num.F(n.CotaTerreno, 3)), ultimo }));
        }
        sb.AppendLine("# TRECHO;NOME;MONTANTE;JUSANTE;DIAMETRO_M;DECLIVIDADE_M/M;COTA_FUNDO_MONT;COTA_FUNDO_JUS");
        foreach (var t in rede.Trechos)
        {
            string F(double? v, int c) => v.HasValue ? Num.F(v.Value, c) : "";
            sb.AppendLine(Csv.Linha(new[] { "TRECHO", t.Nome, rede.NoPorId(t.NoMontanteId)?.Nome, rede.NoPorId(t.NoJusanteId)?.Nome,
                F(t.DiametroForcado, 3), F(t.DeclividadeForcada, 5), F(t.CotaMontanteForcada, 3), F(t.CotaJusanteForcada, 3) }));
        }
        sb.AppendLine("# BACIA;NOME;NO;AREA_HA;C;TC_MIN");
        foreach (var b in rede.Bacias)
            sb.AppendLine(Csv.Linha(new[] { "BACIA", b.Nome, rede.NoPorId(b.NoId)?.Nome, Num.F(b.Area, 4), Num.F(b.C, 2), Num.F(b.Tc(), 2) }));
        return sb.ToString();
    }

    public static string Modelo() => """
# MODELO DE IMPORTAÇÃO DE REDE — PROJETA (separador ';', vírgula decimal aceita)
# TIPO;NOME;X;Y;COTA_TAMPA(opcional);DISPOSITIVO(opcional)
PV;PV-01;612000,00;7801000,00;850,00;PV_CIRCULAR
BL;BL-01;612005,00;7800995,00;849,90;BOCA_LOBO_SIMPLES
PV;PV-02;612050,00;7801000,00;849,10
PV;PV-03;612100,00;7801010,00;848,30
EXUTORIO;EX-01;612150,00;7801020,00;847,20;845,00
# TRECHO;NOME;MONTANTE;JUSANTE;DIAMETRO_M(opc);DECLIV_M/M(opc);CF_MONT(opc);CF_JUS(opc)
TRECHO;T-00;BL-01;PV-01
TRECHO;T-01;PV-01;PV-02
TRECHO;T-02;PV-02;PV-03
TRECHO;T-03;PV-03;EX-01
# BACIA;NOME;NO;AREA_HA;C;TC_MIN(opc)
BACIA;B-01;BL-01;0,80;0,70;10
BACIA;B-02;PV-02;1,20;0,65
BACIA;B-03;PV-03;0,95;0,75
""";
}
