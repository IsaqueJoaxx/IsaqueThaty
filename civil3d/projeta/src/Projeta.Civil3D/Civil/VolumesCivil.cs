using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Superficies;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Volumes de corte e aterro entre duas superfícies. O volume exato vem da superfície de volume TIN do Civil 3D
/// (criada numa transação própria e desfeita no fim, a menos que o usuário queira mantê-la); a malha serve ao mapa
/// de cores e ao quadro por faixas de altura.
/// </summary>
internal static class VolumesCivil
{
    public const string LayerMapa = "PRJ-VOLUMES-MAPA";

    public sealed record Poligono(string Nome, List<(double X, double Y)> Vertices, string Handle);

    public sealed class Entrada
    {
        public ObjectId Base { get; init; }
        public ObjectId Comparacao { get; init; }
        /// <summary>Áreas de cálculo; vazio = onde as duas superfícies se sobrepõem.</summary>
        public List<Poligono> Poligonos { get; init; } = new();
        public double FatorCorte { get; init; } = 1;
        public double FatorAterro { get; init; } = 1;
        public bool ManterSuperficie { get; init; }
        /// <summary>Lado da célula da malha (m); 0 = automático.</summary>
        public double Celula { get; init; }
        public string? Desenho { get; init; }
    }

    public sealed class Saida
    {
        public RelatorioVolumes Relatorio { get; init; } = null!;
        public ResultadoMalha Malha { get; init; } = null!;
        /// <summary>Nome da superfície de volume mantida no desenho (null se foi desfeita).</summary>
        public string? SuperficieVolume { get; init; }
    }

