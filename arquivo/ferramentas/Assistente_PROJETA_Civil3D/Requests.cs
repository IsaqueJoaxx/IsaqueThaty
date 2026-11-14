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

// Volume pelas seções de um grupo de linhas de amostragem. BaseKey e ComparisonKey identificam as fontes
// amostradas (SectionVolumeOps.Key); os nomes só servem de reserva para o relatório.
internal sealed record SectionVolumeRequest(string DrawingPath, string GroupHandle, string BaseKey, string BaseName,
    string ComparisonKey, string ComparisonName, double Start, double End, double CutFactor, double FillFactor);

internal sealed record SectionVolumeResult(string GroupName, string AlignmentName, string BaseName, string ComparisonName,
    double Start, double End, IReadOnlyList<SectionVolumeRow> Rows, IReadOnlyList<string> Warnings,
    double CutFactor, double FillFactor, IReadOnlyList<MaterialListCheck> MaterialLists)
{
    public double Cut => Rows.Sum(r => r.CutVolume);
    public double Fill => Rows.Sum(r => r.FillVolume);
}

// Volumes que o próprio Civil 3D calculou numa lista de materiais do grupo, no mesmo trecho. Os volumes do
// Civil 3D já vêm com os fatores da lista (as áreas não). CutFactor/FillFactor: os fatores, quando a lista tem
// um só material de terraplenagem; nulos quando não dá para saber qual vale (vários materiais).
internal sealed record MaterialListCheck(string Name, string Method, string Factors, IReadOnlyList<SectionVolumeRow> Rows, string? Problem,
    double? CutFactor = null, double? FillFactor = null)
{
    public double Cut => Rows.Sum(r => r.CutVolume);
    public double Fill => Rows.Sum(r => r.FillVolume);
}

internal static class Pending
{
    public static GradeRequest? Grade;
    public static GradeCreateRequest? GradeCreate;
    public static string? ZoomHandle;
    public static VolumeRequest? Volume;
    public static SectionVolumeRequest? SectionVolume;
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
