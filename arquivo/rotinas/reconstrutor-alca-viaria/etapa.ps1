# Uso: etapa.ps1 -Etapa A|B|C|D   (A copia o primitivo para o arquivo de trabalho)
param([Parameter(Mandatory = $true)][string]$Etapa)
$base = "D:\PROJETA ENGENHARIA\Alça Viaria"
$rc = Join-Path $base "_reconstrucao_c3d"
$t = Join-Path $env:TEMP "prj_alca"; New-Item -ItemType Directory -Force $t | Out-Null
$trab = Join-Path $t "trabalho.dwg"
$env:PRJ_PLANO = Join-Path $rc "plano.json"
$env:PRJ_REF = Join-Path $t "ref_prj50998.dwg"
$cmd = @{ A = "PRJPREPARAR"; B = "PRJEIXOS"; C = "PRJCORREDORES"; D = "PRJCONFERIR" }[$Etapa]
if ($Etapa -eq "A") {
  Copy-Item -LiteralPath (Join-Path $base "Alça Viaria 07 11 2026.dwg") $trab -Force
  Copy-Item -LiteralPath (Join-Path $base "PRJ-50998-GEO-BSC-0101-REV02.dwg") $env:PRJ_REF -Force
  Copy-Item $trab (Join-Path $t "trabalho_inicial.dwg") -Force
}
$ant = @{ B = "A"; C = "B" }[$Etapa]
if ($ant) { Copy-Item (Join-Path $t ("trabalho_apos_" + $ant + ".dwg")) $trab -Force }
$salvar = $Etapa -ne "D"
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg $trab -Copia $trab -Comando $cmd -Saida (Join-Path $rc ("leitura\etapa_" + $Etapa + ".json")) -Salvar:$salvar -TimeoutSeg 2400
if ($salvar) { Copy-Item $trab (Join-Path $t ("trabalho_apos_" + $Etapa + ".dwg")) -Force; Get-Item $trab | Select-Object Length, LastWriteTime }
