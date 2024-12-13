using GameControllerForZwift.Core;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Converters
{
    public class ZwiftFunctionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure input value and parameter are not null
            if (value is ZwiftFunction selectedFunction && parameter is string targetFunctionName)
            {
                try
                {
                    // Parse the parameter to match the ZwiftFunction enum
                    var targetFunction = Enum.Parse<ZwiftFunction>(targetFunctionName);

                    // Return Visible if the selected function matches the target function
                    return selectedFunction == targetFunction ? Visibility.Visible : Visibility.Collapsed;
                }
                catch (ArgumentException)
                {
                    // Handle cases where the parameter does not match any enum value
                    return Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not supported for FunctionToVisibilityConverter.");
        }
    }
}
