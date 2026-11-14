namespace Projeta.Sinalizacao.Infra;

/// <summary>Mostra erros na linha de comando (em vez de derrubar o Civil 3D) e guarda o detalhe em erros.log.</summary>
internal static class Erros
{
    public static void Mostrar(System.Exception ex)
    {
        var e = ex is System.Reflection.TargetInvocationException { InnerException: not null } t ? t.InnerException! : ex;
        try { Ctx.Msg($"[ERRO] {e.GetType().Name}: {e.Message}"); } catch { }
        try
        {
            Directory.CreateDirectory(Config.PastaUsuario);
            File.AppendAllText(Path.Combine(Config.PastaUsuario, "erros.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {e}{Environment.NewLine}{Environment.NewLine}");
            Comandos.CmdDemo.Passo("ERRO " + e);
        }
        catch { }
    }
}
