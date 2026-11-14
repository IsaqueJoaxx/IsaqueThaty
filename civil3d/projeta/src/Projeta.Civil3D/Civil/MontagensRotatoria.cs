using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivAssembly = Autodesk.Civil.DatabaseServices.Assembly;
using CivPonto = Autodesk.Civil.DatabaseServices.Point;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Montagens (assemblies) do corredor da rotatória, feitas com subassemblies padrão do Civil 3D (catálogo métrico):
/// faixa com camadas (LaneSuperelevationAOR, sem superelevação, caimento fixo), meio-fio com sarjeta (BasicCurbAndGutter),
/// passeio (UrbanSidewalk), talude até o terreno (DaylightGeneral), meio-fio da ilha central (BasicCurb), faixa transponível
/// e ilha divisória (links). Os valores vêm da ConfigRotatoria e podem ser trocados depois nas propriedades de cada peça.
/// </summary>
internal static class MontagensRotatoria
{
    internal sealed record Peca(string Classe, string Nome, Action<Subassembly> Ajustar);

    internal sealed record Definicao(string Nome, AssemblyType Tipo, string Uso, IReadOnlyList<Peca> Direita, IReadOnlyList<Peca> Esquerda);

    internal sealed record Criada(string Nome, string Tipo, ObjectId Id, string Uso, int Pecas, int Falhas, Point3d Local);

    // ---------- parâmetros ----------
    private static void D(Subassembly sa, string p, double v)
    {
        try { sa.ParamsDouble[p].Value = v; }
        catch (System.Exception e) { throw new InvalidOperationException($"parâmetro {p}: {e.Message}"); }
    }

    private static void L(Subassembly sa, string p, int v)
    {
        try { sa.ParamsLong[p].Value = v; }
        catch (System.Exception e) { throw new InvalidOperationException($"parâmetro {p}: {e.Message}"); }
    }

    private static void S(Subassembly sa, string p, string v)
    {
        try { sa.ParamsString[p].Value = v; }
        catch (System.Exception e) { throw new InvalidOperationException($"parâmetro {p}: {e.Message}"); }
    }

    // ---------- peças ----------
    /// <summary>Faixa com revestimento, base e sub-base; caimento fixo (negativo = desce para fora do eixo).</summary>
    private static Peca Faixa(string nome, double largura, double caimento, ConfigRotatoria c) =>
        new("LaneSuperelevationAOR", nome, sa =>
        {
            D(sa, "Width", largura);
            L(sa, "UseSuperelevation", 6);   // None: usa o caimento padrão
            L(sa, "SlopeDirection", 0);      // AwayFromCrown
            D(sa, "DefaultSlope", caimento);
            D(sa, "Pave1Depth", c.Revestimento);
            D(sa, "Pave2Depth", 0);
            D(sa, "BaseDepth", c.Base);
            D(sa, "SubBaseDepth", c.SubBase);
        });

    private static Peca MeioFio(ConfigRotatoria c) =>
        new("BasicCurbAndGutter", "Meio-fio e sarjeta", sa =>
        {
            L(sa, "InsertionPoint", 1);      // GutterEdge: encosta no bordo da pista
            D(sa, "GutterWidth", c.SarjetaLargura);
            D(sa, "GutterSlope", c.SarjetaCaimento);
            D(sa, "CurbHeight", c.MeioFioAltura);
            D(sa, "CurbWidth", c.MeioFioLargura);
            D(sa, "CurbDepth", c.MeioFioProfundidade);
        });

    private static Peca Passeio(ConfigRotatoria c) =>
        new("UrbanSidewalk", "Passeio", sa =>
        {
            D(sa, "InsideBoulevardWidth", 0);
            D(sa, "SidewalkWidth", c.Passeio);
            D(sa, "OutsideBoulevardWidth", 0);
            D(sa, "Slope", c.PasseioCaimento);
            D(sa, "Depth", c.PasseioEspessura);
        });

