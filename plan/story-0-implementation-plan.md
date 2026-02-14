# Story 0: Tech Spike - Validate Wallpaper API

## Implementation & Test Plan

**Goal**: Prove we can programmatically set desktop wallpaper on Windows 10/11 using .NET. This is throwaway code — focus on learning, not quality.

**Estimated Story Points**: 1

---

## Prerequisites

Before you start, make sure you have:

- [ ] .NET 8 SDK installed (`dotnet --version` should print `8.x.x`)
- [ ] A Windows 10 or Windows 11 machine (this spike uses Windows-only APIs)
- [ ] A test image file (any `.png` or `.jpg` will do — grab one from the internet or use a screenshot)

---

## Step-by-Step Implementation

### Step 1: Create the Spike Directory

From the repository root (`windows-image-viewer/`), create the spike directory:

```bash
mkdir spike
```

### Step 2: Create a Minimal .NET Console App

We're creating a standalone console app just for this spike. It does **not** go in `src/` — it lives in `spike/` because it's throwaway code.

```bash
cd spike
dotnet new console -n WallpaperSpike --framework net8.0
```

This creates `spike/WallpaperSpike/` with a `Program.cs` and `WallpaperSpike.csproj`.

### Step 3: Edit the .csproj to Target Windows

Open `spike/WallpaperSpike/WallpaperSpike.csproj` and change the `TargetFramework` to target Windows:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Why `net8.0-windows`?** The wallpaper API is a Windows-specific P/Invoke call. The `-windows` suffix unlocks Windows-only APIs and makes the intent clear.

### Step 4: Write the Spike Code

Replace the contents of `spike/WallpaperSpike/Program.cs` with the following:

```csharp
using System;
using System.Runtime.InteropServices;

namespace WallpaperSpike
{
    class Program
    {
        // Import the Windows API function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(
            uint uiAction,
            uint uiParam,
            string pvParam,
            uint fWinIni);

        // Constants for the API call
        private const uint SPI_SETDESKWALLPAPER = 0x0014;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        static void Main(string[] args)
        {
            // IMPORTANT: Change this path to point to a real image file on YOUR machine.
            // Use an absolute path. Example:
            //   @"C:\Users\YourName\Pictures\test-wallpaper.png"
            string imagePath = @"C:\temp\test-wallpaper.png";

            if (!System.IO.File.Exists(imagePath))
            {
                Console.WriteLine($"ERROR: Image file not found at: {imagePath}");
                Console.WriteLine("Please update the imagePath variable in Program.cs to point to a real image.");
                return;
            }

            Console.WriteLine($"Attempting to set wallpaper to: {imagePath}");

            int result = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                imagePath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            if (result != 0)
            {
                Console.WriteLine("SUCCESS: Wallpaper has been changed!");
                Console.WriteLine("Look at your desktop to verify.");
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"FAILED: SystemParametersInfo returned 0. Win32 error code: {errorCode}");
            }
        }
    }
}
```

**What this code does, line by line:**

1. **`[DllImport("user32.dll", ...)]`** — This is P/Invoke. It tells .NET to call a function that lives in Windows' `user32.dll` (a core Windows library). Think of it as a bridge between C# and the raw Windows API.

2. **`SPI_SETDESKWALLPAPER = 0x0014`** — This is a constant that tells `SystemParametersInfo` we want to change the wallpaper. Microsoft defines this value; we just use it.

3. **`SPIF_UPDATEINIFILE | SPIF_SENDCHANGE`** — These flags tell Windows to (a) save the setting permanently (survives reboot) and (b) broadcast the change to all windows immediately.

4. **`SystemParametersInfo(...)`** — The actual API call. Returns non-zero on success, 0 on failure.

### Step 5: Place a Test Image

Before running, you need a test image. Do one of the following:

**Option A** (quickest): Copy any `.png` or `.jpg` to `C:\temp\test-wallpaper.png`:
```batch
mkdir C:\temp
copy "C:\Users\YourName\Pictures\some-image.png" "C:\temp\test-wallpaper.png"
```

**Option B**: Update the `imagePath` variable in `Program.cs` to point to any existing image file on your machine.

### Step 6: Build and Run

```bash
cd spike/WallpaperSpike
dotnet build
dotnet run
```

