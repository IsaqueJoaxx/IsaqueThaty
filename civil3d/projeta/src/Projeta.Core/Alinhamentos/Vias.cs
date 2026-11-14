using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

/// <summary>Vértice de polilinha (bulge = tangente de um quarto do ângulo do arco que sai dele; + = anti-horário).</summary>
public sealed record VerticeVia(double X, double Y, double Bulge = 0);

/// <summary>Encontro de duas ruas: cruzamento (os eixos se cortam), T (a ponta de uma encosta na outra) ou ponta com ponta.</summary>
public sealed record CruzamentoVias(int A, double EstacaA, int B, double EstacaB, double X, double Y, string Tipo);

/// <summary>PIV do greide (estaca, cota, comprimento da curva vertical; Fixo = cota imposta, como a de um cruzamento).</summary>
public sealed record PivVia(double Estaca, double Cota, double Curva = 0, bool Fixo = false);

/// <summary>
/// Geometria das vias de loteamento feitas em lote (PRJVIAS): arredondamento dos vértices das polilinhas dos eixos, encontros
/// entre as ruas e greide proposto que acompanha o terreno e passa nas mesmas cotas nos encontros.
/// </summary>
public static class GeometriaVias
{
    /// <summary>
    /// Troca cada vértice entre dois trechos retos por um arco de raio <paramref name="raio"/> tangente aos dois. Onde o raio
    /// não cabe (trechos curtos), usa o maior que cabe e avisa; vértices junto de arcos que já existem ficam como estão.
    /// </summary>
    public static List<VerticeVia> Arredondar(IReadOnlyList<VerticeVia> v, double raio, List<string> avisos, string nome)
    {
        var r = new List<VerticeVia>();
        if (v.Count < 3 || raio <= 0) { r.AddRange(v); return r; }
        // quanto de cada trecho já foi usado pelo arco do vértice anterior
        double usado = 0;
        r.Add(v[0]);
        for (int i = 1; i < v.Count - 1; i++)
        {
            var a = v[i - 1];
            var p = v[i];
            var b = v[i + 1];
            double l1 = Dist(a, p), l2 = Dist(p, b);
            if (Math.Abs(a.Bulge) > 1e-9 || Math.Abs(p.Bulge) > 1e-9 || l1 < 1e-6 || l2 < 1e-6) { r.Add(p); usado = 0; continue; }
            double u1x = (p.X - a.X) / l1, u1y = (p.Y - a.Y) / l1, u2x = (b.X - p.X) / l2, u2y = (b.Y - p.Y) / l2;
            double cos = Math.Clamp(u1x * u2x + u1y * u2y, -1, 1), giro = u1x * u2y - u1y * u2x;
            double delta = Math.Acos(cos);
            if (delta < 0.5 * Math.PI / 180) { r.Add(p); usado = 0; continue; }
            if (delta > 175 * Math.PI / 180)
            {
                avisos.Add($"{nome}: o vértice {i} volta quase sobre o próprio eixo ({delta * 180 / Math.PI:0.0}°) e ficou sem curva.");
                r.Add(p);
                usado = 0;
                continue;
            }
            double tg = Math.Tan(delta / 2);
            // o arco usa no máximo metade de cada trecho (o resto fica para o vértice vizinho); nas pontas, quase tudo
            double livre1 = (i - 1 == 0 ? 0.95 * l1 : 0.5 * l1) - 0, livre2 = i + 1 == v.Count - 1 ? 0.95 * l2 : 0.5 * l2;
            livre1 = Math.Min(livre1, l1 - usado);
            double t = raio * tg, rr = raio;
            if (t > Math.Min(livre1, livre2))
            {
                t = Math.Max(0, Math.Min(livre1, livre2));
                rr = t / tg;
                avisos.Add($"{nome}: o raio de {raio:0.##} m não cabe no vértice {i}; ficou com {rr:0.00} m.");
            }
            if (t < 0.01) { r.Add(p); usado = 0; continue; }
            r.Add(new VerticeVia(p.X - t * u1x, p.Y - t * u1y, Math.Sign(giro) * Math.Tan(delta / 4)));
            r.Add(new VerticeVia(p.X + t * u2x, p.Y + t * u2y, 0));
            usado = t;
        }
        r.Add(v[^1]);
        return r;
    }

