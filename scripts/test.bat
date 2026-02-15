@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."

echo ========================================
echo Running automated tests...
echo ========================================
echo.

cd /d "%REPO_ROOT%\src"

REM Run tests with minimal verbosity for cleaner output
dotnet test WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Tests failed!
    echo ========================================
    pause
    exit /b 1
) else (
    echo.
    echo ========================================
    echo SUCCESS: All tests passed!
    echo ========================================
    pause
    exit /b 0
)
