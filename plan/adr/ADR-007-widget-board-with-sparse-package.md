# ADR-007: Widget Board Integration via Sparse MSIX Identity Package

**Status**: Accepted
**Date**: 2026-02-22
**Deciders**: Project team

## Context

We are adding a Windows 11 Widget Board widget as a second rendering option alongside the existing desktop wallpaper. The Widget Board (Win+W flyout) is the official Windows 11 platform for ambient, glanceable content. Users who prefer not to change their wallpaper can instead see the same image in the Widget Board.

### The Core Problem: Package Identity Requirement

The Windows Widget Board only activates widget providers that have **package identity** — a property established by installing an MSIX package. Win32 applications (EXEs installed by MSIs) do not have package identity by default.

This creates a conflict: our application is already distributed as a WiX MSI, and replacing it with a full MSIX Store package would require significant infrastructure changes (Store submission, MSIX packaging, Trusted Installer privileges).

### Constraints

1. **Keep the existing MSI installer** — users already have it; breaking distribution is unacceptable
2. **Widget Board requires package identity** — COM server activation by Widget Board fails without it
3. **Widget provider must be an out-of-process COM server** — Widget Board requirement; cannot be in-process
4. **No Store submission** — out of scope for this project's maturity level
5. **Must build on existing .NET/WiX pipeline** — no new build technology

## Decision

**Use a sparse MSIX identity package alongside the existing MSI.**

A sparse MSIX is a minimal MSIX containing only an `AppxManifest.xml` with `AllowExternalContent="true"`. It provides package identity and declares the COM server and widget definitions, but it does **not** contain the executable. The executable remains installed by the MSI at its existing location on disk.

### Implementation Approach

1. **MSI installs everything to disk** — TrayApp, WidgetProvider exe, and the sparse MSIX file itself
2. **TrayApp registers the sparse MSIX at first run** via `PackageManager.AddPackageByUriAsync()` with the `ExternalLocationUri` pointing to the MSI install directory
3. **Widget Board can now activate the COM server** — the manifest's `com:ExeServer` entry points to the WidgetProvider exe at its installed path
4. **Signing** — sparse MSIX must be signed; dev builds use a self-signed cert, production requires a code signing certificate

## Alternatives Considered

### 1. Desktop Overlay (WPF TopMost Window)

Create a transparent WPF window pinned above the desktop (below normal windows) that mimics a widget.

- **Pros**: No MSIX complexity, pure WPF, same tech stack
- **Cons**: Not a real Widget Board widget (doesn't appear in Win+W), doesn't coexist with other widgets, looks unofficial, requires always-on-top window management hacks
- **Rejected**: This provides a worse user experience than no widget at all — it's not discoverable and conflicts with other windows

### 2. Full MSIX Conversion (Replace MSI)

Convert the entire application distribution to MSIX and submit to the Microsoft Store.

- **Pros**: First-class Widget Board support, no signing complexity (Store signs for you), automatic updates
- **Cons**: Requires Store account and submission process, significant packaging work, breaking change for existing users, MSIX installer behavior differences
- **Rejected**: Disproportionate effort for adding one feature; can be revisited if the app matures to Store-quality

### 3. Sparse MSIX alongside MSI (Chosen)

Described above.

- **Pros**: Keeps existing MSI intact, minimal MSIX (just a manifest), no Store required, supported Microsoft pattern for exactly this scenario
- **Cons**: Two installer artifacts to manage, MSIX signing required, deregistration gap at uninstall time
- **Accepted**: Best trade-off between capability and complexity

### 4. Widget via Third-Party Widget Framework

Use a third-party widget platform (e.g., Rainmeter, Übersicht) instead of the Windows Widget Board.

- **Pros**: No MSIX, broad Windows version support
- **Cons**: Requires users to install a separate framework, not integrated with Win+W, not the official Windows 11 experience
- **Rejected**: Defeats the purpose of building a first-class Windows 11 widget

## Consequences

### Positive

1. **Official Widget Board integration** — widget appears in Win+W "Add widgets" panel, discoverable by users
2. **Coexists with other widgets** — standard Widget Board behavior, no window management hacks
3. **Existing MSI preserved** — no breaking change for current users
4. **Microsoft-supported pattern** — sparse packages are documented by Microsoft for exactly this scenario
5. **Testable** — widget provider logic is unit-testable independent of Widget Board runtime

### Negative

1. **MSIX signing required** — developer needs a code signing certificate for production; self-signed cert is sufficient for development but will show SmartScreen warnings on end-user machines
2. **Deregistration gap** — MSI uninstall removes files but does not deregister the sparse MSIX from Windows; the widget provider registration lingers until manually removed or a future custom action is added
3. **Windows App SDK dependency** — adds `Microsoft.WindowsAppSDK` NuGet package to the WidgetProvider project; large package (~120 MB), Windows-only
4. **Windows 11 only** — Widget Board does not exist on Windows 10; widget feature silently absent on W10 (wallpaper still works)
5. **Two artifacts to ship** — MSI must bundle both the WidgetProvider exe and the identity MSIX

### Trade-offs Accepted

| Aspect | Full MSIX | Sparse MSIX + MSI (Chosen) | WPF Overlay |
|--------|-----------|---------------------------|-------------|
| Widget Board support | ✅ Native | ✅ Native | ❌ None |
| MSI preserved | ❌ Replaced | ✅ Kept | ✅ N/A |
| Signing complexity | Store signs | Self-sign or CA cert | ❌ N/A |
| Store required | Yes | No ✅ | No |
| Uninstall cleanup | Automatic | Manual gap ⚠️ | N/A |
| Effort | High | Medium ✅ | Low |
| Official Windows UX | Yes | Yes ✅ | No ❌ |

## When to Reconsider

This decision should be reconsidered if:

1. **Store submission becomes desirable** — at that point, switch to full MSIX and drop the sparse package
2. **Uninstall gap becomes a user complaint** — implement a WiX custom action to call `PackageManager.RemovePackageAsync()` during MSI uninstall
3. **Windows App SDK package size is a problem** — evaluate trimming or a future SDK that ships smaller

## References

- [Microsoft: Sparse packages with external location](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/sparse-packages-with-external-location)
- [Windows App SDK: Widget providers](https://learn.microsoft.com/en-us/windows/apps/develop/widgets/widget-providers)
- [WindowsAppSDK-Samples: Widget provider sample](https://github.com/microsoft/WindowsAppSDK-Samples)
- `plan/widget-overview.md` — full implementation overview
- `plan/widget-phase-2-stories.md` — WS-18 (Identity Package) implementation details

## Related ADRs

- **ADR-002**: Self-Contained Deployment Model — sparse MSIX coexists with self-contained EXE approach
- **ADR-005**: Pivot from Windows Service to TrayApp — TrayApp is the entry point for sparse MSIX registration (`WidgetPackageRegistrar`)
