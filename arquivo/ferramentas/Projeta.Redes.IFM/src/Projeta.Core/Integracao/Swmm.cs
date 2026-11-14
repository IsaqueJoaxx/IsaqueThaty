using System.ComponentModel;
using System.Globalization;
using System.Text;
using Projeta.Core.Comum;
using Projeta.Core.Hidrologia;
using Projeta.Core.Redes;

namespace Projeta.Core.Integracao;

public sealed class OpcoesSwmm
{
    [DisplayName("Duração da chuva de projeto (min)")] public double DuracaoChuva { get; set; } = 60;
    [DisplayName("Passo da chuva (min)")] public double PassoChuva { get; set; } = 5;
    [DisplayName("Duração da simulação (h)")] public double DuracaoSimulacao { get; set; } = 3;
    [DisplayName("Roteamento (KINWAVE / DYNWAVE)")] public string Roteamento { get; set; } = "DYNWAVE";
    [DisplayName("Passo de roteamento (s)")] public double PassoRoteamento { get; set; } = 5;
    [DisplayName("Passo de relatório (min)")] public double PassoRelatorio { get; set; } = 1;
    [DisplayName("Manning áreas impermeáveis")] public double NImperv { get; set; } = 0.013;
    [DisplayName("Manning áreas permeáveis")] public double NPerv { get; set; } = 0.10;
    [DisplayName("Armazenamento em depressões impermeáveis (mm)")] public double SImperv { get; set; } = 1.27;
    [DisplayName("Armazenamento em depressões permeáveis (mm)")] public double SPerv { get; set; } = 5.08;
    [DisplayName("Horton — taxa máxima (mm/h)")] public double HortonMax { get; set; } = 76.2;
    [DisplayName("Horton — taxa mínima (mm/h)")] public double HortonMin { get; set; } = 3.81;
    [DisplayName("Horton — decaimento (1/h)")] public double HortonDecay { get; set; } = 4.0;
    [DisplayName("Declividade padrão das bacias (%)")] public double DeclividadeBacia { get; set; } = 2.0;
    [DisplayName("Profundidade extra de alagamento sobre a tampa (m)")] public double SobreTampa { get; set; } = 0.0;
    [DisplayName("Usar chuva de projeto da equação da rede")] public bool ChuvaDeProjeto { get; set; } = true;
}

/// <summary>Gera arquivos INP do SWMM 5 a partir de redes de drenagem/esgoto dimensionadas.</summary>
public static class SwmmInp
{
    private static string F(double v, int c = 4) => Num.I(v, c);

    public static string Gerar(Rede rede, Projeto projeto, OpcoesSwmm op) => Gerar(rede, projeto, op, out _);

