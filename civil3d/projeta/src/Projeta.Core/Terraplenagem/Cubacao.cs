using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Projeta.Core.Relatorios;
using Projeta.Core.Superficies;

namespace Projeta.Core.Terraplenagem;

/// <summary>
/// Linha de uma seção transversal: trechos retos (afastamento, cota) em ordem de afastamento, recortados na faixa válida.
/// Pode ter buracos (onde a superfície não existe: fora do contorno, regiões escondidas, vão de ponte).
/// </summary>
public sealed class LinhaSecao
{
    /// <summary>Trechos com X0 &lt; X1, em ordem e sem sobreposição.</summary>
    public IReadOnlyList<(double X0, double Z0, double X1, double Z1)> Trechos { get; }
    /// <summary>Pontas dos trechos, em ordem de afastamento.</summary>
    public IReadOnlyList<(double X, double Z)> P { get; }
    public double Inicio => Trechos[0].X0;
    public double Fim => Trechos[^1].X1;

    /// <summary>Linha contínua pelos pontos (em qualquer ordem; NaN são ignorados), recortada em [inicio, fim].</summary>
    public LinhaSecao(IEnumerable<(double X, double Z)> pontos, double inicio = double.NegativeInfinity, double fim = double.PositiveInfinity)
        : this(Continua(pontos), inicio, fim) { }

    private LinhaSecao(List<(double X0, double Z0, double X1, double Z1)> trechos, double inicio, double fim)
    {
        if (double.IsNaN(inicio)) inicio = double.NegativeInfinity;
        if (double.IsNaN(fim)) fim = double.PositiveInfinity;
        var r = new List<(double X0, double Z0, double X1, double Z1)>();
        foreach (var t in trechos.Where(t => Finito(t.X0) && Finito(t.Z0) && Finito(t.X1) && Finito(t.Z1))
                                 .Select(t => t.X0 <= t.X1 ? t : (X0: t.X1, Z0: t.Z1, X1: t.X0, Z1: t.Z0)).OrderBy(t => t.X0))
        {
            double x0 = Math.Max(t.X0, inicio), x1 = Math.Min(t.X1, fim);
            if (r.Count > 0) x0 = Math.Max(x0, r[^1].X1); // sobreposição: vale o trecho que veio antes
            if (!(x1 - x0 > 1e-9)) continue;
            double Z(double x) => t.Z0 + (t.Z1 - t.Z0) * (x - t.X0) / (t.X1 - t.X0);
            r.Add((x0, Z(x0), x1, Z(x1)));
        }
        if (r.Count == 0) throw new ArgumentException("A seção não tem trecho na faixa válida.");
        Trechos = r;
        var p = new List<(double X, double Z)>();
        foreach (var t in r)
        {
            if (p.Count == 0 || t.X0 - p[^1].X > 1e-9) p.Add((t.X0, t.Z0));
            p.Add((t.X1, t.Z1));
        }
        P = p;
    }

    /// <summary>Linha a partir de trechos soltos (por exemplo, os segmentos que o Civil 3D liga entre os pontos da seção).</summary>
    public static LinhaSecao DeTrechos(IEnumerable<(double X0, double Z0, double X1, double Z1)> trechos, double inicio = double.NegativeInfinity, double fim = double.PositiveInfinity)
        => new(trechos.ToList(), inicio, fim);

