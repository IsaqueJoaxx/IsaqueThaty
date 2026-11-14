@echo off
chcp 65001 >nul
title REABILITACAO DE PISTA - IFM
cd /d "%~dp0"
where py >nul 2>&1
if %errorlevel%==0 (set "PYEXE=py") else (set "PYEXE=python")
%PYEXE% "%~dp0gerar_nota_arrastar.py" %*
echo.
pause
