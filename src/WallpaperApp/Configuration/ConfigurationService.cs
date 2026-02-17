using System.Text.Json;

namespace WallpaperApp.Configuration
{
    /// <summary>
    /// Service for loading and validating application configuration.
    ///
    /// User configuration is stored in %LOCALAPPDATA%\WallpaperSync\WallpaperApp.json so that
    /// it survives application reinstalls. On first run, the bundled default config (next to the
    /// executable) is copied there to seed the initial settings.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        // Wraps the top-level JSON structure: { "AppSettings": { ... } }
        private class ConfigRoot
        {
            public AppSettings AppSettings { get; set; } = new AppSettings();
        }

        private readonly string _configFilePath;
        private readonly string _defaultConfigFilePath;

        /// <summary>
        /// Production constructor.
        /// User config: %LOCALAPPDATA%\WallpaperSync\WallpaperApp.json (survives reinstalls).
        /// Default config: WallpaperApp.json next to the executable (read-only seed).
        /// </summary>
        public ConfigurationService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string configDirectory = Path.Combine(appDataPath, "WallpaperSync");
            Directory.CreateDirectory(configDirectory);
            _configFilePath = Path.Combine(configDirectory, "WallpaperApp.json");
            _defaultConfigFilePath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
        }

        /// <summary>
        /// Test constructor. Allows injecting both the user config path and the default config
        /// path so tests can use isolated temporary directories.
        /// </summary>
        public ConfigurationService(string configFilePath, string defaultConfigFilePath)
        {
            _configFilePath = configFilePath;
            _defaultConfigFilePath = defaultConfigFilePath;
            string? directory = Path.GetDirectoryName(configFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Loads configuration from the user config file and validates it.
        /// If no user config exists yet, seeds it from the bundled default config.
        /// </summary>
        /// <returns>Validated application settings.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration is missing or invalid.</exception>
        public AppSettings LoadConfiguration()
        {
            // Seed user config from bundled default on first run.
            if (!File.Exists(_configFilePath))
            {
                if (File.Exists(_defaultConfigFilePath))
                {
                    File.Copy(_defaultConfigFilePath, _configFilePath);
                }
                else
                {
                    throw new ConfigurationException("WallpaperApp.json not found. Create it with ImageUrl setting.");
                }
            }

            string json;
            try
            {
                json = File.ReadAllText(_configFilePath);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("WallpaperApp.json not found. Create it with ImageUrl setting.", ex);
            }

            ConfigRoot root;
            try
            {
                root = JsonSerializer.Deserialize<ConfigRoot>(json) ?? new ConfigRoot();
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("WallpaperApp.json not found. Create it with ImageUrl setting.", ex);
            }

            var settings = root.AppSettings;

            // Validate based on source type.
            if (settings.SourceType == Models.ImageSource.LocalFile)
            {
                if (string.IsNullOrWhiteSpace(settings.LocalImagePath))
                {
                    throw new ConfigurationException(
                        "LocalImagePath is required when SourceType is LocalFile");
                }

                if (!File.Exists(settings.LocalImagePath))
                {
                    throw new ConfigurationException(
                        $"Local image file not found: {settings.LocalImagePath}");
                }
            }
            else // ImageSource.Url
            {
                if (string.IsNullOrWhiteSpace(settings.ImageUrl))
                {
                    throw new ConfigurationException("ImageUrl cannot be empty");
                }

                if (!settings.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConfigurationException("ImageUrl must use HTTPS protocol for security");
                }
            }

            return settings;
        }

        /// <summary>
        /// Saves configuration to the user config file in %LOCALAPPDATA%\WallpaperSync\.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <exception cref="ConfigurationException">Thrown when configuration cannot be saved.</exception>
        public void SaveConfiguration(AppSettings settings)
        {
            try
            {
                var root = new ConfigRoot { AppSettings = settings };
                var json = JsonSerializer.Serialize(root, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("Failed to save configuration to WallpaperApp.json", ex);
            }
        }
    }
}
