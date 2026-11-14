using System.Globalization;
using System.Text;
using Projeta.Core.Comum;
using Projeta.Core.Redes;

namespace Projeta.Core.Integracao;

/// <summary>Exportação/importação EPANET 2.2 (redes de água pressurizadas).</summary>
public static class EpanetInp
{
    private static string F(double v, int c = 4) => Num.I(v, c);

    /// <summary>Demanda (L/s) de cada nó: demanda base + parcela da vazão total distribuída pelo comprimento dos tubos.</summary>
    public static Dictionary<string, double> Demandas(Rede rede)
    {
        var d = rede.Nos.ToDictionary(n => n.Id, n => n.Tipo is TipoNo.Reservatorio or TipoNo.Tanque ? 0.0 : n.Demanda);
        var p = rede.Agua;
        if (p.DistribuirPorComprimento)
        {
            double total = p.DemandaTotal();
            double lTotal = rede.Trechos.Sum(rede.ComprimentoHorizontal);
            if (lTotal > 0 && total > 0)
                foreach (var t in rede.Trechos)
                {
                    double q = total * rede.ComprimentoHorizontal(t) / lTotal;
                    var a = rede.NoPorId(t.NoMontanteId); var b = rede.NoPorId(t.NoJusanteId);
                    bool ra = a is null || a.Tipo is TipoNo.Reservatorio or TipoNo.Tanque;
                    bool rb = b is null || b.Tipo is TipoNo.Reservatorio or TipoNo.Tanque;
                    if (ra && rb) continue;
                    if (ra) d[b!.Id] += q;
                    else if (rb) d[a!.Id] += q;
                    else { d[a!.Id] += q / 2; d[b!.Id] += q / 2; }
                }
        }
        return d;
    }

