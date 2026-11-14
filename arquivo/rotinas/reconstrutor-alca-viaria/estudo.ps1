$t = Join-Path $env:TEMP "prj_alca"; $L = "D:\PROJETA ENGENHARIA\Alça Viaria\_reconstrucao_c3d\leitura"
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg "$t\var.dwg" -Comando PRJSECOES -Saida "$L\secoes_variante.json" -TimeoutSeg 1800
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg "$t\raul_base.dwg" -Comando PRJSECOES -Saida "$L\secoes_original.json" -TimeoutSeg 1800
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg "$t\var.dwg" -Comando PRJDEM -Saida "$L\dem.bin" -TimeoutSeg 1800
$env:PRJ_CAMADAS = '^TOP_'; $env:PRJ_BLOCOS = '^TOPOGRAFIA$'
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg "$t\ref_prj50998.dwg" -Comando PRJGEOM -Saida "$L\topografia.json" -TimeoutSeg 900
"FIM_ESTUDO"
