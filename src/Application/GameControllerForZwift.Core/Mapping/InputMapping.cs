using GameControllerForZwift.Core.Controller;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Core.Mapping
{
    public class InputMapping : IJsonOnDeserialized
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ControllerInput Input { get; set; }  // Public setter

        [JsonConverter(typeof(ZwiftFunctionJsonConverter))]
        public ZwiftFunction Function { get; set; }

        /// <summary>
        /// Read-only compatibility shim for profiles saved before RiderAction was removed (never written). Legacy "PlayerView" is simply ignored.
        /// </summary>
        [JsonPropertyName("RiderAction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? LegacyRiderAction
        {
            get => null;
            set => _legacyRiderAction = value;
        }
        private JsonElement? _legacyRiderAction;

        public InputMapping() { }

        public InputMapping(ControllerInput input, ZwiftFunction function)
        {
            Input = input;
            Function = function;
        }

        void IJsonOnDeserialized.OnDeserialized()
        {
            // A legacy "Function": "RiderAction" became RideOn; only a RiderAction of RideOn (or none, which used to default to RideOn) keeps it.
            if (Function == ZwiftFunction.RideOn
                && _legacyRiderAction is { ValueKind: JsonValueKind.String } v
                && !string.Equals(v.GetString(), "RideOn", StringComparison.OrdinalIgnoreCase))
            {
                Function = ZwiftFunction.None;
            }
            _legacyRiderAction = null;
        }
    }
}
