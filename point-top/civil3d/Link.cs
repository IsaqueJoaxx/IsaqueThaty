using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;
[assembly: ExtensionApplication(typeof(PointTop.Civil3D.Link))]
[assembly: CommandClass(typeof(PointTop.Civil3D.Link))]
namespace PointTop.Civil3D;

public sealed class Link : IExtensionApplication
{
    private static Document? doc;
    private static ObjectId axisId = ObjectId.Null, corridorId = ObjectId.Null;
    private static readonly HashSet<ObjectId> watched = new();
    private static bool dirty, automatic, updating, queued;
    private static DateTime lastBeat;
    private static object? lastSnapshot;
    private static string message = "Desconectado";
    private static string Root {
        get {
            var d = new DirectoryInfo(Path.GetDirectoryName(typeof(Link).Assembly.Location)!);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "server.cjs"))) d=d.Parent;
            return d?.FullName ?? throw new InvalidOperationException("Mantenha a DLL dentro da pasta POINT TOP.");
        }
    }
    public void Initialize() { App.Idle += Idle; App.DocumentManager.DocumentToBeDestroyed += Closing; }
    public void Terminate() { Stop(); App.Idle -= Idle; App.DocumentManager.DocumentToBeDestroyed -= Closing; }

    [CommandMethod("PTVINCULAR", CommandFlags.Modal)]
    public void Bind() {
        var target=App.DocumentManager.MdiActiveDocument;
        if(target==null)return;
        var ed=target.Editor;
        var p=new PromptEntityOptions("\nPOINT TOP — IFM: selecione o alinhamento: ");
        p.SetRejectMessage("\nSelecione um alinhamento Civil 3D."); p.AddAllowedClass(typeof(Alignment),true);
        var ar=ed.GetEntity(p); if(ar.Status!=PromptStatus.OK)return;
        var cp=new PromptEntityOptions("\nSelecione o corredor dependente ou Enter para apenas acompanhar: "){AllowNone=true};
        cp.SetRejectMessage("\nSelecione um corredor Civil 3D.");cp.AddAllowedClass(typeof(Corridor),true);
        var cr=ed.GetEntity(cp);if(cr.Status!=PromptStatus.OK && cr.Status!=PromptStatus.None)return;
        var chosen=cr.Status==PromptStatus.OK?cr.ObjectId:ObjectId.Null;
        using(var tr=target.Database.TransactionManager.StartTransaction()) {
            var a=(Alignment)tr.GetObject(ar.ObjectId,OpenMode.ForRead);
            if(a.IsReferenceObject){ed.WriteMessage("\nEixo referenciado: edite no desenho de origem.");return;}
            if(!chosen.IsNull){
                var c=(Corridor)tr.GetObject(chosen,OpenMode.ForRead);
                if(c.IsReferenceObject || !Depends(c,ar.ObjectId)){ed.WriteMessage("\nCorredor referenciado ou sem linha de base vinculada ao eixo selecionado. Vínculo recusado.");return;}
            }
        }
        bool auto=false;
        if(!chosen.IsNull){var prompt=new PromptKeywordOptions("\nReconstruir este corredor automaticamente após editar o eixo/perfil? [Sim/Nao] <Nao>: ");prompt.Keywords.Add("Sim");prompt.Keywords.Add("Nao");prompt.Keywords.Default="Nao";prompt.AllowNone=true;var k=ed.GetKeywords(prompt);if(k.Status==PromptStatus.Cancel)return;auto=k.StringResult=="Sim";}
        Stop();doc=target;axisId=ar.ObjectId;corridorId=chosen;automatic=auto;
        doc.Database.ObjectModified+=Modified;doc.CommandEnded+=Ended;doc.CommandCancelled+=Cancelled;doc.CommandFailed+=Cancelled;
        message=automatic?"Vinculado; reconstrução automática ativada para o corredor escolhido.":"Vinculado; acompanhamento sem reconstrução automática.";
        Snapshot(); ed.WriteMessage("\nPOINT TOP — IFM vinculado. Edite o eixo normalmente. PTATUALIZAR reconstrói; PTDESLIGAR encerra o vínculo.\n");
    }
    private static bool Depends(Corridor c,ObjectId id){foreach(Baseline b in c.Baselines){try{if(b.AlignmentId==id)return true;}catch(System.Exception){/* Linha de base por feature line não usa AlignmentId. */}}return false;}
    private static void Modified(object sender,ObjectEventArgs e){if(!updating&&watched.Contains(e.DBObject.ObjectId))dirty=true;}
    private static void Ended(object sender,CommandEventArgs e){
        if(doc==null)return;
        var cmd=e.GlobalCommandName.ToUpperInvariant();
        if(cmd is "U" or "UNDO" or "REDO" or "MREDO") {automatic=false;dirty=false;message="Desfazer/refazer detectado; automático suspenso. Use PTVINCULAR para reativar.";queued=true;return;}
        queued=true;
    }
    private static void Cancelled(object sender,CommandEventArgs e){automatic=false;dirty=false;queued=true;message="Comando cancelado/falhou: automático suspenso. Confira o desenho e use PTVINCULAR.";}
    private static void Closing(object sender,DocumentCollectionEventArgs e){if(e.Document==doc)Stop();}
    private static void Idle(object? sender,EventArgs e){
        if(doc==null||updating)return;
        if(App.DocumentManager.MdiActiveDocument!=doc)return;
        if(Convert.ToInt32(App.GetSystemVariable("CMDACTIVE"))!=0)return;
        if(queued){queued=false;
            if(dirty&&automatic&&!corridorId.IsNull){
                // A reconstrução roda em comando próprio para integrar o desfazer do Civil 3D.
                dirty=false;doc.SendStringToExecute("PTATUALIZAR ",false,false,false);return;
            }
            if(dirty){message="Eixo/perfil alterado. Reconstrução automática desativada; use PTATUALIZAR.";dirty=false;}
            Snapshot();
        }
        if((DateTime.UtcNow-lastBeat).TotalSeconds>=3)WriteSnapshot();
    }
    [CommandMethod("PTATUALIZAR", CommandFlags.Modal)]
    public void Refresh(){
        if(doc==null||doc!=App.DocumentManager.MdiActiveDocument)return;
        updating=true;
        try{
            if(!corridorId.IsNull){
                using(var tr=doc.Database.TransactionManager.StartTransaction()){
                    var c=(Corridor)tr.GetObject(corridorId,OpenMode.ForWrite);
                    var layer=(LayerTableRecord)tr.GetObject(c.LayerId,OpenMode.ForRead);
                    if(layer.IsLocked||c.IsReferenceObject||!Depends(c,axisId))throw new InvalidOperationException("Corredor bloqueado/referenciado ou perdeu vínculo com o eixo.");
                    if(c.IsOutOfDate)c.Rebuild();
                    tr.Commit();
                }
                using(var verify=doc.Database.TransactionManager.StartTransaction()){
                    var c=(Corridor)verify.GetObject(corridorId,OpenMode.ForRead);
                    if(c.IsOutOfDate)throw new InvalidOperationException("O Civil 3D ainda informa corredor desatualizado. Confira alvos, greide e regiões.");
                }
                message="Corredor vinculado reconstruído/verificado; greide e curvas verticais preservados.";
            }else message="Traçado atualizado no painel; nenhum corredor foi vinculado.";
            dirty=false;doc.Editor.Regen();
        }catch(System.Exception ex){automatic=false;message="Falha: "+ex.Message+" Automático suspenso.";}
        finally{updating=false;Snapshot();doc.Editor.WriteMessage("\nPOINT TOP — IFM: "+message+"\n");}
    }
    [CommandMethod("PTDESLIGAR", CommandFlags.Modal)]
    public void Disconnect(){Stop();App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nPOINT TOP — IFM: vínculo desligado.\n");}

    private static void Snapshot(){
        if(doc==null)return;
        try{
            using var tr=doc.Database.TransactionManager.StartTransaction();
            var a=(Alignment)tr.GetObject(axisId,OpenMode.ForRead);
            watched.Clear();watched.Add(axisId);
            var profiles=new List<object>();
            foreach(ObjectId id in a.GetProfileIds()){
                watched.Add(id);var p=(Profile)tr.GetObject(id,OpenMode.ForRead);
                profiles.Add(new {name=p.Name,handle=p.Handle.ToString(),kind=p.ProfileType.ToString()});
            }
            var points=new List<object>();var failures=0;
            var samples=Math.Clamp((int)Math.Ceiling(a.Length/10),2,1000);
            for(int i=0;i<=samples;i++){var station=a.StartingStation+(a.EndingStation-a.StartingStation)*i/samples;double x=0,y=0;try{a.PointLocation(station,0,ref x,ref y);points.Add(new {station,x,y});}catch(System.Exception){failures++;}}
            object? corridor=null;
            if(!corridorId.IsNull){var c=(Corridor)tr.GetObject(corridorId,OpenMode.ForRead);corridor=new{name=c.Name,handle=c.Handle.ToString(),outOfDate=c.IsOutOfDate};}
            lastSnapshot=new {schema=1,assistant="ALINHAMENTO INTELIGENTE — IFM",observedAt=DateTime.UtcNow,document=doc.Name,units=doc.Database.Insunits.ToString(),axis=new{name=a.Name,handle=a.Handle.ToString(),length=a.Length,startStation=a.StartingStation,endStation=a.EndingStation,points,samplingFailures=failures},profiles,corridor,automatic,message};
            WriteSnapshot();
        }catch(System.Exception ex){automatic=false;message="Leitura indisponível: "+ex.Message;lastSnapshot=new{schema=1,observedAt=DateTime.UtcNow,error=message};WriteSnapshot();}
    }
    private static void WriteSnapshot(){
        if(lastSnapshot==null)return;
        try{
            var folder=Path.Combine(Root,"runtime","civil3d");Directory.CreateDirectory(folder);
            var file=Path.Combine(folder,"session-"+Environment.ProcessId+".json");
            var text=JsonSerializer.Serialize(new{heartbeat=DateTime.UtcNow,connected=doc!=null,active=doc==App.DocumentManager.MdiActiveDocument,data=lastSnapshot});
            File.WriteAllText(file+".tmp",text);File.Move(file+".tmp",file,true);lastBeat=DateTime.UtcNow;
        }catch(System.Exception ex){doc?.Editor.WriteMessage("\nPOINT TOP: falha ao comunicar com o painel: "+ex.Message);}
    }
    private static void Stop(){
        if(doc!=null){doc.Database.ObjectModified-=Modified;doc.CommandEnded-=Ended;doc.CommandCancelled-=Cancelled;doc.CommandFailed-=Cancelled;}
        doc=null;axisId=ObjectId.Null;corridorId=ObjectId.Null;watched.Clear();automatic=false;dirty=false;queued=false;WriteSnapshot();
    }
}
