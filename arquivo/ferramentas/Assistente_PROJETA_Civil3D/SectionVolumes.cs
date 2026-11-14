using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using CivilSection = Autodesk.Civil.DatabaseServices.Section;

namespace Projeta.Civil3D;

// Volume a partir das seções que o Civil 3D já amostrou num grupo de linhas de amostragem.
// Lê, em cada linha, a seção do terreno e a do projeto, calcula corte e aterro na largura comum e
// soma pelo método das áreas médias. Se o grupo tiver lista de materiais calculada no Civil 3D,
// lê também os volumes dela para conferência. Nada é gravado: a transação é sempre abortada.
internal static class SectionVolumeOps
{
    // Fontes que viram uma linha de terreno: superfícies e superfícies de corredor.
    // (Corredor, rede e material têm outra geometria e ficam de fora.)
    public static bool Usable(SectionSourceType type) =>
        type is SectionSourceType.TinSurface or SectionSourceType.GridSurface or SectionSourceType.CorridorSurface;

    public const string CorridorKind = "superfície de corredor";

    public static string KindLabel(SectionSourceType type) => type switch
    {
        SectionSourceType.TinSurface => "TIN",
        SectionSourceType.GridSurface => "grade",
        SectionSourceType.CorridorSurface => CorridorKind,
        _ => type.ToString()
    };

    // Chave de uma fonte amostrada: objeto de origem + superfície de corredor, porque duas superfícies
    // do mesmo corredor podem ter a mesma origem. A mesma chave serve na leitura do painel e no cálculo.
    public static string Key(CivilSection section)
    {
        string H(Func<ObjectId> get)
        {
            try { var id = get(); return id.IsNull ? "0" : id.Handle.ToString(); }
            catch { return "0"; }
        }
        return H(() => section.SourceId) + ":" + H(() => section.CorridorSurfaceId);
    }

    public static string SourceName(CivilSection section)
    {
        try { if (!string.IsNullOrWhiteSpace(section.SourceName)) return section.SourceName; } catch { }
        return section.Name;
    }

    public static bool IsUsable(CivilSection section)
    {
        if (section is MaterialSection or CorridorSection) return false;
        try { return Usable(section.SourceType); }
        catch { return false; }
    }

    // Algumas propriedades não existem em todo tipo de seção (LeftOffset, por exemplo, falha numa seção de superfície).
    private static bool IsStatic(CivilSection? section)
    {
        try { return section?.UpdateMode == SectionUpdateType.Static; }
        catch { return false; }
    }

    // Pontos da seção: X = afastamento (negativo à esquerda), Y = cota; SegmentTo diz a qual ponto cada um se liga.
    public static SectionLine Line(CivilSection section)
    {
        var collection = section.SectionPoints;
        var points = new List<(double X, double Z)>(collection.Count);
        var next = new List<int>(collection.Count);
        for (var i = 0; i < collection.Count; i++)
        {
            var p = collection[i];
            points.Add((p.Location.X, p.Location.Y));
            next.Add(p.SegmentTo);
        }
        return SectionLine.FromLinks(points, next);
    }

