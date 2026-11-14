# Envolve cada [CommandMethod] em U.Rodar(...) para erros virarem mensagem e nao janela de crash.
$S = Join-Path (Split-Path $PSScriptRoot) 'src'
$u = New-Object Text.UTF8Encoding($false)
Get-ChildItem $S -Filter *.cs | ForEach-Object {
  $t = [IO.File]::ReadAllText($_.FullName)
  $n = [regex]::Replace($t, '(\[CommandMethod\([^\]]*\)\]\s*\r?\n\s*)public void (\w+)\(\)(\r?\n)(\s*)\{', '$1public void $2() => U.Rodar($2_);$3$4private void $2_()$3$4{')
  if ($n -ne $t) { [IO.File]::WriteAllText($_.FullName, $n, $u); "envolvido: $($_.Name)" }
}
# U.Rodar
$uf = Join-Path $S 'Util.cs'
$t = [IO.File]::ReadAllText($uf)
if ($t -notmatch 'void Rodar') {
  $t = $t.Replace('        public static void Msg(string s) => Ed.WriteMessage("\n" + s);', @'
        public static void Msg(string s) => Ed.WriteMessage("\n" + s);

        /// <summary>Executa o corpo de um comando mostrando o erro na linha de comando em vez de travar.</summary>
        public static void Rodar(Action corpo)
        {
            try { corpo(); }
            catch (System.Exception ex)
            {
                try
                {
                    Msg("ERRO: " + ex.Message);
                    var pilha = (ex.StackTrace ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Contains("IQC3D")).Take(3);
                    foreach (var l in pilha) Msg("   " + l);
                }
                catch { }
            }
        }
'@)
  [IO.File]::WriteAllText($uf, $t, $u); "Rodar adicionado"
}
