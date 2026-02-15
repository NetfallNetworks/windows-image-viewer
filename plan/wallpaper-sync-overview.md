# Wallpaper Sync - Project Overview

**Project**: Transform "Weather Wallpaper" into "Wallpaper Sync"
**Created**: 2026-02-15
**Status**: Ready for Implementation
**Epic**: UX Refactoring & Production Readiness

---

## Context

The current "Weather Wallpaper" application is functional but has critical security gaps and requires manual JSON editing for configuration. This project transforms it into "Wallpaper Sync" - a polished, user-friendly application that passes the "grandma test" (anyone can configure it without technical knowledge).

**Key Problems Addressed**:
1. **Security**: Extension-only validation allows malicious files
2. **Usability**: Manual JSON editing, no GUI
3. **Reliability**: No last-known-good fallback when downloads fail
4. **Maintainability**: Files accumulate forever, no cleanup
5. **User Experience**: No visual feedback, confusing tray icon behavior

---

## Implementation Strategy

The transformation is organized into **3 incremental, shippable phases**:

### Phase 1: Security & Core Infrastructure
**Priority**: CRITICAL
**Story Points**: 18
**Duration**: 3-4 days

Fix the critical security vulnerability (magic byte validation) and add essential backend services. All changes are backward-compatible.

**Stories**:
- Story WS-1: Image Validation Service (Security Fix)
- Story WS-2: Application State Service
- Story WS-3: Enhanced Configuration Model
- Story WS-4: Wallpaper Fit Modes
- Story WS-5: Last-Known-Good Fallback
- Story WS-6: File Cleanup Service

### Phase 2: Renaming & Branding
**Priority**: HIGH
**Story Points**: 5
**Duration**: 1-2 days

Rebrand from "Weather Wallpaper" to "Wallpaper Sync" throughout the codebase. User-facing changes only; keep assembly names unchanged for backward compatibility.

**Stories**:
- Story WS-7: Rename User-Facing Strings and Paths
- Story WS-8: Update Documentation and Scripts

### Phase 3: UX Features & Settings GUI
**Priority**: HIGH
**Story Points**: 30
**Duration**: 5-7 days

Add complete GUI-based configuration and user experience polish. This is where the "grandma test" is achieved.

**Stories**:
- Story WS-9: Settings Window with Live Preview
- Story WS-10: Welcome Wizard (First-Run Experience)
- Story WS-11: Tray Icon Enhancements
- Story WS-12: Enable/Disable Toggle
- Story WS-13: Startup Toggle Service
- Story WS-14: Reset to Defaults

---

## Total Effort Estimate

- **Story Points**: 53 total
- **Duration**: 9-13 days
- **Team Size**: 1 developer

---

## Success Criteria

**Phase 1 Complete**:
- ✅ Security vulnerability fixed (magic byte validation)
- ✅ No files older than 2 cycles in temp directory
- ✅ All 5 fit modes display correctly
- ✅ Download failures fall back to last-known-good
- ✅ All unit tests pass

**Phase 2 Complete**:
- ✅ No "Weather" strings visible in UI
- ✅ All paths updated to "WallpaperSync"
- ✅ Fresh install works correctly
- ✅ Upgrade from previous version works

**Phase 3 Complete**:
- ✅ **Grandma Test**: Non-technical user can configure without help
- ✅ Left-click opens settings (no double-click needed)
- ✅ URL validation prevents common mistakes
- ✅ Welcome wizard guides first-time setup
- ✅ Visual feedback on enabled/disabled state
- ✅ No notifications on automatic refresh (silent by default)

**Overall Success**:
- ✅ All manual tests pass
- ✅ No regressions in existing functionality
- ✅ App ready for production deployment
- ✅ Code coverage >85%

---

## Story Files

Each story has a detailed implementation plan in:
- `plan/wallpaper-sync-phase-1-stories.md` - Security & Infrastructure (Stories WS-1 to WS-6)
- `plan/wallpaper-sync-phase-2-stories.md` - Renaming & Branding (Stories WS-7 to WS-8)
- `plan/wallpaper-sync-phase-3-stories.md` - UX Features & Settings GUI (Stories WS-9 to WS-14)

---

## Dependencies

**Prerequisites**:
- Phase 1 & Phase 2 of the original roadmap complete (Stories 0-7)
- System Tray App implementation complete
- All existing tests passing

**External Dependencies**:
- None - all features implemented using built-in .NET and Windows APIs

---

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Security Validation | Magic byte checking | Fast, sufficient for security, Windows validates further |
| State Storage | JSON file in %LOCALAPPDATA% | Consistent with config pattern, human-readable, easy reset |
| Fit Mode Implementation | Registry + SystemParametersInfo | Only approach supported by Windows API |
| Settings UI | WPF MVVM (lightweight) | Native, testable, no heavy frameworks needed |
| Image Validation | First 8 bytes only | Performance vs. security trade-off (full parse overkill) |
| Assembly Naming | Keep "WallpaperApp.*" | Backward compatibility, users don't see internal names |

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking existing installations | Medium | High | Keep assembly names, test migration, backward-compatible config |
| Security validation too strict | Low | Low | Support 3 common formats, test with real images |
| Registry writes fail | Low | Medium | Catch exceptions, fall back to default fit mode |
| Settings window UX confusion | Medium | Low | Simple single-page layout, clear labels, user testing |
| Performance degradation | Low | Low | Cleanup runs async, validation <100ms, test performance |

---

## Git Workflow

**Branch**: `claude/generalize-weather-tool-4Qk2L`

**Commit Strategy**:
- Phase 1: Commit per story (6 commits)
- Phase 2: Single commit after complete (1 commit)
- Phase 3: Commit per major feature (4-6 commits)

**Push Strategy**:
```bash
git push -u origin claude/generalize-weather-tool-4Qk2L
```

With exponential backoff retries (2s, 4s, 8s, 16s) on network failures.

---

## Document Status

**Version**: 1.0
**Last Updated**: 2026-02-15
**Status**: Ready for Implementation
**Next Review**: After Phase 1 Complete

---

**END OF OVERVIEW**
