using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public class WindowsGamepad : IController
    {
        private IGameController _gameController;
        public string Name { get { return "XInput Controller"; } }

        public WindowsGamepad(IGameController gamepad)
        {
            _gameController = gamepad;
        }
        public ControllerData ReadData()
        {
            Gamepad _gamepad = (Gamepad)_gameController;

            var reading = _gamepad.GetCurrentReading();
            return reading.AsControllerData();
        }
    }
}
