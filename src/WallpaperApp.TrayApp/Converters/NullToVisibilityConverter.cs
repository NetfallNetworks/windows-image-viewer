using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WallpaperApp.TrayApp.Converters
{
    /// <summary>
    /// Converts null/empty strings to Visibility (null/empty = Collapsed, has value = Visible).
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (value is string str)
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
