using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CivilAssembly = Autodesk.Civil.DatabaseServices.Assembly;

[assembly: CommandClass(typeof(Projeta.Civil3D.JunctionCommands))]

namespace Projeta.Civil3D;

public sealed class JunctionCommands
{
    private sealed class Cancelled : System.Exception { }
    private sealed record Axis(Alignment Alignment, AlignmentLine Line, JunctionPoint Pick);

    [CommandMethod("JUNCAOIFM", CommandFlags.Modal)]
    public void Create()
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        var ed = document.Editor; var db = document.Database;
        ed.WriteMessage("\nJUNÇÃO DE CORREDORES — IFM\nLigação entre trechos retos. Clique no lado desejado em cada eixo.\n");
        try
        {
            if (db.CurrentSpaceId != SymbolUtilityServices.GetBlockModelSpaceId(db))
                throw new ArgumentException("Execute a rotina no espaço do modelo.");
            ObjectId corridorId;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var civil = CivilDocument.GetCivilDocument(db);
                var first = PickAxis(ed, tr, "Primeiro alinhamento (entrada)");
                var second = PickAxis(ed, tr, "Segundo alinhamento (saída)");
                if (first.Alignment.ObjectId == second.Alignment.ObjectId)
                    throw new ArgumentException("Escolha dois alinhamentos diferentes.");
                var radiusResult = ed.GetDouble(new PromptDoubleOptions("\nRaio da junção (unidades do desenho): ")
                    { AllowNegative = false, AllowZero = false, DefaultValue = 12, UseDefaultValue = true });
                if (radiusResult.Status != PromptStatus.OK) throw new Cancelled();
                var radius = radiusResult.Value;
                var arc = JunctionGeometry.Solve(P(first.Line.StartPoint), P(first.Line.EndPoint), first.Pick,
                    P(second.Line.StartPoint), P(second.Line.EndPoint), second.Pick, radius);
                var p1 = PickProfile(ed, tr, first.Alignment);
                var p2 = PickProfile(ed, tr, second.Alignment);
                var s1 = StationOnLine(first.Line, arc.Start);
                var s2 = StationOnLine(second.Line, arc.End);
                if (s1 < p1.StartingStation || s1 > p1.EndingStation || s2 < p2.StartingStation || s2 > p2.EndingStation)
                    throw new ArgumentException("Os greides precisam cobrir os pontos de tangência da ligação.");
                var z1 = p1.ElevationAt(s1); var z2 = p2.ElevationAt(s2);
                var grade = JunctionGeometry.Grade(z1, z2, arc.Length);
                var assemblyId = Choose(ed, "Seção tipo da ligação", civil.AssemblyCollection.Cast<ObjectId>().ToList(),
                    id => ((CivilAssembly)tr.GetObject(id, OpenMode.ForRead)).Name);
                var surfaceIds = civil.GetSurfaceIds().Cast<ObjectId>()
                    .Where(id => tr.GetObject(id, OpenMode.ForRead) is Autodesk.Civil.DatabaseServices.Surface { IsVolumeSurface: false }).ToList();
                var surfaceId = Choose(ed, "Superfície alvo dos taludes", surfaceIds,
                    id => ((Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead)).Name, optional: true);
                var summary = $"\n{first.Alignment.Name} / {p1.Name}: estaca {s1:F3}, cota {z1:F3}" +
                    $"\n{second.Alignment.Name} / {p2.Name}: estaca {s2:F3}, cota {z2:F3}" +
                    $"\nRaio {radius:F3}; comprimento {arc.Length:F3}; rampa {grade:F3}%." +
                    "\nCria eixo, greide reto entre as cotas e corredor próprio. Curvas verticais e recortes dos corredores existentes são manuais." +
                    "\nGreide estático: alterações nos eixos ou greides originais exigem refazer a ligação.";
                ed.WriteMessage(summary);
                var confirm = new PromptKeywordOptions("\nCriar ligação? [Sim/Nao] <Nao>: ", "Sim Nao") { AllowNone = true };
                var answer = ed.GetKeywords(confirm);
                if (answer.Status != PromptStatus.OK || answer.StringResult != "Sim") throw new Cancelled();
                if (civil.Styles.AlignmentStyles.Count == 0 || civil.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count == 0 ||
                    civil.Styles.ProfileStyles.Count == 0 || civil.Styles.LabelSetStyles.ProfileLabelSetStyles.Count == 0)
                    throw new ArgumentException("O desenho precisa ter estilos e conjuntos de rótulos de alinhamento e perfil.");
                var name = Alignment.GetNextUniqueName("JUNCAO — IFM");
                using var pline = new Polyline(2);
                pline.SetDatabaseDefaults(db);
                pline.AddVertexAt(0, new Point2d(arc.Start.X, arc.Start.Y), arc.Bulge, 0, 0);
                pline.AddVertexAt(1, new Point2d(arc.End.X, arc.End.Y), 0, 0, 0);
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                space.AppendEntity(pline); tr.AddNewlyCreatedDBObject(pline, true);
                var alignmentId = Alignment.Create(civil,
                    new PolylineOptions { PlineId = pline.ObjectId, EraseExistingEntities = true, AddCurvesBetweenTangents = false },
                    name, ObjectId.Null, db.Clayer, civil.Styles.AlignmentStyles[0], civil.Styles.LabelSetStyles.AlignmentLabelSetStyles[0]);
                var alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
                if (Math.Abs(alignment.Length - arc.Length) > .001)
                    throw new InvalidOperationException("Comprimento do alinhamento criado diferente do calculado. Operação revertida.");
                JunctionGeometry.Grade(z1, z2, alignment.Length);
                var profileId = Profile.CreateByLayout("GREIDE — IFM", alignmentId, db.Clayer,
                    civil.Styles.ProfileStyles[0], civil.Styles.LabelSetStyles.ProfileLabelSetStyles[0]);
                var profile = (Profile)tr.GetObject(profileId, OpenMode.ForWrite);
                profile.Entities.AddFixedTangent(new Point2d(alignment.StartingStation, z1), new Point2d(alignment.EndingStation, z2));
                var corridorNames = civil.CorridorCollection.Cast<ObjectId>().Select(id => ((Corridor)tr.GetObject(id, OpenMode.ForRead)).Name);
                corridorId = civil.CorridorCollection.Add(Names.Unique(corridorNames, name));
                var corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForWrite);
                var baseline = corridor.Baselines.Add(name, alignmentId, profileId);
                var region = baseline.BaselineRegions.Add("JUNÇÃO — IFM", assemblyId, alignment.StartingStation, alignment.EndingStation);
                var settings = region.AppliedAssemblySetting;
                settings.FrequencyAlongTangents = settings.FrequencyAlongCurves = settings.FrequencyAlongSpirals = settings.FrequencyAlongProfileCurves = Math.Min(1, arc.Length / 10);
                if (!surfaceId.IsNull)
                {
                    using var targets = region.GetTargets();
                    foreach (SubassemblyTargetInfo target in targets)
                        if (target.TargetType == SubassemblyLogicalNameType.Surface) target.TargetIds = new ObjectIdCollection { surfaceId };
                    region.SetTargets(targets);
                }
                tr.Commit();
            }
            // Reconstruir após gravar as linhas de base, como no criador de corredores do painel.
            try
            {
                using var rebuild = db.TransactionManager.StartTransaction();
                var corridor = (Corridor)rebuild.GetObject(corridorId, OpenMode.ForWrite);
                corridor.Rebuild(); rebuild.Commit();
                ed.WriteMessage("\nJUNÇÃO DE CORREDORES — IFM: ligação criada e reconstruída. Confira seção, alvos, encontros e drenagem. Para desfazer: U.\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLigação gravada, mas a reconstrução falhou: {ex.Message}. Revise seção tipo/PKT e alvos; reconstrua o corredor ou use U.\n");
            }
        }
        catch (Cancelled) { ed.WriteMessage("\nLigação cancelada. Nenhum objeto criado.\n"); }
        catch (System.Exception ex) { ed.WriteMessage($"\nJUNÇÃO DE CORREDORES — IFM: {ex.Message}\n"); }
    }

    private static Axis PickAxis(Editor ed, Transaction tr, string title)
    {
        var options = new PromptEntityOptions("\n" + title + ": ");
        options.SetRejectMessage("\nSelecione um alinhamento Civil 3D."); options.AddAllowedClass(typeof(Alignment), true);
        var result = ed.GetEntity(options);
        if (result.Status != PromptStatus.OK) throw new Cancelled();
        var alignment = (Alignment)tr.GetObject(result.ObjectId, OpenMode.ForRead);
        double station = 0, offset = 0;
        alignment.StationOffset(result.PickedPoint.X, result.PickedPoint.Y, ref station, ref offset);
        if (alignment.Entities.EntityAtStation(station) is not AlignmentLine line)
            throw new ArgumentException("Selecione um trecho reto do alinhamento. Curvas e espirais ainda não são atendidas nesta rotina.");
        var fraction = (station - line.StartStation) / line.Length;
        var pick = P(line.StartPoint) + (P(line.EndPoint) - P(line.StartPoint)) * fraction;
        return new(alignment, line, pick);
    }

    private static Profile PickProfile(Editor ed, Transaction tr, Alignment alignment)
    {
        var ids = alignment.GetProfileIds().Cast<ObjectId>().Where(id =>
            tr.GetObject(id, OpenMode.ForRead) is Profile p && p.ProfileType == ProfileType.FG).ToList();
        return (Profile)tr.GetObject(Choose(ed, "Greide de " + alignment.Name, ids,
            id => ((Profile)tr.GetObject(id, OpenMode.ForRead)).Name), OpenMode.ForRead);
    }

    private static ObjectId Choose(Editor ed, string title, IReadOnlyList<ObjectId> ids, Func<ObjectId, string> name, bool optional = false)
    {
        if (ids.Count == 0 && !optional) throw new ArgumentException(title + ": nenhum objeto disponível no desenho.");
        ed.WriteMessage("\n" + title + ":");
        if (optional) ed.WriteMessage("\n  0 — Sem alvo (configurar manualmente)");
        for (var i = 0; i < ids.Count; i++) ed.WriteMessage($"\n  {i + 1} — {name(ids[i])}");
        var result = ed.GetInteger(new PromptIntegerOptions("\nNúmero: ") { LowerLimit = optional ? 0 : 1, UpperLimit = ids.Count, AllowNone = false });
        if (result.Status != PromptStatus.OK) throw new Cancelled();
        return result.Value == 0 ? ObjectId.Null : ids[result.Value - 1];
    }

    private static JunctionPoint P(Point2d p) => new(p.X, p.Y);
    private static double StationOnLine(AlignmentLine line, JunctionPoint p) => line.StartStation + (p - P(line.StartPoint)).Length;
}
