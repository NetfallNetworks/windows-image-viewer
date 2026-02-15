@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."

echo ========================================
echo Building All Projects...
echo ========================================
echo.

cd /d "%REPO_ROOT%"

REM Build all projects using the solution file
dotnet build WallpaperApp.sln -c Release --warnaserror --verbosity minimal --nologo

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
    echo   - WallpaperApp (console/service)
    echo   - WallpaperApp.TrayApp (system tray)
    echo   - WallpaperApp.Tests
    echo ========================================
    pause
    exit /b 0
)
