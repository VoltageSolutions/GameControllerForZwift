using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.USB;
using Microsoft.Extensions.Logging;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputService : IInputService
    {
        private readonly ILogger<DirectInputService> _logger;
        private IDeviceLookup _deviceLookup;
        private SharpDX.DirectInput.DirectInput _directInput;
        public DirectInputService(ILogger<DirectInputService> logger, IDeviceLookup deviceLookup)
        {
            _logger = logger;
            _deviceLookup = deviceLookup;
            _directInput = new SharpDX.DirectInput.DirectInput();
        }
        public IEnumerable<IController> GetControllers()
        {
            return _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).AsControllers(_directInput, _deviceLookup);
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
