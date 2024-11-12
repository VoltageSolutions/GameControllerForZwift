using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class JoystickWrapper : IJoystick
    {
        private readonly Joystick _joystick;

        public JoystickWrapper(SharpDX.DirectInput.DirectInput directInput, Guid instanceGuid)
        {
            _joystick = new Joystick(directInput, instanceGuid);
        }

        public void Acquire() => _joystick.Acquire();
        public void Poll() => _joystick.Poll();
        public IJoystickState GetCurrentState()
        {
            var state = _joystick.GetCurrentState();
            return new JoystickStateWrapper(state);
        }
    }
}
