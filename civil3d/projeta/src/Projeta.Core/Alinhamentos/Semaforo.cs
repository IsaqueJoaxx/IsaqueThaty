using Projeta.Core.Relatorios;

namespace Projeta.Core.Alinhamentos;

// ======================================================================================================================
// Plano semafórico de tempo fixo da interseção (PRJSEMAFORO): estágios, entreverdes, ciclo ótimo de Webster, verdes,
// grau de saturação, atraso, fila, grupos focais e laços. Critérios do Manual Brasileiro de Sinalização de Trânsito,
// Volume V – Sinalização Semafórica (CONTRAN, Res. 973/2022). O que não vem do manual está marcado [PREMISSA].
// ======================================================================================================================

/// <summary>O que o PRJSEMAFORO guarda no desenho para a interseção.</summary>
public sealed class DadosSemaforo
{
    /// <summary>Volume que chega por braço (chave: rumo do braço), ucp/h.</summary>
    public Dictionary<string, double> Volumes { get; set; } = new();
    public double Ciclo { get; set; }
    public List<string> Desenhados { get; set; } = new();
}

/// <summary>Parâmetros do plano semafórico (lembrados entre as sessões).</summary>
public sealed class ParametrosSemaforo
{
    public const string MbstV = "CONTRAN, MBST Vol. V – Sinalização Semafórica (2022, Res. 973/2022)";
    /// <summary>Tempo de percepção e reação (s), desaceleração admissível (m/s²), comprimento do veículo (m), g (m/s²): valores usuais do MBST V, 5.7.</summary>
    public const double Tpr = 1.0, Aad = 3.0, ComprimentoVeiculo = 5.0, G = 9.8;
    /// <summary>Espaço médio por veículo na fila (m), dos exemplos do MBST V.</summary>
    public const double EspacoFila = 6.0;

    /// <summary>Velocidade regulamentada das aproximações (km/h).</summary>
    public double Velocidade { get; set; } = 40;
    /// <summary>Rampa das aproximações (+ subindo), em fração.</summary>
    public double Rampa { get; set; }
    /// <summary>Fluxo de saturação por faixa (ucp/h); o manual cita 1600 a 2000.</summary>
    public double FluxoSaturacaoFaixa { get; set; } = 1800;
    /// <summary>Largura de faixa usada para contar as faixas de cada aproximação (m).</summary>
    public double LarguraFaixa { get; set; } = 3.5;
    /// <summary>Verde de pedestres (s): mínimo 4, recomendado 7 (MBST V, 5.10 e 5.14).</summary>
    public double VerdePedestre { get; set; } = 7;
    public double VelocidadePedestre { get; set; } = 1.2;
    /// <summary>Verde de segurança veicular (s): 10 a 20, nunca abaixo de 10 (MBST V, 5.14).</summary>
    public double VerdeSeguranca { get; set; } = 10;
    public double CicloMaximo { get; set; } = 120;
    public double GrauSaturacaoMax { get; set; } = 0.90;
    public bool Lacos { get; set; }
    public bool ParametrosDoUsuario { get; set; }
}

/// <summary>Aproximação (o que chega por um braço) no plano.</summary>
public sealed class AproximacaoSemaforo
{
    public int Braco { get; set; }
    public string Rumo { get; set; } = "";
    public string Nome { get; set; } = "";
    public bool Principal { get; set; }
    public string Grupo { get; set; } = "";
    public int Estagio { get; set; }
    public double Volume { get; set; }
    public int Faixas { get; set; }
    public double FluxoSaturacao { get; set; }
    public double Y { get; set; }
    public double DRetencao { get; set; }
    /// <summary>Da retenção até o fim da área de conflito (bordo oposto da via transversal), m.</summary>
    public double D2 { get; set; }
    public double Amarelo { get; set; }
    public double VermelhoGeral { get; set; }
    public double VerdeEfetivo { get; set; }
    public double GrauSaturacao { get; set; }
    public double Capacidade { get; set; }
    public double Atraso { get; set; } = double.NaN;
    public double Fila { get; set; }
}

/// <summary>Estágio do plano (movimentos que andam juntos).</summary>
public sealed class EstagioSemaforo
{
    public int Numero { get; set; }
    public string Via { get; set; } = "";
    public List<string> Grupos { get; set; } = new();
    public double Y { get; set; }
    public double Amarelo { get; set; }
    public double VermelhoGeral { get; set; }
    public double Entreverdes => Amarelo + VermelhoGeral;
    public double VerdeMinimo { get; set; }
    public double Verde { get; set; }
    public double Inicio { get; set; }
    public double Duracao => Verde + Entreverdes;
}