    /// <summary>Gera o INP. <paramref name="ids"/> mapeia "N"+Id do nó / "T"+Id do trecho / "B"+Id da bacia → nome usado no SWMM.</summary>
    public static string Gerar(Rede rede, Projeto projeto, OpcoesSwmm op, out Dictionary<string, string> ids)
    {
        var res = rede.Resultado ?? throw new InvalidOperationException("Dimensione a rede antes de exportar para o SWMM.");
        var sb = new StringBuilder();
        ids = new Dictionary<string, string>();
        var mapa = ids;
        string Id(string chave, string nome)
        {
            if (mapa.TryGetValue(chave, out var id)) return id;
            var baseId = Texto.IdSeguro(nome);
            id = baseId; int k = 2;
            while (mapa.ContainsValue(id)) id = baseId + "_" + k++;
            mapa[chave] = id;
            return id;
        }
        var top = new Topologia(rede);
        var inicio = new DateTime(2000, 1, 1);
        var fim = inicio.AddHours(op.DuracaoSimulacao);

        sb.AppendLine("[TITLE]");
        sb.AppendLine($";;Rede {rede.Nome} — exportada pelo PROJETA em {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("[OPTIONS]");
        sb.AppendLine("FLOW_UNITS           CMS");
        sb.AppendLine("INFILTRATION         HORTON");
        sb.AppendLine($"FLOW_ROUTING         {op.Roteamento.ToUpperInvariant()}");
        sb.AppendLine("LINK_OFFSETS         ELEVATION");
        sb.AppendLine("MIN_SLOPE            0");
        sb.AppendLine("ALLOW_PONDING        NO");
        sb.AppendLine("SKIP_STEADY_STATE    NO");
        sb.AppendLine($"START_DATE           {inicio:MM/dd/yyyy}");
        sb.AppendLine("START_TIME           00:00:00");
        sb.AppendLine($"REPORT_START_DATE    {inicio:MM/dd/yyyy}");
        sb.AppendLine("REPORT_START_TIME    00:00:00");
        sb.AppendLine($"END_DATE             {fim:MM/dd/yyyy}");
        sb.AppendLine($"END_TIME             {fim:HH:mm:ss}");
        sb.AppendLine("SWEEP_START          01/01");
        sb.AppendLine("SWEEP_END            12/31");
        sb.AppendLine("DRY_DAYS             0");
        sb.AppendLine($"REPORT_STEP          {TimeSpan.FromMinutes(op.PassoRelatorio):hh\\:mm\\:ss}");
        sb.AppendLine("WET_STEP             00:01:00");
        sb.AppendLine("DRY_STEP             01:00:00");
        sb.AppendLine($"ROUTING_STEP         {F(op.PassoRoteamento, 2)}");
        sb.AppendLine("INERTIAL_DAMPING     PARTIAL");
        sb.AppendLine("NORMAL_FLOW_LIMITED  BOTH");
        sb.AppendLine("FORCE_MAIN_EQUATION  H-W");
        sb.AppendLine("VARIABLE_STEP        0.75");
        sb.AppendLine("LENGTHENING_STEP     0");
        sb.AppendLine("MIN_SURFAREA         1.167");
        sb.AppendLine("MAX_TRIALS           8");
        sb.AppendLine("HEAD_TOLERANCE       0.0015");
        sb.AppendLine("SYS_FLOW_TOL         5");
        sb.AppendLine("LAT_FLOW_TOL         5");
        sb.AppendLine("MINIMUM_STEP         0.5");
        sb.AppendLine("THREADS              1");
        sb.AppendLine();

        bool temBacias = rede.Bacias.Any(b => b.NoId is not null && b.Area > 0);
        var eq = projeto.Equacao(rede.Drenagem.EquacaoId);
        if (temBacias && op.ChuvaDeProjeto && eq is not null)
        {
            sb.AppendLine("[RAINGAGES]");
            sb.AppendLine(";;Name  Format    Interval  SCF  Source");
            sb.AppendLine($"PLUV1  INTENSITY {TimeSpan.FromMinutes(op.PassoChuva):h\\:mm}  1.0  TIMESERIES  CHUVA_PROJETO");
            sb.AppendLine();
        }

        if (temBacias)
        {
            sb.AppendLine("[SUBCATCHMENTS]");
            sb.AppendLine(";;Name  RainGage  Outlet  Area(ha)  %Imperv  Width(m)  %Slope  CurbLen");
            foreach (var b in rede.Bacias.Where(b => b.NoId is not null && b.Area > 0))
            {
                var no = rede.NoPorId(b.NoId)!;
                double areaM2 = b.Area * 10000;
                double comp = b.ComprimentoTalvegue > 1 ? b.ComprimentoTalvegue : Math.Sqrt(areaM2);
                double largura = areaM2 / comp;
                double decl = b.ComprimentoTalvegue > 1 && b.DesnivelTalvegue > 0 ? b.DesnivelTalvegue / b.ComprimentoTalvegue * 100 : op.DeclividadeBacia;
                sb.AppendLine($"{Id("B" + b.Id, b.Nome)}  PLUV1  {Id("N" + no.Id, no.Nome)}  {F(b.Area, 4)}  {F(b.ImpermeabilidadeEstimada(), 2)}  {F(largura, 2)}  {F(Math.Max(decl, 0.1), 3)}  0");
            }
            sb.AppendLine();
            sb.AppendLine("[SUBAREAS]");
            sb.AppendLine(";;Subcatch  N-Imperv  N-Perv  S-Imperv  S-Perv  PctZero  RouteTo");
            foreach (var b in rede.Bacias.Where(b => b.NoId is not null && b.Area > 0))
                sb.AppendLine($"{Id("B" + b.Id, b.Nome)}  {F(op.NImperv)}  {F(op.NPerv)}  {F(op.SImperv)}  {F(op.SPerv)}  25  OUTLET");
            sb.AppendLine();
            sb.AppendLine("[INFILTRATION]");
            sb.AppendLine(";;Subcatch  MaxRate  MinRate  Decay  DryTime  MaxInfil");
            foreach (var b in rede.Bacias.Where(b => b.NoId is not null && b.Area > 0))
                sb.AppendLine($"{Id("B" + b.Id, b.Nome)}  {F(op.HortonMax)}  {F(op.HortonMin)}  {F(op.HortonDecay)}  7  0");
            sb.AppendLine();
        }

        // nós
        var exutorios = rede.Nos.Where(n => top.Saem(n.Id).Count == 0).ToList();
        sb.AppendLine("[JUNCTIONS]");
        sb.AppendLine(";;Name  Elevation  MaxDepth  InitDepth  SurDepth  Aponded");
        foreach (var n in rede.Nos.Where(n => !exutorios.Contains(n)))
        {
            var rn = res.No(n.Id);
            if (rn is null) continue;
            sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(rn.CotaFundo, 3)}  {F(Math.Max(rn.Profundidade, 0.1), 3)}  0  {F(op.SobreTampa, 3)}  0");
        }
        sb.AppendLine();
        sb.AppendLine("[OUTFALLS]");
        sb.AppendLine(";;Name  Elevation  Type  StageData  Gated");
        foreach (var n in exutorios)
        {
            var rn = res.No(n.Id);
            if (rn is null) continue;
            sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(rn.CotaFundo, 3)}  FREE  NO");
        }
        sb.AppendLine();

