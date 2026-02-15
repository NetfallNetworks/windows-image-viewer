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

REM Validate executable exists
if not exist "WallpaperApp.exe" (
    echo ERROR: WallpaperApp.exe not found in current directory
    echo.
    echo Please run this script from the directory containing WallpaperApp.exe
    echo.
    pause
    exit /b 1
)

REM Get the full path to the executable
set "EXE_PATH=%~dp0WallpaperApp.exe"
echo Executable path: %EXE_PATH%
echo.

REM Create service
echo Installing service...
sc create WeatherWallpaperService binPath= "%EXE_PATH%" DisplayName= "Weather Wallpaper Service" start= auto
if %errorLevel% neq 0 (
    echo ERROR: Failed to create service
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
