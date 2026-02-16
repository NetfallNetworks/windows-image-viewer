# Development Guidelines for Claude

## Setup & Installation

### Installing .NET SDK (Linux/Mac)

If you don't have dotnet installed, use the official installation script:

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

After installation, verify dotnet is available:

```bash
dotnet --version
```

### Installing .NET SDK (Windows)

Download and install from: https://dotnet.microsoft.com/download

Or use winget:

```powershell
winget install Microsoft.DotNet.SDK.8
```

### First-Time Setup

Before making any changes, ensure the project builds and all tests pass:

```bash
# Linux/Mac
dotnet build -c Release
dotnet test

# Windows
.\scripts\build.bat
.\scripts\test.bat
```

**If tests fail on initial setup, DO NOT proceed.** Investigate and resolve issues first to establish a clean baseline.

## ⛔ CRITICAL: MANDATORY TESTING POLICY ⛔

### ABSOLUTE REQUIREMENTS - NO EXCEPTIONS

**YOU MUST RUN TESTS AFTER EVERY SINGLE CODE CHANGE. PERIOD.**

This is not a suggestion. This is not optional. This is not "when convenient."

### THE RULE

```
IF you modify ANY .cs or .xaml file
THEN you MUST run: dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj
AND tests MUST show: "Passed! - Failed: 0, Passed: 88, Skipped: 0"
BEFORE you commit or push ANYTHING
```

### ENFORCEMENT

1. **Before making changes:** Run tests to establish baseline
2. **After making changes:** Run tests to verify nothing broke
3. **Before committing:** Run tests one final time
4. **If tests fail:** FIX THE CODE, don't commit broken code
5. **Never ever:** Commit without running tests

### THE COMMAND

```bash
export PATH="$PATH:/root/.dotnet"
dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj
```

### WHAT SUCCESS LOOKS LIKE

```
Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88
```

**If you see ANY failed tests, you MUST fix them before proceeding.**

### WHAT FAILURE LOOKS LIKE

```
Failed!  - Failed:     1, Passed:    87, Skipped:     0, Total:    88
```

**This means you broke something. DO NOT COMMIT. FIX IT FIRST.**

### NO EXCUSES

- ❌ "The tests probably pass" - RUN THEM
- ❌ "It's a small change" - RUN THEM
- ❌ "I'll test later" - NO, TEST NOW
- ❌ "Just XAML changes" - RUN THEM
- ❌ "Tests take too long" - THEY TAKE 2 MINUTES, RUN THEM
- ❌ "I'm confident it works" - PROVE IT, RUN THEM

### YOUR RESPONSIBILITY

You are PERSONALLY RESPONSIBLE for ensuring:
1. ✅ Tests are run after EVERY code change
2. ✅ ALL 88 tests pass before committing
3. ✅ You show the test results to prove they passed
4. ✅ No compiler warnings or errors
5. ✅ You fix any failures immediately

### Test Quality Standards

- All 88 tests must pass - NO EXCEPTIONS
- Zero test failures allowed
- Zero tests skipped
- No compiler warnings (treat warnings as errors)
- No xUnit analyzer warnings
- Platform-specific tests must handle both Windows and Linux correctly
- Check for common issues:
  - xUnit1031: Use async/await instead of blocking task operations
  - Proper disposal of resources in tests
  - No hardcoded paths that break on different platforms
  - Default values must match test expectations (e.g., FitMode defaults)

## Git Workflow

**MANDATORY SEQUENCE - DO NOT DEVIATE:**

1. Develop on designated feature branch (e.g., `claude/wallpaper-sync-phase-1-9oLWN`)
2. **RUN TESTS** - `dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj`
3. **VERIFY** - Confirm all 88 tests passed in the output
4. **ONLY IF TESTS PASS:** Commit with clear messages
5. **BEFORE PUSHING:** Run tests one more time to be absolutely sure
6. **ONLY IF TESTS PASS:** Push to remote

### Communicating with the User

When reporting your work to the user, you MUST:
1. ✅ Show the actual test command you ran
2. ✅ Show the test results (pass/fail counts)
3. ✅ Explicitly state "All 88 tests passed"
4. ❌ Do NOT say "tests pass" without proving it
5. ❌ Do NOT assume tests pass without running them
6. ❌ Do NOT say "should work" - prove it with tests

## Code Quality

- Follow existing code style and patterns
- No blocking operations in async contexts
- Use proper async/await patterns
- Ensure thread safety for concurrent operations
- Add tests for new functionality
