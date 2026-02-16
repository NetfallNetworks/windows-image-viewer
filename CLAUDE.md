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
THEN you MUST run: ./scripts/build.sh && ./scripts/test.sh
AND tests MUST show: "✅ All tests passed!" with 88 tests passing
BEFORE you commit or push ANYTHING
```

### ENFORCEMENT

1. **Before making changes:** Run tests to establish baseline
2. **After making changes:** Run tests to verify nothing broke
3. **Before committing:** Run tests one final time
4. **If tests fail:** FIX THE CODE, don't commit broken code
5. **Never ever:** Commit without running tests

### THE COMMANDS

**On Linux/Mac:**
```bash
./scripts/build.sh && ./scripts/test.sh
```

**On Windows:**
```powershell
.\scripts\build.bat
.\scripts\test.bat
```

**Alternative (if scripts aren't executable):**
```bash
export PATH="$PATH:/root/.dotnet"
dotnet build WallpaperApp.sln -c Release --warnaserror
dotnet test --verbosity minimal --nologo
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
2. **BUILD** - `./scripts/build.sh` (Linux/Mac) or `.\scripts\build.bat` (Windows)
3. **RUN TESTS** - `./scripts/test.sh` (Linux/Mac) or `.\scripts\test.bat` (Windows)
4. **VERIFY** - Confirm "✅ All tests passed!" appears
5. **ONLY IF BUILD & TESTS PASS:** Commit with clear messages
6. **BEFORE PUSHING:** Build and test one more time to be absolutely sure
7. **ONLY IF BUILD & TESTS PASS:** Push to remote

### Communicating with the User

When reporting your work to the user, you MUST:
1. ✅ Show the actual build and test commands you ran
2. ✅ Show the build output confirming successful compilation
3. ✅ Show the test results (pass/fail counts)
4. ✅ Explicitly state "✅ All tests passed!" (88 tests)
5. ❌ Do NOT say "tests pass" without proving it with actual command output
6. ❌ Do NOT assume tests pass without running them
7. ❌ Do NOT say "should work" - prove it with build and tests

## Code Quality

- Follow existing code style and patterns
- No blocking operations in async contexts
- Use proper async/await patterns
- Ensure thread safety for concurrent operations
- Add tests for new functionality
