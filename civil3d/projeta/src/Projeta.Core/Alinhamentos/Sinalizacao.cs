using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Sinalização horizontal e vertical da interseção (PRJINTERSECAO) e da rotatória (PRJROTATORIA) — PRJSINALIZA.
// Medidas do Manual Brasileiro de Sinalização de Trânsito (CONTRAN, Resolução 973/2022): Volume IV (horizontal) e
// Volume I (vertical de regulamentação). O que não vem do manual está marcado [PREMISSA] ou [A CONFIRMAR].
// ======================================================================================================================

/// <summary>Braço de uma interseção do PRJINTERSECAO, guardado no desenho para o PRJSINALIZA.</summary>
public sealed class BracoIntersecaoSinal
{
    public string Nome { get; set; } = "";
    public string Rumo { get; set; } = "";
    /// <summary>Via principal (preferencial); as outras param no PARE.</summary>
    public bool Principal { get; set; }
    /// <summary>Eixo do braço, do centro da interseção para fora: pontos [x, y].</summary>
    public List<double[]> Eixo { get; set; } = new();
    public double MeiaLargura { get; set; }
    /// <summary>Alargamento total da faixa de conversão (metade de cada lado do eixo); 0 = sem alargamento.</summary>
    public double Alargamento { get; set; }
    public double DPlena { get; set; }
    public double Teiper { get; set; }
    /// <summary>Esta aproximação tem a faixa de conversão à esquerda (centrada no eixo).</summary>
    public bool Conversao { get; set; }
    /// <summary>Distância do centro onde começa o armazenamento da faixa de conversão.</summary>
    public double DRetencaoConversao { get; set; }

    public double MeiaLarguraEm(double d) => Alargamento > 0 ? GeometriaIntersecao.MeiaLarguraAlargada(MeiaLargura, Alargamento, DPlena, Teiper, d) : MeiaLargura;
    public PolilinhaEixo Polilinha() => new(Eixo.Select(p => (p[0], p[1])).ToList());
}

/// <summary>Interseção guardada no desenho (centro e braços em ordem anti-horária).</summary>
public sealed class ModeloIntersecaoSinal
{
    public double Ox { get; set; }
    public double Oy { get; set; }
    public List<BracoIntersecaoSinal> Bracos { get; set; } = new();
}

/// <summary>Braço da rotatória guardado no desenho (cópia serializável do <see cref="BracoRotatoria"/>).</summary>
public sealed class BracoRotatoriaSinal
{
    public string Nome { get; set; } = "";
    public string Rumo { get; set; } = "";
    public double Angulo { get; set; }
    public List<double[]> Eixo { get; set; } = new();
    public double SRetencao { get; set; }
    public double BordoEntrada { get; set; }
    public double BordoSaida { get; set; }
    public double BordoNormal { get; set; }
    public double SAlargIni { get; set; } = double.PositiveInfinity;
    public double SAlargFim { get; set; } = double.PositiveInfinity;
    public double[]? MeioFioEntrada { get; set; }
    public double[]? MeioFioSaida { get; set; }
    public double STangEntrada { get; set; } = double.NaN;
    public double STangSaida { get; set; } = double.NaN;
    public double SIlhaIni { get; set; }
    public double SIlhaFim { get; set; }
    public double IlhaLarguraIni { get; set; }
    public double IlhaLarguraFim { get; set; }
}

/// <summary>Rotatória guardada no desenho.</summary>
public sealed class ModeloRotatoriaSinal
{
    public double Ox { get; set; }
    public double Oy { get; set; }
    public double RExt { get; set; }
    public double RIlha { get; set; }
    public double RecuoIlha { get; set; } = 1;
    public double LarguraAnel { get; set; }
    public double Apron { get; set; }
    public List<BracoRotatoriaSinal> Bracos { get; set; } = new();

    public static ModeloRotatoriaSinal De(ModeloRotatoria m, double larguraAnel, double apron)
    {
        var r = new ModeloRotatoriaSinal { Ox = m.Ox, Oy = m.Oy, RExt = m.RExt, RIlha = m.RIlha, RecuoIlha = m.RecuoIlha, LarguraAnel = larguraAnel, Apron = apron };
        foreach (var b in m.Bracos)
            r.Bracos.Add(new BracoRotatoriaSinal
            {
                Nome = b.Nome, Rumo = b.Rumo, Angulo = b.Angulo, Eixo = b.Eixo.Pontos.Select(p => new[] { p.X, p.Y }).ToList(),
                SRetencao = b.SRetencao, BordoEntrada = b.BordoEntrada, BordoSaida = b.BordoSaida, BordoNormal = b.BordoNormal,
                SAlargIni = b.SAlargIni, SAlargFim = b.SAlargFim,
                MeioFioEntrada = b.MeioFioEntrada is { } e ? new[] { e.X, e.Y, e.R } : null,
                MeioFioSaida = b.MeioFioSaida is { } s ? new[] { s.X, s.Y, s.R } : null,
                STangEntrada = b.STangEntrada, STangSaida = b.STangSaida, SIlhaIni = b.SIlhaIni, SIlhaFim = b.SIlhaFim,
                IlhaLarguraIni = b.IlhaLarguraIni, IlhaLarguraFim = b.IlhaLarguraFim,
            });
        return r;
    }

    public ModeloRotatoria Modelo()
    {
        var m = new ModeloRotatoria { Ox = Ox, Oy = Oy, RExt = RExt, RIlha = RIlha, RecuoIlha = RecuoIlha };
        foreach (var b in Bracos)
            m.Bracos.Add(new BracoRotatoria
            {
                Nome = b.Nome, Rumo = b.Rumo, Angulo = b.Angulo, Eixo = new PolilinhaEixo(b.Eixo.Select(p => (p[0], p[1])).ToList()),
                SRetencao = b.SRetencao, BordoEntrada = b.BordoEntrada, BordoSaida = b.BordoSaida, BordoNormal = b.BordoNormal,
                SAlargIni = b.SAlargIni, SAlargFim = b.SAlargFim,
                MeioFioEntrada = b.MeioFioEntrada is { Length: 3 } e ? (e[0], e[1], e[2]) : null,
                MeioFioSaida = b.MeioFioSaida is { Length: 3 } s ? (s[0], s[1], s[2]) : null,
                STangEntrada = b.STangEntrada, STangSaida = b.STangSaida, SIlhaIni = b.SIlhaIni, SIlhaFim = b.SIlhaFim,
                IlhaLarguraIni = b.IlhaLarguraIni, IlhaLarguraFim = b.IlhaLarguraFim,
            });
        return m;
    }
}

