using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Projeta.Civil3D;
internal sealed record RainStudy(string Municipio,string Uf,string Estacao,string Codigo,string Ano,string Url,string TrMax,string Observacao)
{
    public override string ToString()=>$"{Municipio}/{Uf} — {Estacao} — {Ano}";
}
internal static class RainCatalog
{
    public const string Source="https://www.sgb.gov.br/documents/d/guest/tabela_relatorios_idf_municipios_br-2026_04_08";
    public const string Dnit="https://www.gov.br/dnit/pt-br/central-de-conteudos/atos-normativos/tipo/instrucao-normativa/2026/instrucao-normativa-no-08-2026";
    public static string Normalize(string text)=>new string(text.Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).Normalize().Trim().ToUpperInvariant();
    public static List<RainStudy> Bundled()=>JsonSerializer.Deserialize<List<RainStudy>>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(RainCatalog).Assembly.Location)!,"chuvas-sgb.json")))??throw new ArgumentException("Catálogo SGB vazio.");
    public static bool IsOfficial(string text)=>Uri.TryCreate(text,UriKind.Absolute,out var u)&&u.Scheme=="https"&&(u.Host=="rigeo.sgb.gov.br"||u.Host=="www.sgb.gov.br"||u.Host=="sgb.gov.br");
    public static RainStudy[] Find(IEnumerable<RainStudy> all,string city,string uf)=>all.Where(x=>Normalize(x.Municipio)==Normalize(city)&&x.Uf.Equals(uf,StringComparison.OrdinalIgnoreCase)).ToArray();
    public static async Task<List<RainStudy>> Update()
    {
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(25)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PROJETA-IFM/0.15");
        using var response=await client.GetAsync(Source,HttpCompletionOption.ResponseHeadersRead);response.EnsureSuccessStatusCode();
        using var stream=await response.Content.ReadAsStreamAsync();using var buffer=new MemoryStream();
        var chunk=new byte[8192];int count;while((count=await stream.ReadAsync(chunk))>0){if(buffer.Length+count>10_000_000)throw new ArgumentException("Catálogo acima de 10 MB.");buffer.Write(chunk,0,count);}buffer.Position=0;
        return ParseWorkbook(buffer);
    }
    internal static List<RainStudy> ParseWorkbook(Stream stream)
    {
        using var zip=new ZipArchive(stream,ZipArchiveMode.Read,true);
        XDocument Xml(string name)
        {
            var entry=zip.GetEntry(name)??throw new ArgumentException("Estrutura XLSX não reconhecida.");
            if(entry.Length>20_000_000)throw new ArgumentException("Tabela XLSX acima do limite.");using var s=entry.Open();return XDocument.Load(s);
        }
        XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var shared=zip.GetEntry("xl/sharedStrings.xml") is null?Array.Empty<string>():Xml("xl/sharedStrings.xml").Descendants(ns+"si").Select(x=>string.Concat(x.Descendants(ns+"t").Select(t=>t.Value))).ToArray();
        var result=new List<RainStudy>();bool header=false;
        foreach(var row in Xml("xl/worksheets/sheet1.xml").Descendants(ns+"row"))
        {
            var cells=new Dictionary<string,string>();
            foreach(var cell in row.Elements(ns+"c"))
            {
                var col=new string(((string?)cell.Attribute("r")??"").TakeWhile(char.IsLetter).ToArray());var value=cell.Element(ns+"v")?.Value??"";
                var type=(string?)cell.Attribute("t");if(type=="s")value=shared[int.Parse(value,CultureInfo.InvariantCulture)];else if(type=="inlineStr")value=string.Concat(cell.Descendants(ns+"t").Select(t=>t.Value));
                cells[col]=value;
            }
            string V(string c)=>cells.GetValueOrDefault(c,"");
            if(!header){if(V("C")!="UF"||!V("D").Contains("Município"))throw new ArgumentException("Cabeçalho SGB mudou; catálogo não adotado.");header=true;continue;}
            if(V("C").Length==2&&IsOfficial(V("L")))result.Add(new(V("D"),V("C"),V("P"),V("O"),V("M"),V("L"),V("V"),V("N")));
        }
        if(result.Count==0)throw new ArgumentException("Nenhum estudo reconhecido no catálogo SGB.");return result;
    }
}
