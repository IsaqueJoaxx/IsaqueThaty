using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;
using Projeta.Giro.Core.Verificacao;

namespace Projeta.Giro.Infra;

/// <summary>Operações comuns aos comandos: tratamento de erro, escolha de veículo, seleção de manobra, gerar/desenhar.</summary>
internal static class Operacoes
{
    public static void Executar(string nome, Action acao)
    {
        try { acao(); }
        catch (ErroGiro ex) { Ctx.Msg($"{nome}: {ex.Message}"); }
        catch (Autodesk.AutoCAD.Runtime.Exception ex) { Ctx.Msg($"{nome}: erro do AutoCAD ({ex.ErrorStatus}) — {ex.Message}"); Pilha(ex); }
        catch (System.Exception ex) { Ctx.Msg($"{nome}: erro inesperado — {ex.GetType().Name}: {ex.Message}"); Pilha(ex); }
    }

    private static void Pilha(System.Exception ex)
    {
        // no console (testes) mostra onde foi; na interface, só as 3 primeiras linhas
        var linhas = (ex.StackTrace ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Take(Ctx.ComInterface ? 3 : 12);
        foreach (var l in linhas) Ctx.Msg("    " + l);
    }

    public static string Resumo(Veiculo v)
    {
        var e = v.Estatico();
        return $"{v.Id} — {v.Nome} | comprimento {Num.F(v.Comprimento)} m | largura {Num.F(v.Largura)} m | esterçamento máx {Num.F(e.Angulo, 1)}° | " +
               $"raio roda externa {Num.F(e.RodaExternaDianteira)} m | roda interna traseira {Num.F(e.RodaInternaTraseira)} m";
    }

    private static string Chave(string id)
    {
        var s = new string(id.Where(char.IsLetterOrDigit).ToArray());
        return s.Length == 0 || char.IsDigit(s[0]) ? "" : s;
    }

    /// <summary>Pergunta o veículo (palavras-chave com os códigos). Retorna null se cancelado.</summary>
    public static Veiculo? EscolherVeiculo()
    {
        var bib = Config.Biblioteca;
        var cfg = Config.Atual;
        Ctx.Msg("Veículos disponíveis:");
        foreach (var v in bib.Ordenados) Ctx.Msg($"  {(string.Equals(v.Id, cfg.VeiculoAtual, StringComparison.OrdinalIgnoreCase) ? "*" : " ")} {Resumo(v)}");
        var op = new PromptKeywordOptions("\nVeículo de projeto");
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in bib.Ordenados)
        {
            var k = Chave(v.Id);
            if (k.Length == 0 || mapa.ContainsKey(k)) continue;
            mapa[k] = v.Id;
            op.Keywords.Add(k);
        }
        op.Keywords.Add("Outro");
        var atual = Chave(cfg.VeiculoAtual);
        if (mapa.ContainsKey(atual)) op.Keywords.Default = atual;
        op.AllowNone = true;
        var r = Ctx.Ed.GetKeywords(op);
        if (r.Status == PromptStatus.None) return cfg.Veiculo();
        if (r.Status != PromptStatus.OK) return null;
        string id;
        if (r.StringResult == "Outro")
        {
            var s = Ctx.Ed.GetString(new PromptStringOptions("\nCódigo do veículo: ") { AllowSpaces = true });
            if (s.Status != PromptStatus.OK) return null;
            id = s.StringResult.Trim();
        }
        else id = mapa[r.StringResult];
        var veic = bib.Obter(id);
        if (veic is null) { Ctx.Msg($"veículo \"{id}\" não encontrado."); return null; }
        cfg.VeiculoAtual = veic.Id;
        cfg.Salvar();
        Ctx.Msg("Veículo atual: " + Resumo(veic));
        return veic;
    }

    public static bool PerguntarVelocidade()
    {
        var cfg = Config.Atual;
        var r = Ctx.Ed.GetDouble(new PromptDoubleOptions($"\nVelocidade de manobra (km/h) <{Num.F(cfg.Velocidade, 0)}>: ")
        { AllowNone = true, AllowNegative = false, AllowZero = false, DefaultValue = cfg.Velocidade, UseDefaultValue = true });
        if (r.Status != PromptStatus.OK) return false;
        cfg.Velocidade = r.Value;
        cfg.Salvar();
        return true;
    }

    /// <summary>Seleciona uma varredura desenhada (qualquer entidade dela) e carrega a manobra.</summary>
    public static (Manobra M, ObjectId Id)? SelecionarManobra(string prompt)
    {
        var op = new PromptEntityOptions("\n" + prompt);
        while (true)
        {
            var r = Ctx.Ed.GetEntity(op);
            if (r.Status != PromptStatus.OK) return null;
            using var tr = Ctx.Db.TransactionManager.StartTransaction();
            var e = (Entity)tr.GetObject(r.ObjectId, OpenMode.ForRead);
            var marca = Armazenamento.Marca(e);
            if (marca is null || marca.Length < 3) { Ctx.Msg("esse objeto não faz parte de uma varredura do PROJETA Giro."); continue; }
            var m = Armazenamento.Ler(tr, Ctx.Db, marca[2..]);
            tr.Commit();
            if (m is null) { Ctx.Msg("os dados dessa varredura não foram encontrados no desenho."); continue; }
            return (m, r.ObjectId);
        }
    }