    /// <summary>Superfícies que podem ser base ou comparação (as de volume ficam de fora).</summary>
    public static List<(ObjectId Id, string Nome)> Superficies(Transaction tr)
    {
        var l = new List<(ObjectId, string)>();
        foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Surface s && !s.IsVolumeSurface) l.Add((id, s.Name));
        return l;
    }

    /// <summary>Vértices de uma curva fechada (arcos divididos a cada ~0,5 m); null se a curva não é fechada.</summary>
    public static List<(double X, double Y)>? Vertices(Curve c)
    {
        var r = new List<(double X, double Y)>();
        if (c is Polyline p)
        {
            bool fechada = p.Closed || (p.NumberOfVertices > 2 && p.GetPoint2dAt(0).GetDistanceTo(p.GetPoint2dAt(p.NumberOfVertices - 1)) < 1e-6);
            if (!fechada || p.NumberOfVertices < 3) return null;
            int nseg = p.Closed ? p.NumberOfVertices : p.NumberOfVertices - 1;
            for (int i = 0; i < nseg; i++)
            {
                var v = p.GetPoint2dAt(i);
                r.Add((v.X, v.Y));
                if (Math.Abs(p.GetBulgeAt(i)) > 1e-12 && p.GetSegmentType(i) == SegmentType.Arc)
                {
                    var arco = p.GetArcSegment2dAt(i);
                    double len = arco.GetLength(arco.GetParameterOf(arco.StartPoint), arco.GetParameterOf(arco.EndPoint));
                    int k = Math.Clamp((int)Math.Ceiling(len / 0.5), 4, 400);
                    var pts = arco.GetSamplePoints(k + 1);
                    for (int j = 1; j < pts.Length - 1; j++) r.Add((pts[j].X, pts[j].Y));
                }
            }
        }
        else
        {
            if (!c.Closed) return null;
            double len = c.GetDistanceAtParameter(c.EndParam) - c.GetDistanceAtParameter(c.StartParam);
            int n = Math.Clamp((int)Math.Ceiling(len / 0.5), 32, 4000);
            for (int i = 0; i < n; i++)
            {
                var q = c.GetPointAtDist(len * i / n);
                r.Add((q.X, q.Y));
            }
        }
        return r.Count >= 3 ? r : null;
    }

    /// <summary>Pede a seleção de curvas fechadas (lotes, platôs, trechos); null se o usuário cancelar ou nenhuma servir.</summary>
    public static List<Poligono>? SelecionarPoligonos(Sessao s, string mensagem)
    {
        var filtro = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE,CIRCLE,ELLIPSE,SPLINE") });
        var r = s.Ed.GetSelection(new PromptSelectionOptions { MessageForAdding = $"\n{mensagem}: " }, filtro);
        if (r.Status != PromptStatus.OK) return null;
        var l = new List<Poligono>();
        int abertos = 0;
        foreach (var id in r.Value.GetObjectIds())
        {
            if (s.Tr.GetObject(id, OpenMode.ForRead) is not Curve c) continue;
            var v = Vertices(c);
            if (v is null) { abertos++; continue; }
            l.Add(new Poligono($"Polígono {l.Count + 1} ({c.Layer})", v, id.Handle.ToString()));
        }
        if (abertos > 0) Ctx.Msg($"{abertos} curva(s) aberta(s) ignorada(s).");
        if (l.Count == 0) { Ctx.Msg("Nenhum polígono fechado selecionado."); return null; }
        return l;
    }

    /// <summary>Nome de superfície ainda não usado no desenho (acrescenta " (2)", " (3)"...).</summary>
    internal static string NomeLivre(Transaction tr, string desejado)
    {
        var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Surface s) existentes.Add(s.Name);
        var nome = desejado;
        for (int i = 2; existentes.Contains(nome); i++) nome = $"{desejado} ({i})";
        return nome;
    }

    public static Saida Calcular(Database db, Entrada e)
    {
        if (e.Base == e.Comparacao) throw new InvalidOperationException("A superfície base e a de comparação são a mesma.");
        if (!(e.FatorCorte > 0) || !(e.FatorAterro > 0)) throw new InvalidOperationException("Os fatores de corte e de aterro devem ser positivos.");
        var rel = new RelatorioVolumes { FatorCorte = e.FatorCorte, FatorAterro = e.FatorAterro };
        string? mantida = null;
        ResultadoMalha malha;

        // transação própria: a superfície de volume some no fim (Abort), a menos que o usuário queira mantê-la
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var b = (Surface)tr.GetObject(e.Base, OpenMode.ForRead);
            var c = (Surface)tr.GetObject(e.Comparacao, OpenMode.ForRead);
            rel.Base = b.Name;
            rel.Comparacao = c.Name;
            var nome = NomeLivre(tr, $"PRJ-VOL {b.Name} x {c.Name}");
            var volId = TinVolumeSurface.Create(nome, e.Base, e.Comparacao);
            var vol = (TinVolumeSurface)tr.GetObject(volId, OpenMode.ForRead);

            if (e.Poligonos.Count == 0)
            {
                var p = vol.GetVolumeProperties();
                rel.Areas.Add(new AreaVolume { Nome = "Sobreposição das superfícies", Corte = p.UnadjustedCutVolume, Aterro = p.UnadjustedFillVolume });
            }
            else
            {
                foreach (var pg in e.Poligonos)
                {
                    var pts = new Point3dCollection();
                    foreach (var (x, y) in pg.Vertices) pts.Add(new Point3d(x, y, 0));
                    pts.Add(new Point3d(pg.Vertices[0].X, pg.Vertices[0].Y, 0));
                    var av = new AreaVolume { Nome = pg.Nome, Area2D = Malha.AreaPoligono(pg.Vertices) };
                    try
                    {
                        var vi = vol.GetBoundedVolumes(pts);
                        av.Corte = vi.Cut;
                        av.Aterro = vi.Fill;
                    }
                    catch (System.Exception ex)
                    {
                        rel.Avisos.Add($"{pg.Nome}: o Civil 3D não calculou o volume ({ex.Message}). O polígono está dentro das duas superfícies?");
                        av.Corte = double.NaN; av.Aterro = double.NaN;
                    }
                    rel.Areas.Add(av);
                }
            }

            // malha para o mapa e as faixas
            double x0, y0, x1, y1;
            if (e.Poligonos.Count > 0)
            {
                var todos = e.Poligonos.SelectMany(p => p.Vertices).ToList();
                x0 = todos.Min(v => v.X); x1 = todos.Max(v => v.X); y0 = todos.Min(v => v.Y); y1 = todos.Max(v => v.Y);
            }
            else
            {
                var g = vol.GetGeneralProperties();
                x0 = g.MinimumCoordinateX; x1 = g.MaximumCoordinateX; y0 = g.MinimumCoordinateY; y1 = g.MaximumCoordinateY;
            }
            // passada grossa: acha onde as superfícies se sobrepõem (corredor estreito num retângulo grande) e a fina só amostra ali
            var ocup = Malha.Ocupar(x0, y0, x1, y1, Malha.TamanhoCelula((x1 - x0) * (y1 - y0), 20000), Dif);
            double areaRef = ocup.AreaEstimada * 1.3;
            if (e.Poligonos.Count > 0) areaRef = Math.Min(areaRef, e.Poligonos.Sum(p => Malha.AreaPoligono(p.Vertices)));
            areaRef = Math.Max(areaRef, 1);
            double cel = e.Celula > 0 ? e.Celula : Math.Min(Malha.TamanhoCelula(areaRef), ocup.Celula);
            while ((x1 - x0) / cel * ((y1 - y0) / cel) > 3_900_000) cel *= 2; // limite de memória da malha
            var caixas = e.Poligonos.Select(p => (P: p.Vertices, X0: p.Vertices.Min(v => v.X), X1: p.Vertices.Max(v => v.X), Y0: p.Vertices.Min(v => v.Y), Y1: p.Vertices.Max(v => v.Y))).ToList();
            Func<double, double, bool>? dentro = e.Poligonos.Count == 0 ? null
                : (x, y) => caixas.Any(k => x >= k.X0 && x <= k.X1 && y >= k.Y0 && y <= k.Y1 && Malha.Dentro(k.P, x, y));
            double Dif(double x, double y)
            {
                try { return vol.FindElevationAtXY(x, y); } catch { return double.NaN; }
            }
            var faixas = Malha.Faixas();
            malha = Malha.Calcular(x0, y0, x1, y1, cel, Dif, dentro, faixas, ocup);
            rel.Faixas = faixas;
            rel.Celula = cel;
            rel.CorteMalha = malha.Corte;
            rel.AterroMalha = malha.Aterro;
            if (e.Poligonos.Count == 0) rel.Areas[0].Area2D = malha.Area;

            if (e.ManterSuperficie) { tr.Commit(); mantida = nome; }
            else tr.Abort();
        }

        rel.Origem.Add(("Desenho", e.Desenho ?? ""));
        rel.Origem.Add(("Método", "Superfície de volume TIN do Civil 3D (comparação − base), volumes exatos por prisma; " +
                                  (e.Poligonos.Count == 0 ? "área = sobreposição das duas superfícies." : $"{e.Poligonos.Count} polígono(s) de cálculo.")));
        if (e.Poligonos.Count > 0)
            rel.Origem.Add(("Polígonos", string.Join("; ", e.Poligonos.Select(p => $"{p.Nome} [handle {p.Handle}]"))));
        rel.Origem.Add(("Superfície de volume", mantida is null ? "criada só para o cálculo e desfeita" : $"mantida no desenho: {mantida}"));
        rel.Origem.Add(("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm", Num.Br)));
        if (e.Poligonos.Count > 1)
        {
            double soma = e.Poligonos.Sum(p => Malha.AreaPoligono(p.Vertices));
            if (malha.Area > 0 && soma > malha.Area * 1.02 + 1)
                rel.Avisos.Add("Há polígonos sobrepostos ou fora das superfícies: o total soma áreas repetidas ou sem superfície.");
        }
        if (rel.DiferencaMalha() > 5)
            rel.Avisos.Add($"A malha de {rel.Celula.ToString("0.##", Num.Br)} m difere {rel.DiferencaMalha().ToString("0.0", Num.Br)} % do cálculo exato: o mapa e as faixas são aproximados (os volumes da aba Volumes são os exatos).");
        return new Saida { Relatorio = rel, Malha = malha, SuperficieVolume = mantida };
    }

    /// <summary>Entidades do mapa (camada PRJ-VOLUMES-MAPA) no espaço do modelo.</summary>
    public static List<ObjectId> EntidadesDoMapa(Transaction tr, Database db)
    {
        var l = new List<ObjectId>();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (!lt.Has(LayerMapa)) return l;
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
        foreach (ObjectId id in ms)
            if (tr.GetObject(id, OpenMode.ForRead) is Entity en && string.Equals(en.Layer, LayerMapa, StringComparison.OrdinalIgnoreCase)) l.Add(id);
        return l;
    }

    /// <summary>Desenha o mapa de corte/aterro (um sólido por trecho de mesma faixa) e a legenda; manda tudo para trás.</summary>
    public static int DesenharMapa(Transaction tr, Database db, ResultadoMalha m, RelatorioVolumes rel)
    {
        var layer = Ctx.GarantirLayer(tr, db, LayerMapa, 8);
        var ms = Ctx.EspacoModelo(tr, db);
        var ids = new ObjectIdCollection();
        static Color Cor(string rgb) => Color.FromRgb(Convert.ToByte(rgb[..2], 16), Convert.ToByte(rgb.Substring(2, 2), 16), Convert.ToByte(rgb.Substring(4, 2), 16));
        double cel = m.Celula;
        foreach (var (l, c0, c1, f) in Malha.Corridas(m.Faixa))
        {
            double xa = m.X0 + c0 * cel, xb = m.X0 + (c1 + 1) * cel, ya = m.Y0 + l * cel, yb = ya + cel;
            var s = new Solid(new Point3d(xa, ya, 0), new Point3d(xb, ya, 0), new Point3d(xa, yb, 0), new Point3d(xb, yb, 0))
            {
                Layer = layer,
                Color = Cor(rel.Faixas[f].Cor),
            };
            ids.Add(Ctx.Adicionar(tr, ms, s));
        }
        int solidos = ids.Count;

        // legenda à direita da malha
        double largura = m.Colunas * cel, altura = m.Linhas * cel;
        double h = Math.Clamp(Math.Max(largura, altura) / 70, 0.5, 25);
        double lx = m.X0 + largura + 3 * h, ly = m.Y0 + altura;
        var titulo = Ctx.Texto(Ctx.MTextLinhas($"Corte × aterro: {rel.Comparacao} − {rel.Base}", $"malha {rel.Celula.ToString("0.##", Num.Br)} m"), new Point3d(lx, ly, 0), h * 1.2, 0, AttachmentPoint.TopLeft);
        titulo.Layer = layer;
        titulo.ColorIndex = 7;
        ids.Add(Ctx.Adicionar(tr, ms, titulo));
        double y = ly - 5.6 * h; // abaixo das duas linhas do título (1,2 h com entrelinha)
        foreach (var fx in rel.Faixas)
        {
            var q = new Solid(new Point3d(lx, y, 0), new Point3d(lx + 2 * h, y, 0), new Point3d(lx, y + 1.4 * h, 0), new Point3d(lx + 2 * h, y + 1.4 * h, 0)) { Layer = layer, Color = Cor(fx.Cor) };
            ids.Add(Ctx.Adicionar(tr, ms, q));
            var t = Ctx.Texto($"{fx.Rotulo}: {Math.Round(fx.Area).ToString("#,##0", Num.Br)} m², {Math.Round(fx.Volume).ToString("#,##0", Num.Br)} m³",
                              new Point3d(lx + 2.6 * h, y + 0.7 * h, 0), h, 0, AttachmentPoint.MiddleLeft);
            t.Layer = layer;
            t.ColorIndex = 7;
            ids.Add(Ctx.Adicionar(tr, ms, t));
            y -= 2 * h;
        }

        try
        {
            var dot = (DrawOrderTable)tr.GetObject(ms.DrawOrderTableId, OpenMode.ForWrite);
            var paraTras = new ObjectIdCollection();
            for (int i = 0; i < solidos; i++) paraTras.Add(ids[i]);
            if (paraTras.Count > 0) dot.MoveToBottom(paraTras);
        }
        catch { }
        return solidos;
    }
}
