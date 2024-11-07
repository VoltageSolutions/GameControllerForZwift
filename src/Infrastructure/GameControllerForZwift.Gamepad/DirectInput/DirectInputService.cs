using GameControllerForZwift.Core;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputService : IInputService
    {
        private SharpDX.DirectInput.DirectInput _directInput;
        public DirectInputService()
        {
            _directInput = new SharpDX.DirectInput.DirectInput();
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
