using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Alinhamentos;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJAPAGARPIV — apaga o PIV clicado de um greide mesmo quando o Excluir PIV do Civil 3D recusa
/// (rampas fixas ou flutuantes, curvas fixas, greide vindo de arquivo...). O greide é lido como PIVs +
/// curvas verticais (Projeta.Core.Alinhamentos.GreidePVIs), o PIV sai da lista e o greide é refeito no
/// MESMO perfil: rampas fixas entre os PIVs e curvas verticais livres com o comprimento (ou raio) de antes.
/// Estilo, rótulos, profile views e corredores continuam ligados ao perfil. U desfaz o comando inteiro.
/// Sem dependência do resto do plugin: o mesmo arquivo entra no avulso que carrega por NETLOAD.
/// </summary>
public sealed class CmdGreide
{
    [CommandMethod("PRJAPAGARPIV", CommandFlags.Modal)]
    public static void ApagarPIV()
    {
        try
        {
            var ed = AcCore.DocumentManager.MdiActiveDocument.Editor;
            var peo = new PromptEntityOptions("\nSelecione o greide (clique na linha do greide dentro do perfil): ");
            peo.SetRejectMessage("\nIsso não é um greide (perfil).");
            peo.AddAllowedClass(typeof(Profile), true);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;
            ApagarNoGreide(per.ObjectId, per.PickedPoint);
        }
        catch (System.Exception ex) { Msg($"[ERRO] PRJAPAGARPIV: {ex.Message}"); }
    }

    /// <summary>Laço de apagar PIVs. Também é chamado pelo PRJAPAGARPI quando o usuário clica num greide.</summary>
    internal static void ApagarNoGreide(ObjectId perfilId, Point3d pontoUcs)
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var ed = doc.Editor;
        var db = doc.Database;
        var clique = pontoUcs.TransformBy(ed.CurrentUserCoordinateSystem);

        ObjectId vistaId;
        string nome;
        using (var tr = db.TransactionManager.StartOpenCloseTransaction())
        {
            var pf = (Profile)tr.GetObject(perfilId, OpenMode.ForRead);
            nome = pf.Name;
            var problema = PorQueNaoEdita(pf);
            if (problema is not null) { Msg(problema); return; }
            var al = (Alignment)tr.GetObject(pf.AlignmentId, OpenMode.ForRead);
            vistaId = AcharVista(tr, al, clique);
            tr.Commit();
        }
        if (vistaId.IsNull)
        {
            Msg("Não achei o perfil (profile view) onde você clicou. Clique na linha do greide dentro do perfil.");
            return;
        }

        int apagados = 0;
        while (true)
        {
            PlanoGreide plano;
            var xy = new List<Point2d>();
            try
            {
                using var tr = db.TransactionManager.StartOpenCloseTransaction();
                var pf = (Profile)tr.GetObject(perfilId, OpenMode.ForRead);
                var pv = (ProfileView)tr.GetObject(vistaId, OpenMode.ForRead);
                plano = LerGreide(pf);
                for (int i = 0; i < plano.Count; i++)
                {
                    double x = 0, y = 0;
                    bool ok = pv.FindXYAtStationAndElevation(plano.Estacas[i], plano.Cotas[i], ref x, ref y);
                    xy.Add(ok ? new Point2d(x, y) : new Point2d(double.NaN, double.NaN));
                }
                tr.Commit();
            }
            catch (InvalidOperationException ex)
            {
                Msg(ex.Message);
                break;
            }

            if (plano.Count < 3)
            {
                Msg($"{nome} não tem PIV interno para apagar.");
                break;
            }

            var ppo = new PromptPointOptions(
                $"\n{nome}: {plano.Count - 2} PIV(s). Clique perto do PIV que quer apagar <sair>: ")
            { AllowNone = true };
            var ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) break;
            var p = ppr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
            int k = -1;
            double melhor = double.MaxValue;
            for (int i = 1; i < plano.Count - 1; i++)
            {
                if (double.IsNaN(xy[i].X)) continue;
                double d = xy[i].GetDistanceTo(new Point2d(p.X, p.Y));
                if (d < melhor) { melhor = d; k = i; }
            }
            if (k < 0)
            {
                Msg("Não consegui localizar os PIVs neste perfil.");
                break;
            }
            Marcar(ed, xy[k]);

