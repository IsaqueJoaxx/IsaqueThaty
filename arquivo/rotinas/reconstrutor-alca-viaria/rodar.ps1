# Roda um comando da DLL Projeta.Reconstrutor no accoreconsole sobre uma COPIA do DWG (nunca o original).
param(
  [Parameter(Mandatory = $true)][string]$Dwg,
  [Parameter(Mandatory = $true)][string]$Comando,
  [Parameter(Mandatory = $true)][string]$Saida,
  [string[]]$Extra = @(),
  [switch]$Salvar,
  [string]$Copia = "",
  [int]$TimeoutSeg = 1200
)
$t = Join-Path $env:TEMP "prj_alca"; New-Item -ItemType Directory -Force $t | Out-Null
if ($Copia -eq "") { $Copia = Join-Path $t ("copia_" + [IO.Path]::GetFileNameWithoutExtension($Saida) + ".dwg"); Copy-Item -LiteralPath $Dwg $Copia -Force } else { $Copia = $Dwg }
$dll = Join-Path $PSScriptRoot "bin\Release\net10.0-windows\Projeta.Reconstrutor.dll"
$scr = Join-Path $t ("s_" + [IO.Path]::GetFileNameWithoutExtension($Saida) + ".scr")
$linhas = @("SECURELOAD", "0", "NETLOAD", "`"$dll`"", $Comando) + $Extra
if ($Salvar) { $linhas += @("_.QSAVE") }
$linhas += @("_.QUIT", "_Y", "")
[IO.File]::WriteAllLines($scr, [string[]]$linhas, [Text.Encoding]::GetEncoding(1252))
$log = Join-Path $t ("log_" + [IO.Path]::GetFileNameWithoutExtension($Saida) + ".txt")
$env:PRJ_SAIDA = $Saida
$ini = Get-Date
$p = Start-Process -FilePath "C:\Program Files\Autodesk\AutoCAD 2027\accoreconsole.exe" -ArgumentList "/i `"$Copia`" /s `"$scr`" /product C3D /l pt-BR" -NoNewWindow -PassThru -RedirectStandardOutput $log
$null = $p.Handle
if (-not $p.WaitForExit($TimeoutSeg * 1000)) { $p.Kill(); "TIMEOUT" } else { "exit: $($p.ExitCode)  tempo: $([int]((Get-Date)-$ini).TotalSeconds)s" }
((Get-Content $log -Raw -Encoding Unicode) -replace "`0", "") -split "`n" | Select-String -Pattern "PRJ|rro|xception|Unhandled" | Select-Object -Last 25 | ForEach-Object { $_.Line.Trim() }
Get-Item -LiteralPath $Saida -ErrorAction SilentlyContinue | Select-Object Name, Length