    private static bool Finito(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    private static List<(double, double, double, double)> Continua(IEnumerable<(double X, double Z)> pontos)
    {
        var l = pontos.Where(p => Finito(p.X) && Finito(p.Z)).OrderBy(p => p.X).ToList();
        var u = new List<(double X, double Z)>();
        foreach (var p in l)
            if (u.Count == 0 || p.X - u[^1].X > 1e-9) u.Add(p);
        if (u.Count < 2) throw new ArgumentException("A seção precisa de pelo menos dois pontos.");
        var r = new List<(double, double, double, double)>();
        for (int i = 1; i < u.Count; i++) r.Add((u[i - 1].X, u[i - 1].Z, u[i].X, u[i].Z));
        return r;
    }

    /// <summary>Índice do trecho que contém <paramref name="x"/>; -1 em buraco ou fora.</summary>
    public int TrechoEm(double x)
    {
        int lo = 0, hi = Trechos.Count - 1;
        while (lo <= hi)
        {
            int m = (lo + hi) / 2;
            if (x < Trechos[m].X0 - 1e-9) hi = m - 1;
            else if (x > Trechos[m].X1 + 1e-9) lo = m + 1;
            else return m;
        }
        return -1;
    }

    /// <summary>Cota no afastamento <paramref name="x"/> (interpolação linear); NaN fora da faixa e nos buracos.</summary>
    public double Cota(double x)
    {
        int i = TrechoEm(x);
        return i < 0 ? double.NaN : NoTrecho(i, x);
    }

    /// <summary>Cota pela reta do trecho <paramref name="i"/> (também um pouco além das pontas).</summary>
    public double NoTrecho(int i, double x)
    {
        var t = Trechos[i];
        return t.Z0 + (t.Z1 - t.Z0) * (x - t.X0) / (t.X1 - t.X0);
    }
}

/// <summary>Áreas de corte e aterro de uma seção, na faixa onde terreno e projeto existem juntos.</summary>
public readonly record struct AreasSecao(double Corte, double Aterro, double Inicio, double Fim)
{
    public double Largura => double.IsNaN(Inicio) ? 0 : Fim - Inicio;
}

public static class Secoes
{
    /// <summary>
    /// Corte = área onde o terreno está acima do projeto; aterro = onde o projeto está acima do terreno, só onde as duas
    /// linhas existem (os buracos de uma ou de outra ficam de fora). Exato para linhas poligonais: integra a diferença entre
    /// as pontas dos trechos das duas linhas e os cruzamentos.
    /// </summary>
    public static AreasSecao Areas(LinhaSecao terreno, LinhaSecao projeto)
    {
        double a = Math.Max(terreno.Inicio, projeto.Inicio), b = Math.Min(terreno.Fim, projeto.Fim);
        if (!(b - a > 1e-9)) return new AreasSecao(0, 0, double.NaN, double.NaN);
        var xs = new List<double> { a, b };
        foreach (var p in terreno.P) if (p.X > a && p.X < b) xs.Add(p.X);
        foreach (var p in projeto.P) if (p.X > a && p.X < b) xs.Add(p.X);
        xs.Sort();
        double corte = 0, aterro = 0, ini = double.NaN, fim = double.NaN;
        for (int i = 1; i < xs.Count; i++)
        {
            double xa = xs[i - 1], xb = xs[i], w = xb - xa;
            if (w < 1e-12) continue;
            double xm = (xa + xb) / 2;
            int it = terreno.TrechoEm(xm), ip = projeto.TrechoEm(xm);
            if (it < 0 || ip < 0) continue; // buraco em uma das linhas
            if (double.IsNaN(ini)) ini = xa;
            fim = xb;
            double da = terreno.NoTrecho(it, xa) - projeto.NoTrecho(ip, xa), db = terreno.NoTrecho(it, xb) - projeto.NoTrecho(ip, xb);
            if (da >= 0 && db >= 0) corte += (da + db) / 2 * w;
            else if (da <= 0 && db <= 0) aterro -= (da + db) / 2 * w;
            else
            {
                double xc = w * da / (da - db); // distância até o cruzamento
                if (da > 0) { corte += da * xc / 2; aterro -= db * (w - xc) / 2; }
                else { aterro -= da * xc / 2; corte += db * (w - xc) / 2; }
            }
        }
        return new AreasSecao(corte, aterro, ini, fim);
    }
}

/// <summary>Uma estaca da nota de cubação.</summary>
public sealed class LinhaCubacao
{
    public string Nome { get; set; } = "";
    public double Estaca { get; set; }
    public double AreaCorte { get; set; }
    public double AreaAterro { get; set; }
    /// <summary>Distância até a estaca anterior (0 na primeira).</summary>
    public double Distancia { get; set; }
    /// <summary>Volumes geométricos do trecho entre a estaca anterior e esta (NaN se não se sabe).</summary>
    public double VolumeCorte { get; set; }
    public double VolumeAterro { get; set; }
    /// <summary>Volumes com os fatores (corte × fator do corte; aterro × fator de homogeneização).</summary>
    public double CorteFator { get; set; }
    public double AterroFator { get; set; }
    public double Compensacao { get; set; }
    public double Bruckner { get; set; }
    public string Obs { get; set; } = "";
    public double CorteExcedente => CorteFator - Compensacao;
    public double AterroReceber => AterroFator - Compensacao;
}

/// <summary>
/// Nota de cubação por seções: áreas por estaca, volumes por áreas médias (ou os do Civil 3D), compensação lateral e
/// ordenadas de Brückner (acumulado de corte × fator − aterro × fator).
/// </summary>
public sealed class Cubacao
{
    public string Alinhamento { get; set; } = "";
    public string Grupo { get; set; } = "";
    /// <summary>De onde vieram as áreas e volumes, em texto para o relatório.</summary>
    public string Origem { get; set; } = "";
    public string Terreno { get; set; } = "";
    public string Projeto { get; set; } = "";
    public double FatorCorte { get; set; } = 1;
    public double FatorAterro { get; set; } = 1.25;
    /// <summary>Volumes lidos da lista de materiais do Civil 3D (true) ou calculados pelas áreas médias (false).</summary>
    public bool VolumesDoCivil { get; set; }
    public bool CorrecaoCurva { get; set; }
    public double IntervaloEstaca { get; set; } = 20;
    public List<LinhaCubacao> Linhas { get; } = new();
    public List<string> Avisos { get; } = new();
    /// <summary>Áreas do Civil 3D (lista de materiais) por estaca, para conferir as calculadas aqui.</summary>
    public List<(double Estaca, double Corte, double Aterro)> Conferencia { get; } = new();
    public string ConferenciaNome { get; set; } = "";

