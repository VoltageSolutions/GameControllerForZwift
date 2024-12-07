using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;
using GameControllerForZwift.Gamepad.USB;
using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public static class SharpDXExtensionMethods
    {
        private const int TriggerMaxValue = 65565;

        public static IEnumerable<IController> AsControllers(
            this IList<DeviceInstance> gamepads,
            Func<DeviceInstance, IJoystick> joystickFactory,
            IDeviceLookup deviceLookup,
            IControllerMapping controllerMapping)
        {
            if (gamepads == null) throw new ArgumentNullException(nameof(gamepads));
            if (joystickFactory == null) throw new ArgumentNullException(nameof(joystickFactory));
            if (deviceLookup == null) throw new ArgumentNullException(nameof(deviceLookup));

            return gamepads.Select(device =>
            {
                // Get the device name using the DeviceLookup
                string deviceName = deviceLookup.GetDeviceName(device.ProductGuid);

                // Get the ControllerMap for this device
                ControllerMap controllerMap = controllerMapping.GetControllerMap(deviceName);

                // Create and return the DirectInputJoystick with the required parameters
                return new DirectInputJoystick(
                    joystickFactory(device),
                    deviceName,
                    controllerMap);
            });
        }

        /// <summary>
        /// Converts a JoystickState to a ControllerData object, mapping button states and stick positions.
        /// </summary>
        /// <param name="state">The JoystickState to convert.</param>
        /// <returns>A populated ControllerData instance.</returns>
        public static ControllerData AsControllerData(this IJoystickState state, ControllerMap controllerMap)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (controllerMap == null) throw new ArgumentNullException(nameof(controllerMap));

            return new ControllerData
            {
                // Button mappings
                A = controllerMap.GetButtonIndex("A").HasValue && state.Buttons[controllerMap.GetButtonIndex("A").Value],
                B = controllerMap.GetButtonIndex("B").HasValue && state.Buttons[controllerMap.GetButtonIndex("B").Value],
                X = controllerMap.GetButtonIndex("X").HasValue && state.Buttons[controllerMap.GetButtonIndex("X").Value],
                Y = controllerMap.GetButtonIndex("Y").HasValue && state.Buttons[controllerMap.GetButtonIndex("Y").Value],
                LeftBumper = controllerMap.GetButtonIndex("LeftBumper").HasValue && state.Buttons[controllerMap.GetButtonIndex("LeftBumper").Value],
                RightBumper = controllerMap.GetButtonIndex("RightBumper").HasValue && state.Buttons[controllerMap.GetButtonIndex("RightBumper").Value],
                View = controllerMap.GetButtonIndex("View").HasValue && state.Buttons[controllerMap.GetButtonIndex("View").Value],
                Menu = controllerMap.GetButtonIndex("Menu").HasValue && state.Buttons[controllerMap.GetButtonIndex("Menu").Value],
                LeftThumbstick = controllerMap.GetButtonIndex("LeftThumbstick").HasValue && state.Buttons[controllerMap.GetButtonIndex("LeftThumbstick").Value],
                RightThumbstick = controllerMap.GetButtonIndex("RightThumbstick").HasValue && state.Buttons[controllerMap.GetButtonIndex("RightThumbstick").Value],

                // Point of View (D-Pad) mappings
                DPadUp = controllerMap.GetPointOfViewIndex("DPadUp").HasValue &&
                 state.PointOfViewControllers.Contains(controllerMap.GetPointOfViewIndex("DPadUp").Value),
                DPadRight = controllerMap.GetPointOfViewIndex("DPadRight").HasValue &&
                    state.PointOfViewControllers.Contains(controllerMap.GetPointOfViewIndex("DPadRight").Value),
                DPadDown = controllerMap.GetPointOfViewIndex("DPadDown").HasValue &&
                   state.PointOfViewControllers.Contains(controllerMap.GetPointOfViewIndex("DPadDown").Value),
                DPadLeft = controllerMap.GetPointOfViewIndex("DPadLeft").HasValue &&
                   state.PointOfViewControllers.Contains(controllerMap.GetPointOfViewIndex("DPadLeft").Value),

                // Axis mappings
                LeftThumbstickX = state.GetAxisValue(controllerMap.GetAxisMapping("LeftThumbstickX")),
                LeftThumbstickY = state.GetAxisValue(controllerMap.GetAxisMapping("LeftThumbstickY")),
                RightThumbstickX = state.GetAxisValue(controllerMap.GetAxisMapping("RightThumbstickX")),
                RightThumbstickY = state.GetAxisValue(controllerMap.GetAxisMapping("RightThumbstickY")),
                LeftTrigger = state.GetAxisValue(controllerMap.GetAxisMapping("LeftTrigger")),
                RightTrigger = state.GetAxisValue(controllerMap.GetAxisMapping("RightTrigger")),

                // Timestamp for when this data was captured
                Timestamp = DateTime.Now
            };
        }

        private static int GetAxisValue(this IJoystickState state, string axisMapping)
        {
            return axisMapping switch
            {
                "X" => state.X,
                "Y" => state.Y,
                "RotationX" => state.RotationX,
                "RotationY" => state.RotationY,
                //"Z" => (state.Z > 32767) ? state.Z : 0,
                "Z" => state.Z,
                "RotationZ" => (state.Z < 32767) ? state.Z : 0,
                _ => 0
            };
        }
    }
}