        // trechos
        sb.AppendLine("[CONDUITS]");
        sb.AppendLine(";;Name  From  To  Length  Roughness  InOffset  OutOffset  InitFlow  MaxFlow");
        var xs = new StringBuilder();
        xs.AppendLine("[XSECTIONS]");
        xs.AppendLine(";;Link  Shape  Geom1  Geom2  Geom3  Geom4  Barrels");
        double nPadrao = rede.Tipo == TipoRede.Esgoto ? rede.Esgoto.Manning : rede.Drenagem.Manning;
        foreach (var t in rede.Trechos)
        {
            var r = res.Trecho(t.Id);
            var a = rede.NoPorId(t.NoMontanteId); var b = rede.NoPorId(t.NoJusanteId);
            if (r is null || a is null || b is null || r.Diametro <= 0) continue;
            sb.AppendLine($"{Id("T" + t.Id, t.Nome)}  {Id("N" + a.Id, a.Nome)}  {Id("N" + b.Id, b.Nome)}  {F(Math.Max(r.Comprimento, 0.5), 3)}  {F(t.Manning ?? nPadrao, 4)}  {F(r.CotaFundoMontante, 3)}  {F(r.CotaFundoJusante, 3)}  0  0");
            if (r.Forma == FormaSecao.Circular)
                xs.AppendLine($"{Id("T" + t.Id, t.Nome)}  CIRCULAR  {F(r.Diametro, 3)}  0  0  0  {r.Celulas}");
            else
                xs.AppendLine($"{Id("T" + t.Id, t.Nome)}  RECT_CLOSED  {F(r.Altura, 3)}  {F(r.Diametro, 3)}  0  0  {r.Celulas}");
        }
        sb.AppendLine();
        sb.Append(xs).AppendLine();

        // vazões de tempo seco (esgoto) e adicionais
        var inflows = new StringBuilder();
        if (rede.Tipo == TipoRede.Esgoto)
        {
            // contribuição em marcha concentrada no nó de montante de cada trecho (vazão final)
            var porNo = new Dictionary<string, double>();
            foreach (var t in rede.Trechos)
            {
                var r = res.Trecho(t.Id); if (r is null) continue;
                double qMont = top.Chegam(t.NoMontanteId).Sum(x => res.Trecho(x.Id)?.VazaoFinalLs ?? 0);
                double local = Math.Max(0, r.VazaoFinalLs - qMont);
                porNo[t.NoMontanteId] = porNo.GetValueOrDefault(t.NoMontanteId) + local;
            }
            if (porNo.Count > 0)
            {
                inflows.AppendLine("[DWF]");
                inflows.AppendLine(";;Node  Constituent  Baseline  Patterns");
                foreach (var kv in porNo.Where(k => k.Value > 0))
                {
                    var n = rede.NoPorId(kv.Key)!;
                    inflows.AppendLine($"{Id("N" + n.Id, n.Nome)}  FLOW  {F(kv.Value / 1000.0, 6)}");
                }
                inflows.AppendLine();
            }
        }
        else if (rede.Nos.Any(n => n.VazaoAdicional > 0))
        {
            inflows.AppendLine("[DWF]");
            inflows.AppendLine(";;Node  Constituent  Baseline  Patterns");
            foreach (var n in rede.Nos.Where(n => n.VazaoAdicional > 0))
                inflows.AppendLine($"{Id("N" + n.Id, n.Nome)}  FLOW  {F(n.VazaoAdicional / 1000.0, 6)}");
            inflows.AppendLine();
        }
        sb.Append(inflows);

