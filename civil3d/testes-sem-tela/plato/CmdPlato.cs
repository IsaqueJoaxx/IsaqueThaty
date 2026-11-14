using System.Reflection;
using System.Text;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Superficies;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using SurfaceBoundaryType = Autodesk.Civil.SurfaceBoundaryType;
using StyleBase = Autodesk.Civil.DatabaseServices.Styles.StyleBase;

[assembly: CommandClass(typeof(PlatoDemo.CmdPlato))]

namespace PlatoDemo;

/// <summary>
/// Monta o estudo de platô (galpão logístico 120 x 80 m) num terreno fictício: superfície do terreno natural, superfície
/// do platô com taludes até o terreno, superfície final, linhas de crista e pé, tracinhos de talude, cotas, textos e o
/// mapa/relatório de volumes do PROJETA. Mesmas fórmulas do modelo em Python (modelo.py).
/// </summary>
public sealed class CmdPlato
{
    const double E0 = 5000, N0 = 8000, XMAX = 400, YMAX = 320;
    const double XC = 200, YC = 160, A = 60, B = 40, CAIMENTO = 0.005, TAL_CORTE = 1.5, TAL_ATERRO = 2.0, FH = 1.25;
    const double ZP = 850.21;
    static readonly double Teta = -15.0 * Math.PI / 180, Cu = Math.Cos(Teta), Su = Math.Sin(Teta);

    static double Terreno(double x, double y) =>
        850.0 + 0.02 * (x - 200) + 0.075 * (y - 160)
        + 5.0 * Math.Exp(-((x - 320) * (x - 320) + (y - 260) * (y - 260)) / (2 * 70.0 * 70.0))
        - 2.5 * Math.Exp(-((x - 80) * (x - 80) + (y - 70) * (y - 70)) / (2 * 55.0 * 55.0))
        + 0.6 * Math.Sin(x / 41 + 0.7) * Math.Cos(y / 33 - 0.4);

    static (double X, double Y) Mundo(double u, double v) => (XC + u * Cu - v * Su, YC + u * Su + v * Cu);
    static double Piso(double v) => ZP + CAIMENTO * v;
    static Point3d P(double x, double y, double z) => new(E0 + x, N0 + y, z);

    record Borda(double U, double V, double Nu, double Nv, bool Canto);
    record Talude(double X, double Y, double Z, double Xd, double Yd, double Zd, bool Corte, double Dist, bool Canto);

    static List<Borda> Bordas()
    {
        var r = new List<Borda>();
        (double, double)[] cantos = { (-A, -B), (A, -B), (A, B), (-A, B) };
        (double, double)[] normais = { (0, -1), (1, 0), (0, 1), (-1, 0) };
        for (int i = 0; i < 4; i++)
        {
            var (u0, v0) = cantos[i]; var (u1, v1) = cantos[(i + 1) % 4];
            var n = normais[i];
            int k = Math.Max(1, (int)Math.Round(Math.Sqrt((u1 - u0) * (u1 - u0) + (v1 - v0) * (v1 - v0)) / 2.0));
            for (int j = 0; j < k; j++)
            {
                double t = (double)j / k;
                if (j == 0)
                {
                    var na = normais[(i + 3) % 4];
                    double a0 = Math.Atan2(na.Item2, na.Item1), a1 = Math.Atan2(n.Item2, n.Item1);
                    if (a1 < a0) a1 += 2 * Math.PI;
                    int m = Math.Max(1, (int)Math.Round((a1 - a0) * 180 / Math.PI / 10.0));
                    for (int q = 0; q < m; q++)
                    {
                        double ang = a0 + (a1 - a0) * q / m;
                        r.Add(new Borda(u0, v0, Math.Cos(ang), Math.Sin(ang), true));
                    }
                }
                r.Add(new Borda(u0 + (u1 - u0) * t, v0 + (v1 - v0) * t, n.Item1, n.Item2, j == 0));
            }
        }
        return r;
    }

