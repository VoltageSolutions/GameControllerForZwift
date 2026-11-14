namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Snapshot of the virtual controller handed to an <see cref="IOutputService"/>. Steer: -1.0 (full left) .. +1.0 (full right);
    /// Brake: 0.0 .. 1.0; Pressed: digital functions currently active (excluding SteerLeft/SteerRight/Brake);
    /// BatteryPercent: battery level of the physical controller, if known.
    /// </summary>
    public sealed record OutputState(IReadOnlySet<ZwiftFunction> Pressed, double Steer, double Brake, int? BatteryPercent)
    {
        /// <summary>Nothing pressed, no steering, no braking, battery unknown.</summary>
        public static OutputState Neutral { get; } = new(new HashSet<ZwiftFunction>(), 0, 0, null);

        /// <summary>True when nothing is pressed and both analog axes are at rest.</summary>
        public bool IsNeutral => Pressed.Count == 0 && Steer == 0 && Brake == 0;
    }
}
