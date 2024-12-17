using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Tests.Converters
{
    public class DynamicControllerBindingConverterTests
    {
        private readonly DynamicControllerBindingConverter _converter = new DynamicControllerBindingConverter();
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        #region Convert Tests

        [Fact]
        public void Convert_ReturnsPropertyValue_WhenValidControllerDataAndPropertyProvided()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftThumbstickX = 5000
            };
            string parameter = "LeftThumbstickX";

            // Act
            var result = _converter.Convert(controllerData, typeof(int), parameter, _culture);

            // Assert
            Assert.Equal(5000.0, result);
        }

        [Fact]
        public void Convert_ReturnsNullValue_WhenPropertyDoesNotExist()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftThumbstickX = 5000
            };
            string parameter = "NonExistentProperty";

            // Act
            var result = _converter.Convert(controllerData, typeof(int), parameter, _culture);

            // Assert
            Assert.Equal(null, result);
        }

        [Fact]
        public void Convert_ReturnsUnsetValue_WhenValueIsNotControllerData()
        {
            // Arrange
            object invalidValue = "invalid";
            string parameter = "LeftThumbstickX";

            // Act
            var result = _converter.Convert(invalidValue, typeof(int), parameter, _culture);

            // Assert
            Assert.Equal(DependencyProperty.UnsetValue, result);
        }

        [Fact]
        public void Convert_ReturnsUnsetValue_WhenParameterIsNotString()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftThumbstickX = 5000
            };
            object invalidParameter = 123;

            // Act
            var result = _converter.Convert(controllerData, typeof(int), invalidParameter, _culture);

            // Assert
            Assert.Equal(DependencyProperty.UnsetValue, result);
        }

        [Fact]
        public void Convert_ReturnsUnsetValue_WhenValueIsNull()
        {
            // Arrange
            object nullValue = null;
            string parameter = "LeftThumbstickX";

            // Act
            var result = _converter.Convert(nullValue, typeof(int), parameter, _culture);

            // Assert
            Assert.Equal(DependencyProperty.UnsetValue, result);
        }

        #endregion

        #region ConvertBack Tests

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            object value = 5000;
            string parameter = "LeftThumbstickX";

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => _converter.ConvertBack(value, typeof(int), parameter, _culture));
        }

        #endregion
    }
}
