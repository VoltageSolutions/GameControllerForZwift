using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.UI.WPF.Dialogs;
using System.Globalization;
using System.IO;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// View model of the Settings page. It identifies the page for view model first navigation and deletes saved
    /// configurations.
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IConfigurationStore _configurationStore;
        private readonly IAppSettingsStore _appSettings;
        private readonly IDialogService _dialogs;

        public SettingsViewModel(IConfigurationStore configurationStore, IAppSettingsStore appSettings, IDialogService dialogService)
        {
            ArgumentNullException.ThrowIfNull(configurationStore);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(dialogService);
            _configurationStore = configurationStore;
            _appSettings = appSettings;
            _dialogs = dialogService;
        }

        /// <summary>
        /// Deletes every saved configuration after a confirmation that states the count (CFG-035), then clears every
        /// last-used entry so all controllers fall back to Default (CFG-036).
        /// </summary>
        [RelayCommand]
        private void DeleteAllConfigurations()
        {
            int count;
            try
            {
                count = _configurationStore.CountFiles();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError("Couldn't delete saved configurations", ex.Message);
                return;
            }

            if (count == 0)
            {
                _dialogs.ShowNotice("No saved configurations", "There are no saved configurations to delete.");
                return;
            }

            var message = count == 1
                ? "Delete 1 saved configuration file? This cannot be undone. The Default configuration stays."
                : string.Create(CultureInfo.InvariantCulture,
                    $"Delete all {count} saved configuration files? This cannot be undone. The Default configuration stays.");
            if (!_dialogs.Confirm("Delete all saved configurations?", message, "Delete all"))
                return;

            // Clear last-used first: deleting notifies the Controller Setup page, which then falls back to Default.
            _appSettings.ClearLastUsed();
            try
            {
                _configurationStore.DeleteAll();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError("Couldn't delete saved configurations", ex.Message);
            }
        }
    }
}
