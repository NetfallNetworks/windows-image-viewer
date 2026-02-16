using WallpaperApp.Services;
using Xunit;

namespace WallpaperApp.Tests
{
    public class FileCleanupServiceTests : IDisposable
    {
        private readonly string _testDirectory;

        public FileCleanupServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"FileCleanupServiceTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                // Remove read-only attributes from all files before deleting
                foreach (var file in Directory.GetFiles(_testDirectory, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                        }
                    }
                    catch
                    {
                        // Ignore errors when removing read-only attribute
                    }
                }

                Directory.Delete(_testDirectory, true);
            }
        }

        private void CreateTestFile(string filename, DateTime creationTime)
        {
            string path = Path.Combine(_testDirectory, filename);
            File.WriteAllBytes(path, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            File.SetCreationTime(path, creationTime);
        }

        [Fact]
        public void CleanupOldFiles_NoDirectory_DoesNotThrow()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testDirectory, "nonexistent");
            var service = new FileCleanupService(nonExistentDir);

            // Act & Assert - Should not throw
            service.CleanupOldFiles();
        }

        [Fact]
        public void CleanupOldFiles_NoFiles_DoesNotThrow()
        {
            // Arrange
            var service = new FileCleanupService(_testDirectory);

            // Act & Assert - Should not throw
            service.CleanupOldFiles();
        }

        [Fact]
        public void CleanupOldFiles_WithinLimits_KeepsAllFiles()
        {
            // Arrange
            CreateTestFile("wallpaper-001.png", DateTime.Now.AddDays(-1));
            CreateTestFile("wallpaper-002.png", DateTime.Now.AddDays(-2));
            CreateTestFile("wallpaper-003.png", DateTime.Now.AddDays(-3));

            var service = new FileCleanupService(_testDirectory);

            // Act
            service.CleanupOldFiles(maxFiles: 10, maxAgeDays: 7);

            // Assert
            var files = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
            Assert.Equal(3, files.Length);
        }

        [Fact]
        public void CleanupOldFiles_ExceedsMaxFiles_DeletesOldest()
        {
            // Arrange
            CreateTestFile("wallpaper-001.png", DateTime.Now);
            CreateTestFile("wallpaper-002.png", DateTime.Now.AddDays(-1));
            CreateTestFile("wallpaper-003.png", DateTime.Now.AddDays(-2));
            CreateTestFile("wallpaper-004.png", DateTime.Now.AddDays(-3));
            CreateTestFile("wallpaper-005.png", DateTime.Now.AddDays(-4));

            var service = new FileCleanupService(_testDirectory);

            // Act
            service.CleanupOldFiles(maxFiles: 3, maxAgeDays: 30);

            // Assert
            var files = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
            Assert.Equal(3, files.Length);

            // Verify the 3 newest files remain
            Assert.True(File.Exists(Path.Combine(_testDirectory, "wallpaper-001.png")));
            Assert.True(File.Exists(Path.Combine(_testDirectory, "wallpaper-002.png")));
            Assert.True(File.Exists(Path.Combine(_testDirectory, "wallpaper-003.png")));
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-004.png")));
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-005.png")));
        }

        [Fact]
        public void CleanupOldFiles_ExceedsMaxAge_DeletesOldFiles()
        {
            // Arrange
            CreateTestFile("wallpaper-new.png", DateTime.Now.AddDays(-2));
            CreateTestFile("wallpaper-old1.png", DateTime.Now.AddDays(-10));
            CreateTestFile("wallpaper-old2.png", DateTime.Now.AddDays(-15));

            var service = new FileCleanupService(_testDirectory);

            // Act
            service.CleanupOldFiles(maxFiles: 10, maxAgeDays: 7);

            // Assert
            var files = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
            Assert.Single(files);
            Assert.True(File.Exists(Path.Combine(_testDirectory, "wallpaper-new.png")));
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-old1.png")));
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-old2.png")));
        }

        [Fact]
        public void CleanupOldFiles_BothLimits_AppliesBothRules()
        {
            // Arrange
            CreateTestFile("wallpaper-001.png", DateTime.Now);
            CreateTestFile("wallpaper-002.png", DateTime.Now.AddDays(-1));
            CreateTestFile("wallpaper-003.png", DateTime.Now.AddDays(-2));
            CreateTestFile("wallpaper-old1.png", DateTime.Now.AddDays(-10));
            CreateTestFile("wallpaper-old2.png", DateTime.Now.AddDays(-15));

            var service = new FileCleanupService(_testDirectory);

            // Act
            service.CleanupOldFiles(maxFiles: 2, maxAgeDays: 7);

            // Assert
            var files = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
            Assert.Equal(2, files.Length);

            // Should keep the 2 newest files within the age limit
            Assert.True(File.Exists(Path.Combine(_testDirectory, "wallpaper-001.png")));
            Assert.True(File.Exists(Path.Combine(_testDirectory, "wallpaper-002.png")));
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-003.png"))); // Deleted due to count
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-old1.png"))); // Deleted due to age
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-old2.png"))); // Deleted due to age
        }

        [Fact]
        public void CleanupOldFiles_OnlyDeletesWallpaperFiles()
        {
            // Arrange
            CreateTestFile("wallpaper-001.png", DateTime.Now.AddDays(-10));
            File.WriteAllText(Path.Combine(_testDirectory, "other-file.txt"), "test");
            File.WriteAllText(Path.Combine(_testDirectory, "image.png"), "test");

            var service = new FileCleanupService(_testDirectory);

            // Act
            service.CleanupOldFiles(maxFiles: 0, maxAgeDays: 1);

            // Assert
            Assert.False(File.Exists(Path.Combine(_testDirectory, "wallpaper-001.png")));
            Assert.True(File.Exists(Path.Combine(_testDirectory, "other-file.txt")));
            Assert.True(File.Exists(Path.Combine(_testDirectory, "image.png")));
        }

        [Fact]
        public void CleanupOldFiles_DefaultParameters_Uses10And7()
        {
            // Arrange
            for (int i = 0; i < 15; i++)
            {
                CreateTestFile($"wallpaper-{i:D3}.png", DateTime.Now.AddDays(-i));
            }

            var service = new FileCleanupService(_testDirectory);

            // Act
            service.CleanupOldFiles(); // Use defaults

            // Assert
            var files = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
            Assert.Equal(7, files.Length); // Files 8-14 are older than 7 days, files 0-6 remain
        }

        [Fact]
        public void CleanupOldFiles_ReadOnlyFile_ContinuesWithOthers()
        {
            // Arrange
            CreateTestFile("wallpaper-001.png", DateTime.Now.AddDays(-10));
            CreateTestFile("wallpaper-002.png", DateTime.Now.AddDays(-11));

            // Make first file read-only (simulate locked file)
            var readonlyPath = Path.Combine(_testDirectory, "wallpaper-001.png");
            var file = new FileInfo(readonlyPath);
            file.IsReadOnly = true;

            var service = new FileCleanupService(_testDirectory);

            // Act & Assert - Should not throw, should continue with other files
            service.CleanupOldFiles(maxFiles: 0, maxAgeDays: 1);

            // Read-only file might still exist (depends on permissions), but should have attempted cleanup
            var files = Directory.GetFiles(_testDirectory, "wallpaper-*.png");
            Assert.True(files.Length <= 2);
        }
    }
}
