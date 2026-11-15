using System.Globalization;
using System.Text;
using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Locação de campo (PRJLOCACAO): pontos numerados de meios-fios, ilhas, eixos e bordos — início e fim, vértices, PC, PT, PCC,
// PRC e centro das curvas, e pontos intermediários a cada X m nas tangentes e nas curvas — com a cota da superfície, para a
// estação total ou o GNSS (CSV PNEZD), o quadro de coordenadas da prancha e a nota de locação em Excel.
// ======================================================================================================================

public sealed class ParametrosLocacao
{
    /// <summary>Intervalo dos pontos intermediários nas tangentes e nas curvas (m).</summary>
    public double IntervaloTangente { get; set; } = 10;
    public double IntervaloCurva { get; set; } = 5;
    /// <summary>Inclui o centro de cada curva (raio).</summary>
    public bool Centros { get; set; } = true;
    /// <summary>Pontos mais próximos que isso viram um só (m).</summary>
    public double Tolerancia { get; set; } = 0.01;
    /// <summary>Ordem das colunas do CSV: PNEZD (ponto, norte, este, cota, descrição) ou PENZD.</summary>
    public string Formato { get; set; } = "PNEZD";
    public int NumeroInicial { get; set; } = 1;
    public int CasasCoordenadas { get; set; } = 3;
    public bool ParametrosDoUsuario { get; set; }
}

/// <summary>Elemento a locar: polilinha (vértices com bulge), círculo, ou pontos já prontos (alinhamento).</summary>
public sealed class ElementoLocacao
{
    public string Nome { get; set; } = "";
    /// <summary>Código que vai na descrição (MF, ILHA, EIXO...).</summary>
    public string Codigo { get; set; } = "";
    public List<VerticeVia> Vertices { get; set; } = new();
    public bool Fechado { get; set; }
    /// <summary>Círculo: centro e raio (os vértices ficam vazios).</summary>
    public (double X, double Y, double R)? Circulo { get; set; }
    /// <summary>Pontos prontos (ex.: estacas e pontos notáveis de um alinhamento): X, Y, tipo, observação.</summary>
    public List<(double X, double Y, string Tipo, string Obs)>? Prontos { get; set; }
}

public sealed record PontoLocacao(int Numero, double X, double Y, double? Z, string Codigo, string Tipo, string Elemento, string Obs)
{
    public string Descricao => string.IsNullOrEmpty(Tipo) ? Codigo : $"{Codigo} {Tipo}";
}

/// <summary>Curva de um elemento, para a tabela de elementos de curva.</summary>
public sealed record CurvaLocacao(string Elemento, string Codigo, int PontoPc, int PontoPt, int PontoCentro, double Raio, double Deflexao, double Desenvolvimento,
                                  double Tangente, double Corda, string Sentido);

public sealed class ResultadoLocacao
{
    public List<PontoLocacao> Pontos { get; } = new();
    public List<CurvaLocacao> Curvas { get; } = new();
    public List<string> Avisos { get; } = new();
}

public static class LocacaoCampo
{
    private sealed record Bruto(double X, double Y, string Tipo, string Obs, string Codigo, string Elemento, int Prioridade);

    /// <summary>Código pela camada: meio-fio/bordo = MF, ilha = ILHA, eixo = EIXO, retenção = LR; senão a própria camada (curta).</summary>
    public static string CodigoDaCamada(string camada)
    {
        string c = camada.ToUpperInvariant();
        if (c.Contains("ILHA")) return "ILHA";
        if (c.Contains("MEIO") || c.Contains("BORDO") || c.Contains("GUIA") || c.Contains("CURB")) return "MF";
        if (c.Contains("RETEN")) return "LR";
        if (c.Contains("EIXO") || c.Contains("ALIN")) return "EIXO";
        if (c.Contains("PASSEIO") || c.Contains("CALCADA")) return "PAS";
        if (c.Contains("ROTATORIA") || c.Contains("INTERSECAO")) return "MF";
        var sb = new StringBuilder();
        foreach (char ch in c) if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        return sb.Length == 0 ? "PT" : sb.ToString()[..Math.Min(6, sb.Length)];
    }

