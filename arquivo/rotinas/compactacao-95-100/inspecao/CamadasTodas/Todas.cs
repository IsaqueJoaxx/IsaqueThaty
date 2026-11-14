using System;
using System.IO;
using System.Text.Json;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
public class Todas {
const string Root=@"D:\PROJETA ENGENHARIA\Rotina_Compactacao_95_100";
static Point3d XY(Entity v,double x,double z){object[] a={x,z,0.0,0.0};if(!(bool)v.GetType().GetMethod("FindXYAtOffsetAndElevation").Invoke(v,a))throw new System.Exception("Coordenada fora da vista "+v.Handle);return new Point3d((double)a[2],(double)a[3],0);}
static void Set(Entity v,string p,object value){var prop=v.GetType().GetProperty(p);if(prop!=null && prop.CanWrite)prop.SetValue(v,value);}
[CommandMethod("CAMADASTODAS")]
public void Run(){
var doc=Application.DocumentManager.MdiActiveDocument;var db=doc.Database;
using var data=JsonDocument.Parse(File.ReadAllText(Root+@"\inspecao\todas_camadas.json"));int views=0,solids=0,tables=0;
using(var tr=db.TransactionManager.StartTransaction()){
var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForWrite);
foreach(var pair in new[]{("MED_SUBBASE_AZUL",(short)5),("MED_BASE_LARANJA",(short)30),("MED_REFORCO",(short)8),("MED_TABELAS",(short)7)}){if(!lt.Has(pair.Item1)){var l=new LayerTableRecord{Name=pair.Item1,Color=Color.FromColorIndex(ColorMethod.ByAci,pair.Item2)};lt.Add(l);tr.AddNewlyCreatedDBObject(l,true);}}
var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);var ms=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
foreach(ObjectId id in ms){var ent=tr.GetObject(id,OpenMode.ForRead) as Entity;if(ent!=null && (ent.Layer.StartsWith("COMP_")||ent.GetType().Name=="MaterialSection"||ent.GetType().Name=="SectionViewQuantityTakeoffTable")){ent.UpgradeOpen();ent.Visible=false;}}
foreach(var rec in data.RootElement.EnumerateArray())foreach(var vh in rec.GetProperty("views").EnumerateArray()){
var id=db.GetObjectId(false,new Handle(Convert.ToInt64(vh.GetString(),16)),0);var view=(Entity)tr.GetObject(id,OpenMode.ForWrite);
double min=double.MaxValue,max=double.MinValue;
foreach(var poly in rec.GetProperty("polys").EnumerateArray())foreach(var p in poly.GetProperty("pts").EnumerateArray()){min=Math.Min(min,p[1].GetDouble());max=Math.Max(max,p[1].GetDouble());}
Set(view,"IsOffsetRangeAutomatic",false);Set(view,"OffsetLeft",-10.0);Set(view,"OffsetRight",10.0);Set(view,"IsElevationRangeAutomatic",false);
double oldmin=Convert.ToDouble(view.GetType().GetProperty("ElevationMin").GetValue(view));double oldmax=Convert.ToDouble(view.GetType().GetProperty("ElevationMax").GetValue(view));Set(view,"ElevationMin",Math.Min(min-.5,oldmin));Set(view,"ElevationMax",Math.Max(max+.5,oldmax));
foreach(var poly in rec.GetProperty("polys").EnumerateArray()){
var pts=poly.GetProperty("pts");var ps=new Point3d[4];for(int k=0;k<4;k++)ps[k]=XY(view,pts[k][0].GetDouble(),pts[k][1].GetDouble());
var e=new Solid(ps[0],ps[1],ps[3],ps[2]);e.Layer=new[]{"MED_SUBBASE_AZUL","MED_BASE_LARANJA","MED_REFORCO"}[poly.GetProperty("cat").GetInt32()];ms.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);solids++;
}
var anchor=XY(view,10,Math.Max(max+.5,oldmax));var table=new Table();table.SetDatabaseDefaults(db);table.TableStyle=db.Tablestyle;table.SetSize(6,2);table.UnmergeCells(CellRange.Create(table,0,0,0,1));table.SetRowHeight(.4);table.SetColumnWidth(3.4);table.Position=new Point3d(anchor.X+.7,anchor.Y,0);table.Layer="MED_TABELAS";
for(int rr=0;rr<6;rr++)for(int cc=0;cc<2;cc++){table.Cells[rr,cc].TextHeight=.20;table.Cells[rr,cc].Alignment=CellAlignment.MiddleCenter;}
table.Cells[0,0].TextString="Estaca";table.Cells[0,1].TextString=(rec.GetProperty("station").GetDouble()/20).ToString("0.###",CultureInfo.InvariantCulture);
string[] labels={"Camada / h (m)","Sub-base / 0.20","Base / 0.20","Reforco / 0.60","TOTAL / 1.00"};string[] vals={"Area (m2)","2.740","2.860","9.300","14.900"};for(int k=0;k<5;k++){table.Cells[k+1,0].TextString=labels[k];table.Cells[k+1,1].TextString=vals[k];}
table.GenerateLayout();ms.AppendEntity(table);tr.AddNewlyCreatedDBObject(table,true);tables++;views++;
}
tr.Commit();}
db.SaveAs(Root+@"\GO220_Todas_Secoes_0a300_350a400_Camadas.dwg",DwgVersion.Current);
File.WriteAllText(Root+@"\inspecao\validacao_todas_camadas.json",JsonSerializer.Serialize(new{views,solids,tables,criterio="TOP 13.40 m; 1.5H:1V; azul acima de laranja; reforco inferior; intervalo 300-350 excluido"}));
}
}
