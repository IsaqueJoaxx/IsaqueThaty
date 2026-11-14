using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

/// <summary>Critérios das verificações da rotatória (referências complementares; a norma do órgão prevalece).</summary>
public sealed record CriteriosRotatoria
{
    /// <summary>Velocidade máxima na trajetória direta de entrada, V1 (uma faixa: 25 mph ≈ 40 km/h).</summary>
    public double VelMaxEntrada { get; init; } = 40;
    /// <summary>Diferença máxima entre V1 e V2 e entre V1 e V4.</summary>
    public double DifVelMax { get; init; } = 20;
    /// <summary>Largura mínima do refúgio de pedestres na ilha divisória (6 ft).</summary>
    public double RefugioMin { get; init; } = 1.8;
    /// <summary>Comprimento mínimo da ilha divisória elevada (100 ft).</summary>
    public double IlhaCompMin { get; init; } = 30.48;
    /// <summary>Faixa típica da largura de entrada de uma faixa (16 a 20 ft).</summary>
    public double EntradaMin { get; init; } = 4.9;
    public double EntradaMax { get; init; } = 6.1;
    /// <summary>Desvio do eixo do ramo para a direita do centro a partir do qual se aponta (m).</summary>
    public double DesvioDireitaMax { get; init; } = 1.0;
}

public sealed record Verificacao(string Item, string Braco, string Valor, string Criterio, string Fonte, string Resultado);

public sealed record ParametroRotatoria(string Nome, string Valor, string Unidade, string Fonte);

/// <summary>Dados do Civil que o relatório mostra por braço (o resto sai do modelo).</summary>
public sealed record DadosBraco(string Eixo, double Estaca, string EstacaTexto, double Cota, double? ComprimentoEntrada, double? ComprimentoSaida);

public static class VerificacaoRotatoria
{
    public const string Aprovado = "APROVADO", Reprovado = "REPROVADO", Atencao = "ATENÇÃO", Informativo = "INFORMATIVO";

    /// <summary>
    /// Distância do centro à reta do ramo (30 m antes da retenção, no sentido de quem chega): negativa = o eixo passa à
    /// ESQUERDA do centro (preferível na mão direita), positiva = à direita.
    /// </summary>
    public static double DesvioDoCentro(ModeloRotatoria m, BracoRotatoria b)
    {
        double s = b.SRetencao + 30;
        var p = b.Eixo.Ponto(s, 0);
        var (dx, dy) = b.Eixo.Direcao(s);
        // sentido de quem chega = −direção do eixo do braço
        double cx = -dx, cy = -dy;
        double cruz = cx * (m.Oy - p.Y) - cy * (m.Ox - p.X);
        return cruz; // > 0: centro à esquerda de quem chega → a reta passa à direita do centro
    }

    /// <summary>Folga (m, no bordo externo) entre a conexão de entrada do braço i e a de saída do próximo; null sem conexões.</summary>
    public static double? Folga(ModeloRotatoria m, int i)
    {
        int n = m.Bracos.Count;
        if (n < 2) return null;
        var a = m.Bracos[i];
        var b = m.Bracos[(i + 1) % n];
        if (a.MeioFioEntrada is not { } e || b.MeioFioSaida is not { } s) return null;
        double ae = Math.Atan2(e.Y - m.Oy, e.X - m.Ox), asd = Math.Atan2(s.Y - m.Oy, s.X - m.Ox);
        return GeometriaRotatoria.Folga(a.Angulo, ae, b.Angulo, asd) * m.RExt;
    }

    public static string Nome(BracoRotatoria b) => $"{b.Rumo} ({b.Nome})";