/// <summary>O que fica guardado no desenho para cada interseção ou rotatória: a geometria e o que a sinalização desenhou.</summary>
public sealed class ItemSinalizacao
{
    public string Nome { get; set; } = "";
    /// <summary>"Interseção" ou "Rotatória".</summary>
    public string Tipo { get; set; } = "";
    public ModeloIntersecaoSinal? Intersecao { get; set; }
    public ModeloRotatoriaSinal? Rotatoria { get; set; }
    /// <summary>Handles (hexadecimal) dos objetos que o PRJSINALIZA desenhou para este item.</summary>
    public List<string> Desenhados { get; set; } = new();
    public DateTime Gravado { get; set; } = DateTime.Now;
}

public enum TipoViaSinal { Urbana, Estrada, Rodovia }

/// <summary>Parâmetros da sinalização (lembrados entre as sessões).</summary>
public sealed class ParametrosSinalizacao
{
    public const string MbstI = "CONTRAN, MBST Vol. I – Sinalização Vertical de Regulamentação (2022, Res. 973/2022)";
    public const string MbstIV = "CONTRAN, MBST Vol. IV – Sinalização Horizontal (2022, Res. 973/2022)";

    public TipoViaSinal TipoVia { get; set; } = TipoViaSinal.Urbana;
    /// <summary>Velocidade regulamentada das aproximações (km/h).</summary>
    public double Velocidade { get; set; } = 40;
    /// <summary>Secundarias (só nas aproximações com PARE), Todas ou Nenhuma. Na rotatória: Nenhuma = sem faixas; as outras = em todos os ramos.</summary>
    public string FaixasPedestres { get; set; } = "Secundarias";
    public bool LegendaPare { get; set; } = true;
    public int FaixasEntrada { get; set; } = 1;
    public int FaixasAnel { get; set; } = 1;
    public double LarguraLre { get; set; } = 0.40;
    public double LarguraLdp { get; set; } = 0.30;
    public double TracoLdp { get; set; } = 0.50;
    public double LinhaFtp { get; set; } = 0.40;
    public double EspacoFtp { get; set; } = 0.60;
    public double ExtensaoFtp { get; set; } = 4.0;
    public double RecuoFtpRotatoria { get; set; } = 5.0;
    public double ComprimentoLfo { get; set; } = 30;
    /// <summary>Tamanho do símbolo das placas no desenho (m); o poste e a placa em planta ficam no tamanho real.</summary>
    public double EscalaSimbolo { get; set; } = 1.5;
    public bool ParametrosDoUsuario { get; set; }

    /// <summary>Faixa de pedestres a 1,00 m do alinhamento da pista transversal; LRE a 1,60 m da faixa (ou 1,00 m do prolongamento do
    /// meio-fio, sem faixa); LDP a 1,60 m do meio-fio da pista transversal; legenda PARE a 1,60 m antes da LRE.</summary>
    public const double DistFtpTransversal = 1.00, DistLreFtp = 1.60, DistLreSemFtp = 1.00, DistLdp = 1.60, DistLegenda = 1.60;

    public bool Rural => TipoVia != TipoViaSinal.Urbana;
    /// <summary>LFO, LMS e LBO: 0,10 m abaixo de 80 km/h, 0,15 m a partir de 80 km/h (MBST IV, 4.1 a 4.3).</summary>
    public double LarguraLinha => Velocidade >= 80 ? 0.15 : 0.10;
    /// <summary>Altura das letras da legenda (MBST IV, 8.3).</summary>
    public double AlturaLegenda => !Rural ? (Velocidade > 80 ? 2.40 : 1.60) : (Velocidade > 60 ? 4.00 : 2.40);
    /// <summary>Afastamento lateral da placa ao bordo da pista (MBST I, 3.13).</summary>
    public double AfastamentoPlaca => Rural ? 1.20 : 0.30;
    /// <summary>Distância máxima (urbana) ou faixa (rural) da placa ao prolongamento do meio-fio da via transversal (MBST I, 4.1).</summary>
    public double DistMaxPlaca => Rural ? 15 : 10;
    public double DistMinPlaca => Rural ? 1.5 : 0;
    /// <summary>Lado recomendado do R-1 (MBST I, 3.7).</summary>
    public double LadoR1 => TipoVia == TipoViaSinal.Rodovia ? 0.50 : 0.35;
    /// <summary>Lado recomendado do R-2 (MBST I, 3.7).</summary>
    public double LadoR2 => TipoVia == TipoViaSinal.Rodovia ? 1.00 : 0.90;
    /// <summary>Diâmetro recomendado dos sinais circulares (MBST I, 3.7).</summary>
    public double DiametroCircular => TipoVia == TipoViaSinal.Rodovia ? 1.00 : TipoVia == TipoViaSinal.Estrada ? 0.75 : 0.50;
    /// <summary>Comprimento da seta (MBST IV, 8.1.1).</summary>
    public double ComprimentoSeta => (!Rural && Velocidade <= 80) || (Rural && Velocidade < 60) ? 5.00 : 7.50;
    /// <summary>Espaçamento das setas d / d1 / d2 a partir do ponto de saída da faixa (MBST IV, 8.1.1).</summary>
    public (double D, double D1, double D2) EspacamentoSetas => !Rural
        ? (Velocidade < 60 ? (10, 30, 45) : Velocidade <= 80 ? (15, 40, 60) : (15, 50, 75))
        : (Velocidade < 60 ? (30, 30, 45) : Velocidade <= 80 ? (40, 40, 60) : (50, 50, 75));
    /// <summary>Cadência das linhas seccionadas (traço / espaço), a primeira da tabela da faixa de velocidade (MBST IV, 4.1.2 e 4.2.2).</summary>
    public (double Traco, double Espaco) CadenciaSeccionada => Velocidade < 60 ? (2, 4) : Velocidade < 80 ? (3, 6) : (3, 9);
}

/// <summary>Marca no pavimento: polígono pintado (para desenhar e somar a área).</summary>
public sealed record MarcaViaria(string Codigo, string Nome, string Cor, List<(double X, double Y)> Contorno, string Onde)
{
    public double Area => GeoSinal.Area(Contorno);
}

/// <summary>Placa: posição do poste e direção do tráfego que ela atende (rad); a face fica voltada para quem chega.</summary>
public sealed record PlacaSinal(string Codigo, string Nome, string Forma, double Tamanho, double X, double Y, double Angulo, string Onde, string Fonte)
{
    /// <summary>Largura da placa em planta (octógono: lado·(1+√2); triângulo: lado; círculo: diâmetro).</summary>
    public double Largura => Forma == "octogono" ? Tamanho * (1 + Math.Sqrt(2)) : Tamanho;
}

/// <summary>Legenda no pavimento: centro, direção do tráfego (o "para cima" das letras) e altura das letras.</summary>
public sealed record LegendaSinal(string Texto, double X, double Y, double Angulo, double Altura, string Onde);

