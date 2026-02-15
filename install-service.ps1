#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs the Weather Wallpaper Service

.DESCRIPTION
    This script installs and starts the Weather Wallpaper Windows Service.
    Must be run as Administrator.

.EXAMPLE
    .\install-service.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "Weather Wallpaper Service - Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Service configuration
$ServiceName = "WeatherWallpaperService"
$DisplayName = "Weather Wallpaper Service"
$Description = "Automatically updates desktop wallpaper with weather forecasts every 15 minutes"

# Find the executable
$ExePath = $null
$SearchPaths = @(
    "src\WallpaperApp\bin\Release\net8.0-windows\win-x64\publish\WallpaperApp.exe",
    "src\WallpaperApp\bin\Release\net8.0-windows\win-x64\WallpaperApp.exe",
    "WallpaperApp.exe"
)

foreach ($path in $SearchPaths) {
    $fullPath = Join-Path $PSScriptRoot $path
    if (Test-Path $fullPath) {
        $ExePath = $fullPath
        break
    }
}

if (-not $ExePath) {
    Write-Host "ERROR: WallpaperApp.exe not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the project first using:"
    Write-Host "  dotnet publish src\WallpaperApp\WallpaperApp.csproj -c Release" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or place WallpaperApp.exe in the project root"
    Write-Host ""
    exit 1
}

Write-Host "Executable path: $ExePath" -ForegroundColor Gray
Write-Host ""

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "WARNING: Service '$ServiceName' already exists!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Current status: $($existingService.Status)"
    Write-Host ""
    Write-Host "Please uninstall it first using:"
    Write-Host "  .\uninstall-service.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or manually from services.msc (find '$DisplayName', right-click, Delete)"
    Write-Host ""
    exit 1
}

# Create the service
try {
    Write-Host "Installing service..." -ForegroundColor Green
    New-Service -Name $ServiceName `
                -BinaryPathName $ExePath `
                -DisplayName $DisplayName `
                -Description $Description `
                -StartupType Automatic `
                -ErrorAction Stop

    Write-Host "Service created successfully" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR: Failed to create service" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Verify service exists
Start-Sleep -Seconds 1
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "ERROR: Service created but cannot be found" -ForegroundColor Red
    Write-Host "Please check services.msc manually" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Start the service
try {
    Write-Host "Starting service..." -ForegroundColor Green
    Start-Service -Name $ServiceName -ErrorAction Stop

    # Wait a moment and check status
    Start-Sleep -Seconds 2
    $service.Refresh()

    if ($service.Status -eq 'Running') {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "SUCCESS: Service installed and started" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Service Name: $ServiceName" -ForegroundColor Cyan
        Write-Host "Display Name: $DisplayName" -ForegroundColor Cyan
        Write-Host "Status: Running" -ForegroundColor Green
        Write-Host "Startup Type: Automatic" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "The service will now update your wallpaper every 15 minutes." -ForegroundColor Gray
        Write-Host "You can manage it through services.msc or Task Manager." -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "WARNING: Service created but not running (Status: $($service.Status))" -ForegroundColor Yellow
        Write-Host "You can start it manually from services.msc" -ForegroundColor Yellow
        Write-Host ""
    }
} catch {
    Write-Host ""
    Write-Host "WARNING: Service created but failed to start" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "You can start it manually from services.msc" -ForegroundColor Yellow
    Write-Host ""
}
