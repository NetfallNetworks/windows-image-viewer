namespace WallpaperApp.Services
{
    /// <summary>
    /// Exception thrown when an image file is not a valid format (PNG, JPG, BMP).
    /// </summary>
    public class InvalidImageException : Exception
    {
        public InvalidImageException(string message) : base(message)
        {
        }

        public InvalidImageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
