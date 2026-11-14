using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Core.Horizontal;

/// <summary>Resultado genérico de marca transversal, canalização ou inscrição.</summary>
public sealed class ResultadoPeca
{
    public Desenho2D Desenho { get; } = new();
    public string Codigo { get; init; } = "";
    public string Descricao { get; set; } = "";
    public Cor Cor { get; set; } = Cor.Branco;
    public double Area { get; set; }
    /// <summary>Comprimento de referência (largura da via atravessada, extensão etc.).</summary>
    public double Comprimento { get; set; }
    public int Pecas { get; set; }

    internal void Add(List<V2> poli, Cor cor)
    {
        poli = Poligonos.Limpar(poli);
        if (poli.Count < 3) return;
        Desenho.Area(poli, cor);
        Area += Poligonos.Area(poli);
        Pecas++;
    }
}

/// <summary>
/// Marcas transversais e de canalização. Os pontos P1→P2 são sempre a linha que atravessa a via
/// (de meio-fio a meio-fio ou de bordo a eixo); o fluxo de veículos é perpendicular a ela.
/// </summary>
public static class GeradorTransversal
{
    /// <summary>LRE — linha de retenção (0,30 a 0,60 m), contínua, centrada em P1→P2.</summary>
    public static ResultadoPeca LRE(V2 p1, V2 p2, double largura)
    {
        var r = new ResultadoPeca { Codigo = "LRE", Descricao = "Linha de retenção", Comprimento = V2.Dist(p1, p2) };
        r.Add(Poligonos.Retangulo((p1 + p2) / 2, p2 - p1, r.Comprimento, largura), Cor.Branco);
        return r;
    }

    /// <summary>LDP — linha de "Dê a preferência": seccionada (traço e espaço de 0,50 m).</summary>
    public static ResultadoPeca LDP(V2 p1, V2 p2, double largura, double traco, double espaco)
    {
        var r = new ResultadoPeca { Codigo = "LDP", Descricao = "Linha de dê a preferência", Comprimento = V2.Dist(p1, p2) };
        var u = (p2 - p1).Unitario();
        for (double s = 0; s < r.Comprimento - 0.02; s += traco + espaco)
        {
            double b = Math.Min(r.Comprimento, s + traco);
            r.Add(Poligonos.Retangulo(p1 + u * ((s + b) / 2), u, b - s, largura), Cor.Branco);
        }
        return r;
    }

    /// <summary>FTP-1 — faixa de pedestres zebrada: listras paralelas ao fluxo, distribuídas ao longo de P1→P2.</summary>
    public static ResultadoPeca FTP1(V2 p1, V2 p2, double extensao, double largura, double espaco)
    {
        var r = new ResultadoPeca { Codigo = "FTP-1", Descricao = "Faixa de travessia de pedestres zebrada", Comprimento = V2.Dist(p1, p2) };
        var u = (p2 - p1).Unitario();
        var fluxo = u.Esquerda;
        int n = (int)Math.Floor((r.Comprimento + espaco) / (largura + espaco) + 1e-9);
        if (n < 1) return r;
        double ocupado = n * largura + (n - 1) * espaco;
        double s = (r.Comprimento - ocupado) / 2 + largura / 2;
        for (int i = 0; i < n; i++, s += largura + espaco)
            r.Add(Poligonos.Retangulo(p1 + u * s, fluxo, extensao, largura), Cor.Branco);
        return r;
    }

    /// <summary>FTP-2 — faixa de pedestres de linhas paralelas: duas linhas ao longo de P1→P2, com distância livre = extensão.</summary>
    public static ResultadoPeca FTP2(V2 p1, V2 p2, double extensao, double largura)
    {
        var r = new ResultadoPeca { Codigo = "FTP-2", Descricao = "Faixa de travessia de pedestres paralela", Comprimento = V2.Dist(p1, p2) };
        var u = (p2 - p1).Unitario();
        var n = u.Esquerda * ((extensao + largura) / 2);
        var c = (p1 + p2) / 2;
        r.Add(Poligonos.Retangulo(c + n, u, r.Comprimento, largura), Cor.Branco);
        r.Add(Poligonos.Retangulo(c - n, u, r.Comprimento, largura), Cor.Branco);
        return r;
    }

