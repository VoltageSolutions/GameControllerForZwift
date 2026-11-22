namespace GameControllerForZwift.Core.Rides
{
    /// <summary>What a finished ride leaves behind: when it started, how long it lasted, and how many shifts it had (SCR-RIDE-029).</summary>
    /// <param name="StartedAt">When the ride started.</param>
    /// <param name="Duration">How long the ride lasted, up to the last press or the moment a connection was lost.</param>
    /// <param name="Shifts">How many shifts the controller sent during the ride.</param>
    public sealed record RideSummary(DateTimeOffset StartedAt, TimeSpan Duration, int Shifts);
}
