namespace GameControllerForZwift.Core.Controller
{
    /// <summary>
    /// Decides which controller is selected after controllers connect or disconnect (CTRL-004 to CTRL-007).
    /// </summary>
    public static class ControllerSelection
    {
        /// <summary>
        /// Returns the controller to select after the change.
        /// </summary>
        /// <param name="selected">The selected controller before the change, or null if none is selected.</param>
        /// <param name="change">The controllers that connected and disconnected.</param>
        /// <returns>
        /// The selected controller while it stays connected. If it disconnected, the first controller in the list after the
        /// change. If nothing was selected, the first controller that connected. Null if there is nothing to select.
        /// </returns>
        public static IController? Choose(IController? selected, ControllerListChange change)
        {
            ArgumentNullException.ThrowIfNull(change);

            if (selected is null)
                return change.Added.Count > 0 ? change.Added[0] : null;

            if (ControllerListChange.Contains(change.Result, selected))
                return selected;

            return change.Result.Count > 0 ? change.Result[0] : null;
        }
    }
}
