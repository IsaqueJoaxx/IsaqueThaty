using System.Reflection;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Superficies;
using Projeta.Core.Terraplenagem;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(ProjetaTeste.Cmd5))]

namespace ProjetaTeste;

/// <summary>
/// Teste sem tela (fora do repositório) do PRJPLATO: numa cópia do DWG do galpão, apaga o platô da demonstração, deixa só
/// o terreno e o polígono do piso, e roda o PlatoCivil.Executar com o retângulo (cota compensada) e um polígono em L (cota fixa).
/// </summary>
public sealed class Cmd5
{
    [CommandMethod("PRJPLATOTESTE")]
    public static void Teste()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var log = new StringBuilder();
        void L(string s) { log.AppendLine(s); }
        try
        {
            var redes = typeof(Projeta.Civil3D.App).Assembly;
            var tCivil = redes.GetType("Projeta.Civil3D.Civil.PlatoCivil", true)!;
            var tEntrada = redes.GetType("Projeta.Civil3D.Civil.PlatoCivil+Entrada", true)!;
            var tPol = redes.GetType("Projeta.Civil3D.Civil.PlatoCivil+Poligono", true)!;
            var tSaida = redes.GetType("Projeta.Civil3D.Civil.PlatoCivil+Saida", true)!;

            ObjectId tnId = ObjectId.Null;
            List<(double X, double Y)>? ret = null;
            string hRet = "";
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                int apagados = 0;
                foreach (ObjectId id in ms)
                {
                    var ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if (ent is CivSurface sf && sf.Name.StartsWith("TN", StringComparison.OrdinalIgnoreCase)) { tnId = id; continue; }
                    if (ent is Polyline pl && pl.Layer == "PLATO-PISO" && pl.Closed && ret is null)
                    {
                        ret = new List<(double, double)>();
                        for (int i = 0; i < pl.NumberOfVertices; i++) { var p = pl.GetPoint2dAt(i); ret.Add((p.X, p.Y)); }
                        hRet = id.Handle.ToString();
                        continue;
                    }
                    if (ent.Layer.StartsWith("PLATO-", StringComparison.OrdinalIgnoreCase) || ent.Layer.StartsWith("PRJ-VOLUMES", StringComparison.OrdinalIgnoreCase))
                    { ent.UpgradeOpen(); ent.Erase(); apagados++; }
                }
                tr.Commit();
                L($"limpeza: {apagados} objeto(s) da demonstração apagados; TN {(tnId.IsNull ? "NÃO ACHADO" : "ok")}; retângulo {(ret is null ? "NÃO ACHADO" : ret.Count + " vértices, handle " + hRet)}");
            }
            if (tnId.IsNull || ret is null) throw new InvalidOperationException("faltou o terreno ou o polígono");

