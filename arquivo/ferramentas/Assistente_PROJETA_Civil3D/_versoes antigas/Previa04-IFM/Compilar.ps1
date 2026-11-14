param([string]$AutoCADRoot = 'C:\Program Files\Autodesk\AutoCAD 2027')
$ErrorActionPreference = 'Stop'
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { throw 'dotnet não encontrado. É necessário o SDK .NET 10 para compilar.' }
$sdks = & $dotnet --list-sdks
if (!($sdks | Where-Object { $_ -match '^10\.' })) { throw 'SDK .NET 10 ausente. O runtime .NET 10 sozinho não compila este projeto. Nenhum componente foi instalado por este script.' }
foreach ($relative in @('acmgd.dll','accoremgd.dll','acdbmgd.dll','ACA\AecBaseMgd.dll','C3D\AeccDbMgd.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $AutoCADRoot $relative))) { throw "Referência ausente: $relative" }
}
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:MSBuildEnableWorkloadResolver = 'false'
& $dotnet build (Join-Path $PSScriptRoot 'Projeta.Civil3D.csproj') --configuration Release "-p:AutoCADRoot=$AutoCADRoot" --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Compilação não concluída. Consulte a mensagem de erro acima.' }