    private static Peca Talude(ConfigRotatoria c) =>
        new("DaylightGeneral", "Talude", sa =>
        {
            double corte = 1 / Math.Max(0.1, c.TaludeCorte), aterro = 1 / Math.Max(0.1, c.TaludeAterro);
            foreach (var p in new[] { "FlatCutSlope", "MediumCutSlope", "SteepCutSlope" }) D(sa, p, corte);
            foreach (var p in new[] { "FlatFillSlope", "MediumFillSlope", "SteepFillSlope" }) D(sa, p, aterro);
            L(sa, "IncludeGuardrail", 0);
            L(sa, "PlaceMaterial", 3);       // None
        });

    /// <summary>Meio-fio da ilha central, encostado pela face no bordo interno da pista anelar (sem sarjeta: a água corre para fora).</summary>
    private static Peca MeioFioIlha(ConfigRotatoria c) =>
        new("BasicCurb", "Meio-fio da ilha central", sa =>
        {
            L(sa, "AttachmentPoint", 1);     // Front
            D(sa, "Width", c.MeioFioLargura);
            D(sa, "Depth", c.MeioFioProfundidade);
            D(sa, "DepthBelow", Math.Max(0, c.MeioFioProfundidade - c.MeioFioAltura));
            D(sa, "BackHeight", 0);
        });

    private static Peca Apron(ConfigRotatoria c) =>
        new("LinkWidthAndSlope", "Faixa transponível", sa =>
        {
            D(sa, "Width", c.Apron);
            L(sa, "UseSuperelevationSlope", 0);
            D(sa, "Slope", 0.02);
            S(sa, "PointCodes", "Faixa_Transponivel");
        });

    /// <summary>Meia ilha divisória: sobe a altura do meio-fio no eixo (sem link), topo da ilha até o bordo e desce na face.</summary>
    private static IEnumerable<Peca> Ilha(ConfigRotatoria c, double meiaIlha)
    {
        yield return new("LinkVertical", "Ilha divisória - subida", sa =>
        {
            D(sa, "VerticalDeflection", c.MeioFioAltura);
            L(sa, "OmitLink", 1);
        });
        yield return new("LinkWidthAndSlope", "Ilha divisória - topo", sa =>
        {
            D(sa, "Width", meiaIlha);
            L(sa, "UseSuperelevationSlope", 0);
            D(sa, "Slope", 0);
            S(sa, "PointCodes", "Ilha_Divisoria");
        });
        yield return new("LinkVertical", "Ilha divisória - face", sa =>
        {
            D(sa, "VerticalDeflection", -c.MeioFioAltura);
            S(sa, "PointCodes", "Ilha_Divisoria_Face");
        });
    }

