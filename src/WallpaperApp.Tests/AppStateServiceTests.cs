using WallpaperApp.Models;
using WallpaperApp.Services;
using Xunit;

namespace WallpaperApp.Tests
{
    public class AppStateServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _testStateFilePath;

        public AppStateServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"AppStateServiceTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _testStateFilePath = Path.Combine(_testDirectory, "state.json");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void LoadState_NoFile_ReturnsDefaultState()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);

            // Act
            var state = service.LoadState();

            // Assert
            Assert.NotNull(state);
            Assert.True(state.IsEnabled);
            Assert.True(state.IsFirstRun);
            Assert.Null(state.LastKnownGoodImagePath);
            Assert.Null(state.LastUpdateTime);
            Assert.Equal(0, state.UpdateSuccessCount);
            Assert.Equal(0, state.UpdateFailureCount);
        }

        [Fact]
        public void SaveState_ThenLoad_ReturnsSameState()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);
            var originalState = new AppState
            {
                IsEnabled = false,
                LastKnownGoodImagePath = "C:\\test.png",
                IsFirstRun = false,
                LastUpdateTime = DateTime.Parse("2026-02-15 10:00:00"),
                UpdateSuccessCount = 5,
                UpdateFailureCount = 2
            };

            // Act
            service.SaveState(originalState);
            var loadedState = service.LoadState();

            // Assert
            Assert.Equal(originalState.IsEnabled, loadedState.IsEnabled);
            Assert.Equal(originalState.LastKnownGoodImagePath, loadedState.LastKnownGoodImagePath);
            Assert.Equal(originalState.IsFirstRun, loadedState.IsFirstRun);
            Assert.Equal(originalState.UpdateSuccessCount, loadedState.UpdateSuccessCount);
            Assert.Equal(originalState.UpdateFailureCount, loadedState.UpdateFailureCount);
            // DateTime comparison with tolerance for serialization precision
            Assert.NotNull(loadedState.LastUpdateTime);
            Assert.Equal(originalState.LastUpdateTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                        loadedState.LastUpdateTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Fact]
        public void UpdateLastKnownGood_UpdatesPathAndTime()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);
            string imagePath = "/path/to/image.png";
            DateTime beforeUpdate = DateTime.Now.AddSeconds(-1);

            // Act
            service.UpdateLastKnownGood(imagePath);
            var state = service.LoadState();
            DateTime afterUpdate = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.Equal(imagePath, state.LastKnownGoodImagePath);
            Assert.NotNull(state.LastUpdateTime);
            Assert.True(state.LastUpdateTime >= beforeUpdate);
            Assert.True(state.LastUpdateTime <= afterUpdate);
        }

        [Fact]
        public void SetEnabled_UpdatesEnabledFlag()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);

            // Act
            service.SetEnabled(false);
            var state1 = service.LoadState();

            service.SetEnabled(true);
            var state2 = service.LoadState();

            // Assert
            Assert.False(state1.IsEnabled);
            Assert.True(state2.IsEnabled);
        }

        [Fact]
        public void MarkFirstRunComplete_SetsFlagToFalse()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);

            // Act
            service.MarkFirstRunComplete();
            var state = service.LoadState();

            // Assert
            Assert.False(state.IsFirstRun);
        }

        [Fact]
        public void IncrementSuccessCount_IncrementsCountAndUpdatesTime()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);
            DateTime beforeUpdate = DateTime.Now.AddSeconds(-1);

            // Act
            service.IncrementSuccessCount();
            var state1 = service.LoadState();

            service.IncrementSuccessCount();
            var state2 = service.LoadState();
            DateTime afterUpdate = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.Equal(1, state1.UpdateSuccessCount);
            Assert.Equal(2, state2.UpdateSuccessCount);
            Assert.NotNull(state2.LastUpdateTime);
            Assert.True(state2.LastUpdateTime >= beforeUpdate);
            Assert.True(state2.LastUpdateTime <= afterUpdate);
        }

        [Fact]
        public void IncrementFailureCount_IncrementsCount()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);

            // Act
            service.IncrementFailureCount();
            var state1 = service.LoadState();

            service.IncrementFailureCount();
            var state2 = service.LoadState();

            // Assert
            Assert.Equal(1, state1.UpdateFailureCount);
            Assert.Equal(2, state2.UpdateFailureCount);
        }

        [Fact]
        public void LoadState_CorruptFile_ReturnsDefaultState()
        {
            // Arrange
            File.WriteAllText(_testStateFilePath, "{ invalid json }");
            var service = new AppStateService(_testStateFilePath);

            // Act
            var state = service.LoadState();

            // Assert - Should return default state, not crash
            Assert.NotNull(state);
            Assert.True(state.IsEnabled);
            Assert.True(state.IsFirstRun);
        }

        [Fact]
        public void StateFile_CreatesDirectory_IfNotExists()
        {
            // Arrange
            string newTestDir = Path.Combine(_testDirectory, "nested", "path");
            string newStateFilePath = Path.Combine(newTestDir, "state.json");

            // Act
            var service = new AppStateService(newStateFilePath);
            service.SaveState(new AppState { IsEnabled = false });

            // Assert
            Assert.True(Directory.Exists(newTestDir));
            Assert.True(File.Exists(newStateFilePath));
        }

        [Fact]
        public void SaveState_CreatesHumanReadableJson()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);
            var state = new AppState
            {
                IsEnabled = true,
                LastKnownGoodImagePath = "C:\\test.png"
            };

            // Act
            service.SaveState(state);
            string json = File.ReadAllText(_testStateFilePath);

            // Assert
            Assert.Contains("IsEnabled", json);
            Assert.Contains("LastKnownGoodImagePath", json);
            Assert.Contains("test.png", json); // Will be escaped in JSON
            // Check for formatting (indentation)
            Assert.Contains("\n", json);
            Assert.True(json.Length > 100); // Formatted JSON is longer than compact
        }

        [Fact]
        public async Task ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var service = new AppStateService(_testStateFilePath);
            const int threadCount = 10;
            const int incrementsPerThread = 10;

            // Act
            var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < incrementsPerThread; i++)
                {
                    service.IncrementSuccessCount();
                }
            }));

            await Task.WhenAll(tasks.ToArray());

            var finalState = service.LoadState();

            // Assert
            Assert.Equal(threadCount * incrementsPerThread, finalState.UpdateSuccessCount);
        }
    }
}
