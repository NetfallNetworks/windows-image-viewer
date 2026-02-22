# Widget Board Integration - Phase 1: Widget Provider Core

**Epic**: Widget Board Integration
**Priority**: CRITICAL
**Total Story Points**: 13
**Estimated Duration**: 3-4 days

---

## Phase Overview

Phase 1 establishes the `WallpaperApp.WidgetProvider` project — an out-of-process COM server that implements `IWidgetProvider` from the Windows App SDK. At the end of this phase, the executable builds, runs as a COM-activatable server, reads live configuration and state from Core services, and serves Adaptive Card JSON to the Windows 11 Widget Board.

No packaging or installer changes in this phase. Developer testing is done by manually registering the COM server.

**Key Objectives**:
1. Create `WallpaperApp.WidgetProvider` project and wire into solution
2. Bootstrap a Windows App SDK COM server with stub `IWidgetProvider`
3. Design Adaptive Card templates for small, medium, and large widget sizes
4. Bind live data from `IConfigurationService` and `IAppStateService` to card templates
5. Handle widget lifecycle callbacks (create/delete/activate/deactivate)
6. Implement "Refresh Now" custom action

---

## Story WS-15: Widget Provider Project Setup

**Story Points**: 5
**Type**: Feature / Infrastructure
**Priority**: CRITICAL

### Context

There is no `WallpaperApp.WidgetProvider` project today. The Widget Board requires an out-of-process COM server that implements `IWidgetProvider`. The Windows App SDK provides this interface via the `Microsoft.WindowsAppSDK` NuGet package. This story creates the skeleton project, gets it building, and adds it to the solution file so it participates in the full build.

### Description

Create a new .NET 8 console application `WallpaperApp.WidgetProvider` that bootstraps as a Windows App SDK COM server. The project should reference `WallpaperApp.Core` for shared services and register a stub `IWidgetProvider` implementation. At the end of this story, running `WallpaperApp.WidgetProvider.exe` should start, register the COM server, and wait for Widget Board activation (no actual widget rendering yet).

### Tasks

- [ ] Create `src/WallpaperApp.WidgetProvider/WallpaperApp.WidgetProvider.csproj`
  - Target: `net8.0-windows10.0.19041.0` (minimum Win10 2004 for App SDK)
  - Add `Microsoft.WindowsAppSDK` NuGet package reference
  - Add project reference to `WallpaperApp.Core`
- [ ] Implement `Program.cs` COM server bootstrap:
  - Call `WindowsAppRuntime.EnsureIsInstalled()` at startup
  - Register the widget provider COM class with `CoRegisterClassObject`
  - Block on a `ManualResetEvent` until the process is signaled to exit
- [ ] Create `WallpaperImageWidgetProvider.cs` implementing `IWidgetProvider` (stub):
  - `CreateWidget(WidgetContext)` — log and return
  - `DeleteWidget(string widgetId, string)` — log and return
  - `OnActionInvoked(WidgetActionInvokedArgs)` — log and return
  - `OnWidgetContextChanged(WidgetContextChangedArgs)` — log and return
- [ ] Add project to `WallpaperApp.sln`
- [ ] Add project to `.gitignore` bin/obj exclusions (already covered by wildcard)
- [ ] Verify: `dotnet build WallpaperApp.sln -c Release` succeeds

### Files to Create

```
src/WallpaperApp.WidgetProvider/WallpaperApp.WidgetProvider.csproj
src/WallpaperApp.WidgetProvider/Program.cs
src/WallpaperApp.WidgetProvider/WallpaperImageWidgetProvider.cs
```

### Files to Modify

```
WallpaperApp.sln  (add new project entry)
```

### Implementation Notes

**COM server bootstrap pattern** (Program.cs):
```csharp
// 1. Ensure Windows App Runtime is installed
WindowsAppRuntime.EnsureIsInstalled();

// 2. Create the widget provider instance
var provider = new WallpaperImageWidgetProvider(configService, stateService, updater);

// 3. Register as COM class object
// WidgetManager discovers providers via COM activation
using var server = new WidgetProviderServer<WallpaperImageWidgetProvider>();
server.Run(provider);
// server.Run() blocks until Widget Board releases the COM server
```