        if (temBacias && op.ChuvaDeProjeto && eq is not null)
        {
            var hiet = Hietograma.BlocosAlternados(eq, rede.Drenagem.TempoRetorno, op.DuracaoChuva, op.PassoChuva);
            sb.AppendLine("[TIMESERIES]");
            sb.AppendLine($";;Chuva de projeto — blocos alternados, {eq.Nome}, T = {rede.Drenagem.TempoRetorno:0.#} anos (mm/h)");
            for (int k = 0; k < hiet.Length; k++)
            {
                var t = TimeSpan.FromMinutes(k * op.PassoChuva);
                sb.AppendLine($"CHUVA_PROJETO  {(int)t.TotalHours}:{t.Minutes:00}  {F(hiet[k], 3)}");
            }
            sb.AppendLine($"CHUVA_PROJETO  {(int)TimeSpan.FromMinutes(hiet.Length * op.PassoChuva).TotalHours}:{TimeSpan.FromMinutes(hiet.Length * op.PassoChuva).Minutes:00}  0");
            sb.AppendLine();
        }

        sb.AppendLine("[REPORT]");
        sb.AppendLine("INPUT      NO");
        sb.AppendLine("CONTROLS   NO");
        sb.AppendLine("SUBCATCHMENTS ALL");
        sb.AppendLine("NODES ALL");
        sb.AppendLine("LINKS ALL");
        sb.AppendLine();

