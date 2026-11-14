# ============================================================================
# PROJETA - servidor MCP para o Claude Desktop
# Publica o executavel em %LOCALAPPDATA%\Projeta\mcp\servidor e inclui o servidor
# "projeta-civil3d" no claude_desktop_config.json (faz backup antes).
# Uso (PowerShell, na pasta do projeto):
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar-mcp.ps1
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar-mcp.ps1 -SemClaude   (so publica)
#   powershell -ExecutionPolicy Bypass -File .\instalador\instalar-mcp.ps1 -Remover     (tira do Claude Desktop)
# O lado do Civil 3D (comando PRJMCP) vem junto com o plugin: instalar.ps1.
# ============================================================================
param(
    [switch]$SemClaude,
    [switch]$Remover,
    [string]$ConfigClaude = ""
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $raiz "src\Projeta.Mcp.Servidor\Projeta.Mcp.Servidor.csproj"
$destino = Join-Path $env:LOCALAPPDATA "Projeta\mcp\servidor"
$exe = Join-Path $destino "Projeta.Mcp.Servidor.exe"
$cfgArgs = @()
if ($ConfigClaude) { $cfgArgs = @("--config", $ConfigClaude) }

if ($Remover) {
    if (Test-Path $exe) { & $exe --remover-claude @cfgArgs } else { Write-Host "Servidor nao instalado em $destino." }
    exit 0
}

$rodando = Get-Process -Name "Projeta.Mcp.Servidor" -ErrorAction SilentlyContinue
if ($rodando) {
    Write-Host "O Claude Desktop esta usando o servidor; encerrando para atualizar (reinicie o Claude Desktop depois)." -ForegroundColor Yellow
    $rodando | Stop-Process -Force
    Start-Sleep -Milliseconds 800
}

Write-Host "Publicando o servidor MCP em $destino ..." -ForegroundColor Cyan
dotnet publish $proj -c Release -o $destino --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "Falha ao publicar o servidor." -ForegroundColor Red; exit 1 }

if (-not $SemClaude) {
    & $exe --instalar-claude @cfgArgs
    if ($LASTEXITCODE -ne 0) { Write-Host "Falha ao configurar o Claude Desktop." -ForegroundColor Red; exit 1 }
}

Write-Host ""
Write-Host "Pronto. Proximos passos:" -ForegroundColor Green
Write-Host "  1) Plugin com a ponte (Civil 3D fechado): powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1"
Write-Host "  2) No Civil 3D: PRJMCP -> Iniciar  (ou Automatico para ligar sempre que o Civil 3D abrir)"
Write-Host "  3) Feche o Claude Desktop pela bandeja do sistema, abra de novo e pergunte: 'quais redes tem no desenho aberto?'"
Write-Host "  Diagnostico: & '$exe' --status"