/// <summary>Grupo semafórico (veicular ou de pedestres) com os tempos no ciclo (s, a partir do início do estágio 1).</summary>
public sealed class GrupoSemaforico
{
    public string Nome { get; set; } = "";
    public bool Pedestre { get; set; }
    public string Descricao { get; set; } = "";
    public int Braco { get; set; }
    public int Estagio { get; set; }
    public double InicioVerde { get; set; }
    public double Verde { get; set; }
    public double Amarelo { get; set; }
    /// <summary>Vermelho intermitente dos pedestres (s).</summary>
    public double VermelhoIntermitente { get; set; }
    /// <summary>Extensão da travessia (pedestres), m.</summary>
    public double Travessia { get; set; }
}

/// <summary>Grupo focal no desenho: posição da cabeça, poste e direção do tráfego (ou dos pedestres) que ele atende.</summary>
public sealed record CabecaSemaforo(string Grupo, string Tipo, double X, double Y, double Angulo, double PosteX, double PosteY, double Distancia, string Onde);

public sealed class PlanoSemaforico
{
    public string Item { get; set; } = "";
    public double Ciclo { get; set; }
    public double CicloOtimo { get; set; }
    public double CicloMinimo { get; set; }
    public double Y { get; set; }
    public double TempoPerdido { get; set; }
    public List<AproximacaoSemaforo> Aproximacoes { get; } = new();
    public List<EstagioSemaforo> Estagios { get; } = new();
    public List<GrupoSemaforico> Grupos { get; } = new();
    public List<CabecaSemaforo> Cabecas { get; } = new();
    public List<MarcaViaria> Lacos { get; } = new();
    public List<Verificacao> Verificacoes { get; } = new();
    public List<string> Avisos { get; } = new();
}

/// <summary>Contas do plano semafórico.</summary>
public static class CalculoSemaforico
{
    /// <summary>Cosseno do ângulo entre as direções de saída de dois braços (−1 = opostos).</summary>
    public static double Produto(PolilinhaEixo a, PolilinhaEixo b)
    {
        var (ax, ay) = a.Direcao(0);
        var (bx, by) = b.Direcao(0);
        return ax * bx + ay * by;
    }

    /// <summary>Amarelo (s): t = tpr + v / [2(aad + i·g)], arredondado para cima, com o mínimo da tabela (3 s até 40 km/h, 4 s a 50-60, 5 s a 70) e no máximo 5 s (MBST V, 5.7.1).</summary>
    public static double Amarelo(double vKmh, double rampa = 0)
    {
        double v = vKmh / 3.6;
        double t = ParametrosSemaforo.Tpr + v / (2 * (ParametrosSemaforo.Aad + rampa * ParametrosSemaforo.G));
        double min = vKmh <= 40 + 1e-9 ? 3 : vKmh <= 60 + 1e-9 ? 4 : 5;
        return Math.Clamp(Math.Ceiling(t - 1e-9), min, 5);
    }

    /// <summary>Vermelho geral (s): (d2 + c)/v, arredondado para cima, no mínimo 1 s (MBST V, 5.7.1).</summary>
    public static double VermelhoGeral(double d2, double vKmh) =>
        Math.Max(1, Math.Ceiling((d2 + ParametrosSemaforo.ComprimentoVeiculo) / (vKmh / 3.6) - 1e-9));

    /// <summary>Vermelho intermitente dos pedestres (s): tpr + l/vp, arredondado para cima (MBST V, 5.7.2).</summary>
    public static double VermelhoIntermitente(double travessia, double vp) => Math.Ceiling(ParametrosSemaforo.Tpr + travessia / vp - 1e-9);

    /// <summary>Ciclo ótimo de Webster (s): (1,5·Tp + 5)/(1 − Σy) (MBST V, 5.9.2); infinito se Σy ≥ 1.</summary>
    public static double CicloWebster(double tp, double y) => y >= 1 - 1e-9 ? double.PositiveInfinity : (1.5 * tp + 5) / (1 - y);

    /// <summary>Atraso médio de Webster (s/veíc): c(1−λ)²/[2(1−λx)] + x²/[2q(1−x)] − 0,65(c/q²)^(1/3)·x^(2+5λ); NaN se x ≥ 1.</summary>
    public static double AtrasoWebster(double c, double lambda, double x, double qPorSegundo)
    {
        if (x >= 1 - 1e-9 || qPorSegundo <= 0) return double.NaN;
        double d = c * (1 - lambda) * (1 - lambda) / (2 * (1 - lambda * x)) + x * x / (2 * qPorSegundo * (1 - x))
                   - 0.65 * Math.Pow(c / (qPorSegundo * qPorSegundo), 1.0 / 3) * Math.Pow(x, 2 + 5 * lambda);
        return Math.Max(0, d);
    }