    public static string Gerar(Rede rede, out Dictionary<string, string> ids)
    {
        var sb = new StringBuilder();
        var mapa = new Dictionary<string, string>();
        ids = mapa;
        string Id(string chave, string nome)
        {
            if (mapa.TryGetValue(chave, out var id)) return id;
            var baseId = Texto.IdSeguro(nome);
            id = baseId; int k = 2;
            while (mapa.ContainsValue(id)) id = baseId + "_" + k++;
            mapa[chave] = id;
            return id;
        }
        var dem = Demandas(rede);
        var p = rede.Agua;

        sb.AppendLine("[TITLE]");
        sb.AppendLine($"Rede {rede.Nome} — exportada pelo PROJETA em {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("[JUNCTIONS]");
        sb.AppendLine(";ID  Elev  Demand  Pattern");
        foreach (var n in rede.Nos.Where(n => n.Tipo is not (TipoNo.Reservatorio or TipoNo.Tanque)))
            sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(Cota(n), 3)}  {F(dem[n.Id], 5)}  ");
        sb.AppendLine();
        sb.AppendLine("[RESERVOIRS]");
        sb.AppendLine(";ID  Head  Pattern");
        foreach (var n in rede.Nos.Where(n => n.Tipo == TipoNo.Reservatorio))
            sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(n.Carga > 0 ? n.Carga : Cota(n), 3)}  ");
        sb.AppendLine();
        sb.AppendLine("[TANKS]");
        sb.AppendLine(";ID  Elevation  InitLevel  MinLevel  MaxLevel  Diameter  MinVol  VolCurve");
        foreach (var n in rede.Nos.Where(n => n.Tipo == TipoNo.Tanque))
        {
            double nivel = n.Carga > 0 ? n.Carga - Cota(n) : 3.0;
            sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(Cota(n), 3)}  {F(nivel, 3)}  0  {F(Math.Max(nivel * 1.5, 1), 3)}  {F(n.ParametrosDispositivo.GetValueOrDefault("DiametroTanque", 10), 3)}  0  ");
        }
        sb.AppendLine();
        sb.AppendLine("[PIPES]");
        sb.AppendLine(";ID  Node1  Node2  Length  Diameter(mm)  Roughness  MinorLoss  Status");
        foreach (var t in rede.Trechos)
        {
            var a = rede.NoPorId(t.NoMontanteId); var b = rede.NoPorId(t.NoJusanteId);
            if (a is null || b is null) continue;
            double dm = (t.DiametroForcado ?? p.DiametroPadrao) * 1000.0;
            sb.AppendLine($"{Id("T" + t.Id, t.Nome)}  {Id("N" + a.Id, a.Nome)}  {Id("N" + b.Id, b.Nome)}  {F(Math.Max(rede.ComprimentoHorizontal(t), 0.1), 3)}  {F(dm, 1)}  {F(t.RugosidadeHW ?? p.RugosidadeHW, 1)}  0  Open");
        }
        sb.AppendLine();
        sb.AppendLine("[OPTIONS]");
        sb.AppendLine(" Units              LPS");
        sb.AppendLine(" Headloss           H-W");
        sb.AppendLine(" Trials             200");
        sb.AppendLine(" Accuracy           0.001");
        sb.AppendLine(" Unbalanced         Continue 10");
        sb.AppendLine(" Demand Multiplier  1.0");
        sb.AppendLine();
        sb.AppendLine("[TIMES]");
        sb.AppendLine(" Duration           0");
        sb.AppendLine();
        sb.AppendLine("[REPORT]");
        sb.AppendLine(" Status             No");
        sb.AppendLine(" Summary            No");
        sb.AppendLine(" Page               0");
        sb.AppendLine(" Nodes              All");
        sb.AppendLine(" Links              All");
        sb.AppendLine();
        sb.AppendLine("[COORDINATES]");
        sb.AppendLine(";Node  X-Coord  Y-Coord");
        foreach (var n in rede.Nos) sb.AppendLine($"{Id("N" + n.Id, n.Nome)}  {F(n.X, 3)}  {F(n.Y, 3)}");
        sb.AppendLine();
        sb.AppendLine("[END]");
        return sb.ToString();
    }

    private static double Cota(No n) => double.IsNaN(n.CotaTampa) ? 0 : n.CotaTampa;

    public static List<Mensagem> Importar(string inp, Rede rede)
    {
        var msgs = new List<Mensagem>();
        var s = SwmmInp.Secoes(inp);
        double D(string[] l, int i, double padrao = 0) => i < l.Length && double.TryParse(l[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : padrao;
        var coords = new Dictionary<string, P2>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in s.GetValueOrDefault("COORDINATES") ?? new()) if (l.Length >= 3) coords[l[0]] = new P2(D(l, 1), D(l, 2));
        var nos = new Dictionary<string, No>(StringComparer.OrdinalIgnoreCase);
        // unidades: se GPM/CFS etc., avisar (assumimos SI: LPS, m)
        var opts = s.GetValueOrDefault("OPTIONS") ?? new();
        var unid = opts.FirstOrDefault(l => l.Length >= 2 && l[0].Equals("Units", StringComparison.OrdinalIgnoreCase))?[1] ?? "GPM";
        bool si = unid.ToUpperInvariant() is "LPS" or "LPM" or "MLD" or "CMH" or "CMD" or "CMS";
        if (!si) msgs.Add(new(Gravidade.Aviso, $"Unidades {unid} (sistema americano) — valores importados sem conversão"));
        double fatorQ = unid.ToUpperInvariant() switch { "LPM" => 1 / 60.0, "MLD" => 1e6 / 86400.0, "CMH" => 1000 / 3600.0, "CMD" => 1000 / 86400.0, "CMS" => 1000, _ => 1 };

        No Criar(string nome, TipoNo tipo)
        {
            var p = coords.GetValueOrDefault(nome);
            var n = rede.NovoNo(tipo, p.X, p.Y);
            n.Nome = nome; n.DispositivoId = "MARCADOR";
            nos[nome] = n;
            return n;
        }
        foreach (var l in s.GetValueOrDefault("JUNCTIONS") ?? new())
        {
            var n = Criar(l[0], TipoNo.Juncao);
            n.CotaTampaForcada = D(l, 1);
            n.Demanda = D(l, 2) * fatorQ;
        }
        foreach (var l in s.GetValueOrDefault("RESERVOIRS") ?? new())
        {
            var n = Criar(l[0], TipoNo.Reservatorio);
            n.Carga = D(l, 1); n.CotaTampaForcada = D(l, 1);
        }
        foreach (var l in s.GetValueOrDefault("TANKS") ?? new())
        {
            var n = Criar(l[0], TipoNo.Tanque);
            n.CotaTampaForcada = D(l, 1);
            n.Carga = D(l, 1) + D(l, 2);
            n.ParametrosDispositivo["DiametroTanque"] = D(l, 5, 10);
        }
        foreach (var l in s.GetValueOrDefault("PIPES") ?? new())
        {
            if (l.Length < 6 || !nos.TryGetValue(l[1], out var a) || !nos.TryGetValue(l[2], out var b))
            { msgs.Add(new(Gravidade.Erro, "Tubo com nós inválidos", l.Length > 0 ? l[0] : "?")); continue; }
            var t = rede.NovoTrecho(a, b);
            t.Nome = l[0];
            t.ComprimentoForcado = D(l, 3);
            t.DiametroForcado = D(l, 4) / 1000.0;
            t.RugosidadeHW = D(l, 5, 130);
        }
        foreach (var sec in new[] { "PUMPS", "VALVES" })
            foreach (var l in s.GetValueOrDefault(sec) ?? new())
            {
                msgs.Add(new(Gravidade.Aviso, $"{sec} importado como tubo curto (não modelado)", l[0]));
                if (l.Length >= 3 && nos.TryGetValue(l[1], out var a) && nos.TryGetValue(l[2], out var b))
                {
                    var t = rede.NovoTrecho(a, b); t.Nome = l[0]; t.ComprimentoForcado = 1; t.DiametroForcado = 0.10;
                }
            }
        // demandas extras em [DEMANDS]
        foreach (var l in s.GetValueOrDefault("DEMANDS") ?? new())
            if (nos.TryGetValue(l[0], out var n)) n.Demanda += D(l, 1) * fatorQ;
        rede.Agua.DistribuirPorComprimento = false; // demandas vieram do arquivo
        msgs.Add(new(Gravidade.Info, $"EPANET: {rede.Nos.Count} nós e {rede.Trechos.Count} tubos importados."));
        return msgs;
    }
}

