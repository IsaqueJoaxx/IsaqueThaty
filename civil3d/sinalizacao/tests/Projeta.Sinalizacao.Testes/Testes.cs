namespace Projeta.Sinalizacao.Testes;

public static partial class Testes
{
    private static int _ok, _falhas;

    public static void Checar(bool cond, string nome, string detalhe = "")
    {
        if (cond) { _ok++; Console.WriteLine($"  ok   {nome}"); }
        else { _falhas++; Console.WriteLine($"  FALHA {nome} {detalhe}"); }
    }

    public static void Perto(double a, double b, double tol, string nome)
        => Checar(Math.Abs(a - b) <= tol, nome, $"(obtido {a:0.####}, esperado {b:0.####} ± {tol})");

    public static int Rodar(string saida)
    {
        foreach (var t in typeof(Testes).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                     .Where(m => m.Name.StartsWith("T_")).OrderBy(m => m.Name))
        {
            Console.WriteLine(t.Name[2..]);
            try { t.Invoke(null, new object[] { saida }); }
            catch (Exception ex) { _falhas++; Console.WriteLine($"  FALHA exceção: {(ex.InnerException ?? ex).Message}\n{(ex.InnerException ?? ex).StackTrace}"); }
        }
        Console.WriteLine($"\n{_ok} ok, {_falhas} falha(s)");
        return _falhas == 0 ? 0 : 1;
    }
}
