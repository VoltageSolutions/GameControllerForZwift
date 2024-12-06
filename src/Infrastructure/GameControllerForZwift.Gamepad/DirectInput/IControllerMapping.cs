using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public interface IControllerMapping
    {
        /// <summary>
        /// Gets the mapping configuration for a specific Controller.
        /// </summary>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>
        /// A dictionary representing the button and axis mappings for the specified controller.
        /// If no specific mapping exists, the default mapping is returned.
        /// </returns>
        ControllerMap GetControllerMap(string controllerName);
    }
}
