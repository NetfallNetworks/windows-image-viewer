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
                // Load configuration to get refresh interval
                var settings = _configurationService.LoadConfiguration();
                var intervalMilliseconds = settings.RefreshIntervalMinutes * 60 * 1000;

                Console.WriteLine("Weather Wallpaper Service starting...");
                Console.WriteLine($"Refresh interval: {settings.RefreshIntervalMinutes} minutes");
                Console.WriteLine();

                // Execute immediately on startup
                Console.WriteLine("Executing first wallpaper update...");
                await ExecuteUpdateAsync();

                // Calculate next refresh time
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                Console.WriteLine($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();

                // Create timer for subsequent executions
                _timer = new Timer(
                    callback: async _ => await TimerCallbackAsync(),
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
                Console.WriteLine("Shutdown requested...");
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown requested
                Console.WriteLine("Shutdown requested...");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error in service: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Stops the service and cleans up resources.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Weather Wallpaper Service stopping...");

            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }

            await base.StopAsync(cancellationToken);
            Console.WriteLine("Weather Wallpaper Service stopped.");
        }

        /// <summary>
        /// Timer callback that executes wallpaper updates and catches exceptions.
        /// </summary>
        private async Task TimerCallbackAsync()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Timer triggered - updating wallpaper...");
                await ExecuteUpdateAsync();

                // Calculate next refresh time
                var settings = _configurationService.LoadConfiguration();
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                Console.WriteLine($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                // Catch all exceptions to prevent timer from stopping
                Console.Error.WriteLine($"❌ Error in timer callback: {ex.Message}");
                Console.Error.WriteLine("  Service will continue running.");
                Console.WriteLine();
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
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Update failed: {ex.Message}");
                Console.WriteLine();
            }
        }
    }
}
