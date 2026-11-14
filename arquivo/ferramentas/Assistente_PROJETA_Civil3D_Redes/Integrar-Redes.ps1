# Integra a aba REDES — IFM (esgoto NBR 9649, escavação e quantitativos, renomear e associar)
# ao projeto principal: copia os 5 arquivos novos e faz as inserções pequenas em Commands.cs,
# Ribbon.cs e Support.cs. Guarda cópia de segurança antes. Se algum ponto de inserção não for
# achado (o arquivo mudou demais), para sem alterar nada. Rodar de novo não duplica nada.
#   powershell -ExecutionPolicy Bypass -File Integrar-Redes.ps1 [-Destino <pasta>] [-Previa <nome>]
# Com -Previa, compila em seguida (bin\<nome>\2026 e 2027). Não instala.
param(
    [string]$Destino = (Join-Path (Split-Path $PSScriptRoot -Parent) 'Assistente_PROJETA_Civil3D'),
    [string]$Previa = ''
)
$ErrorActionPreference = 'Stop'
$origem = $PSScriptRoot
if (!(Test-Path -LiteralPath (Join-Path $Destino 'Projeta.Civil3D.csproj'))) { throw "Projeto principal não encontrado em $Destino" }
if ((Resolve-Path $Destino).Path -eq (Resolve-Path $origem).Path) { throw 'O destino é a própria pasta Redes.' }
$novos = 'NetworkCore.cs', 'NetworkReader.cs', 'NetworkOps.cs', 'NetworksPage.cs', 'Xlsx.cs', 'REDES-IFM.md'
$utf8 = New-Object Text.UTF8Encoding $false

function Ler([string]$nome) {
    $p = Join-Path $Destino $nome
    $b = [IO.File]::ReadAllBytes($p)
    $bom = $b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF
    $ini = if ($bom) { 3 } else { 0 }
    $t = $utf8.GetString($b, $ini, $b.Length - $ini)
    [pscustomobject]@{ Nome = $nome; Caminho = $p; Bom = $bom; Texto = $t; Original = $t }
}

# Insere só se ainda não houver $jaTem (regex). O ponto $padrao precisa aparecer exatamente uma vez.
function Inserir($arq, [string]$jaTem, [string]$padrao, [string]$troca, [string]$oque) {
    $op = [Text.RegularExpressions.RegexOptions]::Multiline
    if ([regex]::IsMatch($arq.Texto, $jaTem, $op)) { return }
    $n = [regex]::Matches($arq.Texto, $padrao, $op).Count
    if ($n -ne 1) { throw "$($arq.Nome): ponto para $oque achado $n vez(es), esperado 1. Nada foi alterado." }
    $arq.Texto = [regex]::Replace($arq.Texto, $padrao, $troca, $op)
}

foreach ($f in $novos) { if (!(Test-Path -LiteralPath (Join-Path $origem $f))) { throw "Falta $f na pasta Redes." } }
$nl = "`n"
$cmd = Ler 'Commands.cs'
Inserir $cmd 'NetworksPage\? networksPage;' '^([ \t]*)private static TraversesPage\? traversesPage;' `
    ('$0' + $nl + '${1}private static NetworksPage? networksPage;' + $nl + '${1}internal static NetworksPage? NetworksPage => networksPage;') 'declarar a aba'
Inserir $cmd 'networksPage[^\r\n]*\}\.OfType<ToolPage>\(\)' '(new ToolPage\?\[\] \{[^\r\n}]*?)(\s*\}\.OfType<ToolPage>\(\))' `
    '${1}, networksPage${2}' 'a lista de abas'
Inserir $cmd 'networksPage = new NetworksPage\(\);' '^([ \t]*)traversesPage = new TraversesPage\(\);' `
    ('$0' + $nl + '${1}networksPage = new NetworksPage();') 'criar a aba'
Inserir $cmd '\("Redes — IFM", networksPage\)' '(main = new MainPanel\([^\r\n]*)\)\);' `
    '${1}), ("Redes — IFM", networksPage));' 'o painel principal'
Inserir $cmd '"redes" => networksPage' '([ \t,])_ => surfacesPage' `
    '${1}"redes" => networksPage, _ => surfacesPage' 'o atalho da faixa de opções'

$rib = Ler 'Ribbon.cs'
Inserir $rib 'Icons\.Network' '^([ \t]*)source\.Items\.Add\(Button\("POLIGONAL — IFM"[^\r\n]*' `
    ('$0' + $nl + '${1}source.Items.Add(Button("REDES — IFM", "redes", Icons.Network, "Esgoto (NBR 9649), escavação e quantitativos, renomear e associar redes de tubulação."));') 'o botão da faixa de opções'
Inserir $rib 'const string Network =' '^([ \t]*)public const string Section = [^\r\n]*' `
    ('$0' + $nl + '${1}public const string Network = "M3,9 L13,9 M19,9 L29,9 M16,12 L16,20 M3,23 L13,23 M19,23 L29,23 M13,6 L19,6 L19,12 L13,12 Z M13,20 L19,20 L19,26 L13,26 Z";') 'o ícone'

# Roda do mouse sobre campo numérico: rola a aba em vez de mudar o valor (vale para todas as abas).
$sup = Ler 'Support.cs'
Inserir $sup 'class BrNumber[\s\S]*OnMouseWheel' '(internal sealed class BrNumber : NumericUpDown[\s\S]*?UpdateEditText\(\);\r?\n[ \t]*\})' `
    ('${1}' + $nl + $nl + '    // A roda do mouse rola a aba e não muda o valor (antes, rolar por cima de um campo alterava o parâmetro sem aviso).' + $nl +
     '    // Sem chamar a base e sem marcar como tratado, o Windows repassa a rolagem ao painel de cima.' + $nl +
     '    protected override void OnMouseWheel(MouseEventArgs e) { }') 'a roda do mouse nos campos numéricos'

$alterados = @($cmd, $rib, $sup) | Where-Object { $_.Texto -ne $_.Original }
$copiar = $novos | Where-Object {
    $d = Join-Path $Destino $_
    !(Test-Path -LiteralPath $d) -or (Get-FileHash -LiteralPath $d).Hash -ne (Get-FileHash -LiteralPath (Join-Path $origem $_)).Hash
}
if (!$alterados -and !$copiar) { Write-Host 'Nada a fazer: a aba Redes já está integrada e igual.' }
else {
    $backup = Join-Path $Destino ('_versoes antigas\Antes-Redes-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Force -Path $backup | Out-Null
    foreach ($a in $alterados) { Copy-Item -LiteralPath $a.Caminho -Destination $backup }
    foreach ($f in $copiar) { $d = Join-Path $Destino $f; if (Test-Path -LiteralPath $d) { Copy-Item -LiteralPath $d -Destination $backup } }
    foreach ($f in $copiar) { Copy-Item -LiteralPath (Join-Path $origem $f) -Destination (Join-Path $Destino $f) -Force; Write-Host "copiado: $f" }
    foreach ($a in $alterados) { [IO.File]::WriteAllText($a.Caminho, $a.Texto, (New-Object Text.UTF8Encoding $a.Bom)); Write-Host "alterado: $($a.Nome)" }
    Write-Host "Cópia de segurança: $backup"
}
if ($Previa) { & (Join-Path $Destino 'Compilar.ps1') -Previa $Previa }
