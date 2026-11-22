namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// App settings kept in settings.json in the app data folder.
    /// </summary>
    public interface IAppSettingsStore
    {
        /// <summary>
        /// Returns the id of the last-used configuration for a controller model, or null if there is none (CFG-017).
        /// </summary>
        Guid? GetLastUsed(string modelKey);

        /// <summary>
        /// Records the last-used configuration for a controller model. Null (the Default) removes the entry.
        /// </summary>
        void SetLastUsed(string modelKey, Guid? configurationId);

        /// <summary>
        /// Removes every last-used entry (CFG-036).
        /// </summary>
        void ClearLastUsed();

        /// <summary>
        /// Returns whether the configuration list also shows the configurations of other controller models (CFG-039). False if unset.
        /// </summary>
        bool GetShowOtherControllersConfigurations();

        /// <summary>
        /// Records whether the configuration list also shows the configurations of other controller models (CFG-039).
        /// </summary>
        void SetShowOtherControllersConfigurations(bool show);

        /// <summary>The ride timeout used when none is saved, in minutes (SCR-SET-012).</summary>
        public const int DefaultRideTimeoutMinutes = 10;

        /// <summary>The smallest ride timeout, in minutes (SCR-SET-012).</summary>
        public const int MinimumRideTimeoutMinutes = 5;

        /// <summary>
        /// Returns the saved appearance name (for example "Dark"), or null if none is saved (SCR-SET-002).
        /// </summary>
        string? GetAppearance();

        /// <summary>
        /// Records the appearance name (SCR-SET-002).
        /// </summary>
        void SetAppearance(string appearance);

        /// <summary>
        /// Returns how many minutes without a press or a connection end a ride. It is <see cref="DefaultRideTimeoutMinutes"/>
        /// if unset, and never less than <see cref="MinimumRideTimeoutMinutes"/> (SCR-SET-012).
        /// </summary>
        int GetRideTimeoutMinutes();

        /// <summary>
        /// Records the ride timeout in minutes. A value below <see cref="MinimumRideTimeoutMinutes"/> is raised to it (SCR-SET-012).
        /// </summary>
        void SetRideTimeoutMinutes(int minutes);
    }
}
