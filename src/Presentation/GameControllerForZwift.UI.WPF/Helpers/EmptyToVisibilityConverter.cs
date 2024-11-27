using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Converts an empty string to Visibility.Collapsed
/// </summary>
namespace GameControllerForZwift.UI.WPF.Helpers
{
    public sealed class EmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }

            return value is null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}