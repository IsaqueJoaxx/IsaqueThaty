# Copia a compilação nova para o bundle mesmo com o Civil 3D aberto: arquivo travado é renomeado (*.antigo-<hora>) e o novo entra no lugar.
# O Civil 3D aberto continua com a versão antiga na memória; a nova carrega na próxima vez que ele abrir.
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..\projeta")).Path
$bundle = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\Projeta.bundle\Contents"
$ts = Get-Date -Format "yyyyMMdd-HHmmss"
foreach ($v in @(@("2026", "net8.0-windows"), @("2027", "net10.0-windows"))) {
    $src = Join-Path $raiz "src\Projeta.Civil3D\bin\Release\$($v[1])"
    $dest = Join-Path $bundle $v[0]
    $novos = 0; $renomeados = 0
    foreach ($f in Get-ChildItem $src -File | Where-Object { $_.Extension -in ".dll", ".json" }) {
        $alvo = Join-Path $dest $f.Name
        try { Copy-Item $f.FullName $alvo -Force -ErrorAction Stop; $novos++ }
        catch {
            Rename-Item $alvo ($f.Name + ".antigo-" + $ts) -ErrorAction Stop
            Copy-Item $f.FullName $alvo -Force -ErrorAction Stop
            $novos++; $renomeados++
        }
        if ((Get-FileHash $f.FullName).Hash -ne (Get-FileHash $alvo).Hash) { throw "Arquivo diferente depois de copiar: $alvo" }
    }
    "Civil 3D $($v[0]): $novos arquivo(s) instalados, $renomeados estavam em uso (renomeados para .antigo-$ts)"
}
