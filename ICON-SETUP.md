# 🎨 Setting Up Your Custom Icon

Your purple wallpaper sync icon is ready to be integrated! Follow these simple steps.

## Quick Setup (3 Steps)

### 1. Save Your Icon

Save your icon image as **`icon-source.png`** in the repository root:

```
windows-image-viewer/
├── icon-source.png  ← Save your icon here
├── scripts/
├── src/
└── ...
```

The icon should be square (e.g., 512x512 or 1024x1024) for best results.

### 2. Convert to Windows Icon Format

Run the conversion script:

```powershell
.\scripts\create-icon.ps1
```

This creates a multi-resolution `.ico` file with sizes: 16x16, 32x32, 48x48, 256x256.

**Output:** `src\WallpaperApp.TrayApp\Resources\app.ico`

### 3. Build and Install

Build the app with your new icon:

```powershell
# Build with the new icon
.\scripts\build.bat

# Install to startup
.\scripts\install-tray-app.ps1
```

That's it! Your icon will now appear everywhere.

## Where Your Icon Appears

Once built and installed:

| Location | Description |
|----------|-------------|
| **System Tray** | Icon in notification area (color when enabled, gray when disabled) |
| **Executable** | Icon shown for `WallpaperApp.TrayApp.exe` in Windows Explorer |
| **Settings Window** | Icon in window title bar |
| **Welcome Wizard** | Icon in setup window title bar |
| **Taskbar** | When windows are visible or pinned |

## Advanced Options

### Custom Input/Output Paths

```powershell
.\scripts\create-icon.ps1 -InputPng "path\to\your\icon.png" -OutputIco "custom\output.ico"
```

### Manual Icon Creation

If you prefer to create the `.ico` manually:

1. Use a tool like IcoFX, GIMP, or an online converter
2. Create multiple sizes: 16x16, 32x32, 48x48, 256x256
3. Save as `src\WallpaperApp.TrayApp\Resources\app.ico`
4. Run `.\scripts\build.bat`

## Icon Design Tips

For best results:

- ✅ **Square image** (equal width and height)
- ✅ **High resolution** (at least 256x256, ideally 512x512+)
- ✅ **Transparent background** (PNG with alpha channel)
- ✅ **Simple, recognizable design** (works well at small sizes)
- ✅ **Good contrast** (visible on light and dark backgrounds)

## Troubleshooting

**Icon not showing after rebuild?**
- Windows caches icons. Try:
  1. Uninstall: `.\scripts\uninstall-tray-app.ps1`
  2. Delete: `%LOCALAPPDATA%\WallpaperSync`
  3. Rebuild: `.\scripts\build.bat`
  4. Reinstall: `.\scripts\install-tray-app.ps1`
  5. Restart Windows Explorer (or reboot)

**Conversion script fails?**
- Ensure your PNG is a valid image file
- Try re-saving it with an image editor
- Check that you have .NET 8.0 SDK installed

**Fallback icon showing?**
- The app uses a blue "W" as a fallback if `app.ico` is missing
- Verify the file exists: `src\WallpaperApp.TrayApp\Resources\app.ico`
- Check build output for errors about the icon resource

## What's Changed

The following files have been updated to support your icon:

- ✅ `WallpaperApp.TrayApp.csproj` - References icon as ApplicationIcon and EmbeddedResource
- ✅ `MainWindow.xaml.cs` - Loads icon from embedded resources for tray
- ✅ `SettingsWindow.xaml` - Shows icon in window title bar
- ✅ `WelcomeWizard.xaml` - Shows icon in wizard window
- ✅ `scripts/create-icon.ps1` - Converts PNG to multi-resolution ICO
- ✅ `.gitignore` - Ignores `icon-source.png` (but commits `app.ico`)

Your icon infrastructure is ready! Just add your `icon-source.png` and run the script. 🎉
