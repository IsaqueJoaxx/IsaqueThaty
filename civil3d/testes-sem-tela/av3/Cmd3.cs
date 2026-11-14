using System.Diagnostics;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Projeta.Civil3D.Civil;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using TinVol = Autodesk.Civil.DatabaseServices.TinVolumeSurface;

[assembly: CommandClass(typeof(ProjetaTeste.Cmd3))]

namespace ProjetaTeste;

/// <summary>Teste sem tela (fora do repositório) do cálculo de volumes do PROJETA.</summary>
public sealed class Cmd3
{
    [CommandMethod("PRJVOLTESTE")]
    public static void Teste()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var log = new StringBuilder();
        void L(string s) { log.AppendLine(s); }
        try
        {
            ObjectId b = ObjectId.Null, c = ObjectId.Null;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
                {
                    var s = (CivSurface)tr.GetObject(id, OpenMode.ForRead);
                    L($"superficie: {s.Name} volume={s.IsVolumeSurface}");
                    if (s is TinVol v) { var p = v.GetVolumeProperties(); L($"   corte={p.UnadjustedCutVolume:F2} aterro={p.UnadjustedFillVolume:F2}"); }
                }
                var sups = VolumesCivil.Superficies(tr);
                b = sups.First(x => x.Nome.Contains("rimitiv", StringComparison.OrdinalIgnoreCase)).Id;
                c = sups.First(x => x.Id != b).Id;
                L("base=" + sups.First(x => x.Id == b).Nome + " comparacao=" + sups.First(x => x.Id == c).Nome);
                tr.Commit();
            }
            var sw = Stopwatch.StartNew();
            var s1 = VolumesCivil.Calcular(db, new VolumesCivil.Entrada { Base = b, Comparacao = c, Desenho = doc.Name, FatorCorte = 1.25, FatorAterro = 1.3 });
            L($"TUDO ({sw.ElapsedMilliseconds} ms): " + string.Join(" | ", s1.Relatorio.Areas.Select(a => $"{a.Nome} area={a.Area2D:F1} corte={a.Corte:F2} aterro={a.Aterro:F2}")));
            L($"   malha cel={s1.Relatorio.Celula} {s1.Malha.Linhas}x{s1.Malha.Colunas} corte={s1.Relatorio.CorteMalha:F1} aterro={s1.Relatorio.AterroMalha:F1} dif={s1.Relatorio.DiferencaMalha():F2}%");
            foreach (var f in s1.Relatorio.Faixas) L($"   {f.Rotulo}: area={f.Area:F0} vol={f.Volume:F0}");
            foreach (var a in s1.Relatorio.Avisos) L("   AVISO " + a);
            Directory.CreateDirectory(@"C:\Temp\nsteste\relatorios");
            s1.Relatorio.Salvar(@"C:\Temp\nsteste\relatorios\volumes_tudo.xlsx");

            var m = s1.Malha;
            double cel = m.Celula, cx = 0, cy = 0;
            for (int r = m.Linhas / 2, k = 0; k < m.Linhas && cx == 0; k++, r = (r + 1) % m.Linhas)
                for (int col = 0; col < m.Colunas; col++)
                    if (m.Faixa[r, col] >= 0) { cx = m.X0 + (col + 0.5) * cel; cy = m.Y0 + (r + 0.5) * cel; break; }
            double ex0 = m.X0, ey0 = m.Y0, ex1 = m.X0 + m.Colunas * cel, ey1 = m.Y0 + m.Linhas * cel;
            var tudo = new VolumesCivil.Poligono("Retângulo envolvente", new List<(double X, double Y)> { (ex0, ey0), (ex1, ey0), (ex1, ey1), (ex0, ey1) }, "t1");
            var pol = new VolumesCivil.Poligono("Quadrado 30 x 30 no corredor", new List<(double X, double Y)> { (cx - 15, cy - 15), (cx + 15, cy - 15), (cx + 15, cy + 15), (cx - 15, cy + 15) }, "t2");
            sw.Restart();
            var s2 = VolumesCivil.Calcular(db, new VolumesCivil.Entrada { Base = b, Comparacao = c, Poligonos = new() { tudo, pol }, Desenho = doc.Name });
            L($"POLIGONO ({sw.ElapsedMilliseconds} ms): " + string.Join(" | ", s2.Relatorio.Areas.Select(a => $"{a.Nome} area={a.Area2D:F1} corte={a.Corte:F2} aterro={a.Aterro:F2}")));
            L($"   malha cel={s2.Relatorio.Celula} corte={s2.Relatorio.CorteMalha:F1} aterro={s2.Relatorio.AterroMalha:F1} dif={s2.Relatorio.DiferencaMalha():F2}%");
            foreach (var a in s2.Relatorio.Avisos) L("   AVISO " + a);

            using (var tr = db.TransactionManager.StartTransaction())
            {
                int n = VolumesCivil.DesenharMapa(tr, db, s1.Malha, s1.Relatorio);
                L($"MAPA: {n} solidos; entidades na camada: {VolumesCivil.EntidadesDoMapa(tr, db).Count}");
                tr.Commit();
            }
            using (var tr = db.TransactionManager.StartTransaction())
            {
                L("superficies depois: " + string.Join(", ", VolumesCivil.Superficies(tr).Select(x => x.Nome)) + " | total ids=" + CivilApplication.ActiveDocument.GetSurfaceIds().Count);
                tr.Commit();
            }
        }
        catch (System.Exception ex) { L("ERRO " + ex); }
        File.WriteAllText(@"C:\Temp\nsteste\voltest.txt", log.ToString());
        doc.Editor.WriteMessage("\nVOLTESTE FIM");
    }
}
