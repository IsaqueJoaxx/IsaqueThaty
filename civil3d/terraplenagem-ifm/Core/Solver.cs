namespace Terraplenagem.IFM;

public static class Solver {
 const double Eps=1e-7;
 static void Nonnegative(double v,string name){if(!double.IsFinite(v)||v<0)throw new Exception(name+": informe valor finito não negativo.");}
 public static void Validate(Project p){
  if(p.Origens.Count==0&&p.Destinos.Count==0&&p.CompensacaoLateral.Count==0)throw new Exception("Cadastre origens e destinos.");
  if(p.Origens.Count+p.Destinos.Count>600)throw new Exception("Limite desta versão: 600 itens. Agrupe os intervalos antes de calcular.");
  void Names(IEnumerable<string> names,string what){var a=names.ToArray();if(a.Any(string.IsNullOrWhiteSpace)||a.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=a.Length)throw new Exception(what+": nomes vazios ou repetidos.");}
  Names(p.Origens.Select(x=>x.Nome),"Origens");Names(p.Destinos.Select(x=>x.Nome),"Destinos");
  Nonnegative(p.DMT_max_m,"DMT máximo");Nonnegative(p.Franquia_m,"Franquia");
  foreach(var o in p.Origens){if(!double.IsFinite(o.Progressiva_m))throw new Exception("Progressiva inválida: "+o.Nome);Nonnegative(o.Volume_banco_m3,o.Nome);Nonnegative(o.CBR_pct,"CBR");Nonnegative(o.Expansao_pct,"Expansão");Nonnegative(o.Permeabilidade_m_s,"Permeabilidade");Nonnegative(o.Acesso_m,"Acesso");if(o.Categoria<1||o.Categoria>3)throw new Exception("Categoria de origem deve ser 1, 2 ou 3.");}
  foreach(var d in p.Destinos){if(!double.IsFinite(d.Progressiva_m))throw new Exception("Progressiva inválida: "+d.Nome);Nonnegative(d.Volume_geometrico_m3,d.Nome);Nonnegative(d.Banco_por_geometrico,"Fator banco/geométrico");if(d.Banco_por_geometrico<=0)throw new Exception("Fator banco/geométrico deve ser positivo.");if(d.ADME&&Math.Abs(d.Banco_por_geometrico-1)>Eps)throw new Exception("ADME: informe capacidade em m³ de banco e fator 1.");Nonnegative(d.CBR_min_pct,"CBR mínimo");Nonnegative(d.Expansao_max_pct,"Expansão máxima");Nonnegative(d.Permeabilidade_max_m_s,"Permeabilidade máxima");Nonnegative(d.Acesso_m,"Acesso");if(d.Categoria_exigida<0||d.Categoria_exigida>3)throw new Exception("Categoria exigida: 0 (qualquer), 1, 2 ou 3.");}
  foreach(var b in p.Barreiras)if(!double.IsFinite(b.Progressiva_m))throw new Exception("Barreira inválida.");
  if(p.Faixas.Count==0)throw new Exception("Cadastre faixas de DMT.");
  foreach(var b in p.Faixas){Nonnegative(b.Ate_m,"Limite da faixa");Nonnegative(b.Custo_por_m3_banco,"Custo");}
  if(p.Faixas.Select(x=>x.Ate_m).Distinct().Count()!=p.Faixas.Count)throw new Exception("Faixas com limites repetidos.");
  foreach(var r in p.Rotas){if(!p.Origens.Any(x=>x.Nome==r.Origem)||!p.Destinos.Any(x=>x.Nome==r.Destino))throw new Exception("Rota com origem/destino inexistente.");if(!double.IsFinite(r.DMT_override_m)||r.DMT_override_m< -1)throw new Exception("DMT de rota: -1 para automático, ou distância não negativa.");}
  if(p.Rotas.GroupBy(x=>(x.Origem,x.Destino)).Any(g=>g.Count()>1))throw new Exception("Rotas duplicadas para o mesmo par.");
 }
 public static double? Distance(Project p,Origin o,Destination d){
  if(o.Jazida&&d.ADME||o.SoDescarte&&!d.ADME)return null;
  if(!d.ADME&&(o.CBR_pct+Eps<d.CBR_min_pct||o.Expansao_pct>d.Expansao_max_pct+Eps||o.Permeabilidade_m_s>d.Permeabilidade_max_m_s+Eps||d.Categoria_exigida!=0&&o.Categoria!=d.Categoria_exigida))return null;
  var route=p.Rotas.SingleOrDefault(x=>x.Origem==o.Nome&&x.Destino==d.Nome);
  if(route?.Bloqueado==true)return null;
  // A route override may represent a detour around barriers; otherwise crossings are blocked.
  if(route is null||route.DMT_override_m<0){if(p.Barreiras.Any(b=>b.Progressiva_m>Math.Min(o.Progressiva_m,d.Progressiva_m)&&b.Progressiva_m<Math.Max(o.Progressiva_m,d.Progressiva_m)))return null;}
  var dist=route is not null&&route.DMT_override_m>=0?route.DMT_override_m:Math.Abs(o.Progressiva_m-d.Progressiva_m)+o.Acesso_m+d.Acesso_m;
  return dist<=p.DMT_max_m+Eps?dist:null;
 }
 sealed class Edge {public int To,Rev;public double Cap,Cost,Initial;public Edge(int to,int rev,double cap,double cost){To=to;Rev=rev;Cap=Initial=cap;Cost=cost;}}
 public static Result Run(Project p){
  Validate(p);int no=p.Origens.Count,nd=p.Destinos.Count,n=no+nd+4,s=n-2,t=n-1,unused=no+nd,unmet=no+nd+1;
  var graph=Enumerable.Range(0,n).Select(_=>new List<Edge>()).ToArray();
  Edge Add(int a,int b,double cap,double cost){var e=new Edge(b,graph[b].Count,cap,cost);var r=new Edge(a,graph[a].Count,0,-cost);graph[a].Add(e);graph[b].Add(r);return e;}
  var refs=new List<(int I,int J,double D,Band B,Edge E)>();
  for(int i=0;i<no;i++)Add(s,i,p.Origens[i].Volume_banco_m3,0);
  for(int j=0;j<nd;j++)Add(no+j,t,p.Destinos[j].Volume_geometrico_m3*p.Destinos[j].Banco_por_geometrico,0);
  for(int i=0;i<no;i++)for(int j=0;j<nd;j++){
   var dist=Distance(p,p.Origens[i],p.Destinos[j]);if(dist is null)continue;
   var band=p.Faixas.OrderBy(b=>b.Ate_m).FirstOrDefault(b=>dist.Value<=b.Ate_m+Eps)??throw new Exception("Faixas não cobrem DMT de "+dist.Value+" m.");
   double cost=p.UsarCustos?band.Custo_por_m3_banco:p.MinimizarExtraordinario?Math.Max(0,dist.Value-p.Franquia_m):dist.Value;
   refs.Add((i,j,dist.Value,band,Add(i,no+j,Math.Min(p.Origens[i].Volume_banco_m3,p.Destinos[j].Volume_geometrico_m3*p.Destinos[j].Banco_por_geometrico),cost)));
  }
  // Balance the transport tableau without forcing reserve quarries or ADME capacity to be used.
  // Unallocated cuts and unserved fills carry a dominant penalty; optional capacity carries none.
  double supply=p.Origens.Sum(o=>o.Volume_banco_m3),demand=p.Destinos.Sum(d=>d.Volume_geometrico_m3*d.Banco_por_geometrico);
  double penalty=(refs.Count==0?1:refs.Max(x=>x.E.Cost)+1)*(supply+demand+1);
  if(!double.IsFinite(penalty)||penalty>1e18)throw new Exception("Escala numérica excessiva. Reduza unidades/custos/volumes antes de calcular.");
  Add(s,unmet,demand,0);Add(unused,t,supply,0);Add(unmet,unused,Math.Max(supply,demand),0);
  for(int i=0;i<no;i++)Add(i,unused,p.Origens[i].Volume_banco_m3,p.Origens[i].Jazida?0:penalty);
  for(int j=0;j<nd;j++)Add(unmet,no+j,p.Destinos[j].Volume_geometrico_m3*p.Destinos[j].Banco_por_geometrico,p.Destinos[j].ADME?0:penalty);
  // Successive shortest augmenting paths with reverse arcs and reduced-cost potentials.
  var potential=new double[n];int iterations=0;
  while(true){
   var distance=Enumerable.Repeat(double.PositiveInfinity,n).ToArray();var prevNode=Enumerable.Repeat(-1,n).ToArray();var prevEdge=new int[n];var queue=new PriorityQueue<int,double>();distance[s]=0;queue.Enqueue(s,0);
   while(queue.TryDequeue(out int u,out double priority)){
    if(priority>distance[u]+1e-9)continue;
    for(int k=0;k<graph[u].Count;k++){var e=graph[u][k];if(e.Cap<=Eps)continue;var reduced=e.Cost+potential[u]-potential[e.To];if(reduced<0&&reduced> -1e-6)reduced=0;var next=distance[u]+reduced;if(next+1e-9<distance[e.To]){distance[e.To]=next;prevNode[e.To]=u;prevEdge[e.To]=k;queue.Enqueue(e.To,next);}}
   }
   if(prevNode[t]<0)break;
   for(int u=0;u<n;u++)if(double.IsFinite(distance[u]))potential[u]+=distance[u];
   var delta=double.PositiveInfinity;for(int v=t;v!=s;v=prevNode[v])delta=Math.Min(delta,graph[prevNode[v]][prevEdge[v]].Cap);
   for(int v=t;v!=s;v=prevNode[v]){var e=graph[prevNode[v]][prevEdge[v]];e.Cap-=delta;graph[v][e.Rev].Cap+=delta;}
   if(++iterations>200000)throw new Exception("Limite de iterações atingido. Nenhum resultado parcial foi aceito.");
  }
  var result=new Result();
  foreach(var a in refs){double volume=a.E.Initial-a.E.Cap;if(volume<=Eps)continue;var o=p.Origens[a.I];var d=p.Destinos[a.J];result.Movimentos.Add(new(o.Nome,d.Nome,o.Categoria,o.Jazida,d.ADME,d.Energia,volume,volume/d.Banco_por_geometrico,a.D,a.B.Nome,volume*a.D/1000,volume*Math.Max(0,a.D-p.Franquia_m)/1000,volume*a.B.Custo_por_m3_banco));result.Objetivo+=volume*a.E.Cost;}
  foreach(var o in p.Origens){var rest=o.Volume_banco_m3-result.Movimentos.Where(m=>m.Origem==o.Nome).Sum(m=>m.Banco_m3);if(rest>Eps)result.Pendencias.Add(new(o.Nome,o.Jazida?"Reserva de jazida não utilizada":"Corte sem destino — indicar ADME/rota compatível",rest,rest));}
  foreach(var d in p.Destinos){var rest=d.Volume_geometrico_m3-result.Movimentos.Where(m=>m.Destino==d.Nome).Sum(m=>m.Geometrico_m3);if(rest>Eps)result.Pendencias.Add(new(d.Nome,d.ADME?"Capacidade ADME não utilizada":"Demanda não atendida — indicar empréstimo/rota compatível",rest*d.Banco_por_geometrico,rest));}
  return result;
 }
}
public static class Cubation {
 public static void Build(Project p,List<AreaRow> rows,double factor,double maxLength,bool lateral){
  if(rows.Count<2)throw new Exception("Pelo menos duas seções são necessárias.");
  if(!double.IsFinite(factor)||factor<=0||!double.IsFinite(maxLength)||maxLength<=0)throw new Exception("Fator e comprimento devem ser positivos.");
  for(int i=0;i<rows.Count;i++){var r=rows[i];if(!double.IsFinite(r.Progressiva_m)||!double.IsFinite(r.Corte_m2)||!double.IsFinite(r.Aterro_m2)||r.Corte_m2<0||r.Aterro_m2<0||i>0&&r.Progressiva_m<=rows[i-1].Progressiva_m)throw new Exception("Seções devem ter áreas não negativas e progressivas estritamente crescentes.");if(r.VolumeInformado&&(!double.IsFinite(r.Corte_incremental_m3)||!double.IsFinite(r.Aterro_incremental_m3)||r.Corte_incremental_m3<0||r.Aterro_incremental_m3<0))throw new Exception("Volume incremental inválido.");}
  var origins=new List<Origin>();var dests=new List<Destination>();var locals=new List<Lateral>();
  double cv=0,fv=0,cMoment=0,fMoment=0,start=rows[0].Progressiva_m;
  void Flush(){if(cv>1e-7)origins.Add(new(){Nome="C"+(origins.Count+1),Progressiva_m=cMoment/cv,Volume_banco_m3=cv});if(fv>1e-7)dests.Add(new(){Nome="A"+(dests.Count+1),Progressiva_m=fMoment/fv,Volume_geometrico_m3=fv,Banco_por_geometrico=factor});cv=fv=cMoment=fMoment=0;}
  for(int i=1;i<rows.Count;i++){
   var a=rows[i-1];var b=rows[i];double l=b.Progressiva_m-a.Progressiva_m;
   if(l>maxLength+1e-7)throw new Exception("Espaçamento entre seções supera comprimento máximo. Aumente o limite ou forneça mais seções.");
   if(b.Progressiva_m-start>maxLength+1e-7){Flush();start=a.Progressiva_m;}
   double c=b.VolumeInformado?b.Corte_incremental_m3:l*(a.Corte_m2+b.Corte_m2)/2;
   double f=b.VolumeInformado?b.Aterro_incremental_m3:l*(a.Aterro_m2+b.Aterro_m2)/2;
   double cc=b.VolumeInformado?(a.Progressiva_m+b.Progressiva_m)/2:a.Progressiva_m+(a.Corte_m2+b.Corte_m2>0?l*(a.Corte_m2+2*b.Corte_m2)/(3*(a.Corte_m2+b.Corte_m2)):l/2);
   double fc=b.VolumeInformado?(a.Progressiva_m+b.Progressiva_m)/2:a.Progressiva_m+(a.Aterro_m2+b.Aterro_m2>0?l*(a.Aterro_m2+2*b.Aterro_m2)/(3*(a.Aterro_m2+b.Aterro_m2)):l/2);
   // Optional same-interval lateral compensation, explicitly assumes all cut compatible.
   if(lateral){double same=Math.Min(c,f*factor);c-=same;f-=same/factor;if(same>1e-7)locals.Add(new(a.Progressiva_m,b.Progressiva_m,same,same/factor));}
   cv+=c;fv+=f;cMoment+=c*cc;fMoment+=f*fc;
  }
  Flush();p.Origens=origins;p.Destinos=dests;p.Cubacao=rows;p.CompensacaoLateral=locals;p.FatorCubacao=factor;
 }
}