    /// <summary>Arco do segmento a→b com bulge: centro, raio, ângulos e varredura (+ anti-horário).</summary>
    public static (double Cx, double Cy, double R, double A0, double Sweep) Arco(VerticeVia a, VerticeVia b)
    {
        double sweep = 4 * Math.Atan(a.Bulge);
        double dx = b.X - a.X, dy = b.Y - a.Y, c = Math.Sqrt(dx * dx + dy * dy);
        double r = c / (2 * Math.Sin(Math.Abs(sweep) / 2));
        // centro: à esquerda da corda (bulge > 0) a h = r·cos(sweep/2)
        double h = r * Math.Cos(sweep / 2) * Math.Sign(sweep);
        double mx = (a.X + b.X) / 2, my = (a.Y + b.Y) / 2;
        double nx = -dy / c, ny = dx / c;
        double cx = mx + nx * h, cy = my + ny * h;
        return (cx, cy, Math.Abs(r), Math.Atan2(a.Y - cy, a.X - cx), sweep);
    }

    private static double Dir(VerticeVia a, VerticeVia b, bool noFim)
    {
        if (Math.Abs(a.Bulge) < 1e-9) return Math.Atan2(b.Y - a.Y, b.X - a.X);
        var (cx, cy, _, a0, sw) = Arco(a, b);
        double ang = noFim ? a0 + sw : a0;
        return ang + Math.Sign(sw) * Math.PI / 2;   // tangente ao arco no sentido do percurso
    }

    private static (double X, double Y, double R)? Circ3(VerticeVia a, VerticeVia b, VerticeVia c)
    {
        double d = 2 * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
        if (Math.Abs(d) < 1e-9) return null;
        double a2 = a.X * a.X + a.Y * a.Y, b2 = b.X * b.X + b.Y * b.Y, c2 = c.X * c.X + c.Y * c.Y;
        double x = (a2 * (b.Y - c.Y) + b2 * (c.Y - a.Y) + c2 * (a.Y - b.Y)) / d, y = (a2 * (c.X - b.X) + b2 * (a.X - c.X) + c2 * (b.X - a.X)) / d;
        return (x, y, Math.Sqrt((a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y)));
    }

