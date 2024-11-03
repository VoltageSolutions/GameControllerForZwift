using GameControllerForZwift.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public static class GamepadWinRT
    {
        public static IEnumerable<IController> GetControllers
        {
            get
            {
                foreach (Gamepad gamepad in Gamepad.Gamepads)
                    yield return new WindowsGamepad(gamepad);
            }
        }
    }
}