    public static SectionVolumeResult Run(Document document, SectionVolumeRequest request)
    {
        var db = document.Database;
        var groupId = Ids.Resolve(db, request.GroupHandle, "o grupo de linhas de amostragem");
        var stations = new List<SectionStation>();
        var warnings = new List<string>();
        var checks = new List<MaterialListCheck>();
        string groupName, alignmentName = "—", baseName = request.BaseName, comparisonName = request.ComparisonName;
        int missingBase = 0, missingComparison = 0, emptyBase = 0, emptyComparison = 0, noOverlap = 0;
        string? staticNote = null;

        // Primeiro só toca nas seções e aborta. Com atualização pendente no Civil 3D (corredor reconstruído no mesmo
        // comando, por exemplo), as seções vieram vazias na primeira leitura e certas na seguinte (teste da Prévia 18).
        using (var warm = db.TransactionManager.StartTransaction())
        {
            var group = (SampleLineGroup)warm.GetObject(groupId, OpenMode.ForRead);
            foreach (ObjectId id in group.GetSampleLineIds())
            {
                if (id.IsNull || id.IsErased || warm.GetObject(id, OpenMode.ForRead) is not SampleLine line) continue;
                foreach (ObjectId sid in line.GetSectionIds())
                    if (!sid.IsNull && !sid.IsErased && warm.GetObject(sid, OpenMode.ForRead) is CivilSection section && IsUsable(section))
                        _ = section.SectionPoints.Count;
            }
            warm.Abort();
        }

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var group = (SampleLineGroup)tr.GetObject(groupId, OpenMode.ForRead);
            groupName = group.Name;
            try { alignmentName = ((Alignment)tr.GetObject(group.ParentAlignmentId, OpenMode.ForRead)).Name; } catch { }

            foreach (ObjectId id in group.GetSampleLineIds())
            {
                if (id.IsNull || id.IsErased || tr.GetObject(id, OpenMode.ForRead) is not SampleLine line) continue;
                var station = line.Station;
                if (station < request.Start - 1e-6 || station > request.End + 1e-6) continue;
                CivilSection? g = null, d = null;
                foreach (ObjectId sid in line.GetSectionIds())
                {
                    if (sid.IsNull || sid.IsErased || tr.GetObject(sid, OpenMode.ForRead) is not CivilSection section) continue;
                    // Seções de material (lista de materiais) apontam para a mesma superfície e viriam com a mesma chave.
                    if (!IsUsable(section)) continue;
                    var key = Key(section);
                    if (key == request.BaseKey) g ??= section;
                    else if (key == request.ComparisonKey) d ??= section;
                }
                if (g is not null) baseName = SourceName(g);
                if (d is not null) comparisonName = SourceName(d);
                if (staticNote is null && (IsStatic(g) || IsStatic(d)))
                    staticNote = "Há seções estáticas no grupo: elas não acompanham mudanças nas superfícies. " +
                                 "Se a superfície mudou depois da amostragem, atualize as seções antes de usar os volumes.";

                if (g is null || d is null)
                {
                    if (g is null) missingBase++;
                    if (d is null) missingComparison++;
                    var what = g is null && d is null ? "Sem as duas seções" : g is null ? "Sem seção do terreno" : "Sem seção do projeto";
                    stations.Add(new SectionStation(station, line.Name, null, null, what + "; área contada como zero."));
                    continue;
                }
                var groundLine = Line(g);
                var designLine = Line(d);
                var areas = SectionMath.Compare(groundLine, designLine);
                string? note = null;
                if (groundLine.IsEmpty || designLine.IsEmpty)
                {
                    if (groundLine.IsEmpty) emptyBase++;
                    if (designLine.IsEmpty) emptyComparison++;
                    note = (groundLine.IsEmpty && designLine.IsEmpty ? "As duas seções estão vazias"
                        : groundLine.IsEmpty ? "Seção do terreno vazia" : "Seção do projeto vazia") +
                           " nesta linha (a superfície não a cobre); área zero.";
                }
                else if (areas.Width <= 1e-6)
                {
                    noOverlap++;
                    note = "As seções do terreno e do projeto não se sobrepõem; área zero.";
                }
                stations.Add(new SectionStation(station, line.Name, areas.Cut, areas.Fill, note));
            }

            if (stations.Count == 0)
                throw new InvalidOperationException("Nenhuma linha de amostragem do grupo está no trecho escolhido.");

            // Lista de materiais do Civil 3D, se existir: só leitura, para conferência.
            string[] lists;
            try { lists = group.GetQTOMappingNames() ?? Array.Empty<string>(); }
            catch { lists = Array.Empty<string>(); }
            foreach (var name in lists)
                checks.Add(ReadMaterialList(group, name, request.Start, request.End));

            tr.Abort();
        }

