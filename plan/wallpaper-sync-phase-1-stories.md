# Wallpaper Sync - Phase 1: Security & Core Infrastructure

**Epic**: Security & Backend Services
**Priority**: CRITICAL
**Total Story Points**: 18
**Estimated Duration**: 3-4 days

---

## Phase Overview

Phase 1 focuses on fixing the critical security vulnerability and adding essential backend services. All changes are backward-compatible and testable. This phase establishes the foundation for the UX improvements in Phase 3.

**Key Objectives**:
1. Fix security vulnerability (extension-only validation → magic byte validation)
2. Add application state management (enabled/disabled, last-known-good, first-run)
3. Implement wallpaper fit modes (fill, fit, stretch, tile, center)
4. Add last-known-good fallback for reliability
5. Implement file cleanup to prevent temp directory bloat

---

## Story WS-1: Image Validation Service ⚠️ SECURITY FIX

**Story Points**: 5
**Type**: Security Fix / Feature
**Priority**: CRITICAL

### Context

**CRITICAL SECURITY ISSUE**: Current code only validates file extension (`.png`, `.jpg`, `.bmp`) but not actual file content. A malicious file named `virus.exe.png` would pass validation. The `WallpaperService` checks extensions at `WallpaperService.cs:39-45`, and `ImageFetcher` always generates filenames with `.png` extension, so downloaded files always pass the check.

### Description

Create `IImageValidator` service that validates images using magic byte signatures (file headers) instead of extensions. This prevents malicious files from being processed.

### Tasks

- [ ] Create `src/WallpaperApp/Services/IImageValidator.cs` interface
- [ ] Create `src/WallpaperApp/Services/ImageValidator.cs` implementation
- [ ] Implement magic byte validation for PNG, JPEG, BMP:
  - PNG: `89 50 4E 47 0D 0A 1A 0A` (first 8 bytes)
  - JPEG: `FF D8 FF` (first 3 bytes)
  - BMP: `42 4D` (first 2 bytes)
- [ ] Create `ImageFormat` enum (Unknown, PNG, JPEG, BMP)
- [ ] Add validation to `ImageFetcher.DownloadImageAsync()` AFTER file write
- [ ] Add validation to `WallpaperService.SetWallpaper()` BEFORE P/Invoke
- [ ] Register `IImageValidator` in DI container (`MainWindow.xaml.cs`)
- [ ] Write unit tests with crafted byte sequences
- [ ] Delete invalid files immediately if validation fails

### Files to Create

```
src/WallpaperApp/Services/IImageValidator.cs
src/WallpaperApp/Services/ImageValidator.cs
src/WallpaperApp.Tests/Services/ImageValidatorTests.cs
```

### Files to Modify

```
src/WallpaperApp/Services/ImageFetcher.cs (add validation call)
src/WallpaperApp/Services/WallpaperService.cs (replace extension check)
src/WallpaperApp.TrayApp/MainWindow.xaml.cs (register in DI)
```

### Implementation Details

**IImageValidator.cs**:
```csharp
namespace WallpaperApp.Services
{
    public enum ImageFormat
    {
        Unknown,
        PNG,
        JPEG,
        BMP
    }

    public interface IImageValidator
    {
        bool IsValidImage(string filePath, out ImageFormat format);
    }
}
```

**ImageValidator.cs**:
```csharp
public class ImageValidator : IImageValidator
{
    private static readonly byte[] PNG_HEADER =
        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JPEG_HEADER =
        { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] BMP_HEADER =
        { 0x42, 0x4D };

    public bool IsValidImage(string filePath, out ImageFormat format)
    {
        format = ImageFormat.Unknown;

        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = File.OpenRead(filePath);
            byte[] header = new byte[8];
            int bytesRead = stream.Read(header, 0, 8);

            if (bytesRead < 2)
                return false;

            // Check PNG (8 bytes)
            if (bytesRead >= 8 && header.Take(8).SequenceEqual(PNG_HEADER))
            {
                format = ImageFormat.PNG;
                return true;
            }

            // Check JPEG (3 bytes)
            if (bytesRead >= 3 && header.Take(3).SequenceEqual(JPEG_HEADER))
            {
                format = ImageFormat.JPEG;
                return true;
            }

            // Check BMP (2 bytes)
            if (header.Take(2).SequenceEqual(BMP_HEADER))
            {
                format = ImageFormat.BMP;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image validation error: {ex.Message}");
            return false;
        }
    }
}
```

