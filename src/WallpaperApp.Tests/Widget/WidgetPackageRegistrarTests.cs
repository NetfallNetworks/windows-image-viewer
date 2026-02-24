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

            // Create a temporary MSIX file in the widget\ subdirectory so File.Exists passes
            var msixPath = Path.Combine(AppContext.BaseDirectory, WidgetPackageRegistrar.MsixRelativePath);
            var msixDir = Path.GetDirectoryName(msixPath)!;
            var createdDir = false;
            var createdFile = false;

            try
            {
                if (!Directory.Exists(msixDir))
                {
                    Directory.CreateDirectory(msixDir);
                    createdDir = true;
                }
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
                        It.Is<string>(s => s.Contains("WallpaperSync-Identity.msix")),
                        It.Is<string>(s => s.Contains(WidgetPackageRegistrar.WidgetProviderSubdir))),
                    Times.Once);
            }
            finally
            {
                if (createdFile && File.Exists(msixPath))
                {
                    File.Delete(msixPath);
                }
                if (createdDir && Directory.Exists(msixDir))
                {
                    Directory.Delete(msixDir, false);
                }
            }
        }

        [Fact]
        public async Task RegisterIfNeededAsync_RegisterFails_ReturnsFalse()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(It.IsAny<string>()))
                .Returns(false);

            // Create a temporary MSIX file in the widget\ subdirectory
            var msixPath = Path.Combine(AppContext.BaseDirectory, WidgetPackageRegistrar.MsixRelativePath);
            var msixDir = Path.GetDirectoryName(msixPath)!;
            var createdDir = false;
            var createdFile = false;

            try
            {
                if (!Directory.Exists(msixDir))
                {
                    Directory.CreateDirectory(msixDir);
                    createdDir = true;
                }
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
                if (createdDir && Directory.Exists(msixDir))
                {
                    Directory.Delete(msixDir, false);
                }
            }
        }

        [Fact]
        public async Task ForceReregisterAsync_AlreadyRegistered_RemovesThenRegisters()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .Returns(true);
            _mockPackageManager
                .Setup(pm => pm.RemovePackageAsync(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .ReturnsAsync(true);

            // Create a temporary MSIX file so File.Exists passes
            var msixPath = Path.Combine(AppContext.BaseDirectory, WidgetPackageRegistrar.MsixRelativePath);
            var msixDir = Path.GetDirectoryName(msixPath)!;
            var createdDir = false;
            var createdFile = false;

            try
            {
                if (!Directory.Exists(msixDir))
                {
                    Directory.CreateDirectory(msixDir);
                    createdDir = true;
                }
                if (!File.Exists(msixPath))
                {
                    File.WriteAllBytes(msixPath, new byte[] { 0 });
                    createdFile = true;
                }

                _mockPackageManager
                    .Setup(pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(true);

                var result = await _registrar.ForceReregisterAsync();

                Assert.True(result);
                _mockPackageManager.Verify(
                    pm => pm.RemovePackageAsync(WidgetPackageRegistrar.PackageFamilyNamePrefix),
                    Times.Once);
                _mockPackageManager.Verify(
                    pm => pm.RegisterSparsePackageAsync(
                        It.Is<string>(s => s.Contains("WallpaperSync-Identity.msix")),
                        It.Is<string>(s => s.Contains(WidgetPackageRegistrar.WidgetProviderSubdir))),
                    Times.Once);
            }
            finally
            {
                if (createdFile && File.Exists(msixPath))
                {
                    File.Delete(msixPath);
                }
                if (createdDir && Directory.Exists(msixDir))
                {
                    Directory.Delete(msixDir, false);
                }
            }
        }

        [Fact]
        public async Task ForceReregisterAsync_NotRegistered_SkipsRemoveAndRegisters()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .Returns(false);

            // Create a temporary MSIX file so File.Exists passes
            var msixPath = Path.Combine(AppContext.BaseDirectory, WidgetPackageRegistrar.MsixRelativePath);
            var msixDir = Path.GetDirectoryName(msixPath)!;
            var createdDir = false;
            var createdFile = false;

            try
            {
                if (!Directory.Exists(msixDir))
                {
                    Directory.CreateDirectory(msixDir);
                    createdDir = true;
                }
                if (!File.Exists(msixPath))
                {
                    File.WriteAllBytes(msixPath, new byte[] { 0 });
                    createdFile = true;
                }

                _mockPackageManager
                    .Setup(pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(true);

                var result = await _registrar.ForceReregisterAsync();

                Assert.True(result);
                _mockPackageManager.Verify(
                    pm => pm.RemovePackageAsync(It.IsAny<string>()),
                    Times.Never);
                _mockPackageManager.Verify(
                    pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once);
            }
            finally
            {
                if (createdFile && File.Exists(msixPath))
                {
                    File.Delete(msixPath);
                }
                if (createdDir && Directory.Exists(msixDir))
                {
                    Directory.Delete(msixDir, false);
                }
            }
        }

        [Fact]
        public async Task ForceReregisterAsync_RemoveFails_ReturnsFalse()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .Returns(true);
            _mockPackageManager
                .Setup(pm => pm.RemovePackageAsync(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .ReturnsAsync(false);

            var result = await _registrar.ForceReregisterAsync();

            Assert.False(result);
            _mockPackageManager.Verify(
                pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ForceReregisterAsync_MsixNotFound_ReturnsFalse()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .Returns(true);
            _mockPackageManager
                .Setup(pm => pm.RemovePackageAsync(WidgetPackageRegistrar.PackageFamilyNamePrefix))
                .ReturnsAsync(true);

            // No MSIX file on disk — AppContext.BaseDirectory won't contain one in tests
            var result = await _registrar.ForceReregisterAsync();

            Assert.False(result);
            _mockPackageManager.Verify(
                pm => pm.RegisterSparsePackageAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ForceReregisterAsync_Throws_ReturnsFalse()
        {
            _mockPackageManager
                .Setup(pm => pm.IsPackageRegistered(It.IsAny<string>()))
                .Throws(new InvalidOperationException("WinRT not available"));

            var result = await _registrar.ForceReregisterAsync();

            Assert.False(result);
        }
    }
}
