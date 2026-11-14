using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJVIAS — vias do loteamento em lote: das polilinhas dos eixos das ruas (ou de alinhamentos já feitos) saem, de uma vez,
/// os alinhamentos com curva nos vértices, o perfil do terreno, um greide proposto que acompanha o terreno e passa na mesma
/// cota nos encontros entre as ruas, as vistas de perfil empilhadas, a montagem e o corredor (com superfície de topo), as
/// linhas de amostragem e o relatório em Excel. As polilinhas originais não são alteradas; as esquinas ficam para o
/// PRJINTERSECAO (cada encontro sai marcado no desenho).
/// </summary>
public sealed class CmdVias
{
    internal const string LayerVias = "PRJ-VIAS";
    internal const string LayerEncontros = "PRJ-VIAS-ENCONTROS";

    private sealed class Rua
    {
        public ObjectId Id;
        public string Nome = "";
        public string Origem = "";
        public double S0, S1;
        public int Curvas;
        public double? RaioMin;
        public List<(double X, double Y, double S)> Pontos = new();
        public List<(double S, double Z)> Terreno = new();
        public List<(double S, double Z, string Obs)> Fixos = new();
        public List<PivVia> Pivs = new();
        public ObjectId Greide = ObjectId.Null;
        public string GreideNome = "sem greide";
        public string Corredor = "";
    }

    [CommandMethod("PRJVIAS", CommandFlags.Modal)]
    public static void Vias()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            while (true)
            {
                Ctx.Msg(Resumo(cfg));
                var pk = new PromptKeywordOptions("\nVias do loteamento") { AllowNone = true };
                pk.Keywords.Add("Selecionar");
                pk.Keywords.Add("Parametros");
                pk.Keywords.Default = "Selecionar";
                var rk = ed.GetKeywords(pk);
                if (rk.Status == PromptStatus.Cancel) return;
                if (rk.Status == PromptStatus.OK && rk.StringResult == "Parametros") { if (!PedirParametros(ed, cfg)) return; continue; }
                break;
            }
            var filtro = new SelectionFilter(new[]
            {
                new TypedValue(-4, "<OR"), new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT"), new TypedValue(-4, "OR>"),
            });
            var ps = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione as polilinhas dos eixos das ruas (ou alinhamentos já feitos): " }, filtro);
            if (ps.Status != PromptStatus.OK) return;

            using var s = Sessao.Abrir();
            var civ = CivilApplication.ActiveDocument;
            var avisos = new List<string>();

            // superfície do terreno
            var sups = CorredorProjeta.SuperficiesDeTerreno(s.Tr);
            ObjectId supId = ObjectId.Null;
            string supNome = "";
            if (sups.Count == 0) avisos.Add("Não há superfície no desenho: as ruas ficam só com o eixo (sem perfil do terreno, greide e corredor).");
            else
            {
                int k = sups.Count == 1 ? 0 : s.EscolherDaLista("Superfície do terreno", sups.Select(x => x.Nome).ToList(), 0);
                if (k < 0) return;
                (supId, supNome) = sups[k];
            }
            var sup = supId.IsNull ? null : (Surface)s.Tr.GetObject(supId, OpenMode.ForRead);

            string modoGreide = "Nenhum";
            bool vistas = false, amostragem = false;
            Point3d? origemVistas = null;
            if (sup is not null)
            {
                var r = s.PedirPalavra("Greide de cada rua", new[] { "Proposto", "Nenhum" }, "Proposto");
                if (r is null) return;
                modoGreide = r;
                r = s.PedirPalavra("Vistas de perfil", new[] { "Sim", "Nao" }, "Sim");
                if (r is null) return;
                vistas = r == "Sim";
                if (vistas)
                {
                    var pp = ed.GetPoint(new PromptPointOptions("\nCanto inferior esquerdo da primeira vista de perfil <à direita das ruas>: ") { AllowNone = true });
                    if (pp.Status == PromptStatus.Cancel) return;
                    if (pp.Status == PromptStatus.OK) origemVistas = pp.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                }
            }

