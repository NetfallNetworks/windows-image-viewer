# Widget Board Integration - Phase 3: Integration & Pipeline

**Epic**: Widget Board Integration
**Priority**: HIGH
**Total Story Points**: 11
**Estimated Duration**: 2-3 days

---

## Phase Overview

Phase 3 ties everything together: the installer ships the WidgetProvider executable and identity package, the build pipeline produces all artifacts without manual steps, and unit tests cover the widget's testable logic so they run on Linux CI alongside the existing 94 tests.

At the end of Phase 3, a fresh install from the MSI results in a fully functional widget visible in Win+W with no additional user steps.

**Key Objectives**:
1. Update `Package.wxs` to include WidgetProvider exe and identity MSIX
2. Update `build.bat` (Windows) and `build.sh` (Linux) with widget-related build steps
3. Add unit tests for the widget provider's testable logic (Adaptive Cards, data binding, instance tracking)

---

## Story WS-20: Installer Integration

**Story Points**: 5
**Type**: Feature / Packaging
**Priority**: HIGH

### Context

The existing WiX MSI (`installer/Package.wxs`) ships the TrayApp executable and sets up the startup shortcut. It has no knowledge of the WidgetProvider executable or the identity MSIX. After this story, the MSI installs all widget components and the TrayApp's `WidgetPackageRegistrar` handles Widget Board registration at first run.

### Description

Update `installer/Package.wxs` to include:
1. `WallpaperApp.WidgetProvider.exe` in the installation directory
2. `WallpaperSync-Identity.msix` in a `widget\` subdirectory
3. The identity MSIX is installed (via the TrayApp's `WidgetPackageRegistrar` at first run) — no WiX custom action needed

### Tasks

- [ ] Update `Package.wxs`:
  - Add `Component` for `WallpaperApp.WidgetProvider.exe` in the main install directory
  - Create a `widget\` subdirectory under the install root
  - Add `Component` for `WallpaperSync-Identity.msix` in the `widget\` subdirectory
  - Add both components to the main `Feature`
- [ ] Verify WiX build succeeds: `dotnet tool run wix build installer/Package.wxs ...`
- [ ] Update `installer/Package.wxs` version string to reflect widget support
- [ ] Manual install test: MSI installs → WidgetProvider exe present on disk → identity MSIX present in `widget\` folder
- [ ] Verify uninstall: MSI uninstall removes all new files (WidgetProvider exe + identity MSIX)
  - Note: MSI uninstall does not deregister the sparse MSIX. TrayApp unregisters via `PackageManager.RemovePackageAsync()` before exit (or add a WiX custom action in a follow-up story)
- [ ] Document MSIX deregistration behavior in `SIGNING.md` or a separate `WIDGET-INSTALL-NOTES.md`

### Files to Modify

```
installer/Package.wxs  (add WidgetProvider.exe and Identity.msix components)
```

### Files to Create

```
installer/WIDGET-INSTALL-NOTES.md  (documents registration/deregistration lifecycle)
```

### Implementation Notes

**WiX component for WidgetProvider exe** (add inside the existing `DirectoryRef` or `Directory`):
```xml
<Component Id="WidgetProviderComponent" Guid="<!-- new GUID -->">
  <File Id="WidgetProviderExe"
        Source="$(var.WidgetProviderDir)\WallpaperApp.WidgetProvider.exe"
        KeyPath="yes" />
</Component>
```

**WiX subdirectory for identity MSIX**:
```xml
<Directory Id="WidgetDir" Name="widget">
  <Component Id="WidgetIdentityComponent" Guid="<!-- new GUID -->">
    <File Id="WidgetIdentityMsix"
          Source="$(var.InstallerDir)\WallpaperSync-Identity.msix"
          KeyPath="yes" />
  </Component>
