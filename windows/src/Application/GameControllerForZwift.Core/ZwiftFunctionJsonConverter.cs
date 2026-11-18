using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Reads/writes <see cref="ZwiftFunction"/> by name (case-insensitive, like JsonStringEnumConverter). Tolerant when reading:
    /// legacy "FTPBiasUp"/"FTPBiasDown" map to ShiftUp/ShiftDown, legacy "RiderAction" maps to RideOn (InputMapping then resets it
    /// to None if its RiderAction was something other than RideOn), and any other unknown/removed name or number loads as None.
    /// </summary>
    public sealed class ZwiftFunctionJsonConverter : JsonConverter<ZwiftFunction>
    {
        public override ZwiftFunction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var number) && Enum.IsDefined(typeof(ZwiftFunction), number))
                    return (ZwiftFunction)number;
                return ZwiftFunction.None;
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                return ZwiftFunction.None;
            }

            var name = reader.GetString()?.Trim() ?? string.Empty;
            if (name.Equals("FTPBiasUp", StringComparison.OrdinalIgnoreCase))
                return ZwiftFunction.ShiftUp;
            if (name.Equals("FTPBiasDown", StringComparison.OrdinalIgnoreCase))
                return ZwiftFunction.ShiftDown;
            if (name.Equals("RiderAction", StringComparison.OrdinalIgnoreCase))
                return ZwiftFunction.RideOn;
            if (!name.Contains(',') && Enum.TryParse<ZwiftFunction>(name, ignoreCase: true, out var result) && !int.TryParse(name, out _) && Enum.IsDefined(result))
                return result;
            return ZwiftFunction.None;
        }

        public override void Write(Utf8JsonWriter writer, ZwiftFunction value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
