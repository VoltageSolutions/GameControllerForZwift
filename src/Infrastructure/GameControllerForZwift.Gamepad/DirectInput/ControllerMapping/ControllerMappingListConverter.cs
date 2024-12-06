using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Gamepad.DirectInput.ControllerMapping
{
    public class ControllerMappingListConverter : JsonConverter<ControllerMappingList>
    {
        public override ControllerMappingList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Create the target object
            var controllerMappingList = new ControllerMappingList();

            // We expect the root to be an object with named controllers
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var rootElement = doc.RootElement;

                // Loop through each controller in the JSON object
                foreach (var controller in rootElement.EnumerateObject())
                {
                    // Deserialize the controller mapping data (e.g., "Default", "Xbox 360 Controller")
                    var controllerMappingData = JsonSerializer.Deserialize<ControllerMappingData>(controller.Value.GetRawText(), options);

                    if (controllerMappingData != null)
                    {
                        // Add the controller to the Controllers dictionary
                        controllerMappingList.Controllers.Add(controller.Name, controllerMappingData);
                    }
                }
            }

            return controllerMappingList;
        }

        public override void Write(Utf8JsonWriter writer, ControllerMappingList value, JsonSerializerOptions options)
        {
            // Default behavior (optional, for serialization purposes)
            JsonSerializer.Serialize(writer, value.Controllers, options);
        }
    }
}
