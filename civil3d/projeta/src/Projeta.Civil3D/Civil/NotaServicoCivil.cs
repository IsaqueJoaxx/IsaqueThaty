using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Lê do Civil 3D o que a nota de serviço precisa — seções calculadas do corredor (pontos por código), greide da baseline,
/// pontos notáveis do eixo e do greide, terreno — e monta a <see cref="NotaServico"/> do núcleo. Só leitura: não altera o desenho.
/// </summary>
internal static class NotaServicoCivil
{
    public sealed record Regiao(string Nome, double Inicio, double Fim, double[] Estacas);

    public sealed class Leitura
    {
        public NotaServico Nota { get; init; } = null!;
        /// <summary>Códigos de ponto vistos nas seções lidas → em quantas seções aparecem.</summary>
        public SortedDictionary<string, int> Codigos { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int SecoesLidas { get; set; }
        public int Interpoladas { get; set; }
        public int LinhasComOffset { get; set; }
        public int LinhasComBanquetas { get; set; }
        /// <summary>Elementos da configuração sem nenhum ponto em nenhuma linha.</summary>
        public List<string> ElementosSemPonto { get; } = new();
    }

    private sealed class Secao
    {
        public PontoSecao?[] Esq = Array.Empty<PontoSecao?>();
        public PontoSecao?[] Dir = Array.Empty<PontoSecao?>();
        public PontoSecao[] BanqE = Array.Empty<PontoSecao>();
        public PontoSecao[] BanqD = Array.Empty<PontoSecao>();
        public PontoSecao? OffE, OffD;
    }

    // ------------------------------------------------------------------ escolha de objetos

    public static List<(ObjectId Id, string Nome)> Corredores(Transaction tr)
    {
        var l = new List<(ObjectId, string)>();
        foreach (ObjectId id in CivilApplication.ActiveDocument.CorridorCollection)
            if (tr.GetObject(id, OpenMode.ForRead) is Corridor c) l.Add((id, c.Name));
        return l;
    }

    /// <summary>Baselines com alinhamento (as de feature line ficam de fora).</summary>
    public static List<Baseline> Baselines(Corridor c)
    {
        var l = new List<Baseline>();
        foreach (Baseline b in c.Baselines)
        {
            try { if (!b.AlignmentId.IsNull) l.Add(b); }
            catch { }
        }
        return l;
    }

    public static string NomeBaseline(Transaction tr, Baseline b)
    {
        string al = "", pf = "";
        try { al = ((Alignment)tr.GetObject(b.AlignmentId, OpenMode.ForRead)).Name; } catch { }
        try { pf = ((Profile)tr.GetObject(b.ProfileId, OpenMode.ForRead)).Name; } catch { }
        return $"{b.Name} (eixo {al}, greide {pf})";
    }