    public static List<Verificacao> Avaliar(ModeloRotatoria m, IReadOnlyList<AnaliseBraco> analises, CriteriosRotatoria c, IReadOnlyList<Visada>? visadas = null)
    {
        var r = new List<Verificacao>();
        const string fVel = "NCHRP 672 / TxDOT RDM 14.4.5 (referência complementar; confirmar na norma do órgão)";
        string N(double v, int casas = 1) => v.ToString("F" + casas);
        for (int i = 0; i < m.Bracos.Count; i++)
        {
            var b = m.Bracos[i];
            string nb = Nome(b);
            var an = analises.FirstOrDefault(x => x.Braco == i);
            if (an is not null)
            {
                foreach (var p in an.Problemas)
                    r.Add(new Verificacao("Trajetória mais rápida", nb, p, "a trajetória passa a 1,5 m de meios-fios e ilhas", "NCHRP 672 (método da trajetória mais rápida)", Reprovado));
                if (an.R1 is { } r1)
                    r.Add(new Verificacao("Velocidade de entrada V1 (trajetória direta)", nb, $"R1 = {N(r1.Raio)} m → V1 = {N(r1.Velocidade, 0)} km/h",
                        $"≤ {N(c.VelMaxEntrada, 0)} km/h", fVel, r1.Velocidade <= c.VelMaxEntrada ? Aprovado : Reprovado));
                else if (an.Direto is not null && !an.Problemas.Any(p => p.StartsWith("direto", StringComparison.Ordinal)))
                    r.Add(new Verificacao("Velocidade de entrada V1 (trajetória direta)", nb, "a trajetória não curva antes da retenção (entrada sem deflexão)",
                        $"≤ {N(c.VelMaxEntrada, 0)} km/h", fVel, Reprovado));
                if (an.R1 is { } a1 && an.R2 is { } a2)
                {
                    double d = Math.Abs(a1.Velocidade - a2.Velocidade);
                    r.Add(new Verificacao("Consistência V1 × V2", nb, $"|{N(a1.Velocidade, 0)} − {N(a2.Velocidade, 0)}| = {N(d, 0)} km/h", $"≤ {N(c.DifVelMax, 0)} km/h",
                        "AASHTO via TORUS (20 km/h); TxDOT RDM 14.4.5 pede 10 mph ≈ 16 km/h", d <= c.DifVelMax ? Aprovado : Reprovado));
                }
                if (an.R1 is { } b1 && an.R4 is { } b4)
                {
                    double d = Math.Abs(b1.Velocidade - b4.Velocidade);
                    r.Add(new Verificacao("Consistência V1 × V4", nb, $"|{N(b1.Velocidade, 0)} − {N(b4.Velocidade, 0)}| = {N(d, 0)} km/h", $"≤ {N(c.DifVelMax, 0)} km/h",
                        "TxDOT RDM 14.4.5 (V1–V4; lá 10 mph ≈ 16 km/h)", d <= c.DifVelMax ? Aprovado : Reprovado));
                }
                if (an.R1 is { } c1 && an.R3 is { } c3)
                    r.Add(new Verificacao("Saída mais folgada que a entrada (R3 ≥ R1)", nb, $"R3 = {N(c3.Raio)} m, R1 = {N(c1.Raio)} m", "R3 ≥ R1",
                        "[A CONFIRMAR] prática do NCHRP 672", c3.Raio >= c1.Raio ? Aprovado : Atencao));
            }
            if (Folga(m, i) is { } fo)
            {
                var prox = m.Bracos[(i + 1) % m.Bracos.Count];
                r.Add(new Verificacao("Folga no bordo do anel até a saída do próximo braço", $"{b.Rumo}→{prox.Rumo}", $"{N(fo, 2)} m", "≥ 0 (as conexões não se sobrepõem)",
                    "geometria", fo >= 0 ? Aprovado : Reprovado));
            }
            double entrada = b.BordoEntrada - b.IlhaLarguraIni / 2;
            r.Add(new Verificacao("Largura de entrada (da ilha ao meio-fio)", nb, $"{N(entrada, 2)} m", $"{N(c.EntradaMin)} a {N(c.EntradaMax)} m (uma faixa)",
                "TxDOT RDM, Roundabouts (16 a 20 ft) — referência complementar", entrada >= c.EntradaMin - 1e-6 && entrada <= c.EntradaMax + 1e-6 ? Aprovado : Atencao));
            if (b.TemIlha)
            {
                r.Add(new Verificacao("Refúgio de pedestres na ilha divisória", nb, $"{N(b.IlhaLarguraIni, 2)} m de largura junto do anel", $"≥ {N(c.RefugioMin)} m",
                    "FDOT FDM 213 (6 ft) — referência complementar", b.IlhaLarguraIni >= c.RefugioMin - 1e-6 ? Aprovado : Reprovado));
                double comp = b.SIlhaFim - b.SIlhaIni;
                r.Add(new Verificacao("Comprimento da ilha divisória", nb, $"{N(comp)} m", $"≥ {N(c.IlhaCompMin)} m",
                    "FDOT FDM 213 (100 ft) — referência complementar", comp >= c.IlhaCompMin - 1e-6 ? Aprovado : Atencao));
            }
            else r.Add(new Verificacao("Ilha divisória", nb, "sem ilha", "ilha elevada separando entrada e saída", "NCHRP 672 (referência complementar)", Atencao));
            if (visadas?.FirstOrDefault(x => x.Braco == i) is { } vi)
                r.Add(new Verificacao("Visibilidade à esquerda (triângulo de visibilidade)", nb,
                    $"d1 = {N(vi.D1)} m (entrante a {N(vi.VEntrante, 0)} km/h), d2 = {N(vi.D2)} m (circulante a {N(vi.VCirculante, 0)} km/h)" + (vi.Atravessa.Count > 0 ? "; " + string.Join("; ", vi.Atravessa) : ""),
                    "triângulo livre de obstáculos (vegetação alta, muros, postes, placas)", "NCHRP 672 6.7.3 (tc = 5 s, observador a 15 m; velocidades das pernas [A CONFIRMAR])",
                    vi.Atravessa.Count > 0 ? Atencao : Aprovado));
            double dv = DesvioDoCentro(m, b);
            string lado = Math.Abs(dv) < 0.25 ? "passa pelo centro (radial)" : dv > 0 ? $"passa {N(Math.Abs(dv), 2)} m à DIREITA do centro" : $"passa {N(Math.Abs(dv), 2)} m à esquerda do centro";
            r.Add(new Verificacao("Alinhamento do ramo em relação ao centro", nb, lado, "radial ou à esquerda do centro",
                "MaineDOT, Roundabout Design (eixo à esquerda do centro) — referência complementar", dv > c.DesvioDireitaMax ? Atencao : Aprovado));
        }
        double maxEntrada = m.Bracos.Count == 0 ? 0 : m.Bracos.Max(b => b.BordoEntrada - b.IlhaLarguraIni / 2);
        double pista = m.RExt - m.RIlha;
        r.Add(new Verificacao("Pista anelar × maior largura de entrada", "todos", $"{N(pista, 2)} m × {N(maxEntrada, 2)} m", "pista ≥ entrada",
            "[A CONFIRMAR] NCHRP 672 6.4.3", pista >= maxEntrada - 1e-6 ? Aprovado : Atencao));
        return r;
    }
}

