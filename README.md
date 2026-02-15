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

### Output Logging

All scripts automatically save detailed output to log files in the repository root:

- `validate-output.log` - Full validation pipeline output
- `test-output.log` - Test run details
- `build-output.log` - Build output and warnings
- `publish-output.log` - Publish process details

**Why this helps:**
- Easier debugging when builds fail
- Upload logs for team review
- Compare output across runs
- All stdout and stderr captured

Log files are excluded from git (see `.gitignore`).

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

## Configuration

The application is configured using `appsettings.json` located in the same directory as the executable.

### Configuration File Location

- **Development**: `src/WallpaperApp/appsettings.json`
- **Published**: `publish/appsettings.json`
- **Installed Service**: `C:\Program Files\WeatherWallpaper\appsettings.json` (future)

### Available Settings

```json
{
  "AppSettings": {
    "ImageUrl": "https://weather.zamflam.com/latest.png",
    "RefreshIntervalMinutes": 15
  }
}
```

#### AppSettings

- **ImageUrl** (string, required): URL of the image to use as wallpaper
  - Must use HTTPS protocol for security
  - Example: `"https://weather.zamflam.com/latest.png"`

- **RefreshIntervalMinutes** (integer, optional): Interval between wallpaper updates
  - Default: 15 minutes
  - Used in future stories for automatic refresh

### Changing the Image URL

1. Open `appsettings.json` in a text editor (Notepad, VS Code, etc.)
2. Update the `ImageUrl` value to your desired HTTPS URL
3. Save the file
4. Restart the application

Example:
```json
{
  "AppSettings": {
    "ImageUrl": "https://example.com/my-custom-image.png",
    "RefreshIntervalMinutes": 15
  }
}
```

### Configuration Validation

The application validates configuration on startup:

- **Missing file**: If `appsettings.json` is not found, the app will exit with an error message:
  ```
  Configuration Error: appsettings.json not found. Create it with ImageUrl setting.
  ```

- **Non-HTTPS URL**: If the URL does not start with `https://`, the app will exit with:
  ```
  Configuration Error: ImageUrl must use HTTPS protocol for security
  ```

- **Empty URL**: If the URL is empty or missing, the app will exit with:
  ```
  Configuration Error: ImageUrl cannot be empty
  ```

All validation errors are designed to guide you in fixing the configuration issue.
