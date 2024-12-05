using GameControllerForZwift.Core;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputJoystick : IController
    {
        #region Fields
        private readonly IJoystick _joystick;
        private bool _isAcquired;
        #endregion

        #region Constructor
        public DirectInputJoystick(IJoystick joystick, string deviceName)
        {
            _joystick = joystick;
            Name = deviceName;
        }
        #endregion

        #region Properties
        public string Name { get; }
        #endregion

        #region Methods
        public ControllerData ReadData()
        {
            if (!_isAcquired) Initialize();
            _joystick.Poll();
            IJoystickState state = _joystick.GetCurrentState();

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
        #endregion
    }
}
