using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using System.Windows.Forms;
using Acad=Autodesk.AutoCAD.ApplicationServices.Application;
[assembly: CommandClass(typeof(Terraplenagem.IFM.Commands))]
[assembly: CommandClass(typeof(Terraplenagem.IFM.PlanCommands))]
namespace Terraplenagem.IFM;
public sealed class Commands {
 // Scenario per DWG instance: do not silently draw results into a different document.
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Autodesk.AutoCAD.ApplicationServices.Document,State> states=new();
 sealed class State {public Project Project=new();}
 [CommandMethod("PRJTERRA",CommandFlags.Modal)] public void Open()=>Show(false,false);
 [CommandMethod("PRJTERRADEMO",CommandFlags.Modal)] public void Demo()=>Show(false,true);
 [CommandMethod("PRJTERRALINK",CommandFlags.Modal)] public void Link()=>Show(true,false);
 void Show(bool read,bool demo){var doc=Acad.DocumentManager.MdiActiveDocument;if(doc is null)return;try{var state=states.GetValue(doc,_=>new State());if(read){var p=ReadCivil();if(p is null)return;state.Project=p;}if(demo)state.Project=Project.Example();using var panel=new Panel(state.Project);Acad.ShowModalDialog(panel);state.Project=panel.Project;if(panel.DrawRequested&&panel.Result is not null)Draw(panel.Project,panel.Result);}catch(System.Exception e){doc.Editor.WriteMessage("\nDISTRIBUIÇÃO DE TERRAPLENAGEM — IFM: "+e.Message);}}
 static string? Choose(string title,IEnumerable<string> names){using var f=new Form(){Text=title+" — IFM",Width=560,Height=160,StartPosition=FormStartPosition.CenterScreen};var c=new ComboBox(){Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};c.Items.AddRange(names.Cast<object>().ToArray());if(c.Items.Count==0)throw new System.Exception("Nenhuma opção encontrada: "+title);c.SelectedIndex=0;var ok=new Button(){Text="Confirmar seleção",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK};f.Controls.Add(c);f.Controls.Add(ok);return Acad.ShowModalDialog(f)==DialogResult.OK?c.SelectedItem?.ToString():null;}
 static Project? ReadCivil(){
  var doc=Acad.DocumentManager.MdiActiveDocument;var opt=new PromptEntityOptions("\nSelecione o alinhamento para ler a lista de materiais: ");opt.SetRejectMessage("\nSelecione um alinhamento.");opt.AddAllowedClass(typeof(Alignment),true);var selected=doc.Editor.GetEntity(opt);if(selected.Status!=PromptStatus.OK)return null;
  using var tr=doc.Database.TransactionManager.StartTransaction();var alignment=(Alignment)tr.GetObject(selected.ObjectId,OpenMode.ForRead);
  var groups=alignment.GetSampleLineGroupIds().Cast<ObjectId>().Select(id=>(SampleLineGroup)tr.GetObject(id,OpenMode.ForRead)).ToArray();var groupName=Choose("Grupo de linhas de amostragem",groups.Select(x=>x.Name));if(groupName is null)return null;var group=groups.Single(x=>x.Name==groupName);
  var lists=group.MaterialLists.Cast<QTOMaterialList>().ToArray();var listName=Choose("Lista de materiais",lists.Select(x=>x.Name));if(listName is null)return null;var list=lists.Single(x=>x.Name==listName);
  foreach(QTOMaterial m in list){foreach(var kind in new[]{MaterialFactorType.Cut,MaterialFactorType.Fill})if(m.IsFactorApplicable(kind)&&Math.Abs(m.GetFactor(kind)-1)>1e-8)throw new System.Exception("A lista contém fatores de corte/aterro diferentes de 1. Para evitar dupla homogeneização, use uma lista geométrica com fatores 1 ou importe áreas geométricas por CSV.");}
  var data=group.GetTotalVolumeResultDataForMaterialList(group.GetMappingGuid(listName));
  var rows=data.GetResultsAlongSampleLines().Select(r=>new AreaRow(){Progressiva_m=r.Station,Corte_m2=r.AreaResult.CutArea,Aterro_m2=r.AreaResult.FillArea,Corte_incremental_m3=r.VolumeResult.IncrementalCutVolume,Aterro_incremental_m3=r.VolumeResult.IncrementalFillVolume,VolumeInformado=true}).OrderBy(x=>x.Progressiva_m).ToList();
  if(rows.Count<2)throw new System.Exception("Lista sem volumes suficientes. Calcule os materiais no Civil 3D primeiro.");
  if(Math.Abs(rows[0].Corte_incremental_m3)>1e-6||Math.Abs(rows[0].Aterro_incremental_m3)>1e-6)throw new System.Exception("Primeira seção contém volume incremental não nulo. Confira o início da lista antes de importar.");
  var p=new Project(){Nome=alignment.Name,Fonte=$"Civil 3D: {doc.Name}; alinhamento {alignment.Name}; grupo {group.Name}; lista {list.Name}; volumes incrementais da lista, fatores 1; centroides no meio de cada intervalo",Cubacao=rows};tr.Abort();return p;
 }
 static void Draw(Project p,Result result){
  if(result.Movimentos.Count==0)throw new System.Exception("Não há movimentos longitudinais para desenhar.");
  var doc=Acad.DocumentManager.MdiActiveDocument;var point=doc.Editor.GetPoint("\nPonto de inserção do diagrama linear de distribuição: ");if(point.Status!=PromptStatus.OK)return;
  var scale=doc.Editor.GetDouble(new PromptDoubleOptions("\nEscala horizontal (unidades de desenho por metro) <1>: "){DefaultValue=1,AllowNone=true,AllowNegative=false,AllowZero=false});if(scale.Status!=PromptStatus.OK&&scale.Status!=PromptStatus.None)return;double sx=scale.Status==PromptStatus.OK?scale.Value:1;
  using var tr=doc.Database.TransactionManager.StartTransaction();var layers=(LayerTable)tr.GetObject(doc.Database.LayerTableId,OpenMode.ForRead);string layer="IFM_TERRAPLENAGEM";
  if(!layers.Has(layer)){layers.UpgradeOpen();var l=new LayerTableRecord(){Name=layer};layers.Add(l);tr.AddNewlyCreatedDBObject(l,true);}
  var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);var space=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
  void Add(Autodesk.AutoCAD.DatabaseServices.Entity e){e.Layer=layer;space.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);}
  void Text(string text,double x,double y,double h=2.5){Add(new DBText(){TextString=text,Position=new Point3d(x,y,point.Value.Z),Height=h});}
  var allStations=p.Origens.Select(o=>o.Progressiva_m).Concat(p.Destinos.Select(d=>d.Progressiva_m)).ToArray();var min=allStations.Min();var max=allStations.Max();double X(double station)=>point.Value.X+(station-min)*sx;
  Text("DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM",point.Value.X,point.Value.Y+15,4);
  Text("Volumes em banco (m3); distâncias de rota em metros. Cenário: "+p.Nome,point.Value.X,point.Value.Y+9);
  Add(new Line(new Point3d(X(min),point.Value.Y,point.Value.Z),new Point3d(X(max)+1,point.Value.Y,point.Value.Z)));
  int row=0;foreach(var m in result.Movimentos){var o=p.Origens.Single(x=>x.Nome==m.Origem);var d=p.Destinos.Single(x=>x.Nome==m.Destino);double y=point.Value.Y-12*(++row);var line=new Polyline();line.AddVertexAt(0,new Point2d(X(o.Progressiva_m),point.Value.Y),0,0,0);line.AddVertexAt(1,new Point2d(X(o.Progressiva_m),y),0,0,0);line.AddVertexAt(2,new Point2d(X(d.Progressiva_m),y),0,0,0);line.AddVertexAt(3,new Point2d(X(d.Progressiva_m),point.Value.Y),0,0,0);line.Elevation=point.Value.Z;Add(line);Text($"{m.Origem} > {m.Destino}: {m.Banco_m3:F2} m3; DMT {m.DMT_m:F1} m",Math.Min(X(o.Progressiva_m),X(d.Progressiva_m)),y+2);}
  foreach(var o in p.Origens)Text(o.Nome+" "+o.Progressiva_m.ToString("F1"),X(o.Progressiva_m),point.Value.Y+3);
  foreach(var d in p.Destinos)Text(d.Nome+" "+d.Progressiva_m.ToString("F1"),X(d.Progressiva_m),point.Value.Y-5);
  tr.Commit();doc.Editor.WriteMessage("\nDiagrama criado na camada IFM_TERRAPLENAGEM. Use UNDO para desfazer.");
 }
}
