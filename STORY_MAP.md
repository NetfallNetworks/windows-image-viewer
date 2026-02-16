# Windows Wallpaper Sync - Story Map

> **Note**: This project was rebranded from "Weather Wallpaper" to "Wallpaper Sync" in Phase 2 to reflect its general-purpose nature. See `plan/wallpaper-sync-phase-2-stories.md` for details. Historical story descriptions below retain original "Weather" references for context.

## Project Overview

**Goal**: Build a Windows desktop app that automatically updates wallpaper from a remote image URL on a configurable schedule.

**Philosophy**: Ship fast, zero maintenance, incremental progress. Every story delivers a working app with passing tests.

**Repository Structure**:
```
weather-display-apps/
├── plan/
│   ├── requirements.md              # High-level requirements
│   └── STORY_MAP.md                 # This document
├── src/
│   ├── WallpaperApp/                # Priority 1: Wallpaper application
│   │   ├── WallpaperApp.csproj
│   │   ├── Program.cs
│   │   ├── Services/
│   │   ├── Configuration/
│   │   └── appsettings.json
│   ├── WallpaperApp.Tests/          # Unit & integration tests
│   │   ├── WallpaperApp.Tests.csproj
│   │   └── ...
│   └── WidgetApp/                   # Future: Widget application
│       └── (deferred)
├── .gitignore
└── README.md
```

---

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | .NET 8 | Modern, well-supported, self-contained deployment |
| Deployment | Self-contained | No runtime dependencies, clean uninstall |
| Config | appsettings.json | Standard, testable, human-readable |
| Error Handling | Log and continue | No retries, wallpaper unchanged on failure |
| Initial Mode | Console EXE | Simple, iterate to Windows Service later |
| Final Mode | Windows Service | Survives reboot, runs in background |
| Testing | Hybrid (mock APIs + manual UI) | Good coverage, pragmatic for visual output |
| Shared Code | Minimal initially | Don't overcomplicate, consolidate when patterns emerge |

---

## Story Sequence

### Phase 0: Validation (1 story)
- Story 0: Tech Spike - Validate Wallpaper API

### Phase 1: Foundation (5 stories)
- Story 1: Foundation - Console App + First Test
- Story 2: Configuration - Read URL from appsettings.json  
- Story 3: Wallpaper Service - Set Static Image as Wallpaper
- Story 4: HTTP Client - Fetch Image from URL
- Story 5: Integration - Fetch and Set Wallpaper

**→ ARCHITECT REVIEW #1** (Core infrastructure complete)

### Phase 2: Automation (2 stories)
- Story 6: Periodic Refresh - Timer Implementation
- Story 7: Windows Service - Convert to Service

**→ ARCHITECT REVIEW #2** (Epic complete)

### Phase 3: Production Readiness (2 stories)
- Story 8: Logging and Observability
- Story 9: Installation Experience - Installer Scripts

**→ ARCHITECT REVIEW #3** (Pre-launch)

### Phase 4: Polish (Future)
- Story 10+: Edge cases, error recovery, multiple monitors, etc.

---

## Detailed Stories

---

## **Story 0: Tech Spike - Validate Wallpaper API**

**Epic**: Validation  
**Story Points**: 1  
**Type**: Technical Spike

### Description
Prove that we can programmatically set desktop wallpaper on Windows 10/11 using .NET. This is throwaway code to de-risk the project. If this fails, we pivot to Widget approach.

### Tasks
- [ ] Research Windows wallpaper API (SystemParametersInfo or newer)
- [ ] Create minimal .NET console app
- [ ] Write 10-20 lines of code to set a test image as wallpaper
- [ ] Test on developer's Windows 10/11 machine
- [ ] Document API findings in spike results

### Acceptance Criteria
- [ ] Desktop wallpaper changes when running the spike code
- [ ] Works on Windows 10 and/or Windows 11
- [ ] Code is checked in as `spike/wallpaper-api-validation.cs` (not part of main app)
- [ ] Spike results documented in `plan/spike-results.md`

### Testing Requirements
- Manual visual validation only (throwaway code)

### Definition of Done
- [x] Code runs and changes wallpaper
- [x] Results documented
- [x] Decision made: proceed with wallpaper approach or pivot to widget