    /// <summary>
    /// Limpa a polilinha discretizada (como a dos bordos desenhados a cada 1 m): junta segmentos retos alinhados e troca as sequências
    /// de segmentos curtos (até <paramref name="segMax"/> m, deflexão de até 10° por vértice, todos para o mesmo lado) que caem num
    /// círculo, dentro de <paramref name="tol"/> m, por um arco.
    /// </summary>
    public static List<VerticeVia> Simplificar(IReadOnlyList<VerticeVia> v, double tol = 0.02, double segMax = 5)
    {
        if (v.Count < 3) return v.ToList();
        static double Ang(VerticeVia a, VerticeVia b) => Math.Atan2(b.Y - a.Y, b.X - a.X);
        static double Defl(VerticeVia a, VerticeVia b, VerticeVia c) => Math.IEEERemainder(Ang(b, c) - Ang(a, b), 2 * Math.PI);
        static double Dist(VerticeVia a, VerticeVia b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        int n = v.Count;
        var r = new List<VerticeVia>();
        int i = 0;
        while (i < n)
        {
            var a = v[i];
            int fim = -1;
            if (Math.Abs(a.Bulge) < 1e-9)
            {
                int sinal = 0;
                double dMin = double.MaxValue, dMax = 0;
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(v[j - 1].Bulge) > 1e-9 || Dist(v[j - 1], v[j]) > segMax || Dist(v[j - 1], v[j]) < 1e-6) break;
                    if (j >= i + 2)
                    {
                        double dfl = Defl(v[j - 2], v[j - 1], v[j]);
                        if (Math.Abs(dfl) > 10 * Math.PI / 180 || Math.Abs(dfl) < 1e-4) break;
                        int sg = Math.Sign(dfl);
                        if (sinal != 0 && sg != sinal) break;
                        sinal = sg;
                        // curva discretizada tem deflexão uniforme; a passagem da tangente para a curva (meia deflexão) fica de fora
                        dMin = Math.Min(dMin, Math.Abs(dfl)); dMax = Math.Max(dMax, Math.Abs(dfl));
                        if (dMax > 1.5 * dMin + 1e-6) break;
                    }
                    if (j - i < 3) continue;
                    var c = Circ3(v[i], v[(i + j) / 2], v[j]);
                    if (c is not { } cc || cc.R > 5000) break;
                    bool ok = true;
                    for (int k = i; k <= j && ok; k++) ok = Math.Abs(Math.Sqrt((v[k].X - cc.X) * (v[k].X - cc.X) + (v[k].Y - cc.Y) * (v[k].Y - cc.Y)) - cc.R) <= tol;
                    if (!ok) break;
                    fim = j;
                }
            }
            if (fim > 0)
            {
                var c = Circ3(v[i], v[(i + fim) / 2], v[fim])!.Value;
                double sweep = 0;
                for (int k = i; k < fim; k++)
                    sweep += Math.IEEERemainder(Math.Atan2(v[k + 1].Y - c.Y, v[k + 1].X - c.X) - Math.Atan2(v[k].Y - c.Y, v[k].X - c.X), 2 * Math.PI);
                r.Add(new VerticeVia(a.X, a.Y, Math.Tan(sweep / 4)));
                i = fim;
                continue;
            }
            r.Add(a);
            i++;
        }
        // retos alinhados
        var m = new List<VerticeVia> { r[0] };
        for (int k = 1; k < r.Count - 1; k++)
        {
            var ant = m[^1]; var cur = r[k]; var prox = r[k + 1];
            if (Math.Abs(ant.Bulge) < 1e-9 && Math.Abs(cur.Bulge) < 1e-9 && Math.Abs(Defl(ant, cur, prox)) < 0.3 * Math.PI / 180) continue;
            m.Add(cur);
        }
        if (r.Count > 1) m.Add(r[^1]);
        return m;
    }

    /// <summary>Gera os pontos de locação dos elementos (numerados a partir de <see cref="ParametrosLocacao.NumeroInicial"/>).</summary>
    public static ResultadoLocacao Gerar(IEnumerable<ElementoLocacao> elementos, ParametrosLocacao p, Func<double, double, double?>? z = null)
    {
        var r = new ResultadoLocacao();
        var brutos = new List<Bruto>();
        var curvas = new List<(string Elem, string Cod, (double X, double Y) Pc, (double X, double Y) Pt, (double X, double Y) C, double R, double Delta, string Sentido)>();
        foreach (var e in elementos)
        {
            if (e.Prontos is { } pr)
            {
                foreach (var (x, y, t, o) in pr) brutos.Add(new Bruto(x, y, t, o, e.Codigo, e.Nome, t.Length > 0 && !t.StartsWith("EST", StringComparison.Ordinal) ? 2 : 1));
                continue;
            }
            if (e.Circulo is { } ci)
            {
                if (p.Centros) brutos.Add(new Bruto(ci.X, ci.Y, "CENTRO", $"R = {ci.R:0.000} m", e.Codigo, e.Nome, 3));
                double comp = 2 * Math.PI * ci.R;
                int n = Math.Max(8, (int)Math.Ceiling(comp / p.IntervaloCurva));
                for (int k = 0; k < n; k++)
                {
                    double a = 2 * Math.PI * k / n;
                    brutos.Add(new Bruto(ci.X + ci.R * Math.Cos(a), ci.Y + ci.R * Math.Sin(a), k == 0 ? "CIRC" : "", k == 0 ? $"círculo R = {ci.R:0.000} m" : "", e.Codigo, e.Nome, k == 0 ? 2 : 1));
                }
                continue;
            }
            var v = Simplificar(e.Vertices);
            if (v.Count < 2) continue;
            int nseg = e.Fechado ? v.Count : v.Count - 1;
            VerticeVia V(int i) => v[(i % v.Count + v.Count) % v.Count];
            for (int i = 0; i < nseg; i++)
            {
                var a = V(i);
                var b = V(i + 1);
                bool arco = Math.Abs(a.Bulge) > 1e-9;
                // tipo do ponto inicial do segmento
                string tipo;
                if (!e.Fechado && i == 0) tipo = arco ? "PC" : "INÍCIO";
                else
                {
                    var ap = V(i - 1);
                    bool arcoAnt = Math.Abs(ap.Bulge) > 1e-9;
                    double dAnt = Dir(ap, a, true), dEste = Dir(a, b, false);
                    double giro = Math.Abs(Math.IEEERemainder(dEste - dAnt, 2 * Math.PI));
                    if (!arcoAnt && !arco) tipo = giro > 0.5 * Math.PI / 180 ? "V" : "";
                    else if (!arcoAnt && arco) tipo = giro > 1 * Math.PI / 180 ? "V/PC" : "PC";
                    else if (arcoAnt && !arco) tipo = giro > 1 * Math.PI / 180 ? "PT/V" : "PT";
                    else tipo = Math.Sign(ap.Bulge) == Math.Sign(a.Bulge) ? "PCC" : "PRC";
                }
                if (tipo.Length > 0 || i == 0) brutos.Add(new Bruto(a.X, a.Y, tipo, "", e.Codigo, e.Nome, tipo.Length > 0 ? 3 : 1));
                else brutos.Add(new Bruto(a.X, a.Y, "", "", e.Codigo, e.Nome, 1));
                // intermediários e o centro
                if (!arco)
                {
                    double dx = b.X - a.X, dy = b.Y - a.Y, l = Math.Sqrt(dx * dx + dy * dy);
                    for (double s = p.IntervaloTangente; s < l - 0.25 * p.IntervaloTangente; s += p.IntervaloTangente)
                        brutos.Add(new Bruto(a.X + dx * s / l, a.Y + dy * s / l, "", "", e.Codigo, e.Nome, 1));
                }
                else
                {
                    var (cx, cy, rr, a0, sw) = Arco(a, b);
                    double comp = rr * Math.Abs(sw);
                    for (double s = p.IntervaloCurva; s < comp - 0.25 * p.IntervaloCurva; s += p.IntervaloCurva)
                    {
                        double ang = a0 + Math.Sign(sw) * s / rr;
                        brutos.Add(new Bruto(cx + rr * Math.Cos(ang), cy + rr * Math.Sin(ang), "", "", e.Codigo, e.Nome, 1));
                    }
                    if (p.Centros) brutos.Add(new Bruto(cx, cy, "CENTRO", $"R = {rr:0.000} m", e.Codigo, e.Nome, 3));
                    curvas.Add((e.Nome, e.Codigo, (a.X, a.Y), (b.X, b.Y), (cx, cy), rr, Math.Abs(sw), sw > 0 ? "à esquerda" : "à direita"));
                }
            }
            if (!e.Fechado)
            {
                var ul = V(v.Count - 2);
                brutos.Add(new Bruto(v[^1].X, v[^1].Y, Math.Abs(ul.Bulge) > 1e-9 ? "PT" : "FIM", "", e.Codigo, e.Nome, 3));
            }
        }

        // pontos repetidos (o fim de um meio-fio é o início do outro): fica um só, com os tipos juntos
        var unicos = new List<Bruto>();
        double tol2 = p.Tolerancia * p.Tolerancia;
        var grade = new Dictionary<(long, long), List<int>>();
        double cel = Math.Max(p.Tolerancia * 4, 0.05);
        (long, long) Cel(double x, double y) => ((long)Math.Floor(x / cel), (long)Math.Floor(y / cel));
        foreach (var b in brutos)
        {
            var (gx, gy) = Cel(b.X, b.Y);
            int achou = -1;
            for (long ix = gx - 1; ix <= gx + 1 && achou < 0; ix++)
                for (long iy = gy - 1; iy <= gy + 1 && achou < 0; iy++)
                    if (grade.TryGetValue((ix, iy), out var lst))
                        foreach (int k in lst)
                            if ((unicos[k].X - b.X) * (unicos[k].X - b.X) + (unicos[k].Y - b.Y) * (unicos[k].Y - b.Y) <= tol2) { achou = k; break; }
            if (achou < 0)
            {
                unicos.Add(b);
                if (!grade.TryGetValue((gx, gy), out var l2)) grade[(gx, gy)] = l2 = new List<int>();
                l2.Add(unicos.Count - 1);
            }
            else
            {
                var u = unicos[achou];
                string tipo = u.Tipo;
                if (b.Tipo.Length > 0 && !tipo.Split('/').Contains(b.Tipo)) tipo = tipo.Length == 0 ? b.Tipo : $"{tipo}/{b.Tipo}";
                string obs = u.Obs.Length > 0 ? u.Obs : b.Obs;
                if (b.Prioridade > u.Prioridade) unicos[achou] = b with { Tipo = tipo, Obs = obs };
                else unicos[achou] = u with { Tipo = tipo, Obs = obs };
            }
        }
        int num = p.NumeroInicial;
        var numeroDe = new Dictionary<(long, long), int>();
        foreach (var u in unicos)
        {
            double? cota = z?.Invoke(u.X, u.Y);
            r.Pontos.Add(new PontoLocacao(num, u.X, u.Y, cota, u.Codigo, u.Tipo.Replace("INÍCIO/FIM", "INÍCIO").Trim('/'), u.Elemento, u.Obs));
            num++;
        }
        int Num((double X, double Y) q)
        {
            var m = r.Pontos.Where(x => Math.Abs(x.X - q.X) <= p.Tolerancia * 2 && Math.Abs(x.Y - q.Y) <= p.Tolerancia * 2).Select(x => x.Numero).DefaultIfEmpty(0).First();
            return m;
        }
        foreach (var c in curvas)
        {
            double t = c.R * Math.Tan(c.Delta / 2), corda = 2 * c.R * Math.Sin(c.Delta / 2);
            r.Curvas.Add(new CurvaLocacao(c.Elem, c.Cod, Num(c.Pc), Num(c.Pt), p.Centros ? Num(c.C) : 0, c.R, c.Delta * 180 / Math.PI, c.R * c.Delta, c.Delta < Math.PI ? t : double.NaN, corda, c.Sentido));
        }
        if (z is not null)
        {
            int sem = r.Pontos.Count(x => x.Z is null);
            if (sem > 0) r.Avisos.Add($"{sem} ponto(s) fora da superfície ficaram sem cota.");
        }
        return r;
    }

    private static string N(double v, int casas) => v.ToString("F" + casas, CultureInfo.InvariantCulture);

    /// <summary>CSV para a estação total / GNSS: ponto, norte, este, cota, descrição (ou PENZD), separado por vírgula e com ponto decimal.</summary>
    public static string Csv(IEnumerable<PontoLocacao> pontos, ParametrosLocacao p)
    {
        var sb = new StringBuilder();
        bool penzd = string.Equals(p.Formato, "PENZD", StringComparison.OrdinalIgnoreCase);
        foreach (var x in pontos)
        {
            string d = x.Descricao.Replace(',', ' ').Replace(';', ' ').Replace('"', ' ').Trim();
            string zz = x.Z is { } zv ? N(zv, p.CasasCoordenadas) : "";
            string a = N(x.Y, p.CasasCoordenadas), b = N(x.X, p.CasasCoordenadas);
            sb.Append(x.Numero).Append(',').Append(penzd ? b : a).Append(',').Append(penzd ? a : b).Append(',').Append(zz).Append(',').Append(d).Append("\r\n");
        }
        return sb.ToString();
    }

    /// <summary>Quadro de coordenadas da prancha, em blocos de <paramref name="linhasPorQuadro"/> pontos.</summary>
    public static List<TabelaDados> Quadros(IReadOnlyList<PontoLocacao> pontos, int linhasPorQuadro, int casas)
    {
        var r = new List<TabelaDados>();
        for (int i = 0; i < pontos.Count; i += linhasPorQuadro)
        {
            var t = new TabelaDados
            {
                Titulo = pontos.Count > linhasPorQuadro ? $"QUADRO DE LOCAÇÃO ({i / linhasPorQuadro + 1}/{(pontos.Count + linhasPorQuadro - 1) / linhasPorQuadro})" : "QUADRO DE LOCAÇÃO",
                Colunas = new List<string> { "Ponto", "Norte (Y)", "Este (X)", "Cota", "Descrição" },
            };
            foreach (var x in pontos.Skip(i).Take(linhasPorQuadro))
                t.Linhas.Add(new[] { x.Numero.ToString(), N(x.Y, casas), N(x.X, casas), x.Z is { } zz ? N(zz, casas) : "-", x.Descricao });
            r.Add(t);
        }
        return r;
    }
}

