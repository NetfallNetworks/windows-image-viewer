namespace WallpaperApp.Services
{
    /// <summary>
    /// Exception thrown when the wallpaper cannot be set due to a Windows API error.
    /// </summary>
    public class WallpaperException : Exception
    {
        public WallpaperException(string message) : base(message)
        {
        }

        public WallpaperException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
