# ADR-004: Use appsettings.json for Configuration

**Status**: Accepted
**Date**: 2026-02-14
**Deciders**: Project team (from initial requirements)

## Context

The application needs to store user-configurable settings:
- Image URL (e.g., `https://weather.zamflam.com/latest.png`)
- Refresh interval (e.g., 15 minutes)
- Future: logging verbosity, proxy settings, multi-monitor config

Configuration options considered:
1. **appsettings.json** - JSON file in application directory
2. **Windows Registry** - `HKCU\Software\WeatherWallpaper\`
3. **Environment variables** - `WEATHER_WALLPAPER_URL`
4. **Command-line arguments** - `WallpaperApp.exe --url=...`
5. **INI file** - `config.ini` in application directory
6. **Database** - SQLite file for settings

Requirements:
- Must be easy for non-technical users to edit
- Must support validation (e.g., URL must be HTTPS)
- Must be testable (inject config in tests)
- Must work for both console and Windows Service modes
- Should follow .NET conventions

## Decision

**Use `appsettings.json` with `Microsoft.Extensions.Configuration.Json`.**

File location: Same directory as executable
- Console mode: `src/WallpaperApp/appsettings.json`
- Published: `publish/appsettings.json`
- Installed service: `C:\Program Files\WeatherWallpaper\appsettings.json`

File structure:
```json
{
  "AppSettings": {
    "ImageUrl": "https://weather.zamflam.com/latest.png",
    "RefreshIntervalMinutes": 15
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

Load via:
```csharp
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
```

## Consequences

### Positive

- **Standard .NET pattern**: Familiar to .NET developers; follows framework conventions
- **Human-readable**: JSON is easy to edit with Notepad; syntax errors are clear
- **Strongly-typed**: Bind to C# classes (`AppSettings`) for compile-time safety
- **Validation-friendly**: Validate in `ConfigurationService.LoadConfiguration()` with clear error messages
- **Testable**: Easy to create test config files or mock `IConfiguration`
- **Hierarchical**: Supports nested settings (e.g., `Serilog.MinimumLevel`)
- **Extensible**: Can add new settings without schema migration
- **Change detection**: `reloadOnChange: true` allows hot-reload (future story)
- **No elevation required**: JSON file doesn't require admin rights to edit (unlike registry)

### Negative

- **Manual editing**: Users must edit file directly (no GUI)
  - Mitigation: Documentation provides clear examples; syntax is simple
  - Future story: Config GUI (optional)
- **File must be readable**: Service account must have read access
  - Mitigation: Install to user directory or grant LocalSystem read access
- **Syntax errors break app**: Invalid JSON causes startup failure
  - Mitigation: Validation provides clear error messages with fix instructions
- **No encryption**: Secrets stored in plaintext
  - Mitigation: Not a concern for this app (no passwords/API keys)

### Trade-offs Considered

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| appsettings.json (chosen) | Standard, testable, human-readable | Manual editing only | ✅ Best fit |
| Registry | Native Windows, GUI (regedit) | Requires admin, hard to test, non-portable | ❌ Too Windows-specific |
| Environment variables | Easy to override, 12-factor friendly | Hard to manage multiple settings, not user-friendly | ❌ Not suitable for desktop app |
| Command-line args | Simple, scriptable | Lost on restart, no persistence | ❌ Windows Service can't use |
| INI file | Old-school, simple | No .NET support, no hierarchy, outdated | ❌ Not .NET idiomatic |
| Database | Structured, queryable | Massive overkill, requires migration logic | ❌ Over-engineering |

**Verdict**: appsettings.json balances .NET conventions, testability, and user simplicity.

## Implementation Guidance

For **Story 2** (Configuration Service):
```csharp
public class AppSettings
{
    public string ImageUrl { get; set; } = string.Empty;
    public int RefreshIntervalMinutes { get; set; } = 15;
}

public class ConfigurationService
{
    public AppSettings LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = config.GetSection("AppSettings").Get<AppSettings>();

        // Validation
        if (string.IsNullOrWhiteSpace(settings.ImageUrl))
            throw new ConfigurationException("ImageUrl is required in appsettings.json");

        if (!settings.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ConfigurationException("ImageUrl must use HTTPS");

        return settings;
    }
}
```

For **testing**:
- Create `appsettings.test.json` with test values
- Use `ConfigurationBuilder.AddJsonFile("appsettings.test.json")` in tests
- Test validation: missing URL, HTTP (not HTTPS), empty file

For **installation** (Story 9):
- Copy default `appsettings.json` to installation directory
- Document how to customize settings in `INSTALL.md`
- Installer should NOT overwrite existing config on upgrade

## Validation Rules

Settings must be validated on load:

| Setting | Validation | Error Message |
|---------|------------|---------------|
| `ImageUrl` | Not empty | "ImageUrl is required in appsettings.json" |
| `ImageUrl` | Starts with `https://` | "ImageUrl must use HTTPS for security" |
| `RefreshIntervalMinutes` | > 0 | "RefreshIntervalMinutes must be greater than 0" |
| `RefreshIntervalMinutes` | ≤ 1440 (24 hours) | "RefreshIntervalMinutes must be ≤ 1440 (24 hours)" |

## Future Enhancements

Potential additions in later stories:
- **Hot-reload** (Story 13): `reloadOnChange: true` allows config updates without service restart
- **Config GUI** (Future): Simple WPF app to edit settings visually
- **Secrets encryption** (Future): If API keys are needed, use Windows DPAPI
- **Multiple profiles** (Future): `appsettings.Development.json`, `appsettings.Production.json`

## References

- [STORY_MAP.md](../STORY_MAP.md) - Technical Decisions table
- [Story 2 Acceptance Criteria](../STORY_MAP.md#L196-L208) - Configuration structure
- [Microsoft Learn - Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
