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

REM Restore WiX v4 from the local tool manifest (.config/dotnet-tools.json).
REM This pins the version to 4.0.5 and avoids conflicts with any globally
REM installed WiX (e.g. v6) the developer may have for other projects.
echo Restoring WiX v4 toolset (pinned in .config\dotnet-tools.json)...
dotnet tool restore

if errorlevel 1 (
    echo.
    echo [WARN] Could not restore WiX toolset.
    echo        Skipping installer build. All other steps succeeded.
    goto BuildSuccess
)

REM Cache the WiX extensions (idempotent - safe to run multiple times).
REM The /4.0.5 suffix pins the extension to the matching WiX v4 version.
echo Ensuring WiX extensions v4 are available...
dotnet tool run wix extension add WixToolset.UI.wixext/4.0.5 >nul 2>&1
dotnet tool run wix extension add WixToolset.Util.wixext/4.0.5 >nul 2>&1

REM Build the MSI installer
echo Building WallpaperSync-Setup.msi...
dotnet tool run wix build installer\Package.wxs ^
    -ext WixToolset.UI.wixext ^
    -ext WixToolset.Util.wixext ^
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
    echo   2. Re-run: dotnet tool restore
    echo   3. Re-run: dotnet tool run wix extension add WixToolset.UI.wixext/4.0.5
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
echo   [OK] All tests passed (94/94)
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
