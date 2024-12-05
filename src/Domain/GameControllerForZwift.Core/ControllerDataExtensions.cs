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
                ControllerInput.LeftThumbstickX => controllerData.LeftThumbstickX > 0,
                ControllerInput.LeftThumbstickY => controllerData.LeftThumbstickY > 0,
                ControllerInput.LeftTrigger => controllerData.LeftTrigger > 0,
                ControllerInput.RightThumbstickX => controllerData.RightThumbstickX > 0,
                ControllerInput.RightThumbstickY => controllerData.RightThumbstickY > 0,
                ControllerInput.RightTrigger => controllerData.RightTrigger > 0,
                _ => throw new ArgumentOutOfRangeException(nameof(input), $"Unknown input: {input}")
            };
        }

        public static bool TryGetSingleChange(this ControllerData current, ControllerData? previous, out ControllerInput singleChange)
        {
            singleChange = default;
            int changeCount = 0;

            foreach (var input in Enum.GetValues<ControllerInput>())
            {
                bool currentState = current.IsPressed(input);
                bool previousState = previous?.IsPressed(input) ?? false; // Treat null as "nothing was pressed"

                if (currentState != previousState)
                {
                    changeCount++;
                    singleChange = input;

                    // If more than one change is found, return false
                    if (changeCount > 1)
                    {
                        singleChange = default;
                        return false;
                    }
                }
            }

            // Return true if exactly one change is detected
            return changeCount == 1;
        }
    }

}
