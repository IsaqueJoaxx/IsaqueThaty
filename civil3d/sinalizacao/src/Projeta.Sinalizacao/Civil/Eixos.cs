using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Civil;

/// <summary>Acesso a alinhamentos e superfícies do Civil 3D (isolado para o plugin também abrir no AutoCAD puro).</summary>
internal static class Eixos
{
    public static bool CivilDisponivel
    {
        get
        {
            try { return CivilApplication.ActiveDocument is not null; }
            catch { return false; }
        }
    }

    public static bool EhAlinhamento(ObjectId id)
    {
        try { return id.ObjectClass.IsDerivedFrom(Autodesk.AutoCAD.Runtime.RXObject.GetClass(typeof(Alignment))); }
        catch { return false; }
    }

    public static List<(ObjectId Id, string Nome)> Alinhamentos(Transaction tr)
    {
        var l = new List<(ObjectId, string)>();
        try
        {
            foreach (ObjectId id in CivilApplication.ActiveDocument.GetAlignmentIds())
            {
                var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                l.Add((id, a.Name));
            }
        }
        catch { }
        return l;
    }

    public static string Nome(Transaction tr, ObjectId id)
    {
        try { return ((Alignment)tr.GetObject(id, OpenMode.ForRead)).Name; } catch { return ""; }
    }

    /// <summary>Vértices (com bulges) do alinhamento. Espirais saem como trechos de polilinha aproximados.</summary>
    public static List<Vertice> Vertices(Transaction tr, ObjectId alinhamentoId)
    {
        var a = (Alignment)tr.GetObject(alinhamentoId, OpenMode.ForRead);
        try
        {
            var plId = a.GetPolyline();
            var pl = (Polyline)tr.GetObject(plId, OpenMode.ForWrite);
            var v = Selecao.VerticesDe(pl);
            pl.Erase();
            if (v.Count >= 2) return v;
        }
        catch { }
        // alternativa: amostragem a cada 1 m
        var r = new List<Vertice>();
        double ini = a.StartingStation, fim = a.EndingStation;
        double passo = Math.Max(0.5, Math.Min(1.0, (fim - ini) / 4000));
        for (double s = ini; s < fim; s += passo)
        {
            double x = 0, y = 0;
            a.PointLocation(s, 0, ref x, ref y);
            r.Add(new Vertice(x, y));
        }
        double xf = 0, yf = 0;
        a.PointLocation(fim, 0, ref xf, ref yf);
        r.Add(new Vertice(xf, yf));
        return r;
    }

    /// <summary>Estaca e afastamento (negativo = à direita) de um ponto no alinhamento. Null se estiver fora do alcance.</summary>
    public static (double Estaca, double Afast)? EstacaAfast(Transaction tr, ObjectId alinhamentoId, double x, double y)
    {
        try
        {
            var a = (Alignment)tr.GetObject(alinhamentoId, OpenMode.ForRead);
            double s = 0, o = 0;
            a.StationOffset(x, y, ref s, ref o);
            return (s, -o); // Civil 3D: afastamento positivo à direita; aqui positivo = esquerda
        }
        catch { return null; }
    }

    public static (double Inicio, double Fim) Limites(Transaction tr, ObjectId alinhamentoId)
    {
        var a = (Alignment)tr.GetObject(alinhamentoId, OpenMode.ForRead);
        return (a.StartingStation, a.EndingStation);
    }

    public static List<(ObjectId Id, string Nome)> Superficies(Transaction tr)
    {
        var l = new List<(ObjectId, string)>();
        try
        {
            foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
            {
                var s = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
                if (s is TinSurface) l.Add((id, s.Name));
            }
        }
        catch { }
        return l;
    }

    public static double? Cota(Transaction tr, ObjectId superficieId, double x, double y)
    {
        if (superficieId.IsNull) return null;
        try
        {
            var s = (TinSurface)tr.GetObject(superficieId, OpenMode.ForRead);
            return s.FindElevationAtXY(x, y);
        }
        catch { return null; }
    }

    /// <summary>Cria um alinhamento a partir de uma polilinha (usado na demonstração).</summary>
    public static ObjectId CriarDePolilinha(Transaction tr, Database db, ObjectId polilinhaId, string nome)
    {
        var doc = CivilApplication.ActiveDocument;
        var opts = new PolylineOptions { AddCurvesBetweenTangents = false, EraseExistingEntities = false, PlineId = polilinhaId };
        ObjectId estilo = doc.Styles.AlignmentStyles.Count > 0 ? doc.Styles.AlignmentStyles[0] : ObjectId.Null;
        ObjectId rotulos = doc.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count > 0 ? doc.Styles.LabelSetStyles.AlignmentLabelSetStyles[0] : ObjectId.Null;
        string layer = Ctx.GarantirLayer(tr, db, "SIN-EIXO", 4);
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        return Alignment.Create(doc, opts, nome, ObjectId.Null, lt[layer], estilo, rotulos);
    }
}