    static List<Talude> Taludes()
    {
        var r = new List<Talude>();
        foreach (var b in Bordas())
        {
            double zb = Piso(b.V);
            var (x, y) = Mundo(b.U, b.V);
            bool corte = Terreno(x, y) > zb;
            double k = corte ? 1 / TAL_CORTE : -1 / TAL_ATERRO;
            double G(double s) { var (xx, yy) = Mundo(b.U + b.Nu * s, b.V + b.Nv * s); return Terreno(xx, yy) - (zb + k * s); }
            double s0 = 0, s1 = 0.25, g0 = G(0), s;
            if (Math.Abs(g0) <= 1e-9) s = 0;
            else
            {
                while (s1 < 200 && (G(s1) > 0) == (g0 > 0)) { s0 = s1; s1 += 0.25; }
                for (int it = 0; it < 50; it++) { double sm = (s0 + s1) / 2; if ((G(sm) > 0) == (g0 > 0)) s0 = sm; else s1 = sm; }
                s = (s0 + s1) / 2;
            }
            var (xd, yd) = Mundo(b.U + b.Nu * s, b.V + b.Nv * s);
            r.Add(new Talude(x, y, zb, xd, yd, zb + k * s, corte, s, b.Canto));
        }
        return r;
    }

    [CommandMethod("PRJPLATODEMO")]
    public static void Plato()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var log = new StringBuilder();
        void L(string s) { log.AppendLine(s); }
        string pasta = Path.GetDirectoryName(db.Filename) ?? @"C:\Temp\nsteste\plato";
        try
        {
            var civ = CivilApplication.ActiveDocument;
            var tal = Taludes();
            L($"taludes: {tal.Count} pontos; corte max {tal.Where(t => t.Corte).Max(t => t.Zd - t.Z):F2} m; aterro max {tal.Where(t => !t.Corte).Max(t => t.Z - t.Zd):F2} m");
            ObjectId tnId, projId, finId;
            string handleDay = "";
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                string Layer(string nome, short cor, LineWeight? lw = null)
                {
                    if (!lt.Has(nome))
                    {
                        var l = new LayerTableRecord { Name = nome, Color = Color.FromColorIndex(ColorMethod.ByAci, cor) };
                        if (lw is { } w) l.LineWeight = w;
                        lt.Add(l); tr.AddNewlyCreatedDBObject(l, true);
                    }
                    return nome;
                }
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                ObjectId Add(Entity e) { var id = ms.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); return id; }

                string lSup = Layer("PLATO-SUPERFICIES", 8), lPiso = Layer("PLATO-PISO", 4, LineWeight.LineWeight050),
                       lCrista = Layer("PLATO-CRISTA-CORTE", 1, LineWeight.LineWeight035), lPe = Layer("PLATO-PE-ATERRO", 3, LineWeight.LineWeight035),
                       lTal = Layer("PLATO-TALUDE", 8), lTxt = Layer("PLATO-TEXTO", 7), lCota = Layer("PLATO-COTAS", 2), lBrk = Layer("PLATO-BREAKLINES", 9);

                // ---- superfícies: terreno natural (grade de 2 m), projeto do platô e final
                var pts = new Point3dCollection();
                for (double x = 0; x <= XMAX + 1e-9; x += 2)
                    for (double y = 0; y <= YMAX + 1e-9; y += 2)
                        pts.Add(P(x, y, Terreno(x, y)));
                tnId = TinSurface.Create(db, "TN - Terreno natural");
                var tn = (TinSurface)tr.GetObject(tnId, OpenMode.ForWrite);
                tn.AddVertices(pts);
                tn.Layer = lSup;
                L($"TN: {pts.Count} pontos");

                var crista = new Point3dCollection();
                foreach (var t in tal.Where(t => !t.Canto || t == tal.First(q => q.X == t.X && q.Y == t.Y)))
                    if (crista.Count == 0 || crista[crista.Count - 1].DistanceTo(P(t.X, t.Y, t.Z)) > 1e-6) crista.Add(P(t.X, t.Y, t.Z));
                var dayl = new Point3dCollection();
                foreach (var t in tal) if (dayl.Count == 0 || dayl[dayl.Count - 1].DistanceTo(P(t.Xd, t.Yd, t.Zd)) > 1e-3) dayl.Add(P(t.Xd, t.Yd, t.Zd));
                var plCrista = new Polyline3d(Poly3dType.SimplePoly, crista, true) { Layer = lBrk };
                var plDay = new Polyline3d(Poly3dType.SimplePoly, dayl, true) { Layer = lBrk };
                var idCrista = Add(plCrista); var idDay = Add(plDay);
                handleDay = idDay.Handle.ToString();

