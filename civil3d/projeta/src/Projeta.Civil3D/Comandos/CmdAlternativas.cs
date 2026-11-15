using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Alinhamentos;
using Projeta.Giro.Core.Geometria;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJALTERNATIVAS — comparador de alternativas no mesmo cruzamento: PARE simples, interseção canalizada, mini-rotatória e rotatórias
/// de 1 e de 2 faixas, desenhadas lado a lado com área ocupada, pavimento, meio-fio, ilhas, terraplenagem indicativa, custo pela
/// tabela de preços do usuário, capacidade (HCM) e pontos de conflito; quadro na prancha e relatório de decisão em Excel.
/// </summary>
public sealed class CmdAlternativas
{
    internal const string Layer = "PRJ-ALTERNATIVAS", LayerPav = "PRJ-ALTERNATIVAS-PAVIMENTO", LayerIlha = "PRJ-ALTERNATIVAS-ILHAS",
                          LayerTransp = "PRJ-ALTERNATIVAS-TRANSPONIVEL", LayerEixo = "PRJ-ALTERNATIVAS-EIXOS";

    [CommandMethod("PRJALTERNATIVAS", CommandFlags.Modal)]
    public static void Alternativas()
    {
        try
        {
            var ed = Ctx.Ed;
            var cfg = ConfigRotatoria.Carregar();
            var p = cfg.Alternativas ?? new ParametrosAlternativas();
            var pc = cfg.Capacidade ?? new ParametrosCapacidade();
            using var s = Sessao.Abrir();
            while (true)
            {
                Ctx.Msg(Resumo(p));
                var r = s.PedirPalavra("Comparador de alternativas", new[] { "Comparar", "Parametros", "Precos" }, "Comparar");
                if (r is null) return;
                if (r == "Parametros") { if (!PedirParametros(s, p)) return; cfg.Alternativas = p; ConfigRotatoria.Salvar(cfg); continue; }
                if (r == "Precos") { if (!Precos(s, p)) return; cfg.Alternativas = p; ConfigRotatoria.Salvar(cfg); continue; }
                break;
            }

            // o cruzamento
            var itens = ArmazenamentoSinal.Ler(s.Tr, s.Db).Where(x => x.Intersecao is not null || x.Rotatoria is not null).ToList();
            string? modo = itens.Count > 0 ? s.PedirPalavra("Cruzamento", new[] { "Guardado", "Eixos" }, "Guardado") : "Eixos";
            if (modo is null) return;
            CenarioAlternativas? c;
            ItemSinalizacao? item = null;
            if (modo == "Guardado")
            {
                int k = itens.Count == 1 ? 0 : s.EscolherDaLista("Interseção ou rotatória guardada", itens.Select(x => $"{x.Nome} ({x.Tipo})").ToList(), 0);
                if (k < 0) return;
                item = itens[k];
                c = DoItem(item);
            }
            else c = DosEixos(s);
            if (c is null) return;
            if (c.Bracos.Count < 3) { Ctx.Msg($"O cruzamento tem {c.Bracos.Count} braço(s): preciso de 3 ou mais."); return; }

            // via principal
            if (c.Bracos.Count(b => b.Principal) != 2)
            {
                var pares = new List<(int A, int B)>();
                for (int i = 0; i < c.Bracos.Count; i++)
                    for (int j = i + 1; j < c.Bracos.Count; j++) pares.Add((i, j));
                double Vol(int i) => c.Volumes.Where(x => x.Key.O == i || x.Key.D == i).Sum(x => x.Value);
                double Oposto((int A, int B) q) => Math.Abs(Math.Abs(Norm(c.Bracos[q.A].Angulo - c.Bracos[q.B].Angulo)) - Math.PI);
                var ord = pares.OrderBy(q => Oposto(q) > 30 * Math.PI / 180 ? 1 : 0).ThenByDescending(q => Vol(q.A) + Vol(q.B)).ThenBy(Oposto).ToList();
                int kp = s.EscolherDaLista("Via principal (preferencial no PARE)", ord.Select(q => $"{c.Bracos[q.A].Rumo} – {c.Bracos[q.B].Rumo} ('{c.Bracos[q.A].Nome}' / '{c.Bracos[q.B].Nome}')").ToList(), 0);
                if (kp < 0) return;
                foreach (var b in c.Bracos) b.Principal = false;
                c.Bracos[ord[kp].A].Principal = c.Bracos[ord[kp].B].Principal = true;
            }

            // volumes
            if (!c.Volumes.Values.Any(v => v > 0))
            {
                var mv = s.PedirPalavra("Sem contagem guardada (PRJCAPACIDADE). Volumes", new[] { "Digitar", "Sem" }, "Sem");
                if (mv is null) return;
                if (mv == "Digitar" && !DigitarVolumes(s, c)) return;
            }

            // terreno
            var sups = VolumesCivil.Superficies(s.Tr);
            var nomes = new List<string> { "(sem terreno)" };
            nomes.AddRange(sups.Select(x => x.Nome));
            int pt = 0;
            for (int i = 0; i < sups.Count; i++)
                if (sups[i].Nome.Contains("terreno", StringComparison.OrdinalIgnoreCase) || sups[i].Nome.Contains("TN", StringComparison.Ordinal) || sups[i].Nome.Contains("exist", StringComparison.OrdinalIgnoreCase)) { pt = i + 1; break; }
            int ks = sups.Count == 0 ? 0 : s.EscolherDaLista("Superfície do terreno para a terraplenagem indicativa", nomes, pt);
            if (ks < 0) return;
            Surface? sup = ks > 0 ? s.Tr.GetObject(sups[ks - 1].Id, OpenMode.ForRead) as Surface : null;
            Func<double, double, double?>? terreno = sup is null ? null : (x, y) => { double z = CivilUtil.Cota(sup, x, y); return double.IsNaN(z) ? null : z; };

            Ctx.Msg("Montando as cinco alternativas...");
            var cmp = ComparadorAlternativas.Gerar(c, p, pc, terreno);

            // desenho anterior
            int antigos = ContarNasCamadas(s);
            if (antigos > 0 && s.PedirPalavra($"Apagar o desenho anterior do PRJALTERNATIVAS ({antigos} objetos nas camadas {Layer}*)", new[] { "Sim", "Nao" }, "Sim") == "Sim")
                ApagarNasCamadas(s);

            double passo = 2 * cmp.Zona + 30;
            var pb = ed.GetPoint(new PromptPointOptions("\nCentro da 1ª alternativa (as outras vão para a direita) <Enter = à direita do cruzamento>: ") { AllowNone = true });
            if (pb.Status == PromptStatus.Cancel) return;
            var base0 = pb.Status == PromptStatus.OK ? pb.Value.TransformBy(ed.CurrentUserCoordinateSystem) : new Point3d(Math.Max(c.Ox + passo, LadoDireito(s, c.Ox, c.Oy, 400) + cmp.Zona + 40), c.Oy, 0);
            Desenhar(s, cmp, new V2(base0.X, base0.Y), passo);
            var quadro = ComparadorAlternativas.Quadro(cmp);
            TabelasDesenho.Inserir(s, quadro, new Point3d(base0.X - cmp.Zona, base0.Y - cmp.Zona - 30, 0), 1.6, Layer);
            s.Tr.Commit();

            Ctx.Msg($"{c.Nome}: {c.Bracos.Count} braços ({string.Join(", ", c.Bracos.Select(b => b.Rumo + (b.Principal ? "*" : "")))}; * = principal); braços medidos até {cmp.Zona:0} m do centro.");
            foreach (var a in cmp.Alternativas.OrderBy(a => a.Aplicavel ? a.Posicao : 99))
            {
                if (!a.Aplicavel) { Ctx.Msg($"  -  {a.Nome}: não se aplica ({a.Motivo})."); continue; }
                Ctx.Msg($"  {a.Posicao}ª {a.Nome}{(ReferenceEquals(a, cmp.Recomendada) ? " [RECOMENDADA]" : "")}: {a.Pontuacao:0} pontos; pavimento {a.AreaPavimento:N0} m², área adicional {a.AreaAdicional:N0} m², meio-fio {a.MeioFio:N0} m"
                        + (a.Corte.HasValue ? $", corte {a.Corte:N0} / aterro {a.Aterro:N0} m³" : "")
                        + (a.CustoCompleto ? $", R$ {a.Custo:N2}" : "")
                        + (cmp.ComVolumes ? $"; x {CalculoCapacidade.TxtX(a.XMax)}, NS {a.PiorNivel}" : "")
                        + $"; {a.Conflitos} pontos de conflito ({a.Cruzamento} de cruzamento); {a.Situacao}.");
                foreach (var av in a.Avisos) Ctx.Msg("     AVISO: " + av);
            }
            foreach (var av in cmp.Avisos) Ctx.Msg("AVISO: " + av);
            Ctx.Msg($"Desenho: alternativas lado a lado a partir de E {base0.X:0.00} N {base0.Y:0.00} (camadas {Layer}*), com o quadro comparativo embaixo.");

            if (s.PedirPalavra("Gravar o relatório de decisão em Excel", new[] { "Sim", "Nao" }, "Sim") == "Sim")
            {
                var arq = UiUtil.SalvarArquivo("Planilha do Excel (*.xlsx)|*.xlsx", "Alternativas_" + Seguro(c.Nome) + ".xlsx");
                if (arq is not null)
                {
                    new RelatorioAlternativas { Comparacao = cmp, Desenho = s.Doc.Name, Superficie = sup is null ? "" : sups[ks - 1].Nome }.Salvar(arq);
                    Ctx.Msg($"Planilha gravada: {arq}");
                    if (s.PedirPalavra("Abrir a planilha", new[] { "Sim", "Nao" }, "Sim") == "Sim") UiUtil.AbrirNoSistema(arq);
                }
            }
        }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJALTERNATIVAS: {ex.Message}"); }
    }