    /// <summary>Monta a nota pelas áreas: ordena as estacas e calcula os volumes por áreas médias.</summary>
    public static Cubacao PorAreas(IEnumerable<(string Nome, double Estaca, double Corte, double Aterro, string Obs)> secoes, double fatorCorte, double fatorAterro)
    {
        if (!(fatorCorte > 0) || !(fatorAterro > 0)) throw new ArgumentException("Os fatores devem ser positivos.");
        var c = new Cubacao { FatorCorte = fatorCorte, FatorAterro = fatorAterro };
        foreach (var s in secoes.OrderBy(s => s.Estaca))
            c.Linhas.Add(new LinhaCubacao { Nome = s.Nome, Estaca = s.Estaca, AreaCorte = Z(s.Corte), AreaAterro = Z(s.Aterro), Obs = s.Obs });
        if (c.Linhas.Count < 2) throw new ArgumentException("São precisas pelo menos duas seções.");
        for (int i = 0; i < c.Linhas.Count; i++)
        {
            var l = c.Linhas[i];
            if (i == 0) { l.Distancia = 0; l.VolumeCorte = 0; l.VolumeAterro = 0; }
            else
            {
                var a = c.Linhas[i - 1];
                l.Distancia = l.Estaca - a.Estaca;
                l.VolumeCorte = (a.AreaCorte + l.AreaCorte) / 2 * l.Distancia;
                l.VolumeAterro = (a.AreaAterro + l.AreaAterro) / 2 * l.Distancia;
            }
            l.CorteFator = l.VolumeCorte * fatorCorte;
            l.AterroFator = l.VolumeAterro * fatorAterro;
        }
        c.Acumular();
        return c;
        static double Z(double v) => double.IsNaN(v) ? 0 : v;
    }

