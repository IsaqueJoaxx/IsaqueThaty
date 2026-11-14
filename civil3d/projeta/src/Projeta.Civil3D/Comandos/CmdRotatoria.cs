using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Alinhamentos;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Enc = Projeta.Civil3D.Civil.RotatoriaCivil.Encontro;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// Rotatórias. PRJCASAMENTO — confere se dois (ou vários) alinhamentos casam: se a ponta encosta no eixo, se é tangente,
/// se as cotas e as rampas dos greides batem no encontro; marca verde/vermelho no desenho e oferece acertar a cota da ponta.
/// PRJCONECTAREIXO — clica num alinhamento, clica no outro e cria a conexão (arco tangente aos dois, nos eixos ou nos
/// bordos) já com o greide casando com os dois.
/// </summary>
public sealed partial class CmdRotatoria
{
    internal const string LayerMarcas = "PRJ-CASAMENTO";

    /// <summary>Greide escolhido por alinhamento nesta sessão do Civil (nome do alinhamento → nome do greide).</summary>
    private static readonly Dictionary<string, string> GreideEscolhido = new(StringComparer.OrdinalIgnoreCase);

    // ================================================================== PRJCASAMENTO

    [CommandMethod("PRJCASAMENTO", CommandFlags.Modal)]
    public static void Casamento()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            List<ObjectId>? ids = null;
            while (ids is null)
            {
                var c1 = PedirAlinhamento(ed, "\nClique no 1º alinhamento", "Varios", "Tolerancias");
                if (c1 is null) return;
                if (c1.Palavra == "Tolerancias") { if (!PedirTolerancias(ed, cfg)) return; continue; }
                if (c1.Palavra == "Varios")
                {
                    var f = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT") });
                    var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os alinhamentos da rotatória (anel, ramos, conexões): " }, f);
                    if (ps.Status != PromptStatus.OK) return;
                    ids = ps.Value.GetObjectIds().ToList();
                    if (ids.Count < 2) { Ctx.Msg("Selecione pelo menos 2 alinhamentos."); return; }
                    break;
                }
                var c2 = PedirAlinhamento(ed, "\nClique no 2º alinhamento: ");
                if (c2 is null) return;
                if (c2.Id == c1.Id) { Ctx.Msg("É o mesmo alinhamento. Clique em dois diferentes."); continue; }
                ids = new List<ObjectId> { c1.Id, c2.Id };
            }
            ConfigRotatoria.Salvar(cfg);

            using var s = Sessao.Abrir();
            var als = ids.Select(id => (Alignment)s.Tr.GetObject(id, OpenMode.ForRead)).ToList();
            var encontros = new List<Enc>();
            for (int i = 0; i < als.Count; i++)
                for (int j = i + 1; j < als.Count; j++)
                    encontros.AddRange(RotatoriaCivil.Encontros(s.Tr, als[i], als[j], cfg.Busca));

            if (encontros.Count == 0)
            {
                if (als.Count == 2)
                    Ctx.Msg($"'{als[0].Name}' e '{als[1].Name}' NÃO se encontram: a ponta mais perto fica a {RotatoriaCivil.MenorDistancia(als[0], als[1]):0.000} m do outro eixo " +
                            $"(procuro encontros até {cfg.Busca:0.##} m; mude em Tolerancias). Prolongue o eixo ou use o PRJCONECTAREIXO.");
                else Ctx.Msg($"Nenhum encontro entre os {als.Count} alinhamentos (até {cfg.Busca:0.##} m).");
                s.Tr.Commit();
                return;
            }

            // ponta oblíqua que só passa perto de outro eixo (nem encosta nele nem corre junto do bordo dele) não é encontro
            // e ponta com ponta só é emenda quando as duas pontas estão juntas (ou quase, e em linha): duas conexões que
            // começam perto uma da outra no bordo do anel não estão ligadas entre si
            int ignorados = encontros.RemoveAll(e => (e.EhPonta && Math.Abs(e.Afastamento) > TolFalta && !Tangente(e))
                || (e.Tipo == "ponta com ponta" && (Math.Abs(e.Afastamento) > TolFalta || (!Tangente(e) && Math.Abs(e.Afastamento) > cfg.TolPlanta))));
            string sobreIgnorados = ignorados > 0 ? $" {ignorados} ponta(s) que só passam perto de outro eixo (a mais de {TolFalta:0.0} m e oblíquas) não contam como encontro." : "";
            if (encontros.Count == 0)
            {
                Ctx.Msg($"Nenhum encontro entre os alinhamentos.{sobreIgnorados}");
                s.Tr.Commit();
                return;
            }

