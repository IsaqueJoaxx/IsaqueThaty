using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcEntity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(Projeta.Civil3D.ProfileJumpCommands))]

namespace Projeta.Civil3D;

// IR AO PERFIL: clique num eixo e a tela vai para a vista de perfil dele; clique na vista de perfil (ou
// num perfil dentro dela) e a tela volta ao eixo em planta. Repete até Enter ou Esc. Só muda a vista.
internal static class ProfileJump
{
    // Próxima vista de perfil de cada eixo (desenho + handle), para alternar quando o eixo tem várias.
    private static readonly Dictionary<string, int> nextView = new(StringComparer.OrdinalIgnoreCase);

    public static void Run(Document document)
    {
        var ed = document.Editor;
        // As vistas de perfil ficam no espaço do modelo; no espaço do papel, vai para a aba Modelo.
        if (Convert.ToInt32(AcadApplication.GetSystemVariable("TILEMODE")) == 0 && Convert.ToInt32(AcadApplication.GetSystemVariable("CVPORT")) == 1)
            LayoutManager.Current.CurrentLayout = "Model";
        var options = new PromptEntityOptions("\nClique no eixo para ir ao perfil (ou na vista de perfil para voltar ao eixo) [Enter para sair]: ")
        {
            AllowNone = true, AllowObjectOnLockedLayer = true
        };
        while (true)
        {
            var picked = ed.GetEntity(options);
            if (picked.Status != PromptStatus.OK) return;
            // Se o clique pegou outra coisa por cima (a superfície TIN, uma linha...), procura no mesmo ponto.
            var id = Wanted(picked.ObjectId) ? picked.ObjectId : Under(ed, picked.PickedPoint);
            ed.WriteMessage("\n" + (id.IsNull ? "Nesse ponto não há eixo, vista de perfil nem perfil." : Jump(document, id)));
        }
    }

    private static readonly RXClass[] Classes = { RXObject.GetClass(typeof(Alignment)), RXObject.GetClass(typeof(ProfileView)), RXObject.GetClass(typeof(Profile)) };

    private static bool Wanted(ObjectId id) => !id.IsNull && Classes.Any(c => id.ObjectClass.IsDerivedFrom(c));

    // Eixo, vista de perfil ou perfil dentro do quadradinho de seleção em volta do ponto clicado (eixo primeiro).
    private static ObjectId Under(Editor ed, Point3d point)
    {
        var pixels = Math.Max(3, Convert.ToInt32(AcadApplication.GetSystemVariable("PICKBOX")));
        var screen = (Point2d)AcadApplication.GetSystemVariable("SCREENSIZE");
        var half = 1.5 * pixels * Convert.ToDouble(AcadApplication.GetSystemVariable("VIEWSIZE")) / Math.Max(1, screen.Y);
        var types = string.Join(",", Classes.Select(c => c.DxfName).Where(n => !string.IsNullOrEmpty(n)));
        var found = ed.SelectCrossingWindow(new Point3d(point.X - half, point.Y - half, point.Z), new Point3d(point.X + half, point.Y + half, point.Z),
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, types) }));
        if (found.Status != PromptStatus.OK || found.Value.Count == 0) return ObjectId.Null;
        var ids = found.Value.GetObjectIds();
        return ids.FirstOrDefault(i => i.ObjectClass.IsDerivedFrom(Classes[0])) is { IsNull: false } alignment ? alignment : ids[0];
    }

    // Vai do eixo à vista de perfil, ou da vista de perfil (ou perfil) ao eixo. Devolve a mensagem.
    public static string Jump(Document document, ObjectId picked)
    {
        var db = document.Database;
        Extents3d? target = null;
        string message;
        using (var tr = db.TransactionManager.StartOpenCloseTransaction())
        {
            switch (tr.GetObject(picked, OpenMode.ForRead))
            {
                case Alignment alignment:
                {
                    var views = alignment.GetProfileViewIds().Cast<ObjectId>()
                        .Where(id => !id.IsNull && !id.IsErased)
                        .Select(id => (ProfileView)tr.GetObject(id, OpenMode.ForRead))
                        .OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                    if (views.Count == 0)
                        return $"O eixo {alignment.Name} não tem vista de perfil neste desenho. Crie a vista de perfil dele e tente de novo.";
                    var key = db.Filename + "|" + alignment.Handle;
                    var index = nextView.GetValueOrDefault(key) % views.Count;
                    nextView[key] = index + 1;
                    target = Extents(views[index]);
                    message = $"{alignment.Name} → vista de perfil {views[index].Name}" +
                              (views.Count > 1 ? $" ({index + 1} de {views.Count}; clique de novo no eixo para ir à próxima)." : ".");
                    break;
                }
                case ProfileView view:
                    (target, message) = ToAlignment(tr, view.AlignmentId, $"vista de perfil {view.Name}");
                    break;
                case Profile profile:
                    (target, message) = ToAlignment(tr, profile.AlignmentId, $"perfil {profile.Name}");
                    break;
                default:
                    return "Escolha um eixo, uma vista de perfil ou um perfil.";
            }
        }
        if (target is Extents3d box) Zoom(document.Editor, box);
        return message;
    }

    private static (Extents3d?, string) ToAlignment(Transaction tr, ObjectId alignmentId, string from)
    {
        if (alignmentId.IsNull || alignmentId.IsErased) return (null, $"{from}: o eixo não está neste desenho.");
        var alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
        return (Extents(alignment), $"{from} → eixo {alignment.Name}.");
    }

    private static Extents3d? Extents(AcEntity entity)
    {
        try
        {
            var box = entity.GeometricExtents;
            return box.MaxPoint.X > box.MinPoint.X || box.MaxPoint.Y > box.MinPoint.Y ? box : null;
        }
        catch { return null; } // sem geometria (por exemplo, oculto ou vazio)
    }

    // Enquadra a caixa na janela atual, com folga, mantendo a proporção da tela (vale com UCS e vista girados).
    private static void Zoom(Editor ed, Extents3d box, double margin = 0.18)
    {
        using var view = ed.GetCurrentView();
        var toView = (Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) *
                      Matrix3d.Displacement(view.Target - Point3d.Origin) *
                      Matrix3d.PlaneToWorld(view.ViewDirection)).Inverse();
        box.TransformBy(toView);
        var width = Math.Max(box.MaxPoint.X - box.MinPoint.X, 1e-6);
        var height = Math.Max(box.MaxPoint.Y - box.MinPoint.Y, 1e-6);
        var ratio = view.Width / view.Height;
        if (width / height > ratio) height = width / ratio; else width = height * ratio;
        view.CenterPoint = new Point2d((box.MinPoint.X + box.MaxPoint.X) / 2, (box.MinPoint.Y + box.MaxPoint.Y) / 2);
        view.Width = width * (1 + margin);
        view.Height = height * (1 + margin);
        ed.SetCurrentView(view);
    }
}

public sealed class ProfileJumpCommands
{
    [CommandMethod("PROJETAPERFIL", CommandFlags.Modal)]
    public static void GoToProfile()
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        try { ProfileJump.Run(document); }
        catch (System.Exception ex) { document.Editor.WriteMessage($"\nPROJETA: não foi possível ir ao perfil: {ex.Message}\n"); }
    }
}
