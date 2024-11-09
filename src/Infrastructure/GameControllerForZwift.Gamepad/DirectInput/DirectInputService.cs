using GameControllerForZwift.Core;
using Microsoft.Extensions.Logging;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputService : IInputService
    {
        private readonly ILogger<DirectInputService> _logger;
        private SharpDX.DirectInput.DirectInput _directInput;
        public DirectInputService(ILogger<DirectInputService> logger)
        {
            _logger = logger;
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
