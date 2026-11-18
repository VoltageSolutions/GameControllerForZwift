namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>View model of the Connections tab. It shows whether Zwift is connected (SCR-CONN-004).</summary>
    public sealed class ConnectionsViewModel
    {
        public ConnectionsViewModel(ZwiftStatus zwiftStatus)
        {
            ArgumentNullException.ThrowIfNull(zwiftStatus);
            Zwift = zwiftStatus;
        }

        /// <summary>Whether Zwift is connected, updated live.</summary>
        public ZwiftStatus Zwift { get; }
    }
}
