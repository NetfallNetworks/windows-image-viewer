namespace WallpaperApp.Services
{
    /// <summary>
    /// Validates image files using magic byte signatures (file headers).
    /// This provides security against malicious files with fake extensions.
    /// </summary>
    public class ImageValidator : IImageValidator
    {
        // Magic byte signatures for supported image formats
        private static readonly byte[] PNG_HEADER = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JPEG_HEADER = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] BMP_HEADER = { 0x42, 0x4D };

        /// <summary>
        /// Validates an image file by checking its magic bytes (file signature).
        /// </summary>
        /// <param name="filePath">Path to the image file to validate.</param>
        /// <param name="format">Output parameter containing the detected image format.</param>
        /// <returns>True if the file is a valid image (PNG, JPEG, or BMP), false otherwise.</returns>
        public bool IsValidImage(string filePath, out ImageFormat format)
        {
            format = ImageFormat.Unknown;

            if (!File.Exists(filePath))
                return false;

            try
            {
                using var stream = File.OpenRead(filePath);
                byte[] header = new byte[8];
                int bytesRead = stream.Read(header, 0, 8);

                if (bytesRead < 2)
                    return false;

                // Check PNG (8 bytes)
                if (bytesRead >= 8 && header.Take(8).SequenceEqual(PNG_HEADER))
                {
                    format = ImageFormat.PNG;
                    return true;
                }

                // Check JPEG (3 bytes)
                if (bytesRead >= 3 && header.Take(3).SequenceEqual(JPEG_HEADER))
                {
                    format = ImageFormat.JPEG;
                    return true;
                }

                // Check BMP (2 bytes)
                if (header.Take(2).SequenceEqual(BMP_HEADER))
                {
                    format = ImageFormat.BMP;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Image validation error for {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}
