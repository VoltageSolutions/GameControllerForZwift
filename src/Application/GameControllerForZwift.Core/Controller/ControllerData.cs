namespace GameControllerForZwift.Core.Controller
{
    public class ControllerData
    {
        /// <summary>
        /// Threshold constants for input deadzones and trigger values.
        /// </summary>
        internal static class Thresholds
        {
            public const int Deadzone = 5000;
            public const int TriggerDeadzone = 1638; // ~5% of full range
            public const int AxisMax = 32767;
            public const int TriggerMaxValue = 32727;
        }

        #region Button Properties

        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Menu { get; set; }
        public bool View { get; set; }
        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }
        public bool LeftBumper { get; set; }
        public bool RightBumper { get; set; }
        public bool LeftThumbstickClick { get; set; }
        public bool RightThumbstickClick { get; set; }

        #endregion

        #region Thumbstick and Trigger Properties

        public double LeftThumbstickX { get; set; }
        public double LeftThumbstickY { get; set; }
        public double LeftTrigger { get; set; }
        public double RightThumbstickX { get; set; }
        public double RightThumbstickY { get; set; }
        public double RightTrigger { get; set; }
        public DateTime Timestamp { get; set; }
        /// <summary>Approximate battery level 0-100, or null if unknown / not reported.</summary>
        public int? BatteryPercent { get; set; }

        #endregion

        #region Left Thumbstick Directions

        public bool LeftStickTiltUp => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Up);
        public bool LeftStickTiltDown => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Down);
        public bool LeftStickTiltLeft => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Left);
        public bool LeftStickTiltRight => IsStickInDirection(LeftThumbstickX, LeftThumbstickY, Direction.Right);

        #endregion

        #region Right Thumbstick Directions

        public bool RightStickTiltUp => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Up);
        public bool RightStickTiltDown => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Down);
        public bool RightStickTiltLeft => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Left);
        public bool RightStickTiltRight => IsStickInDirection(RightThumbstickX, RightThumbstickY, Direction.Right);

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
