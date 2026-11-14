using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Projeta.Civil3D;

internal sealed record SurveyPoint(string Name, double N, double E, double Z);
internal sealed record SurveyShot(string Station, string Target, double Horizontal, double Slope, double Zenith, double Direction, double HI, double HR, int Line);
internal sealed record SurveyBook(string Source, string Raw, string[] Stations, List<SurveyShot> Shots);
internal sealed record TraverseResult(List<SurveyPoint> RawPoints, List<SurveyPoint> Points, string Report, string Memory, double AngularError, double LinearError);

// This engine is independent of Autodesk and never executes imported files.
internal static class Traverse
{
    public const string Reference = "https://www.gov.br/dnit/pt-br/assuntos/planejamento-e-pesquisa/ipr/coletanea-de-manuais/vigentes/ipr_726_3aedicao_2006.pdf";
    public static double Number(string value)
    {
        value = value.Trim();
        if (value.Contains(',')) value = value.Replace(".", "").Replace(',', '.');
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n))
            throw new ArgumentException("Número inválido: " + value);
        return n;
    }
    public static string ReadText(string path)
    {
        if (new FileInfo(path).Length > 20_000_000) throw new ArgumentException("Arquivo acima de 20 MB.");
        var bytes = File.ReadAllBytes(path);
        try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(1252).GetString(bytes); }
    }
    public static SurveyBook Read(string path)
    {
        var raw = ReadText(path);
        var stations = new List<string>(); var shots = new List<SurveyShot>();
        string? station = null; double hi = 0; int line = 0;
        foreach (var text in raw.Split('\n'))
        {
            line++; if (string.IsNullOrWhiteSpace(text)) continue;
            var m = Regex.Match(text, @"^_'(.+?)_\(E_\)([\d.]+)\s*$");
            if (m.Success)
            {
                station = m.Groups[1].Value;
                if (stations.Contains(station, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"Linha {line}: estação repetida em blocos; separe as campanhas.");
                hi = Number(m.Groups[2].Value); stations.Add(station); continue;
            }
            m = Regex.Match(text.Trim(), @"^_\+(.+?)_ \?\+(\d+)m(\d{7})\+(\d{7})d\+(\d+).*?\*([RV])_,([\d.]+)_?$");
            if (!m.Success || station is null)
                throw new ArgumentException($"Linha {line}: formato não reconhecido. Leitura M21 Topcon; .bat é somente texto, nunca executado. Envie exemplo de outra codificação.");
            var hd = Number(m.Groups[5].Value) / 1000;
            var sd = Number(m.Groups[2].Value) / 1000;
            var z = Dms(m.Groups[3].Value); var hz = Dms(m.Groups[4].Value);
            if (hd <= 0 || sd <= 0 || z > 180 || hz >= 360) throw new ArgumentException($"Linha {line}: distância ou ângulo fora do intervalo.");
            shots.Add(new(station, m.Groups[1].Value, hd, sd, z, hz, hi, Number(m.Groups[7].Value), line));
        }
        if (stations.Count < 2) throw new ArgumentException("São necessárias pelo menos duas estações.");
        return new(path, raw, stations.ToArray(), shots);
    }
    private static double Dms(string text)
    {
        var d = int.Parse(text[..3]); var m = int.Parse(text[3..5]); var s = int.Parse(text[5..]);
        if (m >= 60 || s >= 60) throw new ArgumentException("Ângulo GGGMMSS inválido: " + text);
        return d + m / 60d + s / 3600d;
    }
    public static Dictionary<string, SurveyPoint> Support(string text)
    {
        var result = new Dictionary<string, SurveyPoint>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var a = row.Contains(';') ? row.Trim().Split(';', StringSplitOptions.TrimEntries) : Regex.Split(row.Trim(), @"\s+");
            if (a[0].Equals("Ponto", StringComparison.OrdinalIgnoreCase) || a[0].Equals("Nome", StringComparison.OrdinalIgnoreCase)) continue;
            if (a.Length != 4 && a.Length != 5) throw new ArgumentException("Apoio: use Ponto;N;E;Cota ou Nome Nome N E Cota.");
            if (!result.TryAdd(a[0], new(a[0], Number(a[^3]), Number(a[^2]), Number(a[^1])))) throw new ArgumentException("Apoio repetido: " + a[0]);
        }
        return result;
    }
    public static string[] Route(string text) => Regex.Split(text.Trim(), @"[;>\s]+").Where(x => x.Length > 0).ToArray();
    public static string SuggestedRoute(SurveyBook book)
    {
        var lines=book.Raw.Split('\n');
        string Endpoint(string station,string kind)
        {
            var targets=book.Shots.Where(o=>o.Station==station && lines[o.Line-1].Contains("*"+kind+"_,")).Select(o=>o.Target).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(targets.Length!=1)throw new ArgumentException($"{station}: orientação {kind} ambígua; informe o percurso manualmente.");
            return targets[0];
        }
        return string.Join(" > ",new[]{Endpoint(book.Stations[0],"R")}.Concat(book.Stations).Append(Endpoint(book.Stations[^1],"V")));
    }
    public static Dictionary<string,string> SuggestedAliases(SurveyBook book,string routeText)
    {
        var route=Route(routeText);if(route.Length<4)throw new ArgumentException("Confira o percurso antes de associar nomes.");
        var names=route[1..^1];var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var shot in book.Shots.Where(o=>o.Target.Contains('`')))
        {
            var index=Array.FindIndex(names,n=>n.Equals(shot.Station,StringComparison.OrdinalIgnoreCase));if(index<0)continue;
            var expected=new[]{route[index],route[index+2]};
            var candidates=shot.Target.Split('`').Where(n=>expected.Contains(n,StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(candidates.Length!=1)throw new ArgumentException("Nome composto ambíguo: "+shot.Target+". Confira e informe a associação manualmente.");
            if(result.TryGetValue(shot.Target,out var previous)&&!previous.Equals(candidates[0],StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Associação conflitante: "+shot.Target);
            result[shot.Target]=candidates[0];
        }
        return result;
    }
    private static double Wrap(double a) => ((a + 180) % 360 + 360) % 360 - 180;
    private static double Norm(double a) => (a % 360 + 360) % 360;
    private static double Az(SurveyPoint a, SurveyPoint b)
    {
        if (Math.Sqrt(Math.Pow(b.N-a.N,2)+Math.Pow(b.E-a.E,2)) < 1e-8) throw new ArgumentException("Apoios de orientação coincidentes.");
        return Norm(Math.Atan2(b.E-a.E,b.N-a.N)*180/Math.PI);
    }
    public static TraverseResult Calculate(SurveyBook book, string supportText, string routeText, string aliasesText,
        double factor, double angularLimitSeconds, double precisionDenominator, string system)
    {
        if (!double.IsFinite(factor) || factor <= 0 || !double.IsFinite(angularLimitSeconds) || !double.IsFinite(precisionDenominator) || angularLimitSeconds < 0 || precisionDenominator < 0)
            throw new ArgumentException("Fator positivo; tolerâncias não negativas.");
        var support = Support(supportText); var route = Route(routeText);
        if (route.Length < 4) throw new ArgumentException("Percurso: ré inicial > partida > estações > chegada > vante final.");
        var names = route[1..^1];
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length) throw new ArgumentException("Estações repetidas no percurso não são atendidas nesta versão; separe circuitos.");
        foreach (var name in new[] { route[0], names[0], names[^1], route[^1] })
            if (!support.ContainsKey(name)) throw new ArgumentException("Falta coordenada do apoio: " + name);
        var aliases = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in aliasesText.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var a = row.Trim().Split('=', StringSplitOptions.TrimEntries);
            if (a.Length != 2 || a.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Associação: nome original=nome confirmado.");
            aliases.Add(a[0],a[1]);
        }
        string Target(SurveyShot o) => aliases.GetValueOrDefault(o.Target, o.Target);
        var ambiguous=book.Shots.Select(o=>o.Target).Distinct(StringComparer.OrdinalIgnoreCase).Where(n=>n.Contains('`')&&!aliases.ContainsKey(n)).ToArray();
        if(ambiguous.Length>0)throw new ArgumentException("Confirme as associações dos nomes compostos (original=confirmado): "+string.Join(", ",ambiguous));
        var warnings = new List<string>(); var memory = new StringBuilder("Estação;Alvo;Leituras;Direção média graus;DH média m;Desnível médio m;Amplitude segundos\n");
        (double direction,double distance,double height) Group(string station,string target)
        {
            var group = book.Shots.Where(o => o.Station.Equals(station,StringComparison.OrdinalIgnoreCase) && Target(o).Equals(target,StringComparison.OrdinalIgnoreCase)).ToArray();
            if (group.Length == 0) throw new ArgumentException($"Sem visadas {station} → {target}. Confira percurso e associações de nomes.");
            var origin = group[0].Direction;
            var direction = Norm(origin + group.Average(o => Wrap(o.Direction-origin)));
            var distance = group.Average(o => o.Horizontal)*factor;
            var height = group.Average(o => o.Slope*Math.Cos(o.Zenith*Math.PI/180)+o.HI-o.HR);
            var amplitude = (group.Max(o => Wrap(o.Direction-direction))-group.Min(o => Wrap(o.Direction-direction)))*3600;
            memory.AppendLine(string.Join(";",station,target,group.Length,F(direction),F(distance),F(height),F(amplitude)));
            return (direction,distance,height);
        }
        var back = new (double direction,double distance,double height)[names.Length];
        var fore = new (double direction,double distance,double height)[names.Length];
        var angles = new double[names.Length];
        for (var i=0;i<names.Length;i++)
        {
            back[i]=Group(names[i],route[i]); fore[i]=Group(names[i],route[i+2]);
            angles[i]=Norm(fore[i].direction-back[i].direction);
        }
        var ignored = book.Shots.Count(o => !names.Contains(o.Station,StringComparer.OrdinalIgnoreCase) ||
            !new[]{route[Array.FindIndex(names,n=>n.Equals(o.Station,StringComparison.OrdinalIgnoreCase))],route[Array.FindIndex(names,n=>n.Equals(o.Station,StringComparison.OrdinalIgnoreCase))+2]}.Contains(Target(o),StringComparer.OrdinalIgnoreCase));
        if (ignored>0) warnings.Add($"{ignored} observações fora do percurso não usadas; originais preservados no relatório JSON.");
        var start = support[names[0]]; var end = support[names[^1]];
        var initialAz=Az(start,support[route[0]]); var finalAz=Az(end,support[route[^1]]);
        double current=initialAz;
        for(var i=0;i<angles.Length;i++) current=Norm(current+angles[i]-(i==0?0:180));
        var angular=Wrap(current-finalAz); var correction=-angular/angles.Length;
        memory.AppendLine("Estação;Ângulo observado graus;Correção segundos;Ângulo ajustado graus");
        for(var i=0;i<names.Length;i++)memory.AppendLine(string.Join(";",names[i],F(angles[i]),F(correction*3600),F(angles[i]+correction)));
        if(Math.Abs(angular)>1) throw new ArgumentException("Diferença de orientação acima de 1°. Confira percurso, apoios e ângulos. Bloqueio de incoerência, não tolerância DNIT.");
        var raw = new List<SurveyPoint>{start}; var adjusted = new List<SurveyPoint>{start};
        var legs=new List<(double d,double dn,double de,double dz)>(); current=initialAz;
        memory.AppendLine("Estação;Vante;DH m;Azimute ajustado graus;dN m;dE m;dH m;Diferença DH recíproca m");
        for(var i=0;i<names.Length-1;i++)
        {
            current=Norm(current+angles[i]+correction-(i==0?0:180));
            var d=(fore[i].distance+back[i+1].distance)/2;
            var dz=(fore[i].height-back[i+1].height)/2;
            var dn=d*Math.Cos(current*Math.PI/180);var de=d*Math.Sin(current*Math.PI/180);
            legs.Add((d,dn,de,dz));var previous=raw[^1];raw.Add(new(names[i+1],previous.N+dn,previous.E+de,previous.Z+dz));
            memory.AppendLine(string.Join(";",names[i],names[i+1],F(d),F(current),F(dn),F(de),F(dz),F(fore[i].distance-back[i+1].distance)));
        }
        current=Norm(current+180+angles[^1]+correction);
        if(Math.Abs(Wrap(current-finalAz))>1e-7)throw new ArithmeticException("Falha na conferência angular.");
        var length=legs.Sum(l=>l.d); var en=raw[^1].N-end.N;var ee=raw[^1].E-end.E;var eh=raw[^1].Z-end.Z;
        var linear=Math.Sqrt(en*en+ee*ee); var precision=linear>1e-12?length/linear:double.PositiveInfinity;
        memory.AppendLine("Estação;Vante;Correção N m;Correção E m;Correção H m");
        for(var i=0;i<legs.Count;i++)
        {
            var l=legs[i];var p=adjusted[^1];var cn=-en*l.d/length;var ce=-ee*l.d/length;var cz=-eh*l.d/length;
            adjusted.Add(new(names[i+1],p.N+l.dn+cn,p.E+l.de+ce,p.Z+l.dz+cz));
            memory.AppendLine(string.Join(";",names[i],names[i+1],F(cn),F(ce),F(cz)));
        }
        if(Math.Max(Math.Abs(adjusted[^1].N-end.N),Math.Abs(adjusted[^1].E-end.E))>1e-6 || Math.Abs(adjusted[^1].Z-end.Z)>1e-6)throw new ArithmeticException("Falha de fechamento ajustado.");
        adjusted[^1]=end;
        var angularStatus=angularLimitSeconds>0?(Math.Abs(angular*3600)<=angularLimitSeconds?"ATENDE":"NÃO ATENDE"):"NÃO AVALIADO";
        var linearStatus=precisionDenominator>0?(precision>=precisionDenominator?"ATENDE":"NÃO ATENDE"):"NÃO AVALIADO";
        var report=new StringBuilder("RELATÓRIO DE POLIGONAL — IFM\n");
        report.AppendLine($"Cálculo entre apoios conhecidos: {names[0]} → {names[^1]}. Ré inicial: {route[0]}; orientação final: {names[^1]} → {route[^1]}. A visada final orienta o fechamento; não é necessário um novo bloco de ocupação do ponto inicial.");
        report.AppendLine(factor==1?"Fator 1: sem redução horizontal adicional. O fechamento é calculável, mas a compatibilidade terreno/projeção precisa ser conferida; este valor não é certificação de fator UTM.":"Fator informado pelo usuário aplicado às distâncias; não foi estimado para melhorar o fechamento.");
        report.AppendLine($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\nArquivo: {book.Source}\nSHA256: {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(book.Raw)))}\nPercurso: {string.Join(" > ",route)}\nSistema declarado: {system}\nFator horizontal: {F(factor)}");
        report.AppendLine($"Estações: {names.Length}; lados: {legs.Count}; comprimento: {F(length)} m\nErro angular antes da compensação: {F(angular*3600)} segundos\nCorreção uniforme: {F(correction*3600)} segundos por estação\nErro N após ajuste angular: {F(en)} m\nErro E após ajuste angular: {F(ee)} m\nErro linear após ajuste angular: {F(linear)} m\nPrecisão relativa: 1:{F(precision)}\nErro de cota trigonométrica: {F(eh)} m\nTolerância angular absoluta informada: {F(angularLimitSeconds)} segundos — {angularStatus}\nPrecisão mínima informada: 1:{F(precisionDenominator)} — {linearStatus}");
        report.AppendLine("\nMétodo: médias de direções com passagem por 360°, média de distâncias recíprocas, correção angular uniforme e Bowditch em N/E proporcional aos comprimentos. Cotas trigonometricamente calculadas e compensadas proporcionalmente aos comprimentos. Não é ajuste por mínimos quadrados. Repetições mantidas; faces/séries não identificadas, sem rejeição automática de observações.");
        report.AppendLine("\nReferência: DNIT IPR 726, IS-204, item 2.2.2, página impressa 262 (PDF 264, índice 0). Remete à NBR 13133/94, classe III P ou superior. Os limites informados são critérios do usuário, não limites universais DNIT. Classe, edição contratual, qualidade dos apoios, reduções e demais requisitos devem ser conferidos pelo responsável. Sem declaração automática de conformidade DNIT. Cotas trigonométricas não equivalem a nivelamento geométrico e contranivelamento.\n"+Reference);
        report.AppendLine("\nNão transforma datum/projeção; fator multiplica apenas DH. A ré e a vante aqui são observações recíprocas dos lados, não comprovam duas campanhas independentes de ida e volta. Não usa automaticamente os demais pontos de apoio como injunções intermediárias.");
        report.AppendLine("\nAssociações de nomes explicitamente informadas:\n"+aliasesText);
        foreach(var w in warnings)report.AppendLine(w);
        return new(raw,adjusted,report.ToString(),memory.ToString(),angular*3600,linear);
    }
    public static string F(double n) => n.ToString("F9", CultureInfo.GetCultureInfo("pt-BR"));
}
