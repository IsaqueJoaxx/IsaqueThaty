using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Projeta.Civil3D.NetworkCommands))]

namespace Projeta.Civil3D;

internal enum NetworkMode { Sewer, Trench, Rename, Associate }

internal sealed record NetworkRequest(string DrawingPath, string NetworkHandle, NetworkMode Mode, SewerOptions Sewer,
    TrenchOptions Trench, RenameOptions Rename, IReadOnlyList<string> Alignments, double MaxOffset);

internal sealed record RenameApply(NetworkRequest Input, IReadOnlyList<RenameRow> Reviewed);
internal sealed record PointFlowRequest(NetworkRequest Input, string StructureHandle, double LitersPerSecond);
internal sealed record AssociationRow(string Part, string Kind, string Alignment, double? Station, double? Offset);

// Pedidos do painel para os comandos da aba Redes (mesmo esquema do Pending das outras abas).
internal static class NetworkPending
{
    public static NetworkRequest? Calc;
    public static RenameApply? Rename;
    public static NetworkRequest? Associate;
    public static PointFlowRequest? PointFlow;
}

internal static class NetworkOps
{
    public static NetworkGraph Load(Document document, NetworkRequest r)
    {
        if (!Actions.SameDrawing(document, r.DrawingPath)) throw new InvalidOperationException("O desenho ativo mudou. Clique em Atualizar e tente de novo.");
        return NetworkGraph.Build(NetworkReader.Read(document.Database, r.NetworkHandle));
    }

    // Renomeia exatamente a prévia revisada; se a rede mudou, recusa. Nomes temporários primeiro,
    // para dois nomes trocados entre si não colidirem. Um U desfaz tudo.
    public static string Rename(Document document, RenameApply apply)
    {
        var g = Load(document, apply.Input);
        var plan = Renamer.Plan(g, apply.Input.Rename);
        if (!plan.SequenceEqual(apply.Reviewed)) throw new InvalidOperationException("A rede mudou depois da prévia. Gere a prévia de novo.");
        var changes = plan.Where(p => p.NewName != p.OldName).ToList();
        if (changes.Count == 0) return "Nada a renomear: os nomes já seguem a ordem.";
        var db = document.Database;
        using var tr = db.TransactionManager.StartTransaction();
        var parts = changes.Select(c => (Row: c, Part: (Part)tr.GetObject(Ids.Resolve(db, c.Handle, "uma peça da rede"), OpenMode.ForWrite))).ToList();
        var k = 0;
        foreach (var (_, part) in parts) part.Name = $"~PROJETA-{++k}-{Guid.NewGuid():N}";
        foreach (var (row, part) in parts) part.Name = row.NewName;
        tr.Commit();
        var pipes = changes.Count(c => c.IsPipe);
        return $"Rede {g.Data.Name}: {changes.Count - pipes} estrutura(s) e {pipes} tubo(s) renomeados. Para desfazer: U.";
    }

    // Cada estrutura (e cada tubo, pelo ponto médio) fica com o eixo mais próximo entre os marcados,
    // se o afastamento não passar do máximo. Estaca e afastamento passam a valer nos rótulos do Civil 3D.
    public static (string Message, List<AssociationRow> Rows) Associate(Document document, NetworkRequest r)
    {
        if (!Actions.SameDrawing(document, r.DrawingPath)) throw new InvalidOperationException("O desenho ativo mudou. Clique em Atualizar e tente de novo.");
        if (r.Alignments.Count == 0) throw new ArgumentException("Marque pelo menos um eixo.");
        var db = document.Database;
        var rows = new List<AssociationRow>();
        using var tr = db.TransactionManager.StartTransaction();
        var network = (Network)tr.GetObject(Ids.Resolve(db, r.NetworkHandle, "a rede"), OpenMode.ForRead);
        var axes = r.Alignments.Select(h => (Alignment)tr.GetObject(Ids.Resolve(db, h, "um eixo"), OpenMode.ForRead)).ToList();
        (Alignment? Axis, double Station, double Offset) Nearest(double x, double y)
        {
            Alignment? best = null;
            double station = 0, offset = double.PositiveInfinity;
            foreach (var axis in axes)
            {
                double s = 0, o = 0;
                try { axis.StationOffset(x, y, ref s, ref o); }
                catch { continue; } // ponto fora do trecho do eixo
                if (Math.Abs(o) < Math.Abs(offset)) { best = axis; station = s; offset = o; }
            }
            return (best, station, offset);
        }
        int done = 0, skipped = 0;
        void Assign(Part part, string kind, double x, double y)
        {
            var (axis, station, offset) = Nearest(x, y);
            if (axis is null || Math.Abs(offset) > r.MaxOffset)
            {
                skipped++;
                rows.Add(new AssociationRow(part.Name, kind, axis is null ? "fora dos eixos" : $"mais de {Fmt.N(r.MaxOffset)} m", null, null));
                return;
            }
            part.UpgradeOpen();
            part.RefAlignmentId = axis.ObjectId;
            done++;
            rows.Add(new AssociationRow(part.Name, kind, axis.Name, station, offset));
        }
        foreach (ObjectId id in network.GetStructureIds())
            if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Structure s) Assign(s, "Estrutura", s.Location.X, s.Location.Y);
        foreach (ObjectId id in network.GetPipeIds())
            if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Pipe p)
                Assign(p, "Tubo", (p.StartPoint.X + p.EndPoint.X) / 2, (p.StartPoint.Y + p.EndPoint.Y) / 2);
        tr.Commit();
        return ($"Rede {network.Name}: {done} peça(s) associada(s) ao eixo mais próximo" +
                (skipped > 0 ? $"; {skipped} sem eixo dentro de {Fmt.N(r.MaxOffset)} m (não alteradas)" : "") + ". Para desfazer: U.", rows);
    }

    // Vazão pontual (L/s) gravada na propriedade Vazão conhecida (Known Flow) da estrutura, em m³/s.
    public static string SetPointFlow(Document document, PointFlowRequest p)
    {
        if (!Actions.SameDrawing(document, p.Input.DrawingPath)) throw new InvalidOperationException("O desenho ativo mudou.");
        if (!double.IsFinite(p.LitersPerSecond) || p.LitersPerSecond < 0) throw new ArgumentException("Vazão inválida.");
        var db = document.Database;
        using var tr = db.TransactionManager.StartTransaction();
        var s = (Structure)tr.GetObject(Ids.Resolve(db, p.StructureHandle, "a estrutura"), OpenMode.ForWrite);
        s.KnownFlow = p.LitersPerSecond / 1000;
        tr.Commit();
        return $"Vazão pontual de {Fmt.N(p.LitersPerSecond)} L/s gravada em {s.Name} (Vazão conhecida da estrutura). Para desfazer: U.";
    }
}

