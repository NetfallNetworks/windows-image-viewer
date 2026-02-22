# Uninstallation script for Wallpaper Tray App
# This script removes the app from Windows Startup and deletes installation files

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Wallpaper - Uninstall Tray App" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host

# Get paths
$installPath = Join-Path $env:LOCALAPPDATA "WallpaperSync"
$startupFolder = [System.IO.Path]::Combine($env:APPDATA, "Microsoft\Windows\Start Menu\Programs\Startup")
$shortcutPath = Join-Path $startupFolder "Wallpaper.lnk"

# Check if running
$processName = "WallpaperApp.TrayApp"
$runningProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Host "WARNING: The tray app is currently running." -ForegroundColor Yellow
    Write-Host "   Attempting to stop it..." -ForegroundColor Yellow
    try {
        Stop-Process -Name $processName -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
        Write-Host "App stopped" -ForegroundColor Green
    } catch {
        Write-Host "ERROR: Failed to stop the app. Please close it manually and try again." -ForegroundColor Red
        Write-Host "   (Right-click tray icon -> Exit)" -ForegroundColor Yellow
        exit 1
    }
}
Write-Host

# Remove startup shortcut
if (Test-Path $shortcutPath) {
    Write-Host "Removing startup shortcut..." -ForegroundColor Yellow
    Remove-Item -Path $shortcutPath -Force
    Write-Host "Startup shortcut removed" -ForegroundColor Green
} else {
    Write-Host "INFO: No startup shortcut found" -ForegroundColor Gray
}
Write-Host

# Remove installation directory
if (Test-Path $installPath) {
    Write-Host "Removing installation files..." -ForegroundColor Yellow
    Remove-Item -Path $installPath -Recurse -Force
    Write-Host "Installation files removed" -ForegroundColor Green
} else {
    Write-Host "INFO: No installation directory found" -ForegroundColor Gray
}
Write-Host

# Note: logs and wallpaper images are stored inside the installation directory
# and were already removed above when $installPath was deleted.
Write-Host

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "UNINSTALL COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host
Write-Host "The Wallpaper tray app has been removed." -ForegroundColor Cyan
Write-Host "Thank you for using Wallpaper!" -ForegroundColor Cyan
Write-Host
