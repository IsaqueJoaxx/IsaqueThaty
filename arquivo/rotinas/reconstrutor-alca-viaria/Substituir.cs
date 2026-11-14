using System.Diagnostics;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Assembly = Autodesk.Civil.DatabaseServices.Assembly;
using StyleBase = Autodesk.Civil.DatabaseServices.Styles.StyleBase;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Substituir))]

namespace Projeta.Reconstrutor;

public class Substituir
{
    static ObjectId Estilo(Transaction tr, IEnumerable<ObjectId> ids, string nome)
    {
        foreach (var id in ids) { try { var s = tr.GetObject(id, OpenMode.ForRead) as StyleBase; if (s != null && s.Name.Trim().Equals(nome, StringComparison.OrdinalIgnoreCase)) return id; } catch { } }
        return ObjectId.Null;
    }
    static IEnumerable<ObjectId> Ids(System.Collections.IEnumerable col) { foreach (var o in col) if (o is ObjectId id) yield return id; }

    static ObjectId Modelo(Transaction tr, CivilDocument cd, string montagem, string macro, SubassemblySideType? lado, Log log)
    {
        foreach (ObjectId aid in cd.AssemblyCollection)
        {
            var a = (Assembly)tr.GetObject(aid, OpenMode.ForRead);
            if (!a.Name.Trim().Equals(montagem, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (AssemblyGroup g in a.Groups)
                foreach (ObjectId sid in g.GetSubassemblyIds())
                {
                    var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead);
                    string m = sa.GeometryGenerator?.MacroOrClassName ?? "";
                    if (m.Contains(macro) && (!lado.HasValue || !sa.HasSide || sa.Side == lado.Value)) { log.W($"modelo {macro} ({lado}) <- {a.Name.Trim()} / {sa.Name}"); return sid; }
                }
        }
        log.W($"MODELO NAO ENCONTRADO: {montagem} / {macro} / {lado}");
        return ObjectId.Null;
    }

    static ObjectId Copiar(Transaction tr, ObjectId asmId, ObjectId modelo, ObjectId hook, int hookIdx, string nome, SubassemblySideType? lado, Dictionary<string, double> pd, Dictionary<string, string> ps = null)
    {
        var asm = (Assembly)tr.GetObject(asmId, OpenMode.ForWrite);
        ObjectId nid;
        if (hook.IsNull) { var g = asm.CopySubassembly(modelo); var ids = g.GetSubassemblyIds(); nid = ids[ids.Count - 1]; }
        else { var hs = (Subassembly)tr.GetObject(hook, OpenMode.ForRead); nid = asm.CopySubassembly(modelo, hs.Points[hookIdx]); }
        var sa = (Subassembly)tr.GetObject(nid, OpenMode.ForWrite);
        try { sa.Name = nome; } catch { }
        if (lado.HasValue)
        {
            if (sa.HasSide) sa.Side = lado.Value;
            foreach (var p in sa.ParamsLong) if (p.Key == "Side") p.Value = lado == SubassemblySideType.Left ? 1 : 0;
        }
        if (pd != null) foreach (var p in sa.ParamsDouble) if (pd.TryGetValue(p.Key, out var v)) p.Value = v;
        if (ps != null) foreach (var p in sa.ParamsString) if (ps.TryGetValue(p.Key, out var v)) p.Value = v;
        return nid;
    }

    static ObjectId Estoque(Transaction tr, CivilDocument cd, ObjectId asmId, string cls, string nome, SubassemblySideType? lado, Dictionary<string, double> pd, Dictionary<string, string> ps, ObjectId hook, int hookIdx, Point3d loc)
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

    static void SetProp(object o, string prop, object val, Log log, string rot)
    {
        try { var p = o.GetType().GetProperty(prop); if (p == null) { log.W($"{rot}: sem propriedade {prop}"); return; } p.SetValue(o, val); log.W($"{rot}: {prop} ok"); }
        catch (System.Exception ex) { log.W($"{rot}: {prop} erro {ex.InnerException?.Message ?? ex.Message}"); }
    }

    [CommandMethod("PRJSUBSTITUIR", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Resumo.Saida("prj_subst") + ".log");
        try
        {
            var plano = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_PLANO"))).RootElement;
            var sec = plano.GetProperty("secao");
            double D(string k) => sec.GetProperty(k).GetDouble();
            var L = SubassemblySideType.Left; var R = SubassemblySideType.Right;
            var mon = new Dictionary<string, ObjectId>();
            ObjectId asmStyle, asmCodeSet = ObjectId.Null;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var mLane = Modelo(tr, cd, "SEÇÃO VIARIO 3.5M", "LaneSuperelevationAOR", R, log);
                var mGuia = Modelo(tr, cd, "SEÇÃO VIARIO 3.5M", "BasicCurbAndGutter", R, log);
                var mTalD = Modelo(tr, cd, "TALUDE LD", "TALUDE_NOTA", R, log);
                var mTalE = Modelo(tr, cd, "TALUDE LE", "TALUDE_NOTA", L, log);
                if (mLane.IsNull || mGuia.IsNull || mTalD.IsNull || mTalE.IsNull) throw new InvalidOperationException("faltam montagens modelo do template");
                // estilo e codigo da montagem igual ao template
                ObjectId refAsm = ObjectId.Null;
                foreach (ObjectId aid in cd.AssemblyCollection) { var a = (Assembly)tr.GetObject(aid, OpenMode.ForRead); if (a.Name.Trim() == "SEÇÃO VIARIO 3.5M") refAsm = aid; }
                var ra = (Assembly)tr.GetObject(refAsm, OpenMode.ForRead);
                asmStyle = ra.StyleId;
                try { asmCodeSet = (ObjectId)(ra.GetType().GetProperty("CodeSetStyleId")?.GetValue(ra) ?? ObjectId.Null); } catch { }
                log.W($"estilo montagem: {ra.StyleName} codeset: {(asmCodeSet.IsNull ? "-" : ((StyleBase)tr.GetObject(asmCodeSet, OpenMode.ForRead)).Name)}");

                double x0 = 665200, y0 = 7749700;
                ObjectId Nova(string nome, AssemblyType tipo)
                {
                    var loc = new Point3d(x0, y0, 0); x0 += 70;
                    return asmCodeSet.IsNull ? cd.AssemblyCollection.Add(nome, tipo, loc) : cd.AssemblyCollection.Add(nome, tipo, loc, asmStyle, asmCodeSet);
                }
                Dictionary<string, double> Faixa(double w) => new() { ["Width"] = w, ["Lane Width"] = w };
                var dre = new Dictionary<string, double> { ["GutterWidth"] = D("dre") - 0.12, ["CurbWidth"] = 0.12 };
                ObjectId Tal(SubassemblySideType l) => l == L ? mTalE : mTalD;
                string S(SubassemblySideType l) => l == L ? "E" : "D";

                foreach (var (chave, nomeM, talude) in new[] { ("dupla", "PRJ - PISTA DUPLA 22m", true), ("oae", "PRJ - PISTA DUPLA 22m - OAE", false) })
                {
                    var a = Nova(nomeM, AssemblyType.DividedCrownedRoad); string pf = chave == "dupla" ? "P22" : "OAE";
                    var loc = ((Assembly)tr.GetObject(a, OpenMode.ForRead)).Location;
                    Estoque(tr, cd, a, "BasicBarrier", pf + " - NEW JERSEY 0.60", null, null, null, ObjectId.Null, 0, loc);
                    foreach (var l in new[] { R, L })
                    {
                        var s1 = Estoque(tr, cd, a, "LinkWidthAndSlope", $"{pf} - BASE NJ ({S(l)})", l, new() { ["Width"] = D("nj_meia"), ["Slope"] = 0 }, new() { ["PointCodes"] = "NJ", ["SurfaceCodes"] = "Top,Datum" }, ObjectId.Null, 0, loc);
                        var s2 = Copiar(tr, a, mLane, s1, 1, $"{pf} - PISTA 7.20 ({S(l)})", l, Faixa(D("faixa_principal")));
                        var s3 = Copiar(tr, a, mLane, s2, 1, $"{pf} - ACOSTAMENTO 2.50 ({S(l)})", l, Faixa(D("acost")));
                        if (talude)
                        {
                            var s4 = Copiar(tr, a, mGuia, s3, 1, $"{pf} - DRE 1.00 ({S(l)})", l, dre);
                            Copiar(tr, a, Tal(l), s4, 3, $"{pf} - TALUDE DER-SP ({S(l)})", l, null);
                        }
                        else Estoque(tr, cd, a, "LinkWidthAndSlope", $"{pf} - GUARDA-RODAS 1.00 ({S(l)})", l, new() { ["Width"] = D("dre"), ["Slope"] = D("decliv") }, new() { ["PointCodes"] = "GUARDA_RODAS", ["SurfaceCodes"] = "Top,Datum" }, s3, 1, loc);
                    }
                    mon[chave] = a;
                }
                foreach (var (chave, nomeM, lf, lo) in new[] { ("esquerda", "PRJ - RAMO 7m - FAIXA A ESQUERDA", L, R), ("direita", "PRJ - RAMO 7m - FAIXA A DIREITA", R, L) })
                {
                    var a = Nova(nomeM, AssemblyType.UndividedPlanarRoad); string pf = "R" + S(lf);
                    var a1 = Copiar(tr, a, mLane, ObjectId.Null, 0, $"{pf} - FAIXA 6.00 ({S(lf)})", lf, Faixa(D("faixa_ramo")));
                    var a2 = Copiar(tr, a, mGuia, a1, 1, $"{pf} - DRE 1.00 ({S(lf)})", lf, dre);
                    Copiar(tr, a, Tal(lf), a2, 3, $"{pf} - TALUDE DER-SP ({S(lf)})", lf, null);
                    var b1 = Copiar(tr, a, mGuia, ObjectId.Null, 0, $"{pf} - DRE 1.00 ({S(lo)})", lo, dre);
                    Copiar(tr, a, Tal(lo), b1, 3, $"{pf} - TALUDE DER-SP ({S(lo)})", lo, null);
                    mon[chave] = a;
                }
                foreach (var (chave, nomeM, pf) in new[] { ("central_ini", "PRJ - EIXO CENTRAL TROMBETA - INICIO", "EC1"), ("central", "PRJ - EIXO CENTRAL TROMBETA", "EC2") })
                {
                    var a = Nova(nomeM, AssemblyType.UndividedCrownedRoad);
                    foreach (var l in new[] { R, L })
                    {
                        var c1 = Copiar(tr, a, mLane, ObjectId.Null, 0, $"{pf} - PISTA ({S(l)})", l, Faixa(chave == "central_ini" && l == L ? 1.0 : 7.5));
                        var c2 = Copiar(tr, a, mGuia, c1, 1, $"{pf} - DRE 1.00 ({S(l)})", l, dre);
                        Copiar(tr, a, Tal(l), c2, 3, $"{pf} - TALUDE DER-SP ({S(l)})", l, null);
                    }
                    mon[chave] = a;
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

            var mapa = new Dictionary<string, string>
            {
                ["MONT - PISTA DUPLA 22m"] = "dupla", ["MONT - PISTA DUPLA 22m - OAE"] = "oae",
                ["MONT - RAMO 7m - FAIXA A ESQUERDA"] = "esquerda", ["MONT - RAMO 7m - FAIXA A DIREITA"] = "direita",
                ["MONT - EIXO CENTRAL TROMBETA - INICIO"] = "central_ini", ["MONT - EIXO CENTRAL TROMBETA"] = "central",
            };
            var antigas = new HashSet<ObjectId>();
            ObjectId supId, codeSet;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                supId = ObjectId.Null;
                foreach (ObjectId id in cd.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead); if (s.Name == plano.GetProperty("superficie").GetString()) supId = id; }
                codeSet = Estilo(tr, Ids(cd.Styles.CodeSetStyles), "PLANTA GEOMETRIA");
                log.W("codeset PLANTA GEOMETRIA: " + (codeSet.IsNull ? "NAO ENCONTRADO" : "ok"));
                tr.Commit();
            }
            var taludeCod = new[] { "TALUDECORTE", "TALUDEATERRO", "BANQUETACORTE", "BANQUETAATERRO" };
            foreach (ObjectId cid in cd.CorridorCollection)
            {
                string nome = "?"; var sw = Stopwatch.StartNew();
                try
                {
                    bool central = false;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var cor = (Corridor)tr.GetObject(cid, OpenMode.ForWrite); nome = cor.Name;
                        var ladoGrupo = new Dictionary<string, (int trecho, SubassemblySideType lado)>();
                        foreach (Baseline bl in cor.Baselines)
                            foreach (BaselineRegion r in bl.BaselineRegions)
                            {
                                var velha = (Assembly)tr.GetObject(r.AssemblyId, OpenMode.ForRead);
                                if (!mapa.TryGetValue(velha.Name, out var chave)) { log.W($"[{nome}] regiao {r.StartStation:F1}-{r.EndStation:F1}: montagem {velha.Name} mantida"); continue; }
                                antigas.Add(r.AssemblyId);
                                r.AssemblyId = mon[chave];
                                if (chave.StartsWith("central")) central = true;
                                log.W($"[{nome}] regiao {r.StartStation:F1}-{r.EndStation:F1}: {velha.Name} -> {((Assembly)tr.GetObject(mon[chave], OpenMode.ForRead)).Name}");
                                var na = (Assembly)tr.GetObject(mon[chave], OpenMode.ForRead);
                                foreach (AssemblyGroup g in na.Groups)
                                    foreach (ObjectId sid in g.GetSubassemblyIds()) { var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead); if (sa.HasSide) { ladoGrupo[g.Name] = (chave == "central_ini" ? 0 : 1, sa.Side); break; } }
                            }
                        if (!codeSet.IsNull) SetProp(cor, "CodeSetStyleId", codeSet, log, $"[{nome}] codeset");
                        // alvos de largura do eixo central (sarjetas de referencia)
                        var cand = new Dictionary<(int, SubassemblySideType), ObjectIdCollection>();
                        if (central)
                        {
                            var al = (Alignment)tr.GetObject(cor.Baselines[0].AlignmentId, OpenMode.ForRead);
                            var melhor = new Dictionary<(int, SubassemblySideType), (ObjectId id, double med)>();
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
                                for (int t2 = 0; t2 < 2; t2++)
                                    foreach (var lg in new[] { L, R })
                                    {
                                        var ofs = am.Where(x => (t2 == 0 ? x.st < 200 : x.st >= 200) && (lg == R ? x.of > 0 : x.of < 0)).Select(x => Math.Abs(x.of)).OrderBy(x => x).ToList();
                                        if (ofs.Count < 10) continue;
                                        double med = ofs[ofs.Count / 2]; if (med > 9.5) continue;
                                        if (!melhor.TryGetValue((t2, lg), out var mm) || med > mm.med) melhor[(t2, lg)] = (id, med);
                                    }
                            }
                            foreach (var kv in melhor) cand[kv.Key] = new ObjectIdCollection { kv.Value.id };
                        }
                        var tg = cor.GetTargets();
                        foreach (SubassemblyTargetInfo t in tg)
                        {
                            string acao = "";
                            if (t.TargetType == SubassemblyLogicalNameType.Surface) { t.TargetIds = new ObjectIdCollection { supId }; acao = "-> Primitivo"; }
                            else if (central && t.TargetType == SubassemblyLogicalNameType.Offset && t.SubassemblyName.Contains("PISTA") && ladoGrupo.TryGetValue(t.AssemblyGroupName, out var lg) && cand.TryGetValue(lg, out var alvo))
                            { try { t.TargetIds = alvo; acao = $"-> sarjeta REF (trecho {lg.trecho}, {lg.lado})"; } catch (System.Exception ex) { acao = "recusado " + ex.Message; } }
                            if (acao != "" || t.TargetType == SubassemblyLogicalNameType.Surface) log.W($"[{nome}] alvo: {t.SubassemblyName} | {t.LogicalName} | {t.TargetType} {acao}");
                        }
                        cor.SetTargets(tg);
                        tr.Commit();
                    }
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var cor = (Corridor)tr.GetObject(cid, OpenMode.ForWrite);
                        cor.Rebuild();
                        var cods = cor.GetLinkCodes();
                        log.W($"[{nome}] rebuild ok; links: " + string.Join(",", cods) + " | shapes: " + string.Join(",", cor.GetShapeCodes()));
                        foreach (CorridorSurface cs in cor.CorridorSurfaces)
                        {
                            var extra = cs.Name.EndsWith("DATUM") ? new[] { "DATUM" } : new[] { "TOPO", "Drenagem" };
                            var add = new List<string>();
                            foreach (var c in taludeCod.Concat(extra).Where(c => cods.Contains(c)))
                                try { cs.AddLinkCode(c, true); add.Add(c); } catch { }
                            log.W($"[{nome}] {cs.Name}: + " + string.Join(",", add) + " => " + string.Join(",", cs.LinkCodes()));
                        }
                        cor.Rebuild();
                        tr.Commit();
                    }
                    log.W($"[{nome}] ok em {sw.Elapsed.TotalSeconds:F1}s");
                }
                catch (System.Exception ex) { log.W($"[{nome}] ERRO {ex}"); }
            }
            // remove as montagens genericas antigas
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId aid in cd.AssemblyCollection.Cast<ObjectId>().ToList())
                {
                    var a = (Assembly)tr.GetObject(aid, OpenMode.ForRead);
                    if (!mapa.ContainsKey(a.Name)) continue;
                    try
                    {
                        var subs = new List<ObjectId>(); foreach (AssemblyGroup g in a.Groups) foreach (ObjectId s in g.GetSubassemblyIds()) subs.Add(s);
                        a.UpgradeOpen(); a.Erase();
                        foreach (var s in subs) { try { var so = tr.GetObject(s, OpenMode.ForWrite); if (!so.IsErased) so.Erase(); } catch { } }
                        log.W("montagem antiga removida: " + a.Name);
                    }
                    catch (System.Exception ex) { log.W("nao removi " + a.Name + ": " + ex.Message); }
                }
                tr.Commit();
            }
            // vistas de perfil no padrao INFRA
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pvStyle = Estilo(tr, Ids(cd.Styles.ProfileViewStyles), "_PERFIL_20x5 (INFRA)");
                var bandSet = Estilo(tr, Ids(cd.Styles.ProfileViewBandSetStyles), "_PERFIL 1000 (INFRA)");
                log.W($"estilo vista: {(pvStyle.IsNull ? "NAO ENCONTRADO" : "ok")}  bandas: {(bandSet.IsNull ? "NAO ENCONTRADO" : "ok")}");
                var vistas = new List<(ProfileView pv, double y)>();
                foreach (ObjectId aid in cd.GetAlignmentIds())
                {
                    var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
                    ObjectId tn = ObjectId.Null, fg = ObjectId.Null;
                    foreach (ObjectId pid in al.GetProfileIds()) { var pr = (Profile)tr.GetObject(pid, OpenMode.ForRead); if (pr.ProfileType == ProfileType.EG) tn = pid; else fg = pid; }
                    foreach (ObjectId vid in al.GetProfileViewIds())
                    {
                        var pv = (ProfileView)tr.GetObject(vid, OpenMode.ForWrite);
                        if (!pvStyle.IsNull) pv.StyleId = pvStyle;
                        if (!bandSet.IsNull)
                        {
                            try
                            {
                                pv.Bands.ImportBandSetStyle(bandSet);
                                var itens = pv.Bands.GetBottomBandItems();
                                foreach (ProfileViewBandItem it in itens) { try { if (!tn.IsNull) it.Profile1Id = tn; if (!fg.IsNull) it.Profile2Id = fg.IsNull ? tn : fg; } catch { } }
                                pv.Bands.SetBottomBandItems(itens);
                            }
                            catch (System.Exception ex) { log.W($"bandas {pv.Name}: {ex.Message}"); }
                        }
                        if (Math.Abs(pv.Location.X - 666000) < 0.5) vistas.Add((pv, pv.Location.Y));
                    }
                }
                // reempilha as vistas (a altura muda com o novo exagero)
                double y = 7749300;
                foreach (var (pv, _) in vistas.OrderBy(v => v.y))
                {
                    pv.Location = new Point3d(666000, y, 0);
                    try { var e = pv.GeometricExtents; y = e.MaxPoint.Y + 150; } catch { y += 600; }
                }
                log.W($"vistas de perfil atualizadas: {vistas.Count}");
                tr.Commit();
            }
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJSUBSTITUIR ok\n");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJSUBSTITUIR ERRO " + ex.Message + "\n"); }
    }
}
