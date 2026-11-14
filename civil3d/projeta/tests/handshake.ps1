# Teste de ponta a ponta do servidor MCP por stdio, como um cliente genérico faria.
param([string]$Exe = "C:\Users\User\Projects\Projeta.Civil3D\src\Projeta.Mcp.Servidor\bin\Release\net8.0\Projeta.Mcp.Servidor.exe")
$ErrorActionPreference = "Stop"
$linhas = @(
  '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"teste-generico","version":"1.0"}}}'
  '{"jsonrpc":"2.0","method":"notifications/initialized"}'
  '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
  '{"jsonrpc":"2.0","id":3,"method":"prompts/list"}'
  '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"desenho_status","arguments":{}}}'
)
$psi = New-Object Diagnostics.ProcessStartInfo
$psi.FileName = $Exe
$psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
if ($psi.PSObject.Properties.Name -contains 'StandardInputEncoding') { $psi.StandardInputEncoding = [Text.UTF8Encoding]::new($false) }
$p = [Diagnostics.Process]::Start($psi)
foreach ($l in $linhas) { $p.StandardInput.WriteLine($l) }
$p.StandardInput.Flush()
$saida = @()
$fim = (Get-Date).AddSeconds(40)
while ((Get-Date) -lt $fim -and $saida.Count -lt $linhas.Count - 1) {
  $l = $p.StandardOutput.ReadLine()
  if ($null -eq $l) { break }
  if ($l.Trim()) { $saida += $l }
}
$p.StandardInput.Close(); Start-Sleep -Milliseconds 300
if (-not $p.HasExited) { $p.Kill() }

$r = $saida | ForEach-Object { $_ | ConvertFrom-Json }
function Por($id) { $r | Where-Object { $_.id -eq $id } | Select-Object -First 1 }

$init = (Por 1).result
"protocolo          : $($init.protocolVersion)"
"servidor           : $($init.serverInfo.name) $($init.serverInfo.version) - $($init.serverInfo.title)"
"capacidades        : $($init.capabilities.PSObject.Properties.Name -join ', ')"
"instructions       : $($init.instructions.Length) caracteres"
$tools = (Por 2).result.tools
"ferramentas        : $($tools.Count)"
"  com outputSchema : $(@($tools | Where-Object { $_.outputSchema }).Count) -> $((@($tools | Where-Object { $_.outputSchema }).name) -join ', ')"
"  com annotations  : $(@($tools | Where-Object { $_.annotations }).Count)"
"  so leitura       : $(@($tools | Where-Object { $_.annotations.readOnlyHint }).Count)"
$prompts = (Por 3).result.prompts
"prompts            : $($prompts.Count) -> $($prompts.name -join ', ')"
$call = (Por 4).result
"tools/call desenho_status"
"  isError          : $($call.isError)"
"  blocos de content: $($call.content.Count)"
"  structuredContent: $(if ($call.structuredContent) { 'sim — campos: ' + ($call.structuredContent.PSObject.Properties.Name -join ', ') } else { 'NAO' })"
"  resumo           : $($call.structuredContent.resumo)"
