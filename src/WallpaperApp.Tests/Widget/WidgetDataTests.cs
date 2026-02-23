using WallpaperApp.Configuration;
using WallpaperApp.Models;
using WallpaperApp.Widget;
using Xunit;

namespace WallpaperApp.Tests.Widget
{
    public class WidgetDataTests
    {
        [Fact]
        public void From_UrlMode_UsesImageUrl()
        {
            var settings = new AppSettings
            {
                SourceType = ImageSource.Url,
                ImageUrl = "https://example.com/wallpaper.jpg"
            };
            var state = new AppState { IsEnabled = true };

            var data = WidgetData.From(settings, state);

            Assert.Equal("https://example.com/wallpaper.jpg", data.ImageUrl);
        }

        [Fact]
        public void From_LocalFileMode_UsesPlaceholderUrl()
        {
            var settings = new AppSettings
            {
                SourceType = ImageSource.LocalFile,
                LocalImagePath = @"C:\Photos\image.png"
            };
            var state = new AppState { IsEnabled = true };

            var data = WidgetData.From(settings, state);

            Assert.Equal(WidgetData.LocalFilePlaceholderUrl, data.ImageUrl);
            Assert.DoesNotContain("file://", data.ImageUrl);
            Assert.Contains("placeholder", data.ImageUrl, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void From_StateDisabled_ShowsPausedStatus()
        {
            var settings = new AppSettings { ImageUrl = "https://example.com/img.jpg" };
            var state = new AppState { IsEnabled = false };

            var data = WidgetData.From(settings, state);

            Assert.Equal("Paused", data.Status);
        }

        [Fact]
        public void From_StateEnabled_ShowsActiveStatus()
        {
            var settings = new AppSettings { ImageUrl = "https://example.com/img.jpg" };
            var state = new AppState { IsEnabled = true };

            var data = WidgetData.From(settings, state);

            Assert.Equal("Active", data.Status);
        }

        [Fact]
        public void From_NeverUpdated_ShowsNeverString()
        {
            var settings = new AppSettings { ImageUrl = "https://example.com/img.jpg" };
            var state = new AppState { LastUpdateTime = null };

            var data = WidgetData.From(settings, state);

            Assert.Equal("Never", data.LastUpdated);
        }

        [Fact]
        public void From_LastUpdateTime_FormatsCorrectly()
        {
            var settings = new AppSettings { ImageUrl = "https://example.com/img.jpg" };
            var updateTime = new DateTime(2026, 2, 23, 14, 30, 0);
            var state = new AppState { LastUpdateTime = updateTime };

            var data = WidgetData.From(settings, state);

            // Format is "ddd HH:mm" — e.g., "Mon 14:30"
            var expected = updateTime.ToString("ddd HH:mm");
            Assert.Equal(expected, data.LastUpdated);
        }

        [Fact]
        public void From_UrlModeWithImageUrl_HasImageTrue()
        {
            var settings = new AppSettings
            {
                SourceType = ImageSource.Url,
                ImageUrl = "https://example.com/img.jpg"
            };
            var state = new AppState();

            var data = WidgetData.From(settings, state);

            Assert.True(data.HasImage);
        }

        [Fact]
        public void From_UrlModeWithEmptyUrl_HasImageFalse()
        {
            var settings = new AppSettings
            {
                SourceType = ImageSource.Url,
                ImageUrl = ""
            };
            var state = new AppState();

            var data = WidgetData.From(settings, state);

            Assert.False(data.HasImage);
        }

        [Fact]
        public void From_LocalFileMode_HasImageTrue()
        {
            var settings = new AppSettings
            {
                SourceType = ImageSource.LocalFile,
                LocalImagePath = @"C:\Photos\image.png"
            };
            var state = new AppState();

            var data = WidgetData.From(settings, state);

            Assert.True(data.HasImage);
        }

        [Fact]
        public void From_DefaultSettings_ReturnsValidData()
        {
            var settings = new AppSettings();
            var state = new AppState();

            var data = WidgetData.From(settings, state);

            Assert.NotNull(data);
            Assert.NotNull(data.ImageUrl);
            Assert.NotNull(data.LastUpdated);
            Assert.NotNull(data.Status);
        }
    }
}
