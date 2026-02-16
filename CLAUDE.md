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

## Critical: Build & Test Before Pushing

**⚠️ MANDATORY: ALL TESTS MUST PASS BEFORE COMMITTING ⚠️**

**NEVER commit code without running and verifying tests pass. This is NON-NEGOTIABLE.**

```bash
# On Linux/Mac (add dotnet to PATH first)
export PATH="$PATH:/root/.dotnet"
dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj

# On Windows
.\scripts\build.bat
.\scripts\test.bat
```

**Expected result: "Passed! - Failed: 0, Passed: 88"**

If ANY test fails, you MUST fix it before proceeding.

## Testing Requirements

**CRITICAL WORKFLOW - Follow this EXACTLY:**

1. **BEFORE making any code changes:**
   ```bash
   export PATH="$PATH:/root/.dotnet"
   dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj
   ```
   Verify all 88 tests pass. If any fail, investigate FIRST.

2. **Make your code changes**

3. **AFTER making changes:**
   ```bash
   export PATH="$PATH:/root/.dotnet"
   dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj
   ```
   ALL tests MUST pass. If any fail:
   - Analyze the failure
   - Fix the code
   - Re-run tests
   - Repeat until ALL tests pass

4. **ONLY AFTER all tests pass: commit and push**

**DO NOT return to the user until tests pass. This is your responsibility.**

**Test quality standards:**
- All 88 tests must pass - NO EXCEPTIONS
- Zero test failures allowed
- No compiler warnings (treat warnings as errors)
- No xUnit analyzer warnings
- Platform-specific tests must handle both Windows and Linux correctly
- Check for common issues:
  - xUnit1031: Use async/await instead of blocking task operations
  - Proper disposal of resources in tests
  - No hardcoded paths that break on different platforms
  - Default values must match test expectations (e.g., FitMode defaults)

## Git Workflow

1. Develop on designated feature branch (e.g., `claude/wallpaper-sync-phase-1-9oLWN`)
2. Run build & test scripts
3. Commit with clear messages
4. Push only after all tests pass

## Code Quality

- Follow existing code style and patterns
- No blocking operations in async contexts
- Use proper async/await patterns
- Ensure thread safety for concurrent operations
- Add tests for new functionality
