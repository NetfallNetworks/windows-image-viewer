# Widget Board Integration - Phase 2: Packaging & Identity

**Epic**: Widget Board Integration
**Priority**: HIGH
**Total Story Points**: 11
**Estimated Duration**: 2-3 days

---

## Phase Overview

Widget Board registration requires **package identity** — without it, Windows refuses to activate the COM server for the widget. This phase adds a sparse MSIX "identity package" that sits alongside the existing MSI installer: the MSI deploys the `WallpaperApp.WidgetProvider.exe` to disk, and the sparse MSIX provides the package identity and manifest declarations needed for Widget Board registration.

Phase 2 also wires up the IPC signal so the TrayApp notifies the widget provider instantly after each wallpaper update, instead of waiting for the next polling cycle.

**Key Objectives**:
1. Build a sparse MSIX identity package with `AppxManifest.xml` (COM server + widget definition)
2. Implement certificate signing strategy (self-signed for dev, production cert for release)
3. Register the sparse MSIX at first run via `PackageManager.AddPackageByUriAsync()`
4. Complete the named `EventWaitHandle` IPC so the TrayApp signals the widget on each update

---

## Story WS-18: Identity Package (Sparse MSIX)

**Story Points**: 8
**Type**: Feature / Packaging
**Priority**: CRITICAL

### Context

The Windows Widget Board only activates widget providers that have package identity. For apps distributed as Win32 MSIs (not MSIX Store packages), Microsoft supports a "sparse package" model: a minimal MSIX containing only an `AppxManifest.xml` with `AllowExternalContent="true"`. The manifest declares:
- The COM server class (so Widget Board can CoCreate the provider)
- The widget definition (name, description, supported sizes)

The sparse package does **not** contain the executable — it points to the externally-installed exe on disk. This keeps the existing MSI installer intact.

See ADR-007 for the architectural decision and trade-off analysis.

### Description

Create `installer/IdentityPackage/` containing the sparse MSIX build artifacts. The package is built by `makeappx.exe` (part of Windows SDK). A self-signed certificate is used for developer testing; the production signing strategy is documented but not automated in this story.

At first run of the TrayApp (or via an explicit "Register Widget" menu option), the app calls `PackageManager.AddPackageByUriAsync()` to register the sparse MSIX.

### Tasks

- [ ] Create `installer/IdentityPackage/AppxManifest.xml`:
  - `Identity` element: Name, Publisher, Version matching the app
  - `Properties > AllowExternalContent` = `true`
  - `Extensions > windows.comServer`: Class GUID matching `WallpaperImageWidgetProvider`
  - `Extensions > windows.widgetProvider`: widget ID, display name, description, sizes
- [ ] Create `installer/IdentityPackage/Assets/` with required logo images:
  - `Square44x44Logo.png`, `Square150x150Logo.png`, `StoreLogo.png`
  - Placeholder images acceptable for Phase 2; polished assets in a later story
- [ ] Create `installer/IdentityPackage/build-identity-package.ps1`:
  - Calls `makeappx.exe pack /d installer/IdentityPackage /p installer/WallpaperSync-Identity.msix`
  - Signs with self-signed cert: `signtool.exe sign /fd sha256 /a installer/WallpaperSync-Identity.msix`
- [ ] Create `installer/IdentityPackage/create-dev-cert.ps1`:
  - `New-SelfSignedCertificate` with Subject matching the manifest Publisher
  - Export to `installer/WallpaperSync-Dev.pfx` (`.gitignore` the pfx and cert files)
- [ ] Create `src/WallpaperApp.TrayApp/Services/WidgetPackageRegistrar.cs`:
  - `RegisterIfNeededAsync()` — checks if package is already registered, registers if not
  - Uses `Windows.Management.Deployment.PackageManager` via WinRT interop
  - Called from TrayApp startup after the main window is initialized
