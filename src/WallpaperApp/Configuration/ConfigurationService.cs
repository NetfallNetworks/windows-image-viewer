using Microsoft.Extensions.Configuration;

namespace WallpaperApp.Configuration
{
    /// <summary>
    /// Service for loading and validating application configuration.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        /// <summary>
        /// Loads configuration from appsettings.json and validates it.
        /// </summary>
        /// <returns>Validated application settings.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration is missing or invalid.</exception>
        public AppSettings LoadConfiguration()
        {
            IConfiguration config;

            try
            {
                config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("WallpaperApp.json", optional: false, reloadOnChange: false)
                    .Build();
            }
            catch (FileNotFoundException)
            {
                throw new ConfigurationException("WallpaperApp.json not found. Create it with ImageUrl setting.");
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("WallpaperApp.json not found. Create it with ImageUrl setting.", ex);
            }

            var settings = config.GetSection("AppSettings").Get<AppSettings>()
                ?? throw new ConfigurationException("AppSettings section not found in WallpaperApp.json");

            // Validation: ImageUrl cannot be empty
            if (string.IsNullOrWhiteSpace(settings.ImageUrl))
            {
                throw new ConfigurationException("ImageUrl cannot be empty");
            }

            // Validation: ImageUrl must use HTTPS
            if (!settings.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConfigurationException("ImageUrl must use HTTPS protocol for security");
            }

            return settings;
        }
    }
}
