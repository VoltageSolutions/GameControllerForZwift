using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Tests.Converters
{
    public class EmptyToVisibilityConverterTests
    {
        private readonly EmptyToVisibilityConverter _converter = new EmptyToVisibilityConverter();
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        #region Convert Tests

        [Fact]
        public void Convert_ReturnsCollapsed_WhenStringIsNullOrWhiteSpace()
        {
            // Arrange
            string? value = null;

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ReturnsCollapsed_WhenStringIsEmpty()
        {
            // Arrange
            string value = string.Empty;

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ReturnsCollapsed_WhenStringIsWhitespace()
        {
            // Arrange
            string value = "   "; // Whitespace string

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ReturnsVisible_WhenStringIsNotEmpty()
        {
            // Arrange
            string value = "Some content";

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void Convert_ReturnsVisible_WhenValueIsNonStringObject()
        {
            // Arrange
            object value = 123; // Non-string object

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void Convert_ReturnsCollapsed_WhenValueIsNull()
        {
            // Arrange
            object? value = null;

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        #endregion

        #region ConvertBack Tests

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            object value = "Some content";

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => _converter.ConvertBack(value, typeof(string), null, _culture));
        }

        #endregion
    }
}
