# ============================================================================
# PROJETA - compila tudo e gera o instalador
#   powershell -ExecutionPolicy Bypass -File .\build.ps1                (tudo: plugins, testes, POINT TOP, IRI, LISP, instalador)
#   powershell -ExecutionPolicy Bypass -File .\build.ps1 -SemTestes
#   powershell -ExecutionPolicy Bypass -File .\build.ps1 -SemInstalador  (so monta saida\pacote)
# Precisa, nesta maquina: .NET SDK 8 e 10, Civil 3D 2026 e/ou 2027 (as DLLs de referencia vem dele),
# QGIS 3.44 (Python 3.12 portatil do POINT TOP), Node.js e os pacotes Python do POINT TOP (ver point-top\python\requirements.txt).
# Saida: saida\Instalar-PROJETA.exe
# ============================================================================
param(
    [string[]]$Versoes = @("2026", "2027"),
    [switch]$SemTestes,
    [switch]$SemInstalador,
    [string]$Qgis = "C:\Program Files\QGIS 3.44.15",
    [string]$Node = "C:\Program Files\nodejs\node.exe",
    [string]$PacotesPython = "D:\POINT TOP\runtime\packages",
    [string]$IriExe = "C:\Users\User\Projects\IRI_RODOVIARIO\dist"
)
$ErrorActionPreference = "Stop"
$R = $PSScriptRoot
$S = Join-Path $R "saida"
$P = Join-Path $S "pacote"
$inicio = Get-Date
function Passo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }
function Dn { & dotnet @args; if ($LASTEXITCODE -ne 0) { throw "Falhou: dotnet $($args -join ' ')" } }
function Copiar($de, $para, [string[]]$padroes = @("*")) {
    New-Item -ItemType Directory -Force $para | Out-Null
    foreach ($p in $padroes) { Get-ChildItem -Path $de -Filter $p -File -ErrorAction SilentlyContinue | Copy-Item -Destination $para -Force }
}

if (Test-Path $P) { Remove-Item $P -Recurse -Force }
New-Item -ItemType Directory -Force $P | Out-Null
$tfm = @{ "2026" = "net8.0-windows"; "2027" = "net10.0-windows" }
$acad = [ordered]@{}
foreach ($v in $Versoes) {
    $d = "C:\Program Files\Autodesk\AutoCAD $v"
    if (Test-Path "$d\C3D\AeccDbMgd.dll") { $acad[$v] = $d } else { Write-Warning "Civil 3D $v nao encontrado em $d; essa versao fica de fora." }
}
if ($acad.Count -eq 0) { throw "Nenhum Civil 3D encontrado: os plugins precisam das DLLs dele para compilar." }
$versoesOk = @($acad.Keys)

# ---------------------------------------------------------------- plugins do Civil 3D
function PluginPorVersao($nome, $csproj, $bundle, $pkg, [string[]]$padroes) {
    Passo "$nome ($($versoesOk -join ', '))"
    foreach ($v in $versoesOk) {
        Dn build (Join-Path $R $csproj) -c Release "-p:CivilVersao=$v" "-p:AcadDir=$($acad[$v])" -nologo "-v:quiet"
        $bin = Join-Path (Split-Path (Join-Path $R $csproj)) "bin\Release\$($tfm[$v])"
        Copiar $bin (Join-Path $P "civil3d\$bundle\Contents\$v") $padroes
    }
    Copy-Item (Join-Path $R $pkg) (Join-Path $P "civil3d\$bundle\PackageContents.xml") -Force
}
PluginPorVersao "PROJETA" "civil3d\projeta\src\Projeta.Civil3D\Projeta.Civil3D.csproj" "Projeta.bundle" "civil3d\projeta\instalador\PackageContents.xml" @("*.dll", "*.json")
PluginPorVersao "PROJETA Giro" "civil3d\giro\src\Projeta.Giro\Projeta.Giro.csproj" "ProjetaGiro.bundle" "civil3d\giro\instalador\PackageContents.xml" @("*.dll", "*.json")
PluginPorVersao "PROJETA Sinalizacao" "civil3d\sinalizacao\src\Projeta.Sinalizacao\Projeta.Sinalizacao.csproj" "ProjetaSinalizacao.bundle" "civil3d\sinalizacao\instalador\PackageContents.xml" @("*.dll", "*.json")

Passo "Terraplenagem IFM"
Dn build (Join-Path $R "civil3d\terraplenagem-ifm\Civil\Terraplenagem.Civil.csproj") -c Release "-m:1" "-nr:false" "-p:UseSharedCompilation=false" -nologo "-v:quiet"
foreach ($v in $versoesOk) {
    Copiar (Join-Path $R "civil3d\terraplenagem-ifm\Civil\bin\Release\$($tfm[$v])") (Join-Path $P "civil3d\Terraplenagem.IFM.bundle\Contents\$v") @("Terraplenagem.IFM.*.dll", "Terraplenagem.IFM.*.json")
}
Copy-Item (Join-Path $R "civil3d\terraplenagem-ifm\PackageContents.xml") (Join-Path $P "civil3d\Terraplenagem.IFM.bundle\PackageContents.xml") -Force

