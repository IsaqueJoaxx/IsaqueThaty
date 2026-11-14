using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Modelador;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Desenho;

/// <summary>Gera e atualiza a representação gráfica das redes (planta, rótulos e sólidos 3D).</summary>
internal static class DesenhoRede
{
    private const string ChaveRotacao = "__rotacaoDesenhada";
    private const string ChaveX = "__xDesenhado";
    private const string ChaveY = "__yDesenhado";

    public static string Layer(Rede r, string sufixo) => $"{r.PrefixoLayer}-{sufixo}";

    private static string SufixoNo(TipoNo t) => t switch
    {
        TipoNo.BocaDeLobo => "BL",
        TipoNo.Exutorio => "EXU",
        TipoNo.Caixa => "CX",
        TipoNo.Reservatorio or TipoNo.Tanque => "RES",
        TipoNo.Juncao => "NO",
        _ => "PV",
    };

    public static void GarantirLayers(Sessao s, Rede r)
    {
        var tr = s.Tr; var db = s.Db;
        Ctx.GarantirLayer(tr, db, Layer(r, "PV"), 4);
        Ctx.GarantirLayer(tr, db, Layer(r, "BL"), 30);
        Ctx.GarantirLayer(tr, db, Layer(r, "CX"), 4);
        Ctx.GarantirLayer(tr, db, Layer(r, "EXU"), 6);
        Ctx.GarantirLayer(tr, db, Layer(r, "NO"), 4);
        Ctx.GarantirLayer(tr, db, Layer(r, "RES"), 6);
        Ctx.GarantirLayer(tr, db, Layer(r, "TUBO"), r.Tipo == TipoRede.Esgoto ? (short)32 : r.Tipo == TipoRede.Agua ? (short)140 : (short)5, LineWeight.LineWeight035);
        Ctx.GarantirLayer(tr, db, Layer(r, "SETA"), 5);
        Ctx.GarantirLayer(tr, db, Layer(r, "TEXTO"), 7);
        Ctx.GarantirLayer(tr, db, Layer(r, "BACIA"), 3);
        Ctx.GarantirLayer(tr, db, Layer(r, "3D"), 8);
    }

    /// <summary>Lê de volta posições/rotações editadas no desenho e remove do modelo o que foi apagado.</summary>
    public static List<Mensagem> Sincronizar(Sessao s, Rede rede)
    {
        var msgs = new List<Mensagem>();
        foreach (var n in rede.Nos.ToList())
        {
            if (string.IsNullOrEmpty(n.HandleBloco)) continue;
            var br = Ctx.Abrir<BlockReference>(s.Tr, s.Db, n.HandleBloco);
            if (br is null)
            {
                rede.RemoverNo(n.Id);
                msgs.Add(new(Gravidade.Aviso, "Bloco apagado no desenho — nó removido da rede", n.Nome));
                continue;
            }
            if (Math.Abs(br.Position.X - n.X) > 1e-4 || Math.Abs(br.Position.Y - n.Y) > 1e-4)
            {
                n.X = br.Position.X; n.Y = br.Position.Y;
                msgs.Add(new(Gravidade.Info, "Posição atualizada a partir do desenho", n.Nome));
            }
            double rotDesenho = br.Rotation * 180 / Math.PI;
            if (n.ParametrosDispositivo.TryGetValue(ChaveRotacao, out var rotAnterior) && Math.Abs(DifAngulo(rotDesenho, rotAnterior)) > 0.01)
                n.Rotacao = rotDesenho;
        }
        foreach (var t in rede.Trechos.ToList())
        {
            if (string.IsNullOrEmpty(t.HandleLinha)) continue;
            if (Ctx.IdDeHandle(s.Db, t.HandleLinha).IsNull)
            {
                rede.Trechos.Remove(t);
                Ctx.Apagar(s.Tr, s.Db, t.HandleRotulo); Ctx.Apagar(s.Tr, s.Db, t.HandleSolido);
                msgs.Add(new(Gravidade.Aviso, "Linha apagada no desenho — trecho removido da rede", t.Nome));
            }
        }
        foreach (var b in rede.Bacias.ToList())
        {
            if (string.IsNullOrEmpty(b.HandleContorno)) continue;
            var pl = Ctx.Abrir<Curve>(s.Tr, s.Db, b.HandleContorno);
            if (pl is null)
            {
                rede.Bacias.Remove(b);
                Ctx.Apagar(s.Tr, s.Db, b.HandleRotulo);
                msgs.Add(new(Gravidade.Aviso, "Contorno apagado — bacia removida", b.Nome));
                continue;
            }
            b.AreaHa = Math.Abs(pl.Area) / 10000.0;
            b.Contorno = PontosCurva(pl);
        }
        return msgs;
    }

