param([string[]]$Desenhos = @("C:\Temp\nsteste\uba.dwg", "C:\Temp\nsteste\teste.dwg"))
$d = "C:\Temp\nsteste\av2"
(Get-Content "$(Join-Path $PSScriptRoot '..\projeta')\avulso\NotaServico\CmdNotaServicoAvulso.cs" -Raw -Encoding UTF8).
    Replace('CommandMethod("PRJNOTASERVICO"', 'CommandMethod("PRJNOTASERVICO2"').
    Replace('namespace Projeta.NotaServicoAvulso;', 'namespace Projeta.NotaServicoAvulso2;').
    Replace('typeof(Projeta.NotaServicoAvulso.CmdNotaServicoAvulso)', 'typeof(Projeta.NotaServicoAvulso2.CmdNotaServicoAvulso)') |
    Set-Content "$d\Cmd2.cs" -Encoding UTF8
Push-Location $d
dotnet build -c Release -nologo -v q 2>&1 | Select-String "error|Erro\(s\)"
Pop-Location
$scr = "C:\Temp\nsteste\ns2.scr"
@(
    'SECURELOAD', '0', 'NETLOAD', '"C:\Temp\nsteste\av2\bin\Release\net10.0-windows\Projeta.NotaServico2.dll"',
    'PRJNOTASERVICO2', '', '_.QUIT', '_Y', ''
) | Set-Content $scr -Encoding ASCII
$ac = "C:\Program Files\Autodesk\AutoCAD 2027\accoreconsole.exe"
foreach ($dwg in $Desenhos) {
    $log = "C:\Temp\nsteste\ns2_" + [IO.Path]::GetFileNameWithoutExtension($dwg) + ".log"
    $a = @('/i', $dwg, '/s', $scr, '/product', 'C3D', '/language', 'pt-BR', '/isolate', 'nsteste', 'C:\Temp\nsteste\isolado')
    $p = Start-Process -FilePath $ac -ArgumentList $a -NoNewWindow -PassThru -RedirectStandardOutput $log -RedirectStandardError "$log.err"
    if (-not $p.WaitForExit(500000)) { "TIMEOUT $dwg"; $p.Kill() }
    "===== $dwg"
    Get-Content $log -Encoding Unicode | Where-Object { $_ -match 'Nota|linhas|AVISO|ERRO|Terraplenagem|usa |Banquetas|Offset|Superf|Exce' } | Select-Object -Last 25
}