**Widget provider class registration** (WallpaperApp.WidgetProvider.csproj):
```xml
<ItemGroup>
  <ComServer Include="WallpaperApp.WidgetProvider.WallpaperImageWidgetProvider">
    <Guid><!-- generate a new GUID --></Guid>
  </ComServer>
</ItemGroup>
```

The exact COM registration approach depends on the Windows App SDK version. Consult the [Windows App SDK widget samples](https://github.com/microsoft/WindowsAppSDK-Samples) for the correct bootstrap pattern at time of implementation.

### Acceptance Criteria

- [ ] `WallpaperApp.WidgetProvider.csproj` exists and references Core + Windows App SDK
- [ ] Full solution build succeeds: `dotnet build WallpaperApp.sln -c Release --warnaserror`
- [ ] `WallpaperApp.WidgetProvider.exe` starts and registers as a COM server without errors
- [ ] No compiler warnings

### Testing Requirements

No unit tests in this story — the COM bootstrap is infrastructure. Testing is manual:

**Manual Smoke Test**:
1. Build `WallpaperApp.WidgetProvider.exe` in Release
2. Run `WallpaperApp.WidgetProvider.exe`
3. Expected: No errors in output; process stays running
4. Kill process — expected: clean exit

### Definition of Done

- [x] Solution builds clean with zero warnings
- [x] Provider project references Core project
- [x] Stub `IWidgetProvider` compiles with all required interface members
- [x] Manual smoke test passes
- [x] New project committed to solution file

---

## Story WS-16: Adaptive Card Templates

**Story Points**: 3
**Type**: Feature / Design
**Priority**: HIGH

### Context

The Widget Board renders content as Adaptive Card JSON. Each widget size (small, medium, large) requires a separate card template. Card data is injected via a template data binding object at render time — the static JSON template uses `${propertyName}` placeholders that the Widget Board host substitutes with live values. This story designs and validates all three card templates.

### Description

Create Adaptive Card JSON templates for the three standard widget sizes. Each template displays the image (via URL) and relevant metadata (last updated, status). Templates are embedded as resources in the `WallpaperApp.WidgetProvider` assembly. A `CardTemplateService` loads and hydrates templates with live data.

### Tasks

- [ ] Create `src/WallpaperApp.WidgetProvider/Cards/` directory
- [ ] Create `Cards/small-card.json` — image-only, compact layout
- [ ] Create `Cards/medium-card.json` — image + last updated time + status indicator
- [ ] Create `Cards/large-card.json` — image + last updated + "Refresh Now" action button + source URL
- [ ] Embed all three JSON files as resources in the `.csproj` (`EmbeddedResource`)
- [ ] Create `CardTemplateService.cs`:
  - `LoadTemplate(WidgetSize size)` → returns raw JSON string
  - `HydrateTemplate(string template, WidgetData data)` → returns hydrated JSON via `JsonObject` substitution
- [ ] Create `WidgetData.cs` record — the data object bound to card templates:
  - `string ImageUrl`
  - `string LastUpdated` (formatted string, e.g., "Today 14:23")
  - `string Status` (e.g., "Active", "Disabled", "No image")
  - `bool HasImage`
- [ ] Validate templates using [Adaptive Cards Designer](https://adaptivecards.io/designer/) (manual check)

### Files to Create

```
src/WallpaperApp.WidgetProvider/Cards/small-card.json
src/WallpaperApp.WidgetProvider/Cards/medium-card.json
src/WallpaperApp.WidgetProvider/Cards/large-card.json
src/WallpaperApp.WidgetProvider/CardTemplateService.cs
src/WallpaperApp.WidgetProvider/WidgetData.cs
```

### Implementation Notes

**small-card.json** (image only, no text — tight space):
```json
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.5",
  "body": [
    {
      "type": "Image",
      "url": "${imageUrl}",
      "size": "Stretch",
      "altText": "Wallpaper Sync"
    }
  ]
}
```

**medium-card.json** (image + metadata):
```json
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.5",
  "body": [
    {
      "type": "Image",
      "url": "${imageUrl}",
      "size": "Stretch"
    },
    {
      "type": "TextBlock",
      "text": "Updated ${lastUpdated}",
      "size": "Small",
      "isSubtle": true
    }
  ]
}
```

**large-card.json** (image + metadata + Refresh Now action):
```json
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.5",
  "body": [
    {
      "type": "Image",
      "url": "${imageUrl}",
      "size": "Stretch"
    },
    {
      "type": "ColumnSet",
      "columns": [
        {
          "type": "Column",
          "items": [
            {
              "type": "TextBlock",
              "text": "Wallpaper Sync",
              "weight": "Bolder"
            },
            {
              "type": "TextBlock",
              "text": "Updated ${lastUpdated}",
              "size": "Small",
              "isSubtle": true
            }
          ]
        }
      ]
    }
  ],
  "actions": [
    {
      "type": "Action.Execute",
      "title": "Refresh Now",
      "verb": "refresh"
    }
  ]
}
```

**LocalFile fallback**: When `AppSettings.SourceType == LocalFile`, the widget cannot display a `file://` URL (Adaptive Cards require HTTPS). In this case, set `imageUrl` to a bundled placeholder image URL or omit the image and show "Local file mode — image not available in widget."

### Acceptance Criteria

- [ ] Three JSON card templates exist and are valid Adaptive Card schema
- [ ] Templates compile as embedded resources
- [ ] `CardTemplateService.LoadTemplate(WidgetSize.Small)` returns the small template
- [ ] `CardTemplateService.HydrateTemplate(template, data)` substitutes all `${...}` placeholders
- [ ] LocalFile mode produces a valid card (no broken image URL)
- [ ] Templates validated in Adaptive Cards Designer (manual)

### Testing Requirements

```csharp
// CardTemplateServiceTests.cs
[Fact]
public void LoadTemplate_Small_ReturnsNonEmptyJson()

[Fact]
public void LoadTemplate_Medium_ReturnsNonEmptyJson()

[Fact]
public void LoadTemplate_Large_ReturnsNonEmptyJson()

[Fact]
public void HydrateTemplate_SubstitutesImageUrl()

[Fact]
public void HydrateTemplate_SubstitutesLastUpdated()

[Fact]
public void HydrateTemplate_LocalFileMode_ProducesValidCard()
```

### Definition of Done

- [x] Three card templates validated in Adaptive Cards Designer
- [x] Templates embedded as resources
- [x] `CardTemplateService` unit tests pass (6+ tests)
- [x] LocalFile mode handled gracefully
- [x] No compiler warnings

---

## Story WS-17: Widget Data Binding & Lifecycle

**Story Points**: 5
**Type**: Feature
**Priority**: HIGH

### Context

The stub `IWidgetProvider` from WS-15 does nothing. This story wires it to `IConfigurationService` and `IAppStateService` from Core so the widget displays live data. It also handles the full widget lifecycle: create, delete, activate, deactivate, context change, and the "Refresh Now" custom action.

### Description

Complete `WallpaperImageWidgetProvider` to serve real Adaptive Card content. On `CreateWidget`, send the current card JSON and state JSON. On `OnWidgetContextChanged`, re-send the card when widget size changes. On `OnActionInvoked` with verb "refresh", trigger `IWallpaperUpdater.UpdateWallpaperAsync()` and push an updated card.

### Tasks

- [ ] Inject `IConfigurationService`, `IAppStateService`, `IWallpaperUpdater` into `WallpaperImageWidgetProvider` via constructor
- [ ] Implement `CreateWidget(WidgetContext context)`:
  - Add widget instance to internal dictionary (keyed by `context.Id`)
  - Build `WidgetData` from current config + state
  - Call `WidgetManager.GetDefault().UpdateWidget(update)` with card JSON + state JSON
- [ ] Implement `DeleteWidget(string widgetId, string customStateStr)`:
  - Remove widget instance from dictionary
  - Log deletion
- [ ] Implement `OnWidgetContextChanged(WidgetContextChangedArgs args)`:
  - Retrieve new `WidgetContext` (size may have changed)
  - Re-build card for new size
  - Call `UpdateWidget` with refreshed card
- [ ] Implement `OnActionInvoked(WidgetActionInvokedArgs args)`:
  - Check `args.Verb == "refresh"`
  - Call `_updater.UpdateWallpaperAsync(isManualRefresh: true)` asynchronously
  - After completion, push updated card JSON to all active widget instances
- [ ] Implement internal `PushUpdateToAllWidgets()` helper:
  - Enumerate active widget instances
  - Build current `WidgetData`, hydrate template for each widget's current size
  - Call `WidgetManager.GetDefault().UpdateWidget()` for each
- [ ] Wire `PushUpdateToAllWidgets()` into a periodic refresh timer (fallback polling, default 30 s)
- [ ] Named `EventWaitHandle` listener (stub — IPC is fully implemented in WS-19):
  - Create a `EventWaitHandle` with a fixed name (`Global\WallpaperSyncWidgetRefresh`)
  - Start a background thread waiting on the handle
  - When signaled, call `PushUpdateToAllWidgets()`

### Files to Modify

```
src/WallpaperApp.WidgetProvider/WallpaperImageWidgetProvider.cs  (implement fully)
src/WallpaperApp.WidgetProvider/Program.cs  (inject Core services into provider)
```

### Files to Create

```
src/WallpaperApp.WidgetProvider/WidgetInstanceTracker.cs  (dictionary of active widget contexts)
```

### Implementation Notes

**WidgetData construction from Core services**:
```csharp
private WidgetData BuildWidgetData()
{
    var settings = _configService.LoadConfiguration();
    var state = _stateService.LoadState();

    return new WidgetData(
        ImageUrl: settings.SourceType == ImageSource.Url
            ? settings.ImageUrl
            : "https://via.placeholder.com/400x225?text=Local+File+Mode",
        LastUpdated: state.LastUpdateTime.HasValue
            ? state.LastUpdateTime.Value.ToString("ddd HH:mm")
            : "Never",
        Status: state.IsEnabled ? "Active" : "Paused",
        HasImage: !string.IsNullOrEmpty(settings.ImageUrl) || settings.SourceType == ImageSource.LocalFile
    );
}
```

**State JSON**: Widget Board supports persisting a small JSON string across sessions (`customState`). Store the last-known image URL hash so the widget can detect staleness.

**Thread safety**: `_widgetInstances` dictionary must be accessed under a lock. Multiple Widget Board activations can occur on different threads.

### Acceptance Criteria

- [ ] `CreateWidget` sends an Adaptive Card with current image URL and metadata
- [ ] Widget displays correct image when opened in Win+W
- [ ] Size changes (small ↔ medium ↔ large) re-render the correct template
- [ ] "Refresh Now" button triggers wallpaper update and card refresh within 5 seconds
- [ ] Multiple widget instances (same widget pinned twice) all update on refresh
- [ ] `DeleteWidget` removes instance from tracker; no further updates sent
- [ ] Periodic fallback timer updates the card every 30 seconds (configurable)
- [ ] All new unit tests pass

### Testing Requirements

```csharp
// WallpaperImageWidgetProviderTests.cs (using mocked Core services)

[Fact]
public void CreateWidget_SendsCardWithCurrentImageUrl()

[Fact]
public void CreateWidget_LocalFileMode_SendsPlaceholderCard()

[Fact]
public void OnWidgetContextChanged_SizeChange_SendsResizedCard()

[Fact]
public void OnActionInvoked_RefreshVerb_CallsUpdater()

[Fact]
public async Task OnActionInvoked_RefreshVerb_PushesUpdatedCard()

[Fact]
public void DeleteWidget_RemovesInstanceFromTracker()

[Fact]
public void PushUpdateToAllWidgets_UpdatesAllActiveInstances()

// WidgetInstanceTrackerTests.cs
[Fact]
public void AddWidget_ThenGetAll_ReturnsAllInstances()

[Fact]
public void RemoveWidget_ThenGetAll_ExcludesRemovedInstance()

[Fact]
public void IsThreadSafe_ConcurrentAddRemove_DoesNotThrow()
```

### Definition of Done

- [x] All unit tests pass (9+ new tests)
- [x] Manual test: widget displays live image URL from config
- [x] Manual test: "Refresh Now" triggers wallpaper update
- [x] Manual test: changing widget size re-renders correct template
- [x] Thread safety verified under concurrent access
- [x] No compiler warnings

---

## Phase 1 Complete Checklist

When all 3 stories are done:

- [ ] `WallpaperApp.WidgetProvider.exe` builds as part of the full solution
- [ ] Widget provider boots as a COM server and waits for Widget Board activation
- [ ] Widget displays correct image and metadata from Core services
- [ ] "Refresh Now" action works end-to-end
- [ ] All widget size templates render correctly
- [ ] All unit tests pass (15+ new tests across phase)
- [ ] No compiler warnings
- [ ] Ready for Phase 2 (packaging and identity)

---

**END OF PHASE 1 STORIES**
