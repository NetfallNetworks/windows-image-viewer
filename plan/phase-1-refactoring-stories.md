# Phase 1 Refactoring Stories

> **HISTORICAL NOTE**: This document reflects the state of the project after Phase 1 completion.
> The Windows Service approach mentioned here was later replaced with a System Tray App.
> See [ADR-005](adr/ADR-005-pivot-service-to-tray-app.md) for details. This document is preserved for historical reference.

**Source**: Phase 1 Architecture Review (plan/phase-1-review.md)
**Created**: 2026-02-15
**Status**: Ready for Implementation
**Target Completion**: Before Story 6 begins

---

## Story Priority

### Critical Path (Required before Story 6)
- **Story R1**: Extract IConfigurationService Interface
- **Story R2**: Fix HttpClient Timeout Configuration
- **Story R3**: Restructure Program.cs Error Handling
- **Story R4**: Remove Unreachable Code in Program.cs

### High Priority (Recommended before Story 6)
- **Story R5**: Fix Integration Test Network Dependency
- **Story R6**: Improve Filename Uniqueness
- **Story R7**: Extract Shared Test Fixture

### Medium Priority (Can defer to Phase 2)
- **Story R8**: Add Missing Program.cs Branch Tests

---

## Story R1: Extract IConfigurationService Interface

**Epic**: Phase 1 Refactoring
**Story Points**: 2
**Type**: Technical Debt / Refactoring
**Priority**: CRITICAL (blocks Story 7 DI container)

### Context

From [F-01] in architecture review: `ConfigurationService` is the only service without an interface, violating the Dependency Inversion Principle. `WallpaperUpdater` takes a concrete `ConfigurationService` in its constructor, forcing tests to write temp config files to disk instead of using mocks.

### Description

Create `IConfigurationService` interface and refactor `WallpaperUpdater` to depend on the abstraction instead of the concrete implementation. This enables clean dependency injection registration for Story 7 (Windows Service) and improves testability of `WallpaperUpdater`.

### Tasks

- [ ] Create `Configuration/IConfigurationService.cs` interface with one method:
  ```csharp
  public interface IConfigurationService
  {
      AppSettings LoadConfiguration();
  }
  ```
- [ ] Update `ConfigurationService` to implement `IConfigurationService`
- [ ] Change `WallpaperUpdater` constructor parameter from `ConfigurationService` to `IConfigurationService`
- [ ] Update `Program.cs` to continue using concrete `ConfigurationService` (no change needed)
- [ ] Refactor `WallpaperUpdaterTests` to use `Mock<IConfigurationService>` instead of temp files:
  - Remove `CreateValidConfiguration()` helper
  - Remove file I/O from test setup
  - Mock `LoadConfiguration()` to return test `AppSettings` objects
  - Update all 7 tests to use the mock
- [ ] Verify all tests still pass
- [ ] Run full test suite to ensure no regressions

### Acceptance Criteria

- [ ] `IConfigurationService` interface exists in `Configuration/` namespace
- [ ] `ConfigurationService` implements `IConfigurationService`
- [ ] `WallpaperUpdater` constructor signature is:
  ```csharp
  public WallpaperUpdater(
      IConfigurationService configurationService,
      IImageFetcher imageFetcher,
      IWallpaperService wallpaperService)
  ```
- [ ] `WallpaperUpdaterTests` no longer writes files to disk
- [ ] All 28 tests pass (no regressions)
- [ ] `ConfigurationServiceTests` still uses the real implementation (5 tests unchanged)

### Testing Requirements

