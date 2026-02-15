#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Checks for service startup errors and verifies installation
#>

Write-Host "=== Service Installation Verification ===" -ForegroundColor Cyan
Write-Host ""

$ServiceName = "WeatherWallpaperService"

# Get service details
$wmiService = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
if ($wmiService) {
    Write-Host "Service Details:" -ForegroundColor Yellow
    Write-Host "  Path: $($wmiService.PathName)" -ForegroundColor Gray
    Write-Host "  Account: $($wmiService.StartName)" -ForegroundColor Gray
    Write-Host ""

    # Parse the exe path (remove quotes)
    $exePath = $wmiService.PathName -replace '"', ''
    $exeDir = Split-Path $exePath -Parent

    Write-Host "Checking files:" -ForegroundColor Yellow

    # Check if exe exists
    if (Test-Path $exePath) {
        Write-Host "  [OK] Executable found: $exePath" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Executable NOT found: $exePath" -ForegroundColor Red
    }

    # Check for config file
    $configPath = Join-Path $exeDir "WallpaperApp.json"
    if (Test-Path $configPath) {
        Write-Host "  [OK] Config file found: $configPath" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Config file NOT found: $configPath" -ForegroundColor Red
        Write-Host "         The service needs WallpaperApp.json next to the exe!" -ForegroundColor Yellow
    }

    # Check for .NET dependencies
    $dllsToCheck = @("Microsoft.Extensions.Hosting.dll", "Microsoft.Extensions.Configuration.dll")
    foreach ($dll in $dllsToCheck) {
        $dllPath = Join-Path $exeDir $dll
        if (Test-Path $dllPath) {
            Write-Host "  [OK] Dependency found: $dll" -ForegroundColor Green
        } else {
            Write-Host "  [WARNING] Dependency might be missing: $dll" -ForegroundColor Yellow
        }
    }

    Write-Host ""
} else {
    Write-Host "ERROR: Service not found!" -ForegroundColor Red
    exit 1
}

# Check recent Application Event Log errors
Write-Host "Recent Application Event Log errors:" -ForegroundColor Yellow
Write-Host ""

$recentErrors = Get-EventLog -LogName Application -After (Get-Date).AddHours(-1) -ErrorAction SilentlyContinue |
    Where-Object {
        ($_.EntryType -eq 'Error') -and
        ($_.Source -like "*Service*" -or $_.Source -eq ".NET Runtime" -or $_.Message -like "*Weather*" -or $_.Message -like "*WallpaperApp*")
    } |
    Select-Object -First 5

if ($recentErrors) {
    foreach ($err in $recentErrors) {
        Write-Host "[$($err.TimeGenerated)] $($err.Source)" -ForegroundColor Red
        Write-Host $err.Message
        Write-Host ""
    }
} else {
    Write-Host "No recent errors found in Application log" -ForegroundColor Gray
    Write-Host ""
}

# Check System Event Log for service control manager errors
Write-Host "Recent Service Control Manager errors:" -ForegroundColor Yellow
Write-Host ""

$scmErrors = Get-EventLog -LogName System -After (Get-Date).AddHours(-1) -ErrorAction SilentlyContinue |
    Where-Object {
        ($_.Source -eq "Service Control Manager") -and
        ($_.Message -like "*Weather*" -or $_.Message -like "*WeatherWallpaper*")
    } |
    Select-Object -First 5

if ($scmErrors) {
    foreach ($err in $scmErrors) {
        Write-Host "[$($err.TimeGenerated)] Event ID: $($err.EventID)" -ForegroundColor $(if ($err.EntryType -eq 'Error') { 'Red' } else { 'Yellow' })
        Write-Host $err.Message
        Write-Host ""
    }
} else {
    Write-Host "No recent Service Control Manager errors found" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=== Suggestions ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If config file is missing:" -ForegroundColor Yellow
Write-Host "  Copy WallpaperApp.json to: $exeDir" -ForegroundColor Gray
Write-Host ""
Write-Host "If you see .NET errors above:" -ForegroundColor Yellow
Write-Host "  Rebuild with: dotnet publish src\WallpaperApp\WallpaperApp.csproj -c Release" -ForegroundColor Gray
Write-Host ""
