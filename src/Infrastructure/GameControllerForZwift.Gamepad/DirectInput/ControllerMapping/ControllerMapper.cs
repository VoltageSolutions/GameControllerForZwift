using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;
using GameControllerForZwift.Gamepad.FileSystem;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Gamepad.DirectInput.ControllerMapping
{
    public class ControllerMapper : IControllerMapping
    {
        private readonly ControllerMappingList _controllersList;

        #region Constructors

        public ControllerMapper(string jsonContent)
        {
            _controllersList = GetControllerMappingData(jsonContent);
        }
        public ControllerMapper(IFileService fileService, string filePath) : this(fileService.ReadFileContent(filePath)) { }

        #endregion

        #region Methods

        public ControllerMappingList GetControllerMappingData(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                return new ControllerMappingList();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Make sure the deserialization is case-insensitive
            };

            // Deserialize the entire object at once
            return JsonSerializer.Deserialize<ControllerMappingList>(jsonContent, options) ?? new ControllerMappingList();
        }

        public ControllerMap GetControllerMap(string controllerName)
        {
            return new ControllerMap(_controllersList.Controllers[controllerName]);
        }

        #endregion
    }
}
