# Instala (ou atualiza) o IQ C3D como bundle do Civil 3D 2026 e 2027.
# Cada build ganha um nome de arquivo proprio (IQC3D_aaaammdd_hhmmss.dll), assim da para
# atualizar mesmo com o Civil 3D aberto: a versao nova carrega na proxima vez que ele abrir.
$R = $PSScriptRoot
$dll = Join-Path $R 'build\2026\IQC3D.dll'
if (-not (Test-Path $dll)) { throw "Compile primeiro (build.ps1)." }
$B = 'C:\ProgramData\Autodesk\ApplicationPlugins\IQC3D.bundle'
$carimbo = (Get-Item $dll).LastWriteTime.ToString('yyyyMMdd_HHmmss')
$nome = "IQC3D_$carimbo.dll"
foreach ($v in 2026, 2027) {
  $d = Join-Path $B "Contents\$v"
  New-Item -ItemType Directory -Force $d | Out-Null
  # Civil 3D 2027 roda em .NET 10, que carrega a DLL .NET 8 normalmente
  Copy-Item $dll (Join-Path $d $nome) -Force
  # limpa versoes antigas que nao estejam em uso
  Get-ChildItem $d -Filter 'IQC3D*.dll' | Where-Object { $_.Name -ne $nome } | ForEach-Object {
    $velho = $_.Name; try { Remove-Item $_.FullName -ErrorAction Stop } catch { "  (em uso pelo Civil 3D aberto, sai na proxima instalacao: $velho)" }
  }
}
$xml = @'
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0" AutodeskProduct="AutoCAD" ProductType="Application"
  Name="IQ C3D" AppVersion="0.1.0" Description="Ferramentas de Civil 3D do Isaque" Author="Isaque"
  ProductCode="{6B1E9C52-3F4A-4E0B-9A77-1C2D3E4F5A60}" UpgradeCode="{8C2F0D63-4A5B-4F1C-8B88-2D3E4F5A6B71}">
  <CompanyDetails Name="Isaque" />
  <Components Description="Civil 3D 2026">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="IQC3D" Version="0.1.0" ModuleName="./Contents/2026/ARQ" AppDescription="IQ C3D" LoadOnAutoCADStartup="True" />
  </Components>
  <Components Description="Civil 3D 2027">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R26.0" SeriesMax="R26.0" />
    <ComponentEntry AppName="IQC3D" Version="0.1.0" ModuleName="./Contents/2027/ARQ" AppDescription="IQ C3D" LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
'@
$xml.Replace('ARQ', $nome) | Set-Content -Encoding UTF8 (Join-Path $B 'PackageContents.xml')
"Instalado: $nome em $B"
