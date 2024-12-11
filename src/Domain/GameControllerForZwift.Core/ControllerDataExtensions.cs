namespace GameControllerForZwift.Core
{
    public static class ControllerDataExtensions
    {
        public static bool IsPressed(this ControllerData controllerData, ControllerInput input)
        {
            return input switch
            {
                ControllerInput.A => controllerData.A,
                ControllerInput.B => controllerData.B,
                ControllerInput.X => controllerData.X,
                ControllerInput.Y => controllerData.Y,
                ControllerInput.Menu => controllerData.Menu,
                ControllerInput.View => controllerData.View,
                ControllerInput.DPadUp => controllerData.DPadUp,
                ControllerInput.DPadDown => controllerData.DPadDown,
                ControllerInput.DPadLeft => controllerData.DPadLeft,
                ControllerInput.DPadRight => controllerData.DPadRight,
                ControllerInput.LeftBumper => controllerData.LeftBumper,
                ControllerInput.RightBumper => controllerData.RightBumper,
                ControllerInput.LeftThumbstick => controllerData.LeftThumbstick,
                ControllerInput.RightThumbstick => controllerData.RightThumbstick,
                ControllerInput.LeftThumbstickLeft => controllerData.LeftStickLeft,
                ControllerInput.LeftThumbstickRight => controllerData.LeftStickRight,
                ControllerInput.LeftThumbstickUp => controllerData.LeftStickUp,
                ControllerInput.LeftThumbstickDown => controllerData.LeftStickDown,
                ControllerInput.LeftTrigger => controllerData.LeftTrigger != 0,
                ControllerInput.RightThumbstickLeft => controllerData.RightStickLeft,
                ControllerInput.RightThumbstickRight => controllerData.RightStickRight,
                ControllerInput.RightThumbstickUp => controllerData.RightStickUp,
                ControllerInput.RightThumbstickDown => controllerData.RightStickDown,
                ControllerInput.RightTrigger => controllerData.RightTrigger != 0,
                _ => throw new ArgumentOutOfRangeException(nameof(input), $"Unknown input: {input}")
            };
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
