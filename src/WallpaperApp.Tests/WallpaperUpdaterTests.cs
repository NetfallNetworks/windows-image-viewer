using Moq;
using WallpaperApp.Configuration;
using WallpaperApp.Models;
using WallpaperApp.Services;
using WallpaperApp.Tests.Infrastructure;
using Xunit;

namespace WallpaperApp.Tests
{
    [Collection("CurrentDirectory Tests")]
    public class WallpaperUpdaterTests : IDisposable
    {
        private readonly TestDirectoryFixture _fixture;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Mock<IImageFetcher> _mockImageFetcher;
        private readonly Mock<IWallpaperService> _mockWallpaperService;
        private readonly Mock<IAppStateService> _mockAppStateService;
        private readonly Mock<IFileCleanupService> _mockFileCleanupService;

        public WallpaperUpdaterTests()
        {
            _fixture = new TestDirectoryFixture("WallpaperUpdaterTests");

            // Create mock services
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockImageFetcher = new Mock<IImageFetcher>();
            _mockWallpaperService = new Mock<IWallpaperService>();
            _mockAppStateService = new Mock<IAppStateService>();
            _mockFileCleanupService = new Mock<IFileCleanupService>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task UpdateWallpaperAsync_HappyPath_SucceedsEndToEnd()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });

            string testImagePath = Path.Combine(_fixture.TestDirectory, "test-image.png");
            _mockImageFetcher
                .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
                .ReturnsAsync(testImagePath);

            _mockWallpaperService
                .Setup(w => w.SetWallpaper(testImagePath, It.IsAny<WallpaperFitMode>()))
                .Verifiable();

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.True(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(testImagePath, WallpaperFitMode.Fit), Times.Once);
            _mockAppStateService.Verify(s => s.UpdateLastKnownGood(testImagePath), Times.Once);
            _mockAppStateService.Verify(s => s.IncrementSuccessCount(), Times.Once);
            _mockFileCleanupService.Verify(c => c.CleanupOldFiles(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task UpdateWallpaperAsync_DownloadFails_LogsErrorAndExitsGracefully()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });

            _mockImageFetcher
                .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
                .ReturnsAsync((string?)null); // Download fails

            _mockAppStateService
                .Setup(s => s.LoadState())
                .Returns(new Models.AppState()); // No fallback available

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(It.IsAny<string>(), It.IsAny<WallpaperFitMode>()), Times.Never);
            _mockAppStateService.Verify(s => s.IncrementFailureCount(), Times.Once);
        }

        [Fact]
        public async Task UpdateWallpaperAsync_SetWallpaperFails_LogsErrorAndExitsGracefully()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });

            string testImagePath = Path.Combine(_fixture.TestDirectory, "test-image.png");
            _mockImageFetcher
                .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
                .ReturnsAsync(testImagePath);

            _mockWallpaperService
                .Setup(w => w.SetWallpaper(testImagePath, It.IsAny<WallpaperFitMode>()))
                .Throws(new WallpaperException("Failed to set wallpaper"));

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(testImagePath, WallpaperFitMode.Fit), Times.Once);
            _mockAppStateService.Verify(s => s.IncrementFailureCount(), Times.Once);
        }

        [Fact]
        public async Task UpdateWallpaperAsync_ConfigurationFails_LogsErrorAndExitsGracefully()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Throws(new ConfigurationException("WallpaperApp.json not found. Create it with ImageUrl setting."));

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync(It.IsAny<string>()), Times.Never);
            _mockWallpaperService.Verify(w => w.SetWallpaper(It.IsAny<string>(), It.IsAny<WallpaperFitMode>()), Times.Never);
        }

        [Fact]
        public void Constructor_NullConfigurationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    null!,
                    _mockImageFetcher.Object,
                    _mockWallpaperService.Object,
                    _mockAppStateService.Object,
                    _mockFileCleanupService.Object));
        }

        [Fact]
        public void Constructor_NullImageFetcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    _mockConfigurationService.Object,
                    null!,
                    _mockWallpaperService.Object,
                    _mockAppStateService.Object,
                    _mockFileCleanupService.Object));
        }

        [Fact]
        public void Constructor_NullWallpaperService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    _mockConfigurationService.Object,
                    _mockImageFetcher.Object,
                    null!,
                    _mockAppStateService.Object,
                    _mockFileCleanupService.Object));
        }

        [Fact]
        public void Constructor_NullAppStateService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    _mockConfigurationService.Object,
                    _mockImageFetcher.Object,
                    _mockWallpaperService.Object,
                    null!,
                    _mockFileCleanupService.Object));
        }

        [Fact]
        public void Constructor_NullFileCleanupService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    _mockConfigurationService.Object,
                    _mockImageFetcher.Object,
                    _mockWallpaperService.Object,
                    _mockAppStateService.Object,
                    null!));
        }

        // === Story WS-5: Last-Known-Good Fallback Tests ===

        [Fact]
        public async Task UpdateWallpaperAsync_DownloadFailsWithFallback_UsesFallback()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });

            _mockImageFetcher
                .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
                .ReturnsAsync((string?)null); // Download fails

            string lastKnownGoodPath = Path.Combine(_fixture.TestDirectory, "last-known-good.png");
            File.WriteAllBytes(lastKnownGoodPath, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            _mockAppStateService
                .Setup(s => s.LoadState())
                .Returns(new Models.AppState
                {
                    LastKnownGoodImagePath = lastKnownGoodPath
                });

            _mockWallpaperService
                .Setup(w => w.SetWallpaper(lastKnownGoodPath, It.IsAny<WallpaperFitMode>()))
                .Verifiable();

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.True(result); // Success via fallback
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(lastKnownGoodPath, WallpaperFitMode.Fit), Times.Once);
            _mockAppStateService.Verify(s => s.IncrementFailureCount(), Times.Once); // Still counts as a failure
        }

        [Fact]
        public async Task UpdateWallpaperAsync_DownloadFailsNoFallback_ReturnsFalse()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });

            _mockImageFetcher
                .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
                .ReturnsAsync((string?)null); // Download fails

            _mockAppStateService
                .Setup(s => s.LoadState())
                .Returns(new Models.AppState
                {
                    LastKnownGoodImagePath = null // No fallback available
                });

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockWallpaperService.Verify(w => w.SetWallpaper(It.IsAny<string>(), It.IsAny<WallpaperFitMode>()), Times.Never);
            _mockAppStateService.Verify(s => s.IncrementFailureCount(), Times.Once);
        }

        [Fact]
        public async Task UpdateWallpaperAsync_DownloadFailsFallbackMissing_ReturnsFalse()
        {
            // Arrange
            _mockConfigurationService
                .Setup(c => c.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });

            _mockImageFetcher
                .Setup(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"))
                .ReturnsAsync((string?)null); // Download fails

            _mockAppStateService
                .Setup(s => s.LoadState())
                .Returns(new Models.AppState
                {
                    LastKnownGoodImagePath = "/nonexistent/file.png" // File doesn't exist
                });

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object,
                _mockAppStateService.Object,
                _mockFileCleanupService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockWallpaperService.Verify(w => w.SetWallpaper(It.IsAny<string>(), It.IsAny<WallpaperFitMode>()), Times.Never);
            _mockAppStateService.Verify(s => s.IncrementFailureCount(), Times.Once);
        }
    }
}