### Notes
- This is disposable code - focus on learning, not quality
- If API is broken/unavailable, STOP and reassess project

---

## **Story 1: Foundation - Console App + First Test**

**Epic**: Foundation  
**Story Points**: 2  
**Type**: Feature

### Description
Establish project structure with a "Hello World" console app and first passing test. This proves the build/test pipeline works and sets coding standards.

### Tasks
- [ ] Create .NET 8 console application (`WallpaperApp`)
- [ ] Create test project (`WallpaperApp.Tests`)
- [ ] Add xUnit testing framework
- [ ] Implement `Program.cs` with `Main()` that prints "Weather Wallpaper App - Starting..."
- [ ] Write first test: `ApplicationStartsSuccessfully()`
- [ ] Configure `.csproj` for self-contained publish
- [ ] Add `.gitignore` for .NET projects
- [ ] Create `README.md` with build instructions

### Acceptance Criteria
- [ ] Running `WallpaperApp.exe` prints startup message to console
- [ ] Running `dotnet test` shows 1 passing test
- [ ] `dotnet publish` produces self-contained executable
- [ ] Build instructions in README work on clean machine

### Testing Requirements
```csharp
// WallpaperApp.Tests/ProgramTests.cs
public class ProgramTests
{
    [Fact]
    public void ApplicationStartsSuccessfully()
    {
        // Arrange & Act
        var exitCode = Program.Main(new string[] { });
        
        // Assert
        Assert.Equal(0, exitCode);
    }
}
```

### Definition of Done
- [x] All tests pass (1/1)
- [x] Code builds without warnings
- [x] Executable runs on Windows 10/11
- [x] Follows C# naming conventions (PascalCase for classes, camelCase for locals)
- [x] README.md includes: build, test, run instructions
- [x] Committed to `main` branch

### Notes
- Use .NET 8 SDK (latest LTS)
- Target `net8.0-windows`
- Self-contained publish: `-r win-x64 --self-contained true`

---

## **Story 2: Configuration - Read URL from appsettings.json**

**Epic**: Foundation  
**Story Points**: 3  
**Type**: Feature

### Description
Implement configuration service that reads image URL from `appsettings.json`. This establishes the pattern for all configuration management.

### Tasks
- [ ] Create `appsettings.json` with `ImageUrl` setting
- [ ] Add `Microsoft.Extensions.Configuration.Json` NuGet package
- [ ] Create `Configuration/AppSettings.cs` model class
- [ ] Create `Configuration/ConfigurationService.cs` 
- [ ] Implement `LoadConfiguration()` method
- [ ] Add validation: URL must be HTTPS
- [ ] Write tests for valid/invalid configurations
- [ ] Update `Program.cs` to load and display URL on startup

### Acceptance Criteria
- [ ] `appsettings.json` exists with structure:
```json
{
  "AppSettings": {
    "ImageUrl": "https://weather.zamflam.com/latest.png",
    "RefreshIntervalMinutes": 15
  }
}
```
- [ ] App reads URL from config and prints: "Configured URL: https://..."
- [ ] Invalid URL (non-HTTPS) throws `ConfigurationException` with clear message
- [ ] Missing config file throws `ConfigurationException` with clear message
- [ ] All tests pass (4+ tests)

### Testing Requirements
```csharp
// ConfigurationServiceTests.cs
public class ConfigurationServiceTests
{
    [Fact]
    public void LoadConfiguration_ValidConfig_ReturnsAppSettings() { }
    
    [Fact]
    public void LoadConfiguration_MissingFile_ThrowsException() { }
    
    [Fact]
    public void LoadConfiguration_NonHttpsUrl_ThrowsException() { }
    
    [Fact]
    public void LoadConfiguration_EmptyUrl_ThrowsException() { }
}
```

### Definition of Done
- [x] All tests pass (4+)
- [x] ConfigurationService class has single responsibility (SRP)
- [x] Clear exception messages guide user to fix config issues
- [x] Code follows Uncle Bob naming: `LoadConfiguration()` not `GetConfig()`
- [x] Sample `appsettings.json` included in repo
- [x] README updated with configuration section

---

## **Story 3: Wallpaper Service - Set Static Image as Wallpaper**

**Epic**: Foundation  
**Story Points**: 5  
**Type**: Feature