            // ruas: alinhamentos novos (polilinhas com curva nos vértices) ou os que já existem
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            string layV = Ctx.GarantirLayer(s.Tr, s.Db, LayerVias, 1);
            var layVId = ((LayerTable)s.Tr.GetObject(s.Db.LayerTableId, OpenMode.ForRead))[layV];
            var estiloAl = Estilo(s.Tr, civ.Styles.AlignmentStyles, "Proposed", "Projeto", "Eixo");
            var rotAl = Estilo(s.Tr, civ.Styles.LabelSetStyles.AlignmentLabelSetStyles, "Major and Minor", "Estaca", "Standard");
            var fontes = new List<(ObjectId Id, double Comp)>();
            foreach (var id in ps.Value.GetObjectIds())
            {
                var e = s.Tr.GetObject(id, OpenMode.ForRead);
                double comp = e switch { Polyline pl => pl.Length, Line ln => ln.Length, Alignment al => al.Length, _ => 0 };
                if (comp > 1) fontes.Add((id, comp));
            }
            if (fontes.Count == 0) { Ctx.Msg("Nenhuma polilinha, linha ou alinhamento com mais de 1 m na seleção."); return; }
            var ruas = new List<Rua>();
            foreach (var (id, _) in fontes.OrderByDescending(f => f.Comp))
            {
                var e = s.Tr.GetObject(id, OpenMode.ForRead);
                if (e is Alignment alx)
                {
                    ruas.Add(new Rua { Id = alx.ObjectId, Nome = alx.Name, Origem = "alinhamento existente" });
                    continue;
                }
                var vs = new List<VerticeVia>();
                if (e is Polyline pl)
                {
                    for (int i = 0; i < pl.NumberOfVertices; i++)
                    {
                        var p = pl.GetPoint2dAt(i);
                        vs.Add(new VerticeVia(p.X, p.Y, pl.GetBulgeAt(i)));
                    }
                    if (pl.Closed) { var p0 = pl.GetPoint2dAt(0); vs.Add(new VerticeVia(p0.X, p0.Y)); }
                }
                else if (e is Line ln) { vs.Add(new VerticeVia(ln.StartPoint.X, ln.StartPoint.Y)); vs.Add(new VerticeVia(ln.EndPoint.X, ln.EndPoint.Y)); }
                string nome = NomeLivre(s.Tr, civ, cfg.ViasPrefixo);
                var arr = GeometriaVias.Arredondar(vs, cfg.ViasRaio, avisos, nome);
                var tmp = new Polyline();
                for (int i = 0; i < arr.Count; i++) tmp.AddVertexAt(i, new Point2d(arr[i].X, arr[i].Y), arr[i].Bulge, 0, 0);
                tmp.SetDatabaseDefaults();
                var tmpId = Ctx.Adicionar(s.Tr, ms, tmp);
                ObjectId alId;
                try
                {
                    alId = Alignment.Create(civ, new PolylineOptions { PlineId = tmpId, AddCurvesBetweenTangents = false, EraseExistingEntities = true },
                                            nome, ObjectId.Null, layVId, estiloAl, rotAl);
                }
                catch (System.Exception ex)
                {
                    avisos.Add($"{nome}: o alinhamento não foi criado ({ex.Message}).");
                    try { s.Tr.GetObject(tmpId, OpenMode.ForWrite).Erase(); } catch { }
                    continue;
                }
                ruas.Add(new Rua { Id = alId, Nome = nome, Origem = e is Line ? "linha" : "polilinha" });
            }
            if (ruas.Count == 0) { foreach (var a in avisos) Ctx.Msg("AVISO: " + a); return; }

            // geometria de cada rua (pontos a cada 2 m, curvas) e o terreno a cada 5 m
            foreach (var r in ruas)
            {
                var al = (Alignment)s.Tr.GetObject(r.Id, OpenMode.ForRead);
                r.S0 = al.StartingStation;
                r.S1 = al.EndingStation;
                foreach (AlignmentEntity en in al.Entities)
                    if (en is AlignmentArc arc) { r.Curvas++; r.RaioMin = Math.Min(r.RaioMin ?? double.MaxValue, arc.Radius); }
                int n = Math.Max(2, (int)Math.Ceiling((r.S1 - r.S0) / 2));
                for (int i = 0; i <= n; i++)
                {
                    double st = r.S0 + (r.S1 - r.S0) * i / n;
                    var p = RotatoriaCivil.Ponto(al, st, 0);
                    r.Pontos.Add((p.X, p.Y, st));
                }
                if (sup is null) continue;
                int nt = Math.Max(2, (int)Math.Ceiling((r.S1 - r.S0) / 5));
                for (int i = 0; i <= nt; i++)
                {
                    double st = r.S0 + (r.S1 - r.S0) * i / nt;
                    var p = RotatoriaCivil.Ponto(al, st, 0);
                    try { r.Terreno.Add((st, sup.FindElevationAtXY(p.X, p.Y))); } catch { }
                }
                if (r.Terreno.Count < (nt + 1) * 0.8)
                    avisos.Add($"{r.Nome}: a superfície '{supNome}' não cobre {100.0 * (nt + 1 - r.Terreno.Count) / (nt + 1):0} % do eixo.");
            }

