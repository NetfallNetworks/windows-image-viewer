# Widget Board Integration - Project Overview

**Project**: Add Windows 11 Widget Board as a second rendering option
**Created**: 2026-02-22
**Status**: Ready for Implementation
**Epic**: Widget Board Integration

---

## Context

The Wallpaper Sync application currently renders images as desktop wallpaper via `SystemParametersInfo`. This project adds a **parallel rendering option**: a Windows 11 Widget Board widget that displays the same image in the Widget Board flyout (Win+W) at the same refresh cadence.

This was anticipated from day one — `STORY_MAP.md` already lists `WidgetApp/ # Future: Widget application`, and Story 0 considered a "pivot to Widget approach." We are not pivoting; we are adding the widget alongside wallpaper as an equal, independently selectable option.

**Key Problem Addressed**:
- Users who prefer the Widget Board over a full-screen wallpaper have no option today
- The Widget Board is the official Windows 11 platform for ambient, glanceable content
- Wallpaper changes are all-or-nothing; widgets are user-managed tiles that coexist with other widgets

---

## Implementation Strategy

The integration is organized into **3 incremental, shippable phases**:

### Phase 1: Widget Provider Core
**Priority**: CRITICAL (foundation)
**Story Points**: 13
**Duration**: 3-4 days

Stand up a new out-of-process COM server (`WallpaperApp.WidgetProvider`) that implements `IWidgetProvider` from the Windows App SDK. At the end of this phase, the widget provider executable exists, compiles, and serves a static Adaptive Card. No packaging or installation yet.

**Stories**:
- Story WS-15: Widget Provider Project Setup (5 pts)
- Story WS-16: Adaptive Card Templates (3 pts)
- Story WS-17: Widget Data Binding & Lifecycle (5 pts)

### Phase 2: Packaging & Identity
**Priority**: HIGH
**Story Points**: 11
**Duration**: 2-3 days

Widget Board registration requires package identity — a lightweight sparse MSIX that declares the COM server and widget definition. This phase builds the identity package alongside the existing MSI and adds IPC so the TrayApp can trigger instant widget refresh.

**Stories**:
- Story WS-18: Identity Package (Sparse MSIX) (8 pts)
- Story WS-19: TrayApp ↔ Widget IPC (3 pts)

### Phase 3: Integration & Pipeline
**Priority**: HIGH
**Story Points**: 11
**Duration**: 2-3 days

Wire everything together: installer ships the WidgetProvider executable and identity package, build pipeline handles the new projects, and unit tests cover the widget's testable logic.

**Stories**:
- Story WS-20: Installer Integration (5 pts)
- Story WS-21: Build Pipeline Updates (3 pts)
- Story WS-22: Widget Provider Tests (3 pts)

---

## Total Effort Estimate

- **Story Points**: 35 total
- **Duration**: 7-10 days
- **Team Size**: 1 developer

---

## Success Criteria

**Phase 1 Complete**:
- ✅ `WallpaperApp.WidgetProvider.exe` builds and runs as a COM server
- ✅ Adaptive Card templates render correctly in the Widget Board (manual test)
- ✅ Widget reads live data from `IConfigurationService` and `IAppStateService`
- ✅ "Refresh Now" action triggers `IWallpaperUpdater.UpdateWallpaperAsync()`
- ✅ All new unit tests pass

**Phase 2 Complete**:
- ✅ Sparse MSIX identity package builds and installs without error
- ✅ Widget provider registers with Widget Board after package install
- ✅ Widget appears in Win+W "Add widgets" panel
- ✅ Named EventWaitHandle signals widget refresh within 2 seconds of wallpaper update
- ✅ All tests pass

**Phase 3 Complete**:
- ✅ MSI installer includes WidgetProvider exe and identity package
- ✅ `build.bat` and `build.sh` produce all artifacts without manual steps
- ✅ Widget provider unit tests integrated into CI test run (pass on Linux CI)
- ✅ End-to-end: fresh install → widget visible in Widget Board → refresh fires

**Overall Success**:
- ✅ All 94+ tests pass
- ✅ No regressions in wallpaper functionality
- ✅ Widget displays correct image on first open
- ✅ Widget updates within polling interval or on IPC signal

---

## Story Files

