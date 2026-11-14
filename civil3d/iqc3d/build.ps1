# Compila o IQC3D para o Civil 3D 2026 e 2027 e mostra so os erros.
param([switch]$Todos)
$R = $PSScriptRoot
$ok = $true
foreach ($v in 2026, 2027) {
  if (-not $Todos -and $v -eq 2027) { continue }
  $out = Join-Path $R "build\$v"
  $log = & dotnet build (Join-Path $R 'src\IQC3D.csproj') -t:Rebuild -c Release -o $out -v q -nologo "-p:AcadDir=C:\Program Files\Autodesk\AutoCAD $v" 2>&1
  $erros = $log | Select-String -Pattern ' error [A-Z]+\d+' | ForEach-Object { ($_.Line -replace '^.*\\src\\', '' -replace '\s*\[C:.*$', '') } | Select-Object -Unique
  if ($LASTEXITCODE -ne 0) { $ok = $false; "== $v : FALHOU"; $erros | Select-Object -First 40 } else { "== $v : OK -> $out\IQC3D.dll" }
}
if (-not $ok) { exit 1 }
