using System.Reflection;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Terraplenagem;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(ProjetaTeste.TesteSecoes))]

namespace ProjetaTeste;

/// <summary>Teste sem tela do PRJCUBACAO: lista de materiais do Civil × cálculo pelas superfícies amostradas.</summary>
public sealed class TesteSecoes
{
    [CommandMethod("PRJSECTESTE")]
    public static void Rodar()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        const string arq = @"C:\Temp\nsteste\secoes\resultado.txt";
        File.WriteAllText(arq, "");
        void L(string s) => File.AppendAllText(arq, s + Environment.NewLine);
        try
        {
            var redes = typeof(Projeta.Civil3D.App).Assembly;
            var tC = redes.GetType("Projeta.Civil3D.Civil.CubacaoCivil", true)!;
            var tE = redes.GetType("Projeta.Civil3D.Civil.CubacaoCivil+Entrada", true)!;
            object? Inv(string m, params object[] a) => tC.GetMethod(m, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, a);
            L($"TinVolumeSurface base: {typeof(TinVolumeSurface).BaseType?.Name}");
            using var tr = db.TransactionManager.StartTransaction();
            var grupos = (System.Collections.IList)Inv("Grupos", tr)!;
            L($"grupos: {grupos.Count}");
            var g0 = grupos[0]!;
            var gid = (ObjectId)g0.GetType().GetProperty("Id")!.GetValue(g0)!;
            L($"grupo: {g0.GetType().GetProperty("Rotulo")!.GetValue(g0)}");
            var fontes = (System.Collections.IList)Inv("Fontes", tr, gid)!;
            foreach (var f in fontes) L($"   fonte: {f!.GetType().GetProperty("Rotulo")!.GetValue(f)}");
            var listas = (System.Collections.IList)Inv("Listas", tr, gid)!;
            foreach (var l in listas) L($"   lista: {l!.GetType().GetProperty("Nome")!.GetValue(l)} - {l.GetType().GetProperty("Materiais")!.GetValue(l)}");
            object Ent(params (string, object?)[] ps) { var e = Activator.CreateInstance(tE)!; foreach (var (k, v) in ps) tE.GetProperty(k)!.SetValue(e, v); return e; }
            void Resumo(string titulo, Cubacao c)
            {
                L($"== {titulo}: {c.Linhas.Count} seções, {c.Estaca(c.Linhas[0].Estaca)} a {c.Estaca(c.Linhas[^1].Estaca)}; {c.Origem}");
                L($"   fatores corte {c.FatorCorte} aterro {c.FatorAterro}; corte {c.TotalCorte:F2} (×f {c.TotalCorteFator:F2}); aterro {c.TotalAterro:F2} (×f {c.TotalAterroFator:F2}); comp. lateral {c.TotalCompensacao:F2}; saldo {c.Saldo:F2} {c.Situacao}");
                foreach (var l in c.Linhas.Take(4)) L($"   {c.Estaca(l.Estaca)} Ac {l.AreaCorte:F3} Aa {l.AreaAterro:F3} Vc {l.VolumeCorte:F2} Va {l.VolumeAterro:F2} Vcf {l.CorteFator:F2} Vaf {l.AterroFator:F2} B {l.Bruckner:F2} {l.Obs}");
                foreach (var a in c.Avisos) L("   AVISO " + a);
            }

            // A) lista de materiais do Civil
            var guid = (Guid)listas[0]!.GetType().GetProperty("Guid")!.GetValue(listas[0])!;
            var cA = (Cubacao)Inv("Calcular", tr, Ent(("Grupo", gid), ("Lista", (Guid?)guid)))!;
            Resumo("A) lista de materiais", cA);
            cA.Salvar(@"C:\Temp\nsteste\secoes\cubacao_civil.xlsx");

            // B) superfícies amostradas: terreno = Primitivo, projeto = superfície do corredor
            ObjectId Fid(Func<string, bool> p) { foreach (var f in fontes) { var n = (string)f!.GetType().GetProperty("Nome")!.GetValue(f)!; if (p(n)) return (ObjectId)f.GetType().GetProperty("Id")!.GetValue(f)!; } return ObjectId.Null; }
            var tId = Fid(n => n.Contains("Primitivo"));
            var pId = Fid(n => n.Contains(" T5 Eixo"));
            L($"   ids: terreno {tId.Handle} projeto {pId.Handle}");
            {
                var gg = (SampleLineGroup)tr.GetObject(gid, OpenMode.ForRead);
                foreach (QTOMaterialList ml in gg.MaterialLists)
                    foreach (QTOMaterial m in ml)
                        foreach (QTOMaterialItem it in m)
                        {
                            string cond; try { cond = it.Condition.ToString(); } catch { cond = "?"; }
                            L($"   material {m.Name}: item {it.Name} tipo {it.Type} condição {cond}");
                        }
                foreach (ObjectId slid in new ObjectIdCollection())
                {
                    var sl = (SampleLine)tr.GetObject(slid, OpenMode.ForRead);
                    L($"   SL {sl.Name} estaca {sl.Station}");
                    foreach (ObjectId sid in sl.GetSectionIds())
                    {
                        var sec = (Autodesk.Civil.DatabaseServices.Section)tr.GetObject(sid, OpenMode.ForRead);
                        string off; try { off = $"{sec.LeftOffset:F2}/{sec.RightOffset:F2}"; } catch { off = "-"; }
                        var pts = sec.SectionPoints;
                        L($"      {sec.SourceType} '{sec.SourceName}' {pts.Count} pts, offsets {off}");
                        var sb = new StringBuilder();
                        for (int q = 0; q < pts.Count; q++) sb.Append($"[{q}:{pts[q].Location.X:F2},{pts[q].Location.Y:F2}>{pts[q].SegmentTo}] ");
                        L("         " + sb);
                    }
                }
            }
            var cB = (Cubacao)Inv("Calcular", tr, Ent(("Grupo", gid), ("Terreno", tId), ("Projeto", pId), ("FatorCorte", 1.0), ("FatorAterro", 1.25)))!;
            Resumo("B) superfícies", cB);
            var (d, e) = cB.MaiorDiferenca();
            L($"   conferência com {cB.ConferenciaNome}: {cB.Conferencia.Count} estacas, maior diferença de área {d:F5} m² na {(double.IsNaN(e) ? "-" : cB.Estaca(e))}");
            var difs = cB.Conferencia.Select(x => { var l = cB.Linhas.First(y => Math.Abs(y.Estaca - x.Estaca) < 0.01); return (x.Estaca, dc: l.AreaCorte - x.Corte, da: l.AreaAterro - x.Aterro); })
                                     .OrderByDescending(x => Math.Max(Math.Abs(x.dc), Math.Abs(x.da))).Take(5);
            foreach (var x in difs) L($"      {cB.Estaca(x.Estaca)}: dif corte {x.dc:F5} aterro {x.da:F5}");
            L($"   volumes: B corte {cB.TotalCorte:F2} x A {cA.TotalCorte:F2} ({100 * (cB.TotalCorte - cA.TotalCorte) / Math.Max(1, cA.TotalCorte):F3} %); B aterro {cB.TotalAterro:F2} x A {cA.TotalAterro:F2} ({100 * (cB.TotalAterro - cA.TotalAterro) / Math.Max(1, cA.TotalAterro):F3} %)");
            cB.Salvar(@"C:\Temp\nsteste\secoes\cubacao_superficies.xlsx");

            // C) trecho 200 a 600 m pela lista
            var cC = (Cubacao)Inv("Calcular", tr, Ent(("Grupo", gid), ("Lista", (Guid?)guid), ("EstacaInicial", 200.0), ("EstacaFinal", 600.0)))!;
            Resumo("C) trecho 200-600", cC);
            var somaA = cA.Linhas.Where(l => l.Estaca > 200 + 1e-6 && l.Estaca <= 600 + 1e-6).Sum(l => l.AterroFator);
            L($"   aterro×f do trecho {cC.TotalAterroFator:F2} x soma das linhas de A entre 200 e 600 {somaA:F2}");
            tr.Abort();
        }
        catch (System.Exception ex) { L("ERRO " + (ex is TargetInvocationException ti && ti.InnerException is not null ? ti.InnerException : ex)); }
        doc.Editor.WriteMessage("\nSECTESTE FIM");
    }
}
