using GameControllerForZwift.Core;
using SharpDX.DirectInput;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public static class GamepadSharpFX
    {
        public static IEnumerable<IController> AsControllers(this IList<DeviceInstance> gamepads, DirectInput directInput)
        {
            foreach (DeviceInstance gamepad in gamepads)
                yield return new DirectInputJoystick(directInput, gamepad);
        }

        public static ControllerData AsControllerData(this JoystickState state)
        {
            return new ControllerData
            {
                A = state.Buttons[0],
                B = state.Buttons[1],
                X = state.Buttons[2],
                Y = state.Buttons[3],
                LeftBumper = state.Buttons[4],
                RightBumper = state.Buttons[5],
                LeftTrigger = state.Buttons[6] ? 65565 : 0, // View on xbox
                RightTrigger = state.Buttons[7] ? 65565 : 0, // Menu on xbox
                View = state.Buttons[8], //left stick l3 on xbox
                Menu = state.Buttons[9], //right stick r3 on xbox
                LeftThumbstick = state.Buttons[10],
                RightThumbstick = state.Buttons[11],
                DPadUp = (state.PointOfViewControllers[0] == 0) ? true : false,
                DPadRight = (state.PointOfViewControllers[0] == 9000) ? true : false,
                DPadDown = (state.PointOfViewControllers[0] == 18000) ? true : false,
                DPadLeft = (state.PointOfViewControllers[0] == 27000) ? true : false,
                LeftThumbstickX = state.X,
                LeftThumbstickY = state.Y,
                RightThumbstickX = state.RotationX,
                RightThumbstickY = state.RotationY,
                Timestamp = DateTime.Now
                //Z for left trigger - 32767 unpressed
                //RotationX for right trigger
            };
        }

        public static DateTime ConvertFromUnixTimestamp(ulong timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime;
        }
    }
}
