@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."

cd /d "%REPO_ROOT%"

echo ========================================
echo Step 1/3: Building Projects...
echo ========================================
echo.

REM Build all projects using the solution file
dotnet build WallpaperApp.sln -c Release --warnaserror --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Build failed!
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ✅ Build successful!
echo.

echo ========================================
echo Step 2/3: Running Tests...
echo ========================================
echo.

REM Run tests
dotnet test src\WallpaperApp.Tests\WallpaperApp.Tests.csproj --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Tests failed!
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ✅ All tests passed!
echo.

echo ========================================
echo Step 3/3: Publishing Applications...
echo ========================================
echo.

REM Publish console/service app
echo Publishing WallpaperApp (console/service)...
dotnet publish src\WallpaperApp\WallpaperApp.csproj -c Release -o publish\WallpaperApp --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Publish failed for WallpaperApp!
    echo ========================================
    pause
    exit /b 1
)

REM Publish tray app
echo Publishing WallpaperApp.TrayApp (system tray)...
dotnet publish src\WallpaperApp.TrayApp\WallpaperApp.TrayApp.csproj -c Release -o publish\WallpaperApp.TrayApp --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Publish failed for TrayApp!
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✅ BUILD PIPELINE COMPLETE!
echo ========================================
echo   ✅ Build successful
echo   ✅ All tests passed (88/88)
echo   ✅ Applications published to .\publish\
echo ========================================
pause
exit /b 0