    /// <summary>Simula, desenha, grava e informa. Retorna o resultado.</summary>
    public static ResultadoVarredura Gerar(Manobra m, bool substituir = false)
    {
        var r = Varredura.Simular(m);
        var doc = Ctx.Doc;
        using (doc.LockDocument(Autodesk.AutoCAD.ApplicationServices.DocumentLockMode.ProtectedAutoWrite, "GIRO", "GIRO", true))
        using (var tr = Ctx.Db.TransactionManager.StartTransaction())
        {
            if (substituir) { Desenho.ApagarVarredura(tr, Ctx.Db, m.Id); Desenho.ApagarVerificacao(tr, Ctx.Db, m.Id); }
            Desenho.Varredura(tr, Ctx.Db, r, Config.Atual.DesenharRodas);
            tr.Commit();
        }
        Relatar(r);
        return r;
    }

    public static void Relatar(ResultadoVarredura r)
    {
        var v = r.Manobra.Veiculo;
        Ctx.Msg($"Varredura {r.Manobra.Id}: {v.Id} — {Num.F(r.Comprimento, 1)} m percorridos, largura varrida máx {Num.F(r.LarguraMaxima)} m " +
                $"(a {Num.F(r.SLarguraMaxima, 0)} m do início), esterçamento máx {Num.F(Num.Graus(r.DeltaMaximo), 1)}° de {Num.F(Num.Graus(v.AnguloMaximo), 1)}°" +
                (v.Articulado ? $", articulação máx {Num.F(Num.Graus(r.ArticulacaoMaxima), 0)}°." : "."));
        foreach (var a in r.Avisos) Ctx.Msg("  " + a);
        if (r.Avisos.Count == 0) Ctx.Msg("  Sem avisos: o veículo faz a manobra dentro dos limites de esterçamento.");
    }

    /// <summary>Verifica a varredura contra os obstáculos, desenha os conflitos e informa.</summary>
    public static ResultadoConflito Verificar(Manobra m, List<Obstaculo> obs, double folga, bool desenhar = true)
    {
        var r = Varredura.Simular(m, new OpcoesVarredura { CalcularLargura = false });
        var corpo = Conflitos.Verificar(r.Envoltoria, obs, folga);
        var rodas = Conflitos.Verificar(r.Rodas, obs, 0);
        if (desenhar)
        {
            using (Ctx.Doc.LockDocument(Autodesk.AutoCAD.ApplicationServices.DocumentLockMode.ProtectedAutoWrite, "GIROVERIFICAR", "GIROVERIFICAR", true))
            using (var tr = Ctx.Db.TransactionManager.StartTransaction())
            {
                Desenho.ApagarVerificacao(tr, Ctx.Db, m.Id);
                Desenho.Conflitos(tr, Ctx.Db, m.Id, corpo, folga);
                tr.Commit();
            }
        }
        Ctx.Msg($"Verificação {m.Veiculo.Id} ({m.Id}) contra {obs.Count} objeto(s):");
        Ctx.Msg("  Carroceria: " + corpo.Resumo(folga));
        Ctx.Msg("  Rodas: " + (rodas.TemConflito
            ? $"{rodas.Invasoes.Count} trecho(s) em que o pneu passa sobre a linha ({Num.F(rodas.ComprimentoInvadido)} m) — confira se é meio-fio transponível."
            : "nenhuma roda cruza os objetos selecionados."));
        if (corpo.TemConflito) Ctx.Msg("  Conflitos em vermelho no layer " + Desenho.LConflito + (folga > 0 ? "; trechos sem folga em amarelo no " + Desenho.LFolga : "") + ".");
        return corpo;
    }

    public static Point3d? Ponto(string msg, Point3d? baseUcs = null)
    {
        var o = new PromptPointOptions("\n" + msg);
        if (baseUcs is Point3d b) { o.BasePoint = b; o.UseBasePoint = true; }
        var r = Ctx.Ed.GetPoint(o);
        return r.Status == PromptStatus.OK ? r.Value : null;
    }

    /// <summary>Direção (rad, no sistema universal) entre dois pontos do UCS.</summary>
    public static double Direcao(Point3d aUcs, Point3d bUcs) => (Ctx.V(Ctx.Wcs(bUcs)) - Ctx.V(Ctx.Wcs(aUcs))).Angulo;
}
