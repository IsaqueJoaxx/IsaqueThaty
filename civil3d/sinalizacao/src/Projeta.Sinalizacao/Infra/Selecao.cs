using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Projeta.Sinalizacao.Civil;
using Projeta.Sinalizacao.Core.Comum;

namespace Projeta.Sinalizacao.Infra;

/// <summary>Caminho escolhido pelo usuário (polilinha, linha, arco, alinhamento ou pontos clicados).</summary>
internal sealed record CaminhoEscolhido(List<Vertice> Vertices, bool Fechado, ObjectId Alinhamento, ObjectId Origem, string Nome);

/// <summary>Entradas na linha de comando e conversão de curvas em caminhos.</summary>
internal static class Selecao
{
    public static List<Vertice> VerticesDe(Polyline pl)
    {
        var l = new List<Vertice>();
        for (int i = 0; i < pl.NumberOfVertices; i++)
        {
            var p = pl.GetPoint2dAt(i);
            l.Add(new Vertice(p.X, p.Y, pl.GetBulgeAt(i)));
        }
        if (pl.Closed && l.Count > 0) { /* o Caminho trata o fechamento */ }
        return l;
    }

    /// <summary>Converte uma curva do DWG em vértices (polilinha, linha, arco ou amostragem de outras curvas).</summary>
    public static (List<Vertice> V, bool Fechado)? VerticesDeCurva(Transaction tr, Entity e)
    {
        switch (e)
        {
            case Polyline pl: return (VerticesDe(pl), pl.Closed);
            case Line ln: return (new List<Vertice> { new(ln.StartPoint.X, ln.StartPoint.Y), new(ln.EndPoint.X, ln.EndPoint.Y) }, false);
            case Arc a:
            {
                double sweep = a.EndAngle - a.StartAngle;
                if (sweep <= 0) sweep += 2 * Math.PI;
                double b = Math.Tan(sweep / 4);
                if (a.Normal.Z < 0) b = -b;
                return (new List<Vertice> { new(a.StartPoint.X, a.StartPoint.Y, b), new(a.EndPoint.X, a.EndPoint.Y) }, false);
            }
            case Curve c:
            {
                double L = c.GetDistanceAtParameter(c.EndParam);
                var l = new List<Vertice>();
                double passo = Math.Max(0.25, Math.Min(1.0, L / 2000));
                for (double s = 0; s < L; s += passo) { var p = c.GetPointAtDist(s); l.Add(new Vertice(p.X, p.Y)); }
                var f = c.EndPoint; l.Add(new Vertice(f.X, f.Y));
                return (l, c.Closed);
            }
        }
        return null;
    }

    public static CaminhoEscolhido? PedirCaminho(Editor ed, Transaction tr, string msg, bool permitirPontos = true)
    {
        while (true)
        {
            var peo = new PromptEntityOptions($"\n{msg}" + (permitirPontos ? " ou [Pontos]: " : ": "), permitirPontos ? "Pontos" : "");
            peo.AllowNone = false;
            var r = ed.GetEntity(peo);
            if (r.Status == PromptStatus.Keyword) return PedirPontos(ed, "Primeiro ponto do caminho", "Próximo ponto");
            if (r.Status != PromptStatus.OK) return null;
            var ent = (Entity)tr.GetObject(r.ObjectId, OpenMode.ForRead);
            if (Eixos.EhAlinhamento(r.ObjectId))
            {
                var v = Eixos.Vertices(tr, r.ObjectId);
                return new CaminhoEscolhido(v, false, r.ObjectId, r.ObjectId, Eixos.Nome(tr, r.ObjectId));
            }
            var vc = VerticesDeCurva(tr, ent);
            if (vc is { V.Count: >= 2 }) return new CaminhoEscolhido(vc.Value.V, vc.Value.Fechado, ObjectId.Null, r.ObjectId, "");
            Ctx.Msg("Selecione uma polilinha, linha, arco, curva ou alinhamento.");
        }
    }

