using WallpaperApp.Configuration;
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
            // Write to current directory (already set to _testDirectory in constructor)
            File.WriteAllText("WallpaperApp.json", configContent);
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
            File.WriteAllText("WallpaperApp.json", configContent);
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
            File.WriteAllText("WallpaperApp.json", configContent);
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
            File.WriteAllText("WallpaperApp.json", configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("ImageUrl cannot be empty", exception.Message);
        }
    }
}