    /// <summary>
    /// Superfície de terreno mais provável: fora as do corredor (e as que levam o nome dele),
    /// de preferência com TN/terreno/primitivo/existente/EG no nome.
    /// </summary>
    public static int SuperficiePadrao(IReadOnlyList<string> nomes, IEnumerable<string> doCorredor)
    {
        var excl = doCorredor.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        bool Excluida(string n) => excl.Any(x => string.Equals(n, x, StringComparison.OrdinalIgnoreCase)
                                               || (x.Length >= 4 && n.Contains(x, StringComparison.OrdinalIgnoreCase)));
        string[] dicas = { "tn", "terreno", "primitiv", "existent", "natural", "eg", "topograf", "levantamento", "original" };
        for (int i = 0; i < nomes.Count; i++)
        {
            if (Excluida(nomes[i])) continue;
            var n = nomes[i].ToLowerInvariant();
            var partes = n.Split(new[] { ' ', '-', '_', '.', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (dicas.Any(d => partes.Contains(d) || (d.Length > 3 && n.Contains(d)))) return i;
        }
        for (int i = 0; i < nomes.Count; i++) if (!Excluida(nomes[i])) return i;
        return nomes.Count > 0 ? 0 : -1;
    }

    /// <summary>Nomes a evitar como terreno: as superfícies do corredor e o próprio nome do corredor.</summary>
    public static List<string> SuperficiesDoCorredor(Corridor c)
    {
        var l = new List<string> { c.Name };
        try { foreach (CorridorSurface s in c.CorridorSurfaces) l.Add(s.Name); } catch { }
        return l;
    }

    public static List<Regiao> Regioes(Baseline bl)
    {
        var l = new List<Regiao>();
        foreach (BaselineRegion r in bl.BaselineRegions)
        {
            double[] st;
            try { st = r.SortedStations(); } catch { st = Array.Empty<double>(); }
            Array.Sort(st);
            l.Add(new Regiao(r.Name, r.StartStation, r.EndStation, st));
        }
        return l.OrderBy(r => r.Inicio).ToList();
    }

    /// <summary>Códigos de ponto do corredor nesta baseline (amostra: início, meio e fim de cada região).</summary>
    public static SortedDictionary<string, int> CodigosDaBaseline(Baseline bl)
    {
        var cod = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in Regioes(bl))
        {
            if (r.Estacas.Length == 0) continue;
            foreach (var e in new[] { r.Estacas[0], r.Estacas[r.Estacas.Length / 2], r.Estacas[^1] }.Distinct())
                foreach (var p in Pontos(bl, e))
                    foreach (var c in p.Codigos) cod[c] = cod.TryGetValue(c, out var n) ? n + 1 : 1;
        }
        return cod;
    }

    // ------------------------------------------------------------------ montagem

    public static Leitura Montar(Transaction tr, Corridor cor, Baseline bl, Surface? terreno, ConfigNotaServico cfg,
        double? estacaInicial = null, double? estacaFinal = null, string? desenho = null)
    {
        cfg.Validar();
        Alignment al;
        try { al = (Alignment)tr.GetObject(bl.AlignmentId, OpenMode.ForRead); }
        catch { throw new InvalidOperationException($"A baseline {bl.Name} não tem alinhamento (feature line?). Escolha uma baseline por alinhamento."); }
        Profile? pf = null;
        try { pf = (Profile)tr.GetObject(bl.ProfileId, OpenMode.ForRead); } catch { }

        var regioes = Regioes(bl);
        if (regioes.Count == 0) throw new InvalidOperationException($"A baseline {bl.Name} do corredor {cor.Name} não tem regiões.");
        double bIni = regioes.Min(r => r.Inicio), bFim = regioes.Max(r => r.Fim);
        double ini = Math.Max(estacaInicial ?? bIni, bIni), fim = Math.Min(estacaFinal ?? bFim, bFim);
        if (fim < ini + 1e-6)
            throw new InvalidOperationException($"Trecho vazio: o corredor vai de {NotaServico.FormatarEstaca(bIni)} a {NotaServico.FormatarEstaca(bFim)}.");

        var ns = new NotaServico { Titulo = al.Name, Config = cfg };
        var r = new Leitura { Nota = ns };

        var notH = new List<PontoNotavel>();
        try { notH = NotaServico.PontosNotaveisHorizontais(TrechosDoEixo(al)); }
        catch (Exception ex) { ns.Avisos.Add("Não consegui ler os pontos notáveis do eixo: " + ex.Message); }
        var notV = pf is null ? new List<PontoNotavel>() : NotaveisVerticais(pf, ns.Avisos);
        var extras = cfg.IncluirRegioes ? regioes.SelectMany(x => new[] { x.Inicio, x.Fim }) : Enumerable.Empty<double>();
        var estacas = NotaServico.Estacas(ini, fim, cfg.Intervalo, notH, notV, extras);

        var cache = new Dictionary<long, Secao?>();
        Secao? SecaoEm(double e)
        {
            long k = (long)Math.Round(e * 10000);
            if (!cache.TryGetValue(k, out var s)) { s = LerSecao(bl, e, cfg, r.Codigos, terreno); cache[k] = s; if (s is not null) r.SecoesLidas++; }
            return s;
        }
        double Greide(double e)
        {
            if (pf is null) return double.NaN;
            try { return pf.ElevationAt(e); } catch { return double.NaN; }
        }

        int n = cfg.Elementos.Count, foraRegiao = 0, semGreide = 0, semTerreno = 0;
        foreach (var (e, h, v) in estacas)
        {
            var l = new LinhaNota { Estaca = e, NotavelH = h, NotavelV = v, Esquerdo = new PontoSecao?[n], Direito = new PontoSecao?[n] };
            l.CotaProjeto = Greide(e);
            if (double.IsNaN(l.CotaProjeto)) semGreide++;
            if (terreno is not null) l.CotaTerreno = CotaTerreno(al, terreno, e);
            if (terreno is not null && double.IsNaN(l.CotaTerreno)) semTerreno++;

            var reg = regioes.LastOrDefault(g => e >= g.Inicio - NotaServico.Tolerancia && e <= g.Fim + NotaServico.Tolerancia);
            if (reg is null || reg.Estacas.Length == 0) { foraRegiao++; ns.Linhas.Add(l); continue; }
            l.Regiao = reg.Nome;
            int k = Array.FindIndex(reg.Estacas, s => Math.Abs(s - e) <= NotaServico.Tolerancia);
            if (k >= 0)
            {
                if (SecaoEm(reg.Estacas[k]) is { } sec)
                {
                    l.Esquerdo = sec.Esq; l.Direito = sec.Dir;
                    l.BanquetasEsquerdo = sec.BanqE; l.BanquetasDireito = sec.BanqD;
                    l.OffsetEsquerdo = sec.OffE; l.OffsetDireito = sec.OffD;
                }
            }
            else
            {
                int k1 = Array.FindIndex(reg.Estacas, s => s > e);
                if (k1 > 0)
                {
                    double s0 = reg.Estacas[k1 - 1], s1 = reg.Estacas[k1];
                    var a = SecaoEm(s0); var b = SecaoEm(s1);
                    if (a is not null && b is not null)
                    {
                        double g0 = Greide(s0), g1 = Greide(s1), g = l.CotaProjeto;
                        for (int i = 0; i < n; i++)
                        {
                            l.Esquerdo[i] = NotaServico.Interpolar(e, s0, a.Esq[i], g0, s1, b.Esq[i], g1, g);
                            l.Direito[i] = NotaServico.Interpolar(e, s0, a.Dir[i], g0, s1, b.Dir[i], g1, g);
                        }
                        l.BanquetasEsquerdo = NotaServico.InterpolarLista(e, s0, a.BanqE, g0, s1, b.BanqE, g1, g);
                        l.BanquetasDireito = NotaServico.InterpolarLista(e, s0, a.BanqD, g0, s1, b.BanqD, g1, g);
                        l.OffsetEsquerdo = NotaServico.Interpolar(e, s0, a.OffE, g0, s1, b.OffE, g1, g);
                        l.OffsetDireito = NotaServico.Interpolar(e, s0, a.OffD, g0, s1, b.OffD, g1, g);
                        l.Interpolada = true;
                        r.Interpoladas++;
                    }
                }
            }
            ns.Linhas.Add(l);
        }

        // origem e avisos
        ns.Origem.Add(("Desenho", desenho ?? ""));
        ns.Origem.Add(("Corredor", cor.Name));
        ns.Origem.Add(("Baseline", bl.Name));
        ns.Origem.Add(("Alinhamento (eixo)", al.Name));
        ns.Origem.Add(("Greide (cota de projeto)", pf?.Name ?? "(sem perfil na baseline)"));
        ns.Origem.Add(("Terreno (cota do terreno)", terreno?.Name ?? "(nenhuma superfície)"));
        ns.Origem.Add(("Regiões", string.Join("; ", regioes.Select(g => $"{g.Nome}: {NotaServico.FormatarEstaca(g.Inicio)} a {NotaServico.FormatarEstaca(g.Fim)}"))));
        ns.Origem.Add(("Códigos encontrados", string.Join(", ", r.Codigos.Keys)));
        ns.Origem.Add(("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm", Num.Br)));

        try { if (cor.IsOutOfDate) ns.Avisos.Add($"O corredor {cor.Name} está desatualizado: reconstrua (Rebuild) e gere a nota de novo."); } catch { }
        try { if (al.StationEquations.Count > 0) ns.Avisos.Add("O alinhamento tem equações de estaca; a nota usa a estaca corrida (sem as equações)."); } catch { }
        if (foraRegiao > 0) ns.Avisos.Add($"{foraRegiao} estaca(s) fora das regiões do corredor: só os dados do eixo.");
        if (semGreide > 0) ns.Avisos.Add($"{semGreide} estaca(s) sem cota de projeto (fora do greide {pf?.Name}).");
        if (semTerreno > 0) ns.Avisos.Add($"{semTerreno} estaca(s) fora da superfície {terreno?.Name} (cota do terreno em branco).");
        for (int i = 0; i < n; i++)
            if (!ns.Linhas.Any(l => l.Esquerdo[i] is not null || l.Direito[i] is not null))
            {
                var el = cfg.Elementos[i];
                r.ElementosSemPonto.Add(el.Nome);
                ns.Avisos.Add($"Nenhum ponto com código {string.Join(" / ", el.Codigos)} ({el.Nome}) neste corredor.");
            }
        if (cfg.Terraplenagem)
        {
            r.LinhasComOffset = ns.Linhas.Count(l => l.OffsetEsquerdo is not null || l.OffsetDireito is not null);
            r.LinhasComBanquetas = ns.Linhas.Count(l => l.BanquetasEsquerdo.Length + l.BanquetasDireito.Length > 0);
            ns.Origem.Add(("Terraplenagem", $"{r.LinhasComOffset} linha(s) com offset; {r.LinhasComBanquetas} com banquetas"));
            if (r.LinhasComOffset == 0)
                ns.Avisos.Add("Nenhum offset encontrado: confira os códigos de offset (opção Codigos) ou escolha a superfície do terreno.");
        }
        return r;
    }

