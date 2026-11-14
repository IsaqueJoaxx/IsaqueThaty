using System;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(PointTop.Diagnostics2027.DiagnosticCommands))]
[assembly: CommandClass(typeof(PointTop.Diagnostics2027.DiagnosticCommands))]
namespace PointTop.Diagnostics2027;

// Diagnóstico deliberadamente separado: sem AeccDbMgd, sem estado DBObject,
// sem eventos Idle/ObjectModified e sem transações ou reconstruções.
public sealed class DiagnosticCommands : IExtensionApplication
{
    public void Initialize() { Trace("INITIALIZE", "Início e fim; nenhum evento do host registrado."); }
    public void Terminate() { Trace("TERMINATE", "Encerrado."); }

    [CommandMethod("PTDSTATUS", CommandFlags.Modal)]
    public void Status()
    {
        Trace("STATUS_ENTER", "Comando reconhecido.");
        try
        {
            Trace("STATUS_BEFORE_DOCUMENT", "Obtendo documento ativo.");
            var document=App.DocumentManager.MdiActiveDocument;
            Trace("STATUS_AFTER_DOCUMENT", document==null?"Sem documento.":"Documento disponível.");
            if(document==null)return;
            document.Editor.WriteMessage("\nPOINT TOP DIAGNÓSTICO — IFM 0.3.1: comando reconhecido. Nenhum vínculo ou automático ativado.\n");
            Trace("STATUS_DONE", "Mensagem exibida.");
        }
        catch(System.Exception ex){Trace("STATUS_ERROR",ex.ToString());}
    }

    [CommandMethod("PTDSELECIONAR", CommandFlags.Modal)]
    public void SelectEntity()
    {
        Trace("SELECT_ENTER", "Início de seleção genérica, sem filtro Civil 3D.");
        try
        {
            var document=App.DocumentManager.MdiActiveDocument;
            if(document==null){Trace("SELECT_NO_DOCUMENT", "Sem documento.");return;}
            Trace("SELECT_BEFORE_OPTIONS", "Criando prompt básico.");
            var options=new PromptEntityOptions("\nPOINT TOP DIAGNÓSTICO — IFM: selecione uma linha comum ou Esc: ");
            Trace("SELECT_BEFORE_PROMPT", "Solicitando seleção; nenhum AddAllowedClass.");
            var selected=document.Editor.GetEntity(options);
            Trace("SELECT_AFTER_PROMPT", selected.Status.ToString());
            document.Editor.WriteMessage("\nPOINT TOP DIAGNÓSTICO — IFM: seleção retornou "+selected.Status+". Nenhum objeto aberto ou alterado.\n");
            Trace("SELECT_DONE", "Fim sem acesso ao objeto selecionado.");
        }
        catch(System.Exception ex){Trace("SELECT_ERROR",ex.ToString());}
    }

    private static void Trace(string stage,string detail)
    {
        // Falhas de I/O não escapam para o host; um log ausente não comprova etapa executada.
        try
        {
            var directory=new DirectoryInfo(Path.GetDirectoryName(typeof(DiagnosticCommands).Assembly.Location)!);
            while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"server.cjs")))directory=directory.Parent;
            if(directory==null)return;
            var folder=Path.Combine(directory.FullName,"runtime","civil3d-diagnostics");
            Directory.CreateDirectory(folder);
            var file=Path.Combine(folder,"diagnostico-"+Environment.ProcessId+".log");
            var bytes=Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")+" | "+stage+" | "+detail+Environment.NewLine);
            using var stream=new FileStream(file,FileMode.Append,FileAccess.Write,FileShare.ReadWrite,4096,FileOptions.WriteThrough);
            stream.Write(bytes);stream.Flush(true);
        }
        catch(System.Exception){/* Não emitir mensagens CAD de dentro da falha do logger. */}
    }
}
