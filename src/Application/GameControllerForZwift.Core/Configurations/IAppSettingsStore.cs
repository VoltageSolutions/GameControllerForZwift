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
    }
}
