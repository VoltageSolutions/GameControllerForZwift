using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.USB;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public static class GamepadSharpDX
    {
        private const int TriggerMaxValue = 65565;

        /// <summary>
        /// Converts a list of DeviceInstance objects to an enumerable of IController instances.
        /// </summary>
        /// <param name="gamepads">The list of DeviceInstance objects representing connected gamepads.</param>
        /// <param name="directInput">The DirectInput instance for interacting with DirectInput devices.</param>
        /// <param name="deviceLookup">Lookup for device names based on IDs.</param>
        /// <returns>An enumerable collection of IController instances.</returns>
        public static IEnumerable<IController> AsControllers(this IList<DeviceInstance> gamepads, SharpDX.DirectInput.DirectInput directInput, IDeviceLookup deviceLookup)
        {
            if (gamepads == null) throw new ArgumentNullException(nameof(gamepads));
            if (directInput == null) throw new ArgumentNullException(nameof(directInput));
            if (deviceLookup == null) throw new ArgumentNullException(nameof(deviceLookup));

            return gamepads.Select(gamepad => new DirectInputJoystick(directInput, gamepad, deviceLookup));
        }

        /// <summary>
        /// Converts a JoystickState to a ControllerData object, mapping button states and stick positions.
        /// </summary>
        /// <param name="state">The JoystickState to convert.</param>
        /// <returns>A populated ControllerData instance.</returns>
        public static ControllerData AsControllerData(this JoystickState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var timestamp = DateTime.Now;

            return new ControllerData
            {
                A = GetButtonState(state, 0),
                B = GetButtonState(state, 1),
                X = GetButtonState(state, 2),
                Y = GetButtonState(state, 3),
                LeftBumper = GetButtonState(state, 4),
                RightBumper = GetButtonState(state, 5),
                LeftTrigger = GetTriggerValue(state, 6),
                RightTrigger = GetTriggerValue(state, 7),
                View = GetButtonState(state, 8),
                Menu = GetButtonState(state, 9),
                LeftThumbstick = GetButtonState(state, 10),
                RightThumbstick = GetButtonState(state, 11),
                DPadUp = GetDPadDirection(state, 0),
                DPadRight = GetDPadDirection(state, 9000),
                DPadDown = GetDPadDirection(state, 18000),
                DPadLeft = GetDPadDirection(state, 27000),
                LeftThumbstickX = state.X,
                LeftThumbstickY = state.Y,
                RightThumbstickX = state.RotationX,
                RightThumbstickY = state.RotationY,
                Timestamp = timestamp
                //RotationX for right trigger
            };
        }

        public static bool GetButtonState(JoystickState state, int index)
        {
            return index < state.Buttons.Length && state.Buttons[index];
        }

        public static int GetTriggerValue(JoystickState state, int index)
        {
            return GetButtonState(state, index) ? TriggerMaxValue : 0;
        }

        public static bool GetDPadDirection(JoystickState state, int angle)
        {
            return state.PointOfViewControllers.Length > 0 && state.PointOfViewControllers[0] == angle;
        }
    }
}
