using Projeta.Civil3D;
var count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
void Reject(string csv) { try { IriCore.Parse(csv); } catch (ArgumentException) { count++; return; } throw new Exception("CSV inválido aceito: " + csv); }
var h = IriCore.Header + "\n";
var r = IriCore.Parse(h + "BR;1;C;D;0;100;2\nBR;1;C;D;100;400;4\nBR;1;C;E;0;100;8");
var s = IriCore.Summarize(r, 4);
Check(s.Count==2, "Trilhas independentes");
Check(Math.Abs(s[0].Mean-3.5)<1e-12, "Média ponderada por comprimento");
Check(s[0].AboveLength==0 && s[1].AboveLength==100, "Igualdade e excedência");
Check(s[0].Length==400 && s[0].Max==4, "Extensão e máximo");
Check(IriCore.Summarize(IriCore.Parse(h+"BR;1;C;D;0;100;2\nBR;1;C;D;200;300;4"),null)[0] is { Gaps:1, Length:200, Mean:3 }, "Lacunas excluídas");
Check(IriCore.Parse(h+"\"BR;X\";1;C;D;0;100;2,7")[0] is { Road:"BR;X", Iri:2.7 }, "Aspas e decimal brasileiro");
Check(IriCore.Parse((h+"BR;1;C;D;0;100;2.7").Replace(';','\t'))[0].Iri==2.7, "Tabulação");
Check(IriCore.Parse(IriCore.Example).Count==3, "Exemplo");
Reject(h+"BR;1;C;D;0;100;2\nBR;1;C;D;90;200;3");
Reject(h+"BR;1;C;D;0;100;2\nBR;1;C;D;0;100;2");
Reject(h+"BR;1;C;D;100;0;2"); Reject(h+"BR;1;C;D;0;0;2");
Reject(h+"BR;1;C;D;0;100;-1"); Reject(h+"BR;1;C;D;0;100;NaN");
Reject(h+"BR;1;C;D;0;Infinity;2"); Reject(h+"BR;1;C;D;0;1.000,00;2");
Reject(h+"BR;1;C;;0;100;2"); Reject(h); Reject("inicio;fim;iri\n0;100;2");
Check(IriCore.Export(r,4,"Contrato X").Contains("Até o limite informado"), "Exportação do critério");
Check(IriCore.Export(IriCore.Parse(h+"=FORMULA;1;C;D;0;100;2"),null,"").Contains("'=FORMULA"), "Neutralização de fórmula");
Console.WriteLine($"IRI: {count} verificações passaram.");
