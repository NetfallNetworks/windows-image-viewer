using Moq;
using WallpaperApp.Configuration;
using WallpaperApp.Services;
using Xunit;

namespace WallpaperApp.Tests
{
    public class TimerServiceTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<IWallpaperUpdater> _mockWallpaperUpdater;
        private readonly AppSettings _testSettings;

        public TimerServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockWallpaperUpdater = new Mock<IWallpaperUpdater>();

            _testSettings = new AppSettings
            {
                ImageUrl = "https://test.example.com/image.png",
                RefreshIntervalMinutes = 1  // Use 1 minute for faster tests
            };

            _mockConfigService
                .Setup(x => x.LoadConfiguration())
                .Returns(_testSettings);
        }

        [Fact]
        public async Task Start_SchedulesFirstExecutionImmediately()
        {
            // Arrange
            _mockWallpaperUpdater
                .Setup(x => x.UpdateWallpaperAsync())
                .ReturnsAsync(true);

            var timerService = new TimerService(_mockConfigService.Object, _mockWallpaperUpdater.Object);
            var cts = new CancellationTokenSource();

            // Act
            var startTask = timerService.StartAsync(cts.Token);

            // Give it a moment to execute the first update
            await Task.Delay(500);

            // Cancel the timer
            cts.Cancel();

            try
            {
                await startTask;
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
            }

            // Assert
            _mockWallpaperUpdater.Verify(x => x.UpdateWallpaperAsync(), Times.AtLeastOnce());
        }

        [Fact]
        public async Task Start_SchedulesSubsequentExecutionsAtInterval()
        {
            // Arrange
            _mockWallpaperUpdater
                .Setup(x => x.UpdateWallpaperAsync())
                .ReturnsAsync(true);

            var timerService = new TimerService(_mockConfigService.Object, _mockWallpaperUpdater.Object);
            var cts = new CancellationTokenSource();

            // Act
            var startTask = timerService.StartAsync(cts.Token);

            // Wait for more than one interval (1 minute + buffer) to allow second execution
            // Using shorter delay for test performance
            await Task.Delay(TimeSpan.FromSeconds(65));

            // Cancel the timer
            cts.Cancel();

            try
            {
                await startTask;
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
            }

            // Assert - should have executed at least twice (immediate + one interval)
            _mockWallpaperUpdater.Verify(x => x.UpdateWallpaperAsync(), Times.AtLeast(2));
        }

        [Fact]
        public async Task Stop_CancelsTimerAndStopsExecution()
        {
            // Arrange
            _mockWallpaperUpdater
                .Setup(x => x.UpdateWallpaperAsync())
                .ReturnsAsync(true);

            var timerService = new TimerService(_mockConfigService.Object, _mockWallpaperUpdater.Object);
            var cts = new CancellationTokenSource();

            // Act
            var startTask = timerService.StartAsync(cts.Token);
            await Task.Delay(500);  // Let it start

            await timerService.StopAsync();
            cts.Cancel();

            try
            {
                await startTask;
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            var callCountAfterStop = _mockWallpaperUpdater.Invocations.Count;

            // Wait a bit more to ensure no more executions happen
            await Task.Delay(2000);

            // Assert - no additional calls after stop
            Assert.Equal(callCountAfterStop, _mockWallpaperUpdater.Invocations.Count);
        }

        [Fact]
        public async Task TimerCallback_CatchesExceptionsAndContinues()
        {
            // Arrange
            var callCount = 0;
            _mockWallpaperUpdater
                .Setup(x => x.UpdateWallpaperAsync())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new Exception("Simulated error");
                    }
                    return Task.FromResult(true);
                });

            var timerService = new TimerService(_mockConfigService.Object, _mockWallpaperUpdater.Object);
            var cts = new CancellationTokenSource();

            // Act
            var startTask = timerService.StartAsync(cts.Token);

            // Wait for error to occur and timer to continue
            await Task.Delay(TimeSpan.FromSeconds(65));

            // Cancel the timer
            cts.Cancel();

            try
            {
                await startTask;
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            // Assert - timer should continue despite exception
            Assert.True(callCount >= 2, "Timer should continue executing after an exception");
        }

        [Fact]
        public async Task Start_ThrowsInvalidOperationException_WhenAlreadyRunning()
        {
            // Arrange
            _mockWallpaperUpdater
                .Setup(x => x.UpdateWallpaperAsync())
                .ReturnsAsync(true);

            var timerService = new TimerService(_mockConfigService.Object, _mockWallpaperUpdater.Object);
            var cts = new CancellationTokenSource();

            // Act
            var startTask = timerService.StartAsync(cts.Token);
            await Task.Delay(500);  // Let it start

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await timerService.StartAsync(CancellationToken.None);
            });

            // Cleanup
            cts.Cancel();
            try
            {
                await startTask;
            }
            catch (TaskCanceledException)
            {
                // Expected
            }
        }

        [Fact]
        public async Task StopAsync_WhenNotRunning_CompletesSuccessfully()
        {
            // Arrange
            var timerService = new TimerService(_mockConfigService.Object, _mockWallpaperUpdater.Object);

            // Act & Assert - should not throw
            await timerService.StopAsync();
        }
    }
}