    /// <summary>Cota da superfície no eixo, na estaca (NaN fora da superfície). Sem dependências: o avulso usa este arquivo.</summary>
    private static double CotaTerreno(Alignment al, Surface s, double estaca)
    {
        try
        {
            double x = 0, y = 0;
            al.PointLocation(estaca, 0, ref x, ref y);
            return s.FindElevationAtXY(x, y);
        }
        catch { return double.NaN; }
    }

    /// <summary>Trechos do eixo em ordem (reta, curva, espiral) com estacas e sentido das curvas.</summary>
    public static List<TrechoEixo> TrechosDoEixo(Alignment al)
    {
        var l = new List<TrechoEixo>();
        var ents = al.Entities;
        for (int i = 0; i < ents.Count; i++)
        {
            var e = ents.GetEntityByOrder(i);
            for (int j = 0; j < e.SubEntityCount; j++)
            {
                var s = e[j];
                var tipo = s.SubEntityType switch
                {
                    AlignmentSubEntityType.Arc => TipoTrechoEixo.Curva,
                    AlignmentSubEntityType.Spiral => TipoTrechoEixo.Espiral,
                    _ => TipoTrechoEixo.Reta,
                };
                bool horario = s is AlignmentSubEntityArc arc && arc.Clockwise;
                l.Add(new TrechoEixo(tipo, s.StartStation, s.EndStation, horario));
            }
        }
        return l.OrderBy(t => t.Inicio).ToList();
    }

