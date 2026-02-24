<#
.SYNOPSIS
    Builds and signs the Wallpaper Sync sparse MSIX identity package.

.DESCRIPTION
    This script:
    1. Locates makeappx.exe and signtool.exe from the Windows SDK (10.0.18362+)
    2. Auto-creates a self-signed dev certificate if one doesn't exist
    3. Packs the identity package (manifest + assets only) into an MSIX
    4. Signs the MSIX with the certificate

    The resulting MSIX provides package identity for the widget provider COM server
    so the Windows 11 Widget Board can activate it.

.PARAMETER PfxPath
    Path to the PFX certificate file. Defaults to WallpaperSync-Dev.pfx in this directory.

.PARAMETER PfxPassword
    Password for the PFX file. Defaults to "WallpaperSyncDev".

.EXAMPLE
    .\build-identity-package.ps1

.EXAMPLE
    .\build-identity-package.ps1 -PfxPath "C:\certs\production.pfx" -PfxPassword "secret"
#>

param(
    [string]$PfxPath = (Join-Path $PSScriptRoot "WallpaperSync-Dev.pfx"),
    [string]$PfxPassword = "WallpaperSyncDev"
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$outputMsix = Join-Path (Split-Path $scriptDir -Parent) "WallpaperSync-Identity.msix"
$minSdkVersion = [version]"10.0.18362.0"

Write-Host "=== Building Sparse MSIX Identity Package ===" -ForegroundColor Cyan
Write-Host ""

# -- Locate Windows SDK tools (requires 10.0.18362+ for AllowExternalContent) --

function Find-SdkTool {
    param([string]$ToolName)

    $sdkPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "$env:ProgramFiles\Windows Kits\10\bin"
    )

    foreach ($sdkBase in $sdkPaths) {
        if (Test-Path $sdkBase) {
            $versionDirs = Get-ChildItem -Path $sdkBase -Directory |
                Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
                Sort-Object { [version]$_.Name } -Descending

            foreach ($versionDir in $versionDirs) {
                if ([version]$versionDir.Name -lt $minSdkVersion) { continue }
                $toolPath = Join-Path $versionDir.FullName "x64\$ToolName"
                if (Test-Path $toolPath) {
                    return $toolPath
                }
            }
        }
    }

    # Try PATH as fallback
    $inPath = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($inPath) { return $inPath.Source }

    return $null
}

$makeappx = Find-SdkTool "makeappx.exe"
$signtool = Find-SdkTool "signtool.exe"

if (-not $makeappx) {
    Write-Error "makeappx.exe not found (need SDK $minSdkVersion+). Install from: https://developer.microsoft.com/windows/downloads/windows-sdk/"
}

if (-not $signtool) {
    Write-Error "signtool.exe not found (need SDK $minSdkVersion+). Install from: https://developer.microsoft.com/windows/downloads/windows-sdk/"
}

Write-Host "  makeappx: $makeappx"
Write-Host "  signtool: $signtool"

# -- Verify prerequisites -------------------------------------------------

if (-not (Test-Path (Join-Path $scriptDir "AppxManifest.xml"))) {
    Write-Error "AppxManifest.xml not found in $scriptDir"
}

# -- Auto-create dev certificate if missing --------------------------------

if (-not (Test-Path $PfxPath)) {
    Write-Host "  Certificate not found - creating self-signed dev cert..." -ForegroundColor Yellow

    $certSubject = "CN=WallpaperSync"
    $pfxSecure = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText

    # Remove any stale cert with the same subject
    Get-ChildItem -Path Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $certSubject } |
        Remove-Item -Force -ErrorAction SilentlyContinue

    # Create self-signed code signing certificate (no admin needed)
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $certSubject `
        -KeyUsage DigitalSignature `
        -FriendlyName "Wallpaper Sync Development Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
        -NotAfter (Get-Date).AddYears(1)

    Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password $pfxSecure | Out-Null
    Write-Host "  Certificate created: $($cert.Thumbprint)" -ForegroundColor Green

    # Trust the cert so Windows accepts the signed MSIX (requires admin).
    # If not admin, skip - the user will see a trust prompt on first install.
    try {
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            "TrustedPeople",
            [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
        $store.Open("ReadWrite")
        $store.Add($cert)
        $store.Close()
        Write-Host "  Certificate trusted (TrustedPeople store)" -ForegroundColor Green
    } catch {
        Write-Host "  Skipped TrustedPeople store (not admin - MSIX install may prompt)" -ForegroundColor Yellow
    }
}

# -- Pack the MSIX (manifest + assets only) --------------------------------

Write-Host ""
Write-Host "Packing identity package..." -ForegroundColor Yellow

# Remove existing MSIX if present
if (Test-Path $outputMsix) {
    Remove-Item $outputMsix -Force
}

# Use a mapping file so makeappx only packs the manifest and assets,
# not scripts, certs, or other dev files in the directory.
$mappingFile = Join-Path $env:TEMP "WallpaperSync-msix-mapping.txt"
$mappingContent = @"
[Files]
"$scriptDir\AppxManifest.xml" "AppxManifest.xml"
"$scriptDir\Assets\StoreLogo.png" "Assets\StoreLogo.png"
"$scriptDir\Assets\Square44x44Logo.png" "Assets\Square44x44Logo.png"
"$scriptDir\Assets\Square150x150Logo.png" "Assets\Square150x150Logo.png"
"$scriptDir\WidgetAssets\WidgetIcon.png" "WidgetAssets\WidgetIcon.png"
"$scriptDir\WidgetAssets\WidgetScreenshotLight.png" "WidgetAssets\WidgetScreenshotLight.png"
"$scriptDir\WidgetAssets\WidgetScreenshotDark.png" "WidgetAssets\WidgetScreenshotDark.png"
"$scriptDir\Public\.gitkeep" "Public\.gitkeep"
"@
Set-Content -Path $mappingFile -Value $mappingContent -Encoding UTF8

& $makeappx pack /f $mappingFile /p $outputMsix /nv
if ($LASTEXITCODE -ne 0) {
    Write-Error "makeappx.exe failed with exit code $LASTEXITCODE"
}

Remove-Item $mappingFile -Force -ErrorAction SilentlyContinue

Write-Host "MSIX created: $outputMsix" -ForegroundColor Green

# -- Sign the MSIX --------------------------------------------------------

Write-Host ""
Write-Host "Signing identity package..." -ForegroundColor Yellow

& $signtool sign /fd sha256 /a /f $PfxPath /p $PfxPassword $outputMsix
if ($LASTEXITCODE -ne 0) {
    Write-Error "signtool.exe failed with exit code $LASTEXITCODE"
}

Write-Host "MSIX signed successfully" -ForegroundColor Green

# -- Done ------------------------------------------------------------------

Write-Host ""
Write-Host "=== Identity Package Build Complete ===" -ForegroundColor Cyan
Write-Host "  Output: $outputMsix"
Write-Host "  Size:   $([math]::Round((Get-Item $outputMsix).Length / 1KB, 1)) KB"
