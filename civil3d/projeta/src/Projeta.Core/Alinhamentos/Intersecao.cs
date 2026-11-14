using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

/// <summary>Braço de uma interseção (para o relatório).</summary>
public sealed record DadosBracoIntersecao(string Rumo, string Eixo, string Estaca, double AnguloGraus, double MeiaLargura, double? Rampa, double? Tangencia, bool Principal);

/// <summary>Esquina (curva de concordância entre dois braços).</summary>
public sealed record EsquinaIntersecao(string Nome, string Entre, double Raio, double? Desenvolvimento, double AnguloGraus, string LadoPasseio, bool ComGreide, bool Coube);

/// <summary>Triângulo de visibilidade de uma aproximação com PARE.</summary>
public sealed record VisadaIntersecao(string Aproximacao, double Distancia, double Velocidade, double Tempo, string Situacao);

/// <summary>Geometria da interseção em nível: centro das curvas de esquina e distância de visibilidade de cruzamento.</summary>
public static class GeometriaIntersecao
{
    /// <summary>
    /// Centro da curva de esquina entre o braço de direção <paramref name="a1"/> e o braço seguinte no sentido anti-horário,
    /// de direção <paramref name="a2"/> (radianos, a partir do centro da interseção), com bordos a <paramref name="w1"/> (lado
    /// anti-horário do braço 1) e <paramref name="w2"/> (lado horário do braço 2) do eixo e raio <paramref name="r"/>.
    /// Coordenadas relativas ao centro. Null quando os braços estão em linha (sem esquina).
    /// </summary>
    public static (double X, double Y)? CentroEsquina(double a1, double a2, double w1, double w2, double r)
    {
        double n1x = -Math.Sin(a1), n1y = Math.Cos(a1);   // normal à esquerda do braço 1
        double n2x = Math.Sin(a2), n2y = -Math.Cos(a2);   // normal à direita do braço 2
        double det = n1x * n2y - n1y * n2x;
        if (Math.Abs(det) < 1e-6) return null;
        double c1 = w1 + r, c2 = w2 + r;
        return ((c1 * n2y - n1y * c2) / det, (n1x * c2 - c1 * n2x) / det);
    }

    /// <summary>Ângulo anti-horário de a1 até a2, em [0, 2π).</summary>
    public static double AnguloAntiHorario(double a1, double a2) => GeometriaRotatoria.Normalizar(a2 - a1);

    /// <summary>Tempo de travessia padrão (s): conversão à esquerda partindo do PARE, carro de passeio (AASHTO, caso B1).</summary>
    public const double TempoPadrao = 7.5;

    /// <summary>Recuo do motorista parado em relação ao bordo da via principal (m) (AASHTO: 14,5 ft).</summary>
    public const double RecuoMotorista = 4.4;

    /// <summary>Distância de visibilidade ao longo da via principal: d = 0,278·V·tg (m, km/h, s).</summary>
    public static double DistanciaVisibilidade(double vKmh, double tg = TempoPadrao) => 0.278 * vKmh * tg;

    /// <summary>Se o segmento PQ entra no círculo (centro C, raio r).</summary>
    public static bool SegmentoCortaCirculo((double X, double Y) p, (double X, double Y) q, (double X, double Y) c, double r)
    {
        double dx = q.X - p.X, dy = q.Y - p.Y, l2 = dx * dx + dy * dy;
        double t = l2 < 1e-12 ? 0 : Math.Clamp(((c.X - p.X) * dx + (c.Y - p.Y) * dy) / l2, 0, 1);
        double x = p.X + t * dx - c.X, y = p.Y + t * dy - c.Y;
        return x * x + y * y < r * r - 1e-6;
    }
}

