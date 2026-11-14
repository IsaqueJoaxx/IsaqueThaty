using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(IQC3D.DadosTeste))]

namespace IQC3D
{
    /// <summary>IQ_DADOSTESTE: monta um desenho de teste (pontos, superfície, 2 eixos e um greide).</summary>
    public class DadosTeste
    {
        private static double Z(double x, double y) => 800 + 0.02 * x + 3 * Math.Sin(x / 60.0) + 2 * Math.Cos(y / 50.0);

        [CommandMethod("IQ_DADOSTESTE", CommandFlags.Modal)]
        public void Criar() => U.Rodar(Criar_);
        private void Criar_()
        {
            var civ = U.Civil;
            ObjectId eixoA, eixoB;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                // 1) pontos COGO do terreno + alguns duplicados e cadastro
                var pts = new Point3dCollection();
                for (double x = 0; x <= 400; x += 20)
                    for (double y = 0; y <= 400; y += 20) pts.Add(new Point3d(x, y, Z(x, y)));
                civ.CogoPoints.Add(pts, "TN", false);
                var dup = new Point3dCollection { new Point3d(100.01, 100.0, Z(100, 100)), new Point3d(200.0, 200.02, Z(200, 200)), new Point3d(300.03, 60.0, Z(300, 60)) };
                civ.CogoPoints.Add(dup, "TN DUP", false);
                var cerca = new Point3dCollection();
                for (int i = 0; i < 8; i++) cerca.Add(new Point3d(30 + i * 40, 390, Z(30 + i * 40, 390)));
                civ.CogoPoints.Add(cerca, "CERCA ARAME", false);
                var poste = new Point3dCollection { new Point3d(120, 150, Z(120, 150)), new Point3d(260, 240, Z(260, 240)) };
                civ.CogoPoints.Add(poste, "POSTE", false);

                // 2) superfície TIN
                var sid = TinSurface.Create(U.Db, "TESTE_TN");
                var s = (TinSurface)tr.GetObject(sid, OpenMode.ForWrite);
                s.AddVertices(pts);

                // 3) linha de quebra num layer próprio
                var lb = U.GarantirLayer(tr, "TESTE_BREAK", 2);
                var pb = new Point3dCollection();
                for (double x = 10; x <= 390; x += 38) pb.Add(new Point3d(x, 0.6 * x + 40, Z(x, 0.6 * x + 40) + 0.5));
                U.Adicionar(tr, new Polyline3d(Poly3dType.SimplePoly, pb, false) { LayerId = lb });

                // 4) dois eixos que se cruzam
                eixoA = CriarEixo(tr, civ, "EIXO_A", new[] { new Point2d(20, 60), new Point2d(200, 90), new Point2d(370, 300) });
                eixoB = CriarEixo(tr, civ, "EIXO_B", new[] { new Point2d(60, 360), new Point2d(170, 50), new Point2d(330, 30) });

                // 5) greide de projeto no EIXO_A
                var a = (Alignment)tr.GetObject(eixoA, OpenMode.ForRead);
                ObjectId ps = ObjectId.Null, pl = ObjectId.Null;
                try { ps = civ.Styles.ProfileStyles[0]; } catch { }
                try { pl = civ.Styles.LabelSetStyles.ProfileLabelSetStyles[0]; } catch { }
                var gid = Profile.CreateByLayout("GREIDE_A", eixoA, a.LayerId, ps, pl);
                var g = (Profile)tr.GetObject(gid, OpenMode.ForWrite);
                double L = a.EndingStation - a.StartingStation;
                g.PVIs.AddPVI(a.StartingStation, 804.0);
                g.PVIs.AddPVI(a.EndingStation, 815.5);
                g.PVIs.AddPVISymParabola(a.StartingStation + L * 0.5, 811.0, 80);
                tr.Commit();
            }
            U.Msg("Desenho de teste criado: ~450 pontos COGO (TN, TN DUP, CERCA ARAME, POSTE), superfície TESTE_TN, layer TESTE_BREAK, EIXO_A (com GREIDE_A) e EIXO_B.");
            U.Ed.Command("_.ZOOM", "_E");
        }

        private static ObjectId CriarEixo(Transaction tr, Autodesk.Civil.ApplicationServices.CivilDocument civ, string nome, Point2d[] vs)
        {
            var pl = new Polyline();
            for (int i = 0; i < vs.Length; i++) pl.AddVertexAt(i, vs[i], 0, 0, 0);
            var pid = U.Adicionar(tr, pl);
            ObjectId st = ObjectId.Null, ls = ObjectId.Null;
            try { st = civ.Styles.AlignmentStyles[0]; } catch { }
            try { ls = civ.Styles.LabelSetStyles.AlignmentLabelSetStyles[0]; } catch { }
            var opt = new PolylineOptions { PlineId = pid, AddCurvesBetweenTangents = true, EraseExistingEntities = true };
            return Alignment.Create(civ, opt, nome, ObjectId.Null, U.Db.Clayer, st, ls);
        }
    }
}
