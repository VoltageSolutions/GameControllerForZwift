using System;
using Xunit;
using GameControllerForZwift.Core;

namespace GameControllerForZwift.Core.Tests
{
    public class ControllerDataTests
    {
        private const int DeadzoneThreshold = 2000;
        private const int TriggerMaxValue = 32727;

        #region Left Thumbstick Direction Tests

        [Theory]
        [InlineData(0, -6000, true)]   // Up
        [InlineData(0, 6000, false)]   // Not Up
        public void LeftStickUp_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { LeftThumbstickX = x, LeftThumbstickY = y };
            Assert.Equal(expected, controller.LeftStickUp);
        }

        [Theory]
        [InlineData(0, 6000, true)]    // Down
        [InlineData(0, -6000, false)]  // Not Down
        public void LeftStickDown_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { LeftThumbstickX = x, LeftThumbstickY = y };
            Assert.Equal(expected, controller.LeftStickDown);
        }

        [Theory]
        [InlineData(-6000, 0, true)]   // Left
        [InlineData(6000, 0, false)]   // Not Left
        public void LeftStickLeft_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { LeftThumbstickX = x, LeftThumbstickY = y };
            Assert.Equal(expected, controller.LeftStickLeft);
        }

        [Theory]
        [InlineData(6000, 0, true)]    // Right
        [InlineData(-6000, 0, false)]  // Not Right
        public void LeftStickRight_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { LeftThumbstickX = x, LeftThumbstickY = y };
            Assert.Equal(expected, controller.LeftStickRight);
        }

        #endregion

        #region Right Thumbstick Direction Tests

        [Theory]
        [InlineData(0, -6000, true)]   // Up
        [InlineData(0, 6000, false)]   // Not Up
        public void RightStickUp_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { RightThumbstickX = x, RightThumbstickY = y };
            Assert.Equal(expected, controller.RightStickUp);
        }

        [Theory]
        [InlineData(0, 6000, true)]    // Down
        [InlineData(0, -6000, false)]  // Not Down
        public void RightStickDown_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { RightThumbstickX = x, RightThumbstickY = y };
            Assert.Equal(expected, controller.RightStickDown);
        }

        [Theory]
        [InlineData(-6000, 0, true)]   // Left
        [InlineData(6000, 0, false)]   // Not Left
        public void RightStickLeft_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { RightThumbstickX = x, RightThumbstickY = y };
            Assert.Equal(expected, controller.RightStickLeft);
        }

        [Theory]
        [InlineData(6000, 0, true)]    // Right
        [InlineData(-6000, 0, false)]  // Not Right
        public void RightStickRight_ReturnsCorrectValue(double x, double y, bool expected)
        {
            var controller = new ControllerData { RightThumbstickX = x, RightThumbstickY = y };
            Assert.Equal(expected, controller.RightStickRight);
        }

        #endregion

        #region Trigger State Tests

        [Fact]
        public void LeftTriggerFullyPressed_ReturnsTrue_WhenAtMaxValue()
        {
            var controller = new ControllerData { LeftTrigger = TriggerMaxValue };
            Assert.True(controller.LeftTriggerFullyPressed);
        }

        [Fact]
        public void LeftTriggerFullyPressed_ReturnsFalse_WhenNotAtMaxValue()
        {
            var controller = new ControllerData { LeftTrigger = TriggerMaxValue - 1 };
            Assert.False(controller.LeftTriggerFullyPressed);
        }

        [Fact]
        public void RightTriggerFullyPressed_ReturnsTrue_WhenAtMaxValue()
        {
            var controller = new ControllerData { RightTrigger = TriggerMaxValue };
            Assert.True(controller.RightTriggerFullyPressed);
        }

        [Fact]
        public void RightTriggerFullyPressed_ReturnsFalse_WhenNotAtMaxValue()
        {
            var controller = new ControllerData { RightTrigger = TriggerMaxValue - 1 };
            Assert.False(controller.RightTriggerFullyPressed);
        }

        #endregion

        #region Button Press Tests

        [Fact]
        public void ButtonA_ReturnsCorrectValue()
        {
            var controller = new ControllerData { A = true };
            Assert.True(controller.A);
        }

        [Fact]
        public void ButtonB_ReturnsCorrectValue()
        {
            var controller = new ControllerData { B = false };
            Assert.False(controller.B);
        }

        #endregion
    }
}
