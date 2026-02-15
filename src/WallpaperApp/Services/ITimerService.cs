namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for scheduling periodic execution of wallpaper updates.
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// Starts the timer to execute wallpaper updates at the configured interval.
        /// First execution happens immediately, then repeats at the interval.
        /// </summary>
        /// <param name="cancellationToken">Token to signal shutdown.</param>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the timer and cancels any pending executions.
        /// </summary>
        Task StopAsync();
    }
}