**Update ImageFetcher.cs** (after line 67, after file write):
```csharp
// Validate the downloaded file
var validator = new ImageValidator();
if (!validator.IsValidImage(fullPath, out var format))
{
    FileLogger.Log($"Downloaded file failed validation: {url}");
    File.Delete(fullPath); // Delete invalid file
    return null;
}

FileLogger.Log($"Downloaded valid {format} image: {filename}");
```

**Update WallpaperService.cs** (replace lines 39-45):
```csharp
// Remove old extension check, replace with:
private readonly IImageValidator _imageValidator;

public WallpaperService(IImageValidator imageValidator)
{
    _imageValidator = imageValidator;
}

public void SetWallpaper(string imagePath)
{
    string absolutePath = Path.GetFullPath(imagePath);

    if (!File.Exists(absolutePath))
        throw new FileNotFoundException($"Image file not found: {absolutePath}");

    // Validate using magic bytes instead of extension
    if (!_imageValidator.IsValidImage(absolutePath, out var format))
    {
        throw new InvalidImageException(
            $"Invalid image file. Only PNG, JPG, and BMP formats are supported.");
    }

    // ... rest of method ...
}
```

### Acceptance Criteria

- [ ] `IImageValidator` interface exists with `IsValidImage(string, out ImageFormat)` method
- [ ] `ImageValidator` correctly identifies PNG, JPEG, BMP by magic bytes
- [ ] Fake PNG file (`.png` extension with wrong content) is rejected
- [ ] Valid PNG/JPG/BMP files are accepted
- [ ] Invalid files are deleted immediately after download
- [ ] `WallpaperService` throws `InvalidImageException` for invalid files
- [ ] All tests pass (new + existing)

### Testing Requirements

**Unit Tests** (ImageValidatorTests.cs):
```csharp
[Fact]
public void IsValidImage_ValidPNG_ReturnsTrue()
{
    // Create file with PNG magic bytes
    byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    string testFile = Path.GetTempFileName();
    File.WriteAllBytes(testFile, pngHeader);

    // Act
    var validator = new ImageValidator();
    bool isValid = validator.IsValidImage(testFile, out var format);

    // Assert
    Assert.True(isValid);
    Assert.Equal(ImageFormat.PNG, format);

    File.Delete(testFile);
}

[Fact]
public void IsValidImage_FakePNG_ReturnsFalse()
{
    // Create file with wrong magic bytes but .png extension
    byte[] fakeContent = { 0x00, 0x00, 0x00, 0x00 };
    string testFile = Path.GetTempFileName() + ".png";
    File.WriteAllBytes(testFile, fakeContent);

    // Act
    var validator = new ImageValidator();
    bool isValid = validator.IsValidImage(testFile, out var format);

    // Assert
    Assert.False(isValid);
    Assert.Equal(ImageFormat.Unknown, format);

    File.Delete(testFile);
}

[Fact]
public void IsValidImage_ValidJPEG_ReturnsTrue() { /* ... */ }

[Fact]
public void IsValidImage_ValidBMP_ReturnsTrue() { /* ... */ }

[Fact]
public void IsValidImage_EmptyFile_ReturnsFalse() { /* ... */ }

[Fact]
public void IsValidImage_NonExistentFile_ReturnsFalse() { /* ... */ }
```

**Manual Security Test**:
1. Create malicious test file: `echo "malicious content" > virus.png`
2. Try to set as wallpaper via app
3. Expected: Rejected with "Invalid image format" error
4. Verify file is deleted from temp directory

### Definition of Done

- [x] All unit tests pass (6+ new tests)
- [x] Security test passes (malicious file rejected)
- [x] `ImageFetcher` deletes invalid downloads
- [x] `WallpaperService` rejects invalid files before P/Invoke
- [x] No breaking changes to existing functionality
- [x] Code review: magic byte signatures correct
- [x] Performance: validation <100ms per file

---

##Story WS-2: Application State Service

**Story Points**: 3
**Type**: Feature / Infrastructure
**Priority**: HIGH

### Context

