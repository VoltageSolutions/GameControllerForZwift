using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class JoystickWrapper : IJoystick
    {
        #region Fields
        private readonly Joystick _joystick;
        #endregion
        #region Constructor
        public JoystickWrapper(SharpDX.DirectInput.DirectInput directInput, Guid instanceGuid)
        {
            _joystick = new Joystick(directInput, instanceGuid);
        }
        #endregion
        #region Methods
        public void Acquire() => _joystick.Acquire();
        public void Poll() => _joystick.Poll();
        public IJoystickState GetCurrentState()
        {
            var state = _joystick.GetCurrentState();
            return new JoystickStateWrapper(state);
        }
        #endregion
    }
}
