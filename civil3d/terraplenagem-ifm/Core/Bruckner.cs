using System.Globalization;
using System.Text;
namespace Terraplenagem.IFM;
public static class Bruckner {
 public static string Svg(Project p){
  if(p.Cubacao.Count<2)return "";var rows=p.Cubacao;
  var points=new List<(double X,double Y)>{(rows[0].Progressiva_m,0)};double balance=0;
  for(int i=1;i<rows.Count;i++){var a=rows[i-1];var b=rows[i];double l=b.Progressiva_m-a.Progressiva_m;if(!double.IsFinite(l)||l<=0||a.Corte_m2<0||a.Aterro_m2<0||b.Corte_m2<0||b.Aterro_m2<0)throw new Exception("Cubação inválida para Bruckner. Gere as origens novamente após corrigir as seções.");double c=b.VolumeInformado?b.Corte_incremental_m3:l*(a.Corte_m2+b.Corte_m2)/2;double f=b.VolumeInformado?b.Aterro_incremental_m3:l*(a.Aterro_m2+b.Aterro_m2)/2;balance+=c-f*p.FatorCubacao;points.Add((b.Progressiva_m,balance));}
  double xmin=points[0].X,xmax=points[^1].X,ymin=Math.Min(0,points.Min(x=>x.Y)),ymax=Math.Max(0,points.Max(x=>x.Y));if(ymax-ymin<1e-9)ymax=ymin+1;
  string N(double x)=>x.ToString("0.###",CultureInfo.InvariantCulture);
  double X(double x)=>80+820*(x-xmin)/(xmax-xmin);double Y(double y)=>300-240*(y-ymin)/(ymax-ymin);
  var sb=new StringBuilder("<h2>Diagrama de Bruckner da cubação</h2><p>Saldo acumulado em m³ de banco: corte − aterro × fator de cubação "+N(p.FatorCubacao)+". Não considera a compatibilidade geotécnica; a distribuição deve ser lida no quadro otimizado.</p><svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 960 360' role='img' aria-label='Bruckner: saldo acumulado de corte e aterro' style='width:100%;max-width:960px;background:#f7fbfa'>");
  sb.Append($"<path d='M80 30 V300 H910' fill='none' stroke='#567'/><path d='M80 {N(Y(0))} H910' stroke='#aaa' stroke-dasharray='5 5'/><polyline fill='none' stroke='#177c70' stroke-width='3' points='");foreach(var q in points)sb.Append(N(X(q.X))+","+N(Y(q.Y))+" ");sb.Append("'/>");
  sb.Append($"<g font-family='Arial' font-size='13' fill='#203140'><text x='80' y='330'>{N(xmin)} m</text><text x='840' y='330'>{N(xmax)} m</text><text x='5' y='60'>{N(ymax)}</text><text x='5' y='300'>{N(ymin)}</text><text x='350' y='350'>Progressiva acumulada (m)</text><text x='80' y='20'>Saldo acumulado (m³ de banco)</text></g></svg>");return sb.ToString();
 }
}
