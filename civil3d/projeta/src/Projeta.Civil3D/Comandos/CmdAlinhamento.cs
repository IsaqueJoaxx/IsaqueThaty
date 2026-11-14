using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using C3dCurveParamType = Autodesk.Civil.DatabaseServices.CurveParamType;
using C3dCurveType = Autodesk.Civil.DatabaseServices.CurveType;
using C3dSpiralParamType = Autodesk.Civil.DatabaseServices.SpiralParamType;
using C3dSpiralType = Autodesk.Civil.SpiralType;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJAPAGARPI — apaga o PI clicado de um alinhamento mesmo quando o Excluir PI do Civil 3D recusa
/// (curva fixa, linha flutuante, eixo convertido de polilinha...). O eixo é lido como PIs + curvas
/// (Projeta.Core.Alinhamentos.EixoPIs), o PI sai da lista e o alinhamento é refeito no MESMO objeto:
/// tangentes fixas entre os PIs e curvas livres (arco ou espiral-curva-espiral) com os raios de antes.
/// Estilo, rótulos, perfis e corredores continuam ligados ao alinhamento. U desfaz o comando inteiro.
/// </summary>
public sealed class CmdAlinhamento
{
    [CommandMethod("PRJAPAGARPI", CommandFlags.Modal)]
    public static void ApagarPI()
    {
        try { ApagarPIs(); }
        catch (System.Exception ex) { Ctx.Msg($"[ERRO] PRJAPAGARPI: {ex.Message}"); }
    }