    /// <summary>BVC/EVC das curvas verticais e PIV onde o greide quebra sem curva.</summary>
    public static List<PontoNotavel> NotaveisVerticais(Profile pf, List<string> avisos)
    {
        var l = new List<PontoNotavel>();
        try
        {
            var pvis = new List<(double Est, ProfileEntity? Curva)>();
            foreach (ProfilePVI v in pf.PVIs)
            {
                ProfileEntity? c = null;
                try { c = v.VerticalCurve; } catch { }
                pvis.Add((v.RawStation, c));
            }
            pvis.Sort((a, b) => a.Est.CompareTo(b.Est));
            for (int i = 0; i < pvis.Count; i++)
            {
                var (est, c) = pvis[i];
                if (c is not null) { l.Add(new(c.StartStation, "BVC")); l.Add(new(c.EndStation, "EVC")); }
                else if (i > 0 && i < pvis.Count - 1) l.Add(new(est, "PIV"));
            }
        }
        catch (Exception ex) { avisos.Add($"Não consegui ler as curvas verticais do greide {pf.Name}: {ex.Message}"); }
        return l;
    }


    /// <summary>Ponto calculado do corredor: códigos, afastamento (negativo à esquerda), cota absoluta e XY.</summary>
    private sealed record Ponto(string[] Codigos, double Afastamento, double Cota, double X, double Y);

    /// <summary>
    /// Pontos calculados de uma seção do corredor. O Z de StationOffsetElevationToBaseline é relativo ao greide
    /// da baseline; a cota absoluta e o XY vêm de XYZ.
    /// </summary>
    private static List<Ponto> Pontos(Baseline bl, double estaca)
    {
        var l = new List<Ponto>();
        AppliedAssembly? aa;
        try { aa = bl.GetAppliedAssemblyAtStation(estaca); } catch { return l; }
        if (aa is null) return l;
        foreach (CalculatedPoint p in aa.Points)
        {
            double afastamento = p.StationOffsetElevationToBaseline.Y;
            var xyz = p.XYZ;
            string[] cods;
            try { cods = p.CorridorCodes.Cast<object>().Select(c => c?.ToString() ?? "").Where(c => c.Length > 0).ToArray(); }
            catch { cods = Array.Empty<string>(); }
            l.Add(new Ponto(cods, afastamento, xyz.Z, xyz.X, xyz.Y));
        }
        return l;
    }

