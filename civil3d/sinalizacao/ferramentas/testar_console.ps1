# ============================================================================
# Roda um comando do plugin no console do Civil 3D (accoreconsole), sem abrir janelas,
# numa cópia do template métrico — não toca em nenhum desenho seu.
# Uso:  powershell -ExecutionPolicy Bypass -File .\ferramentas\testar_console.ps1 -Versao 2026
#       (opcional) -Comando SINDEMO  -SemEixos  -Detalhado  -Dwg C:\caminho\arquivo.dwg
#       -Roteiro .\ferramentas\teste_interativo.txt   (respostas dos prompts, uma por linha; linha vazia = Enter)
# Saída: %TEMP%\projeta_sin_demo\ (DWG, planilha, log do console) e %TEMP%\projeta_sin_demo.log
# ============================================================================
param([string]$Versao = "2026", [string]$Comando = "SINDEMO", [switch]$SemEixos, [switch]$Detalhado, [string]$Dwg = "", [string]$Roteiro = "")
$t = Join-Path $env:TEMP "projeta_sin_demo"
New-Item -ItemType Directory -Force $t | Out-Null
$raiz = Split-Path -Parent $PSScriptRoot
if ($Dwg -eq "") {
    $tpl = Get-ChildItem "$env:LOCALAPPDATA\Autodesk\C3D $Versao" -Recurse -Filter "*Civil 3D (Metric) NCS.dwt" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    if (-not $tpl) { Write-Host "Template métrico do Civil 3D $Versao não encontrado." -ForegroundColor Red; exit 1 }
    $Dwg = Join-Path $t "sindemo_$Versao.dwg"
    Copy-Item $tpl $Dwg -Force
}
$tfm = if ($Versao -eq "2027") { "net10.0-windows" } else { "net8.0-windows" }
$dll = Join-Path $raiz "src\Projeta.Sinalizacao\bin\Release\$tfm\Projeta.Sinalizacao.dll"
$scr = Join-Path $t "demo.scr"
$cmds = @($Comando)
if ($Roteiro -ne "") { $cmds = (Get-Content $Roteiro -Encoding UTF8) | ForEach-Object { $_.Replace("%PASTA%", $t) } }
# o script do console é lido como ANSI: grava em Windows-1252 para manter acentos
[System.IO.File]::WriteAllLines($scr, [string[]](@("SECURELOAD", "0", "OSMODE", "0", "NETLOAD", "`"$dll`"") + $cmds + @("_.QSAVE", "")), [System.Text.Encoding]::GetEncoding(1252))
if ($Detalhado) { $env:SINDEMO_DETALHADO = "1" } else { Remove-Item Env:SINDEMO_DETALHADO -ErrorAction SilentlyContinue }
if ($SemEixos) { $env:SINDEMO_SEM_EIXOS = "1" } else { Remove-Item Env:SINDEMO_SEM_EIXOS -ErrorAction SilentlyContinue }
$saida = Join-Path $t "saida_$Versao.txt"
$p = Start-Process -FilePath "C:\Program Files\Autodesk\AutoCAD $Versao\accoreconsole.exe" -ArgumentList "/i `"$Dwg`" /s `"$scr`" /product C3D /l pt-BR" -NoNewWindow -PassThru -RedirectStandardOutput $saida
$null = $p.Handle  # guarda o handle para ler o ExitCode depois
if (-not $p.WaitForExit(300000)) { $p.Kill(); "TIMEOUT (o console ficou esperando uma resposta — veja $saida)" } else { "exit: $($p.ExitCode)" }
$log = Join-Path $env:TEMP "projeta_sin_demo.log"
if (Test-Path $log) { Get-Content $log -Encoding UTF8 }
