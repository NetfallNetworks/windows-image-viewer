using WallpaperApp.Configuration;
using WallpaperApp.Models;
using Xunit;

namespace WallpaperApp.Tests
{
    public class ConfigurationServiceTests : IDisposable
    {
        // Each test gets its own isolated directory so tests don't interfere with each other.
        private readonly string _testDirectory;
        private readonly string _userConfigPath;
        private readonly string _defaultConfigPath;

        public ConfigurationServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ConfigServiceTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _userConfigPath = Path.Combine(_testDirectory, "user", "WallpaperApp.json");
            _defaultConfigPath = Path.Combine(_testDirectory, "defaults", "WallpaperApp.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_userConfigPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(_defaultConfigPath)!);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try { Directory.Delete(_testDirectory, true); }
                catch { /* ignore cleanup failures */ }
            }
        }

        private ConfigurationService CreateService() =>
            new ConfigurationService(_userConfigPath, _defaultConfigPath);

        private void WriteUserConfig(string json) =>
            File.WriteAllText(_userConfigPath, json);

        private void WriteDefaultConfig(string json) =>
            File.WriteAllText(_defaultConfigPath, json);

        // ── Basic load/validation ────────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_ValidConfig_ReturnsAppSettings()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}");
            var settings = CreateService().LoadConfiguration();

