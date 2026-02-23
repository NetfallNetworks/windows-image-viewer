#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Creates a self-signed certificate for signing the Wallpaper Sync sparse MSIX identity package.

.DESCRIPTION
    This script creates a self-signed code signing certificate with Subject "CN=WallpaperSync"
    (matching the Publisher in AppxManifest.xml), exports it to a PFX file, and installs it
    to the TrustedPeople certificate store so Windows will accept the signed MSIX without
    SmartScreen warnings during development.

    For production builds, use a certificate from a trusted CA instead. See SIGNING.md.

.NOTES
    - Must be run as Administrator (for TrustedPeople store access)
    - The PFX file is gitignored and must not be committed to source control
    - Certificate is valid for 1 year from creation

.EXAMPLE
    .\create-dev-cert.ps1
#>

$ErrorActionPreference = "Stop"

$certSubject = "CN=WallpaperSync"
$pfxPath = Join-Path $PSScriptRoot "WallpaperSync-Dev.pfx"
$pfxPassword = ConvertTo-SecureString -String "WallpaperSyncDev" -Force -AsPlainText

Write-Host "=== Creating Self-Signed Development Certificate ===" -ForegroundColor Cyan
Write-Host ""

# Check if certificate already exists
$existingCert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $certSubject }
if ($existingCert) {
    Write-Host "Existing certificate found: $($existingCert.Thumbprint)" -ForegroundColor Yellow
    Write-Host "Removing existing certificate..." -ForegroundColor Yellow
    $existingCert | Remove-Item -Force
}

# Create self-signed certificate
Write-Host "Creating certificate with Subject: $certSubject"
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $certSubject `
    -KeyUsage DigitalSignature `
    -FriendlyName "Wallpaper Sync Development Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
    -NotAfter (Get-Date).AddYears(1)

Write-Host "Certificate created: $($cert.Thumbprint)" -ForegroundColor Green

# Export to PFX
Write-Host "Exporting to: $pfxPath"
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pfxPassword | Out-Null
Write-Host "PFX exported successfully" -ForegroundColor Green

# Install to TrustedPeople store (so Windows trusts packages signed with this cert)
Write-Host "Installing to TrustedPeople store..."
$trustedPeopleStore = New-Object System.Security.Cryptography.X509Certificates.X509Store(
    "TrustedPeople",
    [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
$trustedPeopleStore.Open("ReadWrite")
$trustedPeopleStore.Add($cert)
$trustedPeopleStore.Close()
Write-Host "Certificate installed to TrustedPeople store" -ForegroundColor Green

Write-Host ""
Write-Host "=== Certificate Setup Complete ===" -ForegroundColor Cyan
Write-Host "  Thumbprint:  $($cert.Thumbprint)"
Write-Host "  PFX file:    $pfxPath"
Write-Host "  PFX password: WallpaperSyncDev"
Write-Host ""
Write-Host "Next step: Run build-identity-package.ps1 to build and sign the MSIX" -ForegroundColor Yellow
