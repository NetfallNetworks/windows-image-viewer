#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Grants "Log on as a service" right to the current user
#>

Write-Host "=== Granting 'Log on as a service' right ===" -ForegroundColor Cyan
Write-Host ""

$username = "$env:USERDOMAIN\$env:USERNAME"
Write-Host "User: $username" -ForegroundColor Gray
Write-Host ""

try {
    Write-Host "Exporting current security policy..." -ForegroundColor Yellow

    $tempFile = [System.IO.Path]::GetTempFileName()
    $tempCfg = [System.IO.Path]::GetTempFileName()

    secedit /export /cfg $tempCfg /quiet

    # Read the config
    $cfg = Get-Content $tempCfg

    # Find the SeServiceLogonRight line
    $found = $false
    $newCfg = foreach ($line in $cfg) {
        if ($line -match '^SeServiceLogonRight\s*=\s*(.*)$') {
            $found = $true
            $users = $Matches[1].Trim()

            # Convert username to SID
            $user = New-Object System.Security.Principal.NTAccount($username)
            $sid = $user.Translate([System.Security.Principal.SecurityIdentifier]).Value

            if ($users -notmatch [regex]::Escape($sid)) {
                Write-Host "Adding $username to SeServiceLogonRight..." -ForegroundColor Green
                if ($users) {
                    "SeServiceLogonRight = $users,*$sid"
                } else {
                    "SeServiceLogonRight = *$sid"
                }
            } else {
                Write-Host "$username already has SeServiceLogonRight" -ForegroundColor Green
                $line
            }
        } else {
            $line
        }
    }

    # If SeServiceLogonRight wasn't found, add it
    if (-not $found) {
        Write-Host "SeServiceLogonRight not found, adding..." -ForegroundColor Yellow
        $user = New-Object System.Security.Principal.NTAccount($username)
        $sid = $user.Translate([System.Security.Principal.SecurityIdentifier]).Value
        $newCfg += "SeServiceLogonRight = *$sid"
    }

    # Write new config
    $newCfg | Set-Content $tempCfg -Encoding Unicode

    # Import the new config
    Write-Host "Applying security policy..." -ForegroundColor Yellow
    secedit /configure /db secedit.sdb /cfg $tempCfg /quiet

    # Cleanup
    Remove-Item $tempCfg -ErrorAction SilentlyContinue
    Remove-Item "secedit.sdb" -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "SUCCESS: 'Log on as a service' right granted" -ForegroundColor Green
    Write-Host ""
    Write-Host "Now try starting the service:" -ForegroundColor Cyan
    Write-Host "  sc start WeatherWallpaperService" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or:" -ForegroundColor Cyan
    Write-Host "  Start-Service WeatherWallpaperService" -ForegroundColor Yellow
    Write-Host ""

} catch {
    Write-Host "ERROR: Failed to grant 'Log on as a service' right" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "You can manually grant this right:" -ForegroundColor Yellow
    Write-Host "  1. Run 'secpol.msc'" -ForegroundColor Gray
    Write-Host "  2. Navigate to: Local Policies -> User Rights Assignment" -ForegroundColor Gray
    Write-Host "  3. Double-click 'Log on as a service'" -ForegroundColor Gray
    Write-Host "  4. Add your user account: $username" -ForegroundColor Gray
    Write-Host ""
}
