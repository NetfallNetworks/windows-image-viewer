using WallpaperApp.Models;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for persisting application runtime state to disk.
    /// State is stored in %LOCALAPPDATA%\WallpaperSync\state.json
    /// </summary>
    public interface IAppStateService
    {
        /// <summary>
        /// Loads the application state from disk.
        /// Returns default state if file doesn't exist or is corrupt.
        /// </summary>
        /// <returns>The current application state.</returns>
        AppState LoadState();

        /// <summary>
        /// Saves the application state to disk.
        /// </summary>
        /// <param name="state">The state to save.</param>
        void SaveState(AppState state);

        /// <summary>
        /// Updates the last-known-good image path and last update time.
        /// </summary>
        /// <param name="imagePath">Path to the successfully set wallpaper.</param>
        void UpdateLastKnownGood(string imagePath);

        /// <summary>
        /// Updates the enabled/disabled state.
        /// </summary>
        /// <param name="enabled">Whether wallpaper updates are enabled.</param>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Marks the first run as complete.
        /// Used after welcome wizard in Phase 3.
        /// </summary>
        void MarkFirstRunComplete();

        /// <summary>
        /// Increments the success count and updates last update time.
        /// </summary>
        void IncrementSuccessCount();

        /// <summary>
        /// Increments the failure count.
        /// </summary>
        void IncrementFailureCount();
    }
}
