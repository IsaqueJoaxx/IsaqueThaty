using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Infra;

internal enum ModoCor { Placa, Marca }

/// <summary>Converte o desenho vetorial do núcleo (<see cref="Desenho2D"/>) em entidades do DWG.</summary>
internal static class Conversor
{
    public static Color CorDe(Cor c, ModoCor modo, Situacao sit)
    {
        if (modo == ModoCor.Marca)
        {
            return sit switch
            {
                Situacao.Manter => Color.FromColorIndex(ColorMethod.ByAci, 8),
                Situacao.Remover => Color.FromColorIndex(ColorMethod.ByAci, 1),
                Situacao.Recuperar => Color.FromColorIndex(ColorMethod.ByAci, 6),
                _ => Color.FromColorIndex(ColorMethod.ByAci, Cores.Aci(c)),
            };
        }
        var (r, g, b) = Cores.Rgb(c);
        return Color.FromRgb(r, g, b);
    }

    private static HatchLoop Laco(IList<V2> pts, V2 desloc)
    {
        var laco = new HatchLoop(HatchLoopTypes.External | HatchLoopTypes.Polyline);
        foreach (var p in pts) laco.Polyline.Add(new BulgeVertex(new Point2d(p.X - desloc.X, p.Y - desloc.Y), 0));
        laco.Polyline.Add(new BulgeVertex(new Point2d(pts[0].X - desloc.X, pts[0].Y - desloc.Y), 0));
        return laco;
    }

    private static HatchLoop LacoCirculo(V2 c, double r, V2 desloc)
    {
        var laco = new HatchLoop(HatchLoopTypes.External | HatchLoopTypes.Polyline);
        laco.Polyline.Add(new BulgeVertex(new Point2d(c.X + r - desloc.X, c.Y - desloc.Y), 1));
        laco.Polyline.Add(new BulgeVertex(new Point2d(c.X - r - desloc.X, c.Y - desloc.Y), 1));
        laco.Polyline.Add(new BulgeVertex(new Point2d(c.X + r - desloc.X, c.Y - desloc.Y), 0));
        return laco;
    }

    private static Hatch NovaHachura(Transaction tr, BlockTableRecord btr, Color cor, string layer)
    {
        var h = new Hatch();
        h.SetDatabaseDefaults();
        h.Layer = layer;
        Ctx.Adicionar(tr, btr, h);
        h.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        h.Associative = false;
        h.Color = cor;
        return h;
    }

    /// <summary>
    /// Desenha as primitivas no bloco. <paramref name="desloc"/> é subtraído de todas as coordenadas (ponto base).
    /// Em marcas, áreas consecutivas da mesma cor viram uma única hachura com vários laços.
    /// </summary>
    public static int Desenhar(Transaction tr, BlockTableRecord btr, Desenho2D d, ModoCor modo, Situacao sit, V2 desloc, string layer = "0", bool agrupar = false)
    {
        int n = 0;
        Hatch? atual = null;
        Cor? corAtual = null;
        int lacos = 0;
        foreach (var p in d.Itens)
        {
            var cor = CorDe(p.Cor, modo, sit);
            switch (p)
            {
                case PArea a when a.Pts.Count >= 3:
                    if (Poligonos.Area(a.Pts) < 1e-10) break;
                    if (!agrupar || atual is null || corAtual != p.Cor || lacos >= 400)
                    {
                        if (atual is not null) atual.EvaluateHatch(true);
                        atual = NovaHachura(tr, btr, cor, layer);
                        corAtual = p.Cor; lacos = 0; n++;
                    }
                    atual.AppendLoop(Laco(a.Pts, desloc));
                    lacos++;
                    if (!agrupar) { atual.EvaluateHatch(true); atual = null; }
                    break;
                case PDisco c when c.R > 1e-6:
                    if (atual is not null) { atual.EvaluateHatch(true); atual = null; }
                    var hc = NovaHachura(tr, btr, cor, layer);
                    hc.AppendLoop(LacoCirculo(c.C, c.R, desloc));
                    hc.EvaluateHatch(true);
                    n++;
                    break;
                case PLinha l:
                    if (atual is not null) { atual.EvaluateHatch(true); atual = null; }
                    var pl = new Polyline();
                    pl.SetDatabaseDefaults();
                    for (int i = 0; i < l.Pts.Count; i++) pl.AddVertexAt(i, new Point2d(l.Pts[i].X - desloc.X, l.Pts[i].Y - desloc.Y), 0, 0, 0);
                    pl.Closed = l.Fechada;
                    pl.Color = cor;
                    pl.Layer = layer;
                    Ctx.Adicionar(tr, btr, pl);
                    n++;
                    break;
            }
        }
        if (atual is not null) atual.EvaluateHatch(true);
        return n;
    }

    public static void Disco(Transaction tr, BlockTableRecord btr, V2 c, double r, Color cor, string layer)
    {
        var h = NovaHachura(tr, btr, cor, layer);
        h.AppendLoop(LacoCirculo(c, r, V2.Zero));
        h.EvaluateHatch(true);
    }

    /// <summary>Cria um bloco anônimo (*U) vazio.</summary>
    public static (ObjectId Id, BlockTableRecord Btr) NovoBlocoAnonimo(Transaction tr, Database db)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
        var btr = new BlockTableRecord { Name = "*U", Origin = Point3d.Origin };
        var id = bt.Add(btr);
        tr.AddNewlyCreatedDBObject(btr, true);
        return (id, btr);
    }

    public static Polyline Poli(IList<V2> pts, V2 desloc, bool fechada, double largura = 0)
    {
        var pl = new Polyline();
        pl.SetDatabaseDefaults();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X - desloc.X, pts[i].Y - desloc.Y), 0, largura, largura);
        pl.Closed = fechada;
        return pl;
    }
}
