namespace GameControllerForZwift.Core.Rides
{
    /// <summary>The totals the app has counted over all rides (SCR-RIDE-003).</summary>
    /// <param name="Rides">How many rides have finished.</param>
    /// <param name="Shifts">How many shifts those rides had.</param>
    /// <param name="Since">When the first counted ride started, or null before any ride.</param>
    public sealed record RideStats(int Rides, int Shifts, DateTimeOffset? Since)
    {
        /// <summary>No rides counted yet.</summary>
        public static RideStats None { get; } = new(0, 0, null);
    }
}
