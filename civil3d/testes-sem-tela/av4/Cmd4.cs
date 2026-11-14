using System.Diagnostics;
using System.Reflection;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Superficies;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(ProjetaTeste.Cmd4))]

namespace ProjetaTeste;

/// <summary>
/// Teste sem tela (fora do repositório) do Projeto × obra: monta uma superfície de "obra" a partir da de projeto com
/// desvios conhecidos por faixa de X (+0,15 / −0,08 / ruído de ±2 cm) e confere se o PRJCONFORMIDADE os encontra.
/// </summary>
public sealed class Cmd4
{
    /// <summary>Conferência exata: obra = cópia do projeto (colada) subida/descida de um valor constante.</summary>
    [CommandMethod("PRJCONFTESTE2")]
    public static void Teste2()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var log = new StringBuilder();
        void L(string s) { log.AppendLine(s); }
        try
        {
            var redes = typeof(Projeta.Civil3D.App).Assembly;
            var tCivil = redes.GetType("Projeta.Civil3D.Civil.ConformidadeCivil", true)!;
            var tEntrada = redes.GetType("Projeta.Civil3D.Civil.ConformidadeCivil+Entrada", true)!;
            var tSaida = redes.GetType("Projeta.Civil3D.Civil.ConformidadeCivil+Saida", true)!;
            ObjectId projId = ObjectId.Null;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
                    if (((CivSurface)tr.GetObject(id, OpenMode.ForRead)).Name.Contains("Eixo", StringComparison.OrdinalIgnoreCase)) projId = id;
                tr.Commit();
            }
            foreach (var delta in new[] { 0.15, -0.03, -0.30 })
            {
                ObjectId obraId;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    obraId = TinSurface.Create(db, $"OBRA {delta:+0.00;-0.00}");
                    var obra = (TinSurface)tr.GetObject(obraId, OpenMode.ForWrite);
                    obra.PasteSurface(projId);
                    obra.Rebuild();
                    obra.RaiseSurface(delta);
                    tr.Commit();
                }
                var e = Activator.CreateInstance(tEntrada)!;
                void P(string n, object? v) => tEntrada.GetProperty(n)!.SetValue(e, v);
                P("Projeto", projId); P("Obra", obraId); P("Tolerancia", new ToleranciaObra()); P("Desenho", doc.Name);
                var saida = tCivil.GetMethod("Calcular")!.Invoke(null, new[] { db, e })!;
                var rel = (RelatorioConformidade)tSaida.GetProperty("Relatorio")!.GetValue(saida)!;
                var a = rel.Geral;
                L($"DELTA {delta:+0.000;-0.000}: area {a.Area:F1} m2; dentro {a.PctDentro:F3} %, acima {a.PctAcima:F3} %, abaixo {a.PctAbaixo:F3} %; media {a.Media:F5} dp {a.DesvioPadrao:F5} emq {a.Emq:F5}; " +
                  $"max {a.Maximo:F4} min {a.Minimo:F4}; vol acima {a.VolumeAcima:F2} abaixo {a.VolumeAbaixo:F2} (esperado {Math.Abs(delta) * a.Area:F2} pela area da malha) -> {a.Situacao(95)}; criticos {rel.Criticos.Count}");
                foreach (var f in rel.Faixas.Where(f => f.Area > 0)) L($"   faixa {f.Rotulo}: {f.Area:F1} m2");
                if (rel.Criticos.Count > 0) L($"   P1 desvio {rel.Criticos[0].Desvio:F4} proj {rel.Criticos[0].CotaProjeto:F3} obra {rel.Criticos[0].CotaObra:F3}");
                foreach (var w in rel.Avisos) L("   AVISO " + w);
            }
        }
        catch (System.Exception ex) { L("ERRO " + (ex is TargetInvocationException ti && ti.InnerException is not null ? ti.InnerException : ex)); }
        File.WriteAllText(@"C:\Temp\nsteste\conftest2.txt", log.ToString());
        doc.Editor.WriteMessage("\nCONFTESTE2 FIM");
    }

    [CommandMethod("PRJCONFTESTE")]
    public static void Teste()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var log = new StringBuilder();
        void L(string s) { log.AppendLine(s); }
        try
        {
            var redes = typeof(Projeta.Civil3D.App).Assembly;
            var tCivil = redes.GetType("Projeta.Civil3D.Civil.ConformidadeCivil", true)!;
            var tEntrada = redes.GetType("Projeta.Civil3D.Civil.ConformidadeCivil+Entrada", true)!;
            var tSaida = redes.GetType("Projeta.Civil3D.Civil.ConformidadeCivil+Saida", true)!;

            ObjectId projId = ObjectId.Null, obraId = ObjectId.Null, alId = ObjectId.Null;
            double xa = 0, xb = 0;
            int nA = 0, nB = 0, nC = 0;
            var sw = Stopwatch.StartNew();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var civ = CivilApplication.ActiveDocument;
                var sups = new List<(ObjectId Id, string Nome, bool Vol)>();
                foreach (ObjectId id in civ.GetSurfaceIds())
                {
                    var s = (CivSurface)tr.GetObject(id, OpenMode.ForRead);
                    sups.Add((id, s.Name, s.IsVolumeSurface));
                }
                L("superficies: " + string.Join(", ", sups.Select(s => s.Nome + (s.Vol ? " (vol)" : ""))));
                var proj = sups.FirstOrDefault(s => !s.Vol && s.Nome.Contains("Eixo", StringComparison.OrdinalIgnoreCase));
                if (proj.Id.IsNull) proj = sups.First(s => !s.Vol && !s.Nome.Contains("rimitiv", StringComparison.OrdinalIgnoreCase));
                projId = proj.Id;
                var ps = (CivSurface)tr.GetObject(projId, OpenMode.ForRead);
                var g = ps.GetGeneralProperties();
                double x0 = g.MinimumCoordinateX, x1 = g.MaximumCoordinateX, y0 = g.MinimumCoordinateY, y1 = g.MaximumCoordinateY;
                double w = x1 - x0;
                xa = x0 + 0.25 * w; xb = x0 + 0.35 * w;
                L($"projeto={proj.Nome} bbox {w:F1} x {y1 - y0:F1} m; faixa A: x<{xa:F2} (+0,15); faixa B: {xa:F2}<=x<{xb:F2} (-0,08); resto ruido +-0,02");

                var pts = new Point3dCollection();
                const double passo = 2.0;
                for (double x = x0; x <= x1; x += passo)
                    for (double y = y0; y <= y1; y += passo)
                    {
                        double z;
                        try { z = ps.FindElevationAtXY(x, y); } catch { continue; }
                        double d;
                        if (x < xa) { d = 0.15; nA++; }
                        else if (x < xb) { d = -0.08; nB++; }
                        else { d = 0.02 * Math.Sin(x * 0.7) * Math.Cos(y * 0.9); nC++; }
                        pts.Add(new Point3d(x, y, z + d));
                    }
                L($"pontos da obra: {pts.Count} (A={nA}, B={nB}, C={nC}) em {sw.ElapsedMilliseconds} ms");
                obraId = TinSurface.Create(db, "OBRA TESTE");
                var obra = (TinSurface)tr.GetObject(obraId, OpenMode.ForWrite);
                try { obra.BuildOptions.UseMaximumTriangleLength = true; obra.BuildOptions.MaximumTriangleLength = 3 * passo; }
                catch (System.Exception ex) { L("build options: " + ex.Message); }
                obra.AddVertices(pts);
                obra.Rebuild();

                foreach (ObjectId id in civ.GetAlignmentIds())
                {
                    var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                    L($"alinhamento: {a.Name} {a.StartingStation:F1}..{a.EndingStation:F1}");
                    if (alId.IsNull || a.Name.Contains("T5", StringComparison.OrdinalIgnoreCase)) alId = id;
                }
                tr.Commit();
            }

            double nT = nA + nB + nC;
            L($"ESPERADO (pelos pontos): acima {nA / nT * 100:F1} %, abaixo {nB / nT * 100:F1} %, dentro {nC / nT * 100:F1} %");

            var e = Activator.CreateInstance(tEntrada)!;
            void P(string n, object? v) => tEntrada.GetProperty(n)!.SetValue(e, v);
            P("Projeto", projId); P("Obra", obraId); P("Tolerancia", new ToleranciaObra()); P("Alinhamento", alId); P("Desenho", doc.Name);
            sw.Restart();
            var saida = tCivil.GetMethod("Calcular")!.Invoke(null, new[] { db, e })!;
            var rel = (RelatorioConformidade)tSaida.GetProperty("Relatorio")!.GetValue(saida)!;
            var malha = (ResultadoConformidade)tSaida.GetProperty("Malha")!.GetValue(saida)!;
            L($"CALCULO ({sw.ElapsedMilliseconds} ms): celula {rel.Celula} m, malha {malha.Linhas}x{malha.Colunas}, volumes exatos={rel.VolumesExatos}");
            foreach (var a in rel.Linhas)
                L($"  {a.Nome}: area {a.Area:F1} m2; dentro {a.PctDentro:F2} %, acima {a.PctAcima:F2} %, abaixo {a.PctAbaixo:F2} %; media {a.Media:F4} dp {a.DesvioPadrao:F4} emq {a.Emq:F4}; max {a.Maximo:F3} min {a.Minimo:F3}; vol acima {a.VolumeAcima:F2} abaixo {a.VolumeAbaixo:F2} -> {a.Situacao(rel.Tolerancia.PercentualMinimo)}");
            foreach (var f in rel.Faixas) L($"  faixa {f.Rotulo}: {f.Area:F1} m2, {f.Volume:F2} m3");
            foreach (var q in rel.Criticos.Take(8))
                L($"  P{q.Numero}: ({q.X:F2}; {q.Y:F2}) estaca {q.Estaca:F2} afast {q.Afastamento:F2} proj {q.CotaProjeto:F3} obra {q.CotaObra:F3} desvio {q.Desvio:F3} excesso {q.Excesso:F3} faixa {q.Faixa}");
            L($"  criticos: {rel.Criticos.Count}; na faixa A (x<xa): {rel.Criticos.Count(q => q.X < xa)}; na B: {rel.Criticos.Count(q => q.X >= xa && q.X < xb)}; fora de A/B: {rel.Criticos.Count(q => q.X >= xb)}");
            if (rel.Estacas is { } ce)
            {
                L($"  estacas: {ce.Alinhamento}, {ce.Linhas.Count} linhas, afastamentos [{string.Join("; ", ce.Afastamentos)}]");
                foreach (var lc in ce.Linhas.Take(3).Concat(ce.Linhas.Skip(ce.Linhas.Count / 2).Take(2)))
                {
                    var rs = ce.Resumo(lc, rel.Tolerancia);
                    L($"    {lc.Estaca:F2}: n={rs.Pontos} fora={rs.Fora} media={rs.Media:F3} max={rs.Maximo:F3}@{rs.AfMaximo} min={rs.Minimo:F3}@{rs.AfMinimo} {rs.Situacao}");
                }
            }
            foreach (var o in rel.Origem) L($"  origem {o.Item}: {o.Valor}");
            foreach (var a in rel.Avisos) L("  AVISO " + a);
            Directory.CreateDirectory(@"C:\Temp\nsteste\relatorios");
            rel.Salvar(@"C:\Temp\nsteste\relatorios\conformidade_teste.xlsx");

            using (var tr = db.TransactionManager.StartTransaction())
            {
                int n = (int)tCivil.GetMethod("DesenharMapa")!.Invoke(null, new object[] { tr, db, malha, rel })!;
                var ents = (System.Collections.IList)tCivil.GetMethod("EntidadesDoMapa")!.Invoke(null, new object[] { tr, db })!;
                L($"MAPA: {n} solidos; entidades na camada: {ents.Count}");
                tr.Commit();
            }
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var nomes = new List<string>();
                foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds()) nomes.Add(((CivSurface)tr.GetObject(id, OpenMode.ForRead)).Name);
                L("superficies depois: " + string.Join(", ", nomes));
                tr.Commit();
            }
        }
        catch (System.Exception ex) { L("ERRO " + (ex is TargetInvocationException ti && ti.InnerException is not null ? ti.InnerException : ex)); }
        File.WriteAllText(@"C:\Temp\nsteste\conftest.txt", log.ToString());
        doc.Editor.WriteMessage("\nCONFTESTE FIM");
    }
}
