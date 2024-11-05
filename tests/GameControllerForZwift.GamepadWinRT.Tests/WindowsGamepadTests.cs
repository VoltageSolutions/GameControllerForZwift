using GameControllerForZwift.Core;
using GameControllerForZwift.GamepadWinRT;
using NSubstitute;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT.Tests
{
    public class WindowsGamepadTests
    {
        [Fact]
        public void Constructor_InitializesWithCorrectName()
        {
            // Arrange
            var mockGameController = Substitute.For<IGameController>();

            // Act
            var gamepad = new WindowsGamepad(mockGameController);

            // Assert
            Assert.Equal("XInput Controller", gamepad.Name);
        }

        // Cannot test pulling data from controllers without real hardware - there is no available constructor for Gamepad
        //[Fact]
        //public void ReadData_ReturnsCorrectControllerData()
        //{
        //    // Arrange
        //    var mockGamepad = Substitute.For<Gamepad>();
        //    var reading = new GamepadReading
        //    {
        //        Buttons = GamepadButtons.A | GamepadButtons.B,
        //        LeftThumbstickX = 0.1,
        //        LeftThumbstickY = 0.2,
        //        LeftTrigger = 0.3,
        //        RightThumbstickX = 0.4,
        //        RightThumbstickY = 0.5,
        //        RightTrigger = 0.6,
        //        Timestamp = 1633025800
        //    };
        //    mockGamepad.GetCurrentReading().Returns(reading);

        //    var mockGameController = (IGameController)mockGamepad;
        //    var gamepad = new WindowsGamepad(mockGameController);

        //    // Act
        //    var data = gamepad.ReadData();

        //    // Assert
        //    Assert.Equal(ControllerButtons.A | ControllerButtons.B, data.Buttons);
        //    Assert.Equal(0.1, data.LeftThumbstickX);
        //    Assert.Equal(0.2, data.LeftThumbstickY);
        //    Assert.Equal(0.3, data.LeftTrigger);
        //    Assert.Equal(0.4, data.RightThumbstickX);
        //    Assert.Equal(0.5, data.RightThumbstickY);
        //    Assert.Equal(0.6, data.RightTrigger);
        //    Assert.Equal(new DateTime(2021, 9, 30, 18, 16, 40, DateTimeKind.Utc), data.Timestamp);
        //}
    }
}
