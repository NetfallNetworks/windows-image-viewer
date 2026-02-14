@echo off
echo ========================================
echo Building WallpaperApp...
echo ========================================
echo.

cd /d "%~dp0\..\src\WallpaperApp"
dotnet build -c Release --warnaserror

if errorlevel 1 (
    echo.
    echo [31mBuild failed![0m
    exit /b 1
) else (
    echo.
    echo [32mBuild successful![0m
    exit /b 0
)