            Assert.NotNull(settings);
            Assert.Equal("https://weather.zamflam.com/latest.png", settings.ImageUrl);
            Assert.Equal(15, settings.RefreshIntervalMinutes);
        }

        [Fact]
        public void LoadConfiguration_MissingFile_ThrowsException()
        {
            // Neither user config nor default config exist.
            var exception = Assert.Throws<ConfigurationException>(() => CreateService().LoadConfiguration());
            Assert.Contains("WallpaperApp.json not found", exception.Message);
            Assert.Contains("Create it with ImageUrl setting", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_NonHttpsUrl_ThrowsException()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""http://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}");
            var exception = Assert.Throws<ConfigurationException>(() => CreateService().LoadConfiguration());
            Assert.Contains("ImageUrl must use HTTPS protocol for security", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_EmptyUrl_ThrowsException()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15
  }
}");
            var exception = Assert.Throws<ConfigurationException>(() => CreateService().LoadConfiguration());
            Assert.Contains("ImageUrl cannot be empty", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_NullUrl_ThrowsException()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""RefreshIntervalMinutes"": 15
  }
}");
            var exception = Assert.Throws<ConfigurationException>(() => CreateService().LoadConfiguration());
            Assert.Contains("ImageUrl cannot be empty", exception.Message);
        }

        // ── Default-property handling ────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_OldFormat_AddsDefaults()
        {
            // Old-format JSON (only the two original fields) — new fields should use C# defaults.
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://example.com/image.png"",
    ""RefreshIntervalMinutes"": 15
  }
}");
            var settings = CreateService().LoadConfiguration();

            Assert.Equal(WallpaperFitMode.Fit, settings.FitMode);
            Assert.False(settings.EnableNotifications);
            Assert.Null(settings.LocalImagePath);
            Assert.Equal(ImageSource.Url, settings.SourceType);
        }

        [Fact]
        public void LoadConfiguration_AllNewProperties_LoadsCorrectly()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://example.com/image.png"",
    ""RefreshIntervalMinutes"": 30,
    ""FitMode"": 2,
    ""EnableNotifications"": true,
    ""SourceType"": 0
  }
}");
            var settings = CreateService().LoadConfiguration();

            Assert.Equal(30, settings.RefreshIntervalMinutes);
            Assert.Equal(WallpaperFitMode.Stretch, settings.FitMode);
            Assert.True(settings.EnableNotifications);
            Assert.Equal(ImageSource.Url, settings.SourceType);
        }

        // ── Local-file mode ──────────────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_LocalFileMode_ValidPath_Succeeds()
        {
            var testImagePath = Path.Combine(_testDirectory, "test.png");
            File.WriteAllBytes(testImagePath, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            WriteUserConfig($@"{{
  ""AppSettings"": {{
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15,
    ""SourceType"": 1,
    ""LocalImagePath"": ""{testImagePath.Replace("\\", "\\\\")}""
  }}
}}");
            var settings = CreateService().LoadConfiguration();

            Assert.Equal(ImageSource.LocalFile, settings.SourceType);
            Assert.Equal(testImagePath, settings.LocalImagePath);
        }

        [Fact]
        public void LoadConfiguration_LocalFileMode_MissingPath_ThrowsException()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15,
    ""SourceType"": 1
  }
}");
            var exception = Assert.Throws<ConfigurationException>(() => CreateService().LoadConfiguration());
            Assert.Contains("LocalImagePath is required when SourceType is LocalFile", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_LocalFileMode_FileNotFound_ThrowsException()
        {
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15,
    ""SourceType"": 1,
    ""LocalImagePath"": ""/nonexistent/image.png""
  }
}");
            var exception = Assert.Throws<ConfigurationException>(() => CreateService().LoadConfiguration());
            Assert.Contains("Local image file not found", exception.Message);
            Assert.Contains("nonexistent", exception.Message);
        }

        // ── First-run seeding ────────────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_NoUserConfig_SeedsFromDefault()
        {
            // Default config exists, user config does not.
            WriteDefaultConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}");
            // User config does not exist yet.
            Assert.False(File.Exists(_userConfigPath));

            var settings = CreateService().LoadConfiguration();

            // Settings loaded correctly from the seeded file.
            Assert.Equal("https://weather.zamflam.com/latest.png", settings.ImageUrl);
            Assert.Equal(15, settings.RefreshIntervalMinutes);
            // User config file was created.
            Assert.True(File.Exists(_userConfigPath));
        }

        [Fact]
        public void LoadConfiguration_SeedsUserConfig_ContentMatchesDefault()
        {
            var defaultJson = @"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}";
            WriteDefaultConfig(defaultJson);

            CreateService().LoadConfiguration();

            var seededJson = File.ReadAllText(_userConfigPath);
            Assert.Equal(defaultJson, seededJson);
        }

        [Fact]
        public void LoadConfiguration_UserConfigTakesPrecedenceOverDefault()
        {
            // Both user config and default exist with different URLs.
            WriteDefaultConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://default.example.com/image.png"",
    ""RefreshIntervalMinutes"": 15
  }
}");
            WriteUserConfig(@"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://user.example.com/image.png"",
    ""RefreshIntervalMinutes"": 30
  }
}");
            var settings = CreateService().LoadConfiguration();

            // User config wins — default is ignored when user config exists.
            Assert.Equal("https://user.example.com/image.png", settings.ImageUrl);
            Assert.Equal(30, settings.RefreshIntervalMinutes);
        }

        // ── Save / round-trip ────────────────────────────────────────────────

        [Fact]
        public void SaveConfiguration_WritesToUserConfigPath()
        {
            var service = CreateService();
            var settings = new AppSettings
            {
                ImageUrl = "https://example.com/image.png",
                RefreshIntervalMinutes = 20
            };

            service.SaveConfiguration(settings);

            Assert.True(File.Exists(_userConfigPath));
            var json = File.ReadAllText(_userConfigPath);
            Assert.Contains("https://example.com/image.png", json);
        }

        [Fact]
        public void SaveConfiguration_ThenLoad_RoundTrips()
        {
            var service = CreateService();
            var original = new AppSettings
            {
                ImageUrl = "https://example.com/image.png",
                RefreshIntervalMinutes = 30,
                FitMode = WallpaperFitMode.Stretch,
                EnableNotifications = true,
                SourceType = ImageSource.Url
            };

            service.SaveConfiguration(original);
            var loaded = service.LoadConfiguration();

            Assert.Equal(original.ImageUrl, loaded.ImageUrl);
            Assert.Equal(original.RefreshIntervalMinutes, loaded.RefreshIntervalMinutes);
            Assert.Equal(original.FitMode, loaded.FitMode);
            Assert.Equal(original.EnableNotifications, loaded.EnableNotifications);
            Assert.Equal(original.SourceType, loaded.SourceType);
        }

        [Fact]
        public void SaveConfiguration_WritesHumanReadableJson()
        {
            var service = CreateService();
            service.SaveConfiguration(new AppSettings
            {
                ImageUrl = "https://example.com/image.png",
                RefreshIntervalMinutes = 15
            });

            var json = File.ReadAllText(_userConfigPath);
            Assert.Contains("AppSettings", json);
            Assert.Contains("ImageUrl", json);
            Assert.Contains("\n", json); // Indented (human-readable).
        }
    }
}
