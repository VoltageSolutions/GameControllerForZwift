namespace GameControllerForZwift.Gamepad.SDL2
{
    /// <summary>
    /// The devices that connected and disconnected between two enumerations, by SDL instance id.
    /// </summary>
    /// <param name="Added">Instance ids that are present now and were not known before, in enumeration order.</param>
    /// <param name="Removed">Instance ids that were known before and are gone now.</param>
    internal sealed record SdlDeviceChange(IReadOnlyList<int> Added, IReadOnlyList<int> Removed)
    {
        /// <summary>
        /// Gets a value indicating whether any device connected or disconnected.
        /// </summary>
        public bool HasChanges => Added.Count > 0 || Removed.Count > 0;
    }

    /// <summary>
    /// Compares the devices SDL reports now with the ones already known. SDL gives each connection its own instance id,
    /// which stays the same while the device stays connected, so equal ids mean the same open device.
    /// </summary>
    internal static class SdlDeviceSet
    {
        /// <summary>
        /// Returns the devices that connected and disconnected.
        /// </summary>
        /// <param name="known">The instance ids seen in the previous enumeration.</param>
        /// <param name="present">The instance ids SDL reports now, in device index order.</param>
        public static SdlDeviceChange Diff(IReadOnlyCollection<int> known, IReadOnlyList<int> present)
        {
            ArgumentNullException.ThrowIfNull(known);
            ArgumentNullException.ThrowIfNull(present);

            var presentSet = new HashSet<int>(present);
            var knownSet = new HashSet<int>(known);
            var added = present.Where(id => !knownSet.Contains(id)).Distinct().ToList();
            var removed = known.Where(id => !presentSet.Contains(id)).ToList();
            return new SdlDeviceChange(added, removed);
        }
    }
}
