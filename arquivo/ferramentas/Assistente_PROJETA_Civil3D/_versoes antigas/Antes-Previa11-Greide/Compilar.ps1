# Compila o Assistente PROJETA para Civil 3D 2026 (.NET 8) e Civil 3D 2027 (.NET 10).
# Saída: bin\<Previa>\2026 e bin\<Previa>\2027. Registro: compilacao-<previa>.log
param(
    [string]$Previa = 'Previa08',
    [string]$AutoCAD2026Root = 'C:\Program Files\Autodesk\AutoCAD 2026',
    [string]$AutoCAD2027Root = 'C:\Program Files\Autodesk\AutoCAD 2027'
)
$ErrorActionPreference = 'Stop'
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { throw 'dotnet não encontrado. É necessário o SDK .NET 10 para compilar.' }
$sdks = & $dotnet --list-sdks
if (!($sdks | Where-Object { $_ -match '^10\.' })) { throw 'SDK .NET 10 ausente. O runtime .NET 10 sozinho não compila este projeto. Nenhum componente foi instalado por este script.' }
foreach ($par in @(@('2026', $AutoCAD2026Root), @('2027', $AutoCAD2027Root))) {
    foreach ($relative in @('acmgd.dll','accoremgd.dll','acdbmgd.dll','ACA\AecBaseMgd.dll','C3D\AeccDbMgd.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $par[1] $relative))) { throw "Referência do Civil 3D $($par[0]) ausente: $relative em $($par[1])" }
    }
}
# Versão 2026 (.NET 8): usa os pacotes de referência já instalados pelo SDK 8, sem download.
$packs = Join-Path $env:ProgramFiles 'dotnet\packs'
$net8 = Get-ChildItem (Join-Path $packs 'Microsoft.WindowsDesktop.App.Ref') -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like '8.0.*' -and (Test-Path -LiteralPath (Join-Path $packs "Microsoft.NETCore.App.Ref\$($_.Name)")) } |
    Sort-Object { [version]$_.Name } | Select-Object -Last 1
if (!$net8) { throw 'Pacotes de referência do .NET 8 ausentes. Instale o SDK .NET 8 para compilar a versão do Civil 3D 2026.' }
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:MSBuildEnableWorkloadResolver = 'false'
$log = Join-Path $PSScriptRoot ("compilacao-" + $Previa.ToLowerInvariant() + ".log")
& $dotnet build (Join-Path $PSScriptRoot 'Projeta.Civil3D.csproj') --configuration Release "-p:AutoCAD2026Root=$AutoCAD2026Root" "-p:AutoCAD2027Root=$AutoCAD2027Root" "-p:Net8TargetingPackVersion=$($net8.Name)" --verbosity minimal | Tee-Object -FilePath $log
if ($LASTEXITCODE -ne 0) { throw 'Compilação não concluída. Consulte a mensagem de erro acima.' }
foreach ($par in @(@('2026', 'net8.0-windows'), @('2027', 'net10.0-windows'))) {
    $origem = Join-Path $PSScriptRoot "bin\Release\$($par[1])"
    $destino = Join-Path $PSScriptRoot "bin\$Previa\$($par[0])"
    New-Item -ItemType Directory -Force $destino | Out-Null
    Copy-Item (Join-Path $origem 'Projeta.Civil3D.*') $destino -Force
    "Civil 3D $($par[0]): $destino\Projeta.Civil3D.dll"
}
