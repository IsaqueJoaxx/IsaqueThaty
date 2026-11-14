# ============================================================================
# PROJETA - Giro de Veiculos - compila e instala como bundle do Civil 3D
# Uso (PowerShell, na pasta do projeto):
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1              (todas as versoes encontradas)
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1 -Versoes 2027
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1 -SemInstalar  (so compila)
# Civil 3D 2026 = .NET 8 ; Civil 3D 2027 = .NET 10 (precisa do SDK do .NET 10).
# Feche o Civil 3D antes de instalar; ao abrir de novo, a aba PROJETA Giro aparece.
# ============================================================================
param(
    [string[]]$Versoes = @("2026", "2027"),
    [string]$PastaAutodesk = "C:\Program Files\Autodesk",
    [switch]$SemInstalar
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $raiz "src\Projeta.Giro\Projeta.Giro.csproj"
$bundle = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\ProjetaGiro.bundle"
$compiladas = @()

foreach ($v in $Versoes) {
    $acad = Join-Path $PastaAutodesk "AutoCAD $v"
    if (-not (Test-Path "$acad\AcDbMgd.dll")) { Write-Host "AutoCAD/Civil 3D $v nao encontrado em $acad - pulando." -ForegroundColor Yellow; continue }
    $tfm = if ($v -eq "2027") { "net10.0-windows" } else { "net8.0-windows" }
    Write-Host "Compilando para Civil 3D $v ($tfm)..." -ForegroundColor Cyan
    dotnet build $proj -c Release "-p:CivilVersao=$v" "-p:AcadDir=$acad" -nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { Write-Host "Falha na compilacao para $v." -ForegroundColor Red; exit 1 }
    $saida = Join-Path $raiz "src\Projeta.Giro\bin\Release\$tfm"
    Write-Host "OK: $saida\Projeta.Giro.dll" -ForegroundColor Green
    $compiladas += ,@($v, $saida)
}
if ($compiladas.Count -eq 0) { Write-Host "Nenhuma versao compilada." -ForegroundColor Red; exit 1 }
if ($SemInstalar) { Write-Host "Para testar sem instalar: no Civil 3D, NETLOAD na DLL acima."; exit 0 }

# copia as DLLs primeiro e o PackageContents.xml por ultimo (o Civil 3D aberto carrega o bundle novo assim que ele aparece)
try {
    foreach ($c in $compiladas) {
        $dest = Join-Path $bundle ("Contents\" + $c[0])
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item (Join-Path $c[1] "Projeta.Giro*.dll") $dest -Force
        Copy-Item (Join-Path $c[1] "Clipper2Lib.dll") $dest -Force
    }
    Copy-Item (Join-Path $PSScriptRoot "PackageContents.xml") $bundle -Force
} catch {
    Write-Host "Nao foi possivel copiar: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Se o Civil 3D estiver aberto com o plugin carregado, feche-o e rode de novo." -ForegroundColor Red
    exit 2
}
Write-Host "Instalado em $bundle" -ForegroundColor Green
Write-Host "Abra o Civil 3D: aba PROJETA Giro (ou digite GIROAJUDA)."
