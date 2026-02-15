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
    }
}
