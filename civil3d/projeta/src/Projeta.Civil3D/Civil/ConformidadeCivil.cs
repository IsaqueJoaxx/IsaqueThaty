using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Superficies;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Projeto × obra: compara a superfície executada (levantamento da obra) com a de projeto. O desvio (obra − projeto) vem
/// da superfície de volume TIN do Civil 3D, criada numa transação própria e desfeita no fim; a malha classifica cada
/// célula pela tolerância, dá as estatísticas, o mapa de cores e os pontos críticos; com um alinhamento, faz o controle
/// por estaca em afastamentos fixos.
/// </summary>
internal static class ConformidadeCivil
{
    public const string LayerMapa = "PRJ-CONFORMIDADE-MAPA";

    public sealed class Entrada
    {
        public ObjectId Projeto { get; init; }
        public ObjectId Obra { get; init; }
        /// <summary>Áreas de análise; vazio = onde as duas superfícies se sobrepõem.</summary>
        public List<VolumesCivil.Poligono> Poligonos { get; init; } = new();
        public ToleranciaObra Tolerancia { get; init; } = new();
        /// <summary>Alinhamento do controle por estaca (Null = sem controle por estaca).</summary>
        public ObjectId Alinhamento { get; init; }
        public double EstacaInicial { get; init; } = double.NaN;
        public double EstacaFinal { get; init; } = double.NaN;
        /// <summary>Intervalo entre as estacas do controle (m).</summary>
        public double Intervalo { get; init; } = 20;
        /// <summary>Afastamentos do controle (m, negativo = esquerda); vazio = automático pela faixa coberta.</summary>
        public IReadOnlyList<double>? Afastamentos { get; init; }
        public int Pontos { get; init; } = 20;
        /// <summary>Lado da célula da malha (m); 0 = automático.</summary>
        public double Celula { get; init; }
        public string? Desenho { get; init; }
    }

    public sealed class Saida
    {
        public RelatorioConformidade Relatorio { get; init; } = null!;
        public ResultadoConformidade Malha { get; init; } = null!;
    }

