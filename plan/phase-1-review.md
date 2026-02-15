# Phase 1 Architecture Review - Quality Gate

> **HISTORICAL NOTE**: This document reflects the state of the project after Phase 1 completion.
> The Windows Service approach mentioned here was later replaced with a System Tray App.
> See [ADR-005](adr/ADR-005-pivot-service-to-tray-app.md) for details. This document is preserved for historical reference.

**Review Date**: 2026-02-15
**Trigger**: ARCHITECT REVIEW #1 (after Story 5 - Core infrastructure complete)
**Verdict**: PASS with required refactorings before Story 6

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Story Completion Audit](#2-story-completion-audit)
3. [Architecture Assessment](#3-architecture-assessment)
4. [Code-Level Findings](#4-code-level-findings)
5. [Testing Coverage Analysis](#5-testing-coverage-analysis)
6. [Documentation Assessment](#6-documentation-assessment)
7. [Phase 2 Readiness](#7-phase-2-readiness)
8. [Refactoring Backlog](#8-refactoring-backlog)
9. [Decision](#9-decision)

---

## 1. Executive Summary

Phase 1 delivered all 5 foundation stories (Stories 1-5) plus the tech spike (Story 0). The application runs end-to-end: it reads a URL from configuration, downloads an image over HTTPS, and sets it as the Windows desktop wallpaper via P/Invoke.

**What's working well**: Clean service boundaries, interface-driven testability, comprehensive error handling, solid ADR documentation, and a test suite that covers happy paths and failure modes across all components.

**What needs attention before Phase 2**: A few structural issues that will compound if left unaddressed. Most importantly, `ConfigurationService` lacks an interface (breaking the DI pattern used everywhere else), `Program.cs` has duplicated error handling and an unreachable code branch, and `ImageFetcher` mutates the injected `HttpClient`. None of these are showstoppers, but they need fixing before Story 7 introduces `HostBuilder` and proper DI container wiring.

---

## 2. Story Completion Audit

### Story-by-Story Verification

| Story | Title | Acceptance Criteria | Tests Required | Tests Actual | Verdict |
|-------|-------|:-------------------:|:--------------:|:------------:|:-------:|
| 0 | Tech Spike | Spike code + results doc | Manual only | N/A | PASS |
| 1 | Foundation | Console app + first test + publish | 1 | 1 | PASS |
| 2 | Configuration | JSON config + HTTPS validation | 4+ | 5 | PASS |
| 3 | Wallpaper Service | Set wallpaper via P/Invoke + validation | 5+ | 7 | PASS |
| 4 | Image Fetcher | HTTP download + timeout + error handling | 6+ | 8 | PASS |
| 5 | Integration | End-to-end orchestration | 3+ | 7 | PASS |

**Total automated tests**: 28 (across 5 test classes, excluding collection definition)
**All tests passing**: Yes

### Definition of Done Compliance

Checked against the project's own Definition of Done from `STORY_MAP.md`:

- [x] All acceptance criteria met per story
- [x] All automated tests pass
- [x] Manual test checklists completed (where applicable)
- [x] Code reviewed for naming, clarity, SOLID principles
- [x] No compiler warnings
- [x] Documentation updated (README, code comments)
- [x] Application runs end-to-end without errors
- [x] Ready for next story (no blockers introduced)

---

## 3. Architecture Assessment

### 3.1 Separation of Concerns

The codebase follows a layered architecture with clear responsibility boundaries:

```
Program.cs (Entry Point / Composition Root)
    |
    v
WallpaperUpdater (Orchestration)
    |
    +---> ConfigurationService (Configuration Layer)
    +---> ImageFetcher (HTTP/Network Layer)
    +---> WallpaperService (OS Integration Layer)
```

**Assessment**: This layering is appropriate for the application's complexity. Each service has a single reason to change: `ConfigurationService` changes if the config format changes, `ImageFetcher` changes if the download strategy changes, `WallpaperService` changes if the Windows API changes.

The orchestrator (`WallpaperUpdater`) correctly delegates all work and only coordinates. It does not perform downloads, set wallpaper, or validate configuration itself.

### 3.2 Interface Design and Testability

| Component | Has Interface | Mockable | Tests Use Mocks |
|-----------|:------------:|:--------:|:---------------:|
| `WallpaperService` | `IWallpaperService` | Yes | Yes (in WallpaperUpdaterTests) |
| `ImageFetcher` | `IImageFetcher` | Yes | Yes (in WallpaperUpdaterTests) |
| `ConfigurationService` | **No** | Partial | No (uses real impl + temp files) |
| `HttpClient` | N/A (framework) | Via handler | Yes (Moq.Protected on HttpMessageHandler) |

**Finding [F-01]**: `ConfigurationService` is the only service without an interface. `WallpaperUpdater` takes a concrete `ConfigurationService` in its constructor (`WallpaperUpdater.cs:10`), which breaks the Dependency Inversion Principle. The `WallpaperUpdaterTests` are forced to use the real `ConfigurationService` and write temp config files to disk, adding filesystem coupling to what should be pure unit tests.

### 3.3 Dependency Injection Readiness

Currently, `Program.cs` manually constructs all services with `new`:

```csharp
// Program.cs:40-44
using var httpClient = new HttpClient();
var imageFetcher = new ImageFetcher(httpClient);
var wallpaperService = new WallpaperService();
var updater = new WallpaperUpdater(configService, imageFetcher, wallpaperService);
```

This is fine for Phase 1 (console app), but Story 7 will introduce `Microsoft.Extensions.Hosting` and `BackgroundService`, which use a DI container. The current constructor signatures are compatible with DI registration - **except** for `ConfigurationService`, which has no interface.

**Assessment**: The service constructors are DI-ready. The missing `IConfigurationService` interface is the only blocker.

### 3.4 Error Handling Patterns

The error handling strategy is consistent and well-implemented:

- **Services** throw domain-specific exceptions (`ConfigurationException`, `InvalidImageException`, `WallpaperException`)
- **ImageFetcher** catches exceptions internally and returns `null` (per ADR-003: no retries)
- **WallpaperUpdater** catches all exception types and returns `bool` (success/failure)
- **Program.cs** catches all exception types and maps to exit codes (0/1)

**Finding [F-02]**: The same 5 catch blocks appear in both `Program.cs:103-145` and `WallpaperUpdater.cs:62-101`. When the default code path runs (no args), `Program.cs` calls `UpdateWallpaperAsync()` which already catches every exception and returns false - then `Program.cs` catches the same exceptions again. The outer catch blocks in `Program.cs` are only reachable for the `--download` and `<path>` branches, but the pattern is misleading. This should be restructured so each branch owns its own error handling cleanly.

### 3.5 Review Questions (from STORY_MAP)

**Does the design support adding Windows Service later?**
Yes. The service layer is cleanly separated from `Program.cs`. Story 7 will move orchestration into a `BackgroundService.ExecuteAsync()`, and the existing `WallpaperUpdater` can be called directly. The missing `IConfigurationService` interface is the only prep work needed.

**Are responsibilities clearly separated (SRP)?**
Yes, with one exception: `ImageFetcher` contains private logging methods (`LogInformation`, `LogWarning`, `LogError`) that are a placeholder concern. These are marked with `TODO: Replace with proper logging in Story 8`, which is the right call.

**Are abstractions appropriate (not over-engineered)?**
Yes. Interfaces exist only where they enable testability (`IWallpaperService`, `IImageFetcher`). There are no unnecessary abstractions, no factory patterns, no generic repositories. The complexity matches the problem.

**Is the code testable without excessive mocking?**
Mostly yes. `WallpaperUpdaterTests` mock `IImageFetcher` and `IWallpaperService` cleanly. `ImageFetcherTests` mock `HttpMessageHandler` at the correct seam. The exception is `ConfigurationService`, which requires temp file creation in tests due to its missing interface.

**Does error handling follow consistent patterns?**
Yes across the service layer. The duplication in `Program.cs` is the only inconsistency.

---

## 4. Code-Level Findings

### Critical (must fix before Phase 2)

#### [F-01] ConfigurationService lacks an interface
**Location**: `src/WallpaperApp/Configuration/ConfigurationService.cs`
**Impact**: Violates DI pattern, forces filesystem coupling in WallpaperUpdater tests, blocks clean DI registration for Story 7.

**What to do**:
1. Create `IConfigurationService` with one method: `AppSettings LoadConfiguration()`
2. Have `ConfigurationService` implement it
3. Change `WallpaperUpdater` constructor to accept `IConfigurationService` instead of the concrete class
4. Update `WallpaperUpdaterTests` to use a mock `IConfigurationService` - this eliminates all the temp config file creation in those tests

#### [F-03] ImageFetcher mutates the injected HttpClient
**Location**: `src/WallpaperApp/Services/ImageFetcher.cs:20`
```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(30);
```
**Impact**: Side effect on a shared dependency. When Story 7 introduces `IHttpClientFactory`, the factory-provided `HttpClient` should not be mutated by consumers. If two services share an `HttpClient`, this mutation affects both.

**What to do**: Move the timeout configuration to the point of `HttpClient` creation in `Program.cs`, or (better) configure it via `IHttpClientFactory` registration when Story 7 adds the DI container. For now, set the timeout where the `HttpClient` is created in `Program.cs:40`.

### Moderate (fix before Phase 2)

#### [F-02] Duplicated error handling between Program.cs and WallpaperUpdater
**Location**: `Program.cs:103-145` and `WallpaperUpdater.cs:62-101`
**Impact**: The same 5 catch blocks are copy-pasted. On the default path (no args), the `Program.cs` catches are unreachable because `WallpaperUpdater` already swallows all exceptions. Adding a new exception type requires changes in two places.

**What to do**: Restructure `Program.cs` so each branch has its own focused error handling. The default (Story 5) path only needs to check the `bool` return from `UpdateWallpaperAsync()`. The `--download` and `<path>` paths should have their own try/catch since they don't go through the updater.

#### [F-04] Unreachable code branch in Program.cs
**Location**: `Program.cs:86-98` (the final `else` block)
```csharp
else if (args.Length > 0)  // Line 75 - catches all remaining args
{
    // ... Story 3 path ...
}
else  // Line 86 - UNREACHABLE
{
    Console.WriteLine("USAGE:");
    // ...
}
```
**Impact**: The usage text can never be displayed. The first condition checks `args.Length == 0` (line 35), then `args[0] == "--download"` (line 50), then `args.Length > 0` (line 75) which is always true at that point. The final `else` at line 86 is dead code.

**What to do**: Remove the dead `else` block. If you want a `--help` flag, add it as an explicit check (e.g., `args[0] == "--help"`).

#### [F-05] Timestamp-based filename uniqueness is fragile
**Location**: `src/WallpaperApp/Services/ImageFetcher.cs:88-91`
```csharp
string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
return $"wallpaper-{timestamp}.png";
```
**Impact**: Two downloads within the same second produce the same filename, causing the second to silently overwrite the first. The test for uniqueness (`ImageFetcherTests.cs:226`) works around this with a 1.1-second `Task.Delay`, which confirms the weakness.

**What to do**: Add milliseconds to the timestamp format: `"yyyyMMdd-HHmmss-fff"`. Alternatively, append a short random suffix. This is low-priority since the 15-minute refresh interval makes collisions unlikely in production, but it's a latent bug that could surface in testing or if the timer interval is reduced.

### Low (address when convenient)

#### [F-06] Test cleanup boilerplate is duplicated across 4 test classes
**Location**: `ProgramTests.cs`, `ConfigurationServiceTests.cs`, `WallpaperServiceTests.cs`, `WallpaperUpdaterTests.cs`
**Impact**: Every test class has an identical ~40-line `Dispose()` method that saves/restores the current directory, deletes temp directories, and handles cleanup failures. This is copy-pasted code.

**What to do**: Extract a `TestFixtureBase` class (or a shared helper) that handles temp directory creation, `Directory.SetCurrentDirectory`, and cleanup. Each test class extends or uses it. This reduces each test class by ~40 lines and ensures cleanup logic stays consistent.

#### [F-07] ProgramTests.ApplicationStartsSuccessfully makes real HTTP calls
**Location**: `src/WallpaperApp.Tests/ProgramTests.cs:84`
```csharp
var exitCode = Program.Main(new string[] { });
```
**Impact**: This calls the real `Program.Main` with no args, which triggers the full Story 5 path: load config, create a real `HttpClient`, download from `https://weather.zamflam.com/latest.png`, and attempt to set wallpaper. This test makes a real network call and depends on an external server being up. It will fail in offline/CI environments.

**What to do**: This test is currently passing because the default (no-args) path goes through `WallpaperUpdater` which catches all exceptions and returns false, and `Program.cs` maps that to exit code 1 - wait, the test asserts `exitCode == 0`, which means it's passing because the real HTTP call to weather.zamflam.com is succeeding. This is brittle. Either:
- Convert this to an integration test that's opt-in (e.g., `[Fact(Skip = "Integration")]` or a separate test category), or
- Mock the HTTP layer by restructuring `Program.Main` to accept dependencies, or
- Change the test to verify startup behavior without triggering the full pipeline (e.g., pass `--help` once [F-04] is fixed)

---

## 5. Testing Coverage Analysis

### 5.1 Coverage by Component

| Component | Test Class | Test Count | Happy Path | Error/Edge Cases | Assessment |
|-----------|-----------|:----------:|:----------:|:----------------:|:----------:|
| `Program` | `ProgramTests` | 1 | 1 | 0 | Weak |
| `ConfigurationService` | `ConfigurationServiceTests` | 5 | 1 | 4 | Strong |
| `WallpaperService` | `WallpaperServiceTests` | 7 | 2 | 5 | Strong |
| `ImageFetcher` | `ImageFetcherTests` | 8 | 4 | 4 | Strong |
| `WallpaperUpdater` | `WallpaperUpdaterTests` | 7 | 1 | 6 | Strong |

### 5.2 What's Covered Well

**ConfigurationService** (5 tests):
- Valid config loads correctly
- Missing file produces actionable error
- Non-HTTPS URL rejected
- Empty URL rejected
- Null/missing URL rejected
- All validation rules have corresponding tests

**WallpaperService** (7 tests):
- Missing file detection
- Invalid format rejection with message
- Relative-to-absolute path resolution
- 7 supported format variations (Theory with InlineData)
- 4 unsupported format variations (Theory with InlineData)
- Platform-aware assertions (Windows vs. non-Windows)

**ImageFetcher** (8 tests):
- Successful download returns file path and writes to disk
- HTTP 404 returns null
- HTTP 500 returns null
- Timeout returns null
- Network error (HttpRequestException) returns null
- Saves to correct temp directory
- Generates unique filenames (with timestamp pattern validation)
- Creates directory if it doesn't exist

**WallpaperUpdater** (7 tests):
- Happy path: config loads, image downloads, wallpaper sets
- Download failure: returns false, wallpaper service never called
- SetWallpaper failure: returns false after download succeeds
- Config failure: returns false, no download or wallpaper attempted
- Null constructor args: all 3 parameters validated with ArgumentNullException
- Correct call ordering verified via mock verification

### 5.3 Coverage Gaps

| Gap | Risk | Recommendation |
|-----|------|----------------|
| `Program.cs` has 1 test that makes real network calls | Test is brittle, will fail offline | Restructure as described in [F-07] |
| No test for `--download` code path | Medium - untested branch | Add test calling `Program.Main(new[] { "--download" })` with mocked HTTP |
| No test for `<path>` (Story 3) code path | Medium - untested branch | Add test calling `Program.Main(new[] { "test.bmp" })` with a real temp file |
| No test for invalid args / unknown flags | Low | Not critical since the unreachable else handles this (see [F-04]) |
| `ImageFetcher` - concurrent downloads same second | Low | Covered by [F-05] fix |
| No test for malformed JSON (not missing, but corrupt) | Low | `ConfigurationBuilder` handles this; worth one test |
| No test for very large image downloads | Low | Not a concern for Phase 1 scope |

### 5.4 Test Infrastructure Quality

**What's good**:
- xUnit + Moq is an industry-standard combination
- `[Collection("CurrentDirectory Tests")]` with `DisableParallelization = true` correctly prevents test interference when mutating `Directory.SetCurrentDirectory()`
- Tests create minimal BMP files (70 bytes) with correct headers instead of embedding large test assets
- Consistent Arrange/Act/Assert pattern throughout
- Mock verification confirms correct call ordering (e.g., wallpaper service not called when download fails)
- `IDisposable` pattern for test cleanup

**What could improve**:
- Duplicated cleanup boilerplate (see [F-06])
- The `GeneratesUniqueFilename` test uses `Task.Delay(1100)` which slows the suite and papers over [F-05]
- Consider adopting test naming convention consistently: some tests use `Method_Condition_Result` (good), but names could be more specific in places

### 5.5 Testing Strategy for Phase 2

Phase 2 (Stories 6-7) introduces concurrency (timers) and OS integration (Windows Service). The current test infrastructure supports this well because:

- Services are already behind interfaces, so `TimerService` and `Worker` can mock `WallpaperUpdater` (once it has an interface or stays injectable)
- The `[Collection]` pattern handles shared-state tests
- Platform-aware assertions (Windows vs. non-Windows) are already established

**Recommendations for Phase 2 testing**:
- Introduce `IWallpaperUpdater` interface for `WallpaperUpdater` so `Worker`/`TimerService` tests can mock it
- Use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` (.NET 8) for timer tests instead of real delays
- Add an integration test category (e.g., `[Trait("Category", "Integration")]`) and keep network-dependent tests separate from unit tests
- Run `dotnet test --collect:"XPlat Code Coverage"` and review the Cobertura report to quantify line/branch coverage

---

## 6. Documentation Assessment

### 6.1 Documentation Inventory

| Document | Location | Lines | Purpose | Quality |
|----------|----------|:-----:|---------|:-------:|
| README.md | Root | 253 | Build, run, configure | Strong |
| STORY_MAP.md | Root | 1,032 | Full project roadmap | Excellent |
| spike-results.md | plan/ | 180 | API validation findings | Strong |
| story-0-implementation-plan.md | plan/ | 270 | Spike execution guide | Good |
| story-1-implementation-plan.md | plan/ | 511 | Foundation setup guide | Good |
| ADR-001 through ADR-004 | plan/adr/ | ~400 total | Architecture decisions | Excellent |
| ADR INDEX.md | plan/adr/ | - | Decision catalog | Good |
| scripts/README.md | scripts/ | 175 | Build script usage | Good |

### 6.2 What's Strong

**Architecture Decision Records**: The ADR system is well-implemented. Each ADR includes context, decision, consequences (both positive and negative), alternatives considered, and implementation guidance. ADR-003 (No Retry Logic) is particularly well-reasoned - it explains *why* the simpler approach is correct rather than just stating the decision.

**STORY_MAP.md**: This is a genuinely useful living document. It defines clear acceptance criteria, testing requirements with code examples, and a Definition of Done checklist. The Uncle Bob Principles Checklist and Agent Engineer Execution Guide provide actionable guardrails.

**Code-level documentation**: XML doc comments on all public interfaces and classes. Exception documentation in interface contracts tells callers exactly what can be thrown and why.

### 6.3 What Could Improve

| Item | Current State | Recommendation |
|------|--------------|----------------|
| No `CHANGELOG.md` | Changes tracked via git log only | Add a changelog; keeps stakeholders informed without reading git history |
| README has "Story 3/4/5" references | Internal language in user-facing doc | Replace story numbers with feature descriptions (e.g., "Set Local Image", "Download Image", "Fetch and Set") |
| No API/class documentation | XML comments exist but no generated docs | Not needed now, but consider for Phase 3 |
| STORY_MAP references `appsettings.json` | Config file is actually `WallpaperApp.json` | Update STORY_MAP to match the actual filename |

### 6.4 Documentation Approach Going Forward

The current approach is well-calibrated: heavy documentation where decisions matter (ADRs, story requirements), light documentation where code speaks for itself (inline comments only where non-obvious). Continue this pattern:

- **New ADRs needed for Phase 2**: Timer strategy (Story 6), Service hosting model (Story 7)
- **Update README** when Story 6 changes the runtime behavior (continuous vs. one-shot)
- **Implementation plans** for Stories 6-7 should follow the same template as story-0/story-1 plans

---

## 7. Phase 2 Readiness

### 7.1 Readiness Checklist

| Criterion | Status | Notes |
|-----------|:------:|-------|
| All Phase 1 stories complete | PASS | 6/6 stories delivered |
| All tests passing | PASS | 28 tests, all green |
| Architecture supports Story 6 (Timer) | PASS | `WallpaperUpdater.UpdateWallpaperAsync()` is the correct call target for a timer callback |
| Architecture supports Story 7 (Service) | CONDITIONAL | Needs `IConfigurationService` interface first; rest is clean |
| Error handling is solid | PASS | Consistent patterns across all services |
| No unresolved tech debt blocking Phase 2 | CONDITIONAL | See Refactoring Backlog below |

### 7.2 Story 6 (Timer) Impact Analysis

Story 6 adds a `TimerService` that calls `WallpaperUpdater.UpdateWallpaperAsync()` on an interval. The current codebase supports this with minimal changes:

- `WallpaperUpdater` is already async
- `WallpaperUpdater` already catches all exceptions and returns bool (safe for timer callbacks)
- `AppSettings.RefreshIntervalMinutes` is already loaded and available
- `Program.cs` needs restructuring to stay running (currently exits after one run)

**Pre-requisite from this review**: Fix [F-01] (add `IConfigurationService`) so the timer service can be properly unit tested.

### 7.3 Story 7 (Windows Service) Impact Analysis

Story 7 converts the app to use `Microsoft.Extensions.Hosting`. This is the biggest architectural shift in the project. The current codebase is well-positioned because:

- All services have constructor injection (DI-compatible)
- Service interfaces exist for `IWallpaperService` and `IImageFetcher`
- The orchestrator (`WallpaperUpdater`) is stateless and re-entrant

**Pre-requisites from this review**:
1. Fix [F-01] - `IConfigurationService` for DI registration
2. Fix [F-03] - Remove `HttpClient` mutation from `ImageFetcher`
3. Consider whether `WallpaperUpdater` itself needs an interface for `Worker` to mock it

---

## 8. Refactoring Backlog

Ordered by priority. All items should be completed before starting Story 6.

### Must Do (before Story 6)

| ID | Finding | Effort | Files Changed |
|----|---------|--------|---------------|
| [F-01] | Extract `IConfigurationService` interface | Small | 3 files: new interface, update `ConfigurationService`, update `WallpaperUpdater` constructor |
| [F-03] | Move `HttpClient.Timeout` out of `ImageFetcher` constructor | Small | 2 files: `ImageFetcher.cs`, `Program.cs` |
| [F-02] | Restructure `Program.cs` error handling to eliminate duplication | Medium | 1 file: `Program.cs` |
| [F-04] | Remove unreachable else block in `Program.cs` | Small | 1 file: `Program.cs` |

### Should Do (before Story 6 if possible)

| ID | Finding | Effort | Files Changed |
|----|---------|--------|---------------|
| [F-07] | Fix `ProgramTests` to not make real HTTP calls | Medium | 1 file: `ProgramTests.cs` |
| [F-05] | Add milliseconds to temp filename format | Small | 1 file: `ImageFetcher.cs` |
| [F-06] | Extract shared test fixture base class | Medium | 5 files: new base class + 4 test classes |
| - | Add tests for `--download` and `<path>` Program.cs branches | Medium | 1 file: `ProgramTests.cs` |

### Nice to Have (can defer)

| Item | Effort |
|------|--------|
| Add `CHANGELOG.md` | Small |
| Remove "Story N:" prefixes from user-facing console output | Small |
| Update STORY_MAP.md references from `appsettings.json` to `WallpaperApp.json` | Small |

---

## 9. Decision

### Gate Verdict: PASS (conditional)

Phase 1 is well-executed. The foundation is solid: clean service boundaries, strong test coverage on business logic, interface-driven design where it matters, and thorough documentation. The codebase follows the project's own Uncle Bob principles and SOLID guidelines with only minor deviations.

**Condition**: Complete the "Must Do" refactoring items from the backlog above before starting Story 6. These are structural fixes that will compound if deferred - particularly [F-01] (`IConfigurationService`), which is a prerequisite for clean DI registration in Story 7.

### Risks Accepted

- Temp file cleanup is not implemented (by design - per Story 4 acceptance criteria)
- `ProgramTests` makes real network calls (acceptable for now, should fix before CI pipeline exists)
- No code coverage metrics generated yet (add `dotnet test --collect:"XPlat Code Coverage"` to build scripts)

### What Goes Into Phase 2

With the "Must Do" refactoring complete, the codebase will be ready for:
- **Story 6**: Add `TimerService`, restructure `Program.cs` for continuous operation
- **Story 7**: Introduce `HostBuilder`, register services in DI container, create `Worker : BackgroundService`

---

*End of Phase 1 Architecture Review*