- [ ] Document production signing in `installer/IdentityPackage/SIGNING.md`:
  - Option A: Store-issued certificate (EV cert + Microsoft Store)
  - Option B: Code signing certificate from a CA
  - Mention that the pfx must not be committed to source control

### Files to Create

```
installer/IdentityPackage/AppxManifest.xml
installer/IdentityPackage/Assets/Square44x44Logo.png
installer/IdentityPackage/Assets/Square150x150Logo.png
installer/IdentityPackage/Assets/StoreLogo.png
installer/IdentityPackage/build-identity-package.ps1
installer/IdentityPackage/create-dev-cert.ps1
installer/IdentityPackage/SIGNING.md
src/WallpaperApp.TrayApp/Services/WidgetPackageRegistrar.cs
```

### Files to Modify

```
src/WallpaperApp.TrayApp/MainWindow.xaml.cs  (call WidgetPackageRegistrar.RegisterIfNeededAsync on startup)
.gitignore  (add *.pfx, *.cer, WallpaperSync-Identity.msix)
```

### Implementation Notes

**AppxManifest.xml skeleton**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"
  xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10"
  IgnorableNamespaces="uap3 com">

  <Identity
    Name="WallpaperSync.WidgetProvider"
    Publisher="CN=WallpaperSync"
    Version="1.0.0.0" />

  <Properties>
    <DisplayName>Wallpaper Sync Widget</DisplayName>
    <PublisherDisplayName>WallpaperSync</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <AllowExternalContent>true</AllowExternalContent>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="WallpaperSyncWidgetProvider" Executable="..\..\..\..\WallpaperApp.WidgetProvider\WallpaperApp.WidgetProvider.exe" EntryPoint="Windows.FullTrustApplication">
      <uap3:VisualElements DisplayName="Wallpaper Sync" Description="Displays your Wallpaper Sync image" BackgroundColor="transparent" Square44x44Logo="Assets\Square44x44Logo.png" Square150x150Logo="Assets\Square150x150Logo.png" />
      <Extensions>
        <com:Extension Category="windows.comServer">
          <com:ComServer>
            <com:ExeServer Executable="..\..\..\..\WallpaperApp.WidgetProvider\WallpaperApp.WidgetProvider.exe" Arguments="-RegisterProcessAsComServer" DisplayName="Wallpaper Sync Widget Provider">
              <com:Class Id="<!-- GUID from WS-15 -->" DisplayName="WallpaperImageWidgetProvider" />
            </com:ExeServer>
          </com:ComServer>
        </com:Extension>
        <uap3:Extension Category="windows.appExtension">
          <uap3:AppExtension Name="com.microsoft.windows.widgets" DisplayName="Wallpaper Sync" Id="WallpaperSync.Widget" PublicFolder="Public">
            <uap3:Properties>
              <WidgetProvider>
                <ProviderIcons>
                  <Icon Path="Assets\Square44x44Logo.png" />
                </ProviderIcons>
                <Activation>
                  <CreateInstance ClassId="<!-- GUID from WS-15 -->" />
                </Activation>
                <Definitions>
                  <Definition Id="WallpaperSync_Image"
                    DisplayName="Wallpaper Sync"
                    Description="Shows your synchronized wallpaper image"
                    AllowMultiple="false">
                    <Capabilities>
                      <Capability Type="size" Name="small" />
                      <Capability Type="size" Name="medium" />
                      <Capability Type="size" Name="large" />
                    </Capabilities>
                  </Definition>
                </Definitions>
              </WidgetProvider>
            </uap3:Properties>
          </uap3:AppExtension>
        </uap3:Extension>
      </Extensions>
    </Application>
  </Applications>