    public static CaminhoEscolhido? PedirPontos(Editor ed, string msg1, string msgN)
    {
        var p1 = Ponto(ed, msg1);
        if (p1 is null) return null;
        var l = new List<Vertice> { new(p1.Value.X, p1.Value.Y) };
        var ultimo = p1.Value;
        while (true)
        {
            var o = new PromptPointOptions($"\n{msgN} <terminar>: ") { AllowNone = true, UseBasePoint = true, BasePoint = Ctx.P(ultimo) };
            var r = ed.GetPoint(o);
            if (r.Status != PromptStatus.OK) break;
            ultimo = Ctx.V(r.Value);
            l.Add(new Vertice(ultimo.X, ultimo.Y));
        }
        return l.Count >= 2 ? new CaminhoEscolhido(l, false, ObjectId.Null, ObjectId.Null, "") : null;
    }

    public static V2? Ponto(Editor ed, string msg, V2? baseP = null)
    {
        var o = new PromptPointOptions($"\n{msg}: ") { AllowNone = true };
        if (baseP.HasValue) { o.UseBasePoint = true; o.BasePoint = Ctx.P(baseP.Value); }
        var r = ed.GetPoint(o);
        return r.Status == PromptStatus.OK ? Ctx.V(r.Value) : null;
    }

    public static double? Angulo(Editor ed, string msg, V2 baseP, double padrao)
    {
        var o = new PromptAngleOptions($"\n{msg}: ") { AllowNone = true, UseBasePoint = true, BasePoint = Ctx.P(baseP), DefaultValue = padrao, UseDefaultValue = true };
        var r = ed.GetAngle(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.Value : null;
    }

    public static double? Numero(Editor ed, string msg, double padrao, bool negativo = false)
    {
        // o AutoCAD acrescenta o valor padrão ao final da mensagem
        var o = new PromptDoubleOptions($"\n{msg}: ")
        {
            AllowNone = true, DefaultValue = padrao, UseDefaultValue = true, AllowNegative = negativo, AllowZero = true,
        };
        var r = ed.GetDouble(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.Value : null;
    }

    public static int? Inteiro(Editor ed, string msg, int padrao, int min = 1, int max = 1000)
    {
        var o = new PromptIntegerOptions($"\n{msg}: ") { AllowNone = true, DefaultValue = padrao, UseDefaultValue = true, LowerLimit = min, UpperLimit = max };
        var r = ed.GetInteger(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.Value : null;
    }

    public static string? Texto(Editor ed, string msg, string padrao, bool espacos = true)
    {
        var o = new PromptStringOptions($"\n{msg}: ") { AllowSpaces = espacos, DefaultValue = padrao, UseDefaultValue = true };
        var r = ed.GetString(o);
        if (r.Status != PromptStatus.OK) return null;
        return string.IsNullOrWhiteSpace(r.StringResult) ? padrao : r.StringResult.Trim();
    }

    /// <summary>Palavra-chave (sem hífen). Retorna a opção escolhida ou null se cancelado.</summary>
    public static string? Palavra(Editor ed, string msg, string[] opcoes, string padrao)
    {
        var o = new PromptKeywordOptions($"\n{msg} [{string.Join("/", opcoes)}] <{padrao}>: ", string.Join(" ", opcoes)) { AllowNone = true };
        if (opcoes.Contains(padrao)) o.Keywords.Default = padrao;
        var r = ed.GetKeywords(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.StringResult : null;
    }

    public static int Lista(Editor ed, string titulo, IList<string> itens, int padrao = 0)
    {
        if (itens.Count == 0) return -1;
        Ctx.Msg($"--- {titulo} ---");
        for (int i = 0; i < itens.Count; i++) Ctx.Msg($"  {i + 1}) {itens[i]}");
        var r = Inteiro(ed, $"Número [1-{itens.Count}]", padrao + 1, 1, itens.Count);
        return r is null ? -1 : r.Value - 1;
    }

    public static List<ObjectId> Selecionar(Editor ed, string msg)
    {
        var pso = new PromptSelectionOptions { MessageForAdding = $"\n{msg}: " };
        // referências (objetos 2D) e sólidos 3D gerados pelo SIN3D (mapeados para o objeto de origem)
        var f = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT,3DSOLID") });
        var r = ed.GetSelection(pso, f);
        return r.Status == PromptStatus.OK ? r.Value.GetObjectIds().ToList() : new List<ObjectId>();
    }
}
