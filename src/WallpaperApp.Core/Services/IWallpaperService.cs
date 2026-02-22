using WallpaperApp.Models;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Interface for setting desktop wallpaper.
    /// </summary>
    public interface IWallpaperService
    {
        /// <summary>
        /// Sets the desktop wallpaper to the specified image file with default fit mode (Fill).
        /// </summary>
        /// <param name="imagePath">Path to the image file (absolute or relative).</param>
        /// <exception cref="FileNotFoundException">Thrown when the image file does not exist.</exception>
        /// <exception cref="InvalidImageException">Thrown when the file is not a valid image format (PNG, JPG, BMP).</exception>
        /// <exception cref="WallpaperException">Thrown when the wallpaper cannot be set due to a Windows API error.</exception>
        void SetWallpaper(string imagePath);

        /// <summary>
        /// Sets the desktop wallpaper to the specified image file with a specified fit mode.
        /// </summary>
        /// <param name="imagePath">Path to the image file (absolute or relative).</param>
        /// <param name="fitMode">How the image should be displayed (fill, fit, stretch, tile, center).</param>
        /// <exception cref="FileNotFoundException">Thrown when the image file does not exist.</exception>
        /// <exception cref="InvalidImageException">Thrown when the file is not a valid image format (PNG, JPG, BMP).</exception>
        /// <exception cref="WallpaperException">Thrown when the wallpaper cannot be set due to a Windows API error.</exception>
        void SetWallpaper(string imagePath, WallpaperFitMode fitMode);
    }
}
