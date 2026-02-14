# ADR-003: No Retry Logic for Transient Failures

**Status**: Accepted
**Date**: 2026-02-14
**Deciders**: Project team (from initial requirements)

## Context

When downloading weather images from `weather.zamflam.com`, transient failures can occur:
- Network interruptions
- DNS resolution failures
- HTTP 500/502/503 server errors
- Timeout (server slow to respond)
- Temporary API unavailability

Traditional approaches to handling transient failures:
1. **Immediate retry** - Retry 3-5 times with exponential backoff
2. **Circuit breaker** - Stop trying after N failures, resume after cooldown
3. **Retry on next cycle** - Let the timer retry naturally (15 min later)
4. **No retry** - Log error and continue; user sees stale wallpaper

Our application characteristics:
- Runs every 15 minutes (96 times per day)
- Weather data doesn't change rapidly (hourly updates typical)
- Users won't notice a single missed update
- Adding retry logic increases complexity and failure modes

Project philosophy: **Ship fast, zero maintenance**

## Decision

**Do NOT implement retry logic for download or wallpaper-setting failures.**

Instead:
1. **Attempt operation once**
2. **Log failure** with error details (URL, status code, exception)
3. **Continue to next cycle** (timer keeps running)
4. **Wallpaper remains unchanged** (shows last successful image)

Example flow:
```
15:00 - Attempt download → Success → Wallpaper updated
15:15 - Attempt download → Failure (timeout) → Log error, skip wallpaper update
15:30 - Attempt download → Success → Wallpaper updated
```

## Consequences

### Positive

- **Simpler code**: No retry loops, no backoff timers, no circuit breaker state
- **Fewer failure modes**: Retry logic itself can fail (infinite loops, resource exhaustion)
- **Faster recovery**: Next cycle (15 min) is often faster than multiple immediate retries
- **Graceful degradation**: User sees last successful image, which is acceptable for weather data
- **Less API load**: No hammering the server with rapid retries during outages
- **Easier debugging**: Logs show clear timeline (one entry per cycle) without retry noise
- **No cascading failures**: Stuck retries won't block the timer from running next cycle

### Negative

- **Missed updates during brief outages**: 30-second network blip = no update for 15 minutes
  - Mitigation: Weather updates are typically hourly; 15-minute delay is acceptable
- **No immediate user feedback**: User doesn't know download failed unless checking logs
  - Mitigation: Logging provides observability; notifications are future story (if needed)
- **Stale data tolerance**: Must be acceptable for wallpaper to be 15-30 min out of date
  - Mitigation: Weather forecasts don't change minute-to-minute

### Trade-offs Considered

| Strategy | Complexity | API Load | User Experience | Failure Modes | Verdict |
|----------|------------|----------|-----------------|---------------|---------|
| No retry (chosen) | Low | Minimal | Acceptable | Few | ✅ Best fit |
| Immediate retry (3x) | Medium | High during outages | Slightly better | Moderate | ❌ Over-engineering |
| Circuit breaker | High | Moderate | Good during extended outages | Many | ❌ Unnecessary complexity |
| Exponential backoff | Medium | Moderate | Good | Moderate | ❌ Adds little value |

**Verdict**: Simplicity and reliability outweigh marginal UX improvements from retry logic.

## Implementation Guidance

For **Story 4** (ImageFetcher):
```csharp
public async Task<string?> DownloadImageAsync(string url)
{
    try
    {
        // Single attempt, 30-second timeout
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
        response.EnsureSuccessStatusCode();

        // Save to file...
        return filePath;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Download failed for {Url}", url);
        return null;  // Let timer retry on next cycle
    }
}
```

For **Story 5** (WallpaperUpdater orchestration):
```csharp
var imagePath = await _imageFetcher.DownloadImageAsync(url);
if (imagePath == null)
{
    _logger.LogInformation("Skipping wallpaper update due to download failure");
    return;  // Continue to next cycle
}
```

For **testing**:
- Verify that HTTP errors return `null` (not throw)
- Verify that timer continues after failures
- Manual test: Disconnect network → observe single log entry → reconnect → next cycle succeeds

## When to Reconsider

This decision should be **reconsidered** if:
1. Users report frequent missed updates (>5% failure rate)
2. The image source (weather.zamflam.com) becomes unreliable
3. Real-time updates become a requirement (e.g., severe weather alerts)
4. The refresh interval increases to >30 minutes (longer wait = retry may be worth it)

If retry becomes necessary:
- Start with **simple exponential backoff** (retry after 1, 2, 4 minutes)
- Limit to 2-3 retries max
- Log each retry attempt for observability
- Consider creating **ADR-011** to supersede this decision

## References

- [STORY_MAP.md](../STORY_MAP.md) - Error Handling strategy
- [Story 4 Acceptance Criteria](../STORY_MAP.md#L336-L339) - "Returns `null` on HTTP error (no retries)"
