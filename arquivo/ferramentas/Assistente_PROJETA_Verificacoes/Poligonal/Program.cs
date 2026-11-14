using Projeta.Civil3D;
var folder=Path.Combine(@"D:\PROJETA ENGENHARIA","Calculo de poligonal VLI");
var book=Traverse.Read(Path.Combine(folder,"POLIGONAL03CC.M21"));
var support=Traverse.ReadText(Path.Combine(folder,"APOIO.txt"));
var route=string.Join(" > ",new[]{"M002"}.Concat(book.Stations).Append("M001"));
var aliases="T4`T3=T3\nT13`T12=T12\nT14`T12=T12\nT22`T21=T21";
if(Traverse.SuggestedRoute(book)!=route)throw new Exception("Orientação sugerida incorreta");
var suggested=Traverse.SuggestedAliases(book,route);
foreach(var row in aliases.Split('\n')){var a=row.Split('=');if(suggested.GetValueOrDefault(a[0])!=a[1])throw new Exception("Associação proposta incorreta");}
if(suggested.Count!=4)throw new Exception("Quantidade de associações incorreta");
var result=Traverse.Calculate(book,support,route,aliases,1,0,0,"Não informado");
if(book.Shots.Count!=334||result.Points.Count!=28)throw new Exception("Leituras/estações perdidas");
if(Math.Abs(result.AngularError-73.373)>0.002||Math.Abs(result.LinearError-.0366)>.0001)throw new Exception("Difere da memória independente");
var known=Traverse.Support(support);
if(result.Points[0]!=known["M001"]||result.Points[^1]!=known["M002"])throw new Exception("Apoios alterados");
var csv=File.ReadAllLines(Path.Combine(folder,"Resultado_conferido","COORDENADAS_AJUSTADAS.csv"));
var expected=Traverse.Support(string.Join("\n",csv));
foreach(var p in result.Points){var q=expected[p.Name];if(Math.Max(Math.Abs(p.N-q.N),Math.Abs(p.E-q.E))>.000002||Math.Abs(p.Z-q.Z)>.000002)throw new Exception("Coordenada difere da memória: "+p.Name);}
void Reject(Action a){try{a();}catch(ArgumentException){return;}throw new Exception("Entrada inválida aceita");}
Reject(()=>Traverse.Calculate(book,support,route,"",1,0,0,""));
Reject(()=>Traverse.Calculate(book,support,route,aliases,0,0,0,""));
Reject(()=>Traverse.Support("A;NaN;0;0"));
var limited=Traverse.Calculate(book,support,route,aliases,1,1,1000000,"");
if(!limited.Report.Contains("NÃO ATENDE"))throw new Exception("Fora da tolerância não sinalizado");
var normativeReference=Traverse.Calculate(book,support,route,aliases,1,0,12000,"");
if(!normativeReference.Report.Contains("12000,000000000 — ATENDE")||!normativeReference.Report.Contains("Tolerância angular absoluta informada: 0,000000000 segundos — NÃO AVALIADO"))throw new Exception("Referência linear confundida com aprovação integral");
var reverse=string.Join(" > ",new[]{"M001"}.Concat(book.Stations.Reverse()).Append("M002"));
var reversed=Traverse.Calculate(book,support,reverse,aliases,1,0,0,"");
if(reversed.Points[0]!=known["M002"]||reversed.Points[^1]!=known["M001"])throw new Exception("Percurso inverso incorreto");
var destination=Path.Combine(folder,"Validacao_Previa13");Directory.CreateDirectory(destination);
File.WriteAllText(Path.Combine(destination,"RELATORIO_IFM.txt"),result.Report);
File.WriteAllText(Path.Combine(destination,"MEMORIA_IFM.csv"),result.Memory);
Console.WriteLine($"OK: 334 leituras; 28 pontos; erro angular {result.AngularError:F6} segundos; linear {result.LinearError:F9} m. Coordenadas conferidas com memória anterior; percurso inverso, apoios, rejeições e tolerâncias verificados.");
