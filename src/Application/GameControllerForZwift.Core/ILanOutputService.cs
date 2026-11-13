namespace GameControllerForZwift.Core
{
    /// <summary>
    /// An output service that talks to Zwift over the network (Wahoo DirCon emulation) rather than by
    /// simulating keystrokes. It is only usable while Zwift is connected to it.
    /// </summary>
    public interface ILanOutputService : IOutputService
    {
        /// <summary>True while Zwift has an active connection to the emulated controller.</summary>
        bool IsAvailable { get; }

        /// <summary>True if this service has a button mapping for the given function.</summary>
        bool Supports(ZwiftFunction zwiftFunction, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand);

        /// <summary>True if the LAN controller can express this function/action as controller state.</summary>
        bool CanHandle(ZwiftFunction function, ZwiftRiderAction riderAction);

        /// <summary>Pushes the current controller state to Zwift. Must be cheap and non-blocking.</summary>
        Task UpdateAsync(ZwiftControllerState state, CancellationToken ct = default);
    }
}
