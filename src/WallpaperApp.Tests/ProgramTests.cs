using WallpaperApp;
using WallpaperApp.Tests.Infrastructure;
using Xunit;

namespace WallpaperApp.Tests
{
    [Collection("CurrentDirectory Tests")]
    public class ProgramTests : IDisposable
    {
        private readonly TestDirectoryFixture _fixture;

        public ProgramTests()
        {
            _fixture = new TestDirectoryFixture("ProgramTests");
        }

        public void Dispose()
        {
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
    ""ImageUrl"": ""https://weather.zamflam.com/latest.png"",
    ""RefreshIntervalMinutes"": 15
  }
}";
            File.WriteAllText(Path.Combine(_fixture.TestDirectory, "WallpaperApp.json"), configContent);

            // Act
            var exitCode = Program.Main(new[] { "--download" });

            // Assert
            // Note: This test makes a real HTTP call. It will succeed if the network is available
            // and the server is up. For a true unit test, HTTP should be mocked.
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DownloadMode_MissingConfig_ReturnsErrorCode()
        {
            // Arrange - No config file created

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
            Assert.Equal(0, exitCode);
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
            // Arrange - Create a text file with .png extension
            string invalidImagePath = Path.Combine(_fixture.TestDirectory, "invalid.png");
            File.WriteAllText(invalidImagePath, "This is not an image");

            // Act
            var exitCode = Program.Main(new[] { invalidImagePath });

            // Assert
            Assert.Equal(1, exitCode);
        }
    }
}
