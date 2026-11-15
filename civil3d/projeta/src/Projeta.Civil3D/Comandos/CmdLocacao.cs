using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Table = Autodesk.AutoCAD.DatabaseServices.Table;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJLOCACAO — locação de campo: dos meios-fios, ilhas, bordos e eixos selecionados (polilinhas, linhas, arcos, círculos e
/// alinhamentos) saem pontos numerados — início e fim, vértices, PC, PT, PCC, PRC, centros e intermediários a cada X m —, com a
/// cota da superfície; vira pontos COGO, CSV (PNEZD) para a estação total ou o GNSS, quadro de coordenadas na prancha e Excel.
/// </summary>
public sealed class CmdLocacao
{
    internal const string Layer = "PRJ-LOCACAO";

    [CommandMethod("PRJLOCACAO", CommandFlags.Modal)]
    public static void Locacao()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Locacao ?? new ParametrosLocacao();
            ObjectId[]? ids = null;
            while (ids is null)
            {
                Ctx.Msg(Resumo(p));
                var po = new PromptSelectionOptions { MessageForAdding = "\nSelecione meios-fios, ilhas, bordos e eixos a locar (polilinhas, linhas, arcos, círculos, alinhamentos) ou [Camadas/Parametros]: " };
                po.Keywords.Add("Camadas");
                po.Keywords.Add("Parametros");
                string? palavra = null;
                // a palavra-chave encerra a seleção: a exceção lançada no evento volta pelo GetSelection
                po.KeywordInput += (_, e) => { palavra = e.Input; throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.OK); };
                var filtro = new SelectionFilter(new[]
                {
                    new TypedValue(-4, "<OR"), new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), new TypedValue((int)DxfCode.Start, "LINE"),
                    new TypedValue((int)DxfCode.Start, "ARC"), new TypedValue((int)DxfCode.Start, "CIRCLE"), new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT"), new TypedValue(-4, "OR>"),
                });
                PromptSelectionResult ps;
                try { ps = ed.GetSelection(po, filtro); }
                catch (Autodesk.AutoCAD.Runtime.Exception) { ps = null!; }
                if (palavra == "Parametros") { using (var s0 = Sessao.Abrir()) { if (!PedirParametros(s0, p)) return; } cfg.Locacao = p; ConfigRotatoria.Salvar(cfg); continue; }
                if (palavra == "Camadas")
                {
                    var pr = ed.GetString(new PromptStringOptions("\nCamadas a locar (curinga * e vírgula; ex.: PRJ-ROTATORIA*,PRJ-INTERSECAO*): ") { AllowSpaces = true });
                    if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult)) return;
                    var f2 = new SelectionFilter(new[]
                    {
                        new TypedValue(-4, "<AND"), new TypedValue((int)DxfCode.LayerName, pr.StringResult.Trim()),
                        new TypedValue(-4, "<OR"), new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.Start, "ARC"), new TypedValue((int)DxfCode.Start, "CIRCLE"), new TypedValue((int)DxfCode.Start, "AECC_ALIGNMENT"), new TypedValue(-4, "OR>"),
                        new TypedValue(-4, "AND>"),
                    });
                    var sa = ed.SelectAll(f2);
                    if (sa.Status != PromptStatus.OK) { Ctx.Msg($"Nada nas camadas '{pr.StringResult}'."); return; }
                    ids = sa.Value.GetObjectIds();
                    break;
                }
                if (ps is null || ps.Status != PromptStatus.OK) return;
                ids = ps.Value.GetObjectIds();
            }

            using var s = Sessao.Abrir();
            var civ = CivilApplication.ActiveDocument;
            var elementos = new List<ElementoLocacao>();
            var avisos = new List<string>();
            foreach (var id in ids)
            {
                var ent = s.Tr.GetObject(id, OpenMode.ForRead);
                string camada = (ent as Entity)?.Layer ?? "";
                string cod = LocacaoCampo.CodigoDaCamada(camada);
                switch (ent)
                {
                    case Polyline pl:
                        {
                            var v = new List<VerticeVia>();
                            for (int i = 0; i < pl.NumberOfVertices; i++) { var q = pl.GetPoint2dAt(i); v.Add(new VerticeVia(q.X, q.Y, pl.GetBulgeAt(i))); }
                            bool fech = pl.Closed;
                            if (!fech && v.Count > 2 && Math.Abs(v[0].X - v[^1].X) < 1e-6 && Math.Abs(v[0].Y - v[^1].Y) < 1e-6) { v.RemoveAt(v.Count - 1); fech = true; }
                            elementos.Add(new ElementoLocacao { Nome = $"polilinha {pl.Handle} ({camada})", Codigo = cod, Vertices = v, Fechado = fech });
                            break;
                        }
                    case Line ln:
                        elementos.Add(new ElementoLocacao { Nome = $"linha {ln.Handle} ({camada})", Codigo = cod, Vertices = new() { new(ln.StartPoint.X, ln.StartPoint.Y), new(ln.EndPoint.X, ln.EndPoint.Y) } });
                        break;
                    case Arc ar:
                        {
                            double sw = ar.EndAngle - ar.StartAngle;
                            if (sw <= 0) sw += 2 * Math.PI;
                            elementos.Add(new ElementoLocacao
                            {
                                Nome = $"arco {ar.Handle} ({camada})", Codigo = cod,
                                Vertices = new() { new(ar.StartPoint.X, ar.StartPoint.Y, Math.Tan(sw / 4)), new(ar.EndPoint.X, ar.EndPoint.Y) },
                            });
                            break;
                        }
                    case Circle ci:
                        elementos.Add(new ElementoLocacao { Nome = $"círculo {ci.Handle} ({camada})", Codigo = cod, Circulo = (ci.Center.X, ci.Center.Y, ci.Radius) });
                        break;
                    case Alignment al:
                        elementos.Add(Alinhamento(al, p));
                        break;
                }
            }
            if (elementos.Count == 0) { Ctx.Msg("Nada para locar na seleção."); return; }

            // cota: superfície do projeto (topo do corredor) ou nenhuma
            var sups = VolumesCivil.Superficies(s.Tr);
            var nomes = new List<string> { "(sem cota)" };
            nomes.AddRange(sups.Select(x => x.Nome));
            int pp = sups.FindIndex(x => x.Nome.Contains("topo", StringComparison.OrdinalIgnoreCase));
            int ks = s.EscolherDaLista("Superfície das cotas (a do pavimento acabado; a cota do topo do meio-fio fica a conferir)", nomes, pp + 1);
            if (ks < 0) return;
            var sup = ks > 0 ? s.Tr.GetObject(sups[ks - 1].Id, OpenMode.ForRead) as Surface : null;
            Func<double, double, double?>? z = sup is null ? null : (x, y) => { try { double v = sup.FindElevationAtXY(x, y); return double.IsNaN(v) ? null : v; } catch { return null; } };

            // número inicial: o seguinte ao maior ponto COGO do desenho (ou o dos parâmetros, se maior)
            var usados = new HashSet<uint>();
            try { foreach (ObjectId pid in civ.CogoPoints) if (s.Tr.GetObject(pid, OpenMode.ForRead) is CogoPoint cp) usados.Add(cp.PointNumber); } catch { }
            int sugestao = Math.Max(p.NumeroInicial, usados.Count == 0 ? 1 : (int)usados.Max() + 1);
            var ni = s.PedirNumero("Número do primeiro ponto", sugestao);
            if (ni is null) return;
            p.NumeroInicial = Math.Max(1, (int)Math.Round(ni.Value));

            var res = LocacaoCampo.Gerar(elementos, p, z);
            avisos.AddRange(res.Avisos);
            if (res.Pontos.Count == 0) { Ctx.Msg("Nenhum ponto gerado."); return; }

            // pontos COGO (com o número da locação) ou pontos e textos simples
            bool cogo = s.PedirPalavra("Criar os pontos COGO do Civil 3D", new[] { "Sim", "Nao" }, "Sim") == "Sim";
            int conflitos = 0;
            string lay = Ctx.GarantirLayer(s.Tr, s.Db, Layer, 4);
            var ms = Ctx.EspacoModelo(s.Tr, s.Db);
            if (cogo)
            {
                // rótulo só com o número (menos poluído na prancha), se o desenho tiver um estilo assim
                var estiloNumero = ObjectId.Null;
                string nomeEstilo = "";
                try
                {
                    foreach (ObjectId sid in civ.Styles.LabelStyles.PointLabelStyles.LabelStyles)
                    {
                        if (s.Tr.GetObject(sid, OpenMode.ForRead) is not Autodesk.Civil.DatabaseServices.Styles.LabelStyle st) continue;
                        string nm = st.Name.ToLowerInvariant();
                        if (nm.Contains("number only") || nm.Contains("somente n") || nm.Contains("apenas n") || nm.Contains("só n")) { estiloNumero = sid; nomeEstilo = st.Name; break; }
                    }
                }
                catch { }
                if (!estiloNumero.IsNull) avisos.Add($"rótulo dos pontos COGO no estilo '{nomeEstilo}' (troque nas propriedades do grupo, se quiser a cota e a descrição na prancha)");
                foreach (var x in res.Pontos)
                {
                    if (usados.Contains((uint)x.Numero)) { conflitos++; continue; }
                    var pid = ObjectId.Null;
                    try
                    {
                        pid = civ.CogoPoints.Add(new Point3d(x.X, x.Y, x.Z ?? 0), x.Descricao, true);
                        var cp = (CogoPoint)s.Tr.GetObject(pid, OpenMode.ForWrite);
                        // o Add já numera pelo "próximo número"; trocar para o mesmo número dá "PointNumber is existing"
                        if (cp.PointNumber != (uint)x.Numero) cp.PointNumber = (uint)x.Numero;
                        cp.Layer = lay;
                        if (!estiloNumero.IsNull) try { cp.LabelStyleId = estiloNumero; } catch { }
                        usados.Add((uint)x.Numero);
                    }
                    catch (System.Exception ex)
                    {
                        // não deixa para trás o ponto criado com outro número
                        if (!pid.IsNull) try { civ.CogoPoints.Remove(pid); } catch { }
                        conflitos++;
                        if (conflitos <= 3) avisos.Add($"ponto {x.Numero}: {ex.Message}");
                    }
                }
                try
                {
                    var gid = civ.PointGroups.Add(Unico(s, civ, $"PRJ Locação {res.Pontos[0].Numero}-{res.Pontos[^1].Numero}"));
                    var g = (PointGroup)s.Tr.GetObject(gid, OpenMode.ForWrite);
                    g.SetQuery(new StandardPointGroupQuery { IncludeNumbers = $"{res.Pontos[0].Numero}-{res.Pontos[^1].Numero}" });
                    g.Update();
                }
                catch (System.Exception ex) { avisos.Add($"grupo de pontos não criado ({ex.Message})"); }
                if (conflitos > 0) avisos.Add($"{conflitos} ponto(s) COGO não criados (número já usado no desenho): estão no CSV e no quadro com o número da locação.");
            }
            else
            {
                foreach (var x in res.Pontos)
                {
                    Ctx.Adicionar(s.Tr, ms, new DBPoint(new Point3d(x.X, x.Y, x.Z ?? 0)) { Layer = lay });
                    var tx = Ctx.Texto(x.Numero.ToString(), new Point3d(x.X + 0.3, x.Y + 0.3, 0), 0.5);
                    tx.Layer = lay;
                    Ctx.Adicionar(s.Tr, ms, tx);
                }
            }

            // quadro de coordenadas na prancha
            var pq = ed.GetPoint(new PromptPointOptions("\nCanto superior esquerdo do quadro de coordenadas <Enter = sem quadro>: ") { AllowNone = true });
            int quadros = 0;
            if (pq.Status == PromptStatus.OK)
            {
                var pos = pq.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                double x0 = pos.X;
                foreach (var t in LocacaoCampo.Quadros(res.Pontos, 50, p.CasasCoordenadas))
                {
                    var tid = TabelasDesenho.Inserir(s, t, new Point3d(x0, pos.Y, 0), 1.0, Layer);
                    double w = s.Tr.GetObject(tid, OpenMode.ForRead) is Table tb ? tb.Width : 60;
                    x0 += w + 3;
                    quadros++;
                }
            }
            s.Tr.Commit();

            string sistema = "";
            try { var d = GeoCivil.Detectar(s.Tr, s.Db); sistema = d.Sistema?.Nome ?? d.Codigo ?? ""; } catch { }
            Ctx.Msg($"{res.Pontos.Count} ponto(s) de locação, de {res.Pontos[0].Numero} a {res.Pontos[^1].Numero}, de {elementos.Count} elemento(s); {res.Curvas.Count} curva(s); " +
                    $"cotas {(sup is null ? "não lidas" : $"da superfície '{nomes[ks]}'")}{(cogo ? "; pontos COGO na camada " + Layer : "")}{(quadros > 0 ? $"; {quadros} quadro(s) de coordenadas" : "")}.");
            foreach (var g in res.Pontos.Where(x => x.Tipo.Length > 0).GroupBy(x => x.Tipo.Split('/')[0]).OrderBy(g => g.Key))
                Ctx.Msg($"  {g.Key}: {g.Count()}");
            foreach (var a in avisos) Ctx.Msg("AVISO: " + a);

            if (s.PedirPalavra("Gravar o CSV para a estação total / GNSS", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Arquivo CSV (*.csv)|*.csv|Texto (*.txt)|*.txt", "Locacao.csv");
                if (arq is not null)
                {
                    File.WriteAllText(arq, LocacaoCampo.Csv(res.Pontos, p), new System.Text.UTF8Encoding(false));
                    Ctx.Msg($"CSV gravado ({p.Formato}, vírgula, ponto decimal): {arq}");
                }
            }
            var rel = new RelatorioLocacao { Desenho = s.Doc.Name, Superficie = sup is null ? "" : nomes[ks], Sistema = sistema, Resultado = res, Parametros = p };
            if (s.PedirPalavra("Gravar a nota de locação em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Nota_de_locacao.xlsx");
                if (arq is not null)
                {
                    rel.Salvar(arq);
                    Ctx.Msg($"Planilha gravada: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJLOCACAO: {ex.Message}"); }
    }

    /// <summary>Alinhamento: pontos notáveis (PC, PT, TS, SC, CS, ST) e estacas a cada intervalo.</summary>
    private static ElementoLocacao Alinhamento(Alignment al, ParametrosLocacao p)
    {
        var pr = new List<(double X, double Y, string Tipo, string Obs)>();
        (double X, double Y) Pt(double st) { double x = 0, y = 0; al.PointLocation(st, 0, ref x, ref y); return (x, y); }
        var marcas = new SortedDictionary<double, string>();
        void Marca(double st, string t) { st = Math.Round(st, 4); marcas[st] = marcas.TryGetValue(st, out var a) && a.Length > 0 && !a.Contains(t) ? $"{a}/{t}" : t; }
        Marca(al.StartingStation, "INÍCIO");
        Marca(al.EndingStation, "FIM");
        foreach (AlignmentEntity en in al.Entities)
        {
            try
            {
                switch (en)
                {
                    case AlignmentArc a: Marca(a.StartStation, "PC"); Marca(a.EndStation, "PT"); break;
                    case AlignmentSpiral sp: Marca(sp.StartStation, "TS"); Marca(sp.EndStation, "SC"); break;
                    case AlignmentSCS scs: Marca(scs.StartStation, "TS"); Marca(scs.Arc.StartStation, "SC"); Marca(scs.Arc.EndStation, "CS"); Marca(scs.EndStation, "ST"); break;
                    case AlignmentSTS sts: Marca(sts.StartStation, "TS"); Marca(sts.EndStation, "ST"); break;
                }
            }
            catch { }
        }
        foreach (var (st, t) in marcas) { var q = Pt(st); pr.Add((q.X, q.Y, t, $"est. {RotatoriaCivil.Est(st)}")); }
        double passo = p.IntervaloTangente;
        for (double st = Math.Ceiling(al.StartingStation / passo) * passo; st < al.EndingStation - 0.01; st += passo)
        {
            if (marcas.Keys.Any(k => Math.Abs(k - st) < 0.01)) continue;
            var q = Pt(st);
            pr.Add((q.X, q.Y, "EST", RotatoriaCivil.Est(st)));
        }
        return new ElementoLocacao { Nome = $"alinhamento {al.Name}", Codigo = "EIXO", Prontos = pr };
    }

    private static string Unico(Sessao s, CivilDocument civ, string nome)
    {
        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try { foreach (ObjectId id in civ.PointGroups) if (s.Tr.GetObject(id, OpenMode.ForRead) is PointGroup g) nomes.Add(g.Name); } catch { }
        string n = nome;
        for (int i = 2; nomes.Contains(n); i++) n = $"{nome} ({i})";
        return n;
    }

    private static string Resumo(ParametrosLocacao p) =>
        $"Locação: pontos a cada {p.IntervaloTangente:0.##} m nas tangentes e {p.IntervaloCurva:0.##} m nas curvas{(p.Centros ? ", com os centros das curvas" : "")}, CSV {p.Formato}" +
        (p.ParametrosDoUsuario ? "." : " [PREMISSA; troque em Parametros].");

    private static bool PedirParametros(Sessao s, ParametrosLocacao p)
    {
        var a = s.PedirNumero("Intervalo nas tangentes (m)", p.IntervaloTangente);
        if (a is null) return false;
        var b = s.PedirNumero("Intervalo nas curvas (m)", p.IntervaloCurva);
        if (b is null) return false;
        var c = s.PedirPalavra("Locar o centro das curvas", new[] { "Sim", "Nao" }, p.Centros ? "Sim" : "Nao");
        if (c is null) return false;
        var f = s.PedirPalavra("Ordem das colunas do CSV", new[] { "PNEZD", "PENZD" }, p.Formato);
        if (f is null) return false;
        var k = s.PedirNumero("Casas decimais das coordenadas", p.CasasCoordenadas);
        if (k is null) return false;
        p.IntervaloTangente = Math.Clamp(a.Value, 0.5, 100); p.IntervaloCurva = Math.Clamp(b.Value, 0.5, 100); p.Centros = c == "Sim"; p.Formato = f;
        p.CasasCoordenadas = Math.Clamp((int)Math.Round(k.Value), 0, 4); p.ParametrosDoUsuario = true;
        return true;
    }
}
