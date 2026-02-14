# ADR-002: Self-Contained Deployment Model

**Status**: Accepted
**Date**: 2026-02-14
**Deciders**: Project team (from initial requirements)

## Context

.NET applications can be deployed in two modes:

1. **Framework-dependent** - Requires .NET runtime to be installed on target machine
   - Smaller deployment size (~200 KB for app)
   - Users must install correct .NET version first
   - Potential version conflicts

2. **Self-contained** - Bundles .NET runtime with application
   - Larger deployment size (~60-80 MB)
   - No installation prerequisites
   - Runs on any Windows machine

Our target users:
- May not be technical (not comfortable installing .NET)
- Want "zero friction" installation
- Expect Windows Service to "just work" after extraction

Project philosophy:
- **Ship fast, zero maintenance**
- Clean uninstall = delete directory, uninstall service
- No shared dependencies or global state

## Decision

**Use self-contained deployment model.**

Configuration in `.csproj`:
```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

Publish command:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

## Consequences

### Positive

- **Zero installation prerequisites**: No need for users to install .NET 8 SDK or runtime
- **Isolated deployment**: Each version is completely independent (no DLL hell)
- **Clean uninstall**: Deleting the app folder removes everything - no registry cleanup needed
- **Predictable behavior**: We control the exact .NET version, eliminating "works on my machine" issues
- **Service reliability**: Windows Service won't break if user installs/uninstalls other .NET apps
- **Easier support**: No need to debug .NET version mismatches or missing runtimes

### Negative

- **Large download size**: ~60-80 MB vs. ~200 KB for framework-dependent
  - Mitigation: Not a concern for desktop app (one-time download)
- **Slower startup (first run)**: Self-contained apps have slightly slower first load
  - Mitigation: Negligible for Windows Service (runs continuously)
- **Multiple copies of .NET runtime**: If user has multiple self-contained .NET apps
  - Mitigation: Disk space is cheap; isolation outweighs redundancy cost
- **Updates require full redeployment**: Can't update just the .NET runtime
  - Mitigation: We control updates; full redeploy is simpler than partial patches

### Trade-offs Considered

| Concern | Framework-Dependent | Self-Contained | Decision |
|---------|---------------------|----------------|----------|
| User experience | Requires .NET install step | Works immediately | **Self-contained wins** |
| Download size | Tiny (~200 KB) | Large (~70 MB) | **Acceptable trade-off** |
| Maintenance | .NET updates break compatibility | Isolated, stable | **Self-contained wins** |
| Disk space | Minimal | 70 MB per install | **Not a concern in 2026** |
| Uninstall cleanliness | Depends on shared runtime | Delete folder = done | **Self-contained wins** |

**Verdict**: User experience and zero-maintenance philosophy outweigh download size concerns.

## Implementation Guidance

For **Story 1** (Foundation):
- Set `<SelfContained>true</SelfContained>` in `.csproj` from the start
- Set `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` (target 64-bit Windows)
- Test that `dotnet publish` produces standalone `.exe`

For **Story 9** (Installation Scripts):
- Installer just extracts ZIP to `C:\Program Files\WeatherWallpaper\`
- No need to check for .NET installation
- Uninstaller = stop service + delete directory

For **testing**:
- Verify published app runs on clean Windows VM without .NET installed
- Check that `publish/` folder is self-contained (includes all DLLs)

## Future Considerations

If download size becomes a problem (user feedback):
- Consider **ReadyToRun compilation** (`<PublishReadyToRun>true</PublishReadyToRun>`) to trade larger size for faster startup
- Consider **single-file publish** (`<PublishSingleFile>true</PublishSingleFile>`) to bundle into one `.exe` (cosmetic benefit only)
- **Do NOT switch to framework-dependent** - that undermines zero-maintenance goal

## References

- [STORY_MAP.md](../STORY_MAP.md) - Technical Decisions table
- [.NET 8 Deployment Modes](https://learn.microsoft.com/en-us/dotnet/core/deploying/) - Microsoft documentation
