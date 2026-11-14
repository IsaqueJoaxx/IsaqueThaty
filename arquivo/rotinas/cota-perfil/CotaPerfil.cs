using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Alignment=Autodesk.Civil.DatabaseServices.Alignment;
using Profile=Autodesk.Civil.DatabaseServices.Profile;
public class CotaPerfil {
 static ObjectId Pick(Editor ed, string msg, Type type) {var o=new PromptEntityOptions(msg);o.SetRejectMessage("\nSelecione o objeto Civil 3D solicitado.");o.AddAllowedClass(type,false);var r=ed.GetEntity(o);return r.Status==PromptStatus.OK?r.ObjectId:ObjectId.Null;}
 static int Choose(Editor ed,string msg,string[] names) {for(int i=0;i<names.Length;i++)ed.WriteMessage($"\n{i+1}: {names[i]}");var o=new PromptIntegerOptions(msg){LowerLimit=1,UpperLimit=names.Length,AllowNone=false};var r=ed.GetInteger(o);return r.Status==PromptStatus.OK?r.Value-1:-1;}
 public static ObjectId Make(Transaction tr, Alignment a, Profile p, double station, Point3d position) {
  if(station<a.StartingStation || station>a.EndingStation)throw new System.Exception("A estaca escolhida está fora do eixo. Use FimEixo ou uma estaca comum ao eixo e ao perfil.");
  p.ElevationAt(station); // Fail before creating any label when station is outside usable profile.
  var civil=CivilApplication.ActiveDocument;
  var styles=civil.Styles.LabelStyles.AlignmentLabelStyles.StationOffsetLabelStyles;
  // A dedicated style per label avoids modifying existing drawing styles.
  var sid=styles.Add("PROJETA Cota Perfil "+Guid.NewGuid().ToString("N").Substring(0,8));
  var style=(LabelStyle)tr.GetObject(sid,OpenMode.ForWrite);
  foreach(LabelStyleComponentType ct in Enum.GetValues(typeof(LabelStyleComponentType))){if(!style.IsSupportedComponent(ct))continue;foreach(ObjectId old in style.GetComponents(ct)){var c=(LabelStyleComponent)tr.GetObject(old,OpenMode.ForRead);style.RemoveComponent(c.Name);}}
  var tid=style.AddComponent("Estaca",LabelStyleComponentType.Text);
  var text=(LabelStyleTextComponent)tr.GetObject(tid,OpenMode.ForWrite);
  text.Text.Contents.Value="EST. <[Valor da estaca(Um|FSI|P2|RN|AC|Sn|TP|EN|DZY|W0|OF)]>";
  text.Text.Height.Value=0.0025;
  var rid=style.AddReferenceTextComponent("Cota do perfil",ReferenceTextComponentSelectedType.Profile);
  var reference=(LabelStyleReferenceTextComponent)tr.GetObject(rid,OpenMode.ForWrite);
  reference.Text.Contents.Value="COTA <[Elevação do perfil(Um|P3|RN|AC|Sn|OF)]>";
  reference.Text.Height.Value=0.0025;reference.Text.YOffset.Value=-0.0035;
  double x=0,y=0;a.PointLocation(station,0,ref x,ref y);
  var id=StationOffsetLabel.Create(a.ObjectId,sid,civil.Styles.MarkerStyles[0],new Point2d(x,y));
  var label=(StationOffsetLabel)tr.GetObject(id,OpenMode.ForWrite);
  label.AnchorAtXY=false;label.SetReferenceTextTarget(rid,p.ObjectId);label.LabelLocation=position;
  return id;
 }
 [CommandMethod("COTAPERFIL")]
 public void Run(){var doc=Application.DocumentManager.MdiActiveDocument;var ed=doc.Editor;
  try{var ao=new PromptEntityOptions("\nClique no eixo na estaca desejada: ");ao.SetRejectMessage("\nSelecione um alinhamento Civil 3D.");ao.AddAllowedClass(typeof(Alignment),false);var ar=ed.GetEntity(ao);if(ar.Status!=PromptStatus.OK)return;
   var cid=Pick(ed,"\nClique no corredor: ",typeof(Corridor));if(cid.IsNull)return;
   using(var tr=doc.Database.TransactionManager.StartTransaction()){
    var a=(Alignment)tr.GetObject(ar.ObjectId,OpenMode.ForRead);var corridor=(Corridor)tr.GetObject(cid,OpenMode.ForRead);
    var baselines=new List<Baseline>();foreach(Baseline b in corridor.Baselines)if(!b.IsFeatureLineBased() && b.AlignmentId==a.ObjectId)baselines.Add(b);
    if(baselines.Count==0)throw new System.Exception("O corredor não possui baseline por alinhamento ligada ao eixo selecionado. Selecione o eixo correto; baselines por feature line não são suportadas.");
    int bi=baselines.Count==1?0:Choose(ed,"\nEscolha a baseline: ",baselines.Select(b=>b.Name+" / perfil: "+((Profile)tr.GetObject(b.ProfileId,OpenMode.ForRead)).Name).ToArray());if(bi<0)return;
    var baseline=baselines[bi];ed.WriteMessage("\nEixo: "+a.Name+" | Baseline: "+baseline.Name);
    var ko=new PromptKeywordOptions("\nPerfil [Primitivo/Projeto/Todos] <Projeto>: ");ko.Keywords.Add("Primitivo");ko.Keywords.Add("Projeto");ko.Keywords.Add("Todos");ko.AllowNone=true;var kr=ed.GetKeywords(ko);if(kr.Status!=PromptStatus.OK && kr.Status!=PromptStatus.None)return;
    string kind=kr.Status==PromptStatus.None?"Projeto":kr.StringResult;
    var pp=a.GetProfileIds().Cast<ObjectId>().Select(id=>(Profile)tr.GetObject(id,OpenMode.ForRead)).Where(p=>kind=="Todos" || (kind=="Primitivo"?p.ProfileType==ProfileType.EG:p.ProfileType!=ProfileType.EG)).ToList();
    if(pp.Count==0)throw new System.Exception("Nenhum perfil desta categoria no eixo. Use Todos para consultar os tipos reais.");
    int pi=Choose(ed,"\nEscolha o perfil pelo número: ",pp.Select(p=>p.Name+" ["+p.ProfileType+"]"+(p.ObjectId==baseline.ProfileId?" — usado na baseline":"")+$" ({p.StartingStation:F3} a {p.EndingStation:F3} m)").ToArray());if(pi<0)return;var profile=pp[pi];
    var so=new PromptKeywordOptions("\nEstaca [Clique/InicioPerfil/FimPerfil/InicioEixo/FimEixo] <Clique>: ");foreach(var k in new[]{"Clique","InicioPerfil","FimPerfil","InicioEixo","FimEixo"})so.Keywords.Add(k);so.AllowNone=true;var sr=ed.GetKeywords(so);if(sr.Status!=PromptStatus.OK && sr.Status!=PromptStatus.None)return;
    double sta=0,off=0;var pick=ar.PickedPoint.TransformBy(ed.CurrentUserCoordinateSystem);if(sr.Status==PromptStatus.None || sr.StringResult=="Clique")a.StationOffset(pick.X,pick.Y,ref sta,ref off);
    switch(sr.StringResult){case "InicioPerfil":sta=profile.StartingStation;break;case "FimPerfil":sta=profile.EndingStation;break;case "InicioEixo":sta=a.StartingStation;break;case "FimEixo":sta=a.EndingStation;break;}
    double elev=profile.ElevationAt(sta);ed.WriteMessage($"\nPerfil: {profile.Name} | Estaca (m): {sta:F3} | Cota: {elev:F3}");
    var pos=ed.GetPoint("\nIndique a posição da cota: ");if(pos.Status!=PromptStatus.OK)return;Make(tr,a,profile,sta,pos.Value.TransformBy(ed.CurrentUserCoordinateSystem));tr.Commit();ed.WriteMessage("\nRótulo Civil 3D criado com referência nativa ao perfil.");
   }
  }catch(System.Exception ex){ed.WriteMessage("\nNão foi criado rótulo: "+ex.Message);}
 }
#if COTA_VALIDACAO
 static string Render(Autodesk.AutoCAD.DatabaseServices.Entity e){var parts=new List<string>();var objects=new DBObjectCollection();e.Explode(objects);foreach(Autodesk.AutoCAD.DatabaseServices.DBObject ob in objects){using(ob){if(ob is MText mt)parts.Add(mt.Contents);else if(ob is DBText dt)parts.Add(dt.TextString);else if(ob is Autodesk.AutoCAD.DatabaseServices.Entity ent){try{parts.Add(Render(ent));}catch{}}}}return string.Join(" | ",parts);}
 [CommandMethod("COTATEST")]
 public void Test(){var doc=Application.DocumentManager.MdiActiveDocument;var rows=new List<string>();
 try{using(var tr=doc.Database.TransactionManager.StartTransaction()){
 var civil=CivilApplication.ActiveDocument;
 foreach(ObjectId aid in civil.GetAlignmentIds()){var a=(Alignment)tr.GetObject(aid,OpenMode.ForRead);rows.Add($"EIXO {a.Name} {a.StartingStation} {a.EndingStation}");foreach(ObjectId pid in a.GetProfileIds()){var p=(Profile)tr.GetObject(pid,OpenMode.ForRead);rows.Add($"PERFIL {p.Name} {p.ProfileType} {p.StartingStation} {p.EndingStation}");}}
 foreach(ObjectId sid in civil.Styles.LabelStyles.AlignmentLabelStyles.StationOffsetLabelStyles){var st=(LabelStyle)tr.GetObject(sid,OpenMode.ForRead);foreach(ObjectId co in st.GetComponents(LabelStyleComponentType.Text)){var tc=(LabelStyleTextComponent)tr.GetObject(co,OpenMode.ForRead);rows.Add("STYLE "+st.Name+" "+tc.Text.Contents.Value);}} File.WriteAllLines(@"D:\PROJETA ENGENHARIA\Rotina_Cota_Perfil\estilos.txt",rows); var first=civil.GetAlignmentIds().Cast<ObjectId>().Select(id=>(Alignment)tr.GetObject(id,OpenMode.ForRead)).SelectMany(a=>a.GetProfileIds().Cast<ObjectId>().Select(id=>(Profile)tr.GetObject(id,OpenMode.ForRead))).First(p=>p.ProfileType==ProfileType.FG && !p.IsReferenceObject);
 var align=(Alignment)tr.GetObject(first.AlignmentId,OpenMode.ForRead);var labels=new List<ObjectId>();foreach(double sta in new[]{(first.StartingStation+first.EndingStation)/2,Math.Min(first.EndingStation,align.EndingStation)}){double x=0,y=0;align.PointLocation(sta,0,ref x,ref y);var id=Make(tr,align,first,sta,new Point3d(x+10,y+10,0));labels.Add(id);rows.Add($"ANTES station={sta} elevation={first.ElevationAt(sta)} text={Render((Autodesk.AutoCAD.DatabaseServices.Entity)tr.GetObject(id,OpenMode.ForRead))}");}
 first.UpgradeOpen();foreach(ProfilePVI pvi in first.PVIs)pvi.Elevation+=1;
 tr.Commit();store=labels;
 }
 using(var tr=doc.Database.TransactionManager.StartTransaction()){foreach(var id in store)rows.Add("DEPOIS "+Render((Autodesk.AutoCAD.DatabaseServices.Entity)tr.GetObject(id,OpenMode.ForRead)));tr.Commit();}
 }catch(System.Exception ex){rows.Add(ex.ToString());}File.WriteAllLines(@"D:\PROJETA ENGENHARIA\Rotina_Cota_Perfil\teste.txt",rows);}
 static List<ObjectId> store;
 [CommandMethod("COTAREABRIR")]
 public void Reopen(){var db=Application.DocumentManager.MdiActiveDocument.Database;var rows=new List<string>();using(var tr=db.TransactionManager.StartTransaction()){var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);var ms=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead);foreach(ObjectId id in ms){if(tr.GetObject(id,OpenMode.ForRead) is StationOffsetLabel label){var style=(LabelStyle)tr.GetObject(label.StyleId,OpenMode.ForRead);if(!style.Name.StartsWith("PROJETA Cota Perfil"))continue;var rid=style.GetComponents(LabelStyleComponentType.ReferenceText)[0];var p=(Profile)tr.GetObject(label.GetReferenceTextTarget(rid),OpenMode.ForRead);rows.Add("VINCULO "+p.Name+" | "+Render(label));}}}File.WriteAllLines(@"D:\PROJETA ENGENHARIA\Rotina_Cota_Perfil\reabertura.txt",rows);}
#endif
}

