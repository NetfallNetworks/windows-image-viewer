using Xunit;

namespace WallpaperApp.Tests
{
    /// <summary>
    /// Collection definition for tests that change the current directory.
    /// These tests must run sequentially to avoid interference.
    /// </summary>
    [CollectionDefinition("CurrentDirectory Tests", DisableParallelization = true)]
    public class CurrentDirectoryTestCollection
    {
    }
}
