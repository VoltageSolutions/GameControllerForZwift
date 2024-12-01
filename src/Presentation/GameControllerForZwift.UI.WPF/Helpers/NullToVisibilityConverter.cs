using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Converts a null value to Visibility.Collapsed
/// </summary>
namespace GameControllerForZwift.UI.WPF.Helpers
{
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}