using GameControllerForZwift.Core;
using SharpDX.DirectInput;
using GameControllerForZwift.Gamepad.USB;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputJoystick : IController
    {
        private DeviceInstance _device;
        private Joystick _joystick;
        DeviceLookup _deviceLookup = new DeviceLookup("./DeviceMap.json");
        public string Name { get { return _deviceLookup.GetDeviceName(_device.ProductGuid); } }

        public DirectInputJoystick(SharpDX.DirectInput.DirectInput directInput, DeviceInstance device)
        {
            _device = device;
            var joystickGuid = _device.InstanceGuid;
            _joystick = new Joystick(directInput, joystickGuid);
        }
        public ControllerData ReadData()
        {
            _joystick.Acquire();
            _joystick.Poll();
            var state = _joystick.GetCurrentState();
            //_joystick.Unacquire();

            return state.AsControllerData();
        }
    }
}