    private static void ApagarPIs()
    {
        var ed = Ctx.Ed;
        var db = Ctx.Db;

        var peo = new PromptEntityOptions("\nSelecione o alinhamento (ou o greide, dentro do perfil): ");
        peo.SetRejectMessage("\nIsso não é um alinhamento nem um greide.");
        peo.AddAllowedClass(typeof(Alignment), true);
        peo.AddAllowedClass(typeof(Profile), true);
        var per = ed.GetEntity(peo);
        if (per.Status != PromptStatus.OK) return;
        if (per.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Profile))))
        {
            CmdGreide.ApagarNoGreide(per.ObjectId, per.PickedPoint); // clicou no greide: apaga PIV
            return;
        }
        var alId = per.ObjectId;
        int apagados = 0;

        while (true)
        {
            PlanoPIs plano;
            string nome;
            try
            {
                using var tr = db.TransactionManager.StartOpenCloseTransaction();
                var al = (Alignment)tr.GetObject(alId, OpenMode.ForRead);
                nome = al.Name;
                plano = EixoPIs.Extrair(LerElementos(al));
                tr.Commit();
            }
            catch (InvalidOperationException ex)
            {
                Ctx.Msg(ex.Message);
                break;
            }

            if (plano.PIs.Count < 3)
            {
                Ctx.Msg($"{nome} não tem PI interno para apagar.");
                break;
            }

            var ppo = new PromptPointOptions(
                $"\n{nome}: {plano.PIs.Count - 2} PI(s). Clique perto do PI que quer apagar <sair>: ")
            { AllowNone = true };
            var ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) break;
            var wcs = ppr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
            int k = plano.MaisProximo(new P2(wcs.X, wcs.Y));
            var pi = plano.PIs[k];
            Marcar(ed, pi);

            string desc = plano.Curvas[k]?.Descricao ?? "sem curva";
            Ctx.Msg($"PI {k} em E = {pi.X:0.000}, N = {pi.Y:0.000} — deflexão {Math.Abs(plano.Deflexao(k)) * 180 / Math.PI:0.####}°, {desc}.");
            var pko = new PromptKeywordOptions("\nApagar este PI? [Sim/Nao] <Sim>: ", "Sim Nao") { AllowNone = true };
            pko.Keywords.Default = "Sim";
            var pkr = ed.GetKeywords(pko);
            ed.Regen(); // limpa a marca
            if (pkr.Status == PromptStatus.Cancel) break;
            if (pkr.Status == PromptStatus.OK && pkr.StringResult == "Nao") continue;

            var log = new List<string>();
            try
            {
                var novo = EixoPIs.Apagar(plano, k, log);
                Reconstruir(db, alId, novo, log);
            }
            catch (System.Exception ex)
            {
                Ctx.Msg($"Não consegui refazer o eixo ({ex.Message}). O alinhamento não foi alterado.");
                break;
            }
            apagados++;
            Ctx.Msg($"PI {k} apagado.");
            foreach (var l in log) Ctx.Msg($"  - {l}");
        }

        if (apagados > 0)
            Ctx.Msg($"{apagados} PI(s) apagado(s). U desfaz o comando inteiro.");
    }

    private static List<ElementoEixo> LerElementos(Alignment al)
    {
        var lista = new List<ElementoEixo>();
        var ents = al.Entities;
        for (int i = 0; i < ents.Count; i++)
        {
            var e = ents.GetEntityByOrder(i);
            for (int j = 0; j < e.SubEntityCount; j++)
            {
                var s = e[j];
                var ini = new P2(s.StartPoint.X, s.StartPoint.Y);
                var fim = new P2(s.EndPoint.X, s.EndPoint.Y);
                switch (s.SubEntityType)
                {
                    case AlignmentSubEntityType.Arc:
                        lista.Add(new ElementoEixo(TipoElementoEixo.Arco, ini, fim, s.Length, ((AlignmentSubEntityArc)s).Radius));
                        break;
                    case AlignmentSubEntityType.Spiral:
                        var sp = (AlignmentSubEntitySpiral)s;
                        lista.Add(new ElementoEixo(TipoElementoEixo.Espiral, ini, fim, s.Length, 0, (int)sp.SpiralDefinition));
                        break;
                    default:
                        lista.Add(new ElementoEixo(TipoElementoEixo.Reta, ini, fim, s.Length));
                        break;
                }
            }
        }
        return lista;
    }

    /// <summary>Refaz o alinhamento no mesmo objeto. Numa transação: se algo falhar, o eixo fica como estava.</summary>
    private static void Reconstruir(Database db, ObjectId alId, PlanoPIs p, List<string> log)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var al = (Alignment)tr.GetObject(alId, OpenMode.ForWrite);
        double estacaIni = al.StartingStation;
        double compAntes = al.Length;

        al.Entities.Clear();
        var ids = new List<int>();
        for (int i = 0; i < p.PIs.Count - 1; i++)
            ids.Add(al.Entities.AddFixedLine(Pt(p.PIs[i]), Pt(p.PIs[i + 1])).EntityId);
        for (int k = 1; k < p.PIs.Count - 1; k++)
        {
            if (p.Curvas[k] is not { } c) continue;
            if (c.Tipo == TipoCurvaPI.Arco)
                al.Entities.AddFreeCurve(ids[k - 1], ids[k], c.Raio, C3dCurveParamType.Radius, false, C3dCurveType.Compound);
            else
                al.Entities.AddFreeSCS(ids[k - 1], ids[k], c.L1, c.L2, C3dSpiralParamType.Length, c.Raio, false,
                                       (C3dSpiralType)c.TipoEspiral);
        }

        // mantém a estaca do início do eixo
        if (Math.Abs(al.StartingStation - estacaIni) > 1e-6)
        {
            al.ReferencePoint = new Point2d(p.PIs[0].X, p.PIs[0].Y);
            al.ReferencePointStation = estacaIni;
        }

        log.Add($"extensão {compAntes:0.000} → {al.Length:0.000} m");
        if (al.GetProfileIds().Count > 0)
            log.Add("o eixo tem perfis: revise os perfis de projeto, porque as estacas depois do PI mudaram");
        if (al.SuperelevationCurves.Count > 0)
            log.Add("o eixo tinha superelevação calculada: recalcule");
        tr.Commit();
    }

    private static Point3d Pt(P2 v) => new(v.X, v.Y, 0);

    /// <summary>Marca temporária (X + círculo) no PI escolhido; some no próximo Regen.</summary>
    private static void Marcar(Editor ed, P2 p)
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
