using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.Navigation;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>View model of the Credits screen (docs/spec/screens/credits.md).</summary>
    public sealed partial class CreditsViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        public CreditsViewModel(INavigationService navigationService, IDialogService dialogService)
        {
            ArgumentNullException.ThrowIfNull(navigationService);
            ArgumentNullException.ThrowIfNull(dialogService);
            _navigation = navigationService;
            _dialogs = dialogService;
        }

        /// <summary>The version line, for example "Version 0.6.0" (SCR-CRD-001).</summary>
        public string VersionText { get; } = Localizer.Format("CreditsVersionFormat", AppVersion.Current);

        /// <summary>Opens the project on GitHub (SCR-CRD-002).</summary>
        [RelayCommand]
        private void OpenGitHub() => _dialogs.OpenUrl(ProjectLinks.GitHub);

        /// <summary>Opens the Credits list as a pushed screen (SCR-CRD-003).</summary>
        [RelayCommand]
        private void OpenCreditsList() => _navigation.NavigateTo<CreditsListViewModel>();

        /// <summary>Opens the License screen as a pushed screen (SCR-CRD-004).</summary>
        [RelayCommand]
        private void OpenLicense() => _navigation.NavigateTo<LicenseViewModel>();
    }
}