public sealed class NetworkCommands
{
    private static Document? Active => AcadApplication.DocumentManager.MdiActiveDocument;

    private static void Report(Document document, string text, bool ok)
    {
        Commands.NetworksPage?.ShowOutcome(text, ok);
        document.Editor.WriteMessage($"\nPROJETA: {text}\n");
    }

    [CommandMethod("PROJETAREDESABRIR", CommandFlags.Modal)]
    public static void Open() => Commands.ShowPage("redes");

    [CommandMethod("PROJETAREDES", CommandFlags.Modal)]
    public static void Calculate()
    {
        var request = Pending.Take(ref NetworkPending.Calc);
        Commands.ShowPage("redes");
        var document = Active;
        if (request is null || document is null) return;
        Run(document, request);
    }

    private static void Run(Document document, NetworkRequest request)
    {
        var page = Commands.NetworksPage;
        if (page is null) return;
        try
        {
            var g = NetworkOps.Load(document, request);
            switch (request.Mode)
            {
                case NetworkMode.Sewer: page.ShowSewer(request, g, Sewer.Calculate(g, request.Sewer)); break;
                case NetworkMode.Trench: page.ShowTrench(request, g, Trench.Calculate(g, request.Trench), Trench.Quantities(g)); break;
                case NetworkMode.Rename: page.ShowRename(request, g, Renamer.Plan(g, request.Rename)); break;
            }
        }
        catch (System.Exception ex) { Report(document, "Cálculo não concluído: " + ex.Message, false); }
    }

    [CommandMethod("PROJETAREDESRENOMEAR", CommandFlags.Modal)]
    public static void Rename()
    {
        var request = Pending.Take(ref NetworkPending.Rename);
        Commands.ShowPage("redes");
        var document = Active;
        if (request is null || document is null) return;
        try
        {
            var message = NetworkOps.Rename(document, request);
            var g = NetworkOps.Load(document, request.Input);
            Commands.NetworksPage?.ShowRename(request.Input, g, Renamer.Plan(g, request.Input.Rename));
            Report(document, message, true);
        }
        catch (System.Exception ex) { Report(document, "Nada renomeado: " + ex.Message, false); }
    }

    [CommandMethod("PROJETAREDESASSOCIAR", CommandFlags.Modal)]
    public static void Associate()
    {
        var request = Pending.Take(ref NetworkPending.Associate);
        Commands.ShowPage("redes");
        var document = Active;
        if (request is null || document is null) return;
        try
        {
            var (message, rows) = NetworkOps.Associate(document, request);
            Commands.NetworksPage?.ShowAssociation(request, rows, message);
            document.Editor.WriteMessage($"\nPROJETA: {message}\n");
        }
        catch (System.Exception ex) { Report(document, "Nada associado: " + ex.Message, false); }
    }

    [CommandMethod("PROJETAREDESPONTUAL", CommandFlags.Modal)]
    public static void PointFlow()
    {
        var request = Pending.Take(ref NetworkPending.PointFlow);
        Commands.ShowPage("redes");
        var document = Active;
        if (request is null || document is null) return;
        try
        {
            var message = NetworkOps.SetPointFlow(document, request);
            Run(document, request.Input with { Mode = NetworkMode.Sewer });
            Commands.NetworksPage?.AddNote(message);
            document.Editor.WriteMessage($"\nPROJETA: {message}\n");
        }
        catch (System.Exception ex) { Report(document, "Vazão não gravada: " + ex.Message, false); }
    }
}
