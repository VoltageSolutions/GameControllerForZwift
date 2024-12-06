using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class DirectInputJoystick : IController
    {
        #region Fields
        private readonly IJoystick _joystick;
        private readonly ControllerMap _controllerMap;
        private bool _isAcquired;
        #endregion

        #region Constructor
        public DirectInputJoystick(IJoystick joystick, string deviceName, ControllerMap controllerMap)
        {
            _joystick = joystick;
            _controllerMap = controllerMap;
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

            return state.AsControllerData(_controllerMap);
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
