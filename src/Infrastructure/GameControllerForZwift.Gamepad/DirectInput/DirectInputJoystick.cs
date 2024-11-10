using GameControllerForZwift.Core;
using SharpDX.DirectInput;
using GameControllerForZwift.Gamepad.USB;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputJoystick : IController
    {
        private SharpDX.DirectInput.DirectInput _directInput;
        private DeviceInstance _device;
        private Joystick _joystick;
        private IDeviceLookup _deviceLookup;
        private bool _isAcquired;
        public string Name { get { return _deviceLookup.GetDeviceName(_device.ProductGuid); } }

        public DirectInputJoystick(SharpDX.DirectInput.DirectInput directInput, DeviceInstance device, IDeviceLookup deviceLookup)
        {
            _directInput = directInput;
            _device = device;
            _deviceLookup = deviceLookup;
            var joystickGuid = _device.InstanceGuid;
            _joystick = new Joystick(directInput, joystickGuid);
        }
        public ControllerData ReadData()
        {
            if (!_isAcquired) Initialize();
            _joystick.Poll();
            var state = _joystick.GetCurrentState();
            //_joystick.Unacquire();

            return state.AsControllerData();
        }

        public void Initialize()
        {
            if (!_isAcquired)
            {
                _joystick.Acquire();
                _isAcquired = true;
            }
        }
    }
}
