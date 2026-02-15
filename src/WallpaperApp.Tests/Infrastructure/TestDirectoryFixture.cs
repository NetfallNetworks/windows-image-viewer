namespace WallpaperApp.Tests.Infrastructure
{
    /// <summary>
    /// Provides a temporary test directory with automatic cleanup.
    /// Handles directory switching and restoration for tests that need to manipulate the current directory.
    /// </summary>
    public class TestDirectoryFixture : IDisposable
    {
        private readonly string _originalDirectory;
        private readonly string _testDirectory;

        /// <summary>
        /// Creates a new test directory and switches to it.
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite (used in directory path)</param>
        public TestDirectoryFixture(string testSuiteName)
        {
            _originalDirectory = Directory.GetCurrentDirectory();
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                "WallpaperAppTests",
                testSuiteName,
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(_testDirectory);
            Directory.SetCurrentDirectory(_testDirectory);
        }

        /// <summary>
        /// Gets the path to the temporary test directory.
        /// </summary>
        public string TestDirectory => _testDirectory;

        /// <summary>
        /// Gets the original working directory before the test.
        /// </summary>
        public string OriginalDirectory => _originalDirectory;

        /// <summary>
        /// Restores the original directory and cleans up the test directory.
        /// </summary>
        public void Dispose()
        {
            // Restore original directory with fallback logic
            try
            {
                if (Directory.Exists(_originalDirectory))
                {
                    Directory.SetCurrentDirectory(_originalDirectory);
                }
            }
            catch (Exception)
            {
                // If we can't change back to original, try temp directory as fallback
                try
                {
                    Directory.SetCurrentDirectory(Path.GetTempPath());
                }
                catch
                {
                    // Ignore - we tried our best
                }
            }

            // Delete test directory with error handling
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, recursive: true);
                }

                // Also clean up parent directory if empty
                string? parentDir = Path.GetDirectoryName(_testDirectory);
                if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                {
                    try
                    {
                        Directory.Delete(parentDir, recursive: false);
                    }
                    catch
                    {
                        // Parent directory not empty or in use - that's fine
                    }
                }
            }
            catch (Exception)
            {
                // Cleanup failures are not critical - ignore them
            }
        }
    }
}
