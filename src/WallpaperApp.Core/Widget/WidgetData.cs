using WallpaperApp.Configuration;
using WallpaperApp.Models;

namespace WallpaperApp.Widget
{
    /// <summary>
    /// Data object bound to Adaptive Card templates for widget display.
    /// All string properties are safe for JSON template substitution.
    /// </summary>
    public record WidgetData(
        string ImageUrl,
        string LastUpdated,
        string Status,
        bool HasImage
    )
    {
        /// <summary>
        /// Placeholder URL used when the source is a local file (Adaptive Cards require HTTPS URLs).
        /// </summary>
        public const string LocalFilePlaceholderUrl = "https://via.placeholder.com/400x225?text=Local+File+Mode";

        /// <summary>
        /// Builds a <see cref="WidgetData"/> from the current application settings and state.
        /// Extracted from the widget provider so this logic is testable on all platforms.
        /// </summary>
        public static WidgetData From(AppSettings settings, AppState state)
        {
            var imageUrl = settings.SourceType == ImageSource.LocalFile
                ? LocalFilePlaceholderUrl
                : settings.ImageUrl;

            var lastUpdated = state.LastUpdateTime.HasValue
                ? state.LastUpdateTime.Value.ToString("ddd HH:mm")
                : "Never";

            var status = state.IsEnabled ? "Active" : "Paused";

            var hasImage = !string.IsNullOrEmpty(settings.ImageUrl)
                || settings.SourceType == ImageSource.LocalFile;

            return new WidgetData(imageUrl, lastUpdated, status, hasImage);
        }
    };
}
