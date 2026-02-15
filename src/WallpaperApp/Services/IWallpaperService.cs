namespace WallpaperApp.Services
{
    /// <summary>
    /// Interface for setting desktop wallpaper.
    /// </summary>
    public interface IWallpaperService
    {
        /// <summary>
        /// Sets the desktop wallpaper to the specified image file.
        /// </summary>
        /// <param name="imagePath">Path to the image file (absolute or relative).</param>
        /// <exception cref="FileNotFoundException">Thrown when the image file does not exist.</exception>
        /// <exception cref="InvalidImageException">Thrown when the file is not a valid image format (PNG, JPG, BMP).</exception>
        /// <exception cref="WallpaperException">Thrown when the wallpaper cannot be set due to a Windows API error.</exception>
        void SetWallpaper(string imagePath);
    }
}
