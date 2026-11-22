using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Localization;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>View model of the Support this project screen (docs/spec/screens/support.md).</summary>
    public sealed partial class SupportViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;

        public SupportViewModel(IDialogService dialogService)
        {
            ArgumentNullException.ThrowIfNull(dialogService);
            _dialogs = dialogService;
        }

        /// <summary>Says that the link was copied. It is empty until then (SCR-SUP-005).</summary>
        [ObservableProperty]
        private string _shareStatus = string.Empty;

        /// <summary>Opens the Ko-fi page (SCR-SUP-002).</summary>
        [RelayCommand]
        private void OpenKofi() => _dialogs.OpenUrl(ProjectLinks.Kofi);

        /// <summary>Opens the project on GitHub so you can star it (SCR-SUP-003).</summary>
        [RelayCommand]
        private void OpenStar() => _dialogs.OpenUrl(ProjectLinks.GitHub);

        /// <summary>Opens the contributing guide (SCR-SUP-004).</summary>
        [RelayCommand]
        private void OpenContributing() => _dialogs.OpenUrl(ProjectLinks.Contributing);

        /// <summary>Copies the link to the project to the clipboard and says so (SCR-SUP-005).</summary>
        [RelayCommand]
        private void CopyShareLink()
        {
            ShareStatus = _dialogs.CopyText(ProjectLinks.GitHub) ? Localizer.Get("SupportShareCopied") : string.Empty;
        }
    }
}
