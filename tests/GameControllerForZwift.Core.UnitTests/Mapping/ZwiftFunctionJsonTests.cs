using System.Text.Json;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class ZwiftFunctionJsonTests
    {
        [Theory]
        [InlineData("FTPBiasUp", ZwiftFunction.ShiftUp)]
        [InlineData("FTPBiasDown", ZwiftFunction.ShiftDown)]
        [InlineData("ftpbiasup", ZwiftFunction.ShiftUp)]
        [InlineData("ShiftUp", ZwiftFunction.ShiftUp)]
        [InlineData("shiftdown", ZwiftFunction.ShiftDown)]
        [InlineData("Brake", ZwiftFunction.Brake)]
        [InlineData("Select", ZwiftFunction.Select)]
        public void Reads_LegacyAndCurrentNames(string name, ZwiftFunction expected)
        {
            var json = $"{{\"Input\":\"A\",\"Function\":\"{name}\",\"PlayerView\":null,\"RiderAction\":null}}";
            var mapping = JsonSerializer.Deserialize<InputMapping>(json)!;
            Assert.Equal(expected, mapping.Function);
            Assert.Equal(ControllerInput.A, mapping.Input);
        }

        [Fact]
        public void Writes_CurrentName()
        {
            var json = JsonSerializer.Serialize(new InputMapping(ControllerInput.A, ZwiftFunction.ShiftUp));
            Assert.Contains("\"Function\":\"ShiftUp\"", json);
            Assert.DoesNotContain("FTPBias", json);
        }

        [Fact]
        public void UnknownName_Throws()
        {
            var json = "{\"Input\":\"A\",\"Function\":\"Nope\"}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InputMapping>(json));
        }
    }
}
