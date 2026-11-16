using GameControllerForZwift.Core;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Dialogs
{
    /// <summary>
    /// Shows the app's dialogs as modal windows centered on the main window. While a dialog is open, the selected game
    /// controller can move focus, press buttons and cancel (CFG-038).
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        private readonly IDataIntegrator _dataIntegrator;

        public DialogService(IDataIntegrator dataIntegrator)
        {
            ArgumentNullException.ThrowIfNull(dataIntegrator);
            _dataIntegrator = dataIntegrator;
        }

        public bool Confirm(string title, string message, string confirmText)
        {
            var dialog = new MessageDialogWindow(title, message, confirmText, null, "Cancel");
            ShowModal(dialog);
            return dialog.Result == MessageDialogResult.Primary;
        }

        public UnsavedChangesChoice AskUnsavedChanges(string configurationName)
        {
            var dialog = new MessageDialogWindow(
                "Save your changes?",
                $"'{configurationName}' has unsaved changes. Save them before switching?",
                "Save",
                "Don't save",
                "Cancel");
            ShowModal(dialog);
            return dialog.Result switch
            {
                MessageDialogResult.Primary => UnsavedChangesChoice.Save,
                MessageDialogResult.Secondary => UnsavedChangesChoice.Discard,
                _ => UnsavedChangesChoice.Cancel,
            };
        }

        public string? PromptForName(string title, string message, string initialName, string confirmText, Func<string, string?> validate)
        {
            var dialog = new NamePromptWindow(title, message, initialName, confirmText, validate);
            return ShowModal(dialog) == true ? dialog.EnteredName : null;
        }

        public string? PickConfigurationFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import a configuration",
                Filter = "Configuration files (*.json)|*.json",
                CheckFileExists = true,
                Multiselect = false,
            };
            var owner = GetOwner();
            bool? picked = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
            return picked == true ? dialog.FileName : null;
        }

        public void ShowError(string title, string message)
        {
            ShowModal(new MessageDialogWindow(title, message, null, null, "OK"));
        }

        public void ShowNotice(string title, string message)
        {
            ShowModal(new MessageDialogWindow(title, message, null, null, "OK"));
        }

        public void OpenFolder(string folderPath)
        {
            using var process = Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
        }

        private bool? ShowModal(Window dialog)
        {
            var owner = GetOwner();
            if (owner is not null)
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            using var navigator = new GamepadDialogNavigator(_dataIntegrator, dialog);
            return dialog.ShowDialog();
        }

        private static Window? GetOwner()
        {
            var main = Application.Current?.MainWindow;
            return main is not null && main.IsVisible ? main : null;
        }
    }
}
