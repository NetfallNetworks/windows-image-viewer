using WallpaperApp.Configuration;
using Xunit;

namespace WallpaperApp.Tests
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly string _originalDirectory;
        private readonly string _testDirectory;

        public ConfigurationServiceTests()
        {
            _originalDirectory = Directory.GetCurrentDirectory();
            _testDirectory = Path.Combine(Path.GetTempPath(), "WallpaperAppTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            Directory.SetCurrentDirectory(_testDirectory);
        }

        public void Dispose()
        {
            try
            {
                // Change back to original directory before cleanup
                if (Directory.Exists(_originalDirectory))
                {
                    Directory.SetCurrentDirectory(_originalDirectory);
                }
            }
            catch (Exception)
            {
                // If we can't change directory, try temp directory as fallback
                try
                {
                    Directory.SetCurrentDirectory(Path.GetTempPath());
                }
                catch
                {
                    // Ignore - we tried our best
                }
            }

            // Clean up test directory
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, recursive: true);
                }

                // Also clean up parent directory if empty
                string? parentDir = Path.GetDirectoryName(_testDirectory);
                if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                {
                    try
                    {
                        Directory.Delete(parentDir, recursive: false);
                    }
                    catch
                    {
                        // Parent directory not empty or in use - that's fine
                    }
                }
            }
            catch (Exception)
            {
                // Cleanup failures are not critical - ignore them
            }
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
            File.WriteAllText(Path.Combine(_testDirectory, "WallpaperApp.json"), configContent);
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
            File.WriteAllText(Path.Combine(_testDirectory, "WallpaperApp.json"), configContent);
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
            File.WriteAllText(Path.Combine(_testDirectory, "WallpaperApp.json"), configContent);
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
            File.WriteAllText(Path.Combine(_testDirectory, "WallpaperApp.json"), configContent);
            var service = new ConfigurationService();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => service.LoadConfiguration());
            Assert.Contains("ImageUrl cannot be empty", exception.Message);
        }
    }
}
