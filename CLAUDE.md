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

**ALWAYS** run the following before any git push:

```powershell
# On Windows
.\scripts\build.bat
.\scripts\test.bat

# On Linux/Mac (if dotnet is available)
dotnet build -c Release
dotnet test
```

## Testing Requirements

**Before AND after making changes:**
1. Run tests to verify starting state is clean
2. Make your code changes
3. Run tests again to verify nothing broke

**Test quality standards:**
- All tests must pass in Release configuration
- No xUnit analyzer warnings (treat warnings as errors)
- Verify thread safety tests complete without deadlocks
- Check for common issues:
  - xUnit1031: Use async/await instead of blocking task operations
  - Proper disposal of resources in tests
  - No hardcoded paths that break on different platforms

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
