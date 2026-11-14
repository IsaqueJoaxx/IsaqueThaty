using System.Diagnostics;
using System.Text.Json;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CPoint = Autodesk.Civil.DatabaseServices.Point;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Construtor))]

namespace Projeta.Reconstrutor;

public sealed class Log : IDisposable
{
    readonly StreamWriter w;
    public Log(string p) { w = new StreamWriter(p, false) { AutoFlush = true }; }
    public void W(string s) { w.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + s); }
    public void Dispose() => w.Dispose();
}

public class Construtor
{
    static string Base => Resumo.Saida("prj_constr");
    static JsonElement Plano() => JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_PLANO"))).RootElement;
    static void Msg(string s) => AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + s + "\n");

    static ObjectId Camada(Transaction tr, Database db, string nome, short cor)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(nome)) return lt[nome];
        lt.UpgradeOpen();
        var ltr = new LayerTableRecord { Name = nome, Color = Color.FromColorIndex(ColorMethod.ByAci, cor) };
        var id = lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true); return id;
    }

    static ObjectId Superficie(CivilDocument cd, Transaction tr, string nome)
    {
        foreach (ObjectId id in cd.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead); if (s.Name == nome) return id; }
        return ObjectId.Null;
    }

    static string Stats(Transaction tr, ObjectId sid)
    {
        if (sid.IsNull) return "(sem superficie)";
        var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(sid, OpenMode.ForRead);
        var p = s.GetGeneralProperties();
        string od = ""; try { od = " desatualizada=" + s.IsOutOfDate; } catch { }
        return $"{s.Name}: pontos={p.NumberOfPoints} z={p.MinimumElevation:F2}..{p.MaximumElevation:F2}{od}";
    }

    static ObjectId AlinhamentoPorNome(CivilDocument cd, Transaction tr, string nome)
    {
        foreach (ObjectId id in cd.GetAlignmentIds()) { var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); if (a.Name == nome) return id; }
        return ObjectId.Null;
    }

    static ObjectId Escolher(Transaction tr, IEnumerable<ObjectId> ids, Log log, string rotulo, params string[] preferidos)
    {
        var lista = new List<(ObjectId id, string nome)>();
        foreach (var id in ids) { try { var o = tr.GetObject(id, OpenMode.ForRead); var n = (o as Autodesk.Civil.DatabaseServices.Styles.StyleBase)?.Name ?? "?"; lista.Add((id, n)); } catch { } }
        log.W(rotulo + ": " + string.Join(" | ", lista.Select(x => x.nome)));
        foreach (var pref in preferidos) { var m = lista.FirstOrDefault(x => x.nome.Contains(pref, StringComparison.OrdinalIgnoreCase)); if (!m.id.IsNull) { log.W("  -> " + m.nome); return m.id; } }
        if (lista.Count > 0) { log.W("  -> " + lista[0].nome); return lista[0].id; }
        return ObjectId.Null;
    }

    static IEnumerable<ObjectId> Ids(System.Collections.IEnumerable col) { foreach (var o in col) if (o is ObjectId id) yield return id; }

    // ---------------- ETAPA A ----------------
    [CommandMethod("PRJPREPARAR", CommandFlags.Modal)]
    public void Preparar()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Base + ".log");
        try
        {
            var plano = Plano();
            ObjectId supId;
            using (var tr = db.TransactionManager.StartTransaction()) { supId = Superficie(cd, tr, plano.GetProperty("superficie").GetString()); log.W("antes  " + Stats(tr, supId)); tr.Commit(); }

            var faceClass = RXObject.GetClass(typeof(Face));
            var ids = new List<ObjectId>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                foreach (ObjectId id in ms) if (id.ObjectClass == faceClass) ids.Add(id);
                tr.Commit();
            }
            log.W("faces 3D encontradas: " + ids.Count);
            int apagadas = 0;
            for (int k = 0; k < ids.Count; k += 250000)
            {
                using var tr = db.TransactionManager.StartTransaction();
                for (int j = k; j < Math.Min(ids.Count, k + 250000); j++)
                {
                    var e = (Entity)tr.GetObject(ids[j], OpenMode.ForRead);
                    if (e.Layer != "TOP_MALHA_TRIANGULAR") continue;
                    e.UpgradeOpen(); e.Erase(); apagadas++;
                }
                tr.Commit(); log.W("apagadas: " + apagadas);
            }
            using (var tr = db.TransactionManager.StartTransaction()) { log.W("depois " + Stats(tr, supId)); tr.Commit(); }

            // linhas de referencia do PRJ-50998
            var refPath = Environment.GetEnvironmentVariable("PRJ_REF");
            var camadas = plano.GetProperty("ref_camadas").EnumerateArray().Select(x => x.GetString()).ToHashSet();
            var cores = new Dictionary<string, short> { ["SARJETA"] = 3, ["BORDO"] = 30, ["ACOSTAMENTO"] = 4, ["NEW JERSEY"] = 6, ["OFFSET"] = 8, ["GUIA"] = 2 };
            int n = 0;
            using (var src = new Database(false, true))
            {
                src.ReadDwgFile(refPath, FileOpenMode.OpenForReadAndAllShare, true, "");
                using var trs = src.TransactionManager.StartTransaction();
                using var tr = db.TransactionManager.StartTransaction();
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                var sms = (BlockTableRecord)trs.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(src), OpenMode.ForRead);
                void Copiar(Entity e, Matrix3d xf)
                {
                    if (!camadas.Contains(e.Layer)) return;
                    bool offset = e.Layer.Contains("OFFSET");
                    Entity novo = null;
                    switch (e)
                    {
                        case Polyline pl:
                            {
                                var p = new Polyline();
                                for (int i = 0; i < pl.NumberOfVertices; i++) p.AddVertexAt(i, pl.GetPoint2dAt(i), pl.GetBulgeAt(i), 0, 0);
                                p.Closed = pl.Closed; p.Elevation = pl.Elevation; p.Normal = pl.Normal;
                                p.TransformBy(xf); novo = p; break;
                            }
                        case Polyline3d p3:
                            {
                                var pts = new Point3dCollection();
                                foreach (ObjectId vid in p3) { var vx = (PolylineVertex3d)trs.GetObject(vid, OpenMode.ForRead); pts.Add(vx.Position.TransformBy(xf)); }
                                novo = new Polyline3d(Poly3dType.SimplePoly, pts, p3.Closed); break;
                            }
                        case Line ln when !offset:
                            novo = new Line(ln.StartPoint.TransformBy(xf), ln.EndPoint.TransformBy(xf)); break;
                    }
                    if (novo == null) return;
                    short cor = 7; foreach (var kv in cores) if (e.Layer.Contains(kv.Key)) cor = kv.Value;
                    novo.LayerId = Camada(tr, db, "REF-PRJ50998 " + e.Layer, cor);
                    ms.AppendEntity(novo); tr.AddNewlyCreatedDBObject(novo, true); n++;
                }
                foreach (ObjectId id in sms)
                {
                    var e = trs.GetObject(id, OpenMode.ForRead) as Entity; if (e == null) continue;
                    if (e is BlockReference br)
                    {
                        var def = (BlockTableRecord)trs.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                        foreach (ObjectId sid in def) { var se = trs.GetObject(sid, OpenMode.ForRead) as Entity; if (se != null) Copiar(se, br.BlockTransform); }
                    }
                    else Copiar(e, Matrix3d.Identity);
                }
                tr.Commit(); trs.Commit();
            }
            log.W("linhas de referencia copiadas: " + n);
            Msg("PRJPREPARAR ok");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); Msg("PRJPREPARAR ERRO " + ex.Message); }
    }

    // ---------------- ETAPA B ----------------
    [CommandMethod("PRJEIXOS", CommandFlags.Modal)]
    public void Eixos()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Base + ".log");
        try
        {
            var plano = Plano();
            using var tr = db.TransactionManager.StartTransaction();
            var supId = Superficie(cd, tr, plano.GetProperty("superficie").GetString());
            var camEixo = Camada(tr, db, plano.GetProperty("camada_eixo").GetString(), 1);
            var camGreide = Camada(tr, db, "PRJ-TER-GREIDE", 1);
            var camTN = Camada(tr, db, "PRJ-TER-TN", 8);
            var principal = plano.GetProperty("alinhamentos").EnumerateArray().First(a => a.GetProperty("fonte").GetString() == "alinhamento");
            var oldId = AlinhamentoPorNome(cd, tr, principal.GetProperty("ref").GetString());
            ObjectId alStyle = oldId.IsNull ? ObjectId.Null : ((Alignment)tr.GetObject(oldId, OpenMode.ForRead)).StyleId;
            if (alStyle.IsNull) alStyle = Escolher(tr, Ids(cd.Styles.AlignmentStyles), log, "estilos de alinhamento");
            var alLabel = Escolher(tr, Ids(cd.Styles.LabelSetStyles.AlignmentLabelSetStyles), log, "rotulos de alinhamento", "PROJETA", "INFRA", "Major", "Principal", "Todos", "All");
            var prStyle = Escolher(tr, Ids(cd.Styles.ProfileStyles), log, "estilos de perfil (greide)", "Projeto", "Design", "PROJETO", "Greide");
            var prStyleTN = Escolher(tr, Ids(cd.Styles.ProfileStyles), log, "estilos de perfil (TN)", "Terreno", "Existing", "Existente", "TN");
            var prLabel = Escolher(tr, Ids(cd.Styles.LabelSetStyles.ProfileLabelSetStyles), log, "rotulos de perfil", "PRJ-PADR", "GREIDE", "Projeto", "Design");
            var prLabelTN = Escolher(tr, Ids(cd.Styles.LabelSetStyles.ProfileLabelSetStyles), log, "rotulos de perfil TN", "Sem", "No Labels", "_No");

            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            var pls = new List<Polyline>();
            foreach (ObjectId id in ms) { if (tr.GetObject(id, OpenMode.ForRead) is Polyline p && p.Layer == "F1-HZ-EIXO") pls.Add(p); }
            log.W("polilinhas F1-HZ-EIXO: " + pls.Count);

            double pvY = 7749300;
            foreach (var a in plano.GetProperty("alinhamentos").EnumerateArray())
            {
                string nome = a.GetProperty("nome").GetString(); string fonte = a.GetProperty("fonte").GetString();
                ObjectId alId = ObjectId.Null;
                try
                {
                    if (fonte == "alinhamento") alId = CriarPrincipal(cd, tr, a, oldId, nome, camEixo, alStyle, alLabel, log);
                    else
                    {
                        var ini = a.GetProperty("ini"); var p0 = new Point2d(ini[0].GetDouble(), ini[1].GetDouble()); double L = a.GetProperty("comp").GetDouble();
                        var pl = pls.FirstOrDefault(p => p.GetPoint2dAt(0).GetDistanceTo(p0) < 0.05 && Math.Abs(p.Length - L) < 0.05);
                        if (pl == null) { log.W($"[{nome}] polilinha nao encontrada"); continue; }
                        if (fonte == "polilinha")
                        {
                            var op = new PolylineOptions { PlineId = pl.ObjectId, AddCurvesBetweenTangents = false, EraseExistingEntities = false };
                            alId = Alignment.Create(cd, op, nome, ObjectId.Null, camEixo, alStyle, alLabel);
                        }
                        else alId = CriarEspiral(cd, tr, a, pl, nome, camEixo, alStyle, alLabel, log);
                    }
                    var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead);
                    log.W($"[{nome}] alinhamento ok: L={al.Length:F3} entidades={al.Entities.Count}");

                    if (!supId.IsNull)
                    {
                        try { Profile.CreateFromSurface(nome + " - TN", alId, supId, camTN, prStyleTN, prLabelTN); } catch (System.Exception ex) { log.W($"[{nome}] TN erro {ex.Message}"); }
                    }
                    if (a.GetProperty("perfil").GetBoolean()) CriarGreide(tr, a, alId, nome, camGreide, prStyle, prLabel, log);

                    try
                    {
                        var pvId = ProfileView.Create(alId, new Point3d(666000, pvY, 0));
                        var pv = (ProfileView)tr.GetObject(pvId, OpenMode.ForWrite);
                        try { pv.Name = "PV - " + nome; } catch { }
                        var ext = pv.GeometricExtents; pvY = ext.MaxPoint.Y + 120;
                    }
                    catch (System.Exception ex) { log.W($"[{nome}] vista de perfil erro {ex.Message}"); }
                }
                catch (System.Exception ex) { log.W($"[{nome}] ERRO {ex}"); }
            }
            if (!oldId.IsNull && !AlinhamentoPorNome(cd, tr, principal.GetProperty("nome").GetString()).IsNull) { var old = (Alignment)tr.GetObject(oldId, OpenMode.ForWrite); old.Erase(); log.W("alinhamento antigo (espirais em cordas) removido da copia"); }
            tr.Commit();
            Msg("PRJEIXOS ok");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); Msg("PRJEIXOS ERRO " + ex.Message); }
    }

    static ObjectId CriarPrincipal(CivilDocument cd, Transaction tr, JsonElement a, ObjectId oldId, string nome, ObjectId cam, ObjectId est, ObjectId rot, Log log)
    {
        var old = (Alignment)tr.GetObject(oldId, OpenMode.ForRead);
        var tans = new List<(Point2d a, Point2d b)>();
        for (int i = 0; i < old.Entities.Count; i++)
        {
            var ae = old.Entities.GetEntityByOrder(i);
            if (ae.SubEntityCount == 1 && ae[0] is AlignmentSubEntityLine ln && ln.EndStation - ln.StartStation > 10.0) tans.Add((ln.StartPoint, ln.EndPoint));
        }
        var curvas = a.GetProperty("curvas").EnumerateArray().ToList();
        log.W($"[{nome}] tangentes lidas do alinhamento antigo: {tans.Count} (curvas no plano: {curvas.Count})");
        if (tans.Count != curvas.Count + 1) throw new InvalidOperationException("numero de tangentes nao confere");
        return Montar(cd, tr, nome, cam, est, rot, tans, curvas.Select(c => ("scs", c)).ToList(), log);
    }

    static ObjectId CriarEspiral(CivilDocument cd, Transaction tr, JsonElement a, Polyline pl, string nome, ObjectId cam, ObjectId est, ObjectId rot, Log log)
    {
        var tans = new List<(Point2d a, Point2d b)>();
        for (int i = 0; i < pl.NumberOfVertices - 1; i++)
        {
            var p = pl.GetPoint2dAt(i); var q = pl.GetPoint2dAt(i + 1);
            if (Math.Abs(pl.GetBulgeAt(i)) < 1e-12 && p.GetDistanceTo(q) > 6.0) tans.Add((p, q));
        }
        double ext = a.TryGetProperty("extensao_final", out var ex) ? ex.GetDouble() : 0;
        if (ext > 0)
        {
            int n = pl.NumberOfVertices; var pN = pl.GetPoint2dAt(n - 1); var pM = pl.GetPoint2dAt(n - 2);
            var d = (pN - pM).GetNormal(); tans.Add((pN, pN + d * ext));
        }
        var els = a.GetProperty("elementos").EnumerateArray().Select(e => (e.GetProperty("t").GetString(), e)).ToList();
        log.W($"[{nome}] tangentes da polilinha: {tans.Count} elementos: {els.Count} extensao final: {ext}");
        if (tans.Count != els.Count + 1) throw new InvalidOperationException("numero de tangentes nao confere");
        return Montar(cd, tr, nome, cam, est, rot, tans, els, log);
    }

    static ObjectId Montar(CivilDocument cd, Transaction tr, string nome, ObjectId cam, ObjectId est, ObjectId rot, List<(Point2d a, Point2d b)> tans, List<(string t, JsonElement e)> els, Log log)
    {
        var id = Alignment.Create(cd, nome, ObjectId.Null, cam, est, rot);
        var al = (Alignment)tr.GetObject(id, OpenMode.ForWrite);
        var tid = new List<int>();
        foreach (var t in tans) tid.Add(al.Entities.AddFixedLine(new Point3d(t.a.X, t.a.Y, 0), new Point3d(t.b.X, t.b.Y, 0)).EntityId);
        for (int i = 0; i < els.Count; i++)
        {
            var e = els[i].e; double R = e.GetProperty("R").GetDouble();
            if (els[i].t == "curva")
                al.Entities.AddFreeCurve(tid[i], tid[i + 1], R, CurveParamType.Radius, false, CurveType.Compound);
            else
                al.Entities.AddFreeSCS(tid[i], tid[i + 1], e.GetProperty("L1").GetDouble(), e.GetProperty("L2").GetDouble(), SpiralParamType.Length, R, false, SpiralType.Clothoid);
        }
        return id;
    }

    static void CriarGreide(Transaction tr, JsonElement a, ObjectId alId, string nome, ObjectId cam, ObjectId est, ObjectId rot, Log log)
    {
        var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead);
        var pv = a.GetProperty("pvis").EnumerateArray().Select(x => (s: x[0].GetDouble(), z: x[1].GetDouble(), L: x[2].GetDouble())).ToList();
        double fim = al.EndingStation;
        var u = pv[^1]; var pu = pv[^2];
        if (Math.Abs(u.s - fim) > 0.0005)
        {
            double z = pu.z + (u.z - pu.z) * (fim - pu.s) / (u.s - pu.s);
            log.W($"[{nome}] ultimo PIV ajustado de {u.s:F3} para o fim do eixo {fim:F3} (z {u.z:F3} -> {z:F3})");
            pv[^1] = (fim, z, 0);
        }
        var pid = Profile.CreateByLayout(nome + " - GREIDE", alId, cam, est, rot);
        var pr = (Profile)tr.GetObject(pid, OpenMode.ForWrite);
        pr.Entities.AddFixedTangent(new Point2d(pv[0].s, pv[0].z), new Point2d(pv[^1].s, pv[^1].z));
        for (int i = 1; i < pv.Count - 1; i++) pr.PVIs.AddPVI(pv[i].s, pv[i].z);
        for (int i = 1; i < pv.Count - 1; i++)
            if (pv[i].L > 0) pr.Entities.AddFreeSymmetricParabolaByPVIAndCurveLength(pr.PVIs.GetPVIAt(pv[i].s, pv[i].z), pv[i].L);
        var txt = new List<string>();
        foreach (ProfilePVI v in pr.PVIs) txt.Add($"{v.RawStation:F3}/{v.Elevation:F3}/{(v.VerticalCurve == null ? 0 : v.VerticalCurve.Length):F3}");
        log.W($"[{nome}] greide: " + string.Join("  ", txt));
    }

    // ---------------- ETAPA C ----------------
    static SubassemblySideType? Lado(string s) => s == "E" ? SubassemblySideType.Left : s == "D" ? SubassemblySideType.Right : null;

    static ObjectId Sub(Transaction tr, CivilDocument cd, ObjectId asmId, string cls, string nome, SubassemblySideType? lado,
        Dictionary<string, double> pd, Dictionary<string, string> ps, ObjectId hook, int hookIdx, Point3d loc)
    {
        var sid = cd.SubassemblyCollection.ImportStockSubassembly(nome, "Subassembly." + cls, loc);
        var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForWrite);
        if (lado.HasValue)
        {
            if (sa.HasSide) sa.Side = lado.Value;
            foreach (var p in sa.ParamsLong) if (p.Key == "Side") p.Value = lado == SubassemblySideType.Left ? 1 : 0;
        }
        if (pd != null) foreach (var p in sa.ParamsDouble) if (pd.TryGetValue(p.Key, out var v)) p.Value = v;
        if (ps != null) foreach (var p in sa.ParamsString) if (ps.TryGetValue(p.Key, out var v)) p.Value = v;
        var asm = (Assembly)tr.GetObject(asmId, OpenMode.ForWrite);
        if (hook.IsNull) asm.AddSubassembly(sid);
        else { var hs = (Subassembly)tr.GetObject(hook, OpenMode.ForRead); asm.AddSubassembly(sid, hs.Points[hookIdx]); }
        return sid;
    }

    static Dictionary<string, double> Talude(double corte, double aterro) => new()
    {
        ["FlatCutSlope"] = corte, ["MediumCutSlope"] = corte, ["SteepCutSlope"] = corte,
        ["FlatFillSlope"] = aterro, ["MediumFillSlope"] = aterro, ["SteepFillSlope"] = aterro,
    };

    [CommandMethod("PRJCORREDORES", CommandFlags.Modal)]
    public void Corredores()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Base + ".log");
        try
        {
            var plano = Plano(); var sec = plano.GetProperty("secao");
            double D(string k) => sec.GetProperty(k).GetDouble();
            double inc = D("decliv"), esp = D("esp_pav");
            var tal = Talude(D("talude_corte"), D("talude_aterro"));
            var codTop = new Dictionary<string, string> { ["SurfaceCodes"] = "Top,Datum" };
            Dictionary<string, string> Cod(string c) => new() { ["PointCodes"] = c, ["SurfaceCodes"] = "Top,Datum" };
            var mon = new Dictionary<string, ObjectId>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                double x0 = 665200, y0 = 7749600;
                // pista dupla (com e sem talude)
                foreach (var (chave, nomeM, talude) in new[] { ("dupla", "MONT - PISTA DUPLA 22m", true), ("oae", "MONT - PISTA DUPLA 22m - OAE", false) })
                {
                    var loc = new Point3d(x0, y0, 0); x0 += 80;
                    var asmId = cd.AssemblyCollection.Add(nomeM, AssemblyType.DividedCrownedRoad, loc);
                    string pf = chave == "dupla" ? "P22" : "OAE";
                    Sub(tr, cd, asmId, "BasicBarrier", pf + " - NEW JERSEY 0.60", null, null, null, ObjectId.Null, 0, loc);
                    foreach (var l in new[] { "D", "E" })
                    {
                        var s1 = Sub(tr, cd, asmId, "LinkWidthAndSlope", $"{pf} - BASE NJ ({l})", Lado(l), new() { ["Width"] = D("nj_meia"), ["Slope"] = 0 }, Cod("NJ"), ObjectId.Null, 0, loc);
                        var s2 = Sub(tr, cd, asmId, "BasicLane", $"{pf} - PISTA 7.20 ({l})", Lado(l), new() { ["Width"] = D("faixa_principal"), ["Slope"] = inc, ["Depth"] = esp }, null, s1, 1, loc);
                        var s3 = Sub(tr, cd, asmId, "BasicShoulder", $"{pf} - ACOSTAMENTO 2.50 ({l})", Lado(l), new() { ["Width"] = D("acost"), ["Slope"] = inc, ["Depth"] = esp }, null, s2, 1, loc);
                        var s4 = Sub(tr, cd, asmId, "LinkWidthAndSlope", $"{pf} - DRE 1.00 ({l})", Lado(l), new() { ["Width"] = D("dre"), ["Slope"] = inc }, Cod("DRE"), s3, 1, loc);
                        if (talude) Sub(tr, cd, asmId, "DaylightGeneral", $"{pf} - TALUDE ({l})", Lado(l), tal, null, s4, 1, loc);
                    }
                    mon[chave] = asmId; log.W("montagem ok: " + nomeM);
                }
                // ramos: faixa de 6,00 de um lado, 1,00 do outro
                foreach (var (chave, nomeM, lf, lo) in new[] { ("esquerda", "MONT - RAMO 7m - FAIXA A ESQUERDA", "E", "D"), ("direita", "MONT - RAMO 7m - FAIXA A DIREITA", "D", "E") })
                {
                    var loc = new Point3d(x0, y0, 0); x0 += 60;
                    var asmId = cd.AssemblyCollection.Add(nomeM, AssemblyType.UndividedPlanarRoad, loc);
                    string pf = "R" + lf;
                    var a1 = Sub(tr, cd, asmId, "BasicLane", $"{pf} - FAIXA 6.00 ({lf})", Lado(lf), new() { ["Width"] = D("faixa_ramo"), ["Slope"] = inc, ["Depth"] = esp }, null, ObjectId.Null, 0, loc);
                    var a2 = Sub(tr, cd, asmId, "LinkWidthAndSlope", $"{pf} - DRE 1.00 ({lf})", Lado(lf), new() { ["Width"] = D("dre"), ["Slope"] = inc }, Cod("DRE"), a1, 1, loc);
                    Sub(tr, cd, asmId, "DaylightGeneral", $"{pf} - TALUDE ({lf})", Lado(lf), tal, null, a2, 1, loc);
                    var b1 = Sub(tr, cd, asmId, "LinkWidthAndSlope", $"{pf} - FAIXA LATERAL 1.00 ({lo})", Lado(lo), new() { ["Width"] = D("lado_eixo_ramo"), ["Slope"] = inc }, Cod("DRE"), ObjectId.Null, 0, loc);
                    Sub(tr, cd, asmId, "DaylightGeneral", $"{pf} - TALUDE ({lo})", Lado(lo), tal, null, b1, 1, loc);
                    mon[chave] = asmId; log.W("montagem ok: " + nomeM);
                }
                // eixo central da trombeta: pista dos dois lados (largura por alvo), uma montagem por trecho
                foreach (var (chave, nomeM, pf) in new[] { ("central_ini", "MONT - EIXO CENTRAL TROMBETA - INICIO", "EC1"), ("central", "MONT - EIXO CENTRAL TROMBETA", "EC2") })
                {
                    var loc = new Point3d(x0, y0, 0); x0 += 60;
                    var asmId = cd.AssemblyCollection.Add(nomeM, AssemblyType.UndividedCrownedRoad, loc);
                    foreach (var l in new[] { "D", "E" })
                    {
                        var c1 = Sub(tr, cd, asmId, "LinkWidthAndSlope", $"{pf} - PISTA ({l})", Lado(l), new() { ["Width"] = chave == "central_ini" && l == "E" ? 1.0 : 7.50, ["Slope"] = inc }, Cod("BORDO_PISTA"), ObjectId.Null, 0, loc);
                        var c2 = Sub(tr, cd, asmId, "LinkWidthAndSlope", $"{pf} - DRE 1.00 ({l})", Lado(l), new() { ["Width"] = D("dre"), ["Slope"] = inc }, Cod("DRE"), c1, 1, loc);
                        Sub(tr, cd, asmId, "DaylightGeneral", $"{pf} - TALUDE ({l})", Lado(l), tal, null, c2, 1, loc);
                    }
                    mon[chave] = asmId; log.W("montagem ok: " + nomeM);
                }
                foreach (var kv in mon)
                {
                    var asm = (Assembly)tr.GetObject(kv.Value, OpenMode.ForRead);
                    foreach (AssemblyGroup g in asm.Groups)
                    {
                        var nomes = new List<string>();
                        foreach (ObjectId sid in g.GetSubassemblyIds()) { var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead); nomes.Add(sa.Name + "[" + (sa.HasSide ? sa.Side.ToString() : "-") + "]"); }
                        log.W($"   {asm.Name} / {g.Name}: " + string.Join(" > ", nomes));
                    }
                }
                tr.Commit();
            }

            // corredores
            var oae = plano.GetProperty("oae");
            foreach (var a in plano.GetProperty("alinhamentos").EnumerateArray())
            {
                if (!a.GetProperty("corredor").GetBoolean()) continue;
                string nome = a.GetProperty("nome").GetString(); string lado = a.GetProperty("lado_faixa").GetString();
                var sw = Stopwatch.StartNew();
                try
                {
                    ObjectId corId;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var alId = AlinhamentoPorNome(cd, tr, nome);
                        var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead);
                        ObjectId prId = ObjectId.Null;
                        foreach (ObjectId pid in al.GetProfileIds()) { var pr = (Profile)tr.GetObject(pid, OpenMode.ForRead); if (pr.Name == nome + " - GREIDE") prId = pid; }
                        var supId = Superficie(cd, tr, plano.GetProperty("superficie").GetString());
                        corId = cd.CorridorCollection.Add("COR - " + nome, "BL - " + nome, alId, prId, "REG 01", mon[lado]);
                        var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                        var bl = cor.Baselines[0];
                        if (lado == "dupla" && oae.GetProperty("eixo").GetString() == nome)
                        {
                            double e0 = oae.GetProperty("e0").GetDouble(), e1 = oae.GetProperty("e1").GetDouble();
                            bl.BaselineRegions[0].Split(e0);
                            foreach (BaselineRegion r in bl.BaselineRegions) if (Math.Abs(r.StartStation - e0) < 0.01) { r.Split(e1); break; }
                            foreach (BaselineRegion r in bl.BaselineRegions)
                                if (Math.Abs(r.StartStation - e0) < 0.01) { r.AssemblyId = mon["oae"]; try { r.Name = "OAE"; } catch { } }
                        }
                        double corte = 200.0;
                        if (lado == "central")
                        {
                            bl.BaselineRegions[0].Split(corte);
                            foreach (BaselineRegion r in bl.BaselineRegions) if (r.StartStation < corte - 0.01) { r.AssemblyId = mon["central_ini"]; try { r.Name = "INICIO (0-200)"; } catch { } }
                        }
                        int k = 0;
                        foreach (BaselineRegion r in bl.BaselineRegions)
                        {
                            var s = r.AppliedAssemblySetting;
                            s.FrequencyAlongTangents = 20; s.FrequencyAlongCurves = 10; s.FrequencyAlongSpirals = 10; s.FrequencyAlongProfileCurves = 10;
                            s.AppliedAtHorizontalGeometryPoints = true; s.AppliedAtProfileGeometryPoints = true;
                            log.W($"[{nome}] regiao {++k}: {r.StartStation:F3}-{r.EndStation:F3} montagem={((Assembly)tr.GetObject(r.AssemblyId, OpenMode.ForRead)).Name} freq={r.AppliedAssemblySetting.FrequencyAlongTangents}");
                        }
                        // alvos
                        // cand[trecho][lado]: trecho 0 = antes do corte, 1 = depois
                        var cand = new Dictionary<(int, SubassemblySideType), ObjectIdCollection>();
                        var melhor = new Dictionary<(int, SubassemblySideType), (ObjectId id, double med)>();
                        if (lado == "central")
                        {
                            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                            foreach (ObjectId id in ms)
                            {
                                if (tr.GetObject(id, OpenMode.ForRead) is not Polyline p || p.Layer != "REF-PRJ50998 F1-HZ-SARJETA") continue;
                                var am = new List<(double st, double of)>();
                                for (double d = 0; d <= p.Length; d += 1.0)
                                {
                                    var v = p.GetPointAtDist(d); double st = 0, of = 0; bool fora = false;
                                    try { al.StationOffsetAcceptOutOfRange(v.X, v.Y, ref st, ref of, ref fora); } catch { continue; }
                                    if (!fora && Math.Abs(of) < 12) am.Add((st, of));
                                }
                                for (int tr2 = 0; tr2 < 2; tr2++)
                                    foreach (var lg in new[] { SubassemblySideType.Left, SubassemblySideType.Right })
                                    {
                                        var ofs = am.Where(x => (tr2 == 0 ? x.st < corte : x.st >= corte) && (lg == SubassemblySideType.Right ? x.of > 0 : x.of < 0))
                                                    .Select(x => Math.Abs(x.of)).OrderBy(x => x).ToList();
                                        if (ofs.Count < 10) continue;
                                        double med = ofs[ofs.Count / 2];
                                        if (med > 9.5) { log.W($"[{nome}] sarjeta {p.Handle} trecho {tr2} {lg}: descartada (mediana {med:F2})"); continue; }
                                        log.W($"[{nome}] sarjeta {p.Handle} trecho {tr2} {lg}: {ofs.Count} am., mediana {med:F2}");
                                        if (!melhor.TryGetValue((tr2, lg), out var mm) || med > mm.med) melhor[(tr2, lg)] = (id, med);
                                    }
                            }
                            foreach (var kv in melhor) { cand[kv.Key] = new ObjectIdCollection { kv.Value.id }; log.W($"[{nome}] alvo escolhido trecho {kv.Key.Item1} {kv.Key.Item2}: {kv.Value.id.Handle} (mediana {kv.Value.med:F2})"); }
                        }
                        var ladoGrupo = new Dictionary<string, (int trecho, SubassemblySideType lado)>();
                        foreach (var (ch, trecho) in new[] { (lado, 1), ("central_ini", 0) })
                        {
                            if (!mon.ContainsKey(ch) || (ch == "central_ini" && lado != "central")) continue;
                            var asm = (Assembly)tr.GetObject(mon[ch], OpenMode.ForRead);
                            foreach (AssemblyGroup g in asm.Groups)
                                foreach (ObjectId sid in g.GetSubassemblyIds()) { var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead); if (sa.HasSide) { ladoGrupo[g.Name] = (trecho, sa.Side); break; } }
                        }
                        var tg = cor.GetTargets();
                        foreach (SubassemblyTargetInfo t in tg)
                        {
                            string acao = "";
                            if (t.TargetType == SubassemblyLogicalNameType.Surface) { t.TargetIds = new ObjectIdCollection { supId }; acao = "-> Primitivo"; }
                            else if (lado == "central" && t.TargetType == SubassemblyLogicalNameType.Offset && t.SubassemblyName.Contains("PISTA") && ladoGrupo.TryGetValue(t.AssemblyGroupName, out var lg) && cand.TryGetValue(lg, out var alvo) && alvo.Count > 0)
                            {
                                var ids = new ObjectIdCollection(); foreach (ObjectId x in alvo) ids.Add(x);
                                if (ids.Count == 1) ids.Add(alvo[0]);
                                try { t.TargetIds = alvo; acao = $"-> {alvo.Count} sarjeta(s) REF (trecho {lg.trecho}, {lg.lado})"; try { if (alvo.Count >= 2) t.UseSameSideTarget = true; } catch { } }
                                catch (System.Exception ex) { acao = "-> alvo recusado: " + ex.Message; }
                            }
                            log.W($"[{nome}] alvo: {t.AssemblyGroupName} / {t.SubassemblyName} | {t.LogicalName} | {t.TargetType} {acao}");
                        }
                        cor.SetTargets(tg);
                        tr.Commit();
                    }
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                        cor.Rebuild();
                        var codigos = cor.GetLinkCodes();
                        log.W($"[{nome}] rebuild 1 ok em {sw.Elapsed.TotalSeconds:F1}s; codigos de link: " + string.Join(",", codigos));
                        tr.Commit();
                    }
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                        var codigos = cor.GetLinkCodes();
                        foreach (var (suf, cands) in new[] { ("TOPO", new[] { "Top", "Topo", "TOPO", "Superior" }), ("DATUM", new[] { "Datum", "DATUM", "Plataforma", "Base" }) })
                        {
                            try
                            {
                                var cod = cands.FirstOrDefault(c => codigos.Contains(c));
                                if (cod == null) { log.W($"[{nome}] sem codigo para {suf}"); continue; }
                                var cs = cor.CorridorSurfaces.Add("SUP - " + nome + " - " + suf);
                                cs.AddLinkCode(cod, true);
                                try { cs.Boundaries.AddCorridorExtentsBoundary("LIMITE"); } catch (System.Exception ex) { log.W($"[{nome}] limite {suf}: {ex.Message}"); }
                                log.W($"[{nome}] superficie {suf} com codigo {cod}");
                            }
                            catch (System.Exception ex) { log.W($"[{nome}] superficie {suf} erro {ex.Message}"); }
                        }
                        cor.Rebuild();
                        tr.Commit();
                    }
                    log.W($"[{nome}] corredor ok em {sw.Elapsed.TotalSeconds:F1}s");
                }
                catch (System.Exception ex) { log.W($"[{nome}] ERRO corredor {ex}"); }
            }
            if (Environment.GetEnvironmentVariable("PRJ_REBUILDALL") == "1") try { var sw2 = Stopwatch.StartNew(); cd.CorridorCollection.RebuildAll(); log.W($"RebuildAll ok em {sw2.Elapsed.TotalSeconds:F1}s"); } catch (System.Exception ex) { log.W("RebuildAll erro " + ex.Message); }
            Msg("PRJCORREDORES ok");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); Msg("PRJCORREDORES ERRO " + ex.Message); }
    }

    // ---------------- ETAPA D ----------------
    [CommandMethod("PRJCONFERIR", CommandFlags.Modal)]
    public void Conferir()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        var res = new Dictionary<string, object>();
        using var log = new Log(Base + ".log");
        using var tr = db.TransactionManager.StartTransaction();
        var sups = new List<object>();
        foreach (ObjectId sid in cd.GetSurfaceIds())
        {
            try
            {
                var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(sid, OpenMode.ForRead); var p = s.GetGeneralProperties();
                sups.Add(new { s.Name, pontos = p.NumberOfPoints, zmin = Math.Round(p.MinimumElevation, 3), zmax = Math.Round(p.MaximumElevation, 3) });
            }
            catch (System.Exception ex) { sups.Add("erro " + ex.Message); }
        }
        res["superficies"] = sups; log.W("superficies ok");
        var als = new List<object>();
        foreach (ObjectId aid in cd.GetAlignmentIds())
        {
            var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead); log.W("alinhamento " + al.Name);
            var ents = new List<object>();
            for (int i = 0; i < al.Entities.Count; i++)
            {
                var ae = al.Entities.GetEntityByOrder(i); var subs = new List<object>();
                for (int k = 0; k < ae.SubEntityCount; k++) subs.Add(Alinhamentos.Sub(ae[k]));
                ents.Add(new { tipo = ae.EntityType.ToString(), subs });
            }
            var perfis = new List<object>();
            foreach (ObjectId pid in al.GetProfileIds())
            {
                var pr = (Profile)tr.GetObject(pid, OpenMode.ForRead); var pv = new List<object>(); log.W("  perfil " + pr.Name);
                if (pr.ProfileType != ProfileType.EG) foreach (ProfilePVI v in pr.PVIs) pv.Add(new[] { Math.Round(v.RawStation, 3), Math.Round(v.Elevation, 3), Math.Round(v.VerticalCurve?.Length ?? 0, 3) });
                var amostra = new List<double[]>();
                for (double st = al.StartingStation; st <= al.EndingStation; st += 20) { try { amostra.Add(new[] { Math.Round(st, 1), Math.Round(pr.ElevationAt(st), 3) }); } catch { } }
                perfis.Add(new { pr.Name, tipo = pr.ProfileType.ToString(), pvis = pv, amostra });
            }
            als.Add(new { al.Name, comp = Math.Round(al.Length, 3), ents, perfis });
        }
        res["alinhamentos"] = als;
        var cors = new List<object>();
        foreach (ObjectId cid in cd.CorridorCollection)
        {
            var c = (Corridor)tr.GetObject(cid, OpenMode.ForRead); log.W("corredor " + c.Name);
            var bls = new List<object>();
            foreach (Baseline b in c.Baselines)
            {
                var regs = new List<object>();
                foreach (BaselineRegion r in b.BaselineRegions) regs.Add(new { r.Name, e0 = Math.Round(r.StartStation, 3), e1 = Math.Round(r.EndStation, 3), montagem = ((Assembly)tr.GetObject(r.AssemblyId, OpenMode.ForRead)).Name, estacoes = r.SortedStations().Length });
                bls.Add(new { b.Name, regs });
            }
            var cs = new List<object>();
            foreach (CorridorSurface s in c.CorridorSurfaces)
            {
                try { var ts = (TinSurface)tr.GetObject(s.SurfaceId, OpenMode.ForRead); var p = ts.GetGeneralProperties(); cs.Add(new { s.Name, pontos = p.NumberOfPoints, zmin = Math.Round(p.MinimumElevation, 2), zmax = Math.Round(p.MaximumElevation, 2) }); }
                catch (System.Exception ex) { cs.Add(new { s.Name, erro = ex.Message }); }
            }
            var amostras = new List<double[]>();
            try
            {
                ObjectId topoId = ObjectId.Null;
                foreach (CorridorSurface s in c.CorridorSurfaces) if (s.Name.EndsWith("TOPO")) topoId = s.SurfaceId;
                var b0 = c.Baselines[0];
                var al = (Alignment)tr.GetObject(b0.AlignmentId, OpenMode.ForRead); var pr = (Profile)tr.GetObject(b0.ProfileId, OpenMode.ForRead);
                var ts = topoId.IsNull ? null : (TinSurface)tr.GetObject(topoId, OpenMode.ForRead);
                log.W("  amostras");
                bool central = c.Name.Contains("CENTRAL");
                var offs = central ? Enumerable.Range(-24, 49).Select(i => i * 0.5).ToArray() : new[] { -6.0, -2.0, 2.0, 6.0, 10.5 };
                for (double st = al.StartingStation + 10; st < al.EndingStation; st += central ? 20 : 50)
                    foreach (double of in offs)
                    {
                        double x = 0, y = 0; al.PointLocation(st, of, ref x, ref y);
                        double z = -9999; try { if (ts != null) z = ts.FindElevationAtXY(x, y); } catch { }
                        amostras.Add(new[] { Math.Round(st, 1), of, Math.Round(pr.ElevationAt(st), 3), Math.Round(z, 3) });
                    }
            }
            catch { }
            cors.Add(new { c.Name, desatualizado = c.IsOutOfDate, bls, cs, amostras });
        }
        res["corredores"] = cors;
        var asms = new List<object>();
        foreach (ObjectId id in cd.AssemblyCollection) { var a = (Assembly)tr.GetObject(id, OpenMode.ForRead); asms.Add(new { a.Name, grupos = a.Groups.Count }); }
        res["montagens"] = asms;
        tr.Commit();
        log.W("serializando");
        File.WriteAllText(Base, JsonSerializer.Serialize(res, new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }));
        log.W("fim");
        Msg("PRJCONFERIR ok");
    }
}
