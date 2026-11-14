using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Verificacao;

namespace Projeta.Giro.Infra;

/// <summary>Converte curvas do desenho (polilinha, linha, arco, círculo, spline, alinhamento, feature line...) em pontos 2D.</summary>
internal static class Curvas
{
    /// <summary>Pontos da curva (arcos discretizados com flecha máxima; curvas genéricas a cada <paramref name="passo"/> m).</summary>
    public static List<V2>? Pontos(Entity e, double flecha, double passo, out bool fechada, out string tipo)
    {
        fechada = false;
        tipo = e.GetType().Name;
        switch (e)
        {
            case Polyline pl:
            {
                var p = new Pl { Fechada = pl.Closed };
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    var q = pl.GetPoint2dAt(i);
                    p.Add(new V2(q.X, q.Y), pl.GetBulgeAt(i));
                }
                fechada = pl.Closed;
                tipo = "polilinha";
                return Geo.Limpar(p.Pontos(flecha));
            }
            case Line ln:
                tipo = "linha";
                return new() { Ctx.V(ln.StartPoint), Ctx.V(ln.EndPoint) };
            case Arc a when Math.Abs(a.Normal.Z - 1) < 1e-9:
            {
                tipo = "arco";
                double sw = a.EndAngle - a.StartAngle;
                while (sw <= 0) sw += 2 * Math.PI;
                return Geo.PontosArco(Ctx.V(a.Center), a.Radius, a.StartAngle, sw, flecha);
            }
            case Circle c when Math.Abs(c.Normal.Z - 1) < 1e-9:
            {
                tipo = "círculo";
                fechada = true;
                var r = Geo.PontosArco(Ctx.V(c.Center), c.Radius, 0, 2 * Math.PI, flecha);
                r.RemoveAt(r.Count - 1);
                return r;
            }
            case Autodesk.Civil.DatabaseServices.Alignment al:
            {
                tipo = "alinhamento";
                var r = new List<V2>();
                double s0 = al.StartingStation, s1 = al.EndingStation;
                int n = Math.Clamp((int)Math.Ceiling((s1 - s0) / passo), 1, 200000);
                for (int i = 0; i <= n; i++)
                {
                    double x = 0, y = 0;
                    al.PointLocation(s0 + (s1 - s0) * i / n, 0, ref x, ref y);
                    r.Add(new V2(x, y));
                }
                return Geo.Limpar(r);
            }
            case Curve cv:
            {
                if (cv is Autodesk.Civil.DatabaseServices.FeatureLine) tipo = "feature line";
                else if (cv is Polyline3d) tipo = "polilinha 3D";
                else if (cv is Polyline2d) tipo = "polilinha 2D";
                else if (cv is Spline) tipo = "spline";
                try
                {
                    fechada = cv.Closed;
                    double len = cv.GetDistanceAtParameter(cv.EndParam) - cv.GetDistanceAtParameter(cv.StartParam);
                    int n = Math.Clamp((int)Math.Ceiling(len / passo), 1, 200000);
                    var r = new List<V2>(n + 1);
                    for (int i = 0; i <= n; i++) r.Add(Ctx.V(cv.GetPointAtDist(len * i / n)));
                    if (fechada && r.Count > 2 && r[0].Dist(r[^1]) < 1e-6) r.RemoveAt(r.Count - 1);
                    return Geo.Limpar(r);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception)
                {
                    if (cv is Autodesk.Civil.DatabaseServices.FeatureLine fl)
                    {
                        var pts = fl.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
                        return Geo.Limpar(pts.Cast<Point3d>().Select(Ctx.V));
                    }
                    return null;
                }
            }
        }
        return null;
    }

    /// <summary>Obstáculos a partir de uma seleção (curvas; blocos e hachuras são ignorados).</summary>
    public static List<Obstaculo> Obstaculos(Transaction tr, IEnumerable<ObjectId> ids, out int ignorados)
    {
        var r = new List<Obstaculo>();
        ignorados = 0;
        foreach (var id in ids)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity e) { ignorados++; continue; }
            if (Armazenamento.Marca(e) is not null) { ignorados++; continue; } // não verifica contra as próprias varreduras
            var pts = Pontos(e, 0.01, 0.25, out bool fechada, out _);
            if (pts is null || pts.Count < 2) { ignorados++; continue; }
            r.Add(new Obstaculo { Nome = e.Handle.ToString(), Pontos = pts, Fechado = fechada && pts.Count >= 3 });
        }
        return r;
    }

    public const string FiltroCurvas = "LINE,ARC,CIRCLE,LWPOLYLINE,POLYLINE,SPLINE,ELLIPSE,AECC_ALIGNMENT,AECC_FEATURE_LINE";
}