    // ---------- montagens ----------
    public static List<Definicao> Definir(ConfigRotatoria c)
    {
        double cai = c.Caimento, meiaAnel = c.LarguraAnel / 2;
        bool comIlha = c.IlhaLargura > 0 && c.IlhaComprimento > 0;
        List<Peca> ComMeioFio(Peca faixa) => new() { faixa, MeioFio(c), Passeio(c), Talude(c) };
        List<Peca> Interno()
        {
            var l = new List<Peca> { Faixa("Pista anelar - metade interna", meiaAnel, -cai, c) };
            if (c.Apron > 0) l.Add(Apron(c));
            l.Add(MeioFioIlha(c));
            return l;
        }

        var defs = new List<Definicao>
        {
            new("Anel - pista, meio-fio e passeio", AssemblyType.UndividedPlanarRoad,
                "eixo do anel, nos trechos entre os braços (onde o bordo externo tem meio-fio); o anel é anti-horário: direita = fora, esquerda = ilha central",
                ComMeioFio(Faixa("Pista anelar - metade externa", meiaAnel, cai, c)), Interno()),
            new("Anel - só pista", AssemblyType.UndividedPlanarRoad,
                "eixo do anel, nas aberturas dos braços (entre as duas conexões de cada braço)",
                new List<Peca> { Faixa("Pista anelar - metade externa", meiaAnel, cai, c) }, Interno()),
            new("Pista completa", AssemblyType.UndividedCrownedRoad,
                "eixo de cada ramo longe da rotatória; no alargamento, alvo de largura das faixas nos bordos (camada PRJ-ROTATORIA-BORDO)",
                ComMeioFio(Faixa("Faixa", c.MeiaLarguraRamo, cai, c)), ComMeioFio(Faixa("Faixa", c.MeiaLarguraRamo, cai, c))),
        };
        {
            var ilhaD = comIlha ? Ilha(c, c.IlhaLargura / 2).ToList() : new List<Peca>();
            var ilhaE = comIlha ? Ilha(c, c.IlhaLargura / 2).ToList() : new List<Peca>();
            defs.Add(new("Ramo junto do anel - só pista", comIlha ? AssemblyType.DividedCrownedRoad : AssemblyType.UndividedCrownedRoad,
                "eixo do ramo entre a borda do anel e as tangências das conexões (ali o meio-fio é o das conexões); alvo de largura das faixas nos eixos das conexões",
                ilhaD.Append(Faixa("Faixa de entrada", c.FaixaEntrada, cai, c)).ToList(),
                ilhaE.Append(Faixa("Faixa de saída", c.FaixaSaida, cai, c)).ToList()));
        }
        if (comIlha)
        {
            double meiaIlha = c.IlhaLargura / 2;
            defs.Add(new("Ramo com ilha divisória", AssemblyType.DividedCrownedRoad,
                "eixo do ramo no trecho da ilha divisória; direita = entrada quando o eixo aponta para a rotatória (se apontar para fora, troque as larguras); a ilha afina até o nariz: use alvo de largura",
                Ilha(c, meiaIlha).Append(Faixa("Faixa de entrada", c.FaixaEntrada, cai, c)).Concat(new[] { MeioFio(c), Passeio(c), Talude(c) }).ToList(),
                Ilha(c, meiaIlha).Append(Faixa("Faixa de saída", c.FaixaSaida, cai, c)).Concat(new[] { MeioFio(c), Passeio(c), Talude(c) }).ToList()));
        }
        foreach (var (lado, direita) in new[] { ("direita", true), ("esquerda", false) })
        {
            var vazio = new List<Peca>();
            var so = new List<Peca> { Faixa("Faixa", c.FaixaEntrada, cai, c) };
            defs.Add(new($"Meia pista - {lado}", AssemblyType.Other,
                $"faixa sem meio-fio, à {lado} do eixo (pista que continua em outro corredor); alvo de largura no bordo",
                direita ? so : vazio, direita ? vazio : so));
            var mf = ComMeioFio(Faixa("Faixa", c.FaixaEntrada, cai, c));
            defs.Add(new($"Meia pista com meio-fio e passeio - {lado}", AssemblyType.Other,
                $"faixa, meio-fio, passeio e talude à {lado} do eixo",
                direita ? mf : vazio, direita ? vazio : mf));
            var fora = new List<Peca> { MeioFio(c), Passeio(c), Talude(c) };
            var dentro = new List<Peca> { Faixa("Faixa até o anel ou o ramo", c.FaixaEntrada, -cai, c) };
            defs.Add(new($"Conexão - passeio à {lado}", AssemblyType.Other,
                $"eixo da conexão (no bordo da pista): meio-fio, passeio e talude à {lado}, faixa subindo para o outro lado com alvo de largura no eixo do anel ou do ramo",
                direita ? fora : dentro, direita ? dentro : fora));
        }
        return defs;
    }

