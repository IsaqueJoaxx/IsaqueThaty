param([switch]$SemCompilar)
$ErrorActionPreference='Stop'
if (!$SemCompilar) {
 & dotnet build (Join-Path $PSScriptRoot 'Civil\Terraplenagem.Civil.csproj') -c Release -m:1 -nr:false -p:UseSharedCompilation=false -v minimal
 if ($LASTEXITCODE -ne 0) { throw 'Compilação falhou. Instalação não alterada.' }
}
$bundleIFM=Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\Terraplenagem.IFM.bundle'
foreach ($yearIFM in @('2026','2027')) {
 $tfmIFM=if($yearIFM -eq '2026'){'net8.0-windows'}else{'net10.0-windows'}
 $sourceIFM=Join-Path $PSScriptRoot "Civil\bin\Release\$tfmIFM"
 foreach($fileIFM in @('Terraplenagem.IFM.Civil.dll','Terraplenagem.IFM.Core.dll')) {
  if(!(Test-Path -LiteralPath (Join-Path $sourceIFM $fileIFM))){throw "Arquivo ausente: $sourceIFM\$fileIFM"}
 }
}
foreach ($yearIFM in @('2026','2027')) {
 $tfmIFM=if($yearIFM -eq '2026'){'net8.0-windows'}else{'net10.0-windows'}
 $sourceIFM=Join-Path $PSScriptRoot "Civil\bin\Release\$tfmIFM"
 $destinationIFM=Join-Path $bundleIFM "Contents\$yearIFM"
 New-Item -ItemType Directory -Force -Path $destinationIFM | Out-Null
 foreach($fileIFM in @('Terraplenagem.IFM.Civil.dll','Terraplenagem.IFM.Core.dll','Terraplenagem.IFM.Civil.deps.json','Terraplenagem.IFM.Civil.runtimeconfig.json')) {
  $pathIFM=Join-Path $sourceIFM $fileIFM
  if(Test-Path -LiteralPath $pathIFM){Copy-Item -LiteralPath $pathIFM -Destination (Join-Path $destinationIFM $fileIFM) -Force}
 }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PackageContents.xml') -Destination (Join-Path $bundleIFM 'PackageContents.xml') -Force
Write-Output "Instalado: $bundleIFM. Reabra o Civil 3D e use PRJTERRA."
