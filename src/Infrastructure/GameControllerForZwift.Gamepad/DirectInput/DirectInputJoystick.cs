using GameControllerForZwift.Core;
using SharpDX.DirectInput;
using GameControllerForZwift.Gamepad.USB;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputJoystick : IController
    {
        private SharpDX.DirectInput.DirectInput _directInput;
        private DeviceInstance _device;
        private readonly IJoystick _joystick;
        private IDeviceLookup _deviceLookup;
        private bool _isAcquired;
        //public string Name { get { return _deviceLookup.GetDeviceName(_device.ProductGuid); } }
        public string Name { get; }

        public DirectInputJoystick(IJoystick joystick, IDeviceLookup deviceLookup, string deviceName)
        {
            //_directInput = directInput;
            //_device = device;
            _joystick = joystick;
            _deviceLookup = deviceLookup;
            Name = deviceName;
            var joystickGuid = _device.InstanceGuid;
            //_joystick = new Joystick(directInput, joystickGuid);
        }
        public ControllerData ReadData()
        {
            if (!_isAcquired) Initialize();
            _joystick.Poll();
            IJoystickState state = _joystick.GetCurrentState();
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
