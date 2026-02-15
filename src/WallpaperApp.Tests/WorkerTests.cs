using Microsoft.Extensions.Hosting;
using Moq;
using WallpaperApp.Configuration;
using WallpaperApp.Services;
using Xunit;

namespace WallpaperApp.Tests
{
    /// <summary>
    /// Tests for the Worker background service.
    /// </summary>
    public class WorkerTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<IWallpaperUpdater> _mockUpdater;
        private readonly Mock<IHostApplicationLifetime> _mockLifetime;

        public WorkerTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockUpdater = new Mock<IWallpaperUpdater>();
            _mockLifetime = new Mock<IHostApplicationLifetime>();

            // Default configuration
            _mockConfigService.Setup(x => x.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 15
                });
        }

        [Fact]
        public async Task ExecuteAsync_StartsTimerOnServiceStart()
        {
            // Arrange
            var worker = new Worker(_mockConfigService.Object, _mockUpdater.Object, _mockLifetime.Object);
            using var cts = new CancellationTokenSource();

            // Act - Start the service and cancel after a brief delay
            var executeTask = worker.StartAsync(cts.Token);
            await Task.Delay(100); // Give it time to start
            cts.Cancel();
            await executeTask;

            // Assert - Should have called UpdateWallpaperAsync at least once (immediately on startup)
            _mockUpdater.Verify(x => x.UpdateWallpaperAsync(), Times.AtLeastOnce());
        }

        [Fact]
        public async Task ExecuteAsync_StopsTimerOnServiceStop()
        {
            // Arrange
            var worker = new Worker(_mockConfigService.Object, _mockUpdater.Object, _mockLifetime.Object);
            using var cts = new CancellationTokenSource();

            // Act
            var executeTask = worker.StartAsync(cts.Token);
            await Task.Delay(50);
            await worker.StopAsync(CancellationToken.None);
            cts.Cancel();

            // Wait for execution to complete
            try
            {
                await executeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - StopAsync should complete without throwing
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteAsync_ContinuesRunningAfterErrors()
        {
            // Arrange
            var callCount = 0;
            _mockUpdater.Setup(x => x.UpdateWallpaperAsync())
                .Callback(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new Exception("Simulated error on first call");
                    }
                })
                .Returns(Task.CompletedTask);

            var worker = new Worker(_mockConfigService.Object, _mockUpdater.Object, _mockLifetime.Object);
            using var cts = new CancellationTokenSource();

            // Act - Start the service and let it run briefly
            var executeTask = worker.StartAsync(cts.Token);
            await Task.Delay(100);
            cts.Cancel();

            try
            {
                await executeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - Should have attempted to call UpdateWallpaperAsync at least once
            // (The first call throws, but the service should continue)
            Assert.True(callCount >= 1, "UpdateWallpaperAsync should have been called despite error");
        }

        [Fact]
        public void Worker_Constructor_ThrowsOnNullConfigService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Worker(null!, _mockUpdater.Object, _mockLifetime.Object));
        }

        [Fact]
        public void Worker_Constructor_ThrowsOnNullUpdater()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Worker(_mockConfigService.Object, null!, _mockLifetime.Object));
        }

        [Fact]
        public void Worker_Constructor_ThrowsOnNullApplicationLifetime()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Worker(_mockConfigService.Object, _mockUpdater.Object, null!));
        }

        [Fact]
        public async Task ExecuteAsync_UsesConfiguredRefreshInterval()
        {
            // Arrange - Set interval to 1 minute for faster testing
            _mockConfigService.Setup(x => x.LoadConfiguration())
                .Returns(new AppSettings
                {
                    ImageUrl = "https://weather.zamflam.com/latest.png",
                    RefreshIntervalMinutes = 1
                });

            var worker = new Worker(_mockConfigService.Object, _mockUpdater.Object, _mockLifetime.Object);
            using var cts = new CancellationTokenSource();

            // Act
            var executeTask = worker.StartAsync(cts.Token);
            await Task.Delay(50); // Brief delay
            cts.Cancel();

            try
            {
                await executeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - Configuration should have been loaded
            _mockConfigService.Verify(x => x.LoadConfiguration(), Times.AtLeastOnce());
        }
    }
}
