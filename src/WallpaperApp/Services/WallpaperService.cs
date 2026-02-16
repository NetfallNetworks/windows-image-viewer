using System.Runtime.InteropServices;
using Microsoft.Win32;
using WallpaperApp.Models;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for setting desktop wallpaper using Windows API.
    /// </summary>
    public class WallpaperService : IWallpaperService
    {
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private readonly IImageValidator _imageValidator;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SystemParametersInfo(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni);

        public WallpaperService(IImageValidator imageValidator)
        {
            _imageValidator = imageValidator ?? throw new ArgumentNullException(nameof(imageValidator));
        }

        /// <summary>
        /// Sets the desktop wallpaper to the specified image file with default fit mode (Fit).
        /// </summary>
        /// <param name="imagePath">Path to the image file (absolute or relative).</param>
        /// <exception cref="FileNotFoundException">Thrown when the image file does not exist.</exception>
        /// <exception cref="InvalidImageException">Thrown when the file is not a valid image format (PNG, JPG, BMP).</exception>
        /// <exception cref="WallpaperException">Thrown when the wallpaper cannot be set due to a Windows API error.</exception>
        public void SetWallpaper(string imagePath)
        {
            SetWallpaper(imagePath, WallpaperFitMode.Fit); // Default mode
        }

        /// <summary>
        /// Sets the desktop wallpaper to the specified image file with a specified fit mode.
        /// </summary>
        /// <param name="imagePath">Path to the image file (absolute or relative).</param>
        /// <param name="fitMode">How the image should be displayed (fill, fit, stretch, tile, center).</param>
        /// <exception cref="FileNotFoundException">Thrown when the image file does not exist.</exception>
        /// <exception cref="InvalidImageException">Thrown when the file is not a valid image format (PNG, JPG, BMP).</exception>
        /// <exception cref="WallpaperException">Thrown when the wallpaper cannot be set due to a Windows API error.</exception>
        public void SetWallpaper(string imagePath, WallpaperFitMode fitMode)
        {
            // Convert relative path to absolute path
            string absolutePath = Path.GetFullPath(imagePath);

            // Validate file exists
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Image file not found at: {absolutePath}", absolutePath);
            }

            // Validate using magic bytes instead of extension (Security Fix - Story WS-1)
            if (!_imageValidator.IsValidImage(absolutePath, out var format))
            {
                throw new InvalidImageException(
                    "Invalid image file. Only PNG, JPG, and BMP formats are supported.");
            }

            FileLogger.Log($"Validated {format} image: {absolutePath}");

            // Set wallpaper style in registry (Story WS-4)
            SetWallpaperStyleRegistry(fitMode);

            // Call Windows API to set wallpaper
            int result = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                absolutePath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            if (result == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new WallpaperException(
                    $"Failed to set wallpaper. Windows API error code: {errorCode}");
            }
        }

        /// <summary>
        /// Sets the wallpaper display style in the Windows registry.
        /// Registry keys: HKCU\Control Panel\Desktop - WallpaperStyle and TileWallpaper
        /// </summary>
        /// <param name="mode">The fit mode to apply.</param>
        private void SetWallpaperStyleRegistry(WallpaperFitMode mode)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
                if (key == null)
                {
                    FileLogger.Log("Warning: Failed to open Desktop registry key");
                    return; // Continue without setting style
                }

                switch (mode)
                {
                    case WallpaperFitMode.Fill:
                        key.SetValue("WallpaperStyle", "10");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case WallpaperFitMode.Fit:
                        key.SetValue("WallpaperStyle", "6");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case WallpaperFitMode.Stretch:
                        key.SetValue("WallpaperStyle", "2");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case WallpaperFitMode.Tile:
                        key.SetValue("WallpaperStyle", "0");
                        key.SetValue("TileWallpaper", "1");
                        break;
                    case WallpaperFitMode.Center:
                        key.SetValue("WallpaperStyle", "0");
                        key.SetValue("TileWallpaper", "0");
                        break;
                }

                FileLogger.Log($"Set wallpaper style to {mode}");
            }
            catch (Exception ex)
            {
                // Log but don't fail - wallpaper will use existing style
                FileLogger.Log($"Failed to set wallpaper style in registry: {ex.Message}");
            }
        }
    }
}