public sealed class ResultadoEpanet
{
    public sealed record No(string Nome, double Demanda, double Carga, double Pressao);
    public sealed record Link(string Nome, double Vazao, double Velocidade, double PerdaUnitaria);
    public List<No> Nos { get; } = new();
    public List<Link> Links { get; } = new();
    public List<string> Erros { get; } = new();
    public List<string> Avisos { get; } = new();

    public static ResultadoEpanet Ler(string rpt)
    {
        var r = new ResultadoEpanet();
        var linhas = rpt.Replace("\r", "").Split('\n');
        double D(string[] c, int i) => i < c.Length && double.TryParse(c[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;
        for (int i = 0; i < linhas.Length; i++)
        {
            var l = linhas[i].Trim();
            if (l.StartsWith("Error", StringComparison.OrdinalIgnoreCase)) r.Erros.Add(l);
            else if (l.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase) || l.StartsWith("Warning", StringComparison.Ordinal)) r.Avisos.Add(l);
            else if (l.StartsWith("Node Results")) i = Tabela(linhas, i, c => { if (c.Length >= 4) r.Nos.Add(new No(c[0], D(c, 1), D(c, 2), D(c, 3))); });
            else if (l.StartsWith("Link Results")) i = Tabela(linhas, i, c => { if (c.Length >= 4) r.Links.Add(new Link(c[0], D(c, 1), D(c, 2), D(c, 3))); });
        }
        return r;
    }

    private static int Tabela(string[] linhas, int i, Action<string[]> linha)
    {
        int tracos = 0, j = i + 1;
        for (; j < linhas.Length; j++)
        {
            var t = linhas[j].Trim();
            if (tracos < 2) { if (t.StartsWith("-----")) tracos++; continue; }
            if (t.Length == 0) break;
            linha(t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        return j;
    }

    /// <summary>Aumenta o diâmetro dos tubos com velocidade acima do limite (próximo comercial). Retorna nº de tubos alterados.</summary>
    public int AjustarDiametros(Rede rede, IReadOnlyDictionary<string, string> ids)
    {
        var p = rede.Agua;
        var diam = p.Diametros.OrderBy(x => x).ToList();
        int alterados = 0;
        foreach (var t in rede.Trechos)
        {
            if (!ids.TryGetValue("T" + t.Id, out var id)) continue;
            var l = Links.FirstOrDefault(x => x.Nome.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (l is null || double.IsNaN(l.Velocidade)) continue;
            double atual = t.DiametroForcado ?? p.DiametroPadrao;
            if (Math.Abs(l.Velocidade) > p.VelocidadeMaxima)
            {
                var prox = diam.FirstOrDefault(x => x > atual + 1e-6);
                if (prox > 0) { t.DiametroForcado = prox; alterados++; }
            }
        }
        return alterados;
    }
}

/// <summary>Executa os motores SWMM/EPANET (linha de comando) como processo externo.</summary>
public static class MotorExterno
{
    public sealed record Execucao(int Codigo, string Saida, string Erro, TimeSpan Tempo);

    public static Execucao Executar(string executavel, string inp, string rpt, string? saidaBin = null, int timeoutSeg = 300)
    {
        if (!File.Exists(executavel)) throw new FileNotFoundException("Executável não encontrado: " + executavel);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executavel,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(inp) ?? Environment.CurrentDirectory,
        };
        psi.ArgumentList.Add(inp);
        psi.ArgumentList.Add(rpt);
        if (saidaBin is not null) psi.ArgumentList.Add(saidaBin);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var pr = System.Diagnostics.Process.Start(psi)!;
        var outTask = pr.StandardOutput.ReadToEndAsync();
        var errTask = pr.StandardError.ReadToEndAsync();
        if (!pr.WaitForExit(timeoutSeg * 1000)) { try { pr.Kill(true); } catch { } throw new TimeoutException("Motor excedeu o tempo limite"); }
        return new Execucao(pr.ExitCode, outTask.Result, errTask.Result, sw.Elapsed);
    }
}
