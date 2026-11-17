using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using System.Text.Json;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    public class ConfigurationSerializerTests
    {
        private const string SpecExample = """
            {
              "formatVersion": 1,
              "id": "6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10",
              "name": "Climbing",
              "controller": {
                "vendorId": "045E",
                "productId": "0B13",
                "name": "Xbox Series X Controller"
              },
              "mappings": [
                { "input": "RightBumper", "function": "ShiftUp" },
                { "input": "LeftBumper", "function": "ShiftDown" }
              ],
              "modified": "2026-11-16T21:15:00Z"
            }
            """;

        [Fact]
        public void Deserialize_ReadsTheSpecExample()
        {
            var configuration = ConfigurationSerializer.Deserialize(SpecExample);

            Assert.Equal(Guid.Parse("6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10"), configuration.Id);
            Assert.Equal("Climbing", configuration.Name);
            Assert.Equal(new ControllerModel(0x045E, 0x0B13, "Xbox Series X Controller"), configuration.Controller);
            Assert.Equal(ZwiftFunction.ShiftUp, configuration.Mappings[ControllerInput.RightBumper]);
            Assert.Equal(ZwiftFunction.ShiftDown, configuration.Mappings[ControllerInput.LeftBumper]);
            Assert.Equal(new DateTimeOffset(2026, 11, 16, 21, 15, 0, TimeSpan.Zero), configuration.Modified);
        }

        [Fact]
        public void CFG010_Deserialize_MissingInputsLoadAsNone()
        {
            var configuration = ConfigurationSerializer.Deserialize(SpecExample);

            Assert.Equal(Enum.GetValues<ControllerInput>().Length, configuration.Mappings.Count);
            Assert.Equal(ZwiftFunction.None, configuration.Mappings[ControllerInput.A]);
            Assert.Equal(ZwiftFunction.None, configuration.Mappings[ControllerInput.RightThumbstickTiltDown]);
        }

        [Fact]
        public void CFG009_Deserialize_UnknownFunctionLoadsAsNoneAndUnknownInputIsIgnored()
        {
            var json = SpecExample
                .Replace("\"ShiftUp\"", "\"WarpDrive\"", StringComparison.Ordinal)
                .Replace("\"LeftBumper\"", "\"TailFin\"", StringComparison.Ordinal);

            var configuration = ConfigurationSerializer.Deserialize(json);

            Assert.Equal(ZwiftFunction.None, configuration.Mappings[ControllerInput.RightBumper]);
            Assert.Equal(ZwiftFunction.None, configuration.Mappings[ControllerInput.LeftBumper]);
            Assert.Equal(Enum.GetValues<ControllerInput>().Length, configuration.Mappings.Count);
        }

        [Fact]
        public void CFG007_Deserialize_RefusesHigherFormatVersionWithClearMessage()
        {
            var json = SpecExample.Replace("\"formatVersion\": 1", "\"formatVersion\": 2", StringComparison.Ordinal);

            var ex = Assert.Throws<ConfigurationFormatException>(() => ConfigurationSerializer.Deserialize(json));
            Assert.True(ex.IsNewerFormat);
            Assert.Contains("newer version", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void CFG007_Deserialize_RequiresFormatVersion()
        {
            var json = SpecExample.Replace("\"formatVersion\": 1,", string.Empty, StringComparison.Ordinal);

            var ex = Assert.Throws<ConfigurationFormatException>(() => ConfigurationSerializer.Deserialize(json));
            Assert.False(ex.IsNewerFormat);
        }

        [Theory]
        [InlineData("")]
        [InlineData("{ not json")]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("{ \"formatVersion\": 1, \"name\": \"x\", \"controller\": {} }")]
        [InlineData("{ \"formatVersion\": 1, \"id\": \"6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10\", \"name\": \" \", \"controller\": {} }")]
        [InlineData("{ \"formatVersion\": 1, \"id\": \"6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10\", \"name\": \"x\" }")]
        [InlineData("{ \"formatVersion\": 1, \"id\": \"not-a-guid\", \"name\": \"x\", \"controller\": {} }")]
        public void CFG005_Deserialize_InvalidFilesThrowFormatException(string json)
        {
            Assert.Throws<ConfigurationFormatException>(() => ConfigurationSerializer.Deserialize(json));
        }

        [Fact]
        public void CFG014_Deserialize_MissingIdsLoadAsZero()
        {
            const string json = """
                { "formatVersion": 1, "id": "6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10", "name": "x", "controller": { "name": "Pad" } }
                """;
            var configuration = ConfigurationSerializer.Deserialize(json);

            Assert.Equal(0, configuration.Controller.VendorId);
            Assert.False(configuration.Controller.HasUsbIds);
        }

        [Fact]
        public void Serialize_RoundTripsAndWritesFormatVersionAndEveryInput()
        {
            var original = new SavedConfiguration(
                Guid.NewGuid(),
                "Climbing",
                new ControllerModel(0x045E, 0x0B13, "Xbox Series X Controller"),
                ConfigurationMappings.Normalize([new(ControllerInput.A, ZwiftFunction.Select), new(ControllerInput.RightTrigger, ZwiftFunction.Brake)]),
                new DateTimeOffset(2026, 11, 16, 21, 15, 0, TimeSpan.Zero));

            var json = ConfigurationSerializer.Serialize(original);
            var copy = ConfigurationSerializer.Deserialize(json);

            using var document = JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty("formatVersion").GetInt32());
            Assert.Equal("045E", document.RootElement.GetProperty("controller").GetProperty("vendorId").GetString());
            Assert.Equal(Enum.GetValues<ControllerInput>().Length, document.RootElement.GetProperty("mappings").GetArrayLength());
            Assert.Equal("2026-11-16T21:15:00Z", document.RootElement.GetProperty("modified").GetString());

            Assert.Equal(original.Id, copy.Id);
            Assert.Equal(original.Name, copy.Name);
            Assert.Equal(original.Controller, copy.Controller);
            Assert.Equal(original.Modified, copy.Modified);
            Assert.True(ConfigurationMappings.AreEqual(original.Mappings, copy.Mappings));
        }
    }
}