    /// <summary>Compensação lateral e ordenadas de Brückner a partir de CorteFator e AterroFator de cada linha.</summary>
    public void Acumular()
    {
        double b = 0;
        foreach (var l in Linhas)
        {
            l.Compensacao = Math.Min(l.CorteFator, l.AterroFator);
            b += l.CorteFator - l.AterroFator;
            l.Bruckner = b;
        }
    }

    public double TotalCorte => Linhas.Sum(l => l.VolumeCorte);
    public double TotalAterro => Linhas.Sum(l => l.VolumeAterro);
    public double TotalCorteFator => Linhas.Sum(l => l.CorteFator);
    public double TotalAterroFator => Linhas.Sum(l => l.AterroFator);
    public double TotalCompensacao => Linhas.Sum(l => l.Compensacao);
    /// <summary>Saldo final = última ordenada de Brückner: positivo sobra (bota-fora), negativo falta (empréstimo).</summary>
    public double Saldo => Linhas.Count == 0 ? 0 : Linhas[^1].Bruckner;
    public string Situacao => RelatorioVolumes.Situacao(Saldo, Math.Max(TotalCorteFator, TotalAterroFator));

    /// <summary>Maior diferença de área (m²) entre esta nota e a conferência do Civil, e a estaca onde ocorre.</summary>
    public (double Diferenca, double Estaca) MaiorDiferenca()
    {
        double d = 0, e = double.NaN;
        foreach (var (est, cc, ac) in Conferencia)
        {
            var l = Linhas.FirstOrDefault(x => Math.Abs(x.Estaca - est) < 0.01);
            if (l is null) continue;
            double dd = Math.Max(Math.Abs(l.AreaCorte - cc), Math.Abs(l.AreaAterro - ac));
            if (dd > d) { d = dd; e = est; }
        }
        return (d, e);
    }

    public string Estaca(double m) => NotaServico.FormatarEstaca(m, IntervaloEstaca, 2);

