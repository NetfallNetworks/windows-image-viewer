# Development Guidelines for Claude

## Detecting the Current Platform

**Always detect the OS before running any build, test, or publish command.**

```bash
# Works in Git Bash (Windows) and Linux/Mac terminals
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" || "$OSTYPE" == "cygwin" ]]; then
    IS_WINDOWS=true
else
    IS_WINDOWS=false
fi
```

Key differences:
- **Windows** (Git Bash / `OSTYPE=msys`): `dotnet` is in system PATH, full solution builds (WPF TrayApp + installer)
- **Linux/Mac**: may need `export PATH="$PATH:/root/.dotnet"`, WPF TrayApp and installer steps are skipped
- **MSI installer**: Windows-only — `dotnet tool restore && dotnet tool run wix build ...`
- **Flag syntax**: use `-p:Prop=Value` in Git Bash, not `/p:Prop=Value` (the slash gets path-mangled)

---

## Setup & Installation

### Installing .NET SDK (Linux/Mac)

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
export PATH="$PATH:/root/.dotnet"
dotnet --version
```

### Installing .NET SDK (Windows)

```powershell
winget install Microsoft.DotNet.SDK.8
```

Or download from: https://dotnet.microsoft.com/download

### First-Time Setup

Before making any changes, run the build pipeline to establish a clean baseline:

```bash
# Linux/Mac
./scripts/build.sh

# Windows (Git Bash) — preferred wrapper
cmd.exe /c scripts\\build.bat

# Windows (PowerShell)
.\scripts\build.bat
```

**If the pipeline fails on initial setup, DO NOT proceed.**

---

## ⛔ CRITICAL: MANDATORY TESTING POLICY ⛔

### ABSOLUTE REQUIREMENTS - NO EXCEPTIONS

**YOU MUST RUN TESTS AFTER EVERY SINGLE CODE CHANGE. PERIOD.**

### THE RULE

```
IF you modify ANY .cs, .xaml, or .wxs file
THEN detect the OS and run the correct pipeline
AND all tests must pass with zero warnings
BEFORE you commit or push ANYTHING
```

### ENFORCEMENT

1. **Before making changes:** Run tests to establish baseline
2. **After making changes:** Run tests to verify nothing broke
3. **Before committing:** Confirm tests still pass
4. **If tests fail:** FIX THE CODE, don't commit broken code
5. **Never ever:** Commit without running tests

### THE COMMANDS

**Prefer the wrapper script — it runs the complete pipeline in the correct order.**

#### Windows (Git Bash)
```bash
cmd.exe /c scripts\\build.bat
```

#### Windows (PowerShell / cmd)
```bat
.\scripts\build.bat
```

#### Linux/Mac
```bash
./scripts/build.sh
```

### TARGETED COMMANDS (when wrapper isn't practical)

You may run individual steps instead of the full wrapper, **but only if the commands
exactly match what the wrapper script does.** Always run at minimum: build + tests.

#### Windows targeted commands (Git Bash)
```bash
# Step 1 — build (full solution including WPF TrayApp)
dotnet build WallpaperApp.sln -c Release --warnaserror --verbosity minimal --nologo

# Step 2 — test (mandatory — never skip)
dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity minimal --nologo

# Step 3 — publish TrayApp (note: -p: not /p: in Git Bash)
dotnet publish src/WallpaperApp.TrayApp/WallpaperApp.TrayApp.csproj \
  -c Release -o bin/TrayApp --self-contained true --runtime win-x64 \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  --verbosity minimal --nologo

# Step 4 — build MSI installer (Windows only)
dotnet tool restore
dotnet tool run wix extension add WixToolset.UI.wixext/4.0.5
dotnet tool run wix build installer/Package.wxs \
  -ext WixToolset.UI.wixext -o installer/WallpaperSync-Setup.msi -arch x64
```

#### Linux/Mac targeted commands
```bash
export PATH="$PATH:/root/.dotnet"