### Description
Implement `WallpaperService` that sets a local image file as desktop wallpaper using Windows API. This is the core functionality.

### Tasks
- [ ] Create `Services/WallpaperService.cs`
- [ ] Implement `SetWallpaper(string imagePath)` method
- [ ] Use P/Invoke for `SystemParametersInfo` (user32.dll)
- [ ] Add validation: file must exist, must be valid image format
- [ ] Create `IWallpaperService` interface for testing
- [ ] Write unit tests with mock Windows API calls
- [ ] Add test image asset to test project
- [ ] Create manual test checklist for visual validation

### Acceptance Criteria
- [ ] `WallpaperService.SetWallpaper(@"C:\test\image.png")` changes desktop wallpaper
- [ ] Throws `FileNotFoundException` if image doesn't exist
- [ ] Throws `InvalidImageException` if file is not PNG/JPG/BMP
- [ ] Works with absolute and relative paths
- [ ] All automated tests pass (5+ tests)
- [ ] Manual test checklist completed and documented

### Testing Requirements
```csharp
// WallpaperServiceTests.cs
public class WallpaperServiceTests
{
    [Fact]
    public void SetWallpaper_ValidImage_CallsWindowsApi() { }
    
    [Fact]
    public void SetWallpaper_MissingFile_ThrowsFileNotFoundException() { }
    
    [Fact]
    public void SetWallpaper_InvalidFormat_ThrowsInvalidImageException() { }
    
    [Fact]
    public void SetWallpaper_RelativePath_ResolvesCorrectly() { }
    
    [Theory]
    [InlineData("test.png")]
    [InlineData("test.jpg")]
    [InlineData("test.bmp")]
    public void SetWallpaper_SupportedFormats_Succeeds(string filename) { }
}
```

**Manual Test Checklist**:
- [ ] PNG image sets as wallpaper correctly
- [ ] JPG image sets as wallpaper correctly
- [ ] Wallpaper displays at correct resolution/scaling
- [ ] Wallpaper persists after locking/unlocking screen

### Definition of Done
- [x] All automated tests pass
- [x] Manual test checklist 100% complete
- [x] Interface `IWallpaperService` allows for mocking
- [x] P/Invoke code is isolated and testable
- [x] Error messages are actionable (e.g., "Image file not found at: C:\...")
- [x] Class has single responsibility (sets wallpaper, nothing else)

### Notes
- Use `SPI_SETDESKWALLPAPER` constant (0x0014)
- `SPIF_UPDATEINIFILE | SPIF_SENDCHANGE` for persistence
- Reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa

---

## **Story 4: HTTP Client - Fetch Image from URL**

**Epic**: Foundation  
**Story Points**: 5  
**Type**: Feature

### Description
Implement `ImageFetcher` service that downloads image from URL and saves to temp directory. Handles HTTP errors gracefully.

### Tasks
- [ ] Create `Services/ImageFetcher.cs`
- [ ] Implement `DownloadImageAsync(string url)` method
- [ ] Use `HttpClient` with proper disposal
- [ ] Save image to `%TEMP%/WeatherWallpaper/` directory
- [ ] Generate unique filename per download (timestamp-based)
- [ ] Add timeout (30 seconds)
- [ ] Handle HTTP errors: log and return null (no retries)
- [ ] Write tests with mocked `HttpClient`
- [ ] Test with real URL (integration test, opt-in)

### Acceptance Criteria
- [ ] Downloads image from `https://weather.zamflam.com/latest.png`
- [ ] Saves to `%TEMP%/WeatherWallpaper/image-{timestamp}.png`
- [ ] Returns full path to downloaded file
- [ ] Returns `null` on HTTP error (404, 500, timeout, etc.)
- [ ] Logs error message when download fails
- [ ] All tests pass (6+ tests)
- [ ] Integration test (manual/opt-in) succeeds with real URL

### Testing Requirements
```csharp
// ImageFetcherTests.cs
public class ImageFetcherTests
{
    [Fact]
    public async Task DownloadImageAsync_ValidUrl_ReturnsFilePath() { }
    
    [Fact]
    public async Task DownloadImageAsync_InvalidUrl_ReturnsNull() { }
    
    [Fact]
    public async Task DownloadImageAsync_Timeout_ReturnsNull() { }
    
    [Fact]
    public async Task DownloadImageAsync_SavesTo_TempDirectory() { }
    
    [Fact]
    public async Task DownloadImageAsync_GeneratesUniqueFilename() { }
    
    [Fact]
    public async Task DownloadImageAsync_HttpError_LogsErrorAndReturnsNull() { }
}
```

