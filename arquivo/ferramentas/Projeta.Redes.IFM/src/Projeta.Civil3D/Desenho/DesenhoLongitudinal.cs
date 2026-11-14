using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Longitudinais;
using Projeta.Core.Modelador;

namespace Projeta.Civil3D.Desenho;

/// <summary>Gera valetas, sarjetas, meio-fios e canaletas ao longo de alinhamentos.</summary>
internal static class DesenhoLongitudinal
{
    public const string LayerSolido = "PRJ-LONG-3D";
    public const string LayerPlanta = "PRJ-LONG";
    public const string LayerTexto = "PRJ-LONG-TEXTO";

    /// <summary>Amostra o eixo do dispositivo (estaca, XY no afastamento, cota) ao longo do alinhamento.</summary>
    public static List<AmostraLongitudinal> Amostrar(Sessao s, DispositivoLongitudinal d, List<string> erros)
    {
        var al = Ctx.Abrir<Alignment>(s.Tr, s.Db, d.HandleAlinhamento) ?? throw new InvalidOperationException("Alinhamento do dispositivo não encontrado.");
        Profile? perfil = d.OrigemCota == OrigemCota.Perfil ? Ctx.Abrir<Profile>(s.Tr, s.Db, d.HandlePerfil) : null;
        Surface? surf = d.OrigemCota == OrigemCota.Superficie ? Ctx.Abrir<Surface>(s.Tr, s.Db, d.HandleSuperficie) : null;
        if (d.OrigemCota == OrigemCota.Perfil && perfil is null) throw new InvalidOperationException("Perfil do dispositivo não encontrado.");
        if (d.OrigemCota == OrigemCota.Superficie && surf is null) throw new InvalidOperationException("Superfície do dispositivo não encontrada.");

        double ini = Math.Max(d.EstacaInicial, al.StartingStation), fim = Math.Min(d.EstacaFinal, al.EndingStation);
        if (fim - ini < 0.1) throw new InvalidOperationException("Intervalo de estacas vazio.");
        var estacas = new List<double>();
        double passo = Math.Max(0.5, d.Intervalo);
        for (double e = ini; e < fim - 1e-6; e += passo) estacas.Add(e);
        estacas.Add(fim);

        var lista = new List<AmostraLongitudinal>();
        foreach (var e in estacas)
        {
            if (!CivilUtil.PontoNaEstaca(al, e, d.Afastamento, out var x, out var y)) { erros.Add($"Estaca {Num.Estaca(e)} fora do alinhamento"); continue; }
            double z;
            if (perfil is not null)
            {
                try { z = perfil.ElevationAt(e); } catch { erros.Add($"Perfil sem cota na estaca {Num.Estaca(e)}"); continue; }
            }
            else z = CivilUtil.Cota(surf, x, y);
            if (double.IsNaN(z)) { erros.Add($"Sem cota na estaca {Num.Estaca(e)}"); continue; }
            // direção pelo eixo (diferença finita)
            double e1 = Math.Max(al.StartingStation, e - 0.25), e2 = Math.Min(al.EndingStation, e + 0.25);
            CivilUtil.PontoNaEstaca(al, e1, 0, out var xa, out var ya);
            CivilUtil.PontoNaEstaca(al, e2, 0, out var xb, out var yb);
            lista.Add(new AmostraLongitudinal(e, x, y, z + d.Desnivel, xb - xa, yb - ya));
        }
        return lista;
    }

    public static List<string> Gerar(Sessao s, DispositivoLongitudinal d)
    {
        var erros = new List<string>();
        var am = Amostrar(s, d, erros);
        if (am.Count < 2) { erros.Add("Menos de duas estacas válidas."); return erros; }
        erros.AddRange(GerarComAmostras(s, d, am));
        return erros;
    }

