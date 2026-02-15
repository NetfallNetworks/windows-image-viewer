# Wallpaper Sync - Phase 2: Renaming & Branding

**Epic**: Rebranding
**Priority**: HIGH
**Total Story Points**: 5
**Estimated Duration**: 1-2 days

---

## Phase Overview

Phase 2 rebrands the application from "Weather Wallpaper" to "Wallpaper Sync" throughout all user-facing elements. **Strategy**: Keep assembly/namespace names as `WallpaperApp.*` for backward compatibility. Only rename user-visible strings and file paths.

**Key Objectives**:
1. Replace all "Weather" strings in UI with "Wallpaper Sync"
2. Update directory paths (`%TEMP%\WeatherWallpaper\` → `%TEMP%\WallpaperSync\`)
3. Update documentation and scripts
4. Maintain backward compatibility (no breaking changes)

---

## Story WS-7: Rename User-Facing Strings and Paths

**Story Points**: 3
**Type**: Refactoring
**Priority**: HIGH

### Description

Replace all user-visible "Weather Wallpaper" strings with "Wallpaper Sync" and update file system paths. Keep internal namespaces unchanged.

### Tasks

**Tray App UI (src/WallpaperApp.TrayApp/)**:
- [ ] `MainWindow.xaml.cs` line 64: `Text = "Wallpaper Sync"`
- [ ] `MainWindow.xaml.cs` lines 222-230: Replace "Weather Wallpaper" → "Wallpaper Sync" in status messages
- [ ] `MainWindow.xaml.cs` lines 236-242: Update About dialog text
- [ ] `MainWindow.xaml` line 7: `Title="Wallpaper Sync"`
- [ ] `App.xaml.cs` line 14: Mutex name → `WallpaperSync_SingleInstance`
- [ ] `App.xaml.cs` line 20: MessageBox text → "Wallpaper Sync"

**Service Layer (src/WallpaperApp/Services/)**:
- [ ] `FileLogger.cs` line 9: LogDirectory → `%TEMP%\WallpaperSyncService\`
- [ ] `ImageFetcher.cs` line 21: `_tempDirectory` → `%TEMP%\WallpaperSync\`

**State/Config Paths**:
- [ ] `AppStateService.cs`: State directory → `%LOCALAPPDATA%\WallpaperSync\`
- [ ] Verify last-known-good path → `%LOCALAPPDATA%\WallpaperSync\last-known-good.png`

### Files to Modify

```
src/WallpaperApp.TrayApp/MainWindow.xaml.cs
src/WallpaperApp.TrayApp/MainWindow.xaml
src/WallpaperApp.TrayApp/App.xaml.cs
src/WallpaperApp/Services/FileLogger.cs
src/WallpaperApp/Services/ImageFetcher.cs
src/WallpaperApp/Services/AppStateService.cs
```

### Search Pattern

```bash
# Find all "Weather" references (excluding namespaces/assemblies)
grep -r "Weather" src/ --exclude-dir={bin,obj} \
    | grep -v "namespace WallpaperApp" \
    | grep -v "using WallpaperApp"
```

Expected results: Only the files listed above should contain "Weather" in user-facing strings.

### Acceptance Criteria

- [ ] No "Weather" strings in UI (tray tooltip, menus, dialogs)
- [ ] Temp directory is `%TEMP%\WallpaperSync\`
- [ ] State directory is `%LOCALAPPDATA%\WallpaperSync\`
- [ ] Mutex name is `WallpaperSync_SingleInstance`
- [ ] Namespaces remain `WallpaperApp.*` (unchanged)
- [ ] All tests pass
- [ ] Fresh install creates "WallpaperSync" folders
- [ ] Upgrade from old version works (doesn't break existing installs)

### Testing Requirements

**Manual Testing Checklist**:
- [ ] Run app, check tray tooltip: "Wallpaper Sync"
- [ ] Right-click tray → About: Shows "Wallpaper Sync"
- [ ] Check filesystem:
  - [ ] `%TEMP%\WallpaperSync\` exists
  - [ ] `%TEMP%\WallpaperSyncService\` exists
  - [ ] `%LOCALAPPDATA%\WallpaperSync\` exists
- [ ] Old "WeatherWallpaper" directories are NOT created
- [ ] Double-click tray icon shows "Wallpaper Sync" in any dialogs

### Definition of Done

- [x] All occurrences of "Weather Wallpaper" replaced in UI
- [x] All directory paths updated
- [x] Manual test checklist 100% complete
- [x] All tests pass (no regressions)
- [x] Backward compatibility verified

---

## Story WS-8: Update Documentation and Scripts

**Story Points**: 2
**Type**: Documentation
**Priority**: HIGH

### Description

Update all documentation, README files, and installation scripts to reflect the "Wallpaper Sync" branding.

### Tasks

**Documentation**:
- [ ] `README.md`: Replace "Weather Wallpaper" → "Wallpaper Sync" (title, description, all occurrences)
- [ ] `README.md`: Update feature list to include new Phase 1 features (fit modes, validation, etc.)
- [ ] `STORY_MAP.md`: Add reference to Wallpaper Sync refactoring

**Scripts**:
- [ ] `scripts/install-tray-app.ps1`:
  - Update display name: "Wallpaper Sync"
  - Update shortcut name: "Wallpaper Sync.lnk" (not "Weather Wallpaper.lnk")
  - Update description text
- [ ] Check for other scripts in `scripts/` directory

**Plan Documents** (Optional - create ADR):
- [ ] Create `plan/adr/ADR-006-rename-weather-to-wallpaper-sync.md` documenting the branding decision

### Files to Modify

```
README.md
STORY_MAP.md (add note about Wallpaper Sync refactoring)
scripts/install-tray-app.ps1
plan/adr/ADR-006-rename-weather-to-wallpaper-sync.md (NEW)
```

### Acceptance Criteria

- [ ] README.md has "Wallpaper Sync" as title
- [ ] README.md describes new features (fit modes, security, last-known-good)
- [ ] Installation script creates "Wallpaper Sync.lnk" shortcut
- [ ] No "Weather Wallpaper" strings in documentation
- [ ] ADR documents the renaming decision (optional but recommended)

### Testing Requirements

**Manual Verification**:
- [ ] Read README.md top to bottom - no "Weather" references
- [ ] Run `install-tray-app.ps1` - verify shortcut name is "Wallpaper Sync"
- [ ] Check Start Menu / Startup folder - shortcut name correct

### Definition of Done

- [x] All documentation updated
- [x] Scripts create correct shortcut names
- [x] No "Weather" references in user-facing docs
- [x] Manual verification complete

---

## Phase 2 Complete Checklist

When both stories are done:

- [ ] No "Weather Wallpaper" strings in UI or documentation
- [ ] All file paths use "WallpaperSync" naming
- [ ] Installation scripts updated
- [ ] Fresh install works correctly
- [ ] Upgrade from old version works (if applicable)
- [ ] All tests pass
- [ ] README.md reflects new branding and features
- [ ] Ready for Phase 3 (UX features)

**Manual Acceptance Test**:
```
Fresh install on a clean VM:
1. No prior version installed
2. Run install script
3. Verify shortcut name: "Wallpaper Sync"
4. Launch app
5. Check tray tooltip: "Wallpaper Sync"
6. Check About dialog: "Wallpaper Sync"
7. Check file paths:
   - %TEMP%\WallpaperSync\
   - %LOCALAPPDATA%\WallpaperSync\
```

All items above must show "Wallpaper Sync", not "Weather".

---

**END OF PHASE 2 STORIES**
