@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."

echo ========================================
echo Building WallpaperApp...
echo ========================================
echo.

cd /d "%REPO_ROOT%\src\WallpaperApp"

REM Build with warnings as errors, minimal verbosity
dotnet build -c Release --warnaserror --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Build failed!
    echo ========================================
    pause
    exit /b 1
) else (
    echo.
    echo ========================================
    echo SUCCESS: Build successful!
    echo ========================================
    pause
    exit /b 0
)
