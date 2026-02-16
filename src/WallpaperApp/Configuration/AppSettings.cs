using WallpaperApp.Models;

namespace WallpaperApp.Configuration
{
    /// <summary>
    /// Application configuration settings loaded from appsettings.json.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// URL of the image to use as wallpaper. Must be HTTPS.
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Interval in minutes between wallpaper refresh cycles.
        /// </summary>
        public int RefreshIntervalMinutes { get; set; } = 15;

        // === New Properties (Story WS-3) ===

        /// <summary>
        /// How the wallpaper image should be displayed (fill, fit, stretch, tile, center).
        /// Default: Fill (maintains aspect ratio, crops edges if needed).
        /// </summary>
        public WallpaperFitMode FitMode { get; set; } = WallpaperFitMode.Fill;

        /// <summary>
        /// Whether to show desktop notifications for wallpaper updates.
        /// Default: false (silent operation).
        /// </summary>
        public bool EnableNotifications { get; set; } = false;

        /// <summary>
        /// Path to local image file to use as wallpaper.
        /// Required when SourceType = LocalFile.
        /// </summary>
        public string? LocalImagePath { get; set; }

        /// <summary>
        /// Source of wallpaper image (URL or local file).
        /// Default: Url (original behavior).
        /// </summary>
        public ImageSource SourceType { get; set; } = ImageSource.Url;
    }
}
