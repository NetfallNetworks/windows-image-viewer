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

# Ask user how to run the service
Write-Host "=== SERVICE ACCOUNT CONFIGURATION ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Windows Services run in Session 0 (non-interactive) by default."
Write-Host "To set your wallpaper, the service needs to run as YOUR user account."
Write-Host ""
Write-Host "Choose how to run the service:"
Write-Host "  1. As YOUR user account (RECOMMENDED - can set wallpaper)" -ForegroundColor Green
Write-Host "  2. As LocalSystem (won't be able to set wallpaper)" -ForegroundColor Yellow
Write-Host ""
$choice = Read-Host "Enter choice (1 or 2)"

if ($choice -eq "1") {
    Write-Host ""
    Write-Host "The service will run as: $env:USERDOMAIN\$env:USERNAME" -ForegroundColor Cyan
    Write-Host "You will be prompted for your Windows password." -ForegroundColor Yellow
    Write-Host ""

    $credential = Get-Credential -UserName "$env:USERDOMAIN\$env:USERNAME" -Message "Enter your Windows password to run the service as your user account"

    if (-not $credential) {
        Write-Host "ERROR: Password not provided" -ForegroundColor Red
        exit 1
    }

    $username = $credential.UserName
    $password = $credential.GetNetworkCredential().Password
} else {
    $username = $null
    $password = $null
}

Write-Host ""

# Create the service
try {
    Write-Host "Installing service..." -ForegroundColor Green

    if ($username) {
        # Create service with user account using sc.exe (more reliable for credentials)
        $result = & sc.exe create $ServiceName binPath= "`"$ExePath`"" DisplayName= $DisplayName start= auto obj= $username password= $password

        if ($LASTEXITCODE -ne 0) {
            throw "sc.exe create failed with exit code $LASTEXITCODE"
        }

        # Set description separately
        sc.exe description $ServiceName $Description | Out-Null

        Write-Host "Service created successfully (running as $username)" -ForegroundColor Green
    } else {
        # Create service as LocalSystem
        New-Service -Name $ServiceName `
                    -BinaryPathName $ExePath `
                    -DisplayName $DisplayName `
                    -Description $Description `
                    -StartupType Automatic `
                    -ErrorAction Stop

        Write-Host "Service created successfully (running as LocalSystem)" -ForegroundColor Green
        Write-Host "WARNING: LocalSystem services cannot set user wallpapers!" -ForegroundColor Yellow
    }

    Write-Host ""
} catch {
    Write-Host "ERROR: Failed to create service" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Configure service to restart on failure
try {
    Write-Host "Configuring automatic restart on failure..." -ForegroundColor Green

    # Set recovery options: restart after 1 minute on first 3 failures
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

    Write-Host "Auto-restart configured" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "WARNING: Could not configure auto-restart" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    Write-Host ""
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
        Write-Host "Running As: $(if ($username) { $username } else { 'LocalSystem' })" -ForegroundColor Cyan
        Write-Host "Auto-Restart: Enabled (restarts on failure)" -ForegroundColor Cyan
        Write-Host ""
        if ($username) {
            Write-Host "✓ The service will now update your wallpaper every 15 minutes." -ForegroundColor Green
        } else {
            Write-Host "⚠ WARNING: Service running as LocalSystem cannot set wallpapers!" -ForegroundColor Yellow
            Write-Host "  Reinstall and choose option 1 to run as your user account." -ForegroundColor Yellow
        }
        Write-Host ""
        Write-Host "You can manage the service through services.msc or Task Manager." -ForegroundColor Gray
        Write-Host ""
        $logPath = Join-Path $env:TEMP "WeatherWallpaperService\service.log"
        Write-Host "Service logs: $logPath" -ForegroundColor Cyan
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
