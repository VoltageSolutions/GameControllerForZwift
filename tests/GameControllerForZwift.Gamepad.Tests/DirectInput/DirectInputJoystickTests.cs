using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput;
using GameControllerForZwift.Gamepad.USB;
using NSubstitute;
using Xunit;

namespace GameControllerForZwift.Gamepad.Tests.DirectInput
{
    public class DirectInputJoystickTests
    {
        private readonly string TestDeviceName = "Test Device";
        [Fact]
        public void Constructor_ShouldSetDeviceName()
        {
            // Arrange
            var joystick = Substitute.For<IJoystick>();

            // Act
            var directInputJoystick = new DirectInputJoystick(joystick, TestDeviceName);

            // Assert
            Assert.Equal(TestDeviceName, directInputJoystick.Name);
        }

        [Fact]
        public void Initialize_ShouldAcquireJoystick_WhenNotAlreadyAcquired()
        {
            // Arrange
            var joystick = Substitute.For<IJoystick>();
            var directInputJoystick = new DirectInputJoystick(joystick, TestDeviceName);

            // Act
            directInputJoystick.Initialize();

            // Assert
            joystick.Received(1).Acquire();
        }

        [Fact]
        public void Initialize_ShouldNotAcquireJoystick_WhenAlreadyAcquired()
        {
            // Arrange
            var joystick = Substitute.For<IJoystick>();
            var directInputJoystick = new DirectInputJoystick(joystick, TestDeviceName);

            // Act
            directInputJoystick.Initialize();
            directInputJoystick.Initialize();

            // Assert
            joystick.Received(1).Acquire(); // Ensure Acquire is only called once
        }

        [Fact]
        public void ReadData_ShouldAcquireAndPollJoystick_WhenNotAlreadyAcquired()
        {
            // Arrange
            var joystick = Substitute.For<IJoystick>();
            var joystickState = Substitute.For<IJoystickState>();
            joystick.GetCurrentState().Returns(joystickState);

            var directInputJoystick = new DirectInputJoystick(joystick, TestDeviceName);

            // Act
            var result = directInputJoystick.ReadData();

            // Assert
            joystick.Received(1).Acquire();   // Ensure joystick is acquired
            joystick.Received(1).Poll();      // Ensure joystick is polled
        }

        [Fact]
        public void ReadData_ShouldOnlyPoll_WhenAlreadyAcquired()
        {
            // Arrange
            var joystick = Substitute.For<IJoystick>();
            var joystickState = Substitute.For<IJoystickState>();
            joystick.GetCurrentState().Returns(joystickState);

            var directInputJoystick = new DirectInputJoystick(joystick, TestDeviceName);

            // Acquire the joystick
            directInputJoystick.Initialize();

            // Act
            var result = directInputJoystick.ReadData();
            var result2 = directInputJoystick.ReadData();

            // Assert
            joystick.Received(2).Poll();    // Poll matches number of calls
            joystick.Received(1).Acquire(); // Acquire only called once
        }

        [Fact]
        public void ReadData_ShouldReturnControllerData()
        {
            // Arrange
            var joystick = Substitute.For<IJoystick>();
            var joystickState = Substitute.For<IJoystickState>();

            // Set joystick state properties that would be used in AsControllerData
            joystickState.Buttons.Returns(new bool[12]);
            joystickState.Buttons[0] = true;  // A button pressed
            joystickState.X.Returns(12345);    // X-axis value
            joystickState.Y.Returns(67890);    // Y-axis value

            joystick.GetCurrentState().Returns(joystickState);

            // Expected data that ReadData should return based on joystickState properties
            var expectedData = new ControllerData
            {
                A = true,
                LeftThumbstickX = 12345,
                LeftThumbstickY = 67890
            };

            var directInputJoystick = new DirectInputJoystick(joystick, TestDeviceName);

            // Act
            var result = directInputJoystick.ReadData();

            // Assert
            Assert.Equal(expectedData.A, result.A);
            Assert.Equal(expectedData.LeftThumbstickX, result.LeftThumbstickX);
            Assert.Equal(expectedData.LeftThumbstickY, result.LeftThumbstickY);
        }
    }
}
