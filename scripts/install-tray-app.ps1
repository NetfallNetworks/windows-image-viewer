# Installation script for Weather Wallpaper Tray App
# This script copies the app to a permanent location and adds it to Windows Startup

param(
    [switch]$AutoStart = $true
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Weather Wallpaper - Install Tray App" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host

# Get paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$buildPath = Join-Path $rootPath "bin\TrayApp"
$installPath = Join-Path $env:LOCALAPPDATA "WeatherWallpaper"
$startupFolder = [System.IO.Path]::Combine($env:APPDATA, "Microsoft\Windows\Start Menu\Programs\Startup")

# Check if build exists
if (-not (Test-Path $buildPath)) {
    Write-Host "ERROR: Build not found. Please run .\scripts\publish-tray-app.ps1 first!" -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $buildPath "WallpaperApp.TrayApp.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found at: $exePath" -ForegroundColor Red
    exit 1
}

# Check if already running
$processName = "WallpaperApp.TrayApp"
$runningProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Host "WARNING: The tray app is currently running." -ForegroundColor Yellow
    Write-Host "   Please close it first (right-click tray icon -> Exit)" -ForegroundColor Yellow
    Write-Host
    $continue = Read-Host "Press Enter to continue after closing the app, or Ctrl+C to cancel"

    # Check again
    $runningProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($runningProcess) {
        Write-Host "ERROR: App is still running. Please close it and try again." -ForegroundColor Red
        exit 1
    }
}

# Create install directory
Write-Host "Creating installation directory..." -ForegroundColor Yellow
if (Test-Path $installPath) {
    Write-Host "  Removing old installation..." -ForegroundColor Gray
    Remove-Item -Path $installPath -Recurse -Force
}
New-Item -ItemType Directory -Path $installPath -Force | Out-Null
Write-Host "Created: $installPath" -ForegroundColor Green
Write-Host

# Copy files
Write-Host "Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$buildPath\*" -Destination $installPath -Recurse -Force
Write-Host "Files copied successfully" -ForegroundColor Green
Write-Host

# Create shortcut in Startup folder
if ($AutoStart) {
    Write-Host "Adding to Windows Startup..." -ForegroundColor Yellow

    $shortcutPath = Join-Path $startupFolder "Weather Wallpaper.lnk"
    $targetPath = Join-Path $installPath "WallpaperApp.TrayApp.exe"

    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = $targetPath
    $Shortcut.WorkingDirectory = $installPath
    $Shortcut.Description = "Weather Wallpaper Tray App"
    $Shortcut.Save()

    Write-Host "Startup shortcut created" -ForegroundColor Green
    Write-Host "  Location: $shortcutPath" -ForegroundColor Gray
    Write-Host
}

# Configuration setup
Write-Host "Configuration Setup" -ForegroundColor Yellow
Write-Host
$configPath = Join-Path $installPath "WallpaperApp.json"
Write-Host "The app will use the configuration at:" -ForegroundColor Cyan
Write-Host "  $configPath" -ForegroundColor White
Write-Host

$editConfig = Read-Host "Would you like to edit the configuration now? (y/n)"
if ($editConfig -eq "y" -or $editConfig -eq "Y") {
    notepad $configPath
    Write-Host "Configuration saved" -ForegroundColor Green
    Write-Host
}

# Offer to start now
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "INSTALLATION COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host
Write-Host "Installation location: $installPath" -ForegroundColor Cyan
if ($AutoStart) {
    Write-Host "Auto-start: ENABLED (runs at login)" -ForegroundColor Cyan
} else {
    Write-Host "Auto-start: DISABLED" -ForegroundColor Cyan
}
Write-Host

$startNow = Read-Host "Would you like to start the tray app now? (y/n)"
if ($startNow -eq "y" -or $startNow -eq "Y") {
    Write-Host "Starting Weather Wallpaper..." -ForegroundColor Yellow
    Start-Process -FilePath (Join-Path $installPath "WallpaperApp.TrayApp.exe") -WorkingDirectory $installPath
    Start-Sleep -Seconds 2
    Write-Host "App started! Check your system tray (bottom-right corner)" -ForegroundColor Green
    Write-Host
    Write-Host "Tip: Right-click the tray icon for options" -ForegroundColor Cyan
} else {
    Write-Host "You can start it manually by running:" -ForegroundColor Yellow
    Write-Host "  $installPath\WallpaperApp.TrayApp.exe" -ForegroundColor White
    Write-Host
    Write-Host "Or just log out and back in - it will start automatically!" -ForegroundColor Cyan
}

Write-Host
