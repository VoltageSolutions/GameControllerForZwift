using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.Gamepad.WinRT
{
    public static class GamepadWinRT
    {
        public static IEnumerable<IController> AsControllers(this IReadOnlyList<Windows.Gaming.Input.Gamepad> gamepads)
        {
            foreach (Windows.Gaming.Input.Gamepad gamepad in Windows.Gaming.Input.Gamepad.Gamepads)
                yield return new WindowsGamepad(gamepad);
        }

        public static ControllerData AsControllerData(this GamepadReading gamepadReading)
        {
            return new ControllerData
            {
                A = gamepadReading.Buttons.HasFlag(GamepadButtons.A),
                B = gamepadReading.Buttons.HasFlag(GamepadButtons.B),
                X = gamepadReading.Buttons.HasFlag(GamepadButtons.X),
                Y = gamepadReading.Buttons.HasFlag(GamepadButtons.Y),
                LeftBumper = gamepadReading.Buttons.HasFlag(GamepadButtons.LeftShoulder),
                RightBumper = gamepadReading.Buttons.HasFlag(GamepadButtons.RightShoulder),
                View = gamepadReading.Buttons.HasFlag(GamepadButtons.View),
                Menu = gamepadReading.Buttons.HasFlag(GamepadButtons.Menu),
                LeftThumbstick = gamepadReading.Buttons.HasFlag(GamepadButtons.LeftThumbstick),
                RightThumbstick = gamepadReading.Buttons.HasFlag(GamepadButtons.RightThumbstick),
                DPadUp = gamepadReading.Buttons.HasFlag(GamepadButtons.DPadUp),
                DPadRight = gamepadReading.Buttons.HasFlag(GamepadButtons.DPadRight),
                DPadDown = gamepadReading.Buttons.HasFlag(GamepadButtons.DPadDown),
                DPadLeft = gamepadReading.Buttons.HasFlag(GamepadButtons.DPadLeft),
                LeftThumbstickX = gamepadReading.LeftThumbstickX,
                LeftThumbstickY = gamepadReading.LeftThumbstickY,
                LeftTrigger = gamepadReading.LeftTrigger,
                RightThumbstickX = gamepadReading.RightThumbstickX,
                RightThumbstickY = gamepadReading.RightThumbstickY,
                RightTrigger = gamepadReading.RightTrigger,
                Timestamp = DateTime.Now
            };
        }

        public static DateTime ConvertFromUnixTimestamp(ulong timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime;
        }
    }
}