</Directory>
```

**Variable definitions** (add to `Package.wxs` or a `.wxi` include file):
```xml
<?define WidgetProviderDir="$(sys.SOURCEFILEDIR)\..\bin\WidgetProvider" ?>
<?define InstallerDir="$(sys.SOURCEFILEDIR)" ?>
```

**Deregistration gap**: MSI uninstall will remove files but won't call `PackageManager.RemovePackageAsync()`. The sparse MSIX will remain registered even after MSI uninstall. A clean deregistration requires either:
- A WiX custom action calling `PackageManager.RemovePackageAsync()` (complex, deferred to a follow-up)
- Documenting that users should manually remove "Wallpaper Sync Widget" from Settings > Apps (acceptable for now)

### Acceptance Criteria

- [ ] `Package.wxs` builds without WiX errors
- [ ] Installed MSI places `WallpaperApp.WidgetProvider.exe` in installation directory
- [ ] Installed MSI places `WallpaperSync-Identity.msix` in `widget\` subdirectory
- [ ] MSI uninstall removes both new files
- [ ] `WIDGET-INSTALL-NOTES.md` documents the registration lifecycle gap
- [ ] No regressions in existing MSI functionality (TrayApp install, startup shortcut)

### Testing Requirements

**Manual Installation Test Checklist**:
1. Build MSI: `dotnet tool run wix build installer/Package.wxs ...`
2. Install MSI on clean Windows 11 VM
3. Verify: `%LOCALAPPDATA%\WallpaperSync\WallpaperApp.WidgetProvider.exe` present
4. Verify: `%LOCALAPPDATA%\WallpaperSync\widget\WallpaperSync-Identity.msix` present
5. Start TrayApp — `WidgetPackageRegistrar` registers the identity MSIX
6. Open Win+W → "Add widgets" → "Wallpaper Sync" widget appears
7. Uninstall MSI — verify both files removed
8. Verify existing TrayApp functionality unaffected

### Definition of Done

- [x] MSI build succeeds with zero WiX errors
- [x] All new files installed and uninstalled correctly
- [x] Existing install/uninstall behavior unchanged (regression check)
- [x] `WIDGET-INSTALL-NOTES.md` reviewed and committed
- [x] No compiler warnings in any component

---

## Story WS-21: Build Pipeline Updates

**Story Points**: 3
**Type**: Feature / DevOps
**Priority**: HIGH

### Context

The current `scripts/build.bat` (Windows) and `scripts/build.sh` (Linux) build and publish the TrayApp and console app respectively. Neither knows about the WidgetProvider project or the identity MSIX. After this story, both scripts produce all required artifacts in the correct sequence without manual intervention.

### Description

Update both build scripts to:
1. Build and publish `WallpaperApp.WidgetProvider` (Windows only — it references Windows App SDK)
2. Build the identity MSIX (Windows only — requires `makeappx.exe`)
3. Sign the identity MSIX (Windows only — requires `signtool.exe`)
4. Linux script skips widget steps with a clear `[SKIPPED on Linux]` message

### Tasks

- [ ] Update `scripts/build.bat`:
  - Add Step 5: Publish WidgetProvider (`dotnet publish src/WallpaperApp.WidgetProvider/... -o bin/WidgetProvider`)
  - Add Step 6: Build identity MSIX (`powershell -File installer/IdentityPackage/build-identity-package.ps1`)
  - Add success/failure reporting for new steps in the summary block
- [ ] Update `scripts/build.sh`:
  - Add clear `[SKIPPED on Linux]` messages for widget steps (3b, 4, 5, 6)
  - Verify `WallpaperApp.Core` and `WallpaperApp.Tests` still build cleanly
  - Note: `WallpaperApp.WidgetProvider` references `net8.0-windows` — exclude from Linux build via `dotnet build src/... --no-restore` on Core + Tests only
- [ ] Update `scripts/build.bat` summary block:
  ```
  [OK] Widget provider published to .\bin\WidgetProvider\
  [OK] Identity package built: .\installer\WallpaperSync-Identity.msix
  ```
- [ ] Update `scripts/build.sh` summary block to note what was skipped
- [ ] Verify: `./scripts/build.sh` still passes all 94+ tests on Linux with no errors
- [ ] Verify: `cmd.exe /c scripts\\build.bat` completes successfully on Windows (manual)

### Files to Modify

```
scripts/build.bat   (add Steps 5–6 for widget provider and MSIX)
scripts/build.sh    (add skip messages for widget steps)
```

### Implementation Notes

**build.bat new steps** (append before the summary block):

```batch
echo.
echo Step 5: Publishing Widget Provider...
dotnet publish src\WallpaperApp.WidgetProvider\WallpaperApp.WidgetProvider.csproj ^
  -c Release -o bin\WidgetProvider --self-contained true --runtime win-x64 ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  --verbosity minimal --nologo
if errorlevel 1 (
    echo [FAILED] Widget provider publish failed
    exit /b 1
)
echo [OK] Widget provider published