            // encontros entre as ruas: a cota do terreno no ponto vale para as duas
            var encontros = GeometriaVias.Cruzamentos(ruas.Select(r => (IReadOnlyList<(double X, double Y, double S)>)r.Pontos).ToList(), cfg.ViasMeiaLargura + 1.5);
            var cotasEncontro = new List<double?>();
            foreach (var c in encontros)
            {
                double? z = null;
                if (sup is not null) try { z = sup.FindElevationAtXY(c.X, c.Y); } catch { }
                cotasEncontro.Add(z);
                if (z is not { } zz) continue;
                ruas[c.A].Fixos.Add((c.EstacaA, zz, $"encontro ({c.Tipo}) com {ruas[c.B].Nome}"));
                ruas[c.B].Fixos.Add((c.EstacaB, zz, $"encontro ({c.Tipo}) com {ruas[c.A].Nome}"));
            }

            // greide proposto
            var estiloFg = Estilo(s.Tr, civ.Styles.ProfileStyles, "Design", "Projeto", "Greide");
            var rotFg = Estilo(s.Tr, civ.Styles.LabelSetStyles.ProfileLabelSetStyles, "Complete", "Standard", "Greide");
            var estiloEg = Estilo(s.Tr, civ.Styles.ProfileStyles, "Existing", "Terreno", "EG");
            var rotEg = Estilo(s.Tr, civ.Styles.LabelSetStyles.ProfileLabelSetStyles, "No Labels", "Sem", "_No");
            foreach (var r in ruas)
            {
                var al = (Alignment)s.Tr.GetObject(r.Id, OpenMode.ForRead);
                if (sup is not null)
                    try { Profile.CreateFromSurface(RotatoriaCivil.NomeUnico(s.Tr, civ, $"{r.Nome} - terreno"), r.Id, supId, layVId, estiloEg, rotEg); }
                    catch (System.Exception ex) { avisos.Add($"{r.Nome}: o perfil do terreno não foi criado ({ex.Message})."); }
                if (modoGreide == "Proposto" && r.Terreno.Count >= 2)
                {
                    var pivs = GeometriaVias.GreideProposto(r.Terreno, r.Fixos.Select(f => (f.S, f.Z)), cfg.ViasTolGreide, cfg.ViasDistPiv);
                    r.Pivs = GeometriaVias.CurvasVerticais(pivs, cfg.ViasK, cfg.ViasLMin);
                    try
                    {
                        var pid = Profile.CreateByLayout($"{r.Nome} - greide proposto", r.Id, layVId, estiloFg, rotFg);
                        var pf = (Profile)s.Tr.GetObject(pid, OpenMode.ForWrite);
                        pf.PVIs.AddPVI(r.Pivs[0].Estaca, r.Pivs[0].Cota);
                        pf.PVIs.AddPVI(r.Pivs[^1].Estaca, r.Pivs[^1].Cota);
                        for (int i = 1; i < r.Pivs.Count - 1; i++)
                        {
                            if (r.Pivs[i].Curva > 0) pf.PVIs.AddPVISymParabola(r.Pivs[i].Estaca, r.Pivs[i].Cota, r.Pivs[i].Curva);
                            else pf.PVIs.AddPVI(r.Pivs[i].Estaca, r.Pivs[i].Cota);
                        }
                        r.Greide = pid;
                        r.GreideNome = $"proposto ({r.Pivs.Count} PIVs)";
                    }
                    catch (System.Exception ex) { avisos.Add($"{r.Nome}: o greide proposto não foi criado ({ex.Message})."); }
                }
                else if (r.Origem == "alinhamento existente")
                    foreach (ObjectId pid in al.GetProfileIds())
                        if (s.Tr.GetObject(pid, OpenMode.ForRead) is Profile pf && pf.ProfileType == ProfileType.FG) { r.Greide = pid; r.GreideNome = $"existente: {pf.Name}"; break; }
            }

            // marcas dos encontros (onde montar as esquinas com o PRJINTERSECAO)
            string layE = Ctx.GarantirLayer(s.Tr, s.Db, LayerEncontros, 6);
            for (int i = 0; i < encontros.Count; i++)
            {
                var c = encontros[i];
                var ci = new Circle(new Point3d(c.X, c.Y, 0), Vector3d.ZAxis, 2.0) { Layer = layE };
                Ctx.Adicionar(s.Tr, ms, ci);
                var tx = Ctx.Texto($"{ruas[c.A].Nome} × {ruas[c.B].Nome} ({c.Tipo})", new Point3d(c.X + 2.5, c.Y + 2.5, 0), 1.2);
                tx.Layer = layE;
                Ctx.Adicionar(s.Tr, ms, tx);
            }