            // greide de cada alinhamento envolvido
            var greides = new Dictionary<ObjectId, Profile?>();
            foreach (var al in encontros.SelectMany(e => new[] { e.Principal, e.Ponta }).DistinctBy(a => a.ObjectId))
                greides[al.ObjectId] = EscolherGreide(s, al);

            double maxFora = encontros.Where(e => NoBordo(e) && greides[e.Principal.ObjectId] is not null && greides[e.Ponta.ObjectId] is not null)
                                      .Select(e => Math.Abs(e.Afastamento)).DefaultIfEmpty(0).Max();
            if (maxFora > 0)
            {
                var c = s.PedirNumero($"Há encontro fora do eixo (até {maxFora:0.00} m, no bordo?). Caimento transversal do eixo até lá (%; negativo = desce)", cfg.Caimento * 100, true);
                if (c is null) return;
                cfg.Caimento = c.Value / 100;
                ConfigRotatoria.Salvar(cfg);
            }

            // avaliação
            var lay = Ctx.GarantirLayer(s.Tr, s.Db, LayerMarcas, 3);
            int apagadas = ApagarMarcas(s.Tr, s.Db, lay);
            double h = Math.Clamp(als.Max(a => a.EndingStation - a.StartingStation) / 120, 0.3, 3);
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            var ajustes = new List<(Enc E, Profile Greide, double Cota)>();
            int ok = 0, ruins = 0, semGreide = 0, n = 0;
            foreach (var e in encontros)
            {
                n++;
                var problemas = new List<string>();
                var linhas = new List<string>();
                double ang = RotatoriaCivil.Angulo(e);
                double desvio = Math.Min(ang, Math.PI - ang) * 180 / Math.PI;
                double fora = Math.Abs(e.Afastamento);
                string titulo = e.Tipo == "cruzamento"
                    ? $"cruzamento de '{e.Principal.Name}' (est. {RotatoriaCivil.Est(e.SPrincipal)}) com '{e.Ponta.Name}' (est. {RotatoriaCivil.Est(e.SPonta)})"
                    : $"{e.Tipo} de '{e.Ponta.Name}' (est. {RotatoriaCivil.Est(e.SPonta)}) em '{e.Principal.Name}' (est. {RotatoriaCivil.Est(e.SPrincipal)})";
                Ctx.Msg($"Encontro {n} — {titulo}");

                // planta
                string planta;
                bool bordo = NoBordo(e);
                if (fora <= cfg.TolPlanta) planta = $"encosta no eixo ({fora:0.000} m)";
                else if (bordo) planta = $"liga no bordo, a {fora:0.000} m do eixo de '{e.Principal.Name}' ({(e.Afastamento > 0 ? "direita" : "esquerda")})";
                else { planta = $"NÃO ENCOSTA: falta {fora:0.000} m até o eixo de '{e.Principal.Name}'"; problemas.Add($"falta {fora:0.000} m"); }
                if (e.EhPonta && desvio > 0.01 && desvio < 15) { planta += $"; quebra de {desvio:0.00}° (não é tangente)"; problemas.Add($"quebra {desvio:0.0}°"); }
                else planta += $"; ângulo entre os eixos {ang * 180 / Math.PI:0.0}°";
                linhas.Add("  planta: " + planta);

                // cotas e rampas
                var gp = greides[e.Principal.ObjectId];
                var gt = greides[e.Ponta.ObjectId];
                var cp = gp is null ? null : RotatoriaCivil.CotaRampa(gp, RotatoriaCivil.Ajustar(e.Principal, e.SPrincipal));
                var ct = gt is null ? null : RotatoriaCivil.CotaRampa(gt, RotatoriaCivil.Ajustar(e.Ponta, e.SPonta));
                string rotulo;
                if (cp is null || ct is null)
                {
                    string falta = gp is null ? $"'{e.Principal.Name}' não tem greide de projeto"
                                 : cp is null ? $"o greide '{gp.Name}' não chega na estaca {RotatoriaCivil.Est(e.SPrincipal)}"
                                 : gt is null ? $"'{e.Ponta.Name}' não tem greide de projeto"
                                 : $"o greide '{gt.Name}' não chega na estaca {RotatoriaCivil.Est(e.SPonta)}";
                    linhas.Add($"  cotas: não conferidas — {falta}");
                    rotulo = "sem greide";
                    if (problemas.Count == 0) semGreide++;
                }
                else
                {
                    var av = Projeta.Core.Alinhamentos.Casamento.Avaliar(cp.Value.Z, cp.Value.G, ct.Value.Z, ct.Value.G, ang, bordo ? fora : 0, cfg.Caimento, cfg.TolCota, cfg.TolRampa);
                    string ref_ = bordo ? $"{av.CotaAAjustada:0.000} (eixo {cp.Value.Z:0.000} {Pct(cfg.Caimento, 1)} × {fora:0.00} m)" : $"{cp.Value.Z:0.000}";
                    linhas.Add($"  cotas: '{gp!.Name}' {ref_} | '{gt!.Name}' {ct.Value.Z:0.000} → diferença {Sn(av.Dz, 3)} m  {(av.CotaCasa ? "casa" : "NÃO CASA")}");
                    if (!av.CotaCasa) problemas.Add($"Δz {Sn(av.Dz, 3)}");
                    if (av.RampaComparavel)
                    {
                        linhas.Add($"  rampas: '{gp.Name}' na direção do outro {Pct(av.RampaANaDirecaoB)} | '{gt.Name}' {Pct(ct.Value.G)} → diferença {Pct(av.Di)}  {(av.RampaCasa ? "casa" : "NÃO CASA")}");
                        if (!av.RampaCasa) problemas.Add($"Δi {Pct(av.Di, 1)}");
                    }
                    else linhas.Add("  rampas: não comparadas (encontro quase perpendicular; cada eixo tem a sua)");
                    rotulo = av.CotaCasa ? "casa" : $"Δz {Sn(av.Dz, 3)}";
                    // acerto possível: ponta de um greide editável com PIV na estaca do encontro
                    if (!av.CotaCasa && e.EhPonta && (fora <= cfg.TolPlanta || bordo) && PodeEditar(gt) is null && AcharPVI(gt, RotatoriaCivil.Ajustar(e.Ponta, e.SPonta)) is not null)
                        ajustes.Add((e, gt, av.CotaAAjustada));
                }
                foreach (var l in linhas) Ctx.Msg(l);
                bool bom = problemas.Count == 0 && rotulo != "sem greide";
                if (bom) ok++; else if (problemas.Count > 0) ruins++;
                Ctx.Msg(problemas.Count > 0 ? $"  => NÃO CASA ({string.Join("; ", problemas)})" : bom ? "  => casa" : "  => planta ok, cotas não conferidas");

                short cor = problemas.Count > 0 ? (short)1 : bom ? (short)3 : (short)2;
                var circ = new Circle(e.P, Vector3d.ZAxis, h) { Layer = lay, ColorIndex = cor };
                Ctx.Adicionar(s.Tr, ms, circ);
                var txt = Ctx.Texto(problemas.Count > 0 ? string.Join("\\P", problemas) : rotulo, new Point3d(e.P.X + h * 1.2, e.P.Y + h * 1.2, 0), h * 0.5);
                txt.Layer = lay; txt.ColorIndex = cor;
                Ctx.Adicionar(s.Tr, ms, txt);
            }
            Ctx.Msg($"{encontros.Count} encontro(s): {ok} casa(m), {ruins} não casa(m){(semGreide > 0 ? $", {semGreide} sem greide para conferir a cota" : "")}. " +
                    $"Marcas na camada {LayerMarcas} (verde casa, vermelho não casa, amarelo sem greide){(apagadas > 0 ? "; as marcas da conferência anterior foram apagadas" : "")}.{sobreIgnorados}");

