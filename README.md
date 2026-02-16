# Wallpaper Sync

A Windows desktop application that automatically updates your desktop wallpaper from a remote image URL.

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

### Individual Scripts

Run specific build steps:

| Script | Purpose | Windows | Linux/macOS/WSL |
|--------|---------|---------|-----------------|
| Test | Run all tests (fail-fast) | `scripts\test.bat` | `./scripts/test.sh` |
| Build | Build application | `scripts\build.bat` | `./scripts/build.sh` |
| Publish | Create self-contained .exe | `scripts\publish.bat` | `./scripts/publish.sh` |

All scripts use **minimal verbosity** for clean, readable output that's easy to copy/paste.

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

The application supports multiple modes:

### System Tray App Mode (Recommended)

Run the app as a system tray application for automatic wallpaper updates:

#### Installation

1. Build and publish the tray app:
   ```powershell
   .\scripts\publish-tray-app.ps1
   ```

2. Install and configure:
   ```powershell
   .\scripts\install-tray-app.ps1
   ```

This will:
- Copy the app to `%LOCALAPPDATA%\WallpaperSync`
- Add a shortcut to your Startup folder for auto-start
- Let you configure the image URL and refresh interval
- Offer to start the app immediately

#### Using the Tray App

Once running, the app sits quietly in your system tray:

- **Right-click the tray icon** for options:
  - Refresh Now - Immediately update wallpaper
  - Status - View current status and next refresh time
  - Open Image Folder - Browse downloaded images
  - About - App information
  - Exit - Close the app

- **Double-click the tray icon** to view status

#### Benefits Over Windows Service

The tray app offers several advantages:
- No password required (runs as regular app, not a service)
- Works with Windows Hello and fingerprint login
- Easy control via system tray interface
- Shows notifications when wallpaper updates

#### Uninstallation

To remove the tray app:
```powershell
.\scripts\uninstall-tray-app.ps1
```

For detailed instructions, see [TRAY-APP-README.md](TRAY-APP-README.md).

### Console Mode (Development/Debugging)

**For production use, see System Tray App Mode above.**

The console mode is no longer the primary way to run the app, but the CLI commands remain available for debugging:

Run single commands:
```bash
cd src/WallpaperApp
dotnet run -- --help       # Show help
dotnet run -- --download   # Download image once
dotnet run -- <image.png>  # Set wallpaper to local file
```

Note: The continuous background mode has been moved to the Tray App. For automated wallpaper updates, use the Tray App installation above.

### Download Image from URL (Story 4)

Download the image specified in `WallpaperApp.json`:

```bash
cd src/WallpaperApp
dotnet run -- --download
```

This will:
- Read the `ImageUrl` from your configuration
- Download the image to `%TEMP%/WallpaperSync/`
- Display the path where the image was saved
- Return an error if the download fails

### Set Wallpaper from Local File (Story 3)

Set your desktop wallpaper to a local image:

```bash
cd src/WallpaperApp
dotnet run -- C:\path\to\your\image.png
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

The application is configured using `WallpaperApp.json` located in the same directory as the executable.

### Configuration File Location

- **Development**: `src/WallpaperApp/WallpaperApp.json`
- **Published**: `publish/WallpaperApp.json`
- **Installed Service**: `C:\Program Files\WallpaperSync\WallpaperApp.json` (future)

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

1. Open `WallpaperApp.json` in a text editor (Notepad, VS Code, etc.)
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

- **Missing file**: If `WallpaperApp.json` is not found, the app will exit with an error message:
  ```
  Configuration Error: WallpaperApp.json not found. Create it with ImageUrl setting.
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

## Features Implemented by Story

### Story 1: Foundation - Console App + First Test
- Basic console application structure
- .NET 8 self-contained deployment
- xUnit testing framework

### Story 2: Configuration - Read URL from appsettings.json
- Configuration service that reads from `WallpaperApp.json`
- HTTPS URL validation
- Clear error messages for configuration issues

### Story 3: Wallpaper Service - Set Static Image as Wallpaper
- `WallpaperService` that sets desktop wallpaper using Windows API
- Support for PNG, JPG, and BMP formats
- File validation and clear error messages

### Story 4: HTTP Client - Fetch Image from URL
- `ImageFetcher` service that downloads images from URLs
- Automatic save to temporary directory (`%TEMP%/WallpaperSync/`)
- 30-second timeout for HTTP requests
- Graceful error handling (returns null on failure, no retries)
- Unique timestamp-based filenames (`wallpaper-{yyyyMMdd-HHmmss}.png`)
- Comprehensive logging of download operations

### Story 5: Integration - Fetch and Set Wallpaper
- `WallpaperUpdater` orchestrator that integrates all components
- Complete end-to-end workflow: Configuration → Download → Set Wallpaper
- Graceful error handling at each step with clear console output
- Progress messages showing each step of the process
- Returns success/failure status for automation

### Story 6: Periodic Refresh - Timer Implementation
- `TimerService` for scheduling automatic wallpaper updates
- Runs continuously with configurable refresh interval
- First update executes immediately on startup
- Subsequent updates run every 15 minutes (configurable via `RefreshIntervalMinutes`)
- Graceful shutdown on Ctrl+C using CancellationToken
- Console displays next refresh time after each update
- Robust error handling - timer continues running even if individual updates fail
- Default mode when running the application without arguments
- Thread-safe timer implementation with proper resource disposal

### Story 7: Windows Service - Convert to Service
- `Worker` background service implementing `BackgroundService`
- Uses `HostBuilder` pattern with dependency injection
- Runs as Windows Service or console application (dual-mode)
- Automatic startup on system boot
- Service management via installation scripts
- Runs as LocalSystem account
- Proper service lifecycle management with graceful shutdown
- Service registration in Windows Service Control Manager

#### ImageFetcher Implementation Details

The `ImageFetcher` service provides the following capabilities:

- **Download Location**: All images are saved to `%TEMP%/WallpaperSync/`
  - Example: `C:\Users\YourName\AppData\Local\Temp\WallpaperSync\`
  - Directory is created automatically if it doesn't exist

- **Filename Format**: `wallpaper-{yyyyMMdd-HHmmss}.png`
  - Example: `wallpaper-20240214-153045.png`
  - Each download gets a unique timestamp to avoid conflicts

- **Timeout**: 30 seconds
  - If the server doesn't respond within 30 seconds, the download is cancelled
  - Returns `null` to indicate failure

- **Error Handling**: No retries, fail gracefully
  - HTTP errors (404, 500, etc.) return `null`
  - Network errors return `null`
  - Timeout errors return `null`
  - All errors are logged to the console

- **Cleanup**: Old temporary files are NOT automatically deleted
  - Simplicity over optimization (as per Story Map philosophy)
  - Windows will clean up the temp directory as needed