    private static double DifAngulo(double a, double b)
    {
        double d = (a - b) % 360;
        if (d > 180) d -= 360; if (d < -180) d += 360;
        return d;
    }

    public static List<P2> PontosCurva(Curve c)
    {
        var l = new List<P2>();
        if (c is Polyline pl)
        {
            for (int i = 0; i < pl.NumberOfVertices; i++) { var p = pl.GetPoint2dAt(i); l.Add(new P2(p.X, p.Y)); }
            return l;
        }
        int n = 64;
        for (int i = 0; i < n; i++)
        {
            var p = c.GetPointAtParameter(c.StartParam + (c.EndParam - c.StartParam) * i / n);
            l.Add(new P2(p.X, p.Y));
        }
        return l;
    }

    // ------------------------------------------------------------------------------------------

    public static void ApagarGraficos(Sessao s, Rede rede, bool incluirBacias = false)
    {
        foreach (var n in rede.Nos)
        {
            Ctx.Apagar(s.Tr, s.Db, n.HandleBloco); Ctx.Apagar(s.Tr, s.Db, n.HandleRotulo); Ctx.Apagar(s.Tr, s.Db, n.HandleSolido);
            n.HandleBloco = n.HandleRotulo = n.HandleSolido = null;
        }
        foreach (var t in rede.Trechos)
        {
            Ctx.Apagar(s.Tr, s.Db, t.HandleLinha); Ctx.Apagar(s.Tr, s.Db, t.HandleRotulo); Ctx.Apagar(s.Tr, s.Db, t.HandleSolido);
            t.HandleLinha = t.HandleRotulo = t.HandleSolido = null;
        }
        foreach (var b in rede.Bacias)
        {
            Ctx.Apagar(s.Tr, s.Db, b.HandleRotulo); b.HandleRotulo = null;
            if (incluirBacias) { Ctx.Apagar(s.Tr, s.Db, b.HandleContorno); b.HandleContorno = null; }
        }
        if (rede.HandlesAuxiliares.TryGetValue("setas", out var setas))
        {
            foreach (var h in setas) Ctx.Apagar(s.Tr, s.Db, h);
            setas.Clear();
        }
    }

