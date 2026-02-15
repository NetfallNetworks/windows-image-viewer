using WallpaperApp.Configuration;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for scheduling periodic execution of wallpaper updates.
    /// </summary>
    public class TimerService : ITimerService
    {
        private readonly IConfigurationService _configurationService;
        private readonly WallpaperUpdater _wallpaperUpdater;
        private Timer? _timer;
        private DateTime _nextRefreshTime;
        private readonly object _lock = new object();
        private bool _isRunning;

        public TimerService(
            IConfigurationService configurationService,
            WallpaperUpdater wallpaperUpdater)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _wallpaperUpdater = wallpaperUpdater ?? throw new ArgumentNullException(nameof(wallpaperUpdater));
        }

        /// <summary>
        /// Starts the timer to execute wallpaper updates at the configured interval.
        /// First execution happens immediately, then repeats at the interval.
        /// </summary>
        /// <param name="cancellationToken">Token to signal shutdown.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    throw new InvalidOperationException("Timer is already running.");
                }
                _isRunning = true;
            }

            try
            {
                // Load configuration to get refresh interval
                var settings = _configurationService.LoadConfiguration();
                var intervalMilliseconds = settings.RefreshIntervalMinutes * 60 * 1000;

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
                await Task.Delay(Timeout.Infinite, cancellationToken);
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
            finally
            {
                await StopAsync();
            }
        }

        /// <summary>
        /// Stops the timer and cancels any pending executions.
        /// </summary>
        public Task StopAsync()
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    return Task.CompletedTask;
                }

                _timer?.Dispose();
                _timer = null;
                _isRunning = false;
            }

            Console.WriteLine("Timer stopped.");
            return Task.CompletedTask;
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
                Console.Error.WriteLine("  Timer will continue running.");
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
