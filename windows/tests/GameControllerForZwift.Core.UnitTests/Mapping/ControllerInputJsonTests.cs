using System.Text.Json;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class ControllerInputJsonTests
    {
        private static InputMapping Read(string input) =>
            JsonSerializer.Deserialize<InputMapping>($"{{\"Input\":{input},\"Function\":\"None\"}}")!;

        [Theory]
        [InlineData("DPad_Up", ControllerInput.DPadUp)]
        [InlineData("DPad_Down", ControllerInput.DPadDown)]
        [InlineData("DPad_Left", ControllerInput.DPadLeft)]
        [InlineData("DPad_Right", ControllerInput.DPadRight)]
        [InlineData("LeftThumbstick_Click", ControllerInput.LeftThumbstickClick)]
        [InlineData("LeftThumbstick_TiltLeft", ControllerInput.LeftThumbstickTiltLeft)]
        [InlineData("LeftThumbstick_TiltRight", ControllerInput.LeftThumbstickTiltRight)]
        [InlineData("LeftThumbstick_TiltUp", ControllerInput.LeftThumbstickTiltUp)]
        [InlineData("LeftThumbstick_TiltDown", ControllerInput.LeftThumbstickTiltDown)]
        [InlineData("RightThumbstick_Click", ControllerInput.RightThumbstickClick)]
        [InlineData("RightThumbstick_TiltLeft", ControllerInput.RightThumbstickTiltLeft)]
        [InlineData("RightThumbstick_TiltRight", ControllerInput.RightThumbstickTiltRight)]
        [InlineData("RightThumbstick_TiltUp", ControllerInput.RightThumbstickTiltUp)]
        [InlineData("RightThumbstick_TiltDown", ControllerInput.RightThumbstickTiltDown)]
        [InlineData("dpad_up", ControllerInput.DPadUp)]
        public void Reads_LegacyUnderscoreNames(string legacy, ControllerInput expected)
            => Assert.Equal(expected, Read($"\"{legacy}\"").Input);

        [Theory]
        [InlineData("DPadUp", ControllerInput.DPadUp)]
        [InlineData("leftthumbstickclick", ControllerInput.LeftThumbstickClick)]
        [InlineData("RightThumbstickTiltDown", ControllerInput.RightThumbstickTiltDown)]
        [InlineData("A", ControllerInput.A)]
        [InlineData("LeftBumper", ControllerInput.LeftBumper)]
        public void Reads_CurrentNames_CaseInsensitive(string name, ControllerInput expected)
            => Assert.Equal(expected, Read($"\"{name}\"").Input);

        [Fact]
        public void Reads_DefinedNumber() => Assert.Equal(ControllerInput.B, Read(((int)ControllerInput.B).ToString(System.Globalization.CultureInfo.InvariantCulture)).Input);

        [Theory]
        [InlineData("\"Nope\"")]
        [InlineData("\"\"")]
        [InlineData("\"1,2\"")]
        [InlineData("\"7\"")]
        [InlineData("999")]
        [InlineData("true")]
        public void UnknownValues_Throw(string json)
            => Assert.Throws<JsonException>(() => Read(json));

        [Fact]
        public void Writes_CurrentNames()
        {
            var json = JsonSerializer.Serialize(new InputMapping(ControllerInput.LeftThumbstickTiltLeft, ZwiftFunction.SteerLeft));
            Assert.Contains("\"Input\":\"LeftThumbstickTiltLeft\"", json, StringComparison.Ordinal);
        }

        [Fact]
        public void Legacy_Profile_RoundTrips_ToNewNames()
        {
            var mapping = Read("\"LeftThumbstick_TiltRight\"");
            var json = JsonSerializer.Serialize(mapping);
            Assert.Contains("LeftThumbstickTiltRight", json, StringComparison.Ordinal);
            Assert.DoesNotContain('_', json.Replace("_legacy", "", StringComparison.Ordinal));
        }
    }
}
