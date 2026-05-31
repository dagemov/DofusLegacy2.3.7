@echo off
setlocal EnableExtensions

cd /d "%~dp0"

set "API_DIR=%~dp0OneLauncher.Api"
set "LAUNCHER_DIR=%~dp0OneLauncher-main"
set "API_URL=http://localhost:5074/api/launcher/manifest"

echo.
echo ========================================
echo   OneLauncher - entorno de desarrollo
echo ========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] No se encontro "dotnet" en el PATH.
  echo Instala el SDK de .NET 8 o agrega dotnet al PATH.
  pause
  exit /b 1
)

where npm >nul 2>&1
if errorlevel 1 (
  echo [ERROR] No se encontro "npm" en el PATH.
  echo Instala Node.js o agrega npm al PATH.
  pause
  exit /b 1
)

if not exist "%API_DIR%\OneLauncher.Api.csproj" (
  echo [ERROR] No se encontro el proyecto API en:
  echo   %API_DIR%
  pause
  exit /b 1
)

if not exist "%LAUNCHER_DIR%\package.json" (
  echo [ERROR] No se encontro el launcher Electron en:
  echo   %LAUNCHER_DIR%
  pause
  exit /b 1
)

echo [1/2] Iniciando OneLauncher.Api en http://localhost:5074 ...
start "OneLauncher.Api" cmd /k "cd /d "%API_DIR%" && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --launch-profile http"

echo Esperando a que la API responda en %API_URL% ...
set /a ATTEMPTS=0

:WAIT_API
set /a ATTEMPTS+=1
powershell -NoProfile -Command "try { Invoke-WebRequest -Uri '%API_URL%' -UseBasicParsing -TimeoutSec 2 | Out-Null; exit 0 } catch { exit 1 }" >nul 2>&1
if not errorlevel 1 goto API_READY

if %ATTEMPTS% GEQ 60 (
  echo [ERROR] La API no respondio despues de 2 minutos.
  echo Revisa la ventana "OneLauncher.Api" para ver el error.
  pause
  exit /b 1
)

timeout /t 2 /nobreak >nul
goto WAIT_API

:API_READY
echo API lista.
echo.
echo [2/2] Iniciando Electron ...
cd /d "%LAUNCHER_DIR%"
call npm start

endlocal
