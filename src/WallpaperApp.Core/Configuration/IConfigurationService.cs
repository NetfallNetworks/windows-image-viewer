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

        /// <summary>
        /// Saves configuration to WallpaperApp.json.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <exception cref="ConfigurationException">Thrown when configuration cannot be saved.</exception>
        void SaveConfiguration(AppSettings settings);
    }
}
