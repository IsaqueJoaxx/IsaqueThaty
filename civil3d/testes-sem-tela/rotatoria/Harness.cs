using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(RotHarness.Cmd))]

namespace RotHarness;

/// <summary>Teste sem tela dos comandos de rotatória: anel R 30 com três ramos (um casa, um não encosta, um com cota errada).</summary>
public sealed class Cmd
{
    static readonly CultureInfo I = CultureInfo.InvariantCulture;
    const double X0 = 1000, Y0 = 1000, R = 30;

    static ObjectId Eixo(Transaction tr, Database db, CivilDocument civ, string nome, Polyline pl)
    {
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        pl.SetDatabaseDefaults();
        var pid = ms.AppendEntity(pl); tr.AddNewlyCreatedDBObject(pl, true);
        var st = civ.Styles.AlignmentStyles[0];
        var ls = civ.Styles.LabelSetStyles.AlignmentLabelSetStyles[0];
        return Alignment.Create(civ, new PolylineOptions { PlineId = pid, AddCurvesBetweenTangents = false, EraseExistingEntities = true }, nome, ObjectId.Null, db.Clayer, st, ls);
    }

    static void Greide(Transaction tr, CivilDocument civ, ObjectId alId, string nome, double z0, double z1)
    {
        var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead);
        var gid = Profile.CreateByLayout(nome, alId, al.LayerId, civ.Styles.ProfileStyles[0], civ.Styles.LabelSetStyles.ProfileLabelSetStyles[0]);
        var g = (Profile)tr.GetObject(gid, OpenMode.ForWrite);
        g.PVIs.AddPVI(al.StartingStation, z0);
        g.PVIs.AddPVI(al.EndingStation, z1);
    }

    static Polyline Reta(double x0, double y0, double x1, double y1)
    {
        var pl = new Polyline();
        pl.AddVertexAt(0, new Point2d(x0, y0), 0, 0, 0);
        pl.AddVertexAt(1, new Point2d(x1, y1), 0, 0, 0);
        return pl;
    }

    [CommandMethod("PRJROTPREP")]
    public static void Prep()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        using var tr = db.TransactionManager.StartTransaction();
        // anel: círculo anti-horário começando a leste
        var anel = new Polyline();
        anel.AddVertexAt(0, new Point2d(X0 + R, Y0), 1, 0, 0);
        anel.AddVertexAt(1, new Point2d(X0 - R, Y0), 1, 0, 0);
        anel.Closed = true;
        var a = Eixo(tr, db, civ, "Anel", anel);
        Greide(tr, civ, a, "Anel - greide", 850, 850);
        // ramo leste: chega exatamente no começo do anel (emenda do eixo fechado), cota casa
        var l = Eixo(tr, db, civ, "Ramo Leste", Reta(X0 + 130, Y0, X0 + R, Y0));
        Greide(tr, civ, l, "Ramo Leste - greide", 851, 850);
        // ramo norte: para 12 cm antes do eixo do anel
        var n = Eixo(tr, db, civ, "Ramo Norte", Reta(X0, Y0 + 130, X0, Y0 + R + 0.12));
        Greide(tr, civ, n, "Ramo Norte - greide", 852, 850);
        // ramo oeste: encosta, mas a cota final está 15 cm acima
        var o = Eixo(tr, db, civ, "Ramo Oeste", Reta(X0 - 130, Y0, X0 - R, Y0));
        Greide(tr, civ, o, "Ramo Oeste - greide", 853, 850.15);
        tr.Commit();
        doc.Editor.WriteMessage("\nROTPREP OK");
    }

    /// <summary>
    /// Para o PRJROTATORIA, centro (2000; 2000): via principal leste-oeste atravessando o centro (2 braços), ramo norte que
    /// termina no centro, ramo sudoeste que termina a 14 m do centro (dentro do anel) e um ramo sul curto que não chega no anel.
    /// </summary>
    [CommandMethod("PRJROTPREP2")]
    public static void Prep2()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        using var tr = db.TransactionManager.StartTransaction();
        var v = Eixo(tr, db, civ, "Via Principal", Reta(1880, 2000, 2120, 2000));
        Greide(tr, civ, v, "Via Principal - greide", 852, 848);
        var n = Eixo(tr, db, civ, "Ramo Norte", Reta(2000, 2130, 2000, 2000));
        Greide(tr, civ, n, "Ramo Norte - greide", 851, 850);
        var so = Eixo(tr, db, civ, "Ramo Sudoeste", Reta(1900, 1900, 1990, 1990));
        Greide(tr, civ, so, "Ramo Sudoeste - greide", 853, 850.4);
        var su = Eixo(tr, db, civ, "Ramo Sul Curto", Reta(2000, 1850, 2000, 1950));
        Greide(tr, civ, su, "Ramo Sul Curto - greide", 851, 850);
        tr.Commit();
        doc.Editor.WriteMessage("\nROTPREP2 OK");
    }

    /// <summary>Rotatória "boa" para o PRJROTATORIA, centro (3000; 3000): via leste-oeste atravessando, ramo norte e ramo sul chegando no centro.</summary>
    [CommandMethod("PRJROTPREP3")]
    public static void Prep3()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        using var tr = db.TransactionManager.StartTransaction();
        var v = Eixo(tr, db, civ, "Avenida", Reta(2800, 3000, 3200, 3000));
        Greide(tr, civ, v, "Avenida - greide", 902, 898);
        var n = Eixo(tr, db, civ, "Rua Norte", Reta(3000, 3200, 3000, 3000));
        Greide(tr, civ, n, "Rua Norte - greide", 903, 900);
        var su = Eixo(tr, db, civ, "Rua Sul", Reta(3000, 2800, 3000, 3000));
        Greide(tr, civ, su, "Rua Sul - greide", 899, 900);
        tr.Commit();
        doc.Editor.WriteMessage("\nROTPREP3 OK");
    }

    /// <summary>Rotatória (5000; 5000) com uma via N-S atravessando, um ramo leste que acaba 18 m antes do anel (no fim)
    /// e um ramo oeste que começa 13 m antes do anel (no início): testa a pergunta do número de braços e o prolongamento.</summary>
    [CommandMethod("PRJROTPREP4")]
    public static void Prep4()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        using var tr = db.TransactionManager.StartTransaction();
        var v = Eixo(tr, db, civ, "Via NS", Reta(5000, 4800, 5000, 5200));
        Greide(tr, civ, v, "Via NS - greide", 902, 898);
        var l = Eixo(tr, db, civ, "Rua Leste", Reta(5200, 5000, 5035, 5000));
        Greide(tr, civ, l, "Rua Leste - greide", 903, 901);
        var o = Eixo(tr, db, civ, "Rua Oeste", Reta(4970, 5000, 4800, 5000));
        Greide(tr, civ, o, "Rua Oeste - greide", 900.5, 899);
        // corredor existente na Via NS (faixa simples dos dois lados), para testar o recorte das regiões
        var asmId = civ.AssemblyCollection.Add("Seção existente", AssemblyType.UndividedCrownedRoad, new Point3d(5300, 4900, 0));
        var asm = (Autodesk.Civil.DatabaseServices.Assembly)tr.GetObject(asmId, OpenMode.ForWrite);
        foreach (bool esq in new[] { false, true })
        {
            var sid = civ.SubassemblyCollection.ImportStockSubassembly(esq ? "Faixa existente E" : "Faixa existente D", "Subassembly.BasicLane", new Point3d(5300, 4900, 0));
            var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForWrite);
            sa.ParamsLong["Side"].Value = esq ? 1 : 0;
            asm.AddSubassembly(sid);
        }
        var pidNS = ((Alignment)tr.GetObject(v, OpenMode.ForRead)).GetProfileIds().Cast<ObjectId>().First(id => ((Profile)tr.GetObject(id, OpenMode.ForRead)).ProfileType != ProfileType.EG);
        var cid = civ.CorridorCollection.Add("Corredor existente", "Via NS", v, pidNS, "Via NS - existente", asmId);
        ((Corridor)tr.GetObject(cid, OpenMode.ForWrite)).Rebuild();
        tr.Commit();
        doc.Editor.WriteMessage("\nROTPREP4 OK");
    }

    /// <summary>Interseção em (7000; 7000): avenida leste-oeste atravessando (com corredor existente), rua norte chegando no
    /// cruzamento e rua sul saindo dele a ~80° (com 2 cm de diferença de cota, para a verificação reprovar).</summary>
    [CommandMethod("PRJINTPREP")]
    public static void PrepIntersecao()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        using var tr = db.TransactionManager.StartTransaction();
        var av = Eixo(tr, db, civ, "Avenida Principal", Reta(6800, 7000, 7200, 7000));
        Greide(tr, civ, av, "Avenida Principal - greide", 905, 903);
        var rn = Eixo(tr, db, civ, "Rua Norte", Reta(7000, 7200, 7000, 7000));
        Greide(tr, civ, rn, "Rua Norte - greide", 906, 904);
        var rs = Eixo(tr, db, civ, "Rua Sul", Reta(7000, 7000, 7035, 6801.5));
        Greide(tr, civ, rs, "Rua Sul - greide", 904.02, 902);
        var asmId = civ.AssemblyCollection.Add("Seção existente", AssemblyType.UndividedCrownedRoad, new Point3d(7300, 6900, 0));
        var asm = (Autodesk.Civil.DatabaseServices.Assembly)tr.GetObject(asmId, OpenMode.ForWrite);
        foreach (bool esq in new[] { false, true })
        {
            var sid = civ.SubassemblyCollection.ImportStockSubassembly(esq ? "Faixa existente E" : "Faixa existente D", "Subassembly.BasicLane", new Point3d(7300, 6900, 0));
            var sa = (Subassembly)tr.GetObject(sid, OpenMode.ForWrite);
            sa.ParamsLong["Side"].Value = esq ? 1 : 0;
            asm.AddSubassembly(sid);
        }
        var pid = ((Alignment)tr.GetObject(av, OpenMode.ForRead)).GetProfileIds().Cast<ObjectId>().First(id => ((Profile)tr.GetObject(id, OpenMode.ForRead)).ProfileType != ProfileType.EG);
        var cid = civ.CorridorCollection.Add("Corredor da avenida", "Avenida Principal", av, pid, "Avenida - existente", asmId);
        ((Corridor)tr.GetObject(cid, OpenMode.ForWrite)).Rebuild();
        tr.Commit();
        doc.Editor.WriteMessage("\nINTPREP OK");
    }
    /// <summary>
    /// Loteamento de teste para o PRJVIAS: terreno TIN inclinado e ondulado (9000–9400) e quatro polilinhas de eixo — uma rua
    /// com vértice, uma que a cruza, uma que para a 3 m dela (T) e uma que continua na ponta de outra.
    /// </summary>
    [CommandMethod("PRJVIASPREP")]
    public static void PrepVias()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        using var tr = db.TransactionManager.StartTransaction();
        var sid = TinSurface.Create(db, "Terreno");
        var sup = (TinSurface)tr.GetObject(sid, OpenMode.ForWrite);
        var pts = new Point3dCollection();
        for (double x = 9000; x <= 9400; x += 10)
            for (double y = 9000; y <= 9400; y += 10)
                pts.Add(new Point3d(x, y, 900 + 0.04 * (x - 9000) + 3 * Math.Sin((y - 9000) / 60.0) + 1.5 * Math.Cos((x - 9000) / 45.0)));
        sup.AddVertices(pts);
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        void Pl(params double[] xy)
        {
            var pl = new Polyline();
            for (int i = 0; i < xy.Length / 2; i++) pl.AddVertexAt(i, new Point2d(xy[2 * i], xy[2 * i + 1]), 0, 0, 0);
            pl.SetDatabaseDefaults();
            ms.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
        }
        Pl(9020, 9200, 9200, 9200, 9380, 9260);   // rua com vértice
        Pl(9150, 9020, 9150, 9380);               // cruza a primeira
        Pl(9300, 9050, 9300, 9230);               // para a ~3 m da primeira (T)
        Pl(9150, 9380, 9250, 9380);               // continua na ponta da segunda
        tr.Commit();
        doc.Editor.WriteMessage("\nVIASPREP OK");
    }

    [CommandMethod("PRJROTCHECK")]
    public static void Check()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        var civ = CivilApplication.ActiveDocument;
        var sb = new StringBuilder();
        using var tr = db.TransactionManager.StartTransaction();
        foreach (ObjectId id in civ.GetAlignmentIds())
        {
            var al = (Alignment)tr.GetObject(id, OpenMode.ForRead);
            double x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            al.PointLocation(al.StartingStation, 0, ref x0, ref y0);
            al.PointLocation(al.EndingStation, 0, ref x1, ref y1);
            sb.AppendLine(string.Format(I, "ALINHAMENTO {0}: est {1:0.000} a {2:0.000}; ini ({3:0.0000}; {4:0.0000}) fim ({5:0.0000}; {6:0.0000}); entidades {7}",
                al.Name, al.StartingStation, al.EndingStation, x0, y0, x1, y1, al.Entities.Count));
            foreach (AlignmentEntity e in al.Entities)
                if (e is AlignmentArc arc)
                    sb.AppendLine(string.Format(I, "  arco R {0:0.0000} centro ({1:0.0000}; {2:0.0000}) L {3:0.0000} horario {4}", arc.Radius, arc.CenterPoint.X, arc.CenterPoint.Y, arc.Length, arc.Clockwise));
            foreach (ObjectId pid in al.GetProfileIds())
            {
                var p = (Profile)tr.GetObject(pid, OpenMode.ForRead);
                sb.Append(string.Format(I, "  GREIDE {0} ({1}):", p.Name, p.ProfileType));
                foreach (ProfilePVI v in p.PVIs) sb.Append(string.Format(I, " [{0:0.000}; {1:0.0000}]", v.RawStation, v.Elevation));
                sb.AppendLine();
                double l = p.EndingStation - p.StartingStation;
                for (int k = 0; k <= 4; k++)
                {
                    double s = p.StartingStation + l * k / 4;
                    sb.AppendLine(string.Format(I, "    z({0:0.000}) = {1:0.0000}", s, p.ElevationAt(s)));
                }
            }
        }
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
        int circ = 0, txt = 0;
        foreach (ObjectId id in ms)
        {
            var en = (Entity)tr.GetObject(id, OpenMode.ForRead);
            if (en.Layer != "PRJ-CASAMENTO") continue;
            if (en is Circle c) { circ++; sb.AppendLine(string.Format(I, "MARCA cor {0} em ({1:0.000}; {2:0.000})", c.ColorIndex, c.Center.X, c.Center.Y)); }
            if (en is MText m) { txt++; sb.AppendLine("  texto: " + m.Contents); }
        }
        sb.AppendLine($"MARCAS {circ} circulos {txt} textos");
        Directory.CreateDirectory(@"C:\Temp\rot\saida");
        File.WriteAllText(@"C:\Temp\rot\saida\rot_check.txt", sb.ToString(), Encoding.UTF8);
        sb.Clear();
        // corredores: baselines e regiões
        foreach (ObjectId cid in civ.CorridorCollection)
        {
            try
            {
                var c = (Corridor)tr.GetObject(cid, OpenMode.ForRead);
                File.AppendAllText(@"C:\Temp\rot\saida\rot_check.txt", "CORREDOR " + c.Name + Environment.NewLine, Encoding.UTF8);
                foreach (Baseline b in c.Baselines)
                {
                    var linha = new StringBuilder("  BASELINE " + b.Name + ":");
                    foreach (BaselineRegion r in b.BaselineRegions) linha.Append(string.Format(I, " [{0} {1:0.00}-{2:0.00}]", r.Name, r.StartStation, r.EndStation));
                    File.AppendAllText(@"C:\Temp\rot\saida\rot_check.txt", linha + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (System.Exception e) { File.AppendAllText(@"C:\Temp\rot\saida\rot_check.txt", "  erro corredor: " + e.Message + Environment.NewLine, Encoding.UTF8); }
        }
        // montagens: só os nomes (ler grupos e pontos de uma montagem já gravada derruba o accoreconsole)
        foreach (ObjectId aid in civ.AssemblyCollection)
        {
            string nome = "?";
            try { nome = ((Autodesk.Civil.DatabaseServices.Assembly)tr.GetObject(aid, OpenMode.ForRead)).Name; } catch { }
            File.AppendAllText(@"C:\Temp\rot\saida\rot_check.txt", "MONTAGEM " + nome + Environment.NewLine, Encoding.UTF8);
        }
        tr.Commit();
        Directory.CreateDirectory(@"C:\Temp\rot\saida");
        File.AppendAllText(@"C:\Temp\rot\saida\rot_check.txt", sb.ToString(), Encoding.UTF8);
        doc.Editor.WriteMessage("\nROTCHECK OK");
    }
}
