# Weather Wallpaper App

A Windows desktop application that automatically updates your desktop wallpaper with weather forecast images.

## Requirements

- Windows 10 or Windows 11 (x64)
- .NET 8 SDK (for building from source)
- Git (for version control)

## Quick Start (Automated)

**Recommended workflow:** Use the automated scripts to ensure tests pass before deployment.

### Full Validation Pipeline

Runs: pull → test → build → publish (stops on any failure)

**Windows:**
```cmd
scripts\validate.bat
```

**Linux/macOS/WSL:**
```bash
./scripts/validate.sh
```

This script will:
1. Pull latest code from git
2. Run all automated tests (**stops if any test fails**)
3. Build the application (**stops if build fails**)
4. Publish self-contained executable
5. Prompt for manual testing

### Individual Scripts

Run specific build steps:

| Script | Purpose | Windows | Linux/macOS/WSL |
|--------|---------|---------|-----------------|
| Test | Run all tests (fail-fast) | `scripts\test.bat` | `./scripts/test.sh` |
| Build | Build application | `scripts\build.bat` | `./scripts/build.sh` |
| Publish | Create self-contained .exe | `scripts\publish.bat` | `./scripts/publish.sh` |

## Manual Build Instructions

If you prefer to run commands manually:

### Build

```bash
cd src/WallpaperApp
dotnet build
```

## Test

```bash
cd src
dotnet test
```

## Run

```bash
cd src/WallpaperApp
dotnet run
```

## Publish (Self-Contained)

Produces a standalone executable that runs without .NET installed:

```bash
cd src/WallpaperApp
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

Then run:

```bash
./publish/WallpaperApp.exe
```
