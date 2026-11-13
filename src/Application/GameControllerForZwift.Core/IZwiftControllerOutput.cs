namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Snapshot of the virtual Zwift controller. Steer: -1.0 (full left) .. +1.0 (full right); Brake: 0.0 .. 1.0;
    /// Pressed: digital functions currently active (excluding SteerLeft/SteerRight/Brake).
    /// </summary>
    public sealed record ZwiftControllerState(IReadOnlySet<ZwiftFunction> Pressed, double Steer, double Brake, int? BatteryPercent)
    {
        public static ZwiftControllerState Neutral { get; } = new(new HashSet<ZwiftFunction>(), 0, 0, null);

        public bool IsNeutral => Pressed.Count == 0 && Steer == 0 && Brake == 0;
    }

    /// <summary>A continuous (state based) output, as opposed to <see cref="IOutputService"/> which fires discrete actions.</summary>
    public interface IZwiftControllerOutput
    {
        bool Handles(ZwiftFunction function, ZwiftRiderAction riderAction);

        Task UpdateAsync(ZwiftControllerState state, CancellationToken ct = default);
    }
}
