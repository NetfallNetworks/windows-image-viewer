#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Uninstalls the Weather Wallpaper Service

.DESCRIPTION
    This script stops and removes the Weather Wallpaper Windows Service.
    Must be run as Administrator.

.EXAMPLE
    .\uninstall-service.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Weather Wallpaper Service - Uninstallation" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$ServiceName = "WeatherWallpaperService"
$DisplayName = "Weather Wallpaper Service"

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
    Write-Host "Nothing to uninstall." -ForegroundColor Gray
    Write-Host ""
    exit 0
}

Write-Host "Found service: $DisplayName" -ForegroundColor Gray
Write-Host "Current status: $($service.Status)" -ForegroundColor Gray
Write-Host ""

# Stop the service if running
if ($service.Status -eq 'Running') {
    try {
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop

        # Wait for service to stop
        $service.WaitForStatus('Stopped', '00:00:30')
        Write-Host "Service stopped successfully" -ForegroundColor Green
        Write-Host ""
    } catch {
        Write-Host "WARNING: Failed to stop service" -ForegroundColor Yellow
        Write-Host $_.Exception.Message -ForegroundColor Red
        Write-Host ""
        Write-Host "Attempting to remove anyway..." -ForegroundColor Gray
        Write-Host ""
    }
} else {
    Write-Host "Service is not running (Status: $($service.Status))" -ForegroundColor Gray
    Write-Host ""
}

# Delete the service
try {
    Write-Host "Removing service..." -ForegroundColor Yellow

    # Use sc.exe directly as Remove-Service might not be available in older PowerShell
    $result = & sc.exe delete $ServiceName 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service removed successfully" -ForegroundColor Green
        Write-Host ""

        # Verify it's gone
        Start-Sleep -Seconds 2
        $stillExists = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

        if ($stillExists) {
            Write-Host "WARNING: Service marked for deletion but still visible" -ForegroundColor Yellow
            Write-Host "It should disappear after a system restart" -ForegroundColor Gray
            Write-Host ""
        }
    } else {
        throw "sc.exe delete failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Host "ERROR: Failed to remove service" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "You may need to:" -ForegroundColor Yellow
    Write-Host "  1. Restart your computer" -ForegroundColor Yellow
    Write-Host "  2. Manually delete from services.msc" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "SUCCESS: Service uninstalled" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "The Weather Wallpaper Service has been removed from your system." -ForegroundColor Gray
Write-Host ""
Write-Host "Note: Configuration files and downloaded images have NOT been deleted." -ForegroundColor Gray
Write-Host "You can manually remove them from:" -ForegroundColor Gray
Write-Host "  - %TEMP%\WeatherWallpaper\ (downloaded images)" -ForegroundColor Gray
Write-Host ""