No persistent storage exists for runtime state (enabled/disabled, last-known-good path, first-run flag). This state is needed for:
- Enable/disable toggle (Phase 3)
- Last-known-good fallback (Story WS-5)
- Welcome wizard (Phase 3)
- Usage statistics

### Description

Create `IAppStateService` that persists application state to JSON file in `%LOCALAPPDATA%\WallpaperSync\state.json`. This provides a clean separation between user configuration (settings) and application runtime state.

### Tasks

- [ ] Create `src/WallpaperApp/Models/AppState.cs` model class
- [ ] Create `src/WallpaperApp/Services/IAppStateService.cs` interface
- [ ] Create `src/WallpaperApp/Services/AppStateService.cs` implementation
- [ ] Implement JSON serialization with `System.Text.Json`
- [ ] Create state directory if it doesn't exist
- [ ] Handle missing/corrupt state files (return default state)
- [ ] Register `IAppStateService` in DI container
- [ ] Write unit tests for save/load/update operations

### Files to Create

```
src/WallpaperApp/Models/AppState.cs
src/WallpaperApp/Services/IAppStateService.cs
src/WallpaperApp/Services/AppStateService.cs
src/WallpaperApp.Tests/Services/AppStateServiceTests.cs
```

### Implementation Details

**AppState.cs**:
```csharp
namespace WallpaperApp.Models
{
    public class AppState
    {
        public bool IsEnabled { get; set; } = true;
        public string? LastKnownGoodImagePath { get; set; }
        public bool IsFirstRun { get; set; } = true;
        public DateTime? LastUpdateTime { get; set; }
        public int UpdateSuccessCount { get; set; } = 0;
        public int UpdateFailureCount { get; set; } = 0;
    }
}
```

**IAppStateService.cs**:
```csharp
public interface IAppStateService
{
    AppState LoadState();
    void SaveState(AppState state);
    void UpdateLastKnownGood(string imagePath);
    void SetEnabled(bool enabled);
    void MarkFirstRunComplete();
    void IncrementSuccessCount();
    void IncrementFailureCount();
}
```

**AppStateService.cs**:
```csharp
public class AppStateService : IAppStateService
{
    private readonly string _stateFilePath;

    public AppStateService()
    {
        string appDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        string stateDirectory = Path.Combine(appDataPath, "WallpaperSync");
        Directory.CreateDirectory(stateDirectory);
        _stateFilePath = Path.Combine(stateDirectory, "state.json");
    }

    public AppState LoadState()
    {
        if (!File.Exists(_stateFilePath))
            return new AppState(); // Default state

        try
        {
            string json = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
        }
        catch (Exception ex)
        {
            FileLogger.Log($"Failed to load state, using defaults: {ex.Message}");
            return new AppState();
        }
    }

    public void SaveState(AppState state)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(state, options);
            File.WriteAllText(_stateFilePath, json);
        }
        catch (Exception ex)
        {
            FileLogger.Log($"Failed to save state: {ex.Message}");
        }
    }

    public void UpdateLastKnownGood(string imagePath)
    {
        var state = LoadState();
        state.LastKnownGoodImagePath = imagePath;
        state.LastUpdateTime = DateTime.Now;
        SaveState(state);
    }

    public void SetEnabled(bool enabled)
    {
        var state = LoadState();
        state.IsEnabled = enabled;
        SaveState(state);
    }

    public void MarkFirstRunComplete()
    {
        var state = LoadState();
        state.IsFirstRun = false;
        SaveState(state);
    }

    public void IncrementSuccessCount()
    {
        var state = LoadState();
        state.UpdateSuccessCount++;
        state.LastUpdateTime = DateTime.Now;
        SaveState(state);
    }

    public void IncrementFailureCount()
    {
        var state = LoadState();
        state.UpdateFailureCount++;
        SaveState(state);
    }
}
```

### Acceptance Criteria

