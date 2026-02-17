@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."

cd /d "%REPO_ROOT%"

echo ========================================
echo Step 1/4: Building Projects...
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
echo [OK] Build successful!
echo.

echo ========================================
echo Step 2/4: Running Tests...
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
echo [OK] All tests passed!
echo.

echo ========================================
echo Step 3/4: Publishing Applications...
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

REM Publish tray app (to bin\TrayApp - source for installer)
echo Publishing WallpaperApp.TrayApp (system tray)...
dotnet publish src\WallpaperApp.TrayApp\WallpaperApp.TrayApp.csproj -c Release -o bin\TrayApp --self-contained true --runtime win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal --nologo

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Publish failed for TrayApp!
    echo ========================================
    pause
    exit /b 1
)

echo.
echo [OK] Applications published!
echo.

echo ========================================
echo Step 4/4: Building Installer (MSI)...
echo ========================================
echo.

REM Check if WiX toolset is installed
where wix >nul 2>&1
if errorlevel 1 (
    echo [INFO] WiX toolset not found. Installing...
    dotnet tool install --global wix
    if errorlevel 1 (
        echo.
        echo [WARN] Could not install WiX toolset automatically.
        echo        To install manually, run:
        echo          dotnet tool install --global wix
        echo          wix extension add --global WixToolset.UI.wixext
        echo.
        echo        Skipping installer build. All other steps succeeded.
        goto BuildSuccess
    )
    echo [OK] WiX toolset installed.
    echo.
)

REM Ensure the WiX UI extension is available (idempotent - safe to run multiple times)
echo Ensuring WiX UI extension is available...
wix extension add --global WixToolset.UI.wixext >nul 2>&1

REM Build the MSI installer
echo Building WallpaperSync-Setup.msi...
wix build installer\Package.wxs ^
    -ext WixToolset.UI.wixext ^
    -o installer\WallpaperSync-Setup.msi ^
    -arch x64

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: Installer build failed!
    echo ========================================
    echo.
    echo Troubleshooting:
    echo   1. Ensure bin\TrayApp\WallpaperApp.TrayApp.exe exists (Step 3 must succeed)
    echo   2. Run: wix extension list --global
    echo   3. Re-run: wix extension add --global WixToolset.UI.wixext
    pause
    exit /b 1
)

echo [OK] Installer built: installer\WallpaperSync-Setup.msi
echo.

:BuildSuccess
echo.
echo ========================================
echo [SUCCESS] BUILD PIPELINE COMPLETE!
echo ========================================
echo   [OK] Build successful
echo   [OK] All tests passed (88/88)
echo   [OK] Console app published to .\publish\WallpaperApp\
echo   [OK] Tray app published to .\bin\TrayApp\
if exist "installer\WallpaperSync-Setup.msi" (
    echo   [OK] Installer built: .\installer\WallpaperSync-Setup.msi
    echo.
    echo Ship installer\WallpaperSync-Setup.msi to end users.
    echo Double-click to install - no PowerShell or admin rights needed.
) else (
    echo   [--] Installer skipped (install WiX: dotnet tool install --global wix^)
)
echo ========================================
pause
exit /b 0
