using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;

namespace Projeta.Giro.Core.Verificacao;

/// <summary>Obstáculo: meio-fio, borda, poste, edificação. Linhas abertas ou contornos fechados.</summary>
public sealed class Obstaculo
{
    public string Nome { get; set; } = "";
    public List<V2> Pontos { get; set; } = new();
    public bool Fechado { get; set; }
}

public sealed class ResultadoConflito
{
    /// <summary>Trechos de obstáculos lineares dentro da envoltória (invasão).</summary>
    public List<List<V2>> Invasoes { get; } = new();
    /// <summary>Áreas de obstáculos fechados sobrepostas pela envoltória.</summary>
    public List<List<V2>> AreasInvadidas { get; } = new();
    /// <summary>Trechos dentro da faixa de folga (fora da envoltória, mas a menos que a folga).</summary>
    public List<List<V2>> NaFolga { get; } = new();
    /// <summary>Menor distância entre a envoltória e os obstáculos que não foram invadidos (m).</summary>
    public double DistanciaMinima { get; set; } = double.PositiveInfinity;
    public V2 PontoDistanciaMinima { get; set; }
    public double ComprimentoInvadido => Invasoes.Sum(p => Geo.Comprimento(p));
    public double AreaInvadida => AreasInvadidas.Sum(p => Math.Abs(Geo.Area(p)));
    public bool TemConflito => Invasoes.Count > 0 || AreasInvadidas.Count > 0;

    public string Resumo(double folga)
    {
        var l = new List<string>();
        if (!TemConflito) l.Add("sem invasão de obstáculos.");
        if (Invasoes.Count > 0) l.Add($"{Invasoes.Count} trecho(s) de meio-fio/borda invadidos, total {Num.F(ComprimentoInvadido)} m.");
        if (AreasInvadidas.Count > 0) l.Add($"{AreasInvadidas.Count} área(s) invadidas, total {Num.F(AreaInvadida)} m².");
        if (folga > 0) l.Add(NaFolga.Count > 0 ? $"{NaFolga.Count} trecho(s) a menos de {Num.F(folga)} m da envoltória." : $"folga de {Num.F(folga)} m respeitada.");
        if (double.IsFinite(DistanciaMinima)) l.Add($"menor afastamento sem invasão: {Num.F(DistanciaMinima)} m.");
        return string.Join(" ", l);
    }
}

public static class Conflitos
{
    public static ResultadoConflito Verificar(IReadOnlyList<IReadOnlyList<V2>> envoltoria, IEnumerable<Obstaculo> obstaculos, double folga = 0)
    {
        var r = new ResultadoConflito();
        var obs = obstaculos.Where(o => o.Pontos.Count >= 2).ToList();
        if (envoltoria.Count == 0 || obs.Count == 0) return r;
        var (eMin, eMax) = Geo.Caixa(envoltoria.SelectMany(p => p));
        double margem = Math.Max(folga, 0) + 50;
        bool Perto(Obstaculo o)
        {
            var (a, b) = Geo.Caixa(o.Pontos);
            return !(a.X > eMax.X + margem || a.Y > eMax.Y + margem || b.X < eMin.X - margem || b.Y < eMin.Y - margem);
        }
        obs = obs.Where(Perto).ToList();

        var linhas = obs.Select(o => o.Fechado ? (IReadOnlyList<V2>)o.Pontos.Append(o.Pontos[0]).ToList() : o.Pontos).ToList();
        r.Invasoes.AddRange(Poligonos.LinhasDentro(linhas, envoltoria).Where(p => Geo.Comprimento(p) > 0.005));
        var fechados = obs.Where(o => o.Fechado && o.Pontos.Count >= 3).Select(o => (IReadOnlyList<V2>)o.Pontos).ToList();
        if (fechados.Count > 0)
            r.AreasInvadidas.AddRange(Poligonos.Intersecao(envoltoria, fechados).Where(p => Math.Abs(Geo.Area(p)) > 0.001));

        if (folga > 0)
        {
            var inflada = Poligonos.Inflar(envoltoria, folga);
            var naFaixa = Poligonos.LinhasDentro(linhas, inflada);
            // tira o que já é invasão: trechos da faixa cujo meio está dentro da envoltória
            foreach (var p in naFaixa)
            {
                if (Geo.Comprimento(p) < 0.005) continue;
                var meio = Geo.PontoNaDistancia(p, Geo.Acumulado(p), Geo.Comprimento(p) / 2).P;
                if (!envoltoria.Any(e => Geo.DentroPoligono(meio, e))) r.NaFolga.Add(p);
            }
        }

        // menor distância entre a envoltória e os obstáculos (segmento a segmento, com filtro por caixa)
        double melhor = double.PositiveInfinity;
        V2 ponto = default;
        var segE = new List<(V2 A, V2 B)>();
        foreach (var e in envoltoria)
            for (int i = 0; i < e.Count; i++) segE.Add((e[i], e[(i + 1) % e.Count]));
        foreach (var o in linhas)
        {
            for (int j = 1; j < o.Count; j++)
            {
                V2 c = o[j - 1], d = o[j];
                foreach (var (a, b) in segE)
                {
                    double minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X), minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
                    double cx0 = Math.Min(c.X, d.X), cx1 = Math.Max(c.X, d.X), cy0 = Math.Min(c.Y, d.Y), cy1 = Math.Max(c.Y, d.Y);
                    double gx = Math.Max(0, Math.Max(cx0 - maxX, minX - cx1)), gy = Math.Max(0, Math.Max(cy0 - maxY, minY - cy1));
                    if (gx * gx + gy * gy >= melhor * melhor) continue;
                    double dist = Geo.DistSegmentos(a, b, c, d);
                    if (dist < melhor && dist > 1e-6)
                    {
                        melhor = dist;
                        ponto = Geo.ProjetarNoSegmento(Geo.ProjetarNoSegmento(a, c, d), a, b);
                    }
                }
            }
        }
        if (!r.TemConflito) { r.DistanciaMinima = melhor; r.PontoDistanciaMinima = ponto; }
        return r;
    }
}
