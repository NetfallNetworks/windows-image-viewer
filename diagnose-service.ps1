#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Quick diagnostic for the Weather Wallpaper Service
#>

Write-Host "=== Service Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

$ServiceName = "WeatherWallpaperService"

# Check service status
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Service Status:" -ForegroundColor Yellow
    Write-Host "  Name: $($service.Name)"
    Write-Host "  Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })
    Write-Host "  Start Type: $($service.StartType)"

    # Get service details using WMI to see what account it's running as
    $wmiService = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
    if ($wmiService) {
        Write-Host "  Running As: $($wmiService.StartName)" -ForegroundColor $(if ($wmiService.StartName -eq 'LocalSystem') { 'Red' } else { 'Green' })

        if ($wmiService.StartName -eq 'LocalSystem') {
            Write-Host ""
            Write-Host "  ⚠️  PROBLEM: Service running as LocalSystem CANNOT set wallpapers!" -ForegroundColor Red
            Write-Host "     You need to reinstall with install-service.ps1 and choose option 1" -ForegroundColor Yellow
        }
    }
    Write-Host ""
} else {
    Write-Host "Service not installed" -ForegroundColor Red
    Write-Host ""
    exit 0
}

# Check log file
$logPath = Join-Path $env:TEMP "WeatherWallpaperService\service.log"
Write-Host "Log File:" -ForegroundColor Yellow
Write-Host "  Path: $logPath"

if (Test-Path $logPath) {
    $logSize = (Get-Item $logPath).Length
    Write-Host "  Size: $logSize bytes"
    Write-Host ""
    Write-Host "Last 20 lines:" -ForegroundColor Cyan
    Get-Content $logPath -Tail 20 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host "  Status: NOT FOUND" -ForegroundColor Red
    Write-Host ""
    Write-Host "  This means the service never started, or crashed before logging." -ForegroundColor Yellow
}

Write-Host ""
