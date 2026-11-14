@echo off
cd /d "%~dp0"
set "IFM_PYTHON=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
if exist "%IFM_PYTHON%" (
  "%IFM_PYTHON%" nocturne_ifm.py
) else (
  py -3 nocturne_ifm.py
)
if errorlevel 1 (
  echo Nao foi possivel iniciar. Consulte LEIA-ME.md.
  pause
)