/// <summary>Sinalização gerada para uma interseção ou rotatória.</summary>
public sealed class ResultadoSinalizacao
{
    public string Item { get; set; } = "";
    public string Tipo { get; set; } = "";
    public List<MarcaViaria> Marcas { get; } = new();
    public List<PlacaSinal> Placas { get; } = new();
    public List<LegendaSinal> Legendas { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<string> Avisos { get; } = new();
}

/// <summary>Contas de geometria plana da sinalização.</summary>
public static class GeoSinal
{
    public static double Area(IReadOnlyList<(double X, double Y)> p)
    {
        double a = 0;
        for (int i = 0; i < p.Count; i++)
        {
            var q = p[i];
            var r = p[(i + 1) % p.Count];
            a += q.X * r.Y - r.X * q.Y;
        }
        return Math.Abs(a) / 2;
    }

    /// <summary>Faixa longitudinal (linha ao longo do eixo) de s0 a s1, centrada no afastamento v(s), com largura l.</summary>
    public static List<(double X, double Y)> Longitudinal(PolilinhaEixo e, double s0, double s1, Func<double, double> v, double l, double passo = 1)
    {
        var esq = new List<(double X, double Y)>();
        var dir = new List<(double X, double Y)>();
        int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(s1 - s0) / passo));
        for (int k = 0; k <= n; k++)
        {
            double s = s0 + (s1 - s0) * k / n;
            esq.Add(e.Ponto(s, v(s) + l / 2));
            dir.Add(e.Ponto(s, v(s) - l / 2));
        }
        dir.Reverse();
        esq.AddRange(dir);
        return esq;
    }

    /// <summary>Quadrilátero entre as estacas s0 e s1 e os afastamentos v0 e v1 (linha transversal ou listra da faixa de pedestres).</summary>
    public static List<(double X, double Y)> Retangulo(PolilinhaEixo e, double s0, double s1, double v0, double v1) =>
        new() { e.Ponto(s0, v0), e.Ponto(s0, v1), e.Ponto(s1, v1), e.Ponto(s1, v0) };

    /// <summary>Traços de uma linha seccionada ao longo de um comprimento (de, até).</summary>
    public static IEnumerable<(double De, double Ate)> Tracos(double comprimento, double traco, double espaco)
    {
        if (traco <= 0) yield break;
        for (double a = 0; a < comprimento - 1e-9; a += traco + Math.Max(0, espaco))
            yield return (a, Math.Min(comprimento, a + traco));
    }

    /// <summary>Traço em arco: coroa entre os ângulos a0 e a1, no raio r, com largura l.</summary>
    public static List<(double X, double Y)> Arco(double ox, double oy, double r, double a0, double a1, double l)
    {
        int n = Math.Max(2, (int)Math.Ceiling(Math.Abs(a1 - a0) * r / 0.25));
        var fora = new List<(double X, double Y)>();
        var dentro = new List<(double X, double Y)>();
        for (int k = 0; k <= n; k++)
        {
            double a = a0 + (a1 - a0) * k / n;
            fora.Add((ox + (r + l / 2) * Math.Cos(a), oy + (r + l / 2) * Math.Sin(a)));
            dentro.Add((ox + (r - l / 2) * Math.Cos(a), oy + (r - l / 2) * Math.Sin(a)));
        }
        dentro.Reverse();
        fora.AddRange(dentro);
        return fora;
    }

    /// <summary>Raiz de f entre a e b (f(a) &lt; 0 &lt; f(b)); NaN se não troca de sinal.</summary>
    public static double Bissecao(Func<double, double> f, double a, double b)
    {
        double fa = f(a), fb = f(b);
        if (double.IsNaN(fa) || double.IsNaN(fb) || fa > 0 || fb < 0) return double.NaN;
        for (int i = 0; i < 80; i++)
        {
            double m = (a + b) / 2;
            if (f(m) < 0) a = m; else b = m;
        }
        return (a + b) / 2;
    }

    /// <summary>Onde a polilinha a cruza a polilinha b: distância ao longo de a (a partir do primeiro ponto).</summary>
    public static List<double> Cruzamentos(IReadOnlyList<(double X, double Y)> a, IReadOnlyList<(double X, double Y)> b)
    {
        var r = new List<double>();
        double acum = 0;
        for (int i = 0; i + 1 < a.Count; i++)
        {
            var p = a[i];
            var q = a[i + 1];
            double dx = q.X - p.X, dy = q.Y - p.Y, len = Math.Sqrt(dx * dx + dy * dy);
            for (int j = 0; j + 1 < b.Count; j++)
            {
                var c = b[j];
                var d = b[j + 1];
                double ex = d.X - c.X, ey = d.Y - c.Y;
                double den = dx * ey - dy * ex;
                if (Math.Abs(den) < 1e-12) continue;
                double t = ((c.X - p.X) * ey - (c.Y - p.Y) * ex) / den;
                double u = ((c.X - p.X) * dy - (c.Y - p.Y) * dx) / den;
                if (t >= -1e-9 && t <= 1 + 1e-9 && u >= -1e-9 && u <= 1 + 1e-9) r.Add(acum + t * len);
            }
            acum += len;
        }
        return r;
    }
}

/// <summary>Gera a sinalização (marcas, legendas, placas e verificações) a partir da geometria guardada.</summary>
public static class GeradorSinalizacao
{
    private const string Branca = "branca", Amarela = "amarela";

    // ---------------------------------------------------------------- interseção

