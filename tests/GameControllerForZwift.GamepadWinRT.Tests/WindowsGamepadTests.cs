using GameControllerForZwift.GamepadWinRT;
using NSubstitute;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT.Tests
{
    public class WindowsGamepadTests
    {
        [Fact]
        public void Constructor_SetsDefaultSpecifications()
        {
            // Arrange & Act
            var mockGamepad1 = Substitute.For<IGameController>();
            var gamepad = new WindowsGamepad(mockGamepad1);

            // Assert
            Assert.NotNull(gamepad.Specifications);
            Assert.Equal("XInput Controller", gamepad.Specifications.Name);
        }
    }
}
