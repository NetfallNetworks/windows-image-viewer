using WallpaperApp.Services;
using Xunit;

namespace WallpaperApp.Tests
{
    public class ImageValidatorTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly ImageValidator _validator;

        public ImageValidatorTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ImageValidatorTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _validator = new ImageValidator();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void IsValidImage_ValidPNG_ReturnsTrue()
        {
            // Arrange: Create file with PNG magic bytes
            byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            string testFile = Path.Combine(_testDirectory, "test.png");
            File.WriteAllBytes(testFile, pngHeader);

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.True(isValid);
            Assert.Equal(ImageFormat.PNG, format);
        }

        [Fact]
        public void IsValidImage_ValidJPEG_ReturnsTrue()
        {
            // Arrange: Create file with JPEG magic bytes
            byte[] jpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG with JFIF marker
            string testFile = Path.Combine(_testDirectory, "test.jpg");
            File.WriteAllBytes(testFile, jpegHeader);

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.True(isValid);
            Assert.Equal(ImageFormat.JPEG, format);
        }

        [Fact]
        public void IsValidImage_ValidBMP_ReturnsTrue()
        {
            // Arrange: Create file with BMP magic bytes
            byte[] bmpHeader = { 0x42, 0x4D, 0x00, 0x00 };
            string testFile = Path.Combine(_testDirectory, "test.bmp");
            File.WriteAllBytes(testFile, bmpHeader);

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.True(isValid);
            Assert.Equal(ImageFormat.BMP, format);
        }

        [Fact]
        public void IsValidImage_FakePNG_ReturnsFalse()
        {
            // Arrange: Create file with wrong magic bytes but .png extension
            byte[] fakeContent = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            string testFile = Path.Combine(_testDirectory, "fake.png");
            File.WriteAllBytes(testFile, fakeContent);

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }

        [Fact]
        public void IsValidImage_MaliciousExecutable_ReturnsFalse()
        {
            // Arrange: Create file with executable magic bytes (MZ header)
            byte[] exeHeader = { 0x4D, 0x5A, 0x90, 0x00 }; // "MZ" - DOS/Windows executable
            string testFile = Path.Combine(_testDirectory, "virus.exe.png");
            File.WriteAllBytes(testFile, exeHeader);

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }

        [Fact]
        public void IsValidImage_EmptyFile_ReturnsFalse()
        {
            // Arrange: Create empty file
            string testFile = Path.Combine(_testDirectory, "empty.png");
            File.WriteAllBytes(testFile, Array.Empty<byte>());

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }

        [Fact]
        public void IsValidImage_OneByte_ReturnsFalse()
        {
            // Arrange: Create file with only one byte
            string testFile = Path.Combine(_testDirectory, "onebyte.png");
            File.WriteAllBytes(testFile, new byte[] { 0x89 });

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }

        [Fact]
        public void IsValidImage_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            string testFile = Path.Combine(_testDirectory, "nonexistent.png");

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }

        [Fact]
        public void IsValidImage_PartialPNGHeader_ReturnsFalse()
        {
            // Arrange: Only first 4 bytes of PNG header
            byte[] partialHeader = { 0x89, 0x50, 0x4E, 0x47 };
            string testFile = Path.Combine(_testDirectory, "partial.png");
            File.WriteAllBytes(testFile, partialHeader);

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }

        [Fact]
        public void IsValidImage_TextFile_ReturnsFalse()
        {
            // Arrange: Text file with .png extension
            string testFile = Path.Combine(_testDirectory, "text.png");
            File.WriteAllText(testFile, "This is not an image");

            // Act
            bool isValid = _validator.IsValidImage(testFile, out var format);

            // Assert
            Assert.False(isValid);
            Assert.Equal(ImageFormat.Unknown, format);
        }
    }
}
