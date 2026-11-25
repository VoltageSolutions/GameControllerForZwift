using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GameControllerForZwift.UI.WPF.Converters
{
    /// <summary>
    /// Turns how far a trigger is pulled (0 to 1) and the width of its chip into a clip rectangle over the filled part of the chip,
    /// so a full pill is revealed by a straight edge that moves from left to right (SCR-BTN-041).
    /// </summary>
    public sealed class PullFillConverter : IMultiValueConverter
    {
        private const double ChipHeight = 30;

        public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Length < 2 || values[0] is not double ratio || values[1] is not double width || double.IsNaN(width))
                return DependencyProperty.UnsetValue;

            double filled = Math.Max(0, width) * Math.Clamp(ratio, 0.0, 1.0);
            return new RectangleGeometry(new Rect(0, 0, filled, ChipHeight));
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
