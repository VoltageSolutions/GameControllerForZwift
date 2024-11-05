using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public class GamepadService : IInputService
    {
        public IEnumerable<IController> GetControllers()
        {
            return Gamepad.Gamepads.AsControllers();
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