</Package>
```

**WidgetPackageRegistrar.cs** key logic:
```csharp
var packageManager = new PackageManager();
var existing = packageManager.FindPackagesForUser("", "WallpaperSync.WidgetProvider_*").FirstOrDefault();
if (existing == null)
{
    string msixPath = Path.Combine(AppContext.BaseDirectory, "WallpaperSync-Identity.msix");
    var result = await packageManager.AddPackageByUriAsync(
        new Uri(msixPath),
        new AddPackageOptions { AllowUnsigned = false, ExternalLocationUri = new Uri(AppContext.BaseDirectory) });
    // handle result
}
```

### Acceptance Criteria

- [ ] `AppxManifest.xml` is valid (passes `makeappx.exe` without errors)
- [ ] `build-identity-package.ps1` produces `WallpaperSync-Identity.msix`
- [ ] `create-dev-cert.ps1` creates and installs a self-signed cert to TrustedPeople store
- [ ] Signed MSIX installs successfully: `Add-AppxPackage WallpaperSync-Identity.msix`
- [ ] After install, widget appears in Win+W "Add widgets" panel (manual test)
- [ ] `WidgetPackageRegistrar.RegisterIfNeededAsync()` skips registration if package already installed
- [ ] `.gitignore` excludes certificate and built MSIX artifacts
- [ ] `SIGNING.md` documents production cert options

### Testing Requirements

Unit-testable surface is limited (PackageManager is a WinRT API). Focus on integration testing:

**Manual Integration Tests**:
1. Run `create-dev-cert.ps1` — cert appears in `certmgr.msc > TrustedPeople`
2. Run `build-identity-package.ps1` — MSIX produced without error
3. Install MSIX — no error in Apps & Features
4. Open Win+W → "Add widgets" — "Wallpaper Sync" widget appears in the list
5. Pin widget — widget renders (uses data from Phase 1)
6. Run TrayApp — `WidgetPackageRegistrar` detects existing package and skips re-registration

**Unit Tests** (WidgetPackageRegistrarTests.cs):
```csharp
[Fact]
public async Task RegisterIfNeededAsync_AlreadyRegistered_SkipsRegistration()
// Mock IPackageManager to return existing package

