using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;
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
        private readonly IControllerMapping _controllerMapping;
        private readonly SharpDX.DirectInput.DirectInput _directInput;
        private readonly Func<DeviceInstance, IJoystick> _joystickFactory;

        public DirectInputService(ILogger<DirectInputService> logger, IDeviceLookup deviceLookup, IControllerMapping controllerMapping, Func<DeviceInstance, IJoystick> joystickFactory)
        {
            _logger = logger;
            _deviceLookup = deviceLookup;
            _controllerMapping = controllerMapping;
            _directInput = new SharpDX.DirectInput.DirectInput();
            _joystickFactory = joystickFactory;
        }
        public IEnumerable<IController> GetControllers()
        {
            var gamepads = _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
            return gamepads.Select(device =>
            {
                // Get the device name using the DeviceLookup
                string deviceName = _deviceLookup.GetDeviceName(device.ProductGuid);

                // Get the ControllerMap for this device
                ControllerMap controllerMap = _controllerMapping.GetControllerMap(deviceName);

                // Create and return the DirectInputJoystick with the required parameters
                return new DirectInputJoystick(
                    _joystickFactory(device),
                    deviceName,
                    controllerMap);
            });
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
