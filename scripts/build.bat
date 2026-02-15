@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "LOG_FILE=%REPO_ROOT%\build-output.log"

echo ======================================== > "%LOG_FILE%"
echo Building WallpaperApp... >> "%LOG_FILE%"
echo Started at %date% %time% >> "%LOG_FILE%"
echo ======================================== >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

echo ========================================
echo Building WallpaperApp...
echo ========================================
echo.
echo Working directory: %REPO_ROOT%\src\WallpaperApp
echo Output will be saved to: build-output.log
echo.

cd /d "%REPO_ROOT%\src\WallpaperApp"
dotnet build -c Release --warnaserror >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    echo.
    echo Full output saved to: %LOG_FILE%
    pause
    exit /b 1
) else (
    echo.
    echo SUCCESS: Build successful!
    echo.
    echo Full output saved to: %LOG_FILE%
    pause
    exit /b 0
)
