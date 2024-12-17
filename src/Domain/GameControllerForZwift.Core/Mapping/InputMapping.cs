using System.Text.Json.Serialization;

namespace GameControllerForZwift.Core.Mapping
{
    public class InputMapping
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ControllerInput Input { get; set; }  // Public setter

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ZwiftFunction Function { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ZwiftPlayerView? PlayerView { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ZwiftRiderAction? RiderAction { get; set; }

        public InputMapping() { }

        public InputMapping(ControllerInput input, ZwiftFunction function, ZwiftPlayerView? playerView = null, ZwiftRiderAction? riderAction = null)
        {
            Input = input;
            Function = function;
            PlayerView = playerView;
            RiderAction = riderAction;
        }
    }
}