- `plan/widget-phase-1-stories.md` - Widget Provider Core (Stories WS-15 to WS-17)
- `plan/widget-phase-2-stories.md` - Packaging & Identity (Stories WS-18 to WS-19)
- `plan/widget-phase-3-stories.md` - Integration & Pipeline (Stories WS-20 to WS-22)

---

## Dependencies

**Prerequisites**:
- Wallpaper Sync Phase 1–3 complete (Stories WS-1 through WS-14)
- All 94 existing tests passing
- Windows App SDK NuGet package available (`Microsoft.WindowsAppSDK`)

**External Dependencies**:
- `Microsoft.WindowsAppSDK` — Widget Board APIs (`IWidgetProvider`, `WidgetManager`)
- `WixToolset.UI.wixext` — already used for existing MSI
- `makeappx.exe` — part of Windows SDK (available in CI/build environments)

---

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Widget platform | Windows 11 Widget Board | Official, discoverable, Microsoft-supported API |
| Package identity | Sparse MSIX alongside MSI | Keep existing MSI installer; add lightweight identity only |
| Process model | Separate `WallpaperApp.WidgetProvider.exe` | COM server must be independently activatable by Widget Board host |
| Image display | Remote URL in Adaptive Card | `AppSettings.ImageUrl` works directly; LocalFile mode shows placeholder |
| IPC mechanism | Named `EventWaitHandle` | Lightweight, zero external deps, instant refresh signal |
| Widget framework | Windows App SDK (`Microsoft.WindowsAppSDK`) | Official .NET SDK for Widget Board APIs |
| Card format | Adaptive Cards JSON | Required by Widget Board; supports small/medium/large sizes |

---

## Core Services Reused (no changes needed)

| Service | File | Used For |
|---------|------|----------|
| `IConfigurationService` | `src/WallpaperApp.Core/Configuration/ConfigurationService.cs` | Read `ImageUrl`, `FitMode`, `RefreshIntervalMinutes`, `SourceType` |
| `IAppStateService` | `src/WallpaperApp.Core/Services/IAppStateService.cs` | Read `LastUpdateTime`, `IsEnabled`, `LastKnownGoodImagePath` |
| `IWallpaperUpdater` | `src/WallpaperApp.Core/Services/IWallpaperUpdater.cs` | Trigger `UpdateWallpaperAsync()` for "Refresh Now" action |
| `AppSettings` | `src/WallpaperApp.Core/Configuration/AppSettings.cs` | Config model with `ImageUrl`, `SourceType`, `FitMode` |
| `AppState` | `src/WallpaperApp.Core/Models/AppState.cs` | State model with `LastUpdateTime`, `IsEnabled` |
| `AppPaths` | `src/WallpaperApp.Core/AppPaths.cs` | Shared file paths (`state.json`, `WallpaperApp.json`) |

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Widget Board API not available (Windows < 11) | Medium | Medium | Widget silently absent; wallpaper still works. Guard with OS version check. |
| MSIX signing complexity in CI | Medium | High | Use self-signed cert for dev; document production cert requirement explicitly |
| COM activation fails on first run | Low | High | Log COM registration errors clearly; add diagnostic fallback |
| Adaptive Card render differences across W11 versions | Low | Low | Test on multiple W11 builds; use conservative card schema |
| IPC signal dropped during heavy load | Low | Low | Widget has fallback polling interval (30 s) as safety net |

---

## Architecture Overview

```
┌─────────────────────────────┐     Named EventWaitHandle      ┌─────────────────────────┐
│   WallpaperApp.TrayApp      │ ─────────────────────────────► │ WallpaperApp.WidgetProvider │
│   (existing WPF app)        │                                 │  (new COM server)          │
└─────────────────────────────┘                                 └─────────────────────────┘
             │                                                              │
             │  DI → IWallpaperUpdater                                      │  DI → IConfigurationService
             │       IConfigurationService                                  │       IAppStateService
             ▼                                                              ▼
┌─────────────────────────────┐                                 ┌─────────────────────────┐
│   WallpaperApp.Core         │                                 │   Windows Widget Board   │
│   (shared services)         │                                 │   (Win+W flyout host)    │
└─────────────────────────────┘                                 └─────────────────────────┘
```

---

## Document Status

**Version**: 1.0
**Last Updated**: 2026-02-22
**Status**: Ready for Implementation
**Next Review**: After Phase 1 Complete

---

**END OF OVERVIEW**
