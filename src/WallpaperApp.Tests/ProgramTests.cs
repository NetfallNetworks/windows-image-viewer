using WallpaperApp;
using WallpaperApp.Tests.Infrastructure;
using Xunit;

namespace WallpaperApp.Tests
{
    [Collection("CurrentDirectory Tests")]
    public class ProgramTests : IDisposable
    {
        private readonly TestDirectoryFixture _fixture;

        public ProgramTests()
        {
            _fixture = new TestDirectoryFixture("ProgramTests");
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public void ApplicationDisplaysHelp_HelpFlag()
        {
            // Arrange & Act
            var exitCode = Program.Main(new[] { "--help" });

            // Assert
            Assert.Equal(0, exitCode);
        }
    }
}
