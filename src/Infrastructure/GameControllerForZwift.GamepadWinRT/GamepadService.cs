using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public class GamepadService : IInputService
    {
        IReadOnlyList<Gamepad> _gamepads;
        public IEnumerable<IController> GetControllers()
        {
            //return _gamepads.AsControllers();
            return Gamepad.Gamepads.AsControllers();
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }

        public GamepadService()
        {
            _gamepads = Gamepad.Gamepads;

            Gamepad.GamepadAdded += Gamepad_CollectionChanged;
            Gamepad.GamepadRemoved += Gamepad_CollectionChanged;
        }

        private void Gamepad_CollectionChanged(object? sender, Gamepad e)
        {
            _gamepads = Gamepad.Gamepads;
        }

        
    }
}
