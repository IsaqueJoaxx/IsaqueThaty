using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Acad=Autodesk.AutoCAD.ApplicationServices.Application;
namespace Terraplenagem.IFM;
public sealed partial class PlanCommands {
 [CommandMethod("PRJTERRAPLANTA",CommandFlags.Modal)] public void Draw(){
  var doc=Acad.DocumentManager.MdiActiveDocument;if(doc is null)return;
  try{
   using var open=new System.Windows.Forms.OpenFileDialog(){Filter="Cenário de terraplenagem IFM|*.json"};if(open.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return;
   var p=Files.Load(open.FileName);var r=Solver.Run(p);
   var option=new PromptEntityOptions("\nSelecione o alinhamento cujas progressivas correspondem ao cenário: ");option.SetRejectMessage("\nSelecione alinhamento.");option.AddAllowedClass(typeof(Alignment),true);var selection=doc.Editor.GetEntity(option);if(selection.Status!=PromptStatus.OK)return;
   using var tr=doc.Database.TransactionManager.StartTransaction();var alignment=(Alignment)tr.GetObject(selection.ObjectId,OpenMode.ForRead);
   // Validate all progressives before adding anything to the drawing.
   var stations=p.Origens.Select(o=>o.Progressiva_m).Concat(p.Destinos.Select(d=>d.Progressiva_m)).Distinct().ToArray();var points=new Dictionary<double,Point3d>();
   foreach(var s in stations){double x=0,y=0;alignment.PointLocation(s,0,ref x,ref y);if(!double.IsFinite(x)||!double.IsFinite(y))throw new System.Exception("Progressiva fora do alinhamento: "+s);points[s]=new(x,y,0);}
   var layers=(LayerTable)tr.GetObject(doc.Database.LayerTableId,OpenMode.ForRead);const string layer="IFM_TERRA_MOVIMENTOS";if(!layers.Has(layer)){layers.UpgradeOpen();var lr=new LayerTableRecord(){Name=layer};layers.Add(lr);tr.AddNewlyCreatedDBObject(lr,true);}
   var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);var space=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
   void Add(Autodesk.AutoCAD.DatabaseServices.Entity e){e.Layer=layer;space.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);}
   int index=0;foreach(var m in r.Movimentos){var a=points[p.Origens.Single(o=>o.Nome==m.Origem).Progressiva_m];var b=points[p.Destinos.Single(d=>d.Nome==m.Destino).Progressiva_m];Add(new Line(a,b));var mid=a+(b-a)*.5;Add(new DBText(){TextString=$"{m.Origem}>{m.Destino}: {m.Banco_m3:F2} m3; DMT {m.DMT_m:F1} m",Position=mid+new Vector3d(0,3*(index++%5),0),Height=2.5});if(a.DistanceTo(b)>1e-6){var v=(b-a).GetNormal();var normal=new Vector3d(-v.Y,v.X,0);Add(new Line(b,b-v*5+normal*2));Add(new Line(b,b-v*5-normal*2));}}
   tr.Commit();doc.Editor.WriteMessage("\nMovimentos esquemáticos criados em IFM_TERRA_MOVIMENTOS. As linhas ligam centros; não representam rotas de transporte. DMT é a do cenário, não o comprimento da linha.");
  }catch(System.Exception ex){doc.Editor.WriteMessage("\nTERRAPLENAGEM — IFM: "+ex.Message);}
 }
}
