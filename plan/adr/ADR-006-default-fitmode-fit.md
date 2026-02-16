# ADR-006: Default FitMode Should Be "Fit" Not "Fill"

**Status**: Accepted
**Date**: 2026-02-16
**Deciders**: Project team

## Context

When we implemented WS-3 (User-Configurable Wallpaper Settings), we added support for multiple FitMode options: Fill, Fit, Stretch, Tile, and Center. At that time, we set the default to `Fill` based on the assumption that it would provide the best visual experience by maximizing screen coverage.

However, after using the application and observing real-world behavior, we've identified issues with the `Fill` default:

### Problems with Fill as Default

1. **Image Cropping**: Fill mode crops portions of the image to maintain aspect ratio while filling the screen. This can cut off important parts of weather images, particularly:
   - Temperature/weather text overlays
   - Location names
   - Time stamps
   - Forecast information

2. **Loss of Context**: Weather wallpapers often contain critical information around the edges. Fill mode sacrifices this information for aesthetics, which defeats the purpose of a weather-focused wallpaper app.

3. **Unpredictable Results**: Different screen aspect ratios (16:9, 16:10, 21:9 ultrawide) crop different amounts, leading to inconsistent experiences. What looks good on one monitor may be badly cropped on another.

4. **User Surprise**: Users expect to see the *entire* weather image, not a cropped version. The current default violates the principle of least surprise.

### Why Fit is Better

1. **Shows Complete Image**: Fit mode guarantees the entire image is visible, ensuring all weather information is readable.

2. **Predictable Behavior**: Users always see what they expect - the full image, centered, with letterboxing/pillarboxing as needed.

3. **Preserves Information**: Weather data, forecasts, and overlays remain intact and readable.

4. **User Choice**: Users who prefer Fill for aesthetics can easily change the setting. But starting with Fit ensures information isn't lost by default.

5. **Matches User Expectations**: When users download a weather wallpaper, they expect to see the weather, not a cropped artistic interpretation.

### Alternatives Considered

1. **Keep Fill as Default** ❌
   - Pros: Maximizes visual appeal, no letterboxing
   - Cons: Loses information, unpredictable cropping, violates user expectations

2. **Use Fit as Default** ✅
   - Pros: Shows entire image, preserves information, predictable behavior
   - Cons: Letterboxing on non-matching aspect ratios (acceptable trade-off)

3. **Dynamic Default Based on Aspect Ratio** ❌
   - Pros: Could optimize per-screen
   - Cons: Complex, unpredictable, over-engineered for a simple preference

## Decision

**Change the default FitMode from `Fill` to `Fit`.**

### Implementation

Update `AppSettings.cs`:
```csharp
public WallpaperFitMode FitMode { get; set; } = WallpaperFitMode.Fit;
```

This change affects:
- New installations (fresh config files)
- Explicit documentation updates
- Test expectations

### Existing Users

Users who already have `WallpaperApp.json` configuration files will **not** be affected - their existing `FitMode` setting will be preserved. This change only affects new installations and users who haven't explicitly set a FitMode.

## Consequences

### Positive

1. **Information Preservation**: Weather data, text overlays, and forecasts remain fully visible
2. **Predictable Experience**: All users see the complete image regardless of screen aspect ratio
3. **Better UX**: Meets user expectations for a weather information app
4. **Easy to Change**: Users who prefer Fill can update their config in 2 seconds

### Negative

1. **Letterboxing**: Non-matching aspect ratios will show black bars (top/bottom or left/right)
   - **Mitigation**: This is expected behavior for Fit mode and ensures no information loss
2. **Aesthetic Preference**: Some users may prefer Fill's edge-to-edge appearance
   - **Mitigation**: Fill is still available and easy to configure

### Trade-offs Accepted

| Aspect | Fill (Old Default) | Fit (New Default) |
|--------|-------------------|-------------------|
| Information preserved | No (crops) | Yes ✅ |
| Visual coverage | 100% | Variable (letterboxed) |
| Predictability | Low | High ✅ |
| User expectations | Violates | Meets ✅ |
| Works with text overlays | Poorly | Perfectly ✅ |
| Aesthetic appeal | Higher | Lower (acceptable) |

**Verdict**: For a weather information application, showing the complete image with all data intact is more important than maximizing visual coverage. Fit mode should be the default.

## Validation

### Expected Behavior Changes

**Before (Fill default):**
- User installs app
- Weather wallpaper is cropped to fill screen
- Some weather information may be cut off
- User must change config to see full image

**After (Fit default):**
- User installs app
- Full weather wallpaper is displayed with letterboxing if needed
- All weather information is visible and readable
- User can change config to Fill if they prefer coverage over information

### Testing

- Verify default value in `AppSettings.cs` is `WallpaperFitMode.Fit`
- Update tests that check default FitMode
- Manual testing on various screen aspect ratios
- Verify existing config files aren't affected

### Documentation Updates

Update the following documentation:
- README.md - Default FitMode in configuration section
- CLAUDE.md - Note about FitMode default
- Any inline code comments referencing the default

## When to Reconsider

This decision should be reconsidered if:

1. **Use Case Changes**: App pivots from weather information to pure aesthetic wallpapers
2. **User Feedback**: Overwhelming preference for Fill as default (unlikely for weather app)
3. **Smart Cropping**: Future AI/ML capability to intelligently crop while preserving information zones

## References

- [WallpaperFitMode.cs](../../src/WallpaperApp/Models/WallpaperFitMode.cs) - Enum definition
- [AppSettings.cs](../../src/WallpaperApp/Configuration/AppSettings.cs) - Configuration model
- [ADR-004](./ADR-004-appsettings-json-configuration.md) - Configuration approach

## Related ADRs

- **ADR-001**: Use SystemParametersInfo for Wallpaper Changes (unchanged)
- **ADR-004**: Use appsettings.json for Configuration (unchanged)
- **ADR-005**: Pivot Service to Tray App (unchanged)

---

**Migration Note**: This is a default value change only. Existing users retain their configured FitMode. New installations will start with Fit instead of Fill.
