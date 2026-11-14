using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Core.Controller
{
    /// <summary>
    /// Reads/writes <see cref="ControllerInput"/> by name (case-insensitive, like JsonStringEnumConverter). Profiles saved before the
    /// members were renamed used underscored names ("DPad_Up", "LeftThumbstick_TiltLeft", ...); those are mapped to the current names when
    /// reading. Unknown names or numbers throw <see cref="JsonException"/>, exactly as JsonStringEnumConverter did.
    /// </summary>
    public sealed class ControllerInputJsonConverter : JsonConverter<ControllerInput>
    {
        private static readonly Dictionary<string, ControllerInput> LegacyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DPad_Up"] = ControllerInput.DPadUp,
            ["DPad_Down"] = ControllerInput.DPadDown,
            ["DPad_Left"] = ControllerInput.DPadLeft,
            ["DPad_Right"] = ControllerInput.DPadRight,
            ["LeftThumbstick_Click"] = ControllerInput.LeftThumbstickClick,
            ["LeftThumbstick_TiltLeft"] = ControllerInput.LeftThumbstickTiltLeft,
            ["LeftThumbstick_TiltRight"] = ControllerInput.LeftThumbstickTiltRight,
            ["LeftThumbstick_TiltUp"] = ControllerInput.LeftThumbstickTiltUp,
            ["LeftThumbstick_TiltDown"] = ControllerInput.LeftThumbstickTiltDown,
            ["RightThumbstick_Click"] = ControllerInput.RightThumbstickClick,
            ["RightThumbstick_TiltLeft"] = ControllerInput.RightThumbstickTiltLeft,
            ["RightThumbstick_TiltRight"] = ControllerInput.RightThumbstickTiltRight,
            ["RightThumbstick_TiltUp"] = ControllerInput.RightThumbstickTiltUp,
            ["RightThumbstick_TiltDown"] = ControllerInput.RightThumbstickTiltDown,
        };

        public override ControllerInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var number) && Enum.IsDefined(typeof(ControllerInput), number))
                    return (ControllerInput)number;
                throw new JsonException("Unknown ControllerInput value.");
            }
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected a string for ControllerInput.");

            var name = reader.GetString()?.Trim() ?? string.Empty;
            if (LegacyNames.TryGetValue(name, out var legacy))
                return legacy;
            if (!name.Contains(',') && !int.TryParse(name, out _)
                && Enum.TryParse<ControllerInput>(name, ignoreCase: true, out var result) && Enum.IsDefined(result))
                return result;
            throw new JsonException($"Unknown ControllerInput '{name}'.");
        }

        public override void Write(Utf8JsonWriter writer, ControllerInput value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
