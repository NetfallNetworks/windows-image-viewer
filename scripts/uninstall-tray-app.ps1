# Uninstallation script for Weather Wallpaper Tray App
# This script removes the app from Windows Startup and deletes installation files

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Weather Wallpaper - Uninstall Tray App" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host

# Get paths
$installPath = Join-Path $env:LOCALAPPDATA "WeatherWallpaper"
$startupFolder = [System.IO.Path]::Combine($env:APPDATA, "Microsoft\Windows\Start Menu\Programs\Startup")
$shortcutPath = Join-Path $startupFolder "Weather Wallpaper.lnk"

# Check if running
$processName = "WallpaperApp.TrayApp"
$runningProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Host "⚠️  The tray app is currently running." -ForegroundColor Yellow
    Write-Host "   Attempting to stop it..." -ForegroundColor Yellow
    try {
        Stop-Process -Name $processName -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
        Write-Host "✓ App stopped" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to stop the app. Please close it manually and try again." -ForegroundColor Red
        Write-Host "   (Right-click tray icon -> Exit)" -ForegroundColor Yellow
        exit 1
    }
}
Write-Host

# Remove startup shortcut
if (Test-Path $shortcutPath) {
    Write-Host "🗑️  Removing startup shortcut..." -ForegroundColor Yellow
    Remove-Item -Path $shortcutPath -Force
    Write-Host "✓ Startup shortcut removed" -ForegroundColor Green
} else {
    Write-Host "ℹ️  No startup shortcut found" -ForegroundColor Gray
}
Write-Host

# Remove installation directory
if (Test-Path $installPath) {
    Write-Host "🗑️  Removing installation files..." -ForegroundColor Yellow
    Remove-Item -Path $installPath -Recurse -Force
    Write-Host "✓ Installation files removed" -ForegroundColor Green
} else {
    Write-Host "ℹ️  No installation directory found" -ForegroundColor Gray
}
Write-Host

# Clean up temp files (optional)
$tempPath = Join-Path $env:TEMP "WeatherWallpaperService"
if (Test-Path $tempPath) {
    $cleanTemp = Read-Host "Would you like to delete downloaded wallpaper images? (y/n)"
    if ($cleanTemp -eq "y" -or $cleanTemp -eq "Y") {
        Write-Host "🗑️  Removing temporary files..." -ForegroundColor Yellow
        Remove-Item -Path $tempPath -Recurse -Force
        Write-Host "✓ Temporary files removed" -ForegroundColor Green
    }
}
Write-Host

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ UNINSTALL COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host
Write-Host "The Weather Wallpaper tray app has been removed." -ForegroundColor Cyan
Write-Host "Thank you for using Weather Wallpaper!" -ForegroundColor Cyan
Write-Host