    /// <summary>
    /// LRV — linhas de estímulo à redução de velocidade. As linhas atravessam a faixa (P1→P2 na primeira linha)
    /// e se repetem no sentido do fluxo (à esquerda de P1→P2) com espaçamento decrescente: intervalos de tempo
    /// iguais numa desaceleração constante de v1 para v2 ao longo da extensão total.
    /// </summary>
    public static ResultadoPeca LRV(V2 p1, V2 p2, double v1, double v2, int numLinhas, double extensao, double largura, bool fluxoAEsquerda = true)
    {
        var r = new ResultadoPeca { Codigo = "LRV", Descricao = $"Linhas de redução de velocidade {Num.F0(v1)}→{Num.F0(v2)} km/h", Comprimento = extensao };
        if (numLinhas < 2) numLinhas = 2;
        var u = (p2 - p1).Unitario();
        var fluxo = fluxoAEsquerda ? u.Esquerda : -u.Esquerda;
        var c0 = (p1 + p2) / 2;
        double larg = V2.Dist(p1, p2);
        foreach (var s in PosicoesLRV(v1, v2, numLinhas, extensao))
            r.Add(Poligonos.Retangulo(c0 + fluxo * s, u, larg, largura), Cor.Branco);
        return r;
    }

    public static List<double> PosicoesLRV(double v1kmh, double v2kmh, int n, double extensao)
    {
        double a1 = v1kmh / 3.6, a2 = v2kmh / 3.6;
        var l = new List<double>();
        if (Math.Abs(a1 - a2) < 1e-6) { for (int k = 0; k < n; k++) l.Add(extensao * k / (n - 1)); return l; }
        double dec = (a1 * a1 - a2 * a2) / (2 * extensao);
        double T = (a1 - a2) / dec;
        for (int k = 0; k < n; k++)
        {
            double t = T * k / (n - 1);
            l.Add(a1 * t - dec * t * t / 2);
        }
        return l;
    }

    /// <summary>
    /// ZPA — zebrado de preenchimento de área não utilizável: listras inclinadas (45° em relação ao fluxo),
    /// recortadas pelo contorno, e linha de contorno opcional.
    /// </summary>
    public static ResultadoPeca ZPA(IList<V2> contorno, double anguloFluxo, double larguraListra, double espaco, double larguraContorno, Cor cor, double inclinacaoGraus = 45)
    {
        var r = new ResultadoPeca { Codigo = "ZPA", Descricao = "Zebrado de área não utilizável", Cor = cor };
        var poly = Poligonos.Limpar(contorno);
        if (poly.Count < 3) return r;
        if (Poligonos.AreaComSinal(poly) < 0) poly.Reverse();
        double ang = anguloFluxo + Num.Rad(inclinacaoGraus);
        var dir = V2.Polar(ang);           // direção das listras
        var nrm = dir.Esquerda;            // direção de repetição
        // extensão do polígono projetada na normal
        double min = double.MaxValue, max = double.MinValue, tmin = double.MaxValue, tmax = double.MinValue;
        foreach (var v in poly)
        {
            double a = V2.Dot(v, nrm), b = V2.Dot(v, dir);
            min = Math.Min(min, a); max = Math.Max(max, a); tmin = Math.Min(tmin, b); tmax = Math.Max(tmax, b);
        }
        double comp = (tmax - tmin) + 2;
        double passo = larguraListra + espaco;
        for (double a = min + espaco / 2 + larguraListra / 2; a < max; a += passo)
        {
            var centro = nrm * a + dir * ((tmin + tmax) / 2);
            var faixa = Poligonos.Retangulo(centro, dir, comp, larguraListra);
            var rec = Poligonos.RecortarPorConvexo(poly, faixa);
            if (rec.Count >= 3 && Poligonos.Area(rec) > 1e-4) r.Add(rec, cor);
        }
        if (larguraContorno > 0)
        {
            // contorno como faixa ao longo dos lados, para dentro do polígono (anti-horário: interior à esquerda)
            var cam = Caminho.DePontos(poly.Append(poly[0]));
            r.Add(cam.Faixa(0, cam.Comprimento, larguraContorno / 2, larguraContorno), cor);
            r.Comprimento = cam.Comprimento;
        }
        return r;
    }

    /// <summary>MCC — marcação de cruzamento rodocicloviário: duas fileiras de quadrados ao longo de P1→P2.</summary>
    public static ResultadoPeca MCC(V2 p1, V2 p2, double larguraCiclofaixa, double lado)
    {
        var r = new ResultadoPeca { Codigo = "MCC", Descricao = "Marcação de cruzamento rodocicloviário", Comprimento = V2.Dist(p1, p2) };
        var u = (p2 - p1).Unitario();
        var n = u.Esquerda;
        foreach (double off in new[] { (larguraCiclofaixa - lado) / 2, -(larguraCiclofaixa - lado) / 2 })
            for (double s = lado / 2; s < r.Comprimento - lado / 2 + 1e-6; s += 2 * lado)
                r.Add(Poligonos.Retangulo(p1 + u * s + n * off, u, lado, lado), Cor.Branco);
        return r;
    }
}