            // acertar as cotas das pontas
            int feitos = 0;
            foreach (var (e, g, z) in ajustes)
            {
                var pvi = AcharPVI(g, RotatoriaCivil.Ajustar(e.Ponta, e.SPonta))!;
                var resp = s.PedirPalavra($"Acertar a cota da {e.Tipo} de '{g.Name}' de {pvi.Elevation:0.000} para {z:0.000} (casar com '{e.Principal.Name}')", new[] { "Sim", "Nao" }, "Nao");
                if (resp is null) break;
                if (resp != "Sim") continue;
                g.UpgradeOpen();
                pvi.Elevation = z;
                feitos++;
                Ctx.Msg($"  cota do PIV da estaca {RotatoriaCivil.Est(pvi.RawStation)} de '{g.Name}' = {z:0.000}.");
            }
            s.Tr.Commit();
            if (feitos > 0) Ctx.Msg($"{feitos} cota(s) acertada(s). Rode o PRJCASAMENTO de novo para conferir as rampas; se um corredor usa o greide, reconstrua o corredor. U desfaz.");
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJCASAMENTO: {ex.Message}"); }
    }

    /// <summary>Número com sinal (+/−), sem o "-0" de valores que arredondam para zero.</summary>
    private static string Sn(double v, int casas) { double r = Math.Round(v, casas); if (r == 0) r = 0; return (r > 0 ? "+" : "") + r.ToString("F" + casas); }

    private static string Pct(double fracao, int casas = 2) => Sn(fracao * 100, casas) + "%";

    /// <summary>Até onde uma ponta oblíqua que não encosta conta como "falta X m" (além disso é só um eixo passando perto).</summary>
    private const double TolFalta = 1.5;

    private static bool Tangente(Enc e) { double a = RotatoriaCivil.Angulo(e); return Math.Min(a, Math.PI - a) <= 15 * Math.PI / 180; }

    /// <summary>Ponta que corre junto do eixo do outro, afastada (conexão encostada no bordo).</summary>
    private static bool NoBordo(Enc e) => e.EhPonta && Math.Abs(e.Afastamento) > 0.30 && Tangente(e);

    private static bool PedirTolerancias(Editor ed, ConfigRotatoria cfg)
    {
        double? Num(string m, double v) { var r = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = false, AllowZero = false, DefaultValue = v, UseDefaultValue = true }); return r.Status == PromptStatus.OK ? r.Value : r.Status == PromptStatus.None ? v : null; }
        if (Num("Procurar encontros até (m) do outro eixo", cfg.Busca) is not { } b) return false;
        if (Num("Tolerância em planta (m)", cfg.TolPlanta) is not { } tp) return false;
        if (Num("Tolerância de cota (m)", cfg.TolCota) is not { } tc) return false;
        if (Num("Tolerância de rampa (%)", cfg.TolRampa * 100) is not { } tr) return false;
        cfg.Busca = b; cfg.TolPlanta = tp; cfg.TolCota = tc; cfg.TolRampa = tr / 100;
        ConfigRotatoria.Salvar(cfg);
        return true;
    }

    private static int ApagarMarcas(Transaction tr, Database db, string layer)
    {
        var apagar = new List<Entity>();
        foreach (ObjectId id in Ctx.EspacoModelo(tr, db, OpenMode.ForRead))
            if (tr.GetObject(id, OpenMode.ForRead) is Entity en && en.Layer == layer && (en is Circle || en is MText)) apagar.Add(en);
        foreach (var en in apagar) { en.UpgradeOpen(); en.Erase(); }
        return apagar.Count;
    }

    /// <summary>O greide de projeto do alinhamento: o único, ou o escolhido (lembrado nesta sessão).</summary>
    private static Profile? EscolherGreide(Sessao s, Alignment al)
    {
        var gs = RotatoriaCivil.Greides(s.Tr, al);
        if (gs.Count <= 1) return gs.FirstOrDefault();
        if (GreideEscolhido.TryGetValue(al.Name, out var nome) && gs.FirstOrDefault(g => g.Name == nome) is { } lembrado) return lembrado;
        int padrao = Math.Max(0, gs.FindIndex(g => g.ProfileType == ProfileType.FG));
        int i = s.EscolherDaLista($"'{al.Name}' tem {gs.Count} greides — qual é o de projeto?", gs.Select(g => $"{g.Name} ({g.ProfileType})").ToList(), padrao);
        if (i < 0) return null;
        GreideEscolhido[al.Name] = gs[i].Name;
        return gs[i];
    }

    private static string? PodeEditar(Profile g) =>
        g.IsReferenceObject ? "vem de atalho de dados" : g.ProfileType != ProfileType.FG ? $"é do tipo {g.ProfileType}" : null;

    private static ProfilePVI? AcharPVI(Profile g, double estaca)
    {
        foreach (ProfilePVI v in g.PVIs) if (Math.Abs(v.RawStation - estaca) <= 0.01) return v;
        return null;
    }

    // ================================================================== PRJCONECTAREIXO

    [CommandMethod("PRJCONECTAREIXO", CommandFlags.Modal)]
    public static void ConectarEixo()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            int feitas = 0;
            while (true)
            {
                var a = PedirAlinhamento(ed, feitas == 0 ? "\nClique no 1º alinhamento, perto do canto onde vai a conexão: " : "\nClique no 1º alinhamento da próxima conexão <sair>: ");
                if (a is null) break;
                var b = PedirAlinhamento(ed, "\nClique no 2º alinhamento, perto do mesmo canto: ");
                if (b is null) break;
                if (a.Id == b.Id) { Ctx.Msg("É o mesmo alinhamento. Clique em dois diferentes."); continue; }

                string nomeA, nomeB;
                using (var tr = Ctx.Db.TransactionManager.StartOpenCloseTransaction())
                {
                    nomeA = ((Alignment)tr.GetObject(a.Id, OpenMode.ForRead)).Name;
                    nomeB = ((Alignment)tr.GetObject(b.Id, OpenMode.ForRead)).Name;
                }
                var raio = PedirRaio(ed, cfg, nomeA, nomeB);
                if (raio is null) break;
                cfg.Raio = raio.Value;
                ConfigRotatoria.Salvar(cfg);

                using var s = Sessao.Abrir();
                if (Conectar(s, cfg, a.Id, a.P, b.Id, b.P, raio.Value) is not null) { s.Tr.Commit(); feitas++; }
            }
            if (feitas > 0) Ctx.Msg($"{feitas} conexão(ões) criada(s). Confira tudo com o PRJCASAMENTO (opção Varios). U desfaz a última.");
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJCONECTAREIXO: {ex.Message}"); }
    }

    private sealed record Clique(ObjectId Id, Point3d P, string? Palavra);

    /// <summary>
    /// Clique num alinhamento (ou num rótulo dele — estacas, tangentes, curvas — que é o que o clique pega muitas vezes).
    /// null = Enter/Esc; Palavra preenchida quando o usuário escolheu uma opção.
    /// </summary>
    private static Clique? PedirAlinhamento(Editor ed, string msg, params string[] palavras)
    {
        while (true)
        {
            var peo = new PromptEntityOptions(msg) { AllowNone = true };
            foreach (var k in palavras) peo.Keywords.Add(k);
            if (palavras.Length > 0) peo.AppendKeywordsToMessage = true;
            var r = ed.GetEntity(peo);
            if (r.Status == PromptStatus.Keyword) return new Clique(ObjectId.Null, Point3d.Origin, r.StringResult);
            if (r.Status != PromptStatus.OK) return null;
            var p = r.PickedPoint.TransformBy(ed.CurrentUserCoordinateSystem);
            var id = DoAlinhamento(r.ObjectId, p);
            if (!id.IsNull) return new Clique(id, p, null);
            Ctx.Msg($"Isso é {Tipo(r.ObjectId)}, não um alinhamento. Clique na linha do eixo.");
        }
    }

    private static string Tipo(ObjectId id) { try { return id.ObjectClass.DxfName; } catch { return "outro objeto"; } }

    /// <summary>
    /// O alinhamento clicado: o próprio objeto, o dono do rótulo (grupos de rótulos de estaca, curvas…) ou, se o clique
    /// pegou outra coisa por cima, o alinhamento mais perto do ponto clicado (até 2 m).
    /// </summary>
    private static ObjectId DoAlinhamento(ObjectId id, Point3d p)
    {
        using var tr = Ctx.Db.TransactionManager.StartOpenCloseTransaction();
        var o = tr.GetObject(id, OpenMode.ForRead);
        if (o is Alignment) return id;
        try
        {
            if (o.GetType().GetProperty("FeatureId")?.GetValue(o) is ObjectId fid && !fid.IsNull && tr.GetObject(fid, OpenMode.ForRead) is Alignment) return fid;
        }
        catch { }
        ObjectId melhor = ObjectId.Null;
        double dMin = 2.0;
        foreach (ObjectId aid in CivilApplication.ActiveDocument.GetAlignmentIds())
        {
            if (tr.GetObject(aid, OpenMode.ForRead) is not Alignment al) continue;
            double d = RotatoriaCivil.EstacaAfast(al, p) is { } so ? Math.Abs(so.O)
                     : Math.Min(RotatoriaCivil.Ponto(al, al.StartingStation, 0).DistanceTo(p), RotatoriaCivil.Ponto(al, al.EndingStation, 0).DistanceTo(p));
            if (d < dMin) { dMin = d; melhor = aid; }
        }
        return melhor;
    }

    private static double? PedirRaio(Editor ed, ConfigRotatoria cfg, string a, string b)
    {
        while (true)
        {
            var pdo = new PromptDoubleOptions($"\nRaio da conexão (m) — bordo a {cfg.Bordo(a):0.00} m do eixo em '{a}' e {cfg.Bordo(b):0.00} m em '{b}'")
            {
                AllowNone = true, AllowNegative = false, AllowZero = false, DefaultValue = cfg.Raio, UseDefaultValue = true,
            };
            pdo.Keywords.Add("Bordos");
            pdo.AppendKeywordsToMessage = true;
            var r = ed.GetDouble(pdo);
            if (r.Status == PromptStatus.None) return cfg.Raio;
            if (r.Status == PromptStatus.OK) return r.Value;
            if (r.Status != PromptStatus.Keyword) return null;
            double? Num(string m, double v, bool neg) { var q = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = neg, AllowZero = true, DefaultValue = v, UseDefaultValue = true }); return q.Status == PromptStatus.OK ? q.Value : q.Status == PromptStatus.None ? v : null; }
            if (Num($"Distância do eixo de '{a}' até o bordo onde a conexão encosta (m; 0 = no eixo)", cfg.Bordo(a), false) is not { } da) return null;
            if (Num($"Distância do eixo de '{b}' até o bordo (m)", cfg.Bordo(b), false) is not { } db) return null;
            if (Num("Caimento transversal do eixo até o bordo (%; negativo = desce)", cfg.Caimento * 100, true) is not { } c) return null;
            cfg.Bordos[a] = da; cfg.Bordos[b] = db; cfg.Caimento = c / 100;
            ConfigRotatoria.Salvar(cfg);
        }
    }

    internal sealed record Conectada(Concordancia C, ObjectId Id, string Nome, bool ComGreide);

    /// <summary>
    /// Cria o alinhamento de conexão (e o greide, quando os dois têm greide). pA/pB: pontos nos eixos perto do canto (o lado
    /// de cada um sai da posição do ponto do outro). Devolve null se não deu.
    /// </summary>
    internal static Conectada? Conectar(Sessao s, ConfigRotatoria cfg, ObjectId idA, Point3d pA, ObjectId idB, Point3d pB, double raio,
                                        string? nomePedido = null, double? bordoA = null, double? bordoB = null)
    {
        var alA = (Alignment)s.Tr.GetObject(idA, OpenMode.ForRead);
        var alB = (Alignment)s.Tr.GetObject(idB, OpenMode.ForRead);
        double afA = bordoA ?? cfg.Bordo(alA.Name), afB = bordoB ?? cfg.Bordo(alB.Name);
        int ladoA = RotatoriaCivil.Lado(alA, pB), ladoB = RotatoriaCivil.Lado(alB, pA);
        var c = Conexao.Calcular(RotatoriaCivil.Eixo(alA), RotatoriaCivil.EstacaPerto(alA, pA), ladoA, afA,
                                 RotatoriaCivil.Eixo(alB), RotatoriaCivil.EstacaPerto(alB, pB), ladoB, afB, raio);
        if (c is null)
        {
            Ctx.Msg($"Não cabe um arco de raio {raio:0.##} m tocando '{alA.Name}' e '{alB.Name}' desse lado" +
                    (afA > 0 || afB > 0 ? $" (bordos a {afA:0.00} e {afB:0.00} m)" : "") + ". Tente um raio menor ou clique mais perto do canto.");
            return null;
        }
        double sA = RotatoriaCivil.Ajustar(alA, c.EstacaA), sB = RotatoriaCivil.Ajustar(alB, c.EstacaB);

        // alinhamento: polilinha de um arco só, convertida
        var pl = new Polyline();
        pl.AddVertexAt(0, new Point2d(c.TangenteA.X, c.TangenteA.Y), c.Bulge, 0, 0);
        pl.AddVertexAt(1, new Point2d(c.TangenteB.X, c.TangenteB.Y), 0, 0, 0);
        pl.SetDatabaseDefaults();
        var plId = Ctx.Adicionar(s.Tr, Ctx.EspacoModelo(s.Tr, s.Db), pl);
        var civ = CivilApplication.ActiveDocument;
        string nome = RotatoriaCivil.NomeUnico(s.Tr, civ, nomePedido ?? $"PRJ Conexão {alA.Name} - {alB.Name}");
        ObjectId rotulos = ObjectId.Null;
        try { if (civ.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count > 0) rotulos = civ.Styles.LabelSetStyles.AlignmentLabelSetStyles[0]; } catch { }
        var alId = Alignment.Create(civ, new PolylineOptions { PlineId = plId, AddCurvesBetweenTangents = false, EraseExistingEntities = true },
                                    nome, ObjectId.Null, alA.LayerId, alA.StyleId, rotulos);
        var nova = (Alignment)s.Tr.GetObject(alId, OpenMode.ForRead);
        Ctx.Msg($"Conexão '{nome}': R = {raio:0.00} m, desenvolvimento {c.Desenvolvimento:0.00} m, deflexão {c.AnguloCentral * 180 / Math.PI:0.00}°. " +
                $"Tangente em '{alA.Name}' na est. {RotatoriaCivil.Est(sA)}{(afA > 0 ? $" (bordo a {afA:0.00} m)" : "")} e em '{alB.Name}' na est. {RotatoriaCivil.Est(sB)}{(afB > 0 ? $" (bordo a {afB:0.00} m)" : "")}.");

        // greide casando com os dois
        var gA = EscolherGreide(s, alA);
        var gB = EscolherGreide(s, alB);
        var ca = gA is null ? null : RotatoriaCivil.CotaRampa(gA, sA);
        var cb = gB is null ? null : RotatoriaCivil.CotaRampa(gB, sB);
        if (ca is null || cb is null)
        {
            string por = gA is null ? $"'{alA.Name}' não tem greide de projeto" : ca is null ? $"o greide '{gA.Name}' não chega na tangência"
                       : gB is null ? $"'{alB.Name}' não tem greide de projeto" : $"o greide '{gB.Name}' não chega na tangência";
            Ctx.Msg($"  greide da conexão não criado: {por}.");
            return new Conectada(c, alId, nome, false);
        }
        double z0 = ca.Value.Z + cfg.Caimento * afA, z1 = cb.Value.Z + cfg.Caimento * afB;
        double g0 = ca.Value.G * c.SentidoA, g1 = cb.Value.G * c.SentidoB;
        double l = nova.EndingStation - nova.StartingStation;
        var pvis = PerfilConexao.Calcular(l, z0, g0, z1, g1);
        ObjectId estiloPerfil = gA!.StyleId, rotPerfil = ObjectId.Null;
        try { if (civ.Styles.LabelSetStyles.ProfileLabelSetStyles.Count > 0) rotPerfil = civ.Styles.LabelSetStyles.ProfileLabelSetStyles[0]; } catch { }
        var pid = Profile.CreateByLayout(nome, alId, gA.LayerId, estiloPerfil, rotPerfil);
        var pf = (Profile)s.Tr.GetObject(pid, OpenMode.ForWrite);
        double st0 = nova.StartingStation;
        pf.PVIs.AddPVI(st0, pvis[0].Cota);
        pf.PVIs.AddPVI(st0 + l, pvis[^1].Cota);
        var meio = new List<string>();
        for (int i = 1; i < pvis.Count - 1; i++)
        {
            var v = pvis[i];
            if (v.Curva > 0) pf.PVIs.AddPVISymParabola(st0 + v.Estaca, v.Cota, v.Curva);
            else pf.PVIs.AddPVI(st0 + v.Estaca, v.Cota);
            meio.Add($"PIV na est. {RotatoriaCivil.Est(st0 + v.Estaca)} ({v.Cota:0.000}){(v.Curva > 0 ? $" com parábola L = {v.Curva:0.00} m" : "")}");
        }
        Ctx.Msg($"  greide '{nome}': sai com {z0:0.000} e {Pct(g0)} (de '{gA.Name}'), chega com {z1:0.000} e {Pct(g1)} (de '{gB!.Name}'); " +
                $"{(meio.Count == 0 ? "rampa única" : string.Join("; ", meio))}.");
        return new Conectada(c, alId, nome, true);
    }
}