- [ ] `AppState` model has 6 properties (IsEnabled, LastKnownGoodImagePath, etc.)
- [ ] `AppStateService` creates state directory if missing
- [ ] State persists across app restarts
- [ ] Missing/corrupt state file returns default state (doesn't crash)
- [ ] JSON file is human-readable (WriteIndented = true)
- [ ] All helper methods (UpdateLastKnownGood, SetEnabled, etc.) work
- [ ] All tests pass

### Testing Requirements

```csharp
[Fact]
public void LoadState_NoFile_ReturnsDefaultState()
{
    var service = new AppStateService();
    var state = service.LoadState();

    Assert.True(state.IsEnabled);
    Assert.True(state.IsFirstRun);
    Assert.Null(state.LastKnownGoodImagePath);
}

[Fact]
public void SaveState_ThenLoad_ReturnsSameState()
{
    var service = new AppStateService();
    var originalState = new AppState
    {
        IsEnabled = false,
        LastKnownGoodImagePath = "C:\\test.png",
        IsFirstRun = false
    };

    service.SaveState(originalState);
    var loadedState = service.LoadState();

    Assert.Equal(originalState.IsEnabled, loadedState.IsEnabled);
    Assert.Equal(originalState.LastKnownGoodImagePath, loadedState.LastKnownGoodImagePath);
    Assert.Equal(originalState.IsFirstRun, loadedState.IsFirstRun);
}

[Fact]
public void UpdateLastKnownGood_UpdatesPathAndTime() { /* ... */ }

[Fact]
public void SetEnabled_UpdatesEnabledFlag() { /* ... */ }

[Fact]
public void MarkFirstRunComplete_SetsFlagToFalse() { /* ... */ }
```

### Definition of Done

- [x] All tests pass (5+ tests)
- [x] State file created in correct location
- [x] Handles file system errors gracefully
- [x] JSON format is human-readable
- [x] No breaking changes to existing functionality

---

## Story WS-3: Enhanced Configuration Model

**Story Points**: 2
**Type**: Feature / Enhancement
**Priority**: HIGH

### Context

Current `AppSettings` only has 2 properties (ImageUrl, RefreshIntervalMinutes). Need to add properties for fit modes, local file support, and notification preferences.

### Description

Extend `AppSettings` model with new properties for Phase 3 features. All new properties have sensible defaults for backward compatibility (existing JSON files won't break).

### Tasks

- [ ] Add new properties to `AppSettings.cs`:
  - `WallpaperFitMode FitMode` (enum)
  - `bool EnableNotifications`
  - `string? LocalImagePath`
  - `ImageSource SourceType` (enum)
- [ ] Create `WallpaperFitMode` enum (Fill, Fit, Stretch, Tile, Center)
- [ ] Create `ImageSource` enum (Url, LocalFile)
- [ ] Add validation for new properties in `ConfigurationService`
- [ ] Update existing tests to use default values
- [ ] Test backward compatibility (load old JSON format)

### Files to Create

```
src/WallpaperApp/Models/WallpaperFitMode.cs
src/WallpaperApp/Models/ImageSource.cs
```

### Files to Modify

```
src/WallpaperApp/Configuration/AppSettings.cs
src/WallpaperApp/Configuration/ConfigurationService.cs (add validation)
src/WallpaperApp.Tests/Configuration/ConfigurationServiceTests.cs
```

### Implementation Details

**WallpaperFitMode.cs**:
```csharp
namespace WallpaperApp.Models
{
    public enum WallpaperFitMode
    {
        Fill,      // Default - maintains aspect ratio, crops edges
        Fit,       // Entire image visible, letterboxing
        Stretch,   // Distort to fill screen
        Tile,      // Repeat pattern
        Center     // Center image, no scaling
    }
}
```

**ImageSource.cs**:
```csharp
namespace WallpaperApp.Models
{
    public enum ImageSource
    {
        Url,
        LocalFile
    }
}
```

**Updated AppSettings.cs**:
```csharp
public class AppSettings
{
    // Existing
    public string ImageUrl { get; set; } = string.Empty;
    public int RefreshIntervalMinutes { get; set; } = 15;

    // NEW
    public WallpaperFitMode FitMode { get; set; } = WallpaperFitMode.Fill;
    public bool EnableNotifications { get; set; } = false;
    public string? LocalImagePath { get; set; }
    public ImageSource SourceType { get; set} = ImageSource.Url;
}
```

**Add validation in ConfigurationService**:
```csharp
// After loading from JSON
if (settings.SourceType == ImageSource.LocalFile)
{
    if (string.IsNullOrEmpty(settings.LocalImagePath))
        throw new ConfigurationException("LocalImagePath is required when SourceType is LocalFile");

    if (!File.Exists(settings.LocalImagePath))
        throw new ConfigurationException($"Local image file not found: {settings.LocalImagePath}");
}
else // ImageSource.Url
{
    if (string.IsNullOrEmpty(settings.ImageUrl))
        throw new ConfigurationException("ImageUrl is required when SourceType is Url");
}
```

### Acceptance Criteria

- [ ] `WallpaperFitMode` enum with 5 values exists
- [ ] `ImageSource` enum with 2 values exists
- [ ] `AppSettings` has 6 total properties (2 old + 4 new)
- [ ] Old JSON files load successfully with defaults
- [ ] Validation enforces: URL required for Url mode, LocalImagePath required for LocalFile mode
- [ ] All tests pass

### Testing Requirements

```csharp
[Fact]
public void LoadConfiguration_OldFormat_AddsDefaults()
{
    // Create old format JSON (only ImageUrl and RefreshIntervalMinutes)
    string oldJson = @"{
        ""AppSettings"": {
            ""ImageUrl"": ""https://example.com/image.png"",
            ""RefreshIntervalMinutes"": 15
        }
    }";
    File.WriteAllText("WallpaperApp.json", oldJson);

    var service = new ConfigurationService();
    var settings = service.LoadConfiguration();

    // New properties should have defaults
    Assert.Equal(WallpaperFitMode.Fill, settings.FitMode);
    Assert.False(settings.EnableNotifications);
    Assert.Null(settings.LocalImagePath);
    Assert.Equal(ImageSource.Url, settings.SourceType);
}

[Fact]
public void LoadConfiguration_LocalFileMode_ValidatesPath() { /* ... */ }

[Fact]
public void LoadConfiguration_LocalFileMode_MissingPath_ThrowsException() { /* ... */ }
```

### Definition of Done

- [x] Backward compatible (old JSON files work)
- [x] All validation rules implemented
- [x] All tests pass
- [x] No breaking changes

---

## Story WS-4: Wallpaper Fit Modes

**Story Points**: 3
**Type**: Feature
**Priority**: HIGH

### Context

Windows supports multiple wallpaper display styles (fill, fit, stretch, tile, center) controlled via registry keys. Currently, the app uses whatever style was previously set. Users need control over how images are displayed.

### Description

Enhance `WallpaperService` to set wallpaper fit mode by writing registry values before calling `SystemParametersInfo`. This requires writing to `HKCU\Control Panel\Desktop` registry keys.

### Tasks

- [ ] Add `WallpaperFitMode` parameter to `IWallpaperService.SetWallpaper()`
- [ ] Implement `SetWallpaperStyleRegistry(WallpaperFitMode mode)` private method
- [ ] Write registry keys for each fit mode:
  - Fill: WallpaperStyle="10", TileWallpaper="0"
  - Fit: WallpaperStyle="6", TileWallpaper="0"
  - Stretch: WallpaperStyle="2", TileWallpaper="0"
  - Tile: WallpaperStyle="0", TileWallpaper="1"
  - Center: WallpaperStyle="0", TileWallpaper="0"
- [ ] Update `WallpaperUpdater` to pass `settings.FitMode` to SetWallpaper
- [ ] Keep backward-compatible overload (calls new method with Fill mode)
- [ ] Write tests for each fit mode
- [ ] Manual testing: verify each mode displays correctly

### Files to Modify

```
src/WallpaperApp/Services/IWallpaperService.cs
src/WallpaperApp/Services/WallpaperService.cs
src/WallpaperApp/Services/WallpaperUpdater.cs
src/WallpaperApp.Tests/Services/WallpaperServiceTests.cs
```

### Implementation Details

**Update IWallpaperService.cs**:
```csharp
public interface IWallpaperService
{
    void SetWallpaper(string imagePath); // Keep for backward compat
    void SetWallpaper(string imagePath, WallpaperFitMode fitMode);
}
```

**Update WallpaperService.cs**:
```csharp
using Microsoft.Win32;

public void SetWallpaper(string imagePath)
{
    SetWallpaper(imagePath, WallpaperFitMode.Fill); // Default
}

public void SetWallpaper(string imagePath, WallpaperFitMode fitMode)
{
    string absolutePath = Path.GetFullPath(imagePath);

    if (!File.Exists(absolutePath))
        throw new FileNotFoundException($"Image file not found: {absolutePath}");

    // Validate image (Story WS-1)
    if (!_imageValidator.IsValidImage(absolutePath, out _))
    {
        throw new InvalidImageException(
            "Invalid image file. Only PNG, JPG, and BMP formats are supported.");
    }

    // Set wallpaper style in registry
    SetWallpaperStyleRegistry(fitMode);

    // Call Windows API
    const int SPI_SETDESKWALLPAPER = 0x0014;
    const int SPIF_UPDATEINIFILE = 0x01;
    const int SPIF_SENDCHANGE = 0x02;

    int result = SystemParametersInfo(
        SPI_SETDESKWALLPAPER,
        0,
        absolutePath,
        SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

    if (result == 0)
    {
        int errorCode = Marshal.GetLastWin32Error();
        throw new WallpaperException(
            $"Failed to set wallpaper. Error code: {errorCode}");
    }
}

private void SetWallpaperStyleRegistry(WallpaperFitMode mode)
{
    try
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
        if (key == null)
            throw new WallpaperException("Failed to open Desktop registry key");

        switch (mode)
        {
            case WallpaperFitMode.Fill:
                key.SetValue("WallpaperStyle", "10");
                key.SetValue("TileWallpaper", "0");
                break;
            case WallpaperFitMode.Fit:
                key.SetValue("WallpaperStyle", "6");
                key.SetValue("TileWallpaper", "0");
                break;
            case WallpaperFitMode.Stretch:
                key.SetValue("WallpaperStyle", "2");
                key.SetValue("TileWallpaper", "0");
                break;
            case WallpaperFitMode.Tile:
                key.SetValue("WallpaperStyle", "0");
                key.SetValue("TileWallpaper", "1");
                break;
            case WallpaperFitMode.Center:
                key.SetValue("WallpaperStyle", "0");
                key.SetValue("TileWallpaper", "0");
                break;
        }
    }
    catch (Exception ex)
    {
        // Log but don't fail - wallpaper will use existing style
        FileLogger.Log($"Failed to set wallpaper style in registry: {ex.Message}");
    }
}
```

**Update WallpaperUpdater.cs**:
```csharp
_wallpaperService.SetWallpaper(downloadedPath, settings.FitMode);
```

### Acceptance Criteria

- [ ] Each fit mode sets correct registry values
- [ ] Wallpaper displays correctly for all 5 modes (manual test)
- [ ] Registry write failures are logged but don't crash app
- [ ] Backward-compatible overload exists
- [ ] All tests pass

### Testing Requirements

**Manual Testing Checklist** (CRITICAL):
- [ ] **Fill mode**: Image maintains aspect ratio, crops to fill screen
- [ ] **Fit mode**: Entire image visible, letterboxing on edges
- [ ] **Stretch mode**: Image distorted to fill screen
- [ ] **Tile mode**: Image repeats as pattern
- [ ] **Center mode**: Image centered, not scaled

Use test images with different aspect ratios (portrait, landscape, square).

### Definition of Done

- [x] All 5 fit modes tested manually
- [x] Registry keys verified with RegEdit
- [x] Errors handled gracefully
- [x] All tests pass

---

## Story WS-5: Last-Known-Good Fallback

**Story Points**: 3
**Type**: Feature / Reliability
**Priority**: HIGH

### Context

When image downloads fail (network down, server error, invalid image), the wallpaper remains unchanged. This is acceptable but could be improved: if we have a previously successful image, we should use it as a fallback.

### Description

Copy successful images to a persistent location (`%LOCALAPPDATA%\WallpaperSync\last-known-good.png`). On download failure, use this cached image. This provides graceful degradation in offline scenarios.

### Tasks

- [ ] Update `WallpaperUpdater.UpdateWallpaperAsync()` to copy successful downloads
- [ ] Save to `%LOCALAPPDATA%\WallpaperSync\last-known-good.png`
- [ ] Call `_appStateService.UpdateLastKnownGood(path)` after successful set
- [ ] On download failure (`downloadedPath == null`), check for last-known-good
- [ ] If last-known-good exists and is valid, call `SetWallpaper` with cached path
- [ ] Return `true` on fallback (graceful degradation, not a failure)
- [ ] Only show notification on manual refresh (not automatic fallback)
- [ ] Write tests for fallback scenarios

### Files to Modify

```
src/WallpaperApp/Services/WallpaperUpdater.cs
src/WallpaperApp.Tests/Services/WallpaperUpdaterTests.cs
```

### Implementation Details

**Update WallpaperUpdater.cs**:
```csharp
public async Task<bool> UpdateWallpaperAsync(bool isManualRefresh = false)
{
    var settings = _configurationService.LoadConfiguration();
    string imageUrl = settings.SourceType == ImageSource.Url
        ? settings.ImageUrl
        : settings.LocalImagePath;

    string? downloadedPath = null;

    if (settings.SourceType == ImageSource.Url)
    {
        downloadedPath = await _imageFetcher.DownloadImageAsync(settings.ImageUrl);
    }
    else
    {
        downloadedPath = settings.LocalImagePath; // Use local file directly
    }

    if (downloadedPath == null)
    {
        // FALLBACK: Try last-known-good image
        var state = _appStateService.LoadState();
        if (!string.IsNullOrEmpty(state.LastKnownGoodImagePath) &&
            File.Exists(state.LastKnownGoodImagePath))
        {
            FileLogger.Log("Download failed, using last-known-good image");
            try
            {
                _wallpaperService.SetWallpaper(
                    state.LastKnownGoodImagePath,
                    settings.FitMode);

                _appStateService.IncrementFailureCount();
                return true; // Graceful degradation
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Last-known-good also failed: {ex.Message}");
            }
        }

        _appStateService.IncrementFailureCount();
        return false;
    }

    try
    {
        // SUCCESS: Save as last-known-good
        string appDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        string lkgDirectory = Path.Combine(appDataPath, "WallpaperSync");
        Directory.CreateDirectory(lkgDirectory);
        string lkgPath = Path.Combine(lkgDirectory, "last-known-good.png");

        File.Copy(downloadedPath, lkgPath, overwrite: true);
        _appStateService.UpdateLastKnownGood(lkgPath);

        // Set wallpaper
        _wallpaperService.SetWallpaper(downloadedPath, settings.FitMode);

        _appStateService.IncrementSuccessCount();

        // Only show notification on manual refresh
        if (isManualRefresh)
        {
            // TODO: Add notification in Phase 3
            FileLogger.Log("Manual refresh successful");
        }

        return true;
    }
    catch (Exception ex)
    {
        FileLogger.Log($"Failed to set wallpaper: {ex.Message}");
        _appStateService.IncrementFailureCount();
        return false;
    }
}
```

### Acceptance Criteria

- [ ] Successful downloads copied to `%LOCALAPPDATA%\WallpaperSync\last-known-good.png`
- [ ] `AppState.LastKnownGoodImagePath` updated after success
- [ ] Download failures trigger fallback to cached image
- [ ] Fallback returns `true` (not treated as error)
- [ ] Manual refresh shows notification (Phase 3 integration point)
- [ ] All tests pass

### Testing Requirements

```csharp
[Fact]
public async Task UpdateWallpaperAsync_DownloadFails_UsesLastKnownGood()
{
    // Arrange: Setup last-known-good image in state
    string lkgPath = Path.Combine(_testDirectory, "last-known-good.png");
    CreateTestBmpFile(lkgPath);

    var mockState = new Mock<IAppStateService>();
    mockState.Setup(s => s.LoadState())
        .Returns(new AppState { LastKnownGoodImagePath = lkgPath });

    // Arrange: Download will fail (return null)
    _mockImageFetcher.Setup(f => f.DownloadImageAsync(It.IsAny<string>()))
        .ReturnsAsync((string?)null);

    var updater = new WallpaperUpdater(
        _mockConfigService.Object,
        _mockImageFetcher.Object,
        _mockWallpaperService.Object,
        mockState.Object);

    // Act
    bool result = await updater.UpdateWallpaperAsync();

    // Assert
    Assert.True(result); // Graceful degradation
    _mockWallpaperService.Verify(
        w => w.SetWallpaper(lkgPath, It.IsAny<WallpaperFitMode>()),
        Times.Once);
}

[Fact]
public async Task UpdateWallpaperAsync_Success_SavesLastKnownGood() { /* ... */ }

[Fact]
public async Task UpdateWallpaperAsync_DownloadFails_NoLastKnownGood_ReturnsFalse() { /* ... */ }
```

### Definition of Done

- [x] All tests pass (3+ new tests)
- [x] Manual test: disconnect network, verify fallback works
- [x] Manual test: successful download saves to persistent location
- [x] Fallback provides graceful degradation

---

## Story WS-6: File Cleanup Service

**Story Points**: 2
**Type**: Feature / Maintenance
**Priority**: MEDIUM

### Context

Downloaded images accumulate in `%TEMP%\WallpaperSync\` forever. After months of use, this directory could contain hundreds of files. Need automatic cleanup to keep only recent files.

### Description

Add cleanup logic to `ImageFetcher` that deletes old downloaded images. Keep only 2 most recent files (current + 1 backup for rollback testing). Run cleanup before each download.

### Tasks

- [ ] Add `CleanupOldImages()` private method to `ImageFetcher`
- [ ] Delete files older than the 2 most recent
- [ ] Sort by creation time (newest first)
- [ ] Handle cleanup errors gracefully (log but don't fail download)
- [ ] Call cleanup at start of `DownloadImageAsync()` (before download)
- [ ] Write tests for cleanup logic

### Files to Modify

```
src/WallpaperApp/Services/ImageFetcher.cs
src/WallpaperApp.Tests/Services/ImageFetcherTests.cs
```

### Implementation Details

**Update ImageFetcher.cs**:
```csharp
public async Task<string?> DownloadImageAsync(string url)
{
    // Run cleanup BEFORE download
    CleanupOldImages();

    // ... existing download logic ...
}

private void CleanupOldImages()
{
    try
    {
        if (!Directory.Exists(_tempDirectory))
            return;

        var files = Directory.GetFiles(_tempDirectory, "wallpaper-*.png")
                             .Select(f => new FileInfo(f))
                             .OrderByDescending(f => f.CreationTime)
                             .ToList();

        // Keep 2 most recent files
        foreach (var file in files.Skip(2))
        {
            try
            {
                file.Delete();
                FileLogger.Log($"Deleted old wallpaper file: {file.Name}");
            }
            catch (Exception ex)
            {
                // Log but continue - not critical if cleanup fails
                FileLogger.Log($"Failed to delete {file.Name}: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        FileLogger.Log($"Cleanup error: {ex.Message}");
    }
}
```

### Acceptance Criteria

- [ ] Cleanup runs before each download
- [ ] Only 2 most recent files remain after cleanup
- [ ] Cleanup errors don't prevent downloads
- [ ] Cleanup is logged
- [ ] All tests pass

### Testing Requirements

```csharp
[Fact]
public async Task DownloadImageAsync_CleansUpOldFiles()
{
    // Arrange: Create 5 old files
    for (int i = 0; i < 5; i++)
    {
        string oldFile = Path.Combine(_testDirectory, $"wallpaper-old{i}.png");
        File.WriteAllBytes(oldFile, new byte[] { 0x00 });
        await Task.Delay(10); // Ensure different creation times
    }

    // Act: Download new file (triggers cleanup)
    var result = await _fetcher.DownloadImageAsync("https://example.com/new.png");

    // Assert: Only 2 files remain (new + 1 old)
    var remainingFiles = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
    Assert.Equal(2, remainingFiles.Length);
}

[Fact]
public async Task CleanupOldImages_ErrorsDontPreventDownload() { /* ... */ }
```

### Definition of Done

- [x] All tests pass (2+ new tests)
- [x] Manual test: verify cleanup after 3 downloads
- [x] Errors handled gracefully
- [x] No performance impact (<50ms for cleanup)

---

## Phase 1 Complete Checklist

When all 6 stories are done:

- [ ] All unit tests pass (40+ new tests)
- [ ] Security vulnerability fixed and verified
- [ ] No files older than 2 cycles in temp directory
- [ ] All 5 fit modes display correctly (manual test)
- [ ] Download failures fall back to last-known-good
- [ ] State persists across app restarts
- [ ] No breaking changes to existing functionality
- [ ] Code coverage >85%
- [ ] Performance: validation <100ms, cleanup <50ms
- [ ] Ready for Phase 2 (renaming)

---

**END OF PHASE 1 STORIES**