Passo "IQ C3D"
# o IQC3D e .NET 8: compila contra o Civil 2026 e a mesma DLL roda no 2027 (.NET 10 carrega DLL .NET 8)
if (-not $acad.Contains("2026")) { throw "O IQ C3D compila contra o Civil 3D 2026, que nao foi encontrado." }
$o = Join-Path $S "obj-iqc3d"
Dn build (Join-Path $R "civil3d\iqc3d\src\IQC3D.csproj") -c Release -o $o "-p:AcadDir=$($acad['2026'])" -nologo "-v:quiet"
foreach ($v in $versoesOk) { Copiar $o (Join-Path $P "civil3d\IQC3D.bundle\Contents\$v") @("IQC3D.dll") }
$xmlIq = @"
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0" AutodeskProduct="AutoCAD" ProductType="Application"
  Name="IQ C3D" AppVersion="0.1.0" Description="Ferramentas de Civil 3D do Isaque" Author="Isaque"
  ProductCode="{6B1E9C52-3F4A-4E0B-9A77-1C2D3E4F5A60}" UpgradeCode="{8C2F0D63-4A5B-4F1C-8B88-2D3E4F5A6B71}">
  <CompanyDetails Name="Isaque" />
  <Components Description="Civil 3D 2026">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="IQC3D" Version="0.1.0" ModuleName="./Contents/2026/IQC3D.dll" AppDescription="IQ C3D" LoadOnAutoCADStartup="True" />
  </Components>
  <Components Description="Civil 3D 2027">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R26.0" SeriesMax="R26.0" />
    <ComponentEntry AppName="IQC3D" Version="0.1.0" ModuleName="./Contents/2027/IQC3D.dll" AppDescription="IQ C3D" LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
"@
Set-Content -Path (Join-Path $P "civil3d\IQC3D.bundle\PackageContents.xml") -Value $xmlIq -Encoding UTF8

Passo "Biblioteca do PROJETA (dispositivos e secoes)"
Copy-Item (Join-Path $R "civil3d\projeta\biblioteca") (Join-Path $P "biblioteca") -Recurse -Force

Passo "Servidor MCP (Claude Desktop)"
Dn publish (Join-Path $R "civil3d\projeta\src\Projeta.Mcp.Servidor\Projeta.Mcp.Servidor.csproj") -c Release -o (Join-Path $P "mcp\servidor") "-p:RollForward=Major" -nologo "-v:quiet"

# ---------------------------------------------------------------- testes
if (-not $SemTestes) {
    Passo "Testes"
    $tmp = Join-Path $S "testes"; New-Item -ItemType Directory -Force $tmp | Out-Null
    foreach ($t in @(@("PROJETA", "civil3d\projeta\tests\Projeta.Core.Testes", $tmp),
                     @("Giro", "civil3d\giro\tests\Projeta.Giro.Testes", ""),
                     @("Sinalizacao", "civil3d\sinalizacao\tests\Projeta.Sinalizacao.Testes", ""),
                     @("Terraplenagem IFM", "civil3d\terraplenagem-ifm\Tests", ""))) {
        $a = @("run", "--project", (Join-Path $R $t[1]), "-c", "Release")
        if ($t[2]) { $a += @("--", $t[2]) }
        $ErrorActionPreference = "Continue"   # no PowerShell 5, texto em stderr de programa vira erro fatal com "Stop"
        $out = & dotnet @a 2>&1
        $ErrorActionPreference = "Stop"
        $ultima = ($out | Where-Object { $_ -match '\S' } | Select-Object -Last 1)
        if ($LASTEXITCODE -ne 0) { $out | Select-Object -Last 15 | Write-Host; throw "Testes do $($t[0]) falharam." }
        Write-Host ("  {0}: ok - {1}" -f $t[0], $ultima)
    }
}

# ---------------------------------------------------------------- POINT TOP (com Node e Python portateis)
Passo "POINT TOP"
$pt = Join-Path $P "point-top"
$src = Join-Path $R "point-top"
foreach ($d in "app", "python", "docs", "exemplos") { Copy-Item (Join-Path $src $d) (Join-Path $pt $d) -Recurse -Force }
Get-ChildItem (Join-Path $pt "exemplos") -Recurse -File | Where-Object { $_.Length -gt 5MB } | Remove-Item -Force
foreach ($f in "server.cjs", "civil-link.cjs", "LEIA-ME.md") { Copy-Item (Join-Path $src $f) $pt -Force }
if (-not (Test-Path $Node)) { throw "Node.js nao encontrado em $Node." }
Copiar (Split-Path $Node) (Join-Path $pt "node") @("node.exe", "LICENSE*")
# Python 3.12 portatil a partir do QGIS: executavel, DLLs de base e a biblioteca padrao (sem os pacotes do QGIS)
$py = Join-Path $pt "python-portatil"
Copiar "$Qgis\bin" $py @("python.exe", "pythonw.exe", "python312.dll", "python3.dll", "vcruntime140*.dll", "msvcp140*.dll",
                       "libssl-3-x64.dll", "libcrypto-3-x64.dll", "libffi-8.dll", "sqlite3.dll", "liblzma.dll", "zlib*.dll", "libbz2.dll", "expat.dll", "tcl86t.dll", "tk86t.dll")