**Existing tests to update**:
```csharp
// WallpaperUpdaterTests.cs - Example refactored test
[Fact]
public async Task UpdateWallpaperAsync_HappyPath_SucceedsEndToEnd()
{
    // Arrange
    var mockConfig = new Mock<IConfigurationService>();
    mockConfig.Setup(c => c.LoadConfiguration())
        .Returns(new AppSettings
        {
            ImageUrl = "https://weather.zamflam.com/latest.png",
            RefreshIntervalMinutes = 15
        });

    string testImagePath = Path.Combine(_testDirectory, "test-image.png");
    _mockImageFetcher
        .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
        .ReturnsAsync(testImagePath);

    _mockWallpaperService
        .Setup(w => w.SetWallpaper(testImagePath))
        .Verifiable();

    var updater = new WallpaperUpdater(
        mockConfig.Object,
        _mockImageFetcher.Object,
        _mockWallpaperService.Object);

    // Act
    var result = await updater.UpdateWallpaperAsync();

    // Assert
    Assert.True(result);
    mockConfig.Verify(c => c.LoadConfiguration(), Times.Once);
    _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
    _mockWallpaperService.Verify(w => w.SetWallpaper(testImagePath), Times.Once);
}
```

**No new tests required** - this is a pure refactoring with behavior preservation.

### Definition of Done

- [x] Interface follows .NET naming conventions (IConfigurationService)
- [x] No change to `ConfigurationService` behavior (only adds interface implementation)
- [x] `WallpaperUpdater` logic unchanged (only constructor parameter type changes)
- [x] Test cleanup: `WallpaperUpdaterTests` no longer has filesystem coupling
- [x] All tests pass
- [x] No compiler warnings
- [x] Code builds in Release mode
- [x] Changes committed with clear message

### Notes

- This is a prerequisite for Story 7's DI container registration
- Do NOT add an interface to `WallpaperUpdater` itself in this story - that's a separate decision for Story 6/7
- The interface should live in the `Configuration` namespace alongside `ConfigurationService`

---

## Story R2: Fix HttpClient Timeout Configuration

**Epic**: Phase 1 Refactoring
**Story Points**: 1
**Type**: Technical Debt / Bug Fix
**Priority**: CRITICAL (blocks Story 7 IHttpClientFactory)

### Context

From [F-03] in architecture review: `ImageFetcher` mutates the injected `HttpClient`'s timeout in its constructor (`_httpClient.Timeout = TimeSpan.FromSeconds(30)`). This is a side effect on a shared dependency. When Story 7 introduces `IHttpClientFactory`, the factory-provided `HttpClient` should not be mutated by consumers.

### Description

Move the 30-second timeout configuration to the point where `HttpClient` is created in `Program.cs`. Remove the mutation from `ImageFetcher` constructor. This follows the principle that consumers should not modify injected dependencies.

### Tasks

- [ ] Remove line 20 from `ImageFetcher.cs`:
  ```csharp
  // DELETE THIS LINE
  _httpClient.Timeout = TimeSpan.FromSeconds(30);
  ```
- [ ] Update `Program.cs` to configure timeout at creation (3 locations):
  ```csharp
  // Story 5 path (line ~40)
  using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

  // Story 4 path (line ~56)
  using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
  ```
