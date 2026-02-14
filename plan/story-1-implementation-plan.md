# Story 1: Foundation - Console App + First Test

## Implementation & Test Plan

**Goal**: Establish the real project structure with a "Hello World" console app and a first passing xUnit test. This proves the build/test pipeline works and sets the coding standards for everything that follows.

**Estimated Story Points**: 2

---

## Prerequisites

Before you start, make sure you have:

- [ ] .NET 8 SDK installed (`dotnet --version` should print `8.x.x`)
- [ ] Story 0 completed (spike validated that wallpaper API works)
- [ ] Repository cloned and you're on the correct branch

---

## What You'll Build

By the end of this story you will have:

```
windows-image-viewer/
├── src/
│   ├── WallpaperApp/
│   │   ├── WallpaperApp.csproj
│   │   └── Program.cs
│   └── WallpaperApp.Tests/
│       ├── WallpaperApp.Tests.csproj
│       └── ProgramTests.cs
├── .gitignore
├── README.md            (updated with build/test/run instructions)
├── STORY_MAP.md         (already exists)
├── plan/                (already exists)
└── spike/               (from Story 0)
```

---

## Step-by-Step Implementation

### Step 1: Create the Source Directory Structure

From the repository root:

```bash
mkdir -p src
```

### Step 2: Create the Console Application Project

```bash
cd src
dotnet new console -n WallpaperApp --framework net8.0
```

This creates `src/WallpaperApp/` with a default `Program.cs` and `WallpaperApp.csproj`.

### Step 3: Configure the .csproj File

Open `src/WallpaperApp/WallpaperApp.csproj` and replace its entire contents with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
  </PropertyGroup>

</Project>
```

**What each setting does:**

| Setting | Value | Why |
|---------|-------|-----|
| `OutputType` | `Exe` | We're building a console executable |
| `TargetFramework` | `net8.0-windows` | Windows-only app (we use Windows APIs in later stories) |
| `ImplicitUsings` | `enable` | Auto-imports common namespaces like `System`, `System.IO`, etc. |
| `Nullable` | `enable` | Turns on nullable reference type warnings — catches null bugs at compile time |
| `RuntimeIdentifier` | `win-x64` | Target 64-bit Windows when publishing |
| `SelfContained` | `true` | Bundle the .NET runtime into the exe so users don't need .NET installed |

### Step 4: Write Program.cs

**This is the most important step to get right.** The STORY_MAP specifies a test that calls `Program.Main(new string[] { })` and checks that it returns `0`. This means:

1. We **cannot** use top-level statements (the default in .NET 8). We need a traditional `Program` class with a `Main` method.
2. `Main` must return `int` (not `void`), so the test can check the return value.
3. The `Program` class must be accessible from the test project.

Open `src/WallpaperApp/Program.cs` and replace its entire contents with:

```csharp
namespace WallpaperApp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Weather Wallpaper App - Starting...");
            return 0;
        }
    }
}
```

**Why each detail matters:**

- **`public class Program`** — Must be `public` so the test project can access it. By default, .NET makes classes `internal`, which would make the test fail with a compile error.
- **`public static int Main(string[] args)`** — Must be `public` and return `int`. The test calls `Program.Main(new string[] { })` and asserts the return value equals `0`.
- **`Console.WriteLine("Weather Wallpaper App - Starting...")`** — This exact string is the acceptance criteria. Use this exact text.
- **`return 0;`** — Return code 0 means "success." This is what the test asserts.

### Step 5: Verify the App Builds and Runs

```bash
cd src/WallpaperApp
dotnet build
```

**Expected**: Build succeeds with 0 errors and 0 warnings.

```bash
dotnet run
```

**Expected output**:
```
Weather Wallpaper App - Starting...
```

If you see that message, the app is working. Move on.

### Step 6: Create the Test Project

Go back to the `src/` directory and create the test project:

```bash
cd src
dotnet new xunit -n WallpaperApp.Tests --framework net8.0
```

This creates `src/WallpaperApp.Tests/` with a default test file and the xUnit NuGet packages already referenced.

### Step 7: Configure the Test Project .csproj

Open `src/WallpaperApp.Tests/WallpaperApp.Tests.csproj` and replace its entire contents with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\WallpaperApp\WallpaperApp.csproj" />
  </ItemGroup>

</Project>
```

**Critical piece**: The `<ProjectReference>` at the bottom. This tells the test project "I depend on `WallpaperApp`" — without it, the test won't be able to see the `Program` class.

