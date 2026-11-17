namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Where the app keeps per-user data.
    /// </summary>
    public static class AppDataPaths
    {
        /// <summary>
        /// Gets the app data folder: %LOCALAPPDATA%\GameControllerForZwift.
        /// </summary>
        public static string AppDataFolder =>
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameControllerForZwift");

        /// <summary>
        /// Gets the config folder: %LOCALAPPDATA%\GameControllerForZwift\Profiles.
        /// </summary>
        public static string ConfigurationsFolder => Path.Join(AppDataFolder, "Profiles");

        /// <summary>
        /// Gets the settings file: %LOCALAPPDATA%\GameControllerForZwift\settings.json.
        /// </summary>
        public static string SettingsFile => Path.Join(AppDataFolder, "settings.json");
    }
}
