using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Desenho;

/// <summary>Projeta tubos e PVs de uma rede dimensionada em um profile view do Civil 3D.</summary>
internal static class ProjecaoPerfil
{
    public static int Projetar(Sessao s, Rede rede, ProfileView pv, double tolerancia)
    {
        var res = rede.Resultado ?? throw new InvalidOperationException("Dimensione a rede antes (PRJCALCULAR).");
        var tr = s.Tr; var db = s.Db;
        var al = (Alignment)tr.GetObject(pv.AlignmentId, OpenMode.ForRead);
        string chave = "perfil:" + pv.Handle.ToString();
        if (rede.HandlesAuxiliares.TryGetValue(chave, out var antigos)) foreach (var hAnt in antigos) Ctx.Apagar(tr, db, hAnt);
        var novos = new List<string>();
        rede.HandlesAuxiliares[chave] = novos;

        string layer = DesenhoRede.Layer(rede, "PERFIL");
        Ctx.GarantirLayer(tr, db, layer, 5);
        string layerTxt = DesenhoRede.Layer(rede, "PERFIL-TEXTO");
        Ctx.GarantirLayer(tr, db, layerTxt, 7);
        var ms = Ctx.EspacoModelo(tr, db);
        double h = rede.AlturaTexto * 0.8;

        // estaca de cada nó no alinhamento do perfil
        var est = new Dictionary<string, double>();
        foreach (var n in rede.Nos)
            if (CivilUtil.EstacaAfastamento(al, n.X, n.Y, out var st, out var off) && Math.Abs(off) <= tolerancia && st >= pv.StationStart - 1e-6 && st <= pv.StationEnd + 1e-6)
                est[n.Id] = st;

        Point2d? XY(double estaca, double cota)
        {
            double x = 0, y = 0;
            return pv.FindXYAtStationAndElevation(estaca, cota, ref x, ref y) ? new Point2d(x, y) : null;
        }
        void Add(Entity e, string lay) { e.Layer = lay; Ctx.Adicionar(tr, ms, e); novos.Add(Ctx.HandleDe(e.ObjectId)); }

        int n0 = 0;
        foreach (var t in rede.Trechos)
        {
            if (!est.TryGetValue(t.NoMontanteId, out var e1) || !est.TryGetValue(t.NoJusanteId, out var e2)) continue;
            var r = res.Trecho(t.Id);
            if (r is null) continue;
            double esp = r.Espessura;
            var pts = new[]
            {
                XY(e1, r.CotaFundoMontante - esp), XY(e2, r.CotaFundoJusante - esp),
                XY(e2, r.GeratrizSupJusante + esp), XY(e1, r.GeratrizSupMontante + esp),
            };
            var ptsInt = new[] { XY(e1, r.CotaFundoMontante), XY(e2, r.CotaFundoJusante), XY(e2, r.GeratrizSupJusante), XY(e1, r.GeratrizSupMontante) };
            if (pts.Any(p => p is null) || ptsInt.Any(p => p is null)) continue;
            var ext = new Polyline(); ext.SetDatabaseDefaults();
            for (int i = 0; i < 4; i++) ext.AddVertexAt(i, pts[i]!.Value, 0, 0, 0);
            ext.Closed = true;
            Add(ext, layer);
            var inn = new Polyline(); inn.SetDatabaseDefaults();
            for (int i = 0; i < 4; i++) inn.AddVertexAt(i, ptsInt[i]!.Value, 0, 0, 0);
            inn.Closed = true;
            Add(inn, layer);
            // rótulo no meio, inclinado
            var m1 = XY((e1 + e2) / 2, (r.GeratrizSupMontante + r.GeratrizSupJusante) / 2 + esp)!.Value;
            var d = ptsInt[2]!.Value - ptsInt[3]!.Value;
            double ang = Geo.AnguloLegivel(Math.Atan2(d.Y, d.X));
            var mt = Ctx.Texto(Ctx.MTextLinhas($"{r.Secao} L={Num.F(r.Comprimento)} i={Num.F(r.Declividade * 100, 3)}%"), new Point3d(m1.X, m1.Y + h * 0.4, 0), h, ang, AttachmentPoint.BottomCenter);
            Add(mt, layerTxt);
            n0++;
        }
        foreach (var n in rede.Nos)
        {
            if (!est.TryGetValue(n.Id, out var e)) continue;
            var rn = res.No(n.Id);
            if (rn is null) continue;
            double meia = Math.Max(0.5, rn.MaiorSecao / 2 + 0.3);
            var p1 = XY(e - meia, rn.CotaFundo); var p2 = XY(e + meia, rn.CotaFundo); var p3 = XY(e + meia, rn.CotaTampa); var p4 = XY(e - meia, rn.CotaTampa);
            if (p1 is null || p2 is null || p3 is null || p4 is null) continue;
            var pl = new Polyline(); pl.SetDatabaseDefaults();
            pl.AddVertexAt(0, p1.Value, 0, 0, 0); pl.AddVertexAt(1, p2.Value, 0, 0, 0); pl.AddVertexAt(2, p3.Value, 0, 0, 0); pl.AddVertexAt(3, p4.Value, 0, 0, 0);
            pl.Closed = true;
            Add(pl, layer);
            var topo = XY(e, rn.CotaTampa)!.Value;
            var mt = Ctx.Texto(Ctx.MTextLinhas(n.Nome, $"CT={Num.F(rn.CotaTampa, 3)}", $"CF={Num.F(rn.CotaFundo, 3)}", $"h={Num.F(rn.Profundidade)}"),
                new Point3d(topo.X, topo.Y + h * 1.5, 0), h, Math.PI / 2, AttachmentPoint.MiddleLeft);
            Add(mt, layerTxt);
        }
        return n0;
    }
}