- [ ] Verify tests still pass (they mock the handler, so timeout isn't tested)
- [ ] Add a comment in `ImageFetcher.cs` constructor documenting the expected timeout:
  ```csharp
  /// <summary>
  /// Initializes a new instance of ImageFetcher with an HttpClient.
  /// </summary>
  /// <param name="httpClient">The HttpClient to use for downloads.
  /// Should be configured with a 30-second timeout.</param>
  public ImageFetcher(HttpClient httpClient)
  ```

### Acceptance Criteria

- [ ] `ImageFetcher` constructor does not modify `httpClient` properties
- [ ] `Program.cs` creates `HttpClient` with 30-second timeout in both code paths
- [ ] All tests pass (no behavior change)
- [ ] XML doc comment documents the timeout expectation

### Testing Requirements

**No new tests required** - existing `ImageFetcherTests` mock the `HttpMessageHandler`, so they don't validate timeout behavior. The timeout is an integration-level concern.

Optional (future enhancement): Add an integration test that verifies timeout behavior with a slow server.

### Definition of Done

- [x] No side effects in `ImageFetcher` constructor
- [x] Timeout configured at dependency creation site
- [x] All tests pass
- [x] XML doc comment updated
- [x] Code review: confirm no other injected dependencies are mutated

---

## Story R3: Restructure Program.cs Error Handling

**Epic**: Phase 1 Refactoring
**Story Points**: 3
**Type**: Code Smell / Refactoring
**Priority**: HIGH (reduces maintenance burden)

### Context

From [F-02] in architecture review: The same 5 catch blocks are duplicated between `Program.cs:103-145` and `WallpaperUpdater.cs:62-101`. On the default path (no args), `WallpaperUpdater.UpdateWallpaperAsync()` already catches all exceptions and returns false, so the outer catch blocks in `Program.cs` are unreachable for that branch. This duplication is misleading and makes adding new exception types require changes in two places.

### Description

Restructure `Program.cs` so each execution branch has its own focused error handling. The default (Story 5) path should only check the `bool` return from `UpdateWallpaperAsync()`. The `--download` and `<path>` paths should have their own try/catch blocks since they don't go through the updater.

### Tasks

- [ ] Refactor the default path (no args) to eliminate outer try/catch:
  ```csharp
  if (args.Length == 0)
  {
      // Load config ONCE here (can fail)
      var settings = configService.LoadConfiguration();

      using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
      var imageFetcher = new ImageFetcher(httpClient);
      var wallpaperService = new WallpaperService();
      var updater = new WallpaperUpdater(configService, imageFetcher, wallpaperService);

      bool success = await updater.UpdateWallpaperAsync();
      return success ? 0 : 1;
  }
  ```
- [ ] Move config loading inside each branch so it only happens when needed
- [ ] Add try/catch around the `--download` path with focused error handling:
  ```csharp
  else if (args.Length > 0 && args[0] == "--download")
  {
      try
      {
          var settings = configService.LoadConfiguration();
          using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
          var imageFetcher = new ImageFetcher(httpClient);
          var downloadedPath = await imageFetcher.DownloadImageAsync(settings.ImageUrl);
          // ... handle result ...
      }
      catch (ConfigurationException ex)
      {
          Console.Error.WriteLine($"❌ Configuration Error: {ex.Message}");
          return 1;
      }
  }
  ```
- [ ] Add try/catch around the `<path>` (Story 3) path:
  ```csharp
  else if (args.Length > 0)
  {
      try
      {
          string testImagePath = args[0];
          var wallpaperService = new WallpaperService();
          wallpaperService.SetWallpaper(testImagePath);
          Console.WriteLine("✓ Wallpaper set successfully");
          return 0;
      }
      catch (FileNotFoundException ex) { /* ... */ }
      catch (InvalidImageException ex) { /* ... */ }
      catch (WallpaperException ex) { /* ... */ }
      catch (Exception ex) { /* ... */ }
  }
  ```
- [ ] Remove the outer try/catch block that wraps all branches (lines 103-145)
- [ ] Verify behavior is unchanged for all 3 execution paths
- [ ] Run manual tests for all 3 modes

### Acceptance Criteria

- [ ] Default path (no args) has no try/catch - relies on `UpdateWallpaperAsync()` error handling
- [ ] `--download` path has its own try/catch with `ConfigurationException` handling
- [ ] `<path>` path has its own try/catch with `FileNotFoundException`, `InvalidImageException`, `WallpaperException`
- [ ] Config is only loaded when needed (not for Story 3 path)
- [ ] Error messages remain user-friendly and actionable
- [ ] All 3 execution modes return correct exit codes (0 = success, 1 = failure)

### Testing Requirements

**Manual Testing Checklist**:
- [ ] Run with no args (Story 5): verify wallpaper updates or error displays
- [ ] Run with `--download`: verify download succeeds or error displays
- [ ] Run with `<path-to-image>`: verify wallpaper sets or error displays
- [ ] Run with invalid config: verify error message clarity
- [ ] Run with network down: verify graceful failure

**Automated Tests** (from Story R8):
After this refactoring, Story R8 should add automated tests for the `--download` and `<path>` branches.

### Definition of Done

- [x] No duplicated catch blocks between `Program.cs` and `WallpaperUpdater.cs`
- [x] Each execution branch has focused, minimal error handling
- [x] Config loading happens only in branches that need it
- [x] Manual test checklist 100% passed
- [x] Code is more maintainable (adding new exception type requires change in 1 place)
- [x] All existing tests still pass

### Notes

- This refactoring also addresses part of [F-04] - you'll remove the outer `else` as part of this restructure
- Consider extracting the 3 execution paths into separate methods (`RunDefaultMode()`, `RunDownloadMode()`, `RunSetWallpaperMode()`) if `MainAsync()` gets too long
- The startup banner and config path logging should happen before branching

---

## Story R4: Remove Unreachable Code in Program.cs

**Epic**: Phase 1 Refactoring
**Story Points**: 1
**Type**: Bug Fix / Dead Code
**Priority**: HIGH (covered by Story R3)

### Context

From [F-04] in architecture review: The final `else` block at `Program.cs:86-98` is unreachable. The first condition checks `args.Length == 0` (line 35), then `args[0] == "--download"` (line 50), then `args.Length > 0` (line 75) which catches all remaining cases. The final `else` can never execute.

### Description

Remove the unreachable usage text `else` block. If you want to display usage, add an explicit `--help` flag check.

### Tasks

**NOTE**: This story is absorbed into Story R3. When restructuring error handling, you'll naturally eliminate this dead code. This story exists for tracking purposes but will be completed as part of R3.

- [ ] Remove lines 86-98 (the unreachable `else` block)
- [ ] Optional: Add a `--help` flag:
  ```csharp
  else if (args.Length > 0 && args[0] == "--help")
  {
      Console.WriteLine("USAGE:");
      Console.WriteLine("  WallpaperApp.exe           - Fetch and set wallpaper");
      Console.WriteLine("  WallpaperApp.exe --download - Download image only");
      Console.WriteLine("  WallpaperApp.exe <path>    - Set local image as wallpaper");
      return 0;
  }
  ```

### Acceptance Criteria

- [ ] No unreachable `else` block in `Program.cs`
- [ ] Optional: `--help` flag displays usage and exits with code 0
- [ ] Application behavior unchanged for existing usage patterns

### Definition of Done

- [x] Dead code removed
- [x] No compiler warnings about unreachable code
- [x] Manual test: verify all execution paths still work

---

## Story R5: Fix Integration Test Network Dependency

**Epic**: Phase 1 Refactoring
**Story Points**: 3
**Type**: Test Improvement
**Priority**: HIGH (blocks CI/CD pipeline)

### Context

From [F-07] in architecture review: `ProgramTests.ApplicationStartsSuccessfully` calls the real `Program.Main()` which makes a real HTTP request to `https://weather.zamflam.com/latest.png`. This test will fail in offline environments, in CI pipelines without network access, or if the external server is down.

### Description

Convert `ProgramTests.ApplicationStartsSuccessfully` to either: (1) an opt-in integration test that's skipped in CI, or (2) a unit test that doesn't make real network calls. Given that Story R3 is restructuring `Program.cs`, the best approach is to wait for that refactoring, then add a `--help` test that doesn't trigger network I/O.

### Tasks

- [ ] **Option A** (Recommended after Story R3): Replace the test with a `--help` test:
  ```csharp
  [Fact]
  public void ApplicationStartsSuccessfully_HelpFlag()
  {
      // Arrange & Act
      var exitCode = Program.Main(new[] { "--help" });

      // Assert
      Assert.Equal(0, exitCode);
  }
  ```

- [ ] **Option B**: Mark the existing test as an integration test and skip by default:
  ```csharp
  [Fact(Skip = "Integration test - requires network access")]
  public void ApplicationStartsSuccessfully_IntegrationTest()
  {
      // ... existing test code ...
  }
  ```

- [ ] **Option C**: Restructure `Program.Main()` to accept an `HttpClient` factory delegate (more invasive, not recommended)

### Acceptance Criteria

- [ ] `ProgramTests` no longer makes real network calls during normal `dotnet test` runs
- [ ] Test suite passes in offline environments
- [ ] If Option A: Help flag test verifies startup without side effects
- [ ] If Option B: Integration test can be run explicitly with `dotnet test --filter "Category=Integration"`

### Testing Requirements

**New Test** (if Option A):
```csharp
[Fact]
public void ApplicationStartsSuccessfully_HelpFlag()
{
    // Arrange & Act
    var exitCode = Program.Main(new[] { "--help" });

    // Assert
    Assert.Equal(0, exitCode);
}

[Fact]
public void ApplicationDisplaysUsage_HelpFlag()
{
    // Capture console output
    var output = new StringWriter();
    Console.SetOut(output);

    // Act
    Program.Main(new[] { "--help" });

    // Assert
    Assert.Contains("USAGE:", output.ToString());
    Assert.Contains("WallpaperApp.exe", output.ToString());
}
```

### Definition of Done

- [x] Test suite runs without network access
- [x] All tests pass in isolated environment
- [x] If integration test retained: clearly marked and documented
- [x] CI pipeline can run tests successfully

### Dependencies

- Depends on Story R3 if using Option A (help flag)
- Can be done independently if using Option B (skip attribute)

---

## Story R6: Improve Filename Uniqueness

**Epic**: Phase 1 Refactoring
**Story Points**: 1
**Type**: Bug Fix / Improvement
**Priority**: MEDIUM (low risk in production)

### Context

From [F-05] in architecture review: `ImageFetcher` generates filenames with second-level precision (`yyyyMMdd-HHmmss`). Two downloads within the same second produce the same filename, causing silent overwrites. The test suite works around this with a 1.1-second delay.

### Description

Add millisecond precision to the timestamp format to prevent filename collisions during rapid downloads. This is unlikely to occur in production (15-minute refresh interval) but can happen during testing or if the interval is reduced.

### Tasks

- [ ] Update `ImageFetcher.cs:89` to include milliseconds:
  ```csharp
  string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
  return $"wallpaper-{timestamp}.png";
  ```
- [ ] Update the regex pattern in `ImageFetcherTests.cs:238-239`:
  ```csharp
  Assert.Matches(@"^wallpaper-\d{8}-\d{6}-\d{3}\.png$", filename1);
  Assert.Matches(@"^wallpaper-\d{8}-\d{6}-\d{3}\.png$", filename2);
  ```
- [ ] Remove or reduce the `Task.Delay(1100)` in `ImageFetcherTests.cs:226`:
  ```csharp
  // Can reduce to 10ms or remove entirely - millisecond precision is enough
  await Task.Delay(10);
  ```
- [ ] Verify uniqueness test still passes

### Acceptance Criteria

- [ ] Filename format is `wallpaper-yyyyMMdd-HHmmss-fff.png`
- [ ] Test suite validates the new format
- [ ] Uniqueness test no longer requires 1+ second delay
- [ ] Existing downloaded files (old format) are unaffected

### Testing Requirements

**Existing test to verify**:
```csharp
[Fact]
public async Task DownloadImageAsync_GeneratesUniqueFilename()
{
    // ... arrange ...

    var result1 = await fetcher.DownloadImageAsync("https://example.com/test1.png");
    await Task.Delay(10); // Reduced from 1100ms
    var result2 = await fetcher.DownloadImageAsync("https://example.com/test2.png");

    Assert.NotEqual(result1, result2);
    Assert.Matches(@"^wallpaper-\d{8}-\d{6}-\d{3}\.png$", Path.GetFileName(result1));
    Assert.Matches(@"^wallpaper-\d{8}-\d{6}-\d{3}\.png$", Path.GetFileName(result2));
}
```

### Definition of Done

- [x] Millisecond precision added to filename
- [x] Test regex patterns updated
- [x] Test delay reduced or removed
- [x] All tests pass faster (1+ second saved per test run)
- [x] No change to download behavior (only filename format)

### Notes

- The old filename format was `wallpaper-20260215-143022.png`
- The new filename format is `wallpaper-20260215-143022-847.png`
- This does not affect existing temp files (cleanup strategy is unchanged)

---

## Story R7: Extract Shared Test Fixture

**Epic**: Phase 1 Refactoring
**Story Points**: 3
**Type**: Test Infrastructure Improvement
**Priority**: MEDIUM (reduces duplication)

### Context

From [F-06] in architecture review: Four test classes (`ProgramTests`, `ConfigurationServiceTests`, `WallpaperServiceTests`, `WallpaperUpdaterTests`) each have an identical ~40-line `Dispose()` method that saves/restores the current directory and handles cleanup failures. This is copy-pasted code.

### Description

Extract a reusable test fixture base class that handles temp directory creation, `Directory.SetCurrentDirectory()`, and cleanup. Each test class can inherit from or compose this fixture, reducing duplication and ensuring cleanup logic stays consistent.

### Tasks

- [ ] Create `WallpaperApp.Tests/Infrastructure/TestDirectoryFixture.cs`:
  ```csharp
  public class TestDirectoryFixture : IDisposable
  {
      private readonly string _originalDirectory;
      private readonly string _testDirectory;

      public TestDirectoryFixture(string testSuiteName)
      {
          _originalDirectory = Directory.GetCurrentDirectory();
          _testDirectory = Path.Combine(
              Path.GetTempPath(),
              "WallpaperAppTests",
              testSuiteName,
              Guid.NewGuid().ToString());

          Directory.CreateDirectory(_testDirectory);
          Directory.SetCurrentDirectory(_testDirectory);
      }

      public string TestDirectory => _testDirectory;
      public string OriginalDirectory => _originalDirectory;

      public void Dispose()
      {
          // Restore directory with fallback logic
          // Delete test directory with error handling
          // ... (existing cleanup logic) ...
      }
  }
  ```

- [ ] Refactor each test class to use the fixture:
  ```csharp
  public class ConfigurationServiceTests : IDisposable
  {
      private readonly TestDirectoryFixture _fixture;

      public ConfigurationServiceTests()
      {
          _fixture = new TestDirectoryFixture("ConfigurationServiceTests");
      }

      public void Dispose()
      {
          _fixture.Dispose();
      }

      // Tests can access _fixture.TestDirectory if needed
  }
  ```

- [ ] Update all 4 test classes: `ProgramTests`, `ConfigurationServiceTests`, `WallpaperServiceTests`, `WallpaperUpdaterTests`
- [ ] Delete ~120 lines of duplicated cleanup code (40 lines × 3 test classes)
- [ ] Verify all tests still pass

### Acceptance Criteria

- [ ] `TestDirectoryFixture` class exists in `Infrastructure/` folder
- [ ] Four test classes use the fixture (composition pattern)
- [ ] Total line count reduced by ~100+ lines
- [ ] Cleanup behavior is identical to before (no regressions)
- [ ] All 28 tests pass
- [ ] Tests still run in the `"CurrentDirectory Tests"` collection

### Testing Requirements

**No new tests required** - this is pure refactoring. Verify existing tests:
- [ ] `ConfigurationServiceTests` - 5 tests pass
- [ ] `WallpaperServiceTests` - 7 tests pass
- [ ] `WallpaperUpdaterTests` - 7 tests pass (will be fewer after Story R1 removes file I/O)
- [ ] `ProgramTests` - 1 test passes (or modified per Story R5)

### Definition of Done

- [x] Test fixture extracted and documented
- [x] All test classes refactored to use fixture
- [x] Cleanup logic centralized in one place
- [x] All tests pass
- [x] Code review: confirm fixture is reusable for future test classes
- [x] Test runtime unchanged (no performance regression)

### Notes

- The fixture can be enhanced in the future to support other test setup needs
- Consider adding a `CreateTestFile(string filename, byte[] content)` helper to the fixture
- This pattern makes adding new test classes easier (less boilerplate)

---

## Story R8: Add Missing Program.cs Branch Tests

**Epic**: Phase 1 Refactoring
**Story Points**: 2
**Type**: Test Coverage
**Priority**: MEDIUM (improves confidence)

### Context

From architecture review (Section 5.3): `Program.cs` has three execution branches (`--download`, `<path>`, and default), but only the default branch has an automated test. The `--download` and `<path>` branches are manually tested but not covered by the test suite.

### Description

Add automated tests for the `--download` and `<path>` execution branches in `Program.cs`. This improves test coverage and catches regressions when refactoring `Program.cs` (Story R3).

### Tasks

- [ ] Add test for `--download` flag:
  ```csharp
  [Fact]
  public async Task DownloadMode_ValidConfig_DownloadsImage()
  {
      // Arrange
      var configContent = @"{
        ""AppSettings"": {
          ""ImageUrl"": ""https://weather.zamflam.com/latest.png"",
          ""RefreshIntervalMinutes"": 15
        }
      }";
      File.WriteAllText("WallpaperApp.json", configContent);

      // Act
      var exitCode = Program.Main(new[] { "--download" });

      // Assert
      Assert.Equal(0, exitCode); // May need to mock HTTP for this
  }
  ```

- [ ] Add test for `<path>` flag:
  ```csharp
  [Fact]
  public void SetWallpaperMode_ValidImage_SetsWallpaper()
  {
      // Arrange
      string testImagePath = CreateTestBmpFile("test.bmp");

      // Act
      var exitCode = Program.Main(new[] { testImagePath });

      // Assert
      Assert.Equal(0, exitCode);
  }

  [Fact]
  public void SetWallpaperMode_InvalidImage_ReturnsErrorCode()
  {
      // Arrange
      string invalidPath = "nonexistent.png";

      // Act
      var exitCode = Program.Main(new[] { invalidPath });

      // Assert
      Assert.Equal(1, exitCode);
  }
  ```

- [ ] Add helper method to create test BMP files (copy from `WallpaperServiceTests`)

### Acceptance Criteria

- [ ] Test for `--download` mode exists and passes
- [ ] Test for `<path>` mode with valid image exists and passes
- [ ] Test for `<path>` mode with invalid image exists and passes
- [ ] All 3 execution branches in `Program.Main()` are now covered by tests
- [ ] Tests do not make real network calls (mock or use test HTTP server)

### Testing Requirements

**New Tests** (3 minimum):
1. `DownloadMode_ValidConfig_DownloadsImage()`
2. `SetWallpaperMode_ValidImage_SetsWallpaper()`
3. `SetWallpaperMode_InvalidImage_ReturnsErrorCode()`

**Optional Tests**:
4. `DownloadMode_InvalidConfig_ReturnsErrorCode()`
5. `DownloadMode_NetworkFailure_ReturnsErrorCode()`

### Definition of Done

- [x] Three new tests added to `ProgramTests`
- [x] All tests pass
- [x] Coverage gap closed (all Program.cs branches tested)
- [x] Tests are fast (< 1 second total)
- [x] Tests use mocks or test fixtures (no real network calls)

### Dependencies

- Should be completed after Story R3 (error handling restructure)
- Can reuse fixture from Story R7 if completed

---

## Implementation Order

**Recommended sequence**:

1. **Story R1** (IConfigurationService) - Foundational, unblocks DI
2. **Story R2** (HttpClient timeout) - Small, quick win
3. **Story R3** (Error handling) - Medium complexity, enables R4 and R8
4. **Story R4** (Unreachable code) - Absorbed into R3
5. **Story R6** (Filename uniqueness) - Independent, low risk
6. **Story R5** (Test network dependency) - Depends on R3 if using Option A
7. **Story R7** (Test fixture) - Independent, reduces maintenance burden
8. **Story R8** (Branch tests) - Depends on R3, final polish

**Estimated total effort**: 16 story points (~2-3 days for a single developer)

---

## Success Metrics

After completing all refactoring stories:

- [ ] Zero architectural violations (all services have interfaces where needed)
- [ ] Zero duplicated code (error handling and test fixtures extracted)
- [ ] Test suite runs offline (no network dependencies)
- [ ] Test suite runs faster (millisecond precision, reduced delays)
- [ ] Code coverage improved (all Program.cs branches tested)
- [ ] Ready for Story 6/7 (DI container and Windows Service)

---

**END OF REFACTORING STORIES**
