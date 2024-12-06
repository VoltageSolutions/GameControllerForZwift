using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;

namespace GameControllerForZwift.Gamepad.DirectInput.ControllerMapping
{
    public class ControllerMap
    {
        private readonly ControllerMapping.ControllerMappingData _mappingData;

        public ControllerMap(ControllerMapping.ControllerMappingData mappingData)
        {
            _mappingData = mappingData ?? throw new ArgumentNullException(nameof(mappingData));
        }

        public int? GetButtonIndex(string name)
        {
            return _mappingData.Buttons
                .FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Index;
        }

        public int? GetPointOfViewIndex(string name)
        {
            return _mappingData.PointOfViewControllers
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Index;
        }

        public string? GetAxisMapping(string name)
        {
            return _mappingData.Axes
                .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Mapping;
        }
    }
}
