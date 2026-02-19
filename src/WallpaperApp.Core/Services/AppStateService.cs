using System.Text.Json;
using WallpaperApp.Models;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for persisting application runtime state to JSON file.
    /// State is stored in %LOCALAPPDATA%\WallpaperSync\state.json
    /// </summary>
    public class AppStateService : IAppStateService
    {
        private readonly string _stateFilePath;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of AppStateService.
        /// Creates state directory if it doesn't exist.
        /// </summary>
        public AppStateService()
        {
            Directory.CreateDirectory(AppPaths.BaseDirectory);
            _stateFilePath = AppPaths.StateFile;
        }

        /// <summary>
        /// For testing: allows overriding the state file path.
        /// </summary>
        public AppStateService(string stateFilePath)
        {
            _stateFilePath = stateFilePath;
            string? directory = Path.GetDirectoryName(stateFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Loads the application state from disk.
        /// Returns default state if file doesn't exist or is corrupt.
        /// </summary>
        public AppState LoadState()
        {
            lock (_lockObject)
            {
                if (!File.Exists(_stateFilePath))
                    return new AppState(); // Default state

                try
                {
                    string json = File.ReadAllText(_stateFilePath);
                    return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Failed to load state, using defaults: {ex.Message}");
                    return new AppState();
                }
            }
        }

        /// <summary>
        /// Saves the application state to disk.
        /// JSON is formatted for human readability.
        /// </summary>
        public void SaveState(AppState state)
        {
            lock (_lockObject)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(state, options);
                    File.WriteAllText(_stateFilePath, json);
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Failed to save state: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Updates the last-known-good image path and last update time.
        /// </summary>
        public void UpdateLastKnownGood(string imagePath)
        {
            lock (_lockObject)
            {
                var state = LoadStateInternal();
                state.LastKnownGoodImagePath = imagePath;
                state.LastUpdateTime = DateTime.Now;
                SaveStateInternal(state);
            }
        }

        /// <summary>
        /// Updates the enabled/disabled state.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            lock (_lockObject)
            {
                var state = LoadStateInternal();
                state.IsEnabled = enabled;
                SaveStateInternal(state);
            }
        }

        /// <summary>
        /// Marks the first run as complete.
        /// </summary>
        public void MarkFirstRunComplete()
        {
            lock (_lockObject)
            {
                var state = LoadStateInternal();
                state.IsFirstRun = false;
                SaveStateInternal(state);
            }
        }

        /// <summary>
        /// Increments the success count and updates last update time.
        /// </summary>
        public void IncrementSuccessCount()
        {
            lock (_lockObject)
            {
                var state = LoadStateInternal();
                state.UpdateSuccessCount++;
                state.LastUpdateTime = DateTime.Now;
                SaveStateInternal(state);
            }
        }

        /// <summary>
        /// Increments the failure count.
        /// </summary>
        public void IncrementFailureCount()
        {
            lock (_lockObject)
            {
                var state = LoadStateInternal();
                state.UpdateFailureCount++;
                SaveStateInternal(state);
            }
        }

        /// <summary>
        /// Internal load without lock (for use within locked methods).
        /// </summary>
        private AppState LoadStateInternal()
        {
            if (!File.Exists(_stateFilePath))
                return new AppState();

            try
            {
                string json = File.ReadAllText(_stateFilePath);
                return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Failed to load state, using defaults: {ex.Message}");
                return new AppState();
            }
        }

        /// <summary>
        /// Internal save without lock (for use within locked methods).
        /// </summary>
        private void SaveStateInternal(AppState state)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Failed to save state: {ex.Message}");
            }
        }
    }
}