                projId = TinSurface.Create(db, "PLATO - Projeto");
                var pj = (TinSurface)tr.GetObject(projId, OpenMode.ForWrite);
                pj.Layer = lSup;
                pj.BreaklinesDefinition.AddStandardBreaklines(new ObjectIdCollection { idCrista, idDay }, 0.1, 0, 0, 0);
                pj.BoundariesDefinition.AddBoundaries(new ObjectIdCollection { idDay }, 0.1, SurfaceBoundaryType.Outer, true);
                pj.Rebuild();
                var gp = pj.GetGeneralProperties();
                L($"PLATO - Projeto: {gp.NumberOfPoints} pontos, cotas {gp.MinimumElevation:F2} a {gp.MaximumElevation:F2}");

                finId = TinSurface.Create(db, "TERRENO FINAL");
                var fin = (TinSurface)tr.GetObject(finId, OpenMode.ForWrite);
                fin.Layer = lSup;
                fin.PasteSurface(tnId);
                fin.PasteSurface(projId);
                fin.Rebuild();
                var gf = fin.GetGeneralProperties();
                L($"TERRENO FINAL: {gf.NumberOfPoints} pontos, cotas {gf.MinimumElevation:F2} a {gf.MaximumElevation:F2}");

                // estilos de superfície do template
                var nomes = new List<(ObjectId Id, string Nome)>();
                foreach (ObjectId id in civ.Styles.SurfaceStyles) nomes.Add((id, ((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name));
                L("estilos de superficie: " + string.Join(" | ", nomes.Select(n => n.Nome)));
                ObjectId Estilo(params string[][] grupos)
                {
                    foreach (var g in grupos)
                    {
                        var it = nomes.FirstOrDefault(n => g.All(p => n.Nome.Contains(p, StringComparison.OrdinalIgnoreCase)));
                        if (!it.Id.IsNull) return it.Id;
                    }
                    return ObjectId.Null;
                }
                var sTn = Estilo(new[] { "CURVAS-1&5 (outros)" }, new[] { "1", "5", "fundo" }, new[] { "1", "5", "background" });
                var sFin = Estilo(new[] { "CURVAS-1&5 (GEOMETRIA)" }, new[] { "1", "5", "projeto" }, new[] { "1", "5", "design" });
                var sBorda = Estilo(new[] { "INVISIVEL" }, new[] { "borda" }, new[] { "border" });
                if (!sTn.IsNull) tn.StyleId = sTn;
                if (!sFin.IsNull) fin.StyleId = sFin;
                if (!sBorda.IsNull) pj.StyleId = sBorda;
                string NomeEstilo(ObjectId id) => id.IsNull ? "(padrão)" : nomes.First(n => n.Id == id).Nome;
                L($"estilo TN={NomeEstilo(sTn)} | FINAL={NomeEstilo(sFin)} | PROJETO={NomeEstilo(sBorda)}");

                // ---- desenho: piso, crista/pé, tracinhos
                var piso = new Polyline { Layer = lPiso, Closed = true };
                foreach (var (u, v) in new[] { (-A, -B), (A, -B), (A, B), (-A, B) }) { var (x, y) = Mundo(u, v); piso.AddVertexAt(piso.NumberOfVertices, new Point2d(E0 + x, N0 + y), 0, 0, 0); }
                Add(piso);
                // crista de corte e pé de aterro: trechos contínuos do encontro com o terreno
                int nseg = 0;
                for (int i = 0; i < tal.Count; i++)
                {
                    if (i > 0 && tal[i - 1].Corte == tal[i].Corte) continue;
                    bool c = tal[i].Corte;
                    var pl = new Polyline { Layer = c ? lCrista : lPe };
                    int j = i;
                    do { pl.AddVertexAt(pl.NumberOfVertices, new Point2d(E0 + tal[j].Xd, N0 + tal[j].Yd), 0, 0, 0); j++; }
                    while (j < tal.Count && tal[j].Corte == c);
                    if (j < tal.Count) pl.AddVertexAt(pl.NumberOfVertices, new Point2d(E0 + tal[j].Xd, N0 + tal[j].Yd), 0, 0, 0);
                    else if (tal[0].Corte == c) pl.AddVertexAt(pl.NumberOfVertices, new Point2d(E0 + tal[0].Xd, N0 + tal[0].Yd), 0, 0, 0);
                    if (pl.NumberOfVertices >= 2) { Add(pl); nseg++; }
                }
                int ntraco = 0;
                for (int i = 0; i < tal.Count; i++)
                {
                    var t = tal[i];
                    if (t.Canto || t.Dist < 0.5) continue;
                    double f = i % 2 == 0 ? 1.0 : 0.5;
                    Add(new Line(new Point3d(E0 + t.X, N0 + t.Y, 0), new Point3d(E0 + t.X + (t.Xd - t.X) * f, N0 + t.Y + (t.Yd - t.Y) * f, 0)) { Layer = lTal });
                    ntraco++;
                }
                L($"desenho: piso, {nseg} trecho(s) de crista/pé, {ntraco} tracinhos de talude");

                // ---- textos e cotas
                MText Txt(string s, double x, double y, double h, double rot = 0, AttachmentPoint ap = AttachmentPoint.MiddleCenter, string? layer = null)
                {
                    var m = new MText { Contents = s, Location = new Point3d(E0 + x, N0 + y, 0), TextHeight = h, Rotation = rot, Attachment = ap, Layer = layer ?? lTxt };
                    Add(m); return m;
                }
                var (cx, cy) = Mundo(0, 6);
                Txt($"PLATÔ - GALPÃO LOGÍSTICO\\P120,00 x 80,00 m (9.600 m²)\\PPiso: {ZP:F2} no centro".Replace('.', ','), cx, cy, 3.0, Teta);
                var (ax0, ay0) = Mundo(-10, -14); var (ax1, ay1) = Mundo(-10, -28);
                Add(new Line(new Point3d(E0 + ax0, N0 + ay0, 0), new Point3d(E0 + ax1, N0 + ay1, 0)) { Layer = lCota });
                var ang = Math.Atan2(ay1 - ay0, ax1 - ax0);
                double hx = Math.Cos(ang), hy = Math.Sin(ang);
                Add(new Solid(new Point3d(E0 + ax1, N0 + ay1, 0), new Point3d(E0 + ax1 - 3 * hx + 1.2 * hy, N0 + ay1 - 3 * hy - 1.2 * hx, 0),
                              new Point3d(E0 + ax1 - 3 * hx - 1.2 * hy, N0 + ay1 - 3 * hy + 1.2 * hx, 0)) { Layer = lCota });
                var (tx, ty) = Mundo(-3, -21);
                Txt("i = 0,5 %", tx, ty, 2.2, Teta + Math.PI / 2, AttachmentPoint.MiddleCenter, lCota);
                foreach (var (u, v) in new[] { (-A, -B), (A, -B), (A, B), (-A, B) })
                {
                    var (x, y) = Mundo(u * 0.93, v * 0.88);
                    Txt(Piso(v).ToString("0.00").Replace('.', ','), x, y, 2.0, Teta, AttachmentPoint.MiddleCenter, lCota);
                }
                // cotas de 120 e 80 m por dentro do platô
                var (d1x, d1y) = Mundo(-A, -B + 7); var (d2x, d2y) = Mundo(A, -B + 7); var (d3x, d3y) = Mundo(0, -B + 3);
                Add(new AlignedDimension(P(d1x, d1y, 0), P(d2x, d2y, 0), P(d3x, d3y, 0), "", db.Dimstyle) { Layer = lCota, Dimscale = 1, Dimtxt = 2.2, Dimasz = 1.5 });
                var (e1x, e1y) = Mundo(A - 8, -B); var (e2x, e2y) = Mundo(A - 8, B); var (e3x, e3y) = Mundo(A - 4, 0);
                Add(new AlignedDimension(P(e1x, e1y, 0), P(e2x, e2y, 0), P(e3x, e3y, 0), "", db.Dimstyle) { Layer = lCota, Dimscale = 1, Dimtxt = 2.2, Dimasz = 1.5 });
                // rótulos dos taludes: no meio do maior trecho de corte e do maior de aterro
                foreach (var corte in new[] { true, false })
                {
                    var t = tal.Where(q => q.Corte == corte && !q.Canto).OrderByDescending(q => q.Dist).FirstOrDefault();
                    if (t is null) continue;
                    double mx = (t.X + t.Xd) / 2, my = (t.Y + t.Yd) / 2;
                    double rot = Math.Atan2(t.Yd - t.Y, t.Xd - t.X) + Math.PI / 2;
                    if (Math.Cos(rot) < 0) rot += Math.PI;
                    Txt(corte ? $"CORTE 1V:1,5H\\Ph máx. {t.Zd - t.Z:F1} m".Replace('.', ',') : $"ATERRO 1V:2H\\Ph máx. {t.Z - t.Zd:F1} m".Replace('.', ','),
                        mx + (t.Xd - t.X) / Math.Max(t.Dist, 1) * 9, my + (t.Yd - t.Y) / Math.Max(t.Dist, 1) * 9, 2.2, rot, AttachmentPoint.MiddleCenter, corte ? lCrista : lPe);
                }
                Txt("ESTUDO DE PLATÔ - GALPÃO LOGÍSTICO\\PTerreno fictício para apresentação · coordenadas locais\\P" +
                    $"Taludes: corte 1V:1,5H · aterro 1V:2H · cota do piso que compensa corte e aterro (Fh = 1,25)", 10, YMAX - 8, 4.0, 0, AttachmentPoint.TopLeft);
                tr.Commit();
            }

            // ---- volumes e mapa pelo PRJVOLUMES (classes internas do plugin, por reflexão)
            var redes = typeof(Projeta.Civil3D.App).Assembly;
            var tV = redes.GetType("Projeta.Civil3D.Civil.VolumesCivil", true)!;
            var tE = redes.GetType("Projeta.Civil3D.Civil.VolumesCivil+Entrada", true)!;
            var tS = redes.GetType("Projeta.Civil3D.Civil.VolumesCivil+Saida", true)!;
            var e = Activator.CreateInstance(tE)!;
            void Pr(string n, object? v) => tE.GetProperty(n)!.SetValue(e, v);
            Pr("Base", tnId); Pr("Comparacao", finId); Pr("FatorCorte", 1.0); Pr("FatorAterro", FH); Pr("Desenho", db.Filename);
            // área de cálculo = platô + taludes (polígono do encontro dos taludes com o terreno)
            var tPol = redes.GetType("Projeta.Civil3D.Civil.VolumesCivil+Poligono", true)!;
            var vert = Taludes().Select(t => (E0 + t.Xd, N0 + t.Yd)).ToList();
            var pol = Activator.CreateInstance(tPol, "Platô + taludes", vert, handleDay)!;
            var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tPol))!;
            lista.Add(pol);
            Pr("Poligonos", lista);
            var saida = tV.GetMethod("Calcular")!.Invoke(null, new[] { db, e })!;
            var rel = (RelatorioVolumes)tS.GetProperty("Relatorio")!.GetValue(saida)!;
            var malha = (ResultadoMalha)tS.GetProperty("Malha")!.GetValue(saida)!;
            var tot = rel.Total;
            L($"VOLUMES: corte {tot.Corte:F2} m3, aterro {tot.Aterro:F2} m3, aterro x {FH} = {tot.Aterro * FH:F2}, saldo {rel.Saldo(tot):F2} ({RelatorioVolumes.Situacao(rel.Saldo(tot))}); area {tot.Area2D:F1} m2; malha {rel.Celula} m");
            foreach (var f in rel.Faixas.Where(f => f.Area > 0)) L($"   {f.Rotulo}: {f.Area:F0} m2, {f.Volume:F0} m3");
            using (var tr = db.TransactionManager.StartTransaction())
            {
                int n = (int)tV.GetMethod("DesenharMapa")!.Invoke(null, new object[] { tr, db, malha, rel })!;
                L($"MAPA: {n} solidos");
                tr.Commit();
            }
            rel.Salvar(Path.Combine(pasta, "plato_galpao_volumes.xlsx"));
            L("planilha: " + Path.Combine(pasta, "plato_galpao_volumes.xlsx"));
        }
        catch (System.Exception ex) { L("ERRO " + (ex is TargetInvocationException ti && ti.InnerException is not null ? ti.InnerException : ex)); }
        File.WriteAllText(@"C:\Temp\nsteste\plato\plato_log.txt", log.ToString());
        doc.Editor.WriteMessage("\nPLATO FIM");
    }
}