### Definition of Done
- [x] All automated tests pass
- [x] Uses `async/await` properly (no blocking calls)
- [x] HttpClient disposed correctly (`using` statement or IHttpClientFactory)
- [x] Temp directory created if doesn't exist
- [x] Old temp files NOT cleaned up (simplicity > optimization)
- [x] Method name is verb phrase: `DownloadImageAsync()` not `FetchImage()`

### Notes
- Use `IHttpClientFactory` for testability
- Filename format: `wallpaper-{yyyyMMdd-HHmmss}.png`
- Log at `Information` level for success, `Warning` for failure

---

## **Story 5: Integration - Fetch and Set Wallpaper**

**Epic**: Foundation  
**Story Points**: 3  
**Type**: Feature

### Description
Wire together Configuration → ImageFetcher → WallpaperService into a complete workflow. This is the first end-to-end functionality.

### Tasks
- [ ] Create `WallpaperUpdater` orchestrator class
- [ ] Implement `UpdateWallpaperAsync()` method
- [ ] Sequence: Load config → Download image → Set wallpaper
- [ ] Handle errors: log and exit gracefully (no crash)
- [ ] Update `Program.cs` to call orchestrator
- [ ] Write integration test for happy path
- [ ] Write integration test for error scenarios
- [ ] Manual end-to-end test with weather.zamflam.com

### Acceptance Criteria
- [ ] Running app downloads image and sets as wallpaper
- [ ] Uses URL from `appsettings.json`
- [ ] If download fails, logs error and exits (wallpaper unchanged)
- [ ] If wallpaper set fails, logs error and exits
- [ ] Console output shows: "✓ Downloaded image" → "✓ Wallpaper updated"
- [ ] All tests pass (3+ integration tests)
- [ ] Manual test: Desktop wallpaper shows weather.zamflam.com image

### Testing Requirements
```csharp
// WallpaperUpdaterTests.cs
public class WallpaperUpdaterTests
{
    [Fact]
    public async Task UpdateWallpaperAsync_HappyPath_SucceedsEndToEnd() { }
    
    [Fact]
    public async Task UpdateWallpaperAsync_DownloadFails_LogsErrorAndExitsGracefully() { }
    
    [Fact]
    public async Task UpdateWallpaperAsync_SetWallpaperFails_LogsErrorAndExitsGracefully() { }
}
```

**Manual Test Checklist**:
- [ ] App runs without errors
- [ ] Console shows progress messages
- [ ] Desktop wallpaper updates to weather image
- [ ] Config URL is respected
- [ ] Error logged if URL unreachable (test with fake URL)

