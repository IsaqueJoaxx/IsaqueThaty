$t = Join-Path $env:TEMP "prj_alca"; New-Item -ItemType Directory -Force $t | Out-Null
$tpl = Get-ChildItem "$env:LOCALAPPDATA\Autodesk\C3D 2027" -Recurse -Filter "*Civil 3D (Metric) NCS.dwt" | Select-Object -First 1 -ExpandProperty FullName
"template: $tpl"
$teste = Join-Path $t "teste_sub.dwg"; Copy-Item $tpl $teste -Force
& (Join-Path $PSScriptRoot "rodar.ps1") -Dwg $teste -Copia $teste -Comando "PRJSUBPARAMS" -Saida "D:\PROJETA ENGENHARIA\Alça Viaria\_reconstrucao_c3d\leitura\sub_params.json" -TimeoutSeg 300
