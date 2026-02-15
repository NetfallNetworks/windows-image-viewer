using Microsoft.Extensions.Hosting;
using WallpaperApp.Configuration;
using WallpaperApp.Services;

namespace WallpaperApp
{
    /// <summary>
    /// Background service worker that executes wallpaper updates on a timer.
    /// Runs as a Windows Service or console application.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IWallpaperUpdater _wallpaperUpdater;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private Timer? _timer;
        private DateTime _nextRefreshTime;
        private readonly object _lock = new object();

        public Worker(
            IConfigurationService configurationService,
            IWallpaperUpdater wallpaperUpdater,
            IHostApplicationLifetime applicationLifetime)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _wallpaperUpdater = wallpaperUpdater ?? throw new ArgumentNullException(nameof(wallpaperUpdater));
            _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        }

        /// <summary>
        /// Executes the background service.
        /// Starts the timer and keeps running until cancellation is requested.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                FileLogger.Log("=== Service Starting ===");
                FileLogger.Log($"Log file: {FileLogger.GetLogPath()}");

                // Load configuration to get refresh interval
                var settings = _configurationService.LoadConfiguration();
                var intervalMilliseconds = settings.RefreshIntervalMinutes * 60 * 1000;

                FileLogger.Log("Weather Wallpaper Service starting...");
                FileLogger.Log($"Refresh interval: {settings.RefreshIntervalMinutes} minutes");

                // Execute immediately on startup
                FileLogger.Log("Executing first wallpaper update...");
                await ExecuteUpdateAsync();

                // Calculate next refresh time
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                FileLogger.Log($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");

                // Create timer for subsequent executions
                // Note: Timer callbacks MUST be synchronous (no async void!)
                // We fire-and-forget the async work with proper exception handling
                _timer = new Timer(
                    callback: _ => _ = TimerCallbackAsync(), // Fire and forget (safe because TimerCallbackAsync handles all exceptions)
                    state: null,
                    dueTime: intervalMilliseconds,
                    period: intervalMilliseconds
                );

                // Wait for cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Graceful shutdown requested
                FileLogger.Log("Shutdown requested (TaskCanceledException)");
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown requested
                FileLogger.Log("Shutdown requested (OperationCanceledException)");
            }
            catch (Exception ex)
            {
                FileLogger.LogError("FATAL ERROR in ExecuteAsync", ex);
                throw;
            }
        }

        /// <summary>
        /// Stops the service and cleans up resources.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            FileLogger.Log("Weather Wallpaper Service stopping...");

            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }

            await base.StopAsync(cancellationToken);
            FileLogger.Log("Weather Wallpaper Service stopped.");
        }

        /// <summary>
        /// Timer callback that executes wallpaper updates and catches exceptions.
        /// </summary>
        private async Task TimerCallbackAsync()
        {
            try
            {
                FileLogger.Log("Timer triggered - updating wallpaper...");
                await ExecuteUpdateAsync();

                // Calculate next refresh time
                var settings = _configurationService.LoadConfiguration();
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                FileLogger.Log($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                // Catch all exceptions to prevent timer from stopping
                FileLogger.LogError("Error in timer callback - service will continue running", ex);
            }
        }

        /// <summary>
        /// Executes the wallpaper update workflow.
        /// </summary>
        private async Task ExecuteUpdateAsync()
        {
            try
            {
                await _wallpaperUpdater.UpdateWallpaperAsync();
                FileLogger.Log("Wallpaper update completed successfully");
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Wallpaper update failed", ex);
            }
        }
    }
}
