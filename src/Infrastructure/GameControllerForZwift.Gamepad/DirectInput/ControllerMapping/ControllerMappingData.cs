namespace GameControllerForZwift.Gamepad.DirectInput.ControllerMapping
{
    public class ControllerMappingData
    {
        public List<ButtonMapping> Buttons { get; set; } = new List<ButtonMapping>();
        public List<PointOfViewMapping> PointOfViewControllers { get; set; } = new List<PointOfViewMapping>();
        public List<AxisMapping> Axes { get; set; } = new List<AxisMapping>();
    }
}
