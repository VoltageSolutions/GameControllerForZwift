using GameControllerForZwift.Core;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT
{
    public static class GamepadWinRT
    {
        public static IEnumerable<IController> AsControllers(this IReadOnlyList<Gamepad> gamepads)
        {
            foreach (Gamepad gamepad in Gamepad.Gamepads)
                yield return new WindowsGamepad(gamepad);
        }

        public static ControllerData AsControllerData(this GamepadReading gamepadReading)
        {
            return new ControllerData
            {
                Buttons = gamepadReading.Buttons.ToControllerButtons(),
                LeftThumbstickX = gamepadReading.LeftThumbstickX,
                LeftThumbstickY = gamepadReading.LeftThumbstickY,
                LeftTrigger = gamepadReading.LeftTrigger,
                RightThumbstickX = gamepadReading.RightThumbstickX,
                RightThumbstickY = gamepadReading.RightThumbstickY,
                RightTrigger = gamepadReading.RightTrigger,
                //Timestamp = ConvertFromUnixTimestamp(gamepadReading.Timestamp)
                Timestamp = DateTime.Now
            };
        }

        public static DateTime ConvertFromUnixTimestamp(ulong timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime;
        }

        public static ControllerButtons ToControllerButtons(this GamepadButtons source)
        {
            ControllerButtons target = ControllerButtons.None;

            if (source.HasFlag(GamepadButtons.A))
                target |= ControllerButtons.A;
            if (source.HasFlag(GamepadButtons.B))
                target |= ControllerButtons.B;
            if (source.HasFlag(GamepadButtons.X))
                target |= ControllerButtons.X;
            if (source.HasFlag(GamepadButtons.Y))
                target |= ControllerButtons.Y;
            if (source.HasFlag(GamepadButtons.LeftShoulder))
                target |= ControllerButtons.LeftShoulder;
            if (source.HasFlag(GamepadButtons.RightShoulder))
                target |= ControllerButtons.RightShoulder;
            if (source.HasFlag(GamepadButtons.LeftThumbstick))
                target |= ControllerButtons.LeftThumbstick;
            if (source.HasFlag(GamepadButtons.RightThumbstick))
                target |= ControllerButtons.RightThumbstick;
            if (source.HasFlag(GamepadButtons.DPadUp))
                target |= ControllerButtons.DPadUp;
            if (source.HasFlag(GamepadButtons.DPadDown))
                target |= ControllerButtons.DPadDown;
            if (source.HasFlag(GamepadButtons.DPadLeft))
                target |= ControllerButtons.DPadLeft;
            if (source.HasFlag(GamepadButtons.DPadRight))
                target |= ControllerButtons.DPadRight;
            if (source.HasFlag(GamepadButtons.View))
                target |= ControllerButtons.View;
            if (source.HasFlag(GamepadButtons.Menu))
                target |= ControllerButtons.Menu;

            return target;
        }
    }
}