/// <summary>Nota de locação em Excel.</summary>
public sealed class RelatorioLocacao
{
    public string Titulo { get; set; } = "Nota de locação";
    public string Desenho { get; set; } = "";
    public string Superficie { get; set; } = "";
    public string Sistema { get; set; } = "";
    public ResultadoLocacao Resultado { get; set; } = new();
    public ParametrosLocacao Parametros { get; set; } = new();

    public PastaXlsx Planilha()
    {
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = "D9E1F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n3 = new EstiloCelula { Formato = "0.000" };
        var n2 = new EstiloCelula { Formato = "0.00" };
        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        var res = Resultado;
        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string a, string b) { fr.Texto(lr, 1, a, txt with { Negrito = true }); fr.Texto(lr, 2, b, q); lr++; }
        Par("Pontos", $"{res.Pontos.Count} (de {res.Pontos.FirstOrDefault()?.Numero} a {res.Pontos.LastOrDefault()?.Numero})");
        Par("Curvas", res.Curvas.Count.ToString());
        Par("Intervalos", $"{Parametros.IntervaloTangente:0.##} m nas tangentes e {Parametros.IntervaloCurva:0.##} m nas curvas");
        Par("Cotas", Superficie.Length > 0 ? $"da superfície '{Superficie}'" : "sem cota");
        Par("Sistema de coordenadas", Sistema.Length > 0 ? Sistema : "o do desenho");
        Par("Formato do CSV", $"{Parametros.Formato}, separado por vírgula, ponto decimal");
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        foreach (var a in res.Avisos) Par("Aviso", a);
        lr++;
        fr.Texto(lr++, 1, "Tipos: INÍCIO/FIM, V (vértice), PC (início de curva), PT (fim de curva), PCC (curva composta), PRC (curva reversa), CENTRO (centro da curva), CIRC (círculo), EST (estaca); sem tipo = ponto intermediário.", txt with { Italico = true, Borda = false });
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: confira os pontos de amarração e a cota de projeto do meio-fio (topo ou sarjeta) antes de implantar; a validação e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 26); fr.Largura(2, 90);

        var fp = pasta.NovaFolha("Pontos");
        Cab(fp, 1, ("Ponto", 8), ("Norte (Y)", 15), ("Este (X)", 15), ("Cota", 11), ("Código", 9), ("Tipo", 10), ("Descrição", 18), ("Elemento", 30), ("Observação", 30));
        int l = 2;
        foreach (var x in res.Pontos)
        {
            fp.Numero(l, 1, x.Numero, txt); fp.Numero(l, 2, x.Y, n3); fp.Numero(l, 3, x.X, n3);
            if (x.Z is { } zz) fp.Numero(l, 4, zz, n3); else fp.Texto(l, 4, "-", txt);
            fp.Texto(l, 5, x.Codigo, txt); fp.Texto(l, 6, x.Tipo, txt); fp.Texto(l, 7, x.Descricao, txt); fp.Texto(l, 8, x.Elemento, q); fp.Texto(l, 9, x.Obs, q);
            l++;
        }

        if (res.Curvas.Count > 0)
        {
            var fc = pasta.NovaFolha("Curvas");
            Cab(fc, 1, ("Elemento", 30), ("Código", 8), ("PC", 7), ("PT", 7), ("Centro", 8), ("Raio (m)", 10), ("Deflexão (°)", 10), ("Desenvolvimento (m)", 12),
                ("Tangente (m)", 11), ("Corda (m)", 10), ("Sentido", 11));
            int lc = 2;
            foreach (var c in res.Curvas)
            {
                fc.Texto(lc, 1, c.Elemento, q); fc.Texto(lc, 2, c.Codigo, txt); fc.Numero(lc, 3, c.PontoPc, txt); fc.Numero(lc, 4, c.PontoPt, txt);
                if (c.PontoCentro > 0) fc.Numero(lc, 5, c.PontoCentro, txt); else fc.Texto(lc, 5, "-", txt);
                fc.Numero(lc, 6, c.Raio, n3); fc.Numero(lc, 7, c.Deflexao, n2); fc.Numero(lc, 8, c.Desenvolvimento, n3);
                if (double.IsNaN(c.Tangente)) fc.Texto(lc, 9, "-", txt); else fc.Numero(lc, 9, c.Tangente, n3);
                fc.Numero(lc, 10, c.Corda, n3); fc.Texto(lc, 11, c.Sentido, txt);
                lc++;
            }
        }
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
