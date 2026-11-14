using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Vertical;

namespace Projeta.Sinalizacao.Core.Quantitativos;

/// <summary>Linha do cadastro de campo (placa existente ou a implantar).</summary>
public sealed record LinhaCadastro(int Linha, string Codigo, TipoSuporte Suporte, double X, double Y, double? Rotacao, Situacao Situacao, double? Dimensao, string? Valor, string? Id);

/// <summary>Leitura de cadastro em CSV (separador ; , ou tab) ou XLSX (primeira aba). Colunas: COD, SUP, X, Y [ROT, SIT, DIM, VALOR, ID].</summary>
public static class Cadastro
{
    public static List<LinhaCadastro> Ler(string caminho, out List<string> avisos)
    {
        var tabela = Path.GetExtension(caminho).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ? LerXlsx(caminho) : LerCsv(caminho);
        return Interpretar(tabela, out avisos);
    }

    public static List<List<string>> LerCsv(string caminho)
    {
        var bytes = File.ReadAllBytes(caminho);
        string txt;
        try { txt = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { txt = Encoding.Latin1.GetString(bytes); }
        txt = txt.TrimStart('﻿');
        var linhas = txt.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
        if (linhas.Count == 0) return new();
        char sep = linhas[0].Count(c => c == ';') >= 1 ? ';' : linhas[0].Contains('\t') ? '\t' : ',';
        return linhas.Select(l => Dividir(l, sep)).ToList();
    }

    private static List<string> Dividir(string l, char sep)
    {
        var r = new List<string>();
        var sb = new StringBuilder();
        bool aspas = false;
        for (int i = 0; i < l.Length; i++)
        {
            char c = l[i];
            if (c == '"') { if (aspas && i + 1 < l.Length && l[i + 1] == '"') { sb.Append('"'); i++; } else aspas = !aspas; }
            else if (c == sep && !aspas) { r.Add(sb.ToString().Trim()); sb.Clear(); }
            else sb.Append(c);
        }
        r.Add(sb.ToString().Trim());
        return r;
    }

    /// <summary>Leitor mínimo de XLSX (primeira aba; textos compartilhados e inline; números).</summary>
    public static List<List<string>> LerXlsx(string caminho)
    {
        using var z = ZipFile.OpenRead(caminho);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var compart = new List<string>();
        var ss = z.GetEntry("xl/sharedStrings.xml");
        if (ss is not null)
        {
            using var s = ss.Open();
            var x = XDocument.Load(s);
            foreach (var si in x.Root!.Elements(ns + "si"))
                compart.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));
        }
        var planilha = z.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".xml"))
            .OrderBy(e => e.FullName.Length).ThenBy(e => e.FullName).FirstOrDefault();
        var r = new List<List<string>>();
        if (planilha is null) return r;
        using var st = planilha.Open();
        var doc = XDocument.Load(st);
        foreach (var row in doc.Descendants(ns + "row"))
        {
            var linha = new List<string>();
            foreach (var c in row.Elements(ns + "c"))
            {
                string refc = (string?)c.Attribute("r") ?? "";
                int col = ColunaIndice(refc);
                while (linha.Count < col) linha.Add("");
                string t = (string?)c.Attribute("t") ?? "";
                string v = c.Element(ns + "v")?.Value ?? "";
                string val = t switch
                {
                    "s" => int.TryParse(v, out int k) && k < compart.Count ? compart[k] : "",
                    "inlineStr" => string.Concat(c.Descendants(ns + "t").Select(x => x.Value)),
                    _ => v,
                };
                if (col < linha.Count) linha[col] = val; else linha.Add(val);
            }
            if (linha.Any(x => x.Length > 0)) r.Add(linha);
        }
        return r;
    }

    private static int ColunaIndice(string refc)
    {
        int n = 0;
        foreach (char ch in refc) { if (!char.IsLetter(ch)) break; n = n * 26 + (char.ToUpperInvariant(ch) - 'A' + 1); }
        return Math.Max(0, n - 1);
    }

    private static string Norm(string s)
    {
        var n = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in n) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    public static List<LinhaCadastro> Interpretar(List<List<string>> t, out List<string> avisos)
    {
        avisos = new List<string>();
        var r = new List<LinhaCadastro>();
        if (t.Count < 2) { avisos.Add("Planilha vazia ou sem dados abaixo do cabeçalho."); return r; }
        var cab = t[0].Select(Norm).ToList();
        int Col(params string[] nomes) { foreach (var n in nomes) { int i = cab.IndexOf(n); if (i >= 0) return i; } foreach (var n in nomes) { int i = cab.FindIndex(c => c.StartsWith(n)); if (i >= 0) return i; } return -1; }
        int cCod = Col("COD", "CODIGO", "PLACA"), cSup = Col("SUP", "SUPORTE"), cX = Col("X", "E", "EST", "ESTE", "XEST"), cY = Col("Y", "N", "NORT", "NORTE", "YNORT");
        int cRot = Col("ROT", "ROTACAO", "ANG", "ANGULO", "AZIMUTE"), cSit = Col("SIT", "SITUACAO"), cDim = Col("DIM", "DIMENSAO"), cVal = Col("VALOR", "VAL"), cId = Col("ID");
        if (cCod < 0 || cX < 0 || cY < 0) { avisos.Add("Cabeçalho precisa das colunas COD, X e Y (e opcionalmente SUP, ROT, SIT, DIM, VALOR, ID)."); return r; }
        string Get(List<string> l, int i) => i >= 0 && i < l.Count ? l[i].Trim() : "";
        for (int k = 1; k < t.Count; k++)
        {
            var l = t[k];
            string cod = Get(l, cCod);
            if (cod.Length == 0) continue;
            if (!Num.TentarLer(Get(l, cX), out double x) || !Num.TentarLer(Get(l, cY), out double y)) { avisos.Add($"Linha {k + 1}: coordenadas inválidas — ignorada."); continue; }
            var sup = Suportes.Ler(Get(l, cSup)) ?? TipoSuporte.ColunaSimples;
            double? rot = Num.TentarLer(Get(l, cRot), out double rr) ? rr : null;
            var sit = Get(l, cSit).ToUpperInvariant() switch
            {
                var s when s.StartsWith("REM") => Situacao.Remover,
                var s when s.StartsWith("MAN") || s.StartsWith("EXI") => Situacao.Manter,
                var s when s.StartsWith("REC") => Situacao.Recuperar,
                _ => Situacao.Implantar,
            };
            double? dim = Num.TentarLer(Get(l, cDim), out double dd) && dd > 0 ? dd : null;
            string? val = Get(l, cVal) is { Length: > 0 } v ? v : null;
            if (Catalogo.Obter(cod) is null) avisos.Add($"Linha {k + 1}: código {cod} não está no catálogo — será inserido como placa genérica.");
            r.Add(new LinhaCadastro(k + 1, cod, sup, x, y, rot, sit, dim, val, Get(l, cId) is { Length: > 0 } id ? id : null));
        }
        return r;
    }

    /// <summary>Modelo de cadastro para o usuário preencher.</summary>
    public static string ModeloCsv() =>
        "COD;SUP;X;Y;ROT;SIT;DIM;VALOR\n" +
        "R-1;Coluna Simples;2805,720;2917,765;90;Implantar;;\n" +
        "R-19;Coluna Simples;2173,343;2875,189;0;Implantar;0,50;40\n" +
        "A-14;Coluna Simples;2108,607;2850,582;180;Manter;;\n" +
        "R-6a;Coluna Simples;2099,336;2846,703;270;Remover;;\n";
}