/// <summary>Planilha do PRJROTATORIA: parâmetros, braços, trajetórias R1–R5, verificações e pendências.</summary>
public sealed class RelatorioRotatoria
{
    public string Titulo { get; set; } = "Rotatória";
    public string Desenho { get; set; } = "";
    public ModeloRotatoria Modelo { get; set; } = new();
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<DadosBraco> Dados { get; } = new();
    public List<AnaliseBraco> Analises { get; } = new();
    public List<Visada> Visadas { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<string> Avisos { get; } = new();
    public List<string> Pendencias { get; } = new();
    public string GreideAnel { get; set; } = "";

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";
    private const string F1 = "0.0", F2 = "0.00", F3 = "0.000";

    public PastaXlsx Planilha()
    {
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n1 = new EstiloCelula { Formato = F1 };
        var n2 = new EstiloCelula { Formato = F2 };
        var n3 = new EstiloCelula { Formato = F3 };

        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };

        // Resumo
        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int l = 3;
        void Par(string a, string b) { fr.Texto(l, 1, a, txt with { Negrito = true }); fr.Texto(l, 2, b, q); l++; }
        Par("Centro", $"E {Modelo.Ox:F3}  N {Modelo.Oy:F3}");
        Par("DCI / bordo externo", $"{2 * Modelo.RExt:F2} m (R {Modelo.RExt:F2} m)");
        Par("Ilha central (meio-fio)", $"R {Modelo.RIlha:F2} m");
        Par("Pista anelar", $"{Modelo.RExt - Modelo.RIlha:F2} m");
        Par("Braços", Modelo.Bracos.Count.ToString());
        Par("Greide do anel", GreideAnel);
        int ap = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado), rp = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado), at = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Atencao);
        Par("Verificações", $"{ap} aprovadas, {rp} reprovadas, {at} com atenção");
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        l++;
        fr.Texto(l++, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 26); fr.Largura(2, 70);

        // Parâmetros
        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 36), ("Valor", 12), ("Unidade", 9), ("Fonte", 60));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, txt); fp.Texto(lp, 2, p.Valor, txt with { Horizontal = AlinhamentoH.Direita }); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        // Braços
        var fb = pasta.NovaFolha("Braços");
        Cab(fb, 1, ("Braço", 10), ("Eixo do ramo", 22), ("Estaca do encontro", 12), ("Cota do anel", 10), ("Ângulo até o próximo (°)", 11),
            ("Largura de entrada (m)", 10), ("Largura de saída (m)", 10), ("Ilha: largura × comprimento (m)", 14), ("Conexão de entrada R / L (m)", 14),
            ("Conexão de saída R / L (m)", 14), ("Folga até o próximo (m)", 11), ("Eixo × centro (m; + = à direita)", 13));
        for (int i = 0; i < Modelo.Bracos.Count; i++)
        {
            var b = Modelo.Bracos[i];
            var d = i < Dados.Count ? Dados[i] : null;
            int li = i + 2, c = 1;
            fb.Texto(li, c++, b.Rumo, txt);
            fb.Texto(li, c++, d?.Eixo ?? b.Nome, txt);
            fb.Texto(li, c++, d?.EstacaTexto ?? "", txt);
            if (d is not null) fb.Numero(li, c, d.Cota, n3); c++;
            var prox = Modelo.Bracos[(i + 1) % Modelo.Bracos.Count];
            fb.Numero(li, c++, Modelo.Bracos.Count > 1 ? GeometriaRotatoria.Normalizar(prox.Angulo - b.Angulo) * 180 / Math.PI : 360, n1);
            fb.Numero(li, c++, b.BordoEntrada - b.IlhaLarguraIni / 2, n2);
            fb.Numero(li, c++, b.BordoSaida - b.IlhaLarguraIni / 2, n2);
            fb.Texto(li, c++, b.TemIlha ? $"{b.IlhaLarguraIni:F2} × {b.SIlhaFim - b.SIlhaIni:F1}" : "sem ilha", txt);
            fb.Texto(li, c++, b.MeioFioEntrada is { } e ? $"{e.R:F1} / {(d?.ComprimentoEntrada is { } ce ? ce.ToString("F2") : "-")}" : "não coube", txt);
            fb.Texto(li, c++, b.MeioFioSaida is { } s ? $"{s.R:F1} / {(d?.ComprimentoSaida is { } cs ? cs.ToString("F2") : "-")}" : "não coube", txt);
            if (VerificacaoRotatoria.Folga(Modelo, i) is { } fo) fb.Numero(li, c, fo, n2); c++;
            fb.Numero(li, c++, VerificacaoRotatoria.DesvioDoCentro(Modelo, b), n2);
        }

        // Trajetórias
        var ft = pasta.NovaFolha("Trajetórias");
        Cab(ft, 1, ("Braço", 10), ("R1 (m)", 8), ("V1 (km/h)", 9), ("R2 (m)", 8), ("V2 (km/h)", 9), ("R3 (m)", 8), ("V3 (km/h)", 9),
            ("R4 (m)", 8), ("V4 (km/h)", 9), ("R5 (m)", 8), ("V5 (km/h)", 9), ("|V1 − V2|", 9), ("|V1 − V4|", 9), ("Saída direta / esquerda / direita", 22));
        int lt = 2;
        foreach (var an in Analises)
        {
            var b = Modelo.Bracos[an.Braco];
            int c = 1;
            ft.Texto(lt, c++, b.Rumo, txt);
            foreach (var rc in new[] { an.R1, an.R2, an.R3, an.R4, an.R5 })
            {
                if (rc is not null) { ft.Numero(lt, c, rc.Raio, n1); ft.Numero(lt, c + 1, rc.Velocidade, n1); }
                else { ft.Texto(lt, c, "-", txt); ft.Texto(lt, c + 1, "-", txt); }
                c += 2;
            }
            if (an.R1 is { } a1 && an.R2 is { } a2) ft.Numero(lt, c, Math.Abs(a1.Velocidade - a2.Velocidade), n1); c++;
            if (an.R1 is { } b1 && an.R4 is { } b4) ft.Numero(lt, c, Math.Abs(b1.Velocidade - b4.Velocidade), n1); c++;
            string Sai(Trajetoria? t) => t is null ? "-" : Modelo.Bracos[t.Saida].Rumo;
            ft.Texto(lt, c++, $"{Sai(an.Direto)} / {Sai(an.Esquerda)} / {Sai(an.Direita)}", txt);
            lt++;
        }
        lt++;
        ft.Texto(lt++, 1, "Trajetória mais rápida: curva mais suave a 1,5 m de meios-fios, ilhas e do eixo da via, começando e terminando a 50 m das linhas de retenção (NCHRP 672). Raios pelo arco de 3 pontos de 10 m.", txt with { Borda = false, Italico = true });
        ft.Texto(lt++, 1, "V = 8,7602·R^0,3861 (e = +0,02: R1, R3, R5) e V = 8,6164·R^0,3673 (e = −0,02: R2, R4), V em km/h e R em m (NCHRP 672).", txt with { Borda = false, Italico = true });

        // Visibilidade
        if (Visadas.Count > 0)
        {
            var fs = pasta.NovaFolha("Visibilidade");
            Cab(fs, 1, ("Braço", 10), ("d1 entrante (m)", 11), ("Ve (km/h)", 9), ("d2 circulante (m)", 11), ("Vc (km/h)", 9), ("O que as visadas cruzam", 70));
            int ls = 2;
            foreach (var v in Visadas)
            {
                fs.Texto(ls, 1, Modelo.Bracos[v.Braco].Rumo, txt);
                fs.Numero(ls, 2, v.D1, n1); fs.Numero(ls, 3, v.VEntrante, n1);
                fs.Numero(ls, 4, v.D2, n1); fs.Numero(ls, 5, v.VCirculante, n1);
                fs.Texto(ls, 6, v.Atravessa.Count == 0 ? "nada: as visadas ficam sobre a pista" : string.Join("; ", v.Atravessa), q);
                ls++;
            }
            ls++;
            fs.Texto(ls++, 1, "Observador a 15 m da retenção, no meio da faixa de entrada; d = 0,278·tc·V com tc = 5 s; Ve = média de V1 e V2 do braço de montante, Vc = V4 (NCHRP 672, 6.7.3). O triângulo desenhado tem de ficar livre de obstáculos; o NCHRP também recomenda não dar muito mais visibilidade que a mínima.", txt with { Borda = false, Italico = true });
        }

        // Verificações
        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 38), ("Braço", 14), ("Valor", 40), ("Critério", 22), ("Fonte", 50), ("Resultado", 13));
        int lv = 2;
        foreach (var v in Verificacoes)
        {
            fv.Texto(lv, 1, v.Item, q); fv.Texto(lv, 2, v.Braco, txt); fv.Texto(lv, 3, v.Valor, q); fv.Texto(lv, 4, v.Criterio, q); fv.Texto(lv, 5, v.Fonte, q);
            fv.Texto(lv, 6, v.Resultado, Res(v.Resultado));
            lv++;
        }

        // Pendências e avisos
        var fn = pasta.NovaFolha("Pendências");
        Cab(fn, 1, ("Pendência / aviso", 120));
        int ln = 2;
        foreach (var a in Avisos) fn.Texto(ln++, 1, "AVISO: " + a, q);
        foreach (var p in Pendencias) fn.Texto(ln++, 1, p, q);
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
