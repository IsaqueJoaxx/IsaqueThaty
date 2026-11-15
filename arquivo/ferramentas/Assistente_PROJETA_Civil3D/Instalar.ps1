# Instala (ou atualiza) o Assistente PROJETA como bundle do Civil 3D 2026 e 2027.
# Pasta: C:\Program Files\Autodesk\ApplicationPlugins\Projeta.Civil3D.bundle
# A pasta de Program Files é confiável para o Civil 3D por padrão: não é preciso mexer em
# "Locais confiáveis" nem desligar o SECURELOAD. Por isso o script pede permissão de administrador.
# Cada instalação ganha um nome próprio (Projeta.Civil3D_aaaammdd_hhmmss.dll), para atualizar mesmo
# com o Civil 3D aberto: a versão nova entra na próxima vez que ele abrir.
param([string]$Previa = 'Previa23-ViasPointTop')
$ErrorActionPreference = 'Stop'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) {
    Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -Wait -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Previa', $Previa)
    return
}

$origem = Join-Path $PSScriptRoot "bin\$Previa"
foreach ($v in 2026, 2027) {
    if (-not (Test-Path -LiteralPath (Join-Path $origem "$v\Projeta.Civil3D.dll"))) {
        throw "Compilação $Previa do Civil 3D $v não encontrada em $origem. Rode o Compilar.ps1 primeiro."
    }
}

$bundle = Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins\Projeta.Civil3D.bundle'
foreach ($v in 2026, 2027) {
    if (-not (Test-Path -LiteralPath (Join-Path $origem "$v\TABVOL.lsp"))) {
        throw "TABVOL.lsp ausente no pacote $Previa para $v. Recompile o pacote completo."
    }
}
$carimbo = Get-Date -Format 'yyyyMMdd_HHmmss'
$nome = "Projeta.Civil3D_$carimbo.dll"
foreach ($v in 2026, 2027) {
    $destino = Join-Path $bundle "Contents\$v"
    New-Item -ItemType Directory -Force $destino | Out-Null
    Copy-Item -LiteralPath (Join-Path $origem "$v\Projeta.Civil3D.dll") -Destination (Join-Path $destino $nome) -Force
    Copy-Item -LiteralPath (Join-Path $origem "$v\TABVOL.lsp") -Destination (Join-Path $destino 'TABVOL.lsp') -Force
    # Remove versões anteriores que não estejam em uso pelo Civil 3D aberto.
    Get-ChildItem $destino -Filter 'Projeta.Civil3D_*.dll' | Where-Object { $_.Name -ne $nome } | ForEach-Object {
        $antigo = $_
        try { Remove-Item -LiteralPath $antigo.FullName -ErrorAction Stop }
        catch { "  (em uso pelo Civil 3D aberto; sai na próxima instalação: $($antigo.Name))" }
    }
}

# Carrega na abertura do Civil 3D, para a aba Point top aparecer no ribbon. Ao carregar, o plugin só registra eventos.
$xml = @'
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0" AutodeskProduct="AutoCAD" ProductType="Application"
  Name="Assistente PROJETA" AppVersion="0.21.2" Description="Assistente PROJETA para Civil 3D: superfícies, volumes, seções, corredores, junção, greide, poligonal, redes e IRI rodoviário" Author="PROJETA ENGENHARIA"
  ProductCode="{5E2B7C41-8D3A-4F6E-9B12-7A4C3D2E1F05}" UpgradeCode="{A9C4E2D7-1B3F-4E58-8D6A-2F7B9C0E4D13}">
  <CompanyDetails Name="PROJETA ENGENHARIA" />
  <Components Description="Civil 3D 2026">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="ProjetaCivil3D" Version="0.21.2" ModuleName="./Contents/2026/ARQ" AppDescription="Assistente PROJETA" LoadOnAutoCADStartup="True" />

  </Components>
  <Components Description="Civil 3D 2027">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R26.0" SeriesMax="R26.0" />
    <ComponentEntry AppName="ProjetaCivil3D" Version="0.21.2" ModuleName="./Contents/2027/ARQ" AppDescription="Assistente PROJETA" LoadOnAutoCADStartup="True" />

  </Components>
</ApplicationPackage>
'@
[IO.File]::WriteAllText((Join-Path $bundle 'PackageContents.xml'), $xml.Replace('ARQ', $nome), (New-Object Text.UTF8Encoding $true))
"Instalado: $nome em $bundle"
"Reabra o Civil 3D: a aba Point top aparece no ribbon (ou digite PROJETAASSISTENTE). Não carregue mais as prévias antigas com NETLOAD."
Start-Sleep -Seconds 5




