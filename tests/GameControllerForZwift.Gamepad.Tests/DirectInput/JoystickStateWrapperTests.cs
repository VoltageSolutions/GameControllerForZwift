using GameControllerForZwift.Gamepad.DirectInput;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.Tests.DirectInput
{
    public class JoystickStateWrapperTests
    {
        [Fact]
        public void Buttons_ShouldReturnWrappedJoystickStateButtons()
        {
            // Arrange
            var joystickState = new JoystickState();
            var wrapper = new JoystickStateWrapper(joystickState);

            // Act
            var buttons = wrapper.Buttons;

            // Assert
            Assert.Equal(joystickState.Buttons, buttons);
        }

        [Fact]
        public void PointOfViewControllers_ShouldReturnWrappedJoystickStatePointOfViewControllers()
        {
            // Arrange
            var joystickState = new JoystickState();
            var wrapper = new JoystickStateWrapper(joystickState);

            // Act
            var povControllers = wrapper.PointOfViewControllers;

            // Assert
            Assert.Equal(joystickState.PointOfViewControllers, povControllers);
        }

        [Fact]
        public void X_ShouldReturnWrappedJoystickStateX()
        {
            // Arrange
            var joystickState = new JoystickState();
            var wrapper = new JoystickStateWrapper(joystickState);

            // Act
            var xValue = wrapper.X;

            // Assert
            Assert.Equal(joystickState.X, xValue);
        }

        [Fact]
        public void Y_ShouldReturnWrappedJoystickStateY()
        {
            // Arrange
            var joystickState = new JoystickState();
            var wrapper = new JoystickStateWrapper(joystickState);

            // Act
            var yValue = wrapper.Y;

            // Assert
            Assert.Equal(joystickState.Y, yValue);
        }

        [Fact]
        public void RotationX_ShouldReturnWrappedJoystickStateRotationX()
        {
            // Arrange
            var joystickState = new JoystickState();
            var wrapper = new JoystickStateWrapper(joystickState);

            // Act
            var rotationX = wrapper.RotationX;

            // Assert
            Assert.Equal(joystickState.RotationX, rotationX);
        }

        [Fact]
        public void RotationY_ShouldReturnWrappedJoystickStateRotationY()
        {
            // Arrange
            var joystickState = new JoystickState();
            var wrapper = new JoystickStateWrapper(joystickState);

            // Act
            var rotationY = wrapper.RotationY;

            // Assert
            Assert.Equal(joystickState.RotationY, rotationY);
        }
    }
}