    public static Saida Calcular(Database db, Entrada e)
    {
        if (e.Projeto == e.Obra) throw new InvalidOperationException("A superfície de projeto e a da obra são a mesma.");
        try { e.Tolerancia.Validar(); }
        catch (ArgumentException ex) { throw new InvalidOperationException(ex.Message); }
        if (!(e.Intervalo > 0)) throw new InvalidOperationException("O intervalo entre as estacas do controle deve ser positivo.");
        var rel = new RelatorioConformidade { Tolerancia = e.Tolerancia };
        ResultadoConformidade malha;
        string origemEstacas = "";

        // transação própria: a superfície de volume é só para o cálculo e some no Abort
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var proj = (Surface)tr.GetObject(e.Projeto, OpenMode.ForRead);
            var obra = (Surface)tr.GetObject(e.Obra, OpenMode.ForRead);
            rel.Projeto = proj.Name;
            rel.Obra = obra.Name;
            var volId = TinVolumeSurface.Create(VolumesCivil.NomeLivre(tr, $"PRJ-CONF {proj.Name} x {obra.Name}"), e.Projeto, e.Obra);
            var vol = (TinVolumeSurface)tr.GetObject(volId, OpenMode.ForRead);
            double Dif(double x, double y)
            {
                try { return vol.FindElevationAtXY(x, y); } catch { return double.NaN; }
            }

            // malha: mesmo esquema do PRJVOLUMES (passada grossa para achar a sobreposição, fina só ali)
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
            var ocup = Malha.Ocupar(x0, y0, x1, y1, Malha.TamanhoCelula((x1 - x0) * (y1 - y0), 20000), Dif);
            double areaRef = ocup.AreaEstimada * 1.3;
            if (e.Poligonos.Count > 0) areaRef = Math.Min(areaRef, e.Poligonos.Sum(p => Malha.AreaPoligono(p.Vertices)));
            areaRef = Math.Max(areaRef, 1);
            double cel = e.Celula > 0 ? e.Celula : Math.Min(Malha.TamanhoCelula(areaRef, 120000), ocup.Celula);
            while ((x1 - x0) / cel * ((y1 - y0) / cel) > 3_900_000) cel *= 2;
            var areas = new List<(string Nome, Func<double, double, bool> Dentro)>();
            foreach (var p in e.Poligonos)
            {
                var v = p.Vertices;
                double ax0 = v.Min(q => q.X), ax1 = v.Max(q => q.X), ay0 = v.Min(q => q.Y), ay1 = v.Max(q => q.Y);
                areas.Add((p.Nome, (x, y) => x >= ax0 && x <= ax1 && y >= ay0 && y <= ay1 && Malha.Dentro(v, x, y)));
            }
            var faixas = e.Tolerancia.Faixas();
            malha = Conformidade.Calcular(x0, y0, x1, y1, cel, Dif, areas, faixas, ocup);
            rel.Faixas = faixas;
            rel.Celula = cel;
            rel.Geral = malha.Geral;
            rel.Areas = malha.Areas;

            // volumes exatos acima/abaixo do projeto (Fill = obra acima do projeto; Cut = obra abaixo)
            bool exatos = true;
            if (e.Poligonos.Count == 0)
            {
                try
                {
                    var vp = vol.GetVolumeProperties();
                    malha.Geral.VolumeAcima = vp.UnadjustedFillVolume;
                    malha.Geral.VolumeAbaixo = vp.UnadjustedCutVolume;
                }
                catch (System.Exception ex) { exatos = false; rel.Avisos.Add($"Volumes pela malha: o Civil 3D não calculou o volume exato ({ex.Message})."); }
            }
            else
            {
                for (int i = 0; i < e.Poligonos.Count; i++)
                {
                    var pg = e.Poligonos[i];
                    var pts = new Point3dCollection();
                    foreach (var (x, y) in pg.Vertices) pts.Add(new Point3d(x, y, 0));
                    pts.Add(new Point3d(pg.Vertices[0].X, pg.Vertices[0].Y, 0));
                    try
                    {
                        var vi = vol.GetBoundedVolumes(pts);
                        malha.Areas[i].VolumeAcima = vi.Fill;
                        malha.Areas[i].VolumeAbaixo = vi.Cut;
                    }
                    catch (System.Exception ex)
                    {
                        exatos = false;
                        rel.Avisos.Add($"{pg.Nome}: volumes pela malha; o Civil 3D não calculou o volume exato ({ex.Message}).");
                    }
                }
                malha.Geral.VolumeAcima = malha.Areas.Sum(a => a.VolumeAcima);
                malha.Geral.VolumeAbaixo = malha.Areas.Sum(a => a.VolumeAbaixo);
            }
            rel.VolumesExatos = exatos;

            // pontos críticos com cotas, estaca e afastamento
            Alignment? al = e.Alinhamento.IsNull ? null : (Alignment)tr.GetObject(e.Alinhamento, OpenMode.ForRead);
            if (e.Pontos > 0)
            {
                var crit = Conformidade.Criticos(malha, e.Tolerancia, e.Pontos, Math.Max(10, 10 * cel));
                foreach (var q in crit)
                {
                    q.CotaProjeto = CivilUtil.Cota(proj, q.X, q.Y);
                    q.CotaObra = CivilUtil.Cota(obra, q.X, q.Y);
                    if (!double.IsNaN(q.CotaProjeto) && !double.IsNaN(q.CotaObra))
                    {
                        q.Desvio = q.CotaObra - q.CotaProjeto;
                        q.Excesso = e.Tolerancia.Excesso(q.Desvio);
                        q.Faixa = ToleranciaObra.Indice(faixas, q.Desvio);
                    }
                    if (al is not null && CivilUtil.EstacaAfastamento(al, q.X, q.Y, out var st, out var off)) { q.Estaca = st; q.Afastamento = off; }
                }
                rel.Criticos.AddRange(crit);
            }

            // controle por estaca
            if (al is not null)
            {
                double ini = double.IsNaN(e.EstacaInicial) ? al.StartingStation : Math.Max(al.StartingStation, e.EstacaInicial);
                double fim = double.IsNaN(e.EstacaFinal) ? al.EndingStation : Math.Min(al.EndingStation, e.EstacaFinal);
                if (!(fim > ini)) rel.Avisos.Add($"Controle por estaca: faixa de estacas vazia no alinhamento {al.Name}.");
                else
                {
                    var estacas = Conformidade.Estacas(ini, fim, e.Intervalo);
                    Alignment eixo = al;
                    double DifEm(double st, double off) => CivilUtil.PontoNaEstaca(eixo, st, off, out var x, out var y) ? Dif(x, y) : double.NaN;
                    double[] afs;
                    bool auto = e.Afastamentos is not { Count: > 0 };
                    if (!auto) afs = e.Afastamentos!.Distinct().OrderBy(x => x).ToArray();
                    else
                    {
                        // varre até 30 estacas, de metro em metro, para achar a faixa de afastamentos coberta pelas duas superfícies
                        int lim = (int)Math.Ceiling(Math.Min(300, Math.Max(x1 - x0, y1 - y0)));
                        var amostra = estacas.Count <= 30 ? estacas
                            : Enumerable.Range(0, 30).Select(i => estacas[(int)Math.Round(i * (estacas.Count - 1) / 29.0)]).Distinct().ToList();
                        double mn = double.NaN, mx = double.NaN;
                        foreach (var st in amostra)
                            for (int k = -lim; k <= lim; k++)
                            {
                                if (double.IsNaN(DifEm(st, k))) continue;
                                if (double.IsNaN(mn) || k < mn) mn = k;
                                if (double.IsNaN(mx) || k > mx) mx = k;
                            }
                        afs = double.IsNaN(mn) ? Array.Empty<double>() : Conformidade.Afastamentos(mn, mx);
                    }
                    var ce = new ControleEstacas { Alinhamento = al.Name, Afastamentos = afs };
                    int vazias = 0;
                    if (afs.Length > 0)
                        foreach (var st in estacas)
                        {
                            var ds = afs.Select(off => DifEm(st, off)).ToArray();
                            if (ds.All(double.IsNaN)) { vazias++; continue; }
                            ce.Linhas.Add(new LinhaControle { Estaca = st, Desvios = ds });
                        }
                    if (ce.Linhas.Count == 0)
                        rel.Avisos.Add($"Controle por estaca: as duas superfícies não se sobrepõem ao longo do alinhamento {al.Name}" +
                                       (auto ? " (até 300 m para cada lado)." : " nos afastamentos pedidos."));
                    rel.Estacas = ce;
                    origemEstacas = $"alinhamento {al.Name}, estacas {Est(ini)} a {Est(fim)} a cada {F(e.Intervalo)} m; " +
                                    $"afastamentos {(auto ? "automáticos" : "pedidos")}: {string.Join("; ", afs.Select(x => F(x)))} m; " +
                                    $"{ce.Linhas.Count} estaca(s) com dados" + (vazias > 0 ? $", {vazias} sem as duas superfícies (fora da planilha)" : "");
                }
            }
            tr.Abort();
        }

