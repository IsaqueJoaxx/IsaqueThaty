using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
[assembly: CommandClass(typeof(PointTop.Leitura2027.Commands))]
namespace PointTop.Leitura2027;

// Sem inicializador, campos ObjectId estáticos ou assinatura de eventos.
public sealed class Commands
{
    [CommandMethod("PTLEREIXO",CommandFlags.Modal)]
    public void ReadAxis()
    {
        Log("COMMAND_ENTER","PTLEREIXO 0.3.2");
        try{
            var doc=App.DocumentManager.MdiActiveDocument;
            if(doc==null){Log("NO_DOCUMENT","");return;}
            Log("BEFORE_PROMPT","Seleção genérica, sem filtro Civil.");
            var selected=doc.Editor.GetEntity(new PromptEntityOptions("\nLEITURA DE EIXO — IFM: selecione um alinhamento Civil 3D ou Esc: "));
            Log("AFTER_PROMPT",selected.Status.ToString());
            if(selected.Status!=PromptStatus.OK)return;
            Log("BEFORE_CIVIL_HELPER","Acesso Civil será resolvido apenas após a seleção.");
            var result=ReadCivil(doc.Database,selected.ObjectId);
            Log("READ_RESULT",result);
            doc.Editor.WriteMessage("\nLEITURA DE EIXO — IFM: "+result+"\nNenhum vínculo, corredor ou automático ativado.\n");
            Log("COMMAND_DONE","");
        }catch(System.Exception ex){Log("COMMAND_ERROR",ex.ToString());App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nLEITURA DE EIXO — IFM: falha registrada. "+ex.Message+"\n");}
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ReadCivil(Database database,ObjectId id)
    {
        Log("CIVIL_HELPER_ENTER","");
        using var tr=database.TransactionManager.StartOpenCloseTransaction();
        Log("BEFORE_GET_OBJECT","ForRead");
        var entity=tr.GetObject(id,OpenMode.ForRead);
        Log("AFTER_GET_OBJECT","Objeto aberto somente para leitura.");
        Log("BEFORE_ALIGNMENT_CAST","");
        if(entity is not Autodesk.Civil.DatabaseServices.Alignment axis){Log("NOT_ALIGNMENT",entity.GetType().FullName??"");return "O objeto selecionado não é um alinhamento. Nenhuma alteração realizada.";}
        Log("BEFORE_NAME","");var name=axis.Name;Log("AFTER_NAME",name);
        Log("BEFORE_LENGTH","");var length=axis.Length;Log("AFTER_LENGTH",length.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
        Log("BEFORE_UNITS","");var units=database.Insunits.ToString();Log("AFTER_UNITS",units);
        return name+" | Comprimento: "+length.ToString("N3",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))+" | Unidade do DWG: "+units;
        // A transação de leitura é descartada sem Commit e nenhum objeto é aberto ForWrite.
    }

    private static void Log(string stage,string detail)
    {
        try{
            var root=new DirectoryInfo(Path.GetDirectoryName(typeof(Commands).Assembly.Location)!);
            while(root!=null&&!File.Exists(Path.Combine(root.FullName,"server.cjs")))root=root.Parent;
            if(root==null)return;
            var folder=Path.Combine(root.FullName,"runtime","civil3d-diagnostics");Directory.CreateDirectory(folder);
            var file=Path.Combine(folder,"leitura-"+Environment.ProcessId+".log");
            var bytes=Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")+" | "+stage+" | "+detail+Environment.NewLine);
            using var stream=new FileStream(file,FileMode.Append,FileAccess.Write,FileShare.ReadWrite,4096,FileOptions.WriteThrough);stream.Write(bytes);stream.Flush(true);
        }catch(System.Exception){ }
    }
}