    /// <summary>
    /// Interseção: nas aproximações com PARE, faixa de pedestres (opcional) a 1,00 m do alinhamento da pista transversal,
    /// linha de retenção a 1,60 m dela (ou a 1,00 m do prolongamento do meio-fio, sem faixa), legenda PARE a 1,60 m antes
    /// da retenção, R-1 no lado direito e linha contínua amarela no eixo; na principal, faixa de pedestres (opção Todas),
    /// eixo contínuo e, com faixa de conversão à esquerda, as linhas da faixa e as setas.
    /// </summary>
    public static ResultadoSinalizacao Intersecao(string nome, ModeloIntersecaoSinal m, ParametrosSinalizacao p)
    {
        var r = new ResultadoSinalizacao { Item = nome, Tipo = "Interseção" };
        var eixos = m.Bracos.Select(b => b.Polilinha()).ToList();
        double lw = p.LarguraLinha;
        for (int i = 0; i < m.Bracos.Count; i++)
        {
            var b = m.Bracos[i];
            var e = eixos[i];
            string onde = $"braço {b.Rumo} ({b.Nome})";
            double dT = DistanciaTransversal(m, eixos, i, out bool achou);
            if (!achou) r.Avisos.Add($"{nome}, {onde}: não achei o bordo da via transversal; usei a meia largura das outras vias ({dT:0.00} m).");
            bool pare = !b.Principal;
            bool ftp = p.FaixasPedestres == "Todas" || (p.FaixasPedestres == "Secundarias" && pare);
            double fimFtp = dT;
            if (ftp)
            {
                double f0 = dT + ParametrosSinalizacao.DistFtpTransversal, f1 = f0 + p.ExtensaoFtp;
                double wl = Math.Min(b.MeiaLarguraEm(f0), b.MeiaLarguraEm(f1));
                FaixaPedestres(r, e, f0, f1, -wl, wl, p, onde);
                fimFtp = f1;
            }
            if (pare)
            {
                double l0 = ftp ? fimFtp + ParametrosSinalizacao.DistLreFtp : dT + ParametrosSinalizacao.DistLreSemFtp;
                double w0 = b.MeiaLarguraEm(l0);
                r.Marcas.Add(new MarcaViaria("LRE", "Linha de retenção", Branca, GeoSinal.Retangulo(e, l0, l0 + p.LarguraLre, 0, w0), onde));
                var (ux, uy) = e.Direcao(l0);
                double chegando = Math.Atan2(-uy, -ux);
                if (p.LegendaPare)
                {
                    double sL = l0 + p.LarguraLre + ParametrosSinalizacao.DistLegenda + p.AlturaLegenda / 2;
                    var pt = e.Ponto(sL, w0 / 2);
                    var (lx, ly) = e.Direcao(sL);
                    r.Legendas.Add(new LegendaSinal("PARE", pt.X, pt.Y, Math.Atan2(-ly, -lx), p.AlturaLegenda, onde));
                    if (w0 < 2.8)
                        r.Verificacoes.Add(new Verificacao("Legenda PARE na faixa", $"{nome}, {onde}", $"faixa de {w0:0.00} m", "faixa com largura para a legenda centralizada",
                            $"{ParametrosSinalizacao.MbstIV}, 8.3", VerificacaoRotatoria.Atencao));
                }
                var placa = new PlacaSinal("R-1", "Parada obrigatória", "octogono", p.LadoR1, 0, 0, chegando, onde, $"{ParametrosSinalizacao.MbstI}, 3.7 e 4.1");
                var pp = e.Ponto(l0 + p.LarguraLre / 2, w0 + p.AfastamentoPlaca + placa.Largura / 2);
                r.Placas.Add(placa with { X = pp.X, Y = pp.Y });
                double dist = l0 + p.LarguraLre / 2 - dT;
                bool ok = dist <= p.DistMaxPlaca + 1e-9 && dist >= p.DistMinPlaca - 1e-9;
                r.Verificacoes.Add(new Verificacao("Posição do R-1 (parada obrigatória)", $"{nome}, {onde}", $"{dist:0.00} m do prolongamento do meio-fio da via transversal",
                    p.Rural ? "entre 1,5 e 15,0 m" : "até 10,0 m", $"{ParametrosSinalizacao.MbstI}, 4.1", ok ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                r.Marcas.Add(new MarcaViaria("LFO-1", "Linha simples contínua (fluxos opostos)", Amarela,
                    GeoSinal.Longitudinal(e, l0, l0 + p.ComprimentoLfo, _ => 0, lw), onde));
            }
            else if (b.Conversao && b.Alargamento > 0)
            {
                double c(double d) => GeometriaIntersecao.MeiaLarguraAlargada(0, b.Alargamento, b.DPlena, b.Teiper, d);
                double l0 = Math.Max(b.DRetencaoConversao, ftp ? fimFtp + ParametrosSinalizacao.DistLreFtp : dT + ParametrosSinalizacao.DistLreSemFtp);
                double fimT = b.DPlena + b.Teiper;
                if (b.DPlena > l0)
                    r.Marcas.Add(new MarcaViaria("LMS-1", "Linha simples contínua (mesmo sentido) – faixa de conversão", Branca,
                        GeoSinal.Longitudinal(e, l0, b.DPlena, d => c(d), lw), onde));
                if (fimT > l0)
                    r.Marcas.Add(new MarcaViaria("LFO-1", "Linha simples contínua (fluxos opostos)", Amarela,
                        GeoSinal.Longitudinal(e, l0, fimT, d => -c(d), lw), onde));
                r.Marcas.Add(new MarcaViaria("LFO-1", "Linha simples contínua (fluxos opostos)", Amarela,
                    GeoSinal.Longitudinal(e, Math.Max(l0, fimT), Math.Max(l0, fimT) + p.ComprimentoLfo, _ => 0, lw), onde));
                var (d0, d1, d2) = p.EspacamentoSetas;
                double L = p.ComprimentoSeta;
                int setas = 0;
                foreach (double dd in new[] { d0, d1, d2 }.Distinct())
                {
                    double sFrente = l0 + dd;
                    if (sFrente + 0.70 * L > b.DPlena + 1e-9) break;
                    foreach (var pol in SetaEsquerda(e, sFrente, 0, L))
                        r.Marcas.Add(new MarcaViaria("Seta", $"Seta de conversão à esquerda ({L:0.00} m)", Branca, pol, onde));
                    setas++;
                }
                if (setas == 0)
                    r.Avisos.Add($"{nome}, {onde}: a faixa de conversão é curta para a seta (a primeira fica a {d0:0} m da retenção e tem {L:0.00} m).");
                r.Verificacoes.Add(new Verificacao("Setas na faixa de conversão", $"{nome}, {onde}", $"{setas} seta(s) de {L:0.00} m",
                    $"a {d0:0} / {d1:0} / {d2:0} m do ponto de saída da faixa", $"{ParametrosSinalizacao.MbstIV}, 8.1.1 (desenho da seta simplificado [A CONFIRMAR na figura])",
                    setas > 0 ? VerificacaoRotatoria.Informativo : VerificacaoRotatoria.Atencao));
            }
            else
            {
                double l0 = ftp ? fimFtp + ParametrosSinalizacao.DistFtpTransversal : dT + ParametrosSinalizacao.DistFtpTransversal;
                r.Marcas.Add(new MarcaViaria("LFO-1", "Linha simples contínua (fluxos opostos)", Amarela,
                    GeoSinal.Longitudinal(e, l0, l0 + p.ComprimentoLfo, _ => 0, lw), onde));
            }
            if (ftp)
                r.Verificacoes.Add(new Verificacao("Faixa de pedestres (FTP-1)", $"{nome}, {onde}",
                    $"a {ParametrosSinalizacao.DistFtpTransversal:0.00} m do alinhamento da pista transversal, extensão {p.ExtensaoFtp:0.00} m",
                    "≥ 1,00 m do alinhamento; extensão ≥ 3,00 m (recomendada 4,00 m)", $"{ParametrosSinalizacao.MbstIV}, 5.4",
                    p.ExtensaoFtp >= 3 - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
        }
        return r;
    }

    /// <summary>
    /// Distância (no eixo do braço, a partir do centro) até o alinhamento do meio-fio da pista transversal: o ponto mais longe
    /// onde linhas paralelas ao eixo, dentro da pista do braço, cortam os bordos das outras vias.
    /// </summary>
    public static double DistanciaTransversal(ModeloIntersecaoSinal m, IReadOnlyList<PolilinhaEixo> eixos, int i, out bool achou)
    {
        var bi = m.Bracos[i];
        var ei = eixos[i];
        double wi = bi.MeiaLarguraEm(0);
        const double alcance = 40, passo = 0.5;
        int n = (int)(alcance / passo);
        double melhor = 0;
        achou = false;
        foreach (double f in new[] { 0.95, 0.5, -0.5, -0.95 })
        {
            var linha = new List<(double X, double Y)>();
            for (int k = 0; k <= n; k++) linha.Add(ei.Ponto(k * passo, f * wi));
            for (int j = 0; j < m.Bracos.Count; j++)
            {
                var bj = m.Bracos[j];
                if (j == i || bj.Nome == bi.Nome) continue;
                var ej = eixos[j];
                foreach (int lado in new[] { 1, -1 })
                {
                    var bordo = new List<(double X, double Y)>();
                    for (int k = 0; k <= n; k++) { double d = k * passo; bordo.Add(ej.Ponto(d, lado * bj.MeiaLarguraEm(d))); }
                    foreach (double d in GeoSinal.Cruzamentos(linha, bordo)) { melhor = Math.Max(melhor, d); achou = true; }
                }
            }
        }
        if (!achou)
            melhor = m.Bracos.Where((b, j) => j != i && b.Nome != bi.Nome).Select(b => b.MeiaLarguraEm(0)).DefaultIfEmpty(0).Max();
        return melhor;
    }

    /// <summary>Faixa de pedestres zebrada (FTP-1): listras paralelas ao eixo, de s0 a s1, distribuídas entre os afastamentos vA e vB.</summary>
    private static void FaixaPedestres(ResultadoSinalizacao r, PolilinhaEixo e, double s0, double s1, double vA, double vB, ParametrosSinalizacao p, string onde)
    {
        double larg = vB - vA;
        if (larg < p.LinhaFtp) return;
        int n = (int)Math.Floor((larg + p.EspacoFtp) / (p.LinhaFtp + p.EspacoFtp) + 1e-9);
        double total = n * p.LinhaFtp + (n - 1) * p.EspacoFtp;
        double v = vA + (larg - total) / 2;
        for (int k = 0; k < n; k++)
        {
            r.Marcas.Add(new MarcaViaria("FTP-1", "Faixa de travessia de pedestres (zebrada)", Branca, GeoSinal.Retangulo(e, s0, s1, v, v + p.LinhaFtp), onde));
            v += p.LinhaFtp + p.EspacoFtp;
        }
    }

    /// <summary>
    /// Seta de conversão à esquerda (desenho simplificado [A CONFIRMAR na figura 8.1.1]): comprimento L, ponta c = 0,30·L,
    /// haste d = 0,70·L, ponta de 0,75 m de largura e haste de 0,15 m (valores b, c, d, e da tabela do MBST IV). A frente fica
    /// na estaca <paramref name="sFrente"/> (mais perto do centro), a seta aponta para quem chega.
    /// </summary>
    private static IEnumerable<List<(double X, double Y)>> SetaEsquerda(PolilinhaEixo e, double sFrente, double vCentro, double L)
    {
        double c = 0.30 * L, d = 0.70 * L, b = 0.75, w = 0.15, a = 0.60;
        double yShift = -(a + c) / 2;
        // local: x para a frente (estacas diminuindo), y para a esquerda de quem chega (afastamento negativo)
        (double X, double Y) P(double x, double y) => e.Ponto(sFrente + (d - x), vCentro - (y + yShift));
        yield return new() { P(0, -w / 2), P(d, -w / 2), P(d, w / 2), P(0, w / 2) };
        yield return new() { P(d - w, w / 2), P(d, w / 2), P(d, w / 2 + a), P(d - w, w / 2 + a) };
        double xc = d - w / 2;
        yield return new() { P(xc - b / 2, w / 2 + a), P(xc + b / 2, w / 2 + a), P(xc, w / 2 + a + c) };
    }

    // ---------------------------------------------------------------- rotatória

    /// <summary>
    /// Rotatória: em cada entrada, linha de "Dê a preferência" tracejada a 1,60 m do bordo externo do anel, R-2 no lado
    /// direito (e na ilha, com 2 faixas), sinal de sentido de circulação (R-33 na entrada se a ilha central tem raio &lt; 12 m;
    /// R-24a na ilha central, de frente para quem chega, se tem 12 m ou mais), faixa de pedestres nos dois lados da ilha
    /// divisória e linha contínua amarela no eixo depois do nariz da ilha; no anel de 2 faixas, linha seccionada entre elas.
    /// </summary>
    public static ResultadoSinalizacao Rotatoria(string nome, ModeloRotatoriaSinal ms, ParametrosSinalizacao p)
    {
        var r = new ResultadoSinalizacao { Item = nome, Tipo = "Rotatória" };
        var m = ms.Modelo();
        double ox = m.Ox, oy = m.Oy, lw = p.LarguraLinha;
        double Dist((double X, double Y) q) => Math.Sqrt((q.X - ox) * (q.X - ox) + (q.Y - oy) * (q.Y - oy));
        bool r24 = m.RIlha >= 12 - 1e-9;
        double rL = m.RExt + ParametrosSinalizacao.DistLdp + p.LarguraLdp / 2;
        bool ftp = p.FaixasPedestres != "Nenhuma";
        r.Verificacoes.Add(new Verificacao("Sinal de sentido de circulação", nome,
            r24 ? $"ilha central com raio {m.RIlha:0.00} m ≥ 12 m: R-24a na ilha, de frente para cada entrada" : $"ilha central com raio {m.RIlha:0.00} m < 12 m: R-33 em cada entrada",
            "R-24a em rotatória de raio ≥ 12 m; R-33 abaixo disso (ou demarcada por pintura)", $"{ParametrosSinalizacao.MbstI}, R-24a (p. 63-65) e R-33 (p. 68); raio tomado como o da ilha central [A CONFIRMAR]",
            VerificacaoRotatoria.Informativo));
        foreach (var b in m.Bracos)
        {
            var e = b.Eixo;
            string onde = $"braço {b.Rumo} ({b.Nome})";
            double sIl = GeoSinal.Bissecao(s => Dist(e.Ponto(s, b.MeiaIlha(s))) - rL, 0, 150);
            double sCu = GeoSinal.Bissecao(s => Dist(e.Ponto(s, BordaRamo(b, s, true))) - rL, 0, 150);
            if (double.IsNaN(sIl) || double.IsNaN(sCu)) { r.Avisos.Add($"{nome}, {onde}: não achei onde pôr a linha de dê a preferência (eixo do ramo curto?)."); continue; }
            var pIl = e.Ponto(sIl, b.MeiaIlha(sIl));
            double bordoCu = BordaRamo(b, sCu, true);
            var pCu = e.Ponto(sCu, bordoCu);
            double a0 = Math.Atan2(pIl.Y - oy, pIl.X - ox), a1 = Math.Atan2(pCu.Y - oy, pCu.X - ox);
            double delta = a1 - a0;
            while (delta > Math.PI) delta -= 2 * Math.PI;
            while (delta < -Math.PI) delta += 2 * Math.PI;
            double comp = Math.Abs(delta) * rL;
            foreach (var (de, ate) in GeoSinal.Tracos(comp, p.TracoLdp, p.TracoLdp))
                r.Marcas.Add(new MarcaViaria("LDP", "Linha de “Dê a preferência”", Branca,
                    GeoSinal.Arco(ox, oy, rL, a0 + delta * de / comp, a0 + delta * ate / comp, p.LarguraLdp), onde));
            r.Verificacoes.Add(new Verificacao("Linha de dê a preferência (LDP)", $"{nome}, {onde}",
                $"a {ParametrosSinalizacao.DistLdp:0.00} m do bordo externo do anel, {comp:0.00} m de linha, largura {p.LarguraLdp:0.00} m, traço e espaço de {p.TracoLdp:0.00} m",
                "≥ 1,60 m do meio-fio da pista transversal; largura 0,20 a 0,40 m; traço = espaço (0,50 m recomendado)", $"{ParametrosSinalizacao.MbstIV}, 5.3",
                p.LarguraLdp is >= 0.20 - 1e-9 and <= 0.40 + 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));

            var (dx, dy) = e.Direcao(sCu);
            double chegando = Math.Atan2(-dy, -dx);
            var r2 = new PlacaSinal("R-2", "Dê a preferência", "triangulo", p.LadoR2, 0, 0, chegando, onde, $"{ParametrosSinalizacao.MbstI}, 3.7 e 4.1");
            var q2 = e.Ponto(sCu, bordoCu + p.AfastamentoPlaca + r2.Largura / 2);
            r.Placas.Add(r2 with { X = q2.X, Y = q2.Y });
            double distR2 = Dist(pCu) - m.RExt;
            r.Verificacoes.Add(new Verificacao("Posição do R-2 (dê a preferência)", $"{nome}, {onde}", $"{distR2:0.00} m do bordo externo do anel, lado direito",
                p.Rural ? "entre 1,5 e 15,0 m" : "até 10,0 m", $"{ParametrosSinalizacao.MbstI}, 4.1",
                distR2 <= p.DistMaxPlaca + 1e-9 && distR2 >= p.DistMinPlaca - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
            if (p.FaixasEntrada >= 2)
            {
                double vI = b.MeiaIlha(sIl) - p.AfastamentoPlaca - r2.Largura / 2;
                if (b.TemIlha && vI > 0.05)
                {
                    var qi = e.Ponto(sIl, vI);
                    r.Placas.Add(r2 with { X = qi.X, Y = qi.Y, Onde = onde + " (na ilha divisória)" });
                }
                else r.Avisos.Add($"{nome}, {onde}: entrada de 2 faixas pede o R-2 dos dois lados, mas a ilha divisória é estreita para a placa.");
            }

            double dc = p.DiametroCircular;
            if (!r24)
            {
                double s3 = sCu + 3;
                var q3 = e.Ponto(s3, BordaRamo(b, s3, true) + p.AfastamentoPlaca + dc / 2);
                r.Placas.Add(new PlacaSinal("R-33", "Sentido de circulação na rotatória", "circulo", dc, q3.X, q3.Y, chegando, onde,
                    $"{ParametrosSinalizacao.MbstI}, p. 68; 3 m antes do R-2 [PREMISSA]"));
            }
            else
            {
                double rr = m.RIlha - 1.0 - dc / 2;
                r.Placas.Add(new PlacaSinal("R-24a", "Sentido de circulação da via/pista", "circulo", dc, ox + rr * Math.Cos(b.Angulo), oy + rr * Math.Sin(b.Angulo),
                    b.Angulo + Math.PI, onde + " (na ilha central)", $"{ParametrosSinalizacao.MbstI}, p. 63-65; a 1,0 m do meio-fio da ilha [PREMISSA]"));
            }

            double sFim = sCu + 1;
            if (ftp)
            {
                double rF = m.RExt + ParametrosSinalizacao.DistLdp + p.LarguraLdp + p.RecuoFtpRotatoria;
                double sF0 = GeoSinal.Bissecao(s => Dist(e.Ponto(s, 0)) - rF, 0, 150), sF1 = sF0 + p.ExtensaoFtp;
                if (!double.IsNaN(sF0))
                {
                    double ilha = Math.Max(b.MeiaIlha(sF0), b.MeiaIlha(sF1));
                    FaixaPedestres(r, e, sF0, sF1, ilha, Math.Min(BordaRamo(b, sF0, true), BordaRamo(b, sF1, true)), p, onde + " (entrada)");
                    FaixaPedestres(r, e, sF0, sF1, -Math.Min(BordaRamo(b, sF0, false), BordaRamo(b, sF1, false)), -ilha, p, onde + " (saída)");
                    sFim = sF1 + 1;
                    r.Verificacoes.Add(new Verificacao("Faixa de pedestres (FTP-1)", $"{nome}, {onde}",
                        $"{p.RecuoFtpRotatoria:0.00} m atrás da LDP, extensão {p.ExtensaoFtp:0.00} m" + (b.TemIlha ? "; atravessa a ilha divisória (refúgio)" : ""),
                        "extensão ≥ 3,00 m (recomendada 4,00 m)", $"{ParametrosSinalizacao.MbstIV}, 5.4; recuo atrás da LDP [PREMISSA]",
                        p.ExtensaoFtp >= 3 - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                }
            }
            double sLfo = b.TemIlha ? Math.Max(b.SIlhaFim, sFim) : sFim;
            r.Marcas.Add(new MarcaViaria("LFO-1", "Linha simples contínua (fluxos opostos)", Amarela,
                GeoSinal.Longitudinal(e, sLfo, sLfo + p.ComprimentoLfo, _ => 0, lw), onde));
        }
        if (p.FaixasAnel >= 2)
        {
            double ra = m.RExt - ms.LarguraAnel / 2;
            var (tr, es) = p.CadenciaSeccionada;
            double comp = 2 * Math.PI * ra;
            foreach (var (de, ate) in GeoSinal.Tracos(comp, tr, es))
                r.Marcas.Add(new MarcaViaria("LMS-2", "Linha simples seccionada (mesmo sentido) – entre as faixas do anel", Branca,
                    GeoSinal.Arco(ox, oy, ra, de / ra, ate / ra, lw), "anel"));
        }
        return r;
    }

    /// <summary>Distância do eixo do ramo ao meio-fio na estaca s: o arco da conexão (antes da tangência) ou o bordo do ramo.</summary>
    public static double BordaRamo(BracoRotatoria b, double s, bool entrada)
    {
        double padrao = b.Bordo(s, entrada);
        var arco = entrada ? b.MeioFioEntrada : b.MeioFioSaida;
        double sT = entrada ? b.STangEntrada : b.STangSaida;
        if (arco is not { } a || double.IsNaN(sT) || s >= sT) return padrao;
        var p0 = b.Eixo.Ponto(s, 0);
        var (dx, dy) = b.Eixo.Direcao(s);
        int lado = entrada ? 1 : -1;
        double nx = -dy * lado, ny = dx * lado;
        double qx = p0.X - a.X, qy = p0.Y - a.Y;
        double bq = qx * nx + qy * ny, cq = qx * qx + qy * qy - a.R * a.R;
        double disc = bq * bq - cq;
        if (disc < 0) return padrao;
        double t1 = -bq - Math.Sqrt(disc), t2 = -bq + Math.Sqrt(disc);
        double t = t1 > 0 ? t1 : t2 > 0 ? t2 : double.NaN;
        return double.IsNaN(t) ? padrao : t;
    }
}

/// <summary>Linha do quantitativo da sinalização.</summary>
public sealed record LinhaQuantitativo(string Grupo, string Codigo, string Descricao, string Cor, string Unidade, double Quantidade, int Pecas);

/// <summary>Planilha do PRJSINALIZA: resumo, quantitativo, placas, parâmetros, verificações e pendências.</summary>
public sealed class RelatorioSinalizacao
{
    public string Titulo { get; set; } = "Sinalização";
    public string Desenho { get; set; } = "";
    public List<ResultadoSinalizacao> Itens { get; } = new();
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<string> Avisos { get; } = new();
    public List<string> Pendencias { get; } = new();

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";

    public static string Dimensao(string forma, double tamanho) => forma switch
    {
        "octogono" => $"octógono, lado {tamanho:0.00} m",
        "triangulo" => $"triângulo, lado {tamanho:0.00} m",
        "circulo" => $"círculo, Ø {tamanho:0.00} m",
        _ => $"{tamanho:0.00} m",
    };

    public static List<LinhaQuantitativo> Quantitativo(IEnumerable<ResultadoSinalizacao> itens)
    {
        var todos = itens.ToList();
        var l = new List<LinhaQuantitativo>();
        foreach (var g in todos.SelectMany(r => r.Marcas).GroupBy(x => (x.Codigo, x.Nome, x.Cor)).OrderBy(g => g.Key.Cor).ThenBy(g => g.Key.Codigo))
            l.Add(new LinhaQuantitativo("Horizontal", g.Key.Codigo, g.Key.Nome, g.Key.Cor, "m²", g.Sum(x => x.Area), g.Count()));
        foreach (var g in todos.SelectMany(r => r.Legendas).GroupBy(x => (x.Texto, x.Altura)))
            l.Add(new LinhaQuantitativo("Horizontal", "Legenda", $"Legenda \"{g.Key.Texto}\" (letras de {g.Key.Altura:0.00} m)", "branca", "un", g.Count(), g.Count()));
        foreach (var g in todos.SelectMany(r => r.Placas).GroupBy(x => (x.Codigo, x.Nome, x.Forma, x.Tamanho)).OrderBy(g => g.Key.Codigo))
            l.Add(new LinhaQuantitativo("Vertical", g.Key.Codigo, $"{g.Key.Nome} – {Dimensao(g.Key.Forma, g.Key.Tamanho)}", "", "un", g.Count(), g.Count()));
        int postes = todos.Sum(r => r.Placas.Count);
        if (postes > 0) l.Add(new LinhaQuantitativo("Vertical", "Suporte", "Suporte (poste) de placa, um por placa [PREMISSA]", "", "un", postes, postes));
        return l;
    }

    public static List<ParametroRotatoria> ParametrosDe(ParametrosSinalizacao p)
    {
        string pre = p.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA]";
        string iv = ParametrosSinalizacao.MbstIV, i = ParametrosSinalizacao.MbstI;
        return new List<ParametroRotatoria>
        {
            new("Tipo de via", p.TipoVia.ToString(), "", pre),
            new("Velocidade regulamentada das aproximações", $"{p.Velocidade:0}", "km/h", pre),
            new("Linha de retenção (LRE): largura", $"{p.LarguraLre:0.00}", "m", $"{iv}, 5.1 (0,30 a 0,60 m); valor adotado {pre}"),
            new("LRE: distância à faixa de pedestres / sem faixa", $"{ParametrosSinalizacao.DistLreFtp:0.00} / {ParametrosSinalizacao.DistLreSemFtp:0.00}", "m", $"{iv}, 5.1 (mínimos)"),
            new("Linha de dê a preferência (LDP): largura", $"{p.LarguraLdp:0.00}", "m", $"{iv}, 5.3 (0,20 a 0,40 m); valor adotado {pre}"),
            new("LDP: traço = espaço", $"{p.TracoLdp:0.00}", "m", $"{iv}, 5.3 (recomendado 0,50 m)"),
            new("LDP: distância ao meio-fio da pista transversal", $"{ParametrosSinalizacao.DistLdp:0.00}", "m", $"{iv}, 5.3 (mínimo)"),
            new("Faixa de pedestres FTP-1: largura das linhas / espaço", $"{p.LinhaFtp:0.00} / {p.EspacoFtp:0.00}", "m", $"{iv}, 5.4 (0,30 a 0,40 / 0,30 a 0,80 m); valores adotados {pre}"),
            new("FTP-1: extensão", $"{p.ExtensaoFtp:0.00}", "m", $"{iv}, 5.4 (mínimo 3,00, recomendado 4,00 m)"),
            new("FTP: distância ao alinhamento da pista transversal", $"{ParametrosSinalizacao.DistFtpTransversal:0.00}", "m", $"{iv}, 5.4 (mínimo)"),
            new("FTP na rotatória: recuo atrás da LDP", $"{p.RecuoFtpRotatoria:0.00}", "m", "[PREMISSA] um veículo esperando entre a faixa e a LDP"),
            new("Faixas de pedestres", p.FaixasPedestres, "", pre),
            new("Linhas LFO / LMS: largura", $"{p.LarguraLinha:0.00}", "m", $"{iv}, 4.1 e 4.2 (0,10 m abaixo de 80 km/h; 0,15 m a partir de 80)"),
            new("Linha contínua no eixo junto da interseção", $"{p.ComprimentoLfo:0}", "m", $"comprimento {pre}"),
            new("Legenda PARE: altura das letras", $"{p.AlturaLegenda:0.00}", "m", $"{iv}, 8.3; a 1,60 m antes da LRE"),
            new("Seta: comprimento", $"{p.ComprimentoSeta:0.00}", "m", $"{iv}, 8.1.1"),
            new("R-1: lado", $"{p.LadoR1:0.00}", "m", $"{i}, 3.7 (recomendado)"),
            new("R-2: lado", $"{p.LadoR2:0.00}", "m", $"{i}, 3.7 (recomendado)"),
            new("Sinais circulares (R-33, R-24a): diâmetro", $"{p.DiametroCircular:0.00}", "m", $"{i}, 3.7 (recomendado)"),
            new("Placas: afastamento lateral ao bordo", $"{p.AfastamentoPlaca:0.00}", "m", $"{i}, 3.13 (mínimo)"),
            new("Faixas por entrada / no anel (rotatória)", $"{p.FaixasEntrada} / {p.FaixasAnel}", "", pre),
        };
    }

    public static IEnumerable<string> PendenciasPadrao() => new[]
    {
        "A-12 (interseção em círculo) antes da rotatória onde ela for de difícil percepção à distância, e A-15 (parada obrigatória à frente) onde o R-1 não for visto a tempo (MBST Vol. II e Vol. I, p. 45).",
        "Refúgio nas ilhas divisórias e rebaixamento das calçadas nas faixas de pedestres (acessibilidade, NBR 9050) [A CONFIRMAR].",
        "Desenho das setas e das letras da legenda: conferir com as figuras do MBST Vol. IV (8.1.1 e 8.3); aqui estão simplificados [A CONFIRMAR].",
        "Ilhas canalizadoras e narizes de ilha: marcas de canalização (LCA) e zebrado (ZPA) de aproximação, tachas e tachões (MBST Vol. IV, cap. 6, e Vol. VI) — não incluídos.",
        "Regulamentação de velocidade (R-19), sinalização de indicação (Vol. III) e de obras não incluídas.",
        "Material e espessura da pintura (tinta acrílica, termoplástico) e retrorrefletância: especificação do órgão (DER, DNIT, prefeitura).",
        "Posição final das placas: conferir em campo (postes, árvores, acessos de lote e visibilidade).",
    };

    public PastaXlsx Planilha()
    {
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n2 = new EstiloCelula { Formato = "0.00" };
        var n0 = new EstiloCelula { Formato = "0" };

        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };

        var quant = Quantitativo(Itens);
        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string a, string b) { fr.Texto(lr, 1, a, txt with { Negrito = true }); fr.Texto(lr, 2, b, q); lr++; }
        Par("Itens sinalizados", string.Join(", ", Itens.Select(i => $"{i.Item} ({i.Tipo})")));
        Par("Pintura branca", $"{Itens.SelectMany(i => i.Marcas).Where(m => m.Cor == "branca").Sum(m => m.Area):0.00} m²");
        Par("Pintura amarela", $"{Itens.SelectMany(i => i.Marcas).Where(m => m.Cor == "amarela").Sum(m => m.Area):0.00} m²");
        Par("Legendas", $"{Itens.Sum(i => i.Legendas.Count)} un");
        Par("Placas", $"{Itens.Sum(i => i.Placas.Count)} un ({string.Join(", ", Itens.SelectMany(i => i.Placas).GroupBy(p => p.Codigo).Select(g => $"{g.Count()} {g.Key}"))})");
        var ver = Itens.SelectMany(i => i.Verificacoes).ToList();
        Par("Verificações", $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} reprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção");
        Par("Norma", $"{ParametrosSinalizacao.MbstIV}; {ParametrosSinalizacao.MbstI}");
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        foreach (var a in Avisos.Concat(Itens.SelectMany(i => i.Avisos))) { fr.Texto(lr, 1, "Aviso", txt with { Negrito = true }); fr.Texto(lr, 2, a, q); lr++; }
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 22); fr.Largura(2, 90);

        var fq = pasta.NovaFolha("Quantitativo");
        Cab(fq, 1, ("Grupo", 12), ("Código", 10), ("Descrição", 60), ("Cor", 10), ("Unidade", 9), ("Quantidade", 12), ("Peças", 8));
        int lq = 2;
        foreach (var x in quant)
        {
            fq.Texto(lq, 1, x.Grupo, txt); fq.Texto(lq, 2, x.Codigo, txt); fq.Texto(lq, 3, x.Descricao, q); fq.Texto(lq, 4, x.Cor, txt); fq.Texto(lq, 5, x.Unidade, txt);
            fq.Numero(lq, 6, x.Quantidade, x.Unidade == "m²" ? n2 : n0); fq.Numero(lq, 7, x.Pecas, n0);
            lq++;
        }

        var fi = pasta.NovaFolha("Por item");
        Cab(fi, 1, ("Item", 16), ("Onde", 34), ("Código", 10), ("Descrição", 50), ("Cor", 10), ("Área (m²)", 11), ("Peças", 8));
        int li = 2;
        foreach (var it in Itens)
            foreach (var g in it.Marcas.GroupBy(m => (m.Onde, m.Codigo, m.Nome, m.Cor)))
            {
                fi.Texto(li, 1, it.Item, txt); fi.Texto(li, 2, g.Key.Onde, q); fi.Texto(li, 3, g.Key.Codigo, txt); fi.Texto(li, 4, g.Key.Nome, q); fi.Texto(li, 5, g.Key.Cor, txt);
                fi.Numero(li, 6, g.Sum(m => m.Area), n2); fi.Numero(li, 7, g.Count(), n0);
                li++;
            }

        var fp = pasta.NovaFolha("Placas");
        Cab(fp, 1, ("Item", 16), ("Código", 8), ("Nome", 30), ("Dimensão", 26), ("X", 13), ("Y", 13), ("Onde", 34), ("Fonte", 56));
        int lp = 2;
        foreach (var it in Itens)
            foreach (var p in it.Placas)
            {
                fp.Texto(lp, 1, it.Item, txt); fp.Texto(lp, 2, p.Codigo, txt); fp.Texto(lp, 3, p.Nome, q); fp.Texto(lp, 4, Dimensao(p.Forma, p.Tamanho), q);
                fp.Numero(lp, 5, p.X, n2 with { Formato = "0.000" }); fp.Numero(lp, 6, p.Y, n2 with { Formato = "0.000" }); fp.Texto(lp, 7, p.Onde, q); fp.Texto(lp, 8, p.Fonte, q);
                lp++;
            }

        var fa = pasta.NovaFolha("Parâmetros");
        Cab(fa, 1, ("Parâmetro", 44), ("Valor", 14), ("Unidade", 9), ("Fonte", 70));
        int la = 2;
        foreach (var p in Parametros) { fa.Texto(la, 1, p.Nome, q); fa.Texto(la, 2, p.Valor, txt with { Horizontal = AlinhamentoH.Direita }); fa.Texto(la, 3, p.Unidade, txt); fa.Texto(la, 4, p.Fonte, q); la++; }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 32), ("Onde", 34), ("Valor", 44), ("Critério", 34), ("Fonte", 56), ("Resultado", 13));
        int lv = 2;
        foreach (var v in ver)
        {
            fv.Texto(lv, 1, v.Item, q); fv.Texto(lv, 2, v.Braco, q); fv.Texto(lv, 3, v.Valor, q); fv.Texto(lv, 4, v.Criterio, q); fv.Texto(lv, 5, v.Fonte, q); fv.Texto(lv, 6, v.Resultado, Res(v.Resultado));
            lv++;
        }

        var fd = pasta.NovaFolha("Pendências");
        Cab(fd, 1, ("Pendência", 110));
        int ld = 2;
        foreach (var p in Pendencias) { fd.Texto(ld, 1, p, q); ld++; }
        return pasta;
    }

    public void Salvar(string arquivo) => Planilha().Salvar(arquivo);
}
