using Autodesk.AutoCAD.ApplicationServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D;

// O painel não mexe no desenho. Ele monta um pedido, guarda aqui e dispara um comando;
// o comando roda no contexto do documento, lê o pedido e o apaga.
internal sealed record VolumeRequest(string DrawingPath, string BaseHandle, string ComparisonHandle, string? LimitHandle,
    double CutFactor, double FillFactor, bool Create);

internal sealed record SectionRequest(string DrawingPath, string AlignmentHandle, double Start, double End, double Interval,
    double Left, double Right, IReadOnlyList<string> SurfaceHandles, bool Views);

internal sealed record CorridorItem(string AlignmentHandle, string ProfileHandle);

internal sealed record CorridorRequest(string DrawingPath, IReadOnlyList<CorridorItem> Items, string AssemblyHandle,
    string? TargetHandle, bool Single, string BaseName, double Frequency);

// Corrigir PKT: aponta as submontagens do Subassembly Composer não encontradas para os arquivos desta pasta.
internal sealed record PktRequest(string DrawingPath, string Folder);

internal sealed record VolumeResult(string BaseName, string ComparisonName, string Limit, double? LimitArea,
    double Cut, double Fill, double CutFactor, double FillFactor, string? CreatedName);

internal static class Pending
{
    public static string? ZoomHandle;
    public static VolumeRequest? Volume;
    public static SectionRequest? Sections;
    public static CorridorRequest? Corridors;
    public static PktRequest? Pkt;

    public static T? Take<T>(ref T? slot) where T : class
    {
        var value = slot;
        slot = null;
        return value;
    }
}

internal static class Actions
{
    // Envia um comando fixo ao desenho ativo. Nenhum texto digitado pelo usuário vira comando.
    public static bool Send(string command, ToolPage from)
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) { from.SetStatus("Abra um desenho no Civil 3D.", "warning"); return false; }
        if (document.CommandInProgress.Length != 0)
        {
            from.SetStatus("Conclua o comando atual e tente de novo.", "warning");
            return false;
        }
        document.SendStringToExecute(command + " ", true, false, false);
        return true;
    }

    public static bool SameDrawing(Document document, string drawingPath) =>
        string.Equals(document.Name, drawingPath, StringComparison.OrdinalIgnoreCase);
}
