using GameControllerForZwift.Gamepad.DirectInput;
using GameControllerForZwift.Gamepad.USB;
using NSubstitute;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.Tests.DirectInput
{
    public class SharpDXExtensionMethodsTests
    {
        [Fact]
        public void AsControllers_ShouldThrowArgumentNullException_WhenGamepadsIsNull()
        {
            // Arrange
            Func<DeviceInstance, IJoystick> joystickFactory = _ => Substitute.For<IJoystick>();
            var deviceLookup = Substitute.For<IDeviceLookup>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ((IList<DeviceInstance>)null).AsControllers(joystickFactory, deviceLookup));
        }

        [Fact]
        public void AsControllers_ShouldThrowArgumentNullException_WhenDeviceLookupIsNull()
        {
            // Arrange
            Func<DeviceInstance, IJoystick> joystickFactory = _ => Substitute.For<IJoystick>();
            var deviceLookup = Substitute.For<IDeviceLookup>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ((IList<DeviceInstance>)null).AsControllers(joystickFactory, null));
        }

        [Fact]
        public void AsControllers_ShouldReturnIControllerList_WhenParametersAreValid()
        {
            // Arrange
            var gamepads = new List<DeviceInstance>
            {
                new DeviceInstance(),
                new DeviceInstance()
            };

            // Create a joystick factory that returns a mock IJoystick
            Func<DeviceInstance, IJoystick> joystickFactory = _ => Substitute.For<IJoystick>();
            var deviceLookup = Substitute.For<IDeviceLookup>();

            // Act
            var controllers = gamepads.AsControllers(joystickFactory, deviceLookup).ToList();

            // Assert
            Assert.Equal(2, controllers.Count);
            Assert.IsType<DirectInputJoystick>(controllers[0]);
        }


        [Fact]
        public void AsControllerData_ShouldThrowArgumentNullException_WhenStateIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ((IJoystickState)null).AsControllerData());
        }

        [Fact]
        public void AsControllerData_ShouldMapButtonsCorrectly()
        {
            // Arrange
            var joystickState = Substitute.For<IJoystickState>();
            joystickState.Buttons.Returns(new bool[12]);
            joystickState.Buttons[0] = true;  // A button pressed
            joystickState.Buttons[1] = false; // B button not pressed
            joystickState.X.Returns(100);
            joystickState.Y.Returns(200);
            joystickState.RotationX.Returns(300);
            joystickState.RotationY.Returns(400);
            joystickState.PointOfViewControllers.Returns(new int[] { 0 });

            // Act
            var controllerData = joystickState.AsControllerData();

            // Assert
            Assert.True(controllerData.A);
            Assert.False(controllerData.B);
            Assert.Equal(100, controllerData.LeftThumbstickX);
            Assert.Equal(200, controllerData.LeftThumbstickY);
            Assert.Equal(300, controllerData.RightThumbstickX);
            Assert.Equal(400, controllerData.RightThumbstickY);
            Assert.True(controllerData.DPadUp);
            Assert.Equal(DateTime.Now.Date, controllerData.Timestamp.Date); // Compare only the date part
        }

        [Fact]
        public void GetTriggerValue_ShouldReturnTriggerMaxValue_WhenButtonIsPressed()
        {
            // Arrange
            var joystickState = Substitute.For<IJoystickState>();
            joystickState.Buttons.Returns(new bool[12]);
            joystickState.Buttons[6] = true; // Left trigger pressed

            // Act
            var triggerValue = SharpDXExtensionMethods.GetTriggerValue(joystickState, 6);

            // Assert
            Assert.Equal(65565, triggerValue);
        }

        [Fact]
        public void GetTriggerValue_ShouldReturnZero_WhenButtonIsNotPressed()
        {
            // Arrange
            var joystickState = Substitute.For<IJoystickState>();
            joystickState.Buttons.Returns(new bool[12]);
            joystickState.Buttons[6] = false; // Left trigger not pressed

            // Act
            var triggerValue = SharpDXExtensionMethods.GetTriggerValue(joystickState, 6);

            // Assert
            Assert.Equal(0, triggerValue);
        }

        [Fact]
        public void GetDPadDirection_ShouldReturnTrue_WhenDPadIsAtSpecifiedAngle()
        {
            // Arrange
            var joystickState = Substitute.For<IJoystickState>();
            joystickState.PointOfViewControllers.Returns(new int[] { 0 }); // DPad pointing Up

            // Act
            var dPadUp = SharpDXExtensionMethods.GetDPadDirection(joystickState, 0);
            var dPadRight = SharpDXExtensionMethods.GetDPadDirection(joystickState, 9000);

            // Assert
            Assert.True(dPadUp);
            Assert.False(dPadRight);
        }
    }
}
