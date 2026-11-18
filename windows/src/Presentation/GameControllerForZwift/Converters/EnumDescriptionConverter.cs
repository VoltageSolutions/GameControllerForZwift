using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        private static string GetEnumDescription(Enum? enumObj)
        {
            if (enumObj is null)
            {
                return string.Empty;
            }

            string name = enumObj.ToString();
            FieldInfo? fieldInfo = enumObj.GetType().GetField(name);
            var description = fieldInfo?.GetCustomAttributes(false).OfType<DescriptionAttribute>().FirstOrDefault();
            return description?.Description ?? name;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum? myEnum = (Enum?)value;
            string description = GetEnumDescription(myEnum);
            return description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}



