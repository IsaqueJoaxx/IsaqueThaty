using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

namespace Projeta.Civil3D;

internal sealed record GradeCreateRequest(GradeRequest Input, GradePreview Reviewed, string Name, string StyleHandle, string LabelsHandle);

internal static class GradeCreator
{
    public static string Create(Document document, GradeCreateRequest request)
    {
        var updated = GradeReader.Read(document, request.Input);
        if (updated.SourceSignature != request.Reviewed.SourceSignature || !updated.Pvis.SequenceEqual(request.Reviewed.Pvis))
            throw new InvalidOperationException("O terreno ou a proposta mudou. Gere e revise outra prévia antes de criar.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Informe o nome do greide.");
        var db = document.Database;
        var axisId = Ids.Resolve(db, request.Input.AlignmentHandle, "o eixo");
        var styleId = Ids.Resolve(db, request.StyleHandle, "o estilo de perfil");
        var labelsId = Ids.Resolve(db, request.LabelsHandle, "o conjunto de rótulos");
        var civil = CivilDocument.GetCivilDocument(db);
        if (!civil.Styles.ProfileStyles.Contains(styleId) || !civil.Styles.LabelSetStyles.ProfileLabelSetStyles.Contains(labelsId))
            throw new InvalidOperationException("Os estilos selecionados não pertencem às categorias de perfil esperadas.");
        using var tr = db.TransactionManager.StartTransaction();
        var axis = (Alignment)tr.GetObject(axisId, OpenMode.ForRead);
        var names = new List<string>();
        foreach (ObjectId id in axis.GetProfileIds())
            if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Profile existing) names.Add(existing.Name);
        var name = Names.Unique(names, request.Name.Trim());
        var profileId = Profile.CreateByLayout(name, axisId, db.Clayer, styleId, labelsId);
        var profile = (Profile)tr.GetObject(profileId, OpenMode.ForWrite);
        for (var i = 1; i < updated.Pvis.Count; i++)
        {
            var a = updated.Pvis[i - 1]; var b = updated.Pvis[i];
            var grade = Math.Abs(100 * (b.Elevation - a.Elevation) / (b.Station - a.Station));
            if (!double.IsFinite(grade) || grade > request.Input.Options.MaxPercent + 1e-7 || grade > 7.0000001)
                throw new InvalidOperationException("Rampa excedida. Nenhum perfil será gravado.");
            profile.Entities.AddFixedTangent(new Point2d(a.Station, a.Elevation), new Point2d(b.Station, b.Elevation));
        }
        if (profile.Entities.Count != updated.Pvis.Count - 1)
            throw new InvalidOperationException("Nem todas as tangentes foram criadas. A operação será revertida.");
        tr.Commit();
        return $"Greide '{name}' criado: {updated.Pvis.Count} PIVs, {updated.Pvis.Count - 1} tangentes. Sem curvas automáticas. Para desfazer: U.";
    }
}