        var rows = SectionMath.AverageEndArea(stations);
        if (rows.Count < 2) warnings.Add("Só uma linha de amostragem no trecho: sem distância entre seções, o volume é zero.");
        if (missingBase > 0) warnings.Add($"{missingBase} linha(s) sem seção do terreno; nelas a área entrou como zero. " +
                                          "Confira se a superfície está amostrada em todo o grupo.");
        if (missingComparison > 0) warnings.Add($"{missingComparison} linha(s) sem seção do projeto; nelas a área entrou como zero.");
        if (emptyBase > 0) warnings.Add($"{emptyBase} linha(s) com a seção do terreno vazia: a superfície não cobre essas linhas. Área zero nelas.");
        if (emptyComparison > 0)
            warnings.Add(emptyComparison == rows.Count
                ? "A seção do projeto está vazia em todas as linhas. Confira se a superfície do projeto tem triângulos " +
                  "(numa superfície de corredor, se ela está marcada para construir e o corredor foi reconstruído)."
                : $"{emptyComparison} linha(s) com a seção do projeto vazia: a superfície não cobre essas linhas. Área zero nelas.");
        if (noOverlap > 0) warnings.Add($"{noOverlap} linha(s) onde as seções do terreno e do projeto não se sobrepõem (área zero).");
        if (staticNote is not null) warnings.Add(staticNote);
        warnings.AddRange(SectionMath.SpacingWarnings(rows.Select(r => r.Station).ToList()));

        return new SectionVolumeResult(groupName, alignmentName, baseName, comparisonName,
            rows.Count > 0 ? rows[0].Station : request.Start, rows.Count > 0 ? rows[^1].Station : request.End,
            rows, warnings, request.CutFactor, request.FillFactor, checks);
    }

    private static MaterialListCheck ReadMaterialList(SampleLineGroup group, string name, double start, double end)
    {
        var method = "";
        var factors = new List<string>();
        var cutFactors = new List<double>();
        var fillFactors = new List<double>();
        try
        {
            var lists = group.MaterialLists;
            method = lists.VolumeCalculationMethodType switch
            {
                MaterialVolumeCalculationMethodType.AverageEndArea => "áreas médias",
                MaterialVolumeCalculationMethodType.Prismoidal => "prismoidal",
                MaterialVolumeCalculationMethodType.CompositeVolume => "volume composto",
                var other => other.ToString()
            };
            foreach (QTOMaterialList list in lists)
            {
                if (!string.Equals(list.Name, name, StringComparison.Ordinal)) continue;
                if (list.IsCurveCorrectionEnabled) method += ", com correção de curva";
                foreach (QTOMaterial material in list)
                {
                    var parts = new List<string>();
                    if (material.IsFactorApplicable(MaterialFactorType.Cut))
                    {
                        var f = material.GetFactor(MaterialFactorType.Cut);
                        cutFactors.Add(f);
                        parts.Add("corte ×" + Estacas.N(f, 3));
                    }
                    if (material.IsFactorApplicable(MaterialFactorType.Fill))
                    {
                        var f = material.GetFactor(MaterialFactorType.Fill);
                        fillFactors.Add(f);
                        parts.Add("aterro ×" + Estacas.N(f, 3));
                    }
                    factors.Add(parts.Count == 0 ? material.Name : $"{material.Name} ({string.Join(", ", parts)})");
                }
            }
        }
        catch { }
        try
        {
            var result = group.GetTotalVolumeResultDataForMaterialList(group.GetMappingGuid(name));
            var raw = result.GetResultsAlongSampleLines().Select(r => (r.Station, r.SampleLineName ?? "",
                r.AreaResult.CutArea, r.AreaResult.FillArea, r.VolumeResult.IncrementalCutVolume, r.VolumeResult.IncrementalFillVolume));
            var rows = SectionMath.Clip(raw, start, end);
            return new MaterialListCheck(name, method, string.Join("; ", factors), rows, null,
                cutFactors.Count == 1 ? cutFactors[0] : null, fillFactors.Count == 1 ? fillFactors[0] : null);
        }
        catch (Exception ex)
        {
            return new MaterialListCheck(name, method, string.Join("; ", factors), Array.Empty<SectionVolumeRow>(),
                "não foi possível ler os volumes desta lista (" + ex.Message + "). Calcule os materiais de novo no Civil 3D.");
        }
    }
}
