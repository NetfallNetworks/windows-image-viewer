using Moq;
using WallpaperApp.Configuration;
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

        public WallpaperUpdaterTests()
        {
            _fixture = new TestDirectoryFixture("WallpaperUpdaterTests");

            // Create mock services
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockImageFetcher = new Mock<IImageFetcher>();
            _mockWallpaperService = new Mock<IWallpaperService>();
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
                .Setup(w => w.SetWallpaper(testImagePath))
                .Verifiable();

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.True(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(testImagePath), Times.Once);
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

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(It.IsAny<string>()), Times.Never);
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
                .Setup(w => w.SetWallpaper(testImagePath))
                .Throws(new WallpaperException("Failed to set wallpaper"));

            var updater = new WallpaperUpdater(
                _mockConfigurationService.Object,
                _mockImageFetcher.Object,
                _mockWallpaperService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync("https://weather.zamflam.com/latest.png"), Times.Once);
            _mockWallpaperService.Verify(w => w.SetWallpaper(testImagePath), Times.Once);
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
                _mockWallpaperService.Object);

            // Act
            var result = await updater.UpdateWallpaperAsync();

            // Assert
            Assert.False(result);
            _mockConfigurationService.Verify(c => c.LoadConfiguration(), Times.Once);
            _mockImageFetcher.Verify(f => f.DownloadImageAsync(It.IsAny<string>()), Times.Never);
            _mockWallpaperService.Verify(w => w.SetWallpaper(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Constructor_NullConfigurationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    null!,
                    _mockImageFetcher.Object,
                    _mockWallpaperService.Object));
        }

        [Fact]
        public void Constructor_NullImageFetcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    _mockConfigurationService.Object,
                    null!,
                    _mockWallpaperService.Object));
        }

        [Fact]
        public void Constructor_NullWallpaperService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WallpaperUpdater(
                    _mockConfigurationService.Object,
                    _mockImageFetcher.Object,
                    null!));
        }
    }
}
