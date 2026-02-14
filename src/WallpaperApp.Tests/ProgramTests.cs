using WallpaperApp;
using Xunit;

namespace WallpaperApp.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void ApplicationStartsSuccessfully()
        {
            // Arrange & Act
            var exitCode = Program.Main(new string[] { });

            // Assert
            Assert.Equal(0, exitCode);
        }
    }
}