    /// <summary>
    /// Plano de tempo fixo: um estágio por via (a principal primeiro); cada aproximação é um grupo veicular e cada faixa de
    /// pedestres um grupo de pedestres que anda no estágio da outra via. Entreverdes pelo manual; tempo perdido de cada estágio
    /// igual ao entreverdes; ciclo ótimo de Webster limitado pelos verdes mínimos e pelo ciclo máximo; verdes proporcionais a y.
    /// </summary>
    public static PlanoSemaforico Calcular(string nome, ModeloIntersecaoSinal m, IReadOnlyList<double> volumes, ParametrosSemaforo p, ParametrosSinalizacao ps)
    {
        var plano = new PlanoSemaforico { Item = nome };
        var eixos = m.Bracos.Select(b => b.Polilinha()).ToList();
        string V = ParametrosSemaforo.MbstV;

        // estágios: 1 = via principal, 2 = as outras (que não se cruzam entre si numa interseção de 3 ou 4 braços)
        string principal = m.Bracos.Where(b => b.Principal).Select(b => b.Nome).FirstOrDefault() ?? m.Bracos[0].Nome;
        var outras = m.Bracos.Where(b => !b.Principal).Select(b => b.Nome).Distinct().ToList();
        plano.Estagios.Add(new EstagioSemaforo { Numero = 1, Via = principal });
        if (outras.Count > 0) plano.Estagios.Add(new EstagioSemaforo { Numero = 2, Via = string.Join(" / ", outras) });
        var secIdx = Enumerable.Range(0, m.Bracos.Count).Where(i => !m.Bracos[i].Principal).ToList();
        bool conflito = secIdx.Count > 2 || (secIdx.Count == 2 && Produto(eixos[secIdx[0]], eixos[secIdx[1]]) > -0.866);
        if (conflito)
            plano.Avisos.Add($"{nome}: as aproximações secundárias não são opostas entre si (ou são mais de duas); no estágio 2 elas se cruzam — separe em estágios próprios no projeto.");

        int g = 0;
        var pos = new GeradorSinalizacao.PosicoesBraco[m.Bracos.Count];
        for (int i = 0; i < m.Bracos.Count; i++)
        {
            var b = m.Bracos[i];
            pos[i] = GeradorSinalizacao.Posicoes(m, eixos, i, ps, true);
            double dLonge = GeradorSinalizacao.DistanciaTransversal(m, eixos, i, out _, -1);
            double w = b.MeiaLarguraEm(pos[i].Retencao);
            bool conv = b.Conversao && b.Alargamento > 0;
            int faixas = Math.Max(1, (int)Math.Floor(b.MeiaLargura / p.LarguraFaixa + 0.15)) + (conv ? 1 : 0);
            double q = i < volumes.Count ? Math.Max(0, volumes[i]) : 0;
            var a = new AproximacaoSemaforo
            {
                Braco = i, Rumo = b.Rumo, Nome = b.Nome, Principal = b.Principal, Grupo = $"G{++g}", Estagio = b.Principal || plano.Estagios.Count == 1 ? 1 : 2,
                Volume = q, Faixas = faixas, FluxoSaturacao = faixas * p.FluxoSaturacaoFaixa, DRetencao = pos[i].Retencao,
                D2 = pos[i].Retencao + dLonge,
            };
            a.Y = a.Volume / a.FluxoSaturacao;
            a.Amarelo = Amarelo(p.Velocidade, p.Rampa);
            a.VermelhoGeral = VermelhoGeral(a.D2, p.Velocidade);
            plano.Aproximacoes.Add(a);
            plano.Grupos.Add(new GrupoSemaforico { Nome = a.Grupo, Descricao = $"veicular – chega pelo braço {b.Rumo} ({b.Nome}), {faixas} faixa(s)", Braco = i, Estagio = a.Estagio, Amarelo = a.Amarelo });
            _ = w;
        }
        // grupos de pedestres: a faixa de cada braço anda no estágio da outra via
        var pedestres = new List<GrupoSemaforico>();
        for (int i = 0; i < m.Bracos.Count; i++)
        {
            var b = m.Bracos[i];
            double l = 2 * Math.Min(b.MeiaLarguraEm(pos[i].Ftp0), b.MeiaLarguraEm(pos[i].Ftp1));
            var gp = new GrupoSemaforico
            {
                Nome = $"G{++g}", Pedestre = true, Descricao = $"pedestres – atravessam o braço {b.Rumo} ({b.Nome}), {l:0.0} m", Braco = i, Travessia = l,
                VermelhoIntermitente = VermelhoIntermitente(l, p.VelocidadePedestre),
            };
            plano.Grupos.Add(gp);
            gp.Estagio = plano.Estagios.Count == 1 ? 1 : b.Principal ? 2 : 1;
        }

        // entreverdes, y crítico e verde mínimo de cada estágio
        foreach (var e in plano.Estagios)
        {
            var ap = plano.Aproximacoes.Where(a => a.Estagio == e.Numero).ToList();
            e.Grupos.AddRange(ap.Select(a => a.Grupo));
            e.Y = ap.Count == 0 ? 0 : ap.Max(a => a.Y);
            e.Amarelo = ap.Count == 0 ? Amarelo(p.Velocidade, p.Rampa) : ap.Max(a => a.Amarelo);
            e.VermelhoGeral = ap.Count == 0 ? 1 : ap.Max(a => a.VermelhoGeral);
        }
        foreach (var gp in pedestres) plano.Estagios[gp.Estagio - 1].Grupos.Add(gp.Nome);
        foreach (var e in plano.Estagios)
        {
            double precisaPed = plano.Grupos.Where(x => x.Pedestre && x.Estagio == e.Numero).Select(x => p.VerdePedestre + x.VermelhoIntermitente + 1).DefaultIfEmpty(0).Max();
            e.VerdeMinimo = Math.Max(p.VerdeSeguranca, Math.Ceiling(precisaPed - e.Entreverdes - 1e-9));
        }

        // ciclo
        plano.TempoPerdido = plano.Estagios.Sum(e => e.Entreverdes);
        plano.Y = plano.Estagios.Sum(e => e.Y);
        plano.CicloOtimo = CicloWebster(plano.TempoPerdido, plano.Y);
        plano.CicloMinimo = plano.Estagios.Sum(e => e.VerdeMinimo + e.Entreverdes);
        double c = double.IsInfinity(plano.CicloOtimo) ? p.CicloMaximo : Math.Ceiling(plano.CicloOtimo - 1e-9);
        c = Math.Min(c, p.CicloMaximo);
        c = Math.Max(c, plano.CicloMinimo);
        // verdes proporcionais a y (verde real = efetivo, com o tempo perdido igual ao entreverdes) e mínimos
        double util = c - plano.TempoPerdido;
        foreach (var e in plano.Estagios)
            e.Verde = Math.Max(e.VerdeMinimo, plano.Y > 0 ? Math.Floor(util * e.Y / plano.Y + 1e-9) : Math.Floor(util / plano.Estagios.Count));
        double soma = plano.Estagios.Sum(e => e.Duracao);
        if (soma < c) plano.Estagios.OrderByDescending(e => e.Y).First().Verde += c - soma;
        c = plano.Estagios.Sum(e => e.Duracao);
        plano.Ciclo = c;
        double t0 = 0;
        foreach (var e in plano.Estagios) { e.Inicio = t0; t0 += e.Duracao; }

        // tempos dos grupos
        foreach (var gr in plano.Grupos)
        {
            var e = plano.Estagios[gr.Estagio - 1];
            gr.InicioVerde = e.Inicio;
            if (!gr.Pedestre) { gr.Verde = e.Verde; gr.Amarelo = e.Amarelo; }
            else { gr.Verde = e.Duracao - gr.VermelhoIntermitente - 1; }
        }

        // desempenho de cada aproximação
        foreach (var a in plano.Aproximacoes)
        {
            var e = plano.Estagios[a.Estagio - 1];
            a.VerdeEfetivo = e.Verde;
            double lambda = a.VerdeEfetivo / c;
            a.Capacidade = a.FluxoSaturacao * lambda;
            a.GrauSaturacao = a.Capacidade > 0 ? a.Volume / a.Capacidade : double.PositiveInfinity;
            double qs = a.Volume / 3600;
            a.Atraso = AtrasoWebster(c, lambda, a.GrauSaturacao, qs);
            a.Fila = Math.Ceiling(qs * (c - a.VerdeEfetivo) / Math.Max(1, a.Faixas) - 1e-9) * ParametrosSemaforo.EspacoFila;
        }

        // verificações
        var ver = plano.Verificacoes;
        string itemTxt = nome;
        ver.Add(new Verificacao("Ciclo", itemTxt,
            $"{c:0} s (Webster {(double.IsInfinity(plano.CicloOtimo) ? "sem solução: Σy ≥ 1" : $"{plano.CicloOtimo:0.0} s")}; mínimo pelos verdes {plano.CicloMinimo:0} s; Σy = {plano.Y:0.000}; tempo perdido {plano.TempoPerdido:0} s)",
            $"≤ {p.CicloMaximo:0} s", $"{V}, 5.9 (Webster; ciclo usual até 120 s)",
            double.IsInfinity(plano.CicloOtimo) || plano.Y >= 0.95 ? VerificacaoRotatoria.Reprovado : plano.CicloOtimo > p.CicloMaximo + 1e-9 ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Aprovado));
        foreach (var a in plano.Aproximacoes)
        {
            string onde = $"{itemTxt}, braço {a.Rumo} ({a.Nome}), {a.Grupo}";
            ver.Add(new Verificacao("Grau de saturação", onde,
                $"x = {a.GrauSaturacao:0.00} ({a.Volume:0} ucp/h para capacidade de {a.Capacidade:0} ucp/h; {a.Faixas} faixa(s) × {p.FluxoSaturacaoFaixa:0} ucp/h; verde {a.VerdeEfetivo:0} s)",
                $"≤ {p.GrauSaturacaoMax:0.00} (recomendado 0,75 a 0,90)", $"{V}, 5.8 e 5.9",
                a.GrauSaturacao <= p.GrauSaturacaoMax + 1e-9 ? VerificacaoRotatoria.Aprovado : a.GrauSaturacao < 1 ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
            ver.Add(new Verificacao("Entreverdes", onde, $"amarelo {a.Amarelo:0} s + vermelho geral {a.VermelhoGeral:0} s (d2 = {a.D2:0.0} m, c = 5 m, {p.Velocidade:0} km/h)",
                "amarelo pela fórmula com o mínimo da tabela e máximo 5 s; vermelho geral (d2 + c)/v, mínimo 1 s", $"{V}, 5.7.1", VerificacaoRotatoria.Informativo));
            ver.Add(new Verificacao("Atraso e fila", onde,
                double.IsNaN(a.Atraso) ? $"saturada: sem atraso estável; fila de {a.Fila:0} m por faixa no fim do vermelho" : $"atraso médio {a.Atraso:0.0} s/veíc; fila de {a.Fila:0} m por faixa no fim do vermelho",
                "informativo", $"Webster (atraso, MBST V, 5.18.3); fila = chegadas no vermelho × {ParametrosSemaforo.EspacoFila:0} m [PREMISSA]", VerificacaoRotatoria.Informativo));
        }
        foreach (var e in plano.Estagios)
            ver.Add(new Verificacao("Verde do estágio", $"{itemTxt}, estágio {e.Numero} ({e.Via})", $"{e.Verde:0} s (mínimo {e.VerdeMinimo:0} s)",
                $"≥ {p.VerdeSeguranca:0} s (verde de segurança) e o tempo dos pedestres do estágio", $"{V}, 5.14 e 5.15",
                e.Verde >= e.VerdeMinimo - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
        foreach (var gp in plano.Grupos.Where(x => x.Pedestre))
            ver.Add(new Verificacao("Pedestres", $"{itemTxt}, {gp.Nome}", $"verde {gp.Verde:0} s + vermelho intermitente {gp.VermelhoIntermitente:0} s ({gp.Travessia:0.0} m a {p.VelocidadePedestre:0.0} m/s) + 1 s",
                "verde ≥ 4 s (recomendado 7 s); intermitente = tpr + l/vp", $"{V}, 3.2.3, 5.7.2 e 5.14",
                gp.Verde >= 4 - 1e-9 ? (gp.Verde >= 7 - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Atencao) : VerificacaoRotatoria.Reprovado));

        // grupos focais
        for (int i = 0; i < m.Bracos.Count; i++)
        {
            var b = m.Bracos[i];
            var e = eixos[i];
            var a = plano.Aproximacoes[i];
            string onde = $"braço {b.Rumo} ({b.Nome})";
            double dLonge = GeradorSinalizacao.DistanciaTransversal(m, eixos, i, out _, -1);
            double sPoste = -(dLonge + 1.0);
            double w = b.MeiaLargura;
            // do outro lado do cruzamento a pista pode ser a do braço oposto (mais larga, com o alargamento)
            double wLonge = b.MeiaLarguraEm(0);
            for (int j = 0; j < m.Bracos.Count; j++)
                if (j != i && Produto(eixos[i], eixos[j]) < -0.866) wLonge = Math.Max(wLonge, m.Bracos[j].MeiaLarguraEm(-sPoste));
            var poste = m.NaCalcada(e.Ponto(sPoste, wLonge + 0.80), 0.80);
            var (sCol, vCol) = e.Projetar(poste.X, poste.Y);
            var cab = e.Ponto(sCol, w / 2);
            var (ux, uy) = e.Direcao(0);
            double chegando = Math.Atan2(-uy, -ux);
            double d = a.DRetencao - sCol;
            plano.Cabecas.Add(new CabecaSemaforo(a.Grupo, "veicular – coluna", poste.X, poste.Y, chegando, poste.X, poste.Y, d, onde));
            plano.Cabecas.Add(new CabecaSemaforo(a.Grupo, "veicular – projetado", cab.X, cab.Y, chegando, poste.X, poste.Y, d, onde));
            double ang = Math.Atan(Math.Abs(vCol - w / 2) / Math.Max(d, 0.1)) * 180 / Math.PI;
            ver.Add(new Verificacao("Grupo focal veicular – distância à retenção", $"{itemTxt}, {onde}, {a.Grupo}", $"{d:0.0} m (coluna depois do cruzamento, à direita, com braço projetado)",
                "coluna 4,50 a 30,00 m; projetado 13,00 a 30,00 m", $"{V}, 9.1.2",
                d >= 13 - 1e-9 && d <= 30 + 1e-9 ? VerificacaoRotatoria.Aprovado : d >= 4.5 - 1e-9 && d <= 30 + 1e-9 ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
            ver.Add(new Verificacao("Grupo focal veicular – ângulo de visada", $"{itemTxt}, {onde}, {a.Grupo}", $"{ang:0.0}° da retenção até a coluna",
                "≤ 20° (até 30° com coluna)", $"{V}, 9.1.2", ang <= 20 + 1e-9 ? VerificacaoRotatoria.Aprovado : ang <= 30 + 1e-9 ? VerificacaoRotatoria.Atencao : VerificacaoRotatoria.Reprovado));
            // pedestres: uma cabeça em cada ponta da faixa, voltada para quem atravessa na direção dela
            var gp = plano.Grupos.First(x => x.Pedestre && x.Braco == i);
            double sF = (pos[i].Ftp0 + pos[i].Ftp1) / 2, wf = gp.Travessia / 2;
            var (fx, fy) = e.Direcao(sF);
            double esquerda = Math.Atan2(fx, -fy);
            foreach (int lado in new[] { 1, -1 })
            {
                var q = m.NaCalcada(e.Ponto(sF, lado * (wf + 0.5)), 0.5);
                plano.Cabecas.Add(new CabecaSemaforo(gp.Nome, "pedestres", q.X, q.Y, lado > 0 ? esquerda : esquerda + Math.PI, q.X, q.Y, 0, onde));
            }
            if (gp.Travessia > 12)
                plano.Avisos.Add($"{nome}, {onde}: travessia de {gp.Travessia:0.0} m: acima de 12 m o manual pede pelo menos duas cabeças distribuídas ao longo da travessia (aqui há uma em cada ponta) — MBST V, 9.2.2.");
            // laços (opcional): um por faixa de quem chega, 1,0 m antes da retenção, 1,5 m de comprimento [PREMISSA]
            if (p.Lacos)
            {
                double s0 = a.DRetencao + ps.LarguraLre + 1.0, s1 = s0 + 1.5;
                bool conv = b.Conversao && b.Alargamento > 0;
                double c0 = conv ? GeometriaIntersecao.MeiaLarguraAlargada(0, b.Alargamento, b.DPlena, b.Teiper, s0) : 0;
                double wl = b.MeiaLarguraEm(s0);
                var faixas = new List<(double De, double Ate)>();
                if (conv) faixas.Add((-c0, c0));
                int nf = Math.Max(1, a.Faixas - (conv ? 1 : 0));
                for (int k = 0; k < nf; k++) faixas.Add((c0 + (wl - c0) * k / nf, c0 + (wl - c0) * (k + 1) / nf));
                foreach (var (de, ate) in faixas)
                    plano.Lacos.Add(new MarcaViaria("Laço", "Laço detector", "branca", GeoSinal.Retangulo(e, s0, s1, de + 0.5, ate - 0.5), $"{onde}, {a.Grupo}"));
            }
        }
        if (plano.Aproximacoes.Any(a => a.Volume <= 0)) plano.Avisos.Add($"{nome}: há aproximação sem volume; o plano não reflete o tráfego.");
        return plano;
    }
}

/// <summary>Planilha do PRJSEMAFORO.</summary>
public sealed class RelatorioSemaforo
{
    public string Titulo { get; set; } = "Plano semafórico";
    public string Desenho { get; set; } = "";
    public PlanoSemaforico Plano { get; set; } = new();
    public List<ParametroRotatoria> Parametros { get; } = new();
    public List<string> Pendencias { get; } = new();

    private const string Azul = "D9E1F2", Verde = "C6EFCE", Vermelho = "FFC7CE", Amarelo = "FFEB9C";

    public static List<ParametroRotatoria> ParametrosDe(ParametrosSemaforo p)
    {
        string pre = p.ParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA]";
        string v = ParametrosSemaforo.MbstV;
        return new List<ParametroRotatoria>
        {
            new("Velocidade regulamentada das aproximações", $"{p.Velocidade:0}", "km/h", pre),
            new("Rampa das aproximações", $"{p.Rampa * 100:0.0}", "%", pre),
            new("Fluxo de saturação por faixa", $"{p.FluxoSaturacaoFaixa:0}", "ucp/h", $"{v}, 5.4.1 (em geral 1600 a 2000 ucp/h por faixa); valor adotado {pre}; meça em campo"),
            new("Largura de faixa para contar as faixas", $"{p.LarguraFaixa:0.00}", "m", pre),
            new("Tempo de percepção e reação / desaceleração", $"{ParametrosSemaforo.Tpr:0.0} / {ParametrosSemaforo.Aad:0.0}", "s / m/s²", $"{v}, 5.7.1 (valores usuais)"),
            new("Comprimento do veículo (vermelho geral)", $"{ParametrosSemaforo.ComprimentoVeiculo:0.0}", "m", $"{v}, 5.7.1"),
            new("Tempo perdido de cada estágio", "= entreverdes", "s", $"{v} (exemplo sem levantamento em campo) [PREMISSA]"),
            new("Verde de segurança veicular", $"{p.VerdeSeguranca:0}", "s", $"{v}, 5.14 (10 a 20 s; nunca abaixo de 10)"),
            new("Verde de pedestres", $"{p.VerdePedestre:0}", "s", $"{v}, 5.10 e 5.14 (mínimo 4, recomendado 7)"),
            new("Velocidade de travessia dos pedestres", $"{p.VelocidadePedestre:0.0}", "m/s", $"{v}, 3.2.3"),
            new("Ciclo máximo", $"{p.CicloMaximo:0}", "s", $"{v}, 5.9 (usual até 120 s)"),
            new("Grau de saturação máximo", $"{p.GrauSaturacaoMax:0.00}", "", $"{v}, 5.9 (recomendado 0,75 a 0,90)"),
            new("Espaço por veículo na fila", $"{ParametrosSemaforo.EspacoFila:0.0}", "m", $"{v} (exemplos) [PREMISSA]"),
            new("Grupos focais veiculares", "coluna + projetado", "", $"{v}, 9.1 (dois por aproximação; coluna a 0,80 m do meio-fio; altura livre 2,40 m na coluna e 5,50 m no projetado)"),
            new("Laços detectores", p.Lacos ? "sim: 1,5 m, a 1,0 m da retenção" : "não", "", $"{v}, cap. 8 (seção detectada de 1,5 m no exemplo; perto da retenção) [PREMISSA]"),
        };
    }

    public static IEnumerable<string> PendenciasPadrao() => new[]
    {
        "Contagem classificada na hora de pico por movimento (ucp/h) e fluxo de saturação medido em campo: sem isso o plano é só uma estimativa.",
        "Conversões à esquerda permitidas cruzando o fluxo oposto não foram modeladas; avalie estágio exclusivo de conversão se o volume for alto.",
        "Critério de implantação do semáforo (MBST V, cap. 3: espera total da secundária, pedestres e acidentes) antes de decidir pelo semáforo.",
        "Coordenação com semáforos vizinhos (MBST V, cap. 8: coordenar quando a distância em m é menor que 3 vezes o maior verde em s).",
        "Projeto executivo: controlador, colunas, braços projetados, cabos, dutos, caixas de passagem e alimentação de energia.",
        "Grupo focal sonoro para pedestres com deficiência visual e rebaixamento das calçadas (acessibilidade) [A CONFIRMAR com o órgão].",
        "Rodar o PRJSINALIZA de novo: com semáforo, a interseção fica com retenção e faixa de pedestres em todas as aproximações, sem PARE e sem R-1.",
    };

    public PastaXlsx Planilha()
    {
        var pl = Plano;
        var pasta = new PastaXlsx { Titulo = Titulo };
        var tit = new EstiloCelula { Tamanho = 14, Negrito = true, Borda = false };
        var cab = new EstiloCelula { Negrito = true, Fundo = Azul, Horizontal = AlinhamentoH.Centro, Quebra = true };
        var txt = new EstiloCelula();
        var q = new EstiloCelula { Quebra = true };
        var n0 = new EstiloCelula { Formato = "0" };
        var n1 = new EstiloCelula { Formato = "0.0" };
        var n2 = new EstiloCelula { Formato = "0.00" };
        var n3 = new EstiloCelula { Formato = "0.000" };
        void Cab(FolhaXlsx f, int lin, params (string T, double L)[] cols)
        {
            for (int c = 0; c < cols.Length; c++) { f.Texto(lin, c + 1, cols[c].T, cab); f.Largura(c + 1, cols[c].L); }
            f.CongelarLinhas = lin;
        }
        EstiloCelula Res(string r) => txt with { Negrito = true, Horizontal = AlinhamentoH.Centro, Fundo = r == VerificacaoRotatoria.Aprovado ? Verde : r == VerificacaoRotatoria.Reprovado ? Vermelho : r == VerificacaoRotatoria.Atencao ? Amarelo : null };
        void Num(FolhaXlsx f, int l, int c, double v, EstiloCelula e) { if (double.IsNaN(v) || double.IsInfinity(v)) f.Texto(l, c, "-", txt); else f.Numero(l, c, v, e); }

        var fr = pasta.NovaFolha("Resumo");
        fr.Texto(1, 1, Titulo, tit);
        int lr = 3;
        void Par(string a, string b) { fr.Texto(lr, 1, a, txt with { Negrito = true }); fr.Texto(lr, 2, b, q); lr++; }
        Par("Interseção", pl.Item);
        Par("Ciclo", $"{pl.Ciclo:0} s (ótimo de Webster {(double.IsInfinity(pl.CicloOtimo) ? "sem solução" : $"{pl.CicloOtimo:0.0} s")}; mínimo pelos verdes {pl.CicloMinimo:0} s)");
        Par("Σy / tempo perdido", $"{pl.Y:0.000} / {pl.TempoPerdido:0} s");
        foreach (var e in pl.Estagios) Par($"Estágio {e.Numero}", $"{e.Via}: verde {e.Verde:0} s + amarelo {e.Amarelo:0} s + vermelho geral {e.VermelhoGeral:0} s; grupos {string.Join(", ", e.Grupos)}");
        var ver = pl.Verificacoes;
        Par("Verificações", $"{ver.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado)} reprovadas, {ver.Count(v => v.Resultado == VerificacaoRotatoria.Atencao)} com atenção");
        Par("Norma", ParametrosSemaforo.MbstV);
        Par("Desenho", Desenho);
        Par("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lr++;
        foreach (var a in pl.Avisos) { fr.Texto(lr, 1, "Aviso", txt with { Negrito = true }); fr.Texto(lr, 2, a, q); lr++; }
        lr++;
        fr.Texto(lr, 1, "Rascunho técnico gerado pelo PROJETA: a validação, o projeto executivo e a ART são do engenheiro responsável.", txt with { Italico = true, Borda = false });
        fr.Largura(1, 22); fr.Largura(2, 100);

        var fa = pasta.NovaFolha("Aproximações");
        Cab(fa, 1, ("Grupo", 7), ("Braço", 7), ("Via", 22), ("Estágio", 8), ("Volume (ucp/h)", 10), ("Faixas", 7), ("Fluxo de saturação (ucp/h)", 12), ("y", 8),
            ("Verde (s)", 8), ("Capacidade (ucp/h)", 11), ("Grau de saturação", 10), ("Atraso (s/veíc)", 10), ("Fila por faixa (m)", 10));
        int la = 2;
        foreach (var a in pl.Aproximacoes)
        {
            fa.Texto(la, 1, a.Grupo, txt); fa.Texto(la, 2, a.Rumo, txt); fa.Texto(la, 3, a.Nome, q); fa.Numero(la, 4, a.Estagio, n0); fa.Numero(la, 5, a.Volume, n0); fa.Numero(la, 6, a.Faixas, n0);
            fa.Numero(la, 7, a.FluxoSaturacao, n0); fa.Numero(la, 8, a.Y, n3); fa.Numero(la, 9, a.VerdeEfetivo, n0); fa.Numero(la, 10, a.Capacidade, n0); Num(fa, la, 11, a.GrauSaturacao, n2);
            Num(fa, la, 12, a.Atraso, n1); fa.Numero(la, 13, a.Fila, n0);
            la++;
        }

        var fg = pasta.NovaFolha("Grupos e tempos");
        Cab(fg, 1, ("Grupo", 7), ("Descrição", 50), ("Estágio", 8), ("Início do verde (s)", 10), ("Verde (s)", 8), ("Amarelo (s)", 8), ("Vermelho intermitente (s)", 11), ("Vermelho (s)", 9));
        int lg = 2;
        foreach (var g in pl.Grupos)
        {
            fg.Texto(lg, 1, g.Nome, txt); fg.Texto(lg, 2, g.Descricao, q); fg.Numero(lg, 3, g.Estagio, n0); fg.Numero(lg, 4, g.InicioVerde, n0); fg.Numero(lg, 5, g.Verde, n0);
            fg.Numero(lg, 6, g.Pedestre ? 0 : g.Amarelo, n0); fg.Numero(lg, 7, g.VermelhoIntermitente, n0);
            fg.Numero(lg, 8, pl.Ciclo - g.Verde - (g.Pedestre ? g.VermelhoIntermitente : g.Amarelo), n0);
            lg++;
        }

        var fe = pasta.NovaFolha("Entreverdes");
        Cab(fe, 1, ("Grupo", 7), ("Braço", 7), ("Retenção a (m do centro)", 12), ("d2 (m)", 9), ("Amarelo (s)", 9), ("Vermelho geral (s)", 10));
        int le = 2;
        foreach (var a in pl.Aproximacoes)
        {
            fe.Texto(le, 1, a.Grupo, txt); fe.Texto(le, 2, a.Rumo, txt); fe.Numero(le, 3, a.DRetencao, n2); fe.Numero(le, 4, a.D2, n2); fe.Numero(le, 5, a.Amarelo, n0); fe.Numero(le, 6, a.VermelhoGeral, n0);
            le++;
        }

        var fc = pasta.NovaFolha("Grupos focais");
        Cab(fc, 1, ("Grupo", 7), ("Tipo", 22), ("Onde", 30), ("X", 13), ("Y", 13), ("Distância à retenção (m)", 12));
        int lc = 2;
        foreach (var c in pl.Cabecas)
        {
            fc.Texto(lc, 1, c.Grupo, txt); fc.Texto(lc, 2, c.Tipo, txt); fc.Texto(lc, 3, c.Onde, q); fc.Numero(lc, 4, c.X, n3); fc.Numero(lc, 5, c.Y, n3);
            if (c.Distancia > 0) fc.Numero(lc, 6, c.Distancia, n1); else fc.Texto(lc, 6, "-", txt);
            lc++;
        }

        var fp = pasta.NovaFolha("Parâmetros");
        Cab(fp, 1, ("Parâmetro", 44), ("Valor", 18), ("Unidade", 10), ("Fonte", 70));
        int lp = 2;
        foreach (var p in Parametros) { fp.Texto(lp, 1, p.Nome, q); fp.Texto(lp, 2, p.Valor, txt with { Horizontal = AlinhamentoH.Direita }); fp.Texto(lp, 3, p.Unidade, txt); fp.Texto(lp, 4, p.Fonte, q); lp++; }

        var fv = pasta.NovaFolha("Verificações");
        Cab(fv, 1, ("Verificação", 30), ("Onde", 36), ("Valor", 50), ("Critério", 34), ("Fonte", 44), ("Resultado", 13));
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
