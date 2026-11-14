using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Alinhamentos;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJROTATORIA — monta a rotatória de uma vez: clica no centro, seleciona os eixos dos ramos e sai o anel (eixo fechado
/// anti-horário, mão direita) com o greide passando nas cotas dos braços, as conexões de entrada e de saída de cada braço
/// (arcos tangentes ao bordo externo do anel e aos bordos do ramo, com greide casando com os dois), os bordos do anel e da
/// ilha central desenhados e um relatório. Não mexe nos eixos dos ramos.
/// </summary>
public sealed partial class CmdRotatoria
{
    internal const string LayerRotatoria = "PRJ-ROTATORIA";

    /// <summary>Braço da rotatória: onde um eixo de ramo cruza o eixo do anel.</summary>
    private sealed record Braco(Alignment Eixo, double S, Point3d J, double Angulo, int SentidoFora, string Rumo);

    [CommandMethod("PRJROTATORIA", CommandFlags.Modal)]
    public static void Rotatoria()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            Point3d o;
            while (true)
            {
                Ctx.Msg(ResumoParametros(cfg));
                var ppo = new PromptPointOptions("\nClique no centro da rotatória");
                ppo.Keywords.Add("Parametros");
                ppo.AppendKeywordsToMessage = true;
                var pr = ed.GetPoint(ppo);
                if (pr.Status == PromptStatus.Keyword) { if (!PedirParametros(ed, cfg)) return; continue; }
                if (pr.Status != PromptStatus.OK) return;
                var w = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                o = new Point3d(w.X, w.Y, 0);
                break;
            }
            double rExt = cfg.Dci / 2, rEixo = rExt - cfg.LarguraAnel / 2, rIlha = rExt - cfg.LarguraAnel;
            if (rIlha < 1) { Ctx.Msg($"Com DCI {cfg.Dci:0.##} m e pista anelar de {cfg.LarguraAnel:0.##} m não sobra ilha central. Ajuste em Parametros."); return; }

            var f = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT") });
            var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os eixos dos ramos que chegam na rotatória: " }, f);
            if (ps.Status != PromptStatus.OK) return;

            using var s = Sessao.Abrir();
            var civ = CivilApplication.ActiveDocument;
            var avisos = new List<string>();
            var bracos = new List<Braco>();
            foreach (var id in ps.Value.GetObjectIds())
            {
                var al = (Alignment)s.Tr.GetObject(id, OpenMode.ForRead);
                if (RotatoriaCivil.Fechado(al)) { avisos.Add($"'{al.Name}' é um eixo fechado (outro anel?) e ficou de fora."); continue; }
                var bs = Bracos(al, o, rEixo, out double dMin);
                if (bs.Count == 0)
                    avisos.Add($"'{al.Name}' não chega no eixo do anel: passa a {dMin:0.00} m do centro e o eixo do anel fica a {rEixo:0.00} m. Prolongue o eixo até o anel (ou até o centro) e rode de novo.");
                bracos.AddRange(bs);
            }
            bracos = bracos.OrderBy(b => b.Angulo).ToList();
            for (int i = bracos.Count - 1; i > 0; i--)
                if (bracos[i].J.DistanceTo(bracos[i - 1].J) < 0.5) bracos.RemoveAt(i);
            if (bracos.Count == 0) { foreach (var a in avisos) Ctx.Msg("AVISO: " + a); Ctx.Msg("Nenhum braço: nada foi criado."); return; }

            // cotas do anel nos braços
            var cotas = CotasDosBracos(s, cfg, bracos, avisos, out string origemCotas);
            if (cotas is null) return;

            // eixo do anel, começando no meio do maior vão entre os braços
            string prefixo = Prefixo(s.Tr, civ);
            double emenda = GeometriaRotatoria.AnguloEmenda(bracos.Select(b => b.Angulo).ToList());
            var pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(o.X + rEixo * Math.Cos(emenda), o.Y + rEixo * Math.Sin(emenda)), 1, 0, 0);
            pl.AddVertexAt(1, new Point2d(o.X - rEixo * Math.Cos(emenda), o.Y - rEixo * Math.Sin(emenda)), 1, 0, 0);
            pl.Closed = true;
            pl.SetDatabaseDefaults();
            var plId = Ctx.Adicionar(s.Tr, Ctx.EspacoModelo(s.Tr, s.Db), pl);
            var ref0 = bracos[0].Eixo;
            ObjectId rotulos = ObjectId.Null;
            try { if (civ.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count > 0) rotulos = civ.Styles.LabelSetStyles.AlignmentLabelSetStyles[0]; } catch { }
            string nomeAnel = RotatoriaCivil.NomeUnico(s.Tr, civ, $"{prefixo} - Anel");
            var anelId = Alignment.Create(civ, new PolylineOptions { PlineId = plId, AddCurvesBetweenTangents = false, EraseExistingEntities = true },
                                          nomeAnel, ObjectId.Null, ref0.LayerId, ref0.StyleId, rotulos);
            var anel = (Alignment)s.Tr.GetObject(anelId, OpenMode.ForRead);
            double st0 = anel.StartingStation, comp = anel.EndingStation - anel.StartingStation;

            // greide do anel passando nas cotas dos braços
            var alvos = new List<PerfilAnel.Alvo>();
            for (int i = 0; i < bracos.Count; i++)
            {
                double sa = RotatoriaCivil.EstacaAfast(anel, bracos[i].J) is { } so ? so.S : st0 + rEixo * GeometriaRotatoria.Normalizar(bracos[i].Angulo - emenda);
                alvos.Add(new PerfilAnel.Alvo(sa - st0, cotas[i]));
            }
            var pvis = PerfilAnel.Calcular(comp, alvos);
            var gRef = bracos.Select(b => RotatoriaCivil.Greides(s.Tr, b.Eixo).FirstOrDefault()).FirstOrDefault(g => g is not null);
            ObjectId estiloPerfil = gRef?.StyleId ?? ObjectId.Null, rotPerfil = ObjectId.Null;
            try { if (estiloPerfil.IsNull && civ.Styles.ProfileStyles.Count > 0) estiloPerfil = civ.Styles.ProfileStyles[0]; } catch { }
            try { if (civ.Styles.LabelSetStyles.ProfileLabelSetStyles.Count > 0) rotPerfil = civ.Styles.LabelSetStyles.ProfileLabelSetStyles[0]; } catch { }
            var pid = Profile.CreateByLayout(nomeAnel, anelId, gRef?.LayerId ?? anel.LayerId, estiloPerfil, rotPerfil);
            var pf = (Profile)s.Tr.GetObject(pid, OpenMode.ForWrite);
            for (int i = 0; i + 1 < pvis.Count; i++)
                pf.Entities.AddFixedTangent(new Point2d(st0 + pvis[i].Estaca, pvis[i].Cota), new Point2d(st0 + pvis[i + 1].Estaca, pvis[i + 1].Cota));
            for (int i = 1; i + 1 < pvis.Count; i++)
                if (pvis[i].Curva > 0.05 && AcharPVI(pf, st0 + pvis[i].Estaca) is { } pvi)
                    pf.Entities.AddFreeSymmetricParabolaByPVIAndCurveLength(pvi, pvis[i].Curva);
            GreideEscolhido[nomeAnel] = pf.Name;
            double erroAnel = alvos.Max(a => Math.Abs(pf.ElevationAt(st0 + a.Estaca) - a.Cota));
            var rampaMax = PerfilAnel.RampaMaxima(pvis);

            // bordos desenhados
            var lay = Ctx.GarantirLayer(s.Tr, s.Db, LayerRotatoria, 30);
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            Ctx.Adicionar(s.Tr, ms, new Circle(o, Vector3d.ZAxis, rExt) { Layer = lay });
            Ctx.Adicionar(s.Tr, ms, new Circle(o, Vector3d.ZAxis, rIlha) { Layer = lay });
            var rotulo = Ctx.Texto(Ctx.MTextLinhas(prefixo, $"DCI {cfg.Dci:0.##} m"), o, Math.Clamp(rIlha / 6, 0.5, 3), 0, AttachmentPoint.MiddleCenter);
            rotulo.Layer = lay;
            Ctx.Adicionar(s.Tr, ms, rotulo);

            // conexões de entrada (lado anti-horário do braço) e de saída (lado horário)
            var angEntrada = new double?[bracos.Count];
            var angSaida = new double?[bracos.Count];
            var linhas = new List<string>();
            for (int i = 0; i < bracos.Count; i++)
            {
                var b = bracos[i];
                var (dx, dy) = RotatoriaCivil.Direcao(b.Eixo, b.S);
                double ux = dx * b.SentidoFora, uy = dy * b.SentidoFora;
                var partes = new List<string>();
                foreach (int lado in new[] { +1, -1 })
                {
                    double r = lado > 0 ? cfg.RaioEntrada : cfg.RaioSaida;
                    string tipo = lado > 0 ? "entrada" : "saída";
                    var (cx, cy) = GeometriaRotatoria.ChuteCentroConexao(b.J.X, b.J.Y, ux, uy, lado, cfg.MeiaLarguraRamo + r, o.X, o.Y, rExt + r);
                    var pc = new Point3d(cx, cy, 0);
                    var pB = RotatoriaCivil.Ponto(b.Eixo, RotatoriaCivil.EstacaPerto(b.Eixo, pc), 0);
                    var pA = RotatoriaCivil.Ponto(anel, RotatoriaCivil.EstacaPerto(anel, pc), 0);
                    var feito = Conectar(s, cfg, anelId, pA, b.Eixo.ObjectId, pB, r, $"{prefixo} - {b.Eixo.Name} {b.Rumo} - {tipo}", cfg.LarguraAnel / 2, cfg.MeiaLarguraRamo);
                    if (feito is null) { partes.Add($"{tipo} R {r:0.##}: NÃO coube"); avisos.Add($"Braço {b.Rumo} ('{b.Eixo.Name}'): a conexão de {tipo} de R {r:0.##} m não coube (o eixo do ramo não vai longe o bastante para fora, ou o raio é grande demais)."); continue; }
                    double ang = Math.Atan2(feito.C.TangenteA.Y - o.Y, feito.C.TangenteA.X - o.X);
                    if (lado > 0) angEntrada[i] = ang; else angSaida[i] = ang;
                    partes.Add($"{tipo} R {r:0.##} ({feito.C.Desenvolvimento:0.00} m{(feito.ComGreide ? "" : ", sem greide")})");
                }
                linhas.Add($"  braço {b.Rumo,-2} '{b.Eixo.Name}' est. {RotatoriaCivil.Est(b.S)}: cota {cotas[i]:0.000} — {string.Join("; ", partes)}");
            }

            // folga no bordo externo entre a entrada de um braço e a saída do próximo
            var folgas = new List<string>();
            if (bracos.Count > 1)
                for (int i = 0; i < bracos.Count; i++)
                {
                    int j = (i + 1) % bracos.Count;
                    if (angEntrada[i] is not { } e || angSaida[j] is not { } x) continue;
                    double fm = GeometriaRotatoria.Folga(bracos[i].Angulo, e, bracos[j].Angulo, x) * rExt;
                    folgas.Add($"{bracos[i].Rumo}→{bracos[j].Rumo} {fm:0.00} m");
                    if (fm < 0) avisos.Add($"Entre os braços {bracos[i].Rumo} e {bracos[j].Rumo} a conexão de entrada e a de saída se sobrepõem {-fm:0.00} m no bordo do anel: diminua os raios ou aumente o DCI.");
                }

            s.Tr.Commit();

            string premissa = cfg.ParametrosDoUsuario ? "" : " [PREMISSA: valores de partida, troque em Parametros]";
            Ctx.Msg($"{prefixo} criada no centro ({o.X:0.000}; {o.Y:0.000}): DCI {cfg.Dci:0.##} m, pista anelar {cfg.LarguraAnel:0.##} m → eixo do anel R {rEixo:0.00} m, ilha central R {rIlha:0.00} m; " +
                    $"bordo dos ramos a {cfg.MeiaLarguraRamo:0.##} m do eixo; caimento {Pct(cfg.Caimento, 1)}{premissa}.");
            foreach (var l in linhas) Ctx.Msg(l);
            Ctx.Msg($"  greide do anel ({origemCotas}): passa nas cotas dos braços (erro máx. {erroAnel * 1000:0.0} mm); rampa máxima {Pct(Math.Abs(rampaMax.Rampa))} " +
                    $"entre as est. {RotatoriaCivil.Est(st0 + rampaMax.De)} e {RotatoriaCivil.Est(st0 + rampaMax.Ate)}.");
            if (folgas.Count > 0) Ctx.Msg($"  folga no bordo do anel entre a entrada de um braço e a saída do próximo: {string.Join("; ", folgas)}.");
            foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Bordo externo e ilha central na camada {LayerRotatoria}. Confira tudo com PRJCASAMENTO › Varios. U desfaz a rotatória inteira.");
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJROTATORIA: {ex.Message}"); }
    }

    private static string ResumoParametros(ConfigRotatoria c) =>
        $"Rotatória: DCI {c.Dci:0.##} m, pista anelar {c.LarguraAnel:0.##} m, bordo dos ramos a {c.MeiaLarguraRamo:0.##} m do eixo, " +
        $"raio de entrada {c.RaioEntrada:0.##} m e de saída {c.RaioSaida:0.##} m, caimento {Pct(c.Caimento, 1)}, cota do anel pelos {c.CotaAnel.ToLowerInvariant()}" +
        (c.ParametrosDoUsuario ? "." : " [PREMISSA: valores de partida].");

    private static bool PedirParametros(Editor ed, ConfigRotatoria cfg)
    {
        double? N(string m, double v, bool neg = false, bool zero = false)
        {
            var r = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = neg, AllowZero = zero, DefaultValue = v, UseDefaultValue = true });
            return r.Status == PromptStatus.OK ? r.Value : r.Status == PromptStatus.None ? v : null;
        }
        if (N("Diâmetro do círculo inscrito, DCI (m)", cfg.Dci) is not { } dci) return false;
        if (N("Largura da pista anelar (m)", cfg.LarguraAnel) is not { } la) return false;
        if (N("Distância do eixo dos ramos até o bordo (m)", cfg.MeiaLarguraRamo, zero: true) is not { } mr) return false;
        if (N("Raio de entrada (m)", cfg.RaioEntrada) is not { } re) return false;
        if (N("Raio de saída (m)", cfg.RaioSaida) is not { } rs) return false;
        if (N("Caimento transversal do eixo até o bordo (%; negativo = desce)", cfg.Caimento * 100, neg: true, zero: true) is not { } ca) return false;
        var pko = new PromptKeywordOptions("\nCota do anel nos braços [Greides/Terreno/Fixa]", "Greides Terreno Fixa") { AllowNone = true };
        pko.Keywords.Default = cfg.CotaAnel;
        var pk = ed.GetKeywords(pko);
        if (pk.Status == PromptStatus.Cancel || pk.Status == PromptStatus.Error) return false;
        cfg.Dci = dci; cfg.LarguraAnel = la; cfg.MeiaLarguraRamo = mr; cfg.RaioEntrada = re; cfg.RaioSaida = rs; cfg.Caimento = ca / 100;
        if (pk.Status == PromptStatus.OK && !string.IsNullOrEmpty(pk.StringResult)) cfg.CotaAnel = pk.StringResult;
        cfg.ParametrosDoUsuario = true;
        ConfigRotatoria.Salvar(cfg);
        return true;
    }

    /// <summary>Onde o eixo cruza o círculo do eixo do anel (amostrado a cada 0,5 m e refinado por bisseção).</summary>
    private static List<Braco> Bracos(Alignment al, Point3d o, double r, out double dMin)
    {
        var res = new List<Braco>();
        double ini = al.StartingStation, fim = al.EndingStation, l = fim - ini;
        int n = (int)Math.Clamp(Math.Ceiling(l / 0.5), 10, 40000);
        double h = l / n;
        double dm = double.MaxValue;
        double F(double s)
        {
            double d = RotatoriaCivil.Ponto(al, s, 0).DistanceTo(o);
            dm = Math.Min(dm, d);
            return d - r;
        }
        double s0 = ini, f0 = F(ini);
        for (int k = 1; k <= n; k++)
        {
            double s1 = k == n ? fim : ini + k * h, f1 = F(s1);
            if (f0 * f1 < 0 || (f1 == 0 && f0 != 0))
            {
                double a = s0, b = s1, fa = f0;
                for (int it = 0; it < 60 && b - a > 1e-9; it++)
                {
                    double m = (a + b) / 2, fmv = F(m);
                    if (fa * fmv <= 0) b = m; else { a = m; fa = fmv; }
                }
                double sj = (a + b) / 2;
                var j = RotatoriaCivil.Ponto(al, sj, 0);
                double ang = GeometriaRotatoria.Normalizar(Math.Atan2(j.Y - o.Y, j.X - o.X));
                int sentido = f1 > f0 ? 1 : -1;
                res.Add(new Braco(al, sj, j, ang, sentido, GeometriaRotatoria.Rumo(ang)));
            }
            s0 = s1; f0 = f1;
        }
        dMin = dm;
        return res;
    }

    /// <summary>Cota do eixo do anel em cada braço (greides dos ramos, superfície ou fixa). null = cancelou.</summary>
    private static double[]? CotasDosBracos(Sessao s, ConfigRotatoria cfg, List<Braco> bracos, List<string> avisos, out string origem)
    {
        var z = new double?[bracos.Count];
        string modo = cfg.CotaAnel;
        origem = "";
        if (modo == "Greides")
        {
            for (int i = 0; i < bracos.Count; i++)
                if (EscolherGreide(s, bracos[i].Eixo) is { } g && RotatoriaCivil.CotaRampa(g, bracos[i].S) is { } cr) z[i] = cr.Z;
            if (z.All(v => v is null))
            {
                Ctx.Msg("Nenhum ramo tem greide de projeto no encontro com o anel.");
                var resp = s.PedirPalavra("Cota do anel pelo [Terreno/Fixa]", new[] { "Terreno", "Fixa" }, "Terreno");
                if (resp is null) return null;
                modo = resp;
            }
            else origem = "cotas dos greides dos ramos";
        }
        if (modo == "Terreno")
        {
            var sups = VolumesCivil.Superficies(s.Tr);
            if (sups.Count == 0) { Ctx.Msg("Não há superfície no desenho; use a cota fixa."); modo = "Fixa"; }
            else
            {
                int k = sups.Count == 1 ? 0 : s.EscolherDaLista("Superfície para a cota do anel", sups.Select(x => x.Nome).ToList(), 0);
                if (k < 0) return null;
                var sup = (Surface)s.Tr.GetObject(sups[k].Id, OpenMode.ForRead);
                for (int i = 0; i < bracos.Count; i++)
                    try { z[i] = sup.FindElevationAtXY(bracos[i].J.X, bracos[i].J.Y); } catch { }
                origem = $"superfície '{sups[k].Nome}' nos braços";
                if (z.All(v => v is null)) { Ctx.Msg($"A superfície '{sups[k].Nome}' não cobre os braços; use a cota fixa."); modo = "Fixa"; }
            }
        }
        if (modo == "Fixa")
        {
            var c = s.PedirNumero("Cota do anel (m)", cfg.CotaFixa);
            if (c is null) return null;
            cfg.CotaFixa = c.Value;
            ConfigRotatoria.Salvar(cfg);
            for (int i = 0; i < z.Length; i++) z[i] = c.Value;
            origem = $"cota fixa {c.Value:0.000}";
        }
        double media = z.Where(v => v is not null).Average(v => v!.Value);
        for (int i = 0; i < z.Length; i++)
            if (z[i] is null)
            {
                z[i] = media;
                avisos.Add($"Braço {bracos[i].Rumo} ('{bracos[i].Eixo.Name}'): sem cota no encontro (sem greide ou fora da superfície); usei a média dos outros, {media:0.000} [A CONFIRMAR].");
            }
        return z.Select(v => v!.Value).ToArray();
    }

    /// <summary>"Rotatória n": o primeiro número sem alinhamento com esse prefixo.</summary>
    private static string Prefixo(Transaction tr, CivilDocument civ)
    {
        var nomes = new List<string>();
        foreach (ObjectId id in civ.GetAlignmentIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Alignment al) nomes.Add(al.Name);
        for (int n = 1; ; n++)
        {
            string p = $"Rotatória {n}";
            if (!nomes.Any(x => x.StartsWith(p + " - ", StringComparison.OrdinalIgnoreCase))) return p;
        }
    }
}
