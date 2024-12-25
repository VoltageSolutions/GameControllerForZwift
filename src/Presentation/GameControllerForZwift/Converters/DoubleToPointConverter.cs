using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Converters
{
    public class DoubleToPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
                !(values[0] is double x) ||
                !(values[1] is double y))
            {
                return DependencyProperty.UnsetValue;
            }

            return new Point(x, y);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is Point point)
            {
                return new object[] { point.X, point.Y };
            }

            return new object[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }
}
