namespace WallpaperApp.Configuration
{
    /// <summary>
    /// Exception thrown when configuration is invalid or missing.
    /// </summary>
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        {
        }

        public ConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
