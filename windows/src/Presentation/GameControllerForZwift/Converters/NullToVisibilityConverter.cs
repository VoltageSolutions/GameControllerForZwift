using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Converters
{
    /// <summary>
    /// Converts a null value to Visibility.Collapsed and any other value to Visibility.Visible. With the parameter "Invert",
    /// null is Visible and any other value is Collapsed.
    /// </summary>
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            return (value is null) != invert ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}