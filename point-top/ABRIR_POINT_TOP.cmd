@echo off
cd /d "%~dp0"
where node >nul 2>&1
if errorlevel 1 (
 echo Node.js nao encontrado. Instale Node.js para abrir o POINT TOP integrado.
 pause
 exit /b 1
)
powershell.exe -NoProfile -WindowStyle Hidden -Command "Start-Process -FilePath (Get-Command node.exe).Source -ArgumentList 'server.cjs','--open' -WorkingDirectory $pwd.Path -WindowStyle Hidden"
