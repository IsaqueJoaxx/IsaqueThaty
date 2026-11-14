using Projeta.Sinalizacao.Testes;
using Projeta.Sinalizacao.Core.Vertical;

// roda com a cultura do Windows brasileiro (vírgula decimal) para pegar erros de formatação
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
var saida = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "sin-testes");
Directory.CreateDirectory(saida);

if (args.Length > 0 && args[0] == "folhas")
{
    File.WriteAllText(Path.Combine(saida, "catalogo_R.svg"), Folhas.Catalogo(Catalogo.Tipos.Where(t => t.Familia == Familia.Regulamentacao)));
    File.WriteAllText(Path.Combine(saida, "catalogo_A.svg"), Folhas.Catalogo(Catalogo.Tipos.Where(t => t.Familia == Familia.Advertencia)));
    Console.WriteLine("folhas em " + saida);
    return 0;
}

return Testes.Rodar(saida);
