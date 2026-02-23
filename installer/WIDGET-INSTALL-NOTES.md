# Widget Installation & Registration Lifecycle

## Overview

The Wallpaper Sync widget requires two components beyond the main TrayApp:

1. **WallpaperApp.WidgetProvider.exe** — Out-of-process COM server that implements `IWidgetProvider`
2. **WallpaperSync-Identity.msix** — Sparse MSIX identity package that provides package identity for Widget Board registration

Both are shipped inside the MSI installer and installed to `%LOCALAPPDATA%\WallpaperSync\`.

## Install Flow

1. User runs `WallpaperSync-Setup.msi`
2. MSI installs:
   - `WallpaperApp.TrayApp.exe` → `%LOCALAPPDATA%\WallpaperSync\`
   - `WallpaperApp.WidgetProvider.exe` → `%LOCALAPPDATA%\WallpaperSync\`
   - `WallpaperSync-Identity.msix` → `%LOCALAPPDATA%\WallpaperSync\widget\`
   - `WallpaperApp.json` → `%LOCALAPPDATA%\WallpaperSync\`
3. User launches the TrayApp (automatically via installer checkbox or Start Menu shortcut)
4. On startup, `WidgetPackageRegistrar.RegisterIfNeededAsync()`:
   - Checks if the identity package is already registered (`PackageManager.FindPackagesForUser`)
   - If not registered, calls `PackageManager.AddPackageByUriAsync()` with `AllowExternalContent = true`
   - The sparse MSIX registers the COM server CLSID and widget definition with Widget Board
5. Widget appears in Win+W → "Add widgets" → "Wallpaper Sync"

## Uninstall Flow

1. User uninstalls via Settings → Apps or Control Panel
2. MSI removes all installed files:
   - `WallpaperApp.WidgetProvider.exe`
   - `WallpaperSync-Identity.msix`
   - `widget\` subdirectory
3. **Known gap**: The sparse MSIX registration is **not** automatically deregistered by the MSI uninstaller

## Deregistration Gap

The MSI uninstall removes files from disk but does **not** call `PackageManager.RemovePackageAsync()` to deregister the sparse MSIX identity package. This means:

- After MSI uninstall, the widget entry may linger in Widget Board until Windows detects the missing COM server
- The identity package remains registered in `Settings → Apps → Installed apps` (listed as "Wallpaper Sync Widget")

### Workarounds

**For users**: After uninstalling the MSI, manually remove the identity package:
- Open Settings → Apps → Installed apps
- Search for "Wallpaper Sync Widget"
- Click "..." → Uninstall

**For developers**: The TrayApp calls `PackageManager.RemovePackageAsync()` during graceful shutdown if the user explicitly requests widget deregistration from the settings UI.

### Future improvement

A WiX custom action could call `PackageManager.RemovePackageAsync()` during MSI uninstall. This requires a deferred custom action running managed code, which adds complexity. Deferred to a follow-up story.

## Certificate Requirements

The identity MSIX must be signed. See `installer/IdentityPackage/SIGNING.md` for certificate options:
- **Development**: Self-signed certificate (created by `create-dev-cert.ps1`)
- **Production**: Code signing certificate from a trusted CA

The signing certificate must be trusted on the target machine for `AddPackageByUriAsync()` to succeed.