            Msg($"PIV {k} na estaca {plano.Estacas[k]:0.000}, cota {plano.Cotas[k]:0.000} — rampas " +
                $"{plano.Rampa(k - 1) * 100:0.###}% / {plano.Rampa(k) * 100:0.###}%, {plano.Curvas[k]?.Descricao ?? "sem curva vertical"}.");
            var pko = new PromptKeywordOptions("\nApagar este PIV? [Sim/Nao] <Sim>: ", "Sim Nao") { AllowNone = true };
            pko.Keywords.Default = "Sim";
            var pkr = ed.GetKeywords(pko);
            ed.Regen(); // limpa a marca
            if (pkr.Status == PromptStatus.Cancel) break;
            if (pkr.Status == PromptStatus.OK && pkr.StringResult == "Nao") continue;

            double rampaNova = (plano.Cotas[k + 1] - plano.Cotas[k - 1]) / (plano.Estacas[k + 1] - plano.Estacas[k - 1]);
            var log = new List<string>();
            try
            {
                var novo = GreidePVIs.Apagar(plano, k, log);
                Reconstruir(db, perfilId, novo);
            }
            catch (System.Exception ex)
            {
                Msg($"Não consegui refazer o greide ({ex.Message}). O greide não foi alterado.");
                break;
            }
            apagados++;
            Msg($"PIV {k} apagado. Rampa nova entre a estaca {plano.Estacas[k - 1]:0.000} e a {plano.Estacas[k + 1]:0.000}: {rampaNova * 100:0.###}%.");
            foreach (var l in log) Msg($"  - {l}");
        }

        if (apagados > 0)
            Msg($"{apagados} PIV(s) apagado(s). Se um corredor usa este greide, reconstrua o corredor. U desfaz o comando inteiro.");
    }

    /// <summary>Motivo para não mexer no perfil (ou null se é um greide de projeto editável).</summary>
    private static string? PorQueNaoEdita(Profile pf)
    {
        if (pf.IsReferenceObject)
            return $"O greide '{pf.Name}' vem de um atalho de dados (data shortcut) e aqui é só leitura. " +
                   "Abra o desenho de origem do greide e apague o PIV lá; depois sincronize o atalho.";
        return pf.ProfileType switch
        {
            ProfileType.FG or ProfileType.File => null,
            ProfileType.EG => $"'{pf.Name}' é o perfil do terreno (vem da superfície e se atualiza sozinho), não um greide de projeto. Clique no greide.",
            ProfileType.Superimposed => $"'{pf.Name}' é um perfil sobreposto (ligado ao greide de outro alinhamento). Apague o PIV no greide de origem.",
            _ => $"'{pf.Name}' é um perfil gerado automaticamente ({pf.ProfileType}). Apague o PIV no objeto de origem.",
        };
    }

    /// <summary>Profile view do alinhamento que contém o ponto clicado (ou o único que houver).</summary>
    private static ObjectId AcharVista(Transaction tr, Alignment al, Point3d ponto)
    {
        var ids = al.GetProfileViewIds();
        foreach (ObjectId id in ids)
        {
            try
            {
                var pv = (ProfileView)tr.GetObject(id, OpenMode.ForRead);
                var ext = pv.GeometricExtents;
                double folga = 0.01 * Math.Max(ext.MaxPoint.X - ext.MinPoint.X, ext.MaxPoint.Y - ext.MinPoint.Y);
                if (ponto.X >= ext.MinPoint.X - folga && ponto.X <= ext.MaxPoint.X + folga &&
                    ponto.Y >= ext.MinPoint.Y - folga && ponto.Y <= ext.MaxPoint.Y + folga)
                    return id;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }
        }
        return ids.Count == 1 ? ids[0] : ObjectId.Null;
    }

    /// <summary>PIVs do greide (estaca, cota e curva vertical), em ordem de estaca.</summary>
    private static PlanoGreide LerGreide(Profile pf)
    {
        var lista = new List<(double Est, double Cota, CurvaVertical? Curva)>();
        foreach (ProfilePVI v in pf.PVIs)
        {
            ProfileEntity? vc = null;
            try { vc = v.VerticalCurve; } catch (Autodesk.AutoCAD.Runtime.Exception) { }
            CurvaVertical? c = vc switch
            {
                ProfileParabolaSymmetric ps => CurvaVertical.Parabola(ps.Length),
                ProfileParabolaAsymmetric pa => CurvaVertical.Assimetrica(pa.AsymmetricLength1, pa.AsymmetricLength2),
                ProfileCircular pc => CurvaVertical.Circular(pc.Radius),
                _ => null,
            };
            lista.Add((v.RawStation, v.Elevation, c));
        }
        var plano = new PlanoGreide();
        foreach (var (est, cota, curva) in lista.OrderBy(t => t.Est)) plano.Adicionar(est, cota, curva);
        plano.Validar();
        return plano;
    }

    /// <summary>
    /// Refaz o greide no mesmo perfil: rampas fixas entre os PIVs e curvas verticais livres.
    /// Confere o resultado antes de confirmar; se algo sair diferente, a transação não é confirmada e o greide fica como estava.
    /// </summary>
    private static void Reconstruir(Database db, ObjectId perfilId, PlanoGreide p)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var pf = (Profile)tr.GetObject(perfilId, OpenMode.ForWrite);
        pf.Entities.Clear();
        for (int i = 0; i < p.Count - 1; i++)
            pf.Entities.AddFixedTangent(new Point2d(p.Estacas[i], p.Cotas[i]), new Point2d(p.Estacas[i + 1], p.Cotas[i + 1]));
        for (int k = 1; k < p.Count - 1; k++)
        {
            if (p.Curvas[k] is not { } c) continue;
            var pvi = AcharPVI(pf, p.Estacas[k])
                      ?? throw new InvalidOperationException($"não achei o PIV da estaca {p.Estacas[k]:0.000} depois de refazer as rampas");
            switch (c.Tipo)
            {
                case TipoCurvaVertical.Parabola:
                    pf.Entities.AddFreeSymmetricParabolaByPVIAndCurveLength(pvi, c.L1 + c.L2);
                    break;
                case TipoCurvaVertical.ParabolaAssimetrica:
                    pf.Entities.AddFreeAsymmetricParabolaByPVIAndLengths(pvi, c.L1, c.L2);
                    break;
                default:
                    pf.Entities.AddFreeCircularCurveByPVIAndRadius(pvi, c.Raio);
                    break;
            }
        }

        var lido = LerGreide(pf);
        if (lido.Count != p.Count)
            throw new InvalidOperationException($"o greide refeito ficou com {lido.Count} PIVs em vez de {p.Count}");
        for (int i = 0; i < p.Count; i++)
            if (Math.Abs(lido.Estacas[i] - p.Estacas[i]) > 1e-3 || Math.Abs(lido.Cotas[i] - p.Cotas[i]) > 1e-3)
                throw new InvalidOperationException($"o PIV da estaca {p.Estacas[i]:0.000} saiu no lugar errado");
        tr.Commit();
    }

    private static ProfilePVI? AcharPVI(Profile pf, double estaca)
    {
        ProfilePVI? melhor = null;
        double d = double.MaxValue;
        foreach (ProfilePVI v in pf.PVIs)
        {
            double dv = Math.Abs(v.RawStation - estaca);
            if (dv < d) { d = dv; melhor = v; }
        }
        return d < 1e-3 ? melhor : null;
    }

    private static void Msg(string s) => AcCore.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + s);

    /// <summary>Marca temporária (X + círculo) no PIV escolhido; some no próximo Regen.</summary>
    private static void Marcar(Editor ed, Point2d p)
    {
        double h = Convert.ToDouble(AcCore.GetSystemVariable("VIEWSIZE")) * 0.015;
        var ucs = ed.CurrentUserCoordinateSystem.Inverse();
        Point3d Q(double dx, double dy) => new Point3d(p.X + dx, p.Y + dy, 0).TransformBy(ucs);
        ed.DrawVector(Q(-h, -h), Q(h, h), 1, true);
        ed.DrawVector(Q(-h, h), Q(h, -h), 1, true);
        const int n = 24;
        for (int i = 0; i < n; i++)
        {
            double a0 = 2 * Math.PI * i / n, a1 = 2 * Math.PI * (i + 1) / n;
            ed.DrawVector(Q(1.6 * h * Math.Cos(a0), 1.6 * h * Math.Sin(a0)),
                          Q(1.6 * h * Math.Cos(a1), 1.6 * h * Math.Sin(a1)), 1, true);
        }
    }
}
