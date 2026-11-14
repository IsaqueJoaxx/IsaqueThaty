# ============================================================================
# Roda um comando do plugin no console do Civil 3D (accoreconsole), sem abrir janelas,
# numa copia do template metrico - nao toca em nenhum desenho seu.
# Uso:  powershell -ExecutionPolicy Bypass -File .\ferramentas\testar_console.ps1 -Versao 2027 [-Comando GIRODEMO]
# Saida: %TEMP%\projeta_giro\ (DWG e log do console)
# ============================================================================
param([string]$Versao = "2027", [string]$Comando = "GIRODEMO", [string]$Dwg = "")
$t = Join-Path $env:TEMP "projeta_giro"
New-Item -ItemType Directory -Force $t | Out-Null
$raiz = Split-Path -Parent $PSScriptRoot
if ($Dwg -eq "") {
    $tpl = Get-ChildItem "$env:LOCALAPPDATA\Autodesk\C3D $Versao" -Recurse -Filter "*Civil 3D (Metric) NCS.dwt" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    if (-not $tpl) { Write-Host "Template metrico do Civil 3D $Versao nao encontrado." -ForegroundColor Red; exit 1 }
    $Dwg = Join-Path $t "girodemo_$Versao.dwg"
    Copy-Item $tpl $Dwg -Force
}
$tfm = if ($Versao -eq "2027") { "net10.0-windows" } else { "net8.0-windows" }
$dll = Join-Path $raiz "src\Projeta.Giro\bin\Release\$tfm\Projeta.Giro.dll"
$scr = Join-Path $t "demo.scr"
[System.IO.File]::WriteAllLines($scr, [string[]](@("SECURELOAD", "0", "NETLOAD", "`"$dll`"", $Comando, "_.QSAVE", "")), [System.Text.Encoding]::GetEncoding(1252))
$saida = Join-Path $t "saida_$Versao.txt"
$iso = Join-Path $t "perfil"
$p = Start-Process -FilePath "C:\Program Files\Autodesk\AutoCAD $Versao\accoreconsole.exe" -ArgumentList "/i `"$Dwg`" /s `"$scr`" /product C3D /l pt-BR /isolate projetagiro `"$iso`"" -NoNewWindow -PassThru -RedirectStandardOutput $saida
$null = $p.Handle
if (-not $p.WaitForExit(300000)) { $p.Kill(); "TIMEOUT (veja $saida)" } else { "exit: $($p.ExitCode)" }
# o console escreve em UTF-16 com caracteres nulos intercalados
(Get-Content $saida -Raw -Encoding Unicode) -replace "`0", ""
