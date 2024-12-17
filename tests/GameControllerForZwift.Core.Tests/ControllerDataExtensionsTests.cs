using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Core.Tests
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
                DPad_Up = true,
                DPad_Down = false,
                DPad_Left = true,
                DPad_Right = false
            };

            // Act & Assert
            Assert.True(controllerData.IsPressed(ControllerInput.DPad_Up));
            Assert.False(controllerData.IsPressed(ControllerInput.DPad_Down));
            Assert.True(controllerData.IsPressed(ControllerInput.DPad_Left));
            Assert.False(controllerData.IsPressed(ControllerInput.DPad_Right));
        }

        [Fact]
        public void IsPressed_ReturnsTrue_WhenThumbstickClicksArePressed()
        {
            // Arrange
            var controllerData = new ControllerData
            {
                LeftThumbstick_Click = true,
                RightThumbstick_Click = false
            };

            // Act & Assert
            Assert.True(controllerData.IsPressed(ControllerInput.LeftThumbstick_Click));
            Assert.False(controllerData.IsPressed(ControllerInput.RightThumbstick_Click));
        }

        [Theory]
        [InlineData(-6000, 0, ControllerInput.LeftThumbstick_TiltLeft, true)]    // Left tilt
        [InlineData(6000, 0, ControllerInput.LeftThumbstick_TiltRight, true)]   // Right tilt
        [InlineData(0, -6000, ControllerInput.LeftThumbstick_TiltUp, true)]     // Up tilt
        [InlineData(0, 6000, ControllerInput.LeftThumbstick_TiltDown, true)]    // Down tilt
        [InlineData(-4000, 0, ControllerInput.LeftThumbstick_TiltLeft, false)]  // Within deadzone (left)
        [InlineData(0, 4000, ControllerInput.LeftThumbstick_TiltDown, false)]   // Within deadzone (down)]
        [InlineData(-6000, 7000, ControllerInput.LeftThumbstick_TiltLeft, false)] // Conflicting directions
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
