using System.Text.Json;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class ZwiftFunctionConverterTests
    {
        private static readonly JsonSerializerOptions Options = new() { Converters = { new ZwiftFunctionJsonConverter() } };

        private static ZwiftFunction Read(string json) => JsonSerializer.Deserialize<ZwiftFunction>(json, Options);

        [Fact]
        public void DefinedNumber_ReadsAsEnumValue()
            => Assert.Equal(ZwiftFunction.ShiftUp, Read(((int)ZwiftFunction.ShiftUp).ToString(System.Globalization.CultureInfo.InvariantCulture)));

        [Theory]
        [InlineData("9999")]
        [InlineData("-5")]
        [InlineData("1.5")]
        [InlineData("99999999999")]
        public void UndefinedOrNonIntegerNumber_ReadsAsNone(string json)
            => Assert.Equal(ZwiftFunction.None, Read(json));

        [Theory]
        [InlineData("true")]
        [InlineData("[1,2,3]")]
        [InlineData("{\"a\":1}")]
        public void NonStringNonNumberTokens_ReadAsNone(string json)
            => Assert.Equal(ZwiftFunction.None, Read(json));

        [Theory]
        [InlineData("\"  ShiftUp  \"", ZwiftFunction.ShiftUp)]
        [InlineData("\"riderACTION\"", ZwiftFunction.RideOn)]
        [InlineData("\"12\"", ZwiftFunction.None)]
        [InlineData("\"ShiftUp,ShiftDown\"", ZwiftFunction.None)]
        public void StringVariants(string json, ZwiftFunction expected)
            => Assert.Equal(expected, Read(json));

        [Fact]
        public void Write_UsesName_AndRoundTrips()
        {
            string json = JsonSerializer.Serialize(ZwiftFunction.Powerup, Options);
            Assert.Equal("\"Powerup\"", json);
            Assert.Equal(ZwiftFunction.Powerup, Read(json));
        }
    }
}
