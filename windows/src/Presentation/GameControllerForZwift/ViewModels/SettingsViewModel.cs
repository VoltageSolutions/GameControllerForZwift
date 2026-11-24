using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.Theme;
using System.Globalization;
using System.IO;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// View model of the Settings page (docs/spec/screens/settings.md). It sets the appearance and the ride timeout,
    /// opens the mappings folder, and deletes saved mappings.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        private readonly IConfigurationStore _configurationStore;
        private readonly IAppSettingsStore _appSettings;
        private readonly IDialogService _dialogs;
        private readonly ThemeService? _themeService;

        public SettingsViewModel(IConfigurationStore configurationStore, IAppSettingsStore appSettings, IDialogService dialogService,
            ThemeService? themeService = null)
        {
            ArgumentNullException.ThrowIfNull(configurationStore);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(dialogService);
            _configurationStore = configurationStore;
            _appSettings = appSettings;
            _dialogs = dialogService;
            _themeService = themeService;
            _appearance = themeService?.Appearance ?? Appearance.MatchWindows;
            _rideTimeoutMinutes = appSettings.GetRideTimeoutMinutes();
            _rideTimeoutText = _rideTimeoutMinutes.ToString(CultureInfo.CurrentCulture);
            _hasSavedMappings = CountSavedMappings() > 0;
            _configurationStore.Changed += (_, _) => RefreshSavedMappings();
        }

        /// <summary>The appearance choices, in the order Settings lists them (SCR-SET-001).</summary>
        public static IReadOnlyList<AppearanceOption> AppearanceOptions { get; } =
        [
            new(Appearance.Light, Localizer.Get("AppearanceLight")),
            new(Appearance.Dark, Localizer.Get("AppearanceDark")),
            new(Appearance.MatchWindows, Localizer.Get("AppearanceMatchWindows")),
        ];

        /// <summary>The chosen appearance. Changing it applies it right away and keeps it (SCR-SET-001, SCR-SET-002).</summary>
        [ObservableProperty]
        private Appearance _appearance;

        partial void OnAppearanceChanged(Appearance value)
        {
            _themeService?.Apply(value);
            _appSettings.SetAppearance(value.ToString());
        }

        /// <summary>The ride timeout in whole minutes (SCR-SET-012).</summary>
        public int RideTimeoutMinutes => _rideTimeoutMinutes;

        private int _rideTimeoutMinutes;

        /// <summary>The text of the ride timeout box. A valid value is kept. A value below the minimum is refused (SCR-SET-013).</summary>
        public string RideTimeoutText
        {
            get => _rideTimeoutText;
            set
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out int minutes)
                    && minutes >= IAppSettingsStore.MinimumRideTimeoutMinutes)
                {
                    _appSettings.SetRideTimeoutMinutes(minutes);
                    _rideTimeoutMinutes = minutes;
                    SetProperty(ref _rideTimeoutText, minutes.ToString(CultureInfo.CurrentCulture));
                    OnPropertyChanged(nameof(RideTimeoutMinutes));
                    RideTimeoutMessage = string.Empty;
                    return;
                }

                // Keep the last valid value, say why, and show that value again.
                RideTimeoutMessage = Localizer.Get("SettingsRideTimeoutMinimum");
                _rideTimeoutText = _rideTimeoutMinutes.ToString(CultureInfo.CurrentCulture);
                OnPropertyChanged(nameof(RideTimeoutText));
            }
        }

        private string _rideTimeoutText;

        /// <summary>The message under the ride timeout box. It is empty while the value is valid.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRideTimeoutMessage))]
        private string _rideTimeoutMessage = string.Empty;

        /// <summary>True when the ride timeout message is showing.</summary>
        public bool HasRideTimeoutMessage => RideTimeoutMessage.Length > 0;

        /// <summary>True while at least one mapping file is saved. Delete all is disabled otherwise (SCR-SET-005).</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteAllConfigurationsCommand))]
        [NotifyPropertyChangedFor(nameof(DeleteDescription))]
        private bool _hasSavedMappings;

        /// <summary>The line under Delete all saved mappings. It says there is nothing to delete when that is so.</summary>
        public string DeleteDescription => HasSavedMappings ? string.Empty : Localizer.Get("SettingsDeleteNone");

        /// <summary>Opens the mappings folder, creating it first (SCR-SET-003, CFG-034).</summary>
        [RelayCommand]
        private void OpenMappingsFolder()
        {
            try
            {
                _dialogs.OpenFolder(_configurationStore.EnsureFolder());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError(Localizer.Get("SettingsOpenFolderErrorTitle"), ex.Message);
            }
        }

        /// <summary>
        /// Deletes every saved mapping after a confirmation that states the count (CFG-035), then clears every
        /// last-used entry so all controllers fall back to Default (CFG-036).
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSavedMappings))]
        private void DeleteAllConfigurations()
        {
            int count;
            try
            {
                count = _configurationStore.CountFiles();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError(Localizer.Get("SettingsDeleteErrorTitle"), ex.Message);
                return;
            }

            if (count == 0)
            {
                RefreshSavedMappings();
                return;
            }

            string message = count == 1
                ? Localizer.Get("SettingsDeleteAllConfirmOne")
                : Localizer.Format("SettingsDeleteAllConfirm", count);
            if (!_dialogs.Confirm(Localizer.Get("SettingsDeleteConfirmTitle"), message, Localizer.Get("SettingsDeleteConfirmButton")))
            {
                return;
            }

            // Clear last-used first: deleting notifies the Controller Setup page, which then falls back to Default.
            _appSettings.ClearLastUsed();
            try
            {
                _configurationStore.DeleteAll();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError(Localizer.Get("SettingsDeleteErrorTitle"), ex.Message);
            }

            RefreshSavedMappings();
        }

        private void RefreshSavedMappings() => HasSavedMappings = CountSavedMappings() > 0;

        private int CountSavedMappings()
        {
            try
            {
                return _configurationStore.CountFiles();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return 0;
            }
        }
    }

    /// <summary>One appearance choice and its label.</summary>
    public sealed record AppearanceOption(Appearance Value, string Label);
}