    public PastaXlsx Planilha()
    {
        if (Linhas.Count == 0) throw new InvalidOperationException("Nenhuma seção.");
        var pasta = new PastaXlsx { Titulo = $"Cubação {Alinhamento}" };
        var titulo = new EstiloCelula { Tamanho = 16, Negrito = true, Fundo = "D9D9D9", Horizontal = AlinhamentoH.Centro };
        var sub = titulo with { Tamanho = 10, Negrito = false };
        var cab = new EstiloCelula { Tamanho = 10, Negrito = true, Fundo = "F2F2F2", Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula { Tamanho = 10 };
        var txtC = txt with { Horizontal = AlinhamentoH.Centro };
        var n2 = new EstiloCelula { Tamanho = 10, Formato = "#,##0.00" };
        var n2N = n2 with { Negrito = true, Fundo = "F2F2F2" };
        var txtN = txt with { Negrito = true, Fundo = "F2F2F2" };
        var neg = n2 with { Formato = "#,##0.00;[Red]-#,##0.00" };
        var negN = neg with { Negrito = true, Fundo = "F2F2F2" };
        var nota = new EstiloCelula { Tamanho = 9, Italico = true, Borda = false };
        bool fc = Math.Abs(FatorCorte - 1) > 1e-9;
        string Fx(double v) => v.ToString("0.###", Num.Br);

        // ---- Cubação
        var f = pasta.NovaFolha("Cubação");
        var cols = new List<(string Cab, double Larg, Func<LinhaCubacao, double>? V, bool Neg)>
        {
            ("Estaca", 11, null, false),
            ("Distância (m)", 10, l => l.Distancia, false),
            ("Área de corte (m²)", 11, l => l.AreaCorte, false),
            ("Área de aterro (m²)", 11, l => l.AreaAterro, false),
            ("Volume de corte (m³)", 12, l => l.VolumeCorte, false),
            ("Volume de aterro (m³)", 12, l => l.VolumeAterro, false),
        };
        if (fc) cols.Add(($"Corte × {Fx(FatorCorte)} (m³)", 12, l => l.CorteFator, false));
        cols.Add(($"Aterro × {Fx(FatorAterro)} (m³)", 12, l => l.AterroFator, false));
        cols.Add(("Compensação lateral (m³)", 13.5, l => l.Compensacao, false));
        cols.Add(("Corte excedente (m³)", 12, l => l.CorteExcedente, false));
        cols.Add(("Aterro a receber (m³)", 12, l => l.AterroReceber, false));
        cols.Add(("Ordenada de Brückner (m³)", 13, l => l.Bruckner, true));
        cols.Add(("Observação", 24, null, false));
        int n = cols.Count;
        f.Mesclar(1, 1, 1, n, "Nota de cubação - volumes por seções", titulo);
        f.Mesclar(2, 1, 2, n, $"Alinhamento: {Alinhamento} · grupo de seções: {Grupo} · {Origem}", sub);
        f.Mesclar(3, 1, 3, n, (VolumesDoCivil ? "Volumes da lista de materiais do Civil 3D" + (CorrecaoCurva ? ", com correção de curva" : "") : "Volumes pelas áreas médias entre seções consecutivas") +
                              $" · fator do corte {Fx(FatorCorte)} · fator do aterro (homogeneização) {Fx(FatorAterro)} · Brückner = Σ (corte × fator − aterro × fator)", sub);
        f.Altura(1, 30); f.Altura(2, 18); f.Altura(3, 18); f.Altura(5, 44);
        for (int c = 0; c < n; c++) { f.Texto(5, c + 1, cols[c].Cab, cab); f.Largura(c + 1, cols[c].Larg); }
        int lin = 6;
        foreach (var l in Linhas)
        {
            f.Texto(lin, 1, Estaca(l.Estaca), txtC);
            for (int c = 1; c < n - 1; c++) f.Numero(lin, c + 1, Math.Round(cols[c].V!(l), 2), cols[c].Neg ? neg : n2);
            f.Texto(lin, n, l.Obs, txt);
            lin++;
        }
        f.Texto(lin, 1, "Total", txtN);
        f.Vazio(lin, 2, txtN);
        f.Vazio(lin, 3, txtN);
        f.Vazio(lin, 4, txtN);
        for (int c = 4; c < n - 2; c++) f.Numero(lin, c + 1, Math.Round(Linhas.Sum(cols[c].V!), 2), n2N);
        f.Numero(lin, n - 1, Math.Round(Saldo, 2), negN);
        f.Texto(lin, n, Situacao, txtN);
        lin += 2;
        f.Texto(lin++, 1, $"Corte: {V(TotalCorte)} m³" + (fc ? $" (× {Fx(FatorCorte)} = {V(TotalCorteFator)} m³)" : "") +
                          $" · aterro: {V(TotalAterro)} m³ (× {Fx(FatorAterro)} = {V(TotalAterroFator)} m³) · compensação lateral: {V(TotalCompensacao)} m³", nota);
        f.Texto(lin++, 1, $"Saldo final (última ordenada de Brückner): {V(Saldo)} m³ — {Situacao}. Ordenada subindo = corte sobrando no trecho; descendo = aterro pedindo material.", nota);
        f.Texto(lin++, 1, "Compensação lateral: o que o corte de cada trecho entre estacas cobre do aterro do mesmo trecho; o resto vai para o transporte longitudinal (Brückner).", nota);
        foreach (var a in Avisos) f.Texto(lin++, 1, "Aviso: " + a, nota);
        f.CongelarLinhas = 5;
        f.CongelarColunas = 1;
        f.TituloImpressaoAte = 5;
        f.Paisagem = true;
        f.CaberNaLargura = true;
        f.Papel = 8;

        // ---- Conferência com o Civil
        if (Conferencia.Count > 0)
        {
            var g = pasta.NovaFolha("Conferência Civil");
            string[] cc = { "Estaca", "Corte PROJETA (m²)", "Corte Civil (m²)", "Diferença (m²)", "Aterro PROJETA (m²)", "Aterro Civil (m²)", "Diferença (m²)" };
            g.Mesclar(1, 1, 1, cc.Length, "Conferência das áreas com o Civil 3D", titulo);
            g.Mesclar(2, 1, 2, cc.Length, $"Lista de materiais: {ConferenciaNome} (áreas sem fator)", sub);
            g.Altura(1, 30); g.Altura(4, 32);
            for (int c = 0; c < cc.Length; c++) { g.Texto(4, c + 1, cc[c], cab); g.Largura(c + 1, c == 0 ? 11 : 13); }
            int r = 5;
            foreach (var (est, cCiv, aCiv) in Conferencia)
            {
                var l = Linhas.FirstOrDefault(x => Math.Abs(x.Estaca - est) < 0.01);
                g.Texto(r, 1, Estaca(est), txtC);
                g.Numero(r, 2, l is null ? double.NaN : Math.Round(l.AreaCorte, 3), n2);
                g.Numero(r, 3, Math.Round(cCiv, 3), n2);
                g.Numero(r, 4, l is null ? double.NaN : Math.Round(l.AreaCorte - cCiv, 3), neg);
                g.Numero(r, 5, l is null ? double.NaN : Math.Round(l.AreaAterro, 3), n2);
                g.Numero(r, 6, Math.Round(aCiv, 3), n2);
                g.Numero(r, 7, l is null ? double.NaN : Math.Round(l.AreaAterro - aCiv, 3), neg);
                r++;
            }
            g.CongelarLinhas = 4;
            g.TituloImpressaoAte = 4;
        }

        // ---- Origem
        var o = pasta.NovaFolha("Origem");
        o.Largura(1, 28); o.Largura(2, 90);
        var rot = txt with { Negrito = true };
        int lo = 1;
        void Par(string k, string v) { o.Texto(lo, 1, k, rot); o.Texto(lo, 2, v, txt); lo++; }
        Par("Alinhamento", Alinhamento);
        Par("Grupo de seções", Grupo);
        Par("Origem", Origem);
        if (!string.IsNullOrEmpty(Terreno)) Par("Terreno", Terreno);
        if (!string.IsNullOrEmpty(Projeto)) Par("Projeto", Projeto);
        Par("Seções", $"{Linhas.Count}, de {Estaca(Linhas[0].Estaca)} a {Estaca(Linhas[^1].Estaca)} ({V(Linhas[^1].Estaca - Linhas[0].Estaca)} m)");
        Par("Volumes", VolumesDoCivil ? "da lista de materiais do Civil 3D" + (CorrecaoCurva ? " (com correção de curva)" : " (sem correção de curva)")
                                      : "áreas médias: V = (A1 + A2) / 2 × distância, sem correção de curva");
        Par("Áreas", VolumesDoCivil ? "as da lista de materiais do Civil 3D (sem fator)"
                                    : "entre as linhas de terreno e de projeto de cada seção, na faixa em que as duas existem (corte = terreno acima do projeto)");
        Par("Fatores", $"corte × {Fx(FatorCorte)}; aterro × {Fx(FatorAterro)}");
        if (Conferencia.Count > 0)
        {
            var (d, e) = MaiorDiferenca();
            Par("Conferência", $"maior diferença de área para a lista {ConferenciaNome}: {d.ToString("0.000", Num.Br)} m²" + (double.IsNaN(e) ? "" : $" na estaca {Estaca(e)}"));
        }
        foreach (var a in Avisos) Par("Aviso", a);
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);

    private static string V(double v) => v.ToString("#,##0.00", Num.Br);
}