            // vistas de perfil empilhadas
            int nVistas = 0;
            if (vistas)
            {
                double xMax = ruas.SelectMany(r => r.Pontos).Max(p => p.X), yMin = ruas.SelectMany(r => r.Pontos).Min(p => p.Y);
                var ins = origemVistas ?? new Point3d(xMax + 80, yMin, 0);
                var estiloPv = Estilo(s.Tr, civ.Styles.ProfileViewStyles, "Profile View", "Major Grids", "Perfil");
                var bandas = Estilo(s.Tr, civ.Styles.ProfileViewBandSetStyles, "Elevations and Stations", "Estacas", "Standard");
                foreach (var r in ruas)
                {
                    try
                    {
                        var pvId = ProfileView.Create(r.Id, ins, $"{r.Nome} - perfil", bandas, estiloPv);
                        nVistas++;
                        double alto;
                        try
                        {
                            var ext = ((Entity)s.Tr.GetObject(pvId, OpenMode.ForRead)).GeometricExtents;
                            alto = ext.MaxPoint.Y - ins.Y;
                        }
                        catch
                        {
                            double zMin = r.Terreno.Count > 0 ? r.Terreno.Min(t => t.Z) : 0, zMax = r.Terreno.Count > 0 ? r.Terreno.Max(t => t.Z) : 10;
                            alto = (zMax - zMin + 10) * 10;
                        }
                        ins = new Point3d(ins.X, ins.Y + Math.Max(40, alto) + 40, 0);
                    }
                    catch (System.Exception ex) { avisos.Add($"{r.Nome}: a vista de perfil não foi criada ({ex.Message})."); }
                }
            }

            // montagem, corredor e superfície do corredor
            var comGreide = ruas.Where(r => !r.Greide.IsNull).ToList();
            ObjectId corrId = ObjectId.Null;
            string corrNome = "";
            int regioes = 0;
            var msgsCorredor = new List<string>();
            if (comGreide.Count > 0 && CorredorProjeta.Escolher(s, civ, "Vias do loteamento") is { } cid)
            {
                corrId = cid;
                var defs = MontagensRotatoria.DefinirVias(cfg, cfg.ViasMeiaLargura);
                double yMin = ruas.SelectMany(r => r.Pontos).Min(p => p.Y), xMin = ruas.SelectMany(r => r.Pontos).Min(p => p.X);
                var montagens = MontagensRotatoria.Criar(civ, s.Tr, "Vias", defs, new Point3d(xMin, yMin - 60, 0), 40, avisos);
                if (montagens.Count > 0)
                {
                    var trechos = comGreide.Select(r => new TrechoCorredor(r.Id, r.Nome, r.S0, r.S1, montagens[0].Tipo,
                        "rua inteira; monte cada esquina com o PRJINTERSECAO (corredor Existente)")).ToList();
                    Ctx.Msg("Montando o corredor...");
                    msgsCorredor = CorredorProjeta.Inserir(s.Tr, corrId, trechos, al => comGreide.FirstOrDefault(r => r.Id == al.ObjectId)?.Greide,
                                                           montagens.ToDictionary(m => m.Tipo, m => m.Id), supId, "Vias", recortar: true, out regioes);
                    var corr = (Corridor)s.Tr.GetObject(corrId, OpenMode.ForWrite);
                    corrNome = corr.Name;
                    foreach (var r in comGreide) r.Corredor = corrNome;
                    try
                    {
                        var cs = corr.CorridorSurfaces.Add(UnicoSuperficie(s.Tr, $"{corrNome} - topo"));
                        cs.AddLinkCode("Top", true);
                        try { cs.Boundaries.AddCorridorExtentsBoundary("Limite do corredor"); } catch (System.Exception ex) { avisos.Add($"Superfície do corredor sem limite automático ({ex.Message}); defina o limite nas propriedades."); }
                        corr.Rebuild();
                    }
                    catch (System.Exception ex) { avisos.Add($"A superfície do corredor não foi criada ({ex.Message})."); }
                }
            }

            // linhas de amostragem: pontas calculadas agora (o eixo fica fechado quando elas são criadas)
            var linhas = new List<(ObjectId Al, string Nome, List<(string Nome, Point2d L, Point2d R)> Pts)>();
            if (sup is not null)
            {
                var ra = s.PedirPalavra($"Linhas de amostragem a cada {cfg.ViasEstacas:0.##} m", new[] { "Sim", "Nao" }, "Sim");
                amostragem = ra == "Sim";
                if (amostragem)
                {
                    double meia = cfg.ViasMeiaLargura + cfg.SarjetaLargura + cfg.MeioFioLargura + cfg.Passeio + 15;
                    foreach (var r in ruas)
                    {
                        var al = (Alignment)s.Tr.GetObject(r.Id, OpenMode.ForRead);
                        var lst = new List<(string, Point2d, Point2d)>();
                        var ests = new List<double>();
                        for (double st = r.S0; st < r.S1 - 0.5; st += cfg.ViasEstacas) ests.Add(st);
                        ests.Add(r.S1);
                        foreach (double st in ests)
                        {
                            var pl = RotatoriaCivil.Ponto(al, st, -meia);
                            var pr = RotatoriaCivil.Ponto(al, st, meia);
                            // o nome da linha de amostragem não pode repetir no desenho inteiro (nem em outro grupo)
                            lst.Add(($"{r.Nome} - {RotatoriaCivil.Est(st)}", new Point2d(pl.X, pl.Y), new Point2d(pr.X, pr.Y)));
                        }
                        linhas.Add((r.Id, r.Nome, lst));
                    }
                }
            }

