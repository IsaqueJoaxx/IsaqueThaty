$base = "D:\PROJETA ENGENHARIA\Alça Viaria"
$out = Join-Path $base "_reconstrucao_c3d\leitura"
$env:PRJ_CAMADAS = "^(F1?-HZ-|F1?-VT-(?!MALHA|CAIXA)|PERFIL|A-ANOTACAO|TEXTO|0$)"
$env:PRJ_BLOCOS = '^(A\$C6d36f9af|A\$Cae103dc0)$'
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg (Join-Path $base "PRJ-50998-GEO-BSC-0101-REV02.dwg") -Comando "PRJGEOM" -Saida (Join-Path $out "geom_prj50998.json") -TimeoutSeg 600
$env:PRJ_CAMADAS = "^(F1-HZ-EIXO|0)$"; $env:PRJ_BLOCOS = ""
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg (Join-Path $base "Alça Viaria 07 11 2026r1 Infra.dwg") -Comando "PRJGEOM" -Saida (Join-Path $out "geom_r1.json") -TimeoutSeg 600
"FIM"
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg (Join-Path $base "Alça Viaria 07 11 2026r1 Infra.dwg") -Comando "PRJALIN" -Saida (Join-Path $out "alin_r1.json") -TimeoutSeg 600
