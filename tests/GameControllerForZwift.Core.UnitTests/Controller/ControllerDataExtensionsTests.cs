using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.Core.Tests.Controller
{
    public class ControllerDataExtensionsTests
    {
        [Fact]
        public void IsPressed_ReturnsTrue_WhenButtonIsPressed()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                A = true,
                B = false,
                X = true,
                Y = false
            };

            // Act & Assert
            Assert.True(controllerData.IsPressed(ControllerInput.A));
            Assert.False(controllerData.IsPressed(ControllerInput.B));
            Assert.True(controllerData.IsPressed(ControllerInput.X));
            Assert.False(controllerData.IsPressed(ControllerInput.Y));
        }

        [Fact]
        public void IsPressed_ReturnsTrue_WhenDPadIsPressed()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                DPadUp = true,
                DPadDown = false,
                DPadLeft = true,
                DPadRight = false
            };

            // Act & Assert
            Assert.True(controllerData.IsPressed(ControllerInput.DPadUp));
            Assert.False(controllerData.IsPressed(ControllerInput.DPadDown));
            Assert.True(controllerData.IsPressed(ControllerInput.DPadLeft));
            Assert.False(controllerData.IsPressed(ControllerInput.DPadRight));
        }

        [Fact]
        public void IsPressed_ReturnsTrue_WhenThumbstickClicksArePressed()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftThumbstickClick = true,
                RightThumbstickClick = false
            };

            // Act & Assert
            Assert.True(controllerData.IsPressed(ControllerInput.LeftThumbstickClick));
            Assert.False(controllerData.IsPressed(ControllerInput.RightThumbstickClick));
        }

        [Theory]
        [InlineData(-6000, 0, ControllerInput.LeftThumbstickTiltLeft, true)]    // Left tilt
        [InlineData(6000, 0, ControllerInput.LeftThumbstickTiltRight, true)]   // Right tilt
        [InlineData(0, -6000, ControllerInput.LeftThumbstickTiltUp, true)]     // Up tilt
        [InlineData(0, 6000, ControllerInput.LeftThumbstickTiltDown, true)]    // Down tilt
        [InlineData(-4000, 0, ControllerInput.LeftThumbstickTiltLeft, false)]  // Within deadzone (left)
        [InlineData(0, 4000, ControllerInput.LeftThumbstickTiltDown, false)]   // Within deadzone (down)]
        [InlineData(-6000, 7000, ControllerInput.LeftThumbstickTiltLeft, false)] // Conflicting directions
        public void IsPressed_ReturnsCorrectResult_ForThumbstickTiltDirections(double x, double y, ControllerInput input, bool expectedResult)
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftThumbstickX = x,
                LeftThumbstickY = y
            };

            // Act
            var result = controllerData.IsPressed(input);

            // Assert
            Assert.Equal(expectedResult, result);
        }


        [Fact]
        public void IsPressed_ReturnsTrue_WhenTriggerIsNonZero()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftTrigger = 10000,    // Non-zero value
                RightTrigger = 0        // Zero value
            };

            // Act & Assert
            Assert.True(controllerData.IsPressed(ControllerInput.LeftTrigger));
            Assert.False(controllerData.IsPressed(ControllerInput.RightTrigger));
        }

        [Fact]
        public void IsPressed_ThrowsArgumentOutOfRangeException_WhenInputIsInvalid()
        {
            // Arrange
            var controllerData = new ControllerData();
            var invalidInput = (ControllerInput)999;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => controllerData.IsPressed(invalidInput));
        }
    }
}