**Expected output (success)**:
```
Attempting to set wallpaper to: C:\temp\test-wallpaper.png
SUCCESS: Wallpaper has been changed!
Look at your desktop to verify.
```

**Expected output (file not found)**:
```
ERROR: Image file not found at: C:\temp\test-wallpaper.png
Please update the imagePath variable in Program.cs to point to a real image.
```

**After running**: Minimize all windows and look at your desktop. The wallpaper should have changed to your test image.

### Step 7: Copy the Standalone Spike File

The acceptance criteria require a single file at `spike/wallpaper-api-validation.cs`. Copy the Program.cs there:

```bash
cp spike/WallpaperSpike/Program.cs spike/wallpaper-api-validation.cs
```

This is just for archival purposes — the runnable project stays in `spike/WallpaperSpike/`.

---

## Testing Plan

This is a tech spike, so testing is **manual visual validation only**. No automated tests needed.

### Manual Test Checklist

Run through each of these and check them off:

| # | Test | How to Verify | Pass? |
|---|------|---------------|-------|
| 1 | App builds without errors | `dotnet build` exits with code 0, no errors in output | [ ] |
| 2 | App runs without crashing | `dotnet run` doesn't throw exceptions | [ ] |
| 3 | Wallpaper changes on success | Minimize all windows → desktop shows new image | [ ] |
| 4 | Error message on missing file | Change `imagePath` to a non-existent file → run → see error message | [ ] |
| 5 | Wallpaper persists after lock/unlock | Lock screen (Win+L) → unlock → wallpaper still shows new image | [ ] |
| 6 | Works on target Windows version | Tested on Windows 10 and/or Windows 11 | [ ] |

### If Something Goes Wrong

- **"Win32 error code: 0"** — The path is likely wrong or the image format isn't supported. Try a `.bmp` file instead.
- **No visible change** — The image may have been set but is the same as your current wallpaper, or the image is too small and your wallpaper "fit" setting is stretching it oddly. Try a different image.
- **Access denied errors** — Make sure you're running from a normal user account (not a restricted service account).

---

## Step 8: Document Spike Results

Create the file `plan/spike-results.md` with your findings. Here's a template:

```markdown
# Spike Results: Wallpaper API Validation

## Date
[Today's date]

## Approach
Used P/Invoke to call `SystemParametersInfo` from `user32.dll` with `SPI_SETDESKWALLPAPER`.

## Findings
- API: `SystemParametersInfo` in `user32.dll`
- Constant: `SPI_SETDESKWALLPAPER` (0x0014)
- Flags: `SPIF_UPDATEINIFILE | SPIF_SENDCHANGE` for persistence
- Input: Absolute file path to image (string)
- Return: Non-zero on success, 0 on failure
- Supported formats: PNG, JPG, BMP (tested: [list what you tested])
- Windows versions tested: [Windows 10 / Windows 11 / both]

## Result
[SUCCESS / FAILURE]

## Decision
[Proceed with wallpaper approach / Pivot to widget approach]

## Notes
- [Any gotchas, surprises, or things to watch out for in later stories]
```

Fill this in honestly based on what you observe. If the API doesn't work, that's valuable information — document it and we'll pivot.

---

## Deliverables Checklist

When you're done, make sure all of these exist:

- [ ] `spike/WallpaperSpike/WallpaperSpike.csproj` — buildable project
- [ ] `spike/WallpaperSpike/Program.cs` — working spike code
- [ ] `spike/wallpaper-api-validation.cs` — copy of the spike code (acceptance criteria artifact)
- [ ] `plan/spike-results.md` — documented findings and decision

## Commit

Once everything above is done and the manual test checklist passes:

```bash
git add spike/ plan/spike-results.md
git commit -m "Story 0: Tech spike - validate wallpaper API via P/Invoke

- Created minimal console app using SystemParametersInfo P/Invoke
- Validated wallpaper changes on Windows 10/11
- Documented API findings in plan/spike-results.md

Tests: Manual visual validation passed"
```

---

## What Comes Next

If the spike **succeeds** (wallpaper changes), proceed to **Story 1**. The P/Invoke approach and constants discovered here will be reused in Story 3 when we build the real `WallpaperService`.

If the spike **fails**, STOP and escalate. We'll pivot to the Widget approach.
