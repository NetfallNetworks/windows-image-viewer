namespace WallpaperApp.Models
{
    /// <summary>
    /// Application runtime state persisted to disk.
    /// Separate from user configuration (AppSettings) to distinguish
    /// runtime state from user preferences.
    /// </summary>
    public class AppState
    {
        /// <summary>
        /// Whether wallpaper updates are enabled.
        /// Used for enable/disable toggle in Phase 3.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Path to the last successfully set wallpaper image.
        /// Used for fallback when downloads fail (Story WS-5).
        /// </summary>
        public string? LastKnownGoodImagePath { get; set; }

        /// <summary>
        /// Whether this is the first run of the application.
        /// Used for welcome wizard in Phase 3.
        /// </summary>
        public bool IsFirstRun { get; set; } = true;

        /// <summary>
        /// Timestamp of the last successful wallpaper update.
        /// </summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Count of successful wallpaper updates.
        /// Used for usage statistics and reliability tracking.
        /// </summary>
        public int UpdateSuccessCount { get; set; } = 0;

        /// <summary>
        /// Count of failed wallpaper updates.
        /// Used for reliability tracking and diagnostics.
        /// </summary>
        public int UpdateFailureCount { get; set; } = 0;
    }
}
