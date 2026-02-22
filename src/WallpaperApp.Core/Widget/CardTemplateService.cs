using System.Reflection;

namespace WallpaperApp.Widget
{
    /// <summary>
    /// Loads Adaptive Card JSON templates (embedded in this assembly) and
    /// hydrates them with live widget data via simple placeholder substitution.
    /// </summary>
    public class CardTemplateService
    {
        private static readonly Assembly _assembly = typeof(CardTemplateService).Assembly;

        /// <summary>
        /// Loads the raw Adaptive Card JSON template for the given widget size.
        /// </summary>
        /// <param name="size">The widget size to load a template for.</param>
        /// <returns>Raw JSON string with unsubstituted <c>${...}</c> placeholders.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the embedded template resource is missing.</exception>
        public string LoadTemplate(WidgetSize size)
        {
            var sizeName = size.ToString().ToLowerInvariant();
            var resourceName = $"WallpaperSync.{sizeName}-card.json";

            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{resourceName}' not found in {_assembly.GetName().Name}. " +
                    $"Available resources: {string.Join(", ", _assembly.GetManifestResourceNames())}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Substitutes all <c>${placeholder}</c> tokens in the template with values from <paramref name="data"/>.
        /// </summary>
        /// <param name="template">Raw template JSON from <see cref="LoadTemplate"/>.</param>
        /// <param name="data">Live data to inject into the template.</param>
        /// <returns>Hydrated JSON ready to send to the Widget Board.</returns>
        public string HydrateTemplate(string template, WidgetData data)
        {
            return template
                .Replace("${imageUrl}", data.ImageUrl)
                .Replace("${lastUpdated}", data.LastUpdated)
                .Replace("${status}", data.Status);
        }
    }
}
