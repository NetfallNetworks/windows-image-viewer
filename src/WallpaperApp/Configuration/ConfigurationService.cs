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
                // Use AppContext.BaseDirectory to get the exe's directory
                // This works correctly for both console apps and Windows Services
                // (GetCurrentDirectory() returns C:\Windows\System32 for services!)
                config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
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

            // Validate based on source type (Story WS-3)
            if (settings.SourceType == Models.ImageSource.LocalFile)
            {
                // Validation: LocalImagePath is required for local file mode
                if (string.IsNullOrWhiteSpace(settings.LocalImagePath))
                {
                    throw new ConfigurationException(
                        "LocalImagePath is required when SourceType is LocalFile");
                }

                // Validation: Local file must exist
                if (!File.Exists(settings.LocalImagePath))
                {
                    throw new ConfigurationException(
                        $"Local image file not found: {settings.LocalImagePath}");
                }
            }
            else // ImageSource.Url
            {
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
            }

            return settings;
        }
    }
}
