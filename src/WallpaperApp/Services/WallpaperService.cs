using System.Runtime.InteropServices;

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
        /// Sets the desktop wallpaper to the specified image file.
        /// </summary>
        /// <param name="imagePath">Path to the image file (absolute or relative).</param>
        /// <exception cref="FileNotFoundException">Thrown when the image file does not exist.</exception>
        /// <exception cref="InvalidImageException">Thrown when the file is not a valid image format (PNG, JPG, BMP).</exception>
        /// <exception cref="WallpaperException">Thrown when the wallpaper cannot be set due to a Windows API error.</exception>
        public void SetWallpaper(string imagePath)
        {
            // Convert relative path to absolute path
            string absolutePath = Path.GetFullPath(imagePath);

            // Validate file exists
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Image file not found at: {absolutePath}", absolutePath);
            }

            // Validate using magic bytes instead of extension (Security Fix)
            if (!_imageValidator.IsValidImage(absolutePath, out var format))
            {
                throw new InvalidImageException(
                    "Invalid image file. Only PNG, JPG, and BMP formats are supported.");
            }

            FileLogger.Log($"Validated {format} image: {absolutePath}");

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
    }
}
