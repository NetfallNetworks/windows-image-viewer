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
   ```powershell
   # Right-click install-service.ps1 → "Run with PowerShell"
   # Or from an admin PowerShell:
   .\install-service.ps1
   ```

3. **Choose service account**:
   - **Option 1**: Run as YOUR user account (RECOMMENDED - can set wallpapers)
   - **Option 2**: Run as LocalSystem (cannot set wallpapers)

   You'll be prompted for your Windows password if you choose option 1.

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

**Using the uninstall script:**
```powershell
.\uninstall-service.ps1
```

**Manual removal:**
1. Open services.msc
2. Find "Weather Wallpaper Service"
3. Stop the service (if running)
4. Right-click → Delete

**Command line:**
```powershell
sc stop WeatherWallpaperService
sc delete WeatherWallpaperService
```

## Diagnostic Tools

### Quick Diagnosis
```powershell
.\diagnose-service.ps1
```
Shows service status, account, and recent log entries.

### Check Windows Event Logs
```powershell
.\check-service-logs.ps1
```
Searches Windows Event Logs for service-related errors.

### View Service Logs
The service logs to:
```
%TEMP%\WeatherWallpaperService\service.log
```

View logs:
```powershell
notepad $env:TEMP\WeatherWallpaperService\service.log
```

## Troubleshooting

### Service won't start
1. **Run diagnostics**:
   ```powershell
   .\diagnose-service.ps1
   ```

2. **Check if running as correct account**:
   - Service must run as YOUR user account to set wallpapers
   - Reinstall with `.\install-service.ps1` and choose option 1

3. **Check Windows Event Viewer**:
   ```powershell
   .\check-service-logs.ps1
   ```

4. **Verify executable path**:
   ```powershell
   sc qc WeatherWallpaperService
   ```

### Wallpaper not updating

**Most common cause**: Service running as LocalSystem instead of your user account.

**Solution**: Reinstall with the correct account:
```powershell
.\uninstall-service.ps1
.\install-service.ps1
# Choose option 1 - Run as YOUR user account
```

**Check logs** to see if updates are happening:
```powershell
notepad $env:TEMP\WeatherWallpaperService\service.log
```

## Configuration

The service reads configuration from `appsettings.json` in the same directory as the executable.

Default settings:
- Update interval: 15 minutes
- Image URL: Configured weather forecast image
- Wallpaper style: Centered