    /// <summary>Redesenha toda a rede: blocos dos nós (planta + 3D), linhas e sólidos dos tubos, rótulos.</summary>
    public static List<string> Redesenhar(Sessao s, Rede rede)
    {
        var erros = new List<string>();
        var tr = s.Tr; var db = s.Db;
        GarantirLayers(s, rede);
        var ms = Ctx.EspacoModelo(tr, db);
        var res = rede.Resultado;
        double h = rede.AlturaTexto;
        var bib = Sessao.Bib;
        var top = new Topologia(rede);

        // remove setas antigas
        if (!rede.HandlesAuxiliares.TryGetValue("setas", out var setas)) rede.HandlesAuxiliares["setas"] = setas = new();
        foreach (var hs in setas) Ctx.Apagar(tr, db, hs);
        setas.Clear();

        // ---------- nós
        var raioInterno = new Dictionary<string, double>();
        foreach (var n in rede.Nos)
        {
            var rn = res?.No(n.Id);
            double tampa = double.IsNaN(n.CotaTampa) ? 0 : n.CotaTampa;
            double fundo = rn?.CotaFundo ?? (n.CotaFundoForcada ?? tampa - 1.5);
            double prof = Math.Max(0.3, tampa - fundo);
            if (rede.Tipo == TipoRede.Agua) { fundo = tampa - 1.0; prof = 1.0; }

            if (!bib.Dispositivos.TryGetValue(n.DispositivoId, out var def))
            {
                erros.Add($"{n.Nome}: dispositivo '{n.DispositivoId}' não existe na biblioteca — usando MARCADOR");
                def = bib.Dispositivos.GetValueOrDefault("MARCADOR") ?? new DefinicaoDispositivo { Id = "VAZIO" };
            }
            var valores = new Dictionary<string, double>(n.ParametrosDispositivo.Where(kv => !kv.Key.StartsWith("__")), StringComparer.OrdinalIgnoreCase)
            {
                ["Profundidade"] = prof,
                ["CotaFundo"] = fundo,
                ["CotaTampa"] = tampa,
            };
            if (!n.ParametrosDispositivo.ContainsKey("DiametroMaximo"))
                valores["DiametroMaximo"] = rn is not null && rn.MaiorSecao > 0 ? rn.MaiorSecao : TrechosDoNo(rede, n).Select(t => t.DiametroForcado ?? 0).DefaultIfEmpty(0.4).Max();
            var mod = AvaliadorDispositivo.Avaliar(def, valores);
            foreach (var m in mod.Mensagens.Where(m => m.Gravidade == Gravidade.Erro)) erros.Add($"{n.Nome}: {m.Texto}");
            raioInterno[n.Id] = double.IsNaN(mod.RaioFuros) ? 0 : mod.RaioFuros;

            // rotação: definida pelo usuário ou alinhada com o tubo
            double rot = n.Rotacao * Math.PI / 180;
            if (Math.Abs(n.Rotacao) < 1e-9 && def.AlinharComTubo)
            {
                var sai = top.Saem(n.Id).FirstOrDefault();
                var chega = top.Chegam(n.Id).FirstOrDefault();
                No? outro = sai is not null ? rede.NoPorId(sai.NoJusanteId) : chega is not null ? rede.NoPorId(chega.NoMontanteId) : null;
                if (outro is not null)
                    rot = sai is not null ? Math.Atan2(outro.Y - n.Y, outro.X - n.X) : Math.Atan2(n.Y - outro.Y, n.X - outro.X);
            }
            n.ParametrosDispositivo[ChaveRotacao] = rot * 180 / Math.PI;

            // furos para os tubos
            var furos = new List<Solido>();
            if (rede.AbrirFurosNosPVs && rede.Gerar3D && res is not null && def.Categoria != "Generico")
            {
                foreach (var t in TrechosDoNo(rede, n))
                {
                    var rt = res.Trecho(t.Id);
                    if (rt is null || rt.Diametro <= 0) continue;
                    bool montante = t.NoMontanteId == n.Id;
                    var outro = rede.NoPorId(montante ? t.NoJusanteId : t.NoMontanteId);
                    if (outro is null) continue;
                    double ang = Math.Atan2(outro.Y - n.Y, outro.X - n.X) - rot;
                    double zInv = (montante ? rt.CotaFundoMontante : rt.CotaFundoJusante) - fundo;
                    double r0 = Math.Max(0, raioInterno[n.Id] - 0.01);
                    double comp = 1.2;
                    var dir = new P3(Math.Cos(ang), Math.Sin(ang), 0);
                    if (rt.Forma == FormaSecao.Circular)
                        furos.Add(new SolCilindro(new P3(dir.X * r0, dir.Y * r0, zInv + rt.Altura / 2), dir, rt.Diametro / 2 + rt.Espessura, rt.Diametro / 2 + rt.Espessura, comp));
                    else
                    {
                        Solido cx = new SolCaixa(new P3(r0 + comp / 2, 0, zInv - rt.Espessura), comp, rt.Diametro * rt.Celulas + 2 * rt.Espessura, rt.Altura + 2 * rt.Espessura);
                        furos.Add(new SolTransformado(cx, Matriz.Rotacao(new P3(0, 0, 1), ang)));
                    }
                }
            }

            string nomeBloco = $"PRJ_{rede.Id}_{n.Id}";
            var btrId = DefinirBloco(s, nomeBloco, mod, prof, rede.Gerar3D, furos, Layer(rede, "3D"), erros, n.Nome);

            var pos = new Point3d(n.X, n.Y, rede.Gerar3D ? fundo : 0);
            BlockReference? br = Ctx.Abrir<BlockReference>(tr, db, n.HandleBloco, OpenMode.ForWrite);
            if (br is null || br.BlockTableRecord != btrId)
            {
                if (br is not null) br.Erase();
                br = new BlockReference(pos, btrId);
                br.SetDatabaseDefaults();
                Ctx.Adicionar(tr, ms, br);
                n.HandleBloco = Ctx.HandleDe(br.ObjectId);
            }
            br.Position = pos;
            br.Rotation = rot;
            br.Layer = Layer(rede, SufixoNo(n.Tipo));
            br.RecordGraphicsModified(true);

            // rótulo (se já existe, mantém a posição escolhida pelo usuário e acompanha o deslocamento do nó)
            string txt = rede.Tipo == TipoRede.Agua
                ? Ctx.MTextLinhas(n.Nome, $"Cota={Num.F(tampa, 3)}", n.Tipo == TipoNo.Reservatorio ? $"NA={Num.F(n.Carga, 2)}" : $"Q={Num.F(n.Demanda, 2)} L/s")
                : rn is null ? Ctx.MTextLinhas(n.Nome, $"CT={Num.F(tampa, 3)}")
                : Ctx.MTextLinhas(n.Nome, $"CT={Num.F(rn.CotaTampa, 3)}", $"CF={Num.F(rn.CotaFundo, 3)}", $"h={Num.F(rn.Profundidade, 2)}");
            double raioExt = mod.Planta.OfType<PlCirculo>().Select(c => c.Raio).DefaultIfEmpty(0.6).Max();
            var rotulo = Ctx.Abrir<MText>(tr, db, n.HandleRotulo, OpenMode.ForWrite);
            if (rotulo is not null)
            {
                double x0 = n.ParametrosDispositivo.GetValueOrDefault(ChaveX, n.X), y0 = n.ParametrosDispositivo.GetValueOrDefault(ChaveY, n.Y);
                rotulo.Contents = txt;
                rotulo.TextHeight = h;
                rotulo.Location = rotulo.Location + new Vector3d(n.X - x0, n.Y - y0, 0);
                rotulo.Layer = Layer(rede, "TEXTO");
            }
            else
            {
                Ctx.Apagar(tr, db, n.HandleRotulo);
                var mt = Ctx.Texto(txt, new Point3d(n.X + raioExt + 0.3 * h, n.Y + raioExt + 0.3 * h, 0), h);
                mt.Layer = Layer(rede, "TEXTO");
                Ctx.Adicionar(tr, ms, mt);
                n.HandleRotulo = Ctx.HandleDe(mt.ObjectId);
            }
            n.ParametrosDispositivo[ChaveX] = n.X;
            n.ParametrosDispositivo[ChaveY] = n.Y;
        }

        // ---------- trechos
        foreach (var t in rede.Trechos)
        {
            var a = rede.NoPorId(t.NoMontanteId); var b = rede.NoPorId(t.NoJusanteId);
            if (a is null || b is null) continue;
            var rt = res?.Trecho(t.Id);

            // linha em planta
            Polyline? pl = Ctx.Abrir<Polyline>(tr, db, t.HandleLinha, OpenMode.ForWrite);
            if (pl is null)
            {
                Ctx.Apagar(tr, db, t.HandleLinha);
                pl = new Polyline();
                pl.SetDatabaseDefaults();
                pl.AddVertexAt(0, new Point2d(a.X, a.Y), 0, 0, 0);
                pl.AddVertexAt(1, new Point2d(b.X, b.Y), 0, 0, 0);
                Ctx.Adicionar(tr, ms, pl);
                t.HandleLinha = Ctx.HandleDe(pl.ObjectId);
            }
            else
            {
                while (pl.NumberOfVertices > 2) pl.RemoveVertexAt(pl.NumberOfVertices - 1);
                pl.SetPointAt(0, new Point2d(a.X, a.Y));
                pl.SetPointAt(1, new Point2d(b.X, b.Y));
            }
            pl.Layer = Layer(rede, "TUBO");

            var d2 = new P2(b.X - a.X, b.Y - a.Y);
            double comp = d2.Comprimento;
            if (comp < 1e-6) continue;
            var u = d2.Unitario();
            var meio = new P2((a.X + b.X) / 2, (a.Y + b.Y) / 2);

            // seta de fluxo (redes de gravidade)
            if (rede.Tipo != TipoRede.Agua)
            {
                var ps = meio - u * (h * 0.6);
                var pe = meio + u * (h * 0.6);
                var seta = new Polyline();
                seta.SetDatabaseDefaults();
                seta.AddVertexAt(0, new Point2d(ps.X, ps.Y), 0, h * 0.5, 0);
                seta.AddVertexAt(1, new Point2d(pe.X, pe.Y), 0, 0, 0);
                seta.Layer = Layer(rede, "SETA");
                Ctx.Adicionar(tr, ms, seta);
                setas.Add(Ctx.HandleDe(seta.ObjectId));
            }

            // rótulo
            Ctx.Apagar(tr, db, t.HandleRotulo);
            string txt = rede.Tipo == TipoRede.Agua
                ? $"{t.Nome} DN{(t.DiametroForcado ?? rede.Agua.DiametroPadrao) * 1000:0} L={Num.F(rede.ComprimentoHorizontal(t))}"
                : rt is null ? $"{t.Nome} L={Num.F(comp)}"
                : $"{t.Nome} {rt.Secao} L={Num.F(rt.Comprimento)} i={Num.F(rt.Declividade * 100, 3)}%";
            double ang = Geo.AnguloLegivel(Math.Atan2(u.Y, u.X));
            var nrm = new P2(-Math.Sin(ang), Math.Cos(ang));
            var pt = meio + nrm * (h * 0.9);
            var mt = Ctx.Texto(Ctx.MTextLinhas(txt), new Point3d(pt.X, pt.Y, 0), h * 0.8, ang, AttachmentPoint.BottomCenter);
            mt.Layer = Layer(rede, "TEXTO");
            Ctx.Adicionar(tr, ms, mt);
            t.HandleRotulo = Ctx.HandleDe(mt.ObjectId);

            // sólido 3D
            Ctx.Apagar(tr, db, t.HandleSolido);
            t.HandleSolido = null;
            if (rede.Gerar3D && rt is not null && rt.Diametro > 0)
            {
                double ra = raioInterno.GetValueOrDefault(a.Id), rb = raioInterno.GetValueOrDefault(b.Id);
                if (ra + rb > comp - 0.05) { ra = rb = 0; }
                double s0 = ra / comp, s1 = 1 - rb / comp;
                double z0 = rt.CotaFundoMontante + (rt.CotaFundoJusante - rt.CotaFundoMontante) * s0;
                double z1 = rt.CotaFundoMontante + (rt.CotaFundoJusante - rt.CotaFundoMontante) * s1;
                var p0 = new Point3d(a.X + d2.X * s0, a.Y + d2.Y * s0, z0);
                var p1 = new Point3d(a.X + d2.X * s1, a.Y + d2.Y * s1, z1);
                var solidos = GeradorSolidos.Tubo(p0, p1, rt.Forma == FormaSecao.Circular, rt.Diametro, rt.Altura, rt.Espessura, rt.Celulas, erros);
                if (solidos.Count > 0)
                {
                    var principal = solidos[0];
                    for (int k = 1; k < solidos.Count; k++)
                    {
                        try { principal.BooleanOperation(BooleanOperationType.BoolUnite, solidos[k]); }
                        catch { }
                        solidos[k].Dispose();
                    }
                    principal.Layer = Layer(rede, "3D");
                    Ctx.Adicionar(tr, ms, principal);
                    t.HandleSolido = Ctx.HandleDe(principal.ObjectId);
                }
            }
        }

        // ---------- bacias
        foreach (var bac in rede.Bacias)
        {
            Ctx.Apagar(tr, db, bac.HandleRotulo);
            bac.HandleRotulo = null;
            var c = Ctx.Abrir<Curve>(tr, db, bac.HandleContorno, OpenMode.ForWrite);
            if (c is not null) c.Layer = Layer(rede, "BACIA");
            var pts = bac.Contorno.Count >= 3 ? bac.Contorno : null;
            P2 cen;
            if (pts is not null) cen = Geo.Centroide(pts);
            else
            {
                var no = rede.NoPorId(bac.NoId);
                if (no is null) continue;
                cen = new P2(no.X - 3 * h, no.Y - 3 * h);
            }
            var noLanc = rede.NoPorId(bac.NoId);
            var mt = Ctx.Texto(Ctx.MTextLinhas(bac.Nome, $"A={Num.F(bac.Area, 3)} ha  C={Num.F(bac.C, 2)}", $"tc={Num.F(bac.Tc(), 1)} min → {noLanc?.Nome ?? "?"}"),
                new Point3d(cen.X, cen.Y, 0), h * 0.9, 0, AttachmentPoint.MiddleCenter);
            mt.Layer = Layer(rede, "BACIA");
            Ctx.Adicionar(tr, ms, mt);
            bac.HandleRotulo = Ctx.HandleDe(mt.ObjectId);
        }
        return erros;
    }