echo.
echo Step 6: Building identity MSIX...
powershell -ExecutionPolicy Bypass -File installer\IdentityPackage\build-identity-package.ps1
if errorlevel 1 (
    echo [FAILED] Identity MSIX build failed
    exit /b 1
)
echo [OK] Identity MSIX built: installer\WallpaperSync-Identity.msix
```

**build.sh note about widget**:
```bash
echo ""
echo "[SKIPPED on Linux] Step 5: Widget provider (Windows App SDK required)"
echo "[SKIPPED on Linux] Step 6: Identity MSIX (makeappx.exe Windows-only)"
```

**Conditional WidgetProvider in solution build**: The WidgetProvider targets `net8.0-windows10.0.19041.0`. On Linux, `dotnet build WallpaperApp.sln` will fail because of the Windows-only target. The solution file should be updated to conditionally exclude the WidgetProvider project on non-Windows platforms, OR the Linux build script should continue to build individual projects (Core + Tests) rather than the full solution. The Linux build script already uses this individual-project approach — no change needed to the script structure.

### Acceptance Criteria

- [ ] `build.bat` produces `bin/WidgetProvider/WallpaperApp.WidgetProvider.exe`
- [ ] `build.bat` produces `installer/WallpaperSync-Identity.msix`
- [ ] `build.sh` runs to completion with `[SKIPPED]` messages for widget steps
- [ ] `build.sh` all 94+ tests pass (no regressions)
- [ ] Both scripts' summary blocks accurately reflect new steps
- [ ] No existing build steps broken

### Testing Requirements

**Linux (automated)**:
```bash
./scripts/build.sh
# Expected output includes:
# ✅ All tests passed (94/94)  [or more if WS-22 adds new tests]
# [SKIPPED on Linux] Step 5: Widget provider
# [SKIPPED on Linux] Step 6: Identity MSIX
```

**Windows (manual)**:
```batch
cmd.exe /c scripts\\build.bat
:: Expected summary includes:
:: [OK] Widget provider published to .\bin\WidgetProvider\
:: [OK] Identity package built: .\installer\WallpaperSync-Identity.msix
```

### Definition of Done

- [x] `build.sh` still passes all tests on Linux
- [x] `build.bat` produces all 6 artifacts (build, test, console, tray, widget, MSIX)
- [x] Summary blocks accurate and consistent between platforms
- [x] No compiler warnings in any build step

---

## Story WS-22: Widget Provider Tests

**Story Points**: 3
**Type**: Feature / Testing
**Priority**: HIGH

### Context

`WallpaperApp.WidgetProvider` is a Windows-only project, but many of its components are unit-testable without Windows App SDK runtime: `CardTemplateService`, `WidgetData` construction, `WidgetInstanceTracker`, and the IPC signal path. These tests must run on Linux CI (no Windows App SDK present). Tests that require the actual `WidgetManager` API are manual-only and documented in the phase story checklists.

### Description

Add unit tests for the testable components of `WallpaperApp.WidgetProvider` to `WallpaperApp.Tests`. Tests must not reference the Windows App SDK directly (mock or abstract `WidgetManager` interactions). The test project already targets `net8.0` and runs on Linux — the new tests must maintain this.

### Tasks

- [ ] Add `WidgetProvider/CardTemplateServiceTests.cs` (from WS-16 test list — formalize):
  - `LoadTemplate_Small/Medium/Large_ReturnsNonEmptyJson`
  - `HydrateTemplate_SubstitutesAllPlaceholders`
  - `HydrateTemplate_LocalFileMode_ProducesValidCard`
- [ ] Add `WidgetProvider/WidgetInstanceTrackerTests.cs` (from WS-17 test list — formalize):
  - `AddWidget_ThenGetAll_ReturnsAddedInstance`
  - `RemoveWidget_ThenGetAll_ExcludesRemovedInstance`
  - `GetWidget_UnknownId_ReturnsNull`
  - `IsThreadSafe_ConcurrentAddRemove_DoesNotThrow`
- [ ] Add `WidgetProvider/WidgetDataTests.cs`:
  - `BuildWidgetData_UrlMode_UsesImageUrl`
  - `BuildWidgetData_LocalFileMode_UsesPlaceholder`
  - `BuildWidgetData_StateDisabled_ShowsPausedStatus`
  - `BuildWidgetData_NeverUpdated_ShowsNeverString`
  - `BuildWidgetData_LastUpdateTime_FormatsCorrectly`
- [ ] Add `WidgetProvider/WidgetIpcServiceTests.cs` (from WS-19 test list — formalize):
  - `Start_WhenSignalReceived_InvokesCallback`
  - `Start_WhenCancelled_ExitsCleanly`
  - `Dispose_StopsBackgroundThread`
- [ ] Verify: `dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj` runs all new tests on Linux
- [ ] Verify total test count increases from 94 to 94+N (document exact count in build summary)

### Files to Create

```
src/WallpaperApp.Tests/WidgetProvider/CardTemplateServiceTests.cs
src/WallpaperApp.Tests/WidgetProvider/WidgetInstanceTrackerTests.cs
src/WallpaperApp.Tests/WidgetProvider/WidgetDataTests.cs
src/WallpaperApp.Tests/WidgetProvider/WidgetIpcServiceTests.cs
```

### Implementation Notes

**Keeping tests cross-platform**: `CardTemplateService` reads embedded resources from `WallpaperApp.WidgetProvider` assembly. For tests to work on Linux, either:
- Option A: Copy the card template JSON files into the test project as well (duplication, not preferred)
- Option B: Reference `WallpaperApp.WidgetProvider` assembly in the test project — this only works if the WidgetProvider project targets a framework runnable on Linux. Since WidgetProvider targets `net8.0-windows`, tests must **not** reference the assembly directly.
- **Option C (recommended)**: Extract `CardTemplateService` and `WidgetData` into a new `WallpaperApp.WidgetProvider.Core` library targeting `net8.0` (no Windows constraint). The main `WallpaperApp.WidgetProvider` exe references this library. The test project references `WallpaperApp.WidgetProvider.Core`.

This refactor should be done as part of WS-22 (not a separate story — it's internal to making the tests work).

**Alternatively** (simpler, no new project): Inline the card JSON templates as test fixtures in the test project's `TestData/` folder and test `CardTemplateService` by constructing it with a factory method that accepts a path override. This avoids the cross-platform assembly reference problem entirely.

The implementer should choose the approach that minimizes complexity while keeping all tests runnable on Linux.

### Acceptance Criteria

- [ ] All new tests pass when run on Linux: `dotnet test src/WallpaperApp.Tests/...`
- [ ] No Windows App SDK references in the test project
- [ ] Total test count is documented (was 94, now 94+N)
- [ ] `CardTemplateService` tests cover all three sizes and LocalFile mode
- [ ] `WidgetInstanceTracker` thread safety test runs without flakiness
- [ ] `WidgetIpcService` tests complete within 5 seconds (no blocking waits)

### Testing Requirements

All tests listed in the Tasks section above. Representative examples:

```csharp
// CardTemplateServiceTests.cs
[Theory]
[InlineData(WidgetSize.Small)]
[InlineData(WidgetSize.Medium)]
[InlineData(WidgetSize.Large)]
public void LoadTemplate_ReturnsNonEmptyJson(WidgetSize size)
{
    var service = new CardTemplateService();
    var json = service.LoadTemplate(size);
    Assert.NotEmpty(json);
    Assert.Contains("AdaptiveCard", json);
}

