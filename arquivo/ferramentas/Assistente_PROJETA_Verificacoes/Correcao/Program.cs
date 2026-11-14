using Projeta.Civil3D;
int n=0;
void Check(bool pass,string message){if(!pass)throw new Exception(message);n++;}
void Reject(Action a){try{a();}catch(ArgumentException){n++;return;}throw new Exception("Entrada inválida aceita");}
var rows=CorrectionCore.Parse(CorrectionCore.Example);
var r=CorrectionCore.Calculate(rows);
Check(r[0].BaseAxis==100 && r[0].TopAxis==100.23m,"Base e concreto 23 cm");
Check(r[0].TopLeft==100.16m && r[0].TopRight==100.16m,"-2% em ambos bordos");
Check(r[1].TopAxis==100.2325m && r[1].TopLeft==100.1625m,"Correção mm para m");
Check(r[1].GapAxis==.0025m && r[1].GapLeft==.0025m,"Saldo abaixo da base");
Check(r[0].Check.StartsWith("Inicial") && r[0].DeltaMm is null,"Extremo inicial sem comparação");
Check(r[1].Check=="Dentro da regra" && r[2].Check.StartsWith("Excede"),"Limite e excesso");
var down=CorrectionCore.Calculate(new[]{rows[0],rows[1] with{CorrectionMm=-2.5m}});
Check(down[1].Check=="Dentro da regra" && down[1].GapAxis<0,"Correção negativa");
Check(CorrectionCore.Calculate(new[]{rows[0],rows[1] with{Station=6}})[1].Check.StartsWith("Intervalo"),"Sem interpolação da regra");
var asymmetric=CorrectionCore.Calculate(new[]{rows[0] with{WidthLeft=4,WidthRight=3,SlopeLeft=3,SlopeRight=-4}})[0];
Check(asymmetric.TopLeft==100.35m && asymmetric.TopRight==100.11m,"Larguras e caimentos independentes");
Check(CorrectionCore.Calculate(new[]{rows[0]},.3m)[0].TopAxis==100.3m,"Espessura editável");
Check(CorrectionCore.Calculate(new[]{rows[0] with{CorrectionMm=100},rows[1] with{CorrectionMm=102.5m}})[1].Check=="Dentro da regra","Variação não limita acumulado");
Reject(()=>CorrectionCore.Calculate(new[]{rows[0],rows[0]}));
Reject(()=>CorrectionCore.Calculate(new[]{rows[1],rows[0]}));
Reject(()=>CorrectionCore.Calculate(new[]{rows[0] with{WidthLeft=0}}));
Reject(()=>CorrectionCore.Calculate(rows,0));
Reject(()=>CorrectionCore.Parse(CorrectionCore.Header+"\n0;NaN;0;0;0;3;3;-2;-2;0"));
Check(CorrectionCore.Export(r,.23m).Contains("saldo_base_esq_m") && CorrectionCore.Export(r,.23m).Contains("100.2325"),"Exportação sem perda de milímetros");
Console.WriteLine($"Correção: {n} verificações passaram.");