# Step 1 — build (WPF TrayApp excluded on Linux)
dotnet build src/WallpaperApp/WallpaperApp.csproj -c Release --warnaserror --verbosity minimal --nologo
dotnet build src/WallpaperApp.Tests/WallpaperApp.Tests.csproj -c Release --warnaserror --verbosity minimal --nologo

# Step 2 — test (mandatory — never skip)
dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity minimal --nologo

# Step 3 — publish console app only
dotnet publish src/WallpaperApp/WallpaperApp.csproj -c Release -o publish/WallpaperApp --verbosity minimal --nologo

# Steps 3b & 4 skipped on Linux (WPF TrayApp and MSI are Windows-only)
```

### WHAT SUCCESS LOOKS LIKE

**Windows (`build.bat`):**
```
========================================
[SUCCESS] BUILD PIPELINE COMPLETE!
========================================
  [OK] Build successful
  [OK] All tests passed (94/94)
  [OK] Console app published to .\publish\WallpaperApp\
  [OK] Tray app published to .\bin\TrayApp\
  [OK] Installer built: .\installer\WallpaperSync-Setup.msi
========================================
```

**Linux/Mac (`build.sh`):**
```
========================================
✅ BUILD PIPELINE COMPLETE!
========================================
  ✅ Build successful
  ✅ All tests passed (94/94)
  ✅ Console app published to ./publish/WallpaperApp/
========================================
```

**If you see ANY failed tests or build errors, you MUST fix them before proceeding.**

### WHAT FAILURE LOOKS LIKE

```
Failed!  - Failed:     1, Passed:    93, Skipped:     0, Total:    94
```

**This means you broke something. DO NOT COMMIT. FIX IT FIRST.**

### NO EXCUSES

- ❌ "The tests probably pass" — RUN THEM
- ❌ "It's a small change" — RUN THEM
- ❌ "Just XAML/WiX changes" — RUN THEM
- ❌ "I'll test later" — NO, TEST NOW
- ❌ "We're on Linux so I can't run the full pipeline" — RUN WHAT YOU CAN (steps 1–2 always work)

### YOUR RESPONSIBILITY

You are PERSONALLY RESPONSIBLE for:
1. ✅ Detecting the OS before running any command
2. ✅ Running the correct script/commands for the current platform
3. ✅ Tests run after EVERY code change — all 94 must pass
4. ✅ Showing the test output to prove they passed
5. ✅ No compiler warnings or errors
6. ✅ Fixing any failures immediately before committing

### Test Quality Standards

- All 94 tests must pass — NO EXCEPTIONS
- Zero test failures, zero skipped
- No compiler warnings (warnings-as-errors is enforced)
- No xUnit analyzer warnings
- Check for common issues:
  - xUnit1031: Use async/await instead of blocking task operations
  - Proper disposal of resources in tests
  - No hardcoded paths that break on different platforms
  - Default values must match test expectations (e.g., FitMode defaults)

---

## Git Workflow

**MANDATORY SEQUENCE — DO NOT DEVIATE:**

1. Develop on designated feature branch (e.g., `claude/my-feature-aBcDe`)
2. **RUN BUILD PIPELINE** — platform-appropriate command (see above)
3. **VERIFY** — all tests pass, no warnings
4. **ONLY IF PIPELINE SUCCEEDS:** Commit with clear messages
5. **ONLY IF PIPELINE SUCCEEDS:** Push to remote

### Communicating with the User

When reporting your work you MUST:
1. ✅ State which platform you're on and which command you ran
2. ✅ Show the actual command output (build, tests, publish)
3. ✅ Confirm test count passed (e.g., "94/94 passing")
4. ❌ Do NOT say "should work" — prove it by running the pipeline
5. ❌ Do NOT commit without showing test results

---

## Code Quality

- Follow existing code style and patterns
- No blocking operations in async contexts
- Use proper async/await patterns
- Ensure thread safety for concurrent operations
- Add tests for new functionality
