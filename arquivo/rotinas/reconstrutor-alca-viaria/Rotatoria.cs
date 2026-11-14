using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Rotatoria))]

namespace Projeta.Reconstrutor;

// PRJROTATORIA: cria alinhamento + perfil do anel, montagens, corredor "ROTATORIA" (anel + ligacoes dos ramos) e superficie.
// Le PRJ_ROT (JSON). Rodar SEMPRE em copia do DWG.
public class Rotatoria
{
    static ObjectId AlPorNome(CivilDocument cd, Transaction tr, string n)
    { foreach (ObjectId id in cd.GetAlignmentIds()) if (((Alignment)tr.GetObject(id, OpenMode.ForRead)).Name == n) return id; return ObjectId.Null; }
    static ObjectId PrPorNome(Transaction tr, ObjectId alId, string n)
    { var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead); foreach (ObjectId id in al.GetProfileIds()) if (((Profile)tr.GetObject(id, OpenMode.ForRead)).Name == n) return id; return ObjectId.Null; }

    static ObjectId Sub(Transaction tr, CivilDocument cd, ObjectId asmId, string cls, string nome, SubassemblySideType? lado,
        Dictionary<string, double> pd, ObjectId hook, int hookIdx, Point3d loc)
    {
        var sid = cd.SubassemblyCollection.ImportStockSubassembly(nome, "Subassembly." + cls, loc);
        var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForWrite);
        if (lado.HasValue)
        {
            if (sa.HasSide) sa.Side = lado.Value;
            foreach (var p in sa.ParamsLong) if (p.Key == "Side") p.Value = lado == SubassemblySideType.Left ? 1 : 0;
        }
        if (pd != null) foreach (var p in sa.ParamsDouble) if (pd.TryGetValue(p.Key, out var v)) p.Value = v;
        var asm = (Assembly)tr.GetObject(asmId, OpenMode.ForWrite);
        if (hook.IsNull) asm.AddSubassembly(sid);
        else { var hs = (Subassembly)tr.GetObject(hook, OpenMode.ForRead); asm.AddSubassembly(sid, hs.Points[hookIdx]); }
        return sid;
    }

    static Dictionary<string, double> Faixa(double w, double inc) => new()
    { ["Width"] = w, ["Lane Width"] = w, ["DefaultSlope"] = inc, ["Lane Slope"] = inc, ["Pave1Depth"] = 0.05, ["Pave2Depth"] = 0, ["BaseDepth"] = 0.15, ["SubBaseDepth"] = 0.15 };
    static readonly Dictionary<string, double> MeioFioSarjeta = new() { ["CurbDepth"] = 0.35, ["CurbHeight"] = 0.18, ["CurbWidth"] = 0.12, ["GutterSlope"] = -0.15, ["GutterWidth"] = 0.5 };

    [CommandMethod("PRJROTATORIA", CommandFlags.Modal)]
    public void Rodar()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Resumo.Saida("prj_rot") + ".log");
        var J = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_ROT"))).RootElement;
        double[] V(JsonElement e) => e.EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var res = new Dictionary<string, object>();
        try
        {
            var c = V(J.GetProperty("centro")); double R = J.GetProperty("raio").GetDouble(); double th0 = J.GetProperty("theta0").GetDouble() * Math.PI / 180;
            string nomeAl = J.GetProperty("alinhamento").GetString(), nomePr = J.GetProperty("perfil").GetString();
            ObjectId alId, prId; var mon = new Dictionary<string, ObjectId>();
            // ---------- alinhamento + perfil
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var refAl = (Alignment)tr.GetObject(AlPorNome(cd, tr, J.GetProperty("ref_alinhamento").GetString()), OpenMode.ForRead);
                var refPr = (Profile)tr.GetObject(PrPorNome(tr, refAl.ObjectId, J.GetProperty("ref_perfil").GetString()), OpenMode.ForRead);
                ObjectId alLbl = ObjectId.Null; foreach (ObjectId x in cd.Styles.LabelSetStyles.AlignmentLabelSetStyles) { alLbl = x; break; }
                double eps = J.GetProperty("folga_rad").GetDouble();
                Point2d Q(double a) => new Point2d(c[0] + R * Math.Cos(a), c[1] + R * Math.Sin(a));
                var plc = new Polyline(); plc.AddVertexAt(0, Q(th0), 1.0, 0, 0); plc.AddVertexAt(1, Q(th0 + Math.PI), Math.Tan((Math.PI - eps) / 4), 0, 0); plc.AddVertexAt(2, Q(th0 + 2 * Math.PI - eps), 0, 0, 0);
                plc.SetDatabaseDefaults(db); plc.LayerId = refAl.LayerId;
                var msw = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite); msw.AppendEntity(plc); tr.AddNewlyCreatedDBObject(plc, true);
                var opc = new PolylineOptions { PlineId = plc.ObjectId, AddCurvesBetweenTangents = false, EraseExistingEntities = true };
                alId = Alignment.Create(cd, opc, nomeAl, ObjectId.Null, refAl.LayerId, refAl.StyleId, alLbl);
                var al = (Alignment)tr.GetObject(alId, OpenMode.ForWrite);
                log.W($"alinhamento {nomeAl}: L={al.Length:F3} entidades={al.Entities.Count} e0={al.StartingStation:F3} e1={al.EndingStation:F3}");
                res["anel_L"] = Math.Round(al.Length, 3);
                ObjectId prLbl = ObjectId.Null; foreach (ObjectId x in cd.Styles.LabelSetStyles.ProfileLabelSetStyles) { prLbl = x; break; }
                prId = Profile.CreateByLayout(nomePr, alId, refPr.LayerId, refPr.StyleId, prLbl);
                var pr = (Profile)tr.GetObject(prId, OpenMode.ForWrite);
                var pv = J.GetProperty("pvis").EnumerateArray().Select(x => V(x)).ToList();
                double fim = al.EndingStation;
                pr.Entities.AddFixedTangent(new Point2d(pv[0][0], pv[0][1]), new Point2d(fim, pv[^1][1]));
                for (int i = 1; i < pv.Count - 1; i++) pr.PVIs.AddPVI(pv[i][0], pv[i][1]);
                for (int i = 1; i < pv.Count - 1; i++) if (pv[i][2] > 0) pr.Entities.AddFreeSymmetricParabolaByPVIAndCurveLength(pr.PVIs.GetPVIAt(pv[i][0], pv[i][1]), pv[i][2]);
                var txt = new List<string>(); foreach (ProfilePVI v in pr.PVIs) txt.Add($"{v.RawStation:F2}/{v.Elevation:F3}/{(v.VerticalCurve == null ? 0 : v.VerticalCurve.Length):F1}");
                log.W("perfil: " + string.Join("  ", txt)); res["perfil"] = txt;
                tr.Commit();
            }
            // ---------- montagens
            using (var tr = db.TransactionManager.StartTransaction())
            {
                double x0 = c[0] + 300, y0 = c[1] + 200;
                foreach (var (ch, nm, comMF) in new[] { ("mf", "ROTATORIA - ANEL COM MEIO-FIO", true), ("ab", "ROTATORIA - ANEL ABERTO", false) })
                {
                    var loc = new Point3d(x0, y0, 0); x0 += 40;
                    var asmId = cd.AssemblyCollection.Add(nm, AssemblyType.UndividedPlanarRoad, loc);
                    string pf = ch == "mf" ? "ROT MF" : "ROT AB";
                    var e1 = Sub(tr, cd, asmId, "LaneSuperelevationAOR", pf + " - FAIXA INTERNA 3.50", SubassemblySideType.Left, Faixa(3.5, 0.02), ObjectId.Null, 0, loc);
                    Sub(tr, cd, asmId, "BasicCurb", pf + " - MEIO-FIO ILHA", SubassemblySideType.Left, new() { ["CurbHeight"] = 0.15, ["CurbWidth"] = 0.15, ["CurbDepth"] = 0.35 }, e1, 1, loc);
                    var d1 = Sub(tr, cd, asmId, "LaneSuperelevationAOR", pf + " - FAIXA EXTERNA 4.00", SubassemblySideType.Right, Faixa(4.0, -0.02), ObjectId.Null, 0, loc);
                    if (comMF) Sub(tr, cd, asmId, "BasicCurbAndGutter", pf + " - MEIO-FIO E SARJETA", SubassemblySideType.Right, MeioFioSarjeta, d1, 1, loc);
                    mon[ch] = asmId;
                }
                foreach (var lg in J.GetProperty("ligacoes").EnumerateArray())
                {
                    string tag = lg.GetProperty("tag").GetString();
                    var loc = new Point3d(x0, y0, 0); x0 += 40;
                    var asmId = cd.AssemblyCollection.Add("ROTATORIA - LIGACAO " + tag, AssemblyType.UndividedCrownedRoad, loc);
                    var e1 = Sub(tr, cd, asmId, "LaneSuperelevationAOR", $"LIG {tag} - FAIXA (E)", SubassemblySideType.Left, Faixa(lg.GetProperty("larg_esq").GetDouble(), -0.03), ObjectId.Null, 0, loc);
                    Sub(tr, cd, asmId, "BasicCurbAndGutter", $"LIG {tag} - MEIO-FIO (E)", SubassemblySideType.Left, MeioFioSarjeta, e1, 1, loc);
                    var d1 = Sub(tr, cd, asmId, "LaneSuperelevationAOR", $"LIG {tag} - FAIXA (D)", SubassemblySideType.Right, Faixa(lg.GetProperty("larg_dir").GetDouble(), -0.03), ObjectId.Null, 0, loc);
                    Sub(tr, cd, asmId, "BasicCurbAndGutter", $"LIG {tag} - MEIO-FIO (D)", SubassemblySideType.Right, MeioFioSarjeta, d1, 1, loc);
                    mon["lig_" + tag] = asmId;
                }
                if (J.TryGetProperty("estilos", out var es))
                {
                    var st = cd.Styles;
                    ObjectId Est(dynamic col, string n) { try { return (ObjectId)col[n]; } catch (System.Exception ex) { log.W($"estilo {n} nao encontrado: {ex.Message}"); return ObjectId.Null; } }
                    var aSt = Est(st.AssemblyStyles, es.GetProperty("montagem").GetString());
                    var aCs = Est(st.CodeSetStyles, es.GetProperty("codeset_montagem").GetString());
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead); string camM = es.GetProperty("camada_montagem").GetString();
                    foreach (var kv in mon)
                    {
                        var a = (Assembly)tr.GetObject(kv.Value, OpenMode.ForWrite);
                        try { if (!aSt.IsNull) a.StyleId = aSt; } catch (System.Exception ex) { log.W("estilo montagem: " + ex.Message); }
                        try { if (!aCs.IsNull) a.CodeSetStyleId = aCs; } catch (System.Exception ex) { log.W("codeset montagem: " + ex.Message); }
                        try { if (lt.Has(camM)) a.Layer = camM; } catch (System.Exception ex) { log.W("camada montagem: " + ex.Message); }
                    }
                    log.W("estilos aplicados nas montagens");
                }
                foreach (var kv in mon)
                {
                    var asm = (Assembly)tr.GetObject(kv.Value, OpenMode.ForRead);
                    foreach (AssemblyGroup g in asm.Groups)
                    {
                        var nomes = new List<string>();
                        foreach (ObjectId sid in g.GetSubassemblyIds()) { try { var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForRead); nomes.Add(sa.Name + "[" + (sa.HasSide ? sa.Side.ToString() : "-") + "]"); } catch (System.Exception ex) { nomes.Add("erro:" + ex.Message); } }
                        string gn = "?"; try { gn = g.Name; } catch { } log.W($"montagem {kv.Key} / {gn}: " + string.Join(" > ", nomes));
                    }
                }
                tr.Commit();
            }
            // ---------- corredor
            ObjectId corId;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                corId = cd.CorridorCollection.Add(J.GetProperty("corredor").GetString(), "ANEL", alId, prId, "ANEL", mon["ab"]);
                var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                try { cor.Layer = J.GetProperty("camada_corredor").GetString(); } catch { }
                if (J.TryGetProperty("estilos", out var es2)) { try { cor.CodeSetStyleId = cd.Styles.CodeSetStyles[es2.GetProperty("codeset_corredor").GetString()]; log.W("code set do corredor: " + es2.GetProperty("codeset_corredor").GetString()); } catch (System.Exception ex) { log.W("codeset corredor: " + ex.Message); } try { cor.StyleId = cd.Styles.CorridorStyles[es2.GetProperty("corredor").GetString()]; } catch (System.Exception ex) { log.W("estilo corredor: " + ex.Message); } }
                var bl = cor.Baselines[0];
                var regs = J.GetProperty("regioes").EnumerateArray().Select(x => (s0: x.GetProperty("s0").GetDouble(), s1: x.GetProperty("s1").GetDouble(), mf: x.GetProperty("meio_fio").GetBoolean(), nome: x.GetProperty("nome").GetString())).ToList();
                foreach (var r in regs.Skip(1)) { foreach (BaselineRegion br in bl.BaselineRegions) if (br.StartStation < r.s0 - 0.01 && br.EndStation > r.s0 + 0.01) { br.Split(r.s0); break; } }
                foreach (BaselineRegion br in bl.BaselineRegions)
                {
                    var r = regs.OrderBy(x => Math.Abs(x.s0 - br.StartStation)).First();
                    br.AssemblyId = r.mf ? mon["mf"] : mon["ab"]; try { br.Name = r.nome; } catch { }
                    var s = br.AppliedAssemblySetting; s.FrequencyAlongCurves = 2; s.FrequencyAlongTangents = 2; s.AppliedAtHorizontalGeometryPoints = true; s.AppliedAtProfileGeometryPoints = true;
                    log.W($"anel regiao {r.nome}: {br.StartStation:F2}-{br.EndStation:F2} meio-fio={r.mf}");
                }
                foreach (var lg in J.GetProperty("ligacoes").EnumerateArray())
                {
                    string tag = lg.GetProperty("tag").GetString();
                    var lal = AlPorNome(cd, tr, lg.GetProperty("alinhamento").GetString());
                    var lpr = PrPorNome(tr, lal, lg.GetProperty("perfil").GetString());
                    var b = cor.Baselines.Add("LIGACAO " + tag, lal, lpr);
                    var rg = b.BaselineRegions.Add("LIGACAO " + tag, mon["lig_" + tag], lg.GetProperty("s0").GetDouble(), lg.GetProperty("s1").GetDouble());
                    var s = rg.AppliedAssemblySetting; s.FrequencyAlongCurves = 2; s.FrequencyAlongTangents = 2; s.FrequencyAlongProfileCurves = 2; s.AppliedAtHorizontalGeometryPoints = true; s.AppliedAtProfileGeometryPoints = true;
                    log.W($"ligacao {tag}: {rg.StartStation:F2}-{rg.EndStation:F2}");
                }
                // alvos de largura das ligacoes
                var alvoIds = new Dictionary<string, ObjectIdCollection>();
                foreach (var lg in J.GetProperty("ligacoes").EnumerateArray())
                    foreach (var lado in new[] { "esq", "dir" })
                    {
                        var ids = new ObjectIdCollection();
                        foreach (var a in lg.GetProperty("alvo_" + lado).EnumerateArray())
                        {
                            string s = a.GetString();
                            if (s.StartsWith("h:")) ids.Add(db.GetObjectId(false, new Handle(Convert.ToInt64(s[2..], 16)), 0));
                            else ids.Add(AlPorNome(cd, tr, s));
                        }
                        alvoIds[$"LIG {lg.GetProperty("tag").GetString()} - FAIXA ({(lado == "esq" ? "E" : "D")})"] = ids;
                    }
                {
                    var ids = new ObjectIdCollection();
                    foreach (var a in J.GetProperty("alvos_anel").EnumerateArray()) { string s = a.GetString(); if (s.StartsWith("h:")) ids.Add(db.GetObjectId(false, new Handle(Convert.ToInt64(s[2..], 16)), 0)); else ids.Add(AlPorNome(cd, tr, s)); }
                    alvoIds["ROT MF - FAIXA EXTERNA 4.00"] = ids;
                }
                var tg = cor.GetTargets();
                foreach (SubassemblyTargetInfo t in tg)
                {
                    string acao = "";
                    if (t.TargetType == SubassemblyLogicalNameType.Offset && alvoIds.TryGetValue(t.SubassemblyName, out var ids))
                    {
                        try { t.TargetIds = ids; acao = $"-> {ids.Count} alvo(s)"; try { t.UseSameSideTarget = true; } catch { } } catch (System.Exception ex) { acao = "-> recusado: " + ex.Message; }
                    }
                    log.W($"alvo: {t.SubassemblyName} | {t.LogicalName} | {t.TargetType} {acao}");
                }
                cor.SetTargets(tg);
                tr.Commit();
            }
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                cor.Rebuild();
                log.W("rebuild 1 ok; codigos: " + string.Join(",", cor.GetLinkCodes()));
                tr.Commit();
            }
            // ---------- superficie
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                var codigos = cor.GetLinkCodes();
                foreach (var (suf, cod) in new[] { ("TOPO", "Top"), ("DATUM", "Datum") })
                {
                    if (!codigos.Contains(cod)) { log.W("sem codigo " + cod); continue; }
                    var cs = cor.CorridorSurfaces.Add("SUP - ROTATORIA - " + suf);
                    cs.AddLinkCode(cod, true);
                    if (suf != "TESTE") try { cs.Boundaries.AddCorridorExtentsBoundary("LIMITE"); } catch (System.Exception ex) { log.W("limite: " + ex.Message); }
                    if (suf != "TESTE") foreach (var ilha in J.GetProperty("ilhas").EnumerateArray())
                    {
                        string nm = ilha.GetProperty("nome").GetString();
                        var pts = new Point3dCollection(); foreach (var p in ilha.GetProperty("v").EnumerateArray()) { var a = V(p); pts.Add(new Point3d(a[0], a[1], 0)); }
                        try
                        {
                            var bcol = cs.Boundaries; var tb = bcol.GetType();
                            var mAdd = tb.GetMethods().FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length >= 2 && m.GetParameters()[1].ParameterType == typeof(Point3dCollection));
                            if (mAdd != null) { var args = new object[mAdd.GetParameters().Length]; args[0] = nm; args[1] = pts; for (int k2 = 2; k2 < args.Length; k2++) { var pt = mAdd.GetParameters()[k2].ParameterType; args[k2] = pt == typeof(bool) ? (object)false : pt.IsEnum ? Enum.GetValues(pt).GetValue(0) : null; } var b = mAdd.Invoke(bcol, args); log.W("ilha " + nm + " adicionada via " + mAdd); try { b?.GetType().GetProperty("BoundaryType")?.SetValue(b, CorridorSurfaceBoundaryType.InsideBoundary); } catch (System.Exception ex2) { log.W("tipo: " + ex2.Message); } }
                        }
                        catch (System.Exception ex) { log.W($"ilha {nm}: {ex.Message}"); }
                    }
                    if (J.TryGetProperty("estilos", out var es3)) { try { var ts = (TinSurface)tr.GetObject(cs.SurfaceId, OpenMode.ForWrite); ts.StyleId = cd.Styles.SurfaceStyles[es3.GetProperty("superficie_" + suf.ToLower()).GetString()]; } catch (System.Exception ex) { log.W("estilo superficie: " + ex.Message); } }
                    log.W($"superficie {suf} ({cod}) criada");
                }
                tr.Commit();
            }
            using (var tr = db.TransactionManager.StartTransaction()) { var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite); cor.Rebuild(); log.W("rebuild 2 ok"); tr.Commit(); }
            // ---------- relatorio: superficie TOPO (triangulos) e secoes
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var cor = (Corridor)tr.GetObject(corId, OpenMode.ForRead);
                var sups = new List<object>(); var tris = new List<double[]>();
                foreach (CorridorSurface cs in cor.CorridorSurfaces)
                {
                    var s = (TinSurface)tr.GetObject(cs.SurfaceId, OpenMode.ForRead);
                    try { var gp = s.GetGeneralProperties(); sups.Add(new { cs.Name, pontos = gp.NumberOfPoints, zmin = Math.Round(gp.MinimumElevation, 3), zmax = Math.Round(gp.MaximumElevation, 3) }); log.W($"{cs.Name}: {gp.NumberOfPoints} pontos z {gp.MinimumElevation:F2}..{gp.MaximumElevation:F2}"); } catch (System.Exception ex) { sups.Add(new { cs.Name, erro = ex.Message }); log.W(cs.Name + ": " + ex.Message); }
                    foreach (CorridorSurfaceBoundary bb in cs.Boundaries) { try { log.W($"limite {cs.Name}: {bb.Name} tipo={bb.BoundaryType} extents={bb.IsCorridorExtents}"); } catch { } }
                    if (cs.Name.EndsWith("TOPO"))
                        foreach (TinSurfaceTriangle t in s.Triangles)
                        {
                            if (!t.IsVisible) continue;
                            var a = t.Vertex1.Location; var b = t.Vertex2.Location; var d = t.Vertex3.Location;
                            tris.Add(new[] { Math.Round(a.X, 3), Math.Round(a.Y, 3), Math.Round(a.Z, 3), Math.Round(b.X, 3), Math.Round(b.Y, 3), Math.Round(b.Z, 3), Math.Round(d.X, 3), Math.Round(d.Y, 3), Math.Round(d.Z, 3) });
                        }
                }
                res["superficies"] = sups; res["triangulos_topo"] = tris;
                var bls = new List<object>();
                foreach (Baseline b in cor.Baselines) { var rr = new List<object>(); foreach (BaselineRegion r in b.BaselineRegions) { int n = -1; try { n = r.SortedStations().Length; } catch { } rr.Add(new { r.Name, e0 = Math.Round(r.StartStation, 2), e1 = Math.Round(r.EndStation, 2), estacoes = n }); log.W($"BL {b.Name} / {r.Name}: {r.StartStation:F2}-{r.EndStation:F2} estacoes={n}"); } bls.Add(new { b.Name, regioes = rr }); }
                try { log.W("corredor desatualizado=" + cor.IsOutOfDate); } catch { }
                res["baselines"] = bls;
                tr.Commit();
            }
            res["ok"] = true;
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJROTATORIA ok\n");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); res["erro"] = ex.ToString(); AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJROTATORIA ERRO " + ex.Message + "\n"); }
        File.WriteAllText(Resumo.Saida("prj_rot.json"), JsonSerializer.Serialize(res));
    }
    // PRJSUPTRI: exporta os triangulos visiveis da superficie PRJ_SUP (nome) para JSON
    [CommandMethod("PRJSUPTRI", CommandFlags.Modal)]
    public void SupTri()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        string nome = Environment.GetEnvironmentVariable("PRJ_SUP"); var tris = new List<double[]>(); string info = "";
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (ObjectId id in cd.GetSurfaceIds())
            {
                var s = tr.GetObject(id, OpenMode.ForRead) as TinSurface; if (s == null || s.Name != nome) continue;
                var gp = s.GetGeneralProperties(); info = $"{s.Name}: {gp.NumberOfPoints} pontos";
                foreach (TinSurfaceTriangle t in s.Triangles)
                {
                    if (!t.IsVisible) continue;
                    var a = t.Vertex1.Location; var b = t.Vertex2.Location; var d = t.Vertex3.Location;
                    tris.Add(new[] { Math.Round(a.X, 3), Math.Round(a.Y, 3), Math.Round(a.Z, 3), Math.Round(b.X, 3), Math.Round(b.Y, 3), Math.Round(b.Z, 3), Math.Round(d.X, 3), Math.Round(d.Y, 3), Math.Round(d.Z, 3) });
                }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_suptri.json"), JsonSerializer.Serialize(new { info, tris }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJSUPTRI {info} visiveis={tris.Count}\n");
    }
    // PRJCORPONTOS: pontos calculados (XYZ, offset, codigos) das secoes do corredor PRJ_COR
    [CommandMethod("PRJCORPONTOS", CommandFlags.Modal)]
    public void CorPontos()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        string nome = Environment.GetEnvironmentVariable("PRJ_COR"); var res = new List<object>(); var log = new List<string>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (ObjectId cid in cd.CorridorCollection)
            {
                var cor = (Corridor)tr.GetObject(cid, OpenMode.ForRead); if (cor.Name != nome) continue;
                foreach (Baseline bl in cor.Baselines)
                    foreach (BaselineRegion rg in bl.BaselineRegions)
                        foreach (double st in rg.SortedStations())
                        {
                            try
                            {
                                var aa = bl.GetAppliedAssemblyAtStation(st);
                                var pts = new List<object>();
                                foreach (CalculatedPoint p in aa.Points)
                                {
                                    var so = p.StationOffsetElevationToBaseline; var x = p.XYZ;
                                    pts.Add(new { o = Math.Round(so.Y, 3), x = Math.Round(x.X, 3), y = Math.Round(x.Y, 3), z = Math.Round(x.Z, 3), c = string.Join("|", p.CorridorCodes) });
                                }
                                res.Add(new { bl = bl.Name, rg = rg.Name, st = Math.Round(st, 3), pts });
                            }
                            catch (System.Exception ex) { log.Add($"{bl.Name} {st:F2}: {ex.Message}"); }
                        }
            }
            tr.Commit();
        }
        File.WriteAllText(Resumo.Saida("prj_corpontos.json"), JsonSerializer.Serialize(new { secoes = res, log }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJCORPONTOS secoes={res.Count} erros={log.Count}\n");
    }
}