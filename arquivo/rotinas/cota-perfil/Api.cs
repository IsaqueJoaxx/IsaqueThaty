using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
public class Api {
 [CommandMethod("COTAAPI")]
 public void Run(){var a=Assembly.LoadFrom(@"C:\Program Files\Autodesk\AutoCAD 2026\C3D\AeccDbMgd.dll"); File.WriteAllLines(@"D:\PROJETA ENGENHARIA\Rotina_Cota_Perfil\api.txt",a.GetTypes().Where(t=>new[]{"StationOffsetLabel","LabelStyle","LabelStyleReferenceTextComponent","LabelStyleTextComponent","LabelStyleTextComponentStyleText","ReferenceTextComponentSelectedType","LabelStylesAlignmentRoot","Baseline","Profile","LabelBase","Label","StyleText","StyleGeneral","ProfileType"}.Contains(t.Name)).SelectMany(t=>new[]{"TYPE "+t.FullName}.Concat(t.GetMembers().Select(m=>m.ToString()))));}
}

