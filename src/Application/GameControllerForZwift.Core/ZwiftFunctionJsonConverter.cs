using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Reads/writes <see cref="ZwiftFunction"/> by name (case-insensitive, like JsonStringEnumConverter) and maps the
    /// legacy names "FTPBiasUp"/"FTPBiasDown" from older profiles to ShiftUp/ShiftDown.
    /// </summary>
    public sealed class ZwiftFunctionJsonConverter : JsonConverter<ZwiftFunction>
    {
        public override ZwiftFunction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var number) && Enum.IsDefined(typeof(ZwiftFunction), number))
                    return (ZwiftFunction)number;
                throw new JsonException($"Invalid ZwiftFunction value.");
            }
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected a string for ZwiftFunction.");

            var name = reader.GetString()?.Trim() ?? string.Empty;
            if (name.Equals("FTPBiasUp", StringComparison.OrdinalIgnoreCase))
                return ZwiftFunction.ShiftUp;
            if (name.Equals("FTPBiasDown", StringComparison.OrdinalIgnoreCase))
                return ZwiftFunction.ShiftDown;
            if (Enum.TryParse<ZwiftFunction>(name, ignoreCase: true, out var result) && !int.TryParse(name, out _))
                return result;
            throw new JsonException($"Unknown ZwiftFunction '{name}'.");
        }

        public override void Write(Utf8JsonWriter writer, ZwiftFunction value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
