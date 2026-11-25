using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace GameControllerForZwift.UI.WPF.Tests.Converters
{
    public class PullFillConverterTests
    {
        private readonly PullFillConverter _converter = new();

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(0.5, 30.0)]
        [InlineData(1.0, 60.0)]
        [InlineData(2.0, 60.0)]
        [InlineData(-1.0, 0.0)]
        public void Convert_GivesTheFilledWidth(double ratio, double expected)
        {
            object result = _converter.Convert([ratio, 60.0], typeof(double), null, CultureInfo.InvariantCulture);

            Assert.Equal(expected, (double)result, 6);
        }

        [Fact]
        public void Convert_GivesTheFilledPartAsAClipForAGeometry()
        {
            object result = _converter.Convert([0.25, 80.0], typeof(Geometry), null, CultureInfo.InvariantCulture);

            Rect bounds = ((RectangleGeometry)result).Rect;
            Assert.Equal(0, bounds.X);
            Assert.Equal(20.0, bounds.Width, 6);
        }

        [Fact]
        public void Convert_IsUnsetForMissingValues()
        {
            Assert.Same(DependencyProperty.UnsetValue, _converter.Convert([0.5], typeof(double), null, CultureInfo.InvariantCulture));
            Assert.Same(DependencyProperty.UnsetValue, _converter.Convert([0.5, double.NaN], typeof(double), null, CultureInfo.InvariantCulture));
        }
    }
}
