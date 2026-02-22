using WallpaperApp.Widget;
using Xunit;

namespace WallpaperApp.Tests.Widget
{
    public class CardTemplateServiceTests
    {
        private readonly CardTemplateService _service = new();

        [Fact]
        public void LoadTemplate_Small_ReturnsNonEmptyJson()
        {
            var template = _service.LoadTemplate(WidgetSize.Small);

            Assert.NotEmpty(template);
            Assert.Contains("AdaptiveCard", template);
        }

        [Fact]
        public void LoadTemplate_Medium_ReturnsNonEmptyJson()
        {
            var template = _service.LoadTemplate(WidgetSize.Medium);

            Assert.NotEmpty(template);
            Assert.Contains("AdaptiveCard", template);
        }

        [Fact]
        public void LoadTemplate_Large_ReturnsNonEmptyJson()
        {
            var template = _service.LoadTemplate(WidgetSize.Large);

            Assert.NotEmpty(template);
            Assert.Contains("AdaptiveCard", template);
        }

        [Fact]
        public void HydrateTemplate_SubstitutesImageUrl()
        {
            var template = _service.LoadTemplate(WidgetSize.Medium);
            var data = new WidgetData(
                ImageUrl: "https://example.com/photo.jpg",
                LastUpdated: "Mon 14:23",
                Status: "Active",
                HasImage: true);

            var result = _service.HydrateTemplate(template, data);

            Assert.Contains("https://example.com/photo.jpg", result);
            Assert.DoesNotContain("${imageUrl}", result);
        }

        [Fact]
        public void HydrateTemplate_SubstitutesLastUpdated()
        {
            var template = _service.LoadTemplate(WidgetSize.Medium);
            var data = new WidgetData(
                ImageUrl: "https://example.com/photo.jpg",
                LastUpdated: "Mon 14:23",
                Status: "Active",
                HasImage: true);

            var result = _service.HydrateTemplate(template, data);

            Assert.Contains("Mon 14:23", result);
            Assert.DoesNotContain("${lastUpdated}", result);
        }

        [Fact]
        public void HydrateTemplate_LocalFileMode_ProducesValidCard()
        {
            var template = _service.LoadTemplate(WidgetSize.Large);
            var data = new WidgetData(
                ImageUrl: "https://via.placeholder.com/400x225?text=Local+File+Mode",
                LastUpdated: "Never",
                Status: "Active",
                HasImage: false);

            var result = _service.HydrateTemplate(template, data);

            Assert.NotEmpty(result);
            Assert.Contains("AdaptiveCard", result);
            Assert.DoesNotContain("${imageUrl}", result);
            Assert.DoesNotContain("${lastUpdated}", result);
            Assert.DoesNotContain("${status}", result);
        }
    }
}
