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

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Variante))]

namespace Projeta.Reconstrutor;

public class Variante
{
    const string NOME = "EIXO 01 - PRINCIPAL";
    const string NOME_ORIG = "EIXO 01 - PRINCIPAL (ORIGINAL PRJ-50998)";

    static ObjectId Estilo(Transaction tr, System.Collections.IEnumerable ids, string nome)
    {
        foreach (var o in ids) { if (o is not ObjectId id) continue; try { var s = tr.GetObject(id, OpenMode.ForRead) as StyleBase; if (s != null && s.Name.Trim().Equals(nome, StringComparison.OrdinalIgnoreCase)) return id; } catch { } }
        return ObjectId.Null;
    }
    static ObjectId Alin(CivilDocument cd, Transaction tr, string nome)
    {
        foreach (ObjectId id in cd.GetAlignmentIds()) { var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); if (a.Name == nome) return id; }
        return ObjectId.Null;
    }

    // Etapa 1: novo eixo (trecho norte igual + variante da linha vermelha) e perfil do terreno
    [CommandMethod("PRJVAR1", CommandFlags.Modal)]
    public void Etapa1()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Resumo.Saida("prj_var1") + ".log");
        try
        {
            var vj = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_VARIANTE"))).RootElement;
            double s0 = vj.GetProperty("s0").GetDouble();
            var amostras = new List<double[]>();
            using var tr = db.TransactionManager.StartTransaction();
            var oldId = Alin(cd, tr, NOME);
            var old = (Alignment)tr.GetObject(oldId, OpenMode.ForWrite);
            var tans = new List<(Point2d a, Point2d b)>(); var scs = new List<(double l1, double r, double l2)>();
            for (int i = 0; i < old.Entities.Count; i++)
            {
                var ae = old.Entities.GetEntityByOrder(i);
                if (ae is AlignmentLine && ae[0] is AlignmentSubEntityLine ln) tans.Add((ln.StartPoint, ln.EndPoint));
                else if (ae is AlignmentSCS sc) scs.Add((sc.SpiralIn.Length, sc.Arc.Radius, sc.SpiralOut.Length));
            }
            log.W($"eixo original: {tans.Count} tangentes, {scs.Count} SCS");
            string estilo = old.StyleName, camada = old.Layer;
            old.Name = NOME_ORIG;
            var labelSet = "_PRJ 100x20 (INFRA)";
            var id = Alignment.Create(cd, NOME, "", camada, estilo, labelSet);
            var al = (Alignment)tr.GetObject(id, OpenMode.ForWrite);
            var tid = new List<int>();
            for (int i = 0; i <= 6; i++)
            {
                var (a, b) = tans[i];
                if (i == 6) { var u = (b - a).GetNormal(); b = a + u * Math.Max(s0, 0.5); }
                tid.Add(al.Entities.AddFixedLine(new Point3d(a.X, a.Y, 0), new Point3d(b.X, b.Y, 0)).EntityId);
            }
            for (int i = 0; i < 6; i++) al.Entities.AddFreeSCS(tid[i], tid[i + 1], scs[i].l1, scs[i].l2, SpiralParamType.Length, scs[i].r, false, SpiralType.Clothoid);
            int prev = tid[6];
            foreach (var e in vj.GetProperty("els").EnumerateArray())
            {
                double L = e.GetProperty("L").GetDouble(); if (L < 0.01) continue;
                if (e.GetProperty("t").GetString() == "L") prev = al.Entities.AddFixedLine(prev, L).EntityId;
                else
                {
                    double R = e.GetProperty("R").GetDouble(); bool cw = e.GetProperty("sentido").GetInt32() < 0;
                    prev = al.Entities.AddFloatingCurve(prev, R, L, CurveParamType.CurveLength, cw).EntityId;
                }
            }
            double xe = 0, ye = 0; al.PointLocation(al.EndingStation, 0, ref xe, ref ye);
            var fim = vj.GetProperty("fim");
            log.W($"novo eixo: L={al.Length:F3} entidades={al.Entities.Count} fim=({xe:F3},{ye:F3}) erro_fim={Math.Sqrt(Math.Pow(xe - fim[0].GetDouble(), 2) + Math.Pow(ye - fim[1].GetDouble(), 2)):F4} m");
            // perfil do terreno
            ObjectId supId = ObjectId.Null;
            foreach (ObjectId sid in cd.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(sid, OpenMode.ForRead); if (s.Name == "Primitivo") supId = sid; }
            ObjectId tnOld = ObjectId.Null;
            foreach (ObjectId pid in old.GetProfileIds()) { var p = (Profile)tr.GetObject(pid, OpenMode.ForWrite); if (p.ProfileType == ProfileType.EG) tnOld = pid; p.Name = p.Name.Replace(NOME, NOME_ORIG); }
            var pOld = (Profile)tr.GetObject(tnOld, OpenMode.ForRead);
            var tnId = Profile.CreateFromSurface(NOME + " - TN", id, supId, pOld.LayerId, pOld.StyleId, Estilo(tr, cd.Styles.LabelSetStyles.ProfileLabelSetStyles, "TERRENO"));
            var tn = (Profile)tr.GetObject(tnId, OpenMode.ForRead);
            for (double st = 0; st <= al.EndingStation + 1e-6; st += 5)
            {
                double z = -9999; try { z = tn.ElevationAt(st); } catch { }
                double x = 0, y = 0; al.PointLocation(st, 0, ref x, ref y);
                amostras.Add(new[] { Math.Round(st, 3), Math.Round(z, 3), Math.Round(x, 3), Math.Round(y, 3) });
            }
            // entidades para conferencia
            var ents = new List<object>();
            for (int i = 0; i < al.Entities.Count; i++)
            {
                var ae = al.Entities.GetEntityByOrder(i); var subs = new List<object>();
                for (int k = 0; k < ae.SubEntityCount; k++) subs.Add(Alinhamentos.Sub(ae[k]));
                ents.Add(new { tipo = ae.EntityType.ToString(), subs });
            }
            tr.Commit();
            File.WriteAllText(Resumo.Saida("prj_var1.json"), JsonSerializer.Serialize(new { comp = al.Length, amostras, ents }, new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }));
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJVAR1 ok\n");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJVAR1 ERRO " + ex.Message + "\n"); }
    }

    static void SetProp(object o, string prop, object val, Log log, string rot)
    {
        try { var p = o.GetType().GetProperty(prop); if (p == null) { log.W($"{rot}: sem {prop}"); return; } p.SetValue(o, val); }
        catch (System.Exception ex) { log.W($"{rot}: {prop} erro {ex.InnerException?.Message ?? ex.Message}"); }
    }

    // Etapa 2: greide novo, corredor novo com a secao PRJ, vista de perfil INFRA
    [CommandMethod("PRJVAR2", CommandFlags.Modal)]
    public void Etapa2()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Resumo.Saida("prj_var2") + ".log");
        try
        {
            var gj = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("PRJ_GREIDE"))).RootElement;
            var pv = gj.GetProperty("pvis").EnumerateArray().Select(x => (s: x[0].GetDouble(), z: x[1].GetDouble(), L: x[2].GetDouble())).ToList();
            ObjectId alId, prId, tnId, supId = ObjectId.Null;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                alId = Alin(cd, tr, NOME); var oldId = Alin(cd, tr, NOME_ORIG);
                var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead); var old = (Alignment)tr.GetObject(oldId, OpenMode.ForRead);
                foreach (ObjectId sid in cd.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(sid, OpenMode.ForRead); if (s.Name == "Primitivo") supId = sid; }
                Profile fgOld = null;
                foreach (ObjectId pid in old.GetProfileIds()) { var p = (Profile)tr.GetObject(pid, OpenMode.ForRead); if (p.ProfileType != ProfileType.EG) fgOld = p; }
                tnId = ObjectId.Null; foreach (ObjectId pid in al.GetProfileIds()) { var p = (Profile)tr.GetObject(pid, OpenMode.ForRead); if (p.ProfileType == ProfileType.EG) tnId = pid; }
                var rot = Estilo(tr, cd.Styles.LabelSetStyles.ProfileLabelSetStyles, "PRJ-PADRÃO PERFIL COM LABELS");
                prId = Profile.CreateByLayout(NOME + " - GREIDE", alId, fgOld.LayerId, fgOld.StyleId, rot);
                var pr = (Profile)tr.GetObject(prId, OpenMode.ForWrite);
                if (Math.Abs(pv[^1].s - al.EndingStation) > 0.001) { log.W($"ultimo PIV {pv[^1].s} -> fim {al.EndingStation}"); pv[^1] = (al.EndingStation, pv[^1].z, 0); }
                pr.Entities.AddFixedTangent(new Point2d(pv[0].s, pv[0].z), new Point2d(pv[^1].s, pv[^1].z));
                for (int i = 1; i < pv.Count - 1; i++) pr.PVIs.AddPVI(pv[i].s, pv[i].z);
                for (int i = 1; i < pv.Count - 1; i++) if (pv[i].L > 0) pr.Entities.AddFreeSymmetricParabolaByPVIAndCurveLength(pr.PVIs.GetPVIAt(pv[i].s, pv[i].z), pv[i].L);
                var txt = new List<string>(); foreach (ProfilePVI v in pr.PVIs) txt.Add($"{v.RawStation:F3}/{v.Elevation:F3}/{(v.VerticalCurve == null ? 0 : v.VerticalCurve.Length):F1}");
                log.W("greide: " + string.Join("  ", txt));
                // corredor antigo do eixo principal sai (o eixo original fica como referencia)
                foreach (ObjectId cid in cd.CorridorCollection.Cast<ObjectId>().ToList())
                {
                    var c = (Corridor)tr.GetObject(cid, OpenMode.ForRead);
                    if (c.Baselines.Count > 0 && c.Baselines[0].AlignmentId == oldId)
                    {
                        var sups = new List<ObjectId>(); foreach (CorridorSurface cs in c.CorridorSurfaces) sups.Add(cs.SurfaceId);
                        c.UpgradeOpen(); c.Erase();
                        foreach (var sid in sups) { try { var so = tr.GetObject(sid, OpenMode.ForWrite, true); if (!so.IsErased) so.Erase(); } catch { } }
                        log.W("corredor do eixo original removido: " + c.Name);
                    }
                }
                tr.Commit();
            }
            ObjectId corId;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectId asm = ObjectId.Null, asmOae = ObjectId.Null;
                foreach (ObjectId aid in cd.AssemblyCollection) { var a = (Assembly)tr.GetObject(aid, OpenMode.ForRead); if (a.Name == "PRJ - PISTA DUPLA 22m") asm = aid; if (a.Name == "PRJ - PISTA DUPLA 22m - OAE") asmOae = aid; }
                corId = cd.CorridorCollection.Add("COR - " + NOME, "BL - " + NOME, alId, prId, "REG 01", asm);
                var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                var bl = cor.Baselines[0];
                bl.BaselineRegions[0].Split(2300.0);
                foreach (BaselineRegion r in bl.BaselineRegions) if (Math.Abs(r.StartStation - 2300) < 0.01) { r.Split(2540.0); break; }
                foreach (BaselineRegion r in bl.BaselineRegions) if (Math.Abs(r.StartStation - 2300) < 0.01) { r.AssemblyId = asmOae; try { r.Name = "OAE"; } catch { } }
                foreach (BaselineRegion r in bl.BaselineRegions)
                {
                    var s = r.AppliedAssemblySetting;
                    s.FrequencyAlongTangents = 20; s.FrequencyAlongCurves = 10; s.FrequencyAlongSpirals = 10; s.FrequencyAlongProfileCurves = 10;
                    s.AppliedAtHorizontalGeometryPoints = true; s.AppliedAtProfileGeometryPoints = true;
                    log.W($"regiao {r.StartStation:F1}-{r.EndStation:F1}: {((Assembly)tr.GetObject(r.AssemblyId, OpenMode.ForRead)).Name}");
                }
                var cs0 = Estilo(tr, cd.Styles.CodeSetStyles, "PLANTA GEOMETRIA"); if (!cs0.IsNull) SetProp(cor, "CodeSetStyleId", cs0, log, "codeset");
                var tg = cor.GetTargets();
                foreach (SubassemblyTargetInfo t in tg) if (t.TargetType == SubassemblyLogicalNameType.Surface) t.TargetIds = new ObjectIdCollection { supId };
                cor.SetTargets(tg);
                tr.Commit();
            }
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var cor = (Corridor)tr.GetObject(corId, OpenMode.ForWrite);
                cor.Rebuild();
                var cods = cor.GetLinkCodes();
                var tal = new[] { "TALUDECORTE", "TALUDEATERRO", "BANQUETACORTE", "BANQUETAATERRO" };
                foreach (var (suf, base0) in new[] { ("TOPO", new[] { "Top", "TOPO", "Drenagem" }), ("DATUM", new[] { "Datum", "DATUM" }) })
                {
                    var cs = cor.CorridorSurfaces.Add("SUP - " + NOME + " - " + suf);
                    foreach (var c in base0.Concat(tal).Where(c => cods.Contains(c))) try { cs.AddLinkCode(c, true); } catch { }
                    try { cs.Boundaries.AddCorridorExtentsBoundary("LIMITE"); } catch (System.Exception ex) { log.W("limite: " + ex.Message); }
                    log.W($"superficie {suf}: " + string.Join(",", cs.LinkCodes()));
                }
                cor.Rebuild();
                tr.Commit();
            }
            // vista de perfil no padrao INFRA
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pvStyle = Estilo(tr, cd.Styles.ProfileViewStyles, "_PERFIL_20x5 (INFRA)");
                var bandSet = Estilo(tr, cd.Styles.ProfileViewBandSetStyles, "_PERFIL 1000 (INFRA)");
                var old = (Alignment)tr.GetObject(Alin(cd, tr, NOME_ORIG), OpenMode.ForRead);
                foreach (ObjectId vid in old.GetProfileViewIds()) { var v = (ProfileView)tr.GetObject(vid, OpenMode.ForWrite); try { v.Name = "PV - " + NOME_ORIG; } catch { } }
                double ymin = double.MaxValue;
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                foreach (ObjectId id in ms) if (id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(ProfileView)))) { try { var v = (ProfileView)tr.GetObject(id, OpenMode.ForRead); ymin = Math.Min(ymin, v.GeometricExtents.MinPoint.Y); } catch { } }
                var pvId = ProfileView.Create(alId, new Point3d(666000, ymin - 3000, 0), "PV - " + NOME, bandSet, pvStyle);
                var pvw = (ProfileView)tr.GetObject(pvId, OpenMode.ForWrite);
                try
                {
                    var itens = pvw.Bands.GetBottomBandItems();
                    foreach (ProfileViewBandItem it in itens) { try { it.Profile1Id = tnId; it.Profile2Id = prId; } catch { } }
                    pvw.Bands.SetBottomBandItems(itens);
                }
                catch (System.Exception ex) { log.W("bandas: " + ex.Message); }
                var e = pvw.GeometricExtents; var alt = e.MaxPoint.Y - e.MinPoint.Y;
                pvw.Location = new Point3d(666000, ymin - 200 - alt, 0);
                log.W($"vista de perfil criada abaixo das demais (y={pvw.Location.Y:F1}, altura {alt:F1})");
                tr.Commit();
            }
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJVAR2 ok\n");
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJVAR2 ERRO " + ex.Message + "\n"); }
    }

    // Volumes: superficie DATUM do corredor do eixo principal x Primitivo
    [CommandMethod("PRJVOL", CommandFlags.Modal)]
    public void Volumes()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        using var log = new Log(Resumo.Saida("prj_vol") + ".log");
        var res = new Dictionary<string, object>();
        try
        {
            using var tr = db.TransactionManager.StartTransaction();
            ObjectId supId = ObjectId.Null;
            foreach (ObjectId sid in cd.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(sid, OpenMode.ForRead); if (s.Name == "Primitivo") supId = sid; }
            foreach (ObjectId cid in cd.CorridorCollection)
            {
                var c = (Corridor)tr.GetObject(cid, OpenMode.ForRead);
                if (c.Name != "COR - " + NOME) continue;
                foreach (CorridorSurface cs in c.CorridorSurfaces)
                {
                    if (!cs.Name.EndsWith("DATUM")) continue;
                    var vid = TinVolumeSurface.Create("VOL TEMP " + Guid.NewGuid().ToString("N")[..6], supId, cs.SurfaceId);
                    var vs = (TinVolumeSurface)tr.GetObject(vid, OpenMode.ForRead);
                    var vp = vs.GetVolumeProperties();
                    var d = new Dictionary<string, double>();
                    foreach (var p in vp.GetType().GetProperties()) { try { if (p.GetValue(vp) is double v) d[p.Name] = Math.Round(v, 1); } catch { } }
                    res[cs.Name] = d; log.W(cs.Name + ": " + string.Join(", ", d.Select(k => k.Key + "=" + k.Value)));
                }
                var al = (Alignment)tr.GetObject(c.Baselines[0].AlignmentId, OpenMode.ForRead);
                res["eixo"] = new { al.Name, comp = Math.Round(al.Length, 2) };
            }
            tr.Abort();
        }
        catch (System.Exception ex) { log.W("ERRO " + ex); }
        File.WriteAllText(Resumo.Saida("prj_vol.json"), JsonSerializer.Serialize(res));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPRJVOL ok\n");
    }
}

