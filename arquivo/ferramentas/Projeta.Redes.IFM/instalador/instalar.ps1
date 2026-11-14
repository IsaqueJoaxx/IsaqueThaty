# ============================================================================
# PROJETA - Assistente Civil - compila e instala como bundle do Civil 3D
# Uso (PowerShell, na pasta do projeto):
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1            (todas as versões encontradas)
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1 -Versoes 2026
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1 -SemInstalar   (só compila)
# Civil 3D 2026 = .NET 8 ; Civil 3D 2027 = .NET 10 (precisa do SDK do .NET 10).
# Depois de instalado, o plugin carrega sozinho ao abrir o Civil 3D.
# ============================================================================
param(
    [string[]]$Versoes = @("2026", "2027"),
    [string]$PastaAutodesk = "C:\Program Files\Autodesk",
    [switch]$SemInstalar
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $raiz "src\Projeta.Civil3D\Projeta.Civil3D.csproj"
$bundle = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\Projeta.bundle"
$compiladas = @()

foreach ($v in $Versoes) {
    $acad = Join-Path $PastaAutodesk "AutoCAD $v"
    if (-not (Test-Path "$acad\C3D\AeccDbMgd.dll")) { Write-Host "Civil 3D $v não encontrado em $acad - pulando." -ForegroundColor Yellow; continue }
    $tfm = if ($v -eq "2027") { "net10.0-windows" } else { "net8.0-windows" }
    Write-Host "Compilando para Civil 3D $v ($tfm)..." -ForegroundColor Cyan
    dotnet build $proj -c Release "-p:CivilVersao=$v" "-p:AcadDir=$acad"
    if ($LASTEXITCODE -ne 0) { Write-Host "Falha na compilação para $v." -ForegroundColor Red; exit 1 }
    $saida = Join-Path $raiz "src\Projeta.Civil3D\bin\Release\$tfm"
    Write-Host "OK: $saida\Projeta.Redes.IFM.dll" -ForegroundColor Green
    $compiladas += ,@($v, $saida)
}
if ($compiladas.Count -eq 0) { Write-Host "Nenhuma versão compilada." -ForegroundColor Red; exit 1 }
if ($SemInstalar) { Write-Host "Para testar sem instalar: no Civil 3D, NETLOAD na DLL acima."; exit 0 }

New-Item -ItemType Directory -Force -Path $bundle | Out-Null
Copy-Item (Join-Path $PSScriptRoot "PackageContents.xml") $bundle -Force
foreach ($c in $compiladas) {
    $dest = Join-Path $bundle ("Contents\" + $c[0])
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Copy-Item (Join-Path $c[1] "*.dll"), (Join-Path $c[1] "*.json") $dest -Force -ErrorAction SilentlyContinue
}

# biblioteca do usuário (dispositivos/seções de exemplo)
$bib = Join-Path $env:APPDATA "Projeta\biblioteca"
New-Item -ItemType Directory -Force -Path (Join-Path $bib "dispositivos"), (Join-Path $bib "secoes") | Out-Null
$exDisp = Join-Path $raiz "biblioteca\dispositivos"
if (Test-Path $exDisp) { Copy-Item "$exDisp\*.json" (Join-Path $bib "dispositivos") -Force }
$exSec = Join-Path $raiz "biblioteca\secoes"
if (Test-Path $exSec) { Copy-Item "$exSec\*.json" (Join-Path $bib "secoes") -Force }

Write-Host "Instalado em $bundle" -ForegroundColor Green
Write-Host "Abra o Civil 3D e digite PRJPAINEL (painel) ou PRJAJUDA (lista de comandos)."
