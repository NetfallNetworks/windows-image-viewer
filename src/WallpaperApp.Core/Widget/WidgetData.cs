namespace WallpaperApp.Widget
{
    /// <summary>
    /// Data object bound to Adaptive Card templates for widget display.
    /// All string properties are safe for JSON template substitution.
    /// </summary>
    public record WidgetData(
        string ImageUrl,
        string LastUpdated,
        string Status,
        bool HasImage
    );
}
