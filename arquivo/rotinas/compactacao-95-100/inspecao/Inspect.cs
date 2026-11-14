using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
public class Inspect {
 [CommandMethod("EXPORTINSPECT")]
 public void Export() {
  var db=Application.DocumentManager.MdiActiveDocument.Database;
  var rows=new List<object>();
  using(var tr=db.TransactionManager.StartTransaction()) {
   var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead); foreach(ObjectId lid in lt){var layer=(LayerTableRecord)tr.GetObject(lid,OpenMode.ForRead);rows.Add(new {type="LayerDefinition",Name=layer.Name,color=layer.Color.ToString(),index=layer.Color.ColorIndex});} var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
   foreach(ObjectId bid in bt) {
    var b=(BlockTableRecord)tr.GetObject(bid,OpenMode.ForRead);
    
    foreach(ObjectId id in b) {
     var e=(Entity)tr.GetObject(id,OpenMode.ForRead);
     var d=new Dictionary<string,object>{{"type",e.GetType().FullName},{"dxf",id.ObjectClass.DxfName},{"handle",e.Handle.ToString()},{"layer",e.Layer},{"color",e.Color.ToString()},{"layout",b.Name}};
     foreach(string pn in new[]{"Name","TextString","Contents","StyleName","Station","Location","SectionPoints","SampleLineId","SourceId","Position","ElevationMin","ElevationMax","OffsetLeft","OffsetRight"}) {
      try{var v=e.GetType().GetProperty(pn)?.GetValue(e); if(v!=null)d[pn]=v.ToString();}catch{}
     }
     if(e is Table table){var cells=new List<List<string>>();for(int r=0;r<table.Rows.Count;r++){var rr=new List<string>();for(int c=0;c<table.Columns.Count;c++)rr.Add(table.Cells[r,c].TextString);cells.Add(rr);}d["cells"]=cells;d["tableStyle"]=table.TableStyle.ToString();}
     if(e.GetType().Name=="Section" || e.GetType().Name=="MaterialSection") {
      try{var points=(System.Collections.IEnumerable)e.GetType().GetProperty("SectionPoints").GetValue(e);var pp=new List<object>();foreach(var pt in points){var loc=pt.GetType().GetProperty("Location").GetValue(pt);pp.Add(new {x=loc.GetType().GetProperty("X").GetValue(loc),y=loc.GetType().GetProperty("Y").GetValue(loc),z=loc.GetType().GetProperty("Z").GetValue(loc),to=pt.GetType().GetProperty("SegmentTo").GetValue(pt)});}d["points"]=pp;}catch(System.Exception ex){d["pointError"]=ex.Message;}
     }
     if(e.GetType().Name=="SectionView" || e.GetType().Name=="SampleLine" || e.GetType().Name=="SectionViewQuantityTakeoffTable") {
      foreach(var p in e.GetType().GetProperties()){try{var v=p.GetValue(e);if(v!=null&&(v is string||v is double||v is int||v is ObjectId||v.GetType().Name=="Point3d"))d[p.Name]=v.ToString();}catch{}}
      if(!rows.Exists(r=>r is Dictionary<string,object> rd && rd.ContainsKey("api") && (string)rd["api"]==e.GetType().Name)){var md=new List<string>();foreach(var m in e.GetType().GetMethods())if(m.DeclaringType==e.GetType())md.Add(m.ToString());rows.Add(new Dictionary<string,object>{{"type","API"},{"api",e.GetType().Name},{"methods",md}});}
     }
     rows.Add(d);
    }
   }
   tr.Commit();
  }
  File.WriteAllText(@"D:\PROJETA ENGENHARIA\Rotina_Compactacao_95_100\inspecao\entidades.json",JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
 }
}

