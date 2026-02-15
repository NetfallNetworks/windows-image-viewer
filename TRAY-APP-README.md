# Weather Wallpaper Tray App 🌤️

A system tray application that automatically updates your Windows wallpaper with weather images!

## Features ✨

- **System Tray Icon** - Runs quietly in your system tray
- **Automatic Updates** - Changes wallpaper on your configured schedule
- **No Password Needed** - Runs as a regular app (not a Windows service!)
- **Auto-Start** - Starts automatically when you log in
- **Easy Control** - Right-click menu for instant access

## Installation 🚀

### Step 1: Build the App

```powershell
.\scripts\build-tray-app.ps1
```

### Step 2: Install and Configure

```powershell
.\scripts\install-tray-app.ps1
```

This will:
- Copy the app to `%LOCALAPPDATA%\WeatherWallpaper`
- Add a shortcut to your Startup folder
- Let you configure the image URL and refresh interval
- Offer to start the app immediately

### Step 3: Configure (Optional)

Edit the configuration file at:
```
%LOCALAPPDATA%\WeatherWallpaper\WallpaperApp.json
```

Example configuration:
```json
{
  "ImageUrl": "https://example.com/weather-image.jpg",
  "RefreshIntervalMinutes": 30
}
```

## Usage 💡

### System Tray Menu

Right-click the tray icon for these options:

- **🔄 Refresh Now** - Immediately download and set a new wallpaper
- **📊 Status** - View current status and next refresh time
- **📁 Open Image Folder** - Browse downloaded wallpaper images
- **ℹ️ About** - App information
- **❌ Exit** - Close the app

### Double-Click

Double-click the tray icon to quickly view status.

## Uninstallation 🗑️

To remove the app:

```powershell
.\scripts\uninstall-tray-app.ps1
```

This will:
- Stop the running app
- Remove the startup shortcut
- Delete installation files
- Optionally clean up downloaded images

## Logs 📝

Logs are saved to:
```
%TEMP%\WeatherWallpaperService\service.log
```

Check this file if you encounter any issues!

## Troubleshooting 🔧

### App won't start
- Check the log file for errors
- Make sure the configuration file exists and is valid JSON
- Verify the ImageUrl is accessible

### Wallpaper not changing
- Right-click tray icon -> "Refresh Now" to test
- Check logs for error messages
- Verify your ImageUrl is correct

### Multiple instances running
The app prevents multiple instances. If you see "Already Running", check your system tray or Task Manager.

## Why Tray App Instead of Service?

Windows services require a password to run, but the tray app:
- ✅ Runs in your user session (perfect for wallpapers!)
- ✅ No password needed (works with Windows Hello/fingerprint)
- ✅ Easy to control via system tray
- ✅ Shows notifications when wallpaper updates
- ✅ Can interact with your desktop

## Commands Summary

```powershell
# Build
.\scripts\build-tray-app.ps1

# Install (auto-start enabled)
.\scripts\install-tray-app.ps1

# Uninstall
.\scripts\uninstall-tray-app.ps1

# Manual run (for testing)
bin\TrayApp\WallpaperApp.TrayApp.exe
```

Enjoy your automatically updating weather wallpapers! 🎨
