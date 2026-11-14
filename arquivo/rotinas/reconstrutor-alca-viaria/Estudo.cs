using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(Projeta.Reconstrutor.Estudo))]

namespace Projeta.Reconstrutor;

public class Estudo
{
    static double Z(Surface s, double x, double y) { try { return s.FindElevationAtXY(x, y); } catch { return double.NaN; } }

    // Secoes a cada 10 m do corredor do eixo principal: areas de corte/aterro (DATUM x Primitivo), limites da faixa, alturas
    [CommandMethod("PRJSECOES", CommandFlags.Modal)]
    public void Secoes()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        var linhas = new List<double[]>();
        string nomeAl = "";
        using (var tr = db.TransactionManager.StartTransaction())
        {
            Surface tn = null;
            foreach (ObjectId sid in cd.GetSurfaceIds()) { var s = (Surface)tr.GetObject(sid, OpenMode.ForRead); if (s.Name == "Primitivo") tn = s; }
            Corridor cor = null;
            foreach (ObjectId cid in cd.CorridorCollection) { var c = (Corridor)tr.GetObject(cid, OpenMode.ForRead); if (c.Name == "COR - EIXO 01 - PRINCIPAL") cor = c; }
            Surface datum = null, topo = null;
            foreach (CorridorSurface cs in cor.CorridorSurfaces) { var s = (Surface)tr.GetObject(cs.SurfaceId, OpenMode.ForRead); if (cs.Name.EndsWith("DATUM")) datum = s; if (cs.Name.EndsWith("TOPO")) topo = s; }
            var al = (Alignment)tr.GetObject(cor.Baselines[0].AlignmentId, OpenMode.ForRead); nomeAl = al.Name;
            var pr = (Profile)tr.GetObject(cor.Baselines[0].ProfileId, OpenMode.ForRead);
            const double dO = 0.5, maxO = 150;
            for (double st = 0; st <= al.EndingStation + 1e-6; st += 10)
            {
                double s = Math.Min(st, al.EndingStation);
                double x = 0, y = 0, b = 0; al.PointLocation(s, 0, 0.001, ref x, ref y, ref b);
                double zg = pr.ElevationAt(s), zt = Z(tn, x, y);
                double ac = 0, aa = 0, hc = 0, ha = 0; double ext = 0, exd = 0;
                foreach (int lado in new[] { -1, 1 })
                {
                    double lim = 0; bool fora = false;
                    for (double o = dO / 2; o < maxO; o += dO)
                    {
                        double ox = 0, oy = 0; al.PointLocation(s, lado * o, ref ox, ref oy);
                        double zd = Z(datum, ox, oy);
                        if (double.IsNaN(zd)) { if (o > 12) { fora = true; break; } continue; }
                        double zz = Z(tn, ox, oy); if (double.IsNaN(zz)) continue;
                        double dz = zd - zz;
                        if (dz > 0) { aa += dz * dO; ha = Math.Max(ha, dz); } else { ac += -dz * dO; hc = Math.Max(hc, -dz); }
                        lim = o;
                    }
                    if (lado < 0) ext = lim; else exd = lim;
                }
                double zTop = Z(topo, x, y);
                linhas.Add(new[] { Math.Round(s, 3), Math.Round(x, 3), Math.Round(y, 3), Math.Round(b, 6), Math.Round(zg, 3), Math.Round(zt, 3), Math.Round(ac, 2), Math.Round(aa, 2), Math.Round(hc, 2), Math.Round(ha, 2), Math.Round(ext, 2), Math.Round(exd, 2), Math.Round(zTop, 3) });
            }
            tr.Abort();
        }
        File.WriteAllText(Resumo.Saida("prj_secoes.json"), JsonSerializer.Serialize(new { eixo = nomeAl, campos = "est,x,y,rumo,z_greide,z_tn,area_corte,area_aterro,h_corte,h_aterro,larg_esq,larg_dir,z_topo", linhas }, new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJSECOES ok {linhas.Count}\n");
    }

    // Grade do terreno Primitivo (MDT) para mapas: float32, linhas de sul para norte
    [CommandMethod("PRJDEM", CommandFlags.Modal)]
    public void Dem()
    {
        var db = HostApplicationServices.WorkingDatabase; var cd = CivilApplication.ActiveDocument;
        double x0 = 663250, y0 = 7744100, x1 = 664900, y1 = 7748950, d = 4;
        int nx = (int)((x1 - x0) / d) + 1, ny = (int)((y1 - y0) / d) + 1;
        var buf = new byte[nx * ny * 4];
        using (var tr = db.TransactionManager.StartTransaction())
        {
            Surface tn = null;
            foreach (ObjectId sid in cd.GetSurfaceIds()) { var s = (Surface)tr.GetObject(sid, OpenMode.ForRead); if (s.Name == "Primitivo") tn = s; }
            int k = 0;
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++, k += 4)
                {
                    float z = (float)Z(tn, x0 + i * d, y0 + j * d);
                    BitConverter.GetBytes(z).CopyTo(buf, k);
                }
            tr.Abort();
        }
        var outp = Resumo.Saida("prj_dem.bin");
        File.WriteAllBytes(outp, buf);
        File.WriteAllText(outp + ".json", JsonSerializer.Serialize(new { x0, y0, d, nx, ny }));
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nPRJDEM ok {nx}x{ny}\n");
    }
}
