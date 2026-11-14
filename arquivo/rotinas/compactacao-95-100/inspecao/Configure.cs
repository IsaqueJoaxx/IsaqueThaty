using System;
using System.IO;
using System.Globalization;
using System.Text.Json;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
public class Configure {
 const string Root=@"D:\PROJETA ENGENHARIA\Rotina_Compactacao_95_100";
 static ObjectId Id(Database db,string h)=>db.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0);
 static Point3d XY(Entity view,double offset,double elevation){
  object[] a={offset,elevation,0.0,0.0};
  bool ok=(bool)view.GetType().GetMethod("FindXYAtOffsetAndElevation").Invoke(view,a);
  if(!ok)throw new System.Exception("Ponto fora da vista: "+view.Handle);
  return new Point3d((double)a[2],(double)a[3],0);
 }
 [CommandMethod("CONFIGCOMPACTACAO")]
 public void Run(){
  var doc=Application.DocumentManager.MdiActiveDocument;var db=doc.Database;
  using var data=JsonDocument.Parse(File.ReadAllText(Root+@"\resultado_revisao_dwg.json"));
  int solids=0,tables=0;
  using(var tr=db.TransactionManager.StartTransaction()){
   var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForWrite);
   foreach(var pair in new[]{("COMP_95",(short)30),("COMP_100",(short)5),("COMP_TABELA",(short)7),("COMP_SUBLEITO",(short)4),("COMP_CAMADAS",(short)1)}){
    if(!lt.Has(pair.Item1)){var l=new LayerTableRecord{Name=pair.Item1,Color=Color.FromColorIndex(ColorMethod.ByAci,pair.Item2)};lt.Add(l);tr.AddNewlyCreatedDBObject(l,true);}
   }
   var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
   var ms=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
   foreach(ObjectId id in ms){var e=(Entity)tr.GetObject(id,OpenMode.ForRead);if(e.GetType().Name=="MaterialSection"||e.GetType().Name=="SectionViewQuantityTakeoffTable"){e.UpgradeOpen();e.Visible=false;}}
   var fmt=CultureInfo.GetCultureInfo("pt-BR");
   foreach(var rec in data.RootElement.GetProperty("secoes").EnumerateArray()){
    foreach(var vh in rec.GetProperty("views").EnumerateArray()){
     var view=(Entity)tr.GetObject(Id(db,vh.GetString()),OpenMode.ForRead);
     double needMin=double.MaxValue;
     foreach(var poly in rec.GetProperty("poligonos").EnumerateArray())foreach(var pt in poly.GetProperty("pontos").EnumerateArray())needMin=Math.Min(needMin,pt[1].GetDouble());
     double oldMin=Convert.ToDouble(view.GetType().GetProperty("ElevationMin").GetValue(view));
     if(needMin<oldMin){view.UpgradeOpen();view.GetType().GetProperty("IsElevationRangeAutomatic").SetValue(view,false);view.GetType().GetProperty("ElevationMin").SetValue(view,needMin-0.25);}
     double left=Convert.ToDouble(view.GetType().GetProperty("OffsetLeft").GetValue(view));
     double right=Convert.ToDouble(view.GetType().GetProperty("OffsetRight").GetValue(view));
     double zmin=Convert.ToDouble(view.GetType().GetProperty("ElevationMin").GetValue(view));
     double zmax=Convert.ToDouble(view.GetType().GetProperty("ElevationMax").GetValue(view));
     foreach(var poly in rec.GetProperty("poligonos").EnumerateArray()){
      var ps=new Point3d[4];int k=0;bool visible=true;
      foreach(var p in poly.GetProperty("pontos").EnumerateArray()){
       double x=p[0].GetDouble(),z=p[1].GetDouble();
       if(x<left-1e-6||x>right+1e-6||z<zmin-1e-6||z>zmax+1e-6){visible=false;break;}
       ps[k++]=XY(view,Math.Clamp(x,left,right),Math.Clamp(z,zmin,zmax));
      }
      if(!visible)throw new System.Exception("Vista recorta a geometria: "+view.Handle);
      var solid=new Solid(ps[0],ps[1],ps[3],ps[2]);
      int cat=poly.GetProperty("categoria").GetInt32();solid.Layer=cat==4?"COMP_SUBLEITO":cat%2==0?"COMP_95":"COMP_100";
      solid.Color=Color.FromColorIndex(ColorMethod.ByLayer,256);
      ms.AppendEntity(solid);tr.AddNewlyCreatedDBObject(solid,true);solids++;
     }
     foreach(var line in rec.GetProperty("linhas").EnumerateArray()){
      var e=new Line(XY(view,line[0][0].GetDouble(),line[0][1].GetDouble()),XY(view,line[1][0].GetDouble(),line[1][1].GetDouble()));e.Layer="COMP_CAMADAS";ms.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);
     }
     var top=XY(view,right,zmax);var basept=XY(view,left,zmin);
     double width=Math.Abs(top.X-basept.X);
     double row=Math.Max(0.18,width/55.0),tw=Math.Max(3.0,width*0.23);
     var table=new Table();table.SetDatabaseDefaults(db);table.TableStyle=db.Tablestyle;
     table.SetSize(7,2);table.UnmergeCells(CellRange.Create(table,0,0,0,1));table.SetRowHeight(row);table.SetColumnWidth(tw/2);
     table.Position=new Point3d(top.X+width*0.035,top.Y,0);table.Layer="COMP_TABELA";
     for(int r=0;r<7;r++)for(int c=0;c<2;c++){table.Cells[r,c].TextHeight=row*0.65;table.Cells[r,c].Alignment=CellAlignment.MiddleCenter;table.Cells[r,c].ContentColor=Color.FromColorIndex(ColorMethod.ByAci,7);}
     table.Cells[0,0].TextString="Estaca";table.Cells[0,1].TextString=(rec.GetProperty("station").GetDouble()/20).ToString("0.###",fmt);
     table.MergeCells(CellRange.Create(table,1,0,1,1));table.Cells[1,0].TextString=@"Plataforma - m\U+00B2";
     string[] cats={"Corte 95%","Corte 100%","Aterro 95%","Aterro 100%","Subleito 100%"};
     var areas=rec.GetProperty("areas");for(int i=0;i<5;i++){table.Cells[i+2,0].TextString=cats[i];table.Cells[i+2,1].TextString=areas[i].GetDouble().ToString("N2",fmt);}
     table.GenerateLayout();ms.AppendEntity(table);tr.AddNewlyCreatedDBObject(table,true);tables++;
    }
   }
   tr.Commit();
  }
  db.SaveAs(Root+@"\GO-220_Medicao_0a300_350a400.dwg",DwgVersion.Current);
  File.WriteAllText(Root+@"\verificacao_revisao_dwg.json",JsonSerializer.Serialize(new {solids,tables,arquivo="GO-220_Medicao_0a300_350a400.dwg"}));
 }
}
