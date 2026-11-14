@echo off
rem Arraste o DXF/DWG das ruas sobre este arquivo (ou de dois cliques para escolher)
cd /d "%~dp0"
py -3.10 "%~dp0folha_pavimentacao.py" %*
if errorlevel 1 pause
