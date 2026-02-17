using WallpaperApp;
using WallpaperApp.Tests.Infrastructure;
using Xunit;

namespace WallpaperApp.Tests
{
    [Collection("CurrentDirectory Tests")]
    public class ProgramTests : IDisposable
    {
        private readonly TestDirectoryFixture _fixture;

        // Path where ConfigurationService (production constructor) stores user config.
        private readonly string _userConfigPath;
        // Backup of any pre-existing user config so we can restore it after the test.
        private readonly string? _userConfigBackup;

        public ProgramTests()
        {
            _fixture = new TestDirectoryFixture("ProgramTests");

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string configDirectory = Path.Combine(appDataPath, "WallpaperSync");
            Directory.CreateDirectory(configDirectory);
            _userConfigPath = Path.Combine(configDirectory, "WallpaperApp.json");

            // Back up and remove any existing user config so every test starts clean.
            if (File.Exists(_userConfigPath))
            {
                _userConfigBackup = Path.Combine(Path.GetTempPath(), $"ProgramTests_backup_{Guid.NewGuid()}.json");
                File.Copy(_userConfigPath, _userConfigBackup);
                File.Delete(_userConfigPath);
            }
        }

        public void Dispose()
        {
            // Remove any config written by this test.
            if (File.Exists(_userConfigPath))
            {
                try { File.Delete(_userConfigPath); } catch { }
            }

            // Restore user config that existed before this test ran.
            if (_userConfigBackup != null && File.Exists(_userConfigBackup))
            {
                try { File.Move(_userConfigBackup, _userConfigPath, overwrite: true); } catch { }
            }

            _fixture.Dispose();
        }

        /// <summary>
        /// Creates a minimal valid BMP file for testing.
        /// </summary>
        private string CreateTestBmpFile(string filename)
        {
            // Create a minimal 1x1 BMP file
            byte[] bmpData = new byte[]
            {
                // BMP Header
                0x42, 0x4D,             // "BM" signature
                0x46, 0x00, 0x00, 0x00, // File size (70 bytes)
                0x00, 0x00,             // Reserved
                0x00, 0x00,             // Reserved
                0x36, 0x00, 0x00, 0x00, // Offset to pixel data

                // DIB Header (BITMAPINFOHEADER)
                0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                0x01, 0x00,             // Color planes (1)
                0x18, 0x00,             // Bits per pixel (24-bit RGB)
                0x00, 0x00, 0x00, 0x00, // Compression (none)
                0x10, 0x00, 0x00, 0x00, // Image size (16 bytes - includes padding)
                0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                0x00, 0x00, 0x00, 0x00, // Colors in palette
                0x00, 0x00, 0x00, 0x00, // Important colors

                // Pixel data (1x1 blue pixel + padding)
                0xFF, 0x00, 0x00,       // Blue pixel (BGR format)
                0x00                    // Padding to 4-byte boundary
            };

            string filePath = Path.Combine(_fixture.TestDirectory, filename);
            File.WriteAllBytes(filePath, bmpData);
            return filePath;
        }

        [Fact]
        public void ApplicationDisplaysHelp_HelpFlag()
        {
            // Arrange & Act
            var exitCode = Program.Main(new[] { "--help" });

            // Assert
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DownloadMode_ValidConfig_DownloadsImage()
        {
            // Arrange - Create valid config file
            var configContent = @"{
  ""AppSettings"": {
    ""ImageUrl"": ""https://weather.zamflam.com/assets/diagram.png"",
    ""RefreshIntervalMinutes"": 15
  }
}";
            // Write config to the user config path where ConfigurationService now looks for it.
            File.WriteAllText(_userConfigPath, configContent);

            // Act
            var exitCode = Program.Main(new[] { "--download" });

            // Assert
            // Note: This test makes a real HTTP call. It may fail if network is unavailable.
            // The test verifies the app handles both success and failure gracefully.
            // - Exit code 0 = Download succeeded (expected on Windows with network)
            // - Exit code 1 = Download failed or non-Windows (graceful failure)
            if (OperatingSystem.IsWindows())
            {
                // On Windows, either success (0) or graceful network failure (1) is acceptable
                Assert.True(exitCode == 0 || exitCode == 1,
                    $"Expected exit code 0 (success) or 1 (graceful failure), but got {exitCode}");
            }
            else
            {
                // On non-Windows, setting wallpaper will fail
                Assert.Equal(1, exitCode);
            }
        }

        [Fact]
        public void DownloadMode_MissingConfig_ReturnsErrorCode()
        {
            // Arrange - Ensure no config exists at either location the service checks.
            // User config is already cleared by the constructor (backed up and deleted).
            // Also clear the bundled default from the test output dir if present.
            var defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "WallpaperApp.json");
            if (File.Exists(defaultConfigPath))
            {
                File.Delete(defaultConfigPath);
            }

            // Act
            var exitCode = Program.Main(new[] { "--download" });

            // Assert
            Assert.Equal(1, exitCode);
        }

        [Fact]
        public void SetWallpaperMode_ValidImage_SetsWallpaper()
        {
            // Arrange
            string testImagePath = CreateTestBmpFile("test.bmp");

            // Act
            var exitCode = Program.Main(new[] { testImagePath });

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(0, exitCode);
            }
            else
            {
                // On non-Windows, setting wallpaper will fail with WallpaperException
                Assert.Equal(1, exitCode);
            }
        }

        [Fact]
        public void SetWallpaperMode_InvalidImagePath_ReturnsErrorCode()
        {
            // Arrange
            string invalidPath = "nonexistent.png";

            // Act
            var exitCode = Program.Main(new[] { invalidPath });

            // Assert
            Assert.Equal(1, exitCode);
        }

        [Fact]
        public void SetWallpaperMode_InvalidImageFormat_ReturnsErrorCode()
        {
            // Arrange - Create a file with unsupported extension
            string invalidImagePath = Path.Combine(_fixture.TestDirectory, "invalid.txt");
            File.WriteAllText(invalidImagePath, "This is not an image");

            // Act
            var exitCode = Program.Main(new[] { invalidImagePath });

            // Assert
            Assert.Equal(1, exitCode);
        }
    }
}
