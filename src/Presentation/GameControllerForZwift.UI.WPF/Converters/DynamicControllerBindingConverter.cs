using GameControllerForZwift.Core;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Converters
{
    public class DynamicControllerBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ControllerData controllerData && parameter is string inputName)
            {
                var property = controllerData.GetType().GetProperty(inputName);
                return property?.GetValue(controllerData);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
