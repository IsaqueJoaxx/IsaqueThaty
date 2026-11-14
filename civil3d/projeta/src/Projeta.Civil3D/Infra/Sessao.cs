using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Projeta.Core.Longitudinais;
using Projeta.Core.Modelador;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Infra;

/// <summary>Contexto de um comando: documento, transação aberta e projeto carregado do DWG.</summary>
internal sealed class Sessao : IDisposable
{
    public Document Doc { get; }
    public Database Db { get; }
    public Editor Ed { get; }
    public Transaction Tr { get; }
    public Projeto Projeto { get; }
    public static Biblioteca Bib => _bib ??= CarregarBiblioteca();
    private static Biblioteca? _bib;

    private Sessao()
    {
        Doc = Ctx.Doc; Db = Doc.Database; Ed = Doc.Editor;
        Tr = Db.TransactionManager.StartTransaction();
        Projeto = Armazenamento.Carregar(Tr, Db);
    }

    public static Sessao Abrir() => new();

    public static Biblioteca CarregarBiblioteca()
    {
        var b = Biblioteca.Carregar(Config.Atual.PastaBiblioteca);
        foreach (var m in b.Mensagens) Ctx.Msg("[BIBLIOTECA] " + m);
        return b;
    }

    public static void RecarregarBiblioteca() => _bib = CarregarBiblioteca();

    /// <summary>Grava o projeto no DWG e confirma a transação.</summary>
    public void SalvarEConfirmar()
    {
        Armazenamento.Salvar(Tr, Db, Projeto);
        Tr.Commit();
    }

    public void Dispose() => Tr.Dispose();

    // ------------------------------------------------------------------ localizar objetos do projeto

    public sealed record Item(Rede? Rede, No? No, Trecho? Trecho, Bacia? Bacia, DispositivoLongitudinal? Longitudinal);

    public Item? Localizar(ObjectId id)
    {
        if (id.IsNull) return null;
        var h = id.Handle.ToString();
        foreach (var r in Projeto.Redes)
        {
            foreach (var n in r.Nos)
                if (n.HandleBloco == h || n.HandleRotulo == h || n.HandleSolido == h) return new Item(r, n, null, null, null);
            foreach (var t in r.Trechos)
                if (t.HandleLinha == h || t.HandleRotulo == h || t.HandleSolido == h) return new Item(r, null, t, null, null);
            foreach (var b in r.Bacias)
                if (b.HandleContorno == h || b.HandleRotulo == h || b.HandleTalvegue == h) return new Item(r, null, null, b, null);
        }
        foreach (var d in Projeto.Longitudinais)
            if (d.HandlesDesenho.Contains(h)) return new Item(null, null, null, null, d);
        return null;
    }

    /// <summary>Pede ao usuário um objeto do PROJETA (nó, tubo, bacia ou longitudinal).</summary>
    public Item? PedirItem(string mensagem)
    {
        while (true)
        {
            var peo = new PromptEntityOptions("\n" + mensagem + ": ");
            peo.AllowNone = true;
            var r = Ed.GetEntity(peo);
            if (r.Status != PromptStatus.OK) return null;
            var it = Localizar(r.ObjectId);
            if (it is not null) return it;
            Ctx.Msg("O objeto selecionado não pertence a nenhuma rede do PROJETA.");
        }
    }

    public List<Item> SelecionarItens(string mensagem)
    {
        var pso = new PromptSelectionOptions { MessageForAdding = "\n" + mensagem + ": " };
        var r = Ed.GetSelection(pso);
        var lista = new List<Item>();
        if (r.Status != PromptStatus.OK) return lista;
        foreach (var id in r.Value.GetObjectIds())
        {
            var it = Localizar(id);
            if (it is null) continue;
            if (lista.Any(x => ReferenceEquals(x.No, it.No) && ReferenceEquals(x.Trecho, it.Trecho) && ReferenceEquals(x.Bacia, it.Bacia) && ReferenceEquals(x.Longitudinal, it.Longitudinal))) continue;
            lista.Add(it);
        }
        return lista;
    }

    /// <summary>Escolhe uma rede: única → direto; senão clique em um elemento dela ou escolha na lista.</summary>
    public Rede? EscolherRede(string mensagem = "Selecione um elemento da rede", Func<Rede, bool>? filtro = null)
    {
        var redes = Projeto.Redes.Where(r => filtro?.Invoke(r) ?? true).ToList();
        if (redes.Count == 0) { Ctx.Msg("Nenhuma rede compatível neste desenho. Use PRJREDE para criar uma."); return null; }
        if (redes.Count == 1) { Ctx.Msg($"Rede: {redes[0].Nome}"); return redes[0]; }
        var peo = new PromptEntityOptions($"\n{mensagem} ou [Lista]: ", "Lista");
        peo.AllowNone = true;
        var r = Ed.GetEntity(peo);
        if (r.Status == PromptStatus.OK)
        {
            var it = Localizar(r.ObjectId);
            if (it?.Rede is not null && redes.Contains(it.Rede)) return it.Rede;
            Ctx.Msg("O objeto não pertence a uma rede compatível; escolha na lista.");
        }
        else if (r.Status != PromptStatus.Keyword && r.Status != PromptStatus.None) return null;
        int idx = EscolherDaLista("Redes", redes.Select(x => $"{x.Nome} ({x.Tipo}, {x.Nos.Count} nós, {x.Trechos.Count} trechos)").ToList());
        return idx < 0 ? null : redes[idx];
    }

    public int EscolherDaLista(string titulo, IList<string> itens, int padrao = 0)
    {
        if (itens.Count == 0) return -1;
        Ctx.Msg($"--- {titulo} ---");
        for (int i = 0; i < itens.Count; i++) Ctx.Msg($"  {i + 1}) {itens[i]}");
        var pio = new PromptIntegerOptions($"\nNúmero [1-{itens.Count}] <{padrao + 1}>: ")
        {
            AllowNone = true, LowerLimit = 1, UpperLimit = itens.Count, DefaultValue = padrao + 1, UseDefaultValue = true,
        };
        var r = Ed.GetInteger(pio);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.Value - 1 : -1;
    }

    public double? PedirNumero(string msg, double padrao, bool permitirNegativo = false)
    {
        var o = new PromptDoubleOptions($"\n{msg} <{padrao.ToString("0.###", Projeta.Core.Comum.Num.Br)}>: ")
        {
            AllowNone = true, DefaultValue = padrao, UseDefaultValue = true, AllowNegative = permitirNegativo, AllowZero = true,
        };
        var r = Ed.GetDouble(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.Value : null;
    }

    public string? PedirTexto(string msg, string padrao)
    {
        var o = new PromptStringOptions($"\n{msg} <{padrao}>: ") { AllowSpaces = true, DefaultValue = padrao, UseDefaultValue = true };
        var r = Ed.GetString(o);
        if (r.Status != PromptStatus.OK) return null;
        return string.IsNullOrWhiteSpace(r.StringResult) ? padrao : r.StringResult.Trim();
    }

    public string? PedirPalavra(string msg, string[] opcoes, string padrao)
    {
        var o = new PromptKeywordOptions($"\n{msg} [{string.Join("/", opcoes)}] <{padrao}>: ", string.Join(" ", opcoes)) { AllowNone = true };
        var r = Ed.GetKeywords(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.StringResult : null;
    }
}
