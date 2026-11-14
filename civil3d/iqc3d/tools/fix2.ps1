$S = Join-Path (Split-Path $PSScriptRoot) 'src'
$u = New-Object Text.UTF8Encoding($false)
# comentario com acento corrompido
$f = Join-Path $S 'Util.cs'; $t = [IO.File]::ReadAllText($f, [Text.Encoding]::UTF8)
$t = [regex]::Replace($t, '// trabalha em cent[^\r\n]*', '// trabalha em centimetros')
[IO.File]::WriteAllText($f, $t, $u)
# dialogo de salvar na pasta do desenho
$f = Join-Path $S 'Alinhamentos.cs'; $t = [IO.File]::ReadAllText($f, [Text.Encoding]::UTF8)
$novo = @'
            string pasta = null;
            try { if (U.Db.Filename != null && File.Exists(U.Db.Filename) && !U.Db.Filename.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) pasta = Path.GetDirectoryName(U.Db.Filename); } catch { }
            if (pasta == null) pasta = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var sfd = new Autodesk.AutoCAD.Windows.SaveFileDialog(TITULO, Path.Combine(pasta, $"Nota de servico - {nomeAl}.xlsx"),
'@
$t2 = [regex]::Replace($t, '(?m)^\s*var sfd = new Autodesk\.AutoCAD\.Windows\.SaveFileDialog\(("Salvar nota de servi[^"]*"), \$"Nota de servico - \{nomeAl\}\.xlsx",', { param($m) $novo.Replace('TITULO', $m.Groups[1].Value) })
if ($t2 -eq $t) { 'NAO ACHEI sfd' } else { [IO.File]::WriteAllText($f, $t2, $u); 'ok sfd' }
