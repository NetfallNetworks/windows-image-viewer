# MSIX Signing Strategy

The sparse MSIX identity package **must** be signed before it can be installed on Windows. This document describes the signing options for development and production.

## Development Signing (Self-Signed Certificate)

For local development and testing, use the included `create-dev-cert.ps1` script:

```powershell
# Run as Administrator
.\create-dev-cert.ps1
```

This creates:
- A self-signed certificate with Subject `CN=WallpaperSync`
- A PFX file at `installer/IdentityPackage/WallpaperSync-Dev.pfx`
- Installs the cert to the local machine's TrustedPeople store

**Limitations of self-signed certificates**:
- Only works on machines where the cert is installed to TrustedPeople
- Windows SmartScreen will warn on other machines
- Not suitable for distribution to end users

## Production Signing

For production releases distributed to end users, use one of the following options:

### Option A: Code Signing Certificate from a Certificate Authority

Purchase a standard code signing certificate from a trusted CA (e.g., DigiCert, Sectigo, GlobalSign).

**Requirements**:
- The certificate Subject must match the `Publisher` attribute in `AppxManifest.xml`
- Update the manifest's `Publisher` to match your certificate's Subject (e.g., `CN=Your Company, O=Your Company, L=City, S=State, C=US`)
- EV (Extended Validation) certificates are recommended for SmartScreen reputation

**Signing command**:
```powershell
.\build-identity-package.ps1 -PfxPath "path\to\production.pfx" -PfxPassword "your-password"
```

Or sign manually:
```powershell
signtool.exe sign /fd sha256 /a /f "path\to\production.pfx" /p "password" installer\WallpaperSync-Identity.msix
```

### Option B: Microsoft Store Submission

If the app is submitted to the Microsoft Store, Microsoft signs the package automatically.

**Pros**:
- No certificate purchase needed
- Automatic SmartScreen trust
- Automatic updates via Store

**Cons**:
- Requires Store account ($19 one-time fee for individuals)
- App must meet Store policies
- Would require converting from sparse MSIX to full MSIX packaging

This option is not currently in scope but may be revisited in the future.

## Security Requirements

- **Never commit certificate files to source control** (`.pfx`, `.cer`, `.p12`)
- The `.gitignore` file excludes these extensions
- Store production certificates in a secure vault (e.g., Azure Key Vault, AWS Secrets Manager)
- Use CI/CD secret management for automated signing in build pipelines
- Rotate certificates before expiration (typically 1-3 years for code signing certs)

## Updating the Publisher

If you change the signing certificate, you must update the `Publisher` attribute in `AppxManifest.xml` to match:

```xml
<Identity
  Name="WallpaperSync.WidgetProvider"
  Publisher="CN=Your New Subject Here"
  Version="1.0.0.0" />
```

The Publisher value must **exactly** match the certificate's Subject field. A mismatch will cause `signtool.exe` to fail.
