namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Runtime options for output routing. Registered as a singleton and read on every action, so changes take effect immediately
    /// (starting/stopping the LAN service itself is handled at app startup).
    /// TODO: surface "Use Zwift LAN controller (beta)" on the Settings page and persist it; there is no settings service yet.
    /// </summary>
    public class OutputOptions
    {
        /// <summary>Use Zwift LAN controller (beta).</summary>
        public bool UseZwiftLanController { get; set; } = true;
    }
}
