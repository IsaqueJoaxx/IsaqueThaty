using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Projeta.Sinalizacao.Core.Dados;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Infra;

/// <summary>Contexto de um comando: documento, transação aberta e padrões do desenho.</summary>
internal sealed class Sessao : IDisposable
{
    public Document Doc { get; }
    public Database Db { get; }
    public Editor Ed { get; }
    public Transaction Tr { get; }
    public Config Cfg { get; }
    public N Normas => Cfg.Normas();
    public List<string> Avisos { get; } = new();
    private readonly DocumentLock? _trava;

    private Sessao(bool travar)
    {
        Doc = Ctx.Doc; Db = Doc.Database; Ed = Doc.Editor;
        if (travar) _trava = Doc.LockDocument();
        Tr = Db.TransactionManager.StartTransaction();
        Cfg = Config.Ler(Tr, Db);
    }

    public static Sessao Abrir(bool travar = false) => new(travar);

    /// <summary>Cria um objeto novo (ID, situação padrão) e o desenha.</summary>
    public ObjectId Criar(ObjetoSin o)
    {
        if (string.IsNullOrWhiteSpace(o.Id)) o.Id = Cfg.ProximoId(o.Categoria);
        return Gerador.Materializar(Tr, Db, Cfg, o, ObjectId.Null, Avisos);
    }

    /// <summary>Lê os dados de uma referência, acompanha MOVE/ROTATE e regenera.</summary>
    public bool Regenerar(ObjectId id)
    {
        var br = Tr.GetObject(id, OpenMode.ForRead) as BlockReference;
        if (br is null) return false;
        var o = Armazenamento.Ler(Tr, br);
        if (o is null) return false;
        o.Sincronizar(br.Position.X, br.Position.Y, br.Rotation);
        Gerador.Materializar(Tr, Db, Cfg, o, id, Avisos);
        return true;
    }

    public void Atualizar(ObjectId id, ObjetoSin o) => Gerador.Materializar(Tr, Db, Cfg, o, id, Avisos);

    public ObjetoSin? Ler(ObjectId id)
    {
        var br = Tr.GetObject(id, OpenMode.ForRead) as BlockReference;
        if (br is null) return null;
        var o = Armazenamento.Ler(Tr, br);
        o?.Sincronizar(br.Position.X, br.Position.Y, br.Rotation);
        return o;
    }

    /// <summary>Se o objeto for um sólido 3D gerado pelo SIN3D, devolve a referência de origem.</summary>
    public ObjectId Origem(ObjectId id)
    {
        if (Tr.GetObject(id, OpenMode.ForRead) is BlockReference) return id;
        if (Tr.GetObject(id, OpenMode.ForRead) is Entity e && !e.ExtensionDictionary.IsNull)
        {
            var h = Armazenamento.LerTexto(Tr, e, Armazenamento.Chave3D);
            var o = Ctx.IdDeHandle(Db, h);
            if (!o.IsNull) return o;
        }
        return id;
    }

    public (ObjectId Id, ObjetoSin Obj)? PedirObjeto(string msg)
    {
        while (true)
        {
            var r = Ed.GetEntity(new PromptEntityOptions($"\n{msg}: ") { AllowNone = true });
            if (r.Status != PromptStatus.OK) return null;
            var id = Origem(r.ObjectId);
            var o = Ler(id);
            if (o is not null) return (id, o);
            Ctx.Msg("Esse objeto não é da sinalização PROJETA.");
        }
    }

    public List<(ObjectId Id, ObjetoSin Obj)> SelecionarObjetos(string msg, bool vazioTodos = true)
    {
        var ids = Selecao.Selecionar(Ed, msg + (vazioTodos ? " <Enter = todos>" : ""));
        var l = new List<(ObjectId, ObjetoSin)>();
        if (ids.Count == 0 && vazioTodos) return Armazenamento.Todos(Tr, Db);
        var vistos = new HashSet<ObjectId>();
        foreach (var id0 in ids)
        {
            var id = Origem(id0);
            if (!vistos.Add(id)) continue;
            var o = Ler(id);
            if (o is not null) l.Add((id, o));
        }
        return l;
    }

    public void MostrarAvisos()
    {
        foreach (var a in Avisos.Distinct()) Ctx.Msg("[AVISO] " + a);
    }

    public void Confirmar()
    {
        Cfg.Gravar(Tr, Db);
        Tr.Commit();
        MostrarAvisos();
    }

    public void Dispose()
    {
        Tr.Dispose();
        _trava?.Dispose();
    }
}
