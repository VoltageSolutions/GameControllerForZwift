using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameControllerForZwift.Gamepad.DirectInput.ControllerMapping
{
    [JsonConverter(typeof(ControllerMappingListConverter))]
    public class ControllerMappingList
    {
        public Dictionary<string, ControllerMappingData> Controllers { get; set; } = new Dictionary<string, ControllerMappingData>();
    }
}
