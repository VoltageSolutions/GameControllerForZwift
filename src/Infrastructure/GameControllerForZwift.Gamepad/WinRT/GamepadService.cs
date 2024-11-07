using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.Gamepad.WinRT
{
    public class GamepadService : IInputService
    {
        public IEnumerable<IController> GetControllers()
        {
            return Windows.Gaming.Input.Gamepad.Gamepads.AsControllers();
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
