using System.Text.Json;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class ZwiftFunctionJsonTests
    {
        private static InputMapping Read(string json) => JsonSerializer.Deserialize<InputMapping>(json)!;

        [Theory]
        [InlineData("FTPBiasUp", ZwiftFunction.ShiftUp)]
        [InlineData("FTPBiasDown", ZwiftFunction.ShiftDown)]
        [InlineData("ftpbiasup", ZwiftFunction.ShiftUp)]
        [InlineData("ShiftUp", ZwiftFunction.ShiftUp)]
        [InlineData("shiftdown", ZwiftFunction.ShiftDown)]
        [InlineData("Brake", ZwiftFunction.Brake)]
        [InlineData("Select", ZwiftFunction.Select)]
        [InlineData("RideOn", ZwiftFunction.RideOn)]
        [InlineData("ShowMenu", ZwiftFunction.None)]
        [InlineData("Uturn", ZwiftFunction.None)]
        [InlineData("HideHUD", ZwiftFunction.None)]
        [InlineData("SkipWorkoutBlock", ZwiftFunction.None)]
        [InlineData("AdjustCameraAngle", ZwiftFunction.None)]
        [InlineData("Nope", ZwiftFunction.None)]
        [InlineData("", ZwiftFunction.None)]
        public void Reads_LegacyCurrentAndUnknownNames(string name, ZwiftFunction expected)
        {
            var mapping = Read($"{{\"Input\":\"A\",\"Function\":\"{name}\",\"PlayerView\":null,\"RiderAction\":null}}");
            Assert.Equal(expected, mapping.Function);
            Assert.Equal(ControllerInput.A, mapping.Input);
        }

        [Fact]
        public void UnknownNumber_LoadsAsNone()
            => Assert.Equal(ZwiftFunction.None, Read("{\"Input\":\"A\",\"Function\":999}").Function);

        [Fact]
        public void LegacyRiderAction_RideOn_BecomesRideOn()
        {
            Assert.Equal(ZwiftFunction.RideOn, Read("{\"Input\":\"X\",\"Function\":\"RiderAction\",\"RiderAction\":\"RideOn\"}").Function);
            Assert.Equal(ZwiftFunction.RideOn, Read("{\"Input\":\"X\",\"RiderAction\":\"RideOn\",\"Function\":\"RiderAction\"}").Function);
            Assert.Equal(ZwiftFunction.RideOn, Read("{\"Input\":\"X\",\"Function\":\"RiderAction\",\"RiderAction\":null}").Function);
        }

        [Fact]
        public void LegacyRiderAction_OtherAction_BecomesNone()
            => Assert.Equal(ZwiftFunction.None, Read("{\"Input\":\"X\",\"Function\":\"RiderAction\",\"RiderAction\":\"WaveHand\"}").Function);

        [Fact]
        public void LegacyPlayerViewAndRiderActionProperties_AreIgnored()
        {
            var m = Read("{\"Input\":\"A\",\"Function\":\"ShiftUp\",\"PlayerView\":\"Drone\",\"RiderAction\":\"Nice\"}");
            Assert.Equal(ZwiftFunction.ShiftUp, m.Function);
        }

        [Fact]
        public void Writes_CurrentName_WithoutRemovedProperties()
        {
            var json = JsonSerializer.Serialize(new InputMapping(ControllerInput.A, ZwiftFunction.ShiftUp));
            Assert.Contains("\"Function\":\"ShiftUp\"", json);
            Assert.DoesNotContain("FTPBias", json);
            Assert.DoesNotContain("PlayerView", json);
            Assert.DoesNotContain("RiderAction", json);
        }
    }
}
