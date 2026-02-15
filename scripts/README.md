# Build Scripts - Quick Reference

## Available Scripts

### Development Scripts
- `build.bat` / `build.sh` - Build ALL projects (console app, tray app, tests)
- `test.bat` / `test.sh` - Run unit tests
- `validate.bat` / `validate.sh` - Full validation pipeline

### Tray App Scripts (NEW!)
- `publish-tray-app.ps1` - Publish the system tray application (single-file .exe)
- `install-tray-app.ps1` - Install and configure auto-start
- `uninstall-tray-app.ps1` - Remove the tray app

For detailed tray app documentation, see [TRAY-APP-README.md](../TRAY-APP-README.md)

---

## What Was Fixed

### 1. Directory Navigation Bug ❌ → ✅

**Before (broken):**
```batch
cd /d "%~dp0\.."
cd src                    ← Missing /d flag, relative path fails
dotnet test
```

**After (fixed):**
```batch
set "REPO_ROOT=%~dp0.."
cd /d "%REPO_ROOT%\src"   ← Absolute path with /d flag
dotnet test
```

**Why it matters:** The original script would fail with "project not found" errors because `cd` without `/d` flag doesn't change drives, and relative paths can fail in different contexts.

---

### 2. Output Logging ✨ NEW

Development scripts save complete output to log files:

```batch
validate.bat  → validate-output.log
test.bat      → test-output.log
build.bat     → build-output.log
```

**Log file location:** Repository root (same folder as README.md)

**What's captured:**
- All console output (stdout)
- All error messages (stderr)
- Timestamps
- Working directories
- Exit codes

**Example log file content:**
```
========================================
Running automated tests...
Started at Fri 02/14/2026  5:30:15.23
========================================

Working directory: C:\projects\windows-image-viewer\src

  Determining projects to restore...
  All projects are up-to-date for restore.
  WallpaperApp -> C:\projects\windows-image-viewer\src\WallpaperApp\bin\Release\net8.0-windows\win-x64\WallpaperApp.dll
  WallpaperApp.Tests -> C:\projects\windows-image-viewer\src\WallpaperApp.Tests\bin\Release\net8.0-windows\WallpaperApp.Tests.dll
Test run for C:\projects\windows-image-viewer\src\WallpaperApp.Tests\bin\Release\net8.0-windows\WallpaperApp.Tests.dll (.NETCoreApp,Version=v8.0)
Microsoft (R) Test Execution Command Line Tool Version 17.8.0
...

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms
```

---

### 3. Fixed Formatting

**Before (broken):**
```
[32mCode updated[0m        ← ANSI codes don't work in cmd.exe
```

**After (fixed):**
```
SUCCESS: Code updated     ← Clear prefix, no ANSI codes
```

---

## Usage Examples

### Run validation pipeline with log output:
```cmd
scripts\validate.bat
```

**What happens:**
1. Screen shows progress in real-time
2. Full details saved to `validate-output.log`
3. If error occurs, script tells you to check the log file
4. Upload log file for debugging

### Check test results:
```cmd
scripts\test.bat
type test-output.log       ← View full test output
```

### Debug build failures:
```cmd
scripts\build.bat
notepad build-output.log   ← Open log in editor
```

---

## Log File Benefits

✅ **Easy debugging** - Full dotnet output captured
✅ **Shareable** - Upload logs to GitHub issues or share with team
✅ **Reproducible** - Compare logs across different runs
✅ **Complete** - Nothing is hidden or truncated

---

## Git Status

All log files are excluded from git via `.gitignore`:
```gitignore
## Script output logs
validate-output.log
test-output.log
build-output.log
```

This prevents accidental commits of build artifacts.

---

## Troubleshooting

**Q: Script still fails with "project not found"**
A: Make sure you're running the script from the repository root or use the full path:
```cmd
C:\projects\windows-image-viewer\scripts\validate.bat
```

**Q: Where are the log files?**
A: In the repository root (same folder as README.md), not in the scripts folder.

**Q: Can I run scripts from any directory?**
A: Yes! The scripts use `%~dp0` to calculate the repository root, so they work from anywhere.

**Q: Log file is too large to open**
A: Use `more` or `tail` to view parts:
```cmd
more < validate-output.log
```

---

## Next Steps

After running tests successfully, publish and install the tray app:

```powershell
# Publish the system tray app (creates single-file .exe)
.\scripts\publish-tray-app.ps1

# Install it (adds to Windows Startup)
.\scripts\install-tray-app.ps1
```

See [TRAY-APP-README.md](../TRAY-APP-README.md) for complete usage instructions.
