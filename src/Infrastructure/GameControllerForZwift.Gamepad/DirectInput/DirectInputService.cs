using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.USB;
using Microsoft.Extensions.Logging;
using SharpDX.DirectInput;
using System.Linq;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputService : IInputService
    {
        private readonly ILogger<DirectInputService> _logger;
        private readonly IDeviceLookup _deviceLookup;
        private readonly SharpDX.DirectInput.DirectInput _directInput;
        private readonly Func<DeviceInstance, IJoystick> _joystickFactory;

        public DirectInputService(ILogger<DirectInputService> logger, IDeviceLookup deviceLookup, Func<DeviceInstance, IJoystick> joystickFactory)
        {
            _logger = logger;
            _deviceLookup = deviceLookup;
            _directInput = new SharpDX.DirectInput.DirectInput();
            _joystickFactory = joystickFactory;
        }
        public IEnumerable<IController> GetControllers()
        {
            var gamepads = _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
            return gamepads.Select(device => new DirectInputJoystick(
                _joystickFactory(device),
                _deviceLookup,
                _deviceLookup.GetDeviceName(device.ProductGuid)));
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
