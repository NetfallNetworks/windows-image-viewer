<#
.SYNOPSIS
    Builds and signs the Wallpaper Sync sparse MSIX identity package.

.DESCRIPTION
    This script:
    1. Locates makeappx.exe and signtool.exe from the Windows SDK
    2. Packs the identity package directory into an MSIX
    3. Signs the MSIX with the development certificate (WallpaperSync-Dev.pfx)

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

Write-Host "=== Building Sparse MSIX Identity Package ===" -ForegroundColor Cyan
Write-Host ""

# ── Locate Windows SDK tools ──────────────────────────────────────────────────

function Find-SdkTool {
    param([string]$ToolName)

    # Search in standard Windows SDK paths
    $sdkPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "$env:ProgramFiles\Windows Kits\10\bin"
    )

    foreach ($sdkBase in $sdkPaths) {
        if (Test-Path $sdkBase) {
            # Find the latest SDK version directory
            $versionDirs = Get-ChildItem -Path $sdkBase -Directory |
                Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
                Sort-Object { [version]$_.Name } -Descending

            foreach ($versionDir in $versionDirs) {
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
    Write-Error "makeappx.exe not found. Install the Windows SDK: https://developer.microsoft.com/windows/downloads/windows-sdk/"
}

if (-not $signtool) {
    Write-Error "signtool.exe not found. Install the Windows SDK: https://developer.microsoft.com/windows/downloads/windows-sdk/"
}

Write-Host "  makeappx: $makeappx"
Write-Host "  signtool: $signtool"

# ── Verify prerequisites ──────────────────────────────────────────────────────

if (-not (Test-Path (Join-Path $scriptDir "AppxManifest.xml"))) {
    Write-Error "AppxManifest.xml not found in $scriptDir"
}

if (-not (Test-Path $PfxPath)) {
    Write-Error "Certificate not found: $PfxPath. Run create-dev-cert.ps1 first."
}

# ── Pack the MSIX ─────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Packing identity package..." -ForegroundColor Yellow

# Remove existing MSIX if present
if (Test-Path $outputMsix) {
    Remove-Item $outputMsix -Force
}

& $makeappx pack /d $scriptDir /p $outputMsix /nv
if ($LASTEXITCODE -ne 0) {
    Write-Error "makeappx.exe failed with exit code $LASTEXITCODE"
}

Write-Host "MSIX created: $outputMsix" -ForegroundColor Green

# ── Sign the MSIX ─────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Signing identity package..." -ForegroundColor Yellow

& $signtool sign /fd sha256 /a /f $PfxPath /p $PfxPassword $outputMsix
if ($LASTEXITCODE -ne 0) {
    Write-Error "signtool.exe failed with exit code $LASTEXITCODE"
}

Write-Host "MSIX signed successfully" -ForegroundColor Green

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Identity Package Build Complete ===" -ForegroundColor Cyan
Write-Host "  Output: $outputMsix"
Write-Host "  Size:   $([math]::Round((Get-Item $outputMsix).Length / 1KB, 1)) KB"
Write-Host ""
Write-Host "To install: Add-AppxPackage -Path `"$outputMsix`"" -ForegroundColor Yellow
Write-Host "To register via TrayApp: The app will auto-register on first startup." -ForegroundColor Yellow
