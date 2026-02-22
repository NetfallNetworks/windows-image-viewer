namespace WallpaperApp.Models
{
    /// <summary>
    /// Source type for wallpaper images.
    /// Determines whether to download from URL or use a local file.
    /// </summary>
    public enum ImageSource
    {
        /// <summary>
        /// Download image from a URL (original behavior).
        /// Requires ImageUrl to be set.
        /// </summary>
        Url = 0,

        /// <summary>
        /// Use a local image file.
        /// Requires LocalImagePath to be set.
        /// </summary>
        LocalFile = 1
    }
}