    private static IEnumerable<Trecho> TrechosDoNo(Rede r, No n) => r.Trechos.Where(t => t.NoMontanteId == n.Id || t.NoJusanteId == n.Id);

    /// <summary>Cria ou redefine um bloco com o símbolo em planta (na cota da tampa) e os sólidos do dispositivo.</summary>
    public static ObjectId DefinirBloco(Sessao s, string nome, ModeloAvaliado mod, double cotaPlantaLocal, bool gerar3D,
        List<Solido>? furos, string layer3D, List<string> erros, string rotuloErro)
    {
        var tr = s.Tr; var db = s.Db;
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        BlockTableRecord btr;
        if (bt.Has(nome))
        {
            btr = (BlockTableRecord)tr.GetObject(bt[nome], OpenMode.ForWrite);
            foreach (ObjectId id in btr)
            {
                var e = tr.GetObject(id, OpenMode.ForWrite);
                e.Erase();
            }
        }
        else
        {
            bt.UpgradeOpen();
            btr = new BlockTableRecord { Name = nome, Origin = Point3d.Origin };
            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);
        }

        foreach (var el in mod.Planta)
        {
            Entity? e = el switch
            {
                PlCirculo c when c.Raio > 0 => new Circle(new Point3d(c.Centro.X, c.Centro.Y, cotaPlantaLocal), Vector3d.ZAxis, c.Raio),
                PlArco ar when ar.Raio > 0 => new Arc(new Point3d(ar.Centro.X, ar.Centro.Y, cotaPlantaLocal), ar.Raio, ar.DeGraus * Math.PI / 180, ar.AteGraus * Math.PI / 180),
                PlPolilinha p when p.Pontos.Count >= 2 => Poli(p.Pontos, p.Fechada, cotaPlantaLocal),
                PlTexto tx => Ctx.Texto(tx.Texto, new Point3d(tx.Posicao.X, tx.Posicao.Y, cotaPlantaLocal), tx.Altura, 0, AttachmentPoint.MiddleCenter),
                _ => null,
            };
            if (e is null) continue;
            e.Layer = "0";
            e.Color = el.Camada switch
            {
                "tampa" => Color.FromColorIndex(ColorMethod.ByAci, 2),
                "eixo" => Color.FromColorIndex(ColorMethod.ByAci, 8),
                _ => Color.FromColorIndex(ColorMethod.ByBlock, 0),
            };
            Ctx.Adicionar(tr, btr, e);
        }

        if (gerar3D)
        {
            foreach (var sol in mod.Saida)
            {
                var so = GeradorSolidos.Criar(sol, erros);
                if (so is null) continue;
                if (furos is not null)
                    foreach (var f in furos)
                    {
                        var fs = GeradorSolidos.Criar(f, erros);
                        if (fs is null) continue;
                        try { so.BooleanOperation(BooleanOperationType.BoolSubtract, fs); }
                        catch (System.Exception ex) { erros.Add($"{rotuloErro}: furo não aberto ({ex.Message})"); }
                        finally { fs.Dispose(); }
                    }
                so.Layer = layer3D;
                Ctx.Adicionar(tr, btr, so);
            }
        }
        return btr.ObjectId;
    }

    private static Polyline Poli(IReadOnlyList<P2> pts, bool fechada, double z)
    {
        var pl = new Polyline();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        pl.Closed = fechada;
        pl.Elevation = z;
        return pl;
    }
}