    /// <summary>Gera sólido, contornos, rótulo e cálculo a partir de amostras já calculadas (também usado pelo PRJDEMO).</summary>
    public static List<string> GerarComAmostras(Sessao s, DispositivoLongitudinal d, List<AmostraLongitudinal> am)
    {
        var erros = new List<string>();
        var tr = s.Tr; var db = s.Db;
        foreach (var hnd in d.HandlesDesenho) Ctx.Apagar(tr, db, hnd);
        d.HandlesDesenho.Clear();

        if (!Sessao.Bib.Secoes.TryGetValue(d.SecaoId, out var tipo)) { erros.Add($"Seção '{d.SecaoId}' não existe na biblioteca."); return erros; }
        var sec = AvaliadorDispositivo.AvaliarSecao(tipo, d.Parametros);
        erros.AddRange(sec.Mensagens.Where(m => m.Gravidade == Gravidade.Erro).Select(m => m.ToString()));
        if (sec.Concreto.Count < 3) { erros.Add("Seção sem polígono de concreto."); return erros; }

        Ctx.GarantirLayer(tr, db, LayerSolido, 8);
        Ctx.GarantirLayer(tr, db, LayerPlanta, 3);
        Ctx.GarantirLayer(tr, db, LayerTexto, 7);
        var ms = Ctx.EspacoModelo(tr, db);

        // ---------- sólido: loft regrado entre todas as seções; se a API recusar, monta trecho a trecho
        var secoes = new List<Entity>();
        foreach (var a in am) secoes.Add(Secao3d(sec.Concreto, a, d.Espelhar));
        var solidos = new List<Solid3d>();
        string? falhaLoft = null;
        try
        {
            var lb = new LoftOptionsBuilder { Ruled = true };
            var so = new Solid3d();
            so.SetDatabaseDefaults();
            try { so.CreateLoftedSolid(secoes.ToArray(), Array.Empty<Entity>(), null, lb.ToLoftOptions()); solidos.Add(so); }
            catch { so.Dispose(); throw; }
        }
        catch (System.Exception ex) { falhaLoft = ex.Message; }
        if (solidos.Count == 0)
        {
            int nLoft = 0, nExtr = 0, nFalha = 0;
            Solid3d? atual = null;
            for (int i = 0; i < am.Count - 1; i++)
            {
                Solid3d? seg = new Solid3d();
                seg.SetDatabaseDefaults();
                try { seg.CreateLoftedSolid(new[] { secoes[i], secoes[i + 1] }, Array.Empty<Entity>(), null, new LoftOptionsBuilder { Ruled = true }.ToLoftOptions()); nLoft++; }
                catch
                {
                    seg.Dispose(); seg = null;
                    try { seg = ExtrusaoTrecho(sec.Concreto, am[i], am[i + 1], d.Espelhar); nExtr++; }
                    catch { nFalha++; }
                }
                if (seg is null) continue;
                if (atual is null) { atual = seg; continue; }
                try { atual.BooleanOperation(BooleanOperationType.BoolUnite, seg); seg.Dispose(); }
                catch { solidos.Add(atual); atual = seg; } // mantém separado se a união falhar
            }
            if (atual is not null) solidos.Add(atual);
            Ctx.Msg($"[LONGITUDINAL] {d.Nome}: loft completo recusado pela API ({falhaLoft}); sólido montado por trechos ({nLoft} loft(s), {nExtr} extrusão(ões), {solidos.Count} sólido(s)).");
            if (nFalha > 0 || solidos.Count == 0)
                erros.Add($"Sólido incompleto: loft completo recusado ({falhaLoft}); {nLoft} loft(s), {nExtr} extrusão(ões) e {nFalha} falha(s) por trecho");
        }
        foreach (var e in secoes) e.Dispose();
        foreach (var so in solidos)
        {
            so.Layer = LayerSolido;
            Ctx.Adicionar(tr, ms, so);
            d.HandlesDesenho.Add(Ctx.HandleDe(so.ObjectId));
        }

        // ---------- contorno em planta (bordas externas) e eixo hidráulico
        double xmin = sec.Concreto.Min(p => p.X), xmax = sec.Concreto.Max(p => p.X);
        foreach (var off in new[] { xmin, xmax, 0.0 })
        {
            var pl = new Polyline(); pl.SetDatabaseDefaults();
            for (int i = 0; i < am.Count; i++)
            {
                var q = CalculoLongitudinal.PontoSecao(am[i], new P2(off, 0), d.Espelhar);
                pl.AddVertexAt(i, new Point2d(q.X, q.Y), 0, 0, 0);
            }
            pl.Layer = LayerPlanta;
            if (off == 0.0) pl.ColorIndex = 8;
            Ctx.Adicionar(tr, ms, pl);
            d.HandlesDesenho.Add(Ctx.HandleDe(pl.ObjectId));
        }

        // ---------- cálculo e rótulo
        var eq = s.Projeto.Equacao(d.EquacaoId);
        d.Resultado = CalculoLongitudinal.Calcular(d, sec, am, eq);
        var meio = am[am.Count / 2];
        var dir = new P2(meio.DirX, meio.DirY);
        double ang = Geo.AnguloLegivel(dir.Angulo);
        var pt = CalculoLongitudinal.PontoSecao(meio, new P2(d.Espelhar ? xmin - 0.5 : xmax + 0.5, 0), d.Espelhar);
        var r = d.Resultado;
        string txt = $"{d.Nome} — {tipo.Nome}  L={Num.F(r.Comprimento3D)} m";
        if (!double.IsNaN(r.CapacidadeMinima) && r.CapacidadeMinima > 0) txt += $"  Qcap={Num.F(r.CapacidadeMinima * 1000, 0)} L/s";
        var mt = Ctx.Texto(txt, new Point3d(pt.X, pt.Y, 0), 0.6, ang, AttachmentPoint.BottomCenter);
        mt.Layer = LayerTexto;
        Ctx.Adicionar(tr, ms, mt);
        d.HandlesDesenho.Add(Ctx.HandleDe(mt.ObjectId));
        return erros;
    }

    private static Polyline3d Secao3d(IReadOnlyList<P2> poligono, AmostraLongitudinal a, bool espelhar)
    {
        var col = new Point3dCollection();
        foreach (var p in poligono)
        {
            var q = CalculoLongitudinal.PontoSecao(a, p, espelhar);
            col.Add(new Point3d(q.X, q.Y, q.Z));
        }
        return new Polyline3d(Poly3dType.SimplePoly, col, true);
    }

    /// <summary>Alternativa ao loft: extrude a seção de uma amostra até a seguinte, ao longo da reta entre elas.</summary>
    private static Solid3d ExtrusaoTrecho(IReadOnlyList<P2> poligono, AmostraLongitudinal a, AmostraLongitudinal b, bool espelhar)
    {
        using var pl = Secao3d(poligono, a, espelhar);
        var curvas = new DBObjectCollection { pl };
        var regs = Region.CreateFromCurves(curvas);
        if (regs.Count == 0) throw new InvalidOperationException("seção não forma região");
        var reg = (Region)regs[0];
        for (int i = 1; i < regs.Count; i++) regs[i].Dispose();
        using var caminho = new Line(new Point3d(a.X, a.Y, a.Z), new Point3d(b.X, b.Y, b.Z));
        var so = new Solid3d();
        so.SetDatabaseDefaults();
        try { so.ExtrudeAlongPath(reg, caminho, 0); }
        catch { so.Dispose(); throw; }
        finally { reg.Dispose(); }
        return so;
    }
}