    /// <summary>Maior X do que já está desenhado perto do cruzamento (a até <paramref name="raio"/> m dele), para as alternativas não caírem em cima.</summary>
    private static double LadoDireito(Sessao s, double ox, double oy, double raio)
    {
        double x = ox;
        foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
        {
            if (s.Tr.GetObject(id, OpenMode.ForRead) is not Entity e || !e.Visible) continue;
            try
            {
                var ext = e.GeometricExtents;
                if (ext.MinPoint.X > ox + raio || ext.MaxPoint.X < ox - raio || ext.MinPoint.Y > oy + raio || ext.MaxPoint.Y < oy - raio) continue;
                x = Math.Max(x, Math.Min(ext.MaxPoint.X, ox + raio));
            }
            catch { }
        }
        return x;
    }

    private static double Norm(double a) { while (a > Math.PI) a -= 2 * Math.PI; while (a <= -Math.PI) a += 2 * Math.PI; return a; }
    private static string Seguro(string n) => string.Concat((n.Length > 0 ? n : "cruzamento").Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));

    /// <summary>Cenário a partir da interseção ou rotatória guardada pelo PRJINTERSECAO / PRJROTATORIA (com os volumes do PRJCAPACIDADE).</summary>
    private static CenarioAlternativas DoItem(ItemSinalizacao it)
    {
        var c = new CenarioAlternativas { Nome = it.Nome, Origem = it.Tipo };
        if (it.Intersecao is { } m)
        {
            c.Ox = m.Ox; c.Oy = m.Oy;
            foreach (var b in m.Bracos)
            {
                var (dx, dy) = b.Polilinha().Direcao(0);
                c.Bracos.Add(new BracoAlternativa { Nome = b.Nome, Rumo = b.Rumo, Angulo = Math.Atan2(dy, dx), MeiaLargura = b.MeiaLargura, Principal = b.Principal });
            }
        }
        else if (it.Rotatoria is { } r)
        {
            c.Ox = r.Ox; c.Oy = r.Oy;
            foreach (var b in r.Bracos)
                c.Bracos.Add(new BracoAlternativa { Nome = b.Nome, Rumo = b.Rumo, Angulo = b.Angulo, MeiaLargura = b.BordoNormal > 0 ? b.BordoNormal : 3.5 });
        }
        if (it.Capacidade is { } d)
            for (int o = 0; o < c.Bracos.Count; o++)
                for (int q = 0; q < c.Bracos.Count; q++)
                    if (d.Movimentos.TryGetValue(DadosCapacidade.Chave(c.Bracos[o].Rumo, c.Bracos[q].Rumo), out var v) && v > 0) c.Volumes[(o, q)] = v;
        return c;
    }

    /// <summary>Cenário a partir dos eixos selecionados (polilinhas, linhas, arcos ou alinhamentos): o centro é o cruzamento deles.</summary>
    private static CenarioAlternativas? DosEixos(Sessao s)
    {
        var ps = Ctx.Ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione os eixos das vias que se cruzam (polilinhas, linhas, arcos ou alinhamentos): " },
                                     new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE,LINE,ARC,AECC_ALIGNMENT") }));
        if (ps.Status != PromptStatus.OK) return null;
        var linhas = new List<(string Nome, PolilinhaEixo Eixo)>();
        foreach (var id in ps.Value.GetObjectIds())
        {
            var e = s.Tr.GetObject(id, OpenMode.ForRead);
            List<(double X, double Y)>? pts = null;
            string nome = "";
            if (e is Alignment al) { pts = GeoCivil.PontosAlinhamento(al, 1).Select(q => (q.X, q.Y)).ToList(); nome = al.Name; }
            else if (e is Curve cv && GeoCivil.PontosCurva(cv, 1) is { } pc) { pts = pc.Pontos.Select(q => (q.X, q.Y)).ToList(); nome = $"{cv.Layer} ({cv.Handle})"; }
            if (pts is { Count: >= 2 }) linhas.Add((nome, new PolilinhaEixo(pts)));
        }
        if (linhas.Count < 1) { Ctx.Msg("Nenhum eixo válido na seleção."); return null; }

        // centro: média dos cruzamentos entre os eixos
        var cruz = new List<V2>();
        for (int a = 0; a < linhas.Count; a++)
            for (int b = a + 1; b < linhas.Count; b++)
            {
                var pa = linhas[a].Eixo.Pontos; var pb = linhas[b].Eixo.Pontos;
                for (int i = 0; i + 1 < pa.Count; i++)
                    for (int j = 0; j + 1 < pb.Count; j++)
                        if (Geo.IntersecaoSegmentos(new V2(pa[i].X, pa[i].Y), new V2(pa[i + 1].X, pa[i + 1].Y), new V2(pb[j].X, pb[j].Y), new V2(pb[j + 1].X, pb[j + 1].Y), out var q)) cruz.Add(q);
            }
        V2 o;
        if (cruz.Count > 0 && cruz.Max(q => q.Dist(cruz[0])) < 15) o = new V2(cruz.Average(q => q.X), cruz.Average(q => q.Y));
        else
        {
            var pp = Ctx.Ed.GetPoint("\nOs eixos não se cruzam num ponto só: clique o centro do cruzamento: ");
            if (pp.Status != PromptStatus.OK) return null;
            var w = pp.Value.TransformBy(Ctx.Ed.CurrentUserCoordinateSystem);
            o = new V2(w.X, w.Y);
        }

        // a via principal é escolhida depois, pelos pares de braços (o mais alinhado primeiro)
        var c = new CenarioAlternativas { Nome = "Cruzamento", Origem = $"{linhas.Count} eixo(s) selecionado(s)", Ox = o.X, Oy = o.Y };
        for (int k = 0; k < linhas.Count; k++)
        {
            var (nome, e) = linhas[k];
            var (sc, vc) = e.Projetar(o.X, o.Y);
            if (Math.Abs(vc) > 5) { Ctx.Msg($"O eixo '{nome}' passa a {Math.Abs(vc):0.0} m do centro: ficou de fora."); continue; }
            var w = s.PedirNumero($"Meia largura da pista de '{nome}' (eixo ao bordo, m)", 3.5);
            if (w is null) return null;
            foreach (int sentido in new[] { 1, -1 })
            {
                double sf = sc + sentido * Math.Min(15, sentido > 0 ? e.Comprimento - sc : sc);
                if (Math.Abs(sf - sc) < 3) continue;
                var q = e.Ponto(sf, 0);
                double ang = Math.Atan2(q.Y - o.Y, q.X - o.X);
                c.Bracos.Add(new BracoAlternativa { Nome = nome, Angulo = ang, MeiaLargura = w.Value });
            }
        }
        foreach (var g in c.Bracos.GroupBy(b => GeometriaRotatoria.Rumo(b.Angulo)))
        {
            int i = 0;
            foreach (var b in g) b.Rumo = g.Count() > 1 ? $"{g.Key}{++i}" : g.Key;
        }
        return c;
    }

    /// <summary>Volumes por aproximação e porcentagens de giro (o resto segue em frente).</summary>
    private static bool DigitarVolumes(Sessao s, CenarioAlternativas c)
    {
        int n = c.Bracos.Count;
        var eixos = c.Bracos.Select(b => new PolilinhaEixo(new List<(double X, double Y)> { (c.Ox, c.Oy), (c.Ox + 100 * Math.Cos(b.Angulo), c.Oy + 100 * Math.Sin(b.Angulo)) })).ToList();
        Ctx.Msg("Informe a hora de pico de cada aproximação (veíc/h) e as porcentagens de giro; sem contagem, os valores são só ponto de partida [PREMISSA].");
        for (int o = 0; o < n; o++)
        {
            var b = c.Bracos[o];
            var v = s.PedirNumero($"Volume que chega pelo braço {b.Rumo} ('{b.Nome}'), veíc/h", b.Principal ? 400 : 120);
            if (v is null) return false;
            if (v.Value <= 0) continue;
            var pe = s.PedirNumero($"  % que vira à esquerda", 15);
            if (pe is null) return false;
            var pd = s.PedirNumero($"  % que vira à direita", 15);
            if (pd is null) return false;
            var tipos = Enumerable.Range(0, n).Where(d => d != o).Select(d => (d, t: CalculoCapacidade.TipoMovimento(eixos[o], eixos[d]))).ToList();
            var parte = new Dictionary<string, double> { ["Esquerda"] = pe.Value, ["Direita"] = pd.Value, ["Frente"] = Math.Max(0, 100 - pe.Value - pd.Value) };
            double soma = tipos.Select(x => x.t).Distinct().Sum(t => parte.TryGetValue(t, out var q) ? q : 0);
            if (soma <= 0) continue;
            foreach (var g in tipos.GroupBy(x => x.t))
            {
                double frac = (parte.TryGetValue(g.Key, out var q) ? q : 0) / soma / g.Count();
                foreach (var (d, _) in g) if (frac > 0) c.Volumes[(o, d)] = v.Value * frac;
            }
        }
        return true;
    }

    private static IEnumerable<string> Camadas => new[] { Layer, LayerPav, LayerIlha, LayerTransp, LayerEixo };

    private static int ContarNasCamadas(Sessao s)
    {
        int n = 0;
        foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
            if (s.Tr.GetObject(id, OpenMode.ForRead) is Entity e && Camadas.Any(c => string.Equals(e.Layer, c, StringComparison.OrdinalIgnoreCase))) n++;
        return n;
    }

    private static void ApagarNasCamadas(Sessao s)
    {
        foreach (ObjectId id in (BlockTableRecord)s.Tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(s.Db), OpenMode.ForRead))
            if (s.Tr.GetObject(id, OpenMode.ForRead) is Entity e && Camadas.Any(c => string.Equals(e.Layer, c, StringComparison.OrdinalIgnoreCase)))
            {
                e.UpgradeOpen();
                e.Erase();
            }
    }

    private static void Desenhar(Sessao s, ComparacaoAlternativas cmp, V2 base0, double passo)
    {
        var ms = Ctx.EspacoModelo(s.Tr, s.Db);
        string lay = Ctx.GarantirLayer(s.Tr, s.Db, Layer, 7), layP = Ctx.GarantirLayer(s.Tr, s.Db, LayerPav, 8), layI = Ctx.GarantirLayer(s.Tr, s.Db, LayerIlha, 3),
               layT = Ctx.GarantirLayer(s.Tr, s.Db, LayerTransp, 41), layE = Ctx.GarantirLayer(s.Tr, s.Db, LayerEixo, 1);
        var o = new V2(cmp.Cenario.Ox, cmp.Cenario.Oy);
        void Contorno(List<V2> anel, V2 d, string l)
        {
            var pl = new Polyline();
            for (int i = 0; i < anel.Count; i++) pl.AddVertexAt(i, new Point2d(anel[i].X + d.X, anel[i].Y + d.Y), 0, 0, 0);
            pl.Closed = true;
            pl.SetDatabaseDefaults();
            pl.Layer = l;
            Ctx.Adicionar(s.Tr, ms, pl);
        }
        void Hachura(List<List<V2>> aneis, V2 d, string l, string padrao, short cor, double escala)
        {
            if (aneis.Count == 0) return;
            var h = new Hatch();
            h.SetDatabaseDefaults();
            h.Layer = l;
            h.ColorIndex = cor;
            Ctx.Adicionar(s.Tr, ms, h);
            h.SetHatchPattern(HatchPatternType.PreDefined, padrao);
            if (padrao != "SOLID") h.PatternScale = escala;
            h.Associative = false;
            h.HatchStyle = HatchStyle.Normal;
            foreach (var a in aneis.Where(a => a.Count >= 3))
            {
                var vs = new Point2dCollection();
                var bs = new DoubleCollection();
                foreach (var q in a) { vs.Add(new Point2d(q.X + d.X, q.Y + d.Y)); bs.Add(0); }
                vs.Add(new Point2d(a[0].X + d.X, a[0].Y + d.Y)); bs.Add(0);
                h.AppendLoop(HatchLoopTypes.Polyline, vs, bs);
            }
            try { h.EvaluateHatch(true); } catch { }
        }
        void Tx(string t, V2 q, double alt, short cor, AttachmentPoint ap)
        {
            var m = Ctx.Texto(t, new Point3d(q.X, q.Y, 0), alt, 0, ap);
            m.Layer = lay;
            m.ColorIndex = cor;
            Ctx.Adicionar(s.Tr, ms, m);
        }
        int k = 0;
        foreach (var a in cmp.Alternativas)
        {
            var centro = new V2(base0.X + k * passo, base0.Y);
            var d = centro - o;
            k++;
            if (!a.Aplicavel)
            {
                Tx($"{a.Nome}\\Pnão se aplica: {a.Motivo}", centro, 2.0, 8, AttachmentPoint.MiddleCenter);
                continue;
            }
            Hachura(a.Pavimento, d, layP, "SOLID", 253, 1);
            Hachura(a.Transponivel, d, layT, "ANSI31", 41, 0.5);
            Hachura(a.Ilhas, d, layI, "SOLID", 82, 1);
            foreach (var x in a.Pavimento) Contorno(x, d, layP);
            foreach (var x in a.Transponivel) Contorno(x, d, layT);
            foreach (var x in a.Ilhas) Contorno(x, d, layI);
            foreach (var b in cmp.Cenario.Bracos)
            {
                var q = centro + V2.Polar(b.Angulo, cmp.Zona);
                Ctx.Adicionar(s.Tr, ms, new Line(new Point3d(centro.X, centro.Y, 0), new Point3d(q.X, q.Y, 0)) { Layer = layE });
            }
            bool rec = ReferenceEquals(a, cmp.Recomendada);
            Tx($"{a.Posicao}ª · {a.Nome.ToUpperInvariant()}{(rec ? " (RECOMENDADA)" : "")}\\P{a.Pontuacao:0} pontos · {a.Situacao}", centro + new V2(0, cmp.Zona + 9), 3.0, rec ? (short)3 : (short)7, AttachmentPoint.BottomCenter);
            var l = new List<string>
            {
                (a.Dci > 0 ? $"DCI {a.Dci:0.#} m · " : "") + $"pavimento {a.AreaPavimento:N0} m² · ilhas {a.AreaIlhas:N0} m² · transponível {a.AreaTransponivel:N0} m²",
                $"meio-fio {a.MeioFio:N0} m · área ocupada {a.AreaOcupada:N0} m² · adicional {a.AreaAdicional:N0} m²",
            };
            if (a.Corte.HasValue) l.Add($"terraplenagem indicativa: corte {a.Corte:N0} m³ · aterro {a.Aterro:N0} m³");
            l.Add(a.CustoCompleto ? $"custo R$ {a.Custo:N0}" : "custo: sem tabela de preços");
            if (cmp.ComVolumes) l.Add($"x máximo {CalculoCapacidade.TxtX(a.XMax)} · atraso médio {CalculoCapacidade.TxtAtraso(a.AtrasoMedio)} s · pior NS {a.PiorNivel}");
            l.Add($"pontos de conflito {a.Conflitos} ({a.Cruzamento} de cruzamento)");
            Tx(string.Join("\\P", l), centro + new V2(0, -cmp.Zona - 4), 1.8, 7, AttachmentPoint.TopCenter);
        }
    }

    private static string Resumo(ParametrosAlternativas p)
    {
        int precos = p.Precos.Count(x => x.Preco.HasValue);
        return $"Alternativas: braços até {p.Zona:0} m; PARE com raio {p.RaioEsquina:0.#} m; mini DCI {p.DciMini:0.#} m; rotatórias DCI {p.Dci1:0.#} e {p.Dci2:0.#} m; " +
               $"pesos área {p.PesoArea:0} / terraplenagem {p.PesoTerraplenagem:0} / custo {p.PesoCusto:0} / capacidade {p.PesoCapacidade:0} / conflitos {p.PesoConflitos:0}" +
               (p.ParametrosDoUsuario ? "" : " [PREMISSA; troque em Parametros]") + $"; preços: {precos} de {ParametrosAlternativas.PrecosPadrao().Count} itens.";
    }

    private static bool PedirParametros(Sessao s, ParametrosAlternativas p)
    {
        var g = s.PedirPalavra("Parâmetros", new[] { "Geometria", "Pavimento", "Pesos" }, "Geometria");
        if (g is null) return false;
        double? N(string msg, double v) => s.PedirNumero(msg, v);
        if (g == "Geometria")
        {
            var z = N("Comprimento dos braços considerado (m)", p.Zona); if (z is null) return false;
            var pa = N("Passeio além do bordo, da via existente (m)", p.Passeio); if (pa is null) return false;
            var re = N("Raio de meio-fio do PARE simples (m)", p.RaioEsquina); if (re is null) return false;
            var rc = N("Raio externo do giro à direita canalizado (m)", p.RaioCanalizada); if (rc is null) return false;
            var dm = N("DCI da mini-rotatória (m)", p.DciMini); if (dm is null) return false;
            var d1 = N("DCI da rotatória de 1 faixa (m)", p.Dci1); if (d1 is null) return false;
            var a1 = N("Faixa transponível da rotatória de 1 faixa (m)", p.Apron1); if (a1 is null) return false;
            var d2 = N("DCI da rotatória de 2 faixas (m)", p.Dci2); if (d2 is null) return false;
            p.Zona = Math.Clamp(z.Value, 30, 300); p.Passeio = Math.Clamp(pa.Value, 0, 20); p.RaioEsquina = Math.Clamp(re.Value, 3, 60); p.RaioCanalizada = Math.Clamp(rc.Value, 10, 80);
            p.DciMini = Math.Clamp(dm.Value, 10, 40); p.Dci1 = Math.Clamp(d1.Value, 20, 90); p.Apron1 = Math.Clamp(a1.Value, 0, 8); p.Dci2 = Math.Clamp(d2.Value, 30, 120);
        }
        else if (g == "Pavimento")
        {
            var r = N("Espessura do revestimento (m)", p.Revestimento); if (r is null) return false;
            var b = N("Espessura da base (m)", p.Base); if (b is null) return false;
            var sb = N("Espessura da sub-base (m)", p.SubBase); if (sb is null) return false;
            p.Revestimento = Math.Clamp(r.Value, 0, 0.5); p.Base = Math.Clamp(b.Value, 0, 1); p.SubBase = Math.Clamp(sb.Value, 0, 1);
        }
        else
        {
            var a = N("Peso da área ocupada", p.PesoArea); if (a is null) return false;
            var t = N("Peso da terraplenagem", p.PesoTerraplenagem); if (t is null) return false;
            var c = N("Peso do custo", p.PesoCusto); if (c is null) return false;
            var k = N("Peso da capacidade", p.PesoCapacidade); if (k is null) return false;
            var f = N("Peso dos pontos de conflito", p.PesoConflitos); if (f is null) return false;
            p.PesoArea = Math.Max(0, a.Value); p.PesoTerraplenagem = Math.Max(0, t.Value); p.PesoCusto = Math.Max(0, c.Value); p.PesoCapacidade = Math.Max(0, k.Value); p.PesoConflitos = Math.Max(0, f.Value);
        }
        p.ParametrosDoUsuario = true;
        return true;
    }

    private static bool Precos(Sessao s, ParametrosAlternativas p)
    {
        var r = s.PedirPalavra("Tabela de preços (CSV item;descrição;unidade;preço;fonte)", new[] { "Modelo", "Importar", "Limpar" }, "Modelo");
        if (r is null) return false;
        if (r == "Modelo")
        {
            var arq = UiUtil.SalvarArquivo("CSV separado por ponto e vírgula (*.csv)|*.csv", "precos_alternativas.csv");
            if (arq is null) return true;
            File.WriteAllText(arq, RelatorioAlternativas.ModeloPrecos(p), new System.Text.UTF8Encoding(true));
            Ctx.Msg($"Modelo gravado: {arq}. Preencha o preço e a fonte (SICRO ou SINAPI, estado e data-base) e importe em Precos > Importar.");
        }
        else if (r == "Importar")
        {
            var arq = UiUtil.AbrirArquivo("CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*");
            if (arq is null) return true;
            var lidos = RelatorioAlternativas.LerPrecos(arq);
            int n = 0;
            foreach (var x in lidos)
            {
                var atual = p.Precos.FirstOrDefault(y => string.Equals(y.Codigo, x.Codigo, StringComparison.OrdinalIgnoreCase));
                if (atual is null) { p.Precos.Add(x); continue; }
                atual.Preco = x.Preco; atual.Fonte = x.Fonte;
                if (x.Descricao.Length > 0) atual.Descricao = x.Descricao;
                if (x.Unidade.Length > 0) atual.Unidade = x.Unidade;
                n++;
            }
            p.PrecosDoUsuario = true;
            Ctx.Msg($"{lidos.Count} linha(s) lida(s), {n} item(ns) da tabela atualizados; {p.Precos.Count(x => x.Preco.HasValue)} com preço.");
            var falta = ParametrosAlternativas.PrecosPadrao().Where(d => !p.Preco(d.Codigo).Preco.HasValue).Select(d => d.Codigo).ToList();
            if (falta.Count > 0) Ctx.Msg("Sem preço: " + string.Join(", ", falta) + " (o custo fica fora da comparação até completar).");
        }
        else { p.Precos = ParametrosAlternativas.PrecosPadrao(); p.PrecosDoUsuario = false; Ctx.Msg("Tabela de preços limpa."); }
        return true;
    }
}
