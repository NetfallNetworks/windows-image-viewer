using System;
using System.Globalization;
using System.Windows.Data;
using WallpaperApp.Models;

namespace WallpaperApp.TrayApp.Converters
{
    /// <summary>
    /// Converts WallpaperFitMode enum to display string.
    /// </summary>
    public class FitModeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WallpaperFitMode fitMode)
            {
                return fitMode switch
                {
                    WallpaperFitMode.Fill => "Fill",
                    WallpaperFitMode.Fit => "Fit",
                    WallpaperFitMode.Stretch => "Stretch",
                    WallpaperFitMode.Tile => "Tile",
                    WallpaperFitMode.Center => "Center",
                    _ => value.ToString() ?? string.Empty
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str switch
                {
                    "Fill" => WallpaperFitMode.Fill,
                    "Fit" => WallpaperFitMode.Fit,
                    "Stretch" => WallpaperFitMode.Stretch,
                    "Tile" => WallpaperFitMode.Tile,
                    "Center" => WallpaperFitMode.Center,
                    _ => WallpaperFitMode.Fill
                };
            }
            return WallpaperFitMode.Fill;
        }
    }
}
