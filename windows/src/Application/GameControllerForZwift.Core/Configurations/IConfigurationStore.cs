namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Saved configurations on disk: one JSON file per configuration in the config folder (CFG-001).
    /// </summary>
    public interface IConfigurationStore
    {
        /// <summary>
        /// Gets the config folder.
        /// </summary>
        string FolderPath { get; }

        /// <summary>
        /// Raised after the store changes the folder, or when <see cref="NotifyExternalChange"/> reports a change made outside the app.
        /// </summary>
        event EventHandler? Changed;

        /// <summary>
        /// Loads every readable configuration. Files that cannot be read are skipped and logged, never deleted (CFG-005, CFG-007).
        /// </summary>
        IReadOnlyList<SavedConfiguration> LoadAll();

        /// <summary>
        /// Reads one configuration file, for example one picked for import.
        /// </summary>
        /// <exception cref="ConfigurationFormatException">The file is not a valid configuration or is from a newer app.</exception>
        /// <exception cref="IOException">The file could not be read.</exception>
        /// <exception cref="UnauthorizedAccessException">The file could not be read.</exception>
        SavedConfiguration ReadFile(string filePath);

        /// <summary>
        /// Saves a configuration and sets its modified time. If <see cref="SavedConfiguration.FilePath"/> is set, that file is
        /// overwritten (CFG-022); otherwise a new file is created with a name per CFG-004. Creates the folder if needed (CFG-002).
        /// </summary>
        /// <returns>The configuration as saved, with its file path and modified time.</returns>
        SavedConfiguration Save(SavedConfiguration configuration);

        /// <summary>
        /// Renames a configuration: changes its name, renames its file per CFG-004 and keeps its id (CFG-025).
        /// </summary>
        SavedConfiguration Rename(SavedConfiguration configuration, string newName);

        /// <summary>
        /// Deletes a configuration's file (CFG-029).
        /// </summary>
        void Delete(SavedConfiguration configuration);

        /// <summary>
        /// Counts the configuration files in the folder, including ones that cannot be read (CFG-035).
        /// </summary>
        int CountFiles();

        /// <summary>
        /// Deletes every configuration file in the folder (CFG-035). Returns how many were deleted.
        /// </summary>
        int DeleteAll();

        /// <summary>
        /// Creates the config folder if it does not exist and returns its path (CFG-034).
        /// </summary>
        string EnsureFolder();

        /// <summary>
        /// Reports that files changed outside the app, so listeners refresh (CFG-006).
        /// </summary>
        void NotifyExternalChange();
    }
}
