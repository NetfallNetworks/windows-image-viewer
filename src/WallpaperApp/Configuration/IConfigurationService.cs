namespace WallpaperApp.Configuration
{
    /// <summary>
    /// Interface for loading and validating application configuration.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Loads configuration from appsettings.json and validates it.
        /// </summary>
        /// <returns>Validated application settings.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration is missing or invalid.</exception>
        AppSettings LoadConfiguration();
    }
}
