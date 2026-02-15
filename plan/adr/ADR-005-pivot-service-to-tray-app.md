# ADR-005: Pivot from Windows Service to System Tray Application

**Status**: Accepted
**Date**: 2026-02-15
**Deciders**: Project team
**Supersedes**: Story 7 implementation (Windows Service approach)

## Context

After successfully implementing Stories 1-7 (including Windows Service conversion in Story 7), we encountered a critical usability issue with Windows Services on modern Windows systems.

### The Problem with Windows Services

Windows Services running as user accounts require password authentication during installation and service configuration. This creates several problems:

**Authentication conflicts with modern Windows**:
- Windows Hello (facial recognition)
- Fingerprint readers
- PIN codes
- Microsoft account passwordless login

**Password requirement issues**:
- Service installation scripts prompt for the user's Windows password
- Many users don't have a traditional password (using Hello/PIN instead)
- Users are hesitant to enter passwords in PowerShell scripts
- Authentication failures occur even with correct passwords

**User session limitations**:
- Services running as LocalSystem cannot interact with the user desktop effectively
- Setting wallpapers requires user session context
- Notifications and UI feedback are difficult from service context

### Requirements That Led to This Decision

Our application needs to:
- Update desktop wallpaper (requires user session context)
- Start automatically on user login
- Run in the background without user intervention
- Be easy to install without complex authentication
- Work with modern Windows authentication methods (Hello, fingerprint, PIN)

### Alternatives Considered

1. **Windows Service (original approach - Story 7)**
   - Pros: Runs at system startup before user login, true background service
   - Cons: Requires password, can't interact with user desktop reliably, complex installation

2. **Scheduled Task**
   - Pros: No password required, runs as current user
   - Cons: Limited lifecycle control, harder to manage state, no clean shutdown

3. **System Tray Application** ✅
   - Pros: Runs in user session, no password needed, easy UI control, auto-start via Startup folder
   - Cons: Only runs when user is logged in (acceptable for wallpaper app)

4. **Console App with Task Scheduler**
   - Pros: Simple, no UI needed
   - Cons: No user feedback, harder to debug, scheduled task complexity

## Decision

**Replace the Windows Service architecture with a System Tray Application (WPF + Windows Forms NotifyIcon).**

### Implementation Approach

**Create new project**: `WallpaperApp.TrayApp`
- Technology: WPF application with Windows Forms NotifyIcon for tray icon
- Architecture: Self-contained executable with system tray presence
- Auto-start: Shortcut placed in user's Startup folder (`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`)
- User interface: Context menu on tray icon (Refresh Now, Status, Open Image Folder, About, Exit)

**Reuse core services**: Reference existing `WallpaperApp` project for:
- `IConfigurationService` - Configuration loading from JSON
- `IWallpaperService` - Wallpaper setting via SystemParametersInfo Win32 API
- `IImageFetcher` - HTTP image downloading
- `IWallpaperUpdater` - Orchestration logic

**Remove service infrastructure**:
- Delete `Worker.cs` (BackgroundService implementation)
- Remove `CreateHostBuilder` from `Program.cs`
- Remove `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Hosting.WindowsServices` packages
- Delete PowerShell service installation scripts (install-service.ps1, uninstall-service.ps1, etc.)
- Remove SERVICE-INFO.md documentation

**New installation method**:
- `publish-tray-app.ps1` - Builds tray app as self-contained single-file executable
- `install-tray-app.ps1` - Copies to %LOCALAPPDATA%\WeatherWallpaper, creates startup shortcut
- `uninstall-tray-app.ps1` - Clean removal of app and startup shortcut

### What Changes vs. What Stays

**Removed**:
- `Worker.cs` and `WorkerTests.cs` (BackgroundService infrastructure)
- Service-specific hosting code in Program.cs
- 6 PowerShell service management scripts
- SERVICE-INFO.md documentation
- Windows Service documentation sections in README

**Preserved**:
- All core service classes (Configuration, Wallpaper, ImageFetcher, WallpaperUpdater)
- All existing tests for core services
- CLI modes: `--help`, `--download`, `<path-to-image>` (for debugging/dev use)
- Configuration file format (WallpaperApp.json with AppSettings section)
- Logging infrastructure

**New**:
- WallpaperApp.TrayApp project (WPF application with NotifyIcon)
- System tray icon with right-click context menu
- Auto-startup via Startup folder shortcut
- Tray app installation/uninstallation scripts in `/scripts` directory
- TRAY-APP-README.md documentation

