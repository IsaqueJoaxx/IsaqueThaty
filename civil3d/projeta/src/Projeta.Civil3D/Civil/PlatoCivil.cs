using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Superficies;
using Projeta.Core.Terraplenagem;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using SurfaceBoundaryType = Autodesk.Civil.SurfaceBoundaryType;
using StyleBase = Autodesk.Civil.DatabaseServices.Styles.StyleBase;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Platôs: em cada polígono fechado, um piso plano (cota que compensa corte e aterro, ou fixa) com caimento descendo como
/// o terreno, e taludes até o terreno. Cria a superfície de cada platô (pontos do talude, borda do piso e encontro com o
/// terreno como linhas de quebra) e uma superfície final (terreno + platôs), desenha crista do corte, pé do aterro,
/// tracinhos e cotas, e calcula os volumes exatos pelo mesmo caminho do PRJVOLUMES.
/// </summary>
internal static class PlatoCivil
{
    public const string LPiso = "PRJ-PLATO-PISO", LCrista = "PRJ-PLATO-CRISTA", LPe = "PRJ-PLATO-PE", LTalude = "PRJ-PLATO-TALUDE",
                        LTexto = "PRJ-PLATO-TEXTO", LBreak = "PRJ-PLATO-BREAK", LSup = "PRJ-PLATO-SUPERFICIE";

    /// <summary>Polígono de um platô; <paramref name="CotaFixa"/> NaN = cota que compensa corte e aterro.</summary>
    public sealed record Poligono(string Nome, List<(double X, double Y)> Vertices, string Handle, double CotaFixa);

    public sealed class Entrada
    {
        public ObjectId Terreno { get; init; }
        public List<Poligono> Poligonos { get; init; } = new();
        public ParametrosPlato Parametros { get; init; } = new();
        public string? Desenho { get; init; }
    }

    public sealed class Saida
    {
        public RelatorioPlatos Relatorio { get; init; } = null!;
        public VolumesCivil.Saida? Volumes { get; set; }
        public string SuperficieFinal { get; set; } = "";
        public List<string> SuperficiesPlato { get; } = new();
    }