            // verificações e relatório (lidos antes do Commit)
            var verif = new List<Verificacao>();
            var rel = new RelatorioVias { Desenho = s.Doc.Name, Superficie = supNome, Corredor = corrNome };
            string fonteMax = cfg.ViasParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA] lei municipal de parcelamento do solo / plano diretor [A CONFIRMAR]";
            foreach (var r in ruas)
            {
                var rampas = GeometriaVias.Rampas(r.Pivs);
                double? rMin = rampas.Count > 0 ? rampas.Min(x => Math.Abs(x.Rampa)) : null, rMax = rampas.Count > 0 ? rampas.Max(x => Math.Abs(x.Rampa)) : null;
                rel.Ruas.Add(new RuaVia(r.Nome, r.Origem, r.S1 - r.S0, r.Curvas, r.RaioMin, r.Terreno.Count > 0 ? r.Terreno.Min(t => t.Z) : null,
                    r.Terreno.Count > 0 ? r.Terreno.Max(t => t.Z) : null, rMin, rMax, r.Pivs.Count, r.GreideNome, r.Corredor.Length > 0 ? r.Corredor : "-"));
                for (int i = 0; i < r.Pivs.Count; i++)
                {
                    var p = r.Pivs[i];
                    string obs = p.Fixo ? r.Fixos.FirstOrDefault(f => Math.Abs(f.S - p.Estaca) < 1.01).Obs ?? "cota fixa" : i == 0 || i == r.Pivs.Count - 1 ? "ponta, na cota do terreno" : "no terreno";
                    rel.Pivs.Add(new PivRelatorio(r.Nome, RotatoriaCivil.Est(p.Estaca), p.Cota, p.Curva, i > 0 ? rampas[i - 1].Rampa : null, obs));
                }
                if (rampas.Count == 0) continue;
                var pior = rampas.OrderByDescending(x => Math.Abs(x.Rampa)).First();
                verif.Add(new Verificacao("Rampa máxima do greide", r.Nome, $"{Math.Abs(pior.Rampa) * 100:0.00} % (est. {RotatoriaCivil.Est(pior.De)} a {RotatoriaCivil.Est(pior.Ate)})",
                    $"≤ {cfg.ViasRampaMax * 100:0.##} %", fonteMax, Math.Abs(pior.Rampa) <= cfg.ViasRampaMax + 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Reprovado));
                var plana = rampas.OrderBy(x => Math.Abs(x.Rampa)).First();
                verif.Add(new Verificacao("Rampa mínima do greide (escoamento na sarjeta)", r.Nome, $"{Math.Abs(plana.Rampa) * 100:0.00} % (est. {RotatoriaCivil.Est(plana.De)} a {RotatoriaCivil.Est(plana.Ate)})",
                    $"≥ {cfg.ViasRampaMin * 100:0.##} %", "[PREMISSA] prática usual de drenagem superficial [A CONFIRMAR]",
                    Math.Abs(plana.Rampa) >= cfg.ViasRampaMin - 1e-9 ? VerificacaoRotatoria.Aprovado : VerificacaoRotatoria.Atencao));
                double desvio = r.Terreno.Max(t => Math.Abs(CotaGreide(r.Pivs, t.S) - t.Z));
                verif.Add(new Verificacao("Maior distância do greide ao terreno no eixo", r.Nome, $"{desvio:0.00} m", "corte / aterro no eixo", "geometria (greide sem curvas verticais)", VerificacaoRotatoria.Informativo));
            }
            foreach (var a in avisos.Where(a => a.Contains("não cabe no vértice")))
                verif.Add(new Verificacao("Raio da curva horizontal", a.Split(':')[0], a[(a.IndexOf(':') + 2)..], $"R ≥ {cfg.ViasRaio:0.##} m", "[PREMISSA] raio padrão dos vértices", VerificacaoRotatoria.Atencao));
            for (int i = 0; i < encontros.Count; i++)
            {
                var c = encontros[i];
                rel.Encontros.Add(new EncontroVia(ruas[c.A].Nome, RotatoriaCivil.Est(c.EstacaA), ruas[c.B].Nome, RotatoriaCivil.Est(c.EstacaB), c.X, c.Y, cotasEncontro[i], c.Tipo));
            }
            rel.Parametros.AddRange(Parametros(cfg));
            rel.Verificacoes.AddRange(verif);
            rel.Avisos.AddRange(avisos);
            rel.Pendencias.AddRange(Pendencias(encontros.Count));
            s.Tr.Commit();

            // linhas de amostragem (transação própria, sem o eixo aberto)
            int nLinhas = 0;
            if (linhas.Count > 0)
            {
                using var tr = s.Db.TransactionManager.StartTransaction();
                foreach (var (alId, nome, pts) in linhas)
                {
                    try
                    {
                        var gid = SampleLineGroup.Create($"{nome} - seções", alId);
                        var g = (SampleLineGroup)tr.GetObject(gid, OpenMode.ForWrite);
                        foreach (SectionSource src in g.GetSectionSources())
                            if (src.SourceId == supId || (!corrId.IsNull && src.SourceId == corrId) || src.SourceName.EndsWith(" - topo", StringComparison.Ordinal))
                            {
                                src.IsSampled = true;
                                try { src.UpdateMode = SectionUpdateType.Dynamic; } catch { }
                            }
                        foreach (var (n, pl, pr) in pts)
                        {
                            try { SampleLine.Create(n, gid, new Point2dCollection { pl, pr }); nLinhas++; }
                            catch (System.Exception ex) { avisos.Add($"{nome}: linha {n} não entrou ({ex.Message})."); }
                        }
                    }
                    catch (System.Exception ex) { avisos.Add($"{nome}: o grupo de linhas de amostragem não foi criado ({ex.Message})."); }
                }
                tr.Commit();
            }

            // mensagens
            string premissa = cfg.ViasParametrosDoUsuario ? "" : " [PREMISSA: valores de partida, troque em Parametros]";
            Ctx.Msg($"{ruas.Count} rua(s), {ruas.Sum(r => r.S1 - r.S0):0.00} m de eixo; raio nos vértices {cfg.ViasRaio:0.##} m; terreno '{(supNome.Length > 0 ? supNome : "-")}'{premissa}.");
            foreach (var r in ruas)
            {
                var rampas = GeometriaVias.Rampas(r.Pivs);
                Ctx.Msg($"  {r.Nome} ({r.Origem}): {r.S1 - r.S0:0.00} m, {r.Curvas} curva(s){(r.RaioMin is { } rm ? $" (menor R {rm:0.00} m)" : "")}; greide {r.GreideNome}" +
                        (rampas.Count > 0 ? $", rampas de {rampas.Min(x => Math.Abs(x.Rampa)) * 100:0.00} % a {rampas.Max(x => Math.Abs(x.Rampa)) * 100:0.00} %" : ""));
            }
            if (encontros.Count > 0)
            {
                Ctx.Msg($"{encontros.Count} encontro(s) entre as ruas (marcados na camada {LayerEncontros}); os greides passam na mesma cota em cada um:");
                for (int i = 0; i < encontros.Count; i++)
                {
                    var c = encontros[i];
                    Ctx.Msg($"  {ruas[c.A].Nome} est. {RotatoriaCivil.Est(c.EstacaA)} × {ruas[c.B].Nome} est. {RotatoriaCivil.Est(c.EstacaB)} ({c.Tipo}){(cotasEncontro[i] is { } z ? $", cota {z:0.000}" : "")}");
                }
            }
            int rp = verif.Count(v => v.Resultado == VerificacaoRotatoria.Reprovado), at = verif.Count(v => v.Resultado == VerificacaoRotatoria.Atencao);
            Ctx.Msg($"Verificações: {verif.Count(v => v.Resultado == VerificacaoRotatoria.Aprovado)} aprovada(s), {rp} REPROVADA(S), {at} com atenção.");
            foreach (var v in verif.Where(v => v.Resultado is VerificacaoRotatoria.Reprovado or VerificacaoRotatoria.Atencao))
                Ctx.Msg($"  {v.Resultado}: {v.Item} — {v.Braco}: {v.Valor} (critério {v.Criterio})");
            if (nVistas > 0) Ctx.Msg($"{nVistas} vista(s) de perfil empilhada(s).");
            if (corrNome.Length > 0)
            {
                Ctx.Msg($"Corredor '{corrNome}': {regioes} região(ões), uma por rua, com a superfície '{corrNome} - topo'. As ruas atravessam os encontros: monte cada esquina com o PRJINTERSECAO (corredor Existente).");
                foreach (var m in msgsCorredor) Ctx.Msg("  corredor: " + m);
            }
            if (nLinhas > 0) Ctx.Msg($"{nLinhas} linha(s) de amostragem a cada {cfg.ViasEstacas:0.##} m (terreno e corredor amostrados): dá para rodar o PRJCUBACAO.");
            foreach (var a in avisos) Ctx.Msg("AVISO: " + a);
            Ctx.Msg($"Eixos e greides na camada {LayerVias}; as polilinhas originais ficaram como estavam. U desfaz.");

            if (s.PedirPalavra("Gravar o relatório das vias em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Vias_do_loteamento.xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Relatório gravado: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJVIAS: {ex.Message}"); }
    }

    private static double CotaGreide(IReadOnlyList<PivVia> p, double s)
    {
        for (int i = 0; i + 1 < p.Count; i++)
            if (s <= p[i + 1].Estaca + 1e-9)
                return p[i].Cota + (p[i + 1].Cota - p[i].Cota) * (s - p[i].Estaca) / Math.Max(1e-9, p[i + 1].Estaca - p[i].Estaca);
        return p.Count > 0 ? p[^1].Cota : 0;
    }

    /// <summary>"Rua 01", "Rua 02"...: o primeiro número sem alinhamento com esse nome.</summary>
    private static string NomeLivre(Transaction tr, CivilDocument civ, string prefixo)
    {
        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId id in civ.GetAlignmentIds())
            try { if (tr.GetObject(id, OpenMode.ForRead) is Alignment al) nomes.Add(al.Name); } catch { }
        for (int k = 1; ; k++)
        {
            string n = $"{prefixo} {k:00}";
            if (!nomes.Contains(n)) return n;
        }
    }

    private static string UnicoSuperficie(Transaction tr, string nome)
    {
        var nomes = VolumesCivil.Superficies(tr).Select(x => x.Nome).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string n = nome;
        for (int i = 2; nomes.Contains(n); i++) n = $"{nome} ({i})";
        return n;
    }

    /// <summary>Estilo cujo nome contém uma das chaves (na ordem); senão o primeiro da coleção; Null se vazia.</summary>
    private static ObjectId Estilo(Transaction tr, StyleCollectionBase col, params string[] chaves)
    {
        var lista = new List<(ObjectId Id, string Nome)>();
        try
        {
            foreach (ObjectId id in col)
                try { lista.Add((id, ((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name)); } catch { }
        }
        catch { }
        foreach (var k in chaves)
            if (lista.FirstOrDefault(x => x.Nome.Contains(k, StringComparison.OrdinalIgnoreCase)) is { Id.IsNull: false } achou) return achou.Id;
        return lista.Count > 0 ? lista[0].Id : ObjectId.Null;
    }

    private static string Resumo(ConfigRotatoria c) =>
        $"Vias: nomes '{c.ViasPrefixo} 01'..., raio nos vértices {c.ViasRaio:0.##} m, meia pista {c.ViasMeiaLargura:0.##} m, " +
        $"greide a até {c.ViasTolGreide:0.##} m do terreno (PIVs a {c.ViasDistPiv:0.#} m ou mais, K {c.ViasK:0.#}), rampas de {c.ViasRampaMin * 100:0.##} % a {c.ViasRampaMax * 100:0.##} %, " +
        $"linhas de amostragem a cada {c.ViasEstacas:0.##} m" + (c.ViasParametrosDoUsuario ? "." : " [PREMISSA].");

    private static bool PedirParametros(Editor ed, ConfigRotatoria cfg)
    {
        double? N(string m, double v, bool zero = false)
        {
            var r = ed.GetDouble(new PromptDoubleOptions($"\n{m}") { AllowNone = true, AllowNegative = false, AllowZero = zero, DefaultValue = v, UseDefaultValue = true });
            return r.Status == PromptStatus.OK ? r.Value : r.Status == PromptStatus.None ? v : null;
        }
        var pt = ed.GetString(new PromptStringOptions($"\nPrefixo do nome das ruas <{cfg.ViasPrefixo}>: ") { AllowSpaces = true });
        if (pt.Status != PromptStatus.OK) return false;
        if (N("Raio das curvas nos vértices das polilinhas (m)", cfg.ViasRaio, zero: true) is not { } raio) return false;
        if (N("Meia largura da pista, do eixo ao bordo (m)", cfg.ViasMeiaLargura) is not { } w) return false;
        if (N("Rampa mínima do greide (%)", cfg.ViasRampaMin * 100, zero: true) is not { } imin) return false;
        if (N("Rampa máxima do greide (%)", cfg.ViasRampaMax * 100) is not { } imax) return false;
        if (N("Distância máxima do greide ao terreno (m)", cfg.ViasTolGreide) is not { } tol) return false;
        if (N("Distância mínima entre PIVs (m)", cfg.ViasDistPiv) is not { } dpiv) return false;
        if (N("K das curvas verticais (L = K × diferença das rampas em %)", cfg.ViasK, zero: true) is not { } k) return false;
        if (N("Intervalo das linhas de amostragem (m)", cfg.ViasEstacas) is not { } est) return false;
        if (!CmdRotatoria.PedirMontagens(ed, cfg)) return false;
        if (pt.StringResult.Trim().Length > 0) cfg.ViasPrefixo = pt.StringResult.Trim();
        cfg.ViasRaio = raio; cfg.ViasMeiaLargura = w; cfg.ViasRampaMin = imin / 100; cfg.ViasRampaMax = imax / 100;
        cfg.ViasTolGreide = tol; cfg.ViasDistPiv = dpiv; cfg.ViasK = k; cfg.ViasEstacas = est;
        cfg.ViasParametrosDoUsuario = true;
        ConfigRotatoria.Salvar(cfg);
        return true;
    }

    private static IEnumerable<ParametroRotatoria> Parametros(ConfigRotatoria c)
    {
        string P(string referencia) => c.ViasParametrosDoUsuario ? "informado pelo usuário" : "[PREMISSA] valor de partida" + (referencia.Length > 0 ? " — " + referencia : "");
        yield return new("Raio das curvas nos vértices", c.ViasRaio.ToString("0.00"), "m", P("vias locais de loteamento; conferir na lei municipal e com o giro do veículo [A CONFIRMAR]"));
        yield return new("Meia largura da pista", c.ViasMeiaLargura.ToString("0.00"), "m", P("largura mínima das vias na lei municipal de parcelamento (Lei 6.766/79 remete ao município) [A CONFIRMAR]"));
        yield return new("Rampa mínima", (c.ViasRampaMin * 100).ToString("0.00"), "%", P("escoamento na sarjeta [A CONFIRMAR]"));
        yield return new("Rampa máxima", (c.ViasRampaMax * 100).ToString("0.00"), "%", P("lei municipal / plano diretor [A CONFIRMAR]"));
        yield return new("Greide: distância máxima ao terreno", c.ViasTolGreide.ToString("0.00"), "m", P("critério do PROJETA para o greide inicial"));
        yield return new("Greide: distância mínima entre PIVs", c.ViasDistPiv.ToString("0.0"), "m", P(""));
        yield return new("Curvas verticais: K (L = K·A)", c.ViasK.ToString("0.0"), "m/%", P("AASHTO, 30 km/h, curva côncava ≈ 6 — referência complementar [A CONFIRMAR]"));
        yield return new("Curvas verticais: comprimento mínimo", c.ViasLMin.ToString("0.0"), "m", P(""));
        yield return new("Linhas de amostragem", c.ViasEstacas.ToString("0.0"), "m", P("estaqueamento de 20 m"));
        yield return new("Pavimento: revestimento / base / sub-base", $"{c.Revestimento:0.00} / {c.Base:0.00} / {c.SubBase:0.00}", "m", "[PREMISSA] estrutura a dimensionar [A CONFIRMAR]");
        yield return new("Meio-fio / sarjeta / passeio", $"{c.MeioFioAltura:0.00} / {c.SarjetaLargura:0.00} / {c.Passeio:0.00}", "m", "[PREMISSA] padrão do município [A CONFIRMAR]");
        yield return new("Taludes corte / aterro (H:V)", $"{c.TaludeCorte:0.##}:1 / {c.TaludeAterro:0.##}:1", "", "[PREMISSA] estudo geotécnico [A CONFIRMAR]");
    }

    private static IEnumerable<string> Pendencias(int encontros) => new[]
    {
        encontros > 0 ? $"Montar as {encontros} esquina(s) com o PRJINTERSECAO, no mesmo corredor (opção Existente): raio da esquina, bordos, visibilidade e a região de cada esquina." : "",
        "Revisar o greide proposto: ele acompanha o terreno; acerte rampas, cotas dos lotes (acesso), drenagem e o equilíbrio de corte e aterro.",
        "Confirmar larguras, rampas e raios na lei municipal de parcelamento do solo e nas diretrizes da prefeitura.",
        "Drenagem (sarjetas, bocas de lobo e galerias) com o PRJREDE; pavimento dimensionado; acessibilidade dos passeios e rampas (NBR 9050).",
        "Volumes de terraplenagem pelas linhas de amostragem (PRJCUBACAO) ou entre superfícies (PRJVOLUMES).",
    }.Where(p => p.Length > 0);
}
