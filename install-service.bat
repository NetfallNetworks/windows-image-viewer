@echo off
REM Install Weather Wallpaper Service
REM This script must be run as Administrator

echo ========================================
echo Weather Wallpaper Service - Installation
echo ========================================
echo.

REM Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator
    echo.
    echo Right-click install-service.bat and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Find the executable in standard build locations
set "EXE_PATH="

REM Check for published version first (preferred)
if exist "%~dp0src\WallpaperApp\bin\Release\net8.0-windows\win-x64\publish\WallpaperApp.exe" (
    set "EXE_PATH=%~dp0src\WallpaperApp\bin\Release\net8.0-windows\win-x64\publish\WallpaperApp.exe"
)

REM Check for regular Release build
if "%EXE_PATH%"=="" (
    if exist "%~dp0src\WallpaperApp\bin\Release\net8.0-windows\win-x64\WallpaperApp.exe" (
        set "EXE_PATH=%~dp0src\WallpaperApp\bin\Release\net8.0-windows\win-x64\WallpaperApp.exe"
    )
)

REM Check current directory as fallback
if "%EXE_PATH%"=="" (
    if exist "%~dp0WallpaperApp.exe" (
        set "EXE_PATH=%~dp0WallpaperApp.exe"
    )
)

REM Error if not found
if "%EXE_PATH%"=="" (
    echo ERROR: WallpaperApp.exe not found
    echo.
    echo Please build the project first using:
    echo   dotnet publish src\WallpaperApp\WallpaperApp.csproj -c Release
    echo.
    echo Or place WallpaperApp.exe in the project root
    echo.
    pause
    exit /b 1
)

echo Executable path: %EXE_PATH%
echo.

REM Check if service already exists
echo Checking for existing service...
sc query WeatherWallpaperService >nul 2>&1
if %errorLevel% equ 0 (
    echo.
    echo WARNING: Service "WeatherWallpaperService" already exists!
    echo.
    echo Please uninstall it first using one of these methods:
    echo   1. Run uninstall-service.bat
    echo   2. Open services.msc, find "Weather Wallpaper Service", right-click, Delete
    echo   3. Restart your computer and try again
    echo.
    pause
    exit /b 1
)

REM Create service
echo Installing service...
sc create WeatherWallpaperService binPath= "%EXE_PATH%" DisplayName= "Weather Wallpaper Service" start= auto
if %errorLevel% neq 0 (
    echo ERROR: Failed to create service (error code: %errorLevel%)
    echo.
    echo This could mean:
    echo   - The service already exists (check services.msc)
    echo   - Insufficient permissions (make sure to run as Administrator)
    echo   - The path contains invalid characters
    echo.
    echo Current path: %EXE_PATH%
    echo.
    pause
    exit /b 1
)

REM Verify service was created
timeout /t 1 /nobreak >nul
sc query WeatherWallpaperService >nul 2>&1
if %errorLevel% neq 0 (
    echo WARNING: Service created but cannot be queried
    echo Please check services.msc to verify installation
    echo.
    pause
    exit /b 1
)

REM Set service description
sc description WeatherWallpaperService "Automatically updates desktop wallpaper with weather forecasts every 15 minutes"

REM Start the service
echo.
echo Starting service...
sc start WeatherWallpaperService
if %errorLevel% neq 0 (
    echo WARNING: Service created but failed to start
    echo You can start it manually from services.msc
    echo.
) else (
    echo.
    echo ========================================
    echo SUCCESS: Service installed and started
    echo ========================================
    echo.
    echo Service Name: WeatherWallpaperService
    echo Display Name: Weather Wallpaper Service
    echo Status: Running
    echo Startup Type: Automatic
    echo.
    echo The service will now update your wallpaper every 15 minutes.
    echo You can manage it through services.msc or Task Manager.
    echo.
)

pause
