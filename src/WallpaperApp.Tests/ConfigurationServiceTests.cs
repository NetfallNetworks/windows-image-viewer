using WallpaperApp.Configuration;
using WallpaperApp.Models;
using WallpaperApp.Tests.Infrastructure;
using Xunit;

namespace WallpaperApp.Tests
{
    [Collection("CurrentDirectory Tests")]
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly TestDirectoryFixture _fixture;

        public ConfigurationServiceTests()
        {
            _fixture = new TestDirectoryFixture("ConfigurationServiceTests");
        }

        public void Dispose()
        {
            // Clean up config file from AppContext.BaseDirectory
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            if (File.Exists(configPath))
            {
                try
                {
                    File.Delete(configPath);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }

            _fixture.Dispose();
        }

        [Fact]
        public void LoadConfiguration_ValidConfig_ReturnsAppSettings()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}";
            // Write to AppContext.BaseDirectory (where ConfigurationService looks for config)
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act
            var settings = service.LoadConfiguration();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("https://weather.zamflam.com/latest.png", settings.ImageUrl);
            Assert.Equal(15, settings.RefreshIntervalMinutes);
        }

        [Fact]
        public void LoadConfiguration_MissingFile_ThrowsException()
        {
            // Arrange
            // Make sure config file doesn't exist in AppContext.BaseDirectory
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("WallpaperApp.json not found", exception.Message);
            Assert.Contains("Create it with ImageUrl setting", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_NonHttpsUrl_ThrowsException()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": ""http://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("ImageUrl must use HTTPS protocol for security", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_EmptyUrl_ThrowsException()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("ImageUrl cannot be empty", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_NullUrl_ThrowsException()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""RefreshIntervalMinutes"": 15
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("ImageUrl cannot be empty", exception.Message);
        }

        // === Story WS-3: Enhanced Configuration Model Tests ===

        [Fact]
        public void LoadConfiguration_OldFormat_AddsDefaults()
        {
            // Arrange - Old format JSON (only ImageUrl and RefreshIntervalMinutes)
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://example.com/image.png"",
    ""RefreshIntervalMinutes"": 15
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act
            var settings = service.LoadConfiguration();

            // Assert - New properties should have defaults
            Assert.Equal(WallpaperFitMode.Fill, settings.FitMode);
            Assert.False(settings.EnableNotifications);
            Assert.Null(settings.LocalImagePath);
            Assert.Equal(ImageSource.Url, settings.SourceType);
        }

        [Fact]
        public void LoadConfiguration_LocalFileMode_ValidPath_Succeeds()
        {
            // Arrange - Create a test image file
            var testImagePath = Path.Combine(_fixture.TestDirectory, "test.png");
            File.WriteAllBytes(testImagePath, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // PNG header

            var configContent = $@"{{
  ""AppSettings"": {{
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15,
    ""SourceType"": 1,
    ""LocalImagePath"": ""{testImagePath.Replace("\\", "\\\\")}""
  }}
}}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act
            var settings = service.LoadConfiguration();

            // Assert
            Assert.Equal(ImageSource.LocalFile, settings.SourceType);
            Assert.Equal(testImagePath, settings.LocalImagePath);
        }

        [Fact]
        public void LoadConfiguration_LocalFileMode_MissingPath_ThrowsException()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15,
    ""SourceType"": 1
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("LocalImagePath is required when SourceType is LocalFile", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_LocalFileMode_FileNotFound_ThrowsException()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": """",
    ""RefreshIntervalMinutes"": 15,
    ""SourceType"": 1,
    ""LocalImagePath"": ""C:\\nonexistent\\image.png""
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("Local image file not found", exception.Message);
            Assert.Contains("nonexistent", exception.Message);
        }

        [Fact]
        public void LoadConfiguration_AllNewProperties_LoadsCorrectly()
        {
            // Arrange
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://example.com/image.png"",
    ""RefreshIntervalMinutes"": 30,
    ""FitMode"": 2,
    ""EnableNotifications"": true,
    ""SourceType"": 0
  }
}";
            var configPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            File.WriteAllText(configPath, configContent);
            var service = new ConfigurationService();

            // Act
            var settings = service.LoadConfiguration();

            // Assert
            Assert.Equal(30, settings.RefreshIntervalMinutes);
            Assert.Equal(WallpaperFitMode.Stretch, settings.FitMode);
            Assert.True(settings.EnableNotifications);
            Assert.Equal(ImageSource.Url, settings.SourceType);
        }
    }
}