### Definition of Done
- [x] All automated tests pass
- [x] Manual test checklist 100% complete
- [x] Error handling tested (both download and wallpaper failures)
- [x] Orchestrator follows Single Responsibility (coordinates, doesn't do work)
- [x] Console output is user-friendly (no stack traces)
- [x] This is a **working end-to-end application**

---

## **🏛️ ARCHITECT REVIEW #1**

**Trigger**: After Story 5 (Core infrastructure complete)

### Review Scope
- Overall architecture and separation of concerns
- Dependency injection setup (if any)
- Interface design and testability
- Configuration management approach
- Error handling patterns
- Naming conventions and code clarity

### Deliverables from Review
1. **Architecture Validation Document**
   - Strengths of current design
   - Identified code smells or violations of SOLID
   - Refactoring recommendations

2. **Refactoring Backlog** (if needed)
   - Prioritized list of refactorings
   - Estimated effort per refactor
   - Decision: tackle before Story 6 or defer

### Review Questions
- Does the design support adding Windows Service later?
- Are responsibilities clearly separated (SRP)?
- Are abstractions appropriate (not over-engineered)?
- Is the code testable without excessive mocking?
- Does error handling follow consistent patterns?

### Decision Point
**STOP HERE**: Address critical refactorings before proceeding to Story 6.

---

## **Story 6: Periodic Refresh - Timer Implementation**

**Epic**: Automation  
**Story Points**: 5  
**Type**: Feature

### Description
Add timer to automatically refresh wallpaper every 15 minutes. App now runs continuously instead of once-and-exit.

### Tasks
- [ ] Create `Services/TimerService.cs`
- [ ] Implement interval-based execution using `System.Threading.Timer`
- [ ] Read interval from `appsettings.json` (default: 15 minutes)
- [ ] Call `WallpaperUpdater.UpdateWallpaperAsync()` on each tick
- [ ] Handle timer disposal properly
- [ ] Add console output: "Next refresh in 15 minutes..."
- [ ] Write tests for timer scheduling logic
- [ ] Update `Program.cs` to keep app running (wait for Ctrl+C)

### Acceptance Criteria
- [ ] App starts, updates wallpaper immediately
- [ ] Updates wallpaper again after 15 minutes automatically
- [ ] Continues running until user presses Ctrl+C
- [ ] Console shows countdown: "Next refresh at {time}"
- [ ] Interval configurable via `appsettings.json`
- [ ] All tests pass (4+ tests)
- [ ] Manual test: wallpaper updates at least twice in 30-minute window

### Testing Requirements
```csharp
// TimerServiceTests.cs
public class TimerServiceTests
{
    [Fact]
    public void Start_SchedulesFirstExecutionImmediately() { }
    
    [Fact]
    public void Start_SchedulesSubsequentExecutionsAtInterval() { }
    
    [Fact]
    public void Stop_CancelsTimerAndStopsExecution() { }
    
    [Fact]
    public void TimerCallback_CatchesExceptionsAndContinues() { }
}
```

### Definition of Done
- [x] All automated tests pass
- [x] App runs continuously without memory leaks (test for 1 hour)
- [x] Timer errors don't crash app (catches exceptions)
- [x] Graceful shutdown on Ctrl+C (disposes timer)
- [x] Console output helpful for debugging

### Notes
- First execution: immediate
- Subsequent executions: every 15 minutes from start time
- Use `CancellationToken` for graceful shutdown

---

## **Story 7: Windows Service - Convert to Service**

> **DEPRECATED**: This story was implemented but later superseded by the System Tray App approach.
> See [ADR-005](plan/adr/ADR-005-pivot-service-to-tray-app.md) for rationale. The Windows Service code has been removed from the codebase.
> This story is preserved for historical context only.

**Epic**: Automation
**Story Points**: 8
**Type**: Feature

### Description
Convert console app to Windows Service with install/uninstall capability. This enables "survives reboot" requirement.

### Tasks
- [ ] Add `Microsoft.Extensions.Hosting.WindowsServices` NuGet package
- [ ] Refactor `Program.cs` to use `HostBuilder` pattern
- [ ] Create `Worker.cs` service implementing `BackgroundService`
- [ ] Move timer logic into `Worker.ExecuteAsync()`
- [ ] Add service installation scripts (`install-service.bat`, `uninstall-service.bat`)
- [ ] Configure service: Auto-start, run as LocalSystem
- [ ] Test console mode still works (for debugging)
- [ ] Test service mode (install → start → verify wallpaper updates)
- [ ] Write service lifecycle tests

### Acceptance Criteria
- [ ] Running `WallpaperApp.exe` (no args) runs as console app
- [ ] Running `install-service.bat` installs Windows Service
- [ ] Service appears in `services.msc` as "Weather Wallpaper Service"
- [ ] Service starts automatically on boot
- [ ] Service updates wallpaper every 15 minutes
- [ ] Running `uninstall-service.bat` cleanly removes service
- [ ] All tests pass (6+ tests)
- [ ] Manual test: reboot machine, service resumes updating wallpaper

### Testing Requirements
```csharp
// WorkerTests.cs
public class WorkerTests
{
    [Fact]
    public async Task ExecuteAsync_StartsTimerOnServiceStart() { }
    
    [Fact]
    public async Task ExecuteAsync_StopsTimerOnServiceStop() { }
    
    [Fact]
    public async Task ExecuteAsync_ContinuesRunningAfterErrors() { }
}
```

**Manual Test Checklist**:
- [ ] Install service via script
- [ ] Service visible in `services.msc`
- [ ] Service starts successfully
- [ ] Wallpaper updates within 1 minute
- [ ] Reboot machine
- [ ] Service auto-starts after reboot
- [ ] Wallpaper continues updating
- [ ] Uninstall service via script
- [ ] Service removed from `services.msc`

### Definition of Done
- [x] All automated tests pass
- [x] Manual test checklist 100% complete
- [x] Install scripts work on clean Windows 10/11
- [x] Uninstall leaves no traces (registry, files, services)
- [x] README updated with installation instructions
- [x] Both console and service modes functional

### Notes
- Use `sc create` for installation
- Use `sc delete` for uninstallation
- Service name: `WeatherWallpaperService`
- Display name: "Weather Wallpaper Service"
- Description: "Automatically updates desktop wallpaper with weather forecasts"

---

## **🏛️ ARCHITECT REVIEW #2**

**Trigger**: After Story 7 (Epic: Automation complete)

### Review Scope
- Windows Service integration and lifecycle management
- Background task patterns and threading
- Resource management and disposal
- Configuration in service context
- Installation/uninstallation experience
- Upgrade path considerations

### Deliverables from Review
1. **Architecture Validation Document**
   - Service architecture assessment
   - Threading and concurrency safety
   - Resource lifecycle management
   - Identified gaps or risks

2. **Technical Debt Assessment**
   - Quick wins vs. future backlog
   - Performance or reliability concerns
   - Maintainability issues

### Review Questions
- Is the service lifecycle correctly implemented?
- Are resources disposed properly on shutdown?
- Does configuration reload without restart (if needed)?
- Is error recovery robust enough for unattended operation?
- Can we upgrade without breaking existing installations?

### Decision Point
**EVALUATE**: Is the app production-ready, or do we need Story 8-9?

---

## **Story 8: Logging and Observability**

**Epic**: Production Readiness  
**Story Points**: 5  
**Type**: Feature

### Description
Add structured logging for diagnosing issues in production. Logs written to file for service mode, console for debug mode.

### Tasks
- [ ] Add `Serilog` NuGet packages
- [ ] Configure file logging: `%ProgramData%/WeatherWallpaper/logs/`
- [ ] Configure log rolling (daily, max 7 files)
- [ ] Add logging to all services: `ILogger<T>` injection
- [ ] Log key events: startup, config load, download, wallpaper set, errors
- [ ] Use structured logging: `Log.Information("Downloaded from {Url}", url)`
- [ ] Test log output in both console and service modes
- [ ] Write tests for logging behavior

### Acceptance Criteria
- [ ] Console mode: logs to console (colored output)
- [ ] Service mode: logs to `%ProgramData%/WeatherWallpaper/logs/app-{date}.log`
- [ ] Logs include: timestamp, level, message, context
- [ ] Errors include exception details
- [ ] Log files rotate daily, keep 7 days
- [ ] All tests pass (4+ tests)
- [ ] Manual test: can diagnose download failure from logs

### Testing Requirements
```csharp
// LoggingTests.cs
public class LoggingTests
{
    [Fact]
    public void Logger_ConfiguredCorrectly_InConsoleMode() { }
    
    [Fact]
    public void Logger_ConfiguredCorrectly_InServiceMode() { }
    
    [Fact]
    public void Logger_LogsException_WithFullContext() { }
    
    [Fact]
    public void FileLogger_RotatesDaily_KeepsSevenDays() { }
}
```

### Definition of Done
- [x] All automated tests pass
- [x] Logs readable and useful for troubleshooting
- [x] No sensitive data logged (URLs are fine, credentials are not)
- [x] Performance impact <10ms per log statement
- [x] Log directory created automatically if missing

---

## **Story 9: Installation Experience - Installer Scripts**

**Epic**: Production Readiness  
**Story Points**: 3  
**Type**: Feature

### Description
Polish installation scripts to guide user through setup. Make install/uninstall foolproof.

### Tasks
- [ ] Enhance `install-service.bat`:
  - Check for admin privileges
  - Validate executable exists
  - Create config directory if missing
  - Copy default `appsettings.json`
  - Display success/failure message
- [ ] Enhance `uninstall-service.bat`:
  - Stop service before deleting
  - Clean up logs directory (optional, ask user)
  - Remove service registration
- [ ] Create `INSTALL.md` with step-by-step guide
- [ ] Add troubleshooting section to README
- [ ] Test on clean Windows 10 and Windows 11 machines

### Acceptance Criteria
- [ ] Non-admin user gets clear error: "Run as Administrator"
- [ ] Missing executable gives helpful error
- [ ] Default config copied to `%ProgramData%/WeatherWallpaper/`
- [ ] Install script outputs: "✓ Service installed successfully"
- [ ] Uninstall script asks: "Delete logs? (Y/N)"
- [ ] `INSTALL.md` covers: requirements, installation, configuration, troubleshooting
- [ ] Manual test: complete install → configure → uninstall on 2 machines

### Definition of Done
- [x] Fresh install works on Windows 10 and 11
- [x] Install/uninstall tested by someone else (not developer)
- [x] Documentation complete and tested
- [x] Error messages actionable
- [x] Can go from zero to working wallpaper in <5 minutes

---

## **🏛️ ARCHITECT REVIEW #3**

**Trigger**: After Story 9 (Pre-launch readiness)

### Review Scope
- Overall system architecture
- Code quality and maintainability
- Test coverage and confidence
- Documentation completeness
- Installation/upgrade experience
- Known technical debt and risks

### Deliverables from Review
1. **Launch Readiness Report**
   - Go/No-Go recommendation
   - Outstanding risks and mitigations
   - Post-launch monitoring plan
   - Future enhancement priorities

2. **Maintenance Playbook**
   - Common issues and solutions
   - Log analysis guides
   - Upgrade procedures
   - Rollback procedures

### Review Questions
- Would we trust this to run unattended for 30 days?
- Is documentation sufficient for future debugging?
- Are there any lurking bugs or race conditions?
- Can we support this with zero maintenance?
- What would cause us to abandon the project?

### Decision Point
**SHIP OR ITERATE**: Launch to production or add polish stories?

---

## Future Stories (Backlog)

### Story 10: Multi-Monitor Support
Handle wallpaper across multiple displays

### Story 11: Image Validation
Verify downloaded file is valid image before setting

### Story 12: Graceful Degradation
Show last successful image if download fails N times in a row

### Story 13: Configuration Hot Reload
Change URL without restarting service

### Story 14: Health Check Endpoint
HTTP endpoint for monitoring service health

### Story 15: Metrics and Telemetry
Track success/failure rates, latencies

### Story 16: System Tray Icon
**Priority**: Medium
**Estimated Points**: 5

Add system tray icon for easy service management without opening services.msc:
- Show tray icon when service is running
- Right-click menu: "Refresh Now", "View Status", "Open Settings", "Exit"
- Display last update time and next scheduled update
- Show notification on wallpaper update (optional, configurable)
- Visual indicator: green (running), yellow (error), red (stopped)
- Double-click to open status window
- Tray icon persists across user sessions
- Clean removal when service uninstalled

**Benefits**: Better UX, easier troubleshooting, manual refresh capability

---

## Definition of Ready (for each story)

A story is ready to start when:
- [ ] Acceptance criteria are clear and testable
- [ ] Dependencies on previous stories are met
- [ ] Any architectural decisions are documented
- [ ] Test requirements are defined
- [ ] Developer understands the scope

---

## Definition of Done (for each story)

A story is complete when:
- [x] All acceptance criteria met
- [x] All automated tests pass
- [x] Code coverage >80% for new code (where applicable)
- [x] Manual test checklist completed (if applicable)
- [x] Code reviewed for naming, clarity, SOLID principles
- [x] No compiler warnings
- [x] Documentation updated (README, code comments)
- [x] Committed to `main` branch
- [x] Application runs end-to-end without errors
- [x] Ready for next story (no blockers introduced)

---

## Uncle Bob Principles Checklist

Apply to every story:

### Clean Code
- [ ] Names reveal intent: `DownloadImageAsync()` not `GetImg()`
- [ ] Functions do one thing (SRP)
- [ ] No magic numbers: use named constants
- [ ] Consistent abstraction level in each function
- [ ] Positive conditionals: `if (isValid)` not `if (!isInvalid)`

### SOLID Principles
- [ ] **S**ingle Responsibility: Each class has one reason to change
- [ ] **O**pen/Closed: Extend via inheritance/composition, not modification
- [ ] **L**iskov Substitution: Interfaces are substitutable
- [ ] **I**nterface Segregation: Small, focused interfaces
- [ ] **D**ependency Inversion: Depend on abstractions, not concretions

### Testing
- [ ] Tests are first-class citizens (not afterthoughts)
- [ ] Test names describe behavior: `UpdateWallpaper_WhenUrlInvalid_LogsErrorAndReturnsNull()`
- [ ] AAA pattern: Arrange, Act, Assert
- [ ] One assertion per test (where reasonable)
- [ ] Tests are fast, isolated, repeatable

---

## Agent Engineer Execution Guide

### Story Workflow
1. **Read Story**: Understand description, acceptance criteria, tests
2. **Plan Implementation**: Sketch classes/interfaces needed
3. **Write Tests First** (TDD): Red → Green → Refactor
4. **Implement Feature**: Write minimal code to pass tests
5. **Refactor**: Clean up, apply Uncle Bob principles
6. **Manual Testing**: Complete checklist if applicable
7. **Review Definition of Done**: Check all boxes
8. **Commit**: Descriptive message, single logical change
9. **Move to Next Story**

### When to Ask for Help
- Acceptance criteria unclear
- Architectural decision needed
- Test approach uncertain
- Blocked by external dependency

### Communication Pattern
- **Start of Story**: "Starting Story X: {title}"
- **During Story**: Progress updates if >1 day
- **Blockers**: Immediate escalation
- **End of Story**: "Story X complete. All tests pass. Ready for review."

### Commit Message Format
```
Story X: Brief description of change

- Implemented FeatureService
- Added tests for happy path and errors
- Updated README with new configuration

Tests: 12 passed, 0 failed
Coverage: 87%
```

---

## Success Metrics

### 7-Day Validation (Post-Story 7)
- [ ] Service runs for 7 consecutive days
- [ ] Wallpaper updates 672 times (7 days * 24 hours * 4 updates/hour)
- [ ] Zero crashes or hangs
- [ ] Zero manual interventions
- [ ] Survives at least 1 reboot

### 30-Day Production (Post-Story 9)
- [ ] Still using the app daily
- [ ] Zero maintenance required
- [ ] Would rebuild if lost
- [ ] Wallpaper views >> website visits

---

## Risk Management

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Windows API changes/breaks | Low | High | Spike validates early (Story 0) |
| Service won't start | Medium | High | Extensive testing in Story 7 |
| Memory leak | Low | Medium | 1-hour soak test in Story 6 |
| Image download failures | High | Low | Log and retry next cycle (Story 4) |
| Configuration errors | Medium | Low | Validation and clear errors (Story 2) |
| Installation issues | Medium | Medium | Test on multiple machines (Story 9) |

---

## Tools and Environment

### Development
- **IDE**: Visual Studio 2022 or Rider
- **SDK**: .NET 8 SDK
- **Testing**: xUnit + Moq + FluentAssertions
- **Logging**: Serilog

### Testing
- **Unit Tests**: xUnit
- **Mocking**: Moq or NSubstitute
- **Coverage**: `dotnet test --collect:"XPlat Code Coverage"`
- **Manual**: Checklists in each story

### Deployment
- **Build**: `dotnet publish -c Release -r win-x64 --self-contained true`
- **Install**: Batch scripts
- **Service Management**: `sc.exe` commands

---

## Appendix: Quick Reference

### Build Commands
```bash
# Build
dotnet build

# Test
dotnet test

# Publish (self-contained)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

# Run
./publish/WallpaperApp.exe
```

### Service Commands
```batch
# Install
install-service.bat

# Start
sc start WeatherWallpaperService

# Stop
sc stop WeatherWallpaperService

# Uninstall
uninstall-service.bat
```

### Configuration Example
```json
{
  "AppSettings": {
    "ImageUrl": "https://weather.zamflam.com/latest.png",
    "RefreshIntervalMinutes": 15
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "%ProgramData%/WeatherWallpaper/logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

---

## Document Status

**Version**: 1.0  
**Last Updated**: 2024-02-14  
**Status**: Ready for Implementation  
**Next Review**: After ARCHITECT REVIEW #1

---

**END OF STORY MAP**
