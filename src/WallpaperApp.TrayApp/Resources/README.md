# Application Icon

This directory contains the application icon for Wallpaper Sync.

## Quick Setup

1. **Save your icon image** as `icon-source.png` in the repository root

2. **Run the conversion script:**
   ```powershell
   .\scripts\create-icon.ps1
   ```

   This will create `app.ico` in this directory with multiple resolutions (16x16, 32x32, 48x48, 256x256).

3. **Rebuild the application:**
   ```powershell
   .\scripts\build.bat
   ```

4. **Install with the new icon:**
   ```powershell
   .\scripts\install-tray-app.ps1
   ```

## Where the Icon Appears

Once built with `app.ico`:

- ✅ **System Tray** - Icon in the notification area
- ✅ **Executable File** - Icon shown in Windows Explorer for the .exe
- ✅ **Settings Window** - Icon in the title bar
- ✅ **Welcome Wizard** - Icon in the title bar
- ✅ **Taskbar** - When windows are shown (pinned/running)

## Icon States

The tray icon has two states:
- **Full Color** - When wallpaper sync is enabled
- **Grayscale (50% opacity)** - When wallpaper sync is disabled

## Manual Icon Creation

If you prefer to create the icon manually:

1. Create a `.ico` file with multiple sizes: 16x16, 32x32, 48x48, 256x256
2. Save it as `app.ico` in this directory
3. Rebuild with `.\scripts\build.bat`

**Recommended tools:**
- IcoFX (Windows)
- GIMP (with ICO plugin)
- Online converters (search "PNG to ICO")

## Current Status

⚠️ **Icon file `app.ico` needs to be created**

The application will use a fallback icon (blue "W" on colored background) until you create `app.ico`.
