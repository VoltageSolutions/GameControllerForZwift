using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Tests.Converters
{
    public class DoubleToPointConverterTests
    {
        private readonly DoubleToPointConverter _converter = new DoubleToPointConverter();
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        #region Convert Tests

        [Fact]
        public void Convert_ReturnsPoint_WhenValidDoublesProvided()
        {
            // Arrange
            object[] values = { 10.5, 20.3 };

            // Act
            var result = _converter.Convert(values, typeof(Point), null, _culture);

            // Assert
            Assert.IsType<Point>(result);
            var point = (Point)result;
            Assert.Equal(10.5, point.X);
            Assert.Equal(20.3, point.Y);
        }

        [Theory]
        [InlineData(null, 20.3)]
        [InlineData(10.5, null)]
        [InlineData("invalid", 20.3)]
        [InlineData(10.5, "invalid")]
        [InlineData(10.5)]
        public void Convert_ReturnsUnsetValue_WhenInvalidInputsProvided(params object[] values)
        {
            // Act
            var result = _converter.Convert(values, typeof(Point), null, _culture);

            // Assert
            Assert.Equal(DependencyProperty.UnsetValue, result);
        }

        [Fact]
        public void Convert_ReturnsUnsetValue_WhenLessThanTwoValuesProvided()
        {
            // Arrange
            object[] values = { 10.5 };

            // Act
            var result = _converter.Convert(values, typeof(Point), null, _culture);

            // Assert
            Assert.Equal(DependencyProperty.UnsetValue, result);
        }

        #endregion

        #region ConvertBack Tests

        [Fact]
        public void ConvertBack_ReturnsDoublesArray_WhenPointProvided()
        {
            // Arrange
            var point = new Point(15.7, 25.4);

            // Act
            var result = _converter.ConvertBack(point, new Type[] { typeof(double), typeof(double) }, null, _culture);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(15.7, result[0]);
            Assert.Equal(25.4, result[1]);
        }

        [Fact]
        public void ConvertBack_ReturnsUnsetValues_WhenInvalidValueProvided()
        {
            // Arrange
            object invalidValue = "invalid";

            // Act
            var result = _converter.ConvertBack(invalidValue, new Type[] { typeof(double), typeof(double) }, null, _culture);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(DependencyProperty.UnsetValue, result[0]);
            Assert.Equal(DependencyProperty.UnsetValue, result[1]);
        }

        #endregion
    }
}
