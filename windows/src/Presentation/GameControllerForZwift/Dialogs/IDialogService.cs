namespace GameControllerForZwift.UI.WPF.Dialogs
{
    /// <summary>
    /// The answer to the unsaved-changes prompt (CFG-024).
    /// </summary>
    public enum UnsavedChangesChoice
    {
        Save,
        Discard,
        Cancel,
    }

    /// <summary>
    /// Dialogs and shell actions the view models need, behind an interface so tests can use fakes.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Asks the user to confirm an action. Returns true if they chose <paramref name="confirmText"/>.
        /// </summary>
        bool Confirm(string title, string message, string confirmText);

        /// <summary>
        /// Asks what to do with unsaved changes: Save, Discard or Cancel (CFG-024).
        /// </summary>
        UnsavedChangesChoice AskUnsavedChanges(string configurationName);

        /// <summary>
        /// Asks for a name. <paramref name="validate"/> runs as the user types and returns an inline message, or null if the
        /// name is valid; the confirm button stays disabled until it is valid (CFG-028). Returns the name, or null if canceled.
        /// </summary>
        string? PromptForName(string title, string message, string initialName, string confirmText, Func<string, string?> validate);

        /// <summary>
        /// Opens a file picker for configuration (.json) files. Returns the path, or null if canceled (CFG-030).
        /// </summary>
        string? PickConfigurationFile();

        /// <summary>
        /// Shows an error message.
        /// </summary>
        void ShowError(string title, string message);

        /// <summary>
        /// Shows an informational message with an OK button.
        /// </summary>
        void ShowNotice(string title, string message);

        /// <summary>
        /// Opens a folder in File Explorer (CFG-034).
        /// </summary>
        void OpenFolder(string folderPath);

        /// <summary>Opens a web address in the default browser.</summary>
        void OpenUrl(string url);

        /// <summary>Puts text on the clipboard. Returns false when the clipboard is not available.</summary>
        bool CopyText(string text);
    }
}
