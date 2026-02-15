# Story 3: Wallpaper Service - Set Static Image as Wallpaper

**Context**: Story 2 (Configuration) has been successfully merged to main. The app reads configuration from `WallpaperApp.json` and validates HTTPS URLs.

**Your mission**: Implement Story 3: Wallpaper Service - Set a local image file as desktop wallpaper using Windows API.

---

## 📋 Story 3: Implementation Guide

**Read these documents first:**
1. `plan/adr/ADR-001-use-systemparametersinfo-for-wallpaper.md` - why we use SystemParametersInfo
2. `plan/adr/ADR-003-no-retry-logic-for-failures.md` - error handling strategy
3. `STORY_MAP.md` lines 239-310 - Story 3 acceptance criteria and requirements

---

## 🛠️ What to Build

### 1. Create `src/WallpaperApp/Services/WallpaperService.cs`

Implement a service that sets the desktop wallpaper using Windows API:

```csharp
namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for setting the Windows desktop wallpaper.
    /// </summary>
    public class WallpaperService : IWallpaperService
    {
        // P/Invoke declaration
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
            uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        private const uint SPI_SETDESKWALLPAPER = 0x0014;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        /// <summary>
        /// Sets the desktop wallpaper to the specified image file.
        /// </summary>
        /// <param name="imagePath">Absolute path to the image file.</param>
        /// <exception cref="FileNotFoundException">Image file does not exist.</exception>
        /// <exception cref="InvalidImageException">File is not a valid image format.</exception>
        /// <exception cref="WallpaperException">Windows API call failed.</exception>
        public void SetWallpaper(string imagePath)
        {
            // Implementation here
        }
    }
}
```

### 2. Create `src/WallpaperApp/Services/IWallpaperService.cs`

Interface for testing and dependency injection:

```csharp
namespace WallpaperApp.Services
{
    public interface IWallpaperService
    {
        void SetWallpaper(string imagePath);
    }
}
```

### 3. Create Custom Exceptions

**`src/WallpaperApp/Services/InvalidImageException.cs`**:
```csharp
public class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}
```

**`src/WallpaperApp/Services/WallpaperException.cs`**:
```csharp
public class WallpaperException : Exception
{
    public WallpaperException(string message) : base(message) { }
    public WallpaperException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

### 4. Implementation Requirements

**Validation (fail-fast):**
- Check if file exists using `File.Exists(imagePath)`
  - Throw `FileNotFoundException` with message: `"Image file not found at: {imagePath}"`
- Check file extension is `.png`, `.jpg`, `.jpeg`, or `.bmp`
  - Throw `InvalidImageException` with message: `"Unsupported image format. Supported: PNG, JPG, BMP"`
- Convert relative paths to absolute using `Path.GetFullPath(imagePath)`

**Windows API Call:**
- Call `SystemParametersInfo` with:
  - `uiAction`: `SPI_SETDESKWALLPAPER` (0x0014)
  - `uiParam`: 0
  - `pvParam`: absolute image path
  - `fWinIni`: `SPIF_UPDATEINIFILE | SPIF_SENDCHANGE` (0x03)

**Error Handling:**
- If API returns `false`, get Win32 error code using `Marshal.GetLastWin32Error()`
- Throw `WallpaperException` with message: `"Failed to set wallpaper. Win32 error code: {errorCode}"`

### 5. Write Comprehensive Tests

**`src/WallpaperApp.Tests/WallpaperServiceTests.cs`**:

```csharp
public class WallpaperServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testImagePath;

    public WallpaperServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "WallpaperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create a test image file
        _testImagePath = Path.Combine(_testDirectory, "test.png");
        CreateTestImage(_testImagePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    private void CreateTestImage(string path)
    {
        // Create a minimal valid PNG file (1x1 pixel)
        // You can use System.Drawing or just write minimal PNG bytes
    }

    [Fact]
    public void SetWallpaper_MissingFile_ThrowsFileNotFoundException()
    {
        // Test missing file
    }

    [Fact]
    public void SetWallpaper_InvalidFormat_ThrowsInvalidImageException()
    {
        // Test with .txt file
    }

    [Fact]
    public void SetWallpaper_RelativePath_ResolvesCorrectly()
    {
        // Test relative path conversion
    }

    [Theory]
    [InlineData("test.png")]
    [InlineData("test.jpg")]
    [InlineData("test.jpeg")]
    [InlineData("test.bmp")]
    public void SetWallpaper_SupportedFormats_Succeeds(string filename)
    {
        // Test all supported formats
    }

    // Note: Testing actual Windows API call requires Windows environment
    // Consider marking with [Fact(Skip = "Integration test - requires Windows")]
    // or using a mock/wrapper for the P/Invoke call
}
```

**Testing Strategy:**
- **Unit tests**: Test validation logic (file exists, format checking, path resolution)
- **Integration tests**: Test actual wallpaper setting (requires Windows, can be skipped in CI)
- **Mocking**: For higher-level tests, mock `IWallpaperService`

### 6. Update Program.cs (Minimal Integration)

For now, just demonstrate the service works:

```csharp
// After loading configuration:
Console.WriteLine();
Console.WriteLine("Testing WallpaperService...");

