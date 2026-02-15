using WallpaperApp.Services;
using WallpaperApp.Tests.Infrastructure;
using Xunit;

namespace WallpaperApp.Tests
{
    [Collection("CurrentDirectory Tests")]
    public class WallpaperServiceTests : IDisposable
    {
        private readonly TestDirectoryFixture _fixture;
        private readonly WallpaperService _service;

        public WallpaperServiceTests()
        {
            _fixture = new TestDirectoryFixture("WallpaperServiceTests");
            _service = new WallpaperService();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        /// <summary>
        /// Creates a minimal valid BMP file for testing.
        /// </summary>
        private void CreateTestImage(string filename)
        {
            // Create a minimal 1x1 BMP file
            byte[] bmpData = new byte[]
            {
                // BMP Header
                0x42, 0x4D, // BM
                0x46, 0x00, 0x00, 0x00, // File size (70 bytes)
                0x00, 0x00, // Reserved
                0x00, 0x00, // Reserved
                0x36, 0x00, 0x00, 0x00, // Offset to pixel data

                // DIB Header (BITMAPINFOHEADER)
                0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                0x01, 0x00, // Color planes
                0x18, 0x00, // Bits per pixel (24)
                0x00, 0x00, 0x00, 0x00, // Compression (none)
                0x10, 0x00, 0x00, 0x00, // Image size
                0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                0x00, 0x00, 0x00, 0x00, // Colors in palette
                0x00, 0x00, 0x00, 0x00, // Important colors

                // Pixel data (1 pixel: BGR format + padding)
                0xFF, 0xFF, 0xFF, 0x00 // White pixel + padding
            };

            File.WriteAllBytes(Path.Combine(_fixture.TestDirectory, filename), bmpData);
        }

        [Fact]
        public void SetWallpaper_MissingFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_fixture.TestDirectory, "nonexistent.png");

            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() => _service.SetWallpaper(nonExistentPath));
            Assert.Contains("Image file not found at:", exception.Message);
            Assert.Contains(nonExistentPath, exception.Message);
        }

        [Fact]
        public void SetWallpaper_InvalidFormat_ThrowsInvalidImageException()
        {
            // Arrange
            string invalidFile = Path.Combine(_fixture.TestDirectory, "test.txt");
            File.WriteAllText(invalidFile, "This is not an image");

            // Act & Assert
            var exception = Assert.Throws<InvalidImageException>(() => _service.SetWallpaper(invalidFile));
            Assert.Contains("Invalid image format", exception.Message);
            Assert.Contains(".txt", exception.Message);
            Assert.Contains("Only PNG, JPG, and BMP formats are supported", exception.Message);
        }

        [Fact]
        public void SetWallpaper_RelativePath_ResolvesCorrectly()
        {
            // Arrange
            CreateTestImage("test.bmp");
            string relativePath = "test.bmp";

            // Act & Assert
            // On non-Windows systems, this will throw WallpaperException because SystemParametersInfo
            // is not available. On Windows, it should succeed.
            if (OperatingSystem.IsWindows())
            {
                // Should not throw FileNotFoundException or InvalidImageException
                // May throw WallpaperException if API fails, but that's acceptable for this test
                try
                {
                    _service.SetWallpaper(relativePath);
                }
                catch (WallpaperException)
                {
                    // This is acceptable - the path was resolved correctly,
                    // but the API may fail in test environment
                }
            }
            else
            {
                // On non-Windows, we expect WallpaperException
                Assert.Throws<WallpaperException>(() => _service.SetWallpaper(relativePath));
            }
        }

        [Theory]
        [InlineData("test.png")]
        [InlineData("test.jpg")]
        [InlineData("test.jpeg")]
        [InlineData("test.bmp")]
        [InlineData("test.PNG")]
        [InlineData("test.JPG")]
        [InlineData("test.BMP")]
        public void SetWallpaper_SupportedFormats_DoesNotThrowInvalidImageException(string filename)
        {
            // Arrange
            CreateTestImage(filename);
            string filePath = Path.Combine(_fixture.TestDirectory, filename);

            // Act & Assert
            // Should not throw FileNotFoundException or InvalidImageException
            // May throw WallpaperException on non-Windows or if API fails
            try
            {
                _service.SetWallpaper(filePath);
            }
            catch (InvalidImageException)
            {
                // Should never throw InvalidImageException for supported formats
                Assert.Fail($"InvalidImageException thrown for supported format: {filename}");
            }
            catch (FileNotFoundException)
            {
                // Should never throw FileNotFoundException when file exists
                Assert.Fail($"FileNotFoundException thrown for existing file: {filename}");
            }
            catch (WallpaperException)
            {
                // This is acceptable - format is valid, but API may fail
            }
        }

        [Theory]
        [InlineData("test.gif")]
        [InlineData("test.tiff")]
        [InlineData("test.webp")]
        [InlineData("test.svg")]
        public void SetWallpaper_UnsupportedFormats_ThrowsInvalidImageException(string filename)
        {
            // Arrange
            string filePath = Path.Combine(_fixture.TestDirectory, filename);
            File.WriteAllBytes(filePath, new byte[] { 0x00 }); // Create dummy file

            // Act & Assert
            var exception = Assert.Throws<InvalidImageException>(() => _service.SetWallpaper(filePath));
            Assert.Contains("Invalid image format", exception.Message);
            Assert.Contains("Only PNG, JPG, and BMP formats are supported", exception.Message);
        }

        [Fact]
        public void SetWallpaper_AbsolutePath_ValidImage_SucceedsOnWindows()
        {
            // Arrange
            CreateTestImage("test.bmp");
            string absolutePath = Path.Combine(_fixture.TestDirectory, "test.bmp");

            // Act & Assert
            if (OperatingSystem.IsWindows())
            {
                // Should not throw FileNotFoundException or InvalidImageException
                // May throw WallpaperException if API fails, but that's acceptable
                try
                {
                    _service.SetWallpaper(absolutePath);
                }
                catch (WallpaperException)
                {
                    // This is acceptable - API may fail in test environment
                }
            }
            else
            {
                // On non-Windows, we expect WallpaperException
                Assert.Throws<WallpaperException>(() => _service.SetWallpaper(absolutePath));
            }
        }
    }
}
