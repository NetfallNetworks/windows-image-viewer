namespace WallpaperApp.Services
{
    /// <summary>
    /// Image format types supported by the application.
    /// </summary>
    public enum ImageFormat
    {
        Unknown,
        PNG,
        JPEG,
        BMP
    }

    /// <summary>
    /// Service for validating image files using magic byte signatures.
    /// </summary>
    public interface IImageValidator
    {
        /// <summary>
        /// Validates an image file by checking its magic bytes (file signature).
        /// This prevents security vulnerabilities from extension-only validation.
        /// </summary>
        /// <param name="filePath">Path to the image file to validate.</param>
        /// <param name="format">Output parameter containing the detected image format.</param>
        /// <returns>True if the file is a valid image (PNG, JPEG, or BMP), false otherwise.</returns>
        bool IsValidImage(string filePath, out ImageFormat format);
    }
}
