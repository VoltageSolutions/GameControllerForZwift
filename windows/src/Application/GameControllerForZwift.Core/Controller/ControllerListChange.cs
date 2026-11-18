namespace GameControllerForZwift.Core.Controller
{
    /// <summary>
    /// The difference between the controllers on screen and the controllers connected now. The input service returns the
    /// same instance for a controller while it stays connected, so controllers are compared by reference. Two controllers
    /// of the same model, or of models with the same name, are two entries.
    /// </summary>
    public sealed class ControllerListChange
    {
        private ControllerListChange(IReadOnlyList<IController> added, IReadOnlyList<IController> removed, IReadOnlyList<IController> result)
        {
            Added = added;
            Removed = removed;
            Result = result;
        }

        /// <summary>
        /// Gets the controllers that are connected now and were not listed before, in the order the input service returned them.
        /// </summary>
        public IReadOnlyList<IController> Added { get; }

        /// <summary>
        /// Gets the listed controllers that are no longer connected.
        /// </summary>
        public IReadOnlyList<IController> Removed { get; }

        /// <summary>
        /// Gets the list after the change: the controllers that stay, in their order, then the added ones.
        /// </summary>
        public IReadOnlyList<IController> Result { get; }

        /// <summary>
        /// Gets a value indicating whether any controller connected or disconnected.
        /// </summary>
        public bool HasChanges => Added.Count > 0 || Removed.Count > 0;

        /// <summary>
        /// Compares the listed controllers with the connected ones.
        /// </summary>
        /// <param name="listed">The controllers listed now.</param>
        /// <param name="connected">The controllers the input service reports as connected.</param>
        public static ControllerListChange Between(IEnumerable<IController> listed, IEnumerable<IController> connected)
        {
            ArgumentNullException.ThrowIfNull(listed);
            ArgumentNullException.ThrowIfNull(connected);

            var listedItems = listed.ToList();
            var connectedItems = connected.Where(c => c is not null).Distinct(ReferenceEqualityComparer.Instance).Cast<IController>().ToList();
            var listedSet = new HashSet<object>(listedItems, ReferenceEqualityComparer.Instance);
            var connectedSet = new HashSet<object>(connectedItems, ReferenceEqualityComparer.Instance);

            var removed = listedItems.Where(c => !connectedSet.Contains(c)).ToList();
            var added = connectedItems.Where(c => !listedSet.Contains(c)).ToList();
            var result = listedItems.Where(c => connectedSet.Contains(c)).Concat(added).ToList();
            return new ControllerListChange(added, removed, result);
        }

        /// <summary>
        /// Returns whether the list contains this exact controller instance.
        /// </summary>
        public static bool Contains(IEnumerable<IController> controllers, IController? controller)
        {
            ArgumentNullException.ThrowIfNull(controllers);
            return controller is not null && controllers.Any(c => ReferenceEquals(c, controller));
        }
    }
}
