using Microsoft.Windows.Widgets.Providers;
using WallpaperApp.Configuration;
using SdkWidgetSize = Microsoft.Windows.Widgets.WidgetSize;
using AppWidgetSize = WallpaperApp.Widget.WidgetSize;
using WallpaperApp.Models;
using WallpaperApp.Services;
using WallpaperApp.Widget;

// COM CLSID for this widget provider. Must match the MSIX manifest in Phase 2.
// Generate a new GUID if forking this class: Tools → Create GUID in Visual Studio.
[assembly: System.Runtime.InteropServices.Guid("6B2A4C8E-3F1D-4A9B-8E2C-5D7F1A3B6E4C")]

namespace WallpaperApp.WidgetProvider
{
    /// <summary>
    /// Windows 11 Widget Board provider that displays the current Wallpaper Sync image
    /// as a glanceable widget in the Win+W flyout. Implements <see cref="IWidgetProvider"/>
    /// from the Windows App SDK.
    /// </summary>
    public sealed class WallpaperImageWidgetProvider : IWidgetProvider
    {
        private readonly IConfigurationService _configService;
        private readonly IAppStateService _stateService;
        private readonly IWallpaperUpdater _updater;
        private readonly CardTemplateService _cardService = new();
        private readonly WidgetInstanceTracker _tracker = new();

        // Fallback polling interval when no IPC signal is received.
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

        private readonly CancellationTokenSource _cts = new();

        public WallpaperImageWidgetProvider(
            IConfigurationService configService,
            IAppStateService stateService,
            IWallpaperUpdater updater)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));

            // Start the fallback polling loop. The IPC listener (WidgetIpcService)
            // is started externally by Program.cs and calls NotifyWidgetRefresh().
            Task.Run(RunPollingLoopAsync);
        }

        // ── IWidgetProvider implementation ─────────────────────────────────────

        /// <summary>
        /// Called by Widget Board when the user pins the widget for the first time
        /// or when the widget is activated after being in the background.
        /// </summary>
        public void CreateWidget(WidgetContext widgetContext)
        {
            _tracker.AddOrUpdate(widgetContext.Id, ToWidgetSize(widgetContext.Size));
            SendUpdate(widgetContext.Id, ToWidgetSize(widgetContext.Size));
        }

        /// <summary>
        /// Called by Widget Board when the widget comes into view (user opens Win+W).
        /// Re-sends the current card so the widget shows fresh data immediately.
        /// </summary>
        public void Activate(WidgetContext widgetContext)
        {
            var size = ToWidgetSize(widgetContext.Size);
            _tracker.AddOrUpdate(widgetContext.Id, size);
            SendUpdate(widgetContext.Id, size);
        }

        /// <summary>
        /// Called by Widget Board when the widget goes out of view (user closes Win+W).
        /// Stop pushing updates — the widget is no longer visible so updates are wasteful.
        /// </summary>
        public void Deactivate(string widgetId)
        {
            // No state change needed — _tracker keeps the entry so we can resume
            // on the next Activate call. Updates will still fire from the polling
            // loop but WidgetManager silently drops them for inactive widgets.
        }

        /// <summary>
        /// Called by Widget Board when the user removes (unpins) the widget.
        /// </summary>
        public void DeleteWidget(string widgetId, string customStateStr)
        {
            _tracker.Remove(widgetId);
        }

        /// <summary>
        /// Called when the user resizes the widget or the Widget Board layout changes.
        /// Re-renders the appropriate card template for the new size.
        /// </summary>
        public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
        {
            var context = contextChangedArgs.WidgetContext;
            var size = ToWidgetSize(context.Size);
            _tracker.AddOrUpdate(context.Id, size);
            SendUpdate(context.Id, size);
        }

        /// <summary>
        /// Called when the user taps an action button on the widget card.
        /// The "Refresh Now" button sends verb "refresh".
        /// </summary>
        public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
        {
            if (actionInvokedArgs.Verb == "refresh")
            {
                // Fire-and-forget: update wallpaper then push refreshed card to all instances
                _ = HandleRefreshActionAsync();
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private async Task HandleRefreshActionAsync()
        {
            await _updater.UpdateWallpaperAsync();
            PushUpdateToAllWidgets();
        }

        /// <summary>
        /// Pushes the current card data to all active widget instances.
        /// Called by the IPC service when the TrayApp signals a wallpaper update,
        /// and by the internal polling loop as a fallback.
        /// </summary>
        public void PushUpdateToAllWidgets()
        {
            foreach (var (widgetId, size) in _tracker.GetAll())
            {
                SendUpdate(widgetId, size);
            }
        }

        private void SendUpdate(string widgetId, AppWidgetSize size)
        {
            try
            {
                var data = BuildWidgetData();
                var template = _cardService.LoadTemplate(size);
                var cardJson = _cardService.HydrateTemplate(template, data);

                var customState = data.ImageUrl;  // Store URL hash for staleness detection in Phase 2

                var updateOptions = new WidgetUpdateRequestOptions(widgetId)
                {
                    Template = cardJson,
                    Data = "{}",  // Adaptive Cards data bag (unused — data is in the template)
                    CustomState = customState
                };

                WidgetManager.GetDefault().UpdateWidget(updateOptions);
            }
            catch (Exception ex)
            {
                // Log and continue — a failed update is non-fatal; the card will
                // refresh on the next polling cycle or IPC signal.
                Console.Error.WriteLine($"[WidgetProvider] SendUpdate failed for {widgetId}: {ex.Message}");
            }
        }

        private WidgetData BuildWidgetData()
        {
            AppSettings settings;
            AppState state;

            try { settings = _configService.LoadConfiguration(); }
            catch { settings = new AppSettings(); }

            try { state = _stateService.LoadState(); }
            catch { state = new AppState(); }

            return WidgetData.From(settings, state);
        }

        private async Task RunPollingLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(PollingInterval, _cts.Token).ConfigureAwait(false);
                    PushUpdateToAllWidgets();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[WidgetProvider] Polling error: {ex.Message}");
                }
            }
        }

        // Translates Windows App SDK WidgetSize to our platform-neutral enum.
        private static AppWidgetSize ToWidgetSize(SdkWidgetSize sdkSize) =>
            sdkSize switch
            {
                SdkWidgetSize.Small  => AppWidgetSize.Small,
                SdkWidgetSize.Medium => AppWidgetSize.Medium,
                SdkWidgetSize.Large  => AppWidgetSize.Large,
                _ => AppWidgetSize.Medium
            };
    }
}
