# ADR-001: Use SystemParametersInfo for Wallpaper Changes

**Status**: Accepted
**Date**: 2026-02-14
**Deciders**: Project team (validated via Story 0 spike)

## Context

We need a reliable way to programmatically change the Windows desktop wallpaper from .NET code. Several approaches exist:

1. **Windows API via P/Invoke** - `SystemParametersInfo` with `SPI_SETDESKWALLPAPER`
2. **Registry manipulation** - Modify `HKCU\Control Panel\Desktop` registry keys
3. **PowerShell scripting** - Invoke PowerShell commands from .NET
4. **Third-party libraries** - Use NuGet packages that wrap wallpaper APIs
5. **WMI/CIM** - Use Windows Management Instrumentation

The chosen approach must:
- Work reliably on Windows 10 and Windows 11
- Not require external dependencies
- Support persistence across lock/reboot
- Be simple to implement and test

## Decision

**Use `SystemParametersInfo` from `user32.dll` via P/Invoke.**

Implementation details:
```csharp
[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool SystemParametersInfo(
    uint uiAction, uint uiParam, string pvParam, uint fWinIni);

// Call with:
SystemParametersInfo(
    SPI_SETDESKWALLPAPER,
    0,
    imagePath,
    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
```

Required constants:
- `SPI_SETDESKWALLPAPER = 0x0014`
- `SPIF_UPDATEINIFILE = 0x01` (persist to profile)
- `SPIF_SENDCHANGE = 0x02` (broadcast update)

## Consequences

### Positive

- **Well-documented**: Official Microsoft API with stable documentation
- **Zero dependencies**: No NuGet packages or external tools required
- **Simple**: Implementation is ~15 lines of code
- **Reliable error reporting**: Win32 error codes via `Marshal.GetLastWin32Error()`
- **Persistent**: `SPIF_UPDATEINIFILE` flag ensures wallpaper survives reboot
- **Immediate feedback**: `SPIF_SENDCHANGE` flag updates desktop without requiring logout
- **Battle-tested**: API has existed since Windows 95, highly unlikely to break

### Negative

- **Windows-only**: P/Invoke to `user32.dll` means code won't run on Linux/macOS (acceptable - this is a Windows-only app)
- **Requires testing on Windows**: Unit tests can mock, but integration tests need real Windows environment
- **Absolute path requirement**: API only accepts absolute paths, not relative (minor - we control the path)
- **Format conversion**: Windows internally converts PNG to JPG (acceptable - transparent to user)

### Trade-offs Considered

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| P/Invoke (chosen) | Simple, no dependencies, official API | Windows-only | ✅ Best fit |
| Registry | More control over style/position | Brittle, requires Explorer refresh, unsupported | ❌ Too fragile |
| PowerShell | Easy to prototype | Slow startup, external process overhead | ❌ Performance |
| Third-party lib | Abstraction layer | External dependency, unnecessary | ❌ Over-engineering |
| WMI | Enterprise-friendly | Overly complex for task | ❌ Overkill |

## Implementation Guidance

For **Story 3** (WallpaperService implementation):
- Extract P/Invoke to `IWallpaperService` interface for testability
- Validate file exists before calling API (fail fast)
- Use absolute paths (convert relative if needed)
- Return clear error messages based on Win32 error codes

For **testing**:
- Mock `IWallpaperService` in higher-level tests
- Create manual test checklist for visual validation
- Integration tests require Windows VM or physical machine

## References

- [Spike Results](../spike-results.md) - Detailed API research and validation
- [Microsoft Learn - SystemParametersInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa)
- [PInvoke.net - SystemParametersInfo](https://pinvoke.net/default.aspx/user32/SystemParametersInfo.html)
- Spike code: `spike/wallpaper-api-validation.cs`
