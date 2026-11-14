using System.Globalization;
using System.Text;
using System.Text.Json;
namespace Terraplenagem.IFM;

public sealed class Origin {
 public string Nome {get;set;}="C1"; public double Progressiva_m {get;set;}
 public double Volume_banco_m3 {get;set;} public int Categoria {get;set;}=1;
 public double CBR_pct {get;set;}=10; public double Expansao_pct {get;set;}=1;
 public double Permeabilidade_m_s {get;set;} public double Acesso_m {get;set;}
 public bool Jazida {get;set;} public bool SoDescarte {get;set;}
}
public sealed class Destination {
 public string Nome {get;set;}="A1"; public double Progressiva_m {get;set;}
 public double Volume_geometrico_m3 {get;set;}
 // One bank-equivalent unit per destination avoids conflating bank and compacted volumes.
 public double Banco_por_geometrico {get;set;}=1;
 public double CBR_min_pct {get;set;} public double Expansao_max_pct {get;set;}=100;
 public double Permeabilidade_max_m_s {get;set;}=1;
 public int Categoria_exigida {get;set;} public double Acesso_m {get;set;}
 public bool ADME {get;set;} public string Energia {get;set;}="Informar";
}
public sealed class Band {
 public string Nome {get;set;}="0–200 m"; public double Ate_m {get;set;}=200;
 public double Custo_por_m3_banco {get;set;}=1;
}
public sealed class Rule {
 public string Origem {get;set;}=""; public string Destino {get;set;}="";
 public bool Bloqueado {get;set;}
 // -1 means calculate by station + access. Explicit values are route distances.
 public double DMT_override_m {get;set;}=-1;
}
public sealed class Barrier {public double Progressiva_m {get;set;} public string Motivo {get;set;}="Travessia proibida";}
public sealed class AreaRow {
 public double Progressiva_m {get;set;} public double Corte_m2 {get;set;} public double Aterro_m2 {get;set;}
 public double Corte_incremental_m3 {get;set;} public double Aterro_incremental_m3 {get;set;}
 public bool VolumeInformado {get;set;}
}
public sealed record Lateral(double Inicio_m,double Fim_m,double Banco_m3,double Aterro_geometrico_m3);
public sealed class Project {
 public int Schema {get;set;}=1;
 public string Nome {get;set;}="Distribuição de terraplenagem";
 public List<Origin> Origens {get;set;}=new(); public List<Destination> Destinos {get;set;}=new();
 public List<Band> Faixas {get;set;}=new(){new(){Nome="0–200 m",Ate_m=200,Custo_por_m3_banco=1},new(){Nome="200–1000 m",Ate_m=1000,Custo_por_m3_banco=2},new(){Nome="Acima de 1000 m",Ate_m=1e9,Custo_por_m3_banco=4}};
 public List<Rule> Rotas {get;set;}=new(); public List<Barrier> Barreiras {get;set;}=new();
 public List<AreaRow> Cubacao {get;set;}=new(); public string Fonte {get;set;}="Entrada manual";
 public List<Lateral> CompensacaoLateral {get;set;}=new();
 public double FatorCubacao {get;set;}=1;
 public double DMT_max_m {get;set;}=1e9; public double Franquia_m {get;set;}=0;
 public bool UsarCustos {get;set;} public bool MinimizarExtraordinario {get;set;}
 public static Project Example()=> new(){Nome="Exemplo fictício",Origens=new(){new(){Nome="C1",Progressiva_m=0,Volume_banco_m3=1000,CBR_pct=15},new(){Nome="C2",Progressiva_m=1200,Volume_banco_m3=800,CBR_pct=5},new(){Nome="J1",Progressiva_m=2000,Volume_banco_m3=600,CBR_pct=20,Jazida=true,Acesso_m=300}},Destinos=new(){new(){Nome="A1",Progressiva_m=300,Volume_geometrico_m3=700,Banco_por_geometrico=1.1,CBR_min_pct=10},new(){Nome="A2",Progressiva_m=1400,Volume_geometrico_m3=800,Banco_por_geometrico=1.1,CBR_min_pct=4},new(){Nome="ADME1",Progressiva_m=1700,Volume_geometrico_m3=300,ADME=true}}};
}
public sealed record Movement(string Origem,string Destino,int Categoria,bool Jazida,bool ADME,string Energia,double Banco_m3,double Geometrico_m3,double DMT_m,string Faixa,double Momento_m3_km,double Extraordinario_m3_km,double Custo);
public sealed record Residual(string Nome,string Situacao,double Banco_m3,double Geometrico_m3);
public sealed class Result {
 public List<Movement> Movimentos {get;}=new(); public List<Residual> Pendencias {get;}=new();
 public double Objetivo {get;set;} public double Distribuido_m3=>Movimentos.Sum(x=>x.Banco_m3);
}
public static class Files {
 public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true};
 public static void Save(string path,Project p)=>File.WriteAllText(path,JsonSerializer.Serialize(p,JsonOptions),Encoding.UTF8);
 public static Project Load(string path){var p=JsonSerializer.Deserialize<Project>(File.ReadAllText(path),JsonOptions)??throw new Exception("Projeto vazio.");if(p.Schema!=1)throw new Exception("Versão de projeto não suportada.");return p;}
 public static double Number(string s){s=s.Trim();if(s.Contains(',')&&s.Contains('.'))throw new Exception("Use números sem separador de milhar: "+s);if(!double.TryParse(s.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n))throw new Exception("Número inválido: "+s);return n;}
 public static List<AreaRow> ReadAreas(string text) {
  var lines=text.Split('\n').Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
  if(lines.Length<3)throw new Exception("Informe cabeçalho e pelo menos duas seções.");
  var sep=lines[0].Contains(';')?';':'\t';var h=lines[0].Trim().TrimStart('\uFEFF').Split(sep).Select(x=>x.Trim()).ToArray();
  var required=new[]{"progressiva_m","corte_m2","aterro_m2"};
  if(!h.SequenceEqual(required))throw new Exception("Cabeçalho esperado: progressiva_m;corte_m2;aterro_m2");
  var rows=new List<AreaRow>();foreach(var l in lines.Skip(1)){var a=l.Trim().Split(sep);if(a.Length!=3)throw new Exception("Linha deve ter três campos: "+l);rows.Add(new(){Progressiva_m=Number(a[0]),Corte_m2=Number(a[1]),Aterro_m2=Number(a[2])});}return rows;
 }
 public static void Export(string path,Result r) {
  var sb=new StringBuilder("origem;destino;categoria;volume_banco_m3;volume_geometrico_m3;dmt_m;faixa;momento_m3_km;extraordinario_m3_km;custo\n");
  string N(double x)=>x.ToString("G17",CultureInfo.InvariantCulture);
  string Q(string x)=>"\""+x.Replace("\"","\"\"")+"\"";
  foreach(var m in r.Movimentos)sb.AppendLine(string.Join(";",Q(m.Origem),Q(m.Destino),m.Categoria,N(m.Banco_m3),N(m.Geometrico_m3),N(m.DMT_m),Q(m.Faixa),N(m.Momento_m3_km),N(m.Extraordinario_m3_km),N(m.Custo)));
  sb.AppendLine("\npendencia;situacao;volume_banco_m3;volume_geometrico_m3");foreach(var x in r.Pendencias)sb.AppendLine(string.Join(";",Q(x.Nome),Q(x.Situacao),N(x.Banco_m3),N(x.Geometrico_m3)));
  File.WriteAllText(path,sb.ToString(),Encoding.UTF8);
 }
}