[Fact]
public async Task RegisterIfNeededAsync_MsixNotFound_LogsErrorGracefully()
// Verify it does not throw; logs warning
```

### Definition of Done

- [x] MSIX builds and installs without signing errors
- [x] Widget visible in Win+W "Add widgets" panel
- [x] COM server correctly activated by Widget Board
- [x] `WidgetPackageRegistrar` handles already-registered case
- [x] Certificate files excluded from source control
- [x] `SIGNING.md` reviewed and complete

---

## Story WS-19: TrayApp ↔ Widget IPC

**Story Points**: 3
**Type**: Feature / Integration
**Priority**: HIGH

### Context

Currently the widget provider only refreshes on the periodic 30-second polling timer (stub from WS-17). When the TrayApp updates the wallpaper, the widget should reflect the new image immediately — not after the next poll. A lightweight IPC mechanism allows the TrayApp to signal the widget provider without a dependency between the two processes.

### Description

Complete the named `EventWaitHandle` IPC. The TrayApp signals a named `EventWaitHandle` after every successful wallpaper update. The widget provider's background thread (stubbed in WS-17) wakes up on this signal and calls `PushUpdateToAllWidgets()`.

The EventWaitHandle name is a shared constant defined in `WallpaperApp.Core` so both processes use the same name without hardcoding strings.

### Tasks

- [ ] Add `WidgetIpcConstants.cs` to `WallpaperApp.Core`:
  - `public const string WidgetRefreshEventName = @"Global\WallpaperSyncWidgetRefresh";`
- [ ] Update `WallpaperApp.TrayApp`:
  - After `WallpaperUpdater.UpdateWallpaperAsync()` returns `true`, open/signal the named event:
    ```csharp
    if (EventWaitHandle.TryOpenExisting(WidgetIpcConstants.WidgetRefreshEventName, out var handle))
        handle.Set();
    ```
  - Use `TryOpenExisting` (not `new EventWaitHandle`) — if the widget provider is not running, signal is silently dropped (no error)
- [ ] Complete `WallpaperApp.WidgetProvider` background listener (stub from WS-17):
  - Replace stub with real implementation using `EventWaitHandle` and a `CancellationToken`
  - On signal: call `PushUpdateToAllWidgets()`
  - On cancellation: exit the background thread cleanly
- [ ] Add `WidgetIpcService.cs` to `WallpaperApp.WidgetProvider`:
  - Encapsulates the EventWaitHandle listener loop
  - Injected into `Program.cs` and started on boot
  - Disposable — stops the background thread on dispose

### Files to Create

```
src/WallpaperApp.Core/WidgetIpcConstants.cs
src/WallpaperApp.WidgetProvider/WidgetIpcService.cs
```

### Files to Modify

```
src/WallpaperApp.TrayApp/MainWindow.xaml.cs  (signal after successful wallpaper update)
src/WallpaperApp.WidgetProvider/Program.cs   (start WidgetIpcService)
src/WallpaperApp.WidgetProvider/WallpaperImageWidgetProvider.cs  (receive IPC-triggered refresh)
```

### Implementation Notes

**Why `Global\` prefix**: The `Global\` prefix makes the EventWaitHandle accessible across session boundaries (needed when TrayApp and Widget Provider may run in different Windows sessions). On Windows 10/11 desktop this is the same user session, but using `Global\` is more robust.

**Why signal-only (not request/response)**: The widget provider will re-read config and state on each push. No data needs to pass through the IPC channel — the signal just says "go refresh now." This keeps the IPC completely stateless and failure-tolerant.

**Fallback**: If the widget provider is not running (e.g., widget was never pinned), `TryOpenExisting` returns `false` and the signal is silently dropped. This is correct behavior — no widget to update.

**Polling fallback**: The 30-second polling timer in the widget provider remains active as a safety net for cases where the IPC signal is missed (process restart, race condition, etc.).

### Acceptance Criteria

- [ ] `WidgetIpcConstants.WidgetRefreshEventName` defined in Core
- [ ] TrayApp signals the event after every successful `UpdateWallpaperAsync()`
- [ ] Signaling when widget provider is not running does not throw or log an error
- [ ] Widget provider wakes within 500 ms of signal and pushes updated card
- [ ] Widget provider background thread exits cleanly on process shutdown
- [ ] All new unit tests pass

### Testing Requirements

```csharp
// WidgetIpcServiceTests.cs
[Fact]
public async Task Start_WhenSignalReceived_InvokesCallback()
// Create EventWaitHandle with same name, signal it, verify callback called

[Fact]
public async Task Start_WhenCancelled_ExitsCleanly()
// Cancel the CancellationToken, verify no hang or exception

[Fact]
public void Dispose_StopsBackgroundThread()

// TrayApp IPC tests (unit)
[Fact]
public void SignalWidgetRefresh_HandleNotOpen_DoesNotThrow()
// Verify TryOpenExisting path is safe when widget provider is not running

[Fact]
public void SignalWidgetRefresh_HandleOpen_SetsEvent()
// Mock EventWaitHandle, verify Set() called
```

### Definition of Done

- [x] All unit tests pass (5+ new tests)
- [x] Manual test: update wallpaper via "Refresh Now" in TrayApp → widget updates within 1 second
- [x] Manual test: widget provider not running → TrayApp refresh completes without error
- [x] Background thread exits cleanly on process termination
- [x] No compiler warnings

---

## Phase 2 Complete Checklist

When all 2 stories are done:

- [ ] Sparse MSIX identity package builds and installs on clean Windows 11 machine
- [ ] Widget appears in Win+W "Add widgets" panel after package install
- [ ] Widget displays live wallpaper image after being pinned
- [ ] TrayApp "Refresh Now" triggers immediate widget card update (< 1 second)
- [ ] Polling fallback still works when IPC signal is missed
- [ ] Certificate build artifacts excluded from source control
- [ ] All unit tests pass (10+ new tests in phase)
- [ ] No compiler warnings
- [ ] Ready for Phase 3 (installer and pipeline integration)

---

**END OF PHASE 2 STORIES**
