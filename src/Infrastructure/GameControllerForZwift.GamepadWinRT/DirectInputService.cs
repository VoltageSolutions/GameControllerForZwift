using GameControllerForZwift.Core;
using Windows.Gaming.Input;
using SharpDX.DirectInput;

namespace GameControllerForZwift.GamepadWinRT
{
    public class DirectInputService : IInputService
    {
        private DirectInput _directInput;
        public DirectInputService()
        {
            _directInput = new DirectInput();
        }
        public IEnumerable<IController> GetControllers()
        {
            return _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).AsControllers(_directInput);
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
