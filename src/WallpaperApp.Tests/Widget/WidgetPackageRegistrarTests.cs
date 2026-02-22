using Moq;
using WallpaperApp.Widget;
using Xunit;

namespace WallpaperApp.Tests.Widget
{
    public class WidgetPackageRegistrarTests
    {
        private readonly Mock<IPackageManagerAdapter> _mockPackageManager;
        private readonly WidgetPackageRegistrar _registrar;

        public WidgetPackageRegistrarTests()
        {
            _mockPackageManager = new Mock<IPackageManagerAdapter>();
            _registrar = new WidgetPackageRegistrar(_mockPackageManager.Object);
        }

        [Fact]
        public async Task RegisterIfNeededAsync_AlreadyRegistered_SkipsRegistration()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .Returns(true);

            var result = await _registrar.RegisterIfNeededAsync();

            Assert.True(result);
            _mockPackageManager.Verify(
                pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterIfNeededAsync_MsixNotFound_LogsErrorGracefully()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(It.IsAny<string>()))
                .Returns(false);

            // AppContext.BaseDirectory likely does not contain the MSIX file in test
            var result = await _registrar.RegisterIfNeededAsync();

            // Should return false (MSIX not found) but not throw
            Assert.False(result);
            _mockPackageManager.Verify(
                pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterIfNeededAsync_PackageManagerThrows_ReturnsFalse()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(It.IsAny<string>()))
                .Throws(new InvalidOperationException("WinRT not available"));

            var result = await _registrar.RegisterIfNeededAsync();

            Assert.False(result);
        }

        [Fact]
        public void Constructor_NullPackageManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WidgetPackageRegistrar(null!));
        }

        [Fact]
        public async Task RegisterIfNeededAsync_NotRegistered_WithMsix_CallsRegister()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(It.IsAny<string>()))
                .Returns(false);

            // Create a temporary MSIX file so the File.Exists check passes
            var msixPath = Path.Combine(AppContext.BaseDirectory, WidgetPackageRegistrar.MsixFileName);
            var createdFile = false;

            try
            {
                if (!File.Exists(msixPath))
                {
                    File.WriteAllBytes(msixPath, new byte[] { 0 });
                    createdFile = true;
                }

                _mockPackageManager
                    .Setup(pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(true);

                var result = await _registrar.RegisterIfNeededAsync();

                Assert.True(result);
                _mockPackageManager.Verify(
                    pm => pm.RegisterSparsePackageAsync(
                        It.Is<string>(s => s.Contains(WidgetPackageRegistrar.MsixFileName)),
                        It.IsAny<string>()),
                    Times.Once);
            }
            finally
            {
                if (createdFile && File.Exists(msixPath))
                {
                    File.Delete(msixPath);
                }
            }
        }

        [Fact]
        public async Task RegisterIfNeededAsync_RegisterFails_ReturnsFalse()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(It.IsAny<string>()))
                .Returns(false);

            // Create a temporary MSIX file
            var msixPath = Path.Combine(AppContext.BaseDirectory, WidgetPackageRegistrar.MsixFileName);
            var createdFile = false;

            try
            {
                if (!File.Exists(msixPath))
                {
                    File.WriteAllBytes(msixPath, new byte[] { 0 });
                    createdFile = true;
                }

                _mockPackageManager
                    .Setup(pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(false);

                var result = await _registrar.RegisterIfNeededAsync();

                Assert.False(result);
            }
            finally
            {
                if (createdFile && File.Exists(msixPath))
                {
                    File.Delete(msixPath);
                }
            }
        }
    }
}