    private static Secao? LerSecao(Baseline bl, double estaca, ConfigNotaServico cfg, IDictionary<string, int> codigos, Surface? terreno)
    {
        var pts = Pontos(bl, estaca);
        if (pts.Count == 0) return null;
        foreach (var p in pts)
            foreach (var c in p.Codigos) codigos[c] = codigos.TryGetValue(c, out var q) ? q + 1 : 1;

        int n = cfg.Elementos.Count;
        var s = new Secao { Esq = new PontoSecao?[n], Dir = new PontoSecao?[n] };
        double tol = cfg.ToleranciaEixo;
        bool Esq(double off) => off < -tol;
        bool Dir(double off) => off > tol;
        for (int i = 0; i < n; i++)
        {
            s.Esq[i] = Escolher(pts, cfg.Elementos[i].Codigos, Esq);
            s.Dir[i] = Escolher(pts, cfg.Elementos[i].Codigos, Dir);
        }
        if (cfg.Terraplenagem)
        {
            (s.BanqE, s.OffE) = Terraplenagem(pts, Esq, s.Esq, cfg, terreno);
            (s.BanqD, s.OffD) = Terraplenagem(pts, Dir, s.Dir, cfg, terreno);
        }
        return s;
    }

    /// <summary>
    /// Banquetas (pontos com código de banqueta, do eixo para fora) e offset de um lado: o ponto mais externo com código
    /// de offset; sem ele, o ponto mais externo além da plataforma e das banquetas que está sobre o terreno (até 5 cm).
    /// </summary>
    private static (PontoSecao[] Banquetas, PontoSecao? Offset) Terraplenagem(List<Ponto> pts, Func<double, bool> lado,
        PontoSecao?[] elementos, ConfigNotaServico cfg, Surface? terreno)
    {
        var doLado = pts.Where(p => lado(p.Afastamento)).OrderBy(p => Math.Abs(p.Afastamento)).ToList();
        var banq = new List<PontoSecao>();
        foreach (var p in doLado.Where(p => ConfigNotaServico.CasaAlgum(p.Codigos, cfg.CodigosBanqueta)))
        {
            if (banq.Count > 0 && Math.Abs(banq[^1].Afastamento - p.Afastamento) < 0.001 && Math.Abs(banq[^1].Cota - p.Cota) < 0.001) continue;
            banq.Add(new PontoSecao(p.Afastamento, p.Cota));
        }

        PontoSecao? off = null;
        var porCodigo = doLado.LastOrDefault(p => ConfigNotaServico.CasaAlgum(p.Codigos, cfg.CodigosOffset));
        if (porCodigo is not null) off = new PontoSecao(porCodigo.Afastamento, porCodigo.Cota);
        else if (cfg.OffsetPeloTerreno && terreno is not null)
        {
            double limite = elementos.Where(e => e is not null).Select(e => Math.Abs(e!.Value.Afastamento))
                                     .Concat(banq.Select(b => Math.Abs(b.Afastamento))).DefaultIfEmpty(0).Max();
            for (int i = doLado.Count - 1; i >= 0; i--)
            {
                var p = doLado[i];
                if (Math.Abs(p.Afastamento) <= limite + 0.001) break;
                double z;
                try { z = terreno.FindElevationAtXY(p.X, p.Y); } catch { continue; }
                if (Math.Abs(p.Cota - z) <= 0.05) { off = new PontoSecao(p.Afastamento, p.Cota); break; }
            }
        }
        return (banq.ToArray(), off);
    }

    /// <summary>
    /// Primeiro código da lista (aceita * e ?) que existe no lado; havendo mais de um ponto com ele, o mais afastado do eixo.
    /// </summary>
    private static PontoSecao? Escolher(List<Ponto> pts, List<string> codigos, Func<double, bool> lado)
    {
        foreach (var codigo in codigos)
        {
            var achados = pts.Where(p => lado(p.Afastamento) && p.Codigos.Any(c => ConfigNotaServico.Casa(c, codigo))).ToList();
            if (achados.Count == 0) continue;
            var m = achados.OrderByDescending(p => Math.Abs(p.Afastamento)).First();
            return new PontoSecao(m.Afastamento, m.Cota);
        }
        return null;
    }
}