Copy-Item "$Qgis\apps\Python312\DLLs" (Join-Path $py "DLLs") -Recurse -Force
& robocopy "$Qgis\apps\Python312\Lib" (Join-Path $py "Lib") /E /NFL /NDL /NJH /NJS /NP /XD site-packages __pycache__ test tests idlelib tkinter turtledemo ensurepip | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar a biblioteca padrao do Python." }
New-Item -ItemType Directory -Force (Join-Path $py "Lib\site-packages") | Out-Null
if (-not (Test-Path $PacotesPython)) { throw "Pacotes Python do POINT TOP nao encontrados em $PacotesPython (instale com pip install -r point-top\python\requirements.txt --target <pasta>)." }
& robocopy $PacotesPython (Join-Path $pt "runtime\packages") /E /NFL /NDL /NJH /NJS /NP /XD __pycache__ | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar os pacotes Python." }
# confere o Python portatil isolado (sem o PATH da maquina)
$envAntes = $env:PATH
try {
    $env:PATH = "$py;C:\Windows\System32"
    $env:PYTHONHOME = $py; $env:PYTHONPATH = (Join-Path $pt "runtime\packages")
    $v = & (Join-Path $py "python.exe") -c "import sys, ssl, ctypes, sqlite3, lzma, bz2, json, numpy, rasterio; print(sys.version.split()[0], numpy.__version__, rasterio.__version__)" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Python portatil nao funcionou: $v" }
    Write-Host "  Python portatil ok: $v"
} finally { $env:PATH = $envAntes; Remove-Item Env:PYTHONHOME, Env:PYTHONPATH -ErrorAction SilentlyContinue }

# ---------------------------------------------------------------- IRI e LISP
Passo "IRI Rodoviario"
if (-not (Test-Path (Join-Path $IriExe "IRI_RODOVIARIO.exe"))) { throw "IRI_RODOVIARIO.exe nao encontrado em $IriExe." }
Copiar $IriExe (Join-Path $P "iri") @("IRI_RODOVIARIO.exe", "IRI_PARAMETROS.txt")

Passo "LISPs"
Copiar (Join-Path $R "lisp") (Join-Path $P "lisp") @("*.lsp", "*.dcl", "*.md")
Copy-Item (Join-Path $R "lisp\ExportHatchesToKML") (Join-Path $P "lisp\ExportHatchesToKML") -Recurse -Force

# ---------------------------------------------------------------- manifesto e instalador
$ErrorActionPreference = "Continue"
$git = (& git -C $R rev-parse --short HEAD 2>$null)
if ($LASTEXITCODE -ne 0) { $git = "" }
$ErrorActionPreference = "Stop"
@{ versao = (Get-Date -Format "yyyy.MM.dd.HHmm"); gerado = (Get-Date -Format "s"); commit = "$git"; civil = $versoesOk } | ConvertTo-Json | Set-Content (Join-Path $P "manifesto.json") -Encoding UTF8
$t = Get-ChildItem $P -Recurse -File | Measure-Object Length -Sum
Write-Host ("  pacote: {0} arquivos, {1:N0} MB" -f $t.Count, ($t.Sum / 1MB))

if (-not $SemInstalador) {
    Passo "Instalador"
    $zip = Join-Path $S "pacote.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($P, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    $pub = Join-Path $S "obj-instalador"
    Dn publish (Join-Path $R "instalador\Projeta.Instalador\Projeta.Instalador.csproj") -c Release -r win-x64 --self-contained false "-p:PublishSingleFile=true" "-p:Pacote=$zip" -o $pub -nologo "-v:quiet"
    Copy-Item (Join-Path $pub "Instalar-PROJETA.exe") (Join-Path $S "Instalar-PROJETA.exe") -Force
    Write-Host ("  {0} ({1:N0} MB)" -f (Join-Path $S "Instalar-PROJETA.exe"), ((Get-Item (Join-Path $S "Instalar-PROJETA.exe")).Length / 1MB)) -ForegroundColor Green
}
Write-Host ("Pronto em {0:N0} s." -f ((Get-Date) - $inicio).TotalSeconds) -ForegroundColor Green
