# Weather Wallpaper Service Information

## Service Details

- **Service Name**: `WeatherWallpaperService`
- **Display Name**: Weather Wallpaper Service
- **Description**: Automatically updates desktop wallpaper with weather forecasts every 15 minutes
- **Startup Type**: Automatic

## Installation

1. **Build the project** (if not already done):
   ```powershell
   dotnet publish src\WallpaperApp\WallpaperApp.csproj -c Release
   ```

2. **Run the install script as Administrator**:
   - Right-click `install-service.bat` → "Run as administrator"
   - Or from an admin PowerShell: `.\install-service.bat`

The script will automatically find the executable in the build output directory.

## Managing the Service

### View Service Status
```powershell
sc query WeatherWallpaperService
```

### Start the Service
```powershell
sc start WeatherWallpaperService
```

### Stop the Service
```powershell
sc stop WeatherWallpaperService
```

### Check Service Configuration
```powershell
sc qc WeatherWallpaperService
```

### View Service in GUI
- Press `Win + R`, type `services.msc`, press Enter
- Find "Weather Wallpaper Service" in the list

## Uninstallation

Run the uninstall script as Administrator:
- Right-click `uninstall-service.bat` → "Run as administrator"

## Troubleshooting

### Service won't start
1. Check that the executable path is correct:
   ```powershell
   sc qc WeatherWallpaperService
   ```

2. Verify the executable exists at that path

3. Check Windows Event Viewer for error details:
   - Windows Logs → Application
   - Look for errors from "WeatherWallpaperService"

### Manual service deletion (if needed)
```powershell
sc delete WeatherWallpaperService
```

## Configuration

The service reads configuration from `appsettings.json` in the same directory as the executable.

Default settings:
- Update interval: 15 minutes
- Image URL: Configured weather forecast image
- Wallpaper style: Centered