/// <summary>Planilha do PRJINTERSECAO.</summary>
public sealed class RelatorioIntersecao
{
    public string Titulo { get; set; } = "Interseção";
    public string Desenho { get; set; } = "";
    public double Ox { get; set; }
    public double Oy { get; set; }
    public string Principal { get; set; } = "";
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<DadosBracoIntersecao> Bracos { get; } = new();
    public List<EsquinaIntersecao> Esquinas { get; } = new();
    public List<VisadaIntersecao> Visadas { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<RegiaoCorredor> Corredor { get; } = new();
    public List<string> Montagens { get; } = new();
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
        var n1 = new EstiloCelula { Formato = "0.0" };
        var n2 = new EstiloCelula { Formato = "0.00" };

        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };

        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int l = 3;
        void Par(string a, string b) { fr.Texto(l, 1, a, txt with { Negrito = true }); fr.Texto(l, 2, b, q); l++; }
        Par("Centro", $"E {Ox:F3}  N {Oy:F3}");
        Par("Braços", Bracos.Count.ToString());
        Par("Via principal", Principal);
        Par("Esquinas", $"{Esquinas.Count(e => e.Coube)} de {Esquinas.Count}");
        int ap = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado), rp = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado), at = Verificacoes.Count(v => v.Resultado == VerificacaoRotatoria.Atencao);
        Par("Verificações", $"{ap} aprovadas, {rp} reprovadas, {at} com atenção");
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        l++;
        fr.Texto(l, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 26); fr.Largura(2, 70);

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 40), ("Valor", 14), ("Unidade", 9), ("Fonte", 60));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, txt); fp.Texto(lp, 2, p.Valor, txt with { Horizontal = AlinhamentoH.Direita }); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        var fb = pasta.NovaFolha("Braços");
        Cab(fb, 1, ("Braço", 10), ("Eixo", 28), ("Estaca no círculo de busca", 14), ("Ângulo até o próximo (°)", 12), ("Meia largura (m)", 10), ("Rampa na aproximação (%)", 12), ("Esquina a (m do centro)", 12), ("Via", 12));
        int lb = 2;
        foreach (var b in Bracos)
        {
            fb.Texto(lb, 1, b.Rumo, txt); fb.Texto(lb, 2, b.Eixo, txt); fb.Texto(lb, 3, b.Estaca, txt);
            fb.Numero(lb, 4, b.AnguloGraus, n1); fb.Numero(lb, 5, b.MeiaLargura, n2);
            if (b.Rampa is { } r) fb.Numero(lb, 6, r * 100, n2); else fb.Texto(lb, 6, "sem greide", txt);
            if (b.Tangencia is { } t) fb.Numero(lb, 7, t, n2); else fb.Texto(lb, 7, "-", txt);
            fb.Texto(lb, 8, b.Principal ? "principal" : "secundária", txt);
            lb++;
        }

        var fe = pasta.NovaFolha("Esquinas");
        Cab(fe, 1, ("Esquina", 40), ("Entre", 16), ("Raio (m)", 9), ("Desenvolvimento (m)", 12), ("Ângulo (°)", 9), ("Passeio", 10), ("Greide", 12));
        int le = 2;
        foreach (var e in Esquinas)
        {
            fe.Texto(le, 1, e.Nome, q); fe.Texto(le, 2, e.Entre, txt); fe.Numero(le, 3, e.Raio, n2);
            if (e.Desenvolvimento is { } d) fe.Numero(le, 4, d, n2); else fe.Texto(le, 4, "não coube", Res(VerificacaoRotatoria.Reprovado));
            fe.Numero(le, 5, e.AnguloGraus, n1); fe.Texto(le, 6, e.LadoPasseio, txt); fe.Texto(le, 7, e.ComGreide ? "casa com os dois" : "sem greide", txt);
            le++;
        }

        if (Visadas.Count > 0)
        {
            var fs = pasta.NovaFolha("Visibilidade");
            Cab(fs, 1, ("Aproximação com PARE", 24), ("Distância (m)", 12), ("V da via principal (km/h)", 12), ("tg (s)", 8), ("Situação", 70));
            int ls = 2;
            foreach (var v in Visadas)
            {
                fs.Texto(ls, 1, v.Aproximacao, txt); fs.Numero(ls, 2, v.Distancia, n1); fs.Numero(ls, 3, v.Velocidade, n1); fs.Numero(ls, 4, v.Tempo, n1); fs.Texto(ls, 5, v.Situacao, q);
                ls++;
            }
            ls++;
            fs.Texto(ls, 1, "Motorista parado a 4,4 m do bordo da via principal, no meio da faixa; d = 0,278·V·tg ao longo da via principal, para os dois lados (AASHTO, caso B1 — referência complementar). O triângulo desenhado tem de ficar livre de obstáculos.", txt with { Borda = false, Italico = true });
        }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 38), ("Onde", 22), ("Valor", 40), ("Critério", 22), ("Fonte", 50), ("Resultado", 13));
        int lv = 2;
        foreach (var v in Verificacoes)
        {
            fv.Texto(lv, 1, v.Item, q); fv.Texto(lv, 2, v.Braco, q); fv.Texto(lv, 3, v.Valor, q); fv.Texto(lv, 4, v.Criterio, q); fv.Texto(lv, 5, v.Fonte, q);
            fv.Texto(lv, 6, v.Resultado, Res(v.Resultado));
            lv++;
        }

        if (Corredor.Count > 0 || Montagens.Count > 0)
        {
            var fc = pasta.NovaFolha("Corredor");
            Cab(fc, 1, ("Eixo (baseline)", 44), ("De (est.)", 12), ("Até (est.)", 12), ("Montagem", 46), ("Observação", 70));
            int lc = 2;
            foreach (var r in Corredor)
            {
                fc.Texto(lc, 1, r.Baseline, q); fc.Texto(lc, 2, Comum.Num.Estaca(r.De), txt); fc.Texto(lc, 3, Comum.Num.Estaca(r.Ate), txt);
                fc.Texto(lc, 4, r.Montagem, q); fc.Texto(lc, 5, r.Obs, q);
                lc++;
            }
            lc++;
            foreach (var m in Montagens) fc.Texto(lc++, 1, m, txt with { Borda = false });
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
