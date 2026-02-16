# Publish script for Wallpaper Tray App
# This script publishes the tray app as a single-file executable ready for installation

param(
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Wallpaper - Publish Tray App" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host

# Get paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$projectPath = Join-Path $rootPath "src\WallpaperApp.TrayApp\WallpaperApp.TrayApp.csproj"
$outputPath = Join-Path $rootPath "bin\TrayApp"

# Check if project exists
if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: Project file not found at: $projectPath" -ForegroundColor Red
    exit 1
}

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous publish..." -ForegroundColor Yellow
    if (Test-Path $outputPath) {
        Remove-Item -Path $outputPath -Recurse -Force
    }
    Write-Host "Clean complete" -ForegroundColor Green
    Write-Host
}

# Publish the project
Write-Host "Publishing tray app..." -ForegroundColor Yellow
Write-Host "  Project: $projectPath"
Write-Host "  Output: $outputPath"
Write-Host

try {
    dotnet publish $projectPath `
        --configuration Release `
        --output $outputPath `
        --self-contained true `
        --runtime win-x64 `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }

    Write-Host
    Write-Host "Publish successful!" -ForegroundColor Green
    Write-Host
    Write-Host "Output files:" -ForegroundColor Cyan
    Get-ChildItem $outputPath | ForEach-Object {
        Write-Host "  $($_.Name)" -ForegroundColor Gray
    }
    Write-Host
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Run .\scripts\install-tray-app.ps1 to install to startup"
    Write-Host "  2. Or manually run: $outputPath\WallpaperApp.TrayApp.exe"
    Write-Host

} catch {
    Write-Host
    Write-Host "PUBLISH FAILED" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host
    exit 1
}
