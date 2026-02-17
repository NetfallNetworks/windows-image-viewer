# Installation script for Wallpaper Tray App
# This script copies the app to a permanent location and adds it to Windows Startup

param(
    [switch]$AutoStart = $true
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Wallpaper - Install Tray App" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host

# Get paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$buildPath = Join-Path $rootPath "bin\TrayApp"
$installPath = Join-Path $env:LOCALAPPDATA "Wallpaper"
$startupFolder = [System.IO.Path]::Combine($env:APPDATA, "Microsoft\Windows\Start Menu\Programs\Startup")

# Check if build exists
if (-not (Test-Path $buildPath)) {
    Write-Host "ERROR: Build not found at $buildPath" -ForegroundColor Red
    Write-Host "Please run .\scripts\build.bat first!" -ForegroundColor Yellow
    exit 1
}

$exePath = Join-Path $buildPath "WallpaperApp.TrayApp.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found at: $exePath" -ForegroundColor Red
    exit 1
}

# Check if already running and stop it
$processName = "WallpaperApp.TrayApp"
$runningProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Host "Stopping running instance..." -ForegroundColor Yellow
    $runningProcess | Stop-Process -Force
    Start-Sleep -Seconds 1
    Write-Host "Stopped existing instance" -ForegroundColor Green
    Write-Host
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

    $shortcutPath = Join-Path $startupFolder "Wallpaper.lnk"
    $targetPath = Join-Path $installPath "WallpaperApp.TrayApp.exe"

    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = $targetPath
    $Shortcut.WorkingDirectory = $installPath
    $Shortcut.Description = "Wallpaper Tray App"
    $Shortcut.Save()

    Write-Host "Startup shortcut created" -ForegroundColor Green
    Write-Host "  Location: $shortcutPath" -ForegroundColor Gray
    Write-Host
}

# Ensure configuration file exists
$configPath = Join-Path $installPath "WallpaperApp.json"
if (-not (Test-Path $configPath)) {
    Write-Host "Creating default configuration..." -ForegroundColor Yellow
    $defaultConfig = @{
        AppSettings = @{
            ImageUrl = "https://weather.zamflam.com/assets/diagram.png"
            RefreshIntervalMinutes = 15
        }
    }
    $defaultConfig | ConvertTo-Json -Depth 10 | Set-Content -Path $configPath -Encoding UTF8
    Write-Host "Default configuration created" -ForegroundColor Green
}
Write-Host "Configuration location: $configPath" -ForegroundColor Cyan
Write-Host "  (You can edit settings through the tray app later)" -ForegroundColor Gray
Write-Host

# Installation complete - launch the app
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

# Automatically start the app
Write-Host "Starting Wallpaper..." -ForegroundColor Yellow
Start-Process -FilePath (Join-Path $installPath "WallpaperApp.TrayApp.exe") -WorkingDirectory $installPath
Start-Sleep -Seconds 2

Write-Host "App started! Check your system tray (bottom-right corner)" -ForegroundColor Green
Write-Host
Write-Host "Tip: Right-click the tray icon for options and settings" -ForegroundColor Cyan
Write-Host