    /// <summary>Faz tudo dentro de <paramref name="tr"/>; quem chama confirma (U desfaz o comando inteiro).</summary>
    public static Saida Executar(Transaction tr, Database db, Entrada e)
    {
        try { e.Parametros.Validar(); }
        catch (ArgumentException ex) { throw new InvalidOperationException(ex.Message); }
        if (e.Poligonos.Count == 0) throw new InvalidOperationException("Nenhum polígono de platô.");
        var P = e.Parametros;
        var tn = (Surface)tr.GetObject(e.Terreno, OpenMode.ForRead);
        double T(double x, double y) => CivilUtil.Cota(tn, x, y);
        var rel = new RelatorioPlatos { Terreno = tn.Name, Parametros = P };
        var saida = new Saida { Relatorio = rel };
        var civ = CivilApplication.ActiveDocument;
        var ms = Ctx.EspacoModelo(tr, db);
        string lPiso = Ctx.GarantirLayer(tr, db, LPiso, 4, LineWeight.LineWeight050), lCrista = Ctx.GarantirLayer(tr, db, LCrista, 1, LineWeight.LineWeight035),
               lPe = Ctx.GarantirLayer(tr, db, LPe, 3, LineWeight.LineWeight035), lTal = Ctx.GarantirLayer(tr, db, LTalude, 8),
               lTxt = Ctx.GarantirLayer(tr, db, LTexto, 7), lBrk = Ctx.GarantirLayer(tr, db, LBreak, 9), lSup = Ctx.GarantirLayer(tr, db, LSup, 8);
        ObjectId estiloInvisivel = ObjectId.Null;
        foreach (ObjectId id in civ.Styles.SurfaceStyles)
        {
            var nome = ((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name;
            if (nome.Contains("INVIS", StringComparison.OrdinalIgnoreCase) || nome.Contains("No Display", StringComparison.OrdinalIgnoreCase)) { estiloInvisivel = id; break; }
        }

        var projetos = new List<ObjectId>();
        var encontros = new List<VolumesCivil.Poligono>();
        foreach (var pg in e.Poligonos)
        {
            Poligono2D pol;
            try { pol = new Poligono2D(pg.Vertices); }
            catch (ArgumentException ex) { throw new InvalidOperationException($"{pg.Nome}: {ex.Message}"); }
            double cel = P.Celula > 0 ? P.Celula : ParametrosPlato.CelulaPara(pol.Area);
            double margem = 25;
            CampoPlato campo; double z0; ResumoMalhaPlato r;
            while (true)
            {
                try { campo = new CampoPlato(pol, T, P, cel, margem); }
                catch (InvalidOperationException ex) { throw new InvalidOperationException($"{pg.Nome}: {ex.Message}"); }
                try { z0 = double.IsNaN(pg.CotaFixa) ? campo.Compensar() : pg.CotaFixa; }
                catch (InvalidOperationException ex) { throw new InvalidOperationException($"{pg.Nome}: {ex.Message}"); }
                r = campo.Volumes(z0);
                if (!r.TocaBorda || margem >= 400) break;
                margem *= 2;
            }
            if (r.TocaBorda) rel.Avisos.Add($"{pg.Nome}: o talude passa de 400 m sem encontrar o terreno; o limite foi cortado.");
            if (campo.NosPisoSemTerreno > 0) rel.Avisos.Add($"{pg.Nome}: parte do piso está fora da superfície do terreno; essa parte não entra nos volumes.");

            var lacos = campo.Encontro(z0);
            if (lacos.Count == 0) throw new InvalidOperationException($"{pg.Nome}: não achei o encontro dos taludes com o terreno.");
            var limite = lacos[0];

            // linhas de quebra 3D: borda do piso e encontro com o terreno
            var crista = new Point3dCollection();
            foreach (var (x, y, _) in pol.Densificar(Math.Max(cel, 1.0))) crista.Add(new Point3d(x, y, campo.Piso(z0, x, y)));
            var day = new Point3dCollection();
            var classe = new List<(double X, double Y, bool Corte, double H)>();
            int semCota = 0;
            foreach (var (x, y) in limite)
            {
                double t = T(x, y);
                if (double.IsNaN(t)) { semCota++; continue; }
                var p3 = new Point3d(x, y, t);
                if (day.Count > 0 && day[day.Count - 1].DistanceTo(p3) < 1e-4) continue;
                day.Add(p3);
                var (corte, alt) = campo.Talude(z0, x, y, t);
                classe.Add((x, y, corte, alt));
            }
            if (semCota > 0) rel.Avisos.Add($"{pg.Nome}: {semCota} ponto(s) do encontro com o terreno caíram fora da superfície e foram ignorados.");
            if (day.Count < 3) throw new InvalidOperationException($"{pg.Nome}: o encontro com o terreno ficou fora da superfície.");
            var idCrista = Ctx.Adicionar(tr, ms, new Polyline3d(Poly3dType.SimplePoly, crista, true) { Layer = lBrk });
            var idDay = Ctx.Adicionar(tr, ms, new Polyline3d(Poly3dType.SimplePoly, day, true) { Layer = lBrk });
            var ilhas = new ObjectIdCollection();
            double areaIlhas = 0;
            foreach (var ilha in lacos.Skip(1))
            {
                double a = Math.Abs(Poligono2D.AreaDe(ilha));
                if (a < 4 * cel * cel || pol.Dentro(ilha[0].X, ilha[0].Y)) continue;
                var pi = new Point3dCollection();
                foreach (var (x, y) in ilha) { double t = T(x, y); if (!double.IsNaN(t)) pi.Add(new Point3d(x, y, t)); }
                if (pi.Count < 3) continue;
                ilhas.Add(Ctx.Adicionar(tr, ms, new Polyline3d(Poly3dType.SimplePoly, pi, true) { Layer = lBrk }));
                areaIlhas += a;
            }

            // superfície do platô
            var nomeSup = VolumesCivil.NomeLivre(tr, $"PRJ {pg.Nome} - projeto");
            var sid = TinSurface.Create(db, nomeSup);
            var sup = (TinSurface)tr.GetObject(sid, OpenMode.ForWrite);
            sup.Layer = lSup;
            var pts = new Point3dCollection();
            foreach (var (x, y, z) in campo.PontosTalude(z0)) pts.Add(new Point3d(x, y, z));
            if (pts.Count > 0) sup.AddVertices(pts);
            sup.BreaklinesDefinition.AddStandardBreaklines(new ObjectIdCollection { idCrista, idDay }, 0.1, 0, 0, 0);
            sup.BoundariesDefinition.AddBoundaries(new ObjectIdCollection { idDay }, 0.1, SurfaceBoundaryType.Outer, true);
            if (ilhas.Count > 0) sup.BoundariesDefinition.AddBoundaries(ilhas, 0.1, SurfaceBoundaryType.Hide, true);
            sup.Rebuild();
            if (!estiloInvisivel.IsNull) sup.StyleId = estiloInvisivel;
            projetos.Add(sid);
            saida.SuperficiesPlato.Add(nomeSup);

            // desenho: piso, crista do corte, pé do aterro, tracinhos
            var piso = new Polyline { Layer = lPiso, Closed = true };
            foreach (var (x, y) in pol.V) piso.AddVertexAt(piso.NumberOfVertices, new Point2d(x, y), 0, 0, 0);
            Ctx.Adicionar(tr, ms, piso);
            double extC = 0, extP = 0, hC = 0, hA = 0;
            for (int i = 0; i < classe.Count; i++)
            {
                var a = classe[i]; var b = classe[(i + 1) % classe.Count];
                double l = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                if (a.Corte == b.Corte) { if (a.Corte) extC += l; else extP += l; }
                else { extC += l / 2; extP += l / 2; }
                if (a.Corte) hC = Math.Max(hC, a.H); else hA = Math.Max(hA, a.H);
            }
            int ini = Enumerable.Range(0, classe.Count).FirstOrDefault(i => classe[i].Corte != classe[(i + classe.Count - 1) % classe.Count].Corte);
            for (int c = 0; c < classe.Count;)
            {
                int i0 = (ini + c) % classe.Count;
                bool corte = classe[i0].Corte;
                var pl = new Polyline { Layer = corte ? lCrista : lPe };
                int k = 0;
                while (c + k < classe.Count && classe[(ini + c + k) % classe.Count].Corte == corte)
                {
                    var q = classe[(ini + c + k) % classe.Count];
                    pl.AddVertexAt(pl.NumberOfVertices, new Point2d(q.X, q.Y), 0, 0, 0);
                    k++;
                }
                var prox = classe[(ini + c + k) % classe.Count];
                pl.AddVertexAt(pl.NumberOfVertices, new Point2d(prox.X, prox.Y), 0, 0, 0);
                if (k == classe.Count) pl.Closed = true;
                if (pl.NumberOfVertices >= 2) Ctx.Adicionar(tr, ms, pl);
                c += k;
            }
            foreach (var (x0, y0, x1, y1, _) in campo.Tracos(z0, Math.Max(2, 2 * cel), Math.Max(2, 2 * cel)))
                Ctx.Adicionar(tr, ms, new Line(new Point3d(x0, y0, 0), new Point3d(x1, y1, 0)) { Layer = lTal });

            // textos: nome e cota no meio do piso, cota em cada vértice, seta do caimento
            double h = Math.Clamp(Math.Sqrt(pol.Area) / 40, 0.6, 4);
            bool comp = double.IsNaN(pg.CotaFixa);
            bool temCaimento = campo.Dx != 0 || campo.Dy != 0;
            double zmin = double.PositiveInfinity, zmax = double.NegativeInfinity;
            int nv = pol.V.Count;
            for (int i = 0; i < nv; i++)
            {
                var (x, y) = pol.V[i];
                double z = campo.Piso(z0, x, y);
                zmin = Math.Min(zmin, z); zmax = Math.Max(zmax, z);
                // rótulo na bissetriz interna do vértice, longe o bastante dos dois lados (também no canto reentrante)
                var (xa, ya) = pol.V[(i + nv - 1) % nv]; var (xb, yb) = pol.V[(i + 1) % nv];
                double e1x = x - xa, e1y = y - ya, l1 = Math.Sqrt(e1x * e1x + e1y * e1y), e2x = xb - x, e2y = yb - y, l2 = Math.Sqrt(e2x * e2x + e2y * e2y);
                if (l1 < 1e-9 || l2 < 1e-9) continue;
                double n1x = -e1y / l1, n1y = e1x / l1, n2x = -e2y / l2, n2y = e2x / l2; // normais internas (anti-horário)
                double bx = n1x + n2x, by = n1y + n2y, bl = Math.Sqrt(bx * bx + by * by);
                if (bl < 1e-6) { bx = n1x; by = n1y; } else { bx /= bl; by /= bl; }
                double dist = Math.Min(10 * h, 3.0 * h / Math.Max(0.25, bx * n1x + by * n1y)); // texto horizontal: folga para lados inclinados
                var tv = Ctx.Texto(N2(z), new Point3d(x + bx * dist, y + by * dist, 0), h, 0, AttachmentPoint.MiddleCenter);
                tv.Layer = lPiso;
                Ctx.Adicionar(tr, ms, tv);
            }
            var (px, py, _) = pol.Polo();
            bool noCentro = Math.Abs(px - pol.Cx) + Math.Abs(py - pol.Cy) < 1e-6;
            string l2txt = (noCentro ? $"piso {N2(z0)} no centro" : $"piso de {N2(zmin)} a {N2(zmax)}") + (comp ? " (compensada)" : "");
            string l3txt = temCaimento ? $"i = {N2(P.Caimento * 100)} %" : "piso em nível";
            var cent = Ctx.Texto(Ctx.MTextLinhas(pg.Nome, l2txt, l3txt), new Point3d(px, py, 0), h * 1.3, 0, AttachmentPoint.MiddleCenter);
            cent.Layer = lTxt;
            Ctx.Adicionar(tr, ms, cent);
            if (temCaimento)
            {
                // a seta sai da caixa do texto, no sentido em que o piso cai, e fica dentro do piso
                double ux = -campo.Dx, uy = -campo.Dy;
                double hw = 0.5 * new[] { pg.Nome.Length, l2txt.Length, l3txt.Length }.Max() * 0.55 * 1.3 * h, hh = 0.5 * 3 * 1.3 * h * 1.25;
                double t0 = Math.Min(Math.Abs(ux) > 1e-9 ? hw / Math.Abs(ux) : double.PositiveInfinity, Math.Abs(uy) > 1e-9 ? hh / Math.Abs(uy) : double.PositiveInfinity) + 0.8 * h;
                double sx = px + ux * t0, sy = py + uy * t0;
                double comp2 = Math.Min(15, 0.25 * Math.Sqrt(pol.Area));
                while (comp2 > 2 * h && !pol.Dentro(sx + ux * (comp2 + 1.2 * h), sy + uy * (comp2 + 1.2 * h))) comp2 *= 0.8;
                if (pol.Dentro(sx, sy) && pol.Dentro(sx + ux * (comp2 + 1.2 * h), sy + uy * (comp2 + 1.2 * h)))
                {
                    double ex = sx + ux * comp2, ey = sy + uy * comp2;
                    Ctx.Adicionar(tr, ms, new Line(new Point3d(sx, sy, 0), new Point3d(ex, ey, 0)) { Layer = lPiso });
                    Ctx.Adicionar(tr, ms, new Solid(new Point3d(ex + ux * 1.2 * h, ey + uy * 1.2 * h, 0),
                                                    new Point3d(ex - uy * 0.5 * h, ey + ux * 0.5 * h, 0),
                                                    new Point3d(ex + uy * 0.5 * h, ey - ux * 0.5 * h, 0)) { Layer = lPiso });
                }
            }

            var res = new ResultadoPlato
            {
                Nome = pg.Nome, Compensada = comp, Cota = z0, CotaMin = zmin, CotaMax = zmax,
                Caimento = temCaimento ? P.Caimento : 0, Azimute = campo.AzimuteCaimento,
                AreaPiso = pol.Area, AreaTerraplenagem = Math.Abs(Poligono2D.AreaDe(limite)) - areaIlhas,
                AreaTaludeCorte = r.AreaTaludeCorte, AreaTaludeAterro = r.AreaTaludeAterro,
                AlturaCorte = hC, AlturaAterro = hA, ExtensaoCrista = extC, ExtensaoPe = extP,
                CorteMalha = r.Corte, AterroMalha = r.Aterro, Handle = pg.Handle,
            };
            rel.Platos.Add(res);
            encontros.Add(new VolumesCivil.Poligono(pg.Nome, limite, pg.Handle));
        }

        // taludes de platôs vizinhos que se cruzam
        for (int i = 0; i < encontros.Count; i++)
            for (int j = i + 1; j < encontros.Count; j++)
                if (encontros[j].Vertices.Any(v => Malha.Dentro(encontros[i].Vertices, v.X, v.Y)) || encontros[i].Vertices.Any(v => Malha.Dentro(encontros[j].Vertices, v.X, v.Y)))
                    rel.Avisos.Add($"Os taludes de {encontros[i].Nome} e {encontros[j].Nome} se cruzam: na superfície final vale o último, e os volumes dos dois contam a área comum.");

        // superfície final = terreno + platôs
        var nomeFinal = VolumesCivil.NomeLivre(tr, $"PRJ-PLATOS FINAL ({tn.Name})");
        var fid = TinSurface.Create(db, nomeFinal);
        var fin = (TinSurface)tr.GetObject(fid, OpenMode.ForWrite);
        fin.Layer = lSup;
        fin.PasteSurface(e.Terreno);
        foreach (var id in projetos) fin.PasteSurface(id);
        fin.Rebuild();
        try { fin.StyleId = tn.StyleId; } catch { }
        saida.SuperficieFinal = nomeFinal;

        // volumes exatos (superfície de volume TIN) dentro de cada área de terraplenagem
        try
        {
            var sv = VolumesCivil.Calcular(db, new VolumesCivil.Entrada
            {
                Base = e.Terreno, Comparacao = fid, Poligonos = encontros, FatorCorte = 1, FatorAterro = P.Fh, Desenho = e.Desenho,
            });
            for (int i = 0; i < rel.Platos.Count && i < sv.Relatorio.Areas.Count; i++)
                if (!double.IsNaN(sv.Relatorio.Areas[i].Corte))
                {
                    rel.Platos[i].Corte = sv.Relatorio.Areas[i].Corte;
                    rel.Platos[i].Aterro = sv.Relatorio.Areas[i].Aterro;
                }
            foreach (var a in sv.Relatorio.Avisos) rel.Avisos.Add(a);
            saida.Volumes = sv;
        }
        catch (System.Exception ex) { rel.Avisos.Add($"Volumes exatos não calculados ({ex.Message}); valem os da malha."); }
        return saida;
    }

    private static string N2(double v) => v.ToString("0.00", Num.Br);
}
