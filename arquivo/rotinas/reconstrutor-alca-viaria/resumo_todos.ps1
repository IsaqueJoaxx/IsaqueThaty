$base = "D:\PROJETA ENGENHARIA\Alça Viaria"
$out = Join-Path $base "_reconstrucao_c3d\leitura"; New-Item -ItemType Directory -Force $out | Out-Null
$lista = @(
  @{ dwg = "Alça Viaria 07 11 2026r1 Infra.dwg"; nome = "r1_infra" },
  @{ dwg = "PRJ-50998-GEO-BSC-0101-REV02.dwg"; nome = "prj50998" },
  @{ dwg = "Alça Viaria 07 11 2026.dwg"; nome = "primitivo" },
  @{ dwg = "AlcaViariaCompleto_CIVIL.dwg"; nome = "completo_civil" }
)
foreach ($i in $lista) {
  "=== $($i.nome)"
  & (Join-Path $PSScriptRoot "rodar.ps1") -Dwg (Join-Path $base $i.dwg) -Comando "PRJRESUMO" -Saida (Join-Path $out ("resumo_" + $i.nome + ".json")) -TimeoutSeg 1500
}
"FIM_TODOS"
