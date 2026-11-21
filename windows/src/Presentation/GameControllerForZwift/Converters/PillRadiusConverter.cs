using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Converters
{
    /// <summary>
    /// Turns a height into a corner radius of half that height, so buttons, chips, and keycaps are pills at any size
    /// (UX-V-016).
    /// </summary>
    public sealed class PillRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is double height && height > 0 && !double.IsInfinity(height) ? new CornerRadius(height / 2) : new CornerRadius(0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