        if (rede.Nos.Count > 0)
        {
            double x0 = rede.Nos.Min(n => n.X), y0 = rede.Nos.Min(n => n.Y), x1 = rede.Nos.Max(n => n.X), y1 = rede.Nos.Max(n => n.Y);
            sb.AppendLine("[MAP]");
            sb.AppendLine($"DIMENSIONS {F(x0 - 10, 3)} {F(y0 - 10, 3)} {F(x1 + 10, 3)} {F(y1 + 10, 3)}");
            sb.AppendLine("Units      Meters");
            sb.AppendLine();
        }
        sb.AppendLine("[COORDINATES]");
        sb.AppendLine(";;Node  X-Coord  Y-Coord");
        foreach (var n in rede.Nos.Where(n => res.No(n.Id) is not null))
            sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(n.X, 3)}  {F(n.Y, 3)}");
        sb.AppendLine();
        if (temBacias && rede.Bacias.Any(b => b.Contorno.Count >= 3))
        {
            sb.AppendLine("[Polygons]");
            sb.AppendLine(";;Subcatch  X-Coord  Y-Coord");
            foreach (var b in rede.Bacias.Where(b => b.NoId is not null && b.Area > 0 && b.Contorno.Count >= 3))
                foreach (var p in b.Contorno) sb.AppendLine($"{Id("B" + b.Id, b.Nome)}  {F(p.X, 3)}  {F(p.Y, 3)}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ leitura (importação)

    /// <summary>Lê as seções de um INP (nome da seção em maiúsculas → linhas de dados sem comentários).</summary>
    public static Dictionary<string, List<string[]>> Secoes(string inp)
    {
        var d = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);
        List<string[]>? atual = null;
        foreach (var bruta in inp.Split('\n'))
        {
            var linha = bruta.Trim();
            int c = linha.IndexOf(';');
            if (c >= 0) linha = linha[..c].Trim();
            if (linha.Length == 0) continue;
            if (linha.StartsWith('[') && linha.EndsWith(']'))
            {
                var nome = linha[1..^1].Trim().ToUpperInvariant();
                if (!d.TryGetValue(nome, out atual)) { atual = new(); d[nome] = atual; }
                continue;
            }
            atual?.Add(linha.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        return d;
    }

    private static double D(string[] l, int i, double padrao = 0) =>
        i < l.Length && double.TryParse(l[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : padrao;

    /// <summary>Cria uma rede (nós, trechos, bacias) a partir de um INP do SWMM. Retorna mensagens.</summary>
    public static List<Mensagem> Importar(string inp, Rede rede)
    {
        var msgs = new List<Mensagem>();
        var s = Secoes(inp);
        var coords = new Dictionary<string, P2>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in s.GetValueOrDefault("COORDINATES") ?? new()) if (l.Length >= 3) coords[l[0]] = new P2(D(l, 1), D(l, 2));
        var nosPorNome = new Dictionary<string, No>(StringComparer.OrdinalIgnoreCase);
        var fundo = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        void CriarNo(string nome, TipoNo tipo, double invert, double prof)
        {
            var p = coords.GetValueOrDefault(nome);
            if (!coords.ContainsKey(nome)) msgs.Add(new(Gravidade.Aviso, "Nó sem coordenadas no INP", nome));
            var n = rede.NovoNo(tipo, p.X, p.Y);
            n.Nome = nome;
            if (prof > 0) n.CotaTampaForcada = invert + prof;
            if (tipo == TipoNo.Exutorio) n.CotaFundoForcada = invert;
            nosPorNome[nome] = n; fundo[nome] = invert;
        }
        foreach (var l in s.GetValueOrDefault("JUNCTIONS") ?? new()) CriarNo(l[0], TipoNo.PV, D(l, 1), D(l, 2));
        foreach (var l in s.GetValueOrDefault("OUTFALLS") ?? new()) CriarNo(l[0], TipoNo.Exutorio, D(l, 1), 0);
        foreach (var l in s.GetValueOrDefault("STORAGE") ?? new()) { CriarNo(l[0], TipoNo.Caixa, D(l, 1), D(l, 2)); msgs.Add(new(Gravidade.Aviso, "Reservatório (STORAGE) importado como caixa", l[0])); }
        foreach (var l in s.GetValueOrDefault("DIVIDERS") ?? new()) { CriarNo(l[0], TipoNo.PV, D(l, 1), 0); msgs.Add(new(Gravidade.Aviso, "Divisor importado como PV", l[0])); }

        bool offsetsElev = (s.GetValueOrDefault("OPTIONS") ?? new()).Any(l => l.Length >= 2 && l[0].Equals("LINK_OFFSETS", StringComparison.OrdinalIgnoreCase) && l[1].Equals("ELEVATION", StringComparison.OrdinalIgnoreCase));
        var xsec = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in s.GetValueOrDefault("XSECTIONS") ?? new()) xsec[l[0]] = l;
        foreach (var l in s.GetValueOrDefault("CONDUITS") ?? new())
        {
            if (l.Length < 7 || !nosPorNome.TryGetValue(l[1], out var a) || !nosPorNome.TryGetValue(l[2], out var b))
            { msgs.Add(new(Gravidade.Erro, "Conduto com nós inválidos", l[0])); continue; }
            var t = rede.NovoTrecho(a, b);
            t.Nome = l[0];
            t.ComprimentoForcado = D(l, 3);
            t.Manning = D(l, 4, 0.013);
            double zi = D(l, 5), zo = D(l, 6);
            t.CotaMontanteForcada = offsetsElev ? zi : fundo[l[1]] + zi;
            t.CotaJusanteForcada = offsetsElev ? zo : fundo[l[2]] + zo;
            if (xsec.TryGetValue(l[0], out var x) && x.Length >= 3)
            {
                var forma = x[1].ToUpperInvariant();
                if (forma == "CIRCULAR" || forma == "FORCE_MAIN") { t.Forma = FormaSecao.Circular; t.DiametroForcado = D(x, 2); }
                else if (forma.StartsWith("RECT")) { t.Forma = FormaSecao.Retangular; t.AlturaForcada = D(x, 2); t.DiametroForcado = D(x, 3); }
                else { t.Forma = FormaSecao.Circular; t.DiametroForcado = D(x, 2); msgs.Add(new(Gravidade.Aviso, $"Forma {forma} importada como circular de diâmetro {D(x, 2)}", l[0])); }
                t.Celulas = (int)Math.Max(1, D(x, 6, 1));
            }
        }
        foreach (var sec in new[] { "PUMPS", "ORIFICES", "WEIRS", "OUTLETS" })
            foreach (var l in s.GetValueOrDefault(sec) ?? new()) msgs.Add(new(Gravidade.Aviso, $"{sec} não suportado — ignorado", l[0]));

        var poligonos = new Dictionary<string, List<P2>>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in s.GetValueOrDefault("POLYGONS") ?? new())
            if (l.Length >= 3) { if (!poligonos.TryGetValue(l[0], out var lp)) poligonos[l[0]] = lp = new(); lp.Add(new P2(D(l, 1), D(l, 2))); }
        foreach (var l in s.GetValueOrDefault("SUBCATCHMENTS") ?? new())
        {
            if (l.Length < 5) continue;
            var bac = new Bacia { Nome = l[0], AreaHa = D(l, 3), Impermeabilidade = D(l, 4) };
            bac.C = Math.Clamp(0.20 + 0.70 * D(l, 4) / 100.0, 0.1, 0.95);
            if (nosPorNome.TryGetValue(l[2], out var no)) bac.NoId = no.Id;
            else msgs.Add(new(Gravidade.Aviso, $"Bacia lança em '{l[2]}' (não é nó importado)", l[0]));
            if (poligonos.TryGetValue(l[0], out var pg)) bac.Contorno = pg;
            rede.Bacias.Add(bac);
        }
        msgs.Add(new(Gravidade.Info, $"SWMM: {rede.Nos.Count} nós, {rede.Trechos.Count} condutos, {rede.Bacias.Count} sub-bacias importados."));
        return msgs;
    }
}

/// <summary>Resultados lidos do relatório (.rpt) do SWMM.</summary>
public sealed class ResultadoSwmm
{
    public sealed record No(string Nome, string Tipo, double ProfMedia, double ProfMax, double HglMax, string HoraMax);
    public sealed record Link(string Nome, string Tipo, double VazaoMax, string HoraMax, double VelMax, double MaxPlenaVazao, double MaxPlenaLamina);
    public sealed record Alagamento(string Nome, double Horas, double VazaoMax, double Volume);

    public List<No> Nos { get; } = new();
    public List<Link> Links { get; } = new();
    public List<Alagamento> Alagamentos { get; } = new();
    public List<string> Erros { get; } = new();
    public List<string> Avisos { get; } = new();
    public double ErroContinuidadeEscoamento { get; set; } = double.NaN;
    public double ErroContinuidadeRoteamento { get; set; } = double.NaN;

    public static ResultadoSwmm Ler(string rpt)
    {
        var r = new ResultadoSwmm();
        var linhas = rpt.Replace("\r", "").Split('\n');
        int modo = 0;
        for (int i = 0; i < linhas.Length; i++)
        {
            var l = linhas[i].Trim();
            if (l.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase)) r.Erros.Add(l);
            else if (l.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase)) r.Avisos.Add(l);
            else if (l.StartsWith("Runoff Quantity Continuity", StringComparison.OrdinalIgnoreCase)) modo = 1;
            else if (l.StartsWith("Flow Routing Continuity", StringComparison.OrdinalIgnoreCase)) modo = 2;
            else if (l.StartsWith("Continuity Error (%)", StringComparison.OrdinalIgnoreCase))
            {
                var v = Num.Ler(l.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault());
                if (modo == 1) r.ErroContinuidadeEscoamento = v; else if (modo == 2) r.ErroContinuidadeRoteamento = v;
            }
            else if (l == "Node Depth Summary") i = LerTabela(linhas, i, c =>
            {
                if (c.Length >= 7) r.Nos.Add(new No(c[0], c[1], D(c, 2), D(c, 3), D(c, 4), c.Length > 6 ? c[5] + " " + c[6] : ""));
            });
            else if (l == "Link Flow Summary") i = LerTabela(linhas, i, c =>
            {
                if (c.Length >= 8 && c[1].Equals("CONDUIT", StringComparison.OrdinalIgnoreCase))
                    r.Links.Add(new Link(c[0], c[1], D(c, 2), c[3] + " " + c[4], D(c, 5), D(c, 6), D(c, 7)));
                else if (c.Length >= 5)
                    r.Links.Add(new Link(c[0], c[1], D(c, 2), c[3] + " " + c[4], double.NaN, double.NaN, double.NaN));
            });
            else if (l == "Node Flooding Summary") i = LerTabela(linhas, i, c =>
            {
                if (c.Length >= 6) r.Alagamentos.Add(new Alagamento(c[0], D(c, 1), D(c, 2), D(c, 5)));
            });
        }
        return r;
    }

    private static double D(string[] l, int i) => i < l.Length && double.TryParse(l[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

    /// <summary>Lê linhas de dados após o segundo separador "-----" até uma linha em branco.</summary>
    private static int LerTabela(string[] linhas, int i, Action<string[]> linha)
    {
        int tracos = 0;
        int j = i + 1;
        for (; j < linhas.Length; j++)
        {
            var t = linhas[j].Trim();
            if (tracos < 2)
            {
                if (t.StartsWith("No nodes were", StringComparison.OrdinalIgnoreCase) || t.StartsWith("No conduits", StringComparison.OrdinalIgnoreCase)) return j;
                if (t.StartsWith("-----")) tracos++;
                continue;
            }
            if (t.Length == 0) break;
            linha(t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        return j;
    }
}
