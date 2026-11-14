using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Projeta.Mcp;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D.Mcp;

/// <summary>
/// Executa as ferramentas na thread principal do AutoCAD, que é a única onde a API pode ser usada.
/// As chamadas chegam por TCP numa thread de fundo; um Control invisível criado na thread principal recebe
/// BeginInvoke e a ferramenta roda quando o AutoCAD processa a fila de mensagens.
/// Regras de segurança aplicadas a toda chamada:
///   - recusa se houver comando em andamento (não interfere no que o usuário está fazendo);
///   - trava o documento (leitura ou escrita) durante a execução;
///   - quem altera o desenho trava com nome de comando PRJMCP_* → um passo de UNDO por chamada.
/// </summary>
internal sealed class ExecutorCivil : IExecutor, IDisposable
{
    private readonly System.Windows.Forms.Control _ctl;

    /// <summary>Precisa ser criado na thread principal.</summary>
    public ExecutorCivil()
    {
        _ctl = new System.Windows.Forms.Control();
        _ = _ctl.Handle; // cria a janela agora, nesta thread
    }

    public Execucao Agendar(Ferramenta f, Argumentos a)
    {
        var e = new Execucao();
        try
        {
            _ctl.BeginInvoke(new Action(() =>
            {
                if (!e.TentarIniciar()) return; // cancelada por tempo antes de começar
                ResultadoFerramenta r;
                try { r = ExecutarAqui(f, a, emComando: false); }
                catch (Exception ex) { r = ResultadoFerramenta.Falha("Erro no Civil 3D: " + ex.Message); }
                e.Concluir(r);
            }));
        }
        catch (Exception ex)
        {
            if (e.TentarIniciar()) e.Concluir(ResultadoFerramenta.Falha("Não foi possível agendar a chamada no Civil 3D: " + ex.Message));
        }
        return e;
    }

    /// <summary>Executa na thread atual, que precisa ser a principal. <paramref name="emComando"/> = já dentro de um comando (PRJMCPTESTE).</summary>
    public static ResultadoFerramenta ExecutarAqui(Ferramenta f, Argumentos a, bool emComando)
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        if (doc is null) return ResultadoFerramenta.Falha("Nenhum desenho aberto no Civil 3D. Peça ao usuário para abrir o DWG.");
        if (!emComando)
        {
            string cmd = "";
            try { cmd = doc.CommandInProgress ?? ""; } catch { }
            if (cmd.Length > 0)
                return ResultadoFerramenta.Falha($"O Civil 3D está ocupado com o comando {cmd}. Peça ao usuário para terminar ou cancelar (Esc) e tente de novo.");
        }

        var sw = Stopwatch.StartNew();
        ResultadoFerramenta r;
        DocumentLock? trava = null;
        try
        {
            if (!emComando)
                trava = f.AlteraDesenho
                    ? doc.LockDocument(DocumentLockMode.Write, NomeUndo(f), NomeUndo(f), false)
                    : doc.LockDocument(DocumentLockMode.Read, null!, null!, false);
            r = f.ExecutarSeguro(a);
        }
        catch (Exception ex) { r = ResultadoFerramenta.Falha($"Erro no Civil 3D ao executar {f.Nome}: {ex.Message}"); }
        finally { trava?.Dispose(); }
        sw.Stop();
        Registrar(doc, f, r, sw.ElapsedMilliseconds, emComando);
        return r;
    }

    private static string NomeUndo(Ferramenta f) => "PRJMCP_" + f.Nome.ToUpperInvariant();

    /// <summary>Deixa rastro na linha de comando de tudo que o Claude fez.</summary>
    private static void Registrar(Document doc, Ferramenta f, ResultadoFerramenta r, long ms, bool emComando)
    {
        try
        {
            var ed = doc.Editor;
            var primeira = (r.Texto ?? "").Split('\n')[0];
            if (primeira.Length > 160) primeira = primeira[..157] + "...";
            ed.WriteMessage($"\n[PROJETA MCP] {f.Titulo}: {(r.Erro ? "ERRO — " : "")}{primeira} ({ms} ms)");
            if (emComando) return;
            if (f.AlteraDesenho && !r.Erro) ed.UpdateScreen();
            ed.PostCommandPrompt();
        }
        catch { }
    }

    public void Dispose() => _ctl.Dispose();
}
