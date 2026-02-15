@echo off
REM Uninstall Weather Wallpaper Service
REM This script must be run as Administrator

echo ========================================
echo Weather Wallpaper Service - Uninstallation
echo ========================================
echo.

REM Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator
    echo.
    echo Right-click uninstall-service.bat and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Check if service exists
sc query WeatherWallpaperService >nul 2>&1
if %errorLevel% neq 0 (
    echo Service "WeatherWallpaperService" is not installed.
    echo Nothing to uninstall.
    echo.
    pause
    exit /b 0
)

REM Stop the service first
echo Stopping service...
sc stop WeatherWallpaperService >nul 2>&1
if %errorLevel% equ 0 (
    echo Service stopped.
    REM Wait a moment for service to fully stop
    timeout /t 2 /nobreak >nul
) else (
    echo Service was not running or already stopped.
)
echo.

REM Delete the service
echo Removing service...
sc delete WeatherWallpaperService
if %errorLevel% neq 0 (
    echo ERROR: Failed to remove service
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCCESS: Service uninstalled
echo ========================================
echo.
echo The Weather Wallpaper Service has been removed from your system.
echo.
echo Note: Configuration files and downloaded images have NOT been deleted.
echo You can manually remove them from:
echo   - %%TEMP%%\WeatherWallpaper\ (downloaded images)
echo.

pause