        rel.Origem.Add(("Desenho", e.Desenho ?? ""));
        rel.Origem.Add(("Método", "Superfície de volume TIN do Civil 3D (obra - projeto), criada só para o cálculo e desfeita; desvio amostrado no centro de cada célula da malha" +
                                  (rel.VolumesExatos ? "; volumes acima/abaixo do projeto pelo cálculo exato por prisma." : "; volumes pela malha.")));
        rel.Origem.Add(("Área de análise", e.Poligonos.Count == 0 ? "sobreposição das duas superfícies"
            : string.Join("; ", e.Poligonos.Select(p => $"{p.Nome} [handle {p.Handle}]"))));
        if (origemEstacas.Length > 0) rel.Origem.Add(("Controle por estaca", origemEstacas));
        if (rel.Criticos.Count > 0)
            rel.Origem.Add(("Pontos críticos", $"maior excesso sobre a tolerância em cada região, até {e.Pontos} pontos afastados pelo menos {F(Math.Max(10, 10 * rel.Celula))} m entre si; cotas lidas nas duas superfícies."));
        rel.Origem.Add(("Gerado em", DateTime.Now.ToString("dd/MM/yyyy HH:mm", Num.Br)));
        if (!(rel.Geral.Area > 0))
            rel.Avisos.Add(e.Poligonos.Count == 0 ? "As duas superfícies não se sobrepõem: não há o que comparar." : "Os polígonos não pegam a sobreposição das duas superfícies.");
        if (e.Poligonos.Count > 1)
        {
            double soma = rel.Areas.Sum(a => a.Area);
            if (rel.Geral.Area > 0 && soma > rel.Geral.Area * 1.02 + 1)
                rel.Avisos.Add("Há polígonos sobrepostos: o total soma volumes repetidos (as porcentagens do total contam cada célula uma vez).");
        }
        return new Saida { Relatorio = rel, Malha = malha };
    }

    /// <summary>Entidades do mapa (camada PRJ-CONFORMIDADE-MAPA) no espaço do modelo.</summary>
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

    /// <summary>
    /// Desenha o mapa de desvios (um sólido por trecho de mesma faixa, mandado para trás), a legenda e os pontos críticos
    /// (círculo e rótulo). Devolve o número de sólidos.
    /// </summary>
    public static int DesenharMapa(Transaction tr, Database db, ResultadoConformidade m, RelatorioConformidade rel)
    {
        var layer = Ctx.GarantirLayer(tr, db, LayerMapa, 8);
        var ms = Ctx.EspacoModelo(tr, db);
        var fundo = new ObjectIdCollection();
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
            fundo.Add(Ctx.Adicionar(tr, ms, s));
        }
        int solidos = fundo.Count;

        double largura = m.Colunas * cel, altura = m.Linhas * cel;
        double h = Math.Clamp(Math.Max(largura, altura) / 70, 0.5, 25);

        // legenda à direita da malha
        double lx = m.X0 + largura + 3 * h, ly = m.Y0 + altura;
        var g = rel.Geral;
        var titulo = Ctx.Texto(Ctx.MTextLinhas($"Projeto x obra: {rel.Obra} - {rel.Projeto}",
                                               $"tolerância {rel.Tolerancia.Descricao} · malha {F(rel.Celula)} m",
                                               $"dentro da tolerância: {P(g.PctDentro)} % da área - {rel.Situacao}"),
                               new Point3d(lx, ly, 0), h * 1.2, 0, AttachmentPoint.TopLeft);
        titulo.Layer = layer;
        titulo.ColorIndex = 7;
        Ctx.Adicionar(tr, ms, titulo);
        double y = ly - 7.6 * h; // abaixo das três linhas do título (1,2 h com entrelinha)
        double areaTot = rel.Faixas.Sum(x => x.Area);
        foreach (var fx in rel.Faixas)
        {
            var q = new Solid(new Point3d(lx, y, 0), new Point3d(lx + 2 * h, y, 0), new Point3d(lx, y + 1.4 * h, 0), new Point3d(lx + 2 * h, y + 1.4 * h, 0)) { Layer = layer, Color = Cor(fx.Cor) };
            Ctx.Adicionar(tr, ms, q);
            var t = Ctx.Texto($"{fx.RotuloCurto}: {Math.Round(fx.Area).ToString("#,##0", Num.Br)} m² ({P(areaTot > 0 ? fx.Area / areaTot * 100 : double.NaN)} %)",
                              new Point3d(lx + 2.6 * h, y + 0.7 * h, 0), h, 0, AttachmentPoint.MiddleLeft);
            t.Layer = layer;
            t.ColorIndex = 7;
            Ctx.Adicionar(tr, ms, t);
            y -= 2 * h;
        }

        // pontos críticos por cima do mapa
        double raio = Math.Max(2 * cel, 0.6 * h);
        foreach (var p in rel.Criticos)
        {
            short aci = p.Desvio > 0 ? (short)1 : (short)5;
            var c = new Circle(new Point3d(p.X, p.Y, 0), Vector3d.ZAxis, raio) { Layer = layer, ColorIndex = aci };
            Ctx.Adicionar(tr, ms, c);
            var t = Ctx.Texto($"P{p.Numero} {p.Desvio.ToString("+0.000;-0.000;0.000", Num.Br)}", new Point3d(p.X + raio * 1.2, p.Y + raio * 1.2, 0), h * 0.7, 0, AttachmentPoint.BottomLeft);
            t.Layer = layer;
            t.ColorIndex = aci;
            Ctx.Adicionar(tr, ms, t);
        }

        try
        {
            var dot = (DrawOrderTable)tr.GetObject(ms.DrawOrderTableId, OpenMode.ForWrite);
            if (fundo.Count > 0) dot.MoveToBottom(fundo);
        }
        catch { }
        return solidos;
    }

    private static string F(double v) => v.ToString("0.###", Num.Br);
    private static string P(double v) => double.IsNaN(v) ? "-" : v.ToString("0.0", Num.Br);
    private static string Est(double v) => Projeta.Core.Pavimentacao.NotaServico.FormatarEstaca(v);
}
