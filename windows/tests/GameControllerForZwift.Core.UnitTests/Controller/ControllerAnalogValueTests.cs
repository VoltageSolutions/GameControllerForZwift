using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.Core.Tests.Controller
{
    public class ControllerAnalogValueTests
    {
        [Fact]
        public void Button_IsOneOrZero()
        {
            Assert.Equal(1.0, new ControllerData { A = true }.GetAnalogValue(ControllerInput.A));
            Assert.Equal(0.0, new ControllerData().GetAnalogValue(ControllerInput.A));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5000)]
        [InlineData(-5000)]
        public void Stick_InsideDeadzone_IsZero(double x)
        {
            var d = new ControllerData { LeftThumbstickX = x };
            Assert.Equal(0.0, d.GetAnalogValue(ControllerInput.LeftThumbstickTiltLeft));
            Assert.Equal(0.0, d.GetAnalogValue(ControllerInput.LeftThumbstickTiltRight));
            Assert.False(d.IsPressed(ControllerInput.LeftThumbstickTiltRight));
        }

        [Fact]
        public void Stick_FullTilt_IsOne_AndHalfwayIsRescaled()
        {
            Assert.Equal(1.0, new ControllerData { LeftThumbstickX = 32767 }.GetAnalogValue(ControllerInput.LeftThumbstickTiltRight), 6);
            Assert.Equal(1.0, new ControllerData { RightThumbstickX = -32768 }.GetAnalogValue(ControllerInput.RightThumbstickTiltLeft), 6);
            var mid = (5000 + 32767) / 2.0;
            Assert.Equal(0.5, new ControllerData { LeftThumbstickX = mid }.GetAnalogValue(ControllerInput.LeftThumbstickTiltRight), 3);
        }

        [Fact]
        public void Stick_UpIsNegativeY()
        {
            var d = new ControllerData { LeftThumbstickY = -32767 };
            Assert.Equal(1.0, d.GetAnalogValue(ControllerInput.LeftThumbstickTiltUp), 6);
            Assert.Equal(0.0, d.GetAnalogValue(ControllerInput.LeftThumbstickTiltDown));
        }

        [Fact]
        public void Stick_OnlyDominantAxisCounts()
        {
            var d = new ControllerData { LeftThumbstickX = 20000, LeftThumbstickY = 30000 };
            Assert.Equal(0.0, d.GetAnalogValue(ControllerInput.LeftThumbstickTiltRight));
            Assert.True(d.GetAnalogValue(ControllerInput.LeftThumbstickTiltDown) > 0);
        }

        [Theory]
        [InlineData(0, 0.0)]
        [InlineData(1638, 0.0)]
        [InlineData(32767, 1.0)]
        public void Trigger_Scaling(double raw, double expected)
        {
            Assert.Equal(expected, new ControllerData { RightTrigger = raw }.GetAnalogValue(ControllerInput.RightTrigger), 6);
            Assert.Equal(expected > 0, new ControllerData { LeftTrigger = raw }.IsPressed(ControllerInput.LeftTrigger));
        }

        [Fact]
        public void Trigger_HalfwayIsRescaled()
        {
            var mid = (1638 + 32767) / 2.0;
            Assert.Equal(0.5, new ControllerData { LeftTrigger = mid }.GetAnalogValue(ControllerInput.LeftTrigger), 3);
        }
    }
}
