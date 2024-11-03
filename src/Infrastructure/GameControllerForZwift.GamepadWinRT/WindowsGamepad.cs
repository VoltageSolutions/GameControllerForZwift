using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public class WindowsGamepad : IController
    {
        private ControllerSpecs? _specifications;
        public ControllerSpecs? Specifications 
        {
            get { return _specifications; }
            private set { _specifications = value; }
        }

        public WindowsGamepad(IGameController gamepad)
        {
            Specifications = new ControllerSpecs { Name = "XInput Controller" };
        }
    }
}