    /// <summary>
    /// Montagens da interseção em nível: para cada eixo, pista completa (faixas, meio-fio, passeio e talude dos dois lados) e
    /// pista só com as faixas (dentro da interseção, onde o meio-fio é o das esquinas); e a esquina (meio-fio no eixo da
    /// curva, passeio para fora, faixa até o bordo das vias).
    /// </summary>
    public static List<Definicao> DefinirIntersecao(ConfigRotatoria c, IEnumerable<(string Eixo, double MeiaLargura)> eixos, bool canalizada = false,
        (string Eixo, double MeiaLargura, string Lado)? continuo = null)
    {
        double cai = c.Caimento;
        List<Peca> ComMeioFio(Peca faixa) => new() { faixa, MeioFio(c), Passeio(c), Talude(c) };
        var defs = new List<Definicao>();
        foreach (var (eixo, w) in eixos)
        {
            defs.Add(new($"Pista completa - {eixo}", AssemblyType.UndividedCrownedRoad,
                $"'{eixo}' fora da interseção, a partir das tangências das esquinas",
                ComMeioFio(Faixa("Faixa", w, cai, c)), ComMeioFio(Faixa("Faixa", w, cai, c))));
            defs.Add(new($"Pista só faixas - {eixo}", AssemblyType.UndividedCrownedRoad,
                $"'{eixo}' dentro da interseção (o meio-fio ali é o das esquinas)",
                new List<Peca> { Faixa("Faixa", w, cai, c) }, new List<Peca> { Faixa("Faixa", w, cai, c) }));
        }
        if (continuo is { } ct)
        {
            bool dir = ct.Lado == "direita";
            var mf = ComMeioFio(Faixa("Faixa", ct.MeiaLargura, cai, c));
            var so = new List<Peca> { Faixa("Faixa", ct.MeiaLargura, cai, c) };
            defs.Add(new($"Pista só faixas com meio-fio à {ct.Lado} - {ct.Eixo}", AssemblyType.UndividedCrownedRoad,
                $"'{ct.Eixo}' dentro da interseção em T: faixas dos dois lados; meio-fio, passeio e talude só à {ct.Lado} (o lado sem via transversal)",
                dir ? mf : so, dir ? so : mf));
        }
        foreach (var (lado, direita) in new[] { ("direita", true), ("esquerda", false) })
        {
            var fora = new List<Peca> { MeioFio(c), Passeio(c), Talude(c) };
            var dentro = new List<Peca> { Faixa("Faixa até o bordo das vias", c.MeiaLarguraRamo, -cai, c) };
            defs.Add(new($"Esquina - passeio à {lado}", AssemblyType.Other,
                $"eixo da esquina (no bordo da pista): meio-fio, passeio e talude à {lado}, faixa até o bordo das vias (alvo mais perto)",
                direita ? fora : dentro, direita ? dentro : fora));
            if (!canalizada) continue;
            var giro = new List<Peca> { Faixa("Faixa do giro até a ilha", c.CanalLargura, -cai, c), MeioFioIlhaCanal(c) };
            defs.Add(new($"Esquina canalizada - passeio à {lado}", AssemblyType.Other,
                $"eixo da esquina no trecho da ilha canalizadora: meio-fio, passeio e talude à {lado}; do outro lado a pista de conversão até a ilha (alvo no contorno da ilha) e o meio-fio da ilha",
                direita ? MeioFioPasseioTalude(c) : giro, direita ? giro : MeioFioPasseioTalude(c)));
        }
        return defs;
    }

    private static List<Peca> MeioFioPasseioTalude(ConfigRotatoria c) => new() { MeioFio(c), Passeio(c), Talude(c) };

    /// <summary>Meio-fio da ilha canalizadora, encostado pela face no bordo da pista de conversão.</summary>
    private static Peca MeioFioIlhaCanal(ConfigRotatoria c) =>
        new("BasicCurb", "Meio-fio da ilha canalizadora", sa =>
        {
            L(sa, "AttachmentPoint", 1);     // Front
            D(sa, "Width", c.MeioFioLargura);
            D(sa, "Depth", c.MeioFioProfundidade);
            D(sa, "DepthBelow", Math.Max(0, c.MeioFioProfundidade - c.MeioFioAltura));
            D(sa, "BackHeight", 0);
        });

