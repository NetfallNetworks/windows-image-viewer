@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "LOG_FILE=%REPO_ROOT%\publish-output.log"

echo ======================================== > "%LOG_FILE%"
echo Publishing self-contained executable... >> "%LOG_FILE%"
echo Started at %date% %time% >> "%LOG_FILE%"
echo ======================================== >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

echo ========================================
echo Publishing self-contained executable...
echo ========================================
echo.
echo Working directory: %REPO_ROOT%\src\WallpaperApp
echo Output will be saved to: publish-output.log
echo.

cd /d "%REPO_ROOT%\src\WallpaperApp"

if exist publish (
    echo Cleaning previous publish directory...
    echo Cleaning previous publish directory... >> "%LOG_FILE%"
    rmdir /s /q publish >> "%LOG_FILE%" 2>&1
)

dotnet publish -c Release -r win-x64 --self-contained true -o ./publish >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
    echo.
    echo ERROR: Publish failed!
    echo.
    echo Full output saved to: %LOG_FILE%
    pause
    exit /b 1
) else (
    echo.
    echo SUCCESS: Publish successful!
    echo.
    echo Output location: src\WallpaperApp\publish\
    echo Executable: src\WallpaperApp\publish\WallpaperApp.exe
    echo.
    echo Full output saved to: %LOG_FILE%
    pause
    exit /b 0
)
