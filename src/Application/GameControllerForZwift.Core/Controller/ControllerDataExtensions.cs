namespace GameControllerForZwift.Core.Controller
{
    public static class ControllerDataExtensions
    {
        public static bool IsPressed(this ControllerData controllerData, ControllerInput input)
        {
            if (controllerData == null) return false;
            return controllerData.GetAnalogValue(input) > 0;
        }

        /// <summary>
        /// Returns the input's activation as 0..1. Buttons are 0 or 1; stick tilts are the component along that direction
        /// (0 inside the deadzone, rescaled beyond it); triggers are rescaled past a small deadzone.
        /// </summary>
        public static double GetAnalogValue(this ControllerData d, ControllerInput input)
        {
            if (d == null) return 0;
            return input switch
            {
                ControllerInput.A => Digital(d.A),
                ControllerInput.B => Digital(d.B),
                ControllerInput.X => Digital(d.X),
                ControllerInput.Y => Digital(d.Y),
                ControllerInput.Menu => Digital(d.Menu),
                ControllerInput.View => Digital(d.View),
                ControllerInput.DPad_Up => Digital(d.DPad_Up),
                ControllerInput.DPad_Down => Digital(d.DPad_Down),
                ControllerInput.DPad_Left => Digital(d.DPad_Left),
                ControllerInput.DPad_Right => Digital(d.DPad_Right),
                ControllerInput.LeftBumper => Digital(d.LeftBumper),
                ControllerInput.RightBumper => Digital(d.RightBumper),
                ControllerInput.LeftThumbstick_Click => Digital(d.LeftThumbstick_Click),
                ControllerInput.RightThumbstick_Click => Digital(d.RightThumbstick_Click),
                ControllerInput.LeftThumbstick_TiltLeft => d.LeftStick_TiltLeft ? Scale(-d.LeftThumbstickX, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.LeftThumbstick_TiltRight => d.LeftStick_TiltRight ? Scale(d.LeftThumbstickX, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.LeftThumbstick_TiltUp => d.LeftStick_TiltUp ? Scale(-d.LeftThumbstickY, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.LeftThumbstick_TiltDown => d.LeftStick_TiltDown ? Scale(d.LeftThumbstickY, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.RightThumbstick_TiltLeft => d.RightStick_TiltLeft ? Scale(-d.RightThumbstickX, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.RightThumbstick_TiltRight => d.RightStick_TiltRight ? Scale(d.RightThumbstickX, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.RightThumbstick_TiltUp => d.RightStick_TiltUp ? Scale(-d.RightThumbstickY, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.RightThumbstick_TiltDown => d.RightStick_TiltDown ? Scale(d.RightThumbstickY, ControllerData.Thresholds.Deadzone) : 0,
                ControllerInput.LeftTrigger => Scale(d.LeftTrigger, ControllerData.Thresholds.TriggerDeadzone),
                ControllerInput.RightTrigger => Scale(d.RightTrigger, ControllerData.Thresholds.TriggerDeadzone),
                _ => throw new ArgumentOutOfRangeException(nameof(input), $"Unknown input: {input}")
            };
        }

        private static double Digital(bool pressed) => pressed ? 1.0 : 0.0;

        private static double Scale(double value, double deadzone)
        {
            if (value <= deadzone) return 0;
            return Math.Clamp((value - deadzone) / (ControllerData.Thresholds.AxisMax - deadzone), 0.0, 1.0);
        }

        //const float AnalogTolerance = 0.05f; // Tolerance for detecting changes in analog inputs

        //public static bool TryGetSingleChange(this ControllerData current, ControllerData? previous, out ControllerInput singleChange)
        //{
        //    singleChange = default;
        //    int changeCount = 0;

        //    foreach (var input in Enum.GetValues<ControllerInput>())
        //    {
        //        bool hasChanged;

        //        if (IsAnalogInput(input))
        //        {
        //            hasChanged = previous == null || current.HasAnalogValueChanged(previous, input, AnalogTolerance);
        //        }
        //        else
        //        {
        //            bool currentState = current.IsPressed(input);
        //            bool previousState = previous?.IsPressed(input) ?? false;
        //            hasChanged = currentState != previousState;
        //        }

        //        if (hasChanged)
        //        {
        //            changeCount++;
        //            singleChange = input;

        //            if (changeCount > 1)
        //            {
        //                singleChange = default;
        //                return false;
        //            }
        //        }
        //    }

        //    return changeCount == 1;
        //}

        //// Helper to check if the input is an analog input
        //private static bool IsAnalogInput(ControllerInput input) =>
        //    input is ControllerInput.LeftThumbstickX or ControllerInput.LeftThumbstickY
        //            or ControllerInput.RightThumbstickX or ControllerInput.RightThumbstickY
        //            or ControllerInput.LeftTrigger or ControllerInput.RightTrigger;

        //// Extension method to check if the analog value has changed beyond a tolerance
        //public static bool HasAnalogValueChanged(this ControllerData current, ControllerData previous, ControllerInput input, float tolerance)
        //{
        //    return input switch
        //    {
        //        ControllerInput.LeftThumbstickX => Math.Abs(current.LeftThumbstickX - previous.LeftThumbstickX) > tolerance,
        //        ControllerInput.LeftThumbstickY => Math.Abs(current.LeftThumbstickY - previous.LeftThumbstickY) > tolerance,
        //        ControllerInput.RightThumbstickX => Math.Abs(current.RightThumbstickX - previous.RightThumbstickX) > tolerance,
        //        ControllerInput.RightThumbstickY => Math.Abs(current.RightThumbstickY - previous.RightThumbstickY) > tolerance,
        //        ControllerInput.LeftTrigger => Math.Abs(current.LeftTrigger - previous.LeftTrigger) > tolerance,
        //        ControllerInput.RightTrigger => Math.Abs(current.RightTrigger - previous.RightTrigger) > tolerance,
        //        _ => throw new ArgumentOutOfRangeException(nameof(input), $"Unknown analog input: {input}")
        //    };
        //}
    }

}