// For Story 3, we just verify the service can be instantiated
// Full integration comes in Story 5
var wallpaperService = new WallpaperService();
Console.WriteLine("✓ WallpaperService initialized");
```

### 7. Manual Test Checklist

Create `MANUAL_TESTS.md` to document:
- [ ] PNG image sets as wallpaper correctly
- [ ] JPG image sets as wallpaper correctly
- [ ] BMP image sets as wallpaper correctly
- [ ] Wallpaper displays at correct resolution/scaling
- [ ] Wallpaper persists after locking/unlocking screen
- [ ] Wallpaper persists after reboot
- [ ] Error message is clear when file not found
- [ ] Error message is clear when invalid format

**How to test manually:**
```csharp
// Add temporary test code to Program.cs:
var testImage = @"C:\Windows\Web\Wallpaper\Windows\img0.jpg";
wallpaperService.SetWallpaper(testImage);
Console.WriteLine($"✓ Wallpaper set to: {testImage}");
```

---

## ✅ Definition of Done

Before committing, verify:

### 1. Run Automated Validation
```bash
scripts\validate.bat
```
All steps must pass: pull → test → build → publish

### 2. Test Count: 10+ tests passing
- 1 test from Story 1 (ApplicationStartsSuccessfully)
- 5 tests from Story 2 (configuration scenarios)
- 4+ tests from Story 3 (wallpaper service scenarios)

### 3. Build Quality
- `dotnet build` → 0 warnings
- `dotnet publish -c Release -r win-x64 --self-contained true` → works

### 4. Runtime Behavior
- App initializes WallpaperService without errors
- Manual test: Setting wallpaper with valid image works
- Manual test: Invalid file path throws clear exception
- Manual test: Invalid format throws clear exception

### 5. Code Quality
- `IWallpaperService` interface allows mocking in tests
- P/Invoke code is isolated in `WallpaperService` class
- Error messages are actionable (show file path, error codes)
- Single Responsibility: WallpaperService only sets wallpaper, nothing else
- XML comments on all public methods and classes

### 6. Documentation
- Manual test checklist completed and results documented
- Code has clear XML comments explaining Win32 constants

---

## 🎯 Important Notes

### Windows API Constants
From ADR-001:
- `SPI_SETDESKWALLPAPER = 0x0014` - Set desktop wallpaper
- `SPIF_UPDATEINIFILE = 0x01` - Persist to user profile (survives reboot)
- `SPIF_SENDCHANGE = 0x02` - Broadcast change (immediate visual update)

### Path Requirements
- Windows API requires **absolute paths**
- Use `Path.GetFullPath()` to convert relative paths
- Validate file exists **before** calling Windows API

### Supported Image Formats
- PNG (recommended for transparency)
- JPG/JPEG
- BMP
- Note: Windows may internally convert formats

### Error Handling Philosophy
From ADR-003:
- Fail fast with clear error messages
- No retry logic (simplicity over complexity)
- Log errors but don't crash the app

---

## 🚀 Commit Message Template

```
Story 3: Wallpaper Service - set static image as wallpaper

Implemented WallpaperService using Windows SystemParametersInfo API:
- Created Services/WallpaperService.cs with P/Invoke to user32.dll
- Created IWallpaperService interface for testability
- Added validation: file exists, supported format (PNG/JPG/BMP)
- Converts relative paths to absolute paths
- Throws clear exceptions: FileNotFoundException, InvalidImageException, WallpaperException
- Added 4+ comprehensive tests covering all scenarios
- Documented manual test checklist in MANUAL_TESTS.md

Windows API implementation:
- Uses SPI_SETDESKWALLPAPER (0x0014)
- Flags: SPIF_UPDATEINIFILE | SPIF_SENDCHANGE for persistence
- Win32 error reporting via Marshal.GetLastWin32Error()

All code follows Uncle Bob principles (SRP, clear naming).

https://claude.ai/code/session_YOUR_SESSION_ID
```

---

## 📚 Reference Materials

- **ADR-001**: Why we use SystemParametersInfo (not registry/PowerShell)
- **ADR-003**: Why we don't retry failures
- **Microsoft Docs**: [SystemParametersInfo API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa)
- **PInvoke.net**: [SystemParametersInfo examples](https://pinvoke.net/default.aspx/user32/SystemParametersInfo.html)

---

## 🔧 Troubleshooting

**If tests fail:**
- Check that test images are created correctly (minimal valid PNG)
- Integration tests require Windows environment (can skip in Linux CI)
- File paths must use absolute paths for Windows API

**If wallpaper doesn't update:**
- Verify `SPIF_SENDCHANGE` flag is set (0x02)
- Check Win32 error code from `Marshal.GetLastWin32Error()`
- Ensure image file path uses backslashes (`\`) not forward slashes (`/`)

**If format validation fails:**
- Check extension comparison is case-insensitive (`StringComparison.OrdinalIgnoreCase`)
- Supported: `.png`, `.jpg`, `.jpeg`, `.bmp`

---

## 💡 Pro Tips

1. **Start with validation tests** - easier to test than Windows API calls
2. **Use Windows built-in images** for manual testing: `C:\Windows\Web\Wallpaper\`
3. **Create minimal PNG** for unit tests (1x1 pixel is valid)
4. **Mark integration tests** with `[Fact(Skip = "Requires Windows")]` for cross-platform CI
5. **Test edge cases**: empty path, null path, long paths, special characters

---

**Working Branch**: Create `claude/story-3-wallpaper-service-[YOUR_SESSION_ID]`

When done, push and the team will review! 🚀
