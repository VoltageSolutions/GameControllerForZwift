using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.Gamepad.WinRT
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
            Windows.Gaming.Input.Gamepad _gamepad = (Windows.Gaming.Input.Gamepad)_gameController;

            var reading = _gamepad.GetCurrentReading();
            return reading.AsControllerData();
        }
    }
}