            // polígono em L (canto reentrante) a noroeste, cota fixa = terreno no centróide arredondado
            double ox = 5030, oy = 8245;
            var ele = new List<(double X, double Y)> { (0, 0), (40, 0), (40, 20), (20, 20), (20, 40), (0, 40) }.Select(p => (X: ox + p.Item1, Y: oy + p.Item2)).ToList();
            double zL;
            string hL;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var tn = (CivSurface)tr.GetObject(tnId, OpenMode.ForRead);
                var pc = new Poligono2D(ele);
                zL = Math.Round(tn.FindElevationAtXY(pc.Cx, pc.Cy), 1);
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                var pl = new Polyline { Closed = true, Layer = "0" };
                foreach (var (x, y) in ele) pl.AddVertexAt(pl.NumberOfVertices, new Autodesk.AutoCAD.Geometry.Point2d(x, y), 0, 0, 0);
                ms.AppendEntity(pl); tr.AddNewlyCreatedDBObject(pl, true);
                hL = pl.Handle.ToString();
                tr.Commit();
            }
            L($"L: centróide fora do retângulo, cota fixa {zL:F1}");

            var polis = Array.CreateInstance(tPol, 0).GetType(); // só para forçar o tipo
            var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tPol))!;
            lista.Add(Activator.CreateInstance(tPol, "Platô 1", ret, hRet, double.NaN));
            lista.Add(Activator.CreateInstance(tPol, "Platô 2", ele, hL, zL));
            var e = Activator.CreateInstance(tEntrada)!;
            void P(string n, object? v) => tEntrada.GetProperty(n)!.SetValue(e, v);
            P("Terreno", tnId); P("Poligonos", lista); P("Parametros", new ParametrosPlato()); P("Desenho", doc.Name);

            var t0 = DateTime.Now;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var saida = tCivil.GetMethod("Executar")!.Invoke(null, new object[] { tr, db, e })!;
                var rel = (RelatorioPlatos)tSaida.GetProperty("Relatorio")!.GetValue(saida)!;
                var vol = tSaida.GetProperty("Volumes")!.GetValue(saida);
                var relV = vol is null ? null : (RelatorioVolumes)vol.GetType().GetProperty("Relatorio")!.GetValue(vol)!;
                var fin = (string)tSaida.GetProperty("SuperficieFinal")!.GetValue(saida)!;
                var sps = (List<string>)tSaida.GetProperty("SuperficiesPlato")!.GetValue(saida)!;
                L($"tempo {(DateTime.Now - t0).TotalSeconds:F1} s; superfícies: {string.Join(", ", sps)} + {fin}");
                var par = rel.Parametros;
                foreach (var p in rel.Platos)
                {
                    L($"{p.Nome}: cota {p.Cota:F3} ({(p.Compensada ? "compensada" : "fixa")}), piso {p.CotaMin:F3} a {p.CotaMax:F3}, caimento {p.Caimento * 100:F2} % para {p.Azimute:F1}°");
                    L($"   área piso {p.AreaPiso:F1} m2, terraplenagem {p.AreaTerraplenagem:F1} m2, talude corte {p.AreaTaludeCorte:F0} m2, aterro {p.AreaTaludeAterro:F0} m2");
                    L($"   alturas: corte {p.AlturaCorte:F2} m, aterro {p.AlturaAterro:F2} m; crista {p.ExtensaoCrista:F1} m, pé {p.ExtensaoPe:F1} m");
                    L($"   volumes EXATOS corte {p.Corte:F2} aterro {p.Aterro:F2} | malha corte {p.CorteMalha:F2} aterro {p.AterroMalha:F2} " +
                      $"(dif {100 * (p.Corte - p.CorteMalha) / Math.Max(1, p.Corte):F2} % / {100 * (p.Aterro - p.AterroMalha) / Math.Max(1, p.Aterro):F2} %)");
                    L($"   saldo {rel.Saldo(p):F2} m3 (aterro x {par.Fh} = {RelatorioPlatos.A(p) * par.Fh:F2}) -> {rel.Situacao(p)}");
                }
                foreach (var a in rel.Avisos) L("AVISO " + a);
                if (relV is not null)
                    foreach (var a in relV.Areas) L($"VolumesCivil {a.Nome}: área2D {a.Area2D:F1}, corte {a.Corte:F2}, aterro {a.Aterro:F2}");

                // conferências na superfície final
                ObjectId finId = ObjectId.Null;
                foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
                    if (((CivSurface)tr.GetObject(id, OpenMode.ForRead)).Name == fin) finId = id;
                var sf = (CivSurface)tr.GetObject(finId, OpenMode.ForRead);
                var tn = (CivSurface)tr.GetObject(tnId, OpenMode.ForRead);
                double Z(CivSurface s, double x, double y) { try { return s.FindElevationAtXY(x, y); } catch { return double.NaN; } }
                var pr = new Poligono2D(ret);
                L($"final no centróide do retângulo {Z(sf, pr.Cx, pr.Cy):F3} (piso {rel.Platos[0].Cota:F3}); num vértice {Z(sf, ret[0].X, ret[0].Y):F3}");
                L($"final 60 m fora do retângulo (x+) {Z(sf, pr.Cx + 120, pr.Cy):F3} x TN {Z(tn, pr.Cx + 120, pr.Cy):F3}");
                var pe = new Poligono2D(ele);
                L($"final no L ({ele[0].X + 5:F0},{ele[0].Y + 5:F0}) {Z(sf, ele[0].X + 5, ele[0].Y + 5):F3} (fixa {zL:F1}); no canto reentrante ({ele[3].X - 1:F0},{ele[3].Y - 1:F0}) {Z(sf, ele[3].X - 1, ele[3].Y - 1):F3}");
                // talude: 3 m para fora do meio do lado do retângulo, a cota tem de estar entre piso e terreno, inclinação 1/1,5 ou 1/2
                var (x0, y0) = ret[0]; var (x1, y1) = ret[1];
                double mx = (x0 + x1) / 2, my = (y0 + y1) / 2, ex = x1 - x0, ey = y1 - y0, el = Math.Sqrt(ex * ex + ey * ey);
                var pp = new Poligono2D(ret);
                double nx = ey / el, ny = -ex / el;
                if (pp.Dentro(mx + nx, my + ny)) { nx = -nx; ny = -ny; }
                double zb = Z(sf, mx - nx * 0.05, my - ny * 0.05), z3 = Z(sf, mx + nx * 3, my + ny * 3);
                L($"lado 1: piso {zb:F3}, 3 m fora {z3:F3}, TN ali {Z(tn, mx + nx * 3, my + ny * 3):F3} -> inclinação 1V:{3 / Math.Abs(z3 - zb):F2}H");

                rel.Salvar(@"C:\Temp\nsteste\plato5\platos_teste.xlsx", relV);
                L("planilha C:\\Temp\\nsteste\\plato5\\platos_teste.xlsx");
                tr.Commit();
            }
        }
        catch (System.Exception ex) { L("ERRO " + (ex is TargetInvocationException ti && ti.InnerException is not null ? ti.InnerException : ex)); }
        File.WriteAllText(@"C:\Temp\nsteste\plato5\resultado.txt", log.ToString());
        doc.Editor.WriteMessage("\nPLATOTESTE FIM");
    }
}
