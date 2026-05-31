@echo off
setlocal
set "ROOT=%~dp0"

:start
if exist "%ROOT%bin\Debug\net11.0\Sunshine.exe" (
    pushd "%ROOT%bin\Debug\net11.0"
    Sunshine.exe
    popd
) else if exist "%ROOT%bin\Debug\net10.0\Sunshine.exe" (
    pushd "%ROOT%bin\Debug\net10.0"
    Sunshine.exe
    popd
) else (
    echo Build introuvable ou ancien binaire non recompile.
    echo Compile Sunshine.csproj en Debug, puis relance ce fichier.
    pause
    exit /b 1
)
goto start
