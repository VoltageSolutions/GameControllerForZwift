namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Decides whether a configuration belongs to a connected controller (CFG-011 to CFG-014).
    /// </summary>
    public sealed class ControllerModelMatcher
    {
        /// <summary>
        /// VID/PID pairs that are the same controller model, for example a controller that reports a different PID over
        /// USB and Bluetooth (CFG-013). Each inner list is one model. Starts empty; add entries as real cases appear.
        /// </summary>
        public static readonly IReadOnlyList<IReadOnlyList<UsbId>> KnownAliases = [];

        private readonly Dictionary<UsbId, UsbId> _canonical = new();

        /// <summary>
        /// Creates a matcher that uses <see cref="KnownAliases"/>.
        /// </summary>
        public ControllerModelMatcher() : this(KnownAliases)
        {
        }

        /// <summary>
        /// Creates a matcher with the given alias groups.
        /// </summary>
        public ControllerModelMatcher(IEnumerable<IReadOnlyCollection<UsbId>> aliasGroups)
        {
            ArgumentNullException.ThrowIfNull(aliasGroups);
            foreach (var group in aliasGroups.Where(g => g.Count > 0))
            {
                // The smallest pair stands for the whole group, so the key does not depend on the order of the list.
                var canonical = group.OrderBy(id => id.VendorId).ThenBy(id => id.ProductId).First();
                foreach (var id in group)
                    _canonical[id] = canonical;
            }
        }

        /// <summary>
        /// Returns true if both models are the same controller model.
        /// </summary>
        public bool Matches(ControllerModel first, ControllerModel second)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            return string.Equals(GetKey(first), GetKey(second), StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns a stable key for the model, used for the last-used configuration in settings.json (CFG-017).
        /// Models with USB IDs use the (alias-resolved) IDs; others use the controller name (CFG-014).
        /// </summary>
        public string GetKey(ControllerModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (!model.HasUsbIds)
                return "NAME:" + (model.Name ?? string.Empty).Trim().ToUpperInvariant();

            var id = new UsbId(model.VendorId, model.ProductId);
            if (_canonical.TryGetValue(id, out var canonical))
                id = canonical;
            return $"{ControllerModel.FormatId(id.VendorId)}:{ControllerModel.FormatId(id.ProductId)}";
        }
    }
}
