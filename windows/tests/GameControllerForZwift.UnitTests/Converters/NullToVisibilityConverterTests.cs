using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Tests.Converters
{
    public class NullToVisibilityConverterTests
    {
        private readonly NullToVisibilityConverter _converter = new();

        [Fact]
        public void Convert_NullIsCollapsed_ValueIsVisible()
        {
            Assert.Equal(Visibility.Collapsed, _converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Visible, _converter.Convert("x", typeof(Visibility), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Convert_Inverted_NullIsVisible_ValueIsCollapsed()
        {
            Assert.Equal(Visibility.Visible, _converter.Convert(null, typeof(Visibility), "Invert", CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, _converter.Convert("x", typeof(Visibility), "invert", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ConvertBack_IsNotSupported()
            => Assert.Throws<NotImplementedException>(() => _converter.ConvertBack(Visibility.Visible, typeof(object), null, CultureInfo.InvariantCulture));
    }
}
