#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Checks Windows Event Logs for Weather Wallpaper Service errors

.DESCRIPTION
    Retrieves recent Application and System event log entries related to
    the WeatherWallpaperService to help diagnose why the service stopped.
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Service Error Log Checker" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ServiceName = "WeatherWallpaperService"
$HoursBack = 24

Write-Host "Checking for service errors in the last $HoursBack hours..." -ForegroundColor Yellow
Write-Host ""

# Check Application Log
Write-Host "=== APPLICATION LOG ===" -ForegroundColor Cyan
try {
    $appLogs = Get-EventLog -LogName Application -After (Get-Date).AddHours(-$HoursBack) -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -like "*$ServiceName*" -or $_.Message -like "*$ServiceName*" -or $_.Source -eq ".NET Runtime" } |
        Select-Object -First 50

    if ($appLogs) {
        $appLogs | Format-Table TimeGenerated, EntryType, Source, Message -AutoSize -Wrap
    } else {
        Write-Host "No application log entries found" -ForegroundColor Gray
    }
} catch {
    Write-Host "Could not read Application log: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== SYSTEM LOG ===" -ForegroundColor Cyan
try {
    $sysLogs = Get-EventLog -LogName System -After (Get-Date).AddHours(-$HoursBack) -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -like "*$ServiceName*" } |
        Select-Object -First 50

    if ($sysLogs) {
        $sysLogs | Format-Table TimeGenerated, EntryType, Source, Message -AutoSize -Wrap
    } else {
        Write-Host "No system log entries found" -ForegroundColor Gray
    }
} catch {
    Write-Host "Could not read System log: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== .NET RUNTIME ERRORS ===" -ForegroundColor Cyan
try {
    $dotnetLogs = Get-EventLog -LogName Application -After (Get-Date).AddHours(-$HoursBack) -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -eq ".NET Runtime" -and $_.EntryType -eq "Error" } |
        Select-Object -First 20

    if ($dotnetLogs) {
        foreach ($log in $dotnetLogs) {
            Write-Host "[$($log.TimeGenerated)] $($log.EntryType)" -ForegroundColor Red
            Write-Host $log.Message
            Write-Host ""
        }
    } else {
        Write-Host "No .NET Runtime errors found" -ForegroundColor Green
    }
} catch {
    Write-Host "Could not read .NET Runtime logs: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== SERVICE STATUS ===" -ForegroundColor Cyan
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Service Name: $($service.Name)"
    Write-Host "Display Name: $($service.DisplayName)"
    Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })
    Write-Host "Start Type: $($service.StartType)"
} else {
    Write-Host "Service not found" -ForegroundColor Red
}

Write-Host ""
