using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Tests.Converters
{
    public class ZwiftFunctionToVisibilityConverterTests
    {
        private readonly ZwiftFunctionToVisibilityConverter _converter = new ZwiftFunctionToVisibilityConverter();
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        #region Enum for ZwiftFunction

        public enum ZwiftFunction
        {
            ShowMenu,
            NavigateLeft,
            NavigateRight,
            Uturn,
            Powerup,
            Select,
            GoBack
        }

        #endregion

        #region Convert Tests

        //[Theory]
        //[InlineData(ZwiftFunction.ShowMenu, "ShowMenu", Visibility.Visible)]
        //[InlineData(ZwiftFunction.NavigateLeft, "NavigateLeft", Visibility.Visible)]
        //[InlineData(ZwiftFunction.NavigateRight, "NavigateLeft", Visibility.Collapsed)]
        //[InlineData(ZwiftFunction.Select, "Select", Visibility.Visible)]
        //[InlineData(ZwiftFunction.GoBack, "Powerup", Visibility.Collapsed)]
        //[InlineData(ZwiftFunction.Powerup, "Powerup", Visibility.Visible)]
        //[InlineData(ZwiftFunction.Uturn, "Uturn", Visibility.Visible)]
        //public void Convert_ReturnsCorrectVisibility(ZwiftFunction selectedFunction, string targetFunctionName, Visibility expectedVisibility)
        //{
        //    // Act
        //    var result = _converter.Convert(selectedFunction, typeof(Visibility), targetFunctionName, _culture);

        //    // Assert
        //    Assert.Equal(expectedVisibility, result);
        //}

        [Fact]
        public void Convert_ReturnsCollapsed_WhenParameterIsInvalid()
        {
            // Arrange
            var selectedFunction = ZwiftFunction.ShowMenu;
            var invalidParameter = "InvalidFunction";

            // Act
            var result = _converter.Convert(selectedFunction, typeof(Visibility), invalidParameter, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ReturnsCollapsed_WhenValueIsNotZWiftFunction()
        {
            // Arrange
            object selectedFunction = "InvalidFunction";
            var targetFunctionName = "ShowMenu";

            // Act
            var result = _converter.Convert(selectedFunction, typeof(Visibility), targetFunctionName, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ReturnsCollapsed_WhenParameterIsNull()
        {
            // Arrange
            var selectedFunction = ZwiftFunction.ShowMenu;

            // Act
            var result = _converter.Convert(selectedFunction, typeof(Visibility), null, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ReturnsCollapsed_WhenValueIsNull()
        {
            // Arrange
            object selectedFunction = null;
            var targetFunctionName = "ShowMenu";

            // Act
            var result = _converter.Convert(selectedFunction, typeof(Visibility), targetFunctionName, _culture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        #endregion

        #region ConvertBack Tests

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Act & Assert
            var exception = Assert.Throws<NotImplementedException>(() => _converter.ConvertBack(Visibility.Visible, typeof(string), null, _culture));
            Assert.Equal("ConvertBack is not supported for FunctionToVisibilityConverter.", exception.Message);
        }

        #endregion
    }
}
