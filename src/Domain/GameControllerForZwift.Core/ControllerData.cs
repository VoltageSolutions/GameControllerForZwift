namespace GameControllerForZwift.Core
{
    public class ControllerData
    {
        /// <summary>
        /// Threshold constants for input deadzones and trigger values.
        /// </summary>
        private static class Thresholds
        {
            public const int Deadzone = 5000;
            public const int TriggerMaxValue = 32727;
        }

        #region Button Properties

        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Menu { get; set; }
        public bool View { get; set; }
        public bool DPad_Up { get; set; }
        public bool DPad_Down { get; set; }
        public bool DPad_Left { get; set; }
        public bool DPad_Right { get; set; }
        public bool LeftBumper { get; set; }
        public bool RightBumper { get; set; }
        public bool LeftThumbstick_Click { get; set; }
        public bool RightThumbstick_Click { get; set; }

        #endregion

        #region Thumbstick and Trigger Properties

        public double LeftThumbstickX { get; set; }
        public double LeftThumbstickY { get; set; }
        public double LeftTrigger { get; set; }
        public double RightThumbstickX { get; set; }
        public double RightThumbstickY { get; set; }
        public double RightTrigger { get; set; }
        public DateTime Timestamp { get; set; }

        #endregion

        #region Left Thumbstick Directions

        public bool LeftStick_TiltUp => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Up);
        public bool LeftStick_TiltDown => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Down);
        public bool LeftStick_TiltLeft => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Left);
        public bool LeftStick_TiltRight => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Right);

        #endregion

        #region Right Thumbstick Directions

        public bool RightStick_TiltUp => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Up);
        public bool RightStick_TiltDown => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Down);
        public bool RightStick_TiltLeft => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Left);
        public bool RightStick_TiltRight => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Right);

        #endregion

        #region Trigger States

        public bool LeftTriggerFullyPressed => LeftTrigger == Thresholds.TriggerMaxValue;
        public bool RightTriggerFullyPressed => RightTrigger == Thresholds.TriggerMaxValue;

        #endregion

        #region Helper Methods

        private enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        /// <summary>
        /// Determines if a thumbstick is moved in a specific direction based on deadzone constraints.
        /// </summary>
        private static bool IsStickInDirection(double x, double y, Direction direction)
        {
            return direction switch
            {
                Direction.Up => y < -Thresholds.Deadzone && Math.Abs(y) > Math.Abs(x),
                Direction.Down => y > Thresholds.Deadzone && Math.Abs(y) > Math.Abs(x),
                Direction.Left => x < -Thresholds.Deadzone && Math.Abs(x) > Math.Abs(y),
                Direction.Right => x > Thresholds.Deadzone && Math.Abs(x) > Math.Abs(y),
                _ => false
            };
        }

        #endregion
    }
}