## Consequences

### Positive

1. **No password required**: Works seamlessly with Windows Hello, fingerprint, PIN, and passwordless Microsoft accounts

2. **Better user experience**:
   - Visible tray icon shows app is running
   - Right-click menu for immediate control (Refresh Now, Status, Exit)
   - Double-click for status window
   - Can show notifications on wallpaper updates (future enhancement)

3. **Simpler installation**:
   - No administrator privileges required
   - No service registration complexity
   - No "Log on as a service" rights needed
   - No password prompts

4. **Runs in user session**: Perfect for wallpaper changes - guaranteed desktop access

5. **Easier debugging**: Console output visible, can attach debugger easily, errors visible to user

6. **Modern UX**: Matches user expectations for utility apps (similar to Dropbox, OneDrive, Discord)

### Negative

1. **Requires user login**: App only runs when user is logged in
   - **Mitigation**: Wallpaper apps inherently require a logged-in user to see wallpaper; this is not a real limitation

2. **Not truly "background"**: Appears in system tray (minimally visible)
   - **Mitigation**: This is actually a positive - users can see it's running and control it

3. **Doesn't survive logoff/switch user**: Stops when user logs out
   - **Mitigation**: Wallpaper is user-specific anyway; auto-restarts on next login

4. **Code duplication**: Timer logic duplicated between Worker.cs (old) and TrayApp (new)
   - **Mitigation**: Worker.cs is being deleted; no long-term duplication

### Trade-offs Accepted

| Aspect | Windows Service | Tray App (Chosen) |
|--------|----------------|-------------------|
| Auto-start timing | System boot | User login |
| Password required | Yes | No ✅ |
| User session access | Limited | Full ✅ |
| Installation complexity | High | Low ✅ |
| User control | services.msc | Tray icon ✅ |
| Modern auth support | Poor | Excellent ✅ |
| Debugging ease | Difficult | Easy ✅ |
| Runs without login | Yes | No (acceptable) |

**Verdict**: For a wallpaper application, the tray app approach is strictly superior. The Windows Service architecture was over-engineering for this use case.

## Implementation Validation

### What Was Built

Tray app implementation includes:
- WPF application with Windows Forms NotifyIcon integration
- Auto-startup via Startup folder shortcut (no registry modification needed)
- DispatcherTimer-based wallpaper refresh (configurable interval)
- Context menu: Refresh Now, Status, Open Image Folder, About, Exit
- Mutex-based single-instance prevention (prevents multiple instances)
- Clean resource disposal and shutdown
- PowerShell installation/uninstallation scripts in `/scripts` directory

### Testing Performed

- Manual testing on Windows 10 and Windows 11
- Verified auto-startup after reboot
- Tested with Windows Hello (no password) authentication
- Confirmed wallpaper updates work reliably
- Installation/uninstallation tested on clean machines
- Verified single-instance mutex prevents duplicate processes

### Migration Path

For users who installed the Windows Service version:
1. Uninstall service: Run `uninstall-service.ps1`
2. Pull latest code (service code will be removed)
3. Install tray app: Run `scripts\install-tray-app.ps1`

No data migration needed - configuration file format is unchanged (WallpaperApp.json).

## When to Reconsider

This decision should be reconsidered if:

1. **Requirement changes**: App needs to run without user login (unlikely for wallpaper application)
2. **Enterprise deployment**: Need system-wide deployment before user login (corporate environments)
3. **Multi-user scenarios**: Need to serve multiple users on same machine simultaneously

For the current use case (single-user desktop wallpaper application), the tray app approach is optimal and superior to the service approach.

## References

- [STORY_MAP.md](../STORY_MAP.md) - Story 7 (Windows Service implementation, now deprecated)
- [TRAY-APP-README.md](../../TRAY-APP-README.md) - Tray app user guide
- [Windows Hello authentication](https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/)
- Git history: Service implementation commits now superseded by tray app

## Related ADRs

- **ADR-001**: Use SystemParametersInfo for Wallpaper Changes (unchanged - still applies to tray app)
- **ADR-002**: Self-Contained Deployment Model (unchanged - applies to tray app too)
- **ADR-003**: No Retry Logic for Transient Failures (unchanged - still applies)
- **ADR-004**: Use appsettings.json for Configuration (unchanged - WallpaperApp.json still used)