    /// <summary>Cria as montagens lado a lado a partir de <paramref name="origem"/> (uma a cada <paramref name="passo"/> m em X).</summary>
    public static List<Criada> Criar(CivilDocument civ, Transaction tr, string prefixo, IReadOnlyList<Definicao> defs, Point3d origem, double passo, List<string> avisos)
    {
        var res = new List<Criada>();
        var nomesMont = Nomes(tr, civ.AssemblyCollection.Cast<ObjectId>(), id => ((CivAssembly)tr.GetObject(id, OpenMode.ForRead)).Name);
        var nomesSub = Nomes(tr, civ.SubassemblyCollection.Cast<ObjectId>(), id => ((Subassembly)tr.GetObject(id, OpenMode.ForRead)).Name);
        for (int k = 0; k < defs.Count; k++)
        {
            var d = defs[k];
            string nome = Unico($"{prefixo} - {d.Nome}", nomesMont);
            var loc = new Point3d(origem.X + k * passo, origem.Y, 0);
            ObjectId aid;
            try { aid = civ.AssemblyCollection.Add(nome, d.Tipo, loc); }
            catch (System.Exception e) { avisos.Add($"A montagem '{nome}' não foi criada: {e.Message}"); continue; }
            var asm = (CivAssembly)tr.GetObject(aid, OpenMode.ForWrite);
            int n = 0, falhas = 0;
            foreach (var (pecas, esquerda) in new[] { (d.Direita, false), (d.Esquerda, true) })
            {
                Subassembly? ant = null;
                string antClasse = "";
                foreach (var p in pecas)
                {
                    string ns = Unico($"{nome} - {p.Nome} ({(esquerda ? "E" : "D")})", nomesSub);
                    try
                    {
                        var sid = civ.SubassemblyCollection.ImportStockSubassembly(ns, "Subassembly." + p.Classe, loc);
                        var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForWrite);
                        Lado(sa, esquerda);
                        p.Ajustar(sa);
                        if (ant is null) asm.AddSubassembly(sid);
                        else asm.AddSubassembly(sid, Engate(ant, antClasse, esquerda));
                        ant = sa;
                        antClasse = p.Classe;
                        n++;
                    }
                    catch (System.Exception e)
                    {
                        falhas++;
                        avisos.Add($"Montagem '{nome}': a peça '{p.Nome}' ({p.Classe}) não entrou: {e.Message}");
                        break;
                    }
                }
            }
            res.Add(new Criada(nome, d.Nome, aid, d.Uso, n, falhas, loc));
        }
        return res;
    }

    private static void Lado(Subassembly sa, bool esquerda)
    {
        try { if (sa.HasSide) sa.Side = esquerda ? SubassemblySideType.Left : SubassemblySideType.Right; } catch { }
        try { sa.ParamsLong["Side"].Value = esquerda ? 1 : 0; } catch { }
    }

    /// <summary>
    /// Ponto da peça anterior onde a próxima engata: nos links, o ponto final; nas demais, o ponto mais afastado do eixo
    /// e, entre os de mesmo afastamento, o mais alto (bordo da faixa, topo do meio-fio, fim do passeio).
    /// </summary>
    private static CivPonto Engate(Subassembly ant, string classe, bool esquerda)
    {
        var pts = ant.Points.Cast<CivPonto>().ToList();
        if (pts.Count == 0) throw new InvalidOperationException("a peça anterior não tem pontos");
        if (classe.StartsWith("Link", StringComparison.Ordinal)) return pts[^1];
        double sg = esquerda ? -1 : 1;
        double max = pts.Max(p => sg * p.Offset);
        return pts.Where(p => sg * p.Offset >= max - 1e-4).OrderByDescending(p => p.Elevation).First();
    }

    private static HashSet<string> Nomes(Transaction tr, IEnumerable<ObjectId> ids, Func<ObjectId, string> nome)
    {
        var h = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids) try { h.Add(nome(id)); } catch { }
        return h;
    }

    private static string Unico(string nome, HashSet<string> usados)
    {
        string n = nome;
        for (int i = 2; usados.Contains(n); i++) n = $"{nome} ({i})";
        usados.Add(n);
        return n;
    }

    /// <summary>Descrição das peças de uma montagem (para conferência e teste).</summary>
    public static string Descrever(Transaction tr, ObjectId asmId)
    {
        var sb = new System.Text.StringBuilder();
        var asm = (CivAssembly)tr.GetObject(asmId, OpenMode.ForRead);
        sb.AppendLine($"MONTAGEM {asm.Name} ({asm.Type}) grupos {asm.Groups.Count}");
        foreach (AssemblyGroup g in asm.Groups)
            foreach (ObjectId sid in g.GetSubassemblyIds())
            {
                var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead);
                sb.Append($"  {sa.Name} [{(sa.HasSide ? sa.Side.ToString() : "-")}] status {sa.Status}:");
                foreach (CivPonto p in sa.Points) sb.Append(System.FormattableString.Invariant($" P{p.Index}({p.Offset:0.000};{p.Elevation:0.000})"));
                sb.AppendLine();
            }
        return sb.ToString();
    }
}