[Fact]
public void HydrateTemplate_SubstitutesImageUrl()
{
    var service = new CardTemplateService();
    var template = service.LoadTemplate(WidgetSize.Medium);
    var data = new WidgetData("https://example.com/image.png", "Today 12:00", "Active", true);
    var result = service.HydrateTemplate(template, data);
    Assert.Contains("https://example.com/image.png", result);
    Assert.DoesNotContain("${imageUrl}", result);
}

// WidgetDataTests.cs
[Fact]
public void BuildWidgetData_LocalFileMode_UsesPlaceholderUrl()
{
    var settings = new AppSettings { SourceType = ImageSource.LocalFile, LocalImagePath = @"C:\image.png" };
    var state = new AppState { IsEnabled = true };
    var data = WidgetData.From(settings, state);
    Assert.DoesNotContain("file://", data.ImageUrl);
    Assert.Contains("placeholder", data.ImageUrl, StringComparison.OrdinalIgnoreCase);
}
```

### Definition of Done

- [x] All new unit tests pass on Linux with zero failures
- [x] Test count increased from 94 (document exact new total)
- [x] No Windows-specific code in tests
- [x] Tests complete in < 10 seconds total
- [x] `build.sh` success output updated to show new test count

---

## Phase 3 Complete Checklist

When all 3 stories are done:

- [ ] MSI ships all widget artifacts (WidgetProvider exe + identity MSIX)
- [ ] `build.bat` produces all artifacts end-to-end without manual steps
- [ ] `build.sh` runs all tests on Linux (94+N passing, zero failures)
- [ ] Fresh MSI install → widget visible in Win+W (full end-to-end manual test)
- [ ] Existing wallpaper functionality unchanged (regression test)
- [ ] All unit tests pass (94+N total, documented in summary)
- [ ] No compiler warnings in any component
- [ ] All planning documents committed

---

**END OF PHASE 3 STORIES**