**About the package versions**: The versions above (`6.0.0`, `17.8.0`, `2.5.3`) are known-compatible with .NET 8. If `dotnet new xunit` generated different versions, that's fine — use whatever it generated. The important thing is the `ProjectReference` line.

### Step 8: Write the First Test

Delete the default test file that was generated (it's usually `UnitTest1.cs`):

```bash
rm src/WallpaperApp.Tests/UnitTest1.cs
```

Create `src/WallpaperApp.Tests/ProgramTests.cs` with this content:

```csharp
using WallpaperApp;
using Xunit;

namespace WallpaperApp.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void ApplicationStartsSuccessfully()
        {
            // Arrange & Act
            var exitCode = Program.Main(new string[] { });

            // Assert
            Assert.Equal(0, exitCode);
        }
    }
}
```

**What this test does:**

1. **Calls `Program.Main`** with an empty args array (simulating running the app with no command-line arguments).
2. **Asserts the return value is `0`** (success exit code).
3. **`[Fact]`** is xUnit's attribute for a single test case (as opposed to `[Theory]` which runs multiple data sets).

**Note**: This test will also cause `Console.WriteLine` to execute, which prints to the test runner's output. That's fine — it's harmless side output.

### Step 9: Run the Tests

```bash
cd src
dotnet test
```

**Expected output** (key lines):
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

If you see `Passed: 1` and `Failed: 0`, the test is working. If it fails, read the error message carefully — the most common issues are:

- **"Program is inaccessible due to its protection level"** — You forgot to make the `Program` class `public`. Go back to Step 4.
- **"Program does not contain a definition for Main"** — You're using top-level statements instead of a traditional `Main` method. Go back to Step 4.
- **"The type or namespace 'WallpaperApp' could not be found"** — The `ProjectReference` is missing from the test `.csproj`. Go back to Step 7.

### Step 10: Verify Self-Contained Publish

```bash
cd src/WallpaperApp
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

**Expected**: The `publish/` folder contains `WallpaperApp.exe` and a bunch of supporting DLLs. The exe should run standalone on any Windows 10/11 x64 machine, even without .NET installed.

Verify it runs:

```bash
./publish/WallpaperApp.exe
```

**Expected output**:
```
Weather Wallpaper App - Starting...
```

**Note**: The `publish/` folder will be large (50-80 MB) because it includes the entire .NET runtime. This is expected for self-contained apps. Do not commit this folder (the `.gitignore` in the next step will exclude it).

### Step 11: Add .gitignore

Create a `.gitignore` file in the **repository root** (`windows-image-viewer/.gitignore`). Use the standard .NET gitignore:

```gitignore
## .NET build output
**/bin/
**/obj/
**/publish/

## User-specific files
*.user
*.suo
*.rsuser

## NuGet
**/packages/
*.nupkg

## Visual Studio
.vs/
*.sln.docuser

## Rider
.idea/

## OS files
Thumbs.db
Desktop.ini
.DS_Store

## Test results
**/TestResults/
```

**Why these entries matter:**
- `**/bin/` and `**/obj/` — Build output. Regenerated on every `dotnet build`. Never commit these.
- `**/publish/` — The self-contained publish output. Very large. Never commit.
- `*.user` / `.vs/` / `.idea/` — IDE-specific settings. Different per developer.
- `**/TestResults/` — Test output artifacts. Regenerated on every `dotnet test`.

### Step 12: Update README.md

Replace the contents of the existing `README.md` (in the repository root) with:

```markdown
# Weather Wallpaper App

A Windows desktop application that automatically updates your desktop wallpaper with weather forecast images.

## Requirements

- Windows 10 or Windows 11 (x64)
- .NET 8 SDK (for building from source)

## Build

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
```

---

## Testing Plan

### Automated Tests

There is **one** automated test for this story:

| Test Class | Test Method | What It Validates |
|------------|-------------|-------------------|
| `ProgramTests` | `ApplicationStartsSuccessfully()` | Calling `Program.Main(new string[] { })` returns exit code `0` |

**How to run**: `dotnet test` from the `src/` directory.

**Pass criteria**: `Passed: 1, Failed: 0`.

### Manual Verification Checklist

After the automated test passes, verify these manually:

| # | Check | How to Verify | Pass? |
|---|-------|---------------|-------|
| 1 | App builds without warnings | `dotnet build` output shows `0 Warning(s)` | [ ] |
| 2 | App prints correct startup message | `dotnet run` prints exactly `Weather Wallpaper App - Starting...` | [ ] |
| 3 | `dotnet test` shows 1 passing test | Output includes `Passed: 1, Failed: 0` | [ ] |
| 4 | Self-contained publish succeeds | `dotnet publish -c Release -r win-x64 --self-contained true` exits with code 0 | [ ] |
| 5 | Published exe runs standalone | `./publish/WallpaperApp.exe` prints startup message | [ ] |
| 6 | .gitignore excludes build output | `git status` does NOT show `bin/`, `obj/`, or `publish/` folders | [ ] |
| 7 | README build instructions work | Follow README steps from scratch on a clean checkout — all commands succeed | [ ] |

---

## Common Pitfalls (Read Before You Start)

### Pitfall 1: Top-Level Statements vs. Traditional Main

.NET 8 defaults to "top-level statements" when you run `dotnet new console`. That generates a `Program.cs` like this:

```csharp
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
```

**Do NOT use this.** The test needs to call `Program.Main(...)` explicitly, which isn't possible with top-level statements (the compiler generates an internal class you can't access). You must use the traditional class-based approach shown in Step 4.

### Pitfall 2: Internal vs. Public

If you forget to mark the `Program` class as `public`, you'll get this error when building the test project:

```
error CS0122: 'Program' is inaccessible due to its protection level
```

The fix: make sure `Program.cs` says `public class Program`, not just `class Program`.

### Pitfall 3: void Main vs. int Main

If `Main` returns `void` instead of `int`, the test won't compile:

```
error CS0029: Cannot implicitly convert type 'void' to 'int'
```

The fix: make sure the signature is `public static int Main(string[] args)` and the method `return 0;` at the end.

### Pitfall 4: Test Project Missing ProjectReference

If you forget the `<ProjectReference>` in the test `.csproj`, you'll get:

```
error CS0246: The type or namespace name 'WallpaperApp' could not be found
```

The fix: add this to the test `.csproj` inside an `<ItemGroup>`:
```xml
<ProjectReference Include="..\WallpaperApp\WallpaperApp.csproj" />
```

### Pitfall 5: TargetFramework Mismatch

Both projects must use the same `TargetFramework`. If the app uses `net8.0-windows` but the test project uses `net8.0`, you'll get warnings or errors about incompatible frameworks. Make sure both `.csproj` files specify `net8.0-windows`.

---

## Deliverables Checklist

When you're done, verify every item:

- [ ] `src/WallpaperApp/WallpaperApp.csproj` exists with correct settings (Step 3)
- [ ] `src/WallpaperApp/Program.cs` has `public class Program` with `public static int Main` returning `0` (Step 4)
- [ ] `src/WallpaperApp.Tests/WallpaperApp.Tests.csproj` exists with `ProjectReference` to WallpaperApp (Step 7)
- [ ] `src/WallpaperApp.Tests/ProgramTests.cs` exists with `ApplicationStartsSuccessfully` test (Step 8)
- [ ] `UnitTest1.cs` is deleted (Step 8)
- [ ] `dotnet build` succeeds with 0 warnings (Step 5)
- [ ] `dotnet test` shows 1 passed, 0 failed (Step 9)
- [ ] `dotnet publish` produces self-contained exe that runs (Step 10)
- [ ] `.gitignore` exists in repo root with .NET patterns (Step 11)
- [ ] `README.md` updated with build/test/run instructions (Step 12)

---

## Commit

Once everything above is done:

```bash
git add .gitignore README.md src/
git commit -m "Story 1: Foundation - console app with first passing test

- Created WallpaperApp .NET 8 console application
- Created WallpaperApp.Tests xUnit test project
- Implemented Program.Main with startup message
- Added ApplicationStartsSuccessfully test
- Configured self-contained publish for win-x64
- Added .gitignore for .NET projects
- Updated README with build/test/run instructions

Tests: 1 passed, 0 failed"
```

**Do NOT commit** the `bin/`, `obj/`, or `publish/` directories. The `.gitignore` should prevent this, but double-check with `git status` before committing.

---

## What Comes Next

After Story 1 is done, you'll move to **Story 2: Configuration**. Story 2 adds `appsettings.json` and a `ConfigurationService` class. The project structure from this story (`src/WallpaperApp/` and `src/WallpaperApp.Tests/`) is the foundation everything else builds on — so make sure it's solid before moving on.
