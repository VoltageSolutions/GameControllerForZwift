using GameControllerForZwift.Core;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputJoystick : IController
    {
        private DeviceInstance _device;
        private Joystick _joystick;
        public string Name { get { return _device.ProductName; } }

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
