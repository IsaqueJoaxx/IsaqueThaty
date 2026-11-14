param([string]$Exe = '')
$exe = if ($Exe) { $Exe } else { Join-Path $env:LOCALAPPDATA 'Projeta\mcp\servidor\Projeta.Mcp.Servidor.exe' }
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.UseShellExecute = $false
$p = [System.Diagnostics.Process]::Start($psi)
$p.StandardInput.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"lista","version":"1"}}}')
$p.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
$p.StandardInput.WriteLine('{"jsonrpc":"2.0","id":2,"method":"tools/list"}')
$null = $p.StandardOutput.ReadLine()
$linha = $p.StandardOutput.ReadLine()
$p.StandardInput.Close()
$null = $p.WaitForExit(5000)
if (-not $p.HasExited) { $p.Kill() }
$r = $linha | ConvertFrom-Json
$i = 0
foreach ($t in $r.result.tools) {
    $i++
    $ro = if ($t.annotations.readOnlyHint) { 'leitura' } else { 'ESCREVE' }
    $os = if ($t.outputSchema) { ' +outputSchema' } else { '' }
    $d = ($t.description -split "`n")[0]
    if ($d.Length -gt 150) { $d = $d.Substring(0, 150) }
    Write-Output ('{0,2}. {1,-30} [{2}]{3}' -f $i, $t.name, $ro, $os)
    Write-Output ('    ' + $d)
}
Write-Output ('TOTAL: ' + $i)