    private static double Dist(VerticeVia a, VerticeVia b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    /// <summary>
    /// Encontros entre as ruas, cada uma dada pelos pontos do eixo (X, Y, estaca) em sequência: onde os eixos se cortam e onde
    /// a ponta de uma fica a até <paramref name="tolPonta"/> de outra (T) ou da ponta da outra (ponta com ponta).
    /// </summary>
    public static List<CruzamentoVias> Cruzamentos(IReadOnlyList<IReadOnlyList<(double X, double Y, double S)>> ruas, double tolPonta)
    {
        var r = new List<CruzamentoVias>();
        bool Perto(double x, double y, int a, int b) => r.Any(c => ((c.A == a && c.B == b) || (c.A == b && c.B == a)) && Math.Abs(c.X - x) < 1 && Math.Abs(c.Y - y) < 1);
        for (int a = 0; a < ruas.Count; a++)
            for (int b = a + 1; b < ruas.Count; b++)
            {
                var pa = ruas[a];
                var pb = ruas[b];
                for (int i = 0; i + 1 < pa.Count; i++)
                    for (int j = 0; j + 1 < pb.Count; j++)
                    {
                        if (Corte(pa[i], pa[i + 1], pb[j], pb[j + 1]) is not { } c) continue;
                        double x = pa[i].X + c.T * (pa[i + 1].X - pa[i].X), y = pa[i].Y + c.T * (pa[i + 1].Y - pa[i].Y);
                        if (Perto(x, y, a, b)) continue;
                        double sa = pa[i].S + c.T * (pa[i + 1].S - pa[i].S), sb = pb[j].S + c.U * (pb[j + 1].S - pb[j].S);
                        bool pontaA = sa - pa[0].S < 0.5 || pa[^1].S - sa < 0.5, pontaB = sb - pb[0].S < 0.5 || pb[^1].S - sb < 0.5;
                        r.Add(new CruzamentoVias(a, sa, b, sb, x, y, pontaA && pontaB ? "ponta com ponta" : pontaA || pontaB ? "T" : "cruzamento"));
                    }
            }
        // pontas que param perto de outra rua (sem cortar)
        for (int a = 0; a < ruas.Count; a++)
            foreach (var ponta in new[] { ruas[a][0], ruas[a][^1] })
                for (int b = 0; b < ruas.Count; b++)
                {
                    if (b == a || Projetar(ruas[b], ponta.X, ponta.Y) is not { } pj || pj.D > tolPonta || Perto(pj.X, pj.Y, a, b) || Perto(ponta.X, ponta.Y, a, b)) continue;
                    bool pontaB = pj.S - ruas[b][0].S < 0.5 || ruas[b][^1].S - pj.S < 0.5;
                    var c = a < b
                        ? new CruzamentoVias(a, ponta.S, b, pj.S, pj.X, pj.Y, pontaB ? "ponta com ponta" : "T")
                        : new CruzamentoVias(b, pj.S, a, ponta.S, pj.X, pj.Y, pontaB ? "ponta com ponta" : "T");
                    r.Add(c);
                }
        return r.OrderBy(c => c.A).ThenBy(c => c.EstacaA).ToList();
    }

    private static (double T, double U)? Corte((double X, double Y, double S) p1, (double X, double Y, double S) p2, (double X, double Y, double S) q1, (double X, double Y, double S) q2)
    {
        double rx = p2.X - p1.X, ry = p2.Y - p1.Y, sx = q2.X - q1.X, sy = q2.Y - q1.Y, den = rx * sy - ry * sx;
        if (Math.Abs(den) < 1e-12) return null;
        double qpx = q1.X - p1.X, qpy = q1.Y - p1.Y;
        double t = (qpx * sy - qpy * sx) / den, u = (qpx * ry - qpy * rx) / den;
        return t >= -1e-9 && t <= 1 + 1e-9 && u >= -1e-9 && u <= 1 + 1e-9 ? (t, u) : null;
    }

    private static (double X, double Y, double S, double D)? Projetar(IReadOnlyList<(double X, double Y, double S)> p, double x, double y)
    {
        (double X, double Y, double S, double D)? m = null;
        for (int i = 0; i + 1 < p.Count; i++)
        {
            double dx = p[i + 1].X - p[i].X, dy = p[i + 1].Y - p[i].Y, l2 = dx * dx + dy * dy;
            double t = l2 < 1e-12 ? 0 : Math.Clamp(((x - p[i].X) * dx + (y - p[i].Y) * dy) / l2, 0, 1);
            double px = p[i].X + t * dx, py = p[i].Y + t * dy, d = Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
            if (m is null || d < m.Value.D) m = (px, py, p[i].S + t * (p[i + 1].S - p[i].S), d);
        }
        return m;
    }

    /// <summary>
    /// Greide proposto que acompanha o terreno: começa e termina na cota do terreno, passa nas cotas fixas (encontros com as
    /// outras ruas) e ganha PIV, no próprio terreno, onde o terreno se afasta mais que <paramref name="tol"/> da reta entre os
    /// PIVs (simplificação de Douglas–Peucker), sem PIVs a menos de <paramref name="dMin"/> um do outro.
    /// </summary>
    public static List<PivVia> GreideProposto(IReadOnlyList<(double S, double Z)> terreno, IEnumerable<(double S, double Z)> fixos, double tol, double dMin)
    {
        var t = terreno.OrderBy(p => p.S).ToList();
        if (t.Count < 2) return new List<PivVia>();
        double s0 = t[0].S, s1 = t[^1].S;
        var fx = new List<PivVia> { new(s0, t[0].Z, 0, false), new(s1, t[^1].Z, 0, false) };
        foreach (var (s, z) in fixos.OrderBy(f => f.S))
        {
            double sc = Math.Clamp(s, s0, s1);
            int k = fx.FindIndex(f => Math.Abs(f.Estaca - sc) < 1.0);
            if (k >= 0) { if (!fx[k].Fixo) fx[k] = new PivVia(fx[k].Estaca, z, 0, true); continue; }
            fx.Add(new PivVia(sc, z, 0, true));
        }
        fx = fx.OrderBy(f => f.Estaca).ToList();
        var r = new List<PivVia> { fx[0] };
        for (int k = 0; k + 1 < fx.Count; k++)
        {
            var meio = new List<PivVia>();
            Dividir(t, fx[k], fx[k + 1], tol, dMin, meio);
            r.AddRange(meio.OrderBy(p => p.Estaca));
            r.Add(fx[k + 1]);
        }
        return r;
    }

    private static void Dividir(List<(double S, double Z)> t, PivVia a, PivVia b, double tol, double dMin, List<PivVia> saida)
    {
        if (b.Estaca - a.Estaca < 2 * dMin) return;
        double g = (b.Cota - a.Cota) / (b.Estaca - a.Estaca), max = tol;
        int im = -1;
        for (int i = 0; i < t.Count; i++)
        {
            if (t[i].S <= a.Estaca + dMin || t[i].S >= b.Estaca - dMin) continue;
            double d = Math.Abs(t[i].Z - (a.Cota + g * (t[i].S - a.Estaca)));
            if (d > max) { max = d; im = i; }
        }
        if (im < 0) return;
        var p = new PivVia(t[im].S, t[im].Z);
        saida.Add(p);
        Dividir(t, a, p, tol, dMin, saida);
        Dividir(t, p, b, tol, dMin, saida);
    }

    /// <summary>
    /// Curva vertical (parábola simétrica) em cada PIV interno: L = K·A (A em %, diferença das rampas), no mínimo
    /// <paramref name="lMin"/>; encurtada para caber entre os PIVs vizinhos (as meias curvas não se sobrepõem). Sem curva nos
    /// PIVs fixos, para o greide passar na cota do encontro (a curva vizinha também não chega nele).
    /// </summary>
    public static List<PivVia> CurvasVerticais(IReadOnlyList<PivVia> pivs, double k, double lMin)
    {
        var p = pivs.ToList();
        int n = p.Count;
        if (n < 3) return p;
        var l = new double[n];
        for (int i = 1; i < n - 1; i++)
        {
            double g1 = (p[i].Cota - p[i - 1].Cota) / (p[i].Estaca - p[i - 1].Estaca), g2 = (p[i + 1].Cota - p[i].Cota) / (p[i + 1].Estaca - p[i].Estaca);
            double a = Math.Abs(g2 - g1) * 100;
            // no PIV de cota fixa (encontro com outra rua) não há curva: o greide passa exatamente na cota do encontro
            l[i] = p[i].Fixo || a < 0.2 ? 0 : Math.Max(lMin, k * a);
        }
        for (int volta = 0; volta < 3; volta++)
            for (int i = 0; i + 1 < n; i++)
            {
                double d = p[i + 1].Estaca - p[i].Estaca, soma = l[i] / 2 + l[i + 1] / 2, lim = 0.95 * d;
                if (soma <= lim || soma < 1e-9) continue;
                double f = lim / soma;
                l[i] *= f;
                l[i + 1] *= f;
            }
        for (int i = 1; i < n - 1; i++) p[i] = p[i] with { Curva = l[i] < 1 ? 0 : Math.Round(l[i], 2) };
        return p;
    }

    /// <summary>Rampas das tangentes entre PIVs (de, até, rampa em fração).</summary>
    public static List<(double De, double Ate, double Rampa)> Rampas(IReadOnlyList<PivVia> pivs)
    {
        var r = new List<(double, double, double)>();
        for (int i = 0; i + 1 < pivs.Count; i++)
        {
            double d = pivs[i + 1].Estaca - pivs[i].Estaca;
            if (d > 1e-9) r.Add((pivs[i].Estaca, pivs[i + 1].Estaca, (pivs[i + 1].Cota - pivs[i].Cota) / d));
        }
        return r;
    }
}

/// <summary>Rua feita pelo PRJVIAS (para o relatório).</summary>
public sealed record RuaVia(string Nome, string Origem, double Extensao, int Curvas, double? RaioMin, double? TerrenoMin, double? TerrenoMax,
    double? RampaMin, double? RampaMax, int Pivs, string Greide, string Corredor);

/// <summary>Encontro entre ruas (para o relatório).</summary>
public sealed record EncontroVia(string RuaA, string EstacaA, string RuaB, string EstacaB, double X, double Y, double? Cota, string Tipo);

/// <summary>PIV do greide proposto de uma rua (para o relatório).</summary>
public sealed record PivRelatorio(string Rua, string Estaca, double Cota, double Curva, double? RampaChegando, string Obs);

/// <summary>Planilha do PRJVIAS.</summary>
public sealed class RelatorioVias
{
    public string Titulo { get; set; } = "Vias do loteamento";
    public string Desenho { get; set; } = "";
    public string Superficie { get; set; } = "";
    public string Corredor { get; set; } = "";
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<RuaVia> Ruas { get; } = new();
    public List<EncontroVia> Encontros { get; } = new();
    public List<PivRelatorio> Pivs { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<string> Avisos { get; } = new();
    public List<string> Pendencias { get; } = new();

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";

    public PastaXlsx Planilha()
    {
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n2 = new EstiloCelula { Formato = "0.00" };
        var n3 = new EstiloCelula { Formato = "0.000" };
        var pc = new EstiloCelula { Formato = "0.00%" };

        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };
        void Num(FolhaXlsx f, int l, int c, double? v, EstiloCelula e) { if (v is { } x) f.Numero(l, c, x, e); else f.Texto(l, c, "-", txt); }

        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string a, string b) { fr.Texto(lr, 1, a, txt with { Negrito = true }); fr.Texto(lr, 2, b, q); lr++; }
        Par("Ruas", Ruas.Count.ToString());
        Par("Extensão total", $"{Ruas.Sum(r => r.Extensao):0.00} m");
        Par("Encontros entre ruas", Encontros.Count.ToString());
        Par("Superfície do terreno", Superficie.Length > 0 ? Superficie : "nenhuma");
        Par("Corredor", Corredor.Length > 0 ? Corredor : "não criado");
        int ap = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado), rp = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado), at = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Atencao);
        Par("Verificações", $"{ap} aprovadas, {rp} reprovadas, {at} com atenção");
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: o greide é uma proposta inicial que acompanha o terreno; a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 26); fr.Largura(2, 70);

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 42), ("Valor", 14), ("Unidade", 9), ("Fonte", 64));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, txt); fp.Texto(lp, 2, p.Valor, txt with { Horizontal = AlinhamentoH.Direita }); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        var fu = pasta.NovaFolha("Ruas");
        Cab(fu, 1, ("Rua", 18), ("Origem", 22), ("Extensão (m)", 11), ("Curvas", 8), ("Menor raio (m)", 10), ("Terreno mín. (m)", 11), ("Terreno máx. (m)", 11),
            ("Rampa mín.", 10), ("Rampa máx.", 10), ("PIVs", 7), ("Greide", 26), ("Corredor", 22));
        int lu = 2;
        foreach (var r in Ruas)
        {
            fu.Texto(lu, 1, r.Nome, txt); fu.Texto(lu, 2, r.Origem, q); fu.Numero(lu, 3, r.Extensao, n2); fu.Numero(lu, 4, r.Curvas, txt);
            Num(fu, lu, 5, r.RaioMin, n2); Num(fu, lu, 6, r.TerrenoMin, n3); Num(fu, lu, 7, r.TerrenoMax, n3); Num(fu, lu, 8, r.RampaMin, pc); Num(fu, lu, 9, r.RampaMax, pc);
            fu.Numero(lu, 10, r.Pivs, txt); fu.Texto(lu, 11, r.Greide, q); fu.Texto(lu, 12, r.Corredor, q);
            lu++;
        }

        if (Pivs.Count > 0)
        {
            var fg = pasta.NovaFolha("Greides");
            Cab(fg, 1, ("Rua", 18), ("Estaca", 12), ("Cota (m)", 11), ("Curva vertical L (m)", 12), ("Rampa chegando", 12), ("Observação", 44));
            int lg = 2;
            foreach (var p in Pivs)
            {
                fg.Texto(lg, 1, p.Rua, txt); fg.Texto(lg, 2, p.Estaca, txt); fg.Numero(lg, 3, p.Cota, n3); fg.Numero(lg, 4, p.Curva, n2); Num(fg, lg, 5, p.RampaChegando, pc); fg.Texto(lg, 6, p.Obs, q);
                lg++;
            }
        }

        if (Encontros.Count > 0)
        {
            var fe = pasta.NovaFolha("Encontros");
            Cab(fe, 1, ("Rua", 18), ("Estaca", 12), ("Com a rua", 18), ("Estaca", 12), ("E (m)", 14), ("N (m)", 14), ("Cota do greide (m)", 12), ("Tipo", 16));
            int le = 2;
            foreach (var e in Encontros)
            {
                fe.Texto(le, 1, e.RuaA, txt); fe.Texto(le, 2, e.EstacaA, txt); fe.Texto(le, 3, e.RuaB, txt); fe.Texto(le, 4, e.EstacaB, txt);
                fe.Numero(le, 5, e.X, n3); fe.Numero(le, 6, e.Y, n3); Num(fe, le, 7, e.Cota, n3); fe.Texto(le, 8, e.Tipo, txt);
                le++;
            }
            le++;
            fe.Texto(le, 1, "Nos encontros, os greides das duas ruas passam na mesma cota (a do terreno no ponto). Monte cada esquina com o PRJINTERSECAO, no mesmo corredor (opção Existente).", txt with { Borda = false, Italico = true });
        }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 34), ("Onde", 26), ("Valor", 40), ("Critério", 22), ("Fonte", 50), ("Resultado", 13));
        int lv = 2;
        foreach (var v in Verificacoes)
        {
            fv.Texto(lv, 1, v.Item, q); fv.Texto(lv, 2, v.Braco, q); fv.Texto(lv, 3, v.Valor, q); fv.Texto(lv, 4, v.Criterio, q); fv.Texto(lv, 5, v.Fonte, q);
            fv.Texto(lv, 6, v.Resultado, Res(v.Resultado));
            lv++;
        }

        var fn = pasta.NovaFolha("Pendências");
        Cab(fn, 1, ("Pendência / aviso", 120));
        int ln = 2;
        foreach (var a in Avisos) fn.Texto(ln++, 1, "AVISO: " + a, q);
        foreach (var p in Pendencias) fn.Texto(ln++, 1, p, q);
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
